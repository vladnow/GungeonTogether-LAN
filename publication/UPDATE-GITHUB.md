# Update an existing GitHub repository

Use the source archive for repository files and the Thunderstore archive for an installable release. Do not upload the contents of the complete local workspace or the complete release directory into the Code tab.

1. Back up the existing repository with Code > Download ZIP.
2. Extract the prepared source ZIP into an empty directory. README.md and GungeonTogether/ must be at its top level.
3. Open the repository's default branch and use Add file > Upload files. Upload the top-level files and GungeonTogether/ first, not the outer containing folder.
4. Commit to a new update branch and open a pull request. For larger source archives, switch to that same update branch and upload the remaining publication/, tests/, validation/ and other included folders in another batch. Commit directly to the update branch; its existing pull request updates automatically. Do not create a separate branch for each batch. Check the complete diff before merging.
5. Remove obsolete tracked files separately. Browser uploads do not delete files missing from the new ZIP, and .gitignore does not remove files already tracked by Git.

If present from older uploads, delete these exact obsolete paths from the repository:

```text
AGENTS.md
Build-Launcher.ps1
GungeonTogether-LAN.exe
launcher/Launcher.cs
GungeonTogether/Lan/ClientHud.cs
README.ru.md
publication/README.ru.md
publication/INSTALL-PROFILES.ru.md
publication/PUBLISH-THUNDERSTORE.ru.md
publication/START-HERE.ru.md
publication/UPDATE-GITHUB.ru.md
```

For a browser-only update, open each listed file, choose its delete action, and commit the deletion or include it in the update branch/pull request. Skip paths that do not exist. Review any other old localized documentation individually. Do not delete source/build/test folders to make the file list shorter.

Keep Build-LAN.ps1, Build-Thunderstore.ps1, Test-*.ps1, tests/, GungeonTogether/, LICENSE, NOTICE and the public English documentation. Different `Prepare 0.3.x` labels beside files show the last commit that changed each file; unchanged tests can legitimately retain an older label.

After merging, create a GitHub release from the updated default branch. Use the matching version tag and mark the alpha as a pre-release. Attach the Thunderstore ZIP, source ZIP, checksums and release notes. Code > Download ZIP returns source, not the installable mod.

Preserve the inherited MIT notice and upstream attribution. Exclude game/BepInEx DLLs, saves, private logs, local agent instructions and development caches. Never describe local socket tests as physical two-PC/Radmin tests or claim a full campaign without evidence.
