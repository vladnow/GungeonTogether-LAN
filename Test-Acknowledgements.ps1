$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin = Join-Path $PSScriptRoot 'tests\bin'
New-Item -ItemType Directory -Force -Path $bin | Out-Null
$exe = Join-Path $bin 'AcknowledgementTests.exe'
& $compiler /nologo /target:exe /optimize+ "/out:$exe" (Join-Path $PSScriptRoot 'GungeonTogether\Lan\InputAcknowledgements.cs') (Join-Path $PSScriptRoot 'GungeonTogether\Lan\DungeonProtocol.cs') (Join-Path $PSScriptRoot 'GungeonTogether\Lan\EnemyAssetIdentity.cs') (Join-Path $PSScriptRoot 'GungeonTogether\Lan\CharacterCatalog.cs') (Join-Path $PSScriptRoot 'tests\AcknowledgementTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Input acknowledgement compilation failed.' }
& $exe
if ($LASTEXITCODE -ne 0) { throw 'Input acknowledgement checks failed.' }
