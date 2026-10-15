using System;
using System.IO;
using UnityEngine;

namespace GungeonTogether.Lan
{
    public sealed class DungeonSession : IDisposable
    {
        public enum RunPhase { Lobby, Generating, AwaitingClient, Running, Fault }
        private readonly LanTransport transport;
        private readonly GameObject timeOwner;
        private readonly Action<string> log;
        private SpriteReplication sprites = new SpriteReplication();
        private RemoteControls controls;
        private string scene = "tt_castle", hash = "";
        private string generationContext, clientOriginalContext;
        private int epoch, seed;
        private uint inputSequence, snapshotSequence, lastSnapshot;
        private float deadline, nextInput, nextSnapshot;
        private bool loadingCommand;
        private int hostCharacter = 1;
        public int GuestCharacter { get; private set; }
        public bool GuestChoiceReceived { get; private set; }
        private GameObject clientOriginalPrefab;
        public RunPhase Phase { get; private set; }
        public bool IsHost { get { return transport.IsHost; } }
        public bool Mirroring { get { return !IsHost && Phase == RunPhase.Running; } }
        public bool InRun { get { return Phase != RunPhase.Lobby; } }
        public string Status { get; private set; }
        public int Epoch { get { return epoch; } }
        public int Seed { get { return seed; } }
        public RemoteControls Controls { get { return controls; } }
        public WorldSnapshot Snapshot { get { return sprites.LastSnapshot; } }
        public int RenderedFrames { get { return sprites.RenderedFrames; } }
        public int InterpolatedFrames { get { return sprites.InterpolatedFrames; } }
        public bool MuteClientInput;
        public bool SmokeControls;
        public float SmokeMoveX;
        public bool SmokeShoot;

        public DungeonSession(LanTransport transport, GameObject timeOwner, Action<string> log)
        { this.transport = transport; this.timeOwner = timeOwner; this.log = log; Phase = RunPhase.Lobby; Status = "Connected: host can start synchronized dungeon"; }

        public void ChooseGuest(int character)
        {
            if (IsHost || Phase != RunPhase.Lobby || !CharacterCatalog.ValidGuest(character)) return;
            GuestCharacter = character;
            Send(new LobbyChoice { Character = character }.Encode());
        }

        public void StartHost()
        {
            if (!IsHost || !transport.Connected || Phase != RunPhase.Lobby) throw new IOException("Connect two PCs in the foyer first.");
            GameManager gm = GameManager.Instance;
            if (!gm.IsFoyer || gm.PrimaryPlayer == null) throw new IOException("Start from the foyer with a selected character.");
            if (!GuestChoiceReceived) throw new IOException("Wait for the friend's character selection.");
            hostCharacter = CharacterCatalog.FromIdentity(gm.PrimaryPlayer.characterIdentity.ToString());
            controls = new RemoteControls();
            NativeWorld.EnsureGuest(GuestCharacter); controls.Bind(); controls.Enabled = false;
            generationContext = NativeWorld.ExportGenerationContext();
            epoch = 1; seed = UnityEngine.Random.Range(1, 999999999); scene = "tt_castle";
            if (DungeonPlugin.Instance != null && DungeonPlugin.Instance.IsSmoke) foreach (string arg in Environment.GetCommandLineArgs()) {
                int value;
                if (arg.StartsWith("--gt-smoke-seed=", StringComparison.Ordinal) && int.TryParse(arg.Substring("--gt-smoke-seed=".Length), out value) && value > 0) seed = value;
            }
            SetGenerating(); NativeWorld.DepartFoyer(); gm.InitializeForRunWithSeed(seed); gm.LoadCustomLevel(scene);
        }

        private void SetGenerating()
        {
            Phase = RunPhase.Generating; deadline = Time.realtimeSinceStartup + 120;
            Status = "Generating shared dungeon (epoch " + epoch + ")";
            if (controls != null) controls.Enabled = false;
            BraveTime.SetTimeScaleMultiplier(1f, timeOwner);
            foreach (PlayerController p in GameManager.Instance.AllPlayers) if (p != null) p.SetInputOverride("GT_LevelSync");
        }

        public void HostLevelLoading(string newScene)
        {
            if (!IsHost || !InRun || Phase == RunPhase.Fault || string.IsNullOrEmpty(newScene)) return;
            if (Phase == RunPhase.Generating && scene == newScene) return;
            if (Phase != RunPhase.Running) return;
            epoch++; scene = newScene;
            generationContext = NativeWorld.ExportGenerationContext();
            seed = (int)(((long)seed * 1103515245 + 12345) % 999999998) + 1;
            GameManager.Instance.InitializeForRunWithSeed(seed);
            sprites.Dispose(); sprites = new SpriteReplication(); hash = "";
            SetGenerating();
            log("GT_DUNGEON_TRANSITION epoch=" + epoch + " scene=" + scene);
        }

        public void Update()
        {
            if (Phase == RunPhase.Lobby || Phase == RunPhase.Fault) return;
            try {
                GameManager gm = GameManager.Instance;
                if ((Phase == RunPhase.Generating || Phase == RunPhase.AwaitingClient) && Time.realtimeSinceStartup > deadline)
                    throw new IOException("Dungeon loading/verification timed out.");
                if (Phase == RunPhase.Generating && !gm.IsLoadingLevel && !gm.IsFoyer && gm.Dungeon != null && !Dungeonator.Dungeon.IsGenerating && gm.PrimaryPlayer != null && gm.GetLastLoadedLevelDefinition() != null && gm.GetLastLoadedLevelDefinition().dungeonSceneName == scene)
                    LevelLoaded();
                if (Phase != RunPhase.Running) return;
                if (gm.IsLoadingLevel) return;
                if (gm.IsFoyer) { Fail("The run returned to the foyer; reconnect before starting another run."); return; }
                if (IsHost) {
                    NativeWorld.EnsureGuest(GuestCharacter); controls.Bind(); controls.Enabled = true;
                    if (Time.realtimeSinceStartup >= nextSnapshot) {
                        nextSnapshot = Time.realtimeSinceStartup + 0.05f;
                        WorldSnapshot snapshot = sprites.Capture(epoch, ++snapshotSequence, Send);
                        Send(snapshot.Encode());
                    }
                } else {
                    sprites.SuppressLocalSimulation();
                    if (Time.realtimeSinceStartup >= nextInput) {
                        nextInput = Time.realtimeSinceStartup + 1f / 60f;
                        Send(CaptureInput().Encode());
                    }
                }
            } catch (Exception ex) { Fail(ex.Message); }
        }

        public void LateUpdate()
        {
            if (!Mirroring || GameManager.Instance.IsLoadingLevel) return;
            try { sprites.Render(Time.realtimeSinceStartup); }
            catch (Exception ex) { Fail(ex.Message); }
        }

        private RemoteInput CaptureInput()
        {
            RemoteInput input = new RemoteInput { Epoch = epoch, Sequence = ++inputSequence };
            if (SmokeControls) { input.MoveX = SmokeMoveX; input.AimX = 1; input.Buttons = SmokeShoot ? 1UL << 8 : 0; return input; }
            if (MuteClientInput || !Application.isFocused) return input;
            BraveInput brave = BraveInput.GetInstanceForPlayer(0);
            if (brave == null || brave.ActiveActions == null) return input;
            GungeonActions a = brave.ActiveActions;
            Vector2 move = Vector2.ClampMagnitude(a.Move.Vector, 1);
            Vector2 aim = a.Aim.Vector;
            if (brave.IsKeyboardAndMouse(false)) {
                Camera camera = GameManager.Instance.MainCameraController.Camera;
                Vector3 point = camera.ScreenToWorldPoint(Input.mousePosition);
                aim = new Vector2(point.x, point.y) - sprites.RemotePlayerPosition;
            }
            aim = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
            input.MoveX = move.x; input.MoveY = move.y; input.AimX = aim.x; input.AimY = aim.y;
            for (int i = 8; i < 41; i++) {
                // Pause, map/equipment menus and character-specific punchout are host-only in this alpha.
                if (i == 13 || i == 19 || i == 26 || i >= 34) continue;
                InControl.PlayerAction action = a.GetActionFromType((GungeonActions.GungeonActionType)i);
                if (action != null && action.IsPressed) input.Buttons |= 1UL << i;
            }
            return input;
        }

        private void LevelLoaded()
        {
            string localHash = NativeWorld.LayoutHash(GameManager.Instance.Dungeon);
            NativeWorld.EnableWeapons();
            log("GT_DUNGEON_LAYOUT epoch=" + epoch + " hash=" + localHash);
            if (IsHost) {
                hash = localHash; controls.Bind(); controls.Enabled = false;
                BraveTime.SetTimeScaleMultiplier(0f, timeOwner);
                Phase = RunPhase.AwaitingClient; deadline = Time.realtimeSinceStartup + 120;
                Send(new LevelCommand { Epoch = epoch, Seed = seed, Scene = scene, LayoutHash = hash, GenerationData = generationContext, HostCharacter = hostCharacter, GuestCharacter = GuestCharacter }.Encode());
                Status = "Host dungeon generated; waiting for identical client layout";
            } else {
                if (localHash != hash) throw new IOException("Dungeon layouts differ. Run stopped; use identical clean LAN profiles and game files.");
                NativeWorld.WarmAssets();
                BraveTime.SetTimeScaleMultiplier(1f, timeOwner);
                Phase = RunPhase.Running;
                sprites.SuppressLocalSimulation();
                Send(Wire.Write(DungeonMessage.Ready, delegate(BinaryWriter w) { w.Write(epoch); w.Write(hash); }));
                Status = "Synchronized dungeon: controlling " + CharacterCatalog.Prefab(GuestCharacter);
                log("GT_DUNGEON_CLIENT_READY epoch=" + epoch);
            }
        }

        public void Receive(byte[] bytes)
        {
            if (bytes.Length == 0) throw new IOException("Empty dungeon packet.");
            DungeonMessage type = (DungeonMessage)bytes[0];
            if (type == DungeonMessage.Stop) {
                string reason = Wire.Read(bytes, DungeonMessage.Stop, delegate(BinaryReader r) { return Wire.String(r, 1024); });
                Fail("Friend stopped: " + reason, false); return;
            }
            if (IsHost) {
                if (type == DungeonMessage.Character) {
                    LobbyChoice choice = LobbyChoice.Decode(bytes);
                    if (Phase == RunPhase.Lobby) { GuestCharacter = choice.Character; GuestChoiceReceived = true; log("GT_GUEST_CHARACTER " + CharacterCatalog.Prefab(GuestCharacter)); }
                } else if (type == DungeonMessage.Input) {
                    RemoteInput input = RemoteInput.Decode(bytes);
                    if (input.Epoch == epoch && Phase == RunPhase.Running) controls.Apply(input);
                } else if (type == DungeonMessage.Ready) {
                    string result = Wire.Read(bytes, DungeonMessage.Ready, delegate(BinaryReader r) { int receivedEpoch = r.ReadInt32(); string receivedHash = Wire.String(r, 64); return receivedEpoch == epoch ? receivedHash : ""; });
                    if (Phase == RunPhase.AwaitingClient && result == hash) {
                        Phase = RunPhase.Running; controls.Enabled = true;
                        foreach (PlayerController p in GameManager.Instance.AllPlayers) if (p != null) p.ClearInputOverride("GT_LevelSync");
                        BraveTime.SetTimeScaleMultiplier(1f, timeOwner);
                        Status = "Synchronized dungeon: native two-player co-op (host authority)";
                        log("GT_DUNGEON_RUNNING epoch=" + epoch);
                    }
                } else throw new IOException("Client sent a host-only packet.");
            } else {
                if (type == DungeonMessage.Level) BeginClientLevel(LevelCommand.Decode(bytes));
                else if (type == DungeonMessage.Collection) { CollectionInfo info = CollectionInfo.Decode(bytes); if (info.Epoch == epoch) sprites.Define(info); }
                else if (type == DungeonMessage.Snapshot) {
                    WorldSnapshot snapshot = WorldSnapshot.Decode(bytes);
                    if (snapshot.Epoch == epoch && Phase == RunPhase.Running && Wire.Newer(snapshot.Sequence, lastSnapshot)) { sprites.Enqueue(snapshot); lastSnapshot = snapshot.Sequence; }
                } else throw new IOException("Host sent an unsupported packet.");
            }
        }

        private void BeginClientLevel(LevelCommand command)
        {
            if (command.Epoch <= epoch || Phase == RunPhase.Fault) return;
            epoch = command.Epoch; seed = command.Seed; scene = command.Scene; hash = command.LayoutHash; lastSnapshot = 0;
            sprites.Dispose(); sprites = new SpriteReplication();
            GameManager gm = GameManager.Instance;
            if (clientOriginalContext == null) clientOriginalContext = NativeWorld.ExportGenerationContext();
            if (epoch == 1) clientOriginalPrefab = GameManager.LastUsedPlayerPrefab;
            if (!string.IsNullOrEmpty(command.GenerationData)) NativeWorld.ImportGenerationContext(command.GenerationData);
            hostCharacter = command.HostCharacter; GuestCharacter = command.GuestCharacter;
            NativeWorld.PrepareClientPlayers(hostCharacter, GuestCharacter);
            NativeWorld.DepartFoyer();
            // Client controllers are asset/lighting placeholders, not authoritative inventory.
            // Recreate them for a new floor instead of carrying old physics-tree proxies.
            gm.InitializeForRunWithSeed(seed);
            // Seed setup evaluates run injection prerequisites against players.
            // Keep both controllers available for that step, then remove stale
            // client physics objects before the new scene starts loading.
            if (epoch > 1) gm.ClearPlayers();
            SetGenerating(); loadingCommand = true;
            try { gm.LoadCustomLevel(scene); } finally { loadingCommand = false; }
        }

        public bool CanClientLoadLevel { get { return IsHost || !InRun || loadingCommand; } }
        private void Send(byte[] bytes) { transport.Send(bytes); }

        public void Disconnected() { if (InRun) Fail("Network connection lost. The host run is paused; return to the foyer to recover.", false); }
        private void Fail(string reason, bool notify = true)
        {
            if (Phase == RunPhase.Fault) return;
            Phase = RunPhase.Fault; Status = reason;
            if (controls != null) controls.Reset();
            BraveTime.SetTimeScaleMultiplier(0f, timeOwner);
            log("GT_DUNGEON_STOP: " + reason);
            if (notify && transport.Connected) {
                try { Send(Wire.Write(DungeonMessage.Stop, delegate(BinaryWriter w) { w.Write(reason.Length > 1024 ? reason.Substring(0, 1024) : reason); })); } catch { }
            }
        }

        public void Dispose()
        {
            if (controls != null) { controls.Dispose(); controls = null; }
            sprites.Dispose();
            if (!IsHost && clientOriginalContext != null) NativeWorld.ImportGenerationContext(clientOriginalContext);
            if (!IsHost && clientOriginalPrefab != null) GameManager.PlayerPrefabForNewGame = clientOriginalPrefab;
            BraveTime.SetTimeScaleMultiplier(1f, timeOwner);
            if (GameManager.HasInstance) foreach (PlayerController player in GameManager.Instance.AllPlayers) if (player != null) player.ClearInputOverride("GT_LevelSync");
            Phase = RunPhase.Lobby;
        }
    }
}

