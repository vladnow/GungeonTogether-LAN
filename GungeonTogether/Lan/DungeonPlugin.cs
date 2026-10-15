using System;
using System.IO;
using System.Security.Cryptography;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using GungeonTogether.Networking;

namespace GungeonTogether.Lan
{
    [BepInPlugin("com.gungeontogether.lan", "Gungeon Together LAN Alpha", "0.2.2")]
    [BepInIncompatibility("com.llamerrr.gungeontogether")]
    public sealed class DungeonPlugin : BaseUnityPlugin
    {
        public static DungeonPlugin Instance;
        public DungeonSession Session { get; private set; }
        private LanTransport transport;
        private Harmony harmony;
        private string ip, bind, port, code, fingerprint, error = "";
        private Rect window = new Rect(20, 20, 590, 450);
        private bool visible = true;
        private RemotePlayerAvatar foyerAvatar;
        private float nextPosition;
        private string smokeRole;
        public bool IsSmoke { get { return smokeRole != null || profileCheck; } }
        public bool UsesSeparateProfile { get; private set; }
        private bool profileCheck, profilePathChecked;
        private string profileObservedPath;
        private int smokePort = 27639;
        private float smokeDeadline, smokeStarted;
        private float nextSmokeDiagnostic;
        private float nextControlsDiagnostic;
        private bool smokeRunStarted, smokeLayout, smokeSnapshot;
        private bool smokeMovePassed, smokeShotPassed, smokeCaptureRequested;
        private AIActor smokeEnemy;
        private float smokeEnemyHealth;
        private bool smokeEnemyCreated, smokeDamagePassed, smokeSecondFloor;
        private float smokeSecondFloorAt;
        private bool smokeComplete;
        private Vector3 smokeCultistStart;
        private int smokeStartAmmo;

        private void Awake()
        {
            Instance = this;
            Application.runInBackground = true;
            ProfilePolicy.SaveProfileMode profileMode = Config.Bind("Profile", "SaveProfileMode", ProfilePolicy.SaveProfileMode.Auto,
                "Auto: manager profile uses separate LAN saves; direct Steam launch keeps normal saves. Normal: LAN disabled. LAN: always use LAN saves.").Value;
            UsesSeparateProfile = ProfilePolicy.UsesSeparateProfile(Environment.GetCommandLineArgs(), Paths.GameRootPath, Paths.BepInExRootPath, profileMode);
            foreach (string arg in Environment.GetCommandLineArgs()) {
                if (arg == "--gt-dungeon-smoke-host") smokeRole = "host";
                if (arg == "--gt-dungeon-smoke-client") smokeRole = "client";
                if (arg == "--gt-profile-check") profileCheck = true;
            }
            visible = UsesSeparateProfile && !profileCheck;
            if (IsSmoke) {
                GameManager.AUDIO_ENABLED = false; AudioListener.pause = true; AudioListener.volume = 0;
            }
            if (smokeRole != null) {
                Foyer.DoIntroSequence = false; Foyer.DoMainMenu = false; visible = false; smokeDeadline = Time.realtimeSinceStartup + 110;
            }
            harmony = new Harmony("com.gungeontogether.lan.dungeon-0.2.2");
            harmony.PatchAll(typeof(DungeonPlugin).Assembly);
            ip = Config.Bind("LAN", "HostIP", "127.0.0.1", "Host's Radmin IPv4.").Value;
            bind = Config.Bind("LAN", "BindIP", "0.0.0.0", "Host's own Radmin IPv4 or all interfaces.").Value;
            port = Config.Bind("LAN", "Port", LanTransport.DefaultPort, "TCP port.").Value.ToString();
            code = Guid.NewGuid().ToString("N").Substring(0, 8);
            fingerprint = GameFingerprint();
            transport = new LanTransport(delegate { return Time.realtimeSinceStartup; });
            transport.PacketReceived += Receive;
            transport.ConnectedEvent += delegate { if (Session != null) Session.Dispose(); Session = new DungeonSession(transport, gameObject, Log); Log("GT_DUNGEON_CONNECTED"); };
            transport.DisconnectedEvent += delegate { ClearAvatar(); if (Session != null) Session.Disconnected(); Log(transport.Status); };
            Log("GT_DUNGEON_READY 0.2.2; native rendering, host authority; game=" + fingerprint);
            Log("GT_PROFILE_MODE " + (UsesSeparateProfile ? "LAN" : "NORMAL"));
        }

        private static string GameFingerprint()
        {
            string managed = Path.Combine(Paths.GameRootPath, "EtG_Data/Managed");
            using (MemoryStream ms = new MemoryStream()) using (SHA256 sha = SHA256.Create()) {
                foreach (string file in new string[] { "Assembly-CSharp.dll", "Assembly-CSharp-firstpass.dll", "UnityEngine.CoreModule.dll" })
                    using (FileStream stream = File.OpenRead(Path.Combine(managed, file))) { byte[] digest = sha.ComputeHash(stream); ms.Write(digest, 0, digest.Length); }
                return BitConverter.ToString(sha.ComputeHash(ms.ToArray())).Replace("-", "").ToLowerInvariant();
            }
        }

        private void Update()
        {
            if (transport == null) return;
            if (Input.GetKeyDown(KeyCode.P) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) visible = !visible;
            transport.Poll();
            if (Session != null) { Session.MuteClientInput = visible; Session.Update(); }
            if (Session != null && Session.Phase == DungeonSession.RunPhase.Fault && smokeRole == null) visible = true;
            if (profileCheck) { TickProfileCheck(); return; }
            if (smokeRole != null) { TickSmoke(); return; }
            if (Session != null && Session.InRun) return;
            if (transport.Connected && Time.realtimeSinceStartup >= nextPosition) {
                nextPosition = Time.realtimeSinceStartup + 0.05f;
                GameManager gm = UnityEngine.Object.FindObjectOfType<GameManager>();
                if (gm != null && gm.IsFoyer && gm.PrimaryPlayer != null && gm.PrimaryPlayer.sprite != null) {
                    PlayerController p = gm.PrimaryPlayer;
                    transport.Send(new LanPlayerState { InFoyer = true, X = p.transform.position.x, Y = p.transform.position.y, Rotation = p.transform.eulerAngles.z, SpriteId = p.sprite.spriteId, FlipX = p.sprite.FlipX, Collection = p.sprite.Collection.name }.Encode());
                }
            }
        }

        private void Receive(byte[] bytes)
        {
            if (bytes.Length == 0) return;
            if (bytes[0] == 1) {
                if (Session != null && Session.InRun) return;
                LanPlayerState state = LanPlayerState.Decode(bytes);
                GameManager gm = UnityEngine.Object.FindObjectOfType<GameManager>();
                if (gm == null || !gm.IsFoyer || gm.PrimaryPlayer == null || gm.PrimaryPlayer.sprite == null || state.Collection != gm.PrimaryPlayer.sprite.Collection.name) return;
                if (foyerAvatar == null) foyerAvatar = RemotePlayerAvatar.Create(transport.IsHost ? 2UL : 1UL, new Vector2(state.X, state.Y), state.Rotation);
                foyerAvatar.Apply(new Vector2(state.X, state.Y), state.Rotation, state.FlipX, state.SpriteId);
            } else if (Session != null) { ClearAvatar(); if (bytes[0] == (byte)DungeonMessage.Level) visible = false; Session.Receive(bytes); }
        }
        private void ClearAvatar() { if (foyerAvatar != null) Destroy(foyerAvatar.gameObject); foyerAvatar = null; }

        private void OnGUI()
        {
            if (transport == null) return;
            if (Session != null && Session.Mirroring && Session.Snapshot != null) {
                WorldSnapshot snapshot = Session.Snapshot;
                GUI.Box(new Rect(15, 15, 390, 52), "Cultist | HP " + snapshot.Health.ToString("0.0") + " | Armor " + snapshot.Armor.ToString("0") + " | Ammo " + snapshot.Ammo + "\n" + (snapshot.Paused ? "Host paused" : "Ctrl+P menu; host controls pause and map"));
            }
            if (!visible) return;
            window = GUI.Window(27631, window, DrawWindow, "Gungeon Together 0.2.2 — dungeon synchronization alpha");
        }
        private void DrawWindow(int id)
        {
            if (!UsesSeparateProfile) {
                GUILayout.Label("Normal Steam save profile is active. Your usual progress is used.");
                GUILayout.Label("For LAN, close the game and use Start Modded in your LAN manager profile.");
                GUILayout.Label("LAN is disabled in this launch to protect your normal save profile.");
                GUI.DragWindow(new Rect(0, 0, 590, 20));
                return;
            }
            GUILayout.Label("Two games render locally. The host runs combat for both players.");
            GUILayout.Label("Experimental: use a clean game; a separate LAN save profile is used.");
            GUILayout.Label("Radmin IPv4 of host"); ip = GUILayout.TextField(ip, 64);
            GUILayout.Label("Listen IPv4 on host"); bind = GUILayout.TextField(bind, 64);
            GUILayout.BeginHorizontal(); GUILayout.Label("TCP port", GUILayout.Width(70)); port = GUILayout.TextField(port, 5, GUILayout.Width(70)); GUILayout.Label("Session code", GUILayout.Width(95)); code = GUILayout.TextField(code, 64); GUILayout.EndHorizontal();
            bool enabled = GUI.enabled;
            GUI.enabled = !transport.Active;
            GUILayout.BeginHorizontal(); if (GUILayout.Button("HOST")) Connect(true); if (GUILayout.Button("JOIN BY IP")) Connect(false); GUILayout.EndHorizontal();
            GUI.enabled = transport.Connected && transport.IsHost && Session != null && !Session.InRun;
            if (GUILayout.Button("START SYNCHRONIZED DUNGEON (host only)")) {
                try { ClearAvatar(); Session.StartHost(); visible = false; } catch (Exception ex) { error = ex.Message; Log(error); }
            }
            GUI.enabled = enabled;
            if (GUILayout.Button("DISCONNECT / RETURN TO FOYER")) ReturnToFoyer();
            GUILayout.Label(transport.Status);
            if (Session != null) GUILayout.Label(Session.Status);
            if (error.Length != 0) GUILayout.Label(error);
            GUILayout.Label("Client: your usual movement/aim/shoot/roll/reload/interact/blank keys.\nClient plays native Cultist. Host chooses pause, map and floor transitions.");
            GUI.DragWindow(new Rect(0, 0, 590, 20));
        }
        private void Connect(bool host)
        {
            try {
                if (!UsesSeparateProfile) throw new IOException("Start LAN using the updated GungeonTogether-LAN.exe; normal save profile is active.");
                int value;
                if (!int.TryParse(port, out value) || value < 1 || value > 65535) throw new IOException("Invalid TCP port.");
                GameManager gm = UnityEngine.Object.FindObjectOfType<GameManager>();
                if (gm == null || !gm.IsFoyer || gm.PrimaryPlayer == null) throw new IOException("Choose a character in the foyer before connecting.");
                if (host) transport.Host(bind.Trim(), value, code, fingerprint); else transport.Join(ip.Trim(), value, code, fingerprint);
                error = "";
            } catch (Exception ex) { error = ex.Message; }
        }
        private void ReturnToFoyer()
        {
            bool inRun = Session != null && Session.InRun;
            if (Session != null) { Session.Dispose(); Session = null; }
            transport.Dispose(); ClearAvatar(); error = "";
            if (inRun && GameManager.HasInstance) GameManager.Instance.LoadCharacterSelect(false, true);
        }
        private void Log(string text)
        {
            Logger.LogInfo(text);
            if (smokeRole != null) File.AppendAllText(Path.Combine(Paths.GameRootPath, "dungeon-smoke-" + smokeRole + ".txt"), text + "\n");
        }

        internal void ProfileInitialized()
        {
            if (!profileCheck || profilePathChecked) return;
            profilePathChecked = true;
            profileObservedPath = Path.GetFullPath(SaveManager.SavePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string expected = UsesSeparateProfile
                ? Path.Combine(Paths.GameRootPath, "BepInEx/GungeonTogetherLAN/dungeon-profile")
                : Application.persistentDataPath;
            expected = Path.GetFullPath(expected).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            bool matched = string.Equals(profileObservedPath, expected, StringComparison.OrdinalIgnoreCase);
            // The native Init has been checked, but all subsequent test loads/saves use
            // a fixture in this private game copy, never the real Windows save folder.
            string fixture = Path.Combine(Paths.GameRootPath, "BepInEx/GungeonTogetherLAN/profile-check-fixture");
            Directory.CreateDirectory(fixture);
            SaveManager.SavePath = fixture; SaveManager.OldSavePath = fixture;
            string result = "GT_PROFILE_PATH_" + (matched ? "PASS" : "FAIL") + " mode=" + (UsesSeparateProfile ? "LAN" : "NORMAL");
            File.WriteAllText(Path.Combine(Paths.GameRootPath, "profile-check-result.txt"), result + "\n");
            Logger.LogInfo(result);
        }

        private void TickProfileCheck()
        {
            if (!profilePathChecked || !GameManager.HasInstance || GameManager.Instance.IsLoadingLevel) return;
            string resultPath = Path.Combine(Paths.GameRootPath, "profile-check-result.txt");
            string result = File.ReadAllText(resultPath);
            if (result.IndexOf("GT_PROFILE_GAME_READY", StringComparison.Ordinal) < 0) {
                GameStatsManager loaded;
                bool valid = SaveManager.Load<GameStatsManager>(SaveManager.GameSave, out loaded, true, 0, null, null);
                File.AppendAllText(resultPath, "GT_PROFILE_SAVE_LOAD_" + (valid && loaded != null ? "PASS" : "FAIL") + "\n");
                File.AppendAllText(resultPath, "GT_PROFILE_GAME_READY\n");
                Logger.LogInfo("GT_PROFILE_GAME_READY");
            }
        }

        private void TickSmoke()
        {
            if (smokeComplete) return;
            try {
                if (Time.realtimeSinceStartup > smokeDeadline) throw new IOException("Smoke timed out: " + (Session == null ? transport.Status : Session.Status));
                GameManager gm = UnityEngine.Object.FindObjectOfType<GameManager>();
                if (gm == null || gm.Dungeon == null || Dungeonator.Dungeon.IsGenerating || gm.IsLoadingLevel) return;
                if (Time.realtimeSinceStartup >= nextSmokeDiagnostic) {
                    nextSmokeDiagnostic = Time.realtimeSinceStartup + 5;
                    Log("GT_SMOKE_STATE foyer=" + gm.IsFoyer + " loading=" + gm.IsLoadingLevel + " generating=" + Dungeonator.Dungeon.IsGenerating + " player=" + (gm.PrimaryPlayer != null) + " scene=" + (gm.GetLastLoadedLevelDefinition() == null ? "null" : gm.GetLastLoadedLevelDefinition().dungeonSceneName));
                }
                if (!InControl.InputManager.IsSetup) typeof(InControl.InputManager).GetMethod("SetupInternal", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
                if (gm.IsFoyer && gm.PrimaryPlayer == null) {
                    GameObject prefab = (GameObject)BraveResources.Load("PlayerRogue", ".prefab");
                    GameManager.PlayerPrefabForNewGame = prefab;
                    Dungeonator.RoomHandler room = gm.Dungeon.data.rooms[0];
                    Vector3 pos = new Vector3(room.area.basePosition.x + room.area.dimensions.x / 2f, room.area.basePosition.y + room.area.dimensions.y / 2f, 0);
                    gm.PrimaryPlayer = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity).GetComponent<PlayerController>();
                    gm.PrimaryPlayer.ForceChangeRoom(room);
                }
                if (!transport.Active && Session == null) {
                    if (smokeRole == "host") transport.Host("127.0.0.1", smokePort, "dungeon-smoke", fingerprint);
                    else transport.Join("127.0.0.1", smokePort, "dungeon-smoke", fingerprint);
                    Log("GT_DUNGEON_SMOKE_CONNECTING");
                }
                if (smokeRole == "host" && transport.Connected && Session != null && !smokeRunStarted) {
                    smokeRunStarted = true; Session.StartHost(); Log("GT_DUNGEON_SMOKE_RUN_STARTED");
                }
                if (Session != null && Session.Phase == DungeonSession.RunPhase.Fault) throw new IOException(Session.Status);
                if (Session != null && Session.Phase == DungeonSession.RunPhase.Running && !smokeLayout) {
                    smokeLayout = true; smokeStarted = Time.realtimeSinceStartup; Log("GT_DUNGEON_SMOKE_LAYOUT_PASS");
                    if (smokeRole == "host") {
                        smokeCultistStart = gm.SecondaryPlayer.transform.position;
                        smokeStartAmmo = gm.SecondaryPlayer.CurrentGun == null ? 0 : gm.SecondaryPlayer.CurrentGun.CurrentAmmo;
                    }
                }
                if (smokeRole == "client" && Session != null && Session.Snapshot != null && !smokeSnapshot) {
                    smokeSnapshot = true; Log("GT_DUNGEON_SMOKE_SNAPSHOT_PASS sprites=" + Session.Snapshot.Sprites.Count);
                }
                if (smokeLayout && smokeRole == "client") {
                    float elapsed = Time.realtimeSinceStartup - smokeStarted;
                    Session.SmokeControls = Session.Epoch == 1;
                    Session.SmokeMoveX = elapsed > 2 && elapsed < 2.4f ? 1 : 0;
                    Session.SmokeShoot = elapsed > 3 && elapsed < 12 && ((int)(elapsed * 6) % 2 == 0);
                }
                if (smokeLayout && smokeRole == "host" && Session.Epoch == 1 && !smokeEnemyCreated && Time.realtimeSinceStartup - smokeStarted > 3) {
                    smokeEnemyCreated = true;
                    AIActor prefab = EnemyDatabase.GetOrLoadByGuid("01972dee89fc4404a5c408d50007dad5");
                    PlayerController cultist = gm.SecondaryPlayer;
                    Vector2 point = cultist.specRigidbody.UnitCenter + Vector2.right * 2.5f;
                    smokeEnemy = AIActor.Spawn(prefab, point, cultist.CurrentRoom, true, AIActor.AwakenAnimationType.Default, true);
                    smokeEnemy.behaviorSpeculator.enabled = false;
                    smokeEnemyHealth = smokeEnemy.healthHaver.GetCurrentHealth();
                    Log("GT_DUNGEON_SMOKE_ENEMY_SPAWN health=" + smokeEnemyHealth + " guid=" + smokeEnemy.EnemyGuid);
                }
                if (smokeEnemyCreated && !smokeDamagePassed && (smokeEnemy == null || smokeEnemy.healthHaver.GetCurrentHealth() < smokeEnemyHealth)) {
                    smokeDamagePassed = true; Log("GT_DUNGEON_SMOKE_DAMAGE_PASS: native remote-player projectile damaged enemy");
                }
                if (smokeLayout && smokeRole == "host" && Time.realtimeSinceStartup - smokeStarted > 3) {
                    PlayerController cultist = gm.SecondaryPlayer;
                    float distance = Vector3.Distance(smokeCultistStart, cultist.transform.position);
                    if (distance > 0.2f && !smokeMovePassed) { smokeMovePassed = true; Log("GT_DUNGEON_SMOKE_REMOTE_MOVE_PASS distance=" + distance); }
                    foreach (Projectile projectile in UnityEngine.Object.FindObjectsOfType<Projectile>())
                        if (projectile.Owner == cultist && !smokeShotPassed) { smokeShotPassed = true; Log("GT_DUNGEON_SMOKE_REMOTE_SHOOT_PASS native projectile owned by Cultist"); }
                    if ((!smokeMovePassed || !smokeShotPassed) && Time.realtimeSinceStartup >= nextControlsDiagnostic) {
                        nextControlsDiagnostic = Time.realtimeSinceStartup + 1;
                        Log("GT_SMOKE_CONTROLS move=" + distance + " ammo=" + (cultist.CurrentGun == null ? -1 : cultist.CurrentGun.CurrentAmmo) + " activeShoot=" + Session.Controls.Actions.ShootAction.Value + " activeMove=" + Session.Controls.Actions.Move.Vector + " input=" + cultist.CurrentInputState + " override=" + cultist.IsInputOverridden + " paused=" + gm.IsPaused + " delta=" + BraveTime.DeltaTime + " packetSeq=" + Session.Controls.LastInputSequence + " packetMove=" + Session.Controls.LastMoveX + " attached=" + Session.Controls.Device.IsAttached);
                    }
                }
                if (smokeLayout && !smokeCaptureRequested && Time.realtimeSinceStartup - smokeStarted > 9) {
                    smokeCaptureRequested = true;
                    ScreenCapture.CaptureScreenshot(Path.Combine(Paths.GameRootPath, "dungeon-smoke-" + smokeRole + ".png"));
                }
                if (smokeLayout && smokeRole == "host" && !smokeSecondFloor && Time.realtimeSinceStartup - smokeStarted > 13) {
                    if (!smokeMovePassed || !smokeShotPassed || !smokeDamagePassed) throw new IOException("Native remote move/fire/damage test failed.");
                    smokeSecondFloor = true;
                    Log("GT_DUNGEON_SMOKE_TRANSITION_STARTED");
                    gm.LoadCustomLevel("tt5");
                }
                if (smokeLayout && Session != null && Session.Epoch >= 2 && Session.Phase == DungeonSession.RunPhase.Running && (smokeRole == "host" || (Session.Snapshot != null && Session.Snapshot.Epoch >= 2))) {
                    if (smokeSecondFloorAt == 0) { smokeSecondFloorAt = Time.realtimeSinceStartup; Log("GT_DUNGEON_SMOKE_SECOND_FLOOR_SNAPSHOT_READY"); }
                    if (Time.realtimeSinceStartup - smokeSecondFloorAt < 3) return;
                    Log("GT_DUNGEON_SMOKE_SECOND_FLOOR_PASS");
                    Log("GT_DUNGEON_SMOKE_PASS"); smokeComplete = true; smokeDeadline = float.MaxValue;
                }
            } catch (Exception ex) { Log("GT_DUNGEON_SMOKE_FAIL: " + ex); Application.Quit(); smokeDeadline = float.MaxValue; }
        }

        private void OnDestroy()
        {
            if (Session != null) Session.Dispose();
            if (transport != null) transport.Dispose();
            if (harmony != null) harmony.UnpatchSelf();
            Instance = null; ClearAvatar();
        }
    }
}


