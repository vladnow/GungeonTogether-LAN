using System;
using System.Collections.Generic;
using System.Reflection;
using Dungeonator;
using HarmonyLib;
using UnityEngine;

namespace GungeonTogether.Lan
{
    internal sealed class WorldPresentation
    {
        private static readonly FieldInfo Open = AccessTools.Field(typeof(SecretRoomManager), "m_isOpen");
        private static readonly FieldInfo CeilingCells = AccessTools.Field(typeof(SecretRoomManager), "ceilingCells");
        private byte[] lastInventory, lastSecrets;
        private readonly HashSet<RoomHandler> revealed = new HashSet<RoomHandler>();
        private double next;
        internal int AppliedSecretRooms { get { return revealed.Count; } }

        internal void Reset() { lastInventory = lastSecrets = null; revealed.Clear(); next = 0; }
        internal void Capture(int epoch, int character, double now, Action<byte[]> send)
        {
            if (!NetworkTiming.Due(ref next, now, .25)) return;
            PlayerController player = GameManager.Instance.SecondaryPlayer;
            if (player == null) return;
            InventoryState inventory = new InventoryState { Epoch = epoch, Character = character };
            if (player.inventory != null) foreach (Gun gun in player.inventory.AllGuns) if (gun != null) inventory.Guns.Add(gun.PickupObjectId);
            foreach (PlayerItem item in player.activeItems) if (item != null) inventory.Active.Add(item.PickupObjectId);
            foreach (PassiveItem item in player.passiveItems) if (item != null) inventory.Passive.Add(item.PickupObjectId);
            inventory.Synergies.AddRange(player.ActiveExtraSynergies);
            byte[] bytes = inventory.Encode(); if (!StatePayload.Same(bytes, lastInventory)) { send(bytes); lastInventory = bytes; }
            SecretRoomsState secrets = new SecretRoomsState { Epoch = epoch };
            foreach (RoomHandler room in GameManager.Instance.Dungeon.data.rooms) {
                SecretRoomManager manager = room.secretRoomManager; if (manager == null) continue;
                bool ceiling = manager.ceilingRenderer != null && manager.ceilingRenderer.enabled;
                byte flags = (byte)((manager.IsOpen || !ceiling ? 1 : 0) | (ceiling ? 2 : 0) | (manager.borderRenderer != null && manager.borderRenderer.enabled ? 4 : 0) | (manager.aoRenderer != null && manager.aoRenderer.enabled ? 8 : 0));
                secrets.Rooms.Add(new SecretRoomState { X = room.area.basePosition.x, Y = room.area.basePosition.y, Flags = flags });
            }
            bytes = secrets.Encode(); if (!StatePayload.Same(bytes, lastSecrets)) { send(bytes); lastSecrets = bytes; }
        }

        internal void Apply(SecretRoomsState state)
        {
            DungeonData data = GameManager.Instance.Dungeon.data;
            foreach (SecretRoomState entry in state.Rooms) {
                if ((entry.Flags & 1) == 0) continue; // Never reveal a closed secret from local player movement.
                RoomHandler room = null;
                foreach (RoomHandler candidate in data.rooms) if (candidate.area.basePosition.x == entry.X && candidate.area.basePosition.y == entry.Y) { room = candidate; break; }
                if (room == null || room.secretRoomManager == null) throw new System.IO.IOException("Secret-room resources differ between peers.");
                SecretRoomManager manager = room.secretRoomManager;
                // Presentation only: do not call OpenDoor/OnEntered, grant discovery stats or spawn local gameplay.
                Open.SetValue(manager, true);
                if (manager.ceilingRenderer != null) manager.ceilingRenderer.enabled = (entry.Flags & 2) != 0;
                if (manager.borderRenderer != null) manager.borderRenderer.enabled = (entry.Flags & 4) != 0;
                if (manager.aoRenderer != null) manager.aoRenderer.enabled = (entry.Flags & 8) != 0;
                List<IntVector2> cells = (List<IntVector2>)CeilingCells.GetValue(manager);
                if (cells != null) foreach (IntVector2 point in cells) if (data.CheckInBoundsAndValid(point)) data[point].isSecretRoomCell = false;
                foreach (SecretRoomDoorBeer door in manager.doorObjects) {
                    foreach (IntVector2 point in door.exitDef.GetCellsForRoom(room)) if (data.CheckInBoundsAndValid(point)) data[point].isSecretRoomCell = false;
                    foreach (IntVector2 point in door.exitDef.GetCellsForOtherRoom(room)) if (data.CheckInBoundsAndValid(point)) data[point].isSecretRoomCell = false;
                    if (door.collider != null && door.collider.colliderObject != null) { SpeculativeRigidbody body = door.collider.colliderObject.GetComponent<SpeculativeRigidbody>(); if (body != null) body.enabled = false; }
                    door.gameObject.SetActive(false);
                }
                if (revealed.Add(room)) {
                    if (room.visibility == RoomHandler.VisibilityStatus.OBSCURED) room.visibility = RoomHandler.VisibilityStatus.VISITED;
                    if (Minimap.Instance != null) Minimap.Instance.RevealMinimapRoom(room, true, true, false);
                    if (Pixelator.Instance != null) {
                        IntVector2 center = room.area.basePosition + room.area.dimensions / 2;
                        Pixelator.Instance.ProcessOcclusionChange(center, .3f, room, false);
                        foreach (SecretRoomDoorBeer door in manager.doorObjects) Pixelator.Instance.ProcessRoomAdditionalExits(door.exitDef.GetUpstreamBasePosition(), door.linkedRoom, false);
                    }
                    ShadowSystem.ForceRoomLightsUpdate(room, .1f);
                }
            }
        }
        private static bool Mirroring { get { DungeonSession s = DungeonPlugin.Instance == null ? null : DungeonPlugin.Instance.Session; return s != null && s.Mirroring; } }
        [HarmonyPatch(typeof(SecretRoomManager), "OpenDoor")]
        private static class NoLocalSecretOpen { private static bool Prefix() { return !Mirroring; } }
        [HarmonyPatch(typeof(SecretRoomManager), "HandleDoorBrokenOpen")]
        private static class NoLocalSecretBreak { private static bool Prefix() { return !Mirroring; } }
    }
}
