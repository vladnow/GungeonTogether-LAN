# Validation: 0.4.0 alpha

Runtime SHA-256: `b013bf5e9cedb630ce06a8864ad47abc9cff9eb5560033bc9064e81669b99e0a`.

- **1620 automated assertions** passed: real socket/protocol tests, rendering/effects/inventory/input checks and rule/language validation. The new rules suite checks all eight rule combinations, malformed messages, reward conservation across 1,001 amounts, and complete translations. These assertions are not separate full gameplay sessions.
- **94 exact native managed bindings** resolved against installed Steam 2.1.9 assemblies.
- Final-runtime two-process feature scenarios: all options enabled, all options disabled, and separate money with shared keys. Each includes the base lobby/movement/shooting/projectile-damage checks and multiple native floor loads.
- Separate keys with shared money was additionally exercised on the preceding candidate; that candidate differs in inventory-claim cleanup and view filtering. It is not counted among final-runtime smoke passes.
- Native enemy coin generation preserves attacker attribution; tagged native boss currency pickups conserve and split money. Unmapped HUD wallets cannot modify player balances. Rules cannot change after start or from the client.
- Two native chest openings pay separately, leave the other chest unopened until chosen, and create individually owned randomized loot. The guest receives/renders their chest without the overlapping host copy. Wrong-player ground gun pickup is rejected; the native carried gun copy remains visible to the partner.
- Native run shop purchases charge each player once. The host's purchase retains the guest's stock and visible price label. Guest HUD money and wallet persistence across floor loading were acknowledged.
- Final-runtime title test verifies native button/navigation, supported game-language changes, unsupported Japanese -> English fallback, the native character-selection handoff and the foyer listener. Screenshots were inspected locally. There is no language selector. Character selection is invoked by a diagnostic native callback, not physical mouse/controller input.
- Final-runtime regression probes verify the configured Map action opens/closes the actual guest map, Pause/Reload cannot trigger it, and a newly host-revealed room adds a real native unexplored exit. Secret-room exits remain hidden. Guest local teleport attempts are blocked; fast travel is host-controlled.
- All native remote action bindings remain exclusive after controller initialization, saved-binding reload and reassignment. Each isolated action is checked against other actions. Input is injected diagnostically without changing focus or sending desktop keystrokes.
- Two final-runtime latency runs use two isolated native processes and a TCP shaper adding 7.5 ms per direction. Each records 12 walking starts and 12 native projectile appearances on a clear artificial lane. They retain pause/roll/input-block/teleport prediction resets and wall/pit guards. These are native presentation measurements after diagnostic action injection, not physical keyboard-to-monitor timing.
- Recreating 5,000 native materials leaves 20 distinct definitions at 20; the recorded 0.3.9 material-table exhaustion is addressed without raising the 4,096-definition limit. Native outline atlas values, including rotated packing, reach rendered player/weapon proxies. Not every reported black-halo scene is reproduced or visually certified.
- With all three options enabled, the final controlled campaign reached Castle, Gungeon Proper, Mines, Hollow and Forge with matching layouts and 17 host/client checkpoints. It uses artificial room warps, invulnerability and level progression. No natural full run or Dragon victory is established.
- A second final-runtime campaign uses artificial boss damage on floors 2-4, native boss completion and elevator transitions, then observes the Forge boss introduction. Both peers acknowledge 25 checkpoints. Mastery rewards are deliberately suppressed by the probe; the Forge boss is not defeated.
- All game tests use isolated copies/profiles, disabled sound, 800x600 windowed no-activate execution, an unconstrained cursor and background simulation. No Computer Use was used.
- 10 checkpointed normal save/options/backup files and 695 previous-release files retained their SHA-256 values.

The normal diagnostic output log differs from the older checkpoint; it is not a save and was not restored or overwritten. Its last-write time precedes the current regression probes.

## Controlled latency comparison

The baseline is a superseded 0.4.0 candidate (`1355d1025ee6137f6ce572952fa4889ddda8957794c3b35c5d9eefbf886dd122`), not a shipped release and not a physical Radmin run. The probe, artificial network delay and sample counts are the same. Times below are milliseconds; each p95 is from only 12 samples and does not establish a long-session guarantee.

| Measurement | Baseline | Final build, run 1 | Final build, repeat |
| --- | ---: | ---: | ---: |
| Walking presentation median | 10.944 | 3.159 | 3.262 |
| Walking presentation p95 | 28.835 | 5.070 | 23.041 |
| Native projectile presentation median | 104.946 | 66.109 | 70.183 |
| Native projectile presentation p95 | 195.148 | 155.205 | 110.111 |
| Sampled input acknowledgement median | 32.734 | 33.611 | 32.539 |
| World update interval mean | 33.472 | 16.996 | 16.789 |

Walking uses bounded visual prediction; host authority, native projectile creation and hit decisions remain on the host. Input acknowledgement did not materially improve in these runs. The improvement concerns presentation and world update cadence; it is not a claim of 3 ms network latency. Loaded hosts can fall back from 60 Hz to 30 Hz, and real two-PC/VPN conditions can differ. Client teardown disconnect messages after both probe passes are recorded separately from gameplay failures.

See `validation/features-040/native-summary.json` for per-scenario evidence and exact layout hashes. Earlier failed diagnostic trials and superseded candidates are excluded from final pass counts; private probes and raw logs are not distributed.

## Boundaries

No physical two-PC/Radmin test, audible listening test, fresh manager import, public upload, natural full campaign or final-boss victory was performed. Boss splitting coverage uses synthetic boss attribution; every real boss reward path is not established. Special/quest/rainbow/mimic chests, all lock-pick/fuse/transformation effects, every shop/steal variant, special game modes, arbitrary modded items, all particle/shader variants and long-run performance are not covered. Personal ordinary chests are damage-protected and cannot be broken. Native persistent shop stock retains native behavior. Other networking mods require separate profiles.
