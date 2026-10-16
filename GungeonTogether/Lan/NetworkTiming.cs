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
}
