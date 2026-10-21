$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin = Join-Path $PSScriptRoot 'tests\bin'
New-Item -ItemType Directory -Force -Path $bin | Out-Null
$exe = Join-Path $bin 'RunRulesTests.exe'
$sources = @('DungeonProtocol.cs','EnemyAssetIdentity.cs','CharacterCatalog.cs','RunRules.cs','MenuLocalization.cs') | ForEach-Object { Join-Path $PSScriptRoot ('GungeonTogether\Lan\' + $_) }
& $compiler /nologo /target:exe /optimize+ "/out:$exe" @sources (Join-Path $PSScriptRoot 'tests\RunRulesTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Rules tests failed to compile.' }
& $exe
if ($LASTEXITCODE -ne 0) { throw 'Rules tests failed.' }
