param(
    [Parameter(Mandatory = $true)][string]$PlayerRoot,
    [Parameter(Mandatory = $true)][string]$EvidencePath,
    [string]$CecilPath = 'C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Data\Managed\Unity.Cecil.dll'
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $PlayerRoot).Path
$forbidden = @(Get-ChildItem -LiteralPath $root -Recurse -File |
    Where-Object { $_.Name -match '^(steam_appid\.txt|(?:lib)?steam_api.*)$' })
if ($forbidden) { throw 'Steam native runtime/development appid found in STOVE player.' }
Add-Type -Path $CecilPath
$assemblyPath = Join-Path $root 'BALL-A-RETRO_Data/Managed/Assembly-CSharp.dll'
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyPath)
$checks = [Collections.Generic.List[string]]::new()
try {
    $types = $assembly.MainModule.Types
    foreach ($name in @('CJS_Script_SubmitOnKey', 'KHS_Script_ResetController')) {
        $type = $types | Where-Object Name -eq $name
        if (!$type -or @($type.Methods | Where-Object Name -eq 'Update').Count -ne 0) {
            throw "Release player contains debug Update method: $name"
        }
        $checks.Add("No debug Update: $name")
    }
    foreach ($name in @('YJ_Script_BallController', 'KHS_Script_BallController')) {
        $method = ($types | Where-Object Name -eq $name).Methods | Where-Object Name -eq 'FixedUpdate'
        if (!$method -or !$method.HasBody) { throw "Missing $name.FixedUpdate" }
        if (@($method.Body.Instructions | Where-Object { $_.Operand -and $_.Operand.ToString() -match 'UnityEngine.Input::GetKey' }).Count) {
            throw "Release player still reads debug keys in $name.FixedUpdate"
        }
        $checks.Add("No debug impulse input: $name")
    }
    $platform = ($types | Where-Object Name -eq 'BallARetroPlatform').Methods | Where-Object Name -eq 'get_IsSteamBuild'
    if (!$platform -or !@($platform.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'ldc.i4.0' }).Count -or
        @($platform.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'ldc.i4.1' }).Count) {
        throw 'Player was not compiled with Steam disabled.'
    }
    $checks.Add('Compiled platform flag is STOVE (Steam disabled)')
    $pinvoke = @($types | ForEach-Object Methods | Where-Object { $_.IsPInvokeImpl -and $_.PInvokeInfo.Module.Name -match 'steam_api' })
    if ($pinvoke.Count) { throw 'Steam native API imports found in game assembly.' }
    $checks.Add('No Steam native runtime files or API imports')
    if (@($types | Where-Object { $_.Name -match '^BallARetroScene(Audit|Probe)$' }).Count) {
        throw 'Editor scene audit leaked into player.'
    }
    $checks.Add('No editor scene audit code in player')
    @{ success = $true; assembly = $assemblyPath; sha256 = (Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash;
        checks = $checks.ToArray() } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $EvidencePath -Encoding utf8
    $checks
}
finally { $assembly.Dispose() }
