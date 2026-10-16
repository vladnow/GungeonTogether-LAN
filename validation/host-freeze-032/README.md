# Missing native enemy assets — 0.3.2

Date: 2026-10-16. See `native-summary.json` for the exact runtime hash, peer floor hashes and case results. The full report is `publication/VALIDATION.md` in the source archive.

0.3.1 was reproduced stopping on BlobulinSpriteCollection when the host spawned native blob enemies that were not loaded in the client process. A user log also contained BlobulonSpriteCollection stops. These collections are embedded in native enemy prefabs; their assetName fields are empty. Loading by the prefab database GUID resolves the local dependencies without transmitting textures or creating another enemy instance. Collection identity and ordered-table fingerprints remain mandatory.

Two successful 0.3.2 cases used Hunter and Pilot, each in two local game processes. Both passed move/fire/native-damage tests, matching first/second-floor barriers, and explicit Blobulin/Blobuloid/Blobulon sprite checks. The extended case used artificial invulnerable room warps and native death damage; six further NORMAL-room entries (four named and two unnamed) continued without a session fault. A deliberately wrong sprite-table fingerprint was rejected. It is not a natural walkthrough or full campaign test.

All processes were silent, small windowed, no-activate, with an unconstrained cursor and background simulation. No Computer Use. Nine normal save files retained their hashes.

Automated commands, run from the project directory after building against local libraries:

```powershell
.\Test-LAN.ps1
.\Test-Dungeon.ps1
.\Test-Profile.ps1
.\Test-Rendering.ps1
.\Test-Display.ps1
.\Test-Acknowledgements.ps1
.\Test-SpriteVisibility.ps1
.\Test-Latency.ps1 -ExpectFresh
# Test-GameBindings.ps1 additionally takes GamePath and BepInExCorePath.
```

Totals: 224 automated checks plus 20 managed patch-target checks. Native observers were private, opt-in diagnostics; two invalid observer attempts were corrected and rerun, as recorded in the summary. They are not included as successful cases. The source archive excludes raw personal logs, game copies/assets, saves, screenshots and private diagnostic binaries.

Physical two-PC/Radmin behavior and a complete natural campaign remain unverified. The fourth-floor Sell Creep mismatch is unresolved. No further latency improvement is claimed by this change.
