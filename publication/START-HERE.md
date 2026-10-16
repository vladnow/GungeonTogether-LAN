# Release files

- `*-Thunderstore.zip`: the installable mod package, also suitable for a local manager import. Both players must install the same version.
- `*-source.zip`: source code and development tools for GitHub. This is not the installable mod.
- `RELEASE-NOTES-<version>.md`: implemented changes and known limitations.
- `VALIDATION-<version>.md`: actual checks and their boundaries.
- `*SHA256SUMS.txt`: checksums of the runtime DLL and ZIPs.

See INSTALL-PROFILES.md for installation, UPDATE-GITHUB.md for updating repository files, and PUBLISH-THUNDERSTORE.md for publishing a new package version. Creating these files does not publish them.

Build and test scripts in the source archive are developer tools. Players launch the game through their mod manager.
