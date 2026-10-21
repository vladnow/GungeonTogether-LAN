using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Dungeonator;
using HarmonyLib;
using UnityEngine;

namespace GungeonTogether.Lan
{
    // Runs only on the authority. Detached client HUD wallets never enter this map.
    internal static class RunEconomy
    {
        private static PlayerConsumables hostWallet, guestWallet;
        private static readonly Dictionary<int, int> damageOwners = new Dictionary<int, int>();
        private static AIActor rewardSource;
        private static bool bossReward, oddGuest;
        internal static DungeonSession Session { get { return DungeonPlugin.Instance == null ? null : DungeonPlugin.Instance.Session; } }
        internal static bool Active { get { return Session != null && Session.IsHost && Session.InRun; } }
        internal static RunRules Rules { get { return Active ? Session.Rules : null; } }

        internal static void EnsureWallets()
        {
            if (!GameManager.HasInstance) return;
            PlayerController host = GameManager.Instance.PrimaryPlayer, guest = GameManager.Instance.SecondaryPlayer;
            if (host == null || guest == null || host.carriedConsumables == null) return;
            RunRules rules = Rules;
            if (rules == null || !rules.AnyWallet) { guest.carriedConsumables = host.carriedConsumables; return; }
            hostWallet = host.carriedConsumables;
            if (guestWallet == null) {
                guestWallet = new PlayerConsumables();
                // Native couch co-op shares the primary wallet during player creation.
                // Give the guest the native starting allowance, never half the host's savings.
                AccessTools.Field(typeof(PlayerConsumables), "m_keyBullets").SetValue(guestWallet, 1);
            }
            guest.carriedConsumables = guestWallet;
        }
        internal static void Reset()
        { hostWallet = guestWallet = null; damageOwners.Clear(); rewardSource = null; bossReward = oddGuest = false; }
        internal static void NewFloor() { damageOwners.Clear(); }
        internal static void Shared(ref PlayerConsumables wallet, bool separate)
        { if (Active && !separate && guestWallet != null && ReferenceEquals(wallet, guestWallet) && hostWallet != null) wallet = hostWallet; }
        internal static int Owner(PlayerController player) { return player != null && player.IsPrimaryPlayer ? 0 : 1; }
        internal static PlayerController Player(int owner) { return owner == 0 ? GameManager.Instance.PrimaryPlayer : GameManager.Instance.SecondaryPlayer; }
        internal static PlayerController CoinReceiver(PlayerController collector, CurrencyPickup coin)
        {
            if (!Active || !Rules.SeparateMoney || coin == null || coin.IsMetaCurrency) return GameManager.Instance.PrimaryPlayer;
            CoinCredit credit = coin.GetComponent<CoinCredit>();
            if (credit != null && credit.Boss) {
                int[] shares = RunRules.SplitBossMoney(coin.currencyValue, oddGuest); oddGuest = !oddGuest;
                PlayerController guest = Player(1);
                if (guest != null) guest.carriedConsumables.Currency += shares[1];
                else shares[0] += shares[1];
                coin.currencyValue = shares[0]; return Player(0);
            }
            int owner;
            if (credit != null && damageOwners.TryGetValue(credit.Source, out owner) && Player(owner) != null) return Player(owner);
            return collector ?? Player(0);
        }

        [HarmonyPatch(typeof(PlayerConsumables), "Currency", MethodType.Getter)]
        private static class CashGet { static void Prefix(ref PlayerConsumables __instance) { Shared(ref __instance, Rules != null && Rules.SeparateMoney); } }
        [HarmonyPatch(typeof(PlayerConsumables), "Currency", MethodType.Setter)]
        private static class CashSet { static void Prefix(ref PlayerConsumables __instance) { Shared(ref __instance, Rules != null && Rules.SeparateMoney); } }
        [HarmonyPatch(typeof(PlayerConsumables), "KeyBullets", MethodType.Getter)]
        private static class KeysGet { static void Prefix(ref PlayerConsumables __instance) { Shared(ref __instance, Rules != null && Rules.SeparateKeys); } }
        [HarmonyPatch(typeof(PlayerConsumables), "KeyBullets", MethodType.Setter)]
        private static class KeysSet { static void Prefix(ref PlayerConsumables __instance) { Shared(ref __instance, Rules != null && Rules.SeparateKeys); } }
        [HarmonyPatch(typeof(PlayerConsumables), "ResourcefulRatKeys", MethodType.Getter)]
        private static class RatGet { static void Prefix(ref PlayerConsumables __instance) { Shared(ref __instance, Rules != null && Rules.SeparateKeys); } }
        [HarmonyPatch(typeof(PlayerConsumables), "ResourcefulRatKeys", MethodType.Setter)]
        private static class RatSet { static void Prefix(ref PlayerConsumables __instance) { Shared(ref __instance, Rules != null && Rules.SeparateKeys); } }
        [HarmonyPatch(typeof(PlayerConsumables), "InfiniteKeys", MethodType.Getter)]
        private static class InfiniteGet
        {
            static bool Prefix(PlayerConsumables __instance, ref bool __result)
            {
                if (!Active || !Rules.SeparateKeys || (!ReferenceEquals(__instance, hostWallet) && !ReferenceEquals(__instance, guestWallet))) return true;
                PlayerController p = Player(ReferenceEquals(__instance, hostWallet) ? 0 : 1);
                __result = (bool)AccessTools.Field(typeof(PlayerConsumables), "m_infiniteKeys").GetValue(__instance)
                    || (p != null && p.CurrentGun != null && p.CurrentGun.gunName == "AKey-47"); return false;
            }
        }
        [HarmonyPatch(typeof(PlayerController), "OnDidDamage")]
        private static class DamageCredit
        {
            static void Prefix(PlayerController __instance, HealthHaver target)
            { if (Active && Rules.SeparateMoney && target != null) damageOwners[target.GetInstanceID()] = Owner(__instance); }
        }
        [HarmonyPatch(typeof(AIActor), "HandleLootPinata")]
        private static class EnemyReward
        {
            static void Prefix(AIActor __instance, out AIActor __state) { __state = rewardSource; rewardSource = __instance; }
            static Exception Finalizer(Exception __exception, AIActor __state) { rewardSource = __state; return __exception; }
        }
        [HarmonyPatch(typeof(RoomHandler), "HandleBossClearReward")]
        private static class BossReward
        {
            static void Prefix(out bool __state) { __state = bossReward; bossReward = true; }
            static Exception Finalizer(Exception __exception, bool __state) { bossReward = __state; return __exception; }
        }
        [HarmonyPatch(typeof(SpawnManager), "SpawnDebris", new[] { typeof(GameObject), typeof(Vector3), typeof(Quaternion) })]
        private static class SpawnCoin
        {
            static void Postfix(GameObject __result)
            {
                if (!Active || !Rules.SeparateMoney || __result == null || (rewardSource == null && !bossReward)) return;
                CurrencyPickup coin = __result.GetComponent<CurrencyPickup>();
                if (coin == null || coin.IsMetaCurrency) return;
                CoinCredit credit = __result.GetComponent<CoinCredit>() ?? __result.AddComponent<CoinCredit>();
                credit.Source = rewardSource != null && rewardSource.healthHaver != null ? rewardSource.healthHaver.GetInstanceID() : 0;
                credit.Boss = bossReward || (rewardSource != null && rewardSource.healthHaver != null && rewardSource.healthHaver.IsBoss);
            }
        }
        [HarmonyPatch(typeof(CurrencyPickup), "Pickup")]
        private static class CreditPickup
        {
            static void Prefix(CurrencyPickup __instance, out int __state) { __state = __instance.currencyValue; }
            static Exception Finalizer(Exception __exception, CurrencyPickup __instance, int __state) { if (__instance != null) __instance.currencyValue = __state; return __exception; }
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> original)
            {
                MethodInfo primary = AccessTools.PropertyGetter(typeof(GameManager), "PrimaryPlayer");
                foreach (CodeInstruction c in original) {
                    if (c.Calls(primary)) {
                        // Replace gm.get_PrimaryPlayer() while retaining the original caller's stack.
                        CodeInstruction pop = new CodeInstruction(OpCodes.Pop); pop.labels.AddRange(c.labels); pop.blocks.AddRange(c.blocks); yield return pop;
                        yield return new CodeInstruction(OpCodes.Ldarg_1); yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RunEconomy), "CoinReceiver"));
                    } else yield return c;
                }
            }
        }
    }
    internal sealed class CoinCredit : MonoBehaviour { internal int Source; internal bool Boss; }
}
