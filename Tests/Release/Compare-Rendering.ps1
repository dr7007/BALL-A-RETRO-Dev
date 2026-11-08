param(
    [Parameter(Mandatory = $true)][string]$BaselinePath,
    [Parameter(Mandatory = $true)][string]$OptimizedPath,
    [Parameter(Mandatory = $true)][string]$EvidencePath,
    [string]$BatchManifestPath = (Join-Path $PSScriptRoot 'StaticBoardBatchingManifest.json'),
    [string]$SourceVerificationPath,
    [string]$RepositoryPath = (Join-Path $PSScriptRoot '../..'),
    [bool]$ExpectOptimized = $true,
    [ValidateRange(1, 10000)][int]$MinimumFrames = 20,
    [ValidateRange(0.0000001, 0.01)][double]$BoundsTolerance = 0.0001
)
# Read-only comparison of two completed probes. Does not start Unity or modify inputs.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$failures = [Collections.Generic.List[string]]::new()
$warnings = [Collections.Generic.List[object]]::new()
$results = [Collections.Generic.List[object]]::new()
$baselineRoot = (Resolve-Path -LiteralPath $BaselinePath).Path
$optimizedRoot = (Resolve-Path -LiteralPath $OptimizedPath).Path
$manifest = Get-Content -LiteralPath $BatchManifestPath -Raw | ConvertFrom-Json
$allowedPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$allowedRoots = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$allowedPathCache = [Collections.Generic.Dictionary[string, bool]]::new([StringComparer]::Ordinal)
foreach ($entry in $manifest.entries) {
    [void]$allowedPaths.Add([string]$entry.path)
    [void]$allowedRoots.Add(([string]$entry.path).Split('/')[0])
}
if ($allowedPaths.Count -eq 0 -or $allowedPaths.Count -ne $manifest.count) {
    throw 'Batch manifest is empty or its declared count does not match its unique paths.'
}
$sourceProofValidated = $false
$sourceProofEvidence = $null
if (![string]::IsNullOrWhiteSpace($SourceVerificationPath)) {
    # A tighter renderer AABB can result from baking transformed vertices. Permit
    # it only with an independently verified, hash-bound flag-only source change.
    $proof = Get-Content -LiteralPath $SourceVerificationPath -Raw | ConvertFrom-Json
    $manifestHash = (Get-FileHash -LiteralPath $BatchManifestPath -Algorithm SHA256).Hash
    if ($proof.success -ne $true -or $proof.manifestSha256 -ne $manifestHash -or
        $proof.baselineCommit -ne $manifest.baselineCommit -or $proof.count -ne $manifest.count -or
        @($proof.prefabs).Count -ne @($manifest.prefabs).Count) { throw 'Source verification does not match the successful current manifest.' }
    $repoRoot = (Resolve-Path -LiteralPath $RepositoryPath).Path
    foreach ($prefab in $manifest.prefabs) {
        $verified = @($proof.prefabs | Where-Object { $_.path -ceq $prefab.path })
        if ($verified.Count -ne 1 -or $verified[0].flagsOnly -ne $true -or $verified[0].baselineMatches -ne $true -or
            $verified[0].beforeSha256 -ne $prefab.beforeSha256 -or $verified[0].afterSha256 -ne $prefab.afterSha256 -or
            $verified[0].restoredSha256 -ne $prefab.beforeSha256 -or $verified[0].restoredGitSha256 -ne $verified[0].baselineSha256) {
            throw "Incomplete flag-only proof for $($prefab.path)."
        }
        $actualHash = (Get-FileHash -LiteralPath (Join-Path $repoRoot $prefab.path) -Algorithm SHA256).Hash
        if ($actualHash -ne $prefab.afterSha256) { throw "Source prefab changed after verification: $($prefab.path)." }
    }
    $sourceProofValidated = $true
    $sourceProofEvidence = [ordered]@{ path = (Resolve-Path -LiteralPath $SourceVerificationPath).Path
        sha256 = (Get-FileHash -LiteralPath $SourceVerificationPath -Algorithm SHA256).Hash
        manifestSha256 = $manifestHash; currentPrefabHashesMatch = $true; proof = $proof }
}

function Read-Report([string]$Path) {
    if (!(Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Missing probe output: $Path" }
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}
function Has-Property($Object, [string]$Name) {
    return $null -ne $Object -and $null -ne $Object.PSObject.Properties[$Name]
}
function Get-Statistics($Frames, [string]$Name) {
    $values = @($Frames | ForEach-Object { [double]$_.PSObject.Properties[$Name].Value } | Sort-Object)
    $middle = [int][Math]::Floor($values.Count / 2)
    $median = $values[$middle]
    if ($values.Count % 2 -eq 0) { $median = ($values[$middle - 1] + $values[$middle]) / 2 }
    return [ordered]@{ min = $values[0]; median = $median; max = $values[-1] }
}
function Get-CanonicalJson($Value) {
    # Field order is irrelevant; array order remains significant (camera/effect order).
    if ($null -eq $Value) { return 'null' }
    if ($Value -is [pscustomobject]) {
        $parts = foreach ($property in @($Value.PSObject.Properties | Sort-Object Name)) {
            ($property.Name | ConvertTo-Json -Compress) + ':' + (Get-CanonicalJson $property.Value)
        }
        return '{' + ($parts -join ',') + '}'
    }
    if ($Value -is [Array]) {
        return '[' + ((@($Value | ForEach-Object { Get-CanonicalJson $_ })) -join ',') + ']'
    }
    return ConvertTo-Json -InputObject $Value -Compress -Depth 30
}
function Get-MaterialKey($Materials) {
    $unique = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($material in @($Materials)) { [void]$unique.Add([string]$material) }
    [string[]]$names = @($unique)
    [Array]::Sort($names, [StringComparer]::Ordinal)
    # Static combining can reorder/repeat material slots; assets and names must match.
    return $names -join "`n"
}
function Test-CameraEqual($Left, $Right) {
    if ($null -eq $Left -or $null -eq $Right) { return $null -eq $Left -and $null -eq $Right }
    $names = @(@($Left.PSObject.Properties.Name) + @($Right.PSObject.Properties.Name) | Sort-Object -Unique)
    foreach ($name in $names) {
        if (!(Has-Property $Left $name) -or !(Has-Property $Right $name)) { return $false }
        if ($name -in @('position', 'euler')) {
            foreach ($axis in @('x', 'y', 'z')) {
                if (!(Has-Property $Left.$name $axis) -or !(Has-Property $Right.$name $axis)) { return $false }
                $delta = [Math]::Abs([double]$Left.$name.$axis - [double]$Right.$name.$axis)
                if ($name -eq 'euler') { $delta = [Math]::Min($delta, [Math]::Abs(360 - $delta)) }
                if ([double]::IsNaN($delta) -or $delta -gt 0.0001) { return $false }
            }
        } elseif ($name -in @('fov', 'orthographicSize')) {
            $tolerance = if ($name -eq 'fov') { 0.01 } else { 0.0001 }
            $delta = [Math]::Abs([double]$Left.$name - [double]$Right.$name)
            if ([double]::IsNaN($delta) -or $delta -gt $tolerance) { return $false }
        } elseif ((Get-CanonicalJson $Left.$name) -cne (Get-CanonicalJson $Right.$name)) { return $false }
    }
    return $true
}
function Get-BoundsValues($Bounds) {
    $centerName = if (Has-Property $Bounds 'm_Center') { 'm_Center' } else { 'center' }
    $extentName = if (Has-Property $Bounds 'm_Extent') { 'm_Extent' } else { 'extents' }
    foreach ($name in @($centerName, $extentName)) {
        if (!(Has-Property $Bounds $name)) { throw 'Renderer bounds have no center/extents.' }
        foreach ($axis in @('x', 'y', 'z')) {
            $vector = $Bounds.PSObject.Properties[$name].Value
            if (!(Has-Property $vector $axis)) { throw "Renderer bounds have no $name.$axis." }
            $number = [double]$vector.PSObject.Properties[$axis].Value
            if ([double]::IsNaN($number) -or [double]::IsInfinity($number)) { throw 'Non-finite renderer bounds.' }
            $number
        }
    }
}
function Get-MaxBoundsDifference($Left, $Right) {
    $maximum = 0.0
    for ($i = 0; $i -lt 6; $i++) { $maximum = [Math]::Max($maximum, [Math]::Abs($Left[$i] - $Right[$i])) }
    return $maximum
}
function Test-BoundsContained($Outer, $Inner) {
    for ($i = 0; $i -lt 3; $i++) {
        if ($Outer[$i + 3] -lt 0 -or $Inner[$i + 3] -lt 0 -or
            [Math]::Abs($Outer[$i] - $Inner[$i]) + $Inner[$i + 3] - $Outer[$i + 3] -gt $BoundsTolerance) {
            return $false
        }
    }
    return $true
}
function Test-AllowedPath([string]$Path) {
    if ($allowedPathCache.ContainsKey($Path)) { return $allowedPathCache[$Path] }
    if ($allowedPaths.Contains($Path)) { return $true }
    # An enclosing scene root is allowed; the complete manifest hierarchy is retained.
    foreach ($root in $allowedRoots) {
        $index = $Path.IndexOf('/' + $root + '/', [StringComparison]::Ordinal)
        if ($index -ge 0 -and $allowedPaths.Contains($Path.Substring($index + 1))) {
            $allowedPathCache[$Path] = $true
            return $true
        }
    }
    $allowedPathCache[$Path] = $false
    return $false
}
function Get-MeshGroups($Inventory) {
    if (!(Has-Property $Inventory 'renderers')) { throw 'Renderer inventory has no renderers array.' }
    $groups = [Collections.Generic.Dictionary[string, Collections.Generic.List[object]]]::new([StringComparer]::Ordinal)
    foreach ($renderer in @($Inventory.renderers)) {
        if (!(Has-Property $renderer 'type')) { throw 'Renderer inventory entry has no type.' }
        if ($renderer.type -notin @('MeshRenderer', 'SkinnedMeshRenderer')) { continue }
        foreach ($name in @('path', 'layer', 'enabled', 'active', 'batchingStatic', 'batched', 'bounds', 'materials')) {
            if (!(Has-Property $renderer $name)) { throw "Mesh renderer has no $name field." }
        }
        $path = [string]$renderer.path
        if ([string]::IsNullOrWhiteSpace($path)) { throw 'Mesh renderer has an empty path.' }
        if (!$groups.ContainsKey($path)) { $groups.Add($path, [Collections.Generic.List[object]]::new()) }
        $groups[$path].Add([pscustomobject]@{
            raw = $renderer; bounds = @(Get-BoundsValues $renderer.bounds)
            materialKey = Get-MaterialKey $renderer.materials
            allowed = Test-AllowedPath $path
        })
    }
    return ,$groups
}
function Compare-Inventory($Before, $After, [string]$Scene) {
    $left = Get-MeshGroups $Before
    $right = Get-MeshGroups $After
    $counts = [ordered]@{
        baselineMeshes = 0; optimizedMeshes = 0; baselineStatic = 0; optimizedStatic = 0
        baselineBatched = 0; optimizedBatched = 0; newlyStatic = 0; newlyBatched = 0
        manifestMeshes = 0; manifestBatched = 0; strictBoundsCompared = 0; dynamicCompared = 0
        sourceVerifiedBoundsTightenings = 0; existingStaticRedistributions = 0
        dynamicDifferences = 0; materialSlotOrderIgnored = $true; combinedMeshIdentityIgnored = $true
    }
    foreach ($group in $left.Values) { $counts.baselineMeshes += $group.Count }
    foreach ($group in $right.Values) { $counts.optimizedMeshes += $group.Count }
    $paths = [Collections.Generic.HashSet[string]]::new($left.Keys, [StringComparer]::Ordinal)
    $paths.UnionWith($right.Keys)
    foreach ($path in $paths) {
        $a = if ($left.ContainsKey($path)) { @($left[$path].ToArray()) } else { @() }
        $b = if ($right.ContainsKey($path)) { @($right[$path].ToArray()) } else { @() }
        if ($a.Count -ne $b.Count) { $failures.Add("${Scene}: mesh count at '$path' changed $($a.Count) -> $($b.Count).") }
        foreach ($row in $a) {
            if ($row.raw.batchingStatic) { $counts.baselineStatic++ }
            if ($row.raw.batched) { $counts.baselineBatched++ }
        }
        foreach ($row in $b) {
            if ($row.raw.batchingStatic) { $counts.optimizedStatic++ }
            if ($row.raw.batched) { $counts.optimizedBatched++ }
        }
        $remaining = [Collections.Generic.List[object]]::new()
        foreach ($row in $b) { $remaining.Add($row) }
        foreach ($old in $a) {
            if ($remaining.Count -eq 0) { continue }
            # Duplicate transform names are common (e.g. 122 Coin objects). Match
            # immutable properties first, then the nearest bounds, never by array index.
            $best = -1; $bestScore = [double]::PositiveInfinity
            for ($i = 0; $i -lt $remaining.Count; $i++) {
                $candidate = $remaining[$i]
                $score = Get-MaxBoundsDifference $old.bounds $candidate.bounds
                if ($old.raw.type -cne $candidate.raw.type -or $old.raw.layer -ne $candidate.raw.layer -or
                    $old.materialKey -cne $candidate.materialKey) { $score += 1000000000 }
                if ($old.raw.enabled -ne $candidate.raw.enabled -or $old.raw.active -ne $candidate.raw.active) { $score += 1000000 }
                if ($score -lt $bestScore) { $best = $i; $bestScore = $score }
            }
            $new = $remaining[$best]; $remaining.RemoveAt($best)
            foreach ($name in @('type', 'layer')) {
                if ($old.raw.$name -cne $new.raw.$name) { $failures.Add("${Scene}: $name changed at '$path'.") }
            }
            if ($old.materialKey -cne $new.materialKey) { $failures.Add("${Scene}: material assets/names changed at '$path'.") }
            if (!$old.raw.batched -and !$new.raw.batched -and (Has-Property $old.raw 'mesh') -and
                (Has-Property $new.raw 'mesh') -and $old.raw.mesh -cne $new.raw.mesh) {
                $failures.Add("${Scene}: uncombined mesh identity changed at '$path'.")
            }
            $strict = $old.allowed -or $old.raw.batchingStatic -or $new.raw.batchingStatic -or $old.raw.batched -or $new.raw.batched
            $delta = Get-MaxBoundsDifference $old.bounds $new.bounds
            $stateChanged = $old.raw.enabled -ne $new.raw.enabled -or $old.raw.active -ne $new.raw.active
            if ($strict) {
                $counts.strictBoundsCompared++
                if ($delta -gt $BoundsTolerance) {
                    if ($sourceProofValidated -and $old.allowed -and !$old.raw.batched -and $new.raw.batched -and
                        !$old.raw.batchingStatic -and $new.raw.batchingStatic -and (Test-BoundsContained $old.bounds $new.bounds)) {
                        $counts.sourceVerifiedBoundsTightenings++
                        $warnings.Add([ordered]@{ scene = $Scene; path = $path; kind = 'source-verified-batch-bounds-tightening'
                            reason = 'New manifest batch; source proof confirms only static flags changed; new AABB is contained within old AABB.'
                            boundsMaximumDifference = $delta; baselineBounds = $old.bounds; optimizedBounds = $new.bounds })
                    } else { $failures.Add("${Scene}: static/manifest bounds changed by $delta at '$path'.") }
                }
                if ($stateChanged) { $failures.Add("${Scene}: static/manifest enabled/active changed at '$path'.") }
            } else {
                $counts.dynamicCompared++
                if ($delta -gt $BoundsTolerance -or $stateChanged) {
                    $counts.dynamicDifferences++
                    $warnings.Add([ordered]@{ scene = $Scene; path = $path; kind = 'dynamic-state-difference'
                        reason = 'Gameplay runs before measurement; unbatched dynamic bounds/state are diagnostic only.'
                        boundsMaximumDifference = $delta; baselineBounds = $old.bounds; optimizedBounds = $new.bounds
                        baselineEnabled = $old.raw.enabled; optimizedEnabled = $new.raw.enabled
                        baselineActive = $old.raw.active; optimizedActive = $new.raw.active })
                }
            }
            if ($old.raw.batchingStatic -ne $new.raw.batchingStatic) {
                if (!$old.allowed -or !$new.raw.batchingStatic) { $failures.Add("${Scene}: unauthorized batchingStatic change at '$path'.") }
                elseif (!$old.raw.batchingStatic) { $counts.newlyStatic++ }
            }
            if ($old.raw.batched -ne $new.raw.batched) {
                if (!$old.allowed) {
                    if ($sourceProofValidated -and $old.raw.batchingStatic -and $new.raw.batchingStatic -and
                        $delta -le $BoundsTolerance -and !$stateChanged -and $old.materialKey -ceq $new.materialKey -and
                        $old.raw.type -ceq $new.raw.type -and $old.raw.layer -eq $new.raw.layer) {
                        $counts.existingStaticRedistributions++
                        $warnings.Add([ordered]@{ scene = $Scene; path = $path; kind = 'existing-static-batch-redistribution'
                            reason = 'Already marked static in both runs; bounds, layer, materials and active/enabled state are unchanged.'
                            baselineBatched = $old.raw.batched; optimizedBatched = $new.raw.batched })
                    } else { $failures.Add("${Scene}: static batch membership changed outside manifest at '$path'.") }
                }
                elseif (!$new.raw.batched) { $failures.Add("${Scene}: manifest renderer lost static batch membership at '$path'.") }
                else { $counts.newlyBatched++ }
            }
            if ($old.allowed) {
                $counts.manifestMeshes++
                if ($new.raw.batched) { $counts.manifestBatched++ }
                if ($ExpectOptimized -and !$new.raw.batchingStatic) { $failures.Add("${Scene}: manifest renderer is not batchingStatic at '$path'.") }
                if ($ExpectOptimized -and $new.raw.enabled -and $new.raw.active -and !$new.raw.batched) {
                    $failures.Add("${Scene}: active manifest renderer was not combined at '$path'.")
                }
            }
        }
    }
    if ($ExpectOptimized -and $counts.manifestMeshes -gt 0 -and $counts.newlyStatic -eq 0) {
        $failures.Add("${Scene}: no new batchingStatic renderers; baseline may already be optimized.")
    }
    if ($ExpectOptimized -and $counts.manifestMeshes -gt 0 -and $counts.newlyBatched -eq 0) {
        $failures.Add("${Scene}: no new combined renderers; optimization was not exercised.")
    }
    return $counts
}

$leftFiles = @(Get-ChildItem -LiteralPath $baselineRoot -Filter '*-rendering.json' -File)
$rightFiles = @(Get-ChildItem -LiteralPath $optimizedRoot -Filter '*-rendering.json' -File)
$leftNames = @($leftFiles | ForEach-Object { $_.Name })
$keys = @($leftFiles | ForEach-Object { $_.Name -replace '-rendering\.json$', '' } | Sort-Object)
if ($keys.Count -eq 0) { $failures.Add('Baseline has no rendering reports.') }
foreach ($file in $rightFiles) {
    if ($file.Name -cnotin $leftNames) { $failures.Add("Optimized output has an unmatched scene: $($file.Name).") }
}
# File labels identify scenarios; the probe's human-readable scene header need not
# equal these labels, but must agree between baseline and optimized reports.
foreach ($pattern in @('^lobby$', '^tutorial$', '^(game|main)$', 'bonus', 'floor')) {
    if (@($keys | Where-Object { $_ -match $pattern }).Count -eq 0) { $failures.Add("Missing measured scenario matching '$pattern'.") }
}
foreach ($key in $keys) {
    $beforeFailureCount = $failures.Count
    try {
        $before = Read-Report (Join-Path $baselineRoot ($key + '-rendering.json'))
        $after = Read-Report (Join-Path $optimizedRoot ($key + '-rendering.json'))
        foreach ($name in @('scene', 'device', 'method', 'width', 'height', 'frames', 'quality', 'pipeline',
            'effectSettings', 'selectedCamera', 'activeCameras')) {
            if (!(Has-Property $before $name) -or !(Has-Property $after $name)) { throw "Missing rendering field '$name'." }
        }
        foreach ($name in @('scene', 'device', 'method', 'width', 'height', 'quality', 'pipeline')) {
            if ([string]::IsNullOrWhiteSpace([string]$before.$name) -or $before.$name -cne $after.$name) {
                $failures.Add("${key}: measurement $name differs or is empty.")
            }
        }
        if ($null -eq $before.selectedCamera -or $null -eq $after.selectedCamera -or
            @($before.activeCameras).Count -eq 0 -or @($after.activeCameras).Count -eq 0) {
            $failures.Add("${key}: camera control metadata is empty.")
        }
        if ($before.width -le 0 -or $before.height -le 0) { $failures.Add("${key}: invalid resolution.") }
        foreach ($side in @(@{ name = 'baseline'; report = $before }, @{ name = 'optimized'; report = $after })) {
            $frames = @($side.report.frames)
            if ($frames.Count -lt $MinimumFrames) { throw "$($side.name) has fewer than $MinimumFrames measured frames." }
            foreach ($frame in $frames) {
                foreach ($metric in @('draws', 'batches', 'setPass', 'triangles')) {
                    if (!(Has-Property $frame $metric) -or $null -eq $frame.$metric) { throw "$($side.name) frame lacks $metric." }
                    $value = [double]$frame.$metric
                    if ([double]::IsNaN($value) -or [double]::IsInfinity($value) -or $value -le 0) {
                        throw "$($side.name) contains a non-positive/invalid $metric counter."
                    }
                }
            }
        }
        if (@($before.frames).Count -ne @($after.frames).Count) { $failures.Add("${key}: measured frame counts differ.") }
        $metadata = [ordered]@{}
        $extraNames = @(@($before.PSObject.Properties.Name) + @($after.PSObject.Properties.Name) | Sort-Object -Unique)
        foreach ($name in $extraNames) {
            if ($name -in @('frames', 'scene', 'device', 'method', 'width', 'height')) { continue }
            if (!(Has-Property $before $name) -or !(Has-Property $after $name)) { $failures.Add("${key}: metadata '$name' is absent from one run."); continue }
            if ($name -eq 'selectedCamera') {
                $same = Test-CameraEqual $before.$name $after.$name
            } elseif ($name -eq 'activeCameras') {
                $same = @($before.$name).Count -eq @($after.$name).Count
                if ($same) {
                    for ($index = 0; $index -lt @($before.$name).Count; $index++) {
                        if (!(Test-CameraEqual $before.$name[$index] $after.$name[$index])) { $same = $false }
                    }
                }
            } else { $same = (Get-CanonicalJson $before.$name) -ceq (Get-CanonicalJson $after.$name) }
            $metadata[$name] = [ordered]@{ equal = $same; baseline = $before.$name; optimized = $after.$name }
            if (!$same) { $failures.Add("${key}: control metadata '$name' differs.") }
        }
        $statistics = [ordered]@{}
        foreach ($metric in @('draws', 'batches', 'setPass', 'triangles')) {
            $statistics[$metric] = [ordered]@{ baseline = Get-Statistics $before.frames $metric; optimized = Get-Statistics $after.frames $metric }
        }
        $reduction = 100.0 * ($statistics.draws.baseline.median - $statistics.draws.optimized.median) / $statistics.draws.baseline.median
        if ($ExpectOptimized -and $key -match '(game|main|bonus|floor|tutorial)' -and
            $statistics.draws.optimized.median -gt $statistics.draws.baseline.median + 1) {
            $failures.Add("${key}: median draws regressed $($statistics.draws.baseline.median) -> $($statistics.draws.optimized.median) (tolerance +1).")
        }
        $inventory = Compare-Inventory (Read-Report (Join-Path $baselineRoot ($key + '-renderers.json'))) `
            (Read-Report (Join-Path $optimizedRoot ($key + '-renderers.json'))) $key
        $results.Add([ordered]@{ sceneKey = $key; scene = $before.scene; device = $before.device; method = $before.method
            width = $before.width; height = $before.height; baselineFrames = @($before.frames).Count; optimizedFrames = @($after.frames).Count
            statistics = $statistics; drawReductionPercent = [Math]::Round($reduction, 3); controls = $metadata
            inventory = $inventory; success = $failures.Count -eq $beforeFailureCount })
    } catch { $failures.Add("${key}: $($_.Exception.Message)") }
}
$evidenceFullPath = [IO.Path]::GetFullPath($EvidencePath)
$parentDirectory = [IO.Path]::GetDirectoryName($evidenceFullPath)
[void][IO.Directory]::CreateDirectory($parentDirectory)
$report = [ordered]@{ schemaVersion = 1; success = $failures.Count -eq 0; baselinePath = $baselineRoot; optimizedPath = $optimizedRoot
    expectOptimized = $ExpectOptimized; minimumFrames = $MinimumFrames; boundsTolerance = $BoundsTolerance
    manifestPath = (Resolve-Path -LiteralPath $BatchManifestPath).Path; manifestSha256 = (Get-FileHash -LiteralPath $BatchManifestPath -Algorithm SHA256).Hash
    sourceProof = $sourceProofEvidence
    comparisonScope = 'All MeshRenderer/SkinnedMeshRenderer paths, counts, types, layers, material identities and static flags; strict static/manifest bounds/state, except contained AABB tightening of new manifest batches with hash-bound flag-only source proof. Existing static batch redistribution requires unchanged bounds/state/material/layer and source proof. Dynamic bounds/state differences are reported, not treated as equivalent.'
    cameraTolerance = [ordered]@{ position = 0.0001; euler = 0.0001; fov = 0.01; orthographicSize = 0.0001 }
    limitations = 'Isolated editor frame counters including UI; not player FPS. Material slot order/duplicates and combined mesh identity/vertex counts are intentionally ignored. No screenshot comparison.'
    scenes = $results.ToArray(); warnings = $warnings.ToArray(); failures = $failures.ToArray() }
$report | ConvertTo-Json -Depth 40 | Set-Content -LiteralPath $evidenceFullPath -Encoding utf8
Write-Output 'Draws are shown as min/median/max; batch columns count newly flagged/combined mesh renderers.'
$results | ForEach-Object { [pscustomobject]@{
    Scene = $_.sceneKey; Before = "$($_.statistics.draws.baseline.min)/$($_.statistics.draws.baseline.median)/$($_.statistics.draws.baseline.max)"
    After = "$($_.statistics.draws.optimized.min)/$($_.statistics.draws.optimized.median)/$($_.statistics.draws.optimized.max)"
    'Drop %' = $_.drawReductionPercent; 'Static +' = $_.inventory.newlyStatic; 'Batched +' = $_.inventory.newlyBatched
    DynamicWarn = $_.inventory.dynamicDifferences; Passed = $_.success
} } | Format-Table -AutoSize
Write-Output "Evidence: $evidenceFullPath"
Write-Output "Warnings: $($warnings.Count); failures: $($failures.Count)"
if ($failures.Count -gt 0) { throw "Rendering comparison failed. See evidence JSON for $($failures.Count) failures." }
