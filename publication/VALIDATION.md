# Validation of 0.2.2 alpha

Date: 2026-10-14. This is a local alpha candidate, not a public-release or full-gameplay certification.

Runtime SHA-256: `c8ca24150a1f4f6fea36d3277f014d55a7d3d41682301f6d77a8caccec60d494`.

## Build and automated checks

- Compiled the actual source against Steam Enter the Gungeon 2.1.9 (build 3829397), Unity 2017.4.27.805765 and BepInEx 5.4.21 / Pack EtG 5.4.2101. No game-type stubs were used.
- Rebuilt the final DLL after native testing; its SHA-256 remained identical. No compiler warnings or errors appeared.
- LAN transport: 27 checks, including actual loopback sockets.
- Dungeon protocol: 23 checks.
- Save-profile selection: 19 checks, including external manager paths, Normal priority, legacy arguments and smoke isolation.
- Native profile fixture tests confirmed normal direct launches choose NORMAL and an external BepInEx manager-style launch chooses LAN without `--gt-lan-profile`. Both loaded a private copied save fixture successfully. Subsequent loads/writes were redirected to fixtures. Six live user save/backup/options files were unchanged during these profile tests.

## Native two-process smoke runs

The exact DLL above ran in separate game copies over 127.0.0.1, using separate external BepInEx roots like a manager profile. Both processes used `-nosound`; the diagnostic mode also disables Unity/Wwise playback. The original installed game was not used as the test process.

Two final runs passed with different first- and second-floor layouts:

| Run | First-floor hash | Second-floor hash |
| --- | --- | --- |
| 1 | `82e80139e835b28d1e292afff4fdb4b2192c960cb7e51b5d19e6d18fe93d4d2e` | `4fb8c53d7d8390328c131874c09a0ab72652c70de1bb0ad4e66392f408009b59` |
| 2 | `705db4aa221d76c5d4b6924850d9790db00e06ceda395037372ffaefe0971ad0` | `bfe3565313a57d8d471cfe384118821257327efb9476945e990dd7a20c1d6f36` |

For each run the host and client reported identical geometry, Running state, client sprite snapshots, and the final PASS marker. The host observed Cultist movement, a native projectile owned by the Cultist, and damage to a native Bullet Kin. The client received a second-floor snapshot after the epoch transition.

The enemy's behavior AI was disabled. The transition was forced through native `LoadCustomLevel("tt5")`, not through a completed boss/elevator. Native player LateUpdate and delayed-foyer initialization exceptions occurred before dungeon readiness. These failures were not erased; limited smoke assertions passing does not establish clean foyer/UI initialization or all gameplay behavior.

Earlier candidate DLLs failed second-floor geometry checks. The final source now transfers current-session statistics and the persistent ForceEarlyChest flag and preserves players until native seed setup finishes. Only the final hash and the two runs above count as this candidate's native evidence.

## Limits and publication status

Two physical PCs, real Radmin latency/loss, active enemy fights, every boss/item, full campaigns, real boss transitions, secret-wall tile changes, beam geometry, client combat audio, shop/dialogue UI, active-run reconnection and other modpacks remain unverified or incomplete. Windows is the only validated platform. This candidate should be offered as an experimental alpha with a dedicated mod profile.

The Thunderstore ZIP is prepared locally. Account/team validation, real manager ZIP import/launch, moderation and public availability are separate steps and have not been established. Game/BepInEx assemblies, saves, raw private logs, executable installers and development/test files are excluded from the runtime package. MIT attribution is preserved; source is supplied separately.

All owned game processes were stopped. The user subsequently required that future graphical tests also permit free use of both monitors; the old launcher did not satisfy that condition. Further graphical launches are disabled by default pending a nonblocking harness. No additional game runs were made after this request.
