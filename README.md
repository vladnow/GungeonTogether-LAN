# Gungeon Together LAN

Experimental **two-player Enter the Gungeon co-op** over LAN or Radmin VPN. Connect with **Host / Join by IPv4**. The host runs native co-op; the guest controls player two and renders replicated state using local game resources. Each normal gameplay camera follows its own player. Native manual camera sequences may use cinematic framing.

**0.3.9 is an experimental alpha. Local controlled tests reached the fifth main floor; full natural campaign completion is not established.**

## Install or update

- Both Windows PCs need matching game files and **the same 0.3.9 build**. Built against Steam Enter the Gungeon **2.1.9**.
- Use **BepInExPack EtG 5.4.2101** / BepInEx 5 and a clean mod-manager profile containing one networking mod.
- Install Radmin VPN separately when using a virtual LAN.

After publication, update on both PCs and use **Start Modded**. For the prepared local package, install the loader in the profile and import `GungeonTogether_LAN-0.3.9-Thunderstore.zip` using **Settings > Import local mod**. Local import may require installing the loader dependency separately.

Remove older LAN DLLs and other GungeonTogether variants from that profile. Store backups outside `BepInEx/plugins`. Separate profiles are recommended for this mod and other multiplayer mods. **0.3.9 cannot connect to 0.3.8 or earlier peers.** No launcher is included. Preparing a ZIP does not publish it.

## Connect and play

1. Start in the foyer and choose the host character. Press **Ctrl+P** for the LAN menu; **Esc** closes it.
2. Host: select **HOST**, use listen address `0.0.0.0`, set the port and room code, then create the room.
3. Guest: select **JOIN**, enter the host's LAN/Radmin IPv4, use the same port and room code, and choose player two's character.
4. Once connected, the host starts the run from the LAN menu.

Default port: **TCP 27631**. Allow the game through the firewall for the intended network. Use a trusted LAN/VPN; the mod does not add encryption.

Use normal movement, aiming, shooting, rolling, reloading, interaction, blanks, items and weapon switching. Shooting and interaction remain separate actions. Opening the LAN menu mutes guest input. The host controls shared pause and progression. The guest can hold the normal map binding to view the local native map; guest map teleport selection is not enabled. The guest may view their synchronized native Ammonomicon equipment page locally. Dialogue choices run on the host and receive the interacting guest's menu actions.

## Changes in 0.3.9

- **Fourth-floor generation:** transmit the host primary player's run mastery-token count along with the existing generation prerequisites. The client now uses that count for native mastery-gated room injection while its placeholder player is absent. This fixes the reproduced client-only Sell Creep injection and resulting layout mismatch without disabling the room for eligible hosts.
- Preserve other native room prerequisites, the host's own generation decisions and the layout safety barrier. The override is limited to client generation in an active LAN run.
- **Input diagnostics:** show the actual native input-override keys in DETAILS instead of a generic placeholder. Cinematic and dialogue input rules are preserved.
- Retain the native HUD/map, shop labels, boss widgets, original sprite materials, coordinated ordinary doors, separate cameras, shared cash/ordinary keys and local movement prediction from 0.3.8.

LAN menu **DETAILS** shows RTT, input acknowledgement time, queue size and player-two native input state. RTT and acknowledgement time are diagnostics; they are not measurements of input-to-display latency.

## Shared resources, secrets and equipment

Coins and ordinary keys use **one shared native budget**. Either player collects or spends from it; the two displayed balances must not be added together. Health, weapons, ammo, equipment and blanks remain per-player.

Opened native secret-room geometry/visibility and the guest's gun, active-item, passive-item and synergy lists are synchronized. The guest's native Ammonomicon equipment page uses read-only presentation data. Collection unlock history and every death-book field are not synchronized.

Changing floors does **not** guarantee revival. Native boss-reward/chest revival rules remain in use; no automatic revival on every floor is added.

## Testing limits

The reproduced fourth-floor Sell Creep prerequisite mismatch is fixed. Controlled local main-floor transition checks reached floor five. Full natural campaign completion, all special rooms/NPCs/bosses, arbitrary destructible tilemaps, dynamic/modded goop definitions, general particle/gas-cloud replication, complete inventory foldouts/dual-wield HUDs and every item-specific meter are not established. Unknown required assets can stop the session with a visible error to avoid invisible hazards.

Ordinary doors now wait for both living players and avoid forced entry. This does not establish that every reported movement lock has that same cause. The native HUD uses the current equipped gun/item; special modifier-driven layouts need further coverage.

Local native-process checks are separate from physical two-PC/Radmin play. Automated audio tests disable output and do not establish audible quality. A fresh manager import and remote publication require their own checks. See the versioned validation/release notes beside the prepared archives, or the repository's [validation report](https://github.com/vladnow/GungeonTogether-LAN/blob/main/publication/VALIDATION.md).

## Source and attribution

[Source repository](https://github.com/vladnow/GungeonTogether-LAN). **Code > Download ZIP** downloads source, not an installable mod. Build and test scripts are developer tools; players launch through the mod manager.

Derived from [llamerrr/GungeonTogether](https://github.com/llamerrr/GungeonTogether). Preserve the inherited MIT license and original copyright notice. Game resources are loaded from the installed game; game libraries, textures and audio banks are not distributed in this package. See LICENSE and NOTICE.md.

## Support and bug reports

Join [Mamont's Workshop on Discord](https://discord.gg/wuZQBhbrHk) for support, bug reports and testing discussion. Please create one report per issue and include the mod/game versions, other installed mods, host/guest role, reproduction steps, screenshots and `BepInEx/LogOutput.log` from the affected profile. For networking problems, logs from both players are useful.

The LAN menu **DETAILS** contains the current failure reason and network diagnostics. Copy logs before restarting the game. Check attachments for private information before posting. [GitHub Issues](https://github.com/vladnow/GungeonTogether-LAN/issues) is also available.
