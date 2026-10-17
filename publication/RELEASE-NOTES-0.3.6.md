# Gungeon Together LAN 0.3.6 alpha

This update targets missing client secret-room geometry and the guest's empty/incorrect Ammonomicon equipment page.

- Replicate the host's opened native secret rooms, remove the client's local covering meshes/door blockers, update secret-cell visibility/occlusion and reveal the room on the minimap. Closed secrets remain concealed. Client visual application does not trigger independent combat/room-entry events or discovery statistics.
- Replicate the full player-two gun/active/passive/synergy lists when changed. Native Ammonomicon equipment-page readers use these lists and identify the selected guest hero. Reading the page does not grant items, change client placeholder inventories or execute item/stat callbacks.
- Keep native book item icons visible. Suppress guest movement input and speculative walking while the book is open.
- Retain/enforce the native shared coin/key consumables object. Either player collects and spends from one budget; the guest HUD displays that budget. This preserves native shared resources, rather than inventing a second synchronized wallet.
- Preserve native ghost rules: changing floors does not guarantee revival. Boss-reward/chest revival paths remain native. No automatic revival on every floor change is added.
- Keep the faster movement stream/prediction from 0.3.5, custom guest HUD/audio, separate cameras and synchronization checks.
- Keep public source/runtime ZIP documentation English and exclude internal agent instructions, obsolete launcher files and localized documents.

**Update both PCs to 0.3.6. Earlier peers are incompatible.** Install the matching Thunderstore package into separate clean mod-manager profiles with BepInExPack EtG 5.4.2101. Built against Steam Enter the Gungeon 2.1.9. No launcher is included. Existing release artifacts are kept in their version directories.

See VALIDATION.md for the exact runtime and bounded native scenarios. Tests distinguish ordinary floor loading from a native boss-reward resurrection call; they do not prove a complete natural death/boss/chest cycle. Equipment-page synchronization does not cover collection-unlock history, every death-book field, map/shop/dialogue UI or every special item. Native secret-room managers are covered; arbitrary destructible tilemap changes and every special puzzle are not established.

This remains an early-floor alpha. The fourth-floor Sell Creep layout mismatch remains unresolved; full campaign completion and physical two-PC/Radmin behavior are unverified. All game diagnostics disable audio output and use windowed/no-activate mode. Preparing archives does not publish them.
