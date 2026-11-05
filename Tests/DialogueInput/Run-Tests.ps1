param(
    [Parameter(Mandatory = $true)][string]$OutputRoot,
    [switch]$Baseline,
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
@('com.unity.ugui', 'com.unity.localization', 'com.unity.test-framework') | ForEach-Object { $pending.Enqueue($_) }
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

$scriptPath = 'Assets/Public/Main_Game/UI_Game/Scripts'
if ($Baseline) {
    $oldSource = & git -C $sourceRoot show "HEAD:$scriptPath/PSH_Script_DialogueUI.cs"
    if ($LASTEXITCODE -ne 0) { throw 'Could not read baseline source from HEAD.' }
    $oldSource | Set-Content -LiteralPath "$outputPath/Assets/Runtime/PSH_Script_DialogueUI.cs" -Encoding utf8
} else {
    Get-ChildItem -LiteralPath "$sourceRoot/$scriptPath" -Filter 'PSH_Script_Dialogue*.cs' | Copy-Item -Destination "$outputPath/Assets/Runtime"
}
Copy-Item -LiteralPath "$PSScriptRoot/DialogueInputTests.cs" -Destination "$outputPath/Assets/Tests"
@{ name = 'BallARetro.DialogueUnderTest'; references = @('UnityEngine.UI', 'Unity.TextMeshPro', 'Unity.Localization') } | ConvertTo-Json | Set-Content -LiteralPath "$outputPath/Assets/Runtime/DialogueUnderTest.asmdef" -Encoding utf8
@{ name = 'BallARetro.DialogueInputTests'; references = @('BallARetro.DialogueUnderTest', 'UnityEngine.UI', 'Unity.TextMeshPro', 'Unity.Localization'); optionalUnityReferences = @('TestAssemblies'); autoReferenced = $false } | ConvertTo-Json | Set-Content -LiteralPath "$outputPath/Assets/Tests/DialogueInputTests.asmdef" -Encoding utf8

$arguments = @('-batchmode', '-nographics', '-projectPath', "`"$outputPath`"", '-runTests', '-testPlatform', 'PlayMode', '-testFilter', 'DialogueInputTests', '-testResults', "`"$outputPath/results.xml`"", '-logFile', "`"$outputPath/unity.log`"")
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Test project: $outputPath; Unity process: $($process.Id)"
$process.WaitForExit()
if (!(Test-Path -LiteralPath "$outputPath/results.xml")) { throw "No test results. See $outputPath/unity.log" }
[xml]$results = Get-Content -LiteralPath "$outputPath/results.xml"
$results.'test-run' | Select-Object result, total, passed, failed, skipped
if ($process.ExitCode -ne 0 -or $results.'test-run'.result -ne 'Passed') { throw "Tests failed. See $outputPath/results.xml and unity.log" }
