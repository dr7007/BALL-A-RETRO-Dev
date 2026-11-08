param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [ValidateSet('STOVE', 'Steam', 'SteamDemo')][string]$Platform = 'STOVE'
)
$ErrorActionPreference = 'Stop'
$source = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path.TrimEnd('\', '/')
$snapshot = (Resolve-Path -LiteralPath $ProjectRoot).Path.TrimEnd('\', '/')
if ($snapshot -eq $source -or ($snapshot + '\').StartsWith($source + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Resource exclusions are allowed only in an isolated copy outside the source project.'
}
if (!(Test-Path -LiteralPath "$snapshot/ProjectSettings/ProjectVersion.txt")) { throw 'Not a Unity snapshot.' }
if ($Platform -eq 'SteamDemo') {
    $steamSettings = Get-Content -LiteralPath "$snapshot/Assets/Resources/Release/SteamSettings.asset" -Raw
    $fullIds = [regex]::Matches($steamSettings, '(?m)^\s*steamAppId:\s*(\d+)\s*$')
    $demoIds = [regex]::Matches($steamSettings, '(?m)^\s*demoAppId:\s*(\d+)\s*$')
    if ($fullIds.Count -ne 1 -or $demoIds.Count -ne 1) { throw 'Expected one full-game and one demo App ID.' }
    $fullGameId = [uint32]::Parse($fullIds[0].Groups[1].Value)
    $demoGameId = [uint32]::Parse($demoIds[0].Groups[1].Value)
    if ($fullGameId -in @(0, 480) -or $demoGameId -in @(0, 480) -or $demoGameId -eq $fullGameId) {
        throw 'Steam demo requires a real, separate App ID linked to the full game.'
    }
    # A distinct product name isolates both Windows PlayerPrefs and persistentDataPath.
    # Only this ordinary snapshot copy is changed; the user's project stays untouched.
    $playerSettingsPath = Join-Path $snapshot 'ProjectSettings/ProjectSettings.asset'
    $playerSettingsText = Get-Content -LiteralPath $playerSettingsPath -Raw
    if ([regex]::Matches($playerSettingsText, '(?m)^  productName: [^\r\n]*').Count -ne 1) { throw 'Could not identify one product name.' }
    $playerSettingsText = [regex]::Replace($playerSettingsText, '(?m)^  productName: [^\r\n]*', '  productName: Ball-A-Retro Demo')
    [IO.File]::WriteAllText($playerSettingsPath, $playerSettingsText, [Text.UTF8Encoding]::new($false))
}
# Explicit, reviewed exclusions only. Never remove a whole vendor or Resources directory.
# BillingMode belongs to a nested sample project's Android IAP setup. This game has no IAP package.
# The Steam settings asset is read only by the Steam path, which is compiled out in STOVE.
$exclusions = @(
    'Assets/IgnorePackages/Pinball Creator v2.0.1/Assets/Resources/BillingMode.json'
)
if ($Platform -eq 'STOVE') { $exclusions += 'Assets/Resources/Release/SteamSettings.asset' }
if ($Platform -ne 'SteamDemo') { $exclusions += 'Assets/Resources/Demo' }
$evidence = Join-Path (Split-Path $snapshot -Parent) 'evidence'
$null = New-Item -ItemType Directory -Path $evidence -Force
$records = @()
foreach ($relative in $exclusions) {
    foreach ($suffix in @('', '.meta')) {
        $from = [IO.Path]::GetFullPath((Join-Path $snapshot ($relative + $suffix)))
        $to = [IO.Path]::GetFullPath((Join-Path $snapshot ("ExcludedFrom$Platform/" + $relative + $suffix)))
        if (!$from.StartsWith($snapshot + '\', [StringComparison]::OrdinalIgnoreCase) -or
            !$to.StartsWith($snapshot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe exclusion path.' }
        if (!(Test-Path -LiteralPath $from)) { continue }
        if (Test-Path -LiteralPath $from -PathType Container) {
            $records += @(Get-ChildItem -LiteralPath $from -Recurse -File | ForEach-Object {
                [pscustomobject]@{ path = [IO.Path]::GetRelativePath($snapshot, $_.FullName).Replace('\', '/'); sha256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
            })
        } else {
            $records += [pscustomobject]@{ path = $relative + $suffix; sha256 = (Get-FileHash -LiteralPath $from).Hash }
        }
        $null = New-Item -ItemType Directory -Path (Split-Path $to -Parent) -Force
        Move-Item -LiteralPath $from -Destination $to
    }
}
$records | ConvertTo-Json | Set-Content -LiteralPath "$evidence/excluded-resources.json" -Encoding utf8
Write-Output "Preserved $($records.Count) excluded resource files outside Assets in the snapshot."
