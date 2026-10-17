using System;
using System.Collections.Generic;
using System.IO;

namespace GungeonTogether.Lan
{
    public static class StatePayload
    {
        public static bool Same(byte[] a, byte[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
    public sealed class InventoryState
    {
        public int Epoch, Character;
        public readonly List<int> Guns = new List<int>(), Active = new List<int>(), Passive = new List<int>(), Synergies = new List<int>();
        public byte[] Encode()
        {
            if (Epoch < 1 || !CharacterCatalog.ValidGuest(Character)) throw new IOException("Invalid inventory identity.");
            return Wire.Write(DungeonMessage.Equipment, delegate(BinaryWriter w) {
                w.Write(Epoch); w.Write(Character); Write(w, Guns, 256); Write(w, Active, 64); Write(w, Passive, 1024); Write(w, Synergies, 256);
            });
        }
        public static InventoryState Decode(byte[] bytes)
        {
            return Wire.Read(bytes, DungeonMessage.Equipment, delegate(BinaryReader r) {
                InventoryState state = new InventoryState { Epoch = r.ReadInt32(), Character = r.ReadInt32() };
                if (state.Epoch < 1 || !CharacterCatalog.ValidGuest(state.Character)) throw new IOException("Invalid inventory identity.");
                Read(r, state.Guns, 256); Read(r, state.Active, 64); Read(r, state.Passive, 1024); Read(r, state.Synergies, 256); return state;
            });
        }
        private static void Write(BinaryWriter w, List<int> values, int limit)
        {
            if (values.Count > limit) throw new IOException("Inventory exceeds protocol bound.");
            w.Write(values.Count); foreach (int value in values) { if (value < 0 || value > 1000000) throw new IOException("Invalid inventory ID."); w.Write(value); }
        }
        private static void Read(BinaryReader r, List<int> values, int limit)
        {
            int count = r.ReadInt32(); if (count < 0 || count > limit) throw new IOException("Invalid inventory count.");
            for (int i = 0; i < count; i++) { int value = r.ReadInt32(); if (value < 0 || value > 1000000) throw new IOException("Invalid inventory ID."); values.Add(value); }
        }
    }

    public sealed class SecretRoomState
    {
        // Open, ceiling visible, border visible, ambient-occlusion mesh visible.
        public int X, Y;
        public byte Flags;
    }
    public sealed class SecretRoomsState
    {
        public int Epoch;
        public readonly List<SecretRoomState> Rooms = new List<SecretRoomState>();
        public byte[] Encode()
        {
            Validate();
            return Wire.Write(DungeonMessage.Secrets, delegate(BinaryWriter w) { w.Write(Epoch); w.Write(Rooms.Count); foreach (SecretRoomState room in Rooms) { w.Write(room.X); w.Write(room.Y); w.Write(room.Flags); } });
        }
        public static SecretRoomsState Decode(byte[] bytes)
        {
            return Wire.Read(bytes, DungeonMessage.Secrets, delegate(BinaryReader r) {
                SecretRoomsState value = new SecretRoomsState { Epoch = r.ReadInt32() };
                int count = r.ReadInt32(); if (count < 0 || count > 128) throw new IOException("Invalid secret-room count.");
                for (int i = 0; i < count; i++) value.Rooms.Add(new SecretRoomState { X = r.ReadInt32(), Y = r.ReadInt32(), Flags = r.ReadByte() });
                value.Validate(); return value;
            });
        }
        private void Validate()
        {
            if (Epoch < 1 || Rooms.Count > 128) throw new IOException("Invalid secret-room state.");
            HashSet<long> keys = new HashSet<long>();
            foreach (SecretRoomState room in Rooms) {
                if (room == null || Math.Abs((long)room.X) > 100000 || Math.Abs((long)room.Y) > 100000 || (room.Flags & ~15) != 0 || !keys.Add(((long)(uint)room.X << 32) | (uint)room.Y))
                    throw new IOException("Invalid or duplicate secret-room identity.");
            }
        }
    }
}
