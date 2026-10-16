# Validation of 0.3.4 alpha

Date: 2026-10-16. **Runtime SHA-256: `d3594272f979a4c03d0b70a82a7c8f89f4b92976c2209281a8b7913ff85fce61`.** Both final native scenarios used this exact DLL. Historical validation reports remain separate.

**This is an early-floor alpha. The fourth-floor Sell Creep layout mismatch remains unresolved. Full natural campaign completion is not established.**

## Change

The guest now draws a custom graphical HUD from authoritative player-two state: current/max health, armor, blanks, cash, keys, equipped gun/item IDs, ammo, magazine count/capacity, reload and active-item state. It reads local native UI atlases and pickup sprites, including rotated atlas packing, without distributing game resources or modifying client placeholder inventory/health to drive widgets.

Host combat PostEvent calls are captured as bounded event-ID batches with emitter positions and per-emitter Wwise switches. The guest dispatches them into its own Wwise engine and suppresses duplicate local combat events while mirroring. Local volume settings are not forwarded. Sender queues, source objects and batch sizes are bounded. Old-floor, duplicate and stale batches are dropped; a clock calibrated from snapshots also detects a delayed FIFO sound arriving before a delayed world frame. Sources are stopped/disposed during teardown and after inactivity. Periodic GT_NETWORK diagnostics now include audio counters.

The shared world, separate camera policy, embedded enemy-resource loading and 60 ms snapshot interpolation remain. No further input-latency reduction is claimed.

## Automated checks

**265 checks passed:** transport 32, dungeon protocol 84, profile policy 19, rendering/characters 38, display migration 9, acknowledgements 9, sprite visibility 20, constrained transport 24, and presentation 30.

New coverage includes equipment/max-health wire data, half-heart values, invalid equipment/counts, bounded sound bursts and switches, non-finite positions/timestamps, zero event/emitter IDs, duplicate switch groups, trailing bytes, duplicate/out-of-order/old-floor events, sequence wrap, stale audio under FIFO congestion, clock recovery/new-floor calibration and rejection of a 0.3.3 peer.

**36 native managed targets/contracts passed:** 22 declarative Harmony targets and all twelve PostEvent plus two SetSwitch overloads. The sound wrappers in the installed assemblies call native Wwise directly, rather than nesting PostEvent wrappers and duplicating captures. Compilation used installed Steam Enter the Gungeon 2.1.9 and BepInEx 5 libraries.

## Final native scenarios

Two private local processes per scenario, with Pilot as host and these guests:

| Guest | Verified client PostEvent invocations | Distinct event IDs | Native SetSwitch calls |
| --- | ---: | ---: | ---: |
| Pilot | 77 | 17 | 69 |
| Hunter | 104 | 17 | 64 |

Both first passed equal floor-1/floor-2 layout barriers, artificial remote movement/fire, native projectile damage to an AI-disabled Bullet Kin, interpolation and a forced native transition to tt5. They then changed the host's native guest state to 2.5/3 health, 2 armor, 3 blanks, 31 cash and 4 keys, acquired gun 15 and active item 108, and checked that the client received those values and actually repainted the graphical HUD with nine local UI assets plus the selected equipment. Actual client PNGs were inspected for both final cases.

The real first-floor gun-shot event reached the client before a synthetic second-floor sound cue. Client numeric PostEvent and SetSwitch hooks verified dispatch across the network. These counts span both floors; the session's own per-floor counters reset at transitions.

## Silence and desktop safety

Every launch used -nosound, an active all-overload diagnostic PostEvent suppression patch and a paused Unity listener. The private probe allowed native gun code to reach PostEvent for capture tests; the sound-engine calls remained suppressed. All 77/104 relayed PostEvent invocations returned the forced zero playing ID. The Unity volume getter reported 1 in this no-sound build, so validation does not rely on a zero volume reading as proof of silence. No listening test was performed and audible balance/bank coverage is not established.

All four final game processes verified 800x600 windowed operation, WS_EX_NOACTIVATE, an unconstrained cursor, background simulation and no foreground activation at the desktop-safety check. No Computer Use was used. Owned test processes were stopped after pass markers; the resulting peer-disconnect messages are expected controlled teardown. No game processes remain running. Nine normal save files and 110 existing release files retained their hashes. The private LAN test saves were allowed to change.

## Boundaries

- These are artificial local two-process scenarios, not two physical PCs over Radmin, ordinary room clearing, boss/elevator progression or full-run completion.
- Audible playback, spatial balance, every enemy/weapon event/bank, RTPC parameters, music/ambience, every looping-stop/callback variant and third-party sound banks are unverified or incomplete. Sources idle for five seconds are stopped; long continuous effects can therefore differ.
- The guest HUD is a custom presentation layer, not the full native inventory, map, shopping, dialogue or all special-key interfaces. Some effects/materials and beams remain incomplete.
- The fourth-floor Sell Creep mismatch is unresolved. Active-run reconnection/host migration and fresh manager ZIP import were not tested. No public upload was performed.

Sanitized current evidence: `validation/summary.json` and `validation/presentation-034/native-summary.json`. Raw local logs and game screenshots are excluded from distribution.
