# Validation of 0.3.1 alpha

Date: 2026-10-16. This page separates controlled automated evidence from native gameplay checks.

**Current runtime SHA-256: `56c59bfba820cb46bfbae51eafb13e992a5f361608d954c4e7cb909571b55536`.** This exact DLL passed two bounded native scenarios, each with two local game processes. Historical 0.3.0 evidence remains separate in [VALIDATION-0.3.0.md](VALIDATION-0.3.0.md).

**Known blocker: a fourth-floor layout mismatch involving the Sell Creep room remains unresolved. Geometry checks remain enabled and stop mismatched runs. Full campaign readiness is not claimed.**

## Controlled transport evidence

The test uses real local TCP sockets through a byte-transparent proxy with a **128 KiB/s downstream bottleneck**. It produces incompressible **16 KiB state packets at 30 Hz for three seconds**; high-entropy padding prevents compression from removing the bottleneck.

| Measure | Reliable FIFO state | Replaceable latest state |
| --- | ---: | ---: |
| Idle mod echo RTT, p95 | 79.0 ms | 79.2 ms |
| Delivered-state age, p95 | 2063.1 ms | 341.3 ms |
| Interpolated sample age after 3 s | 2262.8 ms | 435.5 ms |
| Latest received sequence after 3 s | 23 | 78 |
| Peak application pending bytes | 1,048,992 | 16,436 |

Both modes preserve seven reliable control messages in order and eventually deliver the final produced state after the bottleneck is removed. The latest-state test also checks pending replacements, independent state keys and ordering around reliable messages. FIFO uses ordinary Send on the current transport, isolating delivery policy rather than comparing different runtime binaries.

The compressible-packet check restores a 32,768-byte payload byte for byte from 81 wire bytes. A separate 60 Hz sender / 10 Hz receiver-polling test observed a maximum sample age of 136.3 ms and a final interpolation-buffer count of three.

These are controlled queue/compression/interpolation measurements. **No game process, two physical PCs or Radmin network was used.** They do not establish running-dungeon FPS, input-to-visible-action latency or a guarantee for other snapshot sizes/connections. An already transmitting frame cannot be replaced, so its transmission time remains a lower bound.

Evidence and exact commands are in the source archive's `validation/latency-031/` directory. Recorded results: FIFO 16 checks; latest-state 24 checks. Do not add both totals to unrelated native test counts.

## Automated coverage and packaging

The current source passed **214 automated checks**: transport 29, dungeon protocol 66, profile selection 19, interpolation/characters 38, display migration 9, input acknowledgements 9, sprite visibility/material descriptors 20, and constrained transport 24. In addition, 20 managed Harmony targets resolve uniquely against the installed game libraries; six launcher fixture checks passed. Fixture checks do not establish a fresh real installation.

`Build-Thunderstore.ps1` verifies the manifest version and runtime hash against `validation/summary.json`, then requires current native validation for normal packaging. Historical 0.3.0 evidence cannot authorize a 0.3.1 package. `-DevelopmentPreview` is explicitly preliminary and still requires matching-version/hash background evidence.

## Native runtime evidence

The exact DLL passed two scenarios: Hunter through a byte-transparent proxy configured for 7.5 ms delay each direction and 128 KiB/s downstream, and Pilot over direct loopback. Both host/client pairs verified matching first/second-floor geometry, remote movement, real native guest projectiles damaging an AI-disabled Bullet Kin, selected-character retention, interpolation and a forced native transition to `tt5`. These are four process passes in two scenarios, not natural floor clears or boss/elevator progression. No runtime exception or stop occurred before the pass markers. Closing the test host afterward produces the expected client disconnect.

Client-clock instrumentation measured input enqueue to the presented acknowledged **source** snapshot. In the constrained game case, 812 observations had mean 100.643 ms and p95 121.549 ms; acknowledgement receipt p95 was 65.057 ms over 814 observations. Direct loopback presentation p95 was 102.141 ms over 1,592 observations. These are instrumented local game processes, not two PCs or Radmin. The proxy adds 15 ms total forwarding delay; engine polling/processing also contributes to the actual mod RTT.

One artificial stationary-to-moving input step exceeded 0.05 units in reconstructed interpolation after 104.042 ms; the logged source-frame position changed after 151.294 ms. Presentation logging is sparse and can miss earlier visible movement; this single step is not a distribution or a guarantee. Acknowledgement alone means host acceptance before capture and may precede the native action by a frame.

Fresh entrance-room captures show two character bodies and held gun geometry, without the previous obvious duplicated-body outlines. A separate artificial shop-room warp and native range callback verified eight stock sprites, eight outlines and one white highlight over three fresh client frames; actual host/client game screenshots now show the counter and table stock. Preserving native perpendicular/tilt geometry fixed an earlier invisible-stock result that proxy-presence checks alone had missed. Purchasing and physical range detection were not exercised, and client price text is still absent. This does not test every gun, item or material. The ordinary fullscreen startup, menu hover cursor and full guest HUD were not separately exercised visually: diagnostic mode intentionally skips the ordinary startup path and hides its HUD.

All game tests were silent, 800×600 windowed, with WS_EX_NOACTIVATE, unconstrained cursor and background simulation. No Computer Use was used. Nine files in the normal save directory retained their hashes. Native logs, screenshots, private observer plugins and game copies remain local; only a sanitized measurement summary is included in source.

Previous source inspection and native diagnostics motivated the fixes. They do not establish that the user's reported freezes have one confirmed cause or that every freeze is resolved.

No complete natural campaign, physical two-PC/Radmin session, fresh launcher installation or actual manager ZIP import is established by these tests.

## Remaining limits

- Fourth-floor Sell Creep geometry mismatch: unresolved; no full-run readiness.
- Full campaigns, all bosses/items/abilities, natural boss/elevator progression, revival combinations, endings, long runs and modpacks remain unverified.
- Client combat audio, some materials/effects, beams, secret-wall tile changes and shop/dialogue UI remain incomplete. Complete item outlines are not claimed without native asset evidence.
- The display buffer is now 60 ms. It adds latency; client prediction is absent. Low FPS, asset loads, congestion and TCP retransmissions can still cause delay.
- The menu's RTT is measured by the mod. Its response figure measures input-to-host-snapshot acknowledgement and excludes display interpolation; neither is pure VPN ping.
- Native startup/player warnings and earlier unexpected exits are not proven fixed by passing transport checks.
- A lost connection pauses the run; active-run reconnection and host migration are unavailable.

Private native logs, screenshots, saves, game resources and game/BepInEx assemblies are excluded from publication packages. MIT attribution remains intact. No upload or public availability is established by this document.
