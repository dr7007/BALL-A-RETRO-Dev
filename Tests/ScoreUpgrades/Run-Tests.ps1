param(
    [Parameter(Mandatory = $true)][string]$OutputRoot,
    [switch]$Demo,
    [switch]$Baseline,
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$outputPath = [IO.Path]::GetFullPath($OutputRoot)
if ($outputPath.StartsWith($sourceRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Use an output directory outside the source project.' }
if (Test-Path -LiteralPath "$outputPath/Assets") { throw 'Use a fresh output directory for each run.' }
$null = New-Item -ItemType Directory -Force -Path "$outputPath/Assets/Runtime", "$outputPath/Assets/Tests", "$outputPath/Packages", "$outputPath/ProjectSettings"
$lock = Get-Content -LiteralPath "$sourceRoot/Packages/packages-lock.json" -Raw | ConvertFrom-Json
$dependencies = [ordered]@{}
$pending = [Collections.Generic.Queue[string]]::new()
@('com.unity.test-framework', 'com.unity.modules.physics', 'com.unity.modules.audio') | ForEach-Object { $pending.Enqueue($_) }
while ($pending.Count -gt 0) {
    $name = $pending.Dequeue()
    if ($dependencies.Contains($name)) { continue }
    $packageInfo = $lock.dependencies.$name
    if ($null -eq $packageInfo) { throw "Package missing from lock file: $name" }
    $cached = @(Get-ChildItem -LiteralPath "$sourceRoot/Library/PackageCache" -Directory -Filter "$name@*")
    if ($cached.Count -eq 1) { $dependencies[$name] = 'file:' + $cached[0].FullName.Replace('\', '/') }
    elseif ($packageInfo.source -eq 'builtin') { $dependencies[$name] = $packageInfo.version }
    else { throw "Expected one cached package for $name." }
    $packageInfo.dependencies.PSObject.Properties.Name | Where-Object { $_ } | ForEach-Object { $pending.Enqueue($_) }
}
@{ dependencies = $dependencies } | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath "$outputPath/Packages/manifest.json" -Encoding utf8
Copy-Item -LiteralPath "$sourceRoot/ProjectSettings/ProjectVersion.txt" -Destination "$outputPath/ProjectSettings"
$sources = @(
    'Assets/MemberFolder/KHS/KHS_Scripts/KHS_Script_ScoreManager.cs',
    'Assets/MemberFolder/KHS/KHS_Scripts/RogueLike/KHS_Script_RogueLikeManager.cs',
    'Assets/MemberFolder/YJ/YJ_Scripts/YJ_Script_BumperComboManager.cs',
    'Assets/MemberFolder/KHS/KHS_Scripts/KHS_Script_BumperEffect.cs',
    'Assets/Public/Roguelike/BallARetroDemoRun.cs',
    'Assets/Public/Roguelike/BallARetroRunBuild.cs',
    'Assets/Public/Platform/BallARetroPlatform.cs'
)
foreach ($source in $sources) {
    if ($Baseline) {
        $oldSource = & git -C $sourceRoot show "HEAD:$source"
        if ($LASTEXITCODE -ne 0) { throw "Cannot read baseline $source" }
        $oldSource | Set-Content -LiteralPath (Join-Path "$outputPath/Assets/Runtime" (Split-Path $source -Leaf)) -Encoding utf8
    } else { Copy-Item -LiteralPath "$sourceRoot/$source" -Destination "$outputPath/Assets/Runtime" }
}
Copy-Item -LiteralPath "$PSScriptRoot/ScoreUpgradeFixture.cs" -Destination "$outputPath/Assets/Runtime"
Copy-Item -LiteralPath "$PSScriptRoot/ScoreUpgradeTests.cs" -Destination "$outputPath/Assets/Tests"
@{ name = 'BallARetro.ScoreUpgradeRuntime' } | ConvertTo-Json | Set-Content -LiteralPath "$outputPath/Assets/Runtime/Runtime.asmdef" -Encoding utf8
@{ name = 'BallARetro.ScoreUpgradeTests'; references = @('BallARetro.ScoreUpgradeRuntime'); optionalUnityReferences = @('TestAssemblies'); autoReferenced = $false } | ConvertTo-Json | Set-Content -LiteralPath "$outputPath/Assets/Tests/Tests.asmdef" -Encoding utf8
if ($Demo) { '-define:BALLARETRO_STEAM,BALLARETRO_DEMO' | Set-Content -LiteralPath "$outputPath/Assets/csc.rsp" -Encoding utf8 }
$arguments = @('-batchmode', '-nographics', '-projectPath', "`"$outputPath`"", '-runTests', '-testPlatform', 'PlayMode', '-testFilter', 'ScoreUpgradeTests', '-testResults', "`"$outputPath/results.xml`"", '-logFile', "`"$outputPath/unity.log`"")
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Test project: $outputPath; Unity process: $($process.Id)"
$process.WaitForExit()
if (!(Test-Path -LiteralPath "$outputPath/results.xml")) { throw "No results. See $outputPath/unity.log" }
[xml]$results = Get-Content -LiteralPath "$outputPath/results.xml"
$results.'test-run' | Select-Object result, total, passed, failed, skipped
if ($process.ExitCode -ne 0 -or $results.'test-run'.result -ne 'Passed') { throw "Tests failed. See $outputPath/results.xml and unity.log" }
