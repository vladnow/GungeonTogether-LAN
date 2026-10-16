using System;
using System.IO;
using System.Text.RegularExpressions;

namespace GungeonTogether.Lan
{
    // Options are separate from progression. Never copy or write normal saves.
    public static class ProfileDisplayPolicy
    {
        private static readonly string[] Fields = { "preferredResolutionX", "preferredResolutionY", "m_fullscreenStyle", "m_preferredFullscreenMode", "m_preferredScalingMode", "DisplaySafeArea" };

        public static bool Initialize(string normalDirectory, string lanDirectory, Action<string> log)
        {
            string marker = Path.Combine(lanDirectory, "display-profile-0.3.1.initialized");
            if (File.Exists(marker)) return false;
            bool changed = false, found = false;
            Directory.CreateDirectory(lanDirectory);
            foreach (string slot in new string[] { "SlotA", "SlotB", "SlotC" }) {
                string source = Path.Combine(normalDirectory, slot + ".options");
                string target = Path.Combine(lanDirectory, slot + ".options");
                if (!File.Exists(source)) continue;
                string normal = File.ReadAllText(source);
                int width, height;
                if (!int.TryParse(Value(normal, "preferredResolutionX"), out width) || !int.TryParse(Value(normal, "preferredResolutionY"), out height) || width < 640 || height < 480 || width > 16384 || height > 16384) continue;
                found = true;
                if (!File.Exists(target)) {
                    File.WriteAllText(target, normal); changed = true;
                } else {
                    string previous = File.ReadAllText(target), updated = previous;
                    foreach (string field in Fields) {
                        string value = Value(normal, field);
                        if (value == null) continue;
                        Regex pattern = Pattern(field);
                        if (pattern.Matches(updated).Count != 1) continue;
                        updated = pattern.Replace(updated, delegate(Match m) { return m.Groups[1].Value + value; }, 1);
                    }
                    if (updated != previous) {
                        string backup = target + ".before-0.3.1-display";
                        if (!File.Exists(backup)) File.Copy(target, backup);
                        File.WriteAllText(target, updated); changed = true;
                    }
                }
            }
            if (found) File.WriteAllText(marker, "Display settings inherited once from the normal options. Later LAN changes are preserved.\n");
            if (log != null) log("GT_DISPLAY_PROFILE " + (found ? (changed ? "initialized from normal display options" : "already matches normal display options") : "no valid normal resolution available; native defaults retained"));
            return changed;
        }

        private static Regex Pattern(string field)
        { return new Regex("(\"" + Regex.Escape(field) + "\"\\s*:\\s*)(\"[A-Za-z0-9_]+\"|-?[0-9]+(?:\\.[0-9]+)?)(?=\\s*[,}])", RegexOptions.CultureInvariant); }
        private static string Value(string text, string field)
        { MatchCollection values = Pattern(field).Matches(text); return values.Count == 1 ? values[0].Groups[2].Value : null; }
    }
}
