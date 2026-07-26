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

Write-Host "Running isolated G4 formal-chain acceptance: $Resolution..."
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
if ($report.completionStatus -ne "AcceptancePassed") {
    throw "G4 report did not pass: $($report.completionStatus) - $($report.completionMessage)"
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
if (-not $report.cleanup.cleanAtCompletion) {
    throw "G4 presentation cleanup gate failed: activeFx=$($report.cleanup.finalActivePresentationFx), activeNonLoopAudio=$($report.cleanup.finalActiveNonLoopingAudioSources), battleAnimation=$($report.cleanup.finalBattleAnimationPlaying)"
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

$requiredCheckpoints = @(
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
    $expectedScreenshotNames = @(
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
            "G4 screenshot set does not exactly match the 16 expected core-chain files. Missing=[{0}] Unexpected=[{1}]" -f
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
    if ($uniqueScreenshotHashes.Count -lt 8) {
        throw "G4 screenshot diversity gate failed: expected at least 8 unique frames, found $($uniqueScreenshotHashes.Count)."
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
$evidenceManifest = [pscustomobject]@{
    schemaVersion = "spire-chess-g4-evidence-v1"
    generatedAtUtc = [DateTime]::UtcNow.ToString("o")
    runId = $safeRunId
    machineName = [Environment]::MachineName
    resolution = $Resolution
    quality = $Quality
    seed = $Seed
    playerPath = $resolvedPlayerPath
    playerSha256 = $playerHash
    buildId = $buildManifest.buildId
    gitCommit = $buildManifest.gitCommit
    sourceTreeDirty = [bool]$buildManifest.sourceTreeDirty
    buildManifestSha256 = (
        Get-FileHash -LiteralPath $buildManifestPath -Algorithm SHA256
    ).Hash.ToLowerInvariant()
    performanceReport = $reports[0].Name
    performanceReportSha256 = (
        Get-FileHash -LiteralPath $reports[0].FullName -Algorithm SHA256
    ).Hash.ToLowerInvariant()
    provisionalAudio = [bool]$report.provisional
    sampleCatalogExact = [bool]$report.artwork.catalogExact
    cleanupPassed = [bool]$report.cleanup.cleanAtCompletion
    checkpoints = @($reportedCheckpoints)
    screenshots = $screenshotEvidence
    isolatedSaveFiles = $saveFiles
}
$evidenceManifestPath = Join-Path $runRoot "g4-evidence-manifest.json"
$evidenceManifest |
    ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath $evidenceManifestPath -Encoding UTF8

Write-Host "G4 formal-chain acceptance passed: $Resolution"
Write-Host "Report:      $($reports[0].FullName)"
Write-Host "Manifest:    $evidenceManifestPath"
Write-Host "Screenshots: $evidenceRoot"
Write-Host "Save root:   $saveRoot"
Write-Host "Log:         $logPath"
