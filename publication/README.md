# Gungeon Together LAN


Experimental **two-player co-op for Enter the Gungeon** over LAN or Radmin VPN. Connect using **Host / Join by IPv4**. The host runs native co-op; the client controls a selected second character and renders replicated objects using its own game resources. During normal gameplay, each screen follows its own player: the host follows player one, and the client follows player two. Native manual camera sequences can temporarily use shared cinematic framing.

**0.3.5 is an early-floor testing alpha. The known fourth-floor Sell Creep layout mismatch remains unresolved and can stop a run. Full campaign completion is not established.** This update improves guest movement response with a fast player stream and bounded visual prediction. The graphical guest HUD and combat sound-event relay remain available. Some effects and interactions remain incomplete.

## Install or update

- Both Windows PCs need matching game files and **the same 0.3.5 build**. Built against Steam Enter the Gungeon **2.1.9**.
- Use **BepInExPack EtG 5.4.2101** / BepInEx 5 and a clean manager profile with one networking mod.
- Install Radmin VPN separately when using a virtual LAN.

When this version is published on Thunderstore, update the mod on both PCs and use **Start Modded**. For a prepared local package, install the loader in the profile, then import `GungeonTogether_LAN-0.3.5-Thunderstore.zip` through **Settings → Import local mod**. The local import may require installing the loader dependency separately.

Remove older LAN DLLs and other GungeonTogether variants from that profile first. Keep backups outside `BepInEx/plugins`. Use separate profiles for LAN and other networking mods. This release contains a Thunderstore package and source; no updated launcher is provided.

**0.3.5 cannot connect to 0.3.4 or earlier peers. Update both PCs.** Creating a package does not publish it. Source is available from [GitHub](https://github.com/vladnow/GungeonTogether-LAN); **Code → Download ZIP** downloads source, not an installable mod.

## Connect and play

1. Enter the foyer and choose a local character. **Esc** closes the LAN menu; **Ctrl+P** reopens it.
2. Host: **HOST → CREATE ROOM**. Listen address `0.0.0.0` accepts connections on all adapters; a specific local LAN/Radmin address also works.
3. Client: **JOIN**. Enter the host's IPv4, matching port and room code. Select Cultist, Pilot, Marine, Convict or Hunter under **PLAYER TWO**, then **JOIN BY IP**. The run character is separate from the client's foyer character.
4. The host sees the choice and presses **START RUN**. The client loads automatically after the map check. Character selection remains available until the run starts.

Default port: **TCP 27631**. Allow the game through the firewall for the intended network. Use a trusted LAN/VPN; the mod does not add encryption.

The guest uses normal movement, aim, fire, roll, reload, interact, blank, item and weapon-switch controls. The host controls pause, map and shared menus. Opening the LAN menu mutes guest input. Losing window focus during an active LAN run does not automatically pause the game; manual host pause remains available.

## Guest HUD and sound in 0.3.4

The guest's text status box is replaced by a graphical HUD: full/half/empty hearts, armor, blanks, cash, keys, the equipped gun and ammo/magazine counts, reload state, and active item with its charge bar. These values come from the host's player two. Icons are read from the local game; no game assets are included in the mod. This is a custom presentation HUD, not a full replica of every native inventory/menu screen.

Combat sound events (weapons, enemies, objects, character/footstep and voice categories) now travel from the host to the guest, with emitter positions and per-emitter Wwise switches such as weapon type. Playback uses the guest's own game sound banks and volume settings. Local duplicate combat events are suppressed while mirroring. Queues and emitters are bounded; events from an older floor and stale batches are discarded. Music/ambience, RTPC parameters, every looping-sound stop/callback and every modded sound bank are not fully synchronized. The tests keep audio output disabled, so audible balance and completeness still need listening on two PCs.

## Separate cameras retained from 0.3.3

The host camera uses player one's focus instead of averaging both characters. The client receives a separate view around player two. Native room framing can keep the host away from the room edge; manual boss/cinematic camera sequences retain their explicit framing.

Network sprite selection uses the guest view and the guest's reported viewport aspect, rather than the host's screen. Objects around the guest can therefore be transmitted even when they are outside the host view. Deferred native off-screen animation frames are included so their animations continue on the client.

The native shared-screen movement restriction is bypassed during the active LAN run. Wall/pit collision rules remain. This does not make the run two independent campaigns: room sealing, automatic regrouping for combat, floor transitions and other native co-op rules remain shared. Death/ghost/spectator behavior and every cinematic are not fully validated.

## Enemy asset fix retained from 0.3.2

Some enemy sprite collections are embedded inside enemy prefabs and cannot be loaded independently by their tk2d GUID. In 0.3.1, encountering Blobulin/Blobuloid/Blobulon could stop the client and then pause the host with `Missing or incompatible local sprite asset`.

The host now includes the native owning enemy's database GUID with the sprite definition. If the collection is absent locally, the client loads that prefab's dependencies from its own game files and verifies the collection GUID and ordered sprite-table fingerprint. It does not create a second simulated enemy or receive textures over the network. This also supports other enemy collections with an identifiable native owner; it is not proof that every asset in the game is covered.

The missing-asset and layout checks remain active. A real mismatch still stops the session instead of leaving invisible hazards. The exact old failure was reproduced before the fix. See `VALIDATION.md` for the current runtime hash and bounded native tests.

## Latency

The host sends a compact, **75-byte player-motion payload up to 60 times per second**, separately from full world snapshots at up to **30 Hz**. Capture runs after native physics, and input acknowledgements refer to input consumed by the native player controller. Guest input also runs at up to 60 Hz with a scheduler that preserves its cadence through ordinary frame jitter. Actual rates depend on frame rate and load.

Normal guest walking uses bounded visual prediction of unacknowledged inputs. The body, hands, gun and attached outlines move together, and the guest camera follows that presented position. Prediction is limited to **100 ms / 0.65 game units**, stops on stale motion, and checks local walls, pits, room bounds and blocking bodies. Host pause, input overrides, rolls, room changes and teleports reset or disable it. Damage, pickups and gameplay outcomes remain authoritative on the host. This is visual anticipation, not complete client physics or rollback; corrections can still be visible.

The other world objects use adaptive interpolation: **40 ms initially, bounded to 35–90 ms** according to arrival jitter. The menu reports the current smoothing delay. To disable speculative walking, close the game and set `ClientMovementPrediction = false` under `[Network]` in the profile's `BepInEx/config/com.gungeontogether.lan.cfg`, then restart. The fast authoritative player stream remains active.

The menu's RTT measures the mod's round trip; its response figure measures an input acknowledgement and excludes display presentation. Neither is a Radmin ping. Local native movement measurements are documented in `VALIDATION.md`; they do not establish latency between two physical PCs. TCP retransmissions, low FPS, loading and congestion can still add delay. UDP and world delta compression are not implemented. Raising the world update rate can increase total traffic.

Historical constrained-network measurements belong to 0.3.1 and are retained in `publication/VALIDATION-0.3.1.md` and [the transport evidence](https://github.com/vladnow/GungeonTogether-LAN/blob/main/validation/latency-031/README.md).

## Saves

LAN progress is separate from normal progress:

`<game folder>/BepInEx/GungeonTogetherLAN/dungeon-profile`

Normal unlocks and currency are not imported. Initial options/display preferences may be inherited once, with a backup for migrated LAN display fields. Later LAN display choices are preserved.

Manager profiles sharing a game folder also share this LAN save directory. With default settings, direct Steam launches use normal saves and disable LAN. Back up both save directories before changing installations.

The client temporarily imports the host's generation state. Normal disconnect restores its previous in-memory context; crash recovery is not guaranteed. Advanced setting: **Profile / SaveProfileMode** in `com.gungeontogether.lan.cfg`: `Auto`, `Normal` (LAN disabled), or `LAN`. Restart after changing it. Manual launches can use `--gt-lan-profile`.

## Limits

- **Fourth-floor Sell Creep map mismatch remains unresolved.** Geometry checks stop mismatched runs.
- A complete natural campaign, all bosses/items/abilities, normal boss/elevator progression, endings, long runs and modpacks remain unverified.
- Guest sound/HUD coverage has the limits above. Some materials/effects, beams, secret-wall changes and shop/dialogue UI remain incomplete. Client price text is absent. The shop stock/highlight checks recorded for 0.3.1 do not establish complete shopping support.
- A disconnected run pauses. Return to the foyer and create a new session; active-run reconnection and host migration are not implemented.
- Native player/foyer startup warnings have been observed. This release does not claim that all freezes or startup errors are resolved.

Native checks use two local game processes, not two physical PCs over Radmin. Read `VALIDATION.md` for the actual scenarios and their limits.

## If the game stops

Before returning to the foyer, capture the full status text below the main button and open **DETAILS**. An automatically opened LAN menu can indicate a stopped session, not a normal pause. Closing the menu alone does not recover a faulted run.

Keep **both players'** `BepInEx/LogOutput.log` files before launching again. For Thunderstore Mod Manager, open the profile folder from the manager; the log is in its `BepInEx` directory. Include versions, host PC, selected heroes, last action and RTT/response figures in a [GitHub issue](https://github.com/vladnow/GungeonTogether-LAN/issues). Remove personal identifiers before publishing logs.

## Source and license

Based on [GungeonTogether by lam and contributors](https://github.com/llamerrr/GungeonTogether), upstream commit `52be4b636a07f28483ae4e17b16ad471065e89ce`. **Copyright (c) 2025 lam.** The original MIT notice is preserved; LAN changes use the same license. This is an unofficial derivative.

Prepared releases use a separate `release/<version>/` folder containing the packages, checksums and standalone release notes. `Build-Thunderstore.ps1` creates this layout and refuses existing version files.

Build with a .NET SDK and your local game/BepInEx libraries using `Build-LAN.ps1` with `GamePath` and `BepInExCorePath`. Game resources, game assemblies and BepInEx binaries are excluded from distribution. Developed with AI assistance.
