# Separate player cameras — 0.3.3

The host uses the primary player's native camera focus. Ordinary guest snapshots contain a virtual camera around the secondary player; sprite selection uses that view and the guest-reported aspect. Host camera transforms are not temporarily moved to generate packets. Native manual camera control retains explicit cinematic framing.

The native shared-screen movement-boundary method is bypassed only during an active host LAN run. The patch does not change an existing collision rejection. Other collision and native co-op rules remain.

Native sprite Collection/spriteId getters flush deferred invisible animation updates. Capture reads them before renderer bounds, preserving current off-screen sprite geometry in the guest view.

See `native-summary.json` for the exact tested DLL, native cases, viewport fixtures and limits. The full report is `publication/VALIDATION.md` in the source archive. Native evidence uses private silent, windowed, no-activate game copies; it is not a physical two-PC/Radmin or complete campaign test.

Current background checks: transport 31, protocol 84, profile 19, rendering/characters 38, display 9, acknowledgements 9, sprite visibility 20, constrained transport 24: **234 checks**, plus **22 managed patch-target checks**.

```powershell
.\Test-LAN.ps1
.\Test-Dungeon.ps1
.\Test-Profile.ps1
.\Test-Rendering.ps1
.\Test-Display.ps1
.\Test-Acknowledgements.ps1
.\Test-SpriteVisibility.ps1
.\Test-Latency.ps1 -ExpectFresh
# Test-GameBindings.ps1 takes local GamePath and BepInExCorePath.
```

Raw local logs, private observer binaries, screenshots, saves and game assets are excluded from source/runtime packages. Historical 0.3.2 evidence remains separate. The fourth-floor Sell Creep mismatch is still unresolved; death/ghost/cinematic combinations and all assets remain incomplete or unverified.
