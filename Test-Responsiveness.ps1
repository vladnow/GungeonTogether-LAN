$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $PSScriptRoot 'tests\bin\ResponsivenessTests.exe'
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $exe) | Out-Null
$files=@('DungeonProtocol.cs','EnemyAssetIdentity.cs','CharacterCatalog.cs','MotionProtocol.cs','MovementPrediction.cs','NetworkTiming.cs','SnapshotBuffer.cs') | ForEach-Object {Join-Path $PSScriptRoot ('GungeonTogether\Lan\'+$_)}
& $compiler /nologo /target:exe /optimize+ "/out:$exe" @files (Join-Path $PSScriptRoot 'tests\ResponsivenessTests.cs')
if($LASTEXITCODE -ne 0){throw 'Responsiveness tests did not compile.'}
& $exe
if($LASTEXITCODE -ne 0){throw 'Responsiveness tests failed.'}
