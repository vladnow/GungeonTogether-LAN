# Validation of 0.3.6 alpha

Date: 2026-10-16. **Runtime SHA-256: `e741dcb4b8ba6c27edc75068e58009bb9ee7d1a920462242a911a6fb18c45f98`.** All three final native scenarios used this exact DLL.

**Early-floor alpha. The fourth-floor Sell Creep layout mismatch remains unresolved. A complete natural campaign is not established.**

## Changes under test

The host sends cumulative native secret-room presentation state and the guest's full gun/active/passive/synergy lists when changed, sampled at up to 4 Hz. They use independent latest-state message keys. Old-floor state is ignored. Inventory counts, IDs, secret-room coordinates/flags and duplicate room identities are bounded/validated.

The mirror removes opened native secret ceiling/door meshes and blockers, clears corresponding secret-cell flags, updates visibility/occlusion and reveals the room on its minimap. It does not call client room-entry gameplay, OpenDoor or discovery-stat setters. Closed secrets remain concealed. Other arbitrary tilemap damage and every special puzzle remain outside established coverage.

The native Ammonomicon equipment page uses scoped read-only inventory readers and the selected guest identity. The native function still constructs the page and resolves local encounter icons. No pickup grants, item callbacks or client placeholder inventory mutations populate it. Native UI sprites are excluded from world-sprite hiding. Guest walking/input prediction is muted while its book is open. Collection-unlock history, every death-book field and all map/shop/dialogue interfaces are not synchronized by this change.

Coins and ordinary keys retain the native shared consumables object. The host rebinds player two to the primary budget if needed, without adding two balances. Ghost persistence and native revival rules remain; no automatic floor-change resurrection is added.

## Automated checks

**330 checks passed:** transport 34, dungeon protocol 85, profile policy 19, rendering/characters 38, display migration 9, input acknowledgements 9, sprite visibility 20, constrained real-socket transport 24, HUD/audio presentation 30, movement responsiveness 31, and inventory/secret-state protocol 31.

New checks cover complete ordered inventories, stacked passives, character/floor identity, empty ghost inventory, count/ID bounds before allocation, first-send/unchanged/changed state caching, closed/open mesh flags, coordinate overflow, duplicate rooms, malformed/trailing payloads and rejection of a 0.3.5 peer.

**41 exact native managed binding contracts passed** against installed Steam Enter the Gungeon 2.1.9 libraries. These include the native equipment-page patch and secret-opening guards. The equipment-page transpiler validates that all expected native inventory readers exist. Compilation used installed game/BepInEx libraries rather than mocks.

## Final native gameplay cases

Two local game processes per case, Pilot host and Hunter or Pilot guest. Each passed ordinary artificial remote movement/fire and projectile damage to an AI-disabled Bullet Kin, equal first/second-floor layout barriers and a forced native transition to tt5. The ghost-aware private verifier expects no equipped gun for an intentionally dead player and verifies its chosen native identity instead.

Both cases checked:

- A first-floor native secret manager begins closed on both PCs. Calling the host's native OpenDoor exposes the client's native ceiling/door geometry and removes its blocker. After a controlled warp, the mirror's player is in that room and the interior renders in an actual screenshot.
- The client's discovery-stat value stays unchanged during visual reveal. No independent client room-entry combat events are used.
- Forced zero native health plus native BecomeGhost establishes a ghost before the floor load. It remains a ghost on the next floor, and the client receives that state. Calling the native boss-reward resurrection function restores the selected guest and reaches the client.
- Host and guest consumables are the same object. Native coin pickup 68 by the host and key pickup 67 by the guest credit the common budget. Spending from either player debits it. The client HUD receives the resulting 6 coins and 1 key.
- Native acquisition of gun 15, active item 108 and passive item 102 reaches the guest's complete equipment lists. The actual native book draws the corresponding AK-47, Bomb and Scope encounter icons, keeps their renderers visible and identifies the guest hero. Both local placeholder inventories remain unchanged.

The Hunter page had six native equipment icons and the Pilot page had seven. Actual secret-room and book screenshots were inspected for both final cases. Screenshots and game resources are not distributed.

The private book probe advances its internal animation focus flag without activating the OS window, and bypasses queued unlock-notification cards so the equipment page can be inspected without keyboard interaction. These adjustments are confined to the private probe and do not change production book/unlock behavior.

The ghost fixture does not exercise the complete natural lethal-damage animation, item-drop sequence, boss defeat, pedestal landing or chest interaction. In the installed native code, boss rewards and revival chests can invoke resurrection; a floor load alone preserves ghost state. A screenshot of a floor entrance cannot establish whether an expected earlier revival failed.

## Movement regression

A third final native pair, with Pilot guest, repeated twelve synthetic walking starts on a controlled clear lane. Input sample to at least 0.0625 game units of presented displacement: **median 10.609 ms, p95 25.005 ms**. Prediction, attached guest-owned sprite parts, native wall/pit queries, host pause/input override/roll and teleport reset checks passed.

This is a local loopback presentation measurement, not hardware input-to-photon latency, damage confirmation or a physical two-PC/Radmin result. Historical 0.3.5 methodology/results remain under validation/responsiveness-035 and publication/VALIDATION-0.3.5.md. This release does not introduce a new latency algorithm beyond 0.3.5.

## Safety and remaining limits

All six final game processes passed 800x600 windowed, background, cursor-unlocked and no-activate desktop checks. Output was disabled with -nosound, paused Unity listeners and all-overload diagnostic PostEvent suppression. No Computer Use or listening test was used. Owned test processes were stopped after pass markers; later peer-disconnect messages are controlled teardown.

All eight baseline non-log files in the normal save directory and all 318 recorded prior release files retained their hashes. Private test saves could change. The Russian owner-only GitHub guide was updated before taking this release baseline; prior runtime/source ZIPs were not replaced.

No physical two-PC/Radmin test, fresh manager import, complete natural campaign, all secret puzzles/items/synergies, full death/boss/chest cycle or modpack compatibility is established. Native shopping, some materials/effects/beams and arbitrary dynamic tilemap changes remain incomplete. Music/ambience, every looping-sound/RTPC/callback variant and audible coverage retain earlier limits. Active-run reconnection/host migration are not implemented. No public upload was performed.

Sanitized evidence: validation/summary.json and validation/gameplay-036/native-summary.json. Earlier reports remain historical. Raw private probes, paths, logs and native screenshots are excluded from public packages.
