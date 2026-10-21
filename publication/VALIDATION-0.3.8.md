# Validation: GungeonTogether LAN 0.3.8 alpha

Runtime SHA-256: `1e05dcabeb88341cd317ece450aae2ec1fc1e1afae61ab0df06d56d76b31f8cd`.

- **375 automated checks**, including real socket tests and malformed-input/scene/material/protocol tests. The new effects suite contains 39 checks and the responsiveness suite 37.
- **65 exact native managed bindings** resolved against the installed Steam 2.1.9 assemblies.
- **3 local native scenarios / 6 processes**, all using the runtime hash above, with output muted, 800x600 windows, no activation, background simulation and an unconstrained cursor.

## Native scenarios

1. Pilot: native remote movement, firing and projectile damage; matching Castle/Gungeon Proper/Mines layout hashes; shader/material identity; original guest HUD; coordinated doors; native price label, boss widget and map; dialogue; poison/fire; native hurt vignette/material tint; permanent third-floor goop clocks transmitted without numeric disconnect.
2. Hunter: repeat the same scenario with a different guest character and generation, including the third-floor clock fixture.
3. Keyboard responsiveness: menu-navigation bindings accompany the synthetic movement keys, with wall/pit clipping, native pause/action/teleport resets and twelve movement starts. Mean world capture time was 2.752 ms in this run (maximum 21.714 ms).

The floor checks use controlled transitions, not natural play through every intervening room. Boss bars are exercised with a registered native health fixture; this is not a complete natural boss encounter. A price-label fixture verifies the native label rendering path, not every shop/NPC behavior.

## Response measurements

Current local synthetic input-sample to at least 0.0625 presented game units: median **14.118 ms**, P95 **22.133 ms**, 12 starts. The prior 0.3.7 local sample was 14.961 / 41.424 ms, with twelve starts. These small, separately timed background runs are not a controlled physical input-to-photon comparison or a Radmin guarantee. RTT and acknowledgement counters do not measure display latency.

## Preservation and limits

All **554** prior release files match the before-work hashes. All **8** tracked normal save/profile files match the current checkpoint. Tests use separate copies/profiles; normal saves were not restored over newer progress. Test processes have been stopped.

No physical two-PC/Radmin test, audible listening test, fresh manager ZIP import, full natural campaign or remote upload was performed. The reported remote dark halo and all boss projectile variants still require comparison on the affected PCs. Material state preserves a bounded native property allowlist; not every custom shader/particle/mesh effect is covered. Guest map teleport selection, full inventory foldouts/dual-wield layouts and all special doors/puzzles/NPC sequences are not established. Native cinematics retain intentional input locks. Multiple segments in one boss controller use a combined guest health fraction. The known fourth-floor Sell Creep generation mismatch remains unresolved.

See `validation/feedback-038/native-summary.json` in the source archive for sanitized evidence. Raw game assets/logs, user video, screenshots, diagnostic binaries and personal paths are excluded from public packages.
