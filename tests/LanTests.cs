using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using GungeonTogether.Lan;

internal static class LanTests
{
    private static int passed;
    private const string Fingerprint = "test-game-build";
    private static void Assert(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        passed++; Console.WriteLine("PASS: " + name);
    }
    private static void Pump(LanTransport host, LanTransport client, Func<bool> done)
    {
        Stopwatch watch = Stopwatch.StartNew();
        do { host.Poll(); if (client != null) client.Poll(); if (done()) return; Thread.Sleep(1); } while (watch.ElapsedMilliseconds < 3000);
        throw new Exception("Pump timeout: " + host.Status + " / " + (client == null ? "raw peer" : client.Status));
    }
    private static void Pair(LanTransport host, LanTransport client)
    {
        host.Host("127.0.0.1", 0, "abcdef", Fingerprint);
        client.Join("127.0.0.1", host.BoundPort, "abcdef", Fingerprint);
        Pump(host, client, delegate { return host.Connected && client.Connected; });
    }
    private static byte[] Frame(byte[] payload)
    {
        byte[] frame = new byte[payload.Length + 4];
        for (int i = 0; i < 4; i++) frame[i] = (byte)(payload.Length >> (i * 8));
        Buffer.BlockCopy(payload, 0, frame, 4, payload.Length); return frame;
    }
    private static byte[] Hello(string game, string code, string magic, int protocol, bool host)
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms)) {
            writer.Write(magic); writer.Write(protocol); writer.Write(host); writer.Write(game); writer.Write(code); return Frame(ms.ToArray());
        }
    }
    private static readonly string Magic = "GungeonTogether-LAN-dungeon-0.3.0";
    private static void Reject(string game, string code, string magic, int protocol, bool role, string expected, string name)
    {
        using (LanTransport host = new LanTransport()) {
            host.Host("127.0.0.1", 0, "abcdef", Fingerprint);
            using (TcpClient raw = new TcpClient("127.0.0.1", host.BoundPort)) {
                byte[] hello = Hello(game, code, magic, protocol, role);
                raw.GetStream().Write(hello, 0, hello.Length);
                Pump(host, null, delegate { return host.Status.Contains(expected); });
                Assert(!host.Connected && host.Active, name);
            }
        }
    }
    private static void Codec()
    {
        LanPlayerState input = new LanPlayerState { InFoyer = true, X = 12.25f, Y = -4f, Rotation = 90, SpriteId = 7, FlipX = true, Collection = "PlayerMarine" };
        LanPlayerState output = LanPlayerState.Decode(input.Encode());
        Assert(output.X == input.X && output.Y == input.Y && output.Rotation == input.Rotation && output.SpriteId == 7 && output.FlipX && output.InFoyer && output.Collection == input.Collection, "player state round trip");
        byte[] good = input.Encode();
        bool rejected = false;
        try { LanPlayerState.Decode(new byte[] { 1 }); } catch (IOException) { rejected = true; }
        Assert(rejected, "truncated player state rejected");
        input.X = float.NaN; rejected = false;
        try { LanPlayerState.Decode(input.Encode()); } catch (IOException) { rejected = true; }
        Assert(rejected, "NaN rejected");
        input.X = float.PositiveInfinity; rejected = false;
        try { LanPlayerState.Decode(input.Encode()); } catch (IOException) { rejected = true; }
        Assert(rejected, "infinity rejected");
        Array.Resize(ref good, good.Length + 1); rejected = false;
        try { LanPlayerState.Decode(good); } catch (IOException) { rejected = true; }
        Assert(rejected, "trailing player bytes rejected");
        input.X = 0; input.SpriteId = -1; rejected = false;
        try { LanPlayerState.Decode(input.Encode()); } catch (IOException) { rejected = true; }
        Assert(rejected, "negative sprite rejected");
        input.SpriteId = 0; input.Collection = new string('a', 257); rejected = false;
        try { LanPlayerState.Decode(input.Encode()); } catch (IOException) { rejected = true; }
        Assert(rejected, "oversized sprite collection rejected");
    }

    public static int Main()
    {
        try {
            Codec();
            using (LanTransport host = new LanTransport())
            using (LanTransport client = new LanTransport()) {
                int callbackThread = 0, disconnects = 0;
                List<int> received = new List<int>();
                host.PacketReceived += delegate(byte[] data) { callbackThread = Thread.CurrentThread.ManagedThreadId; received.Add(BitConverter.ToInt32(data, 0)); };
                host.DisconnectedEvent += delegate { disconnects++; };
                Pair(host, client);
                Assert(host.Connected && client.Connected, "two endpoint handshake");
                for (int i = 0; i < 500; i++) client.Send(BitConverter.GetBytes(i));
                Pump(host, client, delegate { return received.Count == 500; });
                bool ordered = true;
                for (int i = 0; i < received.Count; i++) ordered &= received[i] == i;
                Assert(ordered, "500 real TCP packets delivered in order");
                Assert(callbackThread == Thread.CurrentThread.ManagedThreadId, "callbacks run on polling thread");
                bool reverse = false;
                client.PacketReceived += delegate(byte[] data) { reverse = data.Length == LanTransport.MaxFrame && data[LanTransport.MaxFrame - 1] == 99; };
                byte[] full = new byte[LanTransport.MaxFrame]; full[LanTransport.MaxFrame - 1] = 99; host.Send(full);
                Pump(host, client, delegate { return reverse; });
                Assert(reverse, "maximum frame and host-to-client delivery");
                using (LanTransport third = new LanTransport()) {
                    third.Join("127.0.0.1", host.BoundPort, "abcdef", Fingerprint);
                    Pump(host, third, delegate { return !third.Active; });
                    Assert(host.Connected && !third.Connected, "third player rejected without dropping pair");
                }
                client.Dispose();
                Pump(host, null, delegate { return !host.Connected; });
                Assert(disconnects == 1 && host.Active, "disconnect event and host listener preserved");
                client.Join("127.0.0.1", host.BoundPort, "abcdef", Fingerprint);
                Pump(host, client, delegate { return host.Connected && client.Connected; });
                Assert(host.Connected, "reconnect after disconnect");
            }
            Reject("other-game", "abcdef", Magic, 1, false, "Game files differ", "game fingerprint mismatch rejected");
            Reject(Fingerprint, "wrong-code", Magic, 1, false, "Wrong session code", "wrong session code rejected");
            Reject(Fingerprint, "abcdef", "other-mod", 1, false, "version/role mismatch", "mod version mismatch rejected");
            Reject(Fingerprint, "abcdef", "GungeonTogether-LAN-dungeon-0.2.2", 1, false, "version/role mismatch", "previous 0.2.2 peer rejected before new wire payloads");
            Reject(Fingerprint, "abcdef", Magic, 2, false, "version/role mismatch", "protocol mismatch rejected");
            Reject(Fingerprint, "abcdef", Magic, 1, true, "version/role mismatch", "host-to-host rejected");
            using (LanTransport host = new LanTransport()) {
                host.Host("127.0.0.1", 0, "abcdef", Fingerprint);
                using (TcpClient raw = new TcpClient("127.0.0.1", host.BoundPort)) {
                    byte[] hello = Hello(Fingerprint, "abcdef", Magic, 1, false);
                    for (int i = 0; i < hello.Length; i++) {
                        raw.GetStream().Write(hello, i, 1); host.Poll();
                        if (i < hello.Length - 1 && host.Connected) throw new Exception("Partial hello accepted");
                    }
                    Pump(host, null, delegate { return host.Connected; });
                    Assert(host.Connected, "one-byte fragmented handshake");
                    int count = 0;
                    host.PacketReceived += delegate(byte[] bytes) { if (bytes.Length == 3 && bytes[0] == 10) count++; };
                    byte[] frame = Frame(new byte[] { 10, 20, 30 });
                    for (int i = 0; i < frame.Length; i++) { raw.GetStream().Write(frame, i, 1); host.Poll(); }
                    Pump(host, null, delegate { return count == 1; });
                    Assert(count == 1, "one-byte fragmented application frame");
                    raw.GetStream().Write(BitConverter.GetBytes(LanTransport.MaxFrame + 1), 0, 4); // 4097 > max
                    Pump(host, null, delegate { return !host.Connected; });
                    Assert(host.Status.Contains("Invalid frame length"), "oversized network frame rejected");
                }
            }
            double fakeTime = 0;
            using (LanTransport host = new LanTransport(delegate { return fakeTime; })) {
                host.Host("127.0.0.1", 0, "abcdef", Fingerprint);
                using (TcpClient raw = new TcpClient("127.0.0.1", host.BoundPort)) {
                    // Wait for actual accept/hello delivery before advancing the
                    // injected clock; accept need not finish in the first poll.
                    Pump(host, null, delegate { return raw.Available > 0; });
                    fakeTime = 11; host.Poll();
                    Assert(host.Status.Contains("Handshake timed out"), "silent peer handshake timeout");
                }
            }
            fakeTime = 0;
            using (LanTransport host = new LanTransport(delegate { return fakeTime; }))
            using (LanTransport client = new LanTransport(delegate { return fakeTime; })) {
                Pair(host, client); fakeTime = 16; host.Poll();
                Assert(!host.Connected && host.Status.Contains("stopped responding"), "inactivity timeout");
            }
            using (LanTransport host = new LanTransport())
            using (LanTransport collision = new LanTransport()) {
                host.Host("127.0.0.1", 0, "abcdef", Fingerprint);
                bool rejected = false;
                try { collision.Host("127.0.0.1", host.BoundPort, "abcdef", Fingerprint); } catch (SocketException) { rejected = true; }
                Assert(rejected && !collision.Active, "occupied port clean failure");
                rejected = false;
                try { collision.Join("example.com", 1234, "abcdef", Fingerprint); } catch (ArgumentException) { rejected = true; }
                Assert(rejected, "hostname rejected with IPv4 error");
                rejected = false;
                try { collision.Host("127.0.0.1", 0, "123", Fingerprint); } catch (ArgumentException) { rejected = true; }
                Assert(rejected, "short session code rejected");
            }
            Console.WriteLine("LAN_TESTS_PASS: " + passed + " checks; actual loopback sockets, no game stubs");
            return 0;
        } catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }
}

