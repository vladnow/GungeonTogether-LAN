using System;
using System.Collections.Generic;
using System.IO;

namespace GungeonTogether.Lan
{
    public enum DungeonMessage : byte { Input = 2, Level = 3, Ready = 4, Snapshot = 5, Collection = 6, Stop = 7 }

    public sealed class RemoteInput
    {
        public uint Sequence;
        public int Epoch;
        public float MoveX, MoveY, AimX, AimY;
        public ulong Buttons;
        public byte[] Encode()
        {
            return Wire.Write(DungeonMessage.Input, delegate(BinaryWriter w) { w.Write(Epoch); w.Write(Sequence); w.Write(MoveX); w.Write(MoveY); w.Write(AimX); w.Write(AimY); w.Write(Buttons); });
        }
        public static RemoteInput Decode(byte[] bytes)
        {
            return Wire.Read(bytes, DungeonMessage.Input, delegate(BinaryReader r) {
                RemoteInput value = new RemoteInput { Epoch = r.ReadInt32(), Sequence = r.ReadUInt32(), MoveX = r.ReadSingle(), MoveY = r.ReadSingle(), AimX = r.ReadSingle(), AimY = r.ReadSingle(), Buttons = r.ReadUInt64() };
                Wire.Range(value.MoveX, -1, 1); Wire.Range(value.MoveY, -1, 1); Wire.Range(value.AimX, -1, 1); Wire.Range(value.AimY, -1, 1);
                if ((value.Buttons >> 41) != 0 || value.Epoch < 1) throw new IOException("Invalid remote controls.");
                return value;
            });
        }
    }

    public sealed class LevelCommand
    {
        public int Epoch, Seed;
        public string Scene = "tt_castle", LayoutHash = "", GenerationData = "";
        public byte[] Encode() { return Wire.Write(DungeonMessage.Level, delegate(BinaryWriter w) { w.Write(Epoch); w.Write(Seed); w.Write(Scene); w.Write(LayoutHash); w.Write(GenerationData); }); }
        public static LevelCommand Decode(byte[] bytes)
        {
            return Wire.Read(bytes, DungeonMessage.Level, delegate(BinaryReader r) {
                LevelCommand value = new LevelCommand { Epoch = r.ReadInt32(), Seed = r.ReadInt32(), Scene = Wire.String(r, 128), LayoutHash = Wire.String(r, 64), GenerationData = Wire.String(r, 131072) };
                if (value.Epoch < 1 || value.Seed <= 0 || !value.Scene.StartsWith("tt", StringComparison.Ordinal) || value.Scene.IndexOfAny(new char[] { '/', '\\', '.' }) >= 0 || value.LayoutHash.Length != 64)
                    throw new IOException("Invalid dungeon command.");
                return value;
            });
        }
    }

    public sealed class CollectionInfo
    {
        public int Epoch, Id;
        public string Guid = "", Name = "", Asset = "";
        public byte[] Encode() { return Wire.Write(DungeonMessage.Collection, delegate(BinaryWriter w) { w.Write(Epoch); w.Write(Id); w.Write(Guid); w.Write(Name); w.Write(Asset); }); }
        public static CollectionInfo Decode(byte[] bytes)
        {
            return Wire.Read(bytes, DungeonMessage.Collection, delegate(BinaryReader r) {
                CollectionInfo value = new CollectionInfo { Epoch = r.ReadInt32(), Id = r.ReadInt32(), Guid = Wire.String(r, 256), Name = Wire.String(r, 256), Asset = Wire.String(r, 256) };
                if (value.Id < 1 || value.Id > 4096 || value.Epoch < 1) throw new IOException("Invalid sprite collection ID.");
                return value;
            });
        }
    }

    public sealed class SpriteState
    {
        public int Id, CollectionId, SpriteId, SortingOrder;
        public float X, Y, Z, Rotation, ScaleX, ScaleY, ScaleZ, Height;
        public uint Color;
        public byte Flags, Layer, Kind, Anchor;
        public float Width, HeightPixels, ClipX, ClipY, ClipRight = 1, ClipTop = 1;
        public void Write(BinaryWriter w)
        {
            w.Write(Id); w.Write(CollectionId); w.Write(SpriteId); w.Write(SortingOrder);
            w.Write(X); w.Write(Y); w.Write(Z); w.Write(Rotation); w.Write(ScaleX); w.Write(ScaleY); w.Write(ScaleZ); w.Write(Height); w.Write(Color); w.Write(Flags); w.Write(Layer);
            w.Write(Kind); w.Write(Anchor); w.Write(Width); w.Write(HeightPixels); w.Write(ClipX); w.Write(ClipY); w.Write(ClipRight); w.Write(ClipTop);
        }
        public static SpriteState Read(BinaryReader r)
        {
            SpriteState value = new SpriteState { Id = r.ReadInt32(), CollectionId = r.ReadInt32(), SpriteId = r.ReadInt32(), SortingOrder = r.ReadInt32(), X = r.ReadSingle(), Y = r.ReadSingle(), Z = r.ReadSingle(), Rotation = r.ReadSingle(), ScaleX = r.ReadSingle(), ScaleY = r.ReadSingle(), ScaleZ = r.ReadSingle(), Height = r.ReadSingle(), Color = r.ReadUInt32(), Flags = r.ReadByte(), Layer = r.ReadByte() };
            value.Kind = r.ReadByte(); value.Anchor = r.ReadByte(); value.Width = r.ReadSingle(); value.HeightPixels = r.ReadSingle(); value.ClipX = r.ReadSingle(); value.ClipY = r.ReadSingle(); value.ClipRight = r.ReadSingle(); value.ClipTop = r.ReadSingle();
            if (value.Id == 0 || value.CollectionId < 1 || value.CollectionId > 4096 || value.SpriteId < 0 || value.SpriteId > 100000 || (value.Flags & ~3) != 0 || value.Layer > 31) throw new IOException("Invalid sprite snapshot.");
            Wire.Range(value.X, -100000, 100000); Wire.Range(value.Y, -100000, 100000); Wire.Range(value.Z, -100000, 100000);
            Wire.Range(value.Rotation, -3600, 3600); Wire.Range(value.ScaleX, -1000, 1000); Wire.Range(value.ScaleY, -1000, 1000); Wire.Range(value.ScaleZ, -1000, 1000); Wire.Range(value.Height, -1000, 1000);
            if (value.Kind > 3 || value.Anchor > 8) throw new IOException("Unknown sprite geometry.");
            Wire.Range(value.Width, 0, 100000); Wire.Range(value.HeightPixels, 0, 100000); Wire.Range(value.ClipX, -1000, 1000); Wire.Range(value.ClipY, -1000, 1000); Wire.Range(value.ClipRight, -1000, 1000); Wire.Range(value.ClipTop, -1000, 1000);
            return value;
        }
    }

    public sealed class WorldSnapshot
    {
        public int Epoch;
        public uint Sequence;
        public float CameraX, CameraY, CameraZ, CameraSize, PlayerX, PlayerY, Health, Armor;
        public int Ammo;
        public bool Paused;
        public readonly List<SpriteState> Sprites = new List<SpriteState>();
        public byte[] Encode()
        {
            if (Sprites.Count > 4096) throw new IOException("Too many replicated sprites.");
            return Wire.Write(DungeonMessage.Snapshot, delegate(BinaryWriter w) {
                w.Write(Epoch); w.Write(Sequence); w.Write(CameraX); w.Write(CameraY); w.Write(CameraZ); w.Write(CameraSize);
                w.Write(PlayerX); w.Write(PlayerY); w.Write(Health); w.Write(Armor); w.Write(Ammo); w.Write(Paused); w.Write(Sprites.Count);
                foreach (SpriteState sprite in Sprites) sprite.Write(w);
            });
        }
        public static WorldSnapshot Decode(byte[] bytes)
        {
            return Wire.Read(bytes, DungeonMessage.Snapshot, delegate(BinaryReader r) {
                WorldSnapshot value = new WorldSnapshot { Epoch = r.ReadInt32(), Sequence = r.ReadUInt32(), CameraX = r.ReadSingle(), CameraY = r.ReadSingle(), CameraZ = r.ReadSingle(), CameraSize = r.ReadSingle(), PlayerX = r.ReadSingle(), PlayerY = r.ReadSingle(), Health = r.ReadSingle(), Armor = r.ReadSingle(), Ammo = r.ReadInt32(), Paused = r.ReadBoolean() };
                Wire.Range(value.CameraX, -100000, 100000); Wire.Range(value.CameraY, -100000, 100000); Wire.Range(value.CameraZ, -100000, 100000); Wire.Range(value.CameraSize, 0.01f, 10000);
                Wire.Range(value.PlayerX, -100000, 100000); Wire.Range(value.PlayerY, -100000, 100000); Wire.Range(value.Health, 0, 10000); Wire.Range(value.Armor, 0, 10000);
                int count = r.ReadInt32();
                if (count < 0 || count > 4096 || value.Epoch < 1 || value.Ammo < 0) throw new IOException("Invalid world snapshot size.");
                HashSet<int> ids = new HashSet<int>();
                for (int i = 0; i < count; i++) { SpriteState sprite = SpriteState.Read(r); if (!ids.Add(sprite.Id)) throw new IOException("Duplicate entity ID."); value.Sprites.Add(sprite); }
                return value;
            });
        }
    }

    public static class Wire
    {
        public static byte[] Write(DungeonMessage type, Action<BinaryWriter> action)
        { using (MemoryStream ms = new MemoryStream()) using (BinaryWriter writer = new BinaryWriter(ms)) { writer.Write((byte)type); action(writer); return ms.ToArray(); } }
        public static T Read<T>(byte[] bytes, DungeonMessage type, Func<BinaryReader, T> action)
        {
            using (MemoryStream ms = new MemoryStream(bytes)) using (BinaryReader reader = new BinaryReader(ms)) {
                if (reader.ReadByte() != (byte)type) throw new IOException("Unexpected dungeon packet.");
                T result = action(reader); if (ms.Position != ms.Length) throw new IOException("Trailing packet bytes."); return result;
            }
        }
        public static string String(BinaryReader r, int max) { string value = r.ReadString(); if (value.Length > max) throw new IOException("String too long."); return value; }
        public static void Range(float value, float min, float max) { if (float.IsNaN(value) || float.IsInfinity(value) || value < min || value > max) throw new IOException("Invalid numeric snapshot value."); }
        public static bool Newer(uint value, uint previous) { return unchecked((int)(value - previous)) > 0; }
    }
}
