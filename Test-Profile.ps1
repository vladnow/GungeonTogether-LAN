$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin = Join-Path $PSScriptRoot 'tests\bin'
New-Item -ItemType Directory -Force -Path $bin | Out-Null
$exe = Join-Path $bin 'ProfileTests.exe'
& $compiler /nologo /target:exe /optimize+ "/out:$exe" (Join-Path $PSScriptRoot 'GungeonTogether\Lan\ProfilePolicy.cs') (Join-Path $PSScriptRoot 'tests\ProfileTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Profile test compilation failed.' }
& $exe $PSScriptRoot
if ($LASTEXITCODE -ne 0) { throw 'Profile regression tests failed.' }
