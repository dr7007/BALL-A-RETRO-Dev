param([Parameter(Mandatory = $true)][string]$ProjectRoot)
$ErrorActionPreference = 'Stop'
$source = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path.TrimEnd('\', '/')
$snapshot = (Resolve-Path -LiteralPath $ProjectRoot).Path.TrimEnd('\', '/')
if ($snapshot -eq $source -or ($snapshot + '\').StartsWith($source + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Resource exclusions are allowed only in an isolated copy outside the source project.'
}
if (!(Test-Path -LiteralPath "$snapshot/ProjectSettings/ProjectVersion.txt")) { throw 'Not a Unity snapshot.' }
# Explicit, reviewed exclusions only. Never remove a whole vendor or Resources directory.
# BillingMode belongs to a nested sample project's Android IAP setup. This game has no IAP package.
# The Steam settings asset is read only by the Steam path, which is compiled out in STOVE.
$exclusions = @(
    'Assets/Resources/Release/SteamSettings.asset',
    'Assets/IgnorePackages/Pinball Creator v2.0.1/Assets/Resources/BillingMode.json'
)
$evidence = Join-Path (Split-Path $snapshot -Parent) 'evidence'
$null = New-Item -ItemType Directory -Path $evidence -Force
$records = @()
foreach ($relative in $exclusions) {
    foreach ($suffix in @('', '.meta')) {
        $from = [IO.Path]::GetFullPath((Join-Path $snapshot ($relative + $suffix)))
        $to = [IO.Path]::GetFullPath((Join-Path $snapshot ('ExcludedFromStove/' + $relative + $suffix)))
        if (!$from.StartsWith($snapshot + '\', [StringComparison]::OrdinalIgnoreCase) -or
            !$to.StartsWith($snapshot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe exclusion path.' }
        if (!(Test-Path -LiteralPath $from)) { continue }
        $records += [pscustomobject]@{ path = $relative + $suffix; sha256 = (Get-FileHash -LiteralPath $from).Hash }
        $null = New-Item -ItemType Directory -Path (Split-Path $to -Parent) -Force
        Move-Item -LiteralPath $from -Destination $to
    }
}
$records | ConvertTo-Json | Set-Content -LiteralPath "$evidence/excluded-resources.json" -Encoding utf8
Write-Output "Preserved $($records.Count) excluded resource files outside Assets in the snapshot."
