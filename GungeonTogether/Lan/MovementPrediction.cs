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
        // Keyboard WASD also drives native menu navigation. Reload/weapon cycling
        // and navigation bits do not change walking physics; do not disable
        // replay merely because those bindings are pressed with a movement key.
        private const ulong MotionActions = (1UL << 9) | (1UL << 10) | (1UL << 12) | (1UL << 28);
        private PlayerMotion motion;
        private double received, blockedUntil;
        private float correctionX, correctionY;
        private double correctedAt;
        public const double CorrectionDuration = .06;
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
            if ((input.Buttons & MotionActions) != 0) { blockedUntil = now + .20; commands.Clear(); }
        }
        public bool Observe(PlayerMotion value, double now)
        {
            if (motion != null && (value.Epoch < motion.Epoch || (value.Epoch == motion.Epoch && !Wire.Newer(value.Sequence, motion.Sequence)))) return false;
            bool roomChange = motion != null && value.Epoch == motion.Epoch && (value.RoomX != motion.RoomX || value.RoomY != motion.RoomY);
            bool reset = motion == null || value.Epoch != motion.Epoch || roomChange ||
                (value.X - motion.X) * (value.X - motion.X) + (value.Y - motion.Y) * (value.Y - motion.Y) > 2.25f || !value.AllowPrediction;
            float oldX = 0, oldY = 0;
            if (!reset && motion.AllowPrediction) { Offset(now, true, out oldX, out oldY); oldX += motion.X; oldY += motion.Y; }
            if (reset) { if (roomChange && motion != null && (value.X - motion.X) * (value.X - motion.X) + (value.Y - motion.Y) * (value.Y - motion.Y) < 2.25f) { commands.RemoveAll(delegate(Command c) { return !Wire.Newer(c.Sequence, value.InputSequence); }); blockedUntil = now; } else { commands.Clear(); blockedUntil = now + .05; } correctionX = correctionY = 0; Resets++; }
            else commands.RemoveAll(delegate(Command c) { return !Wire.Newer(c.Sequence, value.InputSequence); });
            bool smooth = !reset && motion.AllowPrediction && now - received <= StaleAfter;
            motion = value; received = now;
            if (smooth) {
                correctionX = correctionY = 0;
                float newX, newY; Offset(now, true, out newX, out newY);
                correctionX = oldX - (value.X + newX); correctionY = oldY - (value.Y + newY);
                float distance = (float)Math.Sqrt(correctionX * correctionX + correctionY * correctionY);
                if (distance > MaxDistance) { correctionX *= MaxDistance / distance; correctionY *= MaxDistance / distance; }
                correctedAt = now;
            }
            return true;
        }
        public void Offset(double now, bool enabled, out float x, out float y)
        {
            x = y = 0;
            if (!enabled || motion == null || !motion.AllowPrediction || now < blockedUntil || now - received > StaleAfter) return;
            double start = now - MaxWindow;
            for (int i = 0; i < commands.Count; i++) {
                Command c = commands[i];
                if (!Wire.Newer(c.Sequence, motion.InputSequence) || (c.Buttons & MotionActions) != 0) continue;
                double end = i + 1 < commands.Count ? commands[i + 1].Time : now;
                double duration = Math.Max(0, end - Math.Max(start, c.Time));
                x += c.X * motion.Speed * (float)duration; y += c.Y * motion.Speed * (float)duration;
            }
            // Reconciliation is visual and wall-clipped by NativeMovement. An
            // acknowledged packet must not snap the player/camera backwards.
            float remaining = (float)Math.Max(0, 1 - (now - correctedAt) / CorrectionDuration);
            x += correctionX * remaining; y += correctionY * remaining;
            float length = (float)Math.Sqrt(x * x + y * y);
            if (length > MaxDistance) { x *= MaxDistance / length; y *= MaxDistance / length; }
            if (Math.Abs(x) + Math.Abs(y) > .00001f) PredictedFrames++;
        }
        public bool Fresh(double now) { return motion != null && now - received <= StaleAfter; }
        public void Clear() { motion = null; commands.Clear(); received = blockedUntil = correctedAt = 0; correctionX = correctionY = 0; }
    }
    // Smooth only changes in the native camera anchor (animation/pixel rounding),
    // never the locally predicted player position itself.
    public sealed class CameraOffsetFilter
    {
        private float x, y; private double last; private bool initialized;
        public void Update(float targetX, float targetY, double now, out float resultX, out float resultY)
        {
            if (!initialized || now < last || Math.Abs(targetX - x) + Math.Abs(targetY - y) > 2) { x = targetX; y = targetY; initialized = true; }
            else { float gain = (float)(1 - Math.Exp(-Math.Min(.1, now - last) * 25)); x += (targetX - x) * gain; y += (targetY - y) * gain; }
            last = now; resultX = x; resultY = y;
        }
        public void Clear() { initialized = false; }
    }

}
