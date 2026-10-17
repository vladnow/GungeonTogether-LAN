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
    public sealed class EffectsState
    {
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
        public int Selected;
        public byte[] Encode()
        {
            if (Kinds.Count > MaxKinds || Cells.Count > MaxCells || Boxes.Count > MaxBoxes || Responses.Count > 8) throw new IOException("Too many scene effects.");
            return Wire.Write(DungeonMessage.SceneEffects, delegate(BinaryWriter w) {
                w.Write(Epoch); w.Write(Sequence); w.Write(Damage); w.Write(Vignette); w.Write(HostX); w.Write(HostY); w.Write(Fire); w.Write(Poison); w.Write(Curse); w.Write(Drain); w.Write(Burning); w.Write(BlockReason); w.Write(Speaker); w.Write(Selected);
                w.Write(Kinds.Count); foreach (string kind in Kinds) w.Write(kind);
                w.Write(Cells.Count); foreach (SurfaceCell c in Cells) { w.Write(c.X); w.Write(c.Y); w.Write(c.Kind); w.Write(c.Flags); w.Write(c.Life); w.Write(c.FireTime); }
                w.Write(Boxes.Count); foreach (DialogueBox b in Boxes) { w.Write(b.Id); w.Write(b.Style); w.Write(b.Text); w.Write(b.X); w.Write(b.Y); w.Write(b.Z); }
                w.Write(Responses.Count); foreach (string response in Responses) w.Write(response);
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
                    Wire.Range(c.Life, -1, 1000000); Wire.Range(c.FireTime, 0, 1000000); s.Cells.Add(c);
                }
                HashSet<int> boxes = new HashSet<int>(); n = Count(r, MaxBoxes);
                for (int i = 0; i < n; i++) {
                    DialogueBox b = new DialogueBox { Id = r.ReadInt32(), Style = Wire.String(r, 32), Text = Wire.String(r, 8192), X = r.ReadSingle(), Y = r.ReadSingle(), Z = r.ReadSingle() };
                    if (!boxes.Add(b.Id) || !ValidStyle(b.Style)) throw new IOException("Invalid dialogue box.");
                    Wire.Range(b.X, -100000, 100000); Wire.Range(b.Y, -100000, 100000); Wire.Range(b.Z, -100000, 100000); s.Boxes.Add(b);
                }
                n = Count(r, 8); for (int i = 0; i < n; i++) s.Responses.Add(Wire.String(r, 2048));
                if (s.Responses.Count > 0 && s.Selected >= s.Responses.Count) throw new IOException("Invalid dialogue selection.");
                return s;
            });
        }
        public static bool ValidStyle(string style) { return style == "TextBox" || style == "InfoBox" || style == "LetterBox" || style == "StoneTablet" || style == "WoodPanel" || style == "ThoughtBubble" || style == "Note"; }
    }
}
