# Changelog

## 0.3.4

- Replace the guest text box with a graphical, host-authoritative HUD using local heart/armor/blank/cash/key and equipped gun/item icons; include max health, magazine capacity/count, reload and item-charge state.
- Add bounded combat sound-event batches with spatial emitters and per-source Wwise switches. Keep local volume settings and suppress duplicate local combat cues while mirroring.
- Reject duplicate, old-floor and stale sound batches; clear sources during transitions and session teardown. Add sound counters to network diagnostics.
- Require 0.3.4 on both peers. Preserve separate cameras and existing synchronization checks; full campaign and audible two-PC coverage remain unverified.
- Keep silent, windowed diagnostics and separate release/0.3.4/ artifacts with standalone release notes. No launcher update.

## 0.3.3

- Added per-player camera focus during ordinary LAN gameplay: host/player one and client/player two. Native manual camera framing is retained for scripted sequences.
- Select network sprites using the guest's virtual camera and bounded viewport aspect, including objects outside the host's screen.
- Read native deferred off-screen sprite-animation updates before capturing geometry.
- Bypass the native shared-screen movement boundary while retaining other movement restrictions and collision results.
- Require 0.3.3 on both peers; older input payloads do not include the guest viewport aspect.
- Retain the 0.3.2 enemy asset loader and 0.3.1 transport/interpolation changes. Full campaigns and complete death/cinematic coverage remain unverified.
## 0.3.2

- Fixed resolution of native enemy sprite collections embedded in enemy prefabs, including Blobulin/Blobuloid/Blobulon. Missing collections can now load their owning prefab dependencies by native enemy GUID.
- Preserved collection GUID/table checks and the stop on genuinely incompatible resources; no invisible-enemy fallback.
- Added bounded prefab-owner discovery and per-session dependency caching. The network handshake now requires 0.3.2 on both peers.
- Reproduced the 0.3.1 missing-Blobulin stop before testing the fix. Current bounded native evidence is in VALIDATION.md; full-run readiness is not claimed.
- Updated the Thunderstore/source package and public troubleshooting instructions. No launcher update is provided. Existing latency improvements remain; further latency reduction is not claimed.
## 0.3.1 — state freshness and client stability alpha

- Replace pending world/foyer state with newer state while preserving reliable control-message order. Compress large replaceable packets when useful and bound pending stale data.
- Reduce timestamped display interpolation from 120 ms to 60 ms; retain up to 20 dungeon snapshots per second. Add mod RTT and client input-to-host-snapshot acknowledgement readings.
- In a controlled real-socket test with an incompressible 16 KiB / 30 Hz workload and a 128 KiB/s bottleneck, p95 delivered-state age changed from 2063 ms under FIFO to 341 ms under latest-state delivery. This is not a Radmin or running-dungeon result.
- Add guest cash, keys, blanks, weapon/item names, active-item state, ghost/death and run-end HUD information.
- Update mirrored client rooms without native gameplay room-entry events; suppress client collision simulation and independent boss intros while mirroring. Reduce repeated scene scans.
- Prevent native focus-loss automatic pause during an active LAN run; preserve manual host pause.
- Draw the menu cursor above controls. Inherit initial display preferences once and refresh the normal startup viewport/UI with bounded work.
- Add display-profile, acknowledgement, latency and sprite-visibility checks. The exact runtime passed two bounded local two-process early-floor scenarios; see VALIDATION.md for measurements and remaining visual coverage. Complete item-outline/material fidelity is not claimed.
- Require both peers to update: 0.3.0 and older peers are incompatible.
- Keep the unresolved fourth-floor Sell Creep layout mismatch and geometry barrier explicit. No full-campaign or physical-PC/Radmin validation is claimed.

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
