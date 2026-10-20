using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace GungeonTogether.Lan
{
    // Original widgets, with detached inactive equipment data. No Pickup, damage,
    // item effects or changes to the non-authoritative player's inventory.
    public sealed class NativeHud : IDisposable
    {
        [ThreadStatic] private static WorldSnapshot reading;
        [ThreadStatic] private static bool drawing;
        private GameUIRoot root;
        private Gun gun;
        private PlayerItem item;
        private GunInventory inventory;
        private ProjectileVolleyData ownedVolley;
        private readonly List<PlayerItem> items = new List<PlayerItem>();
        private readonly PlayerConsumables consumables = new PlayerConsumables();
        public int DrawnFrames { get; private set; }
        public int GunId { get; private set; } = -1;
        public int ItemId { get; private set; } = -1;
        internal static bool Mirroring { get { DungeonSession s = DungeonPlugin.Instance == null ? null : DungeonPlugin.Instance.Session; return s != null && s.Mirroring; } }
        private static T Copy<T>(int id) where T : PickupObject
        {
            PickupObject prefab = id < 0 ? null : PickupObjectDatabase.GetById(id);
            if (!(prefab is T)) return null;
            GameObject go = UnityEngine.Object.Instantiate(prefab.gameObject);
            go.name = "GT_HudData_" + id; go.SetActive(false); go.AddComponent<NetworkHudAsset>();
            foreach (MonoBehaviour behaviour in go.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            return go.GetComponent<T>();
        }
        public void Update(WorldSnapshot state)
        {
            if (state == null || !Mirroring || GameManager.Instance.IsLoadingLevel) return;
            PlayerController player = GameManager.Instance.PrimaryPlayer;
            root = GameUIRoot.Instance;
            if (root == null || player == null) return;
            if (GunId != state.GunId || (state.GunId >= 0 && gun == null)) {
                if (gun != null) { AccessTools.Field(typeof(Gun), "m_owner").SetValue(gun, null); UnityEngine.Object.Destroy(gun.gameObject); }
                if (ownedVolley != null) UnityEngine.Object.Destroy(ownedVolley); ownedVolley = null;
                GunId = state.GunId; gun = Copy<Gun>(GunId); inventory = new GunInventory(player);
                if (gun != null) {
                    AccessTools.Field(typeof(Gun), "m_owner").SetValue(gun, player);
                    if (gun.Volley == null) { ownedVolley = ScriptableObject.CreateInstance<ProjectileVolleyData>(); ownedVolley.projectiles = new List<ProjectileModule>(); ownedVolley.projectiles.Add(gun.singleModule); AccessTools.Field(typeof(Gun), "rawVolley").SetValue(gun, ownedVolley); }
                    if (gun.RuntimeModuleData == null) AccessTools.Field(typeof(Gun), "m_moduleData").SetValue(gun, new Dictionary<ProjectileModule, ModuleShootData>());
                    inventory.AllGuns.Add(gun); AccessTools.Field(typeof(GunInventory), "m_currentGun").SetValue(inventory, gun);
                }
            }
            if (ItemId != state.ItemId || (state.ItemId >= 0 && item == null)) {
                if (item != null) { item.LastOwner = null; UnityEngine.Object.Destroy(item.gameObject); }
                ItemId = state.ItemId; item = Copy<PlayerItem>(ItemId);
                items.Clear(); if (item != null) { items.Add(item); item.LastOwner = player; }
            }
            reading = state; drawing = true;
            try {
                if (gun != null) {
                    gun.ammo = state.Ammo;
                    foreach (ProjectileModule module in gun.Volley.projectiles) {
                        ModuleShootData data;
                        if (!gun.RuntimeModuleData.TryGetValue(module, out data)) { data = new ModuleShootData(); gun.RuntimeModuleData.Add(module, data); }
                        data.numberShotsFired = Math.Max(0, state.ClipCapacity - state.ClipAmmo);
                    }
                }
                root.ShowCoreUI("GT_NetworkHUD"); root.ToggleAllDefaultLabels(true, "GT_NetworkHUD"); root.ToggleLowerPanels(true, false, "GT_NetworkHUD");
                if (InventoryBook.BookOpen || state.GameOver) { root.HideCoreUI("GT_NetworkBook"); root.ToggleLowerPanels(false, false, "GT_NetworkBook"); }
                else { root.ShowCoreUI("GT_NetworkBook"); root.ToggleLowerPanels(true, false, "GT_NetworkBook"); }
                root.heartControllers[0].UpdateHealth(GameManager.Instance.SecondaryPlayer.healthHaver); root.blankControllers[0].UpdateBlanks(state.Blanks);
                consumables.Currency = state.Currency; consumables.KeyBullets = state.Keys; consumables.InfiniteKeys = state.InfiniteKeys;
                root.UpdatePlayerConsumables(consumables);
                if (inventory != null) { root.ammoControllers[0].UpdateUIGun(inventory, 0); FitAmmo(root.ammoControllers[0]); }
                root.itemControllers[0].UpdateItem(item, items);
                EffectsState effects = DungeonPlugin.Instance.Session.SceneEffects.LastState;
                if (effects != null) {
                    float fire = player.CurrentFireMeterValue, poison = player.CurrentPoisonMeterValue, curse = player.CurrentCurseMeterValue, drain = player.CurrentDrainMeterValue;
                    FieldInfo burning = AccessTools.Field(typeof(PlayerController), "m_isOnFire"); bool wasBurning = player.IsOnFire;
                    try {
                        player.CurrentFireMeterValue = effects.Fire; player.CurrentPoisonMeterValue = effects.Poison; player.CurrentCurseMeterValue = effects.Curse; player.CurrentDrainMeterValue = effects.Drain; burning.SetValue(player, effects.Burning);
                        root.p_playerReloadBar.UpdateStatusBars(player);
                    } finally { player.CurrentFireMeterValue = fire; player.CurrentPoisonMeterValue = poison; player.CurrentCurseMeterValue = curse; player.CurrentDrainMeterValue = drain; burning.SetValue(player, wasBurning); }
                }
                for (int i = 1; i < root.heartControllers.Count; i++) {
                    root.heartControllers[i].Panel.IsVisible = false;
                    foreach (dfSprite heart in root.heartControllers[i].extantHearts) if (heart != null) heart.IsVisible = false;
                    foreach (dfSprite armor in root.heartControllers[i].extantArmors) if (armor != null) armor.IsVisible = false;
                }
                for (int i = 1; i < root.blankControllers.Count; i++) {
                    root.blankControllers[i].Panel.IsVisible = false;
                    foreach (dfSprite blank in root.blankControllers[i].extantBlanks) if (blank != null) blank.IsVisible = false;
                    foreach (dfControl control in root.blankControllers[i].GetComponentsInChildren<dfControl>()) control.IsVisible = false;
                }
                for (int i = 1; i < root.ammoControllers.Count; i++) { root.ammoControllers[i].ToggleRenderers(false); root.ammoControllers[i].GetComponent<dfPanel>().IsVisible = false; }
                for (int i = 1; i < root.itemControllers.Count; i++) { root.itemControllers[i].ToggleRenderers(false); root.itemControllers[i].GetComponent<dfPanel>().IsVisible = false; }
                DrawnFrames++;
            } finally { reading = null; drawing = false; }
        }
        public int ViewportCorrections { get; private set; }
        private void FitAmmo(GameUIAmmoController controller)
        {
            Camera camera = controller.GunBoxSprite.GetManager().RenderCamera; if (camera == null) return;
            float right = 0;
            foreach (dfControl control in controller.GetComponentsInChildren<dfControl>()) {
                if (control is dfPanel || !control.IsVisible || control.Opacity < .01f) continue;
                Bounds bounds = control.GetBounds(); right = Mathf.Max(right, camera.WorldToViewportPoint(bounds.max).x);
            }
            float limit = 1f - 6f / Math.Max(1, Screen.width);
            if (right > limit + .001f) {
                float world = (right - limit) * camera.orthographicSize * 2 * camera.aspect;
                controller.transform.position -= Vector3.right * world; ViewportCorrections++;
            }
        }
        public void Dispose()
        {
            if (gun != null) { AccessTools.Field(typeof(Gun), "m_owner").SetValue(gun, null); UnityEngine.Object.Destroy(gun.gameObject); }
            if (item != null) { item.LastOwner = null; UnityEngine.Object.Destroy(item.gameObject); }
            if (ownedVolley != null) UnityEngine.Object.Destroy(ownedVolley); ownedVolley = null;
            gun = null; item = null; inventory = null; items.Clear(); GunId = ItemId = -1;
            if (root != null) { root.ShowCoreUI("GT_NetworkBook"); root.ToggleLowerPanels(true, false, "GT_NetworkBook"); }
            root = null;
        }
        [HarmonyPatch]
        private static class NativeWriters
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (string name in new[] { "UpdatePlayerHealthUI", "UpdatePlayerBlankUI", "UpdatePlayerConsumables", "UpdateGunData", "UpdateItemData" })
                    foreach (MethodInfo method in typeof(GameUIRoot).GetMethods()) if (method.Name == name) yield return method;
            }
            private static bool Prefix() { return !Mirroring || drawing; }
        }
        [HarmonyPatch(typeof(GameUIRoot), "ConvertCoreUIToCoopMode")]
        private static class OriginalLayout
        {
            private static bool Prefix() { DungeonSession s = DungeonPlugin.Instance == null ? null : DungeonPlugin.Instance.Session; return s == null || s.IsHost || !s.InRun; }
        }
        [HarmonyPatch(typeof(GameUIReloadBarController), "UpdateStatusBars")]
        private static class StatusWriter { private static bool Prefix() { return !Mirroring || drawing; } }
        private static bool CanUse(PlayerItem item, PlayerController player) { return reading == null ? item.CanBeUsed(player) : reading.ActiveItemReady; }
        [HarmonyPatch(typeof(GameUIItemController), "UpdateItemSprite")]
        private static class ItemAvailability
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo original = AccessTools.Method(typeof(PlayerItem), "CanBeUsed", new[] { typeof(PlayerController) });
                foreach (CodeInstruction instruction in instructions) {
                    if (instruction.Calls(original)) { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(NativeHud), "CanUse"); }
                    yield return instruction;
                }
            }
        }
        [HarmonyPatch(typeof(HealthHaver), "GetCurrentHealth")]
        private static class Health { private static bool Prefix(ref float __result) { if (reading == null) return true; __result = reading.Health; return false; } }
        [HarmonyPatch(typeof(HealthHaver), "GetMaxHealth")]
        private static class Maximum { private static bool Prefix(ref float __result) { if (reading == null) return true; __result = reading.MaxHealth; return false; } }
        [HarmonyPatch(typeof(HealthHaver), "get_Armor")]
        private static class Armor { private static bool Prefix(ref float __result) { if (reading == null) return true; __result = reading.Armor; return false; } }
        [HarmonyPatch(typeof(Gun), "get_ClipCapacity")]
        private static class Capacity { private static bool Prefix(ref int __result) { if (reading == null) return true; __result = reading.ClipCapacity; return false; } }
        [HarmonyPatch(typeof(Gun), "get_ClipShotsRemaining")]
        private static class Clip { private static bool Prefix(ref int __result) { if (reading == null) return true; __result = reading.ClipAmmo; return false; } }
        [HarmonyPatch(typeof(Gun), "get_InfiniteAmmo")]
        private static class Infinite { private static bool Prefix(ref bool __result) { if (reading == null) return true; __result = reading.InfiniteAmmo; return false; } }
        [HarmonyPatch(typeof(Gun), "get_AdjustedMaxAmmo")]
        private static class AmmoMaximum { private static bool Prefix(ref int __result) { if (reading == null) return true; __result = reading.InfiniteAmmo ? int.MaxValue : reading.MaxAmmo; return false; } }
        [HarmonyPatch(typeof(Gun), "get_IsReloading")]
        private static class Reload { private static bool Prefix(ref bool __result) { if (reading == null) return true; __result = reading.Reloading; return false; } }
        [HarmonyPatch(typeof(ProjectileModule), "GetModNumberOfShotsInClip")]
        private static class ModuleClip { private static bool Prefix(ref int __result) { if (reading == null) return true; __result = reading.ClipCapacity; return false; } }
        [HarmonyPatch(typeof(PlayerItem), "get_IsOnCooldown")]
        private static class Cooldown { private static bool Prefix(ref bool __result) { if (reading == null) return true; __result = reading.ActiveItemCooldown > 0; return false; } }
        [HarmonyPatch(typeof(PlayerItem), "get_CooldownPercentage")]
        private static class CooldownAmount { private static bool Prefix(ref float __result) { if (reading == null) return true; __result = reading.ActiveItemCooldown; return false; } }
        [HarmonyPatch(typeof(PlayerItem), "get_IsActive")]
        private static class Active { private static bool Prefix(ref bool __result) { if (reading == null) return true; __result = reading.ActiveItemActive; return false; } }
        [HarmonyPatch(typeof(PlayerItem), "get_ActivePercentage")]
        private static class ActiveAmount { private static bool Prefix(ref float __result) { if (reading == null) return true; __result = reading.ActiveItemCooldown; return false; } }
    }
    internal sealed class NetworkHudAsset : MonoBehaviour { }
}
