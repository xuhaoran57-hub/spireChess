[CmdletBinding()]
param(
    [string]$UnityPath = $env:UNITY_EXE,

    [string]$ProjectPath = "",

    [ValidateRange(60, 7200)]
    [int]$CaptureTimeoutSeconds = 1800,

    [ValidateRange(60, 7200)]
    [int]$TestTimeoutSeconds = 1800,

    [ValidateRange(5, 120)]
    [int]$HeartbeatSeconds = 10
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-UnityPath {
    param(
        [string]$RequestedPath,
        [string]$ResolvedProjectPath
    )

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        $resolved = [System.IO.Path]::GetFullPath($RequestedPath)
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
            throw "Unity executable not found: $resolved"
        }
        return $resolved
    }

    $versionFile = Join-Path `
        $ResolvedProjectPath `
        "ProjectSettings\ProjectVersion.txt"
    $versionLine = Get-Content -LiteralPath $versionFile -Encoding UTF8 |
        Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } |
        Select-Object -First 1
    if ($null -eq $versionLine -or
        $versionLine -notmatch '^m_EditorVersion:\s*(.+)$') {
        throw "Unable to read Unity version from $versionFile"
    }

    $version = $Matches[1].Trim()
    $programFiles = [Environment]::GetFolderPath("ProgramFiles")
    $resolved = Join-Path `
        $programFiles `
        "Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
        throw "Unity $version was not found at $resolved."
    }
    return $resolved
}

function Get-FileSha256 {
    param([string]$Path)

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-ProtectedHashes {
    param(
        [string]$RepositoryRoot,
        [string[]]$RelativePaths
    )

    $hashes = @{}
    foreach ($relativePath in $RelativePaths) {
        $absolutePath = Join-Path $RepositoryRoot $relativePath
        if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
            throw "Protected Phase 9C asset is missing: $absolutePath"
        }
        $hashes[$relativePath] = Get-FileSha256 -Path $absolutePath
    }
    return $hashes
}

function Get-UnityAssetGuid {
    param(
        [string]$RepositoryRoot,
        [string]$AssetRelativePath
    )

    $metaPath = Join-Path $RepositoryRoot ($AssetRelativePath + ".meta")
    if (-not (Test-Path -LiteralPath $metaPath -PathType Leaf)) {
        throw "Unity asset metadata is missing: $metaPath"
    }
    $guidLine = Get-Content -LiteralPath $metaPath -Encoding UTF8 |
        Where-Object { $_ -match '^guid:\s*([0-9a-fA-F]{32})\s*$' } |
        Select-Object -First 1
    if ($null -eq $guidLine -or
        $guidLine -notmatch '^guid:\s*([0-9a-fA-F]{32})\s*$') {
        throw "Unity asset GUID is missing or invalid: $metaPath"
    }
    return $Matches[1].ToLowerInvariant()
}

function Assert-ProtectedHashesUnchanged {
    param(
        [hashtable]$Before,
        [hashtable]$After
    )

    $changed = @()
    foreach ($relativePath in $Before.Keys) {
        if ($Before[$relativePath] -ne $After[$relativePath]) {
            $changed += $relativePath
        }
    }
    if ($changed.Count -gt 0) {
        throw (
            "Phase 9C modified protected Runtime/formal assets: " +
            ($changed -join ", ")
        )
    }
}

function Invoke-Capture {
    param(
        [string]$ResolvedUnityPath,
        [string]$ResolvedProjectPath,
        [string]$LogPath,
        [int]$TimeoutSeconds,
        [int]$Heartbeat
    )

    $arguments = @(
        "-batchmode",
        "-projectPath", ('"{0}"' -f $ResolvedProjectPath),
        "-executeMethod",
        "SpireChess.Editor.LightStorybookProductionAcceptance.BuildAndCaptureFromCommandLine",
        "-logFile", ('"{0}"' -f $LogPath),
        "-quit"
    )
    Write-Host "Running Phase 9C catalog rebuild and GPU capture..."
    $process = Start-Process `
        -FilePath $ResolvedUnityPath `
        -ArgumentList $arguments `
        -WindowStyle Hidden `
        -PassThru
    try {
        $startedAt = [DateTime]::UtcNow
        $lastHeartbeatAt = $startedAt
        while (-not $process.HasExited) {
            $process.WaitForExit(1000) | Out-Null
            $process.Refresh()
            $now = [DateTime]::UtcNow
            $elapsed = ($now - $startedAt).TotalSeconds
            if (($now - $lastHeartbeatAt).TotalSeconds -ge $Heartbeat) {
                $logBytes = if (
                    Test-Path -LiteralPath $LogPath -PathType Leaf
                ) {
                    (Get-Item -LiteralPath $LogPath).Length
                } else {
                    0
                }
                $heartbeatMessage =
                    "Phase 9C capture heartbeat: elapsed={0:n0}s, " +
                    "cpu={1:n1}s, log={2} bytes"
                Write-Host (
                    $heartbeatMessage -f
                        $elapsed,
                        $process.TotalProcessorTime.TotalSeconds,
                        $logBytes
                )
                $lastHeartbeatAt = $now
            }
            if ($elapsed -ge $TimeoutSeconds) {
                $process.Kill()
                $process.WaitForExit(10000) | Out-Null
                throw (
                    "Phase 9C capture timed out after " +
                    "$TimeoutSeconds seconds. Log: $LogPath"
                )
            }
        }

        if ($process.ExitCode -ne 0) {
            throw (
                "Phase 9C capture failed with exit code " +
                "$($process.ExitCode). Log: $LogPath"
            )
        }
    } finally {
        $process.Refresh()
        if (-not $process.HasExited) {
            $process.Kill()
            $process.WaitForExit(10000) | Out-Null
        }
    }
}

function Get-TestSummary {
    param(
        [string]$Platform,
        [string]$Path
    )

    [xml]$document = Get-Content -LiteralPath $Path -Encoding UTF8
    $run = $document.'test-run'
    return [ordered]@{
        platform = $Platform
        result = [string]$run.result
        total = [int]$run.total
        passed = [int]$run.passed
        failed = [int]$run.failed
        skipped = [int]$run.skipped
        inconclusive = [int]$run.inconclusive
        durationSeconds = [math]::Round([double]$run.duration, 3)
    }
}

function Get-EvidenceFiles {
    param([string]$ReleaseDirectory)

    return @(
        Get-ChildItem -LiteralPath $ReleaseDirectory -File -Recurse |
            Where-Object {
                $_.Name -ne "release-manifest.json" -and
                $_.Name -ne "phase9c-run.stdout.log" -and
                $_.Name -ne "phase9c-run.stderr.log" -and
                $_.Extension -ne ".md"
            } |
            Sort-Object FullName |
            ForEach-Object {
                $relative = $_.FullName.Substring(
                    $ReleaseDirectory.Length
                ).TrimStart([char]'\').Replace('\', '/')
                [ordered]@{
                    path = $relative
                    sha256 = Get-FileSha256 -Path $_.FullName
                    bytes = $_.Length
                }
            }
    )
}

$scriptDirectory = if (
    -not [string]::IsNullOrWhiteSpace($PSScriptRoot)
) {
    $PSScriptRoot
} else {
    Split-Path -Parent $MyInvocation.MyCommand.Path
}
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $scriptDirectory "..\sc"
}
$resolvedProjectPath = [System.IO.Path]::GetFullPath($ProjectPath)
if (-not (Test-Path -LiteralPath $resolvedProjectPath -PathType Container)) {
    throw "Unity project not found: $resolvedProjectPath"
}
$repositoryRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $resolvedProjectPath "..")
)
$resolvedUnityPath = Resolve-UnityPath `
    -RequestedPath $UnityPath `
    -ResolvedProjectPath $resolvedProjectPath
$releaseDirectory = Join-Path `
    $repositoryRoot `
    "ui-concepts\phase-9c\light-storybook-production-v0.1\unity-batch-release-v0.3.3"
$testDirectory = Join-Path $releaseDirectory "tests"
$logDirectory = Join-Path $releaseDirectory "logs"
New-Item -ItemType Directory -Path $releaseDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null

$protectedRelativePaths = @(
    "sc\Assets\Configs\Presentation\PresentationSpriteCatalog.asset",
    "sc\Assets\Configs\Presentation\PresentationSpriteCatalog.asset.meta",
    "sc\Assets\Configs\Presentation\PresentationSpriteCatalog_LightStorybookFormalCatalogV032.asset",
    "sc\Assets\Configs\Presentation\PresentationSpriteCatalog_LightStorybookFormalCatalogV032.asset.meta",
    "sc\Assets\Prefabs\UI\Common\PF_Card.prefab",
    "sc\Assets\Prefabs\UI\Common\PF_Card.prefab.meta",
    "sc\Assets\Prefabs\UI\Shop\PF_ShopScreen.prefab",
    "sc\Assets\Prefabs\UI\Shop\PF_ShopScreen.prefab.meta",
    "sc\Assets\Prefabs\UI\Battle\PF_BattleScreen.prefab",
    "sc\Assets\Prefabs\UI\Battle\PF_BattleScreen.prefab.meta"
)
$catalogIdentityExpectations = @(
    [ordered]@{
        path = "sc\Assets\Configs\Presentation\PresentationSpriteCatalog_LightStorybookProductionV033Batch01.asset"
        expectedGuid = "d9212ca6f5e4c7bb20693784d5abfc97"
    },
    [ordered]@{
        path = "sc\Assets\Configs\Presentation\PresentationSpriteCatalog_LightStorybookProductionV033Batch02.asset"
        expectedGuid = "1200000000000000000000000000000c"
    },
    [ordered]@{
        path = "sc\Assets\Configs\Presentation\PresentationSpriteCatalog_LightStorybookProductionV033Batch03.asset"
        expectedGuid = "13000000000000000000000000000008"
    },
    [ordered]@{
        path = "sc\Assets\Configs\Presentation\PresentationSpriteCatalog_LightStorybookProductionV033Batch04.asset"
        expectedGuid = "1400000000000000000000000000000c"
    },
    [ordered]@{
        path = "sc\Assets\Configs\Presentation\PresentationSpriteCatalog_LightStorybookProductionV033Batch05.asset"
        expectedGuid = "15000000000000000000000000000007"
    },
    [ordered]@{
        path = "sc\Assets\Configs\Presentation\PresentationSpriteCatalog_LightStorybookProductionV033Batch06.asset"
        expectedGuid = "1600000000000000000000000000000a"
    }
)
$protectedBefore = Get-ProtectedHashes `
    -RepositoryRoot $repositoryRoot `
    -RelativePaths $protectedRelativePaths
$catalogGuidsBefore = @{}
foreach ($identity in $catalogIdentityExpectations) {
    $catalogGuidsBefore[$identity.path] = Get-UnityAssetGuid `
        -RepositoryRoot $repositoryRoot `
        -AssetRelativePath $identity.path
}

$captureLog = Join-Path $logDirectory "phase9c-capture.log"
Invoke-Capture `
    -ResolvedUnityPath $resolvedUnityPath `
    -ResolvedProjectPath $resolvedProjectPath `
    -LogPath $captureLog `
    -TimeoutSeconds $CaptureTimeoutSeconds `
    -Heartbeat $HeartbeatSeconds

$captureIndex = Join-Path $releaseDirectory "capture-index.json"
if (-not (Test-Path -LiteralPath $captureIndex -PathType Leaf)) {
    throw "Phase 9C capture index was not produced: $captureIndex"
}
$screenshots = @(
    Get-ChildItem `
        -LiteralPath (Join-Path $releaseDirectory "screenshots") `
        -Filter "*.png" `
        -File
)
if ($screenshots.Count -ne 42) {
    throw "Phase 9C expected 42 screenshots; found $($screenshots.Count)."
}
$completionLine = Select-String `
    -LiteralPath $captureLog `
    -SimpleMatch `
    "[LightStorybook] Phase 9C acceptance captured 42 screenshots" |
    Select-Object -First 1
if ($null -eq $completionLine) {
    throw "Phase 9C capture completion marker is missing from $captureLog"
}

$testRunner = Join-Path $scriptDirectory "run_unity_tests.ps1"
& $testRunner `
    -Platform All `
    -UnityPath $resolvedUnityPath `
    -ProjectPath $resolvedProjectPath `
    -ResultsDirectory $testDirectory `
    -TimeoutSeconds $TestTimeoutSeconds `
    -HeartbeatSeconds $HeartbeatSeconds

$protectedAfter = Get-ProtectedHashes `
    -RepositoryRoot $repositoryRoot `
    -RelativePaths $protectedRelativePaths
Assert-ProtectedHashesUnchanged `
    -Before $protectedBefore `
    -After $protectedAfter

$catalogIdentityEvidence = @(
    $catalogIdentityExpectations |
        ForEach-Object {
            $actualGuid = Get-UnityAssetGuid `
                -RepositoryRoot $repositoryRoot `
                -AssetRelativePath $_.path
            $beforeGuid = $catalogGuidsBefore[$_.path]
            if ($actualGuid -ne $_.expectedGuid) {
                throw (
                    "Phase 9C catalog GUID drifted for " +
                    "$($_.path): expected $($_.expectedGuid), " +
                    "found $actualGuid"
                )
            }
            if ($actualGuid -ne $beforeGuid) {
                throw (
                    "Phase 9C catalog GUID changed during rebuild for " +
                    "$($_.path): $beforeGuid -> $actualGuid"
                )
            }
            [ordered]@{
                path = $_.path.Replace('\', '/')
                expectedGuid = $_.expectedGuid
                guidBefore = $beforeGuid
                guidAfter = $actualGuid
                unchanged = $true
            }
        }
)

$editModeResult = Join-Path $testDirectory "EditMode-results.xml"
$playModeResult = Join-Path $testDirectory "PlayMode-results.xml"
$protectedEvidence = @(
    $protectedRelativePaths |
        ForEach-Object {
            [ordered]@{
                path = $_.Replace('\', '/')
                sha256Before = $protectedBefore[$_]
                sha256After = $protectedAfter[$_]
                unchanged = $protectedBefore[$_] -eq $protectedAfter[$_]
            }
        }
)
$manifest = [ordered]@{
    version = "0.3.3"
    status = "UNITY_AUTOMATION_PASS"
    generatedAtUtc = [DateTime]::UtcNow.ToString("O")
    unityPath = $resolvedUnityPath
    unityVersion = (
        Get-Content `
            -LiteralPath (
                Join-Path `
                    $resolvedProjectPath `
                    "ProjectSettings\ProjectVersion.txt"
            ) `
            -Encoding UTF8 |
            Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } |
            Select-Object -First 1
    ).Split(':', 2)[1].Trim()
    catalogCandidate = "PresentationSpriteCatalog_LightStorybookProductionV033Batch06.asset"
    catalogIdentities = $catalogIdentityEvidence
    catalogEntryCount = 86
    configuredArtworkCount = 83
    productionArtworkCount = 51
    screenshotCount = $screenshots.Count
    tests = @(
        Get-TestSummary -Platform "EditMode" -Path $editModeResult
        Get-TestSummary -Platform "PlayMode" -Path $playModeResult
    )
    protectedAssets = $protectedEvidence
    evidenceFiles = Get-EvidenceFiles -ReleaseDirectory $releaseDirectory
}
$releaseManifest = Join-Path $releaseDirectory "release-manifest.json"
$manifest |
    ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath $releaseManifest -Encoding UTF8

Write-Host "Phase 9C Unity automated release gate passed."
Write-Host "Evidence: $releaseDirectory"
