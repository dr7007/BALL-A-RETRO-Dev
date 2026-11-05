param([Parameter(Mandatory=$true)][string]$OutputRoot,[string]$UnityPath='C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe')
$ErrorActionPreference='Stop'
$sourceRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$outputPath=[IO.Path]::GetFullPath($OutputRoot)
if ($outputPath.StartsWith($sourceRoot,[StringComparison]::OrdinalIgnoreCase) -or (Test-Path "$outputPath/Assets")) { throw 'Use a fresh folder outside the game project.' }
$null=New-Item -ItemType Directory -Force -Path "$outputPath/Assets/Runtime","$outputPath/Assets/Tests","$outputPath/Assets/Resources","$outputPath/Packages","$outputPath/ProjectSettings"
$lock=Get-Content "$sourceRoot/Packages/packages-lock.json" -Raw | ConvertFrom-Json
$dependencies=[ordered]@{};$pending=[Collections.Generic.Queue[string]]::new()
@('com.unity.ugui','com.unity.localization','com.unity.test-framework','com.unity.modules.audio','com.unity.modules.uielements','com.unity.modules.screencapture','com.unity.modules.physics') | ForEach-Object {$pending.Enqueue($_)}
while($pending.Count -gt 0){
 $name=$pending.Dequeue();if($dependencies.Contains($name)){continue}
 $info=$lock.dependencies.$name
 $cached=@(Get-ChildItem "$sourceRoot/Library/PackageCache" -Directory -Filter "$name@*")
 if($cached.Count -eq 1){$dependencies[$name]='file:'+$cached[0].FullName.Replace('\','/')}
 elseif($info.source -eq 'builtin'){$dependencies[$name]=$info.version}else{throw "Missing package $name"}
 $info.dependencies.PSObject.Properties.Name | Where-Object {$_} | ForEach-Object {$pending.Enqueue($_)}
}
@{dependencies=$dependencies}|ConvertTo-Json -Depth 10|Set-Content "$outputPath/Packages/manifest.json" -Encoding utf8
Copy-Item "$sourceRoot/ProjectSettings/ProjectVersion.txt" "$outputPath/ProjectSettings"
Copy-Item "$sourceRoot/Assets/Public/Font" "$outputPath/Assets/Fonts" -Recurse
Copy-Item "$sourceRoot/Assets/TextMesh Pro" "$outputPath/Assets/TextMesh Pro" -Recurse
Copy-Item "$sourceRoot/Assets/Resources/LocalizationData" "$outputPath/Assets/Resources/LocalizationData" -Recurse
Copy-Item "$sourceRoot/Assets/Localization Settings.asset" "$outputPath/Assets/GameLocalizationSettings.asset"
Copy-Item "$sourceRoot/Assets/Public/UI_Public/Scripts/PSH_Script_Local*.cs" "$outputPath/Assets/Runtime"
Copy-Item "$sourceRoot/Assets/MemberFolder/PSH/UI_Sourse/Scripts/PSH_Script_LanguageManager.cs" "$outputPath/Assets/Runtime"
Copy-Item "$PSScriptRoot/LocaleTestContext.cs","$PSScriptRoot/LocalizationCoreTests.cs" "$outputPath/Assets/Tests"
$refs=@('UnityEngine.UI','Unity.TextMeshPro','Unity.Localization','Unity.ResourceManager')
@{name='BallARetro.LocalizationRuntime';references=$refs}|ConvertTo-Json|Set-Content "$outputPath/Assets/Runtime/Runtime.asmdef" -Encoding utf8
@{name='BallARetro.LocalizationTests';references=@('BallARetro.LocalizationRuntime')+$refs;optionalUnityReferences=@('TestAssemblies');autoReferenced=$false}|ConvertTo-Json|Set-Content "$outputPath/Assets/Tests/Tests.asmdef" -Encoding utf8
$arguments=@('-batchmode','-screen-width','1920','-screen-height','1080','-projectPath',"`"$outputPath`"",'-runTests','-testPlatform','PlayMode','-testFilter','LocalizationCoreTests','-testResults',"`"$outputPath/results.xml`"",'-logFile',"`"$outputPath/unity.log`"")
$process=Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Test project: $outputPath; Unity process: $($process.Id)"
$process.WaitForExit()
if(!(Test-Path "$outputPath/results.xml")){throw 'No results. See unity.log.'}
[xml]$results=Get-Content "$outputPath/results.xml"
$results.'test-run'|Select-Object result,total,passed,failed
if($results.'test-run'.result -ne 'Passed'){throw 'Localization checks failed.'}
