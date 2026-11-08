param(
    [Parameter(Mandatory = $true)][string]$PlayerRoot,
    [Parameter(Mandatory = $true)][string]$EvidenceRoot,
    [uint32]$AppId = 0,
    [ValidateSet('Steam', 'SteamDemo')][string]$Platform = 'Steam'
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $PlayerRoot).Path.TrimEnd('\', '/')
$evidence = [IO.Path]::GetFullPath($EvidenceRoot).TrimEnd('\', '/')
if (Test-Path -LiteralPath $evidence) { throw 'Use a new evidence directory; smoke evidence is never overwritten.' }
if (($evidence + '\').StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase) -or
    ($root + '\').StartsWith($evidence + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Evidence must be outside the source player, with neither path containing the other.'
}
if (!(Test-Path -LiteralPath "$root/BALL-A-RETRO.exe" -PathType Leaf)) { throw 'Player executable is missing.' }

function Assert-NoLinkedAncestor([string]$Path) {
    $current = $Path
    while ($current) {
        if ((Test-Path -LiteralPath $current) -and
            ((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw 'Player and evidence paths must not traverse links or junctions.'
        }
        $parent = [IO.Directory]::GetParent($current)
        $current = if ($null -ne $parent) { $parent.FullName } else { $null }
    }
}
function Get-SteamClients {
    @(Get-Process -Name steam, steamwebhelper -ErrorAction SilentlyContinue |
        Select-Object Id, ProcessName)
}
function Assert-SteamStopped {
    if (@(Get-SteamClients).Count) { throw 'Steam is running. This offline-only check will not stop user processes.' }
}
function Get-PlayerManifest([string]$Directory) {
    $items = @(Get-ChildItem -LiteralPath $Directory -Recurse -Force)
    if ($items | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }) {
        throw 'Player contents must not contain links or junctions.'
    }
    @($items | Where-Object { !$_.PSIsContainer } | Sort-Object FullName | ForEach-Object {
        [pscustomobject]@{ path = [IO.Path]::GetRelativePath($Directory, $_.FullName); bytes = $_.Length;
            sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
    })
}
function Test-ManifestsEqual($Expected, $Actual) {
    $left = @($Expected | ForEach-Object { "$($_.path)|$($_.bytes)|$($_.sha256)" })
    $right = @($Actual | ForEach-Object { "$($_.path)|$($_.bytes)|$($_.sha256)" })
    $left.Count -eq $right.Count -and !(Compare-Object $left $right)
}

# The original depot remains untouched. The development App ID file exists only
# in an ordinary file copy, never in the release player or its upload folder.
$settingsPath = Join-Path $PSScriptRoot '../../Assets/Resources/Release/SteamSettings.asset'
$idField = if ($Platform -eq 'SteamDemo') { 'demoAppId' } else { 'steamAppId' }
$settingsText = Get-Content -LiteralPath $settingsPath -Raw
$idMatches = [regex]::Matches($settingsText, "(?m)^\s*${idField}:\s*(\d+)\s*$")
if ($idMatches.Count -ne 1) { throw 'Could not read one configured Steam App ID.' }
$configuredId = [uint32]::Parse($idMatches[0].Groups[1].Value)
if ($configuredId -in @(0, 480)) { throw 'A real configured App ID is required.' }
if ($Platform -eq 'SteamDemo') {
    $fullMatches = [regex]::Matches($settingsText, '(?m)^\s*steamAppId:\s*(\d+)\s*$')
    if ($fullMatches.Count -ne 1 -or [uint32]::Parse($fullMatches[0].Groups[1].Value) -in @(0, 480, $configuredId)) {
        throw 'The demo App ID must be separate from the real full-game App ID.'
    }
}
if ($PSBoundParameters.ContainsKey('AppId') -and $AppId -ne $configuredId) {
    throw 'Explicit AppId must match the source SteamSettings asset.'
}
$AppId = $configuredId
Assert-NoLinkedAncestor $root
Assert-NoLinkedAncestor $evidence
Assert-SteamStopped
$null = New-Item -ItemType Directory -Path $evidence
$copy = Join-Path $evidence 'player-copy'
$log = Join-Path $evidence 'player.log'
$process = $null
$before = @()
$after = @()
$sourceUnchanged = $false
$copyVerified = $false
$alive = $false
$failureReason = $null
$modules = @{}
$unexpectedClients = @()
$elapsedSeconds = 0
$ownProcessId = $null
$termination = 'No player process started.'

try {
    & "$PSScriptRoot/Verify-Player.ps1" -PlayerRoot $root -EvidencePath "$evidence/player-audit.json" -Platform $Platform
    $before = @(Get-PlayerManifest $root)
    $before | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath "$evidence/source-files-before.json" -Encoding utf8
    $null = New-Item -ItemType Directory -Path $copy
    foreach ($entry in $before) {
        $target = Join-Path $copy $entry.path
        $null = New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force
        Copy-Item -LiteralPath (Join-Path $root $entry.path) -Destination $target
    }
    $copied = @(Get-PlayerManifest $copy)
    $copied | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath "$evidence/copy-files-before-appid.json" -Encoding utf8
    $copyVerified = Test-ManifestsEqual $before $copied
    if (!$copyVerified) { throw 'Disposable player copy does not match source hashes.' }
    [IO.File]::WriteAllText((Join-Path $copy 'steam_appid.txt'), "$AppId`n", [Text.Encoding]::ASCII)
    if ((Get-Content -LiteralPath "$copy/steam_appid.txt" -Raw).Trim() -ne "$AppId") { throw 'Development App ID file verification failed.' }

    # Recheck immediately before launch. Do not invoke the depot executable:
    # without the development file RestartAppIfNecessary can launch Steam.
    Assert-SteamStopped
    $process = Start-Process -FilePath "$copy/BALL-A-RETRO.exe" -WorkingDirectory $copy -WindowStyle Hidden -PassThru -ArgumentList @(
        '-batchmode', '-force-d3d11', '-screen-fullscreen', '0', '-screen-width', '1920', '-screen-height', '1080', '-logFile', "`"$log`"")
    $ownProcessId = $process.Id
    Write-Output "Offline Steam smoke process: $ownProcessId (disposable copy only)"
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while ($timer.Elapsed.TotalSeconds -lt 30 -and !$process.HasExited) {
        $unexpectedClients = @(Get-SteamClients)
        if ($unexpectedClients.Count) { throw 'Steam appeared during the offline observation; user processes were not stopped.' }
        try {
            $process.Refresh()
            foreach ($module in @($process.Modules | Where-Object ModuleName -Match '^steam_api')) {
                $modules[$module.FileName] = [pscustomobject]@{ name = $module.ModuleName; path = $module.FileName;
                    sha256 = (Get-FileHash -LiteralPath $module.FileName -Algorithm SHA256).Hash }
            }
        } catch {
            # Module enumeration can race with startup/exit. Missing native
            # evidence still fails the result; a transient read cannot pass it.
        }
        $null = $process.WaitForExit(500)
    }
    $timer.Stop()
    $elapsedSeconds = [Math]::Round($timer.Elapsed.TotalSeconds, 3)
    $alive = !$process.HasExited -and $elapsedSeconds -ge 30
    $unexpectedClients = @(Get-SteamClients)
    if ($unexpectedClients.Count) { throw 'Steam appeared during the offline observation; user processes were not stopped.' }
} catch {
    $failureReason = $_.Exception.Message
} finally {
    if ($null -ne $process) {
        # Only the Process object returned by our own Start-Process is ended.
        # Never enumerate/stop other games, the user's PID, or Steam processes.
        try {
            if (!$process.HasExited) {
                $process.Kill()
                if (!$process.WaitForExit(5000)) { throw 'Own smoke player did not exit after termination.' }
                $termination = 'Harness ended only its own disposable-copy player process.'
            } else { $termination = 'Disposable-copy player exited before harness termination.' }
        } catch {
            $failureReason = "Own process cleanup failed: $($_.Exception.Message)"
            $termination = 'Own process cleanup reported a failure; no other process was touched.'
        } finally { $process.Dispose() }
    }
    if ($before.Count) {
        try {
            $after = @(Get-PlayerManifest $root)
            $after | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath "$evidence/source-files-after.json" -Encoding utf8
            $sourceUnchanged = Test-ManifestsEqual $before $after
        } catch { $failureReason = "Source verification failed: $($_.Exception.Message)" }
    }
}

$text = if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log -Raw } else { '' }
$expectedSteamFailure = $text.Contains('[Steamworks.NET] SteamAPI_Init() failed. Refer to Valve')
$errors = @($text -split "`n" | Where-Object {
    $_ -match '\b[A-Za-z_][A-Za-z0-9_.]*Exception\b|Shader error|State comes from an incompatible keyword space|Crash!!!|Aborting batchmode|Coroutine couldn.t be started|Packsize Test returned false|DllCheck Test returned false|RestartAppIfNecessary returned true|실행된 App ID가|실제 게임 App ID를 SteamSettings'
})
$lobby = $text -match 'Main_Scene_Lobby'
$graphics = $text -match 'Direct3D:|Renderer:'
$noticeReady = $text -match '\[Startup\] Photosensitivity notice ready: (ko|en|ja)'
$observedModules = @($modules.Values)
$nativeSourceHash = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot '../../Assets/com.rlabrecque.steamworks.net/Plugins/steam_api64.dll')).Hash
$nativeValid = $observedModules.Count -eq 1 -and $observedModules[0].name -ieq 'steam_api64.dll' -and
    $observedModules[0].path.StartsWith($copy + '\', [StringComparison]::OrdinalIgnoreCase) -and
    $observedModules[0].sha256 -eq $nativeSourceHash
$success = !$failureReason -and $copyVerified -and $sourceUnchanged -and $alive -and $lobby -and $graphics -and
    $noticeReady -and $nativeValid -and $expectedSteamFailure -and !$errors.Count -and !$unexpectedClients.Count
@{
    success = $success; scope = 'Local offline startup only; Steam client intentionally absent.';
    platform = $Platform; appId = $AppId; sourcePlayer = $root; disposableCopy = $copy; sourcePlayerUnchanged = $sourceUnchanged;
    copyHashesVerifiedBeforeDevelopmentAppId = $copyVerified; developmentAppIdFile = "$copy/steam_appid.txt";
    processId = $ownProcessId; observedSeconds = $elapsedSeconds; aliveAfter30Seconds = $alive;
    lobbyLoaded = $lobby; graphicsInitialized = $graphics; localizedStartupNoticeReady = $noticeReady;
    expectedSteamInitializationFailure = $expectedSteamFailure; nativeModuleVerified = $nativeValid;
    nativeModules = $observedModules; unexpectedSteamProcesses = $unexpectedClients; errors = $errors;
    failureReason = $failureReason; termination = $termination;
    authenticatedSteam = 'untested'; clientInstallAndLaunch = 'untested'; leaderboard = 'untested'; overlay = 'untested';
    demoConfiguration = $(if ($Platform -eq 'SteamDemo') { 'Demo compile flag and separate configured ID checked; authenticated demo launch remains untested.' } else { 'Full-game edition.' });
    userPreferences = $(if ($Platform -eq 'SteamDemo') { 'No settings interaction; demo product name isolates Windows preferences from the full game.' } else { 'No settings interaction; normal Windows user preferences are shared.' })
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath "$evidence/result.json" -Encoding utf8
if (!$success) { throw "Offline startup check failed; see $evidence/result.json and player.log" }
Write-Output 'Offline startup passed: 30 seconds, lobby, Direct3D, localized notice and matching Steam native module. Steam authentication, client launch, leaderboards and overlay remain untested.'
