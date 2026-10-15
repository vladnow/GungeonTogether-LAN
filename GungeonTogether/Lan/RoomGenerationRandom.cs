using System;
using System.Reflection;
using Dungeonator;
using HarmonyLib;
using UnityEngine;

namespace GungeonTogether.Lan
{
    // DungeonData.Apply yields between rooms. RobotDave's corridor generation
    // uses the global Unity RNG, so actor ticks can otherwise change pit geometry
    // between peers despite identical floor seeds and prerequisite outcomes.
    [HarmonyPatch(typeof(RoomHandler), "WriteRoomData")]
    internal static class RoomGenerationRandom
    {
        private sealed class State
        {
            public UnityEngine.Random.State Unity;
            public System.Random Generation;
            public bool IgnoreDifferentiator;
        }
        private static readonly FieldInfo Generator = AccessTools.Field(typeof(BraveRandom), "m_generationRandom");

        private static void Prefix(RoomHandler __instance, out State __state)
        {
            __state = null;
            DungeonSession session = DungeonPlugin.Instance == null ? null : DungeonPlugin.Instance.Session;
            if (session == null || session.Phase != DungeonSession.RunPhase.Generating) return;
            if (Generator == null) throw new InvalidOperationException("Native generation RNG is missing.");
            __state = new State { Unity = UnityEngine.Random.state, Generation = (System.Random)Generator.GetValue(null), IgnoreDifferentiator = BraveRandom.IgnoreGenerationDifferentiator };
            int seed = session.Seed;
            unchecked {
                seed = seed * 397 ^ __instance.area.basePosition.x;
                seed = seed * 397 ^ __instance.area.basePosition.y;
                seed = seed * 397 ^ __instance.area.dimensions.x;
                seed = seed * 397 ^ __instance.area.dimensions.y;
            }
            UnityEngine.Random.InitState(seed);
            BraveRandom.IgnoreGenerationDifferentiator = true;
            BraveRandom.InitializeWithSeed(seed);
        }

        private static Exception Finalizer(Exception __exception, State __state)
        {
            if (__state != null) {
                UnityEngine.Random.state = __state.Unity;
                Generator.SetValue(null, __state.Generation);
                BraveRandom.IgnoreGenerationDifferentiator = __state.IgnoreDifferentiator;
            }
            return __exception;
        }
    }
}
