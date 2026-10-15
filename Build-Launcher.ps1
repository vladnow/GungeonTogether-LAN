$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$framework = Split-Path -Parent $compiler
$plugin = Join-Path $PSScriptRoot 'bin\GungeonTogether.LAN-0.3.0-alpha.dll'
if (!(Test-Path -LiteralPath $plugin)) { throw 'Build the LAN plugin first.' }
& $compiler /nologo /target:winexe /optimize+ /codepage:65001 ("/out:" + (Join-Path $PSScriptRoot 'GungeonTogether-LAN.exe')) /reference:System.Windows.Forms.dll /reference:System.Drawing.dll ("/reference:" + (Join-Path $framework 'System.IO.Compression.dll')) ("/reference:" + (Join-Path $framework 'System.IO.Compression.FileSystem.dll')) ("/resource:$plugin,LanPlugin") (Join-Path $PSScriptRoot 'launcher\Launcher.cs')
if ($LASTEXITCODE -ne 0) { throw 'Launcher compilation failed.' }

