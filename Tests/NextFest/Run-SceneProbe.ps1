param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$EvidenceRoot,
    [switch]$Baseline,
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$source = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path.TrimEnd('\', '/')
$project = (Resolve-Path -LiteralPath $ProjectRoot).Path.TrimEnd('\', '/')
$evidence = [IO.Path]::GetFullPath($EvidenceRoot).TrimEnd('\', '/')
if (($project + '\').StartsWith($source + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Use a disposable full-project snapshot outside the source repository, after its player build.'
}
if (($evidence + '\').StartsWith($project + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Keep evidence and original-file backups outside the Unity snapshot.'
}
if (Test-Path -LiteralPath $evidence) { throw 'Use a fresh evidence directory.' }
$steam = Join-Path $project 'Assets/Scripts/Steamworks.NET/SteamManager.cs'
$settings = Join-Path $project 'ProjectSettings/ProjectSettings.asset'
$injected = Join-Path $project 'Assets/FullGameSceneProbe.cs'
if (!(Test-Path -LiteralPath $steam) -or !(Test-Path -LiteralPath $settings)) { throw 'Not a full project snapshot.' }
if ((Test-Path -LiteralPath $injected) -or (Test-Path -LiteralPath ($injected + '.meta'))) { throw 'A scene probe is already present.' }
$openEditors = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object {
    $_.CommandLine -and $_.CommandLine.IndexOf($project, [StringComparison]::OrdinalIgnoreCase) -ge 0
}).Count
if ($openEditors -gt 0) {
    throw 'Close the snapshot editor before probing it.'
}
$null = New-Item -ItemType Directory -Path $evidence
$steamBackup = Join-Path $evidence 'SteamManager.original.cs.txt'
$settingsBackup = Join-Path $evidence 'ProjectSettings.original.asset.txt'
Copy-Item -LiteralPath $steam -Destination $steamBackup
Copy-Item -LiteralPath $settings -Destination $settingsBackup
$steamHash = (Get-FileHash -LiteralPath $steam).Hash
$settingsHash = (Get-FileHash -LiteralPath $settings).Hash
$process = $null
try {
    # Change the namespace before editor startup, so native PlayerPrefs cannot
    # cache either the full game's or the user's demo product name.
    $settingsText = [IO.File]::ReadAllText($settings)
    if ([regex]::Matches($settingsText, '(?m)^  productName: [^\r\n]*').Count -ne 1) {
        throw 'Expected one product name in the snapshot settings.'
    }
    $settingsText = [regex]::Replace($settingsText, '(?m)^  productName: [^\r\n]*', '  productName: Ball-A-Retro Full Scene Audit')
    $defineSection = [regex]::Match($settingsText, '(?ms)^  scriptingDefineSymbols:\r?\n.*?(?=^  [^\s])')
    if (!$defineSection.Success) { throw 'No scripting define section.' }
    $defines = [regex]::Matches($defineSection.Value, '(?m)^    Standalone: ([^\r\n]*)')
    if ($defines.Count -ne 1) { throw 'Expected one Standalone scripting define entry.' }
    $commonSymbols = @($defines[0].Groups[1].Value.Split(';') | Where-Object {
        $_ -and $_ -notin @('BALLARETRO_STOVE', 'BALLARETRO_STEAM', 'BALLARETRO_DEMO')
    })
    $fullSymbols = (@($commonSymbols) + @('BALLARETRO_STEAM')) -join ';'
    $fullDefineSection = [regex]::Replace($defineSection.Value, '(?m)^    Standalone: [^\r\n]*', '    Standalone: ' + $fullSymbols)
    $settingsText = $settingsText.Substring(0, $defineSection.Index) + $fullDefineSection + $settingsText.Substring($defineSection.Index + $defineSection.Length)
    [IO.File]::WriteAllText($settings, $settingsText, [Text.UTF8Encoding]::new($false))
    # File-local isolation preserves the Steamworks types compiled into the actual
    # Steam ranking code. The SDK manager itself reports offline without starting it.
    [IO.File]::WriteAllText($steam, "#define DISABLESTEAMWORKS`r`n" + [IO.File]::ReadAllText($steam), [Text.UTF8Encoding]::new($false))
    Copy-Item -LiteralPath "$source/Tests/NextFest/FullGameSceneProbe.cs" -Destination $injected
    $arguments = @('-batchmode', '-screen-width', '1920', '-screen-height', '1080',
        '-projectPath', "`"$project`"", '-executeMethod', 'FullGameSceneAudit.Run',
        '-auditOutput', "`"$evidence`"", '-logFile', "`"$evidence/unity.log`"")
    if ($Baseline) { $arguments += '-auditBaseline' }
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    Write-Output "Scene probe Unity process: $($process.Id)"
    if (!$process.WaitForExit(900000)) {
        Stop-Process -Id $process.Id
        $process.WaitForExit()
        throw 'Scene probe exceeded its editor startup/runtime watchdog.'
    }
    $process.Refresh()
    if (!(Test-Path -LiteralPath "$evidence/full-scene-audit.json")) { throw 'Scene audit produced no report; inspect unity.log.' }
    $report = Get-Content -LiteralPath "$evidence/full-scene-audit.json" -Raw | ConvertFrom-Json
    if ($process.ExitCode -ne 0 -or !$report.success) { throw "Scene audit failed; inspect $evidence/full-scene-audit.json and unity.log." }
    Write-Output "Scene audit passed: $($report.checks.Count) checks. Evidence: $evidence"
}
finally {
    if ($process -and !$process.HasExited) {
        Stop-Process -Id $process.Id
        $process.WaitForExit()
    }
    Copy-Item -LiteralPath $steamBackup -Destination $steam -Force
    Copy-Item -LiteralPath $settingsBackup -Destination $settings -Force
    foreach ($path in @($injected, ($injected + '.meta'))) {
        $absolute = [IO.Path]::GetFullPath($path)
        if (!$absolute.StartsWith($project + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe probe cleanup path.' }
        if (Test-Path -LiteralPath $absolute) { Remove-Item -LiteralPath $absolute }
    }
    $restored = [ordered]@{
        steamManagerRestored = (Get-FileHash -LiteralPath $steam).Hash -eq $steamHash
        projectSettingsRestored = (Get-FileHash -LiteralPath $settings).Hash -eq $settingsHash
        probeRemoved = !(Test-Path -LiteralPath $injected)
    }
    $restored | ConvertTo-Json | Set-Content -LiteralPath "$evidence/restoration.json" -Encoding utf8
    if ($restored.Values -contains $false) { throw 'Snapshot restoration verification failed.' }
}
