param(
    [Parameter(Mandatory = $true)][string]$OutputRoot,
    [switch]$Baseline,
    [switch]$IncludeBallRescue,
    [switch]$IncludeScorePopups,
    [switch]$IncludeTutorial,
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$outputPath = [IO.Path]::GetFullPath($OutputRoot)
if ($outputPath.StartsWith($sourceRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Use an output directory outside the source project.'
}
if (Test-Path -LiteralPath "$outputPath/Assets") { throw 'Use a fresh output directory for each test run.' }
$null = New-Item -ItemType Directory -Force -Path "$outputPath/Assets/Runtime", "$outputPath/Assets/Tests", "$outputPath/Packages", "$outputPath/ProjectSettings"

# Resolve the exact installed package versions without changing the open project.
$lock = Get-Content -LiteralPath "$sourceRoot/Packages/packages-lock.json" -Raw | ConvertFrom-Json
$dependencies = [ordered]@{}
$pending = [Collections.Generic.Queue[string]]::new()
@('com.unity.ugui', 'com.unity.localization', 'com.unity.test-framework', 'com.unity.modules.audio', 'com.unity.modules.uielements', 'com.unity.modules.screencapture', 'com.unity.modules.physics') | ForEach-Object { $pending.Enqueue($_) }
while ($pending.Count -gt 0) {
    $name = $pending.Dequeue()
    if ($dependencies.Contains($name)) { continue }
    $packageInfo = $lock.dependencies.$name
    if ($null -eq $packageInfo) { throw "Package missing from lock file: $name" }
    $cached = @(Get-ChildItem -LiteralPath "$sourceRoot/Library/PackageCache" -Directory -Filter "$name@*")
    if ($cached.Count -eq 1) {
        $dependencies[$name] = 'file:' + $cached[0].FullName.Replace('\', '/')
    } elseif ($packageInfo.source -eq 'builtin') {
        $dependencies[$name] = $packageInfo.version
    } else { throw "Expected one cached package for $name. Open the source project to resolve packages first." }
    $packageInfo.dependencies.PSObject.Properties.Name | Where-Object { $_ } | ForEach-Object { $pending.Enqueue($_) }
}
@{ dependencies = $dependencies } | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath "$outputPath/Packages/manifest.json" -Encoding utf8
Copy-Item -LiteralPath "$sourceRoot/ProjectSettings/ProjectVersion.txt", "$sourceRoot/ProjectSettings/ProjectSettings.asset" -Destination "$outputPath/ProjectSettings"
Copy-Item -LiteralPath "$sourceRoot/ProjectSettings/TagManager.asset" -Destination "$outputPath/ProjectSettings"

$runtimeSources = @(
    'Assets/MemberFolder/KHS/KHS_Scripts/KHS_Script_ScoreManager.cs',
    'Assets/MemberFolder/KHS/KHS_Scripts/KHS_Script_FliperController.cs',
    'Assets/MemberFolder/KHS/KHS_Scripts/UI/KHS_Script_UIToScore.cs'
)
foreach ($source in $runtimeSources) {
    if ($Baseline) {
        $oldSource = & git -C $sourceRoot show "HEAD:$source"
        if ($LASTEXITCODE -ne 0) { throw "Could not read baseline source: $source" }
        $fileName = Split-Path $source -Leaf
        $oldSource | Set-Content -LiteralPath "$outputPath/Assets/Runtime/$fileName" -Encoding utf8
    } else { Copy-Item -LiteralPath "$sourceRoot/$source" -Destination "$outputPath/Assets/Runtime" }
}
$unchangedSources = @(
    'Assets/MemberFolder/YJ/YJ_Scripts/YJ_Script_BallController.cs',
    'Assets/MemberFolder/YJ/YJ_Scripts/YJ_Script_Flipper.cs',
    'Assets/MemberFolder/KHS/KHS_Scripts/UI/KHS_Script_UIToBall.cs',
    'Assets/MemberFolder/CJS/CJS_Scripts/CJS_Script_GameOverUI.cs',
    'Assets/MemberFolder/CJS/CJS_Scripts/CJS_Script_Models.cs',
    'Assets/Public/Main_Game/UI_Game/Scripts/PSH_Script_DialogueUI.cs',
    'Assets/Public/Main_Game/UI_Game/Scripts/PSH_Script_DialogueClickForwarder.cs',
    'Assets/Public/Main_Game/UI_Game/Scripts/PSH_Script_GameOverSequenceDirector.cs',
    'Assets/MemberFolder/KHS/KHS_Scripts/KHS_Script_GameOverUI.cs',
    'Assets/MemberFolder/PSH/UI_Sourse/Scripts/PSH_Script_SettingController.cs',
    'Assets/Public/UI_Public/Scripts/PSH_Script_ModalCanvas.cs'
)
foreach ($source in $unchangedSources) { Copy-Item -LiteralPath "$sourceRoot/$source" -Destination "$outputPath/Assets/Runtime" }
if (!$Baseline) { Copy-Item -LiteralPath "$sourceRoot/Assets/Public/Main_Game/UI_Game/Scripts/PSH_Script_RunStatusUI.cs" -Destination "$outputPath/Assets/Runtime" }
Copy-Item -LiteralPath "$PSScriptRoot/GameEventStubs.cs" -Destination "$outputPath/Assets/Runtime"
Copy-Item -LiteralPath "$PSScriptRoot/RunProgressionTests.cs" -Destination "$outputPath/Assets/Tests"
if ($IncludeTutorial) {
    if ($Baseline) { throw 'Tutorial checks require the current flipper event.' }
    @('CJS_TutorialDirector.cs', 'CJS_TutorialCameraFocus.cs') | ForEach-Object {
        Copy-Item -LiteralPath "$sourceRoot/Assets/MemberFolder/CJS/CJS_Scripts/$_" -Destination "$outputPath/Assets/Runtime"
    }
    Copy-Item -LiteralPath "$sourceRoot/Tests/TutorialProgression/TutorialProgressionTests.cs" -Destination "$outputPath/Assets/Tests"
}
if ($IncludeBallRescue) {
    if ($Baseline) { throw 'Ball rescue requires the current run state API.' }
    Copy-Item -LiteralPath "$sourceRoot/Assets/MemberFolder/YJ/YJ_Scripts/YJ_Script_BallAutoRescue.cs" -Destination "$outputPath/Assets/Runtime"
    Copy-Item -LiteralPath "$sourceRoot/Tests/BallRescue/BallAutoRescueTests.cs" -Destination "$outputPath/Assets/Tests"
}
if ($IncludeScorePopups) {
    if ($Baseline) { throw 'Popup tests require the positional scoring event.' }
    @('Assets/MemberFolder/KHS/KHS_Scripts/KHS_Script_DumpManager.cs',
      'Assets/MemberFolder/CJS/CJS_Scripts/CJS_Script_ScorePopupSpawner.cs',
      'Assets/MemberFolder/YJ/YJ_Scripts/YJ_Script_PacmanCoin.cs') | ForEach-Object {
        Copy-Item -LiteralPath "$sourceRoot/$_" -Destination "$outputPath/Assets/Runtime"
    }
    Copy-Item -LiteralPath "$sourceRoot/Tests/ScorePopups/ScorePopupTests.cs" -Destination "$outputPath/Assets/Tests"
    Copy-Item -LiteralPath "$sourceRoot/Assets/MemberFolder/CJS/CJS_Prefebs/UI_ScoreEffect.prefab" -Destination "$outputPath/Assets/ScorePopup.prefab"
    '-define:SCORE_POPUPS' | Set-Content -LiteralPath "$outputPath/Assets/csc.rsp" -Encoding utf8
}
Copy-Item -LiteralPath "$sourceRoot/Assets/TextMesh Pro" -Destination "$outputPath/Assets/TextMesh Pro" -Recurse
Copy-Item -LiteralPath "$sourceRoot/Assets/Public/Font" -Destination "$outputPath/Assets/Fonts" -Recurse
$null = New-Item -ItemType Directory -Force -Path "$outputPath/Assets/Resources"
Copy-Item "$sourceRoot/Assets/Resources/LocalizationData" "$outputPath/Assets/Resources/LocalizationData" -Recurse
Copy-Item "$sourceRoot/Assets/Public/UI_Public/Scripts/PSH_Script_Local*.cs" "$outputPath/Assets/Runtime"
Copy-Item "$sourceRoot/Tests/Localization/LocaleTestContext.cs" "$outputPath/Assets/Tests"
@{ name = 'BallARetro.RunUnderTest'; references = @('UnityEngine.UI', 'Unity.TextMeshPro', 'Unity.Localization', 'Unity.ResourceManager') } | ConvertTo-Json | Set-Content -LiteralPath "$outputPath/Assets/Runtime/RunUnderTest.asmdef" -Encoding utf8
@{ name = 'BallARetro.RunProgressionTests'; references = @('BallARetro.RunUnderTest', 'UnityEngine.UI', 'Unity.TextMeshPro', 'Unity.Localization', 'Unity.ResourceManager'); optionalUnityReferences = @('TestAssemblies'); autoReferenced = $false } | ConvertTo-Json | Set-Content -LiteralPath "$outputPath/Assets/Tests/RunProgressionTests.asmdef" -Encoding utf8
if ($Baseline) { '-define:BASELINE' | Set-Content -LiteralPath "$outputPath/Assets/csc.rsp" -Encoding utf8 }
$testClasses = @('RunProgressionTests')
if ($IncludeBallRescue) { $testClasses += 'BallAutoRescueTests' }
if ($IncludeScorePopups) { $testClasses += 'ScorePopupTests' }
if ($IncludeTutorial) { $testClasses += 'TutorialProgressionTests' }
$testFilter = $testClasses -join ';'
$arguments = @('-batchmode', '-screen-width', '1920', '-screen-height', '1080', '-projectPath', "`"$outputPath`"", '-runTests', '-testPlatform', 'PlayMode', '-testFilter', $testFilter, '-testResults', "`"$outputPath/results.xml`"", '-logFile', "`"$outputPath/unity.log`"")
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Test project: $outputPath; Unity process: $($process.Id)"
$process.WaitForExit()
if (!(Test-Path -LiteralPath "$outputPath/results.xml")) { throw "No test results. See $outputPath/unity.log" }
[xml]$results = Get-Content -LiteralPath "$outputPath/results.xml"
$results.'test-run' | Select-Object result, total, passed, failed, skipped
if ($process.ExitCode -ne 0 -or $results.'test-run'.result -ne 'Passed') { throw "Tests failed. See $outputPath/results.xml and unity.log" }
