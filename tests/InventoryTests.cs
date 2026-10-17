using System;
using System.IO;
using GungeonTogether.Lan;
public static class InventoryTests
{
    static int checks;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; Console.WriteLine("PASS " + name); }
    static void Reject(Action action, string name) { bool rejected = false; try { action(); } catch (IOException) { rejected = true; } Check(rejected, name); }
    public static void Main()
    {
        Check(!StatePayload.Same(new byte[] { 1 }, null) && !StatePayload.Same(null, new byte[] { 1 }), "first presentation payload differs from empty session cache");
        Check(StatePayload.Same(null, null) && StatePayload.Same(new byte[] { 1, 2 }, new byte[] { 1, 2 }), "unchanged cumulative presentation state is coalesced");
        Check(!StatePayload.Same(new byte[] { 1 }, new byte[] { 1, 2 }), "different length presentation payload is resent");
        Check(!StatePayload.Same(new byte[] { 1, 2 }, new byte[] { 1, 3 }), "changed pickup or reveal payload is resent");
        InventoryState inventory = new InventoryState { Epoch = 2, Character = 4 };
        inventory.Guns.Add(15); inventory.Guns.Add(99); inventory.Active.Add(108); inventory.Passive.Add(42); inventory.Passive.Add(42); inventory.Synergies.Add(7);
        InventoryState copy = InventoryState.Decode(inventory.Encode());
        Check(copy.Epoch == 2 && copy.Character == 4, "authoritative guest character and floor round trip");
        Check(copy.Guns.Count == 2 && copy.Guns[0] == 15 && copy.Guns[1] == 99, "complete ordered gun inventory round trip");
        Check(copy.Active.Count == 1 && copy.Active[0] == 108, "active inventory round trip");
        Check(copy.Passive.Count == 2 && copy.Passive[0] == copy.Passive[1], "stacked passive items preserved");
        Check(copy.Synergies.Count == 1 && copy.Synergies[0] == 7, "active synergy identifiers preserved");
        Check(InventoryState.Decode(new InventoryState { Epoch = 1 }.Encode()).Guns.Count == 0, "empty ghost inventory supported");
        inventory.Epoch = 0; Reject(delegate { inventory.Encode(); }, "invalid inventory floor rejected"); inventory.Epoch = 2;
        inventory.Character = 8; Reject(delegate { inventory.Encode(); }, "invalid guest identity rejected"); inventory.Character = 4;
        inventory.Guns.Add(-1); Reject(delegate { inventory.Encode(); }, "negative pickup identifier rejected"); inventory.Guns.RemoveAt(2);
        inventory.Active[0] = 1000001; Reject(delegate { inventory.Encode(); }, "oversized pickup identifier rejected"); inventory.Active[0] = 108;
        for (int i = 0; i < 256; i++) inventory.Guns.Add(i);
        Reject(delegate { inventory.Encode(); }, "gun list bound enforced"); inventory.Guns.Clear();
        for (int i = 0; i < 65; i++) inventory.Active.Add(1);
        Reject(delegate { inventory.Encode(); }, "active item list bound enforced"); inventory.Active.Clear();
        for (int i = 0; i < 1025; i++) inventory.Passive.Add(1);
        Reject(delegate { inventory.Encode(); }, "passive list bound enforced"); inventory.Passive.Clear();
        for (int i = 0; i < 257; i++) inventory.Synergies.Add(1);
        Reject(delegate { inventory.Encode(); }, "synergy list bound enforced"); inventory.Synergies.Clear();
        byte[] bytes = inventory.Encode(); Array.Resize(ref bytes, bytes.Length + 1);
        Reject(delegate { InventoryState.Decode(bytes); }, "inventory trailing bytes rejected");
        Reject(delegate { InventoryState.Decode(new byte[] { 11, 1 }); }, "truncated inventory rejected");
        SecretRoomsState rooms = new SecretRoomsState { Epoch = 3 }; rooms.Rooms.Add(new SecretRoomState { X = 20, Y = 40, Flags = 1 }); rooms.Rooms.Add(new SecretRoomState { X = 30, Y = 50, Flags = 14 });
        SecretRoomsState decoded = SecretRoomsState.Decode(rooms.Encode());
        Check(decoded.Epoch == 3 && decoded.Rooms.Count == 2, "complete cumulative secret state round trip");
        Check(decoded.Rooms[0].Flags == 1 && decoded.Rooms[1].Flags == 14, "open and closed secret mesh flags remain distinct");
        rooms.Rooms.Add(new SecretRoomState { X = 20, Y = 40 }); Reject(delegate { rooms.Encode(); }, "duplicate secret identity rejected"); rooms.Rooms.RemoveAt(2);
        rooms.Rooms[0].Flags = 16; Reject(delegate { rooms.Encode(); }, "unknown secret flags rejected"); rooms.Rooms[0].Flags = 1;
        rooms.Rooms[0].X = int.MinValue; Reject(delegate { rooms.Encode(); }, "secret coordinates checked without integer overflow"); rooms.Rooms[0].X = 20;
        for (int i = 0; i < 128; i++) rooms.Rooms.Add(new SecretRoomState { X = i, Y = 100 });
        Reject(delegate { rooms.Encode(); }, "secret list bound enforced"); rooms.Rooms.Clear();
        rooms.Epoch = 0; Reject(delegate { rooms.Encode(); }, "invalid secret floor rejected"); rooms.Epoch = 1;
        Check(SecretRoomsState.Decode(rooms.Encode()).Rooms.Count == 0, "floor with no secrets supported");
        bytes = rooms.Encode(); Array.Resize(ref bytes, bytes.Length + 1); Reject(delegate { SecretRoomsState.Decode(bytes); }, "secret trailing bytes rejected");
        byte[] negativeCount = Wire.Write(DungeonMessage.Secrets, delegate(BinaryWriter w) { w.Write(1); w.Write(-1); });
        Reject(delegate { SecretRoomsState.Decode(negativeCount); }, "negative incoming secret count rejected");
        byte[] hugeCount = Wire.Write(DungeonMessage.Equipment, delegate(BinaryWriter w) { w.Write(1); w.Write(4); w.Write(int.MaxValue); });
        Reject(delegate { InventoryState.Decode(hugeCount); }, "incoming inventory bound enforced before allocation");
        Console.WriteLine("INVENTORY_TESTS_PASS: " + checks);
    }
}
