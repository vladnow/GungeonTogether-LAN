$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin = Join-Path $PSScriptRoot 'tests\bin'
New-Item -ItemType Directory -Force -Path $bin | Out-Null
$exe = Join-Path $bin 'DisplayProfileTests.exe'
& $compiler /nologo /target:exe /optimize+ "/out:$exe" (Join-Path $PSScriptRoot 'GungeonTogether\Lan\ProfileDisplayPolicy.cs') (Join-Path $PSScriptRoot 'tests\DisplayProfileTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Display profile test compilation failed.' }
& $exe
if ($LASTEXITCODE -ne 0) { throw 'Display profile tests failed.' }
