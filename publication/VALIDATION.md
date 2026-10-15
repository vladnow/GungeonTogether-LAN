# Validation of 0.3.0 alpha

Date: 2026-10-15. Local alpha validation; this does not establish a full campaign or physical-PC/Radmin playthrough.

**Known blocker: initial seed 299027071 reproduces a fourth-floor layout mismatch. The host/client run stops at the geometry barrier. This alpha is suitable for early-floor testing, not a verified full run. Fifth-floor testing was not reached.**

Runtime SHA-256: `c760078ddd8d271dc28194fed54ae9f2771eced45001ac240d428324409a9d99`.

## Build and background checks

- Compiled against actual Steam Enter the Gungeon 2.1.9 / Unity 2017.4.27 / BepInEx 5.4.21 libraries, without game stubs. No compiler warnings or errors.
- 108 automated checks: 28 real-loopback LAN transport, 23 dungeon protocol, 19 save-profile and 38 interpolation/character checks.
- 15 direct Harmony targets resolve uniquely to managed methods in the installed game assemblies.
- Interpolation tests cover 60/144 FPS, batched delivery, independent clocks, stale packets, sequence wraparound and stalls. A 100 ms buffer failed the tested jitter scenario; the final 120 ms buffer passed.
- Launcher file-fixture checks passed and its embedded DLL hash matches the runtime above. These do not replace testing a complete fresh installation from the launcher.

## Native two-process tests

The exact DLL above passed five separate silent, windowed tests in private game copies. Each used two local processes and external BepInEx roots. The game confirmed fullscreen=false, 800x600, cursor=None, background execution and a nonforeground diagnostic window. No Computer Use interaction was used for this final series.

Every guest character passed: different foyer avatars and remote movement rendering, matching first/second-floor geometry, native player-two identity and starting gun on both floors, remote movement, native projectile ownership, damage to an AI-disabled Bullet Kin, client sprite interpolation and a forced native LoadCustomLevel("tt5") transition. The host's first player was Pilot; the client's original foyer hero was Hunter. The client placeholders were aligned with the host's character for generation.

| Guest | First-floor hash | Second-floor hash | Native exceptions |
| --- | --- | --- | --- |
| cultist | `93d0210570c3129b0a44e01a4c0ffe6d3d97c2b9b82eb973c9affb083cf13775` | `c60821a189b648f703e3da52752f95a1b590fe72107bd1438720d17910a1107f` | 0 |
| pilot | `963e95f3a1b37a48c1b56b09fe0cf1957ba2fed4af618ccaf4bd8c475ab60d14` | `279dd8916da5f7bc9384c14fc4b3fbdd78a0e1694be666ec2ac3450370df1604` | 0 |
| marine | `b344f5bca8ab3e46aaeb24bdaec264998ac1f32a65f7314d5bee22dce95f2356` | `934bbf9ba697b249e5f4c049e1306abe1944a719b0ef9684a6619b1742db4c7b` | 0 |
| convict | `2169c6231258022a36b0cb5520717b34fa8ba457cd6106ae5d07198d36792472` | `392febd3eca7de5e5ee12f91a94a23cf8a035e2b6254fcc8267845c1d8618c99` | 0 |
| hunter | `c6daecc5a0f415fe3aee56f3f71b2594c8aea429170e44238ef537b3c5d9b12e` | `97ec2b9f5256798cf2286ce387bf05aa0e701193e67b7b4401bb5a25f1028187` | 0 |

An additional two-process run with Hunter continued past the second floor. A private diagnostic driver used the game's actual next-floor definitions and LoadCustomLevel, without modifying the runtime DLL. The third floor passed an identical layout hash, two native players, retained guest identity/gun and current-floor snapshots on the client:

| Main floor | Scene | Matching host/client hash |
| --- | --- | --- |
| 3 | `tt_mines` | `35888b0ab6bbb23df710de80a29ee0f1d045ffe612122e50bd0d273489bd958e` |

The fourth floor then failed the geometry check. Rooms differed despite matching initial generation context and the same selected native flow. Attempts to isolate the deferred layout random stream did not resolve the mismatch and were removed from this release. Its cause remains unresolved. These checks do not clear every room or prove all interactions are synchronized.

Menu screenshots were saved by the game and inspected locally. They establish actual rendering at the tested resolution; keyboard/controller navigation, every resolution and live two-PC interactions are not comprehensively tested.

The earlier failing Pilot seed 299027071 was reproduced. Rooms and prerequisites matched while procedural corridor cell types differed. The native RobotDave generator uses global random state during room application, which yields between rooms. The final fix scopes Unity and generation random state per room using floor seed and coordinates, then restores the surrounding state. The same formerly failing seed passed in the final Pilot case. Geometry checks remain enabled. An experimental room-list sort did not fix the failure and was removed.

Six normal save/backup/options files retained their pre-test SHA-256 values. All game-test writes used private copied game/save directories. Save-profile policy for ordinary play was not changed by these features.

## Remaining limits

- A fourth-floor geometry mismatch is a known blocker. The fifth floor was not reached; no full-run readiness is claimed.
- The test enemy AI was disabled; later floors were loaded by native commands. These are bounded smoke tests, not a live co-op playthrough, clearing every room or a boss/elevator test.
- Two physical PCs/Radmin, sustained packet loss, long runs, all abilities/items, revival, every boss, special endings and modpacks remain unverified.
- Client combat audio, some materials/effects, beams, secret-wall tile changes, shop/dialogue UI and active-run reconnection remain incomplete.
- A 120 ms interpolation delay improves presentation but adds latency. There is no client prediction; long stalls and native co-op distance limits can still cause corrections.
- Native base-game warnings remain in raw logs. Do not interpret a passed smoke marker as proof that every logged warning is resolved.
- A separate final-build Pilot attempt ended with an unexpected process exit; its logs also showed connection refusals. The failed attempt was retained privately and rerun; its cause is unresolved. A successful retry does not establish startup stability.
- ZIP import through the actual mod-manager UI and a fresh manual launcher installation were not performed in this series. Archives and embedded runtime are verified separately.

Private raw logs, game screenshots, game assets and game/BepInEx assemblies are excluded from distributable packages. MIT attribution is preserved. No public upload was made by this validation step.