# Gungeon Together LAN

[Русская инструкция](https://github.com/vladnow/GungeonTogether-LAN/blob/main/README.ru.md)

Experimental **two-player co-op for Enter the Gungeon** over LAN or Radmin VPN. Connect using **Host / Join by IPv4**. The host runs native co-op; the client controls a selected second character and renders replicated objects using its own game resources. Both players share the host's camera.

**0.3.2 is an early-floor testing alpha. The known fourth-floor Sell Creep layout mismatch remains unresolved and can stop a run. Full campaign completion is not established.** Client audio, effects and some interactions remain incomplete.

## Install or update

- Both Windows PCs need matching game files and **the same 0.3.2 build**. Built against Steam Enter the Gungeon **2.1.9**.
- Use **BepInExPack EtG 5.4.2101** / BepInEx 5 and a clean manager profile with one networking mod.
- Install Radmin VPN separately when using a virtual LAN.

When this version is published on Thunderstore, update the mod on both PCs and use **Start Modded**. For a prepared local package, install the loader in the profile, then import `GungeonTogether_LAN-0.3.2-Thunderstore.zip` through **Settings → Import local mod**. The local import may require installing the loader dependency separately.

Remove older LAN DLLs and other GungeonTogether variants from that profile first. Keep backups outside `BepInEx/plugins`. Use separate profiles for LAN and other networking mods. This release contains a Thunderstore package and source; no updated launcher is provided.

**0.3.2 cannot connect to 0.3.1 or earlier peers. Update both PCs.** Creating a package does not publish it. Source is available from [GitHub](https://github.com/vladnow/GungeonTogether-LAN); **Code → Download ZIP** downloads source, not an installable mod.

## Connect and play

1. Enter the foyer and choose a local character. **Esc** closes the LAN menu; **Ctrl+P** reopens it.
2. Host: **HOST → CREATE ROOM**. Listen address `0.0.0.0` accepts connections on all adapters; a specific local LAN/Radmin address also works.
3. Client: **JOIN**. Enter the host's IPv4, matching port and room code. Select Cultist, Pilot, Marine, Convict or Hunter under **PLAYER TWO**, then **JOIN BY IP**. The run character is separate from the client's foyer character.
4. The host sees the choice and presses **START RUN**. The client loads automatically after the map check. Character selection remains available until the run starts.

Default port: **TCP 27631**. Allow the game through the firewall for the intended network. Use a trusted LAN/VPN; the mod does not add encryption.

The guest uses normal movement, aim, fire, roll, reload, interact, blank, item and weapon-switch controls. The host controls pause, map and shared menus. Opening the LAN menu mutes guest input. Losing window focus during an active LAN run does not automatically pause the game; manual host pause remains available.

## Fix in 0.3.2

Some enemy sprite collections are embedded inside enemy prefabs and cannot be loaded independently by their tk2d GUID. In 0.3.1, encountering Blobulin/Blobuloid/Blobulon could stop the client and then pause the host with `Missing or incompatible local sprite asset`.

The host now includes the native owning enemy's database GUID with the sprite definition. If the collection is absent locally, the client loads that prefab's dependencies from its own game files and verifies the collection GUID and ordered sprite-table fingerprint. It does not create a second simulated enemy or receive textures over the network. This also supports other enemy collections with an identifiable native owner; it is not proof that every asset in the game is covered.

The missing-asset and layout checks remain active. A real mismatch still stops the session instead of leaving invisible hazards. The exact old failure was reproduced before the fix. See `VALIDATION.md` for the current runtime hash and bounded native tests.

## Latency

The queue/compression improvements from 0.3.1 remain: stale unsent world states are replaced by newer ones, while reliable control messages retain their order. The display buffer is **60 ms**, with dungeon snapshots up to **20 Hz**. There is no client prediction.

The menu's RTT measures the mod's round trip. Its response figure measures guest input to a confirming host snapshot and excludes display interpolation. Neither is a pure Radmin ping or a guarantee. Low FPS, resource loading, congestion and retransmissions can still add delay. **0.3.2 targets the missing-enemy-asset stop; it does not claim a further latency reduction.**

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
- Client combat audio, some materials/effects, beams, secret-wall changes and shop/dialogue UI remain incomplete. Client price text is absent. The shop stock/highlight checks recorded for 0.3.1 do not establish complete shopping support.
- A disconnected run pauses. Return to the foyer and create a new session; active-run reconnection and host migration are not implemented.
- Native player/foyer startup warnings have been observed. This release does not claim that all freezes or startup errors are resolved.

Native checks use two local game processes, not two physical PCs over Radmin. Read `VALIDATION.md` for the actual scenarios and their limits.

## If the game stops

Before returning to the foyer, capture the full status text below the main button and open **DETAILS**. An automatically opened LAN menu can indicate a stopped session, not a normal pause. Closing the menu alone does not recover a faulted run.

Keep **both players'** `BepInEx/LogOutput.log` files before launching again. For Thunderstore Mod Manager, open the profile folder from the manager; the log is in its `BepInEx` directory. Include versions, host PC, selected heroes, last action and RTT/response figures in a [GitHub issue](https://github.com/vladnow/GungeonTogether-LAN/issues). Remove personal identifiers before publishing logs.

## Source and license

Based on [GungeonTogether by lam and contributors](https://github.com/llamerrr/GungeonTogether), upstream commit `52be4b636a07f28483ae4e17b16ad471065e89ce`. **Copyright (c) 2025 lam.** The original MIT notice is preserved; LAN changes use the same license. This is an unofficial derivative.

Build with a .NET SDK and your local game/BepInEx libraries using `Build-LAN.ps1` with `GamePath` and `BepInExCorePath`. Game resources, game assemblies and BepInEx binaries are excluded from distribution. Developed with AI assistance.
