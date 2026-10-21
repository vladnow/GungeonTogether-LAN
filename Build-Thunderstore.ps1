param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'release'), [switch]$DevelopmentPreview)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Drawing
$publication = Join-Path $PSScriptRoot 'publication'
$manifest = Get-Content -LiteralPath (Join-Path $publication 'manifest.json') -Raw | ConvertFrom-Json
if ($manifest.name -notmatch '^[A-Za-z0-9_]{1,128}$' -or $manifest.version_number -notmatch '^\d+\.\d+\.\d+$' -or $manifest.description.Length -gt 250) { throw 'Invalid manifest metadata.' }
$version = $manifest.version_number
$releaseNotesName = "RELEASE-NOTES-$version.md"
if (!(Test-Path -LiteralPath (Join-Path $publication $releaseNotesName) -PathType Leaf)) { throw 'Release notes for the manifest version are missing.' }
$dllName = "GungeonTogether.LAN-$version-alpha.dll"
$runtime = Join-Path $PSScriptRoot ('bin\' + $dllName)
$runtimeHash = (Get-FileHash -LiteralPath $runtime -Algorithm SHA256).Hash.ToLowerInvariant()
$evidence = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'validation\summary.json') -Raw | ConvertFrom-Json
if ($evidence.version -ne $version -or $evidence.runtime_sha256 -ne $runtimeHash) { throw 'Runtime does not match this version of validation evidence.' }
if ($DevelopmentPreview) {
 if ($evidence.automated_checks -lt 108 -or !$evidence.development_preview) { throw 'Preview background checks are incomplete.' }
} elseif (!$evidence.ready_for_alpha_review -or $evidence.dungeon_smoke_passes -lt 2) { throw 'Native game validation is pending. Only explicit -DevelopmentPreview packaging is allowed.' }
$image = [Drawing.Image]::FromFile((Join-Path $publication 'icon.png'))
try { if ($image.Width -ne 256 -or $image.Height -ne 256) { throw 'Icon must be 256x256.' } } finally { $image.Dispose() }
$out = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) $version
New-Item -ItemType Directory -Path $out -Force | Out-Null
$suffix = if ($DevelopmentPreview) { '-preview' } else { '' }
$packageName = "GungeonTogether_LAN-$version$suffix-Thunderstore.zip"
$sourceName = "GungeonTogether_LAN-$version$suffix-source.zip"
$checksumName = "GungeonTogether_LAN-$version$suffix-SHA256SUMS.txt"
$sidecars = @{}
$sidecars[$releaseNotesName] = Join-Path $publication $releaseNotesName
$sidecars["VALIDATION-$version.md"] = Join-Path $publication 'VALIDATION.md'
$sidecars["START-HERE-$version.md"] = Join-Path $publication 'START-HERE.md'
$ownerGuide = Join-Path $publication 'private\UPDATE-GITHUB.ru.md'
$sidecars["UPDATE-GITHUB-$version.md"] = if (Test-Path -LiteralPath $ownerGuide) { $ownerGuide } else { Join-Path $publication 'UPDATE-GITHUB.md' }
$sidecars["PUBLISH-THUNDERSTORE-$version.md"] = Join-Path $publication 'PUBLISH-THUNDERSTORE.md'
$sidecars["INSTALL-PROFILES-$version.md"] = Join-Path $publication 'INSTALL-PROFILES.md'
$referenceNotes = Join-Path $publication ("REFERENCE-NOTES-$version.md")
if (Test-Path -LiteralPath $referenceNotes) { $sidecars["REFERENCE-NOTES-$version.md"] = $referenceNotes }
foreach ($name in (@($packageName,$sourceName,$checksumName) + @($sidecars.Keys))) { if (Test-Path -LiteralPath (Join-Path $out $name)) { throw ('Refusing to overwrite release: ' + $name) } }
$sourceFiles = @('README.md','Build-LAN.ps1','Build-Launcher.ps1','Build-Thunderstore.ps1','Test-LAN.ps1','Test-Dungeon.ps1','Test-Profile.ps1','Test-Rendering.ps1','Test-GameBindings.ps1','Test-Display.ps1','Test-Acknowledgements.ps1','Test-Latency.ps1','Test-SpriteVisibility.ps1','GungeonTogether\GungeonTogether.csproj','GungeonTogether\Networking\Players\RemotePlayerAvatar.cs','launcher\Launcher.cs','tests\LanTests.cs','tests\DungeonTests.cs','tests\ProfileTests.cs','tests\RenderingTests.cs','tests\DisplayProfileTests.cs','tests\AcknowledgementTests.cs','tests\TransportLatencyTests.cs','tests\SpriteVisibilityTests.cs','validation\summary.json','validation\latency-031\README.md','validation\latency-031\fifo-high-entropy.txt','validation\latency-031\latest-high-entropy.txt')
$sourceFiles += @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'GungeonTogether\Lan') -Filter '*.cs' -File | Where-Object Name -ne 'LanPlugin.cs' | ForEach-Object { 'GungeonTogether\Lan\' + $_.Name })
$sourceFiles += 'validation\latency-031\native-summary.json'
$sourceFiles = @($sourceFiles | Where-Object { $_ -notin @('Build-Launcher.ps1','launcher\Launcher.cs') })
$sourceFiles += @('validation\host-freeze-032\README.md','validation\host-freeze-032\native-summary.json')
$sourceFiles += @('validation\camera-033\README.md','validation\camera-033\native-summary.json')
$sourceFiles += @('.gitignore','GungeonTogether.sln')
# AGENTS.md belongs to the local development workspace, not public packages.
$sourceFiles += @('Test-Presentation.ps1','tests\PresentationTests.cs','validation\presentation-034\README.md','validation\presentation-034\native-summary.json')
$sourceFiles += @('Test-Responsiveness.ps1','tests\ResponsivenessTests.cs','validation\responsiveness-035\README.md','validation\responsiveness-035\native-summary.json')
$sourceFiles += @('Test-Inventory.ps1','tests\InventoryTests.cs','validation\gameplay-036\README.md','validation\gameplay-036\native-summary.json')
$sourceFiles += @('validation\feedback-038\README.md','validation\feedback-038\native-summary.json')
$sourceFiles += @('validation\campaign-039\README.md','validation\campaign-039\native-summary.json')
$sourceFiles += @('Test-Effects.ps1','tests\EffectsTests.cs','validation\feedback-037\README.md','validation\feedback-037\native-summary.json')
$publicationSourceFiles = @('README.md','LICENSE','NOTICE.md','CHANGELOG.md','manifest.json','VALIDATION.md','INSTALL-PROFILES.md','Build-Icon.ps1','UPDATE-GITHUB.md','PUBLISH-THUNDERSTORE.md')
$publicationSourceFiles += @(Get-ChildItem -LiteralPath $publication -Filter 'RELEASE-NOTES-*.md' -File | Select-Object -ExpandProperty Name)
$publicationSourceFiles += @(Get-ChildItem -LiteralPath $publication -Filter 'VALIDATION-*.md' -File | Select-Object -ExpandProperty Name)
$publicationSourceFiles += @('START-HERE.md','REFERENCE-NOTES-0.4.0.md')
$sourceFiles += @('Test-RunRules.ps1','tests\RunRulesTests.cs','validation\features-040\README.md','validation\features-040\native-summary.json')
foreach ($relative in $sourceFiles) { if (!(Test-Path -LiteralPath (Join-Path $PSScriptRoot $relative) -PathType Leaf)) { throw ('Missing source package input: ' + $relative) } }
foreach ($name in $publicationSourceFiles) { if (!(Test-Path -LiteralPath (Join-Path $publication $name) -PathType Leaf)) { throw ('Missing publication input: ' + $name) } }
function Add-Entry($zip,[string]$path,[string]$name) {
 if ($name.Contains('..') -or $name.Contains(':') -or $name.StartsWith('/')) { throw 'Unsafe ZIP entry.' }
 [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$path,$name,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
}
$sourceZip = [IO.Compression.ZipFile]::Open((Join-Path $out $sourceName),[IO.Compression.ZipArchiveMode]::Create)
try {
 foreach ($relative in @($sourceFiles | Sort-Object -Unique)) { Add-Entry $sourceZip (Join-Path $PSScriptRoot $relative) $relative.Replace('\','/') }
 foreach ($name in @($publicationSourceFiles | Sort-Object -Unique)) { Add-Entry $sourceZip (Join-Path $publication $name) ('publication/' + $name) }
 Add-Entry $sourceZip (Join-Path $publication 'LICENSE') 'LICENSE'
 Add-Entry $sourceZip (Join-Path $publication 'icon.png') 'publication/icon.png'
} finally { $sourceZip.Dispose() }
$zip = [IO.Compression.ZipFile]::Open((Join-Path $out $packageName),[IO.Compression.ZipArchiveMode]::Create)
try {
 # Runtime packages contain player-facing metadata/license and the runtime only.
 # Development evidence and source stay in the source archive and sidecars.
 foreach ($name in @('manifest.json','README.md','icon.png','CHANGELOG.md','LICENSE','NOTICE.md')) { Add-Entry $zip (Join-Path $publication $name) $name }
 Add-Entry $zip $runtime ('BepInEx/plugins/GungeonTogetherLAN/' + $dllName)
} finally { $zip.Dispose() }
$hashes = @(
 ($runtimeHash + '  ' + $dllName)
 ((Get-FileHash -LiteralPath (Join-Path $out $packageName) -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $packageName)
 ((Get-FileHash -LiteralPath (Join-Path $out $sourceName) -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $sourceName)
)
$hashes | Set-Content -LiteralPath (Join-Path $out $checksumName) -Encoding ascii
foreach ($name in $sidecars.Keys) {
 if ($sidecars[$name] -eq $ownerGuide) {
  $ownerText = [IO.File]::ReadAllText($ownerGuide).Replace('__RELEASE_VERSION__', $version)
  [IO.File]::WriteAllText((Join-Path $out $name), $ownerText, (New-Object Text.UTF8Encoding($false)))
 } elseif ($name -eq "START-HERE-$version.md") {
  $startText = [IO.File]::ReadAllText($sidecars[$name]).Replace('INSTALL-PROFILES.md', "INSTALL-PROFILES-$version.md").Replace('UPDATE-GITHUB.md', "UPDATE-GITHUB-$version.md").Replace('PUBLISH-THUNDERSTORE.md', "PUBLISH-THUNDERSTORE-$version.md")
  [IO.File]::WriteAllText((Join-Path $out $name), $startText, (New-Object Text.UTF8Encoding($false)))
 } elseif ($name -eq "INSTALL-PROFILES-$version.md") {
  $installText = [IO.File]::ReadAllText($sidecars[$name]).Replace('VALIDATION.md', "VALIDATION-$version.md")
  [IO.File]::WriteAllText((Join-Path $out $name), $installText, (New-Object Text.UTF8Encoding($false)))
 } else { [IO.File]::Copy($sidecars[$name], (Join-Path $out $name), $false) }
}
Get-Item -LiteralPath (Join-Path $out $packageName),(Join-Path $out $sourceName) | Select-Object Name,Length
Write-Output ('Release folder: ' + $out)
