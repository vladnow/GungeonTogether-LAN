# Client HUD and sound-event evidence for 0.3.4

The adjacent native-summary.json is sanitized evidence for the exact runtime hash in validation/summary.json. It contains two final local host/client scenarios, using Pilot and Hunter guests, with native move/fire/damage and forced floor-2 transitions before presentation checks.

The host's real guest health/armor/consumables and acquired weapon/item changed. Client snapshot assertions, actual graphical repaint, nine native UI asset bindings and manually inspected PNGs checked the resulting HUD. No client inventory/health authority was created just for the display.

Native host weapon sound events and their per-source switches reached client PostEvent/SetSwitch calls. The Pilot scenario observed 77 relayed PostEvent calls and 69 switches; the Hunter scenario observed 104 and 64. Both observed 17 distinct event IDs. All client sound calls were deliberately suppressed and returned zero playing IDs: these are transport/dispatch counts, not successful audible playback counts.

Tests used -nosound, the diagnostic global Wwise PostEvent interceptor and a paused Unity listener. A private test hook let native firing reach the interceptor despite native audio-code guards. The Unity volume getter stayed at 1; silence is established by interception and no-sound launch, rather than treating that property as proof of a muted Wwise engine.

Both pairs verified silent, 800x600 windowed, no-activate, cursor-free background testing. No Computer Use, ordinary save writes or remote upload were used. Intentional peer disconnects occurred after pass markers when stopping owned test processes.

These are artificial scenarios in private game copies, not normal room clearing, a full campaign or two-PC/Radmin testing. Music/ambience, RTPC/looping details, complete audio bank coverage and audible quality remain unverified. The known fourth-floor Sell Creep map mismatch remains unresolved. Raw private logs, user paths, saves, decompiled game code and game images are not included in the source package.
