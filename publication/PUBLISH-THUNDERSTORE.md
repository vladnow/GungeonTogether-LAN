# Publish a Thunderstore update

Use the installable Thunderstore ZIP rather than the source ZIP. It contains manifest.json, README.md, icon.png, CHANGELOG.md, licensing and the mod DLL under BepInEx/plugins/GungeonTogetherLAN/.

1. Review README.md, CHANGELOG.md and this version's release notes. Keep the alpha status and unresolved issues visible.
2. Check manifest.json: preserve the existing package name/team identity, use a new version number, keep the repository URL and BepInExPack EtG dependency correct.
3. Sign into Thunderstore and select the existing publishing team/package in the Enter the Gungeon community.
4. Upload the matching Thunderstore ZIP using the package upload flow. Follow any validation feedback and confirm the resulting version and public README. Select the AI Generated category if the community offers it, as required by the global package rules.
5. Install the published version into a clean profile and verify that both players receive the same build. Publication and a fresh manager import require their own verification; a local build alone proves neither.

An already published version number cannot be treated as a new update. For changed package contents, prepare a new version. Any site-supported page-description editing is separate from replacing an immutable downloadable package.

This project retains the MIT license and original copyright attribution of GungeonTogether. Do not include game textures, audio banks, game assemblies, loader binaries, personal saves/logs or code from other derivatives without suitable rights. See LICENSE and NOTICE.md. Keep credentials out of source and archives.
