# Validation of 0.3.3 alpha

Date: 2026-10-16. **Runtime SHA-256: `2d90b7494bcfff146dbb227580a3ed9cd1c167f5914d2ab89fba0f8cc36e7781`.** Both successful native cases below used this exact DLL. Historical 0.3.2 and 0.3.1 reports remain separate.

**Known blocker: the fourth-floor Sell Creep layout mismatch remains unresolved. Full natural campaign completion is not established.**

## Change

The host's native camera focus selects player one rather than averaging the two characters. During normal gameplay, guest snapshots contain a virtual camera around player two. The host camera is not moved temporarily to generate snapshots. Explicit native manual camera framing is retained for scripted sequences.

Sprite selection uses this guest view and its reported viewport aspect. Input messages carry a finite aspect in the bounded range 0.25–8; the handshake rejects 0.3.2 and older peers. Native sprite getters flush deferred invisible animation frames before capture and bounds testing. The native shared-screen movement-boundary callback is bypassed only during an active host LAN run; the patch does not turn an existing rejected movement into a valid one.

The 0.3.2 enemy-prefab asset loader and 0.3.1 transport/interpolation policies remain. Native sealed-room regrouping, floor transitions and other co-op rules remain shared. Separate cameras do not make two independent campaigns.

## Automated checks

**234 checks passed:** transport 31, dungeon protocol 84, profile policy 19, rendering/characters 38, display migration 9, acknowledgements 9, sprite visibility/material descriptors 20, and constrained transport 24. Another **22 managed Harmony targets** resolve uniquely in the actual installed game assemblies, including the camera focus and shared-screen movement callbacks. Compilation used installed Steam game and BepInEx 5 libraries.

New protocol tests cover a distinct ultrawide viewport, valid boundary aspects, invalid/NaN/infinite aspects and rejection of a 0.3.2 peer. Transport tests alone do not establish native camera behavior.

## Native evidence

Two scenarios each used two private local game processes:

| Case | Guest / aspect | Coverage |
| --- | --- | --- |
| Standard view | Pilot / 16:9 | Own camera focus and visible framing on each peer after 32-unit separation; three host-spawned blob enemies outside the host view visible and animated in three observed client frames; native guest movement and projectile capture; controlled deferred-frame and manual-camera fixtures |
| Wide-view fixture | Hunter / 3.0 | Same checks, plus a fourth enemy beyond the padded standard guest viewport that is included in the wide guest snapshot and visible to the client |

Both cases first passed matching floor 1/2 layout barriers, remote movement/fire, native projectile damage to an AI-disabled Bullet Kin and a forced native transition to tt5. The camera check then used a native warp into `Gungeon_Normal_HorizontalRoomPitsTraps`, with 32 units of player separation. Entrance tracking/axis locks were reset using native camera APIs after the artificial warp. AI was disabled for stable enemy checks and players were invulnerable. This is not natural walking, ordinary boss/elevator progression or room clearing.

The host's native room framing can keep the camera offset from the hero near a room edge. The test verifies primary-player focus, visibility and framing nearer the primary player, rather than assuming exact screen-center placement. The client follows its own secondary-player view. Actual host/client PNGs for both cases were inspected and show distinct portions of the room with the appropriate player.

The 3.0 aspect was an explicit native aspect/Pixelator fixture in an 800×600 test window, not a physical ultrawide monitor test. The standard 16:9 host remained separate. A fourth enemy beyond the standard guest view checked that the wider selection was actually used, not merely transmitted as unused metadata.

An explicit short deferred-animation fixture queued a valid native sprite frame immediately before capture. Captured IDs matched it; ordinary blob animations also changed on the client in three observed frames. Native getters consume these deferred flags, so checks compare against the fixture's expected ID rather than requiring the flag to remain set afterward.

Off-host-screen guest movement and native projectile inclusion were observed. Calling the movement-boundary callback with an already false collision result left it false. This verifies the patch's narrow behavior, not every wall/pit/minecart interaction. A synthetic native manual-camera interval checked shared explicit framing and return to guest follow afterward; no real boss introduction was exercised.

Both peers passed their camera checks and remained running until intentional shutdown. Successful logs contained no runtime exceptions. Stopping the host afterward causes the expected client disconnect. Exploratory builds/probe assertions were corrected before these cases: guest Z construction was changed to the native vector conversion, room framing was accounted for, and an invalid assumption about deferred animation flags was replaced with the explicit fixture. Earlier attempts are excluded from successful evidence.

All tests were silent, windowed at 800×600, no-activate, with an unconstrained cursor and background simulation. **No Computer Use was used.** Nine normal save-directory files retained their hashes. Test game processes were stopped after evidence collection. Only a sanitized summary is distributed; private logs, observer binaries, game copies/assets, saves and screenshots remain local.

## Release files

Each new version is prepared in **`release/<version>/`**, with Thunderstore ZIP, source ZIP, SHA-256 sums, a required standalone `RELEASE-NOTES-<version>.md`, this validation report and relevant instructions. The builder preserves older releases and refuses to overwrite existing version artifacts. No launcher binary or launcher ZIP is built.

Both PCs require **0.3.3**. Package inspection and successful native loading do not establish a fresh manager ZIP import, public upload or physical two-PC/Radmin session.

## Limits

- Fourth-floor Sell Creep mismatch remains unresolved; no full natural campaign claim.
- Native combat regrouping and shared floor progression still apply. Ghost/death/spectator behavior, every cinematic, all enemies/items/abilities, special endings, long sessions and modpacks remain unverified.
- Client audio, some effects/materials, beams, secret-wall changes and shop/dialogue interfaces remain incomplete.
- The display buffer remains 60 ms, with up to 20 Hz dungeon snapshots and no client prediction. Further latency reduction is not claimed. Wider views can include more objects.
- Ordinary fullscreen startup, actual physical monitor combinations, a fresh actual manager ZIP import and Radmin on two physical PCs were not exercised in this iteration.
- Active-run reconnection and host migration remain unsupported; return to the foyer after a fault.

MIT attribution remains intact. Game/BepInEx binaries, game assets and personal logs are excluded from publication packages. No upload or public availability is established by this report.
