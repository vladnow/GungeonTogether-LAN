$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin = Join-Path $PSScriptRoot 'tests\bin'
New-Item -ItemType Directory -Force -Path $bin | Out-Null
$exe = Join-Path $bin 'DungeonTests.exe'
& $compiler /nologo /target:exe /optimize+ "/out:$exe" (Join-Path $PSScriptRoot 'GungeonTogether\Lan\LanTransport.cs') (Join-Path $PSScriptRoot 'GungeonTogether\Lan\DungeonProtocol.cs') (Join-Path $PSScriptRoot 'tests\DungeonTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Dungeon protocol test compilation failed.' }
& $exe
if ($LASTEXITCODE -ne 0) { throw 'Dungeon protocol tests failed.' }
