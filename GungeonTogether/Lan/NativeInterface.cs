using System;
using System.Collections.Generic;
using System.Reflection;
using Dungeonator;
using HarmonyLib;
using UnityEngine;

namespace GungeonTogether.Lan
{
    // Mirror the game's widgets and map data, without running client interactions.
    internal sealed class NativeInterface : IDisposable
    {
        private readonly Dictionary<int, Transform> labels = new Dictionary<int, Transform>();
        private readonly Dictionary<int, string> texts = new Dictionary<int, string>();
        private readonly HashSet<RoomHandler> revealed = new HashSet<RoomHandler>();
        private static readonly FieldInfo NativeLabels = AccessTools.Field(typeof(GameUIRoot), "extantBasicLabels");
        private static readonly FieldInfo BossHealth = AccessTools.Field(typeof(GameUIBossHealthController), "m_cachedPercentHealths");
        private bool mapHeld;
        private static DungeonDoorController[] hostDoors;
        private static readonly HashSet<long> openDoors = new HashSet<long>();
        private static long DoorKey(DungeonDoorController door) { return ((long)Mathf.RoundToInt(door.transform.position.x * 16) << 32) | (uint)Mathf.RoundToInt(door.transform.position.y * 16); }
        internal static bool OpenDoorCollider(SpeculativeRigidbody body) { DungeonDoorController door = body.GetComponentInParent<DungeonDoorController>(); return door != null && openDoors.Contains(DoorKey(door)); }
        internal static bool MapOpen { get { return NativeHud.Mirroring && Minimap.HasInstance && Minimap.Instance.IsFullscreen; } }
        private static GameUIBossHealthController[] Bosses(GameUIRoot root) { return new[] { root.bossController, root.bossController2, root.bossControllerSide }; }

        internal static void Capture(EffectsState state, PlayerController guest)
        {
            GameUIRoot root = GameUIRoot.Instance;
            if (root == null) return;
            foreach (DefaultLabelController label in (List<DefaultLabelController>)NativeLabels.GetValue(root)) {
                if (label == null || label.targetObject == null || !label.panel.IsVisible || label.label == null || (label.targetObject.position.XY() - guest.CenterPosition).sqrMagnitude > 576) continue;
                Vector3 position = label.targetObject.position + label.offset;
                string text = label.label.Text ?? ""; if (text.Length > 2048) text = text.Substring(0, 2048);
                state.Labels.Add(new WorldLabel { Id = label.GetInstanceID(), X = position.x, Y = position.y, Z = position.z, Text = text });
                if (state.Labels.Count == 64) break;
            }
            GameUIBossHealthController[] bosses = Bosses(root);
            for (int i = 0; i < bosses.Length; i++) {
                GameUIBossHealthController boss = bosses[i]; if (boss == null || !boss.IsActive) continue;
                float health = 0; foreach (float value in (List<float>)BossHealth.GetValue(boss)) if (!float.IsNaN(value) && !float.IsInfinity(value)) health += value;
                state.Bosses.Add(new BossBarState { Slot = (byte)i, Health = Mathf.Clamp01(health), Name = WorldSnapshot.CleanHudName(boss.bossNameLabel.Text) });
            }
            if (hostDoors == null) hostDoors = UnityEngine.Object.FindObjectsOfType<DungeonDoorController>();
            HashSet<long> seenDoors = new HashSet<long>();
            foreach (DungeonDoorController door in hostDoors) if (door != null && seenDoors.Add(DoorKey(door))) state.Doors.Add(new DoorState { X = Mathf.RoundToInt(door.transform.position.x * 16), Y = Mathf.RoundToInt(door.transform.position.y * 16), Open = door.IsOpen && !door.IsSealed && !door.isLocked });
            foreach (RoomHandler room in GameManager.Instance.Dungeon.data.rooms) {
                if (room.visibility == RoomHandler.VisibilityStatus.OBSCURED || room.visibility == RoomHandler.VisibilityStatus.REOBSCURED) continue;
                state.MapRooms.Add(new MapRoomState { X = room.area.basePosition.x, Y = room.area.basePosition.y, Visibility = (byte)room.visibility });
            }
        }
        internal void Apply(EffectsState state)
        {
            GameUIRoot root = GameUIRoot.Instance; if (root == null) return;
            HashSet<int> present = new HashSet<int>();
            foreach (WorldLabel label in state.Labels) {
                present.Add(label.Id); Transform anchor;
                if (!labels.TryGetValue(label.Id, out anchor)) { GameObject go = new GameObject("GT_WorldLabel"); go.AddComponent<NetworkPresentationAsset>(); anchor = go.transform; labels.Add(label.Id, anchor); }
                anchor.position = new Vector3(label.X, label.Y, label.Z);
                string text;
                if (!texts.TryGetValue(label.Id, out text) || text != label.Text) {
                    root.DeregisterDefaultLabel(anchor);
                    DefaultLabelController native = root.RegisterDefaultLabel(anchor, Vector3.zero, label.Text).GetComponent<DefaultLabelController>();
                    native.label.ProcessMarkup = true; native.label.ColorizeSymbols = false; texts[label.Id] = label.Text;
                }
            }
            List<int> removed = new List<int>(); foreach (int id in labels.Keys) if (!present.Contains(id)) removed.Add(id);
            foreach (int id in removed) { root.DeregisterDefaultLabel(labels[id]); UnityEngine.Object.Destroy(labels[id].gameObject); labels.Remove(id); texts.Remove(id); }
            GameUIBossHealthController[] bosses = Bosses(root);
            for (int i = 0; i < bosses.Length; i++) {
                if (bosses[i] == null) continue;
                BossBarState value = state.Bosses.Find(delegate(BossBarState b) { return b.Slot == i; });
                if (value != null && value.Health > 0) bosses[i].ForceUpdateBossHealth(value.Health, 1, value.Name);
                else if (bosses[i].IsActive) bosses[i].DisableBossHealth();
            }
            openDoors.Clear(); foreach (DoorState door in state.Doors) if (door.Open) openDoors.Add(((long)door.X << 32) | (uint)door.Y);
            if (Minimap.HasInstance) foreach (MapRoomState value in state.MapRooms) {
                foreach (RoomHandler room in GameManager.Instance.Dungeon.data.rooms) {
                    if (room.area.basePosition.x != value.X || room.area.basePosition.y != value.Y) continue;
                    if (revealed.Add(room)) Minimap.Instance.RevealMinimapRoom(room, true, true, false);
                    break;
                }
            }
        }
        internal void Update()
        {
            if (!NativeHud.Mirroring || !Minimap.HasInstance) return;
            Minimap map = Minimap.Instance;
            foreach (PlayerController p in GameManager.Instance.AllPlayers) if (p != null) map.UpdatePlayerPositionExact(p.CenterPosition, p);
            BraveInput input = BraveInput.GetInstanceForPlayer(0);
            if (input == null || input.ActiveActions == null || !Application.isFocused) return;
            InControl.PlayerAction action = input.ActiveActions.GetActionFromType((GungeonActions.GungeonActionType)19);
            bool pressed = action != null && action.IsPressed && !InventoryBook.BookOpen && !GameUIRoot.Instance.DisplayingConversationBar;
            if (pressed && !mapHeld) map.ToggleMinimap(true, true);
            else if (!pressed && mapHeld) map.ToggleMinimap(false);
            mapHeld = pressed;
        }
        public void Dispose()
        {
            if (Minimap.HasInstance && Minimap.Instance.IsFullscreen) Minimap.Instance.ToggleMinimap(false);
            foreach (Transform anchor in labels.Values) if (anchor != null) { if (GameUIRoot.Instance != null) GameUIRoot.Instance.DeregisterDefaultLabel(anchor); UnityEngine.Object.Destroy(anchor.gameObject); }
            labels.Clear(); texts.Clear(); revealed.Clear(); mapHeld = false; hostDoors = null; openDoors.Clear();
        }
        [HarmonyPatch(typeof(GameUIBossHealthController), "LateUpdate")]
        private static class NativeBossTick { private static bool Prefix() { return !NativeHud.Mirroring; } }
        [HarmonyPatch(typeof(GameUIRoot), "UnfoldGunventory")]
        private static class LocalMapInventory { private static bool Prefix() { return !NativeHud.Mirroring; } }
    }
}
