# GungeonTogether release delivery

- Create a separate `release/<version>/` directory for every new version, starting with 0.3.3. Keep earlier release files and directories unchanged.
- Deliver the Thunderstore ZIP, source ZIP, SHA-256 sums, standalone `RELEASE-NOTES-<version>.md`, validation report and relevant installation/publication instructions together in that version directory.
- Release notes are required for every version and must describe actual behavior, compatibility requirements and known limits. Building a package does not publish it.
- Do not create launcher binaries or launcher ZIPs unless the user explicitly requests them. Current installation uses Thunderstore profiles.
- Game tests must be silent, windowed, no-activate, with an unconstrained cursor and background simulation. Do not use Computer Use for tests unless the user changes that instruction.
- Use local/forked sources; do not alter upstream repositories or publish externally without authorization. Preserve normal saves and inherited MIT attribution.
