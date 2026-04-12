<# 
    remove_green_bg.ps1
    绿幕去除算法 V3 (严格通道比对 + 像素边缘黑边清理)
    用法: powershell -ExecutionPolicy Bypass -File remove_green_bg.ps1 -InputFile <path> -OutputFile <path>
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$InputFile,
    
    [Parameter(Mandatory=$true)]
    [string]$OutputFile
)

Add-Type -AssemblyName System.Drawing

Write-Host "=== 绿幕去除脚本 V3 (无损颜色 + 黑边清理) ==="
Write-Host "输入: $InputFile"
Write-Host "输出: $OutputFile"

if (-not (Test-Path $InputFile)) {
    Write-Host "错误: 输入文件不存在: $InputFile"
    exit 1
}

$bmp = [System.Drawing.Bitmap]::FromFile((Resolve-Path $InputFile).Path)
$width = $bmp.Width
$height = $bmp.Height
Write-Host "图像尺寸: ${width}x${height}"

$newBmp = New-Object System.Drawing.Bitmap($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

$transparentCount = 0
$totalCount = $width * $height

# Pass 1: 严格绿幕抠除
Write-Host "执行 Pass 1: 严格背景分离..."
for ($y = 0; $y -lt $height; $y++) {
    for ($x = 0; $x -lt $width; $x++) {
        $p = $bmp.GetPixel($x, $y)
        $r = $p.R
        $g = $p.G
        $b = $p.B
        
        # 核心算法：绿通道必须显著大于红、蓝通道
        if ($g -gt ($r + 25) -and $g -gt ($b + 25) -and $g -gt 60) {
            $newBmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
            $transparentCount++
        } else {
            $newBmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $r, $g, $b))
        }
    }
}

# Pass 2: 边缘杂色与黑边清理 (腐蚀算法)
Write-Host "执行 Pass 2: 边缘黑框清理..."
# 进行2次迭代清理较粗的黑边
for ($iter = 1; $iter -le 2; $iter++) {
    $pixelsToRemove = New-Object System.Collections.ArrayList
    
    for ($y = 0; $y -lt $height; $y++) {
        for ($x = 0; $x -lt $width; $x++) {
            $p = $newBmp.GetPixel($x, $y)
            if ($p.A -eq 255) {
                # 检查是否为图像边缘(与透明像素相邻)
                $isEdge = $false
                foreach ($dy in -1..1) {
                    foreach ($dx in -1..1) {
                        if ($dx -eq 0 -and $dy -eq 0) { continue }
                        $nx = $x + $dx
                        $ny = $y + $dy
                        if ($nx -ge 0 -and $nx -lt $width -and $ny -ge 0 -and $ny -lt $height) {
                            if ($newBmp.GetPixel($nx, $ny).A -eq 0) {
                                $isEdge = $true
                                break
                            }
                        }
                    }
                    if ($isEdge) { break }
                }
                
                if ($isEdge) {
                    $r = $p.R
                    $g = $p.G
                    $b = $p.B
                    $brightness = ($r + $g + $b) / 3.0
                    
                    # 清理条件：偏绿杂边，或者太暗的黑边
                    $isSlightlyGreen = ($g -ge $r) -and ($g -ge $b) -and ($g -gt 40)
                    $isDarkBorder = ($brightness -lt 70)
                    
                    if ($isSlightlyGreen -or $isDarkBorder) {
                        [void]$pixelsToRemove.Add(@{X=$x; Y=$y})
                    }
                }
            }
        }
    }
    
    foreach ($pt in $pixelsToRemove) {
        $newBmp.SetPixel($pt.X, $pt.Y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
        $transparentCount++
    }
}

$newBmp.Save($OutputFile, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
$newBmp.Dispose()

$transparentPercent = [Math]::Round(($transparentCount / $totalCount) * 100, 1)
Write-Host "=== 处理完成 ==="
Write-Host "透明像素: $transparentCount / $totalCount ($transparentPercent%)"
Write-Host "输出文件: $OutputFile"
