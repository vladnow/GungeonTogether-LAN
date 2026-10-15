using System;
using System.IO;

namespace GungeonTogether.Lan
{
    public sealed class LanPlayerState
    {
        public float X, Y, Rotation;
        public int SpriteId;
        public bool FlipX, InFoyer;
        public string Collection = "";

        public byte[] Encode()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream)) {
                writer.Write((byte)1); writer.Write(InFoyer);
                writer.Write(X); writer.Write(Y); writer.Write(Rotation);
                writer.Write(SpriteId); writer.Write(FlipX); writer.Write(Collection);
                return stream.ToArray();
            }
        }

        public static LanPlayerState Decode(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream)) {
                if (reader.ReadByte() != 1) throw new IOException("Unknown LAN packet.");
                LanPlayerState state = new LanPlayerState();
                state.InFoyer = reader.ReadBoolean();
                state.X = reader.ReadSingle(); state.Y = reader.ReadSingle(); state.Rotation = reader.ReadSingle();
                state.SpriteId = reader.ReadInt32(); state.FlipX = reader.ReadBoolean(); state.Collection = reader.ReadString();
                if (stream.Position != stream.Length || state.Collection.Length > 256 ||
                    !Finite(state.X) || !Finite(state.Y) || !Finite(state.Rotation) ||
                    Math.Abs(state.X) > 100000 || Math.Abs(state.Y) > 100000 || state.SpriteId < 0 || state.SpriteId > 100000)
                    throw new IOException("Invalid player state.");
                return state;
            }
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
