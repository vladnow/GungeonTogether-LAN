# Validation: 0.3.7 alpha

Date: 2026-10-18. Runtime SHA-256: 6b6a7302faaef3049e64b98af4333f19824f792128b2b5e65e9b08356e2a2066.

Built against the installed Steam Enter the Gungeon 2.1.9 managed assemblies and BepInEx 5. The exact DLL above was used by all four current-version native scenarios below. Packaging is not publication.

## Automated checks

**356 checks passed**, plus **62 managed native binding checks**. Suites: transport 34; dungeon protocol 85; profile 19; rendering/characters 38; display 9; acknowledgements 9; sprite visibility 20; shaped transport freshness/latency 24; HUD/audio protocol 30; responsiveness 34; inventory/secrets 31; scene effects 23. Audio targets include all twelve managed PostEvent overloads and both switch overloads.

The new checks cover bounded surface/text packets, invalid hazard/style data, native status-material tint transport, walking correction continuity/convergence and WASD's additional menu-navigation bindings. Reload/weapon cycling and menu navigation must not disable walking prediction.

## Actual native game processes

Four scenarios passed using **eight current-version native processes**. Each retains two matching native floor-layout handshakes and the base remote movement/fire/projectile-damage smoke checks.

- Pilot and Hunter feedback scenarios: actual native HUD controllers/counters, hidden duplicate HUD, distinct shoot/interact physical mapping, opening an ordinary door for separated players without forced movement, preserving a locked door, native textbox text and conversation-button choices, native poison/fire cells and mesh renderers, live native fire particles, host damage, guest native damage-shader activation, replicated player tint and hurt-event dispatch. Local placeholder health is not damaged to present feedback.
- Keyboard responsiveness: twelve synthetic short walk pulses with the additional menu-navigation bits produced by native WASD bindings. Native pause/input-override/roll, teleport-history reset and wall/pit clipping guards were exercised.
- Gameplay regression: native opened-secret geometry, acquired gun/active/passive icons in the actual guest equipment page, unchanged placeholder inventories, shared native coin/key pickups and spending, forced ghost persistence across a floor change and a native resurrection call. The private UI probe advances an internal focus flag and skips queued unlock cards without activating the window; it does not change production behavior.

All game tests use private game copies and isolated save/loader profiles. They run **without sound**, at **800x600 windowed**, with **WS_EX_NOACTIVATE**, an unconstrained cursor and background simulation. No Computer Use or foreground keyboard/mouse automation is used. Native sound-event dispatch is checked without listening.

## Local keyboard comparison

Same synthetic walk pattern and menu-navigation bits, twelve starts per version:

| Runtime | Median | P95 |
| --- | ---: | ---: |
| 0.3.6 | 30.365 ms | 71.476 ms |
| 0.3.7 | 14.961 ms | 41.424 ms |

Metric: input sample to at least 0.0625 game units of presented displacement in a local client. The old baseline explicitly confirms that those menu bits disable walking prediction. These are **not** physical-PC/Radmin ping, real keyboard/input-to-photon or damage-latency measurements. Small samples and background frame-time variation limit precision.

## Preservation and limits

All 432 prior release files retain their hashes. All eight tracked normal non-log save/profile files retain write times from before these tests and match the current checkpoint. One normal save already differed from the older 0.3.6 checkpoint before this work began; it was preserved, not restored over newer progress.

No full natural campaign, full natural death/boss/chest cycle, fresh mod-manager ZIP import or remote upload was performed. The fourth-floor Sell Creep layout mismatch remains unresolved. The specific reported dark halo still needs a remote visual comparison. An ordinary-door restriction was identified, but not every room-transition lock was reproduced. Full inventory foldouts, dual wield, all item-specific layouts, arbitrary dynamic/modded goops and general particle/gas-cloud replication remain outside established coverage. Audible quality needs a listening test.

See validation/feedback-037/native-summary.json in the source package for sanitized evidence. Raw logs, diagnostic DLLs, save fixtures, host generation envelopes and screenshots remain private. This remains an early-floor alpha, not a full-run recommendation.
