[CmdletBinding()]
param(
    [string]$UnityPath = $env:UNITY_EXE,

    [string]$ProjectPath = (Join-Path $PSScriptRoot "..\sc"),

    [string]$BuildId =
        ([DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")),

    [string]$OutputPath = "",

    [switch]$CleanBuild,

    [ValidateRange(5, 300)]
    [int]$StartupTimeoutSeconds = 45,

    [ValidateRange(5, 60)]
    [int]$HeartbeatSeconds = 10,

    [ValidateRange(30, 1800)]
    [int]$NoProgressTimeoutSeconds = 180,

    [ValidateRange(300, 7200)]
    [int]$TimeoutSeconds = 3600
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

    $versionFile =
        Join-Path $ResolvedProjectPath "ProjectSettings\ProjectVersion.txt"
    if (-not (Test-Path -LiteralPath $versionFile -PathType Leaf)) {
        throw "Unity project version file not found: $versionFile"
    }

    $versionLine = Get-Content -LiteralPath $versionFile -Encoding UTF8 |
        Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } |
        Select-Object -First 1
    if ($null -eq $versionLine -or
        $versionLine -notmatch '^m_EditorVersion:\s*(.+)$') {
        throw "Unable to read m_EditorVersion from $versionFile"
    }

    $version = $Matches[1].Trim()
    $programFiles = [Environment]::GetFolderPath("ProgramFiles")
    $resolved =
        Join-Path $programFiles "Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
        throw "Unity $version was not found at $resolved. Set UNITY_EXE or pass -UnityPath."
    }
    return $resolved
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

$resolvedProjectPath = [System.IO.Path]::GetFullPath($ProjectPath)
if (-not (Test-Path -LiteralPath $resolvedProjectPath -PathType Container)) {
    throw "Unity project not found: $resolvedProjectPath"
}
if ($BuildId -notmatch '^[A-Za-z0-9][A-Za-z0-9_.-]*$' -or
    $BuildId -eq "." -or
    $BuildId -eq "..") {
    throw "BuildId contains unsafe characters: $BuildId"
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path `
        $resolvedProjectPath `
        "Builds\G4\$BuildId\Windows-x64\SpireChess.exe"
}
$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$resolvedOutputDirectory =
    [System.IO.Path]::GetDirectoryName($resolvedOutputPath)
if (Test-Path -LiteralPath $resolvedOutputDirectory -PathType Container) {
    $existingOutput = Get-ChildItem `
        -LiteralPath $resolvedOutputDirectory `
        -Force |
        Select-Object -First 1
    if ($null -ne $existingOutput) {
        throw "Refusing to reuse non-empty G4 build directory: $resolvedOutputDirectory"
    }
}
New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force |
    Out-Null

$resolvedUnityPath = Resolve-UnityPath `
    -RequestedPath $UnityPath `
    -ResolvedProjectPath $resolvedProjectPath
$repositoryRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $resolvedProjectPath ".."))
$gitCommit = ""
$gitDirty = $true
try {
    $gitCommit = (& git -C $repositoryRoot rev-parse HEAD 2>$null).Trim()
    $gitStatus = @(
        & git -C $repositoryRoot status --porcelain=v1 --untracked-files=all
    )
    $gitDirty = $gitStatus.Count -gt 0
} catch {
    Write-Warning "Unable to resolve complete Git identity; manifest will mark the source tree dirty."
}

$logDirectory = Join-Path $resolvedProjectPath "Logs\G4"
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logPath = Join-Path $logDirectory "G4-Windows-Build-$BuildId.log"
if (Test-Path -LiteralPath $logPath) {
    throw "Refusing to reuse an existing G4 build log: $logPath"
}

$arguments = @(
    "-batchmode",
    "-nographics",
    "-projectPath", ('"{0}"' -f $resolvedProjectPath),
    "-executeMethod",
    "SpireChess.Editor.G4WindowsBuildPipeline.BuildDevelopmentPlayerFromCommandLine",
    "-g4BuildOutput", ('"{0}"' -f $resolvedOutputPath),
    "-g4BuildId", ('"{0}"' -f $BuildId),
    "-g4GitDirty", $gitDirty.ToString().ToLowerInvariant(),
    "-logFile", ('"{0}"' -f $logPath)
)
if (-not [string]::IsNullOrWhiteSpace($gitCommit)) {
    $arguments += @("-g4GitCommit", $gitCommit)
}
if ($CleanBuild) {
    $arguments += "-g4CleanBuild"
}

Write-Host "Building isolated G4 Windows x64 Development Player..."
$process = $null
try {
    $process = Start-Process `
        -FilePath $resolvedUnityPath `
        -ArgumentList $arguments `
        -WindowStyle Hidden `
        -PassThru
    $startedAt = [DateTime]::UtcNow
    $lastHeartbeatAt = $startedAt
    $lastProgressAt = $startedAt
    $lastLogBytes = 0L
    $lastCpuSeconds = 0d
    $startupObserved = $false
    while (-not $process.HasExited) {
        Start-Sleep -Milliseconds 500
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
                -Description "G4 Unity build"
            throw "Unity did not create a non-empty build log within $StartupTimeoutSeconds seconds. The editor likely stalled before project load or license initialization. Log: $logPath"
        }
        if ($startupObserved -and
            ($now - $lastProgressAt).TotalSeconds -ge
                $NoProgressTimeoutSeconds) {
            Stop-MonitoredProcess `
                -Process $process `
                -Description "G4 Unity build"
            throw "G4 Windows build made no log or CPU progress for $NoProgressTimeoutSeconds seconds. Log: $logPath"
        }
        if (($now - $lastHeartbeatAt).TotalSeconds -ge
            $HeartbeatSeconds) {
            Write-Host (
                "G4 build heartbeat: elapsed={0:n0}s, cpu={1:n1}s, log={2} bytes" -f
                    $elapsedSeconds,
                    $cpuSeconds,
                    $logBytes)
            $lastHeartbeatAt = $now
        }
        if ($elapsedSeconds -ge $TimeoutSeconds) {
            Stop-MonitoredProcess `
                -Process $process `
                -Description "G4 Unity build"
            throw "G4 Windows build timed out after $TimeoutSeconds seconds. Log: $logPath"
        }
    }

    if ($process.ExitCode -ne 0) {
        throw "G4 Windows build failed with exit code $($process.ExitCode). Log: $logPath"
    }
} finally {
    Stop-MonitoredProcess `
        -Process $process `
        -Description "G4 Unity build"
}
if (-not (Test-Path -LiteralPath $resolvedOutputPath -PathType Leaf)) {
    throw "G4 Windows build produced no executable: $resolvedOutputPath"
}

$manifestPath =
    Join-Path $resolvedOutputDirectory "g4-build-manifest.json"
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "G4 Windows build produced no manifest: $manifestPath"
}
$manifest = Get-Content `
    -LiteralPath $manifestPath `
    -Encoding UTF8 `
    -Raw |
    ConvertFrom-Json
if ($manifest.buildId -ne $BuildId) {
    throw "G4 build manifest BuildId mismatch."
}
if ($manifest.gitCommit -ne $gitCommit) {
    throw "G4 build manifest Git commit mismatch."
}
if ([bool]$manifest.sourceTreeDirty -ne $gitDirty) {
    throw "G4 build manifest dirty-state mismatch."
}
if (-not [string]::Equals(
        [System.IO.Path]::GetFullPath($manifest.outputPath),
        $resolvedOutputPath,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "G4 build manifest output path mismatch."
}
$executableHash = (
    Get-FileHash -LiteralPath $resolvedOutputPath -Algorithm SHA256
).Hash.ToLowerInvariant()
if ($manifest.executableSha256 -ne $executableHash) {
    throw "G4 build executable SHA-256 verification failed."
}
$manifestFiles = @($manifest.buildFiles)
$verifiedBuildFiles = Assert-G4BuildFileManifest `
    -BuildDirectory $resolvedOutputDirectory `
    -ManifestPath $manifestPath `
    -ManifestFiles $manifestFiles
$executableRelativePath = (
    Resolve-NormalizedBuildRelativePath `
        -RootDirectory $resolvedOutputDirectory `
        -RelativePath ([System.IO.Path]::GetFileName($resolvedOutputPath))
).RelativePath
if (-not $verifiedBuildFiles.RelativePaths.Contains(
        $executableRelativePath)) {
    throw "G4 build manifest does not list its executable: $executableRelativePath"
}

Write-Host "G4 Windows x64 Development Player built."
Write-Host "Executable: $resolvedOutputPath"
Write-Host "SHA-256:    $executableHash"
Write-Host "Git dirty:  $gitDirty"
Write-Host "Manifest:   $manifestPath"
Write-Host "Log:        $logPath"
