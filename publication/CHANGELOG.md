# Changelog

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
