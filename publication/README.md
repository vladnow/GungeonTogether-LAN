# Gungeon Together LAN

Experimental **two-player Enter the Gungeon co-op** over LAN or Radmin VPN. Connect with **Host / Join by IPv4**. The host runs native co-op; the guest controls player two and renders replicated state using local game resources. Each normal gameplay camera follows its own player. Native manual camera sequences may use cinematic framing.

**0.3.7 is an early-floor alpha. Full campaign completion is not established. The known fourth-floor Sell Creep layout mismatch can still stop a run.**

## Install or update

- Both Windows PCs need matching game files and **the same 0.3.7 build**. Built against Steam Enter the Gungeon **2.1.9**.
- Use **BepInExPack EtG 5.4.2101** / BepInEx 5 and a clean mod-manager profile containing one networking mod.
- Install Radmin VPN separately when using a virtual LAN.

After publication, update on both PCs and use **Start Modded**. For the prepared local package, install the loader in the profile and import `GungeonTogether_LAN-0.3.7-Thunderstore.zip` using **Settings > Import local mod**. Local import may require installing the loader dependency separately.

Remove older LAN DLLs and other GungeonTogether variants from that profile. Store backups outside `BepInEx/plugins`. Separate profiles are recommended for this mod and other multiplayer mods. **0.3.7 cannot connect to 0.3.6 or earlier peers.** No launcher is included. Preparing a ZIP does not publish it.

## Connect and play

1. Start in the foyer and choose the host character. Press **Ctrl+P** for the LAN menu; **Esc** closes it.
2. Host: select **HOST**, use listen address `0.0.0.0`, set the port and room code, then create the room.
3. Guest: select **JOIN**, enter the host's LAN/Radmin IPv4, use the same port and room code, and choose player two's character.
4. Once connected, the host starts the run from the LAN menu.

Default port: **TCP 27631**. Allow the game through the firewall for the intended network. Use a trusted LAN/VPN; the mod does not add encryption.

Use normal movement, aiming, shooting, rolling, reloading, interaction, blanks, items and weapon switching. Shooting and interaction remain separate actions. Opening the LAN menu mutes guest input. The host controls shared pause/map and progression. The guest may view their synchronized native Ammonomicon equipment page locally. Dialogue choices run on the host and receive the interacting guest's menu actions.

## Changes in 0.3.7

- **Native guest HUD:** actual game controllers and artwork for hearts, armor, blanks, cash/keys, current gun/ammo, active item and status bars, using the original single-player layout. UI equipment copies remain inactive and do not enter player inventories.
- **Movement response:** keep bounded visual walking prediction when WASD also activates native menu-navigation bindings. Reload and weapon cycling no longer disable it. Smooth small host corrections over 60 ms; preserve wall/pit clipping and resets at room changes/teleports.
- **Doors:** an ordinary unlocked/unsealed door opens for one entering player, without forcing both players through the shared-screen entry sequence. Battle-room regrouping and special/locked doors retain native rules.
- **Hazards:** relay nearby native goop cells and burn/electric/frozen flags, render native floor meshes and fire particles locally, and keep damage/spread decisions on the host.
- **Dialogue:** recreate native textboxes and response buttons using actual host text.
- **Damage feedback:** relay the native hurt sound event, screen vignette and player sprite material flashes/status tints.
- **Lighting:** update both native placeholder positions/collider caches and room light state. The exact reported dark halo still needs comparison on the affected remote PC.

LAN menu **DETAILS** shows RTT, input acknowledgement time, queue size and player-two native input state. RTT and acknowledgement time are diagnostics; they are not measurements of input-to-display latency.

## Shared resources, secrets and equipment

Coins and ordinary keys use **one shared native budget**. Either player collects or spends from it; the two displayed balances must not be added together. Health, weapons, ammo, equipment and blanks remain per-player.

Opened native secret-room geometry/visibility and the guest's gun, active-item, passive-item and synergy lists are synchronized. The guest's native Ammonomicon equipment page uses read-only presentation data. Collection unlock history and every death-book field are not synchronized.

Changing floors does **not** guarantee revival. Native boss-reward/chest revival rules remain in use; no automatic revival on every floor is added.

## Testing limits

This remains an alpha for early-floor testing. The fourth-floor Sell Creep layout mismatch is unresolved. Full natural campaign completion, all special rooms/NPCs/bosses, arbitrary destructible tilemaps, dynamic/modded goop definitions, general particle/gas-cloud replication, complete inventory foldouts/dual-wield HUDs and every item-specific meter are not established. Unknown required assets can stop the session with a visible error to avoid invisible hazards.

The ordinary-door restriction was identified and removed. This does not establish that every reported movement lock has that same cause. The native HUD uses the current equipped gun/item; special modifier-driven layouts need further coverage.

Local native-process checks are separate from physical two-PC/Radmin play. Automated audio tests disable output and do not establish audible quality. A fresh manager import and remote publication require their own checks. See the versioned validation/release notes beside the prepared archives, or the repository's [validation report](https://github.com/vladnow/GungeonTogether-LAN/blob/main/publication/VALIDATION.md).

## Source and attribution

[Source repository](https://github.com/vladnow/GungeonTogether-LAN). **Code > Download ZIP** downloads source, not an installable mod. Build and test scripts are developer tools; players launch through the mod manager.

Derived from [llamerrr/GungeonTogether](https://github.com/llamerrr/GungeonTogether). Preserve the inherited MIT license and original copyright notice. Game resources are loaded from the installed game; game libraries, textures and audio banks are not distributed in this package. See LICENSE and NOTICE.md.
