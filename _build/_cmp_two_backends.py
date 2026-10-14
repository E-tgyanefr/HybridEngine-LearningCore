"""⭐ 同一份场景 · Tk 后端 vs C++ 后端 —— **并排**对照台。

⚠ 这一份要回答的问题是作者那条：「在原来的性能基础上提高能效比，尽量提升到原先的一倍」。
   而在这之前，**两个后端从来没有用同一份场景并排量过** ——
   我报过 `tkinter 4.855 ms`（分母是"1000 个对象里 250 个可见"），
   队友报过 `0.11 ms`（分母是"1000 个全可见"）⇒ **两个数不能比**。

⚠⚠ **最大的技术风险：同一个进程里同时开 Tk 窗口和 C++ 子窗口** ——
   队友的探针只验了**一个**子窗口 + 一个按钮，**没验"两个后端同时活着"**。
   ⇒ 所以这个脚本**每一步都打印诊断**，失败时报清楚卡在哪。

判据（缺一条这个数就不算数）：
    ① 两条后端**同一份场景定义**（同一批坐标、同一个 size）
    ② 各自报 **ms/帧** + **真实画了多少个图元**
    ③ ⚠ 两边**裁剪规则相同**（都照 render.py 的 MARGIN×(宽/size)）
    ④ ⚠ **反面**：C++ 的 `WRITTEN_PIXELS` 必须 > 0 —— "什么都没画的光栅化器最快"
"""
import os
import statistics
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
根 = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, 根)
os.chdir(根)

from hybridengine_light import pyengine as E
from hybridengine_light.pyengine import components as C
from hybridengine_light.pyengine_c import backend as CB

宽, 高, 尺寸 = 800, 450, 100


def 造满视野(N: int):
    """N 个对象**铺满视野**（全部可见）—— 分母明确。"""
    s = E.场景("cmp")
    列 = 1
    while 列 * 列 < N:
        列 += 1
    for i in range(N):
        r, c = divmod(i, 列)
        o = s.造("o%d" % i, 2.0 + c * (96.0 / 列), 2.0 + r * (96.0 / 列))
        o.加(C.圆点(半径=1.2, 颜色="红"))
    return s


def 量一次(f, 预热=20, 次=60):
    for _ in range(预热):
        f()
    样 = []
    for _ in range(次):
        t0 = time.perf_counter()
        f()
        样.append((time.perf_counter() - t0) * 1000)
    return statistics.median(样)


N = int(os.environ.get("N", "1000"))
print("  ══ 同一份场景：%d 个圆，铺满视野 ══" % N)
print("    画布 %dx%d · size=%d · MARGIN 两边都用 render.py 的 10.0×(宽/size)" % (宽, 高, 尺寸))
print()

结果 = {}

#: ── ① Tk 后端 ────────────────────────────────────────────────
print("  ── ① Tk 后端 ──")
try:
    s1 = 造满视野(N)
    #: ⚠ 参数名**照签名抄**（`Tk后端` 用 `size`，`游戏` 用 `尺寸`）——
    #:   我第一版把 `尺寸` 传给了 `Tk后端` ⇒ TypeError。
    #:   **本仓的规矩：别猜 API，先 inspect.signature。**
    g1 = E.游戏(场景=s1, 宽=宽, 高=高, 尺寸=尺寸,
                后端=E.Tk后端(宽=宽, 高=高, size=尺寸))
    g1.一步(1 / 60)
    item数 = len(g1.后端._canvas.find_all())
    结果["tk_item"] = item数
    print("     画布 item 数（**真实建了多少**）= %d" % item数)
    结果["tk_ms"] = 量一次(lambda: g1.一步(1 / 60))
    print("     中位 %.4f ms/帧" % 结果["tk_ms"])
    g1.关()
except Exception as e:
    print("     ⚠ Tk 后端失败：%s: %s" % (type(e).__name__, e))
    结果["tk_ms"] = None

#: ── ② C++ 后端 ──────────────────────────────────────────────
print()
print("  ── ② C++ 后端 ──")
try:
    父 = None
    _根 = None
    try:
        import tkinter as tk
        import ctypes as _ct
        _根 = tk.Tk()
        _根.geometry("%dx%d+40+40" % (宽, 高))
        _根.update()
        #: ⚠⚠⚠ **不能用 `_根.winfo_id()`，也不能 `withdraw()`** —— 两条都是实测踩出来的：
        #:
        #:   ① **Tk 的 `winfo_id()` 返回的不是顶层窗**：
        #:        `GetAncestor(winfo_id(), GA_ROOT)` 与 `winfo_id()` **不相等**（实测过）。
        #:      ⇒ 要挂子窗口就该挂**顶层窗**，用 `GA_ROOT` 取。
        #:
        #:   ② ⚠ **`withdraw()` 会让子窗口的 `IsWindowVisible()` 变成假** ——
        #:        因为 `IsWindowVisible` 对**后代**会一直查到根：
        #:        根被 withdraw ⇒ **整棵子树都算不可见**。
        #:      ⇒ 我第一版 withdraw 了根，看到 `子窗口可见=False`，
        #:        **一度以为是这个 API 误报**（还在文档里标了"待办"）。
        #:      ⚠ **真相是我自己的测量前提错了** —— 又一次"仪器坏了，不是被测物坏了"。
        _ct.windll.user32.GetAncestor.restype = _ct.c_void_p
        父 = _ct.windll.user32.GetAncestor(_根.winfo_id(), 2)   #: GA_ROOT = 2
        print("     （父窗口用 GetAncestor(GA_ROOT)：winfo_id=%d → 顶层=%d）"
              % (_根.winfo_id(), 父 or 0))
    except Exception as e:
        print("     （拿不到 Tk 父窗，走顶层窗口路线：%s）" % type(e).__name__)

    s2 = 造满视野(N)
    b2 = CB.Cpp后端(宽=宽, 高=高, size=尺寸, 标题="对照台", 背景="白", 父窗口=父)
    g2 = E.游戏(场景=s2, 宽=宽, 高=高, 尺寸=尺寸, 后端=b2)   #: `游戏` 用 尺寸
    g2.一步(1 / 60)
    print("     后端名 = %s" % CB.后端名())
    print("     允许裁剪 = %s" % getattr(b2, "允许裁剪", "?"))
    结果["cpp_ms"] = 量一次(lambda: g2.一步(1 / 60))
    print("     中位 %.4f ms/帧" % 结果["cpp_ms"])
    #: ⭐ **反面判据**：什么都不画的光栅化器最快 ⇒ 必须证明"真画了像素"。
    try:
        结果["cpp_pixels"] = b2.写过的像素()
        结果["cpp_frames"] = b2.呈现过的帧()
        print("     写过的像素 = %d · 呈现过的帧 = %d · 子窗口可见 = %s"
              % (结果["cpp_pixels"], 结果["cpp_frames"], b2.子窗口可见()))
    except Exception as e:
        结果["cpp_pixels"] = None
        print("     ⚠ 拿不到像素计数：%s" % e)
    b2.关闭()
except Exception as e:
    import traceback
    print("     ⚠ C++ 后端失败：%s: %s" % (type(e).__name__, e))
    traceback.print_exc()
    结果["cpp_ms"] = None

#: ── ③ 并排 ──────────────────────────────────────────────────
print()
print("  ══ 并排 ══")
print("    Tk   ms/帧 = %s   item = %s" % (结果.get("tk_ms"), 结果.get("tk_item")))
print("    C++  ms/帧 = %s   item = %s   pixels = %s"
      % (结果.get("cpp_ms"), 结果.get("cpp_item"), 结果.get("cpp_pixels")))
if 结果.get("tk_ms") and 结果.get("cpp_ms"):
    r = 结果["tk_ms"] / 结果["cpp_ms"]
    print("    ⇒ **%.1f 倍**" % r)
    print("    ⇒ 60 帧预算 16.67 ms 下能画的图元数：")
    print("         Tk  ≈ %.0f 个" % (16.67 / (结果["tk_ms"] / max(结果.get("tk_item") or 1, 1))))
    print("         C++ ≈ %.0f 个" % (16.67 / (结果["cpp_ms"] / max(N, 1))))
else:
    print("    ⚠ 有一边没量到 —— **不给倍数**（两个口径硬凑是本仓犯过的错）")
