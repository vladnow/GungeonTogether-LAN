param([Parameter(Mandatory=$true)][string]$GamePath,[Parameter(Mandatory=$true)][string]$BepInExCorePath)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $BepInExCorePath 'Mono.Cecil.dll')
$managed = Join-Path $GamePath 'EtG_Data\Managed'
$runtime = Join-Path $PSScriptRoot 'bin\GungeonTogether.LAN-0.3.2-alpha.dll'
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory($managed)
$resolver.AddSearchDirectory($BepInExCorePath)
$parameters = New-Object Mono.Cecil.ReaderParameters
$parameters.AssemblyResolver = $resolver
$module = [Mono.Cecil.ModuleDefinition]::ReadModule($runtime,$parameters)
$count = 0
function Check-Type($type) {
 foreach ($attribute in $type.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' }) {
  $target = $null; $name = $null; $signature = $null
  foreach ($argument in $attribute.ConstructorArguments) {
   if ($argument.Type.FullName -eq 'System.Type') { $target = $argument.Value }
   elseif ($argument.Type.FullName -eq 'System.String') { $name = $argument.Value }
   elseif ($argument.Type.FullName -eq 'System.Type[]') { $signature = @($argument.Value | ForEach-Object { $_.Value.FullName }) }
  }
  if ($null -eq $target -or !$name) { continue }
  $definition = $target.Resolve()
  $methods = @($definition.Methods | Where-Object Name -eq $name)
  if ($null -ne $signature) {
   $methods = @($methods | Where-Object { ($_.Parameters.ParameterType.FullName -join ',') -eq ($signature -join ',') })
  }
  if ($methods.Count -ne 1) { throw ("Ambiguous or missing patch target: $target::$name candidates=" + $methods.Count) }
  if (!$methods[0].HasBody) { throw ("Cannot patch native method body: $target::$name") }
  $script:count++
  Write-Output ('PASS: unique managed patch target ' + $methods[0].FullName)
 }
 foreach ($nested in $type.NestedTypes) { Check-Type $nested }
}
try {
 foreach ($type in $module.Types) { Check-Type $type }
 Write-Output ('GAME_BINDINGS_PASS: ' + $count + ' exact targets in actual game assemblies')
}
finally { $module.Dispose(); $resolver.Dispose() }
