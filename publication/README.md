# Gungeon Together LAN — experimental alpha

Two-player cooperation over a trusted LAN or Radmin VPN, with Host / Join by IPv4. The host runs Enter the Gungeon's native two-player simulation. The client controls the native Cultist and renders replicated sprites using its own game resources. The camera is shared. No video streaming or separate server is used.

**Early alpha. Full campaigns, every boss/item, additional modpacks and poor network conditions have not been validated.** Client combat audio, some effects/materials/beam geometry, shop/dialogue interfaces and secret-wall tile changes remain incomplete. This is a test build with partial visual replication, not a promise of complete multiplayer compatibility.

## Install in a dedicated profile

1. Create a fresh Enter the Gungeon profile named `LAN` in Thunderstore Mod Manager or r2modman.
2. Install this package and its BepInExPack EtG dependency.
3. Both players use version **0.2.2**, matching game files, and initially only this mod.
4. Use the manager's **Start Modded** button. LAN mode is enabled automatically when the selected BepInEx root belongs to a separate manager profile. No custom launch argument or external EXE is needed for that installation.
5. Choose a character in the foyer. Press **Ctrl+P**. The host chooses **HOST**; the client enters the host's Radmin IPv4 and the same port/session code, then chooses **JOIN BY IP**.
6. The host presses **START SYNCHRONIZED DUNGEON**. The client loads automatically. Control starts after matching dungeon geometry has been verified.

The default TCP port is **27631**. Radmin VPN must be installed and connected separately when used. Use an appropriate firewall exception for the game/port. The mod does not encrypt its TCP protocol separately from the VPN.

## Keep Steam three-player mods separate

Create a different profile for Rec's `GungeonTogether_3Player`. Do not install the Steam GungeonTogether plugin or legacy LAN DLLs in this LAN profile. The LAN plugin declares them incompatible to prevent two networking implementations from loading together.

Switch profiles and use Start Modded to switch modes; reinstalling the game is unnecessary. A profile export from a published installation can share package versions. Manually imported local builds still need to be sent separately until publication.

## Saves

LAN mode uses **`<game folder>/BepInEx/GungeonTogetherLAN/dungeon-profile`**, separate from the normal Windows game profile. Normal progress is not imported into LAN, so existing normal unlocks/currency may be absent there. This LAN directory is shared by installations using the same game folder; manager profiles separate mods/configs, not automatically all game saves.

Direct Steam launches from the game's ordinary BepInEx folder keep normal saves by default and disable LAN connections. Version 0.2.0 incorrectly selected the LAN profile for every launch; that behavior was fixed in 0.2.1 and remains fixed here.

The client temporarily receives the host's LAN generation history/flags to generate the same rooms. System hash and mid-game GUID are scrubbed from that context. Normal disconnect restores the client's previous in-memory LAN context; crash recovery is not guaranteed. Back up normal saves and this LAN directory before manually removing whole game folders.

Advanced setting: `Profile / SaveProfileMode` in `com.gungeontogether.lan.cfg`. `Auto` is the default; `Normal` disables LAN and retains normal saves; `LAN` explicitly selects LAN saves. The legacy `--gt-lan-profile` argument remains supported for direct manual installations. The configuration is read at startup.

## Controls and limitations

The client uses its usual movement, aim, fire, roll, reload, interact, blank and weapon-switch controls. Pause/map/common menus are controlled by the host. Input is sent up to 60 Hz; full sprite states up to 10 Hz. TCP loss/retransmission can delay fresh states. A disconnected dungeon run is paused; return to the foyer and start again. Active-run reconnect and host migration are not implemented.

## Validation

Tested on Windows against Steam Enter the Gungeon 2.1.9 and BepInExPack EtG 5.4.2101. The final DLL passed 69 automated transport/protocol/profile checks, native normal/LAN save-path and fixture-load checks, and two two-process loopback smoke runs with different room layouts. Both runs checked first-floor layout agreement, client sprite snapshots, native Cultist movement/fire/damage and a forced transition to a matching second floor.

The test enemy had AI disabled and the floor change used a native level-loading command. Boss completion, elevators, active enemy fights and a whole campaign were not established. Some native player/UI initialization exceptions also appear in the test logs; this is not a clean full-gameplay certification. Local two-process tests are not equivalent to two physical PCs over Radmin. Report issues with both participants' BepInEx logs and the last action before the problem; remove private identifiers before sharing logs publicly.

## Credits and source

Independent LAN fork based on [lam and contributors' GungeonTogether](https://github.com/llamerrr/GungeonTogether), commit `52be4b636a07f28483ae4e17b16ad471065e89ce`. Original MIT copyright notice is preserved. LAN changes are released under the same MIT license. This package is not the Steam three-player fork and is not an official game update.

The corresponding source/build scripts are supplied separately as `GungeonTogether_LAN-source.zip` with this prerelease handoff. To build from source, run `Build-LAN.ps1` with your own game and BepInEx reference paths and an installed .NET SDK; the resulting runtime goes into `bin`. Game assemblies and BepInEx binaries are not redistributed. The code was developed with AI assistance; use the **AI Generated** category when listing this package.
