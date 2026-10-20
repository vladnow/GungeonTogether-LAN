# Changelog

## 0.3.9

- Synchronize the authoritative primary player mastery-token count used by native room-injection prerequisites. Prevent the reproduced client-only fourth-floor Sell Creep injection when client placeholder controllers are temporarily absent. Eligible host conditions remain supported.
- Preserve native non-mastery prerequisites, host generation and the layout-hash barrier. The generation override applies only to an active LAN client during generation.
- Show actual native input-override reasons in DETAILS, preserving cinematic/dialogue input restrictions.
- Require matching 0.3.9 builds on both PCs; retain 0.3.8 interface, rendering, movement and audio changes.
- Local controlled tests reached the fifth main floor. Full natural combat/campaign and physical two-PC/Radmin validation remain separate.

## 0.3.8

- Keep original local sprite shaders, keywords, texture identity/UV transforms and supported color/emission/cutout parameters. Apply dynamic authoritative tint separately. This covers native shadow, gun and procedural projectile material variants without shipping game assets.
- Wait for both living players at ordinary doors, open immediately once both are close, and avoid the native forced-movement sequence. Defer a room seal until both living players enter so a trailing player is not pulled into a prematurely sealed room.
- Preserve unacknowledged walking input across ordinary adjacent-room changes, use host door state while clipping client prediction, and filter only small camera-anchor changes caused by animation/pixel rounding. Wall/pit checks and authoritative teleports remain.
- Mirror native default labels such as shop prices; drive native boss bar artwork with host health/name data. Controllers that contain several boss segments use a combined health fraction on the guest.
- Exclude local map artwork from world-sprite suppression, update native markers/exploration, and allow the guest to hold the normal map binding. Guest map teleport selection is not enabled.
- Use authoritative maximum ammo and keep native ammo widgets within the viewport.
- Normalize native goop clock sentinels (including permanent lifetimes), accept finite signed sprite geometry, and discard unrenderable non-finite host visual transforms with diagnostics. Received packet validation remains strict. Numeric failure messages identify the offending value/field and sprite context where available.
- Register network sound emitters with Wwise. Keep native hurt cues/switches and add a damage-counter fallback, with separate hurt dispatch/failure diagnostics. Preserve the first session failure when the transport disconnects.
- Add the support Discord link to the public README: https://discord.gg/wuZQBhbrHk . This becomes visible on Thunderstore when this package description is published.

Update both PCs to 0.3.8. Early-floor alpha; fourth-floor Sell Creep mismatch and full-campaign validation remain open. See RELEASE-NOTES-0.3.8.md and VALIDATION-0.3.8.md.

## 0.3.7

- Replace the IMGUI imitation of the guest HUD with the game's actual heart, blank, consumable, gun, active-item and status-bar controllers. Keep the original single-player layout on the guest. Detached inactive UI equipment copies never enter player inventories or run Pickup callbacks.
- Reserve the native controller confirm/interact button for Interact. Network shooting uses RightTrigger, avoiding direct native menu/controller checks that could mistake a shot for interaction.
- Open ordinary unlocked doors when either network player enters. Remove the shared-screen proximity gate and its forced movement of both players. Locked/sealed doors and battle-room regrouping keep native checks.
- Relay nearby native goop cells, definitions and burning/electric/frozen flags. Build local native goop meshes and fire particles without local cell-gameplay callbacks or independent damage/spread simulation. Unsupported assets stop the run with a visible error instead of concealing a hazard.
- Recreate native textboxes and conversation response buttons using the host's actual text. The host still runs dialogue logic; the guest sends their normal interaction/menu actions.
- Relay guest damage to the native damage vignette and hurt sound event. Replicate native sprite override tints for hit/status flashes. No local damage is applied for feedback.
- Update both native placeholder positions/collider caches and room lighting. This addresses stale light/occlusion positions; the exact dark halo reported on a remote PC still needs a remote comparison.
- Smooth small authoritative walking corrections over 60 ms while retaining immediate bounded visual input replay, collision clipping and resets on teleports/room changes. This is not a reduction of physical network ping.
- Forward menu-navigation bindings only while a host dialogue is present. WASD's additional menu-navigation actions, reload and weapon cycling no longer disable walking replay. The earlier synthetic direction-only test did not cover these real keyboard binding combinations.
- Show the native player-two input state in LAN menu DETAILS to help identify any remaining movement locks.

**Update both PCs to 0.3.7. Earlier peers are incompatible.** Built against Steam Enter the Gungeon 2.1.9 and BepInExPack EtG 5.4.2101. Install through matching clean Thunderstore profiles. No launcher is included.

This is an early-floor alpha. The fourth-floor Sell Creep layout mismatch remains unresolved; full natural campaign completion is not established. The native HUD's current weapon/item display is covered; full inventory foldouts, dual-wield layouts, every item-specific meter, arbitrary dynamic/modded goops, general particle/gas-cloud replication and every NPC/puzzle are not established. A direct ordinary-door restriction was removed; this does not prove that every reported room-transition freeze has the same cause.

See the separate VALIDATION-0.3.7.md for exact checks. Native processes are local, silent, windowed, no-activate and cursor-unconstrained. Sound dispatch is checked without listening. No physical two-PC/Radmin run, fresh manager import or remote publication is claimed. All earlier release directories remain intact.

## 0.3.6

- Synchronize opened native secret-room ceilings, door meshes/blockers, visibility and minimap reveal. Keep closed secrets concealed and apply presentation without client room-entry gameplay or duplicate discovery statistics.
- Send the guest's complete gun, active-item, passive-item and active-synergy inventory when it changes. Use read-only native equipment-page readers and show the selected guest character.
- Keep Ammonomicon item sprites visible instead of hiding them with simulated world actors. Mute guest movement/prediction while its book is open. Do not grant items or mutate the mirror's placeholder inventories to populate the book.
- Preserve and enforce the native shared coin/key budget. Either player collects/spends from the same consumables object; blanks and equipment remain per-player.
- Retain native ghost persistence and resurrection rules. No automatic revival on every floor change is added.
- Require 0.3.6 on both peers. Retain 0.3.5 movement response changes and earlier guest HUD/audio/camera fixes. Full campaign and fourth-floor Sell Creep mismatch limits remain.

## 0.3.5

- Send compact player motion separately at up to 60 Hz and full world snapshots at up to 30 Hz. Capture after native physics and acknowledge input consumed by the native player controller.
- Preserve the guest input/send cadence through frame jitter. Coalesce unsent movement states while retaining reliable action edges.
- Add optional bounded visual prediction for ordinary guest walking, including the attached gun/hands/outlines and guest camera. Keep native damage and interactions authoritative on the host.
- Check terrain and blocking bodies before presenting predicted lead. Disable/reset replay for pause, rolls, input overrides, stale motion, room changes and teleports. Provide Network / ClientMovementPrediction to disable it.
- Use adaptive world interpolation, initially 40 ms and bounded to 35-90 ms. Report actual smoothing in the menu.
- Retain the graphical guest HUD, combat sound relay, independent cameras and resource/layout checks. Require 0.3.5 on both peers.
- Make public packages English only. Exclude local agent instructions, localized documents and obsolete launcher files. Browser repository uploads still require explicit deletion of old tracked files.
- Keep the unresolved fourth-floor Sell Creep layout mismatch and incomplete campaign coverage explicit. Local response measurements do not establish two-PC/Radmin latency.

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



