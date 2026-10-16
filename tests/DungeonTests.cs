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
    // Construct an untrusted wire frame directly so decode checks cannot be satisfied
    // merely because the production encoder already rejected malformed HUD values.
    private static byte[] RawHud(int currency, int keys, int blanks, string gun, string active, float cooldown)
    {
        return Wire.Write(DungeonMessage.Snapshot, delegate(BinaryWriter w) {
            w.Write(1); w.Write((uint)2); w.Write((double)0); w.Write((uint)7);
            w.Write(0f); w.Write(0f); w.Write(0f); w.Write(7.5f);
            w.Write(0f); w.Write(0f); w.Write(3f); w.Write(1f); w.Write(100); w.Write(false);
            w.Write(currency); w.Write(keys); w.Write(blanks); w.Write(gun); w.Write(active); w.Write(cooldown);
            for (int i = 0; i < 7; i++) w.Write(false);
            w.Write(3f); w.Write(-1); w.Write(-1); w.Write(0); w.Write(0); w.Write(false);
            w.Write(0);
        });
    }
    public static int Main()
    {
        try {
            RemoteInput input = new RemoteInput { Epoch = 1, Sequence = 7, MoveX = 0.5f, MoveY = -1, AimX = 1, Buttons = (1UL << 8) | (1UL << 9) | (1UL << 11) };
            RemoteInput decoded = RemoteInput.Decode(input.Encode());
            Assert(decoded.Sequence == 7 && decoded.MoveX == 0.5f && decoded.MoveY == -1 && decoded.AimX == 1 && decoded.Buttons == input.Buttons, "remote native-controller payload round trip");
            input.ViewAspect = 3;
            Assert(RemoteInput.Decode(input.Encode()).ViewAspect == 3, "ultrawide guest viewport survives input round trip independently of host viewport");
            foreach (float invalid in new float[] { 0, -1, 0.24f, 8.01f, float.NaN, float.PositiveInfinity }) {
                input.ViewAspect = invalid; Reject(delegate { RemoteInput.Decode(input.Encode()); }, "invalid guest viewport rejected: " + invalid);
            }
            input.ViewAspect = RemoteInput.MinViewAspect;
            Assert(RemoteInput.Decode(input.Encode()).ViewAspect == RemoteInput.MinViewAspect, "minimum supported viewport aspect allowed");
            input.ViewAspect = RemoteInput.MaxViewAspect;
            Assert(RemoteInput.Decode(input.Encode()).ViewAspect == RemoteInput.MaxViewAspect, "maximum supported viewport aspect allowed");
            input.ViewAspect = RemoteInput.DefaultViewAspect;
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
            foreach (string scene in new string[] { "tt5", "tt_castle", "tt_bullethell", "ss_resourcefulrat", "fs_robot", "fs_soldier" }) {
                level.Scene = scene;
                Assert(LevelCommand.ValidScene(scene) && LevelCommand.Decode(level.Encode()).Scene == scene, "native main / secret / past scene allowed: " + scene);
            }
            foreach (string scene in new string[] { "", "MainMenu", "ss_../outside", "fs_.._outside", "tt\\castle", "tt/castle", "tt:castle", "ss_foo bar", "fs_robot\n", "ss_foo%2fbar", "FS_robot", "tt_замок", "tt_" + new string('a', 126) }) {
                level.Scene = scene;
                Reject(delegate { LevelCommand.Decode(level.Encode()); }, "unsafe or unsupported scene rejected: " + scene.Replace("\n", "\\n"));
            }
            level.Scene = "tt_../../outside"; Reject(delegate { LevelCommand.Decode(level.Encode()); }, "unsafe scene name rejected");
            level.Scene = "tt_castle"; level.LayoutHash = "short"; Reject(delegate { LevelCommand.Decode(level.Encode()); }, "incomplete layout fingerprint rejected");
            CollectionInfo info = new CollectionInfo { Epoch = 1, Id = 4, Guid = "native-sprite-guid", Name = "Projectiles", Asset = "collection" };
            CollectionInfo c = CollectionInfo.Decode(info.Encode()); Assert(c.Id == 4 && c.Guid == info.Guid && c.Name == "Projectiles", "local sprite resource descriptor round trip");
            Assert(c.EnemyGuid == "", "non-enemy collections have no prefab dependency");
            Reject(delegate { CollectionInfo.Decode(Wire.Write(DungeonMessage.Collection, delegate(BinaryWriter w) { w.Write(1); w.Write(1); w.Write("guid"); w.Write("name"); w.Write("asset"); })); }, "prior collection payload without enemy-dependency field rejected");
            info.EnemyGuid = "42be66373a3d4d89b91a35c9ff8adfec";
            Assert(CollectionInfo.Decode(info.Encode()).EnemyGuid == info.EnemyGuid, "embedded enemy collection carries exact native prefab GUID independently of its name");
            foreach (string invalid in new string[] { "Blobulin", "../enemies/blobulin", new string('a', 31), new string('a', 33), new string('A', 32), new string('z', 32) }) {
                info.EnemyGuid = invalid;
                Reject(delegate { CollectionInfo.Decode(info.Encode()); }, "invalid enemy dependency rejected: " + invalid);
            }
            info.EnemyGuid = "";
            info.Id = 5000; Reject(delegate { CollectionInfo.Decode(info.Encode()); }, "collection ID limit enforced");
            WorldSnapshot state = Snapshot();
            state.InputSequence = 765; state.Currency = 123; state.Keys = 4; state.Blanks = 2; state.GunName = "Морской пистолет"; state.ActiveItemName = "Supply Drop";
            state.ActiveItemCooldown = 0.625f; state.ActiveItemActive = true; state.IsGhost = true; state.IsDead = true; state.GameOver = true;
            state.InfiniteAmmo = true; state.InfiniteKeys = true;
            WorldSnapshot hud = WorldSnapshot.Decode(state.Encode());
            Assert(hud.InputSequence == 765, "host acknowledgement of guest input survives state frame");
            Assert(hud.Currency == 123 && hud.Keys == 4 && hud.Blanks == 2 && hud.GunName == state.GunName && hud.ActiveItemName == "Supply Drop" && hud.ActiveItemCooldown == 0.625f && hud.ActiveItemActive && !hud.ActiveItemReady && hud.IsGhost && hud.IsDead && hud.GameOver && hud.InfiniteAmmo && hud.InfiniteKeys, "guest counters / Unicode item names / cooldown / death and result states round trip");
            state.ActiveItemReady = true; state.ActiveItemActive = false; state.IsGhost = state.IsDead = state.GameOver = false; state.ActiveItemCooldown = 0;
            hud = WorldSnapshot.Decode(state.Encode());
            Assert(hud.ActiveItemReady && !hud.ActiveItemActive && !hud.IsGhost && !hud.IsDead && !hud.GameOver && hud.ActiveItemCooldown == 0, "native ready / revived guest state survives wire transition");
            hud = WorldSnapshot.Decode(RawHud(WorldSnapshot.MaxHudCounter, WorldSnapshot.MaxHudCounter, WorldSnapshot.MaxHudCounter, new string('g', WorldSnapshot.MaxHudNameLength), new string('a', WorldSnapshot.MaxHudNameLength), 1));
            Assert(hud.Currency == WorldSnapshot.MaxHudCounter && hud.Keys == WorldSnapshot.MaxHudCounter && hud.Blanks == WorldSnapshot.MaxHudCounter && hud.GunName.Length == WorldSnapshot.MaxHudNameLength && hud.ActiveItemCooldown == 1, "HUD counter / name / cooldown upper bounds accepted");
            Assert(WorldSnapshot.Decode(RawHud(0, 0, 0, "", "", 0)).ActiveItemName == "", "empty equipment and zero counters accepted");
            Reject(delegate { WorldSnapshot.Decode(RawHud(-1, 0, 0, "", "", 0)); }, "negative cash rejected from untrusted wire");
            Reject(delegate { WorldSnapshot.Decode(RawHud(WorldSnapshot.MaxHudCounter + 1, 0, 0, "", "", 0)); }, "excess cash rejected from untrusted wire");
            Reject(delegate { WorldSnapshot.Decode(RawHud(0, -1, 0, "", "", 0)); }, "negative keys rejected from untrusted wire");
            Reject(delegate { WorldSnapshot.Decode(RawHud(0, int.MaxValue, 0, "", "", 0)); }, "excess keys rejected from untrusted wire");
            Reject(delegate { WorldSnapshot.Decode(RawHud(0, 0, -1, "", "", 0)); }, "negative blanks rejected from untrusted wire");
            Reject(delegate { WorldSnapshot.Decode(RawHud(0, 0, int.MaxValue, "", "", 0)); }, "excess blanks rejected from untrusted wire");
            foreach (float cooldown in new float[] { -0.01f, 1.01f, float.NaN, float.PositiveInfinity }) {
                float invalid = cooldown;
                Reject(delegate { WorldSnapshot.Decode(RawHud(0, 0, 0, "", "", invalid)); }, "invalid active cooldown rejected from untrusted wire: " + invalid);
            }
            Reject(delegate { WorldSnapshot.Decode(RawHud(0, 0, 0, new string('g', WorldSnapshot.MaxHudNameLength + 1), "", 0)); }, "oversized gun name rejected from untrusted wire");
            Reject(delegate { WorldSnapshot.Decode(RawHud(0, 0, 0, "", new string('a', WorldSnapshot.MaxHudNameLength + 1), 0)); }, "oversized active item name rejected from untrusted wire");
            Reject(delegate { WorldSnapshot.Decode(RawHud(0, 0, 0, "gun\nforged line", "", 0)); }, "multiline gun name rejected from untrusted wire");
            Reject(delegate { WorldSnapshot.Decode(RawHud(0, 0, 0, "", "item\0hidden", 0)); }, "control character in active item rejected from untrusted wire");
            Assert(WorldSnapshot.CleanHudName("gun\nname\t") == "gun name " && WorldSnapshot.CleanHudName(new string('a', 97)).Length == 96 && WorldSnapshot.CleanHudName(new string('a', 95) + "\ud83d\ude00").Length == 95, "native display names sanitize controls and truncate without splitting surrogate pair");
            state.GunName = null; Reject(delegate { state.Encode(); }, "null HUD name rejected before send"); state.GunName = "Gun";
            state.Ammo = WorldSnapshot.MaxHudCounter + 1; Reject(delegate { state.Encode(); }, "excess ammo rejected before send"); state.Ammo = 100;
            for (int i = 1; i <= 4096; i++) state.Sprites.Add(Sprite(i));
            byte[] bytes = state.Encode();
            WorldSnapshot mirror = WorldSnapshot.Decode(bytes);
            Assert(mirror.Sprites.Count == 4096 && mirror.Sprites[4095].Id == 4096 && mirror.Health == 3 && mirror.Ammo == 100 && bytes.Length < LanTransport.MaxFrame, "4096 native sprites fit transport and preserve state");
            state.Sprites[0].Kind = 1; state.Sprites[0].Anchor = 4; state.Sprites[0].Layer = 16; state.Sprites[0].Width = 192; state.Sprites[0].HeightPixels = 16;
            SpriteState geometry = WorldSnapshot.Decode(state.Encode()).Sprites[0];
            Assert(geometry.Kind == 1 && geometry.Anchor == 4 && geometry.Layer == 16 && geometry.Width == 192 && geometry.HeightPixels == 16, "tiled sprite geometry and render layer round trip");
            state.Sprites[0].Flags = 15;
            Assert(WorldSnapshot.Decode(state.Encode()).Sprites[0].Flags == 15, "flip / perpendicular geometry / tilt flags round trip");
            state.Sprites[0].Flags = 16; Reject(delegate { WorldSnapshot.Decode(state.Encode()); }, "unknown sprite geometry flags rejected"); state.Sprites[0].Flags = 0;
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
