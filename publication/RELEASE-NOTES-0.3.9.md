# GungeonTogether LAN 0.3.9 alpha

Fix a reproduced fourth-floor generation mismatch: the host rejected Sell Creep without a mastery token, while the client accepted it because its placeholder primary player had been cleared during loading. The layout barrier then correctly stopped the session.

## Implemented

- Include the host primary player's run mastery-token count in the generation envelope.
- Evaluate native mastery-gated injection on the generating LAN client against that authoritative count, even while its placeholder controller is absent.
- Preserve other native prerequisites, eligible Sell Creep generation, host behavior, offline behavior and the layout-hash safety check.
- Report actual native input-override keys in DETAILS, using the installed game's override list format. Preserve native input restrictions.
- Retain the 0.3.8 rendering, native HUD/map, shop labels, boss widgets, coordinated ordinary doors, separate cameras, shared cash/ordinary keys and input prediction.

## Compatibility

Both PCs must install **0.3.9**. Earlier peers have a different protocol. Built against Steam Enter the Gungeon 2.1.9 and BepInExPack EtG 5.4.2101. Use a clean mod-manager profile with one networking mod. Keep older DLL backups outside BepInEx/plugins. No launcher is included.

## Checks and limits

See VALIDATION-0.3.9.md for the completed runs and exact runtime hash. Controlled local two-process tests reached the fifth main floor after the fix. The original failing seed and both mastery-gate outcomes were checked.

Tests use separate game copies/profiles with sound disabled, windowed no-activate execution, background simulation and an unconstrained cursor. Artificial warps, invulnerability, accelerated native boss damage and forced progression are explicitly recorded. These checks do not establish a full natural room-by-room campaign, final-boss victory, audible output quality, or physical two-PC/Radmin performance.

Unknown required assets can still stop a session. Every boss projectile/shader, special room/NPC/puzzle, dynamic tile change, particle effect, dual-wield/foldout HUD and rare item combination is not covered. Native cinematic input locks remain. No automatic revival on every floor is added. Guest map teleport selection is not enabled.

Report remaining problems through [Discord](https://discord.gg/wuZQBhbrHk) or GitHub Issues, with both players' logs and the LAN menu DETAILS before restarting. Preparing these archives does not upload or publish them.
