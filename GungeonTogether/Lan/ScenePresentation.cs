using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace GungeonTogether.Lan
{
    internal sealed class ScenePresentation : IDisposable
    {
        private static readonly FieldInfo GoopCells = AccessTools.Field(typeof(DeadlyDeadlyGoopManager), "m_goopedCells");
        private static readonly FieldInfo GoopPositions = AccessTools.Field(typeof(DeadlyDeadlyGoopManager), "m_goopedPositions");
        private static readonly FieldInfo Boxes = AccessTools.Field(typeof(TextBoxManager), "extantTextBoxMap");
        private static readonly FieldInfo BoxPosition = AccessTools.Field(typeof(TextBoxManager), "m_basePosition");
        private static readonly MethodInfo Rebuild = AccessTools.Method(typeof(DeadlyDeadlyGoopManager), "RebuildMeshUvsAndColors");
        private static readonly Type CellType = typeof(DeadlyDeadlyGoopManager).GetNestedType("GoopPositionData", BindingFlags.NonPublic);
        private static readonly FieldInfo CellFire = AccessTools.Field(CellType, "IsOnFire"), CellElectric = AccessTools.Field(CellType, "IsElectrified"), CellFrozen = AccessTools.Field(CellType, "IsFrozen"), CellLife = AccessTools.Field(CellType, "remainingLifespan"), CellFireTime = AccessTools.Field(CellType, "totalOnFireTime");
        private static readonly MethodInfo RemoveCell = AccessTools.Method(typeof(DeadlyDeadlyGoopManager), "RemoveGoopedPosition", new[] { typeof(IntVector2) });
        private static readonly FieldInfo FireSystem = AccessTools.Field(typeof(DeadlyDeadlyGoopManager), "m_fireSystem");
        private static readonly FieldInfo FireTimer = AccessTools.Field(CellType, "remainingFireTimer");
        private static readonly Dictionary<int, DialogueBox> hostBoxes = new Dictionary<int, DialogueBox>();
        private static string[] hostResponses;
        private static int hostSpeaker;
        private readonly Dictionary<int, Transform> localBoxes = new Dictionary<int, Transform>();
        private readonly Dictionary<int, string> boxContents = new Dictionary<int, string>();
        private readonly Dictionary<string, GoopDefinition> assets = new Dictionary<string, GoopDefinition>();
        private readonly Dictionary<DeadlyDeadlyGoopManager, HashSet<IntVector2>> ownedCells = new Dictionary<DeadlyDeadlyGoopManager, HashSet<IntVector2>>();
        private uint sent, accepted, hurt, damage;
        private bool responsesVisible, loadedGoops;
        private string responseSignature;
        private double next;
        private double particleClock;
        private bool surfacesInitialized;
        internal EffectsState LastState { get; private set; }
        public int SurfaceCount { get; private set; }
        public int TextCount { get { return localBoxes.Count; } }
        public int HurtEffects { get; private set; }
        public string MovementBlock { get; private set; } = "";
        private static DungeonSession Session { get { return DungeonPlugin.Instance == null ? null : DungeonPlugin.Instance.Session; } }
        private static bool Host { get { return Session != null && Session.IsHost && Session.Phase == DungeonSession.RunPhase.Running; } }
        private static bool Mirror { get { return Session != null && Session.Mirroring; } }
        private static string Key(GoopDefinition d) { return d.name + "|" + (d.goopTexture == null ? "" : d.goopTexture.name); }
        internal void Capture(int epoch, double now, Action<byte[]> send)
        {
            if (!NetworkTiming.Due(ref next, now, .10)) return;
            PlayerController p = GameManager.Instance.SecondaryPlayer;
            if (p == null) return;
            EffectsState s = new EffectsState { Epoch = epoch, Sequence = ++sent, Damage = damage, Vignette = Pixelator.Instance.vignettePower, HostX = GameManager.Instance.PrimaryPlayer.transform.position.x, HostY = GameManager.Instance.PrimaryPlayer.transform.position.y };
            s.BlockReason = p.CurrentInputState.ToString() + (p.IsInputOverridden ? "; " + InputReasons(p) : "");
            if (s.BlockReason.Length > 256) s.BlockReason = s.BlockReason.Substring(0, 256);
            MovementBlock = s.BlockReason;
            s.Fire = Mathf.Clamp01(p.CurrentFireMeterValue); s.Poison = Mathf.Clamp01(p.CurrentPoisonMeterValue); s.Curse = Mathf.Clamp01(p.CurrentCurseMeterValue); s.Drain = Mathf.Clamp01(p.CurrentDrainMeterValue); s.Burning = p.IsOnFire;
            foreach (KeyValuePair<IntVector2, DeadlyDeadlyGoopManager> pair in DeadlyDeadlyGoopManager.allGoopPositionMap) {
                if (pair.Value == null || (pair.Key.ToVector2() * .25f - p.CenterPosition).sqrMagnitude > 1600) continue;
                string key = Key(pair.Value.goopDefinition); int kind = s.Kinds.IndexOf(key);
                if (kind < 0) { kind = s.Kinds.Count; s.Kinds.Add(key); }
                IDictionary cells = (IDictionary)GoopCells.GetValue(pair.Value);
                object c = cells[pair.Key]; if (c == null) continue;
                s.Cells.Add(new SurfaceCell { X = pair.Key.x, Y = pair.Key.y, Kind = (byte)kind, Flags = (byte)(((bool)CellFire.GetValue(c) ? 1 : 0) | ((bool)CellElectric.GetValue(c) ? 2 : 0) | ((bool)CellFrozen.GetValue(c) ? 4 : 0)), Life = Mathf.Max(-1, (float)CellLife.GetValue(c)), FireTime = (float)CellFireTime.GetValue(c) });
            }
            var extant = (Dictionary<Transform, GameObject>)Boxes.GetValue(null);
            foreach (KeyValuePair<Transform, GameObject> pair in extant) {
                if (pair.Key == null || pair.Value == null) continue;
                TextBoxManager box = pair.Value.GetComponent<TextBoxManager>(); if (box == null || box.IsScalingDown) continue;
                DialogueBox b; if (!hostBoxes.TryGetValue(pair.Key.GetInstanceID(), out b)) continue;
                Vector3 pos = (Vector3)BoxPosition.GetValue(box);
                s.Boxes.Add(new DialogueBox { Id = b.Id, Style = b.Style, Text = b.Text, X = pos.x, Y = pos.y, Z = pos.z });
            }
            GameUIRoot ui = GameUIRoot.Instance;
            if (ui != null && ui.DisplayingConversationBar && hostResponses != null) {
                s.Speaker = hostSpeaker; s.Responses.AddRange(hostResponses);
                s.Selected = (int)AccessTools.Field(typeof(GameUIRoot), "selectedResponse").GetValue(ui);
            }
            send(s.Encode());
        }
        private static string InputReasons(PlayerController player)
        {
            object value = AccessTools.Field(typeof(PlayerController), "m_disableInput").GetValue(player);
            FieldInfo reasons = AccessTools.Field(value.GetType(), "m_overrides");
            if (reasons == null) return "native input override";
            IDictionary map = reasons.GetValue(value) as IDictionary;
            if (map == null) return "native input override";
            List<string> names = new List<string>(); foreach (object key in map.Keys) names.Add(key.ToString());
            return string.Join(",", names.ToArray());
        }
        internal void Damaged() { damage++; }
        internal void Apply(EffectsState s)
        {
            if (!Wire.Newer(s.Sequence, accepted) && accepted != 0) return;
            accepted = s.Sequence; MovementBlock = s.BlockReason;
            if (Wire.Newer(s.Damage, hurt)) {
                if (Pixelator.Instance != null) Pixelator.Instance.HandleDamagedVignette(Vector2.zero);
                Session.Audio.PlayHurt(GameManager.Instance.PrimaryPlayer, Session.GuestCharacter);
                HurtEffects++;
            }
            hurt = s.Damage;
            LastState = s;
            PlayerController remote = GameManager.Instance.SecondaryPlayer;
            if (remote != null) {
                ClientSimulationGuard.MovePlaceholder(remote, new Vector2(s.HostX, s.HostY));
                var room = GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(new IntVector2((int)Math.Floor(s.HostX), (int)Math.Floor(s.HostY)));
                if (room != null) ClientSimulationGuard.UpdatePlaceholderRoom(remote, room);
            }
            if (Pixelator.Instance != null && Math.Abs(Pixelator.Instance.vignettePower - s.Vignette) > .001f) { Pixelator.Instance.vignettePower = s.Vignette; Pixelator.Instance.SetVignettePower(s.Vignette); }
            ApplySurfaces(s);
            HashSet<int> present = new HashSet<int>();
            foreach (DialogueBox b in s.Boxes) {
                present.Add(b.Id); Transform anchor;
                if (!localBoxes.TryGetValue(b.Id, out anchor)) { GameObject go = new GameObject("GT_Dialogue"); go.AddComponent<NetworkPresentationAsset>(); anchor = go.transform; localBoxes.Add(b.Id, anchor); }
                anchor.position = new Vector3(b.X, b.Y, b.Z);
                string signature = b.Style + "\n" + b.Text;
                string previous;
                if (!boxContents.TryGetValue(b.Id, out previous) || previous != signature || !TextBoxManager.HasTextBox(anchor)) {
                    AccessTools.Method(typeof(TextBoxManager), "ShowBoxInternal").Invoke(null, new object[] { anchor.position, anchor, -1f, b.Text, b.Style, b.Style == "InfoBox" ? .25f : .5f, "", true, TextBoxManager.BoxSlideOrientation.NO_ADJUSTMENT, false, false });
                    boxContents[b.Id] = signature;
                }
            }
            List<int> remove = new List<int>(); foreach (int id in localBoxes.Keys) if (!present.Contains(id)) remove.Add(id);
            foreach (int id in remove) { TextBoxManager.ClearTextBox(localBoxes[id]); UnityEngine.Object.Destroy(localBoxes[id].gameObject); localBoxes.Remove(id); boxContents.Remove(id); }
            GameUIRoot ui = GameUIRoot.Instance;
            string response = string.Join("\n", s.Responses.ToArray());
            if (s.Responses.Count > 0) {
                if (!responsesVisible || responseSignature != response) ui.ConversationBar.ShowBar(s.Speaker == 1 ? GameManager.Instance.SecondaryPlayer : GameManager.Instance.PrimaryPlayer, s.Responses.ToArray());
                ui.ConversationBar.SetSelectedResponse(s.Selected); responsesVisible = true; responseSignature = response;
                ui.ToggleLowerPanels(false, false, "GT_NetworkDialogue");
            } else if (responsesVisible) { ui.ConversationBar.HideBar(); ui.ToggleLowerPanels(true, false, "GT_NetworkDialogue"); responsesVisible = false; }
        }
        private GoopDefinition Resolve(string key)
        {
            GoopDefinition d;
            if (assets.TryGetValue(key, out d) && d != null) return d;
            foreach (GoopDefinition candidate in Resources.FindObjectsOfTypeAll<GoopDefinition>()) if (candidate != null) assets[Key(candidate)] = candidate;
            if (!assets.TryGetValue(key, out d) && !loadedGoops) {
                loadedGoops = true;
                foreach (string bundleName in new[] { "shared_auto_001", "shared_auto_002" }) {
                    AssetBundle bundle = ResourceManager.LoadAssetBundle(bundleName);
                    if (bundle != null) foreach (GoopDefinition candidate in bundle.LoadAllAssets<GoopDefinition>()) if (candidate != null) assets[Key(candidate)] = candidate;
                }
                assets.TryGetValue(key, out d);
            }
            if (d == null) throw new System.IO.IOException("Missing local goop asset: " + key);
            return d;
        }
        private void ApplySurfaces(EffectsState s)
        {
            // Write native render data directly. AddGoopCircle would fire local
            // OnCellGooped callbacks, solidify lava and change gameplay state.
            Dictionary<DeadlyDeadlyGoopManager, HashSet<IntVector2>> incoming = new Dictionary<DeadlyDeadlyGoopManager, HashSet<IntVector2>>();
            Dictionary<DeadlyDeadlyGoopManager, HashSet<IntVector2>> dirty = new Dictionary<DeadlyDeadlyGoopManager, HashSet<IntVector2>>();
            if (!surfacesInitialized) {
                surfacesInitialized = true;
                var local = new Dictionary<IntVector2, DeadlyDeadlyGoopManager>(DeadlyDeadlyGoopManager.allGoopPositionMap);
                foreach (var pair in local) if (pair.Value != null) { RemoveCell.Invoke(pair.Value, new object[] { pair.Key }); Mark(dirty, pair.Value, pair.Key); }
            }
            foreach (SurfaceCell cell in s.Cells) {
                DeadlyDeadlyGoopManager manager = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(Resolve(s.Kinds[cell.Kind]));
                IntVector2 pos = new IntVector2(cell.X, cell.Y);
                if (!incoming.ContainsKey(manager)) incoming.Add(manager, new HashSet<IntVector2>());
                incoming[manager].Add(pos);
                IDictionary cells = (IDictionary)GoopCells.GetValue(manager);
                var positions = (HashSet<IntVector2>)GoopPositions.GetValue(manager);
                object c = cells[pos];
                if (c == null) { c = Activator.CreateInstance(CellType, new object[] { pos, cells, cell.Life }); cells.Add(pos, c); positions.Add(pos); }
                CellFire.SetValue(c, (cell.Flags & 1) != 0); CellElectric.SetValue(c, (cell.Flags & 2) != 0); CellFrozen.SetValue(c, (cell.Flags & 4) != 0);
                CellLife.SetValue(c, cell.Life); CellFireTime.SetValue(c, cell.FireTime);
                if ((cell.Flags & 1) != 0 && cell.X % 2 == 0 && cell.Y % 2 == 0) {
                    float timer = (float)FireTimer.GetValue(c) - (float)(Time.realtimeSinceStartup - particleClock);
                    if (timer <= 0) {
                        if (FireSystem.GetValue(manager) == null) AccessTools.Method(typeof(DeadlyDeadlyGoopManager), "InitializeParticleSystems").Invoke(manager, null);
                        ParticleSystem fire = (ParticleSystem)FireSystem.GetValue(manager);
                        Vector3 pos3 = new Vector3(cell.X * .25f + .125f, cell.Y * .25f + .125f, cell.Y * .25f + .125f);
                        fire.Emit(new ParticleSystem.EmitParams { position = pos3, velocity = Vector3.zero, startSize = fire.main.startSizeMultiplier, startLifetime = 1f, startColor = fire.main.startColor.color }, 1);
                        timer = .8f;
                    }
                    FireTimer.SetValue(c, timer);
                }
                DeadlyDeadlyGoopManager.allGoopPositionMap[pos] = manager;
                Mark(dirty, manager, pos);
            }
            foreach (var pair in ownedCells) {
                if (pair.Key == null) continue;
                foreach (IntVector2 pos in pair.Value) if (!incoming.ContainsKey(pair.Key) || !incoming[pair.Key].Contains(pos)) {
                    DeadlyDeadlyGoopManager owner;
                    DeadlyDeadlyGoopManager.allGoopPositionMap.TryGetValue(pos, out owner);
                    RemoveCell.Invoke(pair.Key, new object[] { pos });
                    if (owner != null && owner != pair.Key) DeadlyDeadlyGoopManager.allGoopPositionMap[pos] = owner;
                    Mark(dirty, pair.Key, pos);
                }
            }
            ownedCells.Clear(); foreach (var pair in incoming) ownedCells.Add(pair.Key, pair.Value);
            foreach (var pair in dirty) foreach (IntVector2 chunk in pair.Value) Rebuild.Invoke(pair.Key, new object[] { chunk.x, chunk.y });
            SurfaceCount = s.Cells.Count;
            particleClock = Time.realtimeSinceStartup;
        }
        private static void Mark(Dictionary<DeadlyDeadlyGoopManager, HashSet<IntVector2>> dirty, DeadlyDeadlyGoopManager m, IntVector2 pos)
        { if (!dirty.ContainsKey(m)) dirty.Add(m, new HashSet<IntVector2>()); int chunk = (int)AccessTools.Field(typeof(DeadlyDeadlyGoopManager), "CHUNK_SIZE").GetValue(m) * 4; dirty[m].Add(new IntVector2(pos.x / chunk, pos.y / chunk)); }
        public void Dispose()
        {
            foreach (Transform anchor in localBoxes.Values) if (anchor != null) { TextBoxManager.ClearTextBox(anchor); UnityEngine.Object.Destroy(anchor.gameObject); }
            foreach (var pair in ownedCells) if (pair.Key != null) foreach (IntVector2 cell in pair.Value) RemoveCell.Invoke(pair.Key, new object[] { cell });
            localBoxes.Clear(); boxContents.Clear(); ownedCells.Clear(); assets.Clear(); LastState = null; surfacesInitialized = false;
            if (responsesVisible && GameUIRoot.Instance != null) { GameUIRoot.Instance.ConversationBar.HideBar(); GameUIRoot.Instance.ToggleLowerPanels(true, false, "GT_NetworkDialogue"); }
            responsesVisible = false; accepted = hurt = damage = 0; next = 0;
            if (Session == null || Session.IsHost) { hostBoxes.Clear(); hostResponses = null; }
        }
        [HarmonyPatch(typeof(TextBoxManager), "ShowBoxInternal")]
        private static class TextCreated
        {
            private static void Prefix(Vector3 worldPosition, Transform parent, string text, string prefabName)
            { if (Host && parent != null) hostBoxes[parent.GetInstanceID()] = new DialogueBox { Id = parent.GetInstanceID(), Style = prefabName, Text = text, X = worldPosition.x, Y = worldPosition.y, Z = worldPosition.z }; }
        }
        [HarmonyPatch(typeof(GameUIRoot), "DisplayPlayerConversationOptions", new Type[] { typeof(PlayerController), typeof(string[]) })]
        private static class ResponsesCreated
        {
            private static void Postfix(PlayerController interactingPlayer, string[] responses, bool __result)
            { if (Host && __result) { hostResponses = responses == null ? new string[0] : (string[])responses.Clone(); hostSpeaker = interactingPlayer.PlayerIDX; } }
        }
        [HarmonyPatch(typeof(HealthHaver), "ApplyDamageDirectional")]
        private static class Hurt
        {
            private static void Prefix(HealthHaver __instance, out float __state) { __state = __instance.GetCurrentHealth() + __instance.Armor; }
            private static void Postfix(HealthHaver __instance, float __state)
            { if (Host && GameManager.Instance.SecondaryPlayer != null && __instance == GameManager.Instance.SecondaryPlayer.healthHaver && __instance.GetCurrentHealth() + __instance.Armor < __state) Session.SceneEffects.Damaged(); }
        }
        [HarmonyPatch(typeof(DeadlyDeadlyGoopManager), "LateUpdate")]
        private static class NativeGoopSimulation { private static bool Prefix() { return !Mirror; } }
        [HarmonyPatch(typeof(DeadlyDeadlyGoopManager), "ProcessGameActor")]
        private static class NoLocalGoopDamage { private static bool Prefix(ref bool __result) { if (!Mirror) return true; __result = false; return false; } }
    }
    internal sealed class NetworkPresentationAsset : MonoBehaviour { }
}
