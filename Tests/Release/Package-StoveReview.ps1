param(
    [Parameter(Mandatory = $true)][string]$PlayerRoot,
    [Parameter(Mandatory = $true)][string]$OutputZip,
    [ValidateSet('STOVE', 'Steam', 'SteamDemo')][string]$Platform = 'STOVE'
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $PlayerRoot).Path.TrimEnd('\', '/')
$zipPath = [IO.Path]::GetFullPath($OutputZip)
if (Test-Path -LiteralPath $zipPath) { throw 'Use a new ZIP path.' }
if ($zipPath.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Save the ZIP outside the player folder.'
}
if (!(Test-Path -LiteralPath "$root/BALL-A-RETRO.exe")) { throw 'Missing player executable.' }
& "$PSScriptRoot/Verify-Player.ps1" -PlayerRoot $root -EvidencePath "$zipPath.player-audit.json" -Platform $Platform
$files = @(Get-ChildItem -LiteralPath $root -File -Recurse | Where-Object {
    $relative = [IO.Path]::GetRelativePath($root, $_.FullName)
    $relative -notmatch '(^|[/\\])[^/\\]*_(BurstDebugInformation|BackUpThisFolderButDontShipIt)' -and
        $_.Extension -ne '.pdb'
})
$archive = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        $name = [IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/')
        $null = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $name, [IO.Compression.CompressionLevel]::Optimal)
    }
}
finally { $archive.Dispose() }
# Read the finished archive and compare each decompressed entry with the player file.
$archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
$manifest = @()
try {
    if ($archive.Entries.Count -ne $files.Count) { throw 'ZIP file count mismatch.' }
    foreach ($entry in $archive.Entries) {
        $stream = $entry.Open()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $hash = [Convert]::ToHexString($sha.ComputeHash($stream)) }
        finally { $sha.Dispose(); $stream.Dispose() }
        $original = Join-Path $root $entry.FullName
        if ($hash -ne (Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash) {
            throw "ZIP integrity mismatch: $($entry.FullName)"
        }
        $manifest += [pscustomobject]@{ path = $entry.FullName; bytes = $entry.Length; sha256 = $hash }
    }
}
finally { $archive.Dispose() }
$manifest | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath "$zipPath.files.json" -Encoding utf8
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
"$zipHash  $([IO.Path]::GetFileName($zipPath))" | Set-Content -LiteralPath "$zipPath.sha256" -Encoding utf8
Write-Output "Verified archive: $zipPath ($($files.Count) files)"
