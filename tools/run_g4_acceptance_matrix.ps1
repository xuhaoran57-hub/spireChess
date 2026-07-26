[CmdletBinding()]
param(
    [string]$PlayerPath = "",

    [string]$OutputDirectory =
        (Join-Path $PSScriptRoot "..\sc\Logs\G4\Acceptance"),

    [string]$Quality = "High",

    [ValidateRange(1, 2147483647)]
    [int]$Seed = 940101,

    [ValidateRange(1, 10)]
    [int]$Repetitions = 1,

    [switch]$NoScreenshots,

    [switch]$FrozenVisual,

    [switch]$Stress,

    [ValidateRange(30, 1800)]
    [int]$TimeoutSeconds = 240
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($FrozenVisual -and $Stress) {
    throw "-FrozenVisual and -Stress are mutually exclusive."
}
if ($FrozenVisual -and -not $PSBoundParameters.ContainsKey("Seed")) {
    $Seed = 78
}
elseif ($Stress -and -not $PSBoundParameters.ContainsKey("Seed")) {
    $Seed = 940401
}
$acceptanceMode = if ($Stress) {
    "stress"
} elseif ($FrozenVisual) {
    "frozen-visual"
} else {
    "core"
}

function Resolve-G4MatrixPlayerPath {
    param(
        [string]$RequestedPlayerPath,
        [string]$BuildRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($RequestedPlayerPath)) {
        $resolved = [System.IO.Path]::GetFullPath($RequestedPlayerPath)
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
            throw "G4 Player not found: $resolved"
        }
        return $resolved
    }

    $latestManifest = Get-ChildItem `
        -LiteralPath $BuildRoot `
        -Filter "g4-build-manifest.json" `
        -File `
        -Recurse |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
    if ($null -eq $latestManifest) {
        throw "No G4 build manifest was found under $BuildRoot. Run build_g4_windows.ps1 first or pass -PlayerPath."
    }

    $buildManifest = Get-Content `
        -LiteralPath $latestManifest.FullName `
        -Encoding UTF8 `
        -Raw |
        ConvertFrom-Json
    $declaredOutputPath = [string]$buildManifest.outputPath
    if ([string]::IsNullOrWhiteSpace($declaredOutputPath)) {
        throw "Latest G4 build manifest has no outputPath: $($latestManifest.FullName)"
    }

    # Resolve beside the manifest so a build directory can be relocated without
    # causing a stale absolute outputPath to select another copy of the Player.
    $playerFileName = [System.IO.Path]::GetFileName($declaredOutputPath)
    if ([string]::IsNullOrWhiteSpace($playerFileName)) {
        throw "Latest G4 build manifest outputPath has no executable name: $($latestManifest.FullName)"
    }
    $resolved = [System.IO.Path]::GetFullPath(
        (Join-Path $latestManifest.DirectoryName $playerFileName))
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
        throw "G4 Player declared by the latest build manifest was not found: $resolved"
    }
    return $resolved
}

function Assert-G4MatrixEvidenceIdentity {
    param(
        [object]$EvidenceManifest,
        [string]$ExpectedPlayerPath,
        [string]$ExpectedPlayerSha256,
        [string]$ExpectedBuildId,
        [string]$ExpectedBuildManifestSha256,
        [string]$ExpectedAcceptanceMode,
        [int]$ExpectedSeed,
        [string]$RunId
    )

    if (-not [string]::Equals(
            [string]$EvidenceManifest.schemaVersion,
            "spire-chess-g4-evidence-v2",
            [System.StringComparison]::Ordinal)) {
        throw "G4 matrix run $RunId has an unsupported evidence schema."
    }
    if ([bool]$EvidenceManifest.sourceTreeDirty -or
        -not [string]::Equals(
            [string]$EvidenceManifest.evidenceClassification,
            "FormalCandidate",
            [System.StringComparison]::Ordinal)) {
        throw "G4 matrix run $RunId is not clean FormalCandidate evidence."
    }
    if (-not [string]::Equals(
            [string]$EvidenceManifest.runId,
            $RunId,
            [System.StringComparison]::Ordinal)) {
        throw "G4 matrix evidence runId '$($EvidenceManifest.runId)' does not match '$RunId'."
    }
    if ([int]$EvidenceManifest.seed -ne $ExpectedSeed) {
        throw "G4 matrix run $RunId used seed '$($EvidenceManifest.seed)'; expected '$ExpectedSeed'."
    }

    $evidencePlayerPath = [string]$EvidenceManifest.playerPath
    if ([string]::IsNullOrWhiteSpace($evidencePlayerPath)) {
        throw "G4 evidence manifest for $RunId has no playerPath."
    }
    $resolvedEvidencePlayerPath =
        [System.IO.Path]::GetFullPath($evidencePlayerPath)
    if (-not [string]::Equals(
            $resolvedEvidencePlayerPath,
            $ExpectedPlayerPath,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "G4 matrix run $RunId used a different PlayerPath. Expected '$ExpectedPlayerPath'; found '$resolvedEvidencePlayerPath'."
    }

    $evidencePlayerSha256 = [string]$EvidenceManifest.playerSha256
    if (-not [string]::Equals(
            $evidencePlayerSha256,
            $ExpectedPlayerSha256,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "G4 matrix run $RunId used a different Player binary SHA-256."
    }

    $evidenceBuildId = [string]$EvidenceManifest.buildId
    if ([string]::IsNullOrWhiteSpace($evidenceBuildId) -or
        -not [string]::Equals(
            $evidenceBuildId,
            $ExpectedBuildId,
            [System.StringComparison]::Ordinal)) {
        throw "G4 matrix run $RunId used buildId '$evidenceBuildId'; expected '$ExpectedBuildId'."
    }

    $evidenceBuildManifestSha256 =
        [string]$EvidenceManifest.buildManifestSha256
    if ([string]::IsNullOrWhiteSpace($evidenceBuildManifestSha256) -or
        -not [string]::Equals(
            $evidenceBuildManifestSha256,
            $ExpectedBuildManifestSha256,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "G4 matrix run $RunId used a different build manifest SHA-256."
    }

    if (-not [string]::Equals(
            [string]$EvidenceManifest.acceptanceMode,
            $ExpectedAcceptanceMode,
            [System.StringComparison]::Ordinal)) {
        throw "G4 matrix run $RunId used acceptanceMode '$($EvidenceManifest.acceptanceMode)'; expected '$ExpectedAcceptanceMode'."
    }
}

function Assert-G4MatrixSummaryIdentity {
    param(
        [object[]]$Results
    )

    if ($Results.Count -le 0) {
        throw "G4 matrix produced no run results."
    }
    $buildIds = @($Results |
        ForEach-Object { [string]$_.buildId } |
        Sort-Object -Unique)
    $manifestHashes = @($Results |
        ForEach-Object { [string]$_.buildManifestSha256 } |
        Sort-Object -Unique)
    if ($buildIds.Count -ne 1) {
        throw "G4 matrix mixed buildIds: $($buildIds -join ', ')."
    }
    if ($manifestHashes.Count -ne 1) {
        throw "G4 matrix mixed build manifest SHA-256 values: $($manifestHashes -join ', ')."
    }
}

$runner = Join-Path $PSScriptRoot "run_g4_acceptance.ps1"
$buildRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..\sc\Builds\G4"))
$resolvedPlayerPath = Resolve-G4MatrixPlayerPath `
    -RequestedPlayerPath $PlayerPath `
    -BuildRoot $buildRoot
$buildManifestPath = Join-Path `
    ([System.IO.Path]::GetDirectoryName($resolvedPlayerPath)) `
    "g4-build-manifest.json"
if (-not (Test-Path -LiteralPath $buildManifestPath -PathType Leaf)) {
    throw "G4 build manifest is missing beside Player: $buildManifestPath"
}
$frozenBuildManifest = Get-Content `
    -LiteralPath $buildManifestPath `
    -Encoding UTF8 `
    -Raw |
    ConvertFrom-Json
if ([bool]$frozenBuildManifest.sourceTreeDirty) {
    throw "G4 matrix refuses a Player built from a dirty source tree."
}
$frozenBuildId = [string]$frozenBuildManifest.buildId
if ([string]::IsNullOrWhiteSpace($frozenBuildId)) {
    throw "G4 build manifest has no buildId: $buildManifestPath"
}
$frozenBuildManifestSha256 = (
    Get-FileHash -LiteralPath $buildManifestPath -Algorithm SHA256
).Hash.ToLowerInvariant()
$frozenPlayerSha256 = (
    Get-FileHash -LiteralPath $resolvedPlayerPath -Algorithm SHA256
).Hash.ToLowerInvariant()
Write-Host "Frozen G4 matrix Player: $resolvedPlayerPath"
Write-Host "Frozen build identity: $frozenBuildId / $frozenBuildManifestSha256"

$matrixId = "{0}-{1}" -f
    ([DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")),
    ([Environment]::MachineName -replace '[^A-Za-z0-9_.-]', '-')
$resolvedOutputDirectory =
    [System.IO.Path]::GetFullPath($OutputDirectory)
$matrixResults = @()
foreach ($resolution in @("1920x1080", "1920x1200")) {
    for ($repetition = 1; $repetition -le $Repetitions; $repetition++) {
        $matrixRunId = "{0}-{1}-r{2:d2}" -f
            $matrixId,
            $resolution,
            $repetition
        $runnerArguments = @{
            PlayerPath = $resolvedPlayerPath
            OutputDirectory = $OutputDirectory
            Resolution = $resolution
            Quality = $Quality
            Seed = $Seed
            RunId = $matrixRunId
            TimeoutSeconds = $TimeoutSeconds
        }
        if ($NoScreenshots) {
            $runnerArguments.NoScreenshots = $true
        }
        if ($FrozenVisual) {
            $runnerArguments.FrozenVisual = $true
        }
        if ($Stress) {
            $runnerArguments.Stress = $true
        }
        & $runner @runnerArguments

        $runRoot = Join-Path $resolvedOutputDirectory $matrixRunId
        $reports = @(
            Get-ChildItem `
                -LiteralPath (Join-Path $runRoot "performance") `
                -Filter "g4-performance-*.json" `
                -File
        )
        if ($reports.Count -ne 1) {
            throw "Expected exactly one G4 report for matrix run $matrixRunId; found $($reports.Count)."
        }
        $report = Get-Content `
            -LiteralPath $reports[0].FullName `
            -Encoding UTF8 `
            -Raw |
            ConvertFrom-Json
        $manifestPath = Join-Path $runRoot "g4-evidence-manifest.json"
        $manifest = Get-Content `
            -LiteralPath $manifestPath `
            -Encoding UTF8 `
            -Raw |
            ConvertFrom-Json
        Assert-G4MatrixEvidenceIdentity `
            -EvidenceManifest $manifest `
            -ExpectedPlayerPath $resolvedPlayerPath `
            -ExpectedPlayerSha256 $frozenPlayerSha256 `
            -ExpectedBuildId $frozenBuildId `
            -ExpectedBuildManifestSha256 $frozenBuildManifestSha256 `
            -ExpectedAcceptanceMode $acceptanceMode `
            -ExpectedSeed $Seed `
            -RunId $matrixRunId
        $performanceReportName = [string]$manifest.performanceReport
        if ([string]::IsNullOrWhiteSpace($performanceReportName) -or
            -not [string]::Equals(
                [System.IO.Path]::GetFileName($performanceReportName),
                $performanceReportName,
                [System.StringComparison]::Ordinal) -or
            -not [string]::Equals(
                $reports[0].Name,
                $performanceReportName,
                [System.StringComparison]::Ordinal)) {
            throw "G4 matrix run $matrixRunId has a mismatched or unsafe performance report filename."
        }
        $performanceReportHash = (
            Get-FileHash `
                -LiteralPath $reports[0].FullName `
                -Algorithm SHA256
        ).Hash.ToLowerInvariant()
        if (-not [string]::Equals(
                $performanceReportHash,
                [string]$manifest.performanceReportSha256,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "G4 matrix run $matrixRunId performance report hash does not match its evidence manifest."
        }
        if (-not [string]::Equals(
                [string]$report.schemaVersion,
                "spire-chess-g4-performance-v2",
                [System.StringComparison]::Ordinal) -or
            -not [string]::Equals(
                [string]$report.completionStatus,
                "AcceptancePassed",
                [System.StringComparison]::Ordinal) -or
            -not [string]::Equals(
                [string]$report.runId,
                $matrixRunId,
                [System.StringComparison]::Ordinal) -or
            -not [string]::Equals(
                [string]$report.configuration.acceptanceSeed,
                [string]$Seed,
                [System.StringComparison]::Ordinal)) {
            throw "G4 matrix run $matrixRunId performance report identity or completion gate failed."
        }
        $samplesCsvName = [string]$manifest.samplesCsv
        if ([string]::IsNullOrWhiteSpace($samplesCsvName) -or
            -not [string]::Equals(
                [System.IO.Path]::GetFileName($samplesCsvName),
                $samplesCsvName,
                [System.StringComparison]::Ordinal)) {
            throw "G4 matrix run $matrixRunId has an unsafe raw CSV filename."
        }
        $samplesCsvPath = Join-Path `
            (Join-Path $runRoot "performance") `
            $samplesCsvName
        if (-not (Test-Path -LiteralPath $samplesCsvPath -PathType Leaf)) {
            throw "G4 matrix run $matrixRunId is missing its raw frame-sample CSV."
        }
        $samplesCsvHash = (
            Get-FileHash -LiteralPath $samplesCsvPath -Algorithm SHA256
        ).Hash.ToLowerInvariant()
        if (-not [string]::Equals(
                $samplesCsvHash,
                [string]$manifest.samplesCsvSha256,
                [System.StringComparison]::OrdinalIgnoreCase) -or
            [int]$manifest.samplesCsvSampleCount -ne
                [int]$report.overall.sampleCount) {
            throw "G4 matrix run $matrixRunId raw CSV identity does not match its evidence manifest/report."
        }
        $playerLogPath = Join-Path $runRoot "player.log"
        if (-not (Test-Path -LiteralPath $playerLogPath -PathType Leaf)) {
            throw "G4 matrix run $matrixRunId is missing player.log."
        }
        $playerLogHash = (
            Get-FileHash -LiteralPath $playerLogPath -Algorithm SHA256
        ).Hash.ToLowerInvariant()
        if (-not [string]::Equals(
                $playerLogHash,
                [string]$manifest.playerLogSha256,
                [System.StringComparison]::OrdinalIgnoreCase) -or
            [int]$manifest.runtimeFailureLogCount -ne 0 -or
            -not [bool]$manifest.runtimeLogGatePassed -or
            -not ($manifest.PSObject.Properties.Name -contains
                "runtimeFailureMarkerPresent") -or
            [bool]$manifest.runtimeFailureMarkerPresent) {
            throw "G4 matrix run $matrixRunId Player log/runtime-error gate identity failed."
        }
        $runtimeFailureMarkerPath = Join-Path `
            (Join-Path $runRoot "performance") `
            "g4-runtime-failures.log"
        if (Test-Path -LiteralPath $runtimeFailureMarkerPath -PathType Leaf) {
            throw "G4 matrix run $matrixRunId contains a runtime failure marker."
        }
        $matrixResults += [pscustomobject]@{
            runId = $matrixRunId
            resolution = $resolution
            repetition = $repetition
            reportPath = $reports[0].FullName
            reportSha256 = $performanceReportHash
            samplesCsvPath = $samplesCsvPath
            samplesCsvSha256 = $samplesCsvHash
            samplesCsvLineCount =
                [int]$manifest.samplesCsvLineCount
            playerLogPath = $playerLogPath
            playerLogSha256 = $playerLogHash
            runtimeFailureLogCount =
                [int]$manifest.runtimeFailureLogCount
            runtimeFailureMarkerPresent = $false
            playerPath = [string]$manifest.playerPath
            playerSha256 = [string]$manifest.playerSha256
            buildId = [string]$manifest.buildId
            buildManifestSha256 =
                [string]$manifest.buildManifestSha256
            completionStatus = [string]$report.completionStatus
            sampleCount = [int]$report.overall.sampleCount
            measuredSeconds = [double]$report.overall.measuredSeconds
            frameAverageMs = [double]$report.overall.frameTimeMs.average
            frameP50Ms = [double]$report.overall.frameTimeMs.p50
            frameP95Ms = [double]$report.overall.frameTimeMs.p95
            frameP99Ms = [double]$report.overall.frameTimeMs.p99
            frameMaximumMs = [double]$report.overall.frameTimeMs.maximum
            peakTotalUsedMemoryBytes =
                [long]$report.overall.peakTotalUsedMemoryBytes
            finalTotalUsedMemoryBytes =
                [long]$report.overall.finalTotalUsedMemoryBytes
            peakGcUsedMemoryBytes =
                [long]$report.overall.peakGcUsedMemoryBytes
            peakTextureMemoryBytes =
                [long]$report.overall.peakTextureMemoryBytes
            cleanupPassed = [bool]$report.cleanup.cleanAtCompletion
            sampleCatalogExact = [bool]$report.artwork.catalogExact
            screenshotsCaptured = @($manifest.screenshots).Count
        }
    }
}

Assert-G4MatrixSummaryIdentity -Results $matrixResults

$captureMode = if ($NoScreenshots) {
    "performance-only (screenshots disabled)"
} else {
    "visual evidence"
}
$summaryPath = Join-Path `
    $resolvedOutputDirectory `
    "g4-matrix-$matrixId-summary.json"
$csvPath = Join-Path `
    $resolvedOutputDirectory `
    "g4-matrix-$matrixId-runs.csv"
if ((Test-Path -LiteralPath $summaryPath) -or
    (Test-Path -LiteralPath $csvPath)) {
    throw "Refusing to overwrite an existing G4 matrix summary for $matrixId."
}
$matrixSummary = [pscustomobject]@{
    schemaVersion = "spire-chess-g4-matrix-v2"
    generatedAtUtc = [DateTime]::UtcNow.ToString("o")
    matrixId = $matrixId
    quality = $Quality
    seed = $Seed
    repetitionsPerResolution = $Repetitions
    noScreenshots = [bool]$NoScreenshots
    acceptanceMode = $acceptanceMode
    playerPath = $resolvedPlayerPath
    playerSha256 = $frozenPlayerSha256
    buildId = $frozenBuildId
    buildManifestSha256 = $frozenBuildManifestSha256
    runCount = $matrixResults.Count
    runs = $matrixResults
}
$matrixSummary |
    ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath $summaryPath -Encoding UTF8
$matrixResults |
    Export-Csv -LiteralPath $csvPath -NoTypeInformation -Encoding UTF8

Write-Host "G4 dual-resolution $acceptanceMode matrix passed: $Repetitions repetition(s) per resolution; $captureMode."
Write-Host "Matrix JSON: $summaryPath"
Write-Host "Matrix CSV:  $csvPath"
