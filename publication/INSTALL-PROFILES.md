# Install Gungeon Together LAN

Both Windows PCs need matching Enter the Gungeon game files, the same LAN mod version, and BepInExPack EtG 5.4.2101. Keep a separate mod-manager profile for each networking mod.

1. Close the game and back up normal and LAN saves.
2. Select Enter the Gungeon in your mod manager. Use a clean profile for the first test and install BepInExPack EtG.
3. Remove previous LAN DLLs and other GungeonTogether variants from this profile. Keep backup copies outside BepInEx/plugins.
4. Install the published update, or use Settings > Import local mod with the matching Thunderstore ZIP. A local import may require installing the loader dependency separately.
5. Select Start Modded and check that both players see the same version in the LAN menu.

Use LAN CO-OP on the title/pause menu or Ctrl+P in the Breach. From the title, choose a native character to complete host/join. Esc closes the overlay. The host selects HOST > CREATE ROOM. The guest selects JOIN, enters the host's IPv4 address, matching port and room code, selects PLAYER TWO, and connects. The host selects START RUN. The default port is TCP 27631; Radmin requires a shared virtual network.

LAN saves live in `<game folder>/BepInEx/GungeonTogetherLAN/dungeon-profile`. They are separate from normal saves but shared by manager profiles using that game folder. Normal unlocks and currency are not imported.

If the LAN menu opens by itself and play stops, keep the complete status text, open DETAILS, and save both players' BepInEx/LogOutput.log files before restarting. The affected session may require returning to the foyer. Remove private identifiers before publishing logs.

The guest HUD and combat sound relay remain experimental. Audio tests use disabled output; audible quality still needs listening tests. Full natural campaign completion is not established; the fourth-floor Sell Creep mismatch was fixed in 0.3.9. See VALIDATION.md and this version's release notes.

Configure RULES before starting. The three independent toggles default off. The menu automatically follows supported game languages; unsupported languages fall back to English. Both PCs must use 0.4.0.
