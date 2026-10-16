using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace GungeonTogether.Lan
{
    // Pure rendering policies, shared by native rendering and regression tests.
    internal static class SpriteRenderPolicy
    {
        internal static float ScaleAxis(float transformScale, float spriteScale) { return transformScale * Math.Abs(spriteScale); }
    }

    internal sealed class SpriteCollectionDescriptor
    {
        private const string Prefix = "@GT1|";
        internal string Name, Signature;
        internal int MaterialMode; // 0: native default, 1: single-pass outline, 2: offset outline.

        internal static string Encode(string name, string signature, int materialMode)
        {
            name = name ?? "";
            if (name.Length > 180) name = name.Substring(0, 180);
            string value = Prefix + materialMode + "|" + signature + "|" + name;
            Parse(value); return value;
        }

        internal static SpriteCollectionDescriptor Parse(string value)
        {
            if (value == null) throw new IOException("Missing sprite collection descriptor.");
            if (!value.StartsWith(Prefix, StringComparison.Ordinal)) return new SpriteCollectionDescriptor { Name = value, Signature = "", MaterialMode = 0 };
            int modeEnd = value.IndexOf('|', Prefix.Length), signatureEnd = modeEnd < 0 ? -1 : value.IndexOf('|', modeEnd + 1);
            int mode;
            if (modeEnd < 0 || signatureEnd < 0 || !int.TryParse(value.Substring(Prefix.Length, modeEnd - Prefix.Length), out mode) || mode < 0 || mode > 2)
                throw new IOException("Invalid sprite material descriptor.");
            string signature = value.Substring(modeEnd + 1, signatureEnd - modeEnd - 1);
            if (signature.Length != 64) throw new IOException("Invalid sprite collection signature.");
            foreach (char c in signature) if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f')) throw new IOException("Invalid sprite collection signature.");
            return new SpriteCollectionDescriptor { Name = value.Substring(signatureEnd + 1), Signature = signature, MaterialMode = mode };
        }

        internal static string Fingerprint(IList<string> names)
        {
            using (MemoryStream stream = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    writer.Write(names.Count);
                    foreach (string name in names) writer.Write(name ?? "");
                    writer.Flush();
                    using (SHA256 hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }

    // Reassert visibility at frame and camera boundaries without rediscovery or
    // duplicate restoration entries when native animation reenables a renderer.
    internal sealed class VisibilityOverrides<T> where T : class
    {
        private readonly Dictionary<T, bool> original = new Dictionary<T, bool>();
        private readonly Func<T, bool> alive, enabled;
        private readonly Action<T, bool> setEnabled;
        internal int Count { get { return original.Count; } }
        internal VisibilityOverrides(Func<T, bool> alive, Func<T, bool> enabled, Action<T, bool> setEnabled)
        { this.alive = alive; this.enabled = enabled; this.setEnabled = setEnabled; }
        internal void Track(T item)
        {
            if (item == null || !alive(item)) return;
            if (!original.ContainsKey(item)) original.Add(item, enabled(item));
            if (enabled(item)) setEnabled(item, false);
        }
        internal void Forget(T item) { if (item != null) original.Remove(item); }
        internal void Enforce()
        { foreach (T item in original.Keys) if (alive(item) && enabled(item)) setEnabled(item, false); }
        internal void Restore()
        { foreach (KeyValuePair<T, bool> pair in original) if (alive(pair.Key)) setEnabled(pair.Key, pair.Value); original.Clear(); }
    }
}
