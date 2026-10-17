$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $PSScriptRoot 'tests\bin\EffectsTests.exe'
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $exe) | Out-Null
$files=@('DungeonProtocol.cs','EnemyAssetIdentity.cs','CharacterCatalog.cs','EffectsProtocol.cs') | ForEach-Object {Join-Path $PSScriptRoot ('GungeonTogether\Lan\'+$_)}
& $compiler /nologo /target:exe /optimize+ "/out:$exe" @files (Join-Path $PSScriptRoot 'tests\EffectsTests.cs')
if($LASTEXITCODE -ne 0){throw 'Effects tests did not compile.'}
& $exe
if($LASTEXITCODE -ne 0){throw 'Effects tests failed.'}
