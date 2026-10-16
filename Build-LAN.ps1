param(
    [Parameter(Mandatory=$true)][string]$GamePath,
    [Parameter(Mandatory=$true)][string]$BepInExCorePath,
    [string]$DotnetPath = 'dotnet'
)
$ErrorActionPreference = 'Stop'
$gameManaged = Join-Path $GamePath 'EtG_Data\Managed'
$bepReference = Join-Path $BepInExCorePath 'BepInEx.dll'
if (!(Test-Path -LiteralPath (Join-Path $gameManaged 'Assembly-CSharp.dll'))) { throw 'Missing game Managed assemblies.' }
if (!(Test-Path -LiteralPath $bepReference)) { throw 'Missing BepInEx 5 reference.' }
$sdkInfo = & $DotnetPath --list-sdks
if ($LASTEXITCODE -ne 0 -or !$sdkInfo) { throw 'Install a .NET SDK to build from source. Players do not need an SDK.' }
$sdkLine = @($sdkInfo)[-1]
if ($sdkLine -notmatch '^([^ ]+) \[(.+)\]$') { throw 'Cannot locate the SDK C# compiler.' }
$sdkVersion = $Matches[1]
# Do not decode a non-ASCII install path from dotnet's redirected console output.
$dotnetExecutable = (Get-Command $DotnetPath -ErrorAction Stop).Source
$compiler = Join-Path (Join-Path (Split-Path -Parent $dotnetExecutable) "sdk\$sdkVersion") 'Roslyn\bincore\csc.dll'
$bin = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force -Path $bin | Out-Null
$target = Join-Path $bin 'GungeonTogether.LAN-0.3.4-alpha.dll'
$arguments = @('exec', $compiler, '/nologo', '/noconfig', '/nostdlib+', '/target:library', '/optimize+', '/deterministic+', '/langversion:7.3', "/out:$target")
# Compile against the actual game's old framework/Unity assemblies, not mock types.
foreach ($reference in Get-ChildItem -LiteralPath $gameManaged -Filter '*.dll') { $arguments += "/reference:$($reference.FullName)" }
$arguments += "/reference:$bepReference"
$arguments += "/reference:$(Join-Path $BepInExCorePath '0Harmony.dll')"
foreach ($source in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'GungeonTogether\Lan') -Filter '*.cs' | Where-Object Name -ne 'LanPlugin.cs') { $arguments += $source.FullName }
$arguments += (Join-Path $PSScriptRoot 'GungeonTogether\Networking\Players\RemotePlayerAvatar.cs')
& $DotnetPath @arguments
if ($LASTEXITCODE -ne 0) { throw 'LAN build failed.' }
Get-FileHash -LiteralPath $target -Algorithm SHA256
Write-Host "Built $target. This script does not deploy to your game."


