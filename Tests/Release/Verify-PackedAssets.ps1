param([Parameter(Mandatory=$true)][string]$ReportPath)
$ErrorActionPreference = 'Stop'
$report = Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json
$expectedScenes = @('Assets/MainScenes/Main_Scene_Lobby.unity', 'Assets/MainScenes/Main_Scene_Tutorial.unity', 'Assets/MainScenes/Main_Scene_Game.unity')
if (($report.scenes -join '|') -ne ($expectedScenes -join '|')) { throw 'Unexpected release scene list.' }
if (!$report.assets -or $report.assets.Count -lt 100) { throw 'Missing or incomplete packed asset report.' }
$forbidden = $report.assets | Where-Object {
    $_.path -match '/(BillingMode\.json|SteamSettings\.asset|PerformanceTestRunInfo\.json|PerformanceTestRunSettings\.json)$' -or
    ($_.path -match '\.unity$' -and $_.path -notin $expectedScenes) -or
    $_.path -match '/ReleaseValidation/'
}
if ($forbidden) { throw "Unwanted release assets: $($forbidden.path -join ', ')" }
foreach ($table in @('DialogueTable','UITable')) {
    foreach ($locale in @('ko','en','ja')) {
        if ("Assets/Resources/LocalizationData/${table}_${locale}.asset" -notin $report.assets.path) {
            throw "Required localization missing: ${table}_${locale}"
        }
    }
}
Write-Output "Verified $($report.assets.Count) packed assets: 3 release scenes, 6 language tables, no excluded resources or test scenes."
