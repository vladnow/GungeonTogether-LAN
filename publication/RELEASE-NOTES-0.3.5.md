# Gungeon Together LAN 0.3.5 alpha

This update targets the delay felt by the joining player. A compact player-motion stream runs independently from larger world snapshots. The guest can show bounded anticipation of ordinary walking while the host remains responsible for gameplay simulation.

## Changes

- Player motion and guest input run at up to 60 Hz; full world snapshots run at up to 30 Hz, subject to frame rate and load.
- Capture follows native physics. Input acknowledgements identify input actually consumed by the native guest controller.
- Visual prediction covers at most 100 ms / 0.65 game units. It moves the guest body, attached hands/gun/outlines and camera together. Local terrain/body queries limit the visual lead.
- Pause, rolls, input overrides, stale motion, room changes and teleports disable/reset speculation. Set `[Network] ClientMovementPrediction = false` in the profile config and restart to retain only the fast authoritative stream.
- World interpolation adapts between 35 and 90 ms, starting at 40 ms. The menu shows the current value.
- The guest graphical HUD and combat sound-event relay from 0.3.4 remain included.
- Public source and documentation are English only. Internal AGENTS.md, localized documents and obsolete launcher files are excluded. The GitHub update guide lists old tracked files to delete explicitly.

## Installation and compatibility

Update both PCs to the same 0.3.5 build. Older peers are rejected. Use a separate Thunderstore profile with BepInExPack EtG 5.4.2101 and one networking mod. Built against Steam Enter the Gungeon 2.1.9. Import the Thunderstore ZIP locally or update after publication; the source ZIP is for GitHub/development. No launcher is included. Existing LAN saves remain in the separate dungeon-profile directory.

## Validation and limits

See VALIDATION.md for exact-runtime checks and local movement measurements. Native checks are silent, windowed, two-process tests on one PC; they do not prove latency over Radmin or a complete natural campaign. Prediction improves visual walking response, not the host-confirmed timing of damage, pickups or every animation/action. TCP stalls and correction snaps remain possible. Higher world frequency may increase total bandwidth; UDP and world delta compression are not included.

The fourth-floor Sell Creep map mismatch remains unresolved and can stop a run. Complete boss/elevator progression, endings, modpacks and all special movement/item combinations remain unverified. Sound event dispatch is tested with output suppressed, not by listening. Creating these files does not publish them.
