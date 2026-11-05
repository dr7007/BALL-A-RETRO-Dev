param(
    [Parameter(Mandatory = $true)][string]$OutputRoot,
    [string]$PythonPath = 'python',
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$outputPath = [IO.Path]::GetFullPath($OutputRoot)
if ((Test-Path -LiteralPath "$outputPath/Assets") -or $outputPath.StartsWith($sourceRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Use a fresh directory outside the source project.' }
$null = New-Item -ItemType Directory -Force -Path "$outputPath/Assets/Runtime", "$outputPath/Assets/Tests", "$outputPath/Packages", "$outputPath/ProjectSettings"
$lock = Get-Content -LiteralPath "$sourceRoot/Packages/packages-lock.json" -Raw | ConvertFrom-Json
$dependencies = [ordered]@{}
$pending = [Collections.Generic.Queue[string]]::new()
@('com.unity.ugui', 'com.unity.render-pipelines.universal', 'com.unity.test-framework', 'com.unity.modules.audio', 'com.unity.modules.uielements', 'com.unity.modules.screencapture', 'com.unity.modules.physics', 'com.unity.modules.particlesystem') | ForEach-Object { $pending.Enqueue($_) }
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
@{ name = 'BallARetro.UIRuntime'; references = @('UnityEngine.UI', 'Unity.TextMeshPro', 'Unity.RenderPipelines.Universal.Runtime', 'Unity.RenderPipelines.Core.Runtime') } | ConvertTo-Json | Set-Content -LiteralPath "$outputPath/Assets/Runtime/UIRuntime.asmdef" -Encoding utf8
@{ name = 'BallARetro.UITests'; references = @('BallARetro.UIRuntime', 'UnityEngine.UI', 'Unity.TextMeshPro', 'Unity.RenderPipelines.Universal.Runtime', 'Unity.RenderPipelines.Core.Runtime'); optionalUnityReferences = @('TestAssemblies'); autoReferenced = $false } | ConvertTo-Json | Set-Content -LiteralPath "$outputPath/Assets/Tests/UITests.asmdef" -Encoding utf8

& $PythonPath "$PSScriptRoot/Extract-UIFixture.py" "$sourceRoot/Assets/MainScenes/Main_Scene_Tutorial.unity" "$outputPath/Assets/TutorialUI.unity"
if ($LASTEXITCODE -ne 0) { throw 'Fixture extraction failed.' }
Copy-Item -LiteralPath "$PSScriptRoot/TutorialCanvasTests.cs" -Destination "$outputPath/Assets/Tests"
$arguments = @('-batchmode', '-screen-width', '1920', '-screen-height', '1080', '-projectPath', "`"$outputPath`"", '-runTests', '-testPlatform', 'PlayMode', '-testFilter', 'TutorialCanvasTests', '-testResults', "`"$outputPath/results.xml`"", '-logFile', "`"$outputPath/unity.log`"")
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Test project: $outputPath; Unity process: $($process.Id)"
$process.WaitForExit()
if (!(Test-Path -LiteralPath "$outputPath/results.xml")) { throw 'Unity produced no test results. See unity.log.' }
[xml]$result = Get-Content -LiteralPath "$outputPath/results.xml"
$result.'test-run' | Select-Object result, total, passed, failed
if ($result.'test-run'.result -ne 'Passed') { throw 'Tutorial canvas checks failed.' }
