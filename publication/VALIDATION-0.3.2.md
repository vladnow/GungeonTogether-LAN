# Validation of 0.3.2 alpha

Date: 2026-10-16.

**Runtime SHA-256: `761a6ab788f57457f68af082a51fa3cde8c2bcaf559df9fa0f53c2b144985d5f`.** Native cases below used this exact DLL. Historical results belong to their original binaries and remain separate in VALIDATION-0.3.1.md and VALIDATION-0.3.0.md.

**Known blocker: the fourth-floor Sell Creep layout mismatch is unresolved. This is an early-floor alpha, not a confirmed complete campaign.**

## Confirmed failure and change

A real 0.3.1 profile log contained two stops: the client could not resolve BlobulinSpriteCollection or BlobulonSpriteCollection, sent Stop, and the host paused/opened the LAN menu. Around those stops, mod RTT was approximately 16–33 ms and the application queue was generally empty. A screenshot independently showed the Blobulon failure. This identifies these specific stops; it does not establish that every reported freeze has the same cause.

The old 0.3.1 DLL was run in two private local game copies. After the initial move/fire/transition smoke test, the host explicitly spawned native Blobulin, Blobuloid and Blobulon. The client lacked the collections, stopped on **BlobulinSpriteCollection**, and the host received the same friend-stopped message. This reproduces the original failure before the change.

Native source inspection showed that these sprite collections are embedded prefab dependencies rather than independently loadable BraveResources/tk2d assets. The three enemy names do not map reliably to the collection names: guessing from the name is insufficient. The change transmits the owning native enemy GUID, loads the local enemy prefab dependencies when needed, then retains the original collection GUID and ordered-table fingerprint verification. Prefabs are loaded as assets; no extra client enemy instance is created by the loader.

## Automated checks

The current source passed **224 checks**: transport 30, dungeon protocol 75, save-profile policy 19, rendering/characters 38, display migration 9, input acknowledgements 9, sprite visibility/material descriptors 20, and constrained transport 24. Another **20 managed Harmony targets** resolved uniquely against the installed game libraries. Compilation used the installed Steam game assemblies and BepInEx 5 references.

New checks cover optional/exact enemy dependencies, invalid identifiers, the previous collection payload without its new dependency field, and rejection of a 0.3.1 peer before new wire messages are used. Existing transport/interpolation/visibility safeguards passed. These checks alone do not prove native gameplay.

## Native game cases

Two successful scenarios each used two local game processes and the exact hash above:

| Case | Guest | Native evidence |
| --- | --- | --- |
| Missing enemy assets | Hunter | Matching floor 1/2 barriers, remote movement/fire, native projectile damage to an AI-disabled Bullet Kin, forced transition to tt5; three host-spawned blob enemies rendered in three observed client frames |
| Enemy assets and room entries | Pilot | Same base smoke coverage; native blob prefabs spawned in a generated NORMAL room, all three client sprite meshes/textures/animations checked in three frames; actual host/client PNGs inspected; native death damage invoked, followed by six additional NORMAL-room entries |

The extended case includes **four named combat rooms and two unnamed NORMAL rooms**, using native WarpToPoint/ForceChangeRoom with invulnerable players. These are artificial room entries, not natural walking or six cleared fights. The three explicit blobs had AI disabled for stable rendering checks. Other room simulation remained native. Invoking native death damage exercises callbacks but does not prove every split/death outcome separately.

The client continued receiving advancing world snapshots through the room sequence. An isolated negative native test supplied the correct enemy dependency but a wrong sprite-table fingerprint; resolution still rejected it. Both production sessions remained Running until intentional shutdown. There were no runtime exceptions in these successful logs. Stopping the host afterward produces an expected client disconnect/stop.

Two exploratory diagnostic runs were discarded: an observer plugin still referenced the old runtime assembly, and another observer looked up a blob after the host had killed it. The observers were corrected and the same production DLL was retested successfully. These observer failures are not counted as passing cases.

A private observational latency sample for the Hunter direct-loopback case recorded 1,727 input-to-presented acknowledged-source observations: mean 86.743 ms, p95 114.697 ms. Acknowledgement receipt had mean 26.313 ms and p95 60.179 ms over 1,752 observations. This includes the existing 60 ms display buffer and sparse instrumentation. It is **not physical two-PC/Radmin evidence, a measured improvement over 0.3.1, or a response guarantee**. Cold synchronous asset loading can still cause a short hitch.

All game tests were silent and windowed at 800×600, with WS_EX_NOACTIVATE, unconstrained cursor and background simulation. No Computer Use was used. Nine normal save-directory files retained their hashes. Test processes were stopped after evidence collection. Private game copies, logs, saves, screenshots and observer binaries remain outside publication packages; source includes only a sanitized summary.

## Distribution and limits

The release contains a Thunderstore ZIP and a source ZIP. No updated launcher is built or included. Both PCs require **0.3.2**; old peers are rejected. The package builder checks manifest/runtime/validation agreement and preserves older release archives. A package inspection is distinct from an actual manager ZIP import or public upload.

- Fourth-floor Sell Creep map mismatch remains unresolved; full natural campaign completion is unverified.
- Two physical PCs over Radmin, all enemies/items/abilities, natural boss/elevator progression, endings, long sessions and modpacks are not covered by these tests.
- The fix supports enemy collections with a discoverable native prefab owner. Unknown assets or real table mismatches still stop the session. This is not a promise that every missing-asset stop is resolved.
- Complete client audio, effects/materials, beams, secret-wall changes and shop/dialogue UI are incomplete. Historical stock/highlight evidence does not establish complete shopping support.
- Display interpolation remains 60 ms; client prediction is absent. Further latency reduction is not claimed.
- Ordinary fullscreen startup, cursor hover and a fresh actual manager ZIP import were not separately tested in this iteration.
- Active-run reconnection and host migration remain unsupported. Return to the foyer after a fault.

MIT attribution is preserved. Game assets/assemblies, BepInEx binaries and personal diagnostic logs are excluded. No upload or public availability is established by this report.
