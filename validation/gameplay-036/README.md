# Secret-room, inventory and co-op state evidence

Production version 0.3.6, runtime SHA-256 `e741dcb4b8ba6c27edc75068e58009bb9ee7d1a920462242a911a6fb18c45f98`. See native-summary.json and publication/VALIDATION.md for the sanitized measurements and boundaries.

Two final gameplay cases used Pilot host and Pilot/Hunter guest, with two local native processes each. They passed artificial move/fire/native damage, identical floor 1/2 layouts and a forced tt5 transition. They additionally checked an opened first-floor native secret manager, client cover/door/blocker removal without duplicate discovery stats, forced ghost persistence and the native boss-reward revival function.

Native coin/key pickups and spending exercise the same host consumables object used by both players. This preserves a shared budget. Native acquisition of gun 15, active item 108 and passive 102 reaches the actual guest equipment page with visible native icons and correct hero identification. Both placeholder inventories remain unchanged. Screenshots were inspected but are excluded from distribution.

A third final pair repeated the movement/safety probe: twelve local synthetic walking starts, median 10.609 ms and p95 25.005 ms from input sampling to >=0.0625 units of presented movement. This does not measure hardware input-to-photon timing, damage confirmation or physical two-PC/Radmin latency.

The private GUI probe keeps the native book animation advancing without foreground activation and bypasses queued unlock cards for inspection. The private ghost fixture forces zero health and invokes native BecomeGhost; it does not establish a full natural lethal-damage/drop/boss/pedestal/chest cycle. Production behavior and unlock notifications are not altered by these private probe helpers.

All tests were silent, 800x600 windowed, no-activate, background and cursor-unlocked. No Computer Use or listening test was used. Full campaigns, all secret puzzles and arbitrary destructible tilemap updates remain unverified; the fourth-floor Sell Creep layout mismatch is unresolved. No external publication was performed.
