# Changelog

## 0.3.0 — movement and character alpha

- Interpolate timestamped foyer avatars and client dungeon sprites/camera with a 120 ms buffer. Increase dungeon snapshots to up to 20 Hz.
- Resolve different foyer heroes using their own sprite collections.
- Let the client choose Cultist, Pilot, Marine, Convict or Hunter before the run. Transfer the selected heroes in floor commands and scope native co-op prefab/controller lookup fixes to the LAN run.
- Replace the prototype window with a restrained dark/gold menu, role tabs, hero selector, scalable layout and separate diagnostics. Typing mutes local movement; Esc closes the menu.
- Use English throughout the in-game menu, hero names, hints and client status HUD.
- Require both PCs to update: 0.2.x peers are incompatible.
- Isolate native per-room generation random state on every floor. This fixes a reproduced case where procedural corridor pit/floor cells differed despite matching room names and prerequisites. Keep layout hash checks enabled.
- Known blocker: a separate fourth-floor layout mismatch remains reproducible. The geometry barrier stops the run; the fifth floor was not reached. Attempts to isolate deferred layout RNG did not resolve this and are excluded from the release.
- Add silent windowed diagnostics with background execution and cursor release. See VALIDATION.md for the exact current test scope.

## 0.2.3 — documentation package

- Correct public instructions and repository metadata; the runtime remained 0.2.2.

## 0.2.2 — profile installation alpha

- Automatically enable LAN mode when loaded from a separate mod-manager BepInEx profile.
- Keep ordinary direct Steam launches on normal saves; retain explicit Normal/LAN configuration choices.
- Use a distinct LAN plugin identifier and declare Steam GungeonTogether/legacy LAN plugins incompatible in the same profile.
- Package the runtime for Thunderstore dependency installation without an external installer EXE.
- Transfer the host's current-session generation statistics and retained early-chest flag; preserve native player availability during client seed initialization. This addresses later-floor geometry mismatches reproduced during local preflight tests.

## 0.2.1

- Fixed unconditional save-profile redirection during ordinary Steam launches.

## 0.2.0

- Added host-native Cultist control, local sprite replication, generation-context/layout checks and floor-transition synchronization.

## 0.1.0

- Added direct IPv4/TCP connection and foyer avatar preview.
