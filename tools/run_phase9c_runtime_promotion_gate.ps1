[CmdletBinding()]
param(
    [string]$UnityPath = $env:UNITY_EXE,

    [string]$ProjectPath = "",

    [ValidateRange(60, 1800)]
    [int]$TimeoutSeconds = 600
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
$resolvedUnityPath = Resolve-UnityPath `
    -RequestedPath $UnityPath `
    -ResolvedProjectPath $resolvedProjectPath
$logDirectory = Join-Path `
    $resolvedProjectPath `
    "Logs\Phase9C\RuntimePromotionGate\v0.3.3"
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logPath = Join-Path $logDirectory "unity-gate.log"
$resultPath = Join-Path $logDirectory "gate-result.json"
if (Test-Path -LiteralPath $resultPath -PathType Leaf) {
    Remove-Item -LiteralPath $resultPath -Force
}

$arguments = @(
    "-batchmode",
    "-projectPath", ('"{0}"' -f $resolvedProjectPath),
    "-executeMethod",
    "SpireChess.Editor.LightStorybookRuntimePromotionGate.ValidateFromCommandLine",
    "-logFile", ('"{0}"' -f $logPath),
    "-quit"
)
$process = Start-Process `
    -FilePath $resolvedUnityPath `
    -ArgumentList $arguments `
    -WindowStyle Hidden `
    -PassThru
try {
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill()
        $process.WaitForExit(10000) | Out-Null
        throw (
            "Phase 9C Runtime promotion gate timed out after " +
            "$TimeoutSeconds seconds. Log: $logPath"
        )
    }
    if ($process.ExitCode -ne 0) {
        if (Test-Path -LiteralPath $resultPath -PathType Leaf) {
            $result = Get-Content `
                -LiteralPath $resultPath `
                -Encoding UTF8 `
                -Raw |
                ConvertFrom-Json
            foreach ($failure in $result.failures) {
                Write-Host "BLOCKED: $failure"
            }
        }
        throw (
            "Phase 9C Runtime promotion gate is blocked. " +
            "Log: $logPath"
        )
    }
} finally {
    $process.Refresh()
    if (-not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit(10000) | Out-Null
    }
}

if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
    throw "Phase 9C Runtime promotion gate produced no result."
}
$result = Get-Content `
    -LiteralPath $resultPath `
    -Encoding UTF8 `
    -Raw |
    ConvertFrom-Json
if ($result.status -ne "PASS") {
    throw "Phase 9C Runtime promotion gate did not pass."
}

Write-Host "Phase 9C v0.3.3 Runtime promotion gate passed."
Write-Host "Evidence: $resultPath"
