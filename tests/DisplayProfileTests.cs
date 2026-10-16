using System;
using System.IO;
using System.Security.Cryptography;
using GungeonTogether.Lan;

internal static class DisplayProfileTests
{
    private static int count;
    private static void Check(bool yes, string label) { if (!yes) throw new Exception(label); count++; Console.WriteLine("PASS: " + label); }
    public static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "GT-display-" + Guid.NewGuid().ToString("N"));
        try {
            string normal = Path.Combine(root, "normal"), lan = Path.Combine(root, "lan");
            Directory.CreateDirectory(normal); Directory.CreateDirectory(lan);
            string options = "version: 0\n{\"preferredResolutionX\":1920,\"preferredResolutionY\":1080,\"m_fullscreenStyle\":\"FULLSCREEN\",\"m_preferredFullscreenMode\":\"FULLSCREEN\",\"m_preferredScalingMode\":\"FORCE_PIXEL_PERFECT\",\"DisplaySafeArea\":1.0,\"volume\":0.9}";
            string source = Path.Combine(normal, "SlotA.options"), target = Path.Combine(lan, "SlotA.options");
            File.WriteAllText(source, options); File.WriteAllText(Path.Combine(normal, "SlotA.save"), "normal progress");
            Check(ProfileDisplayPolicy.Initialize(normal, lan, null), "first LAN options initialized");
            Check(File.ReadAllText(target) == options, "header and selected display options preserved");
            Check(!File.Exists(Path.Combine(lan, "SlotA.save")), "normal progression never copied");
            File.WriteAllText(target, options.Replace("1920", "1280"));
            Check(!ProfileDisplayPolicy.Initialize(normal, lan, null) && File.ReadAllText(target).Contains("1280"), "subsequent LAN display choice preserved");
            string migrated = Path.Combine(root, "migrate"); Directory.CreateDirectory(migrated);
            string old = options.Replace("1920", "800").Replace("1080", "600").Replace("0.9", "0.0");
            File.WriteAllText(Path.Combine(migrated, "SlotA.options"), old);
            Check(ProfileDisplayPolicy.Initialize(normal, migrated, null), "old LAN display migrated once");
            string next = File.ReadAllText(Path.Combine(migrated, "SlotA.options"));
            Check(next.Contains("1920") && next.Contains("1080") && next.Contains("\"volume\":0.0"), "only display values replaced in existing profile");
            Check(File.ReadAllText(Path.Combine(migrated, "SlotA.options.before-0.3.1-display")) == old, "previous LAN options backed up");
            Check(File.ReadAllText(source) == options && File.ReadAllText(Path.Combine(normal, "SlotA.save")) == "normal progress", "normal options and progression untouched");
            string invalid = Path.Combine(root, "invalid"); Directory.CreateDirectory(invalid);
            File.WriteAllText(source, options.Replace("1920", "-1"));
            Check(!ProfileDisplayPolicy.Initialize(normal, invalid, null) && !File.Exists(Path.Combine(invalid, "SlotA.options")), "unknown native resolution defaults are not imported");
            Console.WriteLine("DISPLAY_PROFILE_TESTS_PASS: " + count); return 0;
        } catch (Exception ex) { Console.WriteLine(ex); return 1; }
        finally {
            string resolved = Path.GetFullPath(root), temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (resolved.StartsWith(temp, StringComparison.OrdinalIgnoreCase) && Path.GetFileName(resolved).StartsWith("GT-display-", StringComparison.Ordinal) && Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }
    }
}
