using System;
using System.IO;

namespace GungeonTogether.Lan
{
    public sealed class LanPlayerState
    {
        public float X, Y, Z, Rotation, Height;
        public float ScaleX = 1, ScaleY = 1;
        public uint Sequence;
        public double SentAt;
        public int SpriteId;
        public bool FlipX, InFoyer;
        public string Collection = "";
        public string CollectionGuid = "";
        public int Character = 1;

        public byte[] Encode()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream)) {
                writer.Write((byte)1); writer.Write(InFoyer);
                writer.Write(X); writer.Write(Y); writer.Write(Rotation);
                writer.Write(SpriteId); writer.Write(FlipX); writer.Write(Collection);
                writer.Write(Sequence); writer.Write(SentAt); writer.Write(CollectionGuid); writer.Write(Character);
                writer.Write(Z); writer.Write(Height); writer.Write(ScaleX); writer.Write(ScaleY);
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
                state.Sequence = reader.ReadUInt32(); state.SentAt = reader.ReadDouble(); state.CollectionGuid = reader.ReadString(); state.Character = reader.ReadInt32();
                state.Z = reader.ReadSingle(); state.Height = reader.ReadSingle(); state.ScaleX = reader.ReadSingle(); state.ScaleY = reader.ReadSingle();
                if (!Finite(state.Z) || Math.Abs(state.Z) > 100000 || !Finite(state.Height) || Math.Abs(state.Height) > 1000 || !Finite(state.ScaleX) || Math.Abs(state.ScaleX) > 1000 || !Finite(state.ScaleY) || Math.Abs(state.ScaleY) > 1000) throw new IOException("Invalid foyer sprite transform.");
                if (double.IsNaN(state.SentAt) || double.IsInfinity(state.SentAt) || state.SentAt < 0 || state.SentAt > 1e12 || state.CollectionGuid.Length > 256 || !CharacterCatalog.Valid(state.Character)) throw new IOException("Invalid foyer frame metadata.");
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
