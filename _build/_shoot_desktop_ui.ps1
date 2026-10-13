# Launch the desktop GUI and take a screenshot of its window.
#
# WHY THIS SCRIPT EXISTS (two failed attempts before it):
#   I tried `Start-Job` and `python -c` to launch the GUI. Both times the
#   process died as soon as my command returned -- `Start-Job` tears its
#   child down, and `python -c` blocks. So: use `Start-Process` (a real
#   detached process), then poll for the window *within the same command*
#   and capture it.
#
# !! ASCII-ONLY IDENTIFIERS ON PURPOSE !!
#   PowerShell read this file as ANSI when it had no BOM, so every Chinese
#   variable name turned into mojibake and the script would not parse.
#   Git Bash has the same problem. Keep this file ASCII.
#
# !! The GUI only auto-opens in a *frozen* exe !!
#   Running from source goes to the CLI -- that is deliberate (see
#   run.py::该开图形界面). So this calls learn_gui.main() directly to
#   bypass that check.
param(
    [string]$Out = "$env:TEMP\desktop_gui.png",
    [int]$WaitSec = 25,
    [int]$SettleSec = 3
)
$ErrorActionPreference = "Stop"
$lc = "D:\Users\etgya\Desktop\Project\引擎\LearnCode"

$launcher = "$env:TEMP\_launch_gui.py"
@"
import sys, os
sys.path.insert(0, r'$lc')
os.chdir(r'$lc')
from hybridengine_light.learn_gui import main
main()
"@ | Set-Content $launcher -Encoding UTF8

$env:PYTHONIOENCODING = "utf-8"
$py = (Get-Command python).Source
$proc = Start-Process -FilePath $py -ArgumentList $launcher -PassThru -WindowStyle Normal
Write-Host "  started PID=$($proc.Id)"

# Wait for the window to actually appear (MainWindowHandle != 0).
$win = $null
$deadline = (Get-Date).AddSeconds($WaitSec)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 500
    $q = Get-Process -Id $proc.Id -ErrorAction SilentlyContinue
    if (-not $q) { Write-Host "  XX process exited already"; exit 1 }
    if ($q.MainWindowHandle -ne 0) { $win = $q; break }
}
if (-not $win) { Write-Host "  XX no window within $WaitSec s"; exit 1 }
Write-Host "  window: «$($win.MainWindowTitle)»  handle=$($win.MainWindowHandle)"
Start-Sleep -Seconds $SettleSec      # let the opening animation finish

Add-Type -AssemblyName System.Drawing
$sig = @"
using System;
using System.Runtime.InteropServices;
public class WinCap {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
if (-not ("WinCap" -as [type])) { Add-Type -TypeDefinition $sig }

$h = $win.MainWindowHandle
[void][WinCap]::ShowWindow($h, 9)          # SW_RESTORE
[void][WinCap]::SetForegroundWindow($h)
Start-Sleep -Milliseconds 1200

$r = New-Object WinCap+RECT
[void][WinCap]::GetWindowRect($h, [ref]$r)
$w = $r.Right - $r.Left; $hei = $r.Bottom - $r.Top
Write-Host "  size: $w x $hei  at ($($r.Left),$($r.Top))"
if ($w -le 0 -or $hei -le 0) { Write-Host "  XX bad window size"; exit 1 }

$bmp = New-Object System.Drawing.Bitmap $w, $hei
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Host "  saved: $Out  ($([math]::Round((Get-Item $Out).Length / 1KB, 1)) KB)"
Write-Host "  NOTE: GUI is still running (PID=$($proc.Id)) -- close it yourself when done."
$proc.Id | Set-Content "$env:TEMP\_gui_pid.txt"
