param()

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$roundDir = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $roundDir "..\..\..\..")).Path
$manifestPath = Join-Path $roundDir "TOKEN-MANIFEST-v0.3.4.json"
$reportPath = Join-Path $roundDir "VALIDATION-REPORT-v0.3.4.json"

function Get-Sha256Lower {
    param([Parameter(Mandatory = $true)][string]$Path)

    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
}

function Get-RepoPath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    return Join-Path $repoRoot ($RelativePath.Replace("/", "\"))
}

function Get-ImageMetrics {
    param([Parameter(Mandatory = $true)][string]$Path)

    $source = $null
    $sample = $null
    $graphics = $null
    try {
        $source = [System.Drawing.Image]::FromFile($Path)
        $width = $source.Width
        $height = $source.Height
        $sample = New-Object System.Drawing.Bitmap 160, 128
        $graphics = [System.Drawing.Graphics]::FromImage($sample)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.DrawImage($source, 0, 0, 160, 128)

        $lightMid = 0
        $nearBlack = 0
        $lumaSum = 0.0
        for ($y = 0; $y -lt 128; $y++) {
            for ($x = 0; $x -lt 160; $x++) {
                $pixel = $sample.GetPixel($x, $y)
                $luma = 0.2126 * $pixel.R + 0.7152 * $pixel.G + 0.0722 * $pixel.B
                $lumaSum += $luma
                if ($luma -ge 85) {
                    $lightMid++
                }
                if ($luma -lt 25) {
                    $nearBlack++
                }
            }
        }

        return [ordered]@{
            size = @($width, $height)
            aspectRatio = [math]::Round($width / $height, 6)
            luma = [ordered]@{
                meanLuma = [math]::Round($lumaSum / (160 * 128), 2)
                lightMidRatio = [math]::Round($lightMid / (160 * 128), 6)
                nearBlackRatio = [math]::Round($nearBlack / (160 * 128), 6)
            }
        }
    }
    finally {
        if ($null -ne $graphics) {
            $graphics.Dispose()
        }
        if ($null -ne $sample) {
            $sample.Dispose()
        }
        if ($null -ne $source) {
            $source.Dispose()
        }
    }
}

function Get-CatalogEntries {
    param([Parameter(Mandatory = $true)][string]$Path)

    $text = Get-Content -LiteralPath $Path -Encoding UTF8 -Raw
    $pattern = "(?m)^  - id: (.+)\r?\n    sprite: \{fileID: 21300000, guid: ([0-9a-f]{32}),"
    $entries = @{}
    foreach ($match in [regex]::Matches($text, $pattern)) {
        $entries[$match.Groups[1].Value] = $match.Groups[2].Value
    }
    return $entries
}

function Get-MetaGuid {
    param([Parameter(Mandatory = $true)][string]$Path)

    $text = Get-Content -LiteralPath $Path -Encoding UTF8 -Raw
    $match = [regex]::Match($text, "(?m)^guid: ([0-9a-f]{32})$")
    if (-not $match.Success) {
        return $null
    }
    return $match.Groups[1].Value
}

$manifest = Get-Content -LiteralPath $manifestPath -Encoding UTF8 -Raw | ConvertFrom-Json
$items = @($manifest.items)
$checks = @()

$identityPassed = (
    $manifest.version -eq "0.3.4" -and
    $manifest.status -eq "PROMOTED" -and
    $items.Count -eq 3 -and
    @($items | Where-Object { $_.kind -ne "Token" }).Count -eq 0 -and
    $manifest.counts.runtimePromoted -eq 3
)
$checks += [ordered]@{
    id = "candidate-identity"
    status = $(if ($identityPassed) { "pass" } else { "fail" })
    details = [ordered]@{
        count = $items.Count
        ids = @($items | ForEach-Object { $_.id })
        runtimePromoted = $manifest.counts.runtimePromoted
    }
}

$referenceResults = @()
$referencesPassed = $true
$references = @($manifest.generation.styleReference) + @($manifest.inheritedRules)
foreach ($reference in $references) {
    $path = Get-RepoPath $reference.path
    $exists = Test-Path -LiteralPath $path -PathType Leaf
    $actualHash = if ($exists) { Get-Sha256Lower $path } else { $null }
    $passed = $exists -and $actualHash -eq $reference.sha256
    $referencesPassed = $referencesPassed -and $passed
    $referenceResults += [ordered]@{
        path = $reference.path
        exists = $exists
        expectedSha256 = $reference.sha256
        actualSha256 = $actualHash
        passes = $passed
    }
}
$checks += [ordered]@{
    id = "frozen-reference-provenance"
    status = $(if ($referencesPassed) { "pass" } else { "fail" })
    details = $referenceResults
}

$artResults = @()
$artPassed = $true
foreach ($item in $items) {
    $path = Get-RepoPath $item.candidatePath
    $exists = Test-Path -LiteralPath $path -PathType Leaf
    if (-not $exists) {
        $artPassed = $false
        $artResults += [ordered]@{
            id = $item.id
            exists = $false
            passes = $false
        }
        continue
    }

    $metrics = Get-ImageMetrics $path
    $actualHash = Get-Sha256Lower $path
    $passed = (
        $metrics.aspectRatio -ge 1.23 -and
        $metrics.aspectRatio -le 1.27 -and
        $metrics.luma.lightMidRatio -ge 0.5 -and
        $metrics.luma.nearBlackRatio -lt 0.12 -and
        $actualHash -eq $item.candidateSha256
    )
    $artPassed = $artPassed -and $passed
    $artResults += [ordered]@{
        id = $item.id
        exists = $true
        size = $metrics.size
        aspectRatio = $metrics.aspectRatio
        luma = $metrics.luma
        sha256 = $actualHash
        differsFromOldRuntime = $actualHash -ne $item.oldRuntimeSha256
        passes = $passed
    }
}
$checks += [ordered]@{
    id = "candidate-hash-aspect-brightness"
    status = $(if ($artPassed) { "pass" } else { "fail" })
    details = $artResults
}

$catalogPath = Get-RepoPath $manifest.sources.runtimeCatalog.path
$catalog = Get-CatalogEntries $catalogPath
$runtimeResults = @()
$runtimePassed = $true
foreach ($item in $items) {
    $runtimePath = Get-RepoPath $item.runtimePath
    $metaPath = "$runtimePath.meta"
    $runtimeExists = Test-Path -LiteralPath $runtimePath -PathType Leaf
    $metaExists = Test-Path -LiteralPath $metaPath -PathType Leaf
    $runtimeHash = if ($runtimeExists) { Get-Sha256Lower $runtimePath } else { $null }
    $metaGuid = if ($metaExists) { Get-MetaGuid $metaPath } else { $null }
    $catalogGuid = $catalog[$item.artId]
    $passed = (
        $runtimeExists -and
        $metaExists -and
        $runtimeHash -eq $item.candidateSha256 -and
        $metaGuid -eq $item.runtimeGuid -and
        $catalogGuid -eq $item.runtimeGuid
    )
    $runtimePassed = $runtimePassed -and $passed
    $runtimeResults += [ordered]@{
        id = $item.id
        artId = $item.artId
        runtimeSha256 = $runtimeHash
        expectedPromotedSha256 = $item.candidateSha256
        metaGuid = $metaGuid
        catalogGuid = $catalogGuid
        runtimePromoted = $runtimeHash -eq $item.candidateSha256
        passes = $passed
    }
}
$checks += [ordered]@{
    id = "runtime-promotion"
    status = $(if ($runtimePassed) { "pass" } else { "fail" })
    details = [ordered]@{
        catalogPath = $manifest.sources.runtimeCatalog.path
        tokens = $runtimeResults
    }
}

$passed = @($checks | Where-Object { $_.status -ne "pass" }).Count -eq 0
$report = [ordered]@{
    version = "0.3.4"
    releaseId = "token-refresh-v0.3.4"
    generatedAtUtc = [DateTime]::UtcNow.ToString("o")
    result = $(if ($passed) { "PASS_RUNTIME_PROMOTED" } else { "FAIL" })
    visualApproval = "approved"
    checks = $checks
}

$json = $report | ConvertTo-Json -Depth 12
[System.IO.File]::WriteAllText(
    $reportPath,
    "$json`n",
    (New-Object System.Text.UTF8Encoding($false))
)

Write-Output $report.result
if ($passed) {
    exit 0
}
exit 1
