# 0.3.2 alpha — embedded enemy sprite loading

The 0.3.1 stop `Missing or incompatible local sprite asset: BlobulinSpriteCollection` was reproduced in two local game processes. The same stop for BlobulonSpriteCollection was present in a user log and screenshot. The client stopped first; the host then paused on the received Stop message, opening the LAN menu.

Enemy collections embedded in native prefabs now carry the owning enemy database GUID. When the normal GUID/resource lookup cannot find a matching collection, the client loads that native prefab's dependencies from its own installation. It still verifies the collection identity and ordered sprite-table fingerprint. No extra simulated enemy or game texture transfer is introduced.

Update **both players to 0.3.2**. This protocol does not connect to 0.3.1 or earlier. Use the Thunderstore package in a profile with one networking mod; no updated launcher is provided.

The 0.3.1 queue/compression improvements and 60 ms display buffer remain. This update targets a confirmed stop; it does not claim another latency reduction or that every freeze is fixed.

See VALIDATION.md for the exact DLL hash, reproduction, native tests and limits. The fourth-floor Sell Creep layout mismatch remains unresolved. Full campaign completion, physical two-PC/Radmin coverage, complete client effects/audio/UI and all enemy asset combinations are not established.
