[CmdletBinding()]
param(
    [string]$UnityPath = $env:UNITY_EXE,

    [string]$ProjectPath = "",

    [ValidateRange(60, 1800)]
    [int]$TimeoutSeconds = 900
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
$repositoryRoot =
    [System.IO.Path]::GetFullPath((Join-Path $scriptDirectory ".."))
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $repositoryRoot "sc"
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
    "Logs\Phase9C\ArtRefresh\v0.3.4"
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logPath = Join-Path $logDirectory "unity-promotion.log"
$resultRelativePath =
    "ui-concepts\phase-9c\light-storybook-production-v0.1\" +
    "legacy-refresh-v0.3.4\RUNTIME-PROMOTION-RESULT-v0.3.4.json"
$resultPath = Join-Path $repositoryRoot $resultRelativePath

$arguments = @(
    "-batchmode",
    "-projectPath", ('"{0}"' -f $resolvedProjectPath),
    "-executeMethod",
    "SpireChess.Editor.LightStorybookArtRefreshV034Builder.BuildFromCommandLine",
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
            "Phase 9C v0.3.4 art refresh timed out after " +
            "$TimeoutSeconds seconds. Log: $logPath"
        )
    }
    if ($process.ExitCode -ne 0) {
        throw "Phase 9C v0.3.4 art refresh failed. Log: $logPath"
    }
} finally {
    $process.Refresh()
    if (-not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit(10000) | Out-Null
    }
}

if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
    throw "Art refresh produced no Runtime promotion result."
}
$result = Get-Content `
    -LiteralPath $resultPath `
    -Encoding UTF8 `
    -Raw |
    ConvertFrom-Json
if ($result.status -ne "PROMOTED" -or
    $result.configuredArtworkCount -ne 83 -or
    $result.baselineApprovedArtworkCount -ne 66 -or
    $result.refreshedArtworkCount -ne 17 -or
    $result.exactApprovedStyleCoverage -ne "83/83" -or
    -not $result.policy.runtimeCatalogGuidPreserved) {
    throw "Art refresh Runtime promotion result validation failed."
}

Write-Host "Phase 9C v0.3.4 art refresh completed."
Write-Host "Exact approved style coverage: 83/83"
Write-Host "Result: $resultPath"
Write-Host "Unity log: $logPath"
