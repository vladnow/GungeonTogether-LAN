# Gungeon Together LAN 0.3.4 alpha

The joining player previously had only a text status panel and no replicated combat sounds. This update adds a graphical guest HUD and host-to-client combat sound events.

- Show the guest's full/half/empty hearts, armor, blanks, cash and keys using local game UI sprites.
- Show the equipped gun and active-item icons, total ammo, magazine count/capacity, reloading and active-item charge/state. The host remains authoritative; presenting the HUD does not grant items or change health on the client.
- Relay weapon, enemy, object, character/footstep and voice-category sound events using event IDs, source positions and per-source Wwise switch values. Keep each player's volume settings local and suppress duplicate local combat events during mirroring.
- Bound sound batches, queues and source objects; discard old-floor, duplicate and stale batches. Dispose sound sources on transitions, disconnects and session faults. Include audio counters in periodic network diagnostics.
- Preserve separate player cameras and previous enemy-resource/transport fixes. Diagnostic launches now reapply the muted Unity listener every frame; ordinary gameplay audio is unaffected by that diagnostic helper.

**Update both PCs to 0.3.4.** Its wire protocol cannot connect to 0.3.3 or earlier. Install the Thunderstore ZIP into the mod-manager profile and remove old LAN DLLs from that profile. No launcher package is included. The prepared ZIPs have not been uploaded automatically.

This remains an early-floor alpha. The fourth-floor Sell Creep layout mismatch is unresolved and full campaign completion is not established. The guest HUD is custom and does not reproduce every native inventory/shop/menu screen. Music/ambience, RTPC parameters, all looping-stop/callback variants, modded sound banks and some effects/materials remain incomplete.

See VALIDATION.md for the exact DLL hash and bounded two-process checks. All game tests deliberately disable sound output: interception, transport, weapon switches and dispatch can be verified without claiming that audible balance or every sound bank has been listened to. Real two-PC/Radmin audio and full-run checks remain outstanding.
