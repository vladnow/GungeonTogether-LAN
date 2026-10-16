using System;
using System.Collections.Generic;
using System.IO;

namespace GungeonTogether.Lan
{
    public sealed class SoundSwitch
    {
        public uint Group, Value;
    }

    public sealed class SoundCue
    {
        public uint Event, Emitter;
        public float X, Y;
        public readonly List<SoundSwitch> Switches = new List<SoundSwitch>();
    }

    public sealed class SoundBatch
    {
        public const int MaxCues = 64, MaxSwitches = 8;
        public int Epoch;
        public uint Sequence;
        public double SentAt;
        public readonly List<SoundCue> Cues = new List<SoundCue>();
        public byte[] Encode()
        {
            Validate();
            return Wire.Write(DungeonMessage.Audio, delegate(BinaryWriter w) {
                w.Write(Epoch); w.Write(Sequence); w.Write(SentAt); w.Write((byte)Cues.Count);
                foreach (SoundCue c in Cues) {
                    w.Write(c.Event); w.Write(c.Emitter); w.Write(c.X); w.Write(c.Y); w.Write((byte)c.Switches.Count);
                    foreach (SoundSwitch s in c.Switches) { w.Write(s.Group); w.Write(s.Value); }
                }
            });
        }
        public static SoundBatch Decode(byte[] bytes)
        {
            return Wire.Read(bytes, DungeonMessage.Audio, delegate(BinaryReader r) {
                SoundBatch b = new SoundBatch { Epoch = r.ReadInt32(), Sequence = r.ReadUInt32(), SentAt = r.ReadDouble() };
                int count = r.ReadByte();
                if (count > MaxCues) throw new IOException("Audio batch too large.");
                for (int i = 0; i < count; i++) {
                    SoundCue c = new SoundCue { Event = r.ReadUInt32(), Emitter = r.ReadUInt32(), X = r.ReadSingle(), Y = r.ReadSingle() };
                    int switches = r.ReadByte();
                    if (switches > MaxSwitches) throw new IOException("Too many audio switches.");
                    for (int j = 0; j < switches; j++) c.Switches.Add(new SoundSwitch { Group = r.ReadUInt32(), Value = r.ReadUInt32() });
                    b.Cues.Add(c);
                }
                b.Validate(); return b;
            });
        }
        private void Validate()
        {
            if (Epoch < 1 || Cues.Count > MaxCues) throw new IOException("Invalid audio epoch or count.");
            Wire.Time(SentAt);
            foreach (SoundCue c in Cues) {
                if (c == null || c.Event == 0 || c.Emitter == 0 || c.Switches.Count > MaxSwitches) throw new IOException("Invalid sound cue.");
                Wire.Range(c.X, -100000, 100000); Wire.Range(c.Y, -100000, 100000);
                HashSet<uint> groups = new HashSet<uint>();
                foreach (SoundSwitch s in c.Switches) if (s == null || s.Group == 0 || s.Value == 0 || !groups.Add(s.Group)) throw new IOException("Invalid sound switch.");
            }
        }
    }

    // Never replay old-floor sounds or burst seconds of queued shots after a stall.
    public sealed class SoundSequence
    {
        private int epoch;
        private uint sequence;
        private bool seen;
        private double clockOffset = double.PositiveInfinity;
        private void EnsureEpoch(int currentEpoch)
        {
            if (epoch == currentEpoch) return;
            epoch = currentEpoch; sequence = 0; seen = false; clockOffset = double.PositiveInfinity;
        }
        public void ObserveClock(int currentEpoch, double senderTime, double localTime)
        {
            EnsureEpoch(currentEpoch);
            Wire.Time(senderTime); Wire.Time(localTime);
            clockOffset = Math.Min(clockOffset, localTime - senderTime);
        }
        public bool Accept(SoundBatch batch, int currentEpoch, double latestSnapshotTime, double localTime = -1)
        {
            if (batch.Epoch != currentEpoch) return false;
            EnsureEpoch(currentEpoch);
            if (seen && !Wire.Newer(batch.Sequence, sequence)) return false;
            seen = true; sequence = batch.Sequence;
            // TCP can deliver an old sound before the delayed world frame behind
            // it. A previously calibrated monotonic clock detects that case too.
            if (localTime >= 0 && !double.IsPositiveInfinity(clockOffset)) latestSnapshotTime = Math.Max(latestSnapshotTime, localTime - clockOffset);
            return latestSnapshotTime <= 0 || batch.SentAt >= latestSnapshotTime - 0.3;
        }
    }
}
