using System;
using GungeonTogether.Lan;
using UnityEngine;

namespace GungeonTogether.Networking
{
    // A visual proxy only. It never moves or collides with the local controller.
    public sealed class RemotePlayerAvatar : MonoBehaviour
    {
        private readonly SnapshotBuffer<LanPlayerState> frames = new SnapshotBuffer<LanPlayerState>(SnapshotBuffer<LanPlayerState>.DefaultDelay);
        private tk2dSprite sprite;
        private string collectionKey;
        private tk2dSpriteCollectionData collection;
        public int RenderedFrames { get; private set; }
        public float DistanceMoved { get; private set; }
        public string CollectionName { get { return collection == null ? "" : collection.name; } }
        private Vector3 firstPosition;

        public static RemotePlayerAvatar Create(ulong peer, Vector2 position, float rotation)
        {
            GameObject go = new GameObject("GT_FoyerPeer_" + peer);
            return go.AddComponent<RemotePlayerAvatar>();
        }

        public void Apply(LanPlayerState state)
        { frames.Push(state, state.Sequence, state.SentAt, Time.realtimeSinceStartup); }

        private void LateUpdate()
        {
            LanPlayerState from, to; float blend;
            if (!frames.Sample(Time.realtimeSinceStartup, out from, out to, out blend)) return;
            string key = from.CollectionGuid + "|" + from.Collection;
            if (collection == null || collectionKey != key) {
                // Loading the peer's own prefab also resolves different heroes
                // and avoids interpreting its sprite index in our collection.
                NativeWorld.CharacterPrefab(from.Character);
                collection = FindCollection(from);
                collectionKey = key;
            }
            if (collection == null || from.SpriteId >= collection.spriteDefinitions.Length) return;
            if (sprite == null) sprite = gameObject.AddComponent<tk2dSprite>();
            if (sprite.Collection != collection || sprite.spriteId != from.SpriteId) sprite.SetSprite(collection, from.SpriteId);
            Vector3 start = new Vector3(from.X, from.Y, from.Z), end = new Vector3(to.X, to.Y, to.Z);
            transform.position = (end - start).sqrMagnitude < 64 ? Vector3.Lerp(start, end, blend) : start;
            if (RenderedFrames == 0) firstPosition = transform.position;
            RenderedFrames++; DistanceMoved = Mathf.Max(DistanceMoved, Vector3.Distance(firstPosition, transform.position));
            transform.rotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(from.Rotation, to.Rotation, blend));
            transform.localScale = new Vector3(from.ScaleX, from.ScaleY, 1);
            sprite.HeightOffGround = from.Height; sprite.FlipX = from.FlipX;
        }

        private static tk2dSpriteCollectionData FindCollection(LanPlayerState state)
        {
            foreach (tk2dSpriteCollectionData candidate in Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>())
                if ((!string.IsNullOrEmpty(state.CollectionGuid) && candidate.spriteCollectionGUID == state.CollectionGuid) ||
                    (string.IsNullOrEmpty(state.CollectionGuid) && (candidate.name == state.Collection || candidate.spriteCollectionName == state.Collection))) return candidate.inst;
            if (!string.IsNullOrEmpty(state.CollectionGuid)) {
                try { return tk2dSystem.LoadResourceByGUID<tk2dSpriteCollectionData>(state.CollectionGuid); } catch { }
            }
            return null;
        }
    }
}
