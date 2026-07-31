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

$gitStatus = & git -C $repositoryRoot status `
    --porcelain `
    --untracked-files=all
if ($LASTEXITCODE -ne 0) {
    throw "Unable to verify the repository worktree."
}
if ($gitStatus) {
    throw (
        "Runtime promotion requires a clean worktree. " +
        "Commit or stash all changes first."
    )
}

$resolvedUnityPath = Resolve-UnityPath `
    -RequestedPath $UnityPath `
    -ResolvedProjectPath $resolvedProjectPath
$logDirectory = Join-Path `
    $resolvedProjectPath `
    "Logs\Phase9C\RuntimePromotion\v0.3.3"
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logPath = Join-Path $logDirectory "unity-promotion.log"
$manifestRelativePath =
    "ui-concepts\phase-9c\light-storybook-production-v0.1\" +
    "runtime-promotion-v0.3.3\promotion-manifest.json"
$manifestPath = Join-Path $repositoryRoot $manifestRelativePath

$arguments = @(
    "-batchmode",
    "-projectPath", ('"{0}"' -f $resolvedProjectPath),
    "-executeMethod",
    "SpireChess.Editor.LightStorybookRuntimePromotionBuilder.BuildFromCommandLine",
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
            "Phase 9C Runtime promotion timed out after " +
            "$TimeoutSeconds seconds. Log: $logPath"
        )
    }
    if ($process.ExitCode -ne 0) {
        throw "Phase 9C Runtime promotion failed. Log: $logPath"
    }
} finally {
    $process.Refresh()
    if (-not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit(10000) | Out-Null
    }
}

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Runtime promotion produced no promotion manifest."
}
$manifest = Get-Content `
    -LiteralPath $manifestPath `
    -Encoding UTF8 `
    -Raw |
    ConvertFrom-Json
if ($manifest.status -ne "PROMOTED" -or
    $manifest.runtimeCatalogEntryCount -ne 86 -or
    $manifest.productionArtworkCount -ne 51 -or
    $manifest.policy.calibrationReferences -ne 0 -or
    -not $manifest.policy.runtimeCatalogGuidPreserved) {
    throw "Runtime promotion manifest validation failed."
}

Write-Host "Phase 9C v0.3.3 Runtime promotion completed."
Write-Host "Manifest: $manifestPath"
Write-Host "Unity log: $logPath"
