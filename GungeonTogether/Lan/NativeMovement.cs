using System;
using Dungeonator;
using HarmonyLib;
using UnityEngine;

namespace GungeonTogether.Lan
{
    internal static class NativeMovement
    {
        private static DungeonSession Session { get { return DungeonPlugin.Instance == null ? null : DungeonPlugin.Instance.Session; } }
        internal static PlayerMotion Capture(int epoch, uint sequence, RemoteControls controls)
        {
            GameManager gm = GameManager.Instance;
            PlayerController p = gm.SecondaryPlayer;
            CameraController cc = gm.MainCameraController;
            if (p == null || p.stats == null || p.specRigidbody == null || p.specRigidbody.PrimaryPixelCollider == null || cc == null || cc.Camera == null) return null;
            Vector3 camera = PlayerCamera.GuestPosition(cc, p);
            PlayerMotion motion = new PlayerMotion { Epoch = epoch, Sequence = sequence, SentAt = Time.realtimeSinceStartup, InputSequence = controls.SimulatedInputSequence,
                X = p.transform.position.x, Y = p.transform.position.y, CameraX = camera.x, CameraY = camera.y, CameraZ = camera.z, CameraSize = cc.Camera.orthographicSize,
                Speed = Mathf.Clamp(p.stats.MovementSpeed, 0, 100), CameraFollowsPlayer = !cc.ManualControl && !cc.UseOverridePlayerTwoPosition };
            if (p.CurrentRoom != null) { motion.RoomX = p.CurrentRoom.area.basePosition.x; motion.RoomY = p.CurrentRoom.area.basePosition.y; }
            Vector2 offset = p.specRigidbody.UnitCenter - p.transform.position.XY();
            motion.FootX = offset.x; motion.FootY = offset.y;
            Vector2 dimensions = p.specRigidbody.PrimaryPixelCollider.UnitDimensions;
            motion.HalfWidth = Mathf.Clamp(dimensions.x / 2, .01f, 3); motion.HalfHeight = Mathf.Clamp(dimensions.y / 2, .01f, 3);
            motion.AllowPrediction = motion.CameraFollowsPlayer && !gm.IsPaused && !p.IsDodgeRolling && !p.IsFalling && !p.IsGhost && !p.healthHaver.IsDead &&
                !p.IsInMinecart && !p.IsFlying && !p.IsInputOverridden && p.CurrentInputState == PlayerInputState.AllInput && p.CurrentRoom != null;
            Vector2 move = new Vector2(controls.SimulatedMoveX, controls.SimulatedMoveY), velocity = p.specRigidbody.Velocity;
            if (move.sqrMagnitude > .01f && velocity.sqrMagnitude > .01f) {
                float speed = Vector2.Dot(velocity, move.normalized) / move.magnitude;
                if (speed < 0 || speed > motion.Speed * 1.6f || (velocity - move.normalized * Vector2.Dot(velocity, move.normalized)).sqrMagnitude > .0625f) motion.AllowPrediction = false;
                else motion.Speed = Mathf.Clamp(speed, 0, 100);
            }
            return motion;
        }
        internal static Vector2 ClipLead(PlayerMotion motion, Vector2 lead)
        {
            if (lead.sqrMagnitude < .000001f) return Vector2.zero;
            DungeonData data = GameManager.Instance.Dungeon.data;
            Vector2 origin = new Vector2(motion.X + motion.FootX, motion.Y + motion.FootY);
            int steps = Mathf.Max(1, Mathf.CeilToInt(lead.magnitude * 16));
            Vector2 accepted = Vector2.zero;
            int mask = CollisionMask.LayerToMask(CollisionLayer.HighObstacle, CollisionLayer.LowObstacle);
            for (int step = 1; step <= steps; step++) {
                Vector2 delta = lead * ((float)step / steps), center = origin + delta;
                Vector2 half = new Vector2(motion.HalfWidth, motion.HalfHeight);
                for (int ix = -1; ix <= 1; ix++) for (int iy = -1; iy <= 1; iy++) {
                    Vector2 point = center + new Vector2(half.x * ix, half.y * iy);
                    IntVector2 cellPoint = new IntVector2(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
                    if (!data.CheckInBoundsAndValid(cellPoint)) return accepted;
                    CellData cell = data[cellPoint];
                    if (cell.type != CellType.FLOOR || cell.parentRoom == null || cell.parentRoom.area.basePosition.x != motion.RoomX || cell.parentRoom.area.basePosition.y != motion.RoomY) return accepted;
                }
                foreach (ICollidableObject item in PhysicsEngine.Instance.GetOverlappingCollidableObjects(center - half, center + half, false, true, mask, false)) {
                    SpeculativeRigidbody body = item as SpeculativeRigidbody;
                    if (body != null && body.enabled && body.CollideWithOthers && body.gameObject.activeInHierarchy && body.GetComponent<PlayerController>() == null) return accepted;
                }
                accepted = delta;
            }
            return accepted;
        }
        [HarmonyPatch(typeof(PlayerController), "HandlePlayerInput")]
        private static class AppliedInput
        {
            private static void Postfix(PlayerController __instance)
            {
                DungeonSession s = Session;
                if (s != null && s.IsHost && s.Phase == DungeonSession.RunPhase.Running && __instance.PlayerIDX == 1 && s.Controls != null) s.Controls.MarkSimulatedInput();
            }
        }
        [HarmonyPatch(typeof(PhysicsEngine), "LateUpdate")]
        private static class AfterSimulation
        {
            private static void Postfix() { DungeonSession s = Session; if (s != null && s.IsHost) s.AfterPhysics(); }
        }
    }
}
