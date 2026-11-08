param(
    [Parameter(Mandatory = $true)][string]$PlayerRoot,
    [Parameter(Mandatory = $true)][string]$EvidencePath,
    [string]$CecilPath = 'C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Data\Managed\Unity.Cecil.dll',
    [ValidateSet('STOVE', 'Steam', 'SteamDemo')][string]$Platform = 'STOVE'
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $PlayerRoot).Path
$appInfoPath = Join-Path $root 'BALL-A-RETRO_Data/app.info'
if (!(Test-Path -LiteralPath $appInfoPath)) { throw 'Player application identity is missing.' }
$appInfo = @(Get-Content -LiteralPath $appInfoPath | Where-Object { ![string]::IsNullOrWhiteSpace($_) })
$expectedProductName = if ($Platform -eq 'SteamDemo') { 'Ball-A-Retro Demo' } else { 'Ball-A-Retro' }
if ($appInfo.Count -ne 2 -or $appInfo[0] -ne 'G-PIN' -or $appInfo[1] -ne $expectedProductName) {
    throw "Player company/product identity does not match $Platform save-data namespace."
}
$forbidden = @(Get-ChildItem -LiteralPath $root -Recurse -File |
    Where-Object { $_.Name -match '^(steam_appid\.txt|(?:lib)?steam_api.*)$' })
if ($Platform -eq 'STOVE' -and $forbidden) { throw 'Steam native runtime/development appid found in STOVE player.' }
if ($forbidden | Where-Object Name -eq 'steam_appid.txt') { throw 'Development App ID file must not be shipped.' }
$steamNative = @()
if ($Platform -ne 'STOVE') {
    $steamNative = @($forbidden | Where-Object Name -eq 'steam_api64.dll')
    if ($steamNative.Count -ne 1 -or $forbidden.Count -ne 1) { throw 'Steam Win64 player must contain exactly one steam_api64.dll and no other Steam native variants.' }
    $nativeSource = Join-Path $PSScriptRoot '../../Assets/com.rlabrecque.steamworks.net/Plugins/steam_api64.dll'
    if ((Get-FileHash -LiteralPath $steamNative[0].FullName).Hash -ne (Get-FileHash -LiteralPath $nativeSource).Hash) {
        throw 'Steam native library does not match the vendored Steamworks.NET library.'
    }
}
Add-Type -Path $CecilPath
$assemblyPath = Join-Path $root 'BALL-A-RETRO_Data/Managed/Assembly-CSharp.dll'
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyPath)
$checks = [Collections.Generic.List[string]]::new()
$checks.Add("Player product name isolates the expected save-data namespace: $expectedProductName")
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
    $platformGetter = ($types | Where-Object Name -eq 'BallARetroPlatform').Methods | Where-Object Name -eq 'get_IsSteamBuild'
    $expectedConstant = if ($Platform -ne 'STOVE') { 'ldc.i4.1' } else { 'ldc.i4.0' }
    $unexpectedConstant = if ($Platform -ne 'STOVE') { 'ldc.i4.0' } else { 'ldc.i4.1' }
    if (!$platformGetter -or !$platformGetter.HasBody -or !@($platformGetter.Body.Instructions | Where-Object { $_.OpCode.Name -eq $expectedConstant }).Count -or
        @($platformGetter.Body.Instructions | Where-Object { $_.OpCode.Name -eq $unexpectedConstant }).Count) {
        throw "Player was not compiled for $Platform."
    }
    $checks.Add("Compiled platform flag is $Platform")
    $demoGetter = ($types | Where-Object Name -eq 'BallARetroPlatform').Methods | Where-Object Name -eq 'get_IsDemoBuild'
    $expectedDemoConstant = if ($Platform -eq 'SteamDemo') { 'ldc.i4.1' } else { 'ldc.i4.0' }
    $unexpectedDemoConstant = if ($Platform -eq 'SteamDemo') { 'ldc.i4.0' } else { 'ldc.i4.1' }
    if ($demoGetter) {
        if (!$demoGetter.HasBody -or !@($demoGetter.Body.Instructions | Where-Object { $_.OpCode.Name -eq $expectedDemoConstant }).Count -or
            @($demoGetter.Body.Instructions | Where-Object { $_.OpCode.Name -eq $unexpectedDemoConstant }).Count) {
            throw "Player demo flag does not match $Platform."
        }
        $checks.Add("Compiled demo flag matches $Platform")
    } elseif ($Platform -eq 'SteamDemo') { throw 'Demo edition flag is missing.' }
    if ($Platform -eq 'SteamDemo') {
        $backend = $types | Where-Object Name -eq 'BallARetroSteamLeaderboard'
        if (!$backend -or @($backend.Methods | Where-Object Name -in @('FindBoard', 'FetchRoutine', 'SubmitRoutine')).Count -ne 3) {
            throw 'Demo-scoped online leaderboard implementation is missing.'
        }
        $recordKey = ($types | Where-Object Name -eq 'BallARetroPersonalRecordStore').Methods | Where-Object Name -eq 'StorageKey'
        if (!$recordKey -or !@($recordKey.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'ldstr' -and $_.Operand -eq 'BallARetro.SteamDemo.Best.v1.' }).Count) {
            throw 'Demo personal-record namespace is missing.'
        }
        $checks.Add('Demo has separate personal records and its Steam App ID scoped leaderboard implementation')
    }
    $pinvoke = @($types | ForEach-Object Methods | Where-Object { $_.IsPInvokeImpl -and $_.PInvokeInfo.Module.Name -match 'steam_api' })
    if ($Platform -eq 'STOVE') {
        if ($pinvoke.Count) { throw 'Steam native API imports found in game assembly.' }
        $checks.Add('No Steam native runtime files or API imports')
    } else {
        $manager = $types | Where-Object Name -eq 'SteamManager'
        $awake = $manager.Methods | Where-Object Name -eq 'Awake'
        if (!$awake -or !$awake.HasBody) { throw 'Steam initialization was stripped from the player.' }
        $operands = @($awake.Body.Instructions | Where-Object Operand | ForEach-Object { $_.Operand.ToString() }) -join "`n"
        foreach ($required in @('Steamworks.SteamAPI::RestartAppIfNecessary', 'Steamworks.SteamAPI::Init', 'Steamworks.SteamUtils::GetAppID', 'BallARetroSteamSettings::TryGetAppId')) {
            if (!$operands.Contains($required)) { throw "Missing compiled Steam startup call: $required" }
        }
        $wrapperPath = Join-Path $root 'BALL-A-RETRO_Data/Managed/com.rlabrecque.steamworks.net.dll'
        if (!(Test-Path -LiteralPath $wrapperPath)) { throw 'Steamworks.NET runtime assembly is missing.' }
        $wrapper = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($wrapperPath)
        try {
            if (!@($wrapper.MainModule.ModuleReferences | Where-Object Name -Match '^steam_api64').Count) {
                throw 'Steamworks.NET has no Windows x64 native API reference.'
            }
            if (@($wrapper.MainModule.Types | Where-Object FullName -eq 'Steamworks.Fixture').Count) { throw 'Fake Steam client leaked into release.' }
        } finally { $wrapper.Dispose() }
        $checks.Add('Steam initialization, App ID validation and native wrapper are compiled')
        $checks.Add('Exactly one native library, matching the Steamworks.NET package')
    }
    if (@($types | Where-Object { $_.Name -match '^BallARetro(Scene(Audit(Runner)?|Probe)|RenderProbe|ChoiceIcon(Preparation|Probe|Audit(Runner)?))$' }).Count) {
        throw 'Editor scene audit leaked into player.'
    }
    $checks.Add('No editor scene audit code in player')
    @{ success = $true; platform = $Platform; assembly = $assemblyPath; sha256 = (Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash;
        checks = $checks.ToArray() } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $EvidencePath -Encoding utf8
    $checks
}
finally { $assembly.Dispose() }
