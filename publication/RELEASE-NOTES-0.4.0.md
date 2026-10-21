# Gungeon Together LAN 0.4.0 alpha

## Changes

- Poll host packets before native input evaluation and send guest controls immediately after native actions update. Button edges bypass the regular send cadence.
- Present a bounded current-frame walking step locally while retaining host authority and wall/pit guards. Pauses, rolls, teleports and blocked input still stop/reset prediction.
- Increase full world updates from 30 Hz to an adaptive 60 Hz, backing off to 30 Hz when capture/encoding/compression or the outgoing queue exceed the budget. Stable 60 Hz streams can use a shorter interpolation buffer; jitter still raises buffering.
- Fix the guest map input reading Pause instead of the configured Map action. Holding/releasing the map action now opens/closes it locally; host-revealed rooms also add the native exits/unknown-room markers without exposing secret rooms.
- Protect remote action bindings when native controllers initialize, reload saved bindings or are reassigned. Reload remains separate from gun cycling and active-item use.
- Deduplicate equivalent sprite/material descriptions rather than allocating network IDs for each temporary outline material. This addresses the recorded 0.3.9 "Too many sprite collections" stop. Native stress coverage recreates 5,000 material instances without adding definitions.
- Replicate the native outline shader's atlas parameters per sprite/frame, including rotated packing and scale, and build outline geometry with the correct flags. This addresses missing outline data; every reported black-halo scene and boss shader is not established by the local probes.
- Add LAN CO-OP to the game's native title and pause menus, including keyboard/controller navigation. Ctrl+P still opens the overlay. Host/join from the title continues through normal character selection; the overlay yields so the player can choose a character, then reconnects automatically.
- Follow the game's English, Spanish, Simplified Chinese or Russian language setting. Unsupported game languages use English. No language selector or separate language setting is added. Technical diagnostics remain English.
- Add three independent host options under RULES: Separate money, Separate keys, and Personal chests and shops. Defaults preserve shared resources and ordinary native loot. Rules are sent to the guest before generation and cannot change during a run.
- Separate money credits native enemy coin drops to the last attacker. Boss-tagged run coins are split equally, alternating odd remainders; other/unattributed coins go to the collector. Permanent Hegemony Credits are unaffected.
- Separate keys retains independent key budgets. Disabling one resource option keeps that resource shared even when the other is separate. Balances persist through native floor changes.
- Personal ordinary chests have independent openings, key payments and random rewards. One player can leave a chest unopened and receive nothing. Rolls may coincide. Ground loot rejects the other player; legitimately held equipment remains visible to the partner and later normal drops can be traded.
- Finite ordinary run shop stock is purchased independently by each player, at the native price. Native purchase/steal callbacks remain in use. Buying one player's stock does not remove the partner's item or price label.

## Compatibility and limits

The guest map is view-only: the host performs fast travel. Local client teleport attempts are blocked because they would not move the authoritative players.

Both PCs require this exact 0.4.0 build, matching Steam 2.1.9 files and a clean BepInEx 5 profile. Older LAN peers are rejected. Use separate profiles for competing networking mods.

Personal loot applies to ordinary non-mimic, non-rainbow chests without forced/quest contents and ordinary finite run shop stock. Glitched, rainbow, mirror, truth and Rat chests, mimics, permanent unlock purchases and special quest behavior retain native shared rules. Native persistent shop stock retains its persistence. Personal chest breaking is disabled to protect the overlapping per-player copies. Lock-picking, fuses, transformations, every steal effect, special game modes and arbitrary modded chests/items are not established by this validation.

Boss splitting was checked with native currency pickups carrying synthetic boss attribution and conservation checks; every native boss reward path is not established. Native enemy reward attribution was checked through AIActor coin generation and native pickup.

See VALIDATION-0.4.0.md for exact final-build scenarios. All game probes use isolated copies/profiles, sound disabled, windowed no-activate execution, free cursor and background simulation. Artificial grants, warps and accelerated progression are stated explicitly. Controlled local runs are not proof of natural campaign completion, a Dragon victory, audible sound quality or physical two-PC/Radmin behavior. No public upload or fresh manager import is claimed.

## Attribution

Keep the inherited MIT license and NOTICE.md. The native menu entry adapts MIT-licensed Gungeon Online MenuButtons.cs (Copyright 2026 Alexander Vazquez). No game libraries/assets, external test tools or private diagnostic plugins are shipped.
