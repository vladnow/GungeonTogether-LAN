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

        [HarmonyPatch(typeof(GameManager), "LoadResolutionFromOptions")]
        private static class SmokeResolution
        {
            private static bool Prefix()
            {
                if (!Smoke) return true;
                Screen.SetResolution(800, 600, false); return false;
            }
        }

        [HarmonyPatch(typeof(GameManager), "DoSetResolution")]
        private static class SmokeResolutionChanges
        {
            private static void Prefix(ref int __0, ref int __1, ref bool __2)
            { if (Smoke) { __0 = 800; __1 = 600; __2 = false; } }
        }

        [HarmonyPatch(typeof(GameManager), "OnApplicationFocus")]
        private static class SmokeBackground
        {
            private static bool Prefix(GameManager __instance)
            {
                DungeonSession session = Session;
                if (!Smoke && (session == null || !session.InRun || session.Phase == DungeonSession.RunPhase.Fault)) return true;
                AccessTools.Field(typeof(GameManager), "m_applicationHasFocus").SetValue(__instance, true); return false;
            }
        }

        [HarmonyPatch]
        private static class SmokeCursor
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (string name in new string[] { "Start", "DrawCursor", "ToggleClip", "OnDestroy" })
                    yield return AccessTools.Method(typeof(GameCursorController), name);
            }
            private static bool Prefix()
            {
                if (!Smoke) return true;
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true; return false;
            }
        }

        // The native getter always returns Cultist, ignoring its backing field.
        // Scope the replacement to this session so floor loads keep the selection.
        [HarmonyPatch(typeof(GameManager), "get_CoopPlayerPrefabForNewGame")]
        private static class GuestPrefab
        {
            private static bool Prefix(ref GameObject __result)
            {
                DungeonSession session = Session;
                if (session == null || !session.InRun) return true;
                __result = NativeWorld.CharacterPrefab(session.GuestCharacter); return false;
            }
        }

        [HarmonyPatch(typeof(GameManager), "get_SecondaryPlayer")]
        private static class GuestController
        {
            private static bool Prefix(GameManager __instance, ref PlayerController __result)
            {
                DungeonSession session = Session;
                if (session == null || !session.InRun || __instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER) return true;
                // The native fallback searches specifically for Cultist. Identify
                // player two by controller index when caches reset on a new floor.
                foreach (PlayerController player in __instance.AllPlayers) {
                    if (player != null && player.PlayerIDX == 1) {
                        __instance.SecondaryPlayer = player; __result = player; return false;
                    }
                }
                __result = null; return false;
            }
        }

        [HarmonyPatch(typeof(GameManager), "FrameDelayedEnteredFoyer")]
        private static class DepartedFoyer
        {
            private static bool Prefix()
            {
                DungeonSession session = Session;
                // Players created while leaving the foyer can start one frame
                // after its objects have unloaded. Do not queue foyer callbacks
                // for controllers that are already entering the network dungeon.
                return session == null || !session.InRun || GameManager.Instance.IsFoyer;
            }
        }

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

        [HarmonyPatch(typeof(InputManager), "UpdateInternal")]
        private static class FreshNativeInput
        {
            private static void Prefix() { if (DungeonPlugin.Instance != null) DungeonPlugin.Instance.PollBeforeNativeInput(); }
            private static void Postfix()
            {
                DungeonSession s = Session;
                if (s != null) s.SendClientInput();
            }
        }
        [HarmonyPatch(typeof(BraveInput), "CheckForActionInitialization")]
        private static class InitializeRemoteActions
        {
            private static bool Prefix(BraveInput __instance)
            {
                DungeonSession session = Session;
                if (session == null || !session.IsHost || session.Controls == null || !RemoteControls.IsPlayerTwo(__instance)) return true;
                session.Controls.Bind(); return false;
            }
        }
        [HarmonyPatch]
        private static class PreserveRemoteBindings
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (string name in new[] { "InitializeDefaults", "ReinitializeDefaults", "ReinitializeMenuDefaults", "InitializeSwappedTriggersPreset", "PostProcessAdditionalBlankControls" })
                    yield return AccessTools.Method(typeof(GungeonActions), name);
            }
            private static bool Prefix(GungeonActions __instance)
            { DungeonSession s = Session; return s == null || s.Controls == null || __instance != s.Controls.Actions; }
        }
        [HarmonyPatch(typeof(BraveInput), "TryLoadBindings")]
        private static class PreserveRemoteSettings
        {
            private static bool Prefix(GungeonActions actions)
            { DungeonSession s = Session; return s == null || s.Controls == null || actions != s.Controls.Actions; }
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
            private static bool Prefix(GameLevelDefinition __0, ref System.Collections.IEnumerator __result)
            {
                DungeonSession session = Session;
                if (session != null && !session.CanClientLoadLevel) { __result = EmptyTransition(); return false; }
                if (session != null && __0 != null) session.HostLevelLoading(__0.dungeonSceneName);
                return true;
            }
        }
        private static System.Collections.IEnumerator EmptyTransition() { yield break; }
        [HarmonyPatch(typeof(GameManager), "LoadCustomLevel")]
        private static class BeforeCustomTransition
        {
            private static bool Prefix() { return Session == null || Session.CanClientLoadLevel; }
        }
        [HarmonyPatch(typeof(GameManager), "LoadNextLevel")]
        private static class BeforeNormalTransition
        {
            // The async observer above receives the resolved native definition,
            // including injected/secret levels, after session statistics merge.
            private static bool Prefix() { return Session == null || Session.CanClientLoadLevel; }
        }
        [HarmonyPatch(typeof(GameManager), "DelayedLoadNextLevel")]
        private static class BeforeDelayedTransition
        {
            private static bool Prefix()
            { return Session == null || Session.CanClientLoadLevel; }
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
