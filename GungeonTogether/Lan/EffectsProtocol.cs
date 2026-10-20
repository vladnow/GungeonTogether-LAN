using System;
using System.Collections.Generic;
using System.IO;

namespace GungeonTogether.Lan
{
    public sealed class SurfaceCell
    {
        public int X, Y;
        public byte Kind, Flags;
        public float Life, FireTime;
    }
    public sealed class DialogueBox
    {
        public int Id;
        public string Style = "InfoBox", Text = "";
        public float X, Y, Z;
    }
    public sealed class WorldLabel { public int Id; public float X, Y, Z; public string Text = ""; }
    public sealed class BossBarState { public byte Slot; public float Health; public string Name = ""; }
    public sealed class DoorState { public int X, Y; public bool Open; }
    public sealed class MapRoomState { public int X, Y; public byte Visibility; }
    public sealed class EffectsState
    {
        public static float NativeClock(float value, float minimum) { return float.IsNaN(value) || float.IsInfinity(value) ? minimum : Math.Max(minimum, Math.Min(1000000, value)); }
        public const int MaxCells = 8192, MaxKinds = 32, MaxBoxes = 8;
        public int Epoch, Speaker;
        public uint Sequence, Damage;
        public float Vignette, HostX, HostY;
        public float Fire, Poison, Curse, Drain;
        public bool Burning;
        public string BlockReason = "";
        public readonly List<string> Kinds = new List<string>();
        public readonly List<SurfaceCell> Cells = new List<SurfaceCell>();
        public readonly List<DialogueBox> Boxes = new List<DialogueBox>();
        public readonly List<string> Responses = new List<string>();
        public readonly List<WorldLabel> Labels = new List<WorldLabel>();
        public readonly List<BossBarState> Bosses = new List<BossBarState>();
        public readonly List<DoorState> Doors = new List<DoorState>();
        public readonly List<MapRoomState> MapRooms = new List<MapRoomState>();
        public int Selected;
        public byte[] Encode()
        {
            if (Kinds.Count > MaxKinds || Cells.Count > MaxCells || Boxes.Count > MaxBoxes || Responses.Count > 8 || Labels.Count > 64 || Bosses.Count > 3 || MapRooms.Count > 512 || Doors.Count > 1024) throw new IOException("Too many scene effects.");
            return Wire.Write(DungeonMessage.SceneEffects, delegate(BinaryWriter w) {
                w.Write(Epoch); w.Write(Sequence); w.Write(Damage); w.Write(Vignette); w.Write(HostX); w.Write(HostY); w.Write(Fire); w.Write(Poison); w.Write(Curse); w.Write(Drain); w.Write(Burning); w.Write(BlockReason); w.Write(Speaker); w.Write(Selected);
                w.Write(Kinds.Count); foreach (string kind in Kinds) w.Write(kind);
                w.Write(Cells.Count); foreach (SurfaceCell c in Cells) { w.Write(c.X); w.Write(c.Y); w.Write(c.Kind); w.Write(c.Flags); w.Write(c.Life); w.Write(c.FireTime); }
                w.Write(Boxes.Count); foreach (DialogueBox b in Boxes) { w.Write(b.Id); w.Write(b.Style); w.Write(b.Text); w.Write(b.X); w.Write(b.Y); w.Write(b.Z); }
                w.Write(Responses.Count); foreach (string response in Responses) w.Write(response);
                w.Write(Labels.Count); foreach (WorldLabel label in Labels) { w.Write(label.Id); w.Write(label.X); w.Write(label.Y); w.Write(label.Z); w.Write(label.Text); }
                w.Write(Bosses.Count); foreach (BossBarState boss in Bosses) { w.Write(boss.Slot); w.Write(boss.Health); w.Write(boss.Name); }
                w.Write(Doors.Count); foreach (DoorState door in Doors) { w.Write(door.X); w.Write(door.Y); w.Write(door.Open); }
                w.Write(MapRooms.Count); foreach (MapRoomState room in MapRooms) { w.Write(room.X); w.Write(room.Y); w.Write(room.Visibility); }
            });
        }
        private static int Count(BinaryReader r, int maximum) { int n = r.ReadInt32(); if (n < 0 || n > maximum) throw new IOException("Invalid scene effect count."); return n; }
        public static EffectsState Decode(byte[] bytes)
        {
            return Wire.Read(bytes, DungeonMessage.SceneEffects, delegate(BinaryReader r) {
                EffectsState s = new EffectsState { Epoch = r.ReadInt32(), Sequence = r.ReadUInt32(), Damage = r.ReadUInt32(), Vignette = r.ReadSingle(), HostX = r.ReadSingle(), HostY = r.ReadSingle(), Fire = r.ReadSingle(), Poison = r.ReadSingle(), Curse = r.ReadSingle(), Drain = r.ReadSingle(), Burning = r.ReadBoolean(), BlockReason = Wire.String(r, 256), Speaker = r.ReadInt32(), Selected = r.ReadInt32() };
                if (s.Epoch < 1 || s.Speaker < 0 || s.Speaker > 1 || s.Selected < 0 || s.Selected > 7) throw new IOException("Invalid scene effect header.");
                Wire.Range(s.Vignette, 0, 10);
                Wire.Range(s.HostX, -100000, 100000); Wire.Range(s.HostY, -100000, 100000);
                Wire.Range(s.Fire, 0, 1); Wire.Range(s.Poison, 0, 1); Wire.Range(s.Curse, 0, 1); Wire.Range(s.Drain, 0, 1);
                int n = Count(r, MaxKinds); for (int i = 0; i < n; i++) s.Kinds.Add(Wire.String(r, 256));
                HashSet<long> cells = new HashSet<long>();
                n = Count(r, MaxCells);
                for (int i = 0; i < n; i++) {
                    SurfaceCell c = new SurfaceCell { X = r.ReadInt32(), Y = r.ReadInt32(), Kind = r.ReadByte(), Flags = r.ReadByte(), Life = r.ReadSingle(), FireTime = r.ReadSingle() };
                    if (c.X < 0 || c.X > 400000 || c.Y < 0 || c.Y > 400000 || c.Kind >= s.Kinds.Count || (c.Flags & ~7) != 0 || !cells.Add(((long)c.X << 32) | (uint)c.Y)) throw new IOException("Invalid surface cell.");
                    Wire.Range(c.Life, -1, 1000000, "surface lifetime"); Wire.Range(c.FireTime, 0, 1000000, "surface fire time"); s.Cells.Add(c);
                }
                HashSet<int> boxes = new HashSet<int>(); n = Count(r, MaxBoxes);
                for (int i = 0; i < n; i++) {
                    DialogueBox b = new DialogueBox { Id = r.ReadInt32(), Style = Wire.String(r, 32), Text = Wire.String(r, 8192), X = r.ReadSingle(), Y = r.ReadSingle(), Z = r.ReadSingle() };
                    if (!boxes.Add(b.Id) || !ValidStyle(b.Style)) throw new IOException("Invalid dialogue box.");
                    Wire.Range(b.X, -100000, 100000); Wire.Range(b.Y, -100000, 100000); Wire.Range(b.Z, -100000, 100000); s.Boxes.Add(b);
                }
                n = Count(r, 8); for (int i = 0; i < n; i++) s.Responses.Add(Wire.String(r, 2048));
                if (s.Responses.Count > 0 && s.Selected >= s.Responses.Count) throw new IOException("Invalid dialogue selection.");
                n = Count(r, 64); HashSet<int> labelIds = new HashSet<int>();
                for (int i = 0; i < n; i++) { WorldLabel label = new WorldLabel { Id = r.ReadInt32(), X = r.ReadSingle(), Y = r.ReadSingle(), Z = r.ReadSingle(), Text = Wire.String(r, 2048) }; if (!labelIds.Add(label.Id)) throw new IOException("Duplicate world label."); Wire.Range(label.X, -100000, 100000, "label X"); Wire.Range(label.Y, -100000, 100000, "label Y"); Wire.Range(label.Z, -100000, 100000, "label Z"); s.Labels.Add(label); }
                n = Count(r, 3); HashSet<byte> slots = new HashSet<byte>();
                for (int i = 0; i < n; i++) { BossBarState boss = new BossBarState { Slot = r.ReadByte(), Health = r.ReadSingle(), Name = Wire.String(r, 256) }; if (boss.Slot > 2 || !slots.Add(boss.Slot)) throw new IOException("Invalid boss bar slot."); Wire.Range(boss.Health, 0, 1, "boss health"); s.Bosses.Add(boss); }
                n = Count(r, 1024); HashSet<long> doorIds = new HashSet<long>();
                for (int i = 0; i < n; i++) { DoorState door = new DoorState { X = r.ReadInt32(), Y = r.ReadInt32(), Open = r.ReadBoolean() }; if (Math.Abs((long)door.X) > 1600000 || Math.Abs((long)door.Y) > 1600000 || !doorIds.Add(((long)door.X << 32) | (uint)door.Y)) throw new IOException("Invalid door identity."); s.Doors.Add(door); }
                n = Count(r, 512); HashSet<long> rooms = new HashSet<long>();
                for (int i = 0; i < n; i++) { MapRoomState room = new MapRoomState { X = r.ReadInt32(), Y = r.ReadInt32(), Visibility = r.ReadByte() }; if (Math.Abs((long)room.X) > 100000 || Math.Abs((long)room.Y) > 100000 || room.Visibility > 8 || !rooms.Add(((long)room.X << 32) | (uint)room.Y)) throw new IOException("Invalid map room."); s.MapRooms.Add(room); }
                return s;
            });
        }
        public static bool ValidStyle(string style) { return style == "TextBox" || style == "InfoBox" || style == "LetterBox" || style == "StoneTablet" || style == "WoodPanel" || style == "ThoughtBubble" || style == "Note"; }
    }
}
