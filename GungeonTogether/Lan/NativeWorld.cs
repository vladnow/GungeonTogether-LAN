using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Reflection;
using Dungeonator;
using UnityEngine;

namespace GungeonTogether.Lan
{
    public static class NativeWorld
    {
        [Serializable]
        public sealed class GenerationState
        {
            public int Version = 1;
            public GameStatsManager PersistentStats;
            public GameStats SessionStats;
            public GameStats SavedSessionStats;
            public PlayableCharacters SessionCharacter;
            public HashSet<int> SessionSynergies;
            public List<string> ProcessedTrackables;
            public bool ForceEarlyChest;
        }
        private static FieldInfo StatsField(string name)
        {
            FieldInfo field = typeof(GameStatsManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field == null) throw new IOException("Required generation field is missing: " + name);
            return field;
        }
        public static string ExportGenerationContext()
        {
            GameStatsManager original = GameStatsManager.Instance;
            string raw = FullInspector.SerializationHelpers.SerializeToContent<GameStatsManager, FullInspector.FullSerializerSerializer>(original);
            GameStatsManager copy = FullInspector.SerializationHelpers.DeserializeFromContent<GameStatsManager, FullInspector.FullSerializerSerializer>(raw);
            copy.savedSystemHash = 0; copy.midGameSaveGuid = "";
            // The normal save serializer excludes current-session statistics.
            // GetPlayerStatValue includes them, so room prerequisites on later
            // floors must see the host's current session as well as saved history.
            GenerationState state = new GenerationState {
                PersistentStats = copy,
                SessionStats = (GameStats)StatsField("m_sessionStats").GetValue(original),
                SavedSessionStats = (GameStats)StatsField("m_savedSessionStats").GetValue(original),
                SessionCharacter = (PlayableCharacters)StatsField("m_sessionCharacter").GetValue(original),
                SessionSynergies = (HashSet<int>)StatsField("m_sessionSynergies").GetValue(original),
                ProcessedTrackables = (List<string>)StatsField("m_singleProcessedEncounterTrackables").GetValue(original),
                ForceEarlyChest = MetaInjectionData.ForceEarlyChest
            };
            string context = FullInspector.SerializationHelpers.SerializeToContent<GenerationState, FullInspector.FullSerializerSerializer>(state);
            if (context.Length > 131072) throw new IOException("Generation context exceeds alpha protocol limit.");
            return context;
        }

        public static void ImportGenerationContext(string context)
        {
            GenerationState state = FullInspector.SerializationHelpers.DeserializeFromContent<GenerationState, FullInspector.FullSerializerSerializer>(context);
            if (state == null || state.Version != 1 || !Enum.IsDefined(typeof(PlayableCharacters), state.SessionCharacter)) throw new IOException("Invalid generation envelope.");
            GameStatsManager stats = state.PersistentStats;
            if (stats == null || stats.m_flags == null || stats.m_encounteredRooms == null || stats.m_characterStats == null) throw new IOException("Invalid host generation context.");
            StatsField("m_sessionStats").SetValue(stats, state.SessionStats);
            StatsField("m_savedSessionStats").SetValue(stats, state.SavedSessionStats);
            StatsField("m_sessionCharacter").SetValue(stats, state.SessionCharacter);
            StatsField("m_sessionSynergies").SetValue(stats, state.SessionSynergies);
            StatsField("m_singleProcessedEncounterTrackables").SetValue(stats, state.ProcessedTrackables);
            // ClearBlueprint does not reset this static flag. PreprocessRun
            // conditionally sets it, so different prior-floor outcomes otherwise
            // change room generation even with matching seeds and saved history.
            MetaInjectionData.ForceEarlyChest = state.ForceEarlyChest;
            typeof(GameStatsManager).GetField("m_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, stats);
        }

        public static string LayoutHash(Dungeon dungeon)
        {
            if (dungeon == null || dungeon.data == null) throw new IOException("Dungeon has not generated.");
            using (MemoryStream ms = new MemoryStream()) using (BinaryWriter w = new BinaryWriter(ms)) {
                w.Write(dungeon.data.Width); w.Write(dungeon.data.Height);
                for (int x = 0; x < dungeon.data.Width; x++) for (int y = 0; y < dungeon.data.Height; y++) {
                    CellData cell = dungeon.data.cellData[x][y];
                    w.Write(cell == null ? -1 : (int)cell.type);
                }
                List<string> rooms = new List<string>();
                foreach (RoomHandler room in dungeon.data.rooms)
                    rooms.Add(room.GetRoomName() + "|" + room.area.basePosition.x + "|" + room.area.basePosition.y + "|" + room.area.dimensions.x + "|" + room.area.dimensions.y);
                rooms.Sort(StringComparer.Ordinal);
                foreach (string room in rooms) w.Write(room);
                using (SHA256 hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(ms.ToArray())).Replace("-", "").ToLowerInvariant();
            }
        }

        public static GameObject CharacterPrefab(int character)
        {
            GameObject prefab = BraveResources.Load(CharacterCatalog.Prefab(character), ".prefab") as GameObject;
            if (prefab == null || prefab.GetComponent<PlayerController>() == null) throw new IOException("Native character prefab was not found: " + CharacterCatalog.Prefab(character));
            return prefab;
        }

        public static void PrepareClientPlayers(int host, int guest)
        {
            GameManager gm = GameManager.Instance;
            GameObject prefab = CharacterPrefab(host);
            GameManager.PlayerPrefabForNewGame = prefab;
            PlayerController original = gm.PrimaryPlayer;
            if (original == null || original.characterIdentity != prefab.GetComponent<PlayerController>().characterIdentity) {
                Vector3 position = original == null ? Vector3.zero : original.transform.position;
                PlayerController replacement = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity).GetComponent<PlayerController>();
                replacement.PlayerIDX = 0;
                gm.PrimaryPlayer = replacement;
                if (original != null) { original.gameObject.SetActive(false); UnityEngine.Object.Destroy(original.gameObject); }
            }
            EnsureGuest(guest);
        }

        public static PlayerController EnsureGuest(int character)
        {
            GameManager gm = GameManager.Instance;
            if (gm.SecondaryPlayer != null) {
                if (CharacterCatalog.FromIdentity(gm.SecondaryPlayer.characterIdentity.ToString()) != character)
                    throw new IOException("The native second player differs from the LAN selection. Return to the foyer and reconnect.");
                return gm.SecondaryPlayer;
            }
            if (gm.PrimaryPlayer == null) throw new IOException("Choose a character in the foyer first.");
            GameObject prefab = CharacterPrefab(character);
            GameManager.LastUsedCoopPlayerPrefab = prefab;
            GameManager.CoopPlayerPrefabForNewGame = prefab;
            gm.CurrentGameType = GameManager.GameType.COOP_2_PLAYER;
            GameObject go = UnityEngine.Object.Instantiate(prefab, gm.PrimaryPlayer.transform.position + new Vector3(1, 0, 0), Quaternion.identity);
            go.SetActive(true);
            PlayerController player = go.GetComponent<PlayerController>();
            if (player == null) throw new IOException("Guest character lacks PlayerController.");
            player.PlayerIDX = 1; gm.SecondaryPlayer = player;
            GameUIRoot.Instance.ConvertCoreUIToCoopMode();
            return player;
        }

        public static void WarmAssets()
        {
            string[] players = { "PlayerRogue", "PlayerMarine", "PlayerConvict", "PlayerGuide", "PlayerCoopCultist" };
            foreach (string player in players) BraveResources.Load(player, ".prefab");
        }

        public static void DepartFoyer()
        {
            GameManager gm = GameManager.Instance;
            gm.IsFoyer = false;
            foreach (PlayerController player in gm.AllPlayers) {
                if (player == null) continue;
                if (player.inventory != null) player.inventory.ForceNoGun = false;
                if (player.CurrentGun != null) player.CurrentGun.gameObject.SetActive(true);
            }
        }

        public static void EnableWeapons()
        {
            foreach (PlayerController player in GameManager.Instance.AllPlayers) {
                if (player == null) continue;
                if (player.inventory == null || player.inventory.AllGuns.Count == 0)
                    typeof(PlayerController).GetMethod("InitializeInventory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(player, null);
                player.inventory.ForceNoGun = false;
                if (player.CurrentGun != null) player.CurrentGun.gameObject.SetActive(true);
            }
        }
    }
}
