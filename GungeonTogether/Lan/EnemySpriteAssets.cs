using System;
using System.Collections.Generic;
using UnityEngine;

namespace GungeonTogether.Lan
{
    // Some native collections are embedded in enemy prefabs, not independently
    // addressable by tk2d GUID or BraveResources. Refer to the owning native
    // prefab; never send textures or instantiate a second enemy on the client.
    internal sealed class EnemySpriteAssets
    {
        private readonly Dictionary<int, string> owners = new Dictionary<int, string>();
        private readonly HashSet<int> inspected = new HashSet<int>();
        private readonly Dictionary<string, AIActor> loaded = new Dictionary<string, AIActor>();
        private int discoveryFrame = -1;

        private void Register(AIActor actor)
        {
            if (actor == null || !EnemyAssetIdentity.Valid(actor.EnemyGuid) || actor.EnemyGuid.Length == 0 || !inspected.Add(actor.GetInstanceID())) return;
            foreach (tk2dBaseSprite sprite in actor.GetComponentsInChildren<tk2dBaseSprite>(true)) {
                if (sprite == null || sprite.Collection == null) continue;
                tk2dSpriteCollectionData actual = sprite.Collection.inst;
                if (actual != null) owners[actual.GetInstanceID()] = actor.EnemyGuid;
            }
        }

        internal string Owner(tk2dBaseSprite sprite, tk2dSpriteCollectionData collection)
        {
            string guid;
            if (owners.TryGetValue(collection.GetInstanceID(), out guid)) return guid;
            AIActor actor = sprite.GetComponentInParent<AIActor>();
            if (actor != null && EnemyAssetIdentity.Valid(actor.EnemyGuid) && actor.EnemyGuid.Length != 0) {
                owners[collection.GetInstanceID()] = actor.EnemyGuid;
                return actor.EnemyGuid;
            }
            // First encounter may be an outline or a death effect outside the
            // actor hierarchy. Inspect already loaded prefabs without loading
            // every enemy in the database or guessing from collection names.
            if (discoveryFrame != Time.frameCount) {
                discoveryFrame = Time.frameCount;
                foreach (AIActor candidate in Resources.FindObjectsOfTypeAll<AIActor>()) Register(candidate);
            }
            owners.TryGetValue(collection.GetInstanceID(), out guid);
            return guid ?? "";
        }

        internal AIActor Load(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            AIActor prefab;
            if (loaded.TryGetValue(guid, out prefab) && prefab != null) return prefab;
            prefab = EnemyDatabase.GetOrLoadByGuid(guid);
            if (prefab != null) loaded[guid] = prefab;
            return prefab;
        }
    }
}
