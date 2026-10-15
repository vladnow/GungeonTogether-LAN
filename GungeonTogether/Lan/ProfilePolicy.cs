using System;
using System.IO;

namespace GungeonTogether.Lan
{
    internal static class ProfilePolicy
    {
        public const string LanArgument = "--gt-lan-profile";
        public enum SaveProfileMode { Auto, Normal, LAN }

        // Installing the plugin must not change a normal Steam launch's save slot.
        public static bool UsesSeparateProfile(string[] arguments, string gameRoot = null, string bepInExRoot = null, SaveProfileMode mode = SaveProfileMode.Auto)
        {
            bool explicitLan = false;
            if (arguments != null) foreach (string argument in arguments) {
                if (argument == "--gt-dungeon-smoke-host" || argument == "--gt-dungeon-smoke-client") return true;
                if (argument == LanArgument) explicitLan = true;
            }
            if (mode == SaveProfileMode.Normal) return false;
            if (mode == SaveProfileMode.LAN || explicitLan) return true;
            if (string.IsNullOrEmpty(gameRoot) || string.IsNullOrEmpty(bepInExRoot)) return false;
            // A manager selects its profile's BepInEx root. Direct Steam launches
            // use the game's own BepInEx folder and must keep normal saves.
            string gameBep = Canonical(Path.Combine(gameRoot, "BepInEx"));
            string activeBep = Canonical(bepInExRoot);
            return !string.Equals(gameBep, activeBep, StringComparison.OrdinalIgnoreCase);
        }
        private static string Canonical(string path)
        { return Path.GetFullPath(path).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar); }
    }
}
