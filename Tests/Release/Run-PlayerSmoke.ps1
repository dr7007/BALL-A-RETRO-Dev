param(
    [Parameter(Mandatory = $true)][string]$PlayerRoot,
    [Parameter(Mandatory = $true)][string]$EvidenceRoot
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $PlayerRoot).Path
$evidence = [IO.Path]::GetFullPath($EvidenceRoot)
if (Test-Path -LiteralPath $evidence) { throw 'Use a new evidence directory.' }
$null = New-Item -ItemType Directory -Path $evidence
$log = Join-Path $evidence 'player.log'
$process = Start-Process -FilePath "$root/BALL-A-RETRO.exe" -WorkingDirectory $root -WindowStyle Hidden -PassThru -ArgumentList @(
    '-batchmode', '-force-d3d11', '-screen-fullscreen', '0', '-screen-width', '1920', '-screen-height', '1080', '-logFile', "`"$log`"")
Write-Output "Player smoke process: $($process.Id)"
$alive = $false
$steamModules = @()
try {
    $alive = !$process.WaitForExit(30000)
    if ($alive) {
        $process.Refresh()
        $steamModules = @($process.Modules | Where-Object ModuleName -match '^steam_api' | ForEach-Object ModuleName)
    }
}
finally {
    # This is a bounded startup check; the harness ends only the process it launched.
    # Normal Quit-button behavior is covered separately by UI tests and client QA.
    if (!$process.HasExited) { $process.Kill(); $process.WaitForExit() }
}
$text = Get-Content -LiteralPath $log -Raw
$errors = @($text -split "`n" | Where-Object {
    $_ -match 'NullReferenceException|MissingReferenceException|DllNotFoundException|EntryPointNotFoundException|TypeLoadException|Shader error|State comes from an incompatible keyword space|Crash!!!|Aborting batchmode|Coroutine couldn.t be started'
})
$lobby = $text -match 'Main_Scene_Lobby'
$graphics = $text -match 'Direct3D:|Renderer:'
$success = $alive -and $lobby -and $graphics -and !$errors.Count -and !$steamModules.Count
@{ success = $success; aliveAfter30Seconds = $alive; lobbyLoaded = $lobby; graphicsInitialized = $graphics;
    steamModules = $steamModules; errors = $errors; termination = 'Harness stopped its own process after the startup observation window.' } |
    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath "$evidence/result.json" -Encoding utf8
if (!$success) { throw "Player startup check failed; see $evidence/result.json and player.log" }
Write-Output 'Player stayed alive for 30 seconds, loaded the lobby and Direct3D, with no detected startup exceptions or Steam runtime module.'
