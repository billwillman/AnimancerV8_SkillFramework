<# 
    check_alpha.ps1
    Transparency verification script
    Usage: powershell -ExecutionPolicy Bypass -File check_alpha.ps1 -InputFile <path>
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$InputFile
)

Add-Type -AssemblyName System.Drawing

Write-Host "=== Alpha Transparency Check ==="
Write-Host "File: $InputFile"

if (-not (Test-Path $InputFile)) {
    Write-Host "ERROR: File not found: $InputFile"
    exit 1
}

$bmp = [System.Drawing.Bitmap]::FromFile((Resolve-Path $InputFile).Path)
$width = $bmp.Width
$height = $bmp.Height

$totalPixels = $width * $height
$fullyTransparent = 0
$fullyOpaque = 0
$semiTransparent = 0
$edgeOpaqueNonWhite = 0

for ($y = 0; $y -lt $height; $y++) {
    for ($x = 0; $x -lt $width; $x++) {
        $p = $bmp.GetPixel($x, $y)
        if ($p.A -eq 0) {
            $fullyTransparent++
        }
        elseif ($p.A -eq 255) {
            $fullyOpaque++
        }
        else {
            $semiTransparent++
        }
    }
}

# Check outermost 1px border for dark opaque residuals
for ($x = 0; $x -lt $width; $x++) {
    foreach ($yEdge in 0, ($height - 1)) {
        $p = $bmp.GetPixel($x, $yEdge)
        if ($p.A -gt 0 -and ($p.R -lt 50 -and $p.G -lt 50 -and $p.B -lt 50)) {
            $edgeOpaqueNonWhite++
        }
    }
}
for ($y = 1; $y -lt ($height - 1); $y++) {
    foreach ($xEdge in 0, ($width - 1)) {
        $p = $bmp.GetPixel($xEdge, $y)
        if ($p.A -gt 0 -and ($p.R -lt 50 -and $p.G -lt 50 -and $p.B -lt 50)) {
            $edgeOpaqueNonWhite++
        }
    }
}

$bmp.Dispose()

$transparentPercent = [Math]::Round(($fullyTransparent / $totalPixels) * 100, 1)
$opaquePercent = [Math]::Round(($fullyOpaque / $totalPixels) * 100, 1)
$semiPercent = [Math]::Round(($semiTransparent / $totalPixels) * 100, 1)

Write-Host ""
Write-Host "--- Transparency Distribution ---"
Write-Host "Fully transparent (A=0):   $fullyTransparent ($transparentPercent%)"
Write-Host "Fully opaque (A=255):      $fullyOpaque ($opaquePercent%)"
Write-Host "Semi-transparent (0<A<255): $semiTransparent ($semiPercent%)"
Write-Host ""
Write-Host "--- Edge Detection ---"
Write-Host "Edge dark opaque pixels: $edgeOpaqueNonWhite"
Write-Host ""

$passed = $true
$reasons = @()

if ($transparentPercent -lt 40) {
    $passed = $false
    $reasons += "Transparent pixel ratio too low ($transparentPercent%), background may not be properly removed"
}

if ($edgeOpaqueNonWhite -gt 20) {
    $passed = $false
    $reasons += "Edge has $edgeOpaqueNonWhite dark opaque pixels, possible black cut lines"
}

if ($passed) {
    Write-Host "=== PASSED ==="
    Write-Host "Background transparency and edge cleanliness meet requirements."
    exit 0
} else {
    Write-Host "=== FAILED ==="
    foreach ($r in $reasons) {
        Write-Host "  - $r"
    }
    Write-Host ""
    Write-Host "Suggestion: Re-generate image or adjust green screen removal parameters."
    exit 1
}
