param([switch]$ExpectFresh, [switch]$Fifo, [string]$OutputPath)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin = Join-Path $PSScriptRoot 'tests\bin'
New-Item -ItemType Directory -Force -Path $bin | Out-Null
$exe = Join-Path $bin 'TransportLatencyTests.exe'
& $compiler /nologo /target:exe /optimize+ "/out:$exe" (Join-Path $PSScriptRoot 'GungeonTogether\Lan\LanTransport.cs') (Join-Path $PSScriptRoot 'GungeonTogether\Lan\SnapshotBuffer.cs') (Join-Path $PSScriptRoot 'tests\TransportLatencyTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Transport latency test compilation failed.' }
$arguments = @()
if ($ExpectFresh) { $arguments += '--expect-fresh' }
if ($Fifo) { $arguments += '--fifo' }
if ($OutputPath) { & $exe @arguments | Tee-Object -FilePath $OutputPath }
else { & $exe @arguments }
if ($LASTEXITCODE -ne 0) { throw 'Transport latency checks failed.' }
