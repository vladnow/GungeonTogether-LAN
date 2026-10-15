using System;
using System.Collections.Generic;
using System.IO;
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
        private const string Magic = "GungeonTogether-LAN-dungeon-0.2.2";
        private Socket listener, peer;
        private IAsyncResult connecting;
        private readonly List<byte> incoming = new List<byte>();
        private readonly Queue<byte[]> outgoing = new Queue<byte[]>();
        private int offset, queued;
        private string code, fingerprint;
        private double started, received, sent;
        private readonly Func<double> clock;
        public bool IsHost { get; private set; }
        public bool Connected { get; private set; }
        public bool Active { get { return listener != null || peer != null; } }
        public int BoundPort { get; private set; }
        public string Status { get; private set; }
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

        private void Enqueue(byte[] data)
        {
            if (queued + data.Length + 4 > MaxQueuedBytes) throw new IOException("Peer is too slow; queue limit reached.");
            byte[] frame = new byte[data.Length + 4];
            // Explicit little endian framing, independent of CPU endianness.
            int length = data.Length;
            for (int i = 0; i < 4; i++) frame[i] = (byte)(length >> (8 * i));
            Buffer.BlockCopy(data, 0, frame, 4, data.Length);
            outgoing.Enqueue(frame); queued += frame.Length;
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
                if (Connected && clock() - received > 15) throw new IOException("Friend stopped responding.");
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
                byte[] frame = outgoing.Peek();
                int count = peer.Send(frame, offset, frame.Length - offset, SocketFlags.None);
                if (count == 0) throw new IOException("Socket closed while sending.");
                offset += count; sent = clock();
                if (offset == frame.Length) { queued -= frame.Length; outgoing.Dequeue(); offset = 0; }
            }
        }

        private void Read()
        {
            // Bound both bytes and packets per frame to keep the game responsive.
            for (int n = 0; n < 16 && peer != null && peer.Poll(0, SelectMode.SelectRead); n++) {
                byte[] buffer = new byte[16384];
                int count = peer.Receive(buffer);
                if (count == 0) throw new IOException("Friend closed the connection.");
                for (int i = 0; i < count; i++) incoming.Add(buffer[i]);
                if (incoming.Count > 2097152) throw new IOException("Receive buffer limit reached.");
            }
            for (int n = 0; n < 64 && peer != null && incoming.Count >= 4; n++) {
                int length = incoming[0] | incoming[1] << 8 | incoming[2] << 16 | incoming[3] << 24;
                if (length < 0 || length > MaxFrame) throw new IOException("Invalid frame length.");
                if (incoming.Count < length + 4) return;
                byte[] data = incoming.GetRange(4, length).ToArray();
                incoming.RemoveRange(0, length + 4);
                // Count only complete frames for the inactivity deadline.
                received = clock();
                if (!Connected) { if (length == 0) throw new IOException("Missing handshake."); AcceptHandshake(data); }
                else if (length != 0 && PacketReceived != null) PacketReceived(data);
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
            incoming.Clear(); outgoing.Clear(); offset = queued = 0;
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

