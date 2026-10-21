# Reference review for 0.4.0

The following public repositories were inspected as source references. Downloaded sources and tools were not executed. This release does not install or bundle those projects.

| Project | Inspected revision | License | Application here |
|---|---|---|---|
| [Gungeon Online](https://github.com/AlexUnknown37/GungeonOnline) | 799c1a22e5e73be25254fe3bed04236fc19f4d37 | MIT, Copyright 2026 Alexander Vazquez | Adapted native menu-button cloning, placement and controller navigation. Full notice retained in NOTICE.md. |
| [GungeonTogether 3-Player](https://github.com/AhmetRec/GungeonTogether-3Player) | 4391cacec02c75212eddea26a85f35ca462fe8ec | MIT | Reviewed menu/world-event and resource synchronization approaches. No code copied into this release. |
| [Co-op Anything](https://github.com/cranebeaker/coop-anything) | 13475497d67dcd34a9c4490009244db4dc9ecd2e | MIT | Reviewed testing tools and the EtG overview. It does not supply an automatic drop-in multiplayer layer for this fork. No code copied. |

Our host simulates native two-player gameplay; the guest presents replicated state. Gungeon Online's inspected loot synchronization uses different semantics. Independent chest payment/rolls and personal shop opportunities therefore use a new implementation around the installed game's native interactions. Copying a competing transport does not automatically make it compatible with this authority model.

Co-op Anything's EtG overview lists GungeonTogether LAN 0.3.6 with unknown licensing and a reported fourth-floor Sell Creep mismatch. That is a third-party directory row, not a detailed test report or moderation result. The MIT notices and 0.3.9 correction are documented by this project. No external overview was edited.

## Thunderstore publication rules

The [global rules](https://wiki.thunderstore.io/moderation/global-rules) do not impose one co-op mod per game. Distinct implementations can coexist subject to the ordinary licensing, accurate-description, non-spam and package rules. This is a reading of the published rules, not moderator preapproval. Avoid exact reuploads; retain copyright/license notices and describe derivative status accurately. The Enter the Gungeon community offers an AI Generated category; select it for this AI-assisted release. Runtime installation compatibility is a separate matter: use one networking mod per profile.

Preparing a local package does not publish it, prove an accepted manager import, or establish compatibility with other mods.
