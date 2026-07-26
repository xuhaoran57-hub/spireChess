[CmdletBinding()]
param(
    [ValidateSet("All", "EditMode", "PlayMode")]
    [string]$Platform = "All",

    [string]$UnityPath = $env:UNITY_EXE,

    [string]$ProjectPath = (Join-Path $PSScriptRoot "..\sc"),

    [string]$ResultsDirectory = (Join-Path $PSScriptRoot "..\sc\Logs\TestResults"),

    [ValidateRange(60, 7200)]
    [int]$TimeoutSeconds = 900,

    [ValidateRange(5, 300)]
    [int]$StartupTimeoutSeconds = 30,

    [ValidateRange(5, 60)]
    [int]$HeartbeatSeconds = 10,

    [ValidateRange(30, 1800)]
    [int]$NoProgressTimeoutSeconds = 180,

    [ValidateRange(5, 300)]
    [int]$ShutdownGraceSeconds = 30
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

    $versionFile = Join-Path $ResolvedProjectPath "ProjectSettings\ProjectVersion.txt"
    if (-not (Test-Path -LiteralPath $versionFile -PathType Leaf)) {
        throw "Unity project version file not found: $versionFile"
    }

    $versionLine = Get-Content -LiteralPath $versionFile -Encoding UTF8 |
        Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } |
        Select-Object -First 1
    if ($null -eq $versionLine -or $versionLine -notmatch '^m_EditorVersion:\s*(.+)$') {
        throw "Unable to read m_EditorVersion from $versionFile"
    }

    $version = $Matches[1].Trim()
    $programFiles = [Environment]::GetFolderPath('ProgramFiles')
    $resolved = Join-Path $programFiles "Unity\Hub\Editor\$version\Editor\Unity.exe"
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

function Get-RequiredTestRunIntegerAttribute {
    param(
        [System.Xml.XmlElement]$TestRun,
        [string[]]$AttributeNames
    )

    foreach ($attributeName in $AttributeNames) {
        if (-not $TestRun.HasAttribute($attributeName)) {
            continue
        }
        $rawValue = $TestRun.GetAttribute($attributeName)
        $parsedValue = 0
        if (-not [int]::TryParse(
                $rawValue,
                [System.Globalization.NumberStyles]::Integer,
                [System.Globalization.CultureInfo]::InvariantCulture,
                [ref]$parsedValue) -or
            $parsedValue -lt 0) {
            throw "Unity test-run attribute '$attributeName' is not a non-negative integer: '$rawValue'."
        }
        return $parsedValue
    }

    throw "Unity test-run element is missing required attribute '$($AttributeNames -join "' or '")'."
}

function New-UnityTestSummary {
    param(
        [System.Xml.XmlElement]$TestRun,
        [string]$TestPlatform,
        [bool]$ForcedShutdown,
        [string]$ResultPath,
        [string]$LogPath
    )

    if ($null -eq $TestRun) {
        throw "Unity $TestPlatform result has no test-run element: $ResultPath"
    }

    return [pscustomobject]@{
        Platform = $TestPlatform
        Result = [string]$TestRun.GetAttribute("result")
        Total = Get-RequiredTestRunIntegerAttribute `
            -TestRun $TestRun `
            -AttributeNames @("total", "testcasecount", "test-case-count")
        Passed = Get-RequiredTestRunIntegerAttribute `
            -TestRun $TestRun `
            -AttributeNames @("passed")
        Failed = Get-RequiredTestRunIntegerAttribute `
            -TestRun $TestRun `
            -AttributeNames @("failed")
        Skipped = Get-RequiredTestRunIntegerAttribute `
            -TestRun $TestRun `
            -AttributeNames @("skipped")
        Inconclusive = Get-RequiredTestRunIntegerAttribute `
            -TestRun $TestRun `
            -AttributeNames @("inconclusive")
        DurationSeconds = [math]::Round(
            [double]$TestRun.GetAttribute("duration"),
            3)
        ForcedShutdown = $ForcedShutdown
        ResultPath = $ResultPath
        LogPath = $LogPath
    }
}

function Assert-UnityTestSummaryPassed {
    param(
        [object]$Summary
    )

    $failures = @()
    if ($Summary.Result -ne "Passed") {
        $failures += "result=$($Summary.Result)"
    }
    if ($Summary.Total -le 0) {
        $failures += "total=$($Summary.Total) (must be greater than zero)"
    }
    if ($Summary.Passed -ne $Summary.Total) {
        $failures += "passed=$($Summary.Passed) (expected $($Summary.Total))"
    }
    if ($Summary.Failed -ne 0) {
        $failures += "failed=$($Summary.Failed)"
    }
    if ($Summary.Skipped -ne 0) {
        $failures += "skipped=$($Summary.Skipped)"
    }
    if ($Summary.Inconclusive -ne 0) {
        $failures += "inconclusive=$($Summary.Inconclusive)"
    }
    if ($Summary.ForcedShutdown) {
        $failures += "forcedShutdown=true"
    }
    if ($failures.Count -gt 0) {
        throw "Unity $($Summary.Platform) test gate failed: $($failures -join '; ')."
    }
}

function Invoke-UnityTestPlatform {
    param(
        [string]$ResolvedUnityPath,
        [string]$ResolvedProjectPath,
        [string]$ResolvedResultsDirectory,
        [ValidateSet("EditMode", "PlayMode")]
        [string]$TestPlatform,
        [int]$TestTimeoutSeconds,
        [int]$TestStartupTimeoutSeconds,
        [int]$TestHeartbeatSeconds,
        [int]$TestNoProgressTimeoutSeconds,
        [int]$TestShutdownGraceSeconds
    )

    $resultPath = Join-Path $ResolvedResultsDirectory "$TestPlatform-results.xml"
    $logPath = Join-Path $ResolvedResultsDirectory "$TestPlatform.log"
    Remove-Item -LiteralPath $resultPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

    $arguments = @(
        "-batchmode",
        "-nographics",
        "-projectPath", ('"{0}"' -f $ResolvedProjectPath),
        "-runTests",
        "-testPlatform", $TestPlatform,
        "-testResults", ('"{0}"' -f $resultPath),
        "-logFile", ('"{0}"' -f $logPath)
    )

    Write-Host "Running Unity $TestPlatform tests..."
    $process = $null
    $forcedShutdown = $false
    try {
        $process = Start-Process `
            -FilePath $ResolvedUnityPath `
            -ArgumentList $arguments `
            -WindowStyle Hidden `
            -PassThru

        $startedAt = [DateTime]::UtcNow
        $lastHeartbeatAt = $startedAt
        $lastProgressAt = $startedAt
        $lastLogBytes = 0L
        $lastCpuSeconds = 0d
        $startupObserved = $false
        $resultSeenAt = $null
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
                $elapsedSeconds -ge $TestStartupTimeoutSeconds) {
                Stop-MonitoredProcess `
                    -Process $process `
                    -Description "Unity $TestPlatform"
                throw "Unity $TestPlatform did not create a non-empty log within $TestStartupTimeoutSeconds seconds. The editor likely stalled before project load or license initialization. Log: $logPath"
            }
            if ($startupObserved -and
                ($now - $lastProgressAt).TotalSeconds -ge
                    $TestNoProgressTimeoutSeconds) {
                Stop-MonitoredProcess `
                    -Process $process `
                    -Description "Unity $TestPlatform"
                throw "Unity $TestPlatform made no log or CPU progress for $TestNoProgressTimeoutSeconds seconds. Log: $logPath"
            }

            if ($null -eq $resultSeenAt -and
                (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
                $resultSeenAt = $now
            }

            if (($now - $lastHeartbeatAt).TotalSeconds -ge
                $TestHeartbeatSeconds) {
                Write-Host (
                    "Unity {0} heartbeat: elapsed={1:n0}s, cpu={2:n1}s, log={3} bytes, result={4}" -f
                        $TestPlatform,
                        $elapsedSeconds,
                        $cpuSeconds,
                        $logBytes,
                        ($null -ne $resultSeenAt))
                $lastHeartbeatAt = $now
            }

            if ($null -ne $resultSeenAt -and
                ($now - $resultSeenAt).TotalSeconds -ge
                    $TestShutdownGraceSeconds) {
                Write-Warning "Unity $TestPlatform wrote its result but did not exit within $TestShutdownGraceSeconds seconds; stopping the residual process."
                Stop-MonitoredProcess `
                    -Process $process `
                    -Description "Unity $TestPlatform"
                $forcedShutdown = $true
                break
            }

            if ($elapsedSeconds -ge $TestTimeoutSeconds) {
                Stop-MonitoredProcess `
                    -Process $process `
                    -Description "Unity $TestPlatform"
                throw "Unity $TestPlatform timed out after $TestTimeoutSeconds seconds. Log: $logPath"
            }
        }

        if (-not $forcedShutdown -and $process.ExitCode -ne 0) {
            Write-Error "Unity $TestPlatform failed with exit code $($process.ExitCode). Log: $logPath"
        }
    } finally {
        Stop-MonitoredProcess `
            -Process $process `
            -Description "Unity $TestPlatform"
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        Write-Error "Unity $TestPlatform produced no test result file. Log: $logPath"
    }

    [xml]$document = Get-Content -LiteralPath $resultPath -Encoding UTF8
    $testRun = $document.'test-run'
    $summary = New-UnityTestSummary `
        -TestRun $testRun `
        -TestPlatform $TestPlatform `
        -ForcedShutdown $forcedShutdown `
        -ResultPath $resultPath `
        -LogPath $logPath
    Write-Host ($summary | Format-Table -AutoSize | Out-String)

    Assert-UnityTestSummaryPassed -Summary $summary
    return $summary
}

$resolvedProjectPath = [System.IO.Path]::GetFullPath($ProjectPath)
if (-not (Test-Path -LiteralPath $resolvedProjectPath -PathType Container)) {
    throw "Unity project not found: $resolvedProjectPath"
}
$resolvedResultsDirectory = [System.IO.Path]::GetFullPath($ResultsDirectory)
New-Item -ItemType Directory -Path $resolvedResultsDirectory -Force | Out-Null
$resolvedUnityPath = Resolve-UnityPath `
    -RequestedPath $UnityPath `
    -ResolvedProjectPath $resolvedProjectPath

$platforms = if ($Platform -eq "All") {
    @("EditMode", "PlayMode")
} else {
    @($Platform)
}

$summaries = foreach ($testPlatform in $platforms) {
    Invoke-UnityTestPlatform `
        -ResolvedUnityPath $resolvedUnityPath `
        -ResolvedProjectPath $resolvedProjectPath `
        -ResolvedResultsDirectory $resolvedResultsDirectory `
        -TestPlatform $testPlatform `
        -TestTimeoutSeconds $TimeoutSeconds `
        -TestStartupTimeoutSeconds $StartupTimeoutSeconds `
        -TestHeartbeatSeconds $HeartbeatSeconds `
        -TestNoProgressTimeoutSeconds $NoProgressTimeoutSeconds `
        -TestShutdownGraceSeconds $ShutdownGraceSeconds
}

Write-Host "Unity test baseline passed."
$summaries |
    Format-Table `
        Platform,
        Result,
        Total,
        Passed,
        Failed,
        Skipped,
        Inconclusive,
        DurationSeconds,
        ForcedShutdown `
        -AutoSize
