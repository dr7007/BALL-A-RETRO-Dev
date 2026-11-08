param(
    [Parameter(Mandatory=$true)][string]$ReportPath,
    [ValidateSet('STOVE', 'Steam', 'SteamDemo')][string]$Platform = 'STOVE'
)
$ErrorActionPreference = 'Stop'
$report = Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json
if ($report.platform -and $report.platform -ne $Platform) { throw 'Packed report platform mismatch.' }
if ($Platform -eq 'SteamDemo') {
    if ($report.platform -ne 'SteamDemo' -or $report.productName -ne 'Ball-A-Retro Demo') { throw 'Demo identity or isolated save-data product name is missing.' }
    $settingsText = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../../Assets/Resources/Release/SteamSettings.asset') -Raw
    $fullIds = [regex]::Matches($settingsText, '(?m)^\s*steamAppId:\s*(\d+)\s*$')
    $demoIds = [regex]::Matches($settingsText, '(?m)^\s*demoAppId:\s*(\d+)\s*$')
    if ($fullIds.Count -ne 1 -or $demoIds.Count -ne 1) { throw 'Missing full/demo ID settings.' }
    $fullGameId = [uint32]::Parse($fullIds[0].Groups[1].Value)
    $demoGameId = [uint32]::Parse($demoIds[0].Groups[1].Value)
    if ($fullGameId -in @(0, 480) -or $demoGameId -in @(0, 480) -or $fullGameId -eq $demoGameId -or $report.appId -ne $demoGameId) {
        throw 'Packed demo report must use the separate configured demo App ID.'
    }
} elseif ($report.productName -eq 'Ball-A-Retro Demo') { throw 'Full-game build uses the demo save-data namespace.' }
$expectedScenes = @('Assets/MainScenes/Main_Scene_Lobby.unity', 'Assets/MainScenes/Main_Scene_Tutorial.unity', 'Assets/MainScenes/Main_Scene_Game.unity')
if (($report.scenes -join '|') -ne ($expectedScenes -join '|')) { throw 'Unexpected release scene list.' }
if (!$report.assets -or $report.assets.Count -lt 100) { throw 'Missing or incomplete packed asset report.' }
$forbidden = $report.assets | Where-Object {
    $_.path -match '/(BillingMode\.json|PerformanceTestRunInfo\.json|PerformanceTestRunSettings\.json)$' -or
    ($Platform -eq 'STOVE' -and $_.path -match '/SteamSettings\.asset$') -or
    ($Platform -ne 'SteamDemo' -and $_.path -like 'Assets/Resources/Demo/*') -or
    ($_.path -match '\.unity$' -and $_.path -notin $expectedScenes) -or
    $_.path -match '/ReleaseValidation/' -or
    $_.path -eq 'Assets/Public/Roguelike/testIcon.jpg' -or
    ($_.path -match '/(Hovl Studio|VisualX_Studio|Cool Visual Effects - Part 1)/' -and $_.path -notmatch '\.cs$')
}
foreach ($path in @('Assets/Public/OriginalVfx/PixelParticle.mat', 'Assets/Public/UI_Public/Shaders/BallARetroPixelParticle.shader',
    'Assets/Public/OriginalVfx/PixelTrail.mat', 'Assets/Public/UI_Public/Shaders/BallARetroPixelTrail.shader',
    'Assets/Resources/Release/StartupNotice.prefab')) {
    if ($path -notin $report.assets.path) { throw "Required release asset missing: $path" }
}
if ($forbidden) { throw "Unwanted release assets: $($forbidden.path -join ', ')" }
if ($Platform -ne 'STOVE' -and 'Assets/Resources/Release/SteamSettings.asset' -notin $report.assets.path) {
    throw 'Steam release settings are missing from the Steam player.'
}
if ($Platform -eq 'SteamDemo') {
    foreach ($demoIcon in @('Choice_Capacitor', 'Choice_LastBall')) {
        if ("Assets/Resources/Demo/$demoIcon.png" -notin $report.assets.path) { throw "Required demo choice icon missing: $demoIcon" }
    }
    if ('Assets/Resources/Demo/DemoChoiceIcons.spriteatlasv2' -notin $report.assets.path) { throw 'Demo choice icon atlas is missing from the player.' }
    $atlasEvidencePath = Join-Path (Split-Path -Parent $ReportPath) 'demo-atlas.json'
    if (!(Test-Path -LiteralPath $atlasEvidencePath)) { throw 'Missing build-time demo atlas validation.' }
    $atlasEvidence = Get-Content -LiteralPath $atlasEvidencePath -Raw | ConvertFrom-Json
    if (!$atlasEvidence.success -or $atlasEvidence.sprites -ne 2 -or $atlasEvidence.textures -ne 1 -or $atlasEvidence.format -ne 'BC7') {
        throw 'The two demo icons must share one validated BC7 atlas texture.'
    }
}
foreach ($name in @('Choice_0_BonusChipScore', 'Choice_1_ChipRebound', 'Choice_2_BonusMultiplier',
    'Choice_3_GoldenChip', 'Choice_4_TotalScore', 'Choice_5_SecretWeapon',
    'Choice_6_FlipperRecharge', 'Choice_7_UpperFloorPass', 'Choice_8_ExtraBalls')) {
    $path = "Assets/Public/Roguelike/GeneratedIcons/$name.png"
    if ($path -notin $report.assets.path) { throw "Required choice icon missing: $path" }
}
foreach ($table in @('DialogueTable','UITable')) {
    foreach ($locale in @('ko','en','ja')) {
        if ("Assets/Resources/LocalizationData/${table}_${locale}.asset" -notin $report.assets.path) {
            throw "Required localization missing: ${table}_${locale}"
        }
    }
}
Write-Output "Verified $($report.assets.Count) packed assets: 3 release scenes, 6 language tables, 9 generated choice icons, original VFX, no legacy icon sheet, replaced vendor visuals, excluded resources or test scenes."
