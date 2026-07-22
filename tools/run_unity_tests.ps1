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

function Invoke-UnityTestPlatform {
    param(
        [string]$ResolvedUnityPath,
        [string]$ResolvedProjectPath,
        [string]$ResolvedResultsDirectory,
        [ValidateSet("EditMode", "PlayMode")]
        [string]$TestPlatform,
        [int]$TestTimeoutSeconds,
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
    $process = Start-Process `
        -FilePath $ResolvedUnityPath `
        -ArgumentList $arguments `
        -PassThru

    $startedAt = [DateTime]::UtcNow
    $resultSeenAt = $null
    $forcedShutdown = $false
    while (-not $process.HasExited) {
        Start-Sleep -Milliseconds 500

        if ($null -eq $resultSeenAt -and
            (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
            $resultSeenAt = [DateTime]::UtcNow
        }

        if ($null -ne $resultSeenAt -and
            ([DateTime]::UtcNow - $resultSeenAt).TotalSeconds -ge $TestShutdownGraceSeconds) {
            Write-Warning "Unity $TestPlatform wrote its result but did not exit within $TestShutdownGraceSeconds seconds; stopping the residual process."
            $process.Kill()
            $process.WaitForExit(10000) | Out-Null
            $forcedShutdown = $true
            break
        }

        if (([DateTime]::UtcNow - $startedAt).TotalSeconds -ge $TestTimeoutSeconds) {
            $process.Kill()
            $process.WaitForExit(10000) | Out-Null
            throw "Unity $TestPlatform timed out after $TestTimeoutSeconds seconds. Log: $logPath"
        }
    }

    if (-not $forcedShutdown -and $process.ExitCode -ne 0) {
        Write-Error "Unity $TestPlatform failed with exit code $($process.ExitCode). Log: $logPath"
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        Write-Error "Unity $TestPlatform produced no test result file. Log: $logPath"
    }

    [xml]$document = Get-Content -LiteralPath $resultPath -Encoding UTF8
    $testRun = $document.'test-run'
    if ($null -eq $testRun) {
        Write-Error "Unity $TestPlatform result has no test-run element: $resultPath"
    }

    $summary = [pscustomobject]@{
        Platform = $TestPlatform
        Result = [string]$testRun.result
        Total = [int]$testRun.total
        Passed = [int]$testRun.passed
        Failed = [int]$testRun.failed
        Skipped = [int]$testRun.skipped
        DurationSeconds = [math]::Round([double]$testRun.duration, 3)
        ForcedShutdown = $forcedShutdown
        ResultPath = $resultPath
        LogPath = $logPath
    }
    Write-Host ($summary | Format-Table -AutoSize | Out-String)

    if ($summary.Result -ne "Passed" -or $summary.Failed -ne 0) {
        Write-Error "Unity $TestPlatform tests did not pass. Result: $($summary.Result); failed: $($summary.Failed)."
    }
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
        -TestShutdownGraceSeconds $ShutdownGraceSeconds
}

Write-Host "Unity test baseline passed."
$summaries | Format-Table Platform, Result, Total, Passed, Failed, Skipped, DurationSeconds, ForcedShutdown -AutoSize
