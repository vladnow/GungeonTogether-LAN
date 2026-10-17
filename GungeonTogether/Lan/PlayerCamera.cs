using HarmonyLib;
using UnityEngine;

namespace GungeonTogether.Lan
{
    internal static class PlayerCamera
    {
        private static DungeonSession Session { get { return DungeonPlugin.Instance == null ? null : DungeonPlugin.Instance.Session; } }
        private static bool ActiveHost {
            get { DungeonSession session = Session; return session != null && session.IsHost && session.Phase == DungeonSession.RunPhase.Running && !GameManager.Instance.IsLoadingLevel; }
        }

        internal static Vector3 GuestPosition(CameraController controller, PlayerController player)
        {
            // Native boss introductions and other manual camera sequences retain
            // their explicit framing. Normal gameplay follows player two only.
            if (player == null || controller.ManualControl) return controller.Camera.transform.position;
            Vector2 center = controller.UseOverridePlayerTwoPosition ? controller.OverridePlayerTwoPosition : player.CenterPosition;
            return center.ToVector3ZUp(controller.CurrentZOffset) + controller.FINAL_CAMERA_POSITION_OFFSET;
        }

        internal static Plane[] GuestFrustum(Camera camera, Vector3 position, float aspect)
        {
            // A virtual view: do not move the host's real camera during capture.
            // Preserve its rotation, clipping and pixel projection; change only
            // the view origin and the guest's reported viewport aspect.
            Matrix4x4 view = camera.worldToCameraMatrix * Matrix4x4.Translate(camera.transform.position - position);
            Matrix4x4 projection = camera.projectionMatrix;
            projection[0, 0] *= camera.aspect / aspect;
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(projection * view);
            for (int i = 0; i < planes.Length; i++) planes[i].distance += 2f;
            return planes;
        }

        [HarmonyPatch(typeof(CameraController), "GetCoreCurrentBasePosition")]
        private static class HostFocus
        {
            private static bool Prefix(CameraController __instance, ref Vector2 __result)
            {
                if (!ActiveHost || __instance != GameManager.Instance.MainCameraController) return true;
                PlayerController player = GameManager.Instance.PrimaryPlayer;
                if (player == null) return true;
                __result = __instance.UseOverridePlayerOnePosition ? __instance.OverridePlayerOnePosition : CameraController.SuperSmoothCamera ? player.SmoothedCameraCenter : player.CenterPosition;
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerController), "CameraBoundsMovementRestrictor")]
        private static class SeparateScreenBounds
        {
            // Skip only the native shared-screen restriction. Do not turn a
            // rejected location into a valid one or remove wall/pit collisions.
            private static bool Prefix() { return !ActiveHost; }
        }
        [HarmonyPatch(typeof(DungeonDoorController), "CheckForPlayerCollision")]
        private static class SeparateDoorEntry
        {
            private static bool Prefix(DungeonDoorController __instance, SpeculativeRigidbody otherRigidbody, Vector2 normal)
            {
                if (!ActiveHost || __instance.Mode != DungeonDoorController.DungeonDoorMode.COMPLEX) return true;
                if (__instance.IsSealed || __instance.isLocked || __instance.IsOpen || otherRigidbody == null || otherRigidbody.GetComponent<PlayerController>() == null) return true;
                // Native shared-screen entry requires BOTH players within 0.3
                // units and then forces their movement. Separate views need no
                // proximity gate. Open still enforces native locks/seals; room
                // battle regrouping is left to HandleCoopSpecificTimers.
                bool flipped = (__instance.northSouth && normal.y < 0) || (!__instance.northSouth && normal.x < 0);
                __instance.Open(flipped);
                return false;
            }
        }
    }
}
