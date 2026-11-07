param(
    [Parameter(Mandatory = $true)][string]$OutputRoot,
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe',
    [string]$CacheProjectRoot
)
$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$releaseRoot = [IO.Path]::GetFullPath($OutputRoot).TrimEnd('\', '/')
if (($releaseRoot + '\').StartsWith($sourceRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Build in a fresh directory outside the development project.'
}
if (Test-Path -LiteralPath $releaseRoot) { throw 'Use a new output directory; existing builds are never overwritten.' }
if (!(Test-Path -LiteralPath $UnityPath)) { throw "Unity not found: $UnityPath" }
$null = New-Item -ItemType Directory -Path "$releaseRoot/project", "$releaseRoot/player", "$releaseRoot/evidence"
$initialStatus = & git -C $sourceRoot status --porcelain=v1
$sourceCommit = & git -C $sourceRoot rev-parse HEAD
$sourceBranch = & git -C $sourceRoot branch --show-current
$initialStatus | Set-Content -LiteralPath "$releaseRoot/evidence/source-status.txt" -Encoding utf8
@{ commit = $sourceCommit; branch = $sourceBranch; source = $sourceRoot; includesWorkingTreeChanges = [bool]$initialStatus } |
    ConvertTo-Json | Set-Content -LiteralPath "$releaseRoot/evidence/source.json" -Encoding utf8

# Copy, never junction/hardlink, so imports and generated font atlases stay isolated.
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings', 'Library')) {
    $copyRoot = $sourceRoot
    if ($folder -eq 'Library' -and $CacheProjectRoot) {
        $copyRoot = (Resolve-Path -LiteralPath $CacheProjectRoot).Path
        if ((Get-Content "$copyRoot/ProjectSettings/ProjectVersion.txt" -Raw) -ne
            (Get-Content "$sourceRoot/ProjectSettings/ProjectVersion.txt" -Raw)) { throw 'Cache Unity version mismatch.' }
    }
    $excludeDirectories = @()
    if ($folder -eq 'Assets') { $excludeDirectories = @('/XD', 'Library', 'Temp', 'Logs', '.git', 'obj', 'UserSettings') }
    & robocopy (Join-Path $copyRoot $folder) "$releaseRoot/project/$folder" /E /XJ /MT:16 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP @excludeDirectories
    if ($LASTEXITCODE -ge 8) { throw "Snapshot copy failed: $folder ($LASTEXITCODE)" }
}
& "$PSScriptRoot/Prepare-StoveSnapshot.ps1" -ProjectRoot "$releaseRoot/project"
Write-Output "Snapshot ready: $releaseRoot/project"
$arguments = @('-batchmode', '-quit', '-projectPath', "`"$releaseRoot/project`"",
    '-buildTarget', 'Win64', '-executeMethod', 'BallARetroReleaseBuild.BuildStoveBatch',
    '-ballARetroOutput', "`"$releaseRoot/player/BALL-A-RETRO.exe`"", '-logFile', "`"$releaseRoot/evidence/build.log`"")
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Build process: $($process.Id)"
$process.WaitForExit()
$process.Refresh()
if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath "$releaseRoot/player/BALL-A-RETRO.exe")) {
    throw "Build failed (exit $($process.ExitCode)); see $releaseRoot/evidence/build.log"
}
$forbidden = Get-ChildItem -LiteralPath "$releaseRoot/player" -Recurse -File |
    Where-Object { $_.Name -match '^(steam_appid\.txt|(?:lib)?steam_api.*)$' }
if ($forbidden) { throw 'Steam runtime or development App ID found in STOVE output.' }
& "$PSScriptRoot/Verify-Player.ps1" -PlayerRoot "$releaseRoot/player" -EvidencePath "$releaseRoot/evidence/player-audit.json"
& "$PSScriptRoot/Verify-PackedAssets.ps1" -ReportPath "$releaseRoot/evidence/packed-assets.json"
Get-ChildItem -LiteralPath "$releaseRoot/player" -Recurse -File | ForEach-Object {
    [pscustomobject]@{ path = [IO.Path]::GetRelativePath("$releaseRoot/player", $_.FullName); bytes = $_.Length;
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath "$releaseRoot/evidence/files.json" -Encoding utf8
Write-Output "Build ready for runtime verification: $releaseRoot/player/BALL-A-RETRO.exe"
