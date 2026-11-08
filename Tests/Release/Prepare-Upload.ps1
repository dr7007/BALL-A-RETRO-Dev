param(
    [Parameter(Mandatory=$true)][string]$ArchivePath,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [ValidateSet('STOVE', 'Steam')][string]$Platform = 'STOVE'
)
$ErrorActionPreference = 'Stop'
$archivePath = (Resolve-Path -LiteralPath $ArchivePath).Path
$target = [IO.Path]::GetFullPath($OutputDirectory).TrimEnd('\','/')
if (Test-Path -LiteralPath $target) { throw 'Use a new upload directory.' }
$expectedHash = ((Get-Content -LiteralPath "$archivePath.sha256" -Raw).Trim() -split '\s+')[0]
if ((Get-FileHash -LiteralPath $archivePath).Hash -ne $expectedHash) { throw 'Archive hash mismatch.' }
$manifest = @(Get-Content -LiteralPath "$archivePath.files.json" -Raw | ConvertFrom-Json)
$zip = [IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    if ($zip.Entries.Count -ne $manifest.Count) { throw 'Archive manifest count mismatch.' }
    foreach($entry in $zip.Entries) {
        $destination = [IO.Path]::GetFullPath((Join-Path $target $entry.FullName))
        if (!$destination.StartsWith($target + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe ZIP entry.' }
        if ($entry.FullName -match '(?i)(\.pdb$|BurstDebugInformation|BackUpThisFolderButDontShipIt|steam_appid|(^|/)evidence/)' -or
            ($Platform -eq 'STOVE' -and $entry.FullName -match '(?i)steam_api')) {
            throw "Non-distributable file in archive: $($entry.FullName)"
        }
    }
} finally { $zip.Dispose() }
[IO.Compression.ZipFile]::ExtractToDirectory($archivePath, $target)
foreach($entry in $manifest) {
    $file = Join-Path $target $entry.path
    if ((Get-Item -LiteralPath $file).Length -ne $entry.bytes -or
        (Get-FileHash -LiteralPath $file).Hash -ne $entry.sha256) { throw "Upload file mismatch: $($entry.path)" }
}
& "$PSScriptRoot/Verify-Player.ps1" -PlayerRoot $target -EvidencePath "$target.audit.json" -Platform $Platform
Write-Output "Upload this folder: $target ($($manifest.Count) verified files)"
