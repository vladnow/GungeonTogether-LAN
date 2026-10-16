using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using GungeonTogether.Lan;

internal static class TransportLatencyTests
{
    private sealed class State { public int Sequence; public double SentAt; }
    private static readonly Stopwatch Timer = Stopwatch.StartNew();
    private static int assertions;
    private static double Now { get { return Timer.Elapsed.TotalSeconds; } }
    private static bool expectFresh, forceFifo;
    private static MethodInfo Latest = typeof(LanTransport).GetMethod("SendLatest", new Type[] { typeof(byte[]) });
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); assertions++; Console.WriteLine("PASS " + message); }
    private static bool WouldBlock(SocketException ex) { return ex.SocketErrorCode == SocketError.WouldBlock || ex.SocketErrorCode == SocketError.IOPending || ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable; }

    // Rate limiting happens by reading slowly from the server socket, so TCP
    // backpressure reaches the real LanTransport. No unbounded proxy FIFO exists.
    private sealed class ShapedProxy : IDisposable
    {
        private readonly TcpListener listener;
        private readonly Thread worker;
        private readonly int targetPort;
        private volatile bool stop;
        public volatile int BytesPerSecond;
        public Exception Failure;
        private Socket downstream, upstream;
        public int Port { get; private set; }
        public ShapedProxy(int targetPort)
        {
            this.targetPort = targetPort;
            listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            worker = new Thread(Run); worker.IsBackground = true; worker.Start();
        }
        private void Run()
        {
            try {
                downstream = listener.AcceptSocket();
                upstream = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                upstream.ReceiveBufferSize = 4096; upstream.SendBufferSize = 4096;
                upstream.NoDelay = downstream.NoDelay = true;
                upstream.Connect(IPAddress.Loopback, targetPort);
                downstream.Blocking = upstream.Blocking = false;
                byte[] up = new byte[16384], down = new byte[2048];
                int upCount = 0, upOffset = 0, downCount = 0, downOffset = 0;
                double tokens = 0, last = Now;
                while (!stop) {
                    double current = Now, elapsed = current - last; last = current;
                    int rate = BytesPerSecond;
                    tokens = rate <= 0 ? down.Length : Math.Min(down.Length, tokens + elapsed * rate);
                    Pump(downstream, upstream, up, ref upCount, ref upOffset, up.Length);
                    int allowance = Math.Min(down.Length, (int)tokens);
                    int used = Pump(upstream, downstream, down, ref downCount, ref downOffset, allowance);
                    if (rate > 0) tokens -= used;
                    Thread.Sleep(1);
                }
            } catch (Exception ex) { if (!stop) Failure = ex; }
        }
        private static int Pump(Socket source, Socket target, byte[] buffer, ref int count, ref int offset, int allowance)
        {
            int received = 0;
            if (count == offset && allowance > 0 && source.Poll(0, SelectMode.SelectRead)) {
                try { count = source.Receive(buffer, 0, Math.Min(buffer.Length, allowance), SocketFlags.None); offset = 0; received = count; if (count == 0) throw new IOException("Proxy source closed."); }
                catch (SocketException ex) { if (!WouldBlock(ex)) throw; }
            }
            if (count > offset && target.Poll(0, SelectMode.SelectWrite)) {
                try { offset += target.Send(buffer, offset, count - offset, SocketFlags.None); }
                catch (SocketException ex) { if (!WouldBlock(ex)) throw; }
            }
            return received;
        }
        public void Dispose()
        {
            stop = true; listener.Stop();
            if (downstream != null) try { downstream.Close(); } catch { }
            if (upstream != null) try { upstream.Close(); } catch { }
            worker.Join(2000);
        }
    }
    private sealed class Pair : IDisposable
    {
        public readonly LanTransport Host = new LanTransport(delegate { return Now; });
        public readonly LanTransport Client = new LanTransport(delegate { return Now; });
        public readonly ShapedProxy Proxy;
        public Pair()
        {
            Host.Host("127.0.0.1", 0, "latency-test", "native-free-transport-check");
            Proxy = new ShapedProxy(Host.BoundPort);
            Client.Join("127.0.0.1", Proxy.Port, "latency-test", "native-free-transport-check");
            Until(delegate { Poll(); return Host.Connected && Client.Connected; }, 5, "socket handshake");
        }
        public void Poll()
        {
            Host.Poll(); Client.Poll();
            if (Proxy.Failure != null) throw Proxy.Failure;
        }
        public void Dispose() { Host.Dispose(); Client.Dispose(); Proxy.Dispose(); }
    }
    private static void Until(Func<bool> condition, double timeout, string label)
    {
        double end = Now + timeout;
        while (!condition()) { if (Now > end) throw new Exception("Timed out: " + label); Thread.Sleep(1); }
    }
    private static byte[] Frame(byte type, int sequence, double sentAt, int bytes, bool compressible = false)
    {
        byte[] result = new byte[Math.Max(bytes, 13)]; result[0] = type;
        Buffer.BlockCopy(BitConverter.GetBytes(sequence), 0, result, 1, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(sentAt), 0, result, 5, 8);
        // Keep the bottleneck real when transport compression is enabled.
        // Zero padding would shrink to a few dozen bytes and never congest TCP.
        if (!compressible) {
            uint noise = unchecked(0x9e3779b9u ^ (uint)sequence);
            for (int i = 13; i < result.Length; i++) { noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5; result[i] = (byte)noise; }
        }
        return result;
    }
    private static State Decode(byte[] data) { return new State { Sequence = BitConverter.ToInt32(data, 1), SentAt = BitConverter.ToDouble(data, 5) }; }
    private static int Pending(LanTransport transport)
    {
        foreach (string name in new string[] { "PendingBytes", "QueuedBytes" }) {
            PropertyInfo property = typeof(LanTransport).GetProperty(name);
            if (property != null) return Convert.ToInt32(property.GetValue(transport, null));
        }
        FieldInfo field = typeof(LanTransport).GetField("queued", BindingFlags.NonPublic | BindingFlags.Instance);
        return field == null ? -1 : (int)field.GetValue(transport);
    }
    private static long Metric(LanTransport transport, string name)
    {
        PropertyInfo property = typeof(LanTransport).GetProperty(name);
        return property == null ? -1 : Convert.ToInt64(property.GetValue(transport, null));
    }
    private static void SendState(LanTransport transport, byte[] data)
    {
        if (!forceFifo && Latest != null) Latest.Invoke(transport, new object[] { data });
        else transport.Send(data);
    }
    private static double Percentile(List<double> values, double fraction)
    {
        if (values.Count == 0) return double.NaN;
        values.Sort(); return values[Math.Min(values.Count - 1, (int)Math.Floor(values.Count * fraction))];
    }
    private static void LowRtt()
    {
        using (Pair pair = new Pair()) {
            List<double> rtt = new List<double>();
            pair.Client.PacketReceived += delegate(byte[] data) { pair.Client.Send(data); };
            pair.Host.PacketReceived += delegate(byte[] data) { rtt.Add(Now - Decode(data).SentAt); };
            for (int i = 0; i < 8; i++) {
                int expected = i + 1; pair.Host.Send(Frame(3, i, Now, 24));
                Until(delegate { pair.Poll(); return rtt.Count >= expected; }, 2, "echo");
            }
            double p95 = Percentile(rtt, .95);
            Console.WriteLine("METRIC idle_rtt_p95_ms=" + (1000 * p95).ToString("F1"));
            Check(p95 < .15, "real loopback socket RTT stays below 150 ms before congestion");
        }
    }
    private static void Congestion()
    {
        using (Pair pair = new Pair()) {
            const int payloadBytes = 16384, byteRate = 131072;
            List<double> ages = new List<double>(); List<int> controls = new List<int>();
            SnapshotBuffer<State> frames = new SnapshotBuffer<State>(.12);
            int latestReceived = 0, previous = 0, produced = 0, sentControls = 0, peakPending = 0;
            long replacedBefore = Metric(pair.Host, "ReplacedFrames");
            pair.Client.PacketReceived += delegate(byte[] data) {
                State value = Decode(data);
                if (data[0] == 2) { controls.Add(value.Sequence); return; }
                if (data[0] != 1) return;
                if (value.Sequence <= previous) throw new Exception("State delivery reordered or duplicated.");
                previous = value.Sequence; latestReceived = value.Sequence;
                ages.Add(Now - value.SentAt); frames.Push(value, (uint)value.Sequence, value.SentAt, Now);
            };
            pair.Proxy.BytesPerSecond = byteRate;
            double start = Now, next = start, nextControl = start + .25, finish = start + 3.0;
            while (Now < finish) {
                double now = Now;
                if (now >= next) { SendState(pair.Host, Frame(1, ++produced, now, payloadBytes)); next += 1.0 / 30; }
                if (now >= nextControl) { pair.Host.Send(Frame(2, ++sentControls, now, 24)); nextControl += .4; }
                pair.Poll(); peakPending = Math.Max(peakPending, Pending(pair.Host));
                Thread.Sleep(1);
            }
            Check(pair.Host.Connected && pair.Client.Connected, "sustained bandwidth pressure preserves the connection");
            Check(ages.Count >= 5, "throttled socket delivers enough snapshots to measure age");
            double p50 = Percentile(ages, .50), p95 = Percentile(ages, .95);
            State from, to; float blend;
            Check(frames.Sample(Now, out from, out to, out blend), "snapshot buffer samples the received stream");
            double sampledAge = Now - (from.SentAt + (to.SentAt - from.SentAt) * blend);
            Console.WriteLine("METRIC mode=" + ((!forceFifo && Latest != null) ? "latest" : "fifo") + " link_bytes_per_second=" + byteRate + " produced=" + produced + " received=" + ages.Count + " latest_sequence=" + latestReceived + " state_age_p50_ms=" + (p50 * 1000).ToString("F1") + " state_age_p95_ms=" + (p95 * 1000).ToString("F1") + " sampled_age_ms=" + (sampledAge * 1000).ToString("F1") + " peak_app_pending_bytes=" + peakPending);
            if (expectFresh) {
                Check(p95 < .80, "congested newest-state delivery age stays below 800 ms");
                Check(sampledAge < 1.0, "render sample remains younger than one second under sustained congestion");
                Check(peakPending <= payloadBytes * 5, "replaceable application backlog stays below five state frames");
                Check(Metric(pair.Host, "ReplacedFrames") > replacedBefore, "congestion exercises replacement of unsent state frames");
            } else if (forceFifo || Latest == null) {
                Check(p95 > 1.0 && sampledAge > 1.0, "baseline FIFO reproduces seconds of stale state despite low idle RTT");
                Console.WriteLine("BASELINE_FIFO_BACKLOG_REPRODUCED");
            }
            pair.Proxy.BytesPerSecond = 0;
            Until(delegate { pair.Poll(); return controls.Count == sentControls && latestReceived == produced; }, 10, "drain reliable controls and final state");
            for (int i = 0; i < controls.Count; i++) Check(controls[i] == i + 1, "reliable control " + (i + 1) + " survives state coalescing in order");
            Check(latestReceived == produced, "final produced state arrives after the bottleneck is removed");
        }
    }
    private static void CoalescingOrderAndCompression()
    {
        if (Latest == null || forceFifo) return;
        using (Pair pair = new Pair()) {
            List<string> received = new List<string>();
            pair.Client.PacketReceived += delegate(byte[] data) { received.Add(data[0] + ":" + Decode(data).Sequence); };
            SendState(pair.Host, Frame(1, 1, Now, 128));
            pair.Host.Send(Frame(2, 100, Now, 24));
            SendState(pair.Host, Frame(1, 2, Now, 128));
            pair.Host.Send(Frame(2, 101, Now, 24));
            SendState(pair.Host, Frame(5, 7, Now, 128));
            Until(delegate { pair.Poll(); return received.Count >= 4; }, 3, "coalesced message ordering");
            Check(string.Join(",", received.ToArray()) == "2:100,1:2,2:101,5:7", "latest state replacement preserves reliable message order and independent state keys");
            Check(Metric(pair.Host, "ReplacedFrames") == 1, "one queued state is replaced before any socket flush");
        }
        using (Pair pair = new Pair()) {
            byte[] original = Frame(4, 500, Now, 32768, true), delivered = null;
            pair.Client.PacketReceived += delegate(byte[] data) { delivered = data; };
            long before = Metric(pair.Host, "WireBytesSent");
            SendState(pair.Host, original);
            Until(delegate { pair.Poll(); return delivered != null; }, 3, "compressed frame round trip");
            bool same = original.Length == delivered.Length;
            for (int i = 0; same && i < original.Length; i++) same = original[i] == delivered[i];
            Check(same, "outer compressed header round-trips the exact original packet bytes");
            long wireBytes = Metric(pair.Host, "WireBytesSent") - before;
            Console.WriteLine("METRIC compressible_payload_bytes=" + original.Length + " actual_wire_bytes=" + wireBytes);
            Check(wireBytes > 0 && wireBytes < original.Length / 4, "compression reduces a repetitive full state on the real socket");
        }
    }
    private static void SlowPolling()
    {
        using (Pair pair = new Pair()) {
            SnapshotBuffer<State> frames = new SnapshotBuffer<State>(.12);
            int sequence = 0, received = 0; double nextSend = Now, nextRead = Now, end = Now + 1.8, largestAge = 0, lastSampleTime = -1;
            pair.Client.PacketReceived += delegate(byte[] data) { State value = Decode(data); received++; frames.Push(value, (uint)value.Sequence, value.SentAt, Now); };
            while (Now < end) {
                double now = Now;
                if (now >= nextSend) { SendState(pair.Host, Frame(1, ++sequence, now, 512)); nextSend += 1.0 / 60; }
                pair.Host.Poll();
                if (now >= nextRead) { pair.Client.Poll(); nextRead += .1; }
                State from, to; float blend;
                if (frames.Sample(now, out from, out to, out blend)) {
                    double time = from.SentAt + (to.SentAt - from.SentAt) * blend;
                    if (time + .000001 < lastSampleTime) throw new Exception("Playback moved backwards after a TCP batch.");
                    lastSampleTime = time; largestAge = Math.Max(largestAge, now - time);
                }
                Thread.Sleep(1);
            }
            Console.WriteLine("METRIC slow_poll_interval_ms=100 received=" + received + " max_sample_age_ms=" + (largestAge * 1000).ToString("F1") + " buffer_count=" + frames.Count);
            Check(received > 50, "ten-Hz receiver polling drains sixty-Hz state batches");
            Check(largestAge < .40, "slow polling does not retain a one-second SnapshotBuffer playback backlog");
            Check(frames.Count <= 32, "snapshot interpolation buffer remains bounded");
        }
    }
    public static int Main(string[] args)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        foreach (string arg in args) { if (arg == "--expect-fresh") expectFresh = true; if (arg == "--fifo") forceFifo = true; }
        try {
            if (expectFresh) Check(!forceFifo && Latest != null, "freshness run uses public SendLatest(byte[]) API");
            LowRtt(); CoalescingOrderAndCompression(); Congestion(); SlowPolling();
            Console.WriteLine("TRANSPORT_LATENCY_PASS assertions=" + assertions + " game_launched=false real_sockets=true shaped_delivery=true"); return 0;
        } catch (Exception ex) { Console.Error.WriteLine("TRANSPORT_LATENCY_FAIL " + ex); return 1; }
    }
}
