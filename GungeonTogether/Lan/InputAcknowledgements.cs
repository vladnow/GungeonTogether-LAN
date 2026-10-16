using System;

namespace GungeonTogether.Lan
{
    // Uses one local clock: includes input queueing, host processing and state
    // return. It is an application response measurement, not a VPN ICMP ping.
    public sealed class InputAcknowledgements
    {
        private readonly uint[] sequences = new uint[512];
        private readonly double[] times = new double[512];
        private readonly bool[] present = new bool[512];
        private uint lastAck;
        private bool hasAck;
        public double Milliseconds { get; private set; }
        public void Sent(uint sequence, double now)
        {
            int i = (int)(sequence % 512);
            sequences[i] = sequence; times[i] = now; present[i] = true;
        }
        public bool Observe(uint sequence, double now)
        {
            int i = (int)(sequence % 512);
            if (!present[i] || sequences[i] != sequence || (hasAck && !Wire.Newer(sequence, lastAck)) || now < times[i]) return false;
            Milliseconds = (now - times[i]) * 1000; lastAck = sequence; hasAck = true;
            return true;
        }
        public void Clear() { Array.Clear(present, 0, present.Length); hasAck = false; Milliseconds = 0; }
    }
}
