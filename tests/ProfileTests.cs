using System;
using System.IO;
using GungeonTogether.Lan;

internal static class ProfileTests
{
    private static int checks;
    private static void Check(bool condition, string text)
    {
        if (!condition) throw new Exception(text);
        checks++; Console.WriteLine("PASS: " + text);
    }
    public static int Main(string[] args)
    {
        try {
            Check(!ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe" }), "ordinary Steam launch keeps normal profile");
            Check(!ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe", "-screen-fullscreen", "1", "-nosound" }), "ordinary game flags keep normal profile");
            Check(!ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe", "--gt-lan-profile=false" }), "substring is not LAN authorization");
            Check(!ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe", "C:\\--gt-lan-profile\\EtG.exe" }), "path text cannot activate LAN profile");
            Check(!ProfilePolicy.UsesSeparateProfile(null), "missing arguments default to normal profile");
            Check(!ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe", "--gt-profile-check" }), "normal profile regression harness keeps normal selection");
            Check(ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe", "--gt-lan-profile" }), "explicit launcher argument selects LAN profile");
            Check(ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe", "--gt-dungeon-smoke-host" }), "host game test isolates saves");
            Check(ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe", "--gt-dungeon-smoke-client" }), "client game test isolates saves");
            Check(File.ReadAllText(Path.Combine(args[0], "launcher/Launcher.cs")).Contains("\"--gt-lan-profile\""), "launcher passes the explicit LAN argument");
            string game = Path.Combine(Path.GetTempPath(), "GT-Game");
            string localBep = Path.Combine(game, "BepInEx");
            string managerBep = Path.Combine(Path.GetTempPath(), "GT-Profiles/LAN/BepInEx");
            Check(!ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe" }, game, localBep), "direct Steam installation keeps normal saves in Auto mode");
            Check(!ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe" }, game, localBep.ToUpperInvariant() + Path.DirectorySeparatorChar), "case and trailing separator cannot change profile selection");
            Check(!ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe" }, game, Path.Combine(game, "BepInEx/../BepInEx")), "equivalent paths keep normal saves");
            Check(ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe" }, game, managerBep), "manager profile automatically enables LAN without custom arguments");
            Check(!ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe" }, game, managerBep, ProfilePolicy.SaveProfileMode.Normal), "Normal setting protects ordinary saves in a manager profile");
            Check(!ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe", "--gt-lan-profile" }, game, localBep, ProfilePolicy.SaveProfileMode.Normal), "Normal setting takes precedence over stale LAN arguments");
            Check(ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe" }, game, localBep, ProfilePolicy.SaveProfileMode.LAN), "explicit LAN setting isolates direct installation saves");
            Check(!ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe" }, game, null), "unknown manager location defaults to normal saves");
            Check(ProfilePolicy.UsesSeparateProfile(new[] { "EtG.exe", "--gt-dungeon-smoke-host" }, game, localBep, ProfilePolicy.SaveProfileMode.Normal), "game smoke remains isolated even with a Normal fixture setting");
            Console.WriteLine("PROFILE_TESTS_PASS: " + checks + " checks"); return 0;
        } catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }
}
