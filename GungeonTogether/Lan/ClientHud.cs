using System;
using System.Collections.Generic;
using UnityEngine;

namespace GungeonTogether.Lan
{
    // A presentation-only HUD: never mutate the client's non-authoritative health
    // or inventory just to drive native widgets. All pictures come from local assets.
    public sealed class ClientHud : IDisposable
    {
        private sealed class Icon
        {
            public Texture Texture;
            public Rect Uv = new Rect(0, 0, 1, 1);
            public float Width, Height;
        }
        private GameUIRoot root;
        private Icon full, half, empty, armor, blank, coin, key, gunFrame, itemFrame;
        private readonly Dictionary<int, Icon> equipment = new Dictionary<int, Icon>();
        private readonly List<Texture2D> owned = new List<Texture2D>();
        private GUIStyle label, small;
        public int DrawnFrames { get; private set; }
        public int NativeIcons { get; private set; }
        public int GunId { get; private set; }
        public int ItemId { get; private set; }
        private static Icon Atlas(dfSprite sprite, string name = null)
        {
            if (sprite == null || sprite.Atlas == null) return null;
            dfAtlas.ItemInfo info = sprite.Atlas[name ?? sprite.SpriteName];
            if (info == null || info.deleted || info.rotated) return null;
            return new Icon { Texture = sprite.Atlas.Texture, Uv = info.region, Width = info.sizeInPixels.x, Height = info.sizeInPixels.y };
        }
        private void Bind()
        {
            if (root != null && root == GameUIRoot.Instance) return;
            root = GameUIRoot.Instance; if (root == null) return;
            GameUIHeartController hearts = root.heartControllers[0];
            full = Atlas(hearts.heartSpritePrefab, hearts.fullHeartSpriteName);
            half = Atlas(hearts.heartSpritePrefab, hearts.halfHeartSpriteName);
            empty = Atlas(hearts.heartSpritePrefab, hearts.emptyHeartSpriteName);
            armor = Atlas(hearts.armorSpritePrefab);
            blank = Atlas(root.blankControllers[0].blankSpritePrefab);
            coin = Atlas(root.p_playerCoinLabel.Parent.GetComponentInChildren<dfSprite>());
            key = Atlas(root.p_playerKeyLabel.Parent.GetComponentInChildren<dfSprite>());
            gunFrame = Atlas(root.ammoControllers[0].GunBoxSprite);
            itemFrame = Atlas(root.itemControllers[0].ItemBoxSprite);
            NativeIcons = 0;
            foreach (Icon icon in new Icon[] { full, half, empty, armor, blank, coin, key, gunFrame, itemFrame }) if (icon != null) NativeIcons++;
        }
        private Icon Equipment(int id)
        {
            if (id < 0) return null;
            Icon icon;
            if (equipment.TryGetValue(id, out icon)) return icon;
            if (equipment.Count >= 64) { foreach (Texture2D texture in owned) UnityEngine.Object.Destroy(texture); owned.Clear(); equipment.Clear(); }
            PickupObject item = PickupObjectDatabase.GetById(id);
            if (item == null || item.sprite == null) return null;
            tk2dSpriteDefinition definition = item.sprite.GetCurrentSpriteDef();
            if (definition == null || definition.uvs == null || definition.uvs.Length != 4 || definition.material == null) return null;
            Texture textureSource = definition.material.mainTexture;
            if (textureSource == null) return null;
            // Render all four UVs so rotated atlas packing cannot rotate the gun icon.
            int width = Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(definition.position3.x - definition.position0.x) * 16), 1, 256);
            int height = Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(definition.position3.y - definition.position0.y) * 16), 1, 256);
            Vector2[] uv = definition.uvs;
            float minU = 1, minV = 1, maxU = 0, maxV = 0;
            foreach (Vector2 point in uv) { minU = Mathf.Min(minU, point.x); minV = Mathf.Min(minV, point.y); maxU = Mathf.Max(maxU, point.x); maxV = Mathf.Max(maxV, point.y); }
            int cropWidth = Mathf.Clamp(Mathf.RoundToInt((maxU - minU) * textureSource.width), 1, 256);
            int cropHeight = Mathf.Clamp(Mathf.RoundToInt((maxV - minV) * textureSource.height), 1, 256);
            RenderTexture target = RenderTexture.GetTemporary(cropWidth, cropHeight, 0, RenderTextureFormat.ARGB32);
            RenderTexture before = RenderTexture.active;
            Texture2D output = null, crop = null;
            try {
                // Unity's built-in blit works even when optional sprite shaders are
                // stripped. The atlas itself need not be CPU-readable.
                Graphics.Blit(textureSource, target, new Vector2(maxU - minU, maxV - minV), new Vector2(minU, minV));
                RenderTexture.active = target;
                crop = new Texture2D(cropWidth, cropHeight, TextureFormat.ARGB32, false);
                crop.ReadPixels(new Rect(0, 0, cropWidth, cropHeight), 0, 0); crop.Apply();
                Color32[] source = crop.GetPixels32(), pixels = new Color32[width * height];
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) {
                    Vector2 point = uv[0] + (uv[1] - uv[0]) * ((x + .5f) / width) + (uv[2] - uv[0]) * ((y + .5f) / height);
                    int sx = Mathf.Clamp(Mathf.FloorToInt((point.x - minU) / Mathf.Max(.000001f, maxU - minU) * cropWidth), 0, cropWidth - 1);
                    int sy = Mathf.Clamp(Mathf.FloorToInt((point.y - minV) / Mathf.Max(.000001f, maxV - minV) * cropHeight), 0, cropHeight - 1);
                    pixels[y * width + x] = source[sy * cropWidth + sx];
                }
                output = new Texture2D(width, height, TextureFormat.ARGB32, false);
                output.SetPixels32(pixels); output.Apply(); output.filterMode = FilterMode.Point;
                owned.Add(output);
            } finally {
                RenderTexture.active = before; RenderTexture.ReleaseTemporary(target);
                if (crop != null) UnityEngine.Object.Destroy(crop);
            }
            icon = new Icon { Texture = output, Width = width, Height = height }; equipment[id] = icon; return icon;
        }
        private static void Picture(Icon icon, Rect rect)
        {
            if (icon == null || icon.Texture == null) return;
            float scale = Mathf.Min(rect.width / icon.Width, rect.height / icon.Height);
            float w = icon.Width * scale, h = icon.Height * scale;
            GUI.DrawTextureWithTexCoords(new Rect(rect.center.x - w / 2, rect.center.y - h / 2, w, h), icon.Texture, icon.Uv, true);
        }
        private void Text(Rect rect, string text, bool tiny = false)
        {
            GUIStyle style = tiny ? small : label;
            Color before = GUI.color;
            GUI.color = Color.black; GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style);
            GUI.color = Color.white; GUI.Label(rect, text, style); GUI.color = before;
        }
        private static void Bar(Rect rect, float percent, Color color)
        {
            Color before = GUI.color;
            GUI.color = Color.black; GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color; GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 1, (rect.width - 2) * Mathf.Clamp01(percent), rect.height - 2), Texture2D.whiteTexture);
            GUI.color = before;
        }
        public void Draw(WorldSnapshot state)
        {
            if (Event.current.type != EventType.Repaint) return;
            Bind();
            if (label == null) {
                label = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, wordWrap = false, richText = false, padding = new RectOffset() };
                label.normal.textColor = Color.white;
                small = new GUIStyle(label) { fontSize = 8, fontStyle = FontStyle.Normal };
            }
            Matrix4x4 before = GUI.matrix; Color color = GUI.color; int depth = GUI.depth;
            float scale = Mathf.Min(Screen.width / 480f, Screen.height / 270f);
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1)); GUI.color = Color.white; GUI.depth = -200;
            try {
                int hearts = Mathf.Min(24, Mathf.CeilToInt(state.MaxHealth));
                int shields = Mathf.Min(16, Mathf.CeilToInt(state.Armor));
                for (int i = 0; i < hearts; i++) Picture(state.Health >= i + 1 ? full : state.Health >= i + .5f ? half : empty, new Rect(7 + i % 8 * 13, 6 + i / 8 * 12, 12, 11));
                int rows = Mathf.Max(1, (hearts + 7) / 8);
                for (int i = 0; i < shields; i++) Picture(armor, new Rect(7 + i % 8 * 12, 6 + rows * 12 + i / 8 * 12, 11, 11));
                float y = 9 + rows * 12 + ((shields + 7) / 8) * 12;
                if (state.MaxHealth > 24 || state.Armor > 16) { Text(new Rect(8, y, 155, 13), "HP " + state.Health.ToString("0.#") + " / " + state.MaxHealth.ToString("0.#") + "   Armor " + state.Armor.ToString("0"), true); y += 12; }
                for (int i = 0; i < Mathf.Min(8, state.Blanks); i++) Picture(blank, new Rect(8 + i * 10, y, 9, 9));
                if (state.Blanks > 8) Text(new Rect(92, y - 1, 60, 13), "×" + state.Blanks, true);
                y += 13;
                Picture(coin, new Rect(8, y, 9, 11)); Text(new Rect(21, y - 1, 70, 15), state.Currency.ToString());
                Picture(key, new Rect(69, y, 11, 11)); Text(new Rect(84, y - 1, 65, 15), state.InfiniteKeys ? "∞" : state.Keys.ToString());
                Rect gunBox = new Rect(7, height - 51, 48, 43);
                Picture(gunFrame, gunBox); Picture(Equipment(state.GunId), new Rect(gunBox.x + 6, gunBox.y + 8, 35, 25));
                GunId = state.GunId; ItemId = state.ItemId;
                Text(new Rect(8, height - 65, 140, 15), state.InfiniteAmmo ? "∞" : state.Ammo.ToString());
                Text(new Rect(60, height - 24, 170, 16), state.Reloading ? "RELOADING" : state.ClipAmmo + " / " + state.ClipCapacity);
                // Additional thin clip gauge stays legible for unusual modded magazines.
                Bar(new Rect(59, height - 11, 58, 3), state.ClipCapacity > 0 ? (float)state.ClipAmmo / state.ClipCapacity : 0, new Color(.95f, .75f, .24f));
                if (state.ItemId >= 0) {
                    Rect box = new Rect(width - 47, height - 49, 39, 40);
                    Picture(itemFrame, box); Picture(Equipment(state.ItemId), new Rect(box.x + 6, box.y + 5, 27, 26));
                    Bar(new Rect(box.x + 3, height - 13, 33, 4), state.ActiveItemReady || state.ActiveItemActive ? 1 : 1 - state.ActiveItemCooldown, state.ActiveItemReady ? Color.cyan : new Color(.65f, .65f, .65f));
                    if (state.ActiveItemActive) Text(new Rect(box.x - 4, box.y - 12, 65, 14), "ACTIVE", true);
                }
                Text(new Rect(width - 110, 6, 105, 12), "Ctrl+P · co-op menu", true);
                string status = state.GameOver ? "RUN ENDED" : state.IsGhost ? "GHOST · AWAITING REVIVAL" : state.IsDead ? "DOWNED" : state.Paused ? "PAUSED BY HOST" : "";
                if (status.Length > 0) Text(new Rect(width / 2 - 85, 8, 240, 20), status);
                DrawnFrames++;
            } finally { GUI.matrix = before; GUI.color = color; GUI.depth = depth; }
        }
        public void Dispose()
        {
            foreach (Texture2D texture in owned) if (texture != null) UnityEngine.Object.Destroy(texture);
            owned.Clear(); equipment.Clear(); root = null;
        }
    }
}
