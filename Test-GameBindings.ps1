param([Parameter(Mandatory=$true)][string]$GamePath,[Parameter(Mandatory=$true)][string]$BepInExCorePath)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $BepInExCorePath 'Mono.Cecil.dll')
$managed = Join-Path $GamePath 'EtG_Data\Managed'
$runtime = Join-Path $PSScriptRoot 'bin\GungeonTogether.LAN-0.3.5-alpha.dll'
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
 $game = [Mono.Cecil.ModuleDefinition]::ReadModule((Join-Path $managed 'Assembly-CSharp.dll'))
 try {
  $sound = $game.Types | Where-Object Name -eq 'AkSoundEngine'
  $events = @($sound.Methods | Where-Object { $_.IsPublic -and $_.IsStatic -and $_.Name -eq 'PostEvent' -and $_.ReturnType.FullName -eq 'System.UInt32' })
  $switches = @($sound.Methods | Where-Object { $_.IsPublic -and $_.IsStatic -and $_.Name -eq 'SetSwitch' -and $_.Parameters.Count -eq 3 -and $_.Parameters[2].ParameterType.FullName -eq 'UnityEngine.GameObject' })
  if($events.Count -ne 12 -or $switches.Count -ne 2){throw 'Unexpected native sound overload set.'}
  foreach($method in @($events)+@($switches)) {
   if(!$method.HasBody){throw 'Sound hook requires managed Wwise wrapper.'}
   if($method.Name -eq 'PostEvent') {
    if($method.Parameters[0].ParameterType.FullName -notin @('System.UInt32','System.String') -or $method.Parameters[1].ParameterType.FullName -ne 'UnityEngine.GameObject'){throw 'Unsupported sound hook arguments.'}
    if(@($method.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.DeclaringType.Name -eq 'AkSoundEngine' -and $_.Operand.Name -eq 'PostEvent' }).Count){throw 'Nested PostEvent wrappers would duplicate captured sounds.'}
   }
   $count++; Write-Output ('PASS: managed sound hook ' + $method.FullName)
  }
 } finally {$game.Dispose()}
 Write-Output ('GAME_BINDINGS_PASS: ' + $count + ' exact targets in actual game assemblies')
}
finally { $module.Dispose(); $resolver.Dispose() }
