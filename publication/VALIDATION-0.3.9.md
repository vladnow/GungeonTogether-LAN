# Validation: GungeonTogether LAN 0.3.9 alpha

Runtime SHA-256: `3963e65f3dff187e28b480756339265df90827abde2f5d16721ca67878c1d063`.

- **375 automated checks** passed, including real sockets, protocol validation, rendering, effects, inventory and responsiveness.
- **66 native managed bindings** resolved against the installed Steam 2.1.9 game assemblies.
- **2 completed local native scenarios / 4 processes**, with sound disabled, 800x600 windows, no activation, an unconstrained cursor and background simulation.
- Both peers reached Castle, Gungeon Proper, Mines, Hollow and Forge with five matching layout hashes in each scenario. The client acknowledged fresh matching health/armor/ammo/position snapshots and advancing rendering on floors two through five.

## Reproduced defect and fix

On the original failing seed, 0.3.8 reached the fourth floor and stopped at the layout barrier. Native getter traces show the host rejected mastery-gated Sell Creep with count zero, while the client had no placeholder primary player and incorrectly accepted it. The generation envelope now includes the authoritative mastery count. The generating client evaluates that specific gate using the host count while preserving other native prerequisites. Zero rejects and positive count accepts on both peers. The host and offline getter behavior are unchanged.

## Scenario scope

See validation/campaign-039/native-summary.json for individual layouts, native control-release counts, checkpoint counts and any accelerated boss clears. Stock Castle checks include remote movement, firing and projectile damage. Extra room samples use teleports and invulnerability; boss clears use artificial native damage. Native main-floor loading and selected native elevators are exercised. These are not proof of natural walking through every room or a legitimate final-boss victory.

Preliminary diagnostic attempts were retained privately and are excluded from the completed-run count. Their driver was corrected to select main bosses rather than subbosses, respect native boss intro/death-camera input locks, approach elevators on solid floor until arrival, avoid a duplicate departure call, and support the expected override destination. No blanket input-unlock patch was added. The final scenarios separately check input release after each floor load.

## Preservation and limits

All **563** pre-existing release files and **8** tracked normal save/profile files match their before-work hashes. Tests use separate copies/profiles. No normal saves were restored over newer progress. Owned test processes have been stopped.

No full natural campaign, physical two-PC/Radmin play, audible listening test, fresh manager ZIP import or remote publication was performed. Every special room/NPC/puzzle, all projectile/particle/shader variants, dynamic tilemap changes, rare item combinations and full dual-wield/foldout HUD layouts remain outside this coverage. Native cinematics retain their intended input restrictions. Unknown required assets can still stop a session. The new fix targets the demonstrated fourth-floor prerequisite mismatch; it does not establish that every reported freeze has that cause.
