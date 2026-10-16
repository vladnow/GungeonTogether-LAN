# Validation of 0.3.5 alpha

Date: 2026-10-16. **Runtime SHA-256: `96c874eecead6eb57a668b3e3f605938900cff86ad33c7af62feb38a32924dbf`.** All four final scenarios used this exact DLL. Historical reports remain separate.

**Early-floor alpha: the fourth-floor Sell Creep layout mismatch is unresolved. Full natural campaign completion is not established.**

## Implemented change

The guest receives a compact 75-byte motion payload independently from full world snapshots. Motion and input are scheduled up to 60 Hz, with full world capture up to 30 Hz after native physics. Acknowledgements refer to input consumed by the native controller. The scheduler preserves cadence through ordinary frame jitter and skips missed ticks without catch-up bursts.

Ordinary walking can replay unacknowledged input visually, bounded to 100 ms / 0.65 game units. Stale motion, pause, input overrides, rolls, room changes and teleports suppress/reset speculation. Terrain/room checks and native blocking-body overlap queries clip the visual lead. Body, hands, gun, attached outlines and guest camera share the presented offset. Native gameplay, damage and interactions remain on the host. The Network / ClientMovementPrediction config setting disables speculation while keeping the fast authoritative stream.

Other world objects use interpolation initially set to 40 ms, adapting within 35-90 ms. TCP, latest-state coalescing and earlier synchronization/resource checks remain. UDP and world delta compression are not implemented; higher world frequency may increase total bandwidth.

## Automated checks

**298 checks passed:** transport 33, dungeon protocol 85, profile policy 19, rendering/characters 38, display migration 9, input acknowledgements 9, sprite visibility 20, constrained real-socket transport 24, HUD/audio presentation 30, and responsiveness 31.

New coverage includes motion encoding and invalid fields, ownership flags, protocol version rejection, frame-jitter send cadence, no catch-up bursts, input replay and acknowledgement removal, stale epochs/authority, action cooldowns, reset paths, bounded history/distance, configuration disable and adaptive interpolation under stable and jittered arrivals.

**38 exact native binding contracts passed** against installed Steam Enter the Gungeon 2.1.9 libraries: 24 declarative Harmony targets, all twelve Wwise PostEvent overloads and both SetSwitch overloads. Production code was compiled against real installed game/BepInEx libraries, not stub types.

## Local movement comparison

Two native game processes ran over loopback. After the forced second-floor transition, the probe placed both players on a checked clear lane, disabled enemy AI/damage and alternated twelve short walking starts. The metric is elapsed time from the synthetic guest input sample to at least 0.0625 game units of displacement in the presented guest position. It is not hardware input-to-photon latency, damage confirmation timing or a Radmin measurement.

| Runtime / guest | Trials | Median | p95 |
| --- | ---: | ---: | ---: |
| 0.3.4 / Pilot | 12 | 80.456 ms | 91.576 ms |
| 0.3.5 / Pilot | 12 | 11.738 ms | 29.060 ms |
| 0.3.5 / Hunter | 12 | 12.424 ms | 28.130 ms |

The baseline used the released 0.3.4 DLL with SHA-256 `d3594272f979a4c03d0b70a82a7c8f89f4b92976c2209281a8b7913ff85fce61`. The final 0.3.5 cases observed nonzero predicted lead in 301 and 317 client frames respectively, with seven guest-owned sprite parts. This is a small controlled comparison on one machine; frame rate, load and real network behavior vary.

Both final movement scenarios checked native terrain queries at wall and pit boundaries, native host pause/input override/roll states, and replay reset after actions/teleportation. The clear-lane trials had no normal-lane guard clipping. These checks do not cover every dynamic door, enemy collision, knockback, special ability or modified movement speed.

## Native regression scenarios

Four final scenarios used eight game processes: movement/safety with Pilot and Hunter guests, and HUD/audio regression with each guest. All four passed the first- and second-floor layout barriers, artificial remote movement/fire, native projectile damage to an AI-disabled Bullet Kin, and a forced native transition to tt5. This does not establish natural room clearing or boss/elevator progression.

Both presentation scenarios set the native guest to 2.5/3 health, 2 armor, 3 blanks, 31 cash and 4 keys; acquired gun 15 and active item 108; and verified the received state and actual HUD repaint with nine native UI assets and equipped icons. Actual client screenshots were inspected.

| Guest | Native client PostEvent calls | Distinct event IDs | Native SetSwitch calls |
| --- | ---: | ---: | ---: |
| Pilot | 77 | 17 | 69 |
| Hunter | 133 | 18 | 88 |

The real first-floor gun-shot event reached the client before a synthetic second-floor cue. All relayed PostEvent invocations returned the deliberately suppressed zero playing ID. Audible quality and completeness were not tested.

## Desktop and file safety

Every final process passed the 800x600 windowed/background/cursor-unlocked check. Diagnostic windows used WS_EX_NOACTIVATE and did not activate the foreground at the desktop safety check. Output was disabled with -nosound, paused Unity listeners and all-overload diagnostic PostEvent suppression. The Unity volume getter reported 1, so a zero-volume reading is not used as proof. No Computer Use was used. All owned test processes were stopped after pass markers; subsequent peer-disconnect messages are controlled teardown.

After the user closed their ordinary Steam session, a new baseline of eight non-log files in the normal save directory was taken; all eight retained their hashes through the final tests. Earlier user play is not attributed to diagnostics. All 206 previously recorded release files retained their hashes. Private LAN test saves could change.

An earlier combined-probe launch did not reach mod startup while a normal Steam game session was present. It was stopped and excluded from pass counts; the cause was not established. The final probes ran separately after that normal session closed.

## Remaining boundaries

- No physical two-PC/Radmin run, sustained real-WAN congestion or fresh manager ZIP import was performed. Visual corrections and TCP head-of-line stalls remain possible.
- No full natural campaign, all bosses/items/abilities, endings, long-run endurance or modpack compatibility is established. The fourth-floor Sell Creep mismatch remains unresolved.
- The custom guest HUD does not reproduce every native inventory/map/shop/dialogue screen. Some effects/materials and beams remain incomplete.
- Music/ambience, RTPC parameters, every looping-stop/callback variant and third-party sound banks remain incomplete/unverified. All audible checks remain outstanding.
- Active-run reconnection and host migration are not implemented. No public upload was performed.

Sanitized evidence: `validation/summary.json` and `validation/responsiveness-035/native-summary.json`. Historical 0.3.4 HUD/audio evidence remains under `validation/presentation-034/`. Private probes, raw logs, normal saves and native screenshots are excluded from distribution.
