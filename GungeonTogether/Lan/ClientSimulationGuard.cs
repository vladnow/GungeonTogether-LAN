using System;
using System.Reflection;
using Dungeonator;
using HarmonyLib;

namespace GungeonTogether.Lan
{
    // The client owns native map assets, while combat and room events run on the host.
    internal static class ClientSimulationGuard
    {
        private static readonly FieldInfo CurrentRoomField = AccessTools.Field(typeof(PlayerController), "m_currentRoom");
        private static readonly FieldInfo CurrentlyVisibleField = AccessTools.Field(typeof(RoomHandler), "m_currentlyVisible");

        private static bool Mirroring
        {
            get {
                DungeonSession session = DungeonPlugin.Instance == null ? null : DungeonPlugin.Instance.Session;
                return session != null && session.Mirroring;
            }
        }

        [HarmonyPatch(typeof(GenericIntroDoer), "TriggerSequence")]
        private static class ClientBossIntro
        {
            private static bool Prefix() { return !Mirroring; }
        }

        [HarmonyPatch(typeof(PhysicsEngine), "LateUpdate")]
        private static class ClientPhysics
        {
            // Generation still needs native physics registration. Stop collision
            // callbacks only after the client has verified and mirrors this floor.
            private static bool Prefix() { return !Mirroring; }
        }

        internal static void UpdatePlaceholderRoom(PlayerController player, RoomHandler room)
        {
            if (player == null || room == null || player.CurrentRoom == room) return;
            if (CurrentRoomField == null || CurrentlyVisibleField == null)
                throw new MissingFieldException("The native client room fields are missing from this game build.");

            RoomHandler previous = player.CurrentRoom;
            CurrentRoomField.SetValue(player, room);
            bool shared = false;
            foreach (PlayerController other in GameManager.Instance.AllPlayers) if (other != null && other != player && other.CurrentRoom == previous) shared = true;
            if (previous != null && !shared) {
                CurrentlyVisibleField.SetValue(previous, false);
                previous.visibility = RoomHandler.VisibilityStatus.VISITED;
            }
            CurrentlyVisibleField.SetValue(room, true);
            room.visibility = RoomHandler.VisibilityStatus.CURRENT;

            // ForceChangeRoom invokes PlayerEnter/Entered, which can seal rooms,
            // start reinforcements and independently pause for a local boss intro.
            // These Pixelator methods only update the local occlusion grid.
            if (Pixelator.Instance != null) {
                IntVector2 position = player.transform.position.IntXY(VectorConversions.Floor);
                if (previous != null) Pixelator.Instance.ProcessOcclusionChange(position, 0.3f, previous, false);
                Pixelator.Instance.ProcessOcclusionChange(position, 1f, room, false);
                Pixelator.Instance.ProcessRoomAdditionalExits(position, room, false);
            }
            ShadowSystem.ForceRoomLightsUpdate(room, .1f);
        }
        internal static void MovePlaceholder(PlayerController player, UnityEngine.Vector2 position)
        {
            player.transform.position = new UnityEngine.Vector3(position.x, position.y, player.transform.position.z);
            if (player.specRigidbody != null) player.specRigidbody.Position = new Position(position);
        }
    }
}
