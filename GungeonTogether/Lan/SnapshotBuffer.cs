using System;
using System.Collections.Generic;

namespace GungeonTogether.Lan
{
    // Sender time keeps TCP batches from becoming simultaneous animation frames.
    // Never extrapolate: a stalled connection must not invent movement or bullets.
    public sealed class SnapshotBuffer<T> where T : class
    {
        public const double DefaultDelay = 0.06;
        private sealed class Frame { public T Value; public uint Sequence; public double Time; }
        private readonly List<Frame> frames = new List<Frame>();
        private double delay;
        private readonly bool adaptive;
        private double lastArrival, jitter;
        private double offset, playback = double.NegativeInfinity;
        public int Count { get { return frames.Count; } }
        public double DelaySeconds { get { return delay; } }
        public SnapshotBuffer(double delay, bool adaptive = false) { this.delay = Math.Max(0, delay); this.adaptive = adaptive; }

        public bool Push(T value, uint sequence, double sentAt, double receivedAt)
        {
            if (value == null || !Finite(sentAt) || !Finite(receivedAt) || sentAt < 0) return false;
            if (frames.Count != 0) {
                Frame last = frames[frames.Count - 1];
                if (unchecked((int)(sequence - last.Sequence)) <= 0 || sentAt <= last.Time) return false;
                if (adaptive) {
                    double interval = sentAt - last.Time;
                    jitter += (Math.Abs((receivedAt - lastArrival) - interval) - jitter) * .1;
                    double target = Math.Max(.035, Math.Min(.09, interval * 1.1 + jitter * 2));
                    delay += (target - delay) * (target > delay ? .35 : .05);
                }
                offset = Math.Min(offset, receivedAt - sentAt);
            } else offset = receivedAt - sentAt;
            frames.Add(new Frame { Value = value, Sequence = sequence, Time = sentAt });
            lastArrival = receivedAt;
            if (frames.Count > 32) frames.RemoveAt(0);
            return true;
        }

        public bool Sample(double now, out T from, out T to, out float blend)
        {
            from = to = null; blend = 0;
            if (frames.Count == 0 || !Finite(now)) return false;
            double target = Math.Max(playback, now - offset - delay);
            // Capping the playback clock as well as the position lets playback
            // resume normally after a short network stall.
            target = Math.Min(target, frames[frames.Count - 1].Time);
            playback = target;
            while (frames.Count > 2 && frames[1].Time <= target) frames.RemoveAt(0);
            Frame a = frames[0], b = frames.Count > 1 ? frames[1] : a;
            if (target >= b.Time) a = b;
            from = a.Value; to = b.Value;
            if (b.Time > a.Time) blend = (float)Math.Max(0, Math.Min(1, (target - a.Time) / (b.Time - a.Time)));
            return true;
        }

        public void Clear() { frames.Clear(); playback = double.NegativeInfinity; lastArrival = jitter = 0; }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
