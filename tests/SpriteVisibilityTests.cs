using System;
using System.IO;
using GungeonTogether.Lan;

internal static class SpriteVisibilityTests
{
    private sealed class FakeRenderer { internal bool Alive = true, Enabled; internal int Writes; }
    private static int passed;
    private static void Assert(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
    private static void Reject(Action action, string name) { bool rejected = false; try { action(); } catch (IOException) { rejected = true; } Assert(rejected, name); }
    public static int Main()
    {
        try {
            var visibility = new VisibilityOverrides<FakeRenderer>(delegate(FakeRenderer r) { return r.Alive; }, delegate(FakeRenderer r) { return r.Enabled; }, delegate(FakeRenderer r, bool enabled) { r.Enabled = enabled; r.Writes++; });
            FakeRenderer body = new FakeRenderer { Enabled = true }, gun = new FakeRenderer { Enabled = false }, proxy = new FakeRenderer { Enabled = true };
            visibility.Track(body); visibility.Track(gun);
            Assert(!body.Enabled && !gun.Enabled && proxy.Enabled && visibility.Count == 2, "only tracked native renderers hidden; independent network proxy stays visible");
            for (int frame = 0; frame < 100; frame++) {
                body.Enabled = true; visibility.Track(body); // A native animation reenables the source after discovery.
                gun.Enabled = true; visibility.Enforce(); // A later render boundary reenables a previously hidden gun.
                if (body.Enabled || gun.Enabled || !proxy.Enabled) throw new Exception("native duplicate leaked into render frame");
            }
            Assert(visibility.Count == 2, "native reenable for 100 frames does not duplicate saved state entries");
            int writes = body.Writes + gun.Writes;
            visibility.Enforce(); visibility.Enforce();
            Assert(body.Writes + gun.Writes == writes, "unchanged hidden renderers require no repeated setters");
            visibility.Restore();
            Assert(body.Enabled && !gun.Enabled && proxy.Enabled && visibility.Count == 0, "dispose restores each original enabled state instead of enabling all sources");
            visibility.Track(body); body.Alive = false; writes = body.Writes;
            visibility.Enforce(); visibility.Restore();
            Assert(body.Writes == writes && visibility.Count == 0, "destroyed native renderer is not accessed during enforce or restoration");
            body.Alive = true; visibility.Track(body); visibility.Forget(body);
            Assert(visibility.Count == 0, "native lifecycle removal releases saved renderer reference");
            Assert(SpriteRenderPolicy.ScaleAxis(1, -1) == 1 && SpriteRenderPolicy.ScaleAxis(2, -3) == 6, "sprite flip is excluded from transform scale so client applies it only once");
            Assert(SpriteRenderPolicy.ScaleAxis(-2, -3) == -6 && SpriteRenderPolicy.ScaleAxis(0, -3) == 0, "parent mirror and zero scale remain represented independently of sprite flip");
            string signature = SpriteCollectionDescriptor.Fingerprint(new string[] { "gun_idle_001", "gun_fire_001" });
            Assert(signature.Length == 64 && signature == SpriteCollectionDescriptor.Fingerprint(new string[] { "gun_idle_001", "gun_fire_001" }), "sprite table signature is stable for identical ordered indices");
            Assert(signature != SpriteCollectionDescriptor.Fingerprint(new string[] { "gun_fire_001", "gun_idle_001" }), "same named collection with reordered sprite indices is distinguishable");
            Assert(signature != SpriteCollectionDescriptor.Fingerprint(new string[] { "gun_idle_001", "gun_fire_001", "gun_reload_001" }), "different sprite table length cannot reuse the same cached collection");
            for (int mode = 0; mode <= 2; mode++) {
                SpriteCollectionDescriptor descriptor = SpriteCollectionDescriptor.Parse(SpriteCollectionDescriptor.Encode("Weapons|Collection", signature, mode));
                Assert(descriptor.Name == "Weapons|Collection" && descriptor.Signature == signature && descriptor.MaterialMode == mode, "native material variant survives descriptor round trip: " + mode);
            }
            SpriteCollectionDescriptor legacy = SpriteCollectionDescriptor.Parse("Weapons");
            Assert(legacy.Name == "Weapons" && legacy.Signature == "" && legacy.MaterialMode == 0, "legacy plain collection name retains default material semantics");
            Assert(SpriteCollectionDescriptor.Encode(new string('a', 1000), signature, 2).Length <= 256, "extended collection identity fits existing protocol descriptor budget");
            Reject(delegate { SpriteCollectionDescriptor.Parse("@GT1|3|" + signature + "|Weapons"); }, "unknown material mode rejected");
            Reject(delegate { SpriteCollectionDescriptor.Parse("@GT1|1|short|Weapons"); }, "short collection signature rejected");
            Reject(delegate { SpriteCollectionDescriptor.Parse("@GT1|0|" + new string('x', 64) + "|Weapons"); }, "nonhex collection signature rejected");
            Reject(delegate { SpriteCollectionDescriptor.Parse("@GT1|broken"); }, "truncated material descriptor rejected");
            Console.WriteLine("SPRITE_VISIBILITY_TESTS_PASS: " + passed + " checks"); return 0;
        } catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }
}
