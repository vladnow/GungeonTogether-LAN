$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$bitmap = [Drawing.Bitmap]::new(256,256)
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$background = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(20,27,44))
$orange = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255,179,64))
$cyan = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(76,211,229))
$white = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(238,246,255))
$line = [Drawing.Pen]::new([Drawing.Color]::FromArgb(238,246,255),7)
$border = [Drawing.Pen]::new([Drawing.Color]::FromArgb(53,74,101),4)
$font = [Drawing.Font]::new('Segoe UI',42,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
try {
 $graphics.FillRectangle($background,0,0,256,256)
 $graphics.DrawRectangle($border,10,10,236,236)
 $graphics.DrawLine($line,86,116,170,116)
 $graphics.FillEllipse($orange,45,39,48,48)
 $graphics.FillRectangle($orange,44,91,50,68)
 $graphics.FillEllipse($cyan,163,39,48,48)
 $graphics.FillRectangle($cyan,162,91,50,68)
 $graphics.FillEllipse($white,117,105,22,22)
 $size = $graphics.MeasureString('LAN',$font)
 $graphics.DrawString('LAN',$font,$white,([single](256-$size.Width)/2),174)
 $bitmap.Save((Join-Path $PSScriptRoot 'icon.png'),[Drawing.Imaging.ImageFormat]::Png)
} finally {
 $font.Dispose();$border.Dispose();$line.Dispose();$white.Dispose();$cyan.Dispose();$orange.Dispose();$background.Dispose();$graphics.Dispose();$bitmap.Dispose()
}
