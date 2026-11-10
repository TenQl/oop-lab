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

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

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
    return ,@($origin.X, $origin.Y, $w, $hh)
}

function Click-ClientPoint($proc, $cx, $cy, $clientW) {
    # cx, cy - логические координаты внутри клиентской области (при ширине 1090)
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

    # 1. Начальная сцена (моделирование остановлено)
    $info = Save-ClientShot $proc (Join-Path $OutDir "shot1_start.png")
    Write-Host "shot1 done, client: $($info[2])x$($info[3]) at $($info[0]),$($info[1])"

    # 2. Запуск моделирования (кнопка "Старт", центр в лог. коорд. (473, 53))
    Click-ClientPoint $proc 473 53 $info[2]
    Start-Sleep -Milliseconds 800

    # 3. Первая задача (кнопка "Задача", центр в лог. коорд. (577, 53))
    Click-ClientPoint $proc 577 53 $info[2]

    # 4. Повышаем вероятность отказа БД до 0,2: клик по комбинированному
    #    списку (центр в лог. (460, 19)), затем стрелки вниз и Enter.
    Click-ClientPoint $proc 460 19 $info[2]
    Start-Sleep -Milliseconds 300
    Send-Key ([Win32]::VK_DOWN)
    Send-Key ([Win32]::VK_DOWN)
    Send-Key ([Win32]::VK_DOWN)
    Send-Key ([Win32]::VK_RETURN)
    Start-Sleep -Milliseconds 300

    # 5. Ещё две задачи и ожидание отказа/ремонта
    Click-ClientPoint $proc 577 53 $info[2]
    Start-Sleep -Seconds 3
    Click-ClientPoint $proc 577 53 $info[2]
    Start-Sleep -Seconds 14
    Save-ClientShot $proc (Join-Path $OutDir "shot2_running.png") | Out-Null
    Write-Host "shot2 done"

    # 6. Ждём завершения задач и доставки результатов
    Start-Sleep -Seconds 14
    Save-ClientShot $proc (Join-Path $OutDir "shot3_late.png") | Out-Null
    Write-Host "shot3 done"
    Write-Host "HasExited: $($proc.HasExited), Responding: $($proc.Responding)"

    # 7. Полный снимок экрана (поиск скрытых диалогов) + повторный снимок клиента через 5 с
    $screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp = New-Object System.Drawing.Bitmap($screen.Width, $screen.Height)
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)
    $gfx.CopyFromScreen(0, 0, 0, 0, $bmp.Size)
    $gfx.Dispose()
    $bmp.Save((Join-Path $OutDir "shot4_fullscreen.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Start-Sleep -Seconds 5
    Save-ClientShot $proc (Join-Path $OutDir "shot5_final.png") | Out-Null
    Write-Host "shots 4,5 done"
}
finally {
    if (-not $proc.HasExited) { $proc.Kill() }
    $proc.WaitForExit(5000) | Out-Null
}
Write-Host "OK"
