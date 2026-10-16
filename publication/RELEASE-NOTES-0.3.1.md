# Gungeon Together LAN 0.3.1 alpha

Experimental two-player Enter the Gungeon co-op through LAN / Radmin IPv4. **Update both PCs: 0.3.0 and older peers are incompatible.**

**Known blocker: a fourth-floor layout mismatch involving the Sell Creep room remains unresolved and can stop a run. This is an early-floor testing alpha; full campaigns and two physical PCs over Radmin are unverified.**

- Replace queued world/foyer state with newer state, preserve reliable control-message order, and compress large replaceable packets when beneficial.
- Reduce timestamped display smoothing from 120 ms to **60 ms**; keep up to 20 dungeon snapshots per second. There is no client prediction.
- Show the mod's RTT and client input-to-host-snapshot acknowledgement time in the menu. The acknowledgement measurement excludes display interpolation.
- Expand the guest HUD with cash, keys, blanks, gun/item names, active-item status, ghost/death and run-end information.
- Avoid local client room-entry gameplay events, independent boss intros and collision simulation while mirroring; reduce repeated scene scans.
- Prevent focus loss from invoking automatic native pause during a LAN run while preserving manual host pause.
- Draw the menu cursor above buttons and fields; inherit initial display preferences once and refresh the normal startup viewport/UI without choosing an arbitrary resolution.

A controlled **real-socket** test with a **128 KiB/s bottleneck**, incompressible 16 KiB state packets at 30 Hz, measured p95 delivered-state age of **2063 ms with FIFO delivery versus 341 ms with latest-state delivery**. This verifies queue behavior, not Radmin latency, running-dungeon performance or guaranteed player response.

The exact 0.3.1 DLL passed two local two-process early-floor scenarios. With 7.5 ms added forwarding delay per direction and 128 KiB/s downstream, presented acknowledged source state averaged about 100 ms with p95 about 120 ms; one artificial movement step first appeared after approximately 104 ms in sparse observer samples. This is not a two-PC/Radmin benchmark. See VALIDATION.md for the runtime hash, artificial controls/transitions and untested behavior. Complete item-outline/material fidelity is not claimed.

Client audio, some effects/materials, beams, secret-wall tile changes and shop/dialogue interfaces remain incomplete. Full campaigns, all bosses/items/abilities, complete normal boss/elevator progression, special endings, fresh installation and modpacks remain unverified. Active-run reconnection and host migration are unavailable.

Use a dedicated profile, keep backups outside BepInEx/plugins, and retain the MIT attribution. Package preparation does not mean upload or publication.
