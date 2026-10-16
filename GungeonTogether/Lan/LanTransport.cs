using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GungeonTogether.Lan
{
    // Poll only from Unity Update. No socket callback touches game objects.
    public sealed class LanTransport : IDisposable
    {
        public const int DefaultPort = 27631;
        public const int Protocol = 1;
        public const int MaxFrame = 524288;
        private const int MaxQueuedBytes = 2097152;
        private const int CompressedFrame = 0x40000000;
        private const string Magic = "GungeonTogether-LAN-dungeon-0.3.4";
        private Socket listener, peer;
        private IAsyncResult connecting;
        private byte[] incoming = new byte[65536];
        private readonly byte[] readBuffer = new byte[16384];
        private int readStart, readEnd;
        private sealed class Outbound { public byte[] Frame; public int Key; public double EnqueuedAt; }
        private readonly LinkedList<Outbound> outgoing = new LinkedList<Outbound>();
        private readonly Dictionary<int, LinkedListNode<Outbound>> latest = new Dictionary<int, LinkedListNode<Outbound>>();
        private int offset, queued;
        private string code, fingerprint;
        private double started, received, sent;
        private readonly Func<double> clock;
        public bool IsHost { get; private set; }
        public bool Connected { get; private set; }
        public bool Active { get { return listener != null || peer != null; } }
        public int BoundPort { get; private set; }
        public string Status { get; private set; }
        public int PendingBytes { get { return Math.Max(0, queued - offset); } }
        public int PendingFrames { get { return outgoing.Count; } }
        public long ReplacedFrames { get; private set; }
        public long WireBytesSent { get; private set; }
        public long PayloadBytesQueued { get; private set; }
        public double OldestPendingMilliseconds { get { return outgoing.Count == 0 ? 0 : Math.Max(0, (clock() - outgoing.First.Value.EnqueuedAt) * 1000); } }
        public double RoundTripMilliseconds { get; private set; }
        public double InactivityTimeout = 30;
        private double nextPing, pingStarted;
        private uint pingSequence, waitingPing;
        public event Action<byte[]> PacketReceived;
        public event Action ConnectedEvent;
        public event Action DisconnectedEvent;

        public LanTransport() : this(delegate { return DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond; }) { }
        public LanTransport(Func<double> clock) { this.clock = clock; Status = "Offline"; }

        private void Prepare(string sessionCode, string gameFingerprint)
        {
            Dispose();
            if (sessionCode == null || sessionCode.Length < 6 || sessionCode.Length > 64)
                throw new ArgumentException("Use a session code of 6 to 64 characters.");
            if (gameFingerprint == null || gameFingerprint.Length < 1 || gameFingerprint.Length > 256)
                throw new ArgumentException("Invalid game fingerprint.");
            code = sessionCode; fingerprint = gameFingerprint;
        }

        public void Host(string bindAddress, int port, string sessionCode, string gameFingerprint)
        {
            Prepare(sessionCode, gameFingerprint);
            IPAddress address;
            if (!IPAddress.TryParse(bindAddress, out address) || address.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("Enter an IPv4 bind address.");
            if (port < 0 || port > 65535) throw new ArgumentException("Invalid port.");
            IsHost = true;
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try {
                listener.ExclusiveAddressUse = true;
                listener.Bind(new IPEndPoint(address, port));
                listener.Listen(2);
                listener.Blocking = false;
                BoundPort = ((IPEndPoint)listener.LocalEndPoint).Port;
                Status = "Hosting on " + bindAddress + ":" + BoundPort + "; waiting for friend";
            } catch { Dispose(); throw; }
        }

        public void Join(string hostAddress, int port, string sessionCode, string gameFingerprint)
        {
            Prepare(sessionCode, gameFingerprint);
            IPAddress address;
            if (!IPAddress.TryParse(hostAddress, out address) || address.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("Enter the host's IPv4 address (no hostname).");
            if (port < 1 || port > 65535) throw new ArgumentException("Invalid port.");
            IsHost = false;
            peer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            peer.NoDelay = true;
            started = clock();
            try {
                connecting = peer.BeginConnect(new IPEndPoint(address, port), null, null);
                Status = "Connecting to " + hostAddress + ":" + port;
            } catch { Dispose(); throw; }
        }

        private void BeginHandshake()
        {
            peer.Blocking = false;
            peer.NoDelay = true;
            // Bound kernel-side stale state as well as our application queue.
            peer.SendBufferSize = 16384;
            started = received = sent = clock();
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream)) {
                writer.Write(Magic); writer.Write(Protocol); writer.Write(IsHost);
                writer.Write(fingerprint); writer.Write(code);
                Enqueue(stream.ToArray());
            }
            Status = "Checking session code and game version";
        }

        public void Send(byte[] data)
        {
            if (!Connected) return;
            if (data == null || data.Length == 0 || data.Length > MaxFrame)
                throw new ArgumentException("Invalid packet length.");
            Enqueue(data);
        }

        // Full snapshots are replaceable state. Reliable scene/asset messages
        // retain their order; a frame already partially written is immutable.
        public void SendLatest(byte[] data)
        {
            if (!Connected) return;
            if (data == null || data.Length == 0 || data.Length > MaxFrame) throw new ArgumentException("Invalid packet length.");
            Enqueue(data, data[0] + 1, true);
        }

        private void Enqueue(byte[] data, int key = 0, bool compress = false)
        {
            byte[] payload = data;
            bool compressed = false;
            if (compress && data.Length >= 1024) {
                using (MemoryStream stream = new MemoryStream()) {
                    using (DeflateStream zipper = new DeflateStream(stream, CompressionMode.Compress, true)) zipper.Write(data, 0, data.Length);
                    if (stream.Length + 16 < data.Length) { payload = stream.ToArray(); compressed = true; }
                }
            }
            LinkedListNode<Outbound> previous;
            if (key != 0 && latest.TryGetValue(key, out previous)) {
                if (previous != outgoing.First || offset == 0) {
                    queued -= previous.Value.Frame.Length; outgoing.Remove(previous); ReplacedFrames++;
                }
                latest.Remove(key);
            }
            if (queued + payload.Length + 4 > MaxQueuedBytes) throw new IOException("Peer is too slow; queue limit reached.");
            byte[] frame = new byte[payload.Length + 4];
            // Explicit little endian framing, independent of CPU endianness.
            int length = payload.Length | (compressed ? CompressedFrame : 0);
            for (int i = 0; i < 4; i++) frame[i] = (byte)(length >> (8 * i));
            Buffer.BlockCopy(payload, 0, frame, 4, payload.Length);
            LinkedListNode<Outbound> node = outgoing.AddLast(new Outbound { Frame = frame, Key = key, EnqueuedAt = clock() });
            if (key != 0) latest[key] = node;
            queued += frame.Length; PayloadBytesQueued += data.Length;
        }

        public void Poll()
        {
            try {
                if (listener != null && listener.Poll(0, SelectMode.SelectRead)) {
                    Socket accepted = listener.Accept();
                    if (peer != null) accepted.Close(); // Strictly two players.
                    else { peer = accepted; BeginHandshake(); }
                }
                if (peer == null) return;
                if (connecting != null) {
                    if (clock() - started > 10) throw new IOException("Connection timed out. Check IP, port and firewall.");
                    if (!connecting.IsCompleted) return;
                    peer.EndConnect(connecting); connecting = null; BeginHandshake();
                }
                if (!Connected && clock() - started > 10) throw new IOException("Handshake timed out.");
                if (Connected && clock() - received > InactivityTimeout) throw new IOException("Friend stopped responding.");
                if (Connected && clock() >= nextPing) {
                    nextPing = clock() + 1;
                    if (waitingPing == 0) {
                        waitingPing = ++pingSequence; if (waitingPing == 0) waitingPing = ++pingSequence;
                        pingStarted = clock(); Enqueue(Ping(1, waitingPing));
                    }
                }
                if (Connected && clock() - sent > 2 && outgoing.Count == 0) Enqueue(new byte[0]);
                Flush();
                Read();
            } catch (Exception ex) {
                if (ex is SocketException && IsWouldBlock((SocketException)ex)) return;
                Drop("Disconnected: " + ex.Message);
            }
        }

        private static bool IsWouldBlock(SocketException ex)
        { return ex.SocketErrorCode == SocketError.WouldBlock || ex.SocketErrorCode == SocketError.IOPending || ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable; }

        private void Flush()
        {
            for (int budget = 0; budget < 64 && outgoing.Count != 0; budget++) {
                if (!peer.Poll(0, SelectMode.SelectWrite)) return;
                LinkedListNode<Outbound> first = outgoing.First;
                byte[] frame = first.Value.Frame;
                int count = peer.Send(frame, offset, frame.Length - offset, SocketFlags.None);
                if (count == 0) throw new IOException("Socket closed while sending.");
                offset += count; WireBytesSent += count; sent = clock();
                if (offset == frame.Length) {
                    LinkedListNode<Outbound> mapped;
                    if (first.Value.Key != 0 && latest.TryGetValue(first.Value.Key, out mapped) && mapped == first) latest.Remove(first.Value.Key);
                    queued -= frame.Length; outgoing.RemoveFirst(); offset = 0;
                }
            }
        }

        public void FlushPending()
        {
            if (peer == null || connecting != null) return;
            try { Flush(); }
            catch (Exception ex) { if (!(ex is SocketException) || !IsWouldBlock((SocketException)ex)) Drop("Disconnected: " + ex.Message); }
        }

        private static byte[] Ping(byte kind, uint sequence)
        {
            byte[] data = new byte[6]; data[0] = 0; data[1] = kind;
            for (int i = 0; i < 4; i++) data[i + 2] = (byte)(sequence >> (8 * i));
            return data;
        }

        private bool HandlePing(byte[] data)
        {
            if (data.Length != 6 || data[0] != 0 || (data[1] != 1 && data[1] != 2)) return false;
            uint sequence = (uint)(data[2] | data[3] << 8 | data[4] << 16 | data[5] << 24);
            if (data[1] == 1) Enqueue(Ping(2, sequence));
            else if (waitingPing != 0 && sequence == waitingPing) {
                RoundTripMilliseconds = Math.Max(0, (clock() - pingStarted) * 1000); waitingPing = 0;
            }
            return true;
        }

        private void Read()
        {
            // Bound both bytes and packets per frame to keep the game responsive.
            for (int n = 0; n < 16 && peer != null && peer.Poll(0, SelectMode.SelectRead); n++) {
                int count = peer.Receive(readBuffer);
                if (count == 0) throw new IOException("Friend closed the connection.");
                int retained = readEnd - readStart;
                if (retained + count > 2097152) throw new IOException("Receive buffer limit reached.");
                if (readEnd + count > incoming.Length) {
                    if (retained + count > incoming.Length) Array.Resize(ref incoming, Math.Min(2097152, Math.Max(incoming.Length * 2, retained + count)));
                    Buffer.BlockCopy(incoming, readStart, incoming, 0, retained); readStart = 0; readEnd = retained;
                }
                Buffer.BlockCopy(readBuffer, 0, incoming, readEnd, count); readEnd += count;
            }
            for (int n = 0; n < 128 && peer != null && readEnd - readStart >= 4; n++) {
                int header = incoming[readStart] | incoming[readStart + 1] << 8 | incoming[readStart + 2] << 16 | incoming[readStart + 3] << 24;
                bool compressed = (header & CompressedFrame) != 0;
                int length = header & ~CompressedFrame;
                if (length < 0 || length > MaxFrame) throw new IOException("Invalid frame length.");
                if (readEnd - readStart < length + 4) return;
                byte[] data = new byte[length]; Buffer.BlockCopy(incoming, readStart + 4, data, 0, length);
                readStart += length + 4;
                if (readStart == readEnd) readStart = readEnd = 0;
                if (compressed) data = Inflate(data);
                // Count only complete frames for the inactivity deadline.
                received = clock();
                if (!Connected) { if (length == 0) throw new IOException("Missing handshake."); AcceptHandshake(data); }
                else if (data.Length != 0 && !HandlePing(data) && PacketReceived != null) PacketReceived(data);
            }
        }

        private static byte[] Inflate(byte[] data)
        {
            using (MemoryStream input = new MemoryStream(data)) using (DeflateStream zipper = new DeflateStream(input, CompressionMode.Decompress)) using (MemoryStream output = new MemoryStream()) {
                byte[] buffer = new byte[8192]; int read;
                while ((read = zipper.Read(buffer, 0, buffer.Length)) > 0) {
                    if (output.Length + read > MaxFrame) throw new IOException("Decompressed frame limit reached.");
                    output.Write(buffer, 0, read);
                }
                if (output.Length == 0) throw new IOException("Empty compressed frame.");
                return output.ToArray();
            }
        }

        private void AcceptHandshake(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8)) {
                if (reader.ReadString() != Magic || reader.ReadInt32() != Protocol || reader.ReadBoolean() == IsHost)
                    throw new IOException("LAN mod version/role mismatch.");
                if (reader.ReadString() != fingerprint) throw new IOException("Game files differ; use the same game build.");
                if (reader.ReadString() != code) throw new IOException("Wrong session code.");
                if (stream.Position != stream.Length) throw new IOException("Malformed handshake.");
            }
            Connected = true; Status = "Connected (2/2): authoritative dungeon preview";
            if (ConnectedEvent != null) ConnectedEvent();
        }

        private void Drop(string status)
        {
            bool hadPeer = peer != null;
            if (peer != null) { try { peer.Close(); } catch { } }
            peer = null; connecting = null; Connected = false;
            outgoing.Clear(); latest.Clear(); readStart = readEnd = offset = queued = 0;
            waitingPing = 0; nextPing = 0; RoundTripMilliseconds = 0;
            Status = status + (listener != null ? " Waiting for another connection." : "");
            if (hadPeer && DisconnectedEvent != null) DisconnectedEvent();
        }

        public void Dispose()
        {
            if (listener != null) { try { listener.Close(); } catch { } }
            listener = null;
            Drop("Offline");
            BoundPort = 0;
        }
    }
}

