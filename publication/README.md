# Gungeon Together LAN

[Русская инструкция](https://github.com/vladnow/GungeonTogether-LAN/blob/main/README.ru.md)

Experimental **two-player co-op for Enter the Gungeon** over a local network or Radmin VPN. Connect through **Host / Join by IPv4**. The host runs the game's native co-op simulation; the client controls the Cultist and renders replicated objects with local game resources. Both players share the host's camera view.

**Version 0.2.2 is an early alpha.** Full campaigns, every boss/item and additional modpacks have not been validated. Client visuals, sound and some interactions remain incomplete.

## Requirements

- Two Windows PCs with matching game files and the same mod version. The tested game version is Steam **2.1.9**.
- **BepInExPack EtG 5.4.2101** / BepInEx 5.
- A shared LAN or connected Radmin VPN network. Radmin VPN is installed separately.
- Start with this mod and its loader only. Other mod combinations are unverified.

## Installation

Packaged builds are listed under [Releases](https://github.com/vladnow/GungeonTogether-LAN/releases). GitHub's **Code → Download ZIP** contains source code, not an installer.

### Mod-manager ZIP

1. Create a clean Enter the Gungeon profile in Thunderstore Mod Manager or r2modman.
2. Install **BepInExPack EtG 5.4.2101** in that profile.
3. Import the `GungeonTogether_LAN-0.2.2-Thunderstore.zip` release through **Settings → Import local mod**, or install the package from the manager's catalogue when available.
4. Remove older LAN DLLs and other GungeonTogether plugins from the same profile before updating. Keep backups outside `BepInEx/plugins`.
5. Launch with **Start Modded**. LAN mode is selected automatically when loaded from a separate manager profile.

Local imports may require dependencies to be installed separately. Both players must install the same build.

### Windows launcher ZIP

For a release that includes `GungeonTogether-LAN.exe`:

1. Extract the Windows archive into a separate folder.
2. Close the game, start Steam and open `GungeonTogether-LAN.exe` from that folder.
3. Select the game folder containing `EtG.exe`, then use the install/launch button.
4. The launcher can back up recognized older LAN DLLs and install its embedded version. If BepInEx is missing, it downloads the official loader package and verifies its checksum; the first installation needs internet access.
5. Use the launcher for subsequent manual LAN launches. Update the launcher when updating the mod: its embedded version is fixed.

The launcher installs into the game folder and does not update manager profiles.

## Connecting

1. Both players launch the mod, enter the foyer and choose a character.
2. Press **Ctrl+P** to open the LAN menu.
3. The host selects **HOST**.
4. The client enters the host's LAN/Radmin IPv4, uses the same **TCP port** and **Session code**, then selects **JOIN BY IP**.
5. Once connected, the host selects **START SYNCHRONIZED DUNGEON**. The client loads automatically and controls the Cultist after the dungeon layout check succeeds.

The default TCP port is **27631**. Allow the game through the firewall on the intended network. Use a trusted LAN or VPN: the mod does not add encryption to its TCP connection.

## Controls

The client uses the usual movement, aim, fire, roll, reload, interact, blank and weapon-switch controls. The host controls pause, map and shared menus. **Ctrl+P** reopens the connection menu.

## Saves

LAN progress is stored separately from normal progress in:

`<game folder>/BepInEx/GungeonTogetherLAN/dungeon-profile`

Normal unlocks and currency are not imported automatically. Manager profiles using the same game folder share this LAN save directory; separate mod profiles do not automatically create separate game saves.

With the default configuration, direct Steam launches use normal saves and disable LAN connections. The manual launcher selects LAN saves explicitly. Back up normal saves and the LAN directory before changing installations.

The client temporarily uses the host's generation state to create matching rooms. Normal disconnect restores the client's previous in-memory context; crash recovery is not guaranteed.

Advanced setting: **Profile / SaveProfileMode** in `com.gungeontogether.lan.cfg`. `Auto` selects the profile from the launch method, `Normal` disables LAN, and `LAN` explicitly selects LAN saves. Restart after changing it. The `--gt-lan-profile` argument remains available for manual launches.

## Compatibility and known limitations

- Other Steam GungeonTogether variants and older LAN plugins are incompatible in the same installation/profile. Use separate profiles for other networking mods.
- Client combat audio, some effects/materials, beam geometry, shop/dialogue interfaces and secret-wall tile changes are incomplete.
- TCP retransmission can delay state updates on poor connections.
- A disconnected dungeon run is paused. Return to the foyer and start a new session; active-run reconnection and host migration are not implemented.
- Native player/foyer initialization errors have occurred during diagnostic runs.
- Two physical PCs over Radmin, full campaigns, all bosses/items, normal boss-to-floor transitions and third-party modpacks remain unverified.

## Validation

The 0.2.2 runtime passed **69 automated transport, protocol and profile checks** and **two local two-process dungeon smoke runs**. These checked matching first/second-floor layouts, client snapshots, native Cultist movement, firing and damage. Normal/LAN save-path selection was checked with copied save fixtures.

The smoke enemy had AI disabled and the next floor was loaded with a native level command. These results do not establish a complete multiplayer playthrough. See [VALIDATION.md](https://github.com/vladnow/GungeonTogether-LAN/blob/main/publication/VALIDATION.md) for detailed evidence.

## Reporting problems

Open an [issue](https://github.com/vladnow/GungeonTogether-LAN/issues) with the game/mod versions, installation method, last action before the problem and logs from both players. Remove private identifiers before publishing logs.

## Source and license

Source: [vladnow/GungeonTogether-LAN](https://github.com/vladnow/GungeonTogether-LAN).

Based on [GungeonTogether by lam and contributors](https://github.com/llamerrr/GungeonTogether), commit `52be4b636a07f28483ae4e17b16ad471065e89ce`. **Copyright (c) 2025 lam.** The original MIT license notice is preserved; LAN changes are distributed under the same MIT license. This is an unofficial fork.

Building requires a .NET SDK and local game/BepInEx reference assemblies. Run `Build-LAN.ps1` with `GamePath` and `BepInExCorePath` pointing to those installations. `Build-Launcher.ps1` builds the optional Windows launcher after the DLL has been built. Game assemblies, assets and BepInEx binaries are not redistributed in this package.

Developed with AI assistance.
