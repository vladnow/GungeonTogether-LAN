# Gungeon Together LAN

Experimental **two-player Enter the Gungeon co-op** over LAN or Radmin VPN. Connect with **Host / Join by IPv4**. The host runs native co-op; the guest controls player two and renders replicated state using local game resources. Each normal gameplay camera follows its own player. Native manual camera sequences may use cinematic framing.

**0.4.0 is an experimental alpha. Local controlled tests reached the fifth main floor; full natural campaign completion is not established.**

The guest map uses the game's Map binding (Tab by default). Hold to view, release to close. Guest map viewing is local; the host performs fast travel. This version also fixes remote binding resets and repeated material definitions behind the recorded 0.3.9 disconnect. Native outline parameters are replicated; visual parity across all rooms/bosses remains under testing. This version also sends fresher input, presents bounded local walking immediately, and targets adaptive 60 Hz world updates with a 30 Hz fallback under load. Network latency remains measurable and depends on frame rate, the connection and host simulation.

## Install or update

- Both Windows PCs need matching game files and **the same 0.4.0 build**. Built against Steam Enter the Gungeon **2.1.9**.
- Use **BepInExPack EtG 5.4.2101** / BepInEx 5 and a clean mod-manager profile containing one networking mod.
- Install Radmin VPN separately when using a virtual LAN.

After publication, update on both PCs and use **Start Modded**. For the prepared local package, install the loader in the profile and import `GungeonTogether_LAN-0.4.0-Thunderstore.zip` using **Settings > Import local mod**. Local import may require installing the loader dependency separately.

Remove older LAN DLLs and other GungeonTogether variants from that profile. Store backups outside `BepInEx/plugins`. Separate profiles are recommended for this mod and other multiplayer mods. **0.4.0 cannot connect to 0.3.9 or earlier peers.** No launcher is included. Preparing a ZIP does not publish it.

## Connect and play

1. Select **LAN CO-OP** on the title screen, or press **Ctrl+P** in the Breach. Host/join from the title screen continues to the native character selection; choose a character to finish connecting. **Esc** closes the overlay.
2. Host: select **HOST**, use listen address `0.0.0.0`, set the port and room code, then create the room.
3. Guest: select **JOIN**, enter the host's LAN/Radmin IPv4, use the same port and room code, and choose player two's character.
4. Once connected, the host starts the run from the LAN menu.

Default port: **TCP 27631**. Allow the game through the firewall for the intended network. Use a trusted LAN/VPN; the mod does not add encryption.

Use normal movement, aiming, shooting, rolling, reloading, interaction, blanks, items and weapon switching. Shooting and interaction remain separate actions. Opening the LAN menu mutes guest input. The host controls shared pause and progression. The guest can hold the normal map binding to view the local native map; guest map teleport selection is not enabled. The guest may view their synchronized native Ammonomicon equipment page locally. Dialogue choices run on the host and receive the interacting guest's menu actions.

## Changes in 0.4.0

- **Native entry:** LAN CO-OP appears in the original title and pause menus, with keyboard/controller navigation. Ctrl+P remains available.
- **Automatic menu language:** follows the game's English, Spanish, Simplified Chinese or Russian setting. Other game languages use English. There is no separate language button or language setting. Game content and technical diagnostics retain their existing language.
- **Host run rules:** independent switches for Separate money, Separate keys, and Personal chests and shops. Configure RULES before starting; the client receives the host rules. All switches default off, and rules are locked during the run.
- Preserve the 0.3.9 Sell Creep generation fix, native guest HUD/map, coordinated ordinary doors and local movement prediction.

## Shared resources, secrets and equipment

By default, coins and ordinary keys use **one shared native budget**. Either player collects or spends from it; do not add the two displayed balances together. Health, weapons, ammo, equipment and blanks remain per-player.

With **Separate money**, enemy coin drops belong to the last attacking player, even when the partner collects them. Boss-tagged coins are split evenly, alternating odd remainders; non-enemy coins belong to the collector. Unattributed/environmental kills fall back to the collector. This is last-attacker credit, not proportional damage scoring. The rules affect run casings, not permanent Hegemony Credits.

With **Separate keys**, each player has an independent key budget. With either wallet option off, that resource still uses the shared primary budget. Balances persist across floor transitions.

With **Personal chests and shops**, each player opens and pays for an ordinary chest separately, with an independent random reward. Independent rolls can produce the same item. Declining to open costs nothing and grants no item. Ground rewards belong to their opener; after legitimate pickup, equipped items are visible to both players and normal later drops can be traded. Each player also has a separate purchase opportunity for ordinary finite run shop stock, using the native price and selected wallet rules. Persistent native stock keeps its native behavior. Special quest/forced-content chests, mimics, glitched/rainbow/mirror/truth/Rat chests and permanent unlock purchases retain shared native behavior. Personal chest breaking is disabled to protect the overlapping per-player copies.

Opened native secret-room geometry/visibility and the guest's gun, active-item, passive-item and synergy lists are synchronized. The guest's native Ammonomicon equipment page uses read-only presentation data. Collection unlock history and every death-book field are not synchronized.

Changing floors does **not** guarantee revival. Native boss-reward/chest revival rules remain in use; no automatic revival on every floor is added.

## Testing limits

The reproduced fourth-floor Sell Creep prerequisite mismatch is fixed. Controlled local main-floor transition checks reached floor five. Full natural campaign completion, all special rooms/NPCs/bosses, arbitrary destructible tilemaps, dynamic/modded goop definitions, general particle/gas-cloud replication, complete inventory foldouts/dual-wield HUDs and every item-specific meter are not established. Unknown required assets can stop the session with a visible error to avoid invisible hazards.

Ordinary doors now wait for both living players and avoid forced entry. This does not establish that every reported movement lock has that same cause. The native HUD uses the current equipped gun/item; special modifier-driven layouts need further coverage.

Local native-process checks are separate from physical two-PC/Radmin play. Automated audio tests disable output and do not establish audible quality. A fresh manager import and remote publication require their own checks. See the versioned validation/release notes beside the prepared archives, or the repository's [validation report](https://github.com/vladnow/GungeonTogether-LAN/blob/main/publication/VALIDATION.md).

## Source and attribution

[Source repository](https://github.com/vladnow/GungeonTogether-LAN). **Code > Download ZIP** downloads source, not an installable mod. Build and test scripts are developer tools; players launch through the mod manager.

Derived from [llamerrr/GungeonTogether](https://github.com/llamerrr/GungeonTogether). Preserve the inherited MIT license and original copyright notice. Game resources are loaded from the installed game; game libraries, textures and audio banks are not distributed in this package. See LICENSE and NOTICE.md. The native menu entry adapts MIT-licensed Gungeon Online menu-button code, with its copyright and full license retained in NOTICE.md.

## Support and bug reports

Join [Mamont's Workshop on Discord](https://discord.gg/wuZQBhbrHk) for support, bug reports and testing discussion. Please create one report per issue and include the mod/game versions, other installed mods, host/guest role, reproduction steps, screenshots and `BepInEx/LogOutput.log` from the affected profile. For networking problems, logs from both players are useful.

The LAN menu **DETAILS** contains the current failure reason and network diagnostics. Copy logs before restarting the game. Check attachments for private information before posting. [GitHub Issues](https://github.com/vladnow/GungeonTogether-LAN/issues) is also available.
