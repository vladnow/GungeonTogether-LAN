using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Dungeonator;
using HarmonyLib;
using UnityEngine;

namespace GungeonTogether.Lan
{
    internal sealed class PersonalOwnership : MonoBehaviour { internal int Owner; }
    internal sealed class PersonalShopStock : MonoBehaviour { internal int SoldMask; }

    internal static class PersonalLoot
    {
        private static float nextScan;
        private static int dropOwner = -1;
        private static ShopItemController purchasing;
        private static readonly List<Renderer> hidden = new List<Renderer>();
        private static readonly List<PersonalOwnership> owned = new List<PersonalOwnership>();
        private static readonly List<PersonalShopStock> stocks = new List<PersonalShopStock>();
        private static readonly HashSet<int> hiddenLabels = new HashSet<int>();
        private static bool hooked;
        internal static bool Active { get { return RunEconomy.Active && RunEconomy.Rules.PersonalLoot; } }
        internal static bool Allowed(Component thing, PlayerController player) { return thing == null || player == null || VisibleTo(thing, RunEconomy.Owner(player)); }
        internal static bool VisibleTo(Component thing, int owner)
        {
            if (!Active || thing == null) return true;
            PersonalOwnership claim = thing.GetComponentInParent<PersonalOwnership>();
            if (claim != null && claim.Owner != owner) return false;
            PersonalShopStock stock = thing.GetComponentInParent<PersonalShopStock>();
            return stock == null || (stock.SoldMask & (1 << owner)) == 0;
        }
        internal static void Claim(GameObject go, int owner)
        {
            if (go == null) return;
            PersonalOwnership claim = go.GetComponent<PersonalOwnership>();
            if (claim == null) claim = go.AddComponent<PersonalOwnership>();
            if (!owned.Contains(claim)) owned.Add(claim);
            claim.Owner = owner;
        }
        internal static void Tick()
        {
            if (!Active) return;
            if (!hooked) { Camera.onPreCull += BeforeRender; Camera.onPostRender += AfterRender; hooked = true; }
            if (Time.realtimeSinceStartup < nextScan) return; nextScan = Time.realtimeSinceStartup + 0.25f;
            Chest[] chests = StaticReferenceManager.AllChests.ToArray();
            foreach (Chest chest in chests) {
                if (chest == null || !chest.gameObject.activeInHierarchy) continue;
                PersonalOwnership claim = chest.GetComponent<PersonalOwnership>();
                if (claim != null) { if (chest.majorBreakable != null) chest.majorBreakable.TemporarilyInvulnerable = true; continue; }
                if (!Eligible(chest)) continue;
                RoomHandler room = (RoomHandler)AccessTools.Field(typeof(Chest), "m_room").GetValue(chest);
                if (room == null) continue;
                Claim(chest.gameObject, 0);
                Chest second = Chest.Spawn(chest, chest.transform.position, room, true);
                if (second == null) throw new InvalidOperationException("Personal chest copy failed.");
                // Instantiate copies public serialized contents; generate each reward independently on first open.
                second.contents = null; second.forceContentIds = new List<int>();
                Claim(second.gameObject, 1);
                if (chest.majorBreakable != null) chest.majorBreakable.TemporarilyInvulnerable = true;
                if (second.majorBreakable != null) second.majorBreakable.TemporarilyInvulnerable = true;
            }
        }
        private static bool Eligible(Chest chest)
        {
            return !chest.IsOpen && !chest.IsBroken && !chest.IsMimic && !chest.IsGlitched && !chest.ForceGlitchChest
                && !chest.IsRainbowChest && !chest.IsMirrorChest && !chest.IsTruthChest && chest.ChestIdentifier == Chest.SpecialChestIdentifier.NORMAL
                && (chest.forceContentIds == null || chest.forceContentIds.Count == 0);
        }
        private static bool Eligible(ShopItemController item)
        {
            return Active && item != null && item.item != null && !item.IsResourcefulRatKey
                && item.CurrencyType != ShopItemController.ShopCurrencyType.META_CURRENCY
                && !item.item.PersistsOnPurchase;
        }
        internal static void Reset()
        {
            Restore(); if (hooked) { Camera.onPreCull -= BeforeRender; Camera.onPostRender -= AfterRender; hooked = false; }
            owned.Clear(); stocks.Clear(); hiddenLabels.Clear(); purchasing = null; dropOwner = -1; nextScan = 0;
        }
        internal static bool LabelVisible(DefaultLabelController label, int owner)
        { return VisibleTo(label.targetObject, owner) && (label.panel.IsVisible || (Active && hiddenLabels.Contains(label.GetInstanceID()))); }
        private static void Hide(Component component)
        {
            foreach (Renderer r in component.GetComponentsInChildren<Renderer>(true)) if (r != null && r.enabled) { r.enabled = false; hidden.Add(r); }
        }
        private static void BeforeRender(Camera camera)
        {
            if (!Active || GameManager.Instance.MainCameraController == null || camera != GameManager.Instance.MainCameraController.Camera) return;
            Restore();
            owned.RemoveAll(delegate(PersonalOwnership p) { return p == null; });
            stocks.RemoveAll(delegate(PersonalShopStock p) { return p == null; });
            foreach (PersonalOwnership p in owned) if (p.Owner != 0) Hide(p);
            foreach (PersonalShopStock p in stocks) if ((p.SoldMask & 1) != 0) Hide(p);
        }
        private static void AfterRender(Camera camera) { Restore(); }
        private static void Restore() { foreach (Renderer r in hidden) if (r != null) r.enabled = true; hidden.Clear(); }
        internal static float InteractDistance(IPlayerInteractable item, Vector2 position, PlayerController player)
        { return Allowed(item as Component, player) ? item.GetDistanceToPoint(position) : float.MaxValue; }
        internal static bool Persists(PickupObject item)
        { return item.PersistsOnPurchase || (Active && purchasing != null && purchasing.item == item); }
        private static IEnumerable<CodeInstruction> KeepStock(IEnumerable<CodeInstruction> original)
        {
            FieldInfo native = AccessTools.Field(typeof(PickupObject), "PersistsOnPurchase");
            foreach (CodeInstruction c in original) {
                if (c.opcode == OpCodes.Ldfld && Equals(c.operand, native)) { c.opcode = OpCodes.Call; c.operand = AccessTools.Method(typeof(PersonalLoot), "Persists"); }
                yield return c;
            }
        }
        [HarmonyPatch(typeof(RoomHandler), "GetNearestInteractable")]
        private static class Interactions
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> original)
            {
                MethodInfo native = AccessTools.Method(typeof(IPlayerInteractable), "GetDistanceToPoint");
                foreach (CodeInstruction c in original) {
                    if (c.Calls(native)) {
                        CodeInstruction arg = new CodeInstruction(OpCodes.Ldarg_3); arg.labels.AddRange(c.labels); arg.blocks.AddRange(c.blocks); yield return arg;
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PersonalLoot), "InteractDistance"));
                    } else yield return c;
                }
            }
        }
        [HarmonyPatch(typeof(Chest), "Interact")]
        private static class OpenChest {
            static bool Prefix(Chest __instance, PlayerController player) {
                if (Active && __instance.GetComponent<PersonalOwnership>() == null && Eligible(__instance)) { nextScan = 0; Tick(); }
                return Allowed(__instance, player);
            }
        }
        [HarmonyPatch(typeof(Chest), "DetermineContents")]
        private static class RollChest
        {
            static bool Prefix(Chest __instance, PlayerController player, int tierShift)
            {
                PersonalOwnership claim = Active ? __instance.GetComponent<PersonalOwnership>() : null;
                if (claim == null) return true;
                if (__instance.contents == null || __instance.contents.Count == 0) {
                    int seed = unchecked(RunEconomy.Session.Seed ^ __instance.GetInstanceID() * 397 ^ (claim.Owner + 1) * 7919);
                    __instance.contents = (List<PickupObject>)AccessTools.Method(typeof(Chest), "GenerateContents").Invoke(__instance, new object[] { player, tierShift, new System.Random(seed) });
                    if (__instance.contents == null || __instance.contents.Count == 0) throw new InvalidOperationException("Personal chest generated no items.");
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(Chest), "SpewContentsOntoGround")]
        private static class ChestDrops
        {
            static void Prefix(Chest __instance, out int __state) { __state = dropOwner; PersonalOwnership claim = Active ? __instance.GetComponent<PersonalOwnership>() : null; dropOwner = claim == null ? -1 : claim.Owner; }
            static Exception Finalizer(Exception __exception, int __state) { dropOwner = __state; return __exception; }
        }
        [HarmonyPatch(typeof(LootEngine), "SpawnItem")]
        private static class SpawnDrop
        { static void Postfix(DebrisObject __result) { if (Active && dropOwner >= 0 && __result != null) Claim(__result.gameObject, dropOwner); } }
        [HarmonyPatch(typeof(LootEngine), "SpewLoot", new[] { typeof(GameObject), typeof(Vector3) })]
        private static class SpewDrop
        { static void Postfix(DebrisObject __result) { if (Active && dropOwner >= 0 && __result != null) Claim(__result.gameObject, dropOwner); } }
        [HarmonyPatch(typeof(LootEngine), "SpewLoot", new[] { typeof(List<GameObject>), typeof(Vector3) })]
        private static class SpewDrops
        { static void Postfix(List<DebrisObject> __result) { if (Active && dropOwner >= 0 && __result != null) foreach (DebrisObject debris in __result) if (debris != null) Claim(debris.gameObject, dropOwner); } }
        [HarmonyPatch(typeof(DefaultLabelController), "LateUpdate")]
        private static class ShopLabels
        {
            static void Postfix(DefaultLabelController __instance)
            {
                if (!Active || __instance.targetObject == null || VisibleTo(__instance.targetObject, 0) || __instance.panel == null) return;
                if (__instance.panel.IsVisible) hiddenLabels.Add(__instance.GetInstanceID());
                __instance.panel.IsVisible = false;
            }
        }
        [HarmonyPatch]
        private static class PickupGuard
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (Type type in new[] { typeof(Gun), typeof(PassiveItem), typeof(PlayerItem), typeof(HealthPickup), typeof(AmmoPickup), typeof(KeyBulletPickup), typeof(CurrencyPickup) }) {
                    MethodInfo method = AccessTools.DeclaredMethod(type, "Pickup", new[] { typeof(PlayerController) }); if (method != null) yield return method;
                }
            }
            static bool Prefix(PickupObject __instance, PlayerController player, out bool __state)
            {
                __state = false;
                if (Allowed(__instance, player)) { __state = Active && __instance.GetComponentInParent<PersonalOwnership>() != null; return true; }
                FieldInfo picked = AccessTools.Field(__instance.GetType(), "m_hasBeenPickedUp"); if (picked != null) picked.SetValue(__instance, false);
                return false;
            }
            static void Postfix(PlayerController player, bool __state)
            {
                if (!__state || !Active || player == null) return;
                // Native Pickup may copy the object into inventory and destroy the
                // original. Release claims on the actual carried copies as well.
                if (player.inventory != null) foreach (Gun gun in player.inventory.AllGuns) Release(gun);
                foreach (PassiveItem item in player.passiveItems) Release(item);
                foreach (PlayerItem item in player.activeItems) Release(item);
            }
        }
        private static void Release(Component item)
        {
            if (item == null) return;
            PersonalOwnership claim = item.GetComponent<PersonalOwnership>();
            if (claim != null) { owned.Remove(claim); UnityEngine.Object.Destroy(claim); }
        }
        [HarmonyPatch]
        private static class CollisionGuard
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (Type type in new[] { typeof(CurrencyPickup), typeof(KeyBulletPickup), typeof(HealthPickup), typeof(AmmoPickup) }) {
                    MethodInfo method = AccessTools.DeclaredMethod(type, "OnPreCollision"); if (method != null) yield return method;
                }
            }
            static bool Prefix(Component __instance, object[] __args)
            {
                foreach (object arg in __args) { SpeculativeRigidbody body = arg as SpeculativeRigidbody; if (body == null) continue; PlayerController p = body.GetComponent<PlayerController>(); if (p != null && !Allowed(__instance, p)) { PhysicsEngine.SkipCollision = true; return false; } }
                return true;
            }
        }
        [HarmonyPatch(typeof(ShopItemController), "Interact")]
        private static class Buy
        {
            internal static bool Prefix(ShopItemController __instance, PlayerController player, out ShopItemController __state)
            {
                __state = purchasing;
                if (!Allowed(__instance, player)) return false;
                if (Eligible(__instance)) {
                    PersonalShopStock stock = __instance.GetComponent<PersonalShopStock>();
                    if (stock == null) { stock = __instance.gameObject.AddComponent<PersonalShopStock>(); stocks.Add(stock); }
                    purchasing = __instance;
                }
                return true;
            }
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> original) { return KeepStock(original); }
            internal static Exception Finalizer(Exception __exception, ShopItemController __state) { purchasing = __state; return __exception; }
        }
        [HarmonyPatch(typeof(ShopItemController), "ForceSteal")]
        private static class Steal
        {
            static bool Prefix(ShopItemController __instance, PlayerController player, out ShopItemController __state) { return Buy.Prefix(__instance, player, out __state); }
            static void Postfix(ShopItemController __instance) { if (Active && __instance != null && __instance.GetComponent<PersonalShopStock>() != null) AccessTools.Field(typeof(ShopItemController), "pickedUp").SetValue(__instance, false); }
            static Exception Finalizer(Exception __exception, ShopItemController __state) { return Buy.Finalizer(__exception, __state); }
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> original)
            {
                MethodInfo remove = AccessTools.Method(typeof(GameUIRoot), "DeregisterDefaultLabel");
                foreach (CodeInstruction c in original) { if (c.Calls(remove)) { c.opcode = OpCodes.Call; c.operand = AccessTools.Method(typeof(PersonalLoot), "RemoveLabel"); } yield return c; }
            }
        }
        internal static void RemoveLabel(GameUIRoot root, Transform target) { if (!Active || purchasing == null) root.DeregisterDefaultLabel(target); }
        [HarmonyPatch(typeof(BaseShopController), "PurchaseItem")]
        private static class BaseBuy { static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> original) { return KeepStock(original); } }
        [HarmonyPatch(typeof(ShopController), "PurchaseItem")]
        private static class OldBuy { static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> original) { return KeepStock(original); } }
        [HarmonyPatch]
        private static class Bought
        {
            static IEnumerable<MethodBase> TargetMethods() { yield return AccessTools.Method(typeof(PlayerController), "HandleItemPurchased"); yield return AccessTools.Method(typeof(PlayerController), "HandleItemStolen"); }
            static void Postfix(PlayerController __instance, ShopItemController __0)
            {
                if (!Active || purchasing == null || __0 != purchasing) return;
                PersonalShopStock stock = purchasing.GetComponent<PersonalShopStock>(); if (stock == null) return;
                int owner = RunEconomy.Owner(__instance); stock.SoldMask |= 1 << owner;
                GameObject sign = UnityEngine.Object.Instantiate(BraveResources.Load("Global Prefabs/Sign_SoldOut")) as GameObject;
                if (sign != null) {
                    tk2dBaseSprite sprite = sign.GetComponent<tk2dBaseSprite>(); sprite.PlaceAtPositionByAnchor(purchasing.sprite.WorldCenter, tk2dBaseSprite.Anchor.MiddleCenter);
                    sprite.HeightOffGround = purchasing.sprite.HeightOffGround; sprite.UpdateZDepth(); Claim(sign, owner);
                }
                // Keep native pricing, stealing, callbacks and both stocks. The per-owner filter
                // hides the sold item; it must not destroy the other player's opportunity.
            }
        }
    }
}
