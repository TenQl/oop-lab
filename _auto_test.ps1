param([string]$OutDir = (Join-Path $PSScriptRoot "_smoke"))

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32 {
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    public const uint LEFTDOWN = 0x02; public const uint LEFTUP = 0x04;
}
"@
[Win32]::SetProcessDPIAware() | Out-Null

function Save-ClientShot($proc, $path) {
    $h = $proc.MainWindowHandle
    $rect = New-Object Win32+RECT
    [Win32]::GetClientRect($h, [ref]$rect) | Out-Null
    $origin = New-Object Win32+POINT
    [Win32]::ClientToScreen($h, [ref]$origin) | Out-Null
    $w = $rect.Right - $rect.Left
    $hh = $rect.Bottom - $rect.Top
    $bmp = New-Object System.Drawing.Bitmap($w, $hh)
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)
    $gfx.CopyFromScreen($origin.X, $origin.Y, 0, 0, $bmp.Size)
    $gfx.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

function Click-ClientPoint($proc, $cx, $cy) {
    $h = $proc.MainWindowHandle
    $rect = New-Object Win32+RECT
    [Win32]::GetClientRect($h, [ref]$rect) | Out-Null
    $origin = New-Object Win32+POINT
    [Win32]::ClientToScreen($h, [ref]$origin) | Out-Null
    $physW = $rect.Right - $rect.Left
    $scale = $physW / 1090.0
    $x = [int]($origin.X + $cx * $scale)
    $y = [int]($origin.Y + $cy * $scale)
    [Win32]::SetCursorPos($x, $y) | Out-Null
    Start-Sleep -Milliseconds 150
    [Win32]::mouse_event([Win32]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 60
    [Win32]::mouse_event([Win32]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
}

$exe = Join-Path $PSScriptRoot "NetworkSim\bin\Debug\net8.0-windows\NetworkSim.exe"
$proc = Start-Process -FilePath $exe -PassThru
try {
    $proc.WaitForInputIdle(15000) | Out-Null
    Start-Sleep -Seconds 2

    # Старт + максимальная скорость (клик по правому краю дорожки: лог. (235, 53))
    Click-ClientPoint $proc 473 53
    Click-ClientPoint $proc 235 53
    Start-Sleep -Milliseconds 300

    # Включаем автоотправку (чекбокс, лог. (268, 53))
    Click-ClientPoint $proc 268 53
    Start-Sleep -Seconds 12
    Save-ClientShot $proc (Join-Path $OutDir "auto1.png") | Out-Null

    # Сброс
    Click-ClientPoint $proc 681 53
    Start-Sleep -Seconds 2
    Save-ClientShot $proc (Join-Path $OutDir "reset.png") | Out-Null
    Write-Host "done, Responding: $($proc.Responding)"
}
finally {
    if (-not $proc.HasExited) { $proc.Kill() }
    $proc.WaitForExit(5000) | Out-Null
}
Write-Host "OK"
