# GungeonTogether LAN 0.3.8 alpha

This update addresses the reported third-floor numeric disconnect, guest rendering artifacts, missing interface elements and forced room entry. Both PCs must update to **0.3.8**; earlier versions use a different network protocol.

## Implemented

- Keep original local sprite shaders, keywords, texture identity/UV transforms and supported color/emission/cutout parameters. Apply dynamic authoritative tint separately. This covers native shadow, gun and procedural projectile material variants without shipping game assets.
- Wait for both living players at ordinary doors, open immediately once both are close, and avoid the native forced-movement sequence. Defer a room seal until both living players enter so a trailing player is not pulled into a prematurely sealed room.
- Preserve unacknowledged walking input across ordinary adjacent-room changes, use host door state while clipping client prediction, and filter only small camera-anchor changes caused by animation/pixel rounding. Wall/pit checks and authoritative teleports remain.
- Mirror native default labels such as shop prices; drive native boss bar artwork with host health/name data. Controllers that contain several boss segments use a combined health fraction on the guest.
- Exclude local map artwork from world-sprite suppression, update native markers/exploration, and allow the guest to hold the normal map binding. Guest map teleport selection is not enabled.
- Use authoritative maximum ammo and keep native ammo widgets within the viewport.
- Normalize native goop clock sentinels (including permanent lifetimes), accept finite signed sprite geometry, and discard unrenderable non-finite host visual transforms with diagnostics. Received packet validation remains strict. Numeric failure messages identify the offending value/field and sprite context where available.
- Register network sound emitters with Wwise. Keep native hurt cues/switches and add a damage-counter fallback, with separate hurt dispatch/failure diagnostics. Preserve the first session failure when the transport disconnects.
- Add the support Discord link to the public README: https://discord.gg/wuZQBhbrHk . This becomes visible on Thunderstore when this package description is published.

## Compatibility and installation

Built against Steam Enter the Gungeon 2.1.9 and BepInExPack EtG 5.4.2101. Use matching clean mod-manager profiles and one multiplayer mod per profile. Import the Thunderstore ZIP or update after publication. No launcher is included. Keep older DLL backups outside BepInEx/plugins.

## Validation and limits

See VALIDATION-0.3.8.md for actual completed checks. Native tests are local, silent, windowed, no-activate and cursor-unconstrained. Audio dispatch can be inspected without audible playback; actual sound quality remains unverified.

This remains an early-floor alpha. Forced generation/transitions and controlled fixtures do not establish a full natural campaign, a complete boss fight, or physical two-PC/Radmin performance. The known fourth-floor Sell Creep layout mismatch remains unresolved. The exact remote dark-halo/jerk reproduction, every boss projectile/particle/custom shader, full dual-wield/foldout HUD layouts and all special doors/NPC/puzzle sequences are not established. Native boss introductions and other cinematics retain intentional input locks. No remote upload/publication is claimed.
