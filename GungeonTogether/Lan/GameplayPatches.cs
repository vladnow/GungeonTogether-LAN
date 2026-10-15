using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using InControl;
using UnityEngine;

namespace GungeonTogether.Lan
{
    internal static class GameplayPatches
    {
        private static DungeonSession Session { get { return DungeonPlugin.Instance == null ? null : DungeonPlugin.Instance.Session; } }
        private static bool Smoke { get { return DungeonPlugin.Instance != null && DungeonPlugin.Instance.IsSmoke; } }

        [HarmonyPatch]
        private static class ClientActorTicks
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (Type type in new Type[] { typeof(PlayerController), typeof(AIActor) })
                    foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                        if ((method.Name == "Update" || method.Name == "LateUpdate") && method.GetMethodBody() != null) yield return method;
            }
            private static bool Prefix() { return Session == null || !Session.Mirroring; }
        }

        [HarmonyPatch]
        private static class SilentSmoke
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (MethodInfo method in typeof(AkSoundEngine).GetMethods(BindingFlags.Public | BindingFlags.Static))
                    if (method.Name == "PostEvent" && method.ReturnType == typeof(uint)) yield return method;
            }
            private static bool Prefix(ref uint __result) { if (!Smoke) return true; __result = 0; return false; }
        }

        [HarmonyPatch]
        private static class NoSteamWritesInSmoke
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (Type type in typeof(GameManager).Assembly.GetTypes()) {
                    if (type.Name.IndexOf("Steam", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                        if ((method.Name == "StoreStats" || method.Name == "SetAchievement" || method.Name == "ClearAchievement") && method.GetMethodBody() != null) yield return method;
                }
            }
            private static bool Prefix() { return !Smoke; }
        }

        [HarmonyPatch(typeof(BraveInput), "AssignActionsDevice")]
        private static class AssignDevice
        {
            private static bool Prefix(BraveInput __instance)
            {
                DungeonSession session = Session;
                if (session != null && session.IsHost && session.Controls != null && RemoteControls.IsPlayerTwo(__instance)) { session.Controls.Bind(); return false; }
                return true;
            }
        }
        [HarmonyPatch(typeof(BraveInput), "IsKeyboardAndMouse")]
        private static class InputStyle
        {
            private static bool Prefix(BraveInput __instance, ref bool __result)
            {
                DungeonSession session = Session;
                if (session != null && session.IsHost && session.Controls != null && RemoteControls.IsPlayerTwo(__instance)) { __result = false; return false; }
                return true;
            }
        }
        [HarmonyPatch(typeof(InputManager), "GetActiveDeviceForPlayer")]
        private static class DevicePort
        {
            private static bool Prefix(int __0, ref InputDevice __result)
            {
                DungeonSession session = Session;
                if (session == null || !session.IsHost || session.Controls == null) return true;
                if (__0 == 1) { __result = session.Controls.Device; return false; }
                if (__0 == 0) {
                    foreach (InputDevice device in InputManager.Devices) if (device != session.Controls.Device) { __result = device; return false; }
                    __result = InputDevice.Null; return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Dungeonator.Dungeon), "GetDungeonSeed")]
        private static class Seed
        {
            private static bool Prefix(ref int __result)
            {
                DungeonSession session = Session;
                if (session == null || !session.InRun || session.Seed <= 0) return true;
                GameManager.Instance.InitializeForRunWithSeed(session.Seed);
                __result = session.Seed; return false;
            }
        }
        [HarmonyPatch(typeof(GameManager), "LoadNextLevelAsync_CR")]
        private static class Transition
        {
            private static void Prefix(GameLevelDefinition __0)
            { DungeonSession session = Session; if (session != null && __0 != null) session.HostLevelLoading(__0.dungeonSceneName); }
        }
        [HarmonyPatch(typeof(GameManager), "LoadCustomLevel")]
        private static class BeforeCustomTransition
        {
            private static void Prefix(string __0)
            { DungeonSession session = Session; if (session != null) session.HostLevelLoading(__0); }
        }
        [HarmonyPatch(typeof(GameManager), "LoadNextLevel")]
        private static class BeforeNormalTransition
        {
            private static void Prefix(GameManager __instance)
            {
                DungeonSession session = Session;
                if (session == null || !session.IsHost || !session.InRun) return;
                FieldInfo index = typeof(GameManager).GetField("nextLevelIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                int next = (int)index.GetValue(__instance);
                if (__instance.dungeonFloors != null && next >= 0 && next < __instance.dungeonFloors.Count)
                    session.HostLevelLoading(__instance.dungeonFloors[next].dungeonSceneName);
            }
        }
        [HarmonyPatch(typeof(SaveManager), "Init")]
        private static class SeparateProfile
        {
            private static void Prefix()
            {
                if (DungeonPlugin.Instance == null || !DungeonPlugin.Instance.UsesSeparateProfile) return;
                string path = Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx/GungeonTogetherLAN/dungeon-profile");
                Directory.CreateDirectory(path);
                SaveManager.SavePath = path; SaveManager.OldSavePath = path;
                SaveManager.PreventMidgameSaveDeletionOnExit = true;
            }
            private static void Postfix()
            {
                if (DungeonPlugin.Instance != null) DungeonPlugin.Instance.ProfileInitialized();
            }
        }
    }
}
