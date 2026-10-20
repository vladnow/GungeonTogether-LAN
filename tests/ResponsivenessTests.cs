using System;
using System.IO;
using GungeonTogether.Lan;
static class ResponsivenessTests
{
    static int passed;
    static void Check(bool condition, string name) { if (!condition) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    static void Reject(Action action, string name) { try { action(); } catch (IOException) { Check(true, name); return; } throw new Exception(name); }
    static PlayerMotion Motion(uint seq, uint ack) { return new PlayerMotion { Epoch = 1, Sequence = seq, InputSequence = ack, SentAt = 100, X = 10, Y = 20, Speed = 7, RoomX = 1, RoomY = 2, AllowPrediction = true, CameraFollowsPlayer = true, CameraX = 10, CameraY = 21, CameraZ = -10 }; }
    static RemoteInput Input(uint seq, float x, float y = 0, ulong buttons = 0) { return new RemoteInput { Epoch = 1, Sequence = seq, MoveX = x, MoveY = y, Buttons = buttons }; }
    static void Main()
    {
        double next = 0, oldNext = 0, now = 0; int phase = 0, reset = 0;
        for (int i = 0; i < 600; i++) { now += i % 2 == 0 ? .0161 : .0172; if (NetworkTiming.Due(ref next, now, 1.0 / 60)) phase++; if (now >= oldNext) { oldNext = now + 1.0 / 60; reset++; } }
        Check(phase >= 590 && phase > reset + 100, "phase-preserving send cadence survives alternating frame jitter");
        next = 1; Check(NetworkTiming.Due(ref next, 30, 1.0 / 30) && !NetworkTiming.Due(ref next, 30, 1.0 / 30), "no catch-up burst after long frame stall");
        Check(!NetworkTiming.Due(ref next, 30.001, 1.0 / 30), "scheduler cannot exceed target frequency");
        PlayerMotion value = Motion(3, 6); PlayerMotion decoded = PlayerMotion.Decode(value.Encode());
        Check(decoded.Sequence == 3 && decoded.InputSequence == 6 && decoded.X == 10 && decoded.RoomY == 2 && decoded.AllowPrediction && decoded.CameraFollowsPlayer && decoded.CameraZ == -10, "small authoritative motion packet round trip");
        Check(value.Encode().Length < 100, "player motion frame stays below one hundred payload bytes");
        value.X = float.NaN; Reject(delegate { value.Encode(); }, "non-finite motion rejected"); value.X = 10;
        value.Speed = -1; Reject(delegate { value.Encode(); }, "negative movement speed rejected"); value.Speed = 7;
        value.RoomX = int.MinValue; Reject(delegate { value.Encode(); }, "invalid room identity rejected without integer overflow"); value.RoomX = 1;
        value.HalfWidth = 0; Reject(delegate { value.Encode(); }, "zero footprint rejected"); value.HalfWidth = .25f;
        byte[] bytes = value.Encode(); Array.Resize(ref bytes, bytes.Length + 1); Reject(delegate { PlayerMotion.Decode(bytes); }, "trailing motion bytes rejected");
        MovementPrediction prediction = new MovementPrediction(); prediction.Observe(Motion(1, 1), 100);
        prediction.Record(Input(2, 1), 100.06); float x, y; prediction.Offset(100.08, true, out x, out y);
        Check(Math.Abs(x - .14f) < .0001 && y == 0, "replay unacknowledged walk immediately without world interpolation");
        Check(prediction.Pending == 1, "unacknowledged history retained");
        PlayerMotion acknowledged = Motion(2, 2); acknowledged.X += .14f;
        prediction.Observe(acknowledged, 100.08); prediction.Offset(100.09, true, out x, out y);
        Check(Math.Abs(x) < .0001 && prediction.Pending == 0, "acknowledged native movement is not replayed twice");
        Check(!prediction.Observe(Motion(1, 1), 100.10), "stale authority cannot roll presentation backwards");
        prediction.Record(Input(3, 0, 1), 100.1); prediction.Offset(100.12, true, out x, out y);
        Check(Math.Abs(x) < .0001 && Math.Abs(y - .14f) < .0001, "independent vertical replay");
        prediction.Offset(100.13, false, out x, out y); Check(x == 0 && y == 0, "prediction can be disabled while retaining authority stream");
        prediction.Offset(100.5, true, out x, out y); Check(x == 0 && y == 0, "network stall stops speculative movement");
        prediction = new MovementPrediction(); prediction.Observe(Motion(1, 0), 100); prediction.Record(Input(2, 1), 100.06); prediction.Offset(100.14, true, out x, out y);
        Check(Math.Abs(x - .56f) < .001, "normal replay duration follows sample intervals");
        PlayerMotion fast = Motion(2, 0); fast.Speed = 50; prediction.Observe(fast, 100.14); prediction.Offset(100.145, true, out x, out y);
        Check(x <= MovementPrediction.MaxDistance + .0001, "distance cap protects modified high-speed characters");
        prediction.Record(Input(3, 1, 0, 1UL << 9), 100.15); prediction.Offset(100.16, true, out x, out y);
        Check(x == 0 && prediction.Pending == 0, "dodge-roll button clears local speculation before host response");
        prediction.Record(Input(4, 1), 100.2); prediction.Offset(100.25, true, out x, out y); Check(x == 0, "brief roll/action cooldown prevents immediate re-prediction");
        PlayerMotion blocked = Motion(3, 4); blocked.AllowPrediction = false; prediction.Observe(blocked, 100.3); prediction.Record(Input(5, 1), 100.31); prediction.Offset(100.4, true, out x, out y);
        Check(x == 0, "host pause, death, cinematic or blocked input disables replay");
        prediction = new MovementPrediction(); prediction.Observe(Motion(1, 0), 100); prediction.Record(Input(2, 1), 100.06);
        PlayerMotion warp = Motion(2, 0); warp.X = 30; prediction.Observe(warp, 100.07); prediction.Offset(100.08, true, out x, out y);
        Check(x == 0 && prediction.Pending == 0 && prediction.Resets == 2, "native teleport clears all pending offsets");
        PlayerMotion room = Motion(3, 0); room.X = 30; room.RoomX = 3; prediction.Observe(room, 100.1);
        Check(prediction.Resets == 3, "room boundary forces prediction reset");
        PlayerMotion floor = Motion(1, 0); floor.Epoch = 2; Check(prediction.Observe(floor, 101), "new floor accepts fresh sequence");
        Check(!prediction.Observe(Motion(99, 0), 102), "old floor cannot revive old movement");
        for (uint i = 1; i <= 500; i++) { RemoteInput input = Input(i, 1); input.Epoch = 2; prediction.Record(input, 101 + i * .001); }
        Check(prediction.Pending <= 128, "input replay history remains bounded during backlog");
        prediction.Clear(); Check(prediction.Motion == null && prediction.Pending == 0, "session disposal clears all prediction state");
        prediction.Observe(Motion(1, 0), 200); prediction.Record(Input(1, 1), 200.06);
        prediction.Offset(200.08, true, out x, out y); float before = 10 + x;
        PlayerMotion corrected = Motion(2, 1); corrected.X = 10.10f; prediction.Observe(corrected, 200.08);
        prediction.Offset(200.08, true, out x, out y);
        Check(Math.Abs(corrected.X + x - before) < .0001, "authority correction preserves presentation at packet arrival");
        prediction.Offset(200.08 + MovementPrediction.CorrectionDuration + .001, true, out x, out y);
        Check(Math.Abs(x) < .0001, "visual correction converges without replaying acknowledged input");
        prediction.Clear(); prediction.Observe(Motion(1, 0), 300);
        prediction.Record(Input(1, 1, 0, (1UL << 23) | (1UL << 11) | (1UL << 14)), 300.06);
        prediction.Offset(300.08, true, out x, out y);
        Check(Math.Abs(x - .14f) < .0001, "WASD menu-navigation, reload and gun-cycle bindings do not suppress walking prediction");
        SnapshotBuffer<object> buffer = new SnapshotBuffer<object>(.04, true); object sample = new object();
        for (uint i = 1; i <= 180; i++) buffer.Push(sample, i, i / 30.0, 10 + i / 30.0);
        Check(buffer.DelaySeconds >= .035 && buffer.DelaySeconds < .045, "stable thirty-Hz stream uses a shorter adaptive world buffer");
        double received = 16;
        for (uint i = 181; i <= 240; i++) { received = Math.Max(received, 10 + i / 30.0 + (i % 2 == 0 ? .06 : 0)); buffer.Push(sample, i, i / 30.0, received); }
        Check(buffer.DelaySeconds > .05 && buffer.DelaySeconds <= .09, "jitter raises smoothing within bounded range");
        object a, b; float blend; Check(buffer.Sample(received, out a, out b, out blend) && buffer.Count <= 32, "adaptive buffer remains sampleable and bounded");

        prediction.Clear(); prediction.Observe(Motion(1, 0), 400); prediction.Record(Input(2, 1), 400.06);
        PlayerMotion adjacent = Motion(2, 1); adjacent.X += .1f; adjacent.RoomX = 2; prediction.Observe(adjacent, 400.07); prediction.Offset(400.08, true, out x, out y);
        Check(x > 0 && prediction.Pending == 1, "ordinary adjacent room entry preserves fresh input without an artificial cooldown");
        CameraOffsetFilter anchor = new CameraOffsetFilter(); double square = 0; float cx, cy;
        for (int i = 0; i < 120; i++) { anchor.Update((i % 2 == 0 ? 1 : -1) * .0625f, 1, i / 60.0, out cx, out cy); if (i > 20) square += cx * cx; }
        Check(square / 99 < .0005, "alternating native camera-anchor jitter is attenuated");
        anchor.Clear(); anchor.Update(4, 5, 3, out cx, out cy); Check(cx == 4 && cy == 5, "camera reset uses a new cinematic or floor anchor immediately");
        Console.WriteLine("RESPONSIVENESS_TESTS_PASS: " + passed);
    }
}
