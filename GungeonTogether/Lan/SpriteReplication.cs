using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GungeonTogether.Lan
{
    // Native assets render locally. These are transforms/sprite states, never video frames.
    public sealed class SpriteReplication : IDisposable
    {
        private readonly Dictionary<int, int> hostCollections = new Dictionary<int, int>();
        private readonly Dictionary<int, CollectionInfo> definitions = new Dictionary<int, CollectionInfo>();
        private readonly Dictionary<int, tk2dSpriteCollectionData> collections = new Dictionary<int, tk2dSpriteCollectionData>();
        private readonly Dictionary<int, tk2dBaseSprite> proxies = new Dictionary<int, tk2dBaseSprite>();
        private readonly List<Renderer> hidden = new List<Renderer>();
        private readonly List<Behaviour> disabled = new List<Behaviour>();
        private readonly Dictionary<HealthHaver, bool> protectedHealth = new Dictionary<HealthHaver, bool>();
        private GameUIRoot hud;
        private readonly SnapshotBuffer<WorldSnapshot> frames = new SnapshotBuffer<WorldSnapshot>(0.12);
        private readonly Dictionary<int, SpriteState> nextSprites = new Dictionary<int, SpriteState>();
        private WorldSnapshot indexedFrame;
        private readonly HashSet<int> present = new HashSet<int>();
        private readonly List<int> removed = new List<int>();
        public int RenderedFrames { get; private set; }
        public int InterpolatedFrames { get; private set; }
        public Vector2 RemotePlayerPosition;
        public WorldSnapshot LastSnapshot;

        public void Define(CollectionInfo info) { definitions[info.Id] = info; }

        public WorldSnapshot Capture(int epoch, uint sequence, Action<byte[]> send)
        {
            WorldSnapshot snapshot = new WorldSnapshot { Epoch = epoch, Sequence = sequence, SentAt = Time.realtimeSinceStartup };
            GameManager gm = GameManager.Instance;
            Camera camera = gm.MainCameraController.Camera;
            Vector3 cam = camera.transform.position;
            snapshot.CameraX = cam.x; snapshot.CameraY = cam.y; snapshot.CameraZ = cam.z; snapshot.CameraSize = camera.orthographicSize;
            PlayerController player = gm.SecondaryPlayer;
            if (player != null) {
                snapshot.PlayerX = player.transform.position.x; snapshot.PlayerY = player.transform.position.y;
                snapshot.Health = Mathf.Max(0, player.healthHaver.GetCurrentHealth()); snapshot.Armor = Mathf.Max(0, player.healthHaver.Armor);
                snapshot.Ammo = player.CurrentGun != null ? Mathf.Max(0, player.CurrentGun.CurrentAmmo) : 0;
            }
            snapshot.Paused = gm.IsPaused;
            foreach (tk2dBaseSprite sprite in UnityEngine.Object.FindObjectsOfType<tk2dBaseSprite>()) {
                Renderer renderer = sprite.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled || sprite.Collection == null || sprite.spriteId < 0) continue;
                int collectionId;
                if (!hostCollections.TryGetValue(sprite.Collection.GetInstanceID(), out collectionId)) {
                    collectionId = hostCollections.Count + 1;
                    if (collectionId > 4096) throw new IOException("Too many sprite collections.");
                    hostCollections.Add(sprite.Collection.GetInstanceID(), collectionId);
                    CollectionInfo info = new CollectionInfo { Epoch = epoch, Id = collectionId, Guid = sprite.Collection.spriteCollectionGUID ?? "", Name = sprite.Collection.spriteCollectionName ?? sprite.Collection.name, Asset = sprite.Collection.assetName ?? "" };
                    send(info.Encode());
                }
                Transform t = sprite.transform; Vector3 pos = t.position; Vector3 scale = t.lossyScale;
                Color32 color = sprite.color;
                SpriteState state = new SpriteState {
                    Id = sprite.GetInstanceID(), CollectionId = collectionId, SpriteId = sprite.spriteId, SortingOrder = sprite.SortingOrder,
                    X = pos.x, Y = pos.y, Z = pos.z, Rotation = t.eulerAngles.z, ScaleX = scale.x * sprite.scale.x, ScaleY = scale.y * sprite.scale.y, ScaleZ = scale.z * sprite.scale.z,
                    Height = sprite.HeightOffGround, Color = (uint)(color.r | color.g << 8 | color.b << 16 | color.a << 24), Flags = (byte)((sprite.FlipX ? 1 : 0) | (sprite.FlipY ? 2 : 0)), Layer = (byte)sprite.gameObject.layer
                };
                tk2dTiledSprite tiled = sprite as tk2dTiledSprite;
                tk2dSlicedSprite sliced = sprite as tk2dSlicedSprite;
                tk2dClippedSprite clipped = sprite as tk2dClippedSprite;
                if (tiled != null) { state.Kind = 1; state.Width = tiled.dimensions.x; state.HeightPixels = tiled.dimensions.y; state.Anchor = (byte)tiled.anchor; }
                else if (sliced != null) { state.Kind = 2; state.Width = sliced.dimensions.x; state.HeightPixels = sliced.dimensions.y; state.Anchor = (byte)sliced.anchor; }
                else if (clipped != null) { state.Kind = 3; state.ClipX = clipped.clipBottomLeft.x; state.ClipY = clipped.clipBottomLeft.y; state.ClipRight = clipped.clipTopRight.x; state.ClipTop = clipped.clipTopRight.y; }
                snapshot.Sprites.Add(state);
            }
            return snapshot;
        }

        private tk2dSpriteCollectionData Resolve(int id)
        {
            tk2dSpriteCollectionData collection;
            if (collections.TryGetValue(id, out collection) && collection != null) return collection;
            CollectionInfo info;
            if (!definitions.TryGetValue(id, out info)) throw new IOException("Snapshot arrived before its sprite definition.");
            foreach (tk2dSpriteCollectionData candidate in Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>()) {
                if ((!string.IsNullOrEmpty(info.Guid) && candidate.spriteCollectionGUID == info.Guid) ||
                    (candidate.spriteCollectionName == info.Name && string.IsNullOrEmpty(info.Guid))) {
                    collections[id] = candidate.inst; return candidate.inst;
                }
            }
            if (!string.IsNullOrEmpty(info.Guid)) {
                try {
                    collection = tk2dSystem.LoadResourceByGUID<tk2dSpriteCollectionData>(info.Guid);
                    if (collection != null) { collections[id] = collection.inst; return collection.inst; }
                } catch { }
            }
            if (!string.IsNullOrEmpty(info.Asset) && info.Asset.IndexOf("..", StringComparison.Ordinal) < 0) {
                try {
                    GameObject asset = BraveResources.Load(info.Asset, ".prefab") as GameObject;
                    if (asset != null) {
                        collection = asset.GetComponent<tk2dSpriteCollectionData>();
                        if (collection != null) { collections[id] = collection.inst; return collection.inst; }
                    }
                } catch { }
            }
            throw new IOException("Missing local sprite asset: " + info.Name + ". Session stopped to avoid invisible hazards.");
        }

        public void Enqueue(WorldSnapshot snapshot)
        {
            if (frames.Push(snapshot, snapshot.Sequence, snapshot.SentAt, Time.realtimeSinceStartup)) LastSnapshot = snapshot;
        }

        public void Render(double now)
        {
            WorldSnapshot snapshot, next; float blend;
            if (!frames.Sample(now, out snapshot, out next, out blend)) return;
            RenderedFrames++;
            if (snapshot != next && blend > 0 && blend < 1) InterpolatedFrames++;
            if (indexedFrame != next) {
                indexedFrame = next; nextSprites.Clear();
                foreach (SpriteState state in next.Sprites) nextSprites[state.Id] = state;
            }
            ApplyFrame(snapshot, next, blend);
        }

        private void ApplyFrame(WorldSnapshot snapshot, WorldSnapshot next, float blend)
        {
            present.Clear();
            foreach (SpriteState state in snapshot.Sprites) {
                present.Add(state.Id);
                tk2dSpriteCollectionData collection = Resolve(state.CollectionId);
                if (state.SpriteId >= collection.spriteDefinitions.Length) throw new IOException("Sprite index differs between game builds.");
                tk2dBaseSprite sprite;
                if (!proxies.TryGetValue(state.Id, out sprite) || sprite == null) {
                    GameObject go = new GameObject("GT_NetworkSprite_" + state.Id);
                    if (state.Kind == 1) sprite = go.AddComponent<tk2dTiledSprite>();
                    else if (state.Kind == 2) sprite = go.AddComponent<tk2dSlicedSprite>();
                    else if (state.Kind == 3) sprite = go.AddComponent<tk2dClippedSprite>();
                    else sprite = go.AddComponent<tk2dSprite>();
                    proxies[state.Id] = sprite;
                }
                if (sprite.Collection != collection || sprite.spriteId != state.SpriteId) sprite.SetSprite(collection, state.SpriteId);
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
                if (nextSprites.TryGetValue(state.Id, out following) && following.CollectionId == state.CollectionId && following.Kind == state.Kind) {
                    Vector3 target = new Vector3(following.X, following.Y, following.Z);
                    // Room teleports must remain teleports, not a trip through walls.
                    if ((target - position).sqrMagnitude < 64f) position = Vector3.Lerp(position, target, blend);
                    rotation = Mathf.LerpAngle(rotation, following.Rotation, blend);
                }
                sprite.transform.position = position;
                sprite.transform.rotation = Quaternion.Euler(0, 0, rotation);
                sprite.transform.localScale = new Vector3(state.ScaleX, state.ScaleY, state.ScaleZ);
                sprite.FlipX = (state.Flags & 1) != 0; sprite.FlipY = (state.Flags & 2) != 0;
                sprite.SortingOrder = state.SortingOrder; sprite.HeightOffGround = state.Height;
                sprite.color = new Color32((byte)state.Color, (byte)(state.Color >> 8), (byte)(state.Color >> 16), (byte)(state.Color >> 24));
            }
            removed.Clear();
            foreach (KeyValuePair<int, tk2dBaseSprite> pair in proxies) if (!present.Contains(pair.Key)) { if (pair.Value != null) UnityEngine.Object.Destroy(pair.Value.gameObject); removed.Add(pair.Key); }
            foreach (int id in removed) proxies.Remove(id);
            Vector2 playerStart = new Vector2(snapshot.PlayerX, snapshot.PlayerY);
            Vector2 playerEnd = new Vector2(next.PlayerX, next.PlayerY);
            RemotePlayerPosition = (playerEnd - playerStart).sqrMagnitude < 64 ? Vector2.Lerp(playerStart, playerEnd, blend) : playerStart;
            PlayerController local = GameManager.Instance.PrimaryPlayer;
            if (local != null) {
                local.transform.position = new Vector3(RemotePlayerPosition.x, RemotePlayerPosition.y, local.transform.position.z);
                Dungeonator.RoomHandler room = GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(new IntVector2((int)Math.Floor(RemotePlayerPosition.x), (int)Math.Floor(RemotePlayerPosition.y)));
                if (room != null && room != local.CurrentRoom) local.ForceChangeRoom(room);
            }
            CameraController controller = GameManager.Instance.MainCameraController;
            if (controller != null) {
                Disable(controller);
                Vector3 start = new Vector3(snapshot.CameraX, snapshot.CameraY, snapshot.CameraZ);
                Vector3 end = new Vector3(next.CameraX, next.CameraY, next.CameraZ);
                controller.Camera.transform.position = (end - start).sqrMagnitude < 144 ? Vector3.Lerp(start, end, blend) : start;
                controller.Camera.orthographicSize = Mathf.Lerp(snapshot.CameraSize, next.CameraSize, blend);
            }
        }

        private void Disable(Behaviour behaviour)
        { if (behaviour != null && behaviour.enabled) { disabled.Add(behaviour); behaviour.enabled = false; } }

        private void Protect(HealthHaver health)
        {
            if (health == null) return;
            if (!protectedHealth.ContainsKey(health)) protectedHealth.Add(health, health.PreventAllDamage);
            health.PreventAllDamage = true;
        }

        public void SuppressLocalSimulation()
        {
            foreach (PlayerController player in UnityEngine.Object.FindObjectsOfType<PlayerController>()) {
                player.SetInputOverride("GT_NetworkClient"); Protect(player.healthHaver);
                if (player.specRigidbody != null) player.specRigidbody.Velocity = Vector2.zero;
            }
            foreach (AIActor enemy in UnityEngine.Object.FindObjectsOfType<AIActor>()) {
                Disable(enemy.behaviorSpeculator); Protect(enemy.healthHaver);
                if (enemy.specRigidbody != null) enemy.specRigidbody.Velocity = Vector2.zero;
            }
            foreach (Projectile projectile in UnityEngine.Object.FindObjectsOfType<Projectile>()) {
                Disable(projectile); if (projectile.specRigidbody != null) projectile.specRigidbody.Velocity = Vector2.zero;
            }
            HashSet<tk2dBaseSprite> network = new HashSet<tk2dBaseSprite>();
            foreach (tk2dBaseSprite sprite in proxies.Values) if (sprite != null) network.Add(sprite);
            foreach (tk2dBaseSprite sprite in UnityEngine.Object.FindObjectsOfType<tk2dBaseSprite>()) {
                if (network.Contains(sprite)) continue;
                Renderer renderer = sprite.GetComponent<Renderer>();
                if (renderer != null && renderer.enabled) { hidden.Add(renderer); renderer.enabled = false; }
            }
            if (GameUIRoot.Instance != null) {
                hud = GameUIRoot.Instance; hud.HideCoreUI("GT_NetworkHUD");
                hud.ToggleLowerPanels(false, false, "GT_NetworkHUD");
                hud.ToggleAllDefaultLabels(false, "GT_NetworkHUD");
            }
        }

        public void Dispose()
        {
            foreach (tk2dBaseSprite sprite in proxies.Values) if (sprite != null) UnityEngine.Object.Destroy(sprite.gameObject);
            proxies.Clear(); definitions.Clear(); collections.Clear(); hostCollections.Clear();
            frames.Clear(); nextSprites.Clear(); indexedFrame = null;
            foreach (Renderer renderer in hidden) if (renderer != null) renderer.enabled = true;
            hidden.Clear();
            foreach (Behaviour behaviour in disabled) if (behaviour != null) behaviour.enabled = true;
            disabled.Clear();
            foreach (KeyValuePair<HealthHaver, bool> pair in protectedHealth) if (pair.Key != null) pair.Key.PreventAllDamage = pair.Value;
            protectedHealth.Clear();
            if (hud != null) { hud.ShowCoreUI("GT_NetworkHUD"); hud.ToggleLowerPanels(true, false, "GT_NetworkHUD"); hud.ToggleAllDefaultLabels(true, "GT_NetworkHUD"); }
            foreach (PlayerController player in UnityEngine.Object.FindObjectsOfType<PlayerController>()) player.ClearInputOverride("GT_NetworkClient");
            LastSnapshot = null;
        }
    }
}
