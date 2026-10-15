param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'release'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Drawing
$publication = Join-Path $PSScriptRoot 'publication'
$manifest = Get-Content -LiteralPath (Join-Path $publication 'manifest.json') -Raw | ConvertFrom-Json
if ($manifest.name -notmatch '^[A-Za-z0-9_]{1,128}$' -or $manifest.version_number -notmatch '^\d+\.\d+\.\d+$' -or $manifest.description.Length -gt 250) { throw 'Invalid manifest metadata.' }
$runtime = Join-Path $PSScriptRoot 'bin\GungeonTogether.LAN-0.2.2-alpha.dll'
$runtimeHash = (Get-FileHash -LiteralPath $runtime -Algorithm SHA256).Hash.ToLowerInvariant()
$evidence = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'validation\summary.json') -Raw | ConvertFrom-Json
if ($evidence.runtime_sha256 -ne $runtimeHash -or !$evidence.ready_for_alpha_review -or $evidence.dungeon_smoke_passes -lt 2) { throw 'Final build does not match completed alpha validation.' }
$image = [Drawing.Image]::FromFile((Join-Path $publication 'icon.png'))
try { if ($image.Width -ne 256 -or $image.Height -ne 256) { throw 'Icon must be 256x256.' } } finally { $image.Dispose() }
$out = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $out -Force | Out-Null
$packageName = 'GungeonTogether_LAN-0.2.2-Thunderstore.zip'
$sourceName = 'GungeonTogether_LAN-source.zip'
foreach ($name in @($packageName,$sourceName,'SHA256SUMS.txt')) { if (Test-Path -LiteralPath (Join-Path $out $name)) { throw ('Refusing to overwrite release: ' + $name) } }
$sourceFiles = @('Build-LAN.ps1','Build-Launcher.ps1','Build-Thunderstore.ps1','Test-LAN.ps1','Test-Dungeon.ps1','Test-Profile.ps1','GungeonTogether\GungeonTogether.csproj','GungeonTogether\Networking\Players\RemotePlayerAvatar.cs','launcher\Launcher.cs','tests\LanTests.cs','tests\DungeonTests.cs','tests\ProfileTests.cs','validation\summary.json')
$sourceFiles += @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'GungeonTogether\Lan') -Filter '*.cs' -File | Where-Object Name -ne 'LanPlugin.cs' | ForEach-Object { 'GungeonTogether\Lan\' + $_.Name })
function Add-Entry($zip,[string]$path,[string]$name) {
 if ($name.Contains('..') -or $name.Contains(':') -or $name.StartsWith('/')) { throw 'Unsafe ZIP entry.' }
 [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$path,$name,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
}
$sourceZip = [IO.Compression.ZipFile]::Open((Join-Path $out $sourceName),[IO.Compression.ZipArchiveMode]::Create)
try {
 foreach ($relative in @($sourceFiles | Sort-Object -Unique)) { Add-Entry $sourceZip (Join-Path $PSScriptRoot $relative) $relative.Replace('\','/') }
 foreach ($name in @('README.md','LICENSE','NOTICE.md','CHANGELOG.md','manifest.json','VALIDATION.md','INSTALL-PROFILES.ru.md','Build-Icon.ps1')) { Add-Entry $sourceZip (Join-Path $publication $name) ('publication/' + $name) }
 Add-Entry $sourceZip (Join-Path $publication 'README.md') 'README.md'
 Add-Entry $sourceZip (Join-Path $publication 'LICENSE') 'LICENSE'
 Add-Entry $sourceZip (Join-Path $publication 'icon.png') 'publication/icon.png'
} finally { $sourceZip.Dispose() }
$zip = [IO.Compression.ZipFile]::Open((Join-Path $out $packageName),[IO.Compression.ZipArchiveMode]::Create)
try {
 foreach ($name in @('manifest.json','README.md','icon.png','CHANGELOG.md','LICENSE','NOTICE.md','INSTALL-PROFILES.ru.md')) { Add-Entry $zip (Join-Path $publication $name) $name }
 Add-Entry $zip $runtime 'BepInEx/plugins/GungeonTogetherLAN/GungeonTogether.LAN-0.2.2-alpha.dll'
} finally { $zip.Dispose() }
$hashes = @(
 ($runtimeHash + '  GungeonTogether.LAN-0.2.2-alpha.dll')
 ((Get-FileHash -LiteralPath (Join-Path $out $packageName) -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $packageName)
 ((Get-FileHash -LiteralPath (Join-Path $out $sourceName) -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $sourceName)
)
$hashes | Set-Content -LiteralPath (Join-Path $out 'SHA256SUMS.txt') -Encoding ascii
Get-Item -LiteralPath (Join-Path $out $packageName),(Join-Path $out $sourceName) | Select-Object Name,Length
