param([Parameter(Mandatory = $true)][string]$OutputRoot,
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$outputPath = [IO.Path]::GetFullPath($OutputRoot)
if (Test-Path -LiteralPath $outputPath) { throw 'Use a fresh output directory.' }
if (($outputPath + '\').StartsWith($sourceRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Use an isolated directory outside the source project.' }
$null = New-Item -ItemType Directory -Path "$outputPath/Assets/Runtime", "$outputPath/Assets/Tests", "$outputPath/Packages", "$outputPath/ProjectSettings"
$lock = Get-Content -LiteralPath "$sourceRoot/Packages/packages-lock.json" -Raw | ConvertFrom-Json
$dependencies = [ordered]@{}
$pending = [Collections.Generic.Queue[string]]::new()
@('com.unity.test-framework', 'com.unity.modules.audio', 'com.unity.modules.physics', 'com.unity.modules.uielements') | ForEach-Object { $pending.Enqueue($_) }
while ($pending.Count -gt 0) {
    $name = $pending.Dequeue()
    if ($dependencies.Contains($name)) { continue }
    $info = $lock.dependencies.$name
    if ($null -eq $info) { throw "Package missing: $name" }
    $cached = @(Get-ChildItem -LiteralPath "$sourceRoot/Library/PackageCache" -Directory -Filter "$name@*")
    if ($cached.Count -eq 1) { $dependencies[$name] = 'file:' + $cached[0].FullName.Replace('\', '/') }
    elseif ($info.source -eq 'builtin') { $dependencies[$name] = $info.version }
    else { throw "Missing package cache: $name" }
    $info.dependencies.PSObject.Properties.Name | Where-Object { $_ } | ForEach-Object { $pending.Enqueue($_) }
}
@{dependencies=$dependencies} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath "$outputPath/Packages/manifest.json" -Encoding utf8
Copy-Item -LiteralPath "$sourceRoot/ProjectSettings/ProjectVersion.txt" -Destination "$outputPath/ProjectSettings"
@('Assets/MemberFolder/CJS/CJS_Scripts/CJS_Script_LobbyBGM.cs',
  'Assets/MemberFolder/CJS/CJS_Scripts/CJS_Script_AudioDirector.cs',
  'Assets/MemberFolder/PSH/UI_Sourse/Scripts/PSH_Script_UI_SoundBridge.cs') | ForEach-Object {
    Copy-Item -LiteralPath "$sourceRoot/$_" -Destination "$outputPath/Assets/Runtime"
}
Copy-Item -LiteralPath "$PSScriptRoot/GameEventStubs.cs" -Destination "$outputPath/Assets/Runtime"
Copy-Item -LiteralPath "$PSScriptRoot/AudioSettingsTests.cs" -Destination "$outputPath/Assets/Tests"
@{name='BallARetro.AudioSettings'} | ConvertTo-Json | Set-Content -LiteralPath "$outputPath/Assets/Runtime/AudioSettings.asmdef" -Encoding utf8
@{name='BallARetro.AudioSettings.Tests';references=@('BallARetro.AudioSettings');optionalUnityReferences=@('TestAssemblies');autoReferenced=$false} | ConvertTo-Json | Set-Content -LiteralPath "$outputPath/Assets/Tests/AudioSettings.Tests.asmdef" -Encoding utf8
$arguments = @('-batchmode', '-nographics', '-projectPath', "`"$outputPath`"", '-runTests', '-testPlatform', 'PlayMode', '-testFilter', 'AudioSettingsTests', '-testResults', "`"$outputPath/results.xml`"", '-logFile', "`"$outputPath/unity.log`"")
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Audio tests: $outputPath; Unity process: $($process.Id)"
$process.WaitForExit(); $process.Refresh()
if (!(Test-Path -LiteralPath "$outputPath/results.xml")) { throw 'No test results; see unity.log.' }
[xml]$result = Get-Content -LiteralPath "$outputPath/results.xml"
$result.'test-run' | Select-Object result,total,passed,failed
if ($process.ExitCode -ne 0 -or $result.'test-run'.result -ne 'Passed') { throw 'Audio settings tests failed.' }
