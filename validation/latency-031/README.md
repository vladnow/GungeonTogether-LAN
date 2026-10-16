# Transport freshness checks

These checks use real local TCP sockets through a byte-transparent proxy that limits reads from the sender. TCP backpressure therefore reaches LanTransport rather than accumulating in an unbounded test proxy queue. No game or Unity process was launched.

The congestion workload produces 16 KiB high-entropy state packets at 30 Hz for 3 seconds, with a 128 KiB/s downstream limit. High-entropy padding prevents compression from removing the bottleneck. Both runs produce 90 states and receive 23 during the limited interval.

| Measure | Reliable FIFO state | Replaceable latest state |
| --- | ---: | ---: |
| Idle echo RTT, p95 | 79.0 ms | 79.2 ms |
| Delivered state age, p95 | 2063.1 ms | 341.3 ms |
| Interpolated sample age after 3 s | 2262.8 ms | 435.5 ms |
| Latest received sequence after 3 s | 23 | 78 |
| Peak application pending bytes | 1,048,992 | 16,436 |

Both runs preserve the seven reliable control messages in order and deliver the final produced state after the bottleneck is removed. The latest-state run additionally proves an actual pending replacement happened, separate state keys coexist, and reliable messages retain their ordering around coalesced state.

The compressible-packet test sends 32,768 bytes through SendLatest and verifies byte-for-byte restoration with 81 wire bytes. A separate 60 Hz state / 10 Hz receiver polling test has a maximum sample age of 136.3 ms and a final interpolation buffer count of 3, showing that the interpolation buffer does not independently accumulate seconds of FIFO playback delay.

Commands:

```powershell
.\Test-Latency.ps1 -Fifo -OutputPath validation\latency-031\fifo-high-entropy.txt
.\Test-Latency.ps1 -ExpectFresh -OutputPath validation\latency-031\latest-high-entropy.txt
```

Results: FIFO reproduction 16 checks passed; latest-state path 24 checks passed. The FIFO run forces ordinary Send on the current transport; it isolates delivery policy rather than comparing different binary builds. The original pre-change transport also reproduced approximately 2.08 s p95 state age.

Source hashes at the final checks:

- LanTransport.cs: `591941acf63328d1422f19fac0e3300a703d6ea8c1263bcf6616aa1aeaa8a18a`
- TransportLatencyTests.cs: `e4d820c699e4b3202adae067255b99a9abc05b992ca14985b91ecf0853ad61d8`
- Test-Latency.ps1: `c1eb5b7918b4d32e5aadbe9fce26587ed2201b7c9eae542c747aaf42686b4a74`

These controlled measurements establish the queue behavior. They do not establish real Radmin RTT, performance in a running dungeon, or a latency guarantee for snapshots larger than the tested 16 KiB. Any frame already being transmitted remains intact, so serialization time on a slow link is an unavoidable lower bound.


## Native game follow-up

The exact 0.3.1 DLL was also exercised separately in two local two-process native scenarios. See native-summary.json and publication/VALIDATION.md for the runtime hash, artificial controls/transitions, constrained-game presentation p95 of121.549ms, and missing physical Radmin/full-campaign validation. Raw native/probe files remain private.
