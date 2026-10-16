using System;
using System.IO;
using GungeonTogether.Lan;

static class PresentationTests
{
    static int passed;
    static void Check(bool good, string name) { if (!good) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
    static void Reject(Action action, string name) { try { action(); } catch (IOException) { Check(true, name); return; } throw new Exception(name); }
    static SoundBatch Batch(int epoch, uint seq, double time)
    {
        SoundBatch b = new SoundBatch { Epoch = epoch, Sequence = seq, SentAt = time };
        SoundCue c = new SoundCue { Event = uint.MaxValue, Emitter = 13, X = -78, Y = 99 };
        c.Switches.Add(new SoundSwitch { Group = 77, Value = 88 }); b.Cues.Add(c); return b;
    }
    static byte[] Raw(int epoch, double time, byte count, uint eventId, uint emitter, float x, byte switches)
    {
        return Wire.Write(DungeonMessage.Audio, delegate(BinaryWriter w) {
            w.Write(epoch); w.Write((uint)1); w.Write(time); w.Write(count);
            w.Write(eventId); w.Write(emitter); w.Write(x); w.Write(0f); w.Write(switches);
        });
    }
    static void Main()
    {
        SoundBatch b = SoundBatch.Decode(Batch(2, 55, 100).Encode());
        Check(b.Epoch == 2 && b.Sequence == 55 && b.Cues[0].Event == uint.MaxValue && b.Cues[0].Emitter == 13 && b.Cues[0].X == -78 && b.Cues[0].Y == 99 && b.Cues[0].Switches[0].Value == 88, "spatial cue and weapon switch round trip");
        for (int i = 1; i < 64; i++) b.Cues.Add(b.Cues[0]);
        Check(SoundBatch.Decode(b.Encode()).Cues.Count == 64 && b.Encode().Length < 8192, "bounded full audio burst fits a small frame");
        b.Cues.Add(b.Cues[0]); Reject(delegate { b.Encode(); }, "oversized host burst rejected");
        Reject(delegate { SoundBatch.Decode(Raw(1, 0, 65, 1, 1, 0, 0)); }, "untrusted cue budget rejected before allocating payloads");
        Reject(delegate { SoundBatch.Decode(Raw(1, 0, 1, 1, 1, 0, 9)); }, "untrusted switch budget rejected");
        Reject(delegate { SoundBatch.Decode(Raw(0, 0, 1, 1, 1, 0, 0)); }, "zero epoch rejected");
        Reject(delegate { SoundBatch.Decode(Raw(1, double.NaN, 1, 1, 1, 0, 0)); }, "NaN audio clock rejected");
        Reject(delegate { SoundBatch.Decode(Raw(1, 0, 1, 0, 1, 0, 0)); }, "zero sound event rejected");
        Reject(delegate { SoundBatch.Decode(Raw(1, 0, 1, 1, 0, 0, 0)); }, "zero emitter rejected");
        Reject(delegate { SoundBatch.Decode(Raw(1, 0, 1, 1, 1, float.PositiveInfinity, 0)); }, "infinite emitter position rejected");
        b = Batch(1, 1, 0); b.Cues[0].Switches.Add(new SoundSwitch { Group = 77, Value = 99 }); Reject(delegate { b.Encode(); }, "duplicate switch group rejected");
        byte[] packet = Batch(1, 1, 0).Encode(); Array.Resize(ref packet, packet.Length + 1); Reject(delegate { SoundBatch.Decode(packet); }, "trailing audio bytes rejected");
        SoundSequence sequence = new SoundSequence();
        Check(!sequence.Accept(Batch(1, 10, 100), 2, 100), "old floor event cannot play on current floor");
        Check(sequence.Accept(Batch(2, 10, 100), 2, 100), "current floor event accepted");
        Check(!sequence.Accept(Batch(2, 10, 100), 2, 100), "duplicate batch never plays twice");
        Check(!sequence.Accept(Batch(2, 9, 100), 2, 100), "out of order batch rejected");
        Check(!sequence.Accept(Batch(2, 11, 98), 2, 100), "seconds-old shots dropped after stall");
        Check(sequence.Accept(Batch(2, 12, 99.9), 2, 100), "normal network jitter preserves cue");
        Check(sequence.Accept(Batch(3, 1, 1), 3, 0), "sequence restarts cleanly for next floor");
        sequence = new SoundSequence(); Check(sequence.Accept(Batch(1, uint.MaxValue, 1), 1, 0) && sequence.Accept(Batch(1, 0, 2), 1, 0), "audio sequence wraps correctly");
        sequence = new SoundSequence(); sequence.ObserveClock(1, 50, 100);
        Check(!sequence.Accept(Batch(1, 1, 50.1), 1, 50, 102), "delayed FIFO audio rejected even before the delayed following snapshot arrives");
        sequence.ObserveClock(1, 50.2, 103);
        Check(!sequence.Accept(Batch(1, 2, 50.3), 1, 50.2, 103.1), "congested snapshots cannot move sound clock backwards to revive stale shots");
        Check(sequence.Accept(Batch(1, 3, 53.1), 1, 50.2, 103.2), "fresh sound plays immediately after link recovery");
        sequence.ObserveClock(2, 1, 104);
        Check(sequence.Accept(Batch(2, 1, 1.1), 2, 1, 104.2), "new floor recalibrates sound clock without carrying prior offset");
        WorldSnapshot hud = new WorldSnapshot { Epoch = 1, CameraSize = 7, Health = 2.5f, MaxHealth = 4, Armor = 2, GunId = 15, ItemId = 51, ClipAmmo = 5, ClipCapacity = 30, Reloading = true };
        WorldSnapshot result = WorldSnapshot.Decode(hud.Encode());
        Check(result.Health == 2.5f && result.MaxHealth == 4 && result.Armor == 2 && result.GunId == 15 && result.ItemId == 51 && result.ClipAmmo == 5 && result.ClipCapacity == 30 && result.Reloading, "real HUD half hearts, max health, equipment and magazine survive wire");
        hud.MaxHealth = float.NaN; Reject(delegate { hud.Encode(); }, "invalid heart capacity rejected"); hud.MaxHealth = 4;
        hud.GunId = -2; Reject(delegate { hud.Encode(); }, "invalid gun asset ID rejected"); hud.GunId = 15;
        hud.ItemId = 1000001; Reject(delegate { hud.Encode(); }, "equipment ID bound enforced"); hud.ItemId = -1;
        hud.ClipAmmo = -1; Reject(delegate { hud.Encode(); }, "negative magazine count rejected"); hud.ClipAmmo = 0;
        hud.ClipCapacity = int.MaxValue; Reject(delegate { hud.Encode(); }, "excess magazine capacity rejected");
        Console.WriteLine("PRESENTATION_TESTS_PASS: " + passed);
    }
}
