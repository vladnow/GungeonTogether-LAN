# Guest movement response evidence for 0.3.5

The current production runtime is `96c874eecead6eb57a668b3e3f605938900cff86ad33c7af62feb38a32924dbf`. See `native-summary.json` for sanitized case results and `publication/VALIDATION.md` for method and boundaries.

Four final scenarios used two local native game processes each: Pilot/Hunter movement and safety, then Pilot/Hunter HUD and muted audio-event regression. All passed artificial move/fire/native enemy damage, equal floor 1/2 geometry and a forced transition to tt5. No full natural campaign or physical two-PC/Radmin test is claimed.

Twelve synthetic walking starts per case measured time from a guest input sample to at least one native game pixel (0.0625 units) of presented displacement. Baseline 0.3.4 Pilot: median 80.456 ms, p95 91.576 ms. Final 0.3.5 Pilot: 11.738 / 29.060 ms; Hunter: 12.424 / 28.130 ms. These are local visual-response measurements, not hardware input-to-photon or authoritative damage timing. The baseline used the released 0.3.4 DLL, not a modified stand-in.

The final movement probes verified nonzero presented prediction, guest-owned body/hand/gun/outline parts, wall/pit terrain queries, prediction disabled during native pause/input override/roll and replay reset after actions/teleport. Prediction remains bounded and optional. Full physics rollback, all special movement and arbitrary dynamic blockers are not established.

Both HUD/audio regressions verified native icons and host-authoritative health/armor/consumables/equipment. Native sound dispatch was intercepted with output suppressed; no listening test was performed. All diagnostics were silent, 800x600 windowed, no-activate, background and cursor-unlocked. No Computer Use was used.

The fourth-floor Sell Creep layout mismatch remains unresolved. Raw logs, private fixture/probe code, paths, screenshots and game resources are excluded from public packages.
