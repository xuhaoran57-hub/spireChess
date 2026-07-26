[CmdletBinding()]
param(
    [string]$PlayerPath = "",

    [string]$OutputDirectory =
        (Join-Path $PSScriptRoot "..\sc\Logs\G4\Acceptance"),

    [ValidateSet("1920x1080", "1920x1200")]
    [string]$Resolution = "1920x1080",

    [string]$Quality = "High",

    [ValidateRange(1, 2147483647)]
    [int]$Seed = 940101,

    [string]$RunId = "",

    [switch]$NoScreenshots,

    [switch]$FrozenVisual,

    [switch]$Stress,

    [switch]$AllowDirtyProbe,

    [ValidateRange(5, 300)]
    [int]$StartupTimeoutSeconds = 30,

    [ValidateRange(5, 60)]
    [int]$HeartbeatSeconds = 10,

    [ValidateRange(30, 1800)]
    [int]$NoProgressTimeoutSeconds = 60,

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

function Measure-ScreenshotContent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    Add-Type -AssemblyName System.Drawing
    $bitmap = [System.Drawing.Bitmap]::new($Path)
    try {
        [int]$sampleColumns = 64
        [int]$sampleRows = 36
        $sampleCount = 0
        $brightSampleCount = 0
        $minimumLuminance = [double]::PositiveInfinity
        $maximumLuminance = [double]::NegativeInfinity
        for ($column = 0; $column -lt $sampleColumns; $column++) {
            $x = [math]::Min(
                $bitmap.Width - 1,
                [int](($column + 0.5) * $bitmap.Width / $sampleColumns))
            for ($row = 0; $row -lt $sampleRows; $row++) {
                $y = [math]::Min(
                    $bitmap.Height - 1,
                    [int](($row + 0.5) * $bitmap.Height / $sampleRows))
                $pixel = $bitmap.GetPixel($x, $y)
                $luminance =
                    (0.2126 * $pixel.R) +
                    (0.7152 * $pixel.G) +
                    (0.0722 * $pixel.B)
                $minimumLuminance =
                    [math]::Min($minimumLuminance, $luminance)
                $maximumLuminance =
                    [math]::Max($maximumLuminance, $luminance)
                if ($luminance -gt 12) {
                    $brightSampleCount++
                }
                $sampleCount++
            }
        }

        return [pscustomobject]@{
            width = $bitmap.Width
            height = $bitmap.Height
            brightSampleRatio = if ($sampleCount -eq 0) {
                0
            } else {
                $brightSampleCount / $sampleCount
            }
            luminanceRange = $maximumLuminance - $minimumLuminance
        }
    } finally {
        $bitmap.Dispose()
    }
}

function Stop-MonitoredProcess {
    param(
        [System.Diagnostics.Process]$Process,
        [string]$Description
    )

    if ($null -eq $Process) {
        return
    }
    $Process.Refresh()
    if ($Process.HasExited) {
        return
    }
    try {
        $Process.Kill()
    } catch {
        $Process.Refresh()
        if (-not $Process.HasExited) {
            throw "Failed to stop $Description (PID $($Process.Id)): $($_.Exception.Message)"
        }
        return
    }
    if (-not $Process.WaitForExit(10000)) {
        throw "$Description (PID $($Process.Id)) did not exit within 10 seconds after Kill."
    }
}

function Resolve-NormalizedBuildRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RootDirectory,

        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$RelativePath
    )

    if ([string]::IsNullOrWhiteSpace($RelativePath) -or
        [System.IO.Path]::IsPathRooted($RelativePath)) {
        throw "G4 build manifest contains an invalid relative path: $RelativePath"
    }

    $platformRelativePath = $RelativePath.Replace(
        [char][System.IO.Path]::AltDirectorySeparatorChar,
        [char][System.IO.Path]::DirectorySeparatorChar)
    try {
        $resolvedRoot = [System.IO.Path]::GetFullPath($RootDirectory)
        $resolvedPath = [System.IO.Path]::GetFullPath(
            (Join-Path $resolvedRoot $platformRelativePath))
    } catch {
        throw "G4 build manifest contains an invalid relative path '$RelativePath': $($_.Exception.Message)"
    }

    $rootPrefix =
        $resolvedRoot.TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar) +
        [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolvedPath.StartsWith(
            $rootPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "G4 build manifest relative path escapes the build directory: $RelativePath"
    }

    $normalizedRelativePath =
        $resolvedPath.Substring($rootPrefix.Length).Replace(
            [char][System.IO.Path]::DirectorySeparatorChar,
            [char]'/')
    if ([string]::IsNullOrWhiteSpace($normalizedRelativePath)) {
        throw "G4 build manifest contains an invalid relative path: $RelativePath"
    }

    return [pscustomobject]@{
        FullPath = $resolvedPath
        RelativePath = $normalizedRelativePath
    }
}

function Assert-G4BuildFileManifest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BuildDirectory,

        [Parameter(Mandatory = $true)]
        [string]$ManifestPath,

        [Parameter(Mandatory = $true)]
        [object[]]$ManifestFiles
    )

    if ($ManifestFiles.Count -lt 2) {
        throw "G4 build manifest contains too few build files."
    }

    $resolvedBuildDirectory =
        [System.IO.Path]::GetFullPath($BuildDirectory)
    $resolvedManifestPath = [System.IO.Path]::GetFullPath($ManifestPath)
    $buildDirectoryPrefix =
        $resolvedBuildDirectory.TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar) +
        [System.IO.Path]::DirectorySeparatorChar
    $manifestRelativePaths =
        [System.Collections.Generic.HashSet[string]]::new(
            [System.StringComparer]::OrdinalIgnoreCase)
    $normalizedEntries = @()

    foreach ($entry in $ManifestFiles) {
        $resolvedEntry = Resolve-NormalizedBuildRelativePath `
            -RootDirectory $resolvedBuildDirectory `
            -RelativePath ([string]$entry.relativePath)
        if ([string]::Equals(
                $resolvedEntry.FullPath,
                $resolvedManifestPath,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "G4 build manifest must not list itself as a build file."
        }
        if (-not $manifestRelativePaths.Add(
                $resolvedEntry.RelativePath)) {
            throw "G4 build manifest contains a duplicate normalized relative path: $($resolvedEntry.RelativePath)"
        }

        [long]$expectedSize = 0L
        if (-not [long]::TryParse(
                [string]$entry.sizeBytes,
                [System.Globalization.NumberStyles]::Integer,
                [System.Globalization.CultureInfo]::InvariantCulture,
                [ref]$expectedSize) -or
            $expectedSize -lt 0L) {
            throw "G4 build manifest contains an invalid file size: $($resolvedEntry.RelativePath)"
        }
        $expectedHash = ([string]$entry.sha256).ToLowerInvariant()
        if ($expectedHash -notmatch '^[0-9a-f]{64}$') {
            throw "G4 build manifest contains an invalid SHA-256: $($resolvedEntry.RelativePath)"
        }

        $normalizedEntries += [pscustomobject]@{
            FullPath = $resolvedEntry.FullPath
            RelativePath = $resolvedEntry.RelativePath
            SizeBytes = $expectedSize
            Sha256 = $expectedHash
        }
    }

    $actualRelativePaths =
        [System.Collections.Generic.HashSet[string]]::new(
            [System.StringComparer]::OrdinalIgnoreCase)
    $actualFiles = @(
        Get-ChildItem `
            -LiteralPath $resolvedBuildDirectory `
            -File `
            -Recurse `
            -Force |
        Where-Object {
            -not [string]::Equals(
                [System.IO.Path]::GetFullPath($_.FullName),
                $resolvedManifestPath,
                [System.StringComparison]::OrdinalIgnoreCase)
        }
    )
    foreach ($actualFile in $actualFiles) {
        $actualFullPath =
            [System.IO.Path]::GetFullPath($actualFile.FullName)
        if (-not $actualFullPath.StartsWith(
                $buildDirectoryPrefix,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "G4 build directory enumeration escaped its root: $actualFullPath"
        }
        $actualRelativePath = (
            Resolve-NormalizedBuildRelativePath `
                -RootDirectory $resolvedBuildDirectory `
                -RelativePath $actualFullPath.Substring(
                    $buildDirectoryPrefix.Length)
        ).RelativePath
        if (-not $actualRelativePaths.Add($actualRelativePath)) {
            throw "G4 build directory contains duplicate normalized relative paths: $actualRelativePath"
        }
    }

    $missingFiles = @(
        $manifestRelativePaths |
        Where-Object { -not $actualRelativePaths.Contains($_) } |
        Sort-Object
    )
    $unexpectedFiles = @(
        $actualRelativePaths |
        Where-Object { -not $manifestRelativePaths.Contains($_) } |
        Sort-Object
    )
    if ($missingFiles.Count -gt 0 -or $unexpectedFiles.Count -gt 0) {
        throw (
            "G4 build file set does not exactly match its manifest. Missing=[{0}] Unexpected=[{1}]" -f
                ($missingFiles -join ", "),
                ($unexpectedFiles -join ", "))
    }

    foreach ($entry in $normalizedEntries) {
        if (-not (Test-Path -LiteralPath $entry.FullPath -PathType Leaf)) {
            throw "G4 build file is missing: $($entry.RelativePath)"
        }
        $actualSize = (Get-Item -LiteralPath $entry.FullPath).Length
        if ($actualSize -ne $entry.SizeBytes) {
            throw "G4 build file size verification failed: $($entry.RelativePath)"
        }
        $actualHash = (
            Get-FileHash -LiteralPath $entry.FullPath -Algorithm SHA256
        ).Hash.ToLowerInvariant()
        if ($actualHash -ne $entry.Sha256) {
            throw "G4 build file SHA-256 verification failed: $($entry.RelativePath)"
        }
    }

    return [pscustomobject]@{
        Entries = $normalizedEntries
        RelativePaths = $manifestRelativePaths
    }
}

if ([string]::IsNullOrWhiteSpace($PlayerPath)) {
    $buildRoot = [System.IO.Path]::GetFullPath(
        (Join-Path $PSScriptRoot "..\sc\Builds\G4"))
    $latestManifest = Get-ChildItem `
        -LiteralPath $buildRoot `
        -Filter "g4-build-manifest.json" `
        -File `
        -Recurse `
        -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
    if ($null -eq $latestManifest) {
        throw "No G4 build manifest was found under $buildRoot. Run build_g4_windows.ps1 first or pass -PlayerPath."
    }
    $latestBuild = Get-Content `
        -LiteralPath $latestManifest.FullName `
        -Encoding UTF8 `
        -Raw |
        ConvertFrom-Json
    $latestExecutableName =
        [System.IO.Path]::GetFileName([string]$latestBuild.outputPath)
    if ([string]::IsNullOrWhiteSpace($latestExecutableName)) {
        throw "Latest G4 build manifest contains an invalid outputPath."
    }
    $PlayerPath = Join-Path `
        $latestManifest.Directory.FullName `
        $latestExecutableName
}
$resolvedPlayerPath = [System.IO.Path]::GetFullPath($PlayerPath)
if (-not (Test-Path -LiteralPath $resolvedPlayerPath -PathType Leaf)) {
    throw "G4 Player not found: $resolvedPlayerPath"
}

$buildDirectory =
    [System.IO.Path]::GetDirectoryName($resolvedPlayerPath)
$buildManifestPath = Join-Path $buildDirectory "g4-build-manifest.json"
if (-not (Test-Path -LiteralPath $buildManifestPath -PathType Leaf)) {
    throw "G4 build manifest is missing beside Player: $buildManifestPath"
}
$buildManifest =
    Get-Content -LiteralPath $buildManifestPath -Encoding UTF8 -Raw |
    ConvertFrom-Json
if ([bool]$buildManifest.sourceTreeDirty -and -not $AllowDirtyProbe) {
    throw (
        "G4 formal acceptance refuses a Player built from a dirty source " +
        "tree. Rebuild from a clean candidate, or pass -AllowDirtyProbe " +
        "for non-formal diagnosis only.")
}
$manifestExecutableName =
    [System.IO.Path]::GetFileName([string]$buildManifest.outputPath)
if ([string]::IsNullOrWhiteSpace($manifestExecutableName) -or
    -not [string]::Equals(
        $manifestExecutableName,
        [System.IO.Path]::GetFileName($resolvedPlayerPath),
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "G4 Player filename does not match the executable named by the build manifest."
}
$playerHash = (Get-FileHash `
    -LiteralPath $resolvedPlayerPath `
    -Algorithm SHA256).Hash.ToLowerInvariant()
if ($buildManifest.executableSha256 -ne $playerHash) {
    throw "G4 Player SHA-256 does not match its build manifest."
}
$manifestFiles = @($buildManifest.buildFiles)
$verifiedBuildFiles = Assert-G4BuildFileManifest `
    -BuildDirectory $buildDirectory `
    -ManifestPath $buildManifestPath `
    -ManifestFiles $manifestFiles
$manifestExecutableRelativePath = (
    Resolve-NormalizedBuildRelativePath `
        -RootDirectory $buildDirectory `
        -RelativePath $manifestExecutableName
).RelativePath
if (-not $verifiedBuildFiles.RelativePaths.Contains(
        $manifestExecutableRelativePath)) {
    throw "G4 build manifest does not list its executable: $manifestExecutableRelativePath"
}

if ($Resolution -notmatch '^(\d+)x(\d+)$') {
    throw "Invalid resolution: $Resolution"
}
$width = [int]$Matches[1]
$height = [int]$Matches[2]

if ([string]::IsNullOrWhiteSpace($RunId)) {
    $machine = [Environment]::MachineName -replace '[^A-Za-z0-9_.-]', '-'
    $RunId = "{0}-{1}-{2}" -f
        ([DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")),
        $machine,
        $Resolution
}
$safeRunId = $RunId -replace '[^A-Za-z0-9_.-]', '-'
$resolvedOutputDirectory =
    [System.IO.Path]::GetFullPath($OutputDirectory)
if ($safeRunId -eq "." -or $safeRunId -eq "..") {
    throw "RunId cannot be a dot segment."
}
$runRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $resolvedOutputDirectory $safeRunId))
$outputPrefix =
    $resolvedOutputDirectory.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) +
    [System.IO.Path]::DirectorySeparatorChar
if (-not $runRoot.StartsWith(
        $outputPrefix,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Resolved G4 run directory escapes OutputDirectory: $runRoot"
}
if (Test-Path -LiteralPath $runRoot) {
    $existing = Get-ChildItem -LiteralPath $runRoot -Force |
        Select-Object -First 1
    if ($null -ne $existing) {
        throw "Refusing to reuse non-empty G4 run directory: $runRoot"
    }
}

$saveRoot = Join-Path $runRoot "isolated-save"
$performanceRoot = Join-Path $runRoot "performance"
$evidenceRoot = Join-Path $runRoot "screenshots"
New-Item -ItemType Directory -Path $saveRoot -Force | Out-Null
New-Item -ItemType Directory -Path $performanceRoot -Force | Out-Null
New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
$isolationMarker = Join-Path $saveRoot ".spirechess-g4-isolated-save"
[System.IO.File]::WriteAllText(
    $isolationMarker,
    "spire-chess-g4-isolated-save-v1",
    [System.Text.Encoding]::ASCII)
$logPath = Join-Path $runRoot "player.log"

$arguments = @(
    "-screen-fullscreen", "0",
    "-screen-width", $width,
    "-screen-height", $height,
    "-logFile", ('"{0}"' -f $logPath),
    "-g4Acceptance",
    "-g4Perf",
    "-g4PerfAutoQuit",
    "-g4RunId", ('"{0}"' -f $safeRunId),
    "-g4Resolution", $Resolution,
    "-g4Quality", ('"{0}"' -f $Quality),
    "-g4AcceptanceSeed", $Seed,
    "-g4PerfWarmup", "1",
    "-g4PerfSampleInterval", "0.25",
    "-g4PerfOutput", ('"{0}"' -f $performanceRoot),
    "-g4EvidenceOutput", ('"{0}"' -f $evidenceRoot),
    "-g4SaveRoot", ('"{0}"' -f $saveRoot)
)
if ($NoScreenshots) {
    $arguments += "-g4NoScreenshots"
}
if ($FrozenVisual) {
    $arguments += "-g4FrozenVisual"
}
if ($Stress) {
    $arguments += "-g4Stress"
}

$acceptanceMode = if ($Stress) {
    "stress"
} elseif ($FrozenVisual) {
    "frozen-visual"
} else {
    "core"
}
Write-Host "Running isolated G4 $acceptanceMode acceptance: $Resolution..."
$startProcessArguments = @{
    FilePath = $resolvedPlayerPath
    ArgumentList = $arguments
    PassThru = $true
}
$process = $null
try {
    $process = Start-Process @startProcessArguments
    $startedAt = [DateTime]::UtcNow
    $lastHeartbeatAt = $startedAt
    $lastProgressAt = $startedAt
    $lastLogBytes = 0L
    $lastCpuSeconds = 0d
    $startupObserved = $false
    while (-not $process.HasExited) {
        Start-Sleep -Milliseconds 250
        $now = [DateTime]::UtcNow
        $elapsedSeconds = ($now - $startedAt).TotalSeconds
        $logBytes = 0L
        if (Test-Path -LiteralPath $logPath -PathType Leaf) {
            $logBytes = (Get-Item -LiteralPath $logPath).Length
            $startupObserved = $startupObserved -or $logBytes -gt 0
        }
        $process.Refresh()
        $cpuSeconds = $process.TotalProcessorTime.TotalSeconds
        if ($logBytes -gt $lastLogBytes -or
            $cpuSeconds -gt $lastCpuSeconds + 0.01d) {
            $lastProgressAt = $now
            $lastLogBytes = $logBytes
            $lastCpuSeconds = $cpuSeconds
        }
        if (-not $startupObserved -and
            $elapsedSeconds -ge $StartupTimeoutSeconds) {
            Stop-MonitoredProcess `
                -Process $process `
                -Description "G4 Player"
            throw "G4 Player did not create a non-empty log within $StartupTimeoutSeconds seconds. The Player likely stalled before initialization. Log: $logPath"
        }
        if ($startupObserved -and
            ($now - $lastProgressAt).TotalSeconds -ge
                $NoProgressTimeoutSeconds) {
            Stop-MonitoredProcess `
                -Process $process `
                -Description "G4 Player"
            throw "G4 Player made no log or CPU progress for $NoProgressTimeoutSeconds seconds. Log: $logPath"
        }
        if (($now - $lastHeartbeatAt).TotalSeconds -ge
            $HeartbeatSeconds) {
            Write-Host (
                "G4 Player heartbeat: elapsed={0:n0}s, cpu={1:n1}s, log={2} bytes" -f
                    $elapsedSeconds,
                    $cpuSeconds,
                    $logBytes)
            $lastHeartbeatAt = $now
        }
        if ($elapsedSeconds -ge $TimeoutSeconds) {
            Stop-MonitoredProcess `
                -Process $process `
                -Description "G4 Player"
            throw "G4 Player timed out after $TimeoutSeconds seconds. Log: $logPath"
        }
    }

    if ($process.ExitCode -ne 0) {
        throw "G4 Player acceptance failed with exit code $($process.ExitCode). Log: $logPath"
    }
} finally {
    Stop-MonitoredProcess `
        -Process $process `
        -Description "G4 Player"
}

$runtimeFailureMarkerPath = Join-Path `
    $performanceRoot `
    "g4-runtime-failures.log"
if (Test-Path -LiteralPath $runtimeFailureMarkerPath -PathType Leaf) {
    $runtimeFailurePreview = @(
        Get-Content `
            -LiteralPath $runtimeFailureMarkerPath `
            -Encoding UTF8 `
            -TotalCount 5
    ) -join " | "
    throw (
        "G4 Player recorded a runtime failure after launch. " +
        "Marker: $runtimeFailureMarkerPath. " +
        "First failures: $runtimeFailurePreview")
}

$reports = @(
    Get-ChildItem `
        -LiteralPath $performanceRoot `
        -Filter "g4-performance-*.json" `
        -File
)
if ($reports.Count -ne 1) {
    throw "Expected exactly one G4 JSON report; found $($reports.Count) in $performanceRoot"
}
$report = Get-Content -LiteralPath $reports[0].FullName -Encoding UTF8 -Raw |
    ConvertFrom-Json
if ($report.schemaVersion -ne "spire-chess-g4-performance-v2") {
    throw "Unsupported G4 performance report schema: '$($report.schemaVersion)'."
}
if ($report.completionStatus -ne "AcceptancePassed") {
    throw "G4 report did not pass: $($report.completionStatus) - $($report.completionMessage)"
}
if (-not [string]::Equals(
        [string]$report.runId,
        $safeRunId,
        [System.StringComparison]::Ordinal)) {
    throw "G4 report runId '$($report.runId)' does not match requested runId '$safeRunId'."
}
if (-not [string]::Equals(
        [string]$report.configuration.acceptanceSeed,
        [string]$Seed,
        [System.StringComparison]::Ordinal)) {
    throw "G4 report seed '$($report.configuration.acceptanceSeed)' does not match requested seed '$Seed'."
}
if ($report.configuration.actualWidth -ne $width -or
    $report.configuration.actualHeight -ne $height) {
    throw "G4 Player resolution mismatch: requested $Resolution, got $($report.configuration.actualWidth)x$($report.configuration.actualHeight)"
}
if ($report.configuration.requestedWidth -ne $width -or
    $report.configuration.requestedHeight -ne $height) {
    throw "G4 report requested-resolution mismatch: expected $Resolution, got $($report.configuration.requestedWidth)x$($report.configuration.requestedHeight)"
}
if ($report.configuration.fullScreenMode -ne "Windowed") {
    throw "G4 Player must run windowed; got '$($report.configuration.fullScreenMode)'."
}
if (-not [string]::Equals(
        [System.IO.Path]::GetFullPath($report.environment.injectedSaveRoot),
        [System.IO.Path]::GetFullPath($saveRoot),
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "G4 report save root does not match the isolated run root."
}
if ($report.configuration.qualityName -ne $Quality) {
    throw "G4 Player quality mismatch: requested '$Quality', got '$($report.configuration.qualityName)'."
}
if ($report.overall.sampleCount -lt 1 -or
    $report.overall.frameTimeMs.sampleCount -lt 1) {
    throw "G4 report contains no usable frame samples."
}
$reportedSamplesCsv = [string]$report.samplesCsvPath
if ([string]::IsNullOrWhiteSpace($reportedSamplesCsv)) {
    throw "G4 report does not identify its raw frame-sample CSV."
}
$resolvedSamplesCsv =
    [System.IO.Path]::GetFullPath($reportedSamplesCsv)
$resolvedPerformanceRoot =
    [System.IO.Path]::GetFullPath($performanceRoot)
$performancePrefix =
    $resolvedPerformanceRoot.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) +
    [System.IO.Path]::DirectorySeparatorChar
if (-not $resolvedSamplesCsv.StartsWith(
        $performancePrefix,
        [System.StringComparison]::OrdinalIgnoreCase) -or
    -not (Test-Path -LiteralPath $resolvedSamplesCsv -PathType Leaf)) {
    throw "G4 raw frame-sample CSV is missing or escapes this run's performance directory."
}
$samplesCsvInfo = Get-Item -LiteralPath $resolvedSamplesCsv
if ($samplesCsvInfo.Length -le 0) {
    throw "G4 raw frame-sample CSV is empty: $resolvedSamplesCsv"
}
$samplesCsvLineCount = 0
$samplesCsvHeader = $null
foreach ($line in [System.IO.File]::ReadLines($resolvedSamplesCsv)) {
    if ($samplesCsvLineCount -eq 0) {
        $samplesCsvHeader = $line
    }
    $samplesCsvLineCount++
}
$expectedSamplesCsvHeader =
    "elapsed_seconds,scene,frame_ms,main_thread_ns," +
    "gc_allocated_bytes,total_used_bytes,gc_used_bytes,texture_bytes," +
    "audio_bytes,active_fx,active_non_loop_audio,battle_animation"
if ($samplesCsvHeader -cne $expectedSamplesCsvHeader -or
    $samplesCsvLineCount -ne ([int]$report.overall.sampleCount + 1)) {
    throw (
        "G4 raw frame-sample CSV does not match the JSON sample count: " +
        "lines=$samplesCsvLineCount, samples=" +
        "$($report.overall.sampleCount).")
}
$samplesCsvHash = (
    Get-FileHash -LiteralPath $resolvedSamplesCsv -Algorithm SHA256
).Hash.ToLowerInvariant()
if (-not $report.cleanup.cleanAtCompletion) {
    throw "G4 presentation cleanup gate failed: activeFx=$($report.cleanup.finalActivePresentationFx), activeNonLoopAudio=$($report.cleanup.finalActiveNonLoopingAudioSources), battleAnimation=$($report.cleanup.finalBattleAnimationPlaying)"
}
if (-not $report.runtimeLogs.clean -or
    $report.runtimeLogs.totalFailureCount -ne 0) {
    throw (
        "G4 runtime log gate failed: errors=" +
        "$($report.runtimeLogs.errorCount), exceptions=" +
        "$($report.runtimeLogs.exceptionCount), asserts=" +
        "$($report.runtimeLogs.assertCount).")
}
if (-not $report.artwork.sampleScopeExact -or
    -not $report.artwork.catalogExact -or
    $report.artwork.catalogExactCount -ne
        $report.artwork.catalogExpectedCount) {
    throw "G4 full sample Sprite Catalog Exact gate failed."
}

$requiredCounters = @(
    "Total Used Memory",
    "GC Used Memory",
    "Texture Memory",
    "GC Allocated In Frame",
    "Main Thread"
)
$unavailableCounters = @($report.unavailableProfilerCounters)
$missingRequiredCounters = @(
    $requiredCounters | Where-Object {
        $unavailableCounters -contains $_
    }
)
if ($missingRequiredCounters.Count -gt 0) {
    throw "G4 required ProfilerRecorder counters are unavailable: $($missingRequiredCounters -join ', ')"
}

$requiredCheckpoints = if ($Stress) {
    @(
        "stress-shop-ten-compact",
        "stress-battle-nested-ready",
        "stress-battle-nested-result",
        "sample-catalog-exact",
        "acceptance-complete"
    )
} elseif ($FrozenVisual) {
    @(
        "main-menu-new-run",
        "run-map-left",
        "run-map-center",
        "run-map-right",
        "shop-entry",
        "shop-refresh",
        "shop-buy-play",
        "shop-target-or-warcry",
        "shop-frozen",
        "shop-unfrozen",
        "shop-upgrade",
        "battle-start",
        "battle-attack-shield",
        "battle-death-summon",
        "battle-result",
        "run-reward",
        "run-returned-map",
        "run-system-menu",
        "run-audio-settings",
        "main-menu-saved-run",
        "continued-run",
        "sample-catalog-exact",
        "acceptance-complete"
    )
} else {
    @(
        "main-menu",
        "run-map",
        "shop",
        "shop-buy-play",
        "shop-frozen",
        "shop-unfrozen",
        "run-after-shop",
        "battle-ready",
        "battle-death-summon",
        "battle-result",
        "run-return",
        "run-map-after-battle",
        "run-system-menu",
        "run-audio-settings",
        "main-menu-continue",
        "continued-run",
        "sample-catalog-exact",
        "acceptance-complete"
    )
}
$reportedCheckpoints = @($report.checkpoints | ForEach-Object {
    [string]$_.checkpoint
})
$missingCheckpoints = @(
    $requiredCheckpoints | Where-Object {
        $reportedCheckpoints -notcontains $_
    }
)
if ($missingCheckpoints.Count -gt 0) {
    throw "G4 report is missing required core-chain checkpoints: $($missingCheckpoints -join ', ')"
}
if (@($report.checkpoints | Where-Object { -not $_.passed }).Count -gt 0) {
    throw "G4 report contains a failed checkpoint."
}

$screenshots = @()
$screenshotEvidence = @()
if (-not $NoScreenshots) {
    $expectedScreenshotNames = if ($Stress) {
        @(
            "01-stress-shop-ten-compact-$Resolution.png",
            "02-stress-battle-nested-ready-$Resolution.png",
            "03-stress-battle-nested-result-$Resolution.png"
        )
    } elseif ($FrozenVisual) {
        @(
            "01-main-menu-new-run-$Resolution.png",
            "02-run-map-left-$Resolution.png",
            "03-run-map-center-$Resolution.png",
            "04-run-map-right-$Resolution.png",
            "05-shop-entry-$Resolution.png",
            "06-shop-refresh-$Resolution.png",
            "07-shop-buy-play-$Resolution.png",
            "08-shop-target-or-warcry-$Resolution.png",
            "09-shop-frozen-$Resolution.png",
            "10-shop-unfrozen-$Resolution.png",
            "11-shop-upgrade-$Resolution.png",
            "12-battle-start-$Resolution.png",
            "13-battle-attack-shield-$Resolution.png",
            "14-battle-death-summon-$Resolution.png",
            "15-battle-result-$Resolution.png",
            "16-run-reward-$Resolution.png",
            "17-run-returned-map-$Resolution.png",
            "18-run-system-menu-$Resolution.png",
            "19-run-audio-settings-$Resolution.png",
            "20-main-menu-saved-run-$Resolution.png",
            "21-continued-run-$Resolution.png"
        )
    } else {
        @(
            "01-main-menu-$Resolution.png",
            "02-run-map-$Resolution.png",
            "03-shop-$Resolution.png",
            "04-shop-buy-play-$Resolution.png",
            "05-shop-frozen-$Resolution.png",
            "06-shop-unfrozen-$Resolution.png",
            "07-run-after-shop-$Resolution.png",
            "08-battle-ready-$Resolution.png",
            "09-battle-death-summon-$Resolution.png",
            "10-battle-result-$Resolution.png",
            "11-run-return-$Resolution.png",
            "12-run-map-after-battle-$Resolution.png",
            "13-run-system-menu-$Resolution.png",
            "14-run-audio-settings-$Resolution.png",
            "15-main-menu-continue-$Resolution.png",
            "16-continued-run-$Resolution.png"
        )
    }
    $screenshots = @(
        Get-ChildItem `
            -LiteralPath $evidenceRoot `
            -Filter "*.png" `
            -File `
            -Recurse
    )
    $resolvedEvidenceRoot =
        [System.IO.Path]::GetFullPath($evidenceRoot)
    $evidenceRootPrefix =
        $resolvedEvidenceRoot.TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar) +
        [System.IO.Path]::DirectorySeparatorChar
    $actualScreenshotNames = @(
        $screenshots |
        ForEach-Object {
            $resolvedScreenshotPath =
                [System.IO.Path]::GetFullPath($_.FullName)
            $resolvedScreenshotPath.Substring(
                $evidenceRootPrefix.Length).Replace(
                [char][System.IO.Path]::DirectorySeparatorChar,
                [char]'/')
        }
    )
    $missingScreenshots = @(
        $expectedScreenshotNames |
        Where-Object { $actualScreenshotNames -notcontains $_ }
    )
    $unexpectedScreenshots = @(
        $actualScreenshotNames |
        Where-Object { $expectedScreenshotNames -notcontains $_ }
    )
    if ($missingScreenshots.Count -gt 0 -or
        $unexpectedScreenshots.Count -gt 0) {
        throw (
            "G4 screenshot set does not exactly match the {0} expected {1} files. Missing=[{2}] Unexpected=[{3}]" -f
                $expectedScreenshotNames.Count,
                $acceptanceMode,
                ($missingScreenshots -join ", "),
                ($unexpectedScreenshots -join ", "))
    }
    $emptyScreenshots = @(
        $screenshots | Where-Object { $_.Length -le 0 }
    )
    if ($emptyScreenshots.Count -gt 0) {
        throw "G4 contains empty screenshot files: $($emptyScreenshots.Name -join ', ')"
    }

    $screenshotEvidence = @(
        $screenshots |
            Sort-Object Name |
            ForEach-Object {
                $content = Measure-ScreenshotContent -Path $_.FullName
                [pscustomobject]@{
                    file = $_.Name
                    bytes = $_.Length
                    width = $content.width
                    height = $content.height
                    sha256 = (
                        Get-FileHash `
                            -LiteralPath $_.FullName `
                            -Algorithm SHA256
                    ).Hash.ToLowerInvariant()
                    brightSampleRatio = $content.brightSampleRatio
                    luminanceRange = $content.luminanceRange
                }
            }
    )
    $wrongSizeScreenshots = @(
        $screenshotEvidence |
        Where-Object {
            $_.width -ne $width -or
            $_.height -ne $height
        }
    )
    if ($wrongSizeScreenshots.Count -gt 0) {
        $wrongSizeDetails = @(
            $wrongSizeScreenshots |
            ForEach-Object {
                "$($_.file)=$($_.width)x$($_.height)"
            }
        )
        throw "G4 screenshot resolution gate failed; expected ${Resolution}: $($wrongSizeDetails -join ', ')"
    }
    $blackScreenshots = @(
        $screenshotEvidence |
            Where-Object {
                $_.brightSampleRatio -lt 0.005 -or
                $_.luminanceRange -lt 20
            }
    )
    if ($blackScreenshots.Count -gt 0) {
        throw "G4 contains black or visually empty screenshots: $($blackScreenshots.file -join ', ')"
    }
    $uniqueScreenshotHashes = @(
        $screenshotEvidence |
            Select-Object -ExpandProperty sha256 -Unique
    )
    $minimumUniqueScreenshotCount = if ($Stress) {
        3
    } else {
        8
    }
    if ($uniqueScreenshotHashes.Count -lt $minimumUniqueScreenshotCount) {
        throw "G4 screenshot diversity gate failed for ${acceptanceMode}: expected at least $minimumUniqueScreenshotCount unique frames, found $($uniqueScreenshotHashes.Count)."
    }
    if ($FrozenVisual) {
        $mapScreenshotHashes = @(
            $screenshotEvidence |
            Where-Object {
                $_.file -match '^0[234]-run-map-(left|center|right)-'
            } |
            Select-Object -ExpandProperty sha256 -Unique
        )
        if ($mapScreenshotHashes.Count -ne 3) {
            throw "G4 frozen map evidence must contain three visually distinct left/center/right frames."
        }
    }
} else {
    $unexpectedScreenshotArtifacts = @(
        Get-ChildItem `
            -LiteralPath $evidenceRoot `
            -File `
            -Recurse `
            -Force
    )
    if ($unexpectedScreenshotArtifacts.Count -gt 0) {
        throw (
            "G4 -NoScreenshots run wrote unexpected screenshot artifacts: " +
            (($unexpectedScreenshotArtifacts |
                ForEach-Object { $_.FullName }) -join ", "))
    }
}

$saveFiles = @(
    Get-ChildItem -LiteralPath $saveRoot -File |
        Where-Object { $_.Name -ne ".spirechess-g4-isolated-save" } |
        Sort-Object Name |
        ForEach-Object {
            [pscustomobject]@{
                file = $_.Name
                bytes = $_.Length
                sha256 = (
                    Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
                ).Hash.ToLowerInvariant()
            }
        }
)
$playerLogInfo = Get-Item -LiteralPath $logPath
$playerLogHash = (
    Get-FileHash -LiteralPath $logPath -Algorithm SHA256
).Hash.ToLowerInvariant()
$evidenceManifest = [pscustomobject]@{
    schemaVersion = "spire-chess-g4-evidence-v2"
    generatedAtUtc = [DateTime]::UtcNow.ToString("o")
    runId = $safeRunId
    machineName = [Environment]::MachineName
    resolution = $Resolution
    quality = $Quality
    seed = $Seed
    acceptanceMode = $acceptanceMode
    playerPath = $resolvedPlayerPath
    playerSha256 = $playerHash
    playerLog = $playerLogInfo.Name
    playerLogSha256 = $playerLogHash
    playerLogBytes = $playerLogInfo.Length
    buildId = $buildManifest.buildId
    gitCommit = $buildManifest.gitCommit
    sourceTreeDirty = [bool]$buildManifest.sourceTreeDirty
    evidenceClassification = if ([bool]$buildManifest.sourceTreeDirty) {
        "DirtyProbe"
    } else {
        "FormalCandidate"
    }
    buildManifestSha256 = (
        Get-FileHash -LiteralPath $buildManifestPath -Algorithm SHA256
    ).Hash.ToLowerInvariant()
    performanceReport = $reports[0].Name
    performanceReportSha256 = (
        Get-FileHash -LiteralPath $reports[0].FullName -Algorithm SHA256
    ).Hash.ToLowerInvariant()
    samplesCsv = $samplesCsvInfo.Name
    samplesCsvSha256 = $samplesCsvHash
    samplesCsvBytes = $samplesCsvInfo.Length
    samplesCsvLineCount = $samplesCsvLineCount
    samplesCsvSampleCount = [int]$report.overall.sampleCount
    provisionalAudio = [bool]$report.provisional
    sampleCatalogExact = [bool]$report.artwork.catalogExact
    cleanupPassed = [bool]$report.cleanup.cleanAtCompletion
    runtimeLogGatePassed = [bool]$report.runtimeLogs.clean
    runtimeFailureLogCount =
        [int]$report.runtimeLogs.totalFailureCount
    runtimeFailureMarkerPresent = $false
    checkpoints = @($reportedCheckpoints)
    screenshots = $screenshotEvidence
    isolatedSaveFiles = $saveFiles
}
$evidenceManifestPath = Join-Path $runRoot "g4-evidence-manifest.json"
$evidenceManifest |
    ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath $evidenceManifestPath -Encoding UTF8

if ([bool]$buildManifest.sourceTreeDirty) {
    Write-Host "G4 $acceptanceMode dirty probe passed: $Resolution"
} else {
    Write-Host "G4 $acceptanceMode acceptance passed: $Resolution"
}
Write-Host "Report:      $($reports[0].FullName)"
Write-Host "Manifest:    $evidenceManifestPath"
Write-Host "Screenshots: $evidenceRoot"
Write-Host "Save root:   $saveRoot"
Write-Host "Log:         $logPath"
