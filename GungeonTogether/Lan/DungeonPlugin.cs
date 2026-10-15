using System;
using System.IO;
using System.Security.Cryptography;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using GungeonTogether.Networking;

namespace GungeonTogether.Lan
{
    [BepInPlugin("com.gungeontogether.lan", "Gungeon Together LAN Alpha", "0.3.0")]
    [BepInIncompatibility("com.llamerrr.gungeontogether")]
    public sealed partial class DungeonPlugin : BaseUnityPlugin
    {
        public static DungeonPlugin Instance;
        public DungeonSession Session { get; private set; }
        private LanTransport transport;
        private Harmony harmony;
        private string ip, bind, port, code, fingerprint, error = "";
        private Rect window = new Rect(36, 36, 620, 586);
        private bool visible = true;
        private RemotePlayerAvatar foyerAvatar;
        private float nextPosition;
        private uint foyerSequence;
        private int guestChoice;
        private PlayerController menuPlayer;
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
        private DesktopSmoke desktopSmoke;
        private float smokeLobbyAt;
        private bool smokeFoyerPassed, smokeDesktopPassed, smokeMenuRequested, smokeMenuVisible;

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
                string privateSaves = Path.Combine(Paths.GameRootPath, "BepInEx/GungeonTogetherLAN/dungeon-profile");
                Directory.CreateDirectory(privateSaves);
                SaveManager.SavePath = privateSaves; SaveManager.OldSavePath = privateSaves;
                desktopSmoke = gameObject.AddComponent<DesktopSmoke>();
            }
            if (smokeRole != null) {
                Foyer.DoIntroSequence = false; Foyer.DoMainMenu = false; visible = false; smokeDeadline = Time.realtimeSinceStartup + 150;
            }
            harmony = new Harmony("com.gungeontogether.lan.dungeon-0.3.0");
            try { harmony.PatchAll(typeof(DungeonPlugin).Assembly); }
            catch (Exception ex) {
                if (IsSmoke) { Log("GT_DUNGEON_SMOKE_FAIL: patch initialization: " + ex); Application.Quit(); }
                throw;
            }
            ip = Config.Bind("LAN", "HostIP", "127.0.0.1", "Host's Radmin IPv4.").Value;
            bind = Config.Bind("LAN", "BindIP", "0.0.0.0", "Host's own Radmin IPv4 or all interfaces.").Value;
            port = Config.Bind("LAN", "Port", LanTransport.DefaultPort, "TCP port.").Value.ToString();
            guestChoice = Config.Bind("LAN", "GuestCharacter", 0, "Guest run character: 0 Cultist, 1 Pilot, 2 Marine, 3 Convict, 4 Hunter.").Value;
            if (!CharacterCatalog.ValidGuest(guestChoice)) guestChoice = 0;
            foreach (string arg in Environment.GetCommandLineArgs()) {
                if (smokeRole != null && arg.StartsWith("--gt-smoke-character=", StringComparison.Ordinal)) {
                    int choice;
                    if (int.TryParse(arg.Substring("--gt-smoke-character=".Length), out choice) && CharacterCatalog.ValidGuest(choice)) guestChoice = choice;
                }
            }
            code = Guid.NewGuid().ToString("N").Substring(0, 8);
            fingerprint = GameFingerprint();
            transport = new LanTransport(delegate { return Time.realtimeSinceStartup; });
            transport.PacketReceived += Receive;
            transport.ConnectedEvent += delegate { ClearAvatar(); foyerSequence = 0; if (Session != null) Session.Dispose(); Session = new DungeonSession(transport, gameObject, Log); if (!transport.IsHost) Session.ChooseGuest(guestChoice); Log("GT_DUNGEON_CONNECTED"); };
            transport.DisconnectedEvent += delegate { ClearAvatar(); if (Session != null) Session.Disconnected(); Log(transport.Status); };
            Log("GT_DUNGEON_READY 0.3.0; buffered rendering, character selection; game=" + fingerprint);
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
            if (visible && Input.GetKeyDown(KeyCode.Escape)) visible = false;
            UpdateMenuInput();
            transport.Poll();
            if (Session != null) { Session.MuteClientInput = visible; Session.Update(); }
            if (Session != null && Session.Phase == DungeonSession.RunPhase.Fault && smokeRole == null) visible = true;
            if (profileCheck) { TickProfileCheck(); return; }
            if (smokeRole != null) TickSmoke();
            if (Session != null && Session.InRun) return;
            if (transport.Connected && Time.realtimeSinceStartup >= nextPosition) {
                nextPosition = Time.realtimeSinceStartup + 0.05f;
                GameManager gm = UnityEngine.Object.FindObjectOfType<GameManager>();
                if (gm != null && gm.IsFoyer && gm.PrimaryPlayer != null && gm.PrimaryPlayer.sprite != null) {
                    PlayerController p = gm.PrimaryPlayer;
                    Vector3 position = p.sprite.transform.position, scale = p.sprite.transform.lossyScale;
                    transport.Send(new LanPlayerState { InFoyer = true, Sequence = ++foyerSequence, SentAt = Time.realtimeSinceStartup, X = position.x, Y = position.y, Z = position.z, Height = p.sprite.HeightOffGround, ScaleX = scale.x * p.sprite.scale.x, ScaleY = scale.y * p.sprite.scale.y, Rotation = p.sprite.transform.eulerAngles.z, SpriteId = p.sprite.spriteId, FlipX = p.sprite.FlipX, Collection = p.sprite.Collection.name, CollectionGuid = p.sprite.Collection.spriteCollectionGUID ?? "", Character = CharacterCatalog.FromIdentity(p.characterIdentity.ToString()) }.Encode());
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
                if (!state.InFoyer || gm == null || !gm.IsFoyer || gm.PrimaryPlayer == null) return;
                if (foyerAvatar == null) foyerAvatar = RemotePlayerAvatar.Create(transport.IsHost ? 2UL : 1UL, new Vector2(state.X, state.Y), state.Rotation);
                foyerAvatar.Apply(state);
            } else if (Session != null) { if (bytes[0] == (byte)DungeonMessage.Level) { ClearAvatar(); visible = false; } Session.Receive(bytes); }
        }
        private void ClearAvatar() { if (foyerAvatar != null) Destroy(foyerAvatar.gameObject); foyerAvatar = null; }

        private void LateUpdate() { if (Session != null) Session.LateUpdate(); }

        private void UpdateMenuInput()
        {
            PlayerController player = GameManager.HasInstance ? GameManager.Instance.PrimaryPlayer : null;
            if (menuPlayer != null && (menuPlayer != player || !visible || !UsesSeparateProfile)) menuPlayer.ClearInputOverride("GT_Menu");
            menuPlayer = player;
            if (menuPlayer != null && visible && UsesSeparateProfile && !IsSmoke) menuPlayer.SetInputOverride("GT_Menu");
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
                if (desktopSmoke != null && desktopSmoke.Verified && !smokeDesktopPassed) {
                    smokeDesktopPassed = true;
                    Log("GT_DESKTOP_SAFE_PASS fullscreen=" + Screen.fullScreen + " size=" + Screen.width + "x" + Screen.height + " cursor=" + Cursor.lockState + " background=" + Application.runInBackground);
                }
                if (Time.realtimeSinceStartup >= nextSmokeDiagnostic) {
                    nextSmokeDiagnostic = Time.realtimeSinceStartup + 5;
                    Log("GT_SMOKE_STATE foyer=" + gm.IsFoyer + " loading=" + gm.IsLoadingLevel + " generating=" + Dungeonator.Dungeon.IsGenerating + " player=" + (gm.PrimaryPlayer != null) + " scene=" + (gm.GetLastLoadedLevelDefinition() == null ? "null" : gm.GetLastLoadedLevelDefinition().dungeonSceneName));
                }
                if (!InControl.InputManager.IsSetup) typeof(InControl.InputManager).GetMethod("SetupInternal", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
                if (gm.IsFoyer && gm.PrimaryPlayer == null) {
                    GameObject prefab = NativeWorld.CharacterPrefab(smokeRole == "host" ? 1 : 4);
                    GameManager.PlayerPrefabForNewGame = prefab;
                    Dungeonator.RoomHandler room = gm.Dungeon.data.rooms[0];
                    Vector3 pos = new Vector3(room.area.basePosition.x + room.area.dimensions.x / 2f, room.area.basePosition.y + room.area.dimensions.y / 2f, 0);
                    gm.PrimaryPlayer = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity).GetComponent<PlayerController>();
                    gm.PrimaryPlayer.PlayerIDX = 0;
                    gm.PrimaryPlayer.ForceChangeRoom(room);
                }
                if (!transport.Active && Session == null) {
                    if (smokeRole == "host") transport.Host("127.0.0.1", smokePort, "dungeon-smoke", fingerprint);
                    else transport.Join("127.0.0.1", smokePort, "dungeon-smoke", fingerprint);
                    Log("GT_DUNGEON_SMOKE_CONNECTING");
                }
                if (transport.Connected && gm.IsFoyer && Session != null && !Session.InRun) {
                    if (smokeLobbyAt == 0) smokeLobbyAt = Time.realtimeSinceStartup;
                    float lobbyTime = Time.realtimeSinceStartup - smokeLobbyAt;
                    gm.PrimaryPlayer.usingForcedInput = lobbyTime > 0.3f && lobbyTime < 1.0f;
                    gm.PrimaryPlayer.forcedInput = new Vector2(smokeRole == "host" ? 1 : -1, 0);
                    if (lobbyTime > 1.7f && !smokeFoyerPassed) {
                        if (foyerAvatar == null || foyerAvatar.RenderedFrames < 10 || foyerAvatar.DistanceMoved < 0.2f) throw new IOException("Foyer interpolation did not render remote movement.");
                        smokeFoyerPassed = true;
                        Log("GT_FOYER_INTERPOLATION_PASS frames=" + foyerAvatar.RenderedFrames + " distance=" + foyerAvatar.DistanceMoved + " collection=" + foyerAvatar.CollectionName);
                    }
                    if (lobbyTime > 1.8f && !smokeMenuRequested) { smokeMenuRequested = true; StartCoroutine(CaptureSmokeMenu()); }
                }
                if (smokeRole == "host" && transport.Connected && Session != null && Session.GuestChoiceReceived && smokeFoyerPassed && Time.realtimeSinceStartup - smokeLobbyAt > 3 && !smokeRunStarted) {
                    gm.PrimaryPlayer.usingForcedInput = false;
                    smokeRunStarted = true; Session.StartHost(); Log("GT_DUNGEON_SMOKE_RUN_STARTED");
                }
                if (Session != null && Session.Phase == DungeonSession.RunPhase.Fault) throw new IOException(Session.Status);
                if (Session != null && Session.Phase == DungeonSession.RunPhase.Running && !smokeLayout) {
                    smokeLayout = true; smokeStarted = Time.realtimeSinceStartup; Log("GT_DUNGEON_SMOKE_LAYOUT_PASS");
                    if (smokeRole == "host") {
                        smokeCultistStart = gm.SecondaryPlayer.transform.position;
                        smokeStartAmmo = gm.SecondaryPlayer.CurrentGun == null ? 0 : gm.SecondaryPlayer.CurrentGun.CurrentAmmo;
                    }
                    VerifySmokeCharacters(gm);
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
                        if (projectile.Owner == cultist && !smokeShotPassed) { smokeShotPassed = true; Log("GT_DUNGEON_SMOKE_REMOTE_SHOOT_PASS native projectile owned by selected guest"); }
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
                    VerifySmokeCharacters(gm);
                    if (!smokeDesktopPassed) throw new IOException("Desktop-safe window verification did not pass.");
                    if (smokeRole == "client" && Session.InterpolatedFrames < 10) throw new IOException("Dungeon render interpolation did not execute.");
                    if (smokeRole == "client") Log("GT_DUNGEON_INTERPOLATION_PASS renderFrames=" + Session.RenderedFrames + " interpolated=" + Session.InterpolatedFrames);
                    Log("GT_DUNGEON_SMOKE_SECOND_FLOOR_PASS");
                    Log("GT_DUNGEON_SMOKE_PASS"); smokeComplete = true; smokeDeadline = float.MaxValue;
                }
            } catch (Exception ex) { Log("GT_DUNGEON_SMOKE_FAIL: " + ex); Application.Quit(); smokeDeadline = float.MaxValue; }
        }

        private void VerifySmokeCharacters(GameManager gm)
        {
            if (gm.AllPlayers.Length != 2 || gm.SecondaryPlayer == null || CharacterCatalog.FromIdentity(gm.SecondaryPlayer.characterIdentity.ToString()) != Session.GuestCharacter)
                throw new IOException("Selected guest character was not retained across generation.");
            if (gm.PrimaryPlayer.characterIdentity != PlayableCharacters.Pilot) throw new IOException("Host character identity differs between PCs.");
            if (gm.SecondaryPlayer.CurrentGun == null) throw new IOException("Selected guest has no starting gun.");
            Log("GT_CHARACTER_PASS epoch=" + Session.Epoch + " identity=" + gm.SecondaryPlayer.characterIdentity + " gun=" + gm.SecondaryPlayer.CurrentGun.PickupObjectId);
        }

        private System.Collections.IEnumerator CaptureSmokeMenu()
        {
            hostTab = smokeRole == "host"; visible = true; smokeMenuVisible = true;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Paths.GameRootPath, "menu-smoke-" + smokeRole + ".png"));
            yield return new WaitForEndOfFrame();
            visible = false; smokeMenuVisible = false;
            Log("GT_MENU_CAPTURED");
        }

        private void OnDestroy()
        {
            if (menuPlayer != null) menuPlayer.ClearInputOverride("GT_Menu");
            if (Session != null) Session.Dispose();
            if (transport != null) transport.Dispose();
            if (harmony != null) harmony.UnpatchSelf();
            Instance = null; ClearAvatar();
        }
    }
}


