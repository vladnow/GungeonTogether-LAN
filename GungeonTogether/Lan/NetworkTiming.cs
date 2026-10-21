using System;
namespace GungeonTogether.Lan
{
    public static class NetworkTiming
    {
        // Preserve cadence through frame jitter; do not burst missed ticks.
        public static bool Due(ref double next, double now, double interval)
        {
            if (now + .000001 < next) return false;
            next = now - next > interval ? now + interval : next + interval;
            return true;
        }
    }
    public sealed class AdaptiveSnapshotRate
    {
        public int Hertz { get; private set; }
        public AdaptiveSnapshotRate() { Hertz = 60; }
        private double nextReview, cost;
        public void Observe(double now, double captureMilliseconds, int pendingBytes, double pendingMilliseconds)
        {
            cost += (Math.Max(0, captureMilliseconds) - cost) * .2;
            if (now < nextReview) return;
            if (captureMilliseconds > 8 || cost > 5 || pendingBytes > 49152 || pendingMilliseconds > 50) Hertz = 30;
            else if (cost < 3 && pendingBytes < 8192 && pendingMilliseconds < 20) Hertz = 60;
            nextReview = now + 1;
        }
    }
}
