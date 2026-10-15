# Gungeon Together LAN 0.3.0 alpha

Experimental two-player Enter the Gungeon co-op over LAN / Radmin VPN. Both PCs must update; 0.2.x peers are incompatible.

**Known blocker: a fourth-floor layout mismatch can stop the run. This release is for early-floor testing, not a verified full campaign. The fifth floor was not reached in the continued test.**

- Smoother foyer avatars and client dungeon sprites/camera with timestamped interpolation; up to 20 world updates per second. The 120 ms display buffer adds latency and does not implement client prediction.
- Choose Cultist, Pilot, Marine, Convict or Hunter for player two before starting a run.
- Redesigned dark/gold co-op menu, entirely in English. Ctrl+P opens it; Esc closes it.
- Deterministic random state for procedural room application; geometry checks remain enabled on every floor. Later-floor synchronization still requires work.
- Separate LAN saves. Normal unlocks and currency are not imported automatically.

Use the Thunderstore ZIP for a mod-manager profile, or the Windows ZIP for the standalone launcher. Remove older LAN DLLs and other GungeonTogether variants from the same plugin profile. Keep backups outside BepInEx/plugins. BepInExPack EtG 5.4.2101 and matching game files are required.

See VALIDATION.md for the exact checks and remaining gaps. This is an alpha: full campaigns, all room interactions, bosses/items/abilities, normal boss/elevator progression, two physical PCs over Radmin and other modpacks remain unverified. Client combat audio and some effects/interactions are incomplete. Active-run reconnection and host migration are not implemented.

Based on GungeonTogether by lam and contributors. Copyright (c) 2025 lam. MIT LICENSE and NOTICE are included. No game or BepInEx binaries/assets are redistributed.
