using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace GungeonTogether.Lan
{
    // Read-only native database references for the equipment page. Never grant
    // pickups or run item callbacks/stat recalculation on the mirror client.
    internal sealed class InventoryBook
    {
        internal readonly List<Gun> Guns = new List<Gun>();
        internal readonly List<PlayerItem> Active = new List<PlayerItem>();
        internal readonly List<PassiveItem> Passive = new List<PassiveItem>();
        internal readonly List<int> Synergies = new List<int>();
        internal InventoryBook(InventoryState state)
        {
            foreach (int id in state.Guns) Guns.Add(Resolve<Gun>(id));
            foreach (int id in state.Active) Active.Add(Resolve<PlayerItem>(id));
            foreach (int id in state.Passive) Passive.Add(Resolve<PassiveItem>(id));
            foreach (int id in state.Synergies) {
                if (id >= GameManager.Instance.SynergyManager.synergies.Length) throw new System.IO.IOException("Guest synergy database differs.");
                Synergies.Add(id);
            }
        }
        private static T Resolve<T>(int id) where T : PickupObject
        {
            T item = PickupObjectDatabase.GetById(id) as T;
            if (item == null) throw new System.IO.IOException("Missing or incompatible inventory entry: " + id);
            return item;
        }
        private static InventoryBook reading;
        private static DungeonSession Session { get { return DungeonPlugin.Instance == null ? null : DungeonPlugin.Instance.Session; } }
        internal static int NativePagesBuilt { get; private set; }
        internal static List<Gun> ReadGuns(GunInventory inventory) { return reading == null ? inventory.AllGuns : reading.Guns; }
        internal static List<PlayerItem> ReadActive(PlayerController player) { return reading == null ? player.activeItems : reading.Active; }
        internal static List<PassiveItem> ReadPassive(PlayerController player) { return reading == null ? player.passiveItems : reading.Passive; }
        internal static List<int> ReadSynergies(PlayerController player) { return reading == null ? player.ActiveExtraSynergies : reading.Synergies; }
        private struct Scope { internal InventoryBook Previous; internal int PausingPlayer; internal bool Active; }

        [HarmonyPatch(typeof(AmmonomiconPageRenderer), "InitializeEquipmentPageLeft")]
        private static class EquipmentPage
        {
            private static void Prefix(out Scope __state)
            {
                __state = new Scope(); DungeonSession s = Session;
                if (s == null || !s.Mirroring || s.Book == null) return;
                __state = new Scope { Previous = reading, PausingPlayer = GameManager.Instance.LastPausingPlayerID, Active = true };
                reading = s.Book; GameManager.Instance.LastPausingPlayerID = 1;
            }
            private static Exception Finalizer(Exception __exception, Scope __state)
            {
                if (__state.Active) { reading = __state.Previous; GameManager.Instance.LastPausingPlayerID = __state.PausingPlayer; if (__exception == null) NativePagesBuilt++; }
                return __exception;
            }
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo active = AccessTools.Field(typeof(PlayerController), "activeItems"), passive = AccessTools.Field(typeof(PlayerController), "passiveItems"), synergies = AccessTools.Field(typeof(PlayerController), "ActiveExtraSynergies");
                MethodInfo guns = AccessTools.PropertyGetter(typeof(GunInventory), "AllGuns");
                int a = 0, p = 0, s = 0, g = 0;
                foreach (CodeInstruction instruction in instructions) {
                    string helper = null;
                    if (instruction.opcode == OpCodes.Ldfld) {
                        if (Equals(instruction.operand, active)) { helper = "ReadActive"; a++; }
                        else if (Equals(instruction.operand, passive)) { helper = "ReadPassive"; p++; }
                        else if (Equals(instruction.operand, synergies)) { helper = "ReadSynergies"; s++; }
                    } else if ((instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call) && Equals(instruction.operand, guns)) { helper = "ReadGuns"; g++; }
                    if (helper != null) { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(InventoryBook), helper); }
                    yield return instruction;
                }
                if (a == 0 || p == 0 || s == 0 || g == 0) throw new MissingMethodException("Native equipment-page inventory readers differ.");
            }
        }
        internal static bool BookOpen { get { return AmmonomiconController.HasInstance && (AmmonomiconController.Instance.IsOpen || AmmonomiconController.Instance.IsOpening); } }
        internal static void DrawCaption(DungeonSession session)
        {
            if (!BookOpen || session.Equipment == null) return;
            AmmonomiconPageRenderer page = AmmonomiconController.Instance.CurrentLeftPageRenderer;
            if (page == null || page.pageType != AmmonomiconPageRenderer.PageType.EQUIPMENT_LEFT) return;
            GUIStyle style = new GUIStyle(GUI.skin.label); style.fontSize = 15; style.normal.textColor = new Color(.9f, .81f, .56f);
            GUI.Label(new Rect(12, 10, 400, 24), "PLAYER TWO · " + CharacterCatalog.Name(session.Equipment.Character), style);
        }
    }
}
