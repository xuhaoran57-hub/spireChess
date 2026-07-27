[CmdletBinding()]
param(
    [string]$UnityPath = $env:UNITY_EXE,

    [string]$ProjectPath = (Join-Path $PSScriptRoot "..\sc"),

    [string]$OutputDirectory =
        (Join-Path $PSScriptRoot "..\sc\Logs\G4\G4V"),

    [string]$BuildId =
        (([DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")) + "-g4v"),

    [string]$Quality = "High",

    [switch]$AllowDirtyProbe,

    [ValidateRange(60, 7200)]
    [int]$TestTimeoutSeconds = 1200,

    [ValidateRange(300, 7200)]
    [int]$BuildTimeoutSeconds = 3600,

    [ValidateRange(30, 1800)]
    [int]$PlayerTimeoutSeconds = 300
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-Sha256 {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return (
        Get-FileHash -LiteralPath $Path -Algorithm SHA256
    ).Hash.ToLowerInvariant()
}

function Read-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required JSON file not found: $Path"
    }
    return Get-Content -LiteralPath $Path -Encoding UTF8 -Raw |
        ConvertFrom-Json
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

$repositoryRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $resolvedProjectPath ".."))
$gitStatus = @()
try {
    $gitStatus = @(
        & git -C $repositoryRoot status --porcelain=v1 --untracked-files=all
    )
    if ($LASTEXITCODE -ne 0) {
        throw "git status exited with code $LASTEXITCODE."
    }
} catch {
    if (-not $AllowDirtyProbe) {
        throw (
            "Formal G4-V acceptance could not verify a clean source tree. " +
            "Use -AllowDirtyProbe only for non-formal diagnosis. " +
            $_.Exception.Message)
    }
    $gitStatus = @("git-identity-unavailable")
}
if ($gitStatus.Count -gt 0 -and -not $AllowDirtyProbe) {
    throw (
        "Formal G4-V acceptance requires a clean source tree. " +
        "Commit or remove all changes, or use -AllowDirtyProbe for a " +
        "non-formal diagnostic run.")
}

$resolvedOutputDirectory =
    [System.IO.Path]::GetFullPath($OutputDirectory)
$bundleRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $resolvedOutputDirectory $BuildId))
$outputPrefix =
    $resolvedOutputDirectory.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) +
    [System.IO.Path]::DirectorySeparatorChar
if (-not $bundleRoot.StartsWith(
        $outputPrefix,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Resolved G4-V bundle directory escapes OutputDirectory: $bundleRoot"
}
if (Test-Path -LiteralPath $bundleRoot) {
    $existing = Get-ChildItem -LiteralPath $bundleRoot -Force |
        Select-Object -First 1
    if ($null -ne $existing) {
        throw "Refusing to reuse non-empty G4-V bundle directory: $bundleRoot"
    }
}

$testScript = Join-Path $PSScriptRoot "run_unity_tests.ps1"
$buildScript = Join-Path $PSScriptRoot "build_g4_windows.ps1"
$acceptanceScript = Join-Path $PSScriptRoot "run_g4_acceptance.ps1"
foreach ($script in @($testScript, $buildScript, $acceptanceScript)) {
    if (-not (Test-Path -LiteralPath $script -PathType Leaf)) {
        throw "Required G4 script not found: $script"
    }
}

$testResultsDirectory = Join-Path $bundleRoot "tests"
$runsDirectory = Join-Path $bundleRoot "runs"
New-Item -ItemType Directory -Path $testResultsDirectory -Force |
    Out-Null
New-Item -ItemType Directory -Path $runsDirectory -Force |
    Out-Null

Write-Host "[1/4] Running the complete Unity EditMode + PlayMode suite..."
& $testScript `
    -Platform All `
    -UnityPath $UnityPath `
    -ProjectPath $resolvedProjectPath `
    -ResultsDirectory $testResultsDirectory `
    -TimeoutSeconds $TestTimeoutSeconds

Write-Host "[2/4] Building a clean Windows x64 Development Player..."
& $buildScript `
    -UnityPath $UnityPath `
    -ProjectPath $resolvedProjectPath `
    -BuildId $BuildId `
    -CleanBuild `
    -TimeoutSeconds $BuildTimeoutSeconds

$playerPath = Join-Path `
    $resolvedProjectPath `
    "Builds\G4\$BuildId\Windows-x64\SpireChess.exe"
$resolvedPlayerPath = [System.IO.Path]::GetFullPath($playerPath)
if (-not (Test-Path -LiteralPath $resolvedPlayerPath -PathType Leaf)) {
    throw "G4-V build produced no Player: $resolvedPlayerPath"
}
$buildManifestPath = Join-Path `
    ([System.IO.Path]::GetDirectoryName($resolvedPlayerPath)) `
    "g4-build-manifest.json"
$buildManifest = Read-JsonFile -Path $buildManifestPath

$resolutions = @("1920x1080", "1920x1200")
$runContracts = @()
Write-Host "[3/4] Capturing the five-screen slice at two resolutions..."
foreach ($resolution in $resolutions) {
    $runId = "$BuildId-g4v-$resolution"
    $acceptanceParameters = @{
        PlayerPath = $resolvedPlayerPath
        OutputDirectory = $runsDirectory
        Resolution = $resolution
        Quality = $Quality
        Seed = 10
        RunId = $runId
        VisualSlice = $true
        TimeoutSeconds = $PlayerTimeoutSeconds
    }
    if ($AllowDirtyProbe) {
        $acceptanceParameters.AllowDirtyProbe = $true
    }
    & $acceptanceScript @acceptanceParameters

    $runRoot = Join-Path $runsDirectory $runId
    $evidenceManifestPath =
        Join-Path $runRoot "g4-evidence-manifest.json"
    $evidenceManifest =
        Read-JsonFile -Path $evidenceManifestPath
    if ($evidenceManifest.schemaVersion -ne
        "spire-chess-g4-evidence-v2") {
        throw "Unexpected G4 evidence schema for ${resolution}."
    }
    if ($evidenceManifest.acceptanceMode -ne "visual-slice" -or
        $evidenceManifest.resolution -ne $resolution -or
        [int]$evidenceManifest.seed -ne 10 -or
        $evidenceManifest.buildId -ne $BuildId) {
        throw "G4-V evidence identity mismatch for ${resolution}."
    }

    $expectedScreenshotNames = @(
        "01-main-menu-$resolution.png",
        "02-floor-map-$resolution.png",
        "03-shop-environment-$resolution.png",
        "04-battle-background-$resolution.png",
        "05-event-tranquil-grove-$resolution.png"
    )
    $screenshots = @($evidenceManifest.screenshots)
    $actualScreenshotNames = @(
        $screenshots | ForEach-Object { [string]$_.file }
    )
    $missingScreenshotNames = @(
        $expectedScreenshotNames |
        Where-Object { $actualScreenshotNames -notcontains $_ }
    )
    $unexpectedScreenshotNames = @(
        $actualScreenshotNames |
        Where-Object { $expectedScreenshotNames -notcontains $_ }
    )
    if ($screenshots.Count -ne 5 -or
        $missingScreenshotNames.Count -gt 0 -or
        $unexpectedScreenshotNames.Count -gt 0) {
        throw (
            "G4-V screenshot contract mismatch for ${resolution}. " +
            "Missing=[$($missingScreenshotNames -join ', ')] " +
            "Unexpected=[$($unexpectedScreenshotNames -join ', ')]")
    }

    $collectedScreenshots = @(
        $screenshots |
        Sort-Object file |
        ForEach-Object {
            $screenshotPath =
                Join-Path (Join-Path $runRoot "screenshots") $_.file
            if (-not (Test-Path `
                    -LiteralPath $screenshotPath `
                    -PathType Leaf)) {
                throw "G4-V screenshot not found: $screenshotPath"
            }
            if ((Get-Sha256 -Path $screenshotPath) -ne $_.sha256) {
                throw "G4-V screenshot hash mismatch: $screenshotPath"
            }
            [pscustomobject]@{
                file = [string]$_.file
                path = [System.IO.Path]::GetFullPath($screenshotPath)
                bytes = [long]$_.bytes
                width = [int]$_.width
                height = [int]$_.height
                sha256 = [string]$_.sha256
            }
        }
    )
    $runContracts += [pscustomobject]@{
        resolution = $resolution
        runId = $runId
        evidenceManifestPath =
            [System.IO.Path]::GetFullPath($evidenceManifestPath)
        evidenceManifestSha256 =
            Get-Sha256 -Path $evidenceManifestPath
        screenshots = $collectedScreenshots
    }
}

Write-Host "[4/4] Writing the aggregate G4-V evidence manifest..."
$testContracts = @(
    foreach ($platform in @("EditMode", "PlayMode")) {
        $resultPath =
            Join-Path $testResultsDirectory "$platform-results.xml"
        if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
            throw "Unity test result not found: $resultPath"
        }
        [xml]$testDocument =
            Get-Content -LiteralPath $resultPath -Encoding UTF8 -Raw
        $testRun = $testDocument.'test-run'
        if ($null -eq $testRun -or
            [string]$testRun.result -ne "Passed") {
            throw "Unity $platform result is not Passed: $resultPath"
        }
        [pscustomobject]@{
            platform = $platform
            result = [string]$testRun.result
            total = [int]$testRun.total
            passed = [int]$testRun.passed
            failed = [int]$testRun.failed
            skipped = [int]$testRun.skipped
            inconclusive = [int]$testRun.inconclusive
            path = [System.IO.Path]::GetFullPath($resultPath)
            sha256 = Get-Sha256 -Path $resultPath
        }
    }
)

$aggregateManifest = [pscustomobject]@{
    schemaVersion = "spire-chess-g4v-visual-acceptance-v1"
    generatedAtUtc = [DateTime]::UtcNow.ToString("o")
    buildId = $BuildId
    unityVersion = [string]$buildManifest.unityVersion
    gitCommit = [string]$buildManifest.gitCommit
    sourceTreeDirty = [bool]$buildManifest.sourceTreeDirty
    evidenceClassification = if ([bool]$buildManifest.sourceTreeDirty) {
        "DirtyProbe"
    } else {
        "FormalCandidate"
    }
    playerPath = $resolvedPlayerPath
    playerSha256 = Get-Sha256 -Path $resolvedPlayerPath
    buildManifestPath =
        [System.IO.Path]::GetFullPath($buildManifestPath)
    buildManifestSha256 =
        Get-Sha256 -Path $buildManifestPath
    tests = $testContracts
    visualFixture = [pscustomobject]@{
        seed = 10
        eventNodeId = "f1_event"
        eventId = "tranquil_grove"
        eventArtworkId = "event_tranquil_grove"
        boundary = (
            "Only event-node reachability is injected. Event selection, " +
            "configuration, artwork loading, and formal UI rendering use " +
            "production runtime paths."
        )
    }
    runs = $runContracts
    screenshotCount = 10
}
$aggregateManifestPath =
    Join-Path $bundleRoot "g4v-visual-acceptance-manifest.json"
$aggregateManifest |
    ConvertTo-Json -Depth 10 |
    Set-Content -LiteralPath $aggregateManifestPath -Encoding UTF8

Write-Host "G4-V one-click acceptance passed."
Write-Host "Classification: $($aggregateManifest.evidenceClassification)"
Write-Host "Manifest:       $aggregateManifestPath"
Write-Host "Evidence root:  $bundleRoot"
