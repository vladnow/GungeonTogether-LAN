# 0.3.3 alpha — separate player cameras

During normal gameplay, the host's camera focuses on player one and the client receives a separate view around player two. Native room framing and explicit manual camera sequences remain available.

Network sprite selection uses the guest's view and reported viewport aspect instead of the host's screen. Objects around player two can therefore be sent even when they are off-screen on the host. Native getters flush deferred off-screen animation frames before capture. The shared-screen movement restriction is bypassed only during the active LAN run, without making an already invalid movement valid or removing wall/pit rules.

The embedded enemy-prefab loading fix from 0.3.2 and queue/interpolation improvements from 0.3.1 remain. This is still one host-authoritative co-op world: combat regrouping, sealed rooms and floor transitions retain native co-op rules. Death/ghost behavior and every cinematic are not fully validated. Separate cameras are not separate campaigns.

Both PCs must update to **0.3.3**. This protocol does not connect to 0.3.2 or earlier. Use the Thunderstore package; no updated launcher is included. See VALIDATION.md for the exact runtime hash and actual native tests.

Full campaign completion remains unverified. The fourth-floor Sell Creep layout mismatch is unresolved. Client audio, effects/materials, beams and some interfaces remain incomplete. No additional latency reduction or physical two-PC/Radmin guarantee is claimed.
