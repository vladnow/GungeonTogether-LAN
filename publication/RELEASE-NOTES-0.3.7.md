# Gungeon Together LAN 0.3.7 alpha

- Replace the IMGUI imitation of the guest HUD with the game's actual heart, blank, consumable, gun, active-item and status-bar controllers. Keep the original single-player layout on the guest. Detached inactive UI equipment copies never enter player inventories or run Pickup callbacks.
- Reserve the native controller confirm/interact button for Interact. Network shooting uses RightTrigger, avoiding direct native menu/controller checks that could mistake a shot for interaction.
- Open ordinary unlocked doors when either network player enters. Remove the shared-screen proximity gate and its forced movement of both players. Locked/sealed doors and battle-room regrouping keep native checks.
- Relay nearby native goop cells, definitions and burning/electric/frozen flags. Build local native goop meshes and fire particles without local cell-gameplay callbacks or independent damage/spread simulation. Unsupported assets stop the run with a visible error instead of concealing a hazard.
- Recreate native textboxes and conversation response buttons using the host's actual text. The host still runs dialogue logic; the guest sends their normal interaction/menu actions.
- Relay guest damage to the native damage vignette and hurt sound event. Replicate native sprite override tints for hit/status flashes. No local damage is applied for feedback.
- Update both native placeholder positions/collider caches and room lighting. This addresses stale light/occlusion positions; the exact dark halo reported on a remote PC still needs a remote comparison.
- Smooth small authoritative walking corrections over 60 ms while retaining immediate bounded visual input replay, collision clipping and resets on teleports/room changes. This is not a reduction of physical network ping.
- Forward menu-navigation bindings only while a host dialogue is present. WASD's additional menu-navigation actions, reload and weapon cycling no longer disable walking replay. The earlier synthetic direction-only test did not cover these real keyboard binding combinations.
- Show the native player-two input state in LAN menu DETAILS to help identify any remaining movement locks.

**Update both PCs to 0.3.7. Earlier peers are incompatible.** Built against Steam Enter the Gungeon 2.1.9 and BepInExPack EtG 5.4.2101. Install through matching clean Thunderstore profiles. No launcher is included.

This is an early-floor alpha. The fourth-floor Sell Creep layout mismatch remains unresolved; full natural campaign completion is not established. The native HUD's current weapon/item display is covered; full inventory foldouts, dual-wield layouts, every item-specific meter, arbitrary dynamic/modded goops, general particle/gas-cloud replication and every NPC/puzzle are not established. A direct ordinary-door restriction was removed; this does not prove that every reported room-transition freeze has the same cause.

See the separate VALIDATION-0.3.7.md for exact checks. Native processes are local, silent, windowed, no-activate and cursor-unconstrained. Sound dispatch is checked without listening. No physical two-PC/Radmin run, fresh manager import or remote publication is claimed. All earlier release directories remain intact.
