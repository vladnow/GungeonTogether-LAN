using System;
using System.IO;
using GungeonTogether.Lan;
static class EffectsTests
{
    static int passed;
    static void Check(bool good, string name) { if (!good) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    static EffectsState State()
    {
        EffectsState s = new EffectsState { Epoch = 3, Sequence = uint.MaxValue, Damage = 7, Vignette = .25f, HostX = 12, HostY = 18, Speaker = 1 };
        s.Kinds.Add("Poison|native_texture"); s.Cells.Add(new SurfaceCell { X = 100, Y = 120, Flags = 3, Life = 4.5f, FireTime = .7f });
        s.Boxes.Add(new DialogueBox { Id = -10, Style = "InfoBox", Text = "Native dialogue", X = 30, Y = 50 });
        s.Responses.Add("Continue"); return s;
    }
    static void Reject(EffectsState s, string name) { try { EffectsState.Decode(s.Encode()); } catch (IOException) { Check(true, name); return; } throw new Exception(name); }
    static void Main()
    {
        EffectsState s = EffectsState.Decode(State().Encode());
        Check(s.Epoch == 3 && s.Sequence == uint.MaxValue && s.Damage == 7 && s.HostX == 12 && s.HostY == 18, "effect epoch, damage and native lighting positions survive wire");
        Check(s.Cells[0].Flags == 3 && s.Cells[0].Life == 4.5f && s.Cells[0].FireTime == .7f && s.Kinds[0] == "Poison|native_texture", "surface identity, burning and poison render data survive wire");
        Check(s.Boxes[0].Text == "Native dialogue" && s.Responses[0] == "Continue" && s.Speaker == 1, "textbox and conversation selection survive wire");
        s = State(); s.Cells.Clear(); s.Boxes.Clear(); s.Responses.Clear(); s.Kinds.Clear();
        Check(EffectsState.Decode(s.Encode()).Cells.Count == 0, "empty authoritative state can remove stale presentation");
        s = State(); s.Cells.Add(s.Cells[0]); Reject(s, "duplicate coordinates rejected");
        s = State(); s.Cells[0].Kind = 1; Reject(s, "unresolved surface kind rejected");
        s = State(); s.Cells[0].Flags = 8; Reject(s, "unknown hazard flags rejected");
        s = State(); s.Cells[0].Life = float.NaN; Reject(s, "non-finite goop lifetime rejected");
        s = State(); s.Cells[0].FireTime = float.PositiveInfinity; Reject(s, "non-finite burn time rejected");
        s = State(); s.Cells[0].X = -1; Reject(s, "off-map negative goop coordinate rejected");
        s = State(); s.Vignette = float.NaN; Reject(s, "non-finite post effect rejected");
        s = State(); s.HostX = float.PositiveInfinity; Reject(s, "non-finite lighting anchor rejected");
        s = State(); s.Epoch = 0; Reject(s, "invalid floor epoch rejected");
        s = State(); s.Boxes[0].Style = "../../arbitrary_asset"; Reject(s, "dialogue resource path injection rejected by style allowlist");
        s = State(); s.Boxes.Add(s.Boxes[0]); Reject(s, "duplicate dialogue identity rejected");
        s = State(); s.Boxes[0].Text = new string('x', 8193); Reject(s, "oversized dialogue text rejected");
        s = State(); s.Selected = 1; Reject(s, "response index outside choices rejected");
        s = State(); s.Speaker = 2; Reject(s, "invalid native portrait player rejected");
        s = State(); for (int i = 1; i <= EffectsState.MaxCells; i++) s.Cells.Add(new SurfaceCell { X = i, Y = 1 }); Reject(s, "surface packet cannot exceed bounded payload");
        s = State(); for (int i = 1; i <= EffectsState.MaxKinds; i++) s.Kinds.Add("asset" + i); Reject(s, "surface asset list bounded");
        s = State(); byte[] bytes = s.Encode(); Array.Resize(ref bytes, bytes.Length + 1);
        try { EffectsState.Decode(bytes); throw new Exception("trailing bytes accepted"); } catch (IOException) { Check(true, "trailing effect bytes rejected"); }
        WorldSnapshot frame = new WorldSnapshot { Epoch = 1, CameraSize = 7 }; frame.Sprites.Add(new SpriteState { Id = 1, CollectionId = 1, SpriteId = 1, Color = 0xffffffff, OverrideColor = 0x7fffffff });
        Check(WorldSnapshot.Decode(frame.Encode()).Sprites[0].OverrideColor == 0x7fffffff, "native damage/status material tint survives sprite frame");
        Check(CharacterCatalog.Identity(0) == "CoopCultist" && CharacterCatalog.Identity(1) == "Pilot" && CharacterCatalog.Identity(4) == "Guide", "native hurt audio switches use character identity instead of unrelated enum ordinal");

        s = State(); s.Labels.Add(new WorldLabel { Id = 5, Text = "Gun: 35", X = 12, Y = 34 }); s.Bosses.Add(new BossBarState { Slot = 1, Health = .625f, Name = "Boss" }); s.MapRooms.Add(new MapRoomState { X = 10, Y = 20, Visibility = 2 }); s.Doors.Add(new DoorState { X = 100, Y = 200, Open = true });
        EffectsState ui = EffectsState.Decode(s.Encode());
        Check(ui.Labels[0].Text == "Gun: 35" && ui.Labels[0].Y == 34, "shop label text and world anchor survive wire");
        Check(ui.Bosses[0].Name == "Boss" && ui.Bosses[0].Health == .625f && ui.Bosses[0].Slot == 1, "native boss health and widget identity survive wire");
        Check(ui.MapRooms[0].X == 10 && ui.MapRooms[0].Visibility == 2 && ui.Doors[0].Open, "authoritative exploration and open door state survive wire");
        s = State(); s.Labels.Add(new WorldLabel { X = float.NaN }); Reject(s, "nonfinite price label rejected");
        s = State(); s.Labels.Add(new WorldLabel { Id = 1 }); s.Labels.Add(new WorldLabel { Id = 1 }); Reject(s, "duplicate price label rejected");
        s = State(); s.Bosses.Add(new BossBarState { Health = 1.1f }); Reject(s, "invalid boss health fraction rejected");
        s = State(); s.Bosses.Add(new BossBarState { Slot = 3 }); Reject(s, "invalid native boss widget rejected");
        s = State(); s.MapRooms.Add(new MapRoomState { X = int.MinValue }); Reject(s, "map coordinate overflow rejected");
        s = State(); s.Doors.Add(new DoorState { X = 1 }); s.Doors.Add(new DoorState { X = 1 }); Reject(s, "duplicate door position rejected");
        s = State(); s.Cells[0].Life = EffectsState.NativeClock(float.MaxValue, -1); Check(EffectsState.Decode(s.Encode()).Cells[0].Life == 1000000, "permanent native surface lifetime fits bounded network value");
        Check(EffectsState.NativeClock(float.PositiveInfinity, -1) == -1 && EffectsState.NativeClock(float.NaN, 0) == 0 && EffectsState.NativeClock(-4, 0) == 0, "native goop sentinels normalize before transmission");
        CollectionInfo descriptor = new CollectionInfo { Epoch = 1, Id = 1, Shader = "Brave/Enemy Projectile Emissive", Keywords = "EMISSIVE_ON", Texture = "native-atlas" }; descriptor = CollectionInfo.Decode(descriptor.Encode());
        Check(descriptor.Shader == "Brave/Enemy Projectile Emissive" && descriptor.Texture == "native-atlas" && descriptor.Keywords == "EMISSIVE_ON", "native shader, texture and keyword variant survive wire");
        frame.Sprites[0].Kind = 1; frame.Sprites[0].Width = -20;
        Check(WorldSnapshot.Decode(frame.Encode()).Sprites[0].Width == -20, "finite signed native sprite dimensions do not disconnect a session");
        frame.Sprites[0].ScaleX = float.NaN;
        try { WorldSnapshot.Decode(frame.Encode()); throw new Exception("invalid sprite accepted"); } catch (IOException ex) { Check(ex.Message.Contains("scale X") && ex.Message.Contains("collection 1"), "invalid transform identifies exact sprite field and collection"); }

        descriptor.Parameters.Add(new MaterialParameter { Name = "_EmissivePower", X = 45 }); descriptor.Parameters.Add(new MaterialParameter { Name = "_MainTex_ST", Vector = true, X = 1, Y = 1, Z = .25f });
        descriptor = CollectionInfo.Decode(descriptor.Encode()); Check(descriptor.Parameters[0].X == 45 && descriptor.Parameters[1].Z == .25f, "procedural bullet emission and texture UV configuration survive wire");
        descriptor.Parameters[0].Name = "arbitrary_shader_property";
        try { CollectionInfo.Decode(descriptor.Encode()); throw new Exception("unknown shader property accepted"); } catch (IOException) { Check(true, "network material property allowlist enforced"); }
        Console.WriteLine("EFFECTS_TESTS_PASS: " + passed);
    }
}
