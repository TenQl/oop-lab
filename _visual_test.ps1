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
    [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    public const uint LEFTDOWN = 0x02; public const uint LEFTUP = 0x04;
    public const byte VK_DOWN = 0x28; public const byte VK_RETURN = 0x0D;
    public const uint KEYUP = 0x02;
}
"@
[Win32]::SetProcessDPIAware() | Out-Null

function Send-Key([byte]$vk) {
    [Win32]::keybd_event($vk, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [Win32]::keybd_event($vk, 0, [Win32]::KEYUP, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
}

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

    Click-ClientPoint $proc 473 53          # Старт
    Start-Sleep -Milliseconds 300
    Click-ClientPoint $proc 232 19          # ошибка данных
    Start-Sleep -Milliseconds 250
    Send-Key ([Win32]::VK_DOWN); Send-Key ([Win32]::VK_DOWN); Send-Key ([Win32]::VK_DOWN); Send-Key ([Win32]::VK_RETURN)
    Start-Sleep -Milliseconds 250
    Click-ClientPoint $proc 460 19          # отказ БД
    Start-Sleep -Milliseconds 250
    Send-Key ([Win32]::VK_DOWN); Send-Key ([Win32]::VK_DOWN); Send-Key ([Win32]::VK_DOWN); Send-Key ([Win32]::VK_RETURN)
    Start-Sleep -Milliseconds 250

    Click-ClientPoint $proc 577 53          # задача 1
    Start-Sleep -Seconds 2
    Click-ClientPoint $proc 577 53          # задача 2
    Start-Sleep -Seconds 2
    Click-ClientPoint $proc 577 53          # задача 3

    Start-Sleep -Seconds 6
    Save-ClientShot $proc (Join-Path $OutDir "fix1.png") | Out-Null
    Start-Sleep -Seconds 7
    Save-ClientShot $proc (Join-Path $OutDir "fix2.png") | Out-Null
    Start-Sleep -Seconds 8
    Save-ClientShot $proc (Join-Path $OutDir "fix3.png") | Out-Null
    Start-Sleep -Seconds 8
    Save-ClientShot $proc (Join-Path $OutDir "fix4.png") | Out-Null
    Write-Host "done, Responding: $($proc.Responding)"
}
finally {
    if (-not $proc.HasExited) { $proc.Kill() }
    $proc.WaitForExit(5000) | Out-Null
}
Write-Host "OK"
