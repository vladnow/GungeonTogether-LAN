using System;
using System.Collections.Generic;
namespace GungeonTogether.Lan
{
    // Bounded visual replay; native physics/damage remain on the host.
    public sealed class MovementPrediction
    {
        private sealed class Command { public uint Sequence; public double Time; public float X, Y; public ulong Buttons; }
        private readonly List<Command> commands = new List<Command>();
        public const double MaxWindow = .10, StaleAfter = .15;
        public const float MaxDistance = .65f;
        private PlayerMotion motion;
        private double received, blockedUntil;
        public int Pending { get { return commands.Count; } }
        public int Resets { get; private set; }
        public int PredictedFrames { get; private set; }
        public PlayerMotion Motion { get { return motion; } }
        public void Record(RemoteInput input, double now)
        {
            if (motion != null && input.Epoch != motion.Epoch) return;
            if (commands.Count > 0 && !Wire.Newer(input.Sequence, commands[commands.Count - 1].Sequence)) return;
            commands.Add(new Command { Sequence = input.Sequence, Time = now, X = input.MoveX, Y = input.MoveY, Buttons = input.Buttons });
            while (commands.Count > 128) commands.RemoveAt(0);
            if ((input.Buttons & ~(1UL << 8)) != 0) { blockedUntil = now + .20; commands.Clear(); }
        }
        public bool Observe(PlayerMotion value, double now)
        {
            if (motion != null && (value.Epoch < motion.Epoch || (value.Epoch == motion.Epoch && !Wire.Newer(value.Sequence, motion.Sequence)))) return false;
            bool reset = motion == null || value.Epoch != motion.Epoch || value.RoomX != motion.RoomX || value.RoomY != motion.RoomY ||
                (value.X - motion.X) * (value.X - motion.X) + (value.Y - motion.Y) * (value.Y - motion.Y) > 2.25f || !value.AllowPrediction;
            if (reset) { commands.Clear(); blockedUntil = now + .05; Resets++; }
            else commands.RemoveAll(delegate(Command c) { return !Wire.Newer(c.Sequence, value.InputSequence); });
            motion = value; received = now; return true;
        }
        public void Offset(double now, bool enabled, out float x, out float y)
        {
            x = y = 0;
            if (!enabled || motion == null || !motion.AllowPrediction || now < blockedUntil || now - received > StaleAfter) return;
            double start = now - MaxWindow;
            for (int i = 0; i < commands.Count; i++) {
                Command c = commands[i];
                if (!Wire.Newer(c.Sequence, motion.InputSequence) || (c.Buttons & ~(1UL << 8)) != 0) continue;
                double end = i + 1 < commands.Count ? commands[i + 1].Time : now;
                double duration = Math.Max(0, end - Math.Max(start, c.Time));
                x += c.X * motion.Speed * (float)duration; y += c.Y * motion.Speed * (float)duration;
            }
            float length = (float)Math.Sqrt(x * x + y * y);
            if (length > MaxDistance) { x *= MaxDistance / length; y *= MaxDistance / length; }
            if (Math.Abs(x) + Math.Abs(y) > .00001f) PredictedFrames++;
        }
        public bool Fresh(double now) { return motion != null && now - received <= StaleAfter; }
        public void Clear() { motion = null; commands.Clear(); received = blockedUntil = 0; }
    }
}
