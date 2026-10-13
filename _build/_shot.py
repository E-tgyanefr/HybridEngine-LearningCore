"""启动桌面 GUI，**让它自己报客户区位置**，然后精确截图。

⚠⚠ 为什么需要这个（我在这上面栽了三次）：

  我原来直接 `GetWindowRect` + `CopyFromScreen` 截屏，结果**三次**都截错了：

    ① 第一次：Edge 盖在应用上面 ⇒ 我截到的是 Edge，
       于是误判"主页布局是坏的（内容挤在右边）"。
       实际上 Tk 量的结果是**完美居中**（左右各 306px）。
    ② 第二次：窗口被挪过，截图区域偏了 ⇒ 左边出现一条黑边和别的窗口。
    ③ 第三次：同样的偏移 ⇒ 看起来像"应用被推到右边"。

  ⇒ 根因：**我在猜窗口在屏幕上的位置，而且猜的区域里可能有别的窗口。**

  ⇒ 正确做法：**问应用自己**。
     Tk 的 `winfo_rootx/rooty/width/height` 给的就是客户区的**绝对位置和尺寸**，
     和哪个窗口在前面、有没有 DPI 缩放都无关。
     应用把这三个数打到 stdout，我照它截 —— 不再猜。

⚠ 还有一条：截之前**把窗口放到 (0,0) 并且给足尺寸**，
  否则它可能有一半在屏幕外（那就真的截不到了）。
"""
import json
import os
import re
import subprocess
import sys
import time

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）

#: 启动脚本 —— 建界面、把几何打到 stdout、可选地执行一个动作、然后等死
启动器 = r"""
import json, os, sys
sys.path.insert(0, r"%(lc)s")
os.chdir(r"%(lc)s")
from hybridengine_light.learn_gui import 界面

a = 界面()
a.geometry("1180x760+0+0")          # 放到左上角，保证整个窗口在屏幕内

def 报到():
    a.update_idletasks()
    a.update()
    #: ⚠ 这就是"问应用自己"的那一步 —— 客户区的绝对位置
    print("GEOM " + json.dumps({
        "x": a.winfo_rootx(), "y": a.winfo_rooty(),
        "w": a.winfo_width(), "h": a.winfo_height(),
        "sw": a.winfo_screenwidth(), "sh": a.winfo_screenheight(),
    }), flush=True)

动作 = %(act)r
if 动作:
    a.after(1500, lambda: (getattr(a, 动作)(), None)[1])
a.after(3200, 报到)
a.after(%(hold)d, a.destroy)
a.mainloop()
"""


def 抓屏(x: int, y: int, w: int, h: int, 存到: str) -> bool:
    """用 ctypes 调 GDI 抓一块屏幕，存成 BMP。

    ⚠ 为什么不用 PowerShell（`System.Drawing`）：
      从 Python 里 `subprocess.run(["pwsh", ...])` 会 `FileNotFoundError` ——
      Python 的 PATH 里没有 `pwsh`（它只在 PowerShell 会话里）。
      而**外面套一层 PowerShell** 又会绕回"我被别的窗口挡住"那个老问题。
    ⚠ 为什么不用 `PIL`：项目铁律**零第三方依赖**。
      ⇒ `ctypes` + GDI 是标准库能拿到的最直接的办法。
      存 BMP 而不是 PNG：BMP 头我手写就行，PNG 要 zlib + CRC（那是两百行）。
      ⚠ 代价是文件大（1180x760 约 3.6 MB）—— 但截图是**临时**的，
        看完就删，不值得为它引依赖。
    """
    import ctypes
    from ctypes import wintypes

    user32 = ctypes.windll.user32
    gdi32 = ctypes.windll.gdi32

    屏 = user32.GetDC(0)
    内存 = gdi32.CreateCompatibleDC(屏) if False else gdi32.CreateCompatibleDC(屏)
    位图 = gdi32.CreateCompatibleBitmap(屏, w, h)
    gdi32.SelectObject(内存, 位图)
    #: SRCCOPY = 0x00CC0020；从屏幕 (x,y) 拷 (w,h) 到内存位图左上角
    gdi32.BitBlt(内存, 0, 0, w, h, 屏, x, y, 0x00CC0020)

    #: BITMAPINFOHEADER + 像素。存成 32 位 BMP（负高度 = 自上而下）
    class BITMAPINFOHEADER(ctypes.Structure):
        _fields_ = [("biSize", wintypes.DWORD), ("biWidth", wintypes.LONG),
                    ("biHeight", wintypes.LONG), ("biPlanes", wintypes.WORD),
                    ("biBitCount", wintypes.WORD), ("biCompression", wintypes.DWORD),
                    ("biSizeImage", wintypes.DWORD),
                    ("biXPelsPerMeter", wintypes.LONG),
                    ("biYPelsPerMeter", wintypes.LONG),
                    ("biClrUsed", wintypes.DWORD), ("biClrImportant", wintypes.DWORD)]

    头 = BITMAPINFOHEADER()
    头.biSize = ctypes.sizeof(BITMAPINFOHEADER)
    头.biWidth, 头.biHeight = w, -h
    头.biPlanes, 头.biBitCount, 头.biCompression = 1, 32, 0
    头.biSizeImage = w * h * 4
    缓冲 = ctypes.create_string_buffer(w * h * 4)
    行数 = gdi32.GetDIBits(内存, 位图, 0, h, 缓冲, ctypes.byref(头), 0)
    gdi32.DeleteObject(位图)
    gdi32.DeleteDC(内存)
    user32.ReleaseDC(0, 屏)
    if 行数 == 0:
        print("    XX GetDIBits 返回 0")
        return False

    #: ── 写 BMP 文件 ──
    import struct
    像素 = 缓冲.raw
    文件头 = struct.pack("<2sIHHI", b"BM", 14 + 40 + len(像素), 0, 0, 14 + 40)
    信息头 = struct.pack("<IiiHHIIiiII", 40, w, -h, 1, 32, 0, len(像素), 0, 0, 0, 0)
    with open(存到, "wb") as f:
        f.write(文件头 + 信息头 + 像素)
    return True


def 拍(动作: str | None, 存到: str, 保持秒: int = 6) -> bool:
    脚本 = os.path.join(os.environ["TEMP"], "_shoot_gui.py")
    with open(脚本, "w", encoding="utf-8", newline="\n") as f:
        f.write(启动器 % {"lc": LC, "act": 动作, "hold": 保持秒 * 1000})

    p = subprocess.Popen([sys.executable, 脚本], stdout=subprocess.PIPE,
                         stderr=subprocess.STDOUT, text=True,
                         encoding="utf-8", errors="replace")
    #: 等它把几何打出来（同时把窗口抬到最前）
    geo = None
    t0 = time.time()
    while time.time() - t0 < 20:
        行 = p.stdout.readline()
        if not 行:
            break
        if 行.startswith("GEOM "):
            geo = json.loads(行[5:])
            break
        if 行.strip():
            print("    [应用] " + 行.rstrip())

    if not geo:
        print("    XX 应用没报到几何")
        p.kill()
        return False
    print("    客户区：%(w)dx%(h)d @ (%(x)d,%(y)d)  屏幕 %(sw)dx%(sh)d" % geo)

    #: 把窗口抬到最前，再等一下让它真的画完 —— 不然截到的是它下面的东西
    import ctypes
    hwnd = ctypes.windll.user32.FindWindowW(None, None)   # 占位，下面按标题找
    标题 = "HybridEngine-light —— 学编程，从第一行代码看到画面"
    hwnd = ctypes.windll.user32.FindWindowW(None, 标题)
    if hwnd:
        ctypes.windll.user32.SetForegroundWindow(hwnd)
        ctypes.windll.user32.BringWindowToTop(hwnd)
    time.sleep(1.2)

    存到 = os.path.abspath(存到)
    if not 抓屏(geo["x"], geo["y"], geo["w"], geo["h"], 存到):
        p.kill()
        return False
    print("    存到 %s（%.1f KB）" % (存到, os.path.getsize(存到) / 1024))
    try:
        p.wait(timeout=保持秒 + 6)
    except subprocess.TimeoutExpired:
        p.kill()
    return True


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    动作 = sys.argv[1] if len(sys.argv) > 1 and sys.argv[1] != "-" else None
    存到 = sys.argv[2] if len(sys.argv) > 2 else os.path.join(
        os.environ["TEMP"], "gui_shot.png")
    print("  动作：%s" % (动作 or "（不动作，停在主页）"))
    sys.exit(0 if 拍(动作, 存到) else 1)
