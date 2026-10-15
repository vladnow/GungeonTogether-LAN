using System;
using System.IO;
using GungeonTogether.Lan;

internal static class DungeonTests
{
    private static int passed;
    private static void Assert(bool condition, string name) { if (!condition) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
    private static void Reject(Action action, string name) { bool failed = false; try { action(); } catch (IOException) { failed = true; } Assert(failed, name); }
    private static SpriteState Sprite(int id) { return new SpriteState { Id = id, CollectionId = 1, SpriteId = 7, ScaleX = 1, ScaleY = 1, ScaleZ = 1, X = 23.5f, Y = 42, Color = 0xffffffff }; }
    private static WorldSnapshot Snapshot() { return new WorldSnapshot { Epoch = 1, Sequence = 2, CameraSize = 7.5f, Health = 3, Armor = 1, Ammo = 100 }; }
    public static int Main()
    {
        try {
            RemoteInput input = new RemoteInput { Epoch = 1, Sequence = 7, MoveX = 0.5f, MoveY = -1, AimX = 1, Buttons = (1UL << 8) | (1UL << 9) | (1UL << 11) };
            RemoteInput decoded = RemoteInput.Decode(input.Encode());
            Assert(decoded.Sequence == 7 && decoded.MoveX == 0.5f && decoded.MoveY == -1 && decoded.AimX == 1 && decoded.Buttons == input.Buttons, "remote native-controller payload round trip");
            input.MoveX = 2; Reject(delegate { RemoteInput.Decode(input.Encode()); }, "out-of-range movement rejected");
            input.MoveX = float.NaN; Reject(delegate { RemoteInput.Decode(input.Encode()); }, "NaN input rejected");
            input.MoveX = 0; input.Buttons = 1UL << 60; Reject(delegate { RemoteInput.Decode(input.Encode()); }, "unknown action bits rejected");
            input.Buttons = 0; input.Epoch = 0; Reject(delegate { RemoteInput.Decode(input.Encode()); }, "zero input epoch rejected");
            LevelCommand level = new LevelCommand { Epoch = 3, Seed = 123456, Scene = "tt_castle", LayoutHash = new string('a', 64) };
            LevelCommand l = LevelCommand.Decode(level.Encode()); Assert(l.Epoch == 3 && l.Seed == 123456 && l.Scene == "tt_castle" && l.LayoutHash == level.LayoutHash, "seed / scene / layout barrier payload round trip");
            level.GenerationData = "{\"flags\":[1,2],\"rooms\":[]}";
            Assert(LevelCommand.Decode(level.Encode()).GenerationData == level.GenerationData, "generation context included in level barrier");
            level.GenerationData = new string('a', 131073); Reject(delegate { LevelCommand.Decode(level.Encode()); }, "generation context size budget enforced");
            level.GenerationData = "";
            level.Scene = "tt_../../outside"; Reject(delegate { LevelCommand.Decode(level.Encode()); }, "unsafe scene name rejected");
            level.Scene = "tt_castle"; level.LayoutHash = "short"; Reject(delegate { LevelCommand.Decode(level.Encode()); }, "incomplete layout fingerprint rejected");
            CollectionInfo info = new CollectionInfo { Epoch = 1, Id = 4, Guid = "native-sprite-guid", Name = "Projectiles", Asset = "collection" };
            CollectionInfo c = CollectionInfo.Decode(info.Encode()); Assert(c.Id == 4 && c.Guid == info.Guid && c.Name == "Projectiles", "local sprite resource descriptor round trip");
            info.Id = 5000; Reject(delegate { CollectionInfo.Decode(info.Encode()); }, "collection ID limit enforced");
            WorldSnapshot state = Snapshot();
            for (int i = 1; i <= 4096; i++) state.Sprites.Add(Sprite(i));
            byte[] bytes = state.Encode();
            WorldSnapshot mirror = WorldSnapshot.Decode(bytes);
            Assert(mirror.Sprites.Count == 4096 && mirror.Sprites[4095].Id == 4096 && mirror.Health == 3 && mirror.Ammo == 100 && bytes.Length < LanTransport.MaxFrame, "4096 native sprites fit transport and preserve state");
            state.Sprites[0].Kind = 1; state.Sprites[0].Anchor = 4; state.Sprites[0].Layer = 16; state.Sprites[0].Width = 192; state.Sprites[0].HeightPixels = 16;
            SpriteState geometry = WorldSnapshot.Decode(state.Encode()).Sprites[0];
            Assert(geometry.Kind == 1 && geometry.Anchor == 4 && geometry.Layer == 16 && geometry.Width == 192 && geometry.HeightPixels == 16, "tiled sprite geometry and render layer round trip");
            state.Sprites[0].Layer = 32; Reject(delegate { WorldSnapshot.Decode(state.Encode()); }, "invalid Unity render layer rejected"); state.Sprites[0].Layer = 16;
            state.Sprites[1].Id = 1; Reject(delegate { WorldSnapshot.Decode(state.Encode()); }, "duplicate entity ID rejected");
            state.Sprites[1].Id = 2; state.Sprites[0].X = float.PositiveInfinity; Reject(delegate { WorldSnapshot.Decode(state.Encode()); }, "invalid entity position rejected");
            state.Sprites[0].X = 0; state.Sprites[0].SpriteId = -1; Reject(delegate { WorldSnapshot.Decode(state.Encode()); }, "invalid sprite index rejected");
            state.Sprites[0].SpriteId = 0; state.CameraSize = 0; Reject(delegate { WorldSnapshot.Decode(state.Encode()); }, "invalid camera scale rejected");
            state.CameraSize = 7; state.Sprites.Add(Sprite(4097)); Reject(delegate { state.Encode(); }, "snapshot entity budget enforced");
            Reject(delegate { WorldSnapshot.Decode(new byte[] { 5, 0 }); }, "truncated world snapshot rejected");
            byte[] extra = level.Encode(); Array.Resize(ref extra, extra.Length + 1); Reject(delegate { LevelCommand.Decode(extra); }, "trailing protocol data rejected");
            Assert(Wire.Newer(0, uint.MaxValue) && !Wire.Newer(uint.MaxValue, 0) && !Wire.Newer(4, 4), "sequence wraparound and stale update ordering");
            Console.WriteLine("DUNGEON_PROTOCOL_TESTS_PASS: " + passed + " checks"); return 0;
        } catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }
}
