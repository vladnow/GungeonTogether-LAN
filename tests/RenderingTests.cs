using System;
using System.IO;
using GungeonTogether.Lan;

internal static class RenderingTests
{
    private sealed class Position { public double X; public Position(double x) { X = x; } }
    private static int passed;
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); passed++; Console.WriteLine("PASS: " + message); }
    private static double Sample(SnapshotBuffer<Position> buffer, double now)
    {
        Position a, b; float blend;
        if (!buffer.Sample(now, out a, out b, out blend)) throw new Exception("No buffered frame");
        return a.X + (b.X - a.X) * blend;
    }
    private static void Reject(Action action, string name)
    {
        bool rejected = false; try { action(); } catch (IOException) { rejected = true; }
        Assert(rejected, name);
    }
    private static void Motion(int fps, bool jitter)
    {
        SnapshotBuffer<Position> buffer = new SnapshotBuffer<Position>(0.12);
        int sent = 0, movingFrames = 0;
        double previous = 0, maxError = 0, maxStep = 0;
        for (int frame = 0; frame < fps * 8; frame++) {
            double now = frame / (double)fps;
            while (sent < 161) {
                double time = sent * 0.05;
                // TCP can deliver two delayed frames together. Both keep their
                // original simulation timestamps and a 600s clock offset.
                double arrival = time + 0.02 + (jitter && sent % 5 == 2 ? 0.065 : 0);
                if (now + 1e-9 < arrival) break;
                buffer.Push(new Position(time * 6), (uint)(sent + 1), 600 + time, now);
                sent++;
            }
            if (buffer.Count == 0) continue;
            double x = Sample(buffer, now);
            if (now > 1) {
                AssertSilent(x >= previous - 1e-5, "motion reversed during ordered delivery");
                double step = x - previous;
                if (step > 1e-5) movingFrames++;
                maxStep = Math.Max(maxStep, step);
                maxError = Math.Max(maxError, Math.Abs(x - Math.Max(0, now - 0.14) * 6));
            }
            previous = x;
        }
        Assert(movingFrames > fps * 6, fps + " FPS " + (jitter ? "batched jitter" : "steady link") + ": movement on render frames between 20Hz updates");
        Assert(maxError < 0.11 && maxStep < 6.0 / fps + 0.055, fps + " FPS: bounded interpolation error and no packet-sized steps (error=" + maxError.ToString("0.000") + ", step=" + maxStep.ToString("0.000") + ")");
        Assert(buffer.Count <= 32, "bounded snapshot history");
    }
    private static void AssertSilent(bool value, string message) { if (!value) throw new Exception(message); }
    public static int Main()
    {
        try {
            SnapshotBuffer<Position> buffer = new SnapshotBuffer<Position>(0.1);
            Position a, b; float blend;
            Assert(!buffer.Sample(0, out a, out b, out blend), "empty history has no invented frame");
            Assert(buffer.Push(new Position(0), 1, 100, 10), "independent sender and receiver clocks accepted");
            buffer.Push(new Position(1), 2, 100.1, 10.1);
            Assert(Math.Abs(Sample(buffer, 10.15) - 0.5) < 1e-5, "position interpolated midway between snapshots");
            Assert(!buffer.Push(new Position(500), 1, 100.2, 10.2), "stale sequence rejected without rewinding");
            Assert(!buffer.Push(new Position(500), 3, 99, 10.2), "backward sender clock rejected");
            Assert(!buffer.Push(new Position(500), 3, double.NaN, 10.2), "NaN clock rejected");
            Assert(Sample(buffer, 20) == 1, "network stall freezes at last known state without extrapolation");
            buffer.Clear();
            Assert(buffer.Count == 0 && !buffer.Sample(0, out a, out b, out blend), "level change clears interpolation and timing");
            buffer.Push(new Position(0), uint.MaxValue, 1, 1);
            Assert(buffer.Push(new Position(1), 0, 2, 2), "sequence wraparound accepted");
            Motion(60, false); Motion(144, false); Motion(60, true); Motion(144, true);
            for (int i = 0; i < CharacterCatalog.GuestCount; i++) {
                Assert(LobbyChoice.Decode(new LobbyChoice { Character = i }.Encode()).Character == i, "guest selection round trip: " + CharacterCatalog.Prefab(i));
                LevelCommand command = new LevelCommand { Epoch = 1, Seed = 42, LayoutHash = new string('a', 64), HostCharacter = 1, GuestCharacter = i };
                Assert(LevelCommand.Decode(command.Encode()).GuestCharacter == i, "character retained in floor command: " + i);
            }
            Reject(delegate { LobbyChoice.Decode(new LobbyChoice { Character = 5 }.Encode()); }, "unsupported guest character rejected");
            Assert(CharacterCatalog.FromIdentity("Pilot") == 1 && CharacterCatalog.FromIdentity("Soldier") == 2, "native character identity differs from prefab name");
            LanPlayerState foyer = new LanPlayerState { Sequence = 17, SentAt = 45.5, CollectionGuid = "sprite-guid", Character = 4, Z = 3, Height = 0.25f, ScaleX = 2 };
            LanPlayerState decoded = LanPlayerState.Decode(foyer.Encode());
            Assert(decoded.Sequence == 17 && decoded.SentAt == 45.5 && decoded.Character == 4 && decoded.CollectionGuid == foyer.CollectionGuid && decoded.ScaleX == 2 && decoded.Z == 3, "foyer timing and peer appearance round trip");
            foyer.SentAt = double.NaN; Reject(delegate { LanPlayerState.Decode(foyer.Encode()); }, "invalid foyer timestamp rejected");
            foyer.SentAt = 0; foyer.Character = 999; Reject(delegate { LanPlayerState.Decode(foyer.Encode()); }, "arbitrary foyer resource ID rejected");
            WorldSnapshot snapshot = new WorldSnapshot { Epoch = 1, Sequence = 1, CameraSize = 7, SentAt = 12.75 };
            Assert(WorldSnapshot.Decode(snapshot.Encode()).SentAt == 12.75, "world simulation timestamp round trip");
            snapshot.SentAt = double.PositiveInfinity; Reject(delegate { WorldSnapshot.Decode(snapshot.Encode()); }, "infinite world timestamp rejected");
            Console.WriteLine("RENDERING_TESTS_PASS: " + passed + " checks"); return 0;
        } catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }
}
