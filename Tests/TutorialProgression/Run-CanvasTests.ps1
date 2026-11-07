param(
    [Parameter(Mandatory = $true)][string]$OutputRoot,
    [switch]$IncludeHover,
    [switch]$IncludeLobby,
    [switch]$IncludeMenuInteractions,
    [switch]$IncludeLocalization,
    [switch]$IncludeEffects,
    [switch]$IncludePersonalRecords,
    [switch]$IncludeSteamLogin,
    [string]$PythonPath = 'python',
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
if ($IncludePersonalRecords -and $IncludeSteamLogin) { throw 'Run STOVE and Steam configurations in separate test projects.' }
$sourceRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$outputPath = [IO.Path]::GetFullPath($OutputRoot)
if ((Test-Path -LiteralPath "$outputPath/Assets") -or $outputPath.StartsWith($sourceRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Use a fresh directory outside the source project.' }
$null = New-Item -ItemType Directory -Force -Path "$outputPath/Assets/Runtime", "$outputPath/Assets/Tests", "$outputPath/Packages", "$outputPath/ProjectSettings"
$lock = Get-Content -LiteralPath "$sourceRoot/Packages/packages-lock.json" -Raw | ConvertFrom-Json
$dependencies = [ordered]@{}
$pending = [Collections.Generic.Queue[string]]::new()
if ($IncludePersonalRecords -or $IncludeSteamLogin) { $pending.Enqueue('com.unity.modules.unitywebrequest') }
@('com.unity.localization', 'com.unity.ugui', 'com.unity.render-pipelines.universal', 'com.unity.test-framework', 'com.unity.modules.audio', 'com.unity.modules.animation', 'com.unity.modules.uielements', 'com.unity.modules.screencapture', 'com.unity.modules.physics', 'com.unity.modules.particlesystem') | ForEach-Object { $pending.Enqueue($_) }
while ($pending.Count -gt 0) {
    $name = $pending.Dequeue()
    if ($dependencies.Contains($name)) { continue }
    $info = $lock.dependencies.$name
    if ($null -eq $info) { throw "Package missing: $name" }
    $cached = @(Get-ChildItem -LiteralPath "$sourceRoot/Library/PackageCache" -Directory -Filter "$name@*")
    if ($cached.Count -eq 1) { $dependencies[$name] = 'file:' + $cached[0].FullName.Replace('\', '/') }
    elseif ($info.source -eq 'builtin') { $dependencies[$name] = $info.version }
    else { throw "Missing cache: $name" }
    $info.dependencies.PSObject.Properties.Name | Where-Object { $_ } | ForEach-Object { $pending.Enqueue($_) }
}
@{ dependencies = $dependencies } | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath "$outputPath/Packages/manifest.json" -Encoding utf8
Copy-Item -LiteralPath "$sourceRoot/ProjectSettings/ProjectVersion.txt", "$sourceRoot/ProjectSettings/TagManager.asset" -Destination "$outputPath/ProjectSettings"
Copy-Item -LiteralPath "$sourceRoot/Assets/Public/Font" -Destination "$outputPath/Assets/Fonts" -Recurse
Copy-Item -LiteralPath "$sourceRoot/Assets/TextMesh Pro" -Destination "$outputPath/Assets/TextMesh Pro" -Recurse
Copy-Item -LiteralPath "$sourceRoot/Assets/Public/Main_Game/UI_Game/Textures/RectanChoice_518-911.png", "$sourceRoot/Assets/Public/Main_Game/UI_Game/Textures/RectanChoice_518-911.png.meta" -Destination "$outputPath/Assets"
@{ name = 'BallARetro.UIRuntime'; references = @('Unity.Localization', 'Unity.ResourceManager', 'UnityEngine.UI', 'Unity.TextMeshPro', 'Unity.RenderPipelines.Universal.Runtime', 'Unity.RenderPipelines.Core.Runtime') } | ConvertTo-Json | Set-Content -LiteralPath "$outputPath/Assets/Runtime/UIRuntime.asmdef" -Encoding utf8
@{ name = 'BallARetro.UITests'; references = @('BallARetro.UIRuntime', 'Unity.Localization', 'Unity.ResourceManager', 'UnityEngine.UI', 'Unity.TextMeshPro', 'Unity.RenderPipelines.Universal.Runtime', 'Unity.RenderPipelines.Core.Runtime'); optionalUnityReferences = @('TestAssemblies'); autoReferenced = $false } | ConvertTo-Json | Set-Content -LiteralPath "$outputPath/Assets/Tests/UITests.asmdef" -Encoding utf8

& $PythonPath "$PSScriptRoot/Extract-UIFixture.py" "$sourceRoot/Assets/MainScenes/Main_Scene_Tutorial.unity" "$outputPath/Assets/TutorialUI.unity"
if ($LASTEXITCODE -ne 0) { throw 'Fixture extraction failed.' }
Copy-Item -LiteralPath "$PSScriptRoot/TutorialCanvasTests.cs" -Destination "$outputPath/Assets/Tests"
@('PSH_Script_BaseButtonHover.cs', 'PSH_Script_CursorManager.cs') | ForEach-Object {
    Copy-Item -LiteralPath "$sourceRoot/Assets/Public/UI_Public/Scripts/$_", "$sourceRoot/Assets/Public/UI_Public/Scripts/$_.meta" -Destination "$outputPath/Assets/Runtime"
}
$filter = 'TutorialCanvasTests'
$null = New-Item -ItemType Directory -Force -Path "$outputPath/Assets/Resources"
Copy-Item "$sourceRoot/Assets/Resources/LocalizationData" "$outputPath/Assets/Resources/LocalizationData" -Recurse
Copy-Item "$sourceRoot/Assets/Public/UI_Public/Scripts/PSH_Script_Local*.cs*" "$outputPath/Assets/Runtime"
Copy-Item "$sourceRoot/Tests/Localization/LocaleTestContext.cs", "$sourceRoot/Tests/Localization/DefaultLocaleFixture.cs" "$outputPath/Assets/Tests"
if ($IncludeHover) {
    Copy-Item -LiteralPath "$sourceRoot/Tests/UISettings/ButtonHoverTests.cs" -Destination "$outputPath/Assets/Tests"
    $filter += ';ButtonHoverTests'
}
if ($IncludeEffects) {
    Copy-Item -LiteralPath "$sourceRoot/Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Scripts/CFXR_Effect.cs", "$sourceRoot/Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Scripts/CFXR_Effect.CameraShake.cs" -Destination "$outputPath/Assets/Runtime"
    Copy-Item "$sourceRoot/Assets/Public/UI_Public/Scripts/PSH_Script_Effect*.cs" -Destination "$outputPath/Assets/Runtime"
    Copy-Item -LiteralPath "$sourceRoot/Assets/MemberFolder/PSH/UI_Sourse/Scripts/PSH_Script_ToggleUIController.cs", "$sourceRoot/Assets/MemberFolder/CJS/CJS_Scripts/CJS_Script_CameraShaker.cs" -Destination "$outputPath/Assets/Runtime"
    Copy-Item -LiteralPath "$sourceRoot/Assets/MemberFolder/PSH/UI_Sourse/Scripts/PSH_Script_ToggleUIController.cs.meta" -Destination "$outputPath/Assets/Runtime"
    & $PythonPath "$sourceRoot/Tests/UISettings/Extract-EffectRow.py" "$sourceRoot/Assets/Public/Main_Game/UI_Game/Prefabs/Panel_Setting.prefab" "$outputPath/Assets/EffectSettingsRow.prefab"
    if ($LASTEXITCODE -ne 0) { throw 'Effects settings row extraction failed.' }
    Copy-Item -LiteralPath "$sourceRoot/Tests/UISettings/EffectSettingsTests.cs", "$sourceRoot/Tests/UISettings/EffectOptionsTests.cs" -Destination "$outputPath/Assets/Tests"
    $null = New-Item -ItemType Directory -Path "$outputPath/Assets/EffectShaders"
    @('ProBuilder Glow.shadergraph', 'S_glitchEffect.shadergraph', 'S_glitchEffect_Canvase.shadergraph', 'S_twisterFace.shadergraph', 'BallARetroEffectTime.hlsl') | ForEach-Object {
        Copy-Item -LiteralPath "$sourceRoot/Assets/MemberFolder/PSH/PSH_Shaders/$_", "$sourceRoot/Assets/MemberFolder/PSH/PSH_Shaders/$_.meta" -Destination "$outputPath/Assets/EffectShaders"
    }
    $filter += ';EffectSettingsTests;EffectOptionsTests'
}
if ($IncludePersonalRecords -or $IncludeSteamLogin -or $IncludeLobby) {
    Copy-Item "$sourceRoot/Assets/Public/Platform/*.cs*" -Destination "$outputPath/Assets/Runtime"
    Copy-Item -LiteralPath "$sourceRoot/Assets/MemberFolder/CJS/CJS_Scripts/CJS_Script_PinballRankingService.cs", "$sourceRoot/Assets/MemberFolder/CJS/CJS_Scripts/CJS_Script_Models.cs", "$sourceRoot/Assets/Scripts/Steamworks.NET/SteamManager.cs" -Destination "$outputPath/Assets/Runtime"
}
if ($IncludePersonalRecords) {
    Copy-Item -LiteralPath "$sourceRoot/Tests/UISettings/PersonalRecordTests.cs" -Destination "$outputPath/Assets/Tests"
    $filter += ';PersonalRecordTests'
}
if ($IncludeSteamLogin) {
    Copy-Item -LiteralPath "$sourceRoot/Assets/MemberFolder/KHS/KHS_Scripts/KHS_Script_SteamNickLoader.cs", "$sourceRoot/Tests/SteamLogin/SteamApiStub.cs", "$sourceRoot/Tests/SteamLogin/SteamLeaderboardStub.cs", "$sourceRoot/Assets/Public/Platform/BallARetroSteamSettings.cs.meta" -Destination "$outputPath/Assets/Runtime"
    Copy-Item -LiteralPath "$sourceRoot/Tests/SteamLogin/SteamIdentityTests.cs" -Destination "$outputPath/Assets/Tests"
    $null = New-Item -ItemType Directory -Force -Path "$outputPath/Assets/Resources/Release", "$outputPath/Assets/Editor"
    # A synthetic ID is used exclusively with the fake Steam client, outside the game project.
    (Get-Content -LiteralPath "$sourceRoot/Assets/Resources/Release/SteamSettings.asset" -Raw) -replace 'steamAppId: \d+', 'steamAppId: 123456' | Set-Content -LiteralPath "$outputPath/Assets/Resources/Release/SteamSettings.asset" -Encoding utf8
    Copy-Item -LiteralPath "$sourceRoot/Assets/Editor/BallARetroReleaseBuild.cs" -Destination "$outputPath/Assets/Editor"
    '-define:BALLARETRO_STEAM' | Set-Content -LiteralPath "$outputPath/Assets/csc.rsp" -Encoding utf8
    $filter += ';SteamIdentityTests'
}
if ($IncludeLocalization) {
    if (!$IncludeEffects) { throw 'Localization UI checks include the effects dialog; pass -IncludeEffects.' }
    @('CJS_ChoiceData.cs', 'CJS_ChoiceSnapshot.cs', 'CJS_Script_ChoiceCard.cs', 'CJS_Script_ChoiceSlot.cs', 'CJS_Script_TooltipTarget.cs', 'CJS_Script_TooltipUI.cs') | ForEach-Object {
        Copy-Item "$sourceRoot/Assets/MemberFolder/CJS/CJS_Scripts/$_", "$sourceRoot/Assets/MemberFolder/CJS/CJS_Scripts/$_.meta" "$outputPath/Assets/Runtime"
    }
    @('PSH_Script_LanguageManager.cs', 'PSH_Script_ResolutionSettings.cs') | ForEach-Object {
        Copy-Item "$sourceRoot/Assets/MemberFolder/PSH/UI_Sourse/Scripts/$_", "$sourceRoot/Assets/MemberFolder/PSH/UI_Sourse/Scripts/$_.meta" "$outputPath/Assets/Runtime"
    }
    Copy-Item "$sourceRoot/Tests/Localization/ChoiceUIStub.cs" "$outputPath/Assets/Runtime"
    Copy-Item "$sourceRoot/Tests/Localization/LocalizationUITests.cs" "$outputPath/Assets/Tests"
    & $PythonPath "$sourceRoot/Tests/Localization/Extract-UI.py" $sourceRoot $outputPath
    if ($LASTEXITCODE -ne 0) { throw 'Localized UI extraction failed.' }
    $filter += ';LocalizationUITests'
}
if ($IncludeMenuInteractions -or $IncludeLobby) {
    if (!$IncludeLocalization) { throw 'Menu interaction checks use the localized UI fixture; pass -IncludeLocalization.' }
    @('Assets/MemberFolder/PSH/UI_Sourse/Scripts/PSH_Script_SettingController.cs',
      'Assets/Public/UI_Public/Scripts/PSH_Script_ModalCanvas.cs',
      'Assets/Public/Main_Lobby/UI_Lobby/Scripts/PSH_Script_InputFieldHover.cs') | ForEach-Object {
        Copy-Item "$sourceRoot/$_", "$sourceRoot/$_.meta" "$outputPath/Assets/Runtime"
    }
    Copy-Item "$sourceRoot/Tests/DialogueInput/GameEventStubs.cs" "$outputPath/Assets/Runtime"
    Copy-Item "$sourceRoot/Tests/UISettings/SettingsInteractionTests.cs", "$sourceRoot/Tests/UISettings/CursorInteractionTests.cs" "$outputPath/Assets/Tests"
    & $PythonPath "$sourceRoot/Tests/Localization/Extract-UI.py" $sourceRoot $outputPath --interactions
    if ($LASTEXITCODE -ne 0) { throw 'Settings interaction extraction failed.' }
    $filter += ';SettingsInteractionTests;CursorInteractionTests'
}
if ($IncludeLobby) {
    Copy-Item "$sourceRoot/Assets/MemberFolder/CJS/CJS_Scripts/CJS_Script_GameOverUI.cs", "$sourceRoot/Assets/MemberFolder/CJS/CJS_Scripts/CJS_Script_GameOverUI.cs.meta", "$sourceRoot/Assets/MemberFolder/CJS/CJS_Scripts/CJS_Script_LeaderboardList.cs" "$outputPath/Assets/Runtime"
    @('Assets/MemberFolder/PSH/PSH_Lobby/Scripts/PSH_Script_LobbyManager.cs',
      'Assets/Public/Main_Lobby/UI_Lobby/Scripts/PSH_Script_StartButton.cs',
      'Assets/Public/UI_Public/Scripts/PSH_Script_WorldRankingUI.cs') | ForEach-Object {
        Copy-Item "$sourceRoot/$_", "$sourceRoot/$_.meta" "$outputPath/Assets/Runtime"
    }
    Copy-Item "$sourceRoot/Tests/UISettings/LobbySceneLoaderStub.cs" "$outputPath/Assets/Runtime"
    Copy-Item "$sourceRoot/Tests/UISettings/LobbyFlowTests.cs" "$outputPath/Assets/Tests"
    & $PythonPath "$sourceRoot/Tests/Localization/Extract-UI.py" $sourceRoot $outputPath --interactions --lobby
    if ($LASTEXITCODE -ne 0) { throw 'Lobby UI extraction failed.' }
    $filter = 'LobbyFlowTests'
    if ($IncludeSteamLogin) {
        Copy-Item "$sourceRoot/Tests/SteamLogin/SteamLeaderboardTests.cs" "$outputPath/Assets/Tests"
        $filter += ';SteamIdentityTests;SteamLeaderboardTests'
    }
    if ($IncludePersonalRecords) { $filter += ';PersonalRecordTests' }
}
$arguments = @('-batchmode', '-screen-width', '1920', '-screen-height', '1080', '-projectPath', "`"$outputPath`"", '-runTests', '-testPlatform', 'PlayMode', '-testFilter', $filter, '-testResults', "`"$outputPath/results.xml`"", '-logFile', "`"$outputPath/unity.log`"")
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Test project: $outputPath; Unity process: $($process.Id)"
$process.WaitForExit()
if (!(Test-Path -LiteralPath "$outputPath/results.xml")) { throw 'Unity produced no test results. See unity.log.' }
[xml]$result = Get-Content -LiteralPath "$outputPath/results.xml"
$result.'test-run' | Select-Object result, total, passed, failed
if ($result.'test-run'.result -ne 'Passed') { throw 'Tutorial canvas checks failed.' }
