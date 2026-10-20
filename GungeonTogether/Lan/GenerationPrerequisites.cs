using Dungeonator;
using HarmonyLib;

namespace GungeonTogether.Lan
{
    // Later client floors recreate their placeholder players. Native injection
    // treats a missing PrimaryPlayer as passing RequiresMasteryToken, while the
    // authoritative host still has its real player. Preserve all other native
    // prerequisites and evaluate this one against the transmitted host counter.
    [HarmonyPatch(typeof(ProceduralFlowModifierData), "get_PrerequisitesMet")]
    internal static class GenerationPrerequisites
    {
        private static bool Prefix(ProceduralFlowModifierData __instance, ref bool __result)
        {
            DungeonSession session = DungeonPlugin.Instance == null ? null : DungeonPlugin.Instance.Session;
            if (session == null || session.IsHost || session.Phase != DungeonSession.RunPhase.Generating || !__instance.RequiresMasteryToken) return true;
            __result = false;
            foreach (DungeonPrerequisite prerequisite in __instance.prerequisites)
                if (!prerequisite.CheckConditionsFulfilled()) return false;
            __result = NativeWorld.GenerationPrimaryMasteryTokens > 0;
            return false;
        }
    }
}
