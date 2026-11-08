param([Parameter(Mandatory=$true)][string]$ReportPath)
$ErrorActionPreference = 'Stop'
$report = Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json
$expectedScenes = @('Assets/MainScenes/Main_Scene_Lobby.unity', 'Assets/MainScenes/Main_Scene_Tutorial.unity', 'Assets/MainScenes/Main_Scene_Game.unity')
if (($report.scenes -join '|') -ne ($expectedScenes -join '|')) { throw 'Unexpected release scene list.' }
if (!$report.assets -or $report.assets.Count -lt 100) { throw 'Missing or incomplete packed asset report.' }
$forbidden = $report.assets | Where-Object {
    $_.path -match '/(BillingMode\.json|SteamSettings\.asset|PerformanceTestRunInfo\.json|PerformanceTestRunSettings\.json)$' -or
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
