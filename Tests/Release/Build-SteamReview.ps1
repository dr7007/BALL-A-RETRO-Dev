param(
    [Parameter(Mandatory = $true)][string]$OutputRoot,
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe',
    [string]$CacheProjectRoot
)
$ErrorActionPreference = 'Stop'
& "$PSScriptRoot/Build-StoveReview.ps1" -OutputRoot $OutputRoot -UnityPath $UnityPath -CacheProjectRoot $CacheProjectRoot -Platform Steam
