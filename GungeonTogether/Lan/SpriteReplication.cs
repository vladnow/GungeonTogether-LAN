using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GungeonTogether.Lan
{
    // Native assets render locally. These are transforms/sprite states, never video frames.
    public sealed class SpriteReplication : IDisposable
    {
        private readonly Dictionary<long, int> hostCollections = new Dictionary<long, int>();
        private readonly Dictionary<int, CollectionInfo> definitions = new Dictionary<int, CollectionInfo>();
        private readonly Dictionary<int, SpriteCollectionDescriptor> descriptors = new Dictionary<int, SpriteCollectionDescriptor>();
        private readonly Dictionary<int, string> collectionSignatures = new Dictionary<int, string>();
        private readonly Dictionary<int, tk2dSpriteCollectionData> collections = new Dictionary<int, tk2dSpriteCollectionData>();
        private readonly EnemySpriteAssets enemyAssets = new EnemySpriteAssets();
        private readonly Dictionary<int, tk2dBaseSprite> proxies = new Dictionary<int, tk2dBaseSprite>();
        private readonly Dictionary<int, Material> proxyMaterials = new Dictionary<int, Material>();
        private readonly List<tk2dBaseSprite> nativeSprites = new List<tk2dBaseSprite>();
        private readonly HashSet<tk2dBaseSprite> knownSprites = new HashSet<tk2dBaseSprite>();
        private readonly VisibilityOverrides<Renderer> hidden = new VisibilityOverrides<Renderer>(delegate(Renderer r) { return r != null; }, delegate(Renderer r) { return r.enabled; }, delegate(Renderer r, bool value) { r.enabled = value; });
        private readonly VisibilityOverrides<Behaviour> disabled = new VisibilityOverrides<Behaviour>(delegate(Behaviour b) { return b != null; }, delegate(Behaviour b) { return b.enabled; }, delegate(Behaviour b, bool value) { b.enabled = value; });
        private readonly Dictionary<HealthHaver, bool> protectedHealth = new Dictionary<HealthHaver, bool>();
        private PlayerController[] localPlayers = new PlayerController[0];
        private AIActor[] localEnemies = new AIActor[0];
        private Projectile[] localProjectiles = new Projectile[0];
        private float nextActorRefresh;
        private bool seeded, suppressing, disposed;
        private GameUIRoot hud;
        private readonly SnapshotBuffer<WorldSnapshot> frames = new SnapshotBuffer<WorldSnapshot>(.04, true);
        public readonly MovementPrediction Prediction = new MovementPrediction();
        public bool PredictionEnabled = true;
        public int GuardClips { get; private set; }
        public int PredictedVisibleFrames { get; private set; }
        public Vector2 PresentedLead { get; private set; }
        public double SmoothingMilliseconds { get { return frames.DelaySeconds * 1000; } }
        private bool guardUnavailable;
        private readonly List<SpriteState> renderStates = new List<SpriteState>();
        private readonly Dictionary<int, SpriteState> nextSprites = new Dictionary<int, SpriteState>();
        private WorldSnapshot indexedFrame;
        private readonly HashSet<int> present = new HashSet<int>();
        private readonly List<int> removed = new List<int>();
        public int RenderedFrames { get; private set; }
        public int InterpolatedFrames { get; private set; }
        public int SpriteDiscoveryPasses { get; private set; }
        public int ActorDiscoveryPasses { get; private set; }
        public int CaptureCandidates { get; private set; }
        public int CapturedSprites { get; private set; }
        public int CulledSprites { get; private set; }
        public int HiddenNativeRenderers { get { return hidden.Count; } }
        public double LastCaptureMilliseconds { get; private set; }
        public double LastRenderMilliseconds { get; private set; }
        public Vector2 RemotePlayerPosition;
        public WorldSnapshot LastSnapshot;

        public SpriteReplication() { NativeSpriteLifecycle.Register(this); }

        public void Define(CollectionInfo info) { definitions[info.Id] = info; descriptors[info.Id] = SpriteCollectionDescriptor.Parse(info.Name); }

        internal void Observe(tk2dBaseSprite sprite)
        {
            if (disposed || sprite == null || sprite.GetComponent<NetworkSpriteProxy>() != null || !knownSprites.Add(sprite)) return;
            nativeSprites.Add(sprite);
            if (suppressing) hidden.Track(sprite.GetComponent<Renderer>());
        }

        internal void Forget(tk2dBaseSprite sprite)
        {
            if (object.ReferenceEquals(sprite, null)) return;
            knownSprites.Remove(sprite); nativeSprites.Remove(sprite);
            hidden.Forget(sprite.GetComponent<Renderer>());
        }

        private void SeedSprites()
        {
            if (seeded) return;
            seeded = true; SpriteDiscoveryPasses++;
            foreach (tk2dBaseSprite sprite in UnityEngine.Object.FindObjectsOfType<tk2dBaseSprite>()) Observe(sprite);
        }

        private string CollectionSignature(tk2dSpriteCollectionData collection)
        {
            int key = collection.GetInstanceID(); string signature;
            if (collectionSignatures.TryGetValue(key, out signature)) return signature;
            List<string> names = new List<string>(collection.spriteDefinitions.Length);
            foreach (tk2dSpriteDefinition definition in collection.spriteDefinitions) names.Add(definition == null ? "" : definition.name);
            signature = SpriteCollectionDescriptor.Fingerprint(names); collectionSignatures[key] = signature; return signature;
        }

        public WorldSnapshot Capture(int epoch, uint sequence, Action<byte[]> send, float guestAspect = RemoteInput.DefaultViewAspect)
        {
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            WorldSnapshot snapshot = new WorldSnapshot { Epoch = epoch, Sequence = sequence, SentAt = Time.realtimeSinceStartup };
            GameManager gm = GameManager.Instance;
            Camera camera = gm.MainCameraController.Camera;
            PlayerController player = gm.SecondaryPlayer;
            Vector3 cam = PlayerCamera.GuestPosition(gm.MainCameraController, player);
            snapshot.CameraX = cam.x; snapshot.CameraY = cam.y; snapshot.CameraZ = cam.z; snapshot.CameraSize = camera.orthographicSize;
            if (player != null) {
                snapshot.PlayerX = player.transform.position.x; snapshot.PlayerY = player.transform.position.y;
                snapshot.Health = Mathf.Max(0, player.healthHaver.GetCurrentHealth()); snapshot.Armor = Mathf.Max(0, player.healthHaver.Armor);
                snapshot.MaxHealth = Mathf.Clamp(player.healthHaver.GetMaxHealth(), 0, 10000);
                Gun gun = player.CurrentGun;
                if (gun != null) {
                    snapshot.GunId = gun.PickupObjectId;
                    snapshot.ClipAmmo = Mathf.Clamp(gun.ClipShotsRemaining, 0, WorldSnapshot.MaxHudCounter);
                    snapshot.ClipCapacity = Mathf.Clamp(gun.ClipCapacity, 0, WorldSnapshot.MaxHudCounter);
                    snapshot.Reloading = gun.IsReloading;
                }
                snapshot.Ammo = gun != null ? Mathf.Clamp(gun.CurrentAmmo, 0, WorldSnapshot.MaxHudCounter) : 0;
                snapshot.GunName = gun != null ? WorldSnapshot.CleanHudName(gun.EncounterNameOrDisplayName) : "";
                snapshot.InfiniteAmmo = gun != null && gun.InfiniteAmmo;
                snapshot.Blanks = Mathf.Clamp(player.Blanks, 0, WorldSnapshot.MaxHudCounter);
                if (player.carriedConsumables != null) {
                    snapshot.Currency = Mathf.Clamp(player.carriedConsumables.Currency, 0, WorldSnapshot.MaxHudCounter);
                    snapshot.Keys = Mathf.Clamp(player.carriedConsumables.KeyBullets, 0, WorldSnapshot.MaxHudCounter);
                    snapshot.InfiniteKeys = player.carriedConsumables.InfiniteKeys;
                }
                snapshot.IsGhost = player.IsGhost; snapshot.IsDead = player.healthHaver.IsDead;
                PlayerItem item = player.CurrentItem;
                if (item != null) {
                    snapshot.ItemId = item.PickupObjectId;
                    snapshot.ActiveItemName = WorldSnapshot.CleanHudName(item.EncounterNameOrDisplayName);
                    float cooldown = item.CooldownPercentage;
                    snapshot.ActiveItemCooldown = float.IsNaN(cooldown) || float.IsInfinity(cooldown) ? 0 : Mathf.Clamp01(cooldown);
                    snapshot.ActiveItemActive = item.IsCurrentlyActive;
                    snapshot.ActiveItemReady = !snapshot.IsDead && !snapshot.IsGhost && !item.IsOnCooldown && !item.IsCurrentlyActive && item.CanBeUsed(player);
                }
            }
            snapshot.Paused = gm.IsPaused;
            if (AmmonomiconController.HasInstance) {
                AmmonomiconController book = AmmonomiconController.Instance;
                snapshot.GameOver = book != null && (book.IsOpen || book.IsOpening) && book.CurrentRightPageRenderer != null &&
                    book.CurrentRightPageRenderer.pageType == AmmonomiconPageRenderer.PageType.DEATH_RIGHT;
            }
            SeedSprites();
            CaptureCandidates = nativeSprites.Count; CulledSprites = 0;
            // Only the guest needs network sprites; the host renders its native
            // world. Cull against the guest's view even when it is off-screen here.
            Plane[] view = PlayerCamera.GuestFrustum(camera, cam, guestAspect);
            for (int spriteIndex = 0; spriteIndex < nativeSprites.Count; spriteIndex++) {
                tk2dBaseSprite sprite = nativeSprites[spriteIndex];
                if (sprite == null || !sprite.gameObject.activeInHierarchy) continue;
                Renderer renderer = sprite.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled) continue;
                // Native getters flush the animator's deferred invisible frame.
                // Read them before bounds so the guest gets current geometry
                // even when the sprite never enters the host's own screen.
                tk2dSpriteCollectionData sourceCollection = sprite.Collection;
                int spriteId = sprite.spriteId;
                if (sourceCollection == null || spriteId < 0) continue;
                if (!GeometryUtility.TestPlanesAABB(view, renderer.bounds)) { CulledSprites++; continue; }
                tk2dSpriteCollectionData actualCollection = sourceCollection.inst;
                int materialMode = sprite.IsBraveOutlineSprite ? 1 : sprite.IsOutlineSprite ? 2 : 0;
                long key = ((long)(uint)actualCollection.GetInstanceID() << 2) | (uint)materialMode;
                int collectionId;
                if (!hostCollections.TryGetValue(key, out collectionId)) {
                    collectionId = hostCollections.Count + 1;
                    if (collectionId > 4096) throw new IOException("Too many sprite collections.");
                    hostCollections.Add(key, collectionId);
                    string name = !string.IsNullOrEmpty(sourceCollection.spriteCollectionName) ? sourceCollection.spriteCollectionName : !string.IsNullOrEmpty(actualCollection.spriteCollectionName) ? actualCollection.spriteCollectionName : sourceCollection.name;
                    string guid = !string.IsNullOrEmpty(sourceCollection.spriteCollectionGUID) ? sourceCollection.spriteCollectionGUID : actualCollection.spriteCollectionGUID ?? "";
                    string asset = !string.IsNullOrEmpty(sourceCollection.assetName) ? sourceCollection.assetName : actualCollection.assetName ?? "";
                    CollectionInfo info = new CollectionInfo { Epoch = epoch, Id = collectionId, Guid = guid, Name = SpriteCollectionDescriptor.Encode(name, CollectionSignature(actualCollection), materialMode), Asset = asset, EnemyGuid = enemyAssets.Owner(sprite, actualCollection) };
                    send(info.Encode());
                }
                Transform t = sprite.transform; Vector3 pos = t.position; Vector3 scale = t.lossyScale;
                bool guestPart = player != null && (t.IsChildOf(player.transform) || (player.CurrentGun != null && t.IsChildOf(player.CurrentGun.transform)));
                Color32 color = sprite.color;
                if (materialMode != 0 && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_OverrideColor")) {
                    Color outline = renderer.sharedMaterial.GetColor("_OverrideColor"); outline.a *= sprite.color.a; color = outline;
                }
                SpriteState state = new SpriteState {
                    Id = sprite.GetInstanceID(), CollectionId = collectionId, SpriteId = spriteId, SortingOrder = sprite.SortingOrder,
                    X = pos.x, Y = pos.y, Z = pos.z, Rotation = t.eulerAngles.z, ScaleX = SpriteRenderPolicy.ScaleAxis(scale.x, sprite.scale.x), ScaleY = SpriteRenderPolicy.ScaleAxis(scale.y, sprite.scale.y), ScaleZ = scale.z * sprite.scale.z,
                    Height = sprite.HeightOffGround, Color = (uint)(color.r | color.g << 8 | color.b << 16 | color.a << 24), Flags = (byte)((sprite.FlipX ? 1 : 0) | (sprite.FlipY ? 2 : 0) | (sprite.ShouldDoTilt && sprite.IsPerpendicular ? 4 : 0) | (sprite.ShouldDoTilt ? 8 : 0)), Layer = (byte)sprite.gameObject.layer
                };
                if (guestPart) state.Flags |= 16;
                tk2dTiledSprite tiled = sprite as tk2dTiledSprite;
                tk2dSlicedSprite sliced = sprite as tk2dSlicedSprite;
                tk2dClippedSprite clipped = sprite as tk2dClippedSprite;
                if (tiled != null) { state.Kind = 1; state.Width = tiled.dimensions.x; state.HeightPixels = tiled.dimensions.y; state.Anchor = (byte)tiled.anchor; }
                else if (sliced != null) { state.Kind = 2; state.Width = sliced.dimensions.x; state.HeightPixels = sliced.dimensions.y; state.Anchor = (byte)sliced.anchor; }
                else if (clipped != null) { state.Kind = 3; state.ClipX = clipped.clipBottomLeft.x; state.ClipY = clipped.clipBottomLeft.y; state.ClipRight = clipped.clipTopRight.x; state.ClipTop = clipped.clipTopRight.y; }
                snapshot.Sprites.Add(state);
            }
            CapturedSprites = snapshot.Sprites.Count;
            LastCaptureMilliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            return snapshot;
        }

        private bool MatchCollection(CollectionInfo info, SpriteCollectionDescriptor descriptor, tk2dSpriteCollectionData source, out tk2dSpriteCollectionData actual)
        {
            actual = null;
            if (source == null) return false;
            try {
                actual = source.inst;
                if (actual == null || actual.spriteDefinitions == null || actual.spriteDefinitions.Length == 0) return false;
                bool identity = !string.IsNullOrEmpty(info.Guid)
                    ? source.spriteCollectionGUID == info.Guid || actual.spriteCollectionGUID == info.Guid
                    : !string.IsNullOrEmpty(descriptor.Name) && (source.spriteCollectionName == descriptor.Name || source.name == descriptor.Name || actual.spriteCollectionName == descriptor.Name || actual.name == descriptor.Name);
                return identity && (descriptor.Signature.Length == 0 || CollectionSignature(actual) == descriptor.Signature);
            } catch { actual = null; return false; }
        }

        private tk2dSpriteCollectionData Resolve(int id)
        {
            tk2dSpriteCollectionData collection;
            if (collections.TryGetValue(id, out collection) && collection != null) return collection;
            CollectionInfo info;
            if (!definitions.TryGetValue(id, out info)) throw new IOException("Snapshot arrived before its sprite definition.");
            SpriteCollectionDescriptor descriptor = descriptors[id];
            if (!string.IsNullOrEmpty(info.Guid)) {
                try {
                    collection = tk2dSystem.LoadResourceByGUID<tk2dSpriteCollectionData>(info.Guid);
                    tk2dSpriteCollectionData actual;
                    if (MatchCollection(info, descriptor, collection, out actual)) { collections[id] = actual; return actual; }
                } catch { }
            }
            if (!string.IsNullOrEmpty(info.Asset) && info.Asset.IndexOf("..", StringComparison.Ordinal) < 0) {
                try {
                    GameObject asset = BraveResources.Load(info.Asset, ".prefab") as GameObject;
                    if (asset != null) {
                        collection = asset.GetComponent<tk2dSpriteCollectionData>();
                        tk2dSpriteCollectionData actual;
                        if (MatchCollection(info, descriptor, collection, out actual)) { collections[id] = actual; return actual; }
                    }
                } catch { }
            }
            foreach (tk2dSpriteCollectionData candidate in Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>()) {
                tk2dSpriteCollectionData actual;
                if (MatchCollection(info, descriptor, candidate, out actual)) { collections[id] = actual; return actual; }
            }
            if (!string.IsNullOrEmpty(info.EnemyGuid)) {
                // Load the local prefab's dependencies only. Its sprite table
                // still has to match the host exactly before it can be rendered.
                AIActor prefab = enemyAssets.Load(info.EnemyGuid);
                if (prefab != null) {
                    foreach (tk2dBaseSprite sprite in prefab.GetComponentsInChildren<tk2dBaseSprite>(true)) {
                        tk2dSpriteCollectionData actual;
                        if (MatchCollection(info, descriptor, sprite.Collection, out actual)) { collections[id] = actual; return actual; }
                    }
                    foreach (tk2dSpriteCollectionData candidate in Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>()) {
                        tk2dSpriteCollectionData actual;
                        if (MatchCollection(info, descriptor, candidate, out actual)) { collections[id] = actual; return actual; }
                    }
                }
            }
            throw new IOException("Missing or incompatible local sprite asset: " + descriptor.Name + ". Session stopped to avoid invisible hazards.");
        }

        public void Enqueue(WorldSnapshot snapshot)
        {
            if (frames.Push(snapshot, snapshot.Sequence, snapshot.SentAt, Time.realtimeSinceStartup)) LastSnapshot = snapshot;
        }
        public void RecordInput(RemoteInput input, double now) { Prediction.Record(input, now); }
        public bool EnqueueMotion(PlayerMotion motion) { return Prediction.Observe(motion, Time.realtimeSinceStartup); }

        public void Render(double now)
        {
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            try {
                WorldSnapshot snapshot, next; float blend;
                if (!frames.Sample(now, out snapshot, out next, out blend)) return;
                RenderedFrames++;
                if (snapshot != next && blend > 0 && blend < 1) InterpolatedFrames++;
                if (indexedFrame != next) {
                    indexedFrame = next; nextSprites.Clear();
                    foreach (SpriteState state in next.Sprites) nextSprites[state.Id] = state;
                }
                ApplyFrame(snapshot, next, blend);
            } finally {
                if (suppressing) hidden.Enforce();
                LastRenderMilliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            }
        }

        private void ApplyMaterial(SpriteState state, tk2dBaseSprite sprite, tk2dSpriteCollectionData collection)
        {
            int mode = descriptors[state.CollectionId].MaterialMode;
            Material material;
            if (mode == 0) {
                if (proxyMaterials.TryGetValue(state.Id, out material)) { UnityEngine.Object.Destroy(material); proxyMaterials.Remove(state.Id); }
                sprite.usesOverrideMaterial = false; sprite.IsOutlineSprite = false; sprite.IsBraveOutlineSprite = false;
                sprite.ForceUpdateMaterial(); return;
            }
            string shaderName = mode == 1 ? "Brave/Internal/SinglePassOutline" : "tk2d/SpriteOutlineCutout";
            if (!proxyMaterials.TryGetValue(state.Id, out material) || material == null || material.shader == null || material.shader.name != shaderName) {
                if (material != null) UnityEngine.Object.Destroy(material);
                material = new Material(ShaderCache.Acquire(shaderName));
                material.hideFlags = HideFlags.DontSave;
                proxyMaterials[state.Id] = material;
            }
            sprite.usesOverrideMaterial = true; sprite.IsOutlineSprite = true; sprite.IsBraveOutlineSprite = mode == 1;
            material.mainTexture = collection.spriteDefinitions[state.SpriteId].materialInst.mainTexture;
            material.SetColor("_OverrideColor", new Color32((byte)state.Color, (byte)(state.Color >> 8), (byte)(state.Color >> 16), (byte)(state.Color >> 24)));
            material.SetFloat("_LuminanceCutoff", 0);
            if (mode == 1) { material.DisableKeyword("OUTLINE_OFF"); material.EnableKeyword("OUTLINE_ON"); }
            sprite.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void ApplyFrame(WorldSnapshot snapshot, WorldSnapshot next, float blend)
        {
            Vector2 playerStart = new Vector2(snapshot.PlayerX, snapshot.PlayerY);
            Vector2 playerEnd = new Vector2(next.PlayerX, next.PlayerY);
            RemotePlayerPosition = (playerEnd - playerStart).sqrMagnitude < 64 ? Vector2.Lerp(playerStart, playerEnd, blend) : playerStart;
            PlayerMotion motion = Prediction.Fresh(Time.realtimeSinceStartup) ? Prediction.Motion : null;
            Vector2 lead = Vector2.zero, shift = Vector2.zero;
            if (motion != null) {
                float x, y; Prediction.Offset(Time.realtimeSinceStartup, PredictionEnabled && !guardUnavailable, out x, out y);
                Vector2 requested = new Vector2(x, y);
                try { lead = NativeMovement.ClipLead(motion, requested); if ((lead - requested).sqrMagnitude > .000001f) GuardClips++; }
                catch (Exception ex) { guardUnavailable = true; Debug.LogWarning("GT_PREDICTION_FALLBACK: " + ex.Message); }
                PresentedLead = lead;
                if (lead.sqrMagnitude > .000001f) PredictedVisibleFrames++;
                RemotePlayerPosition = new Vector2(motion.X, motion.Y) + lead;
                shift = RemotePlayerPosition - new Vector2(LastSnapshot.PlayerX, LastSnapshot.PlayerY);
            }
            renderStates.Clear();
            foreach (SpriteState state in snapshot.Sprites) if (motion == null || (state.Flags & 16) == 0) renderStates.Add(state);
            if (motion != null) foreach (SpriteState state in LastSnapshot.Sprites) if ((state.Flags & 16) != 0) renderStates.Add(state);
            present.Clear();
            foreach (SpriteState state in renderStates) {
                present.Add(state.Id);
                tk2dSpriteCollectionData collection = Resolve(state.CollectionId);
                if (state.SpriteId >= collection.spriteDefinitions.Length) throw new IOException("Sprite index differs between game builds.");
                tk2dBaseSprite sprite;
                if (proxies.TryGetValue(state.Id, out sprite) && sprite != null &&
                    ((state.Kind == 1) != (sprite is tk2dTiledSprite) || (state.Kind == 2) != (sprite is tk2dSlicedSprite) || (state.Kind == 3) != (sprite is tk2dClippedSprite))) {
                    UnityEngine.Object.Destroy(sprite.gameObject); proxies.Remove(state.Id); sprite = null;
                }
                if (sprite == null) {
                    GameObject go = new GameObject("GT_NetworkSprite_" + state.Id);
                    go.AddComponent<NetworkSpriteProxy>(); // Exclude before sprite Awake notifies lifecycle observers.
                    if (state.Kind == 1) sprite = go.AddComponent<tk2dTiledSprite>();
                    else if (state.Kind == 2) sprite = go.AddComponent<tk2dSlicedSprite>();
                    else if (state.Kind == 3) sprite = go.AddComponent<tk2dClippedSprite>();
                    else sprite = go.AddComponent<tk2dSprite>();
                    proxies[state.Id] = sprite;
                }
                sprite.automaticallyManagesDepth = false; sprite.IsZDepthDirty = false;
                if (sprite.Collection != collection || sprite.spriteId != state.SpriteId) sprite.SetSprite(collection, state.SpriteId);
                ApplyMaterial(state, sprite, collection);
                tk2dBaseSprite.PerpendicularState orientation = (state.Flags & 4) != 0 ? tk2dBaseSprite.PerpendicularState.PERPENDICULAR : tk2dBaseSprite.PerpendicularState.FLAT;
                bool tilt = (state.Flags & 8) != 0;
                bool orientationChanged = sprite.CachedPerpState != orientation || sprite.ShouldDoTilt != tilt;
                sprite.CachedPerpState = orientation; sprite.ShouldDoTilt = tilt;
                tk2dTiledSprite tiled = sprite as tk2dTiledSprite;
                tk2dSlicedSprite sliced = sprite as tk2dSlicedSprite;
                tk2dClippedSprite clipped = sprite as tk2dClippedSprite;
                if (tiled != null) { tiled.anchor = (tk2dBaseSprite.Anchor)state.Anchor; tiled.dimensions = new Vector2(state.Width, state.HeightPixels); }
                if (sliced != null) { sliced.anchor = (tk2dBaseSprite.Anchor)state.Anchor; sliced.dimensions = new Vector2(state.Width, state.HeightPixels); }
                if (clipped != null) { clipped.clipBottomLeft = new Vector2(state.ClipX, state.ClipY); clipped.clipTopRight = new Vector2(state.ClipRight, state.ClipTop); }
                sprite.gameObject.layer = state.Layer;
                Vector3 position = new Vector3(state.X, state.Y, state.Z);
                float rotation = state.Rotation;
                SpriteState following;
                bool own = motion != null && (state.Flags & 16) != 0;
                if (!own && nextSprites.TryGetValue(state.Id, out following) && following.CollectionId == state.CollectionId && following.Kind == state.Kind) {
                    Vector3 target = new Vector3(following.X, following.Y, following.Z);
                    // Room teleports must remain teleports, not a trip through walls.
                    if ((target - position).sqrMagnitude < 64f) position = Vector3.Lerp(position, target, blend);
                    rotation = Mathf.LerpAngle(rotation, following.Rotation, blend);
                }
                if (own) position += new Vector3(shift.x, shift.y, 0);
                sprite.transform.position = position;
                sprite.transform.rotation = Quaternion.Euler(0, 0, rotation);
                Vector3 visualScale = new Vector3(state.ScaleX, state.ScaleY, state.ScaleZ);
                bool scaleChanged = sprite.transform.localScale != visualScale;
                sprite.transform.localScale = visualScale;
                sprite.FlipX = (state.Flags & 1) != 0; sprite.FlipY = (state.Flags & 2) != 0;
                sprite.SortingOrder = state.SortingOrder; sprite.HeightOffGround = state.Height;
                sprite.color = descriptors[state.CollectionId].MaterialMode == 0 ? (Color)new Color32((byte)state.Color, (byte)(state.Color >> 8), (byte)(state.Color >> 16), (byte)(state.Color >> 24)) : Color.white;
                if (scaleChanged || orientationChanged) sprite.ForceBuild(); else sprite.ForceRotationRebuild();
                Renderer proxyRenderer = sprite.GetComponent<Renderer>();
                if (proxyRenderer != null) proxyRenderer.enabled = true;
            }
            removed.Clear();
            foreach (KeyValuePair<int, tk2dBaseSprite> pair in proxies) if (!present.Contains(pair.Key)) { if (pair.Value != null) UnityEngine.Object.Destroy(pair.Value.gameObject); removed.Add(pair.Key); }
            foreach (int id in removed) {
                proxies.Remove(id); Material material;
                if (proxyMaterials.TryGetValue(id, out material)) { if (material != null) UnityEngine.Object.Destroy(material); proxyMaterials.Remove(id); }
            }
            PlayerController local = GameManager.Instance.PrimaryPlayer;
            if (local != null) {
                local.transform.position = new Vector3(RemotePlayerPosition.x, RemotePlayerPosition.y, local.transform.position.z);
                Dungeonator.RoomHandler room = GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(new IntVector2((int)Math.Floor(RemotePlayerPosition.x), (int)Math.Floor(RemotePlayerPosition.y)));
                if (room != null && room != local.CurrentRoom) ClientSimulationGuard.UpdatePlaceholderRoom(local, room);
            }
            CameraController controller = GameManager.Instance.MainCameraController;
            if (controller != null) {
                Disable(controller);
                Vector3 start = new Vector3(snapshot.CameraX, snapshot.CameraY, snapshot.CameraZ);
                Vector3 end = new Vector3(next.CameraX, next.CameraY, next.CameraZ);
                controller.Camera.transform.position = motion != null ? new Vector3(motion.CameraX, motion.CameraY, motion.CameraZ) + (motion.CameraFollowsPlayer ? new Vector3(lead.x, lead.y, 0) : Vector3.zero) : (end - start).sqrMagnitude < 144 ? Vector3.Lerp(start, end, blend) : start;
                controller.Camera.orthographicSize = motion != null ? motion.CameraSize : Mathf.Lerp(snapshot.CameraSize, next.CameraSize, blend);
            }
        }

        private void Disable(Behaviour behaviour)
        { disabled.Track(behaviour); }

        private void Protect(HealthHaver health)
        {
            if (health == null) return;
            if (!protectedHealth.ContainsKey(health)) protectedHealth.Add(health, health.PreventAllDamage);
            health.PreventAllDamage = true;
        }

        public void SuppressLocalSimulation()
        {
            if (disposed) return;
            if (!suppressing) {
                suppressing = true;
                Camera.onPreCull += BeforeCameraCull;
                SeedSprites();
                foreach (tk2dBaseSprite sprite in nativeSprites) if (sprite != null) hidden.Track(sprite.GetComponent<Renderer>());
            }
            if (Time.realtimeSinceStartup >= nextActorRefresh) {
                nextActorRefresh = Time.realtimeSinceStartup + 0.2f;
                ActorDiscoveryPasses++;
                localPlayers = UnityEngine.Object.FindObjectsOfType<PlayerController>();
                localEnemies = UnityEngine.Object.FindObjectsOfType<AIActor>();
                localProjectiles = UnityEngine.Object.FindObjectsOfType<Projectile>();
            }
            foreach (PlayerController player in localPlayers) {
                if (player == null) continue;
                player.SetInputOverride("GT_NetworkClient"); Protect(player.healthHaver);
                if (player.specRigidbody != null) player.specRigidbody.Velocity = Vector2.zero;
            }
            foreach (AIActor enemy in localEnemies) {
                if (enemy == null) continue;
                Disable(enemy.behaviorSpeculator); Protect(enemy.healthHaver);
                if (enemy.specRigidbody != null) enemy.specRigidbody.Velocity = Vector2.zero;
            }
            foreach (Projectile projectile in localProjectiles) {
                if (projectile == null) continue;
                Disable(projectile); if (projectile.specRigidbody != null) projectile.specRigidbody.Velocity = Vector2.zero;
            }
            disabled.Enforce(); hidden.Enforce();
            if (GameUIRoot.Instance != null && hud != GameUIRoot.Instance) {
                hud = GameUIRoot.Instance; hud.HideCoreUI("GT_NetworkHUD");
                hud.ToggleLowerPanels(false, false, "GT_NetworkHUD");
                hud.ToggleAllDefaultLabels(false, "GT_NetworkHUD");
            }
        }

        private void BeforeCameraCull(Camera camera)
        {
            // Native LateUpdate/animation can reenable player, hand, gun and shop
            // renderers after the session's Update. No scene search occurs here.
            if (!disposed && suppressing) hidden.Enforce();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true; Camera.onPreCull -= BeforeCameraCull; NativeSpriteLifecycle.Unregister(this);
            foreach (tk2dBaseSprite sprite in proxies.Values) if (sprite != null) UnityEngine.Object.Destroy(sprite.gameObject);
            foreach (Material material in proxyMaterials.Values) if (material != null) UnityEngine.Object.Destroy(material);
            proxyMaterials.Clear(); proxies.Clear(); definitions.Clear(); descriptors.Clear(); collections.Clear(); hostCollections.Clear(); collectionSignatures.Clear();
            frames.Clear(); nextSprites.Clear(); indexedFrame = null;
            Prediction.Clear(); renderStates.Clear();
            hidden.Restore(); disabled.Restore();
            foreach (KeyValuePair<HealthHaver, bool> pair in protectedHealth) if (pair.Key != null) pair.Key.PreventAllDamage = pair.Value;
            protectedHealth.Clear();
            if (hud != null) { hud.ShowCoreUI("GT_NetworkHUD"); hud.ToggleLowerPanels(true, false, "GT_NetworkHUD"); hud.ToggleAllDefaultLabels(true, "GT_NetworkHUD"); }
            foreach (PlayerController player in UnityEngine.Object.FindObjectsOfType<PlayerController>()) player.ClearInputOverride("GT_NetworkClient");
            nativeSprites.Clear(); knownSprites.Clear(); localPlayers = new PlayerController[0]; localEnemies = new AIActor[0]; localProjectiles = new Projectile[0];
            LastSnapshot = null;
        }
    }

    internal sealed class NetworkSpriteProxy : MonoBehaviour { }

    // Observe actual sprite lifecycle once, including newly spawned/pool sprites,
    // instead of searching the entire scene on every snapshot or render frame.
    internal static class NativeSpriteLifecycle
    {
        private static readonly List<SpriteReplication> observers = new List<SpriteReplication>();
        internal static void Register(SpriteReplication observer) { observers.Add(observer); }
        internal static void Unregister(SpriteReplication observer) { observers.Remove(observer); }

        [HarmonyLib.HarmonyPatch(typeof(tk2dBaseSprite), "Awake")]
        private static class Created
        {
            private static void Postfix(tk2dBaseSprite __instance) { foreach (SpriteReplication observer in observers) observer.Observe(__instance); }
        }
        [HarmonyLib.HarmonyPatch(typeof(tk2dBaseSprite), "OnDestroy")]
        private static class Destroyed
        {
            private static void Prefix(tk2dBaseSprite __instance) { foreach (SpriteReplication observer in observers) observer.Forget(__instance); }
        }
    }
}
