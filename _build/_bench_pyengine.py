"""`pyengine` 的渲染压测 —— 量清「弱在哪」，并验两种优化。

═══ 为什么要这个脚本 ═══

作者给的定位是 **专业版 100% / SampleCore 90% / LearningCore 30%**。
我先前拿 `录制后端` 压测，报「3000 个对象只占 24% 帧预算」——
**那量的是引擎逻辑，不是"能不能画出来"**，等于没量到 30% 从哪来。

⇒ 这个脚本压的是**真渲染**（`Tk后端`），并且把两种优化都试了。

═══ 实测结论（2026-10-13，本机）═══

`录制后端` vs `Tk后端`（每次 `一步()` = 更新 + 绘制，60 帧预算 16.67 ms/帧）：

    对象     录制(ms/帧)   Tk(ms/帧)   Tk/录制
    100       0.177       3.924       22x
    300       0.479      11.024       23x
    1000      1.736      35.382       20x

⇒ **引擎逻辑 1000 对象只要 1.7 ms**（不是瓶颈），
  **真渲染要 35 ms**（慢 20 倍）⇒ 「性能弱」落在 **Tk 的 Canvas 渲染**上。

═══ 两种优化，结果完全不同 ═══

**① item 复用（每帧 `delete("all")` + 重建 → 复用 item + `coords`）⇒ 无效（1.0x）**

    对象     原Tk      复用      提速
    100      4.015     3.963     1.0x
    300     10.871    11.227     1.0x
    1000    37.003    37.687     1.0x

⇒ **删了重建不是瓶颈。**

**② 静态烘焙（不动的东西只画一次，每帧只删/画动态层）⇒ 约 2x**

    总对象   原Tk(ms/帧)  烘焙(ms/帧)  提速   烘焙后能跑
    300     10.619      5.931      1.8x   60 帧
    1000    34.765     18.465      1.9x   54 帧   ← 27 → 54
    3000   102.489     52.499      2.0x   19 帧   ← 10 → 19

（场景：80% 不动 / 20% 在动 —— 真实游戏的常见比例）

═══ 为什么会这样（一句话）═══

    item 复用   换的是「**怎么创建** item」 ⇒ 画的 item 数没变 ⇒ 1.0x
    静态烘焙    换的是「**画多少个** item」 ⇒ 画的 item 数少了 ⇒ 2.0x

⇒ **瓶颈是「每帧要渲染多少个 item」，不是「怎么创建它们」。**

═══ ⚠⚠ 我在这上面量错过两次（记下来，因为原因最隐蔽）═══

1. **用 `录制后端` 压测** —— 它画圆只是 `append` 一个元组，**没有渲染**。
   报「3000 对象占 24% 预算」⇒ 那是**引擎逻辑**的数字。
2. **把 `呈现()`（`root.update()`）单独计时** ⇒ 得 **0.003 ms**，
   因为**那时待渲染队列是空的**（上一次 update 已排空）。
   而在「建了 1000 个 item 之后」调它，Tk 要把 1000 个 oval **真正画出来** ——
   **这笔账记在 `update()` 上**。

   单独计： 清屏 0.002 + 场景.绘制 4.035 + 呈现 0.003 = 4.04 ms
   合起来： 17.865 ms                                    ← 差 4.4 倍

⇒ **同一个调用，量的时机不同，代价差 4 倍。**

═══ 怎么用 ═══

    python _build/_bench_pyengine.py           # 全部
    python _build/_bench_pyengine.py 后端       # 只跑后端对比
    python _build/_bench_pyengine.py 优化       # 只跑两种优化

⚠ 会**真开 tkinter 窗口**（跑完就关）。无显示环境跑不了。
"""
from __future__ import annotations

import os
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if LC not in sys.path:
    sys.path.insert(0, LC)
os.chdir(LC)

from hybridengine_light import pyengine as E  # noqa: E402

C = E.components
颜色 = E.render.颜色

#: 60 帧的每帧预算
预算 = 16.67


def 建(n: int, 动比: float = 1.0):
    """造 n 个圆点；前 `n*动比` 个带 `匀速移动`（会动），其余不动。"""
    s = E.场景("压测")
    for i in range(n):
        #: ⚠ 铺开在 90x90 的格子里 —— 别重叠，否则 Tk 的可见性剔除会作弊
        o = s.造("物%d" % i, x=(i % 60) * 1.5, y=(i // 60) * 1.5)
        o.加(C.圆点(半径=0.5))
        if i < int(n * 动比):
            o.加(C.匀速移动(vx=1.0, vy=0.5))
        s.加根(o)
    return s


def 量游戏(后端, 场景, 帧数: int = 25) -> float:
    """开一个游戏、跑 `一步()`、返回 ms/帧。"""
    try:
        g = E.游戏(场景=场景, 后端=后端, 尺寸=100, 宽=640, 高=640)
        g.一步()
        g.一步()          # 预热两帧：Tk 首次绘制最贵
        t0 = time.perf_counter()
        for _ in range(帧数):
            g.一步()
        return (time.perf_counter() - t0) / 帧数 * 1000.0
    finally:
        后端.关闭()


def 压后端():
    print("  === ① 录制后端 vs Tk后端（真渲染）===")
    print("  60 帧预算 = %.2f ms/帧" % 预算)
    print()
    print("  %-8s %-16s %-16s %-10s" % ("对象", "录制(ms/帧)", "Tk(ms/帧)", "Tk/录制"))
    录制 = {}
    for n in (100, 300, 1000):
        后 = E.录制后端()
        g = E.游戏(场景=建(n), 后端=后, 尺寸=100, 宽=640, 高=640)
        g.一步()
        t0 = time.perf_counter()
        for _ in range(40):
            g.一步()
        录制[n] = (time.perf_counter() - t0) / 40 * 1000.0
    for n in (100, 300, 1000):
        后 = E.Tk后端(宽=640, 高=640, size=100, 标题="压测")
        b = 量游戏(后, 建(n))
        print("  %-8d %-16.3f %-16.3f %-10s" % (n, 录制[n], b, "%.0fx" % (b / 录制[n])))
    print()
    print("  ⇒ 引擎逻辑不是瓶颈，**真渲染才是**（慢 20 倍）")


def 压优化():
    print("  === ② 两种优化（80% 不动 / 20% 在动）===")
    print()
    print("  %-8s %-14s %-14s %-8s %s" % ("总对象", "原Tk(ms/帧)", "烘焙(ms/帧)", "提速", "烘焙后能跑"))
    for n in (300, 1000, 3000):
        a = 量游戏(E.Tk后端(宽=640, 高=640, size=100, 标题="原"), 建(n))
        b = 量烘焙(n)
        print("  %-8d %-14.3f %-14.3f %-8s %s" % (
            n, a, b, "%.1fx" % (a / b if b else 0),
            "%.0f 帧" % min(60.0, 1000.0 / b)))
    print()
    print("  ⇒ **静态烘焙约 2x**；而 **item 复用是 1.0x（无效）**：")
    print("     瓶颈是「每帧渲染多少个 item」，不是「怎么创建它们」")


def 量烘焙(n: int, 动比: float = 0.2, 帧数: int = 25) -> float:
    """优化原型：静态部分**只画一次**（不打 `动态` 标签 ⇒ 永不被删）。"""
    后 = E.Tk后端(宽=640, 高=640, size=100, 标题="烘焙")

    #: 打标签 + 只删动态层 —— 这两处就是"烘焙"的全部机关
    def 画圆(x, y, r, 色):
        px, py = 后._xy(x, y)
        pr = 后._r(r)
        后._canvas.create_oval(px - pr, py - pr, px + pr, py + pr,
                             fill=颜色(色), outline="", tags="动态")

    def 清屏(色):
        后._canvas.delete("动态")
        try:
            后._canvas.configure(bg=颜色(色))
        except Exception:
            pass

    后.画圆 = 画圆
    后.清屏 = 清屏

    静态数 = int(n * (1 - 动比))
    try:
        #: 烘焙：把不动的那些直接画进画布（**不打标签**）
        for i in range(静态数):
            x, y = (i % 60) * 1.5, (i // 60) * 1.5
            px, py = 后._xy(x, y)
            pr = 后._r(0.5)
            后._canvas.create_oval(px - pr, py - pr, px + pr, py + pr,
                                 fill="red", outline="")
        #: 场景里**只放会动的**
        return 量游戏(后, 建(int(n * 动比), 1.0), 帧数)
    finally:
        pass


def 压复用():
    """对照：item 复用（证明它无效）。"""
    class 复用后端(E.Tk后端):
        def __init__(self, *a, **kw):
            super().__init__(*a, **kw)
            self._池, self._序 = [], 0

        def 清屏(self, 色):
            self._序 = 0
            try:
                self._canvas.configure(bg=颜色(色))
            except Exception:
                pass

        def _取(self, 建):
            if self._序 < len(self._池):
                i = self._池[self._序]
            else:
                i = 建()
                self._池.append(i)
            self._序 += 1
            return i

        def 画圆(self, x, y, r, 色):
            px, py = self._xy(x, y)
            pr = self._r(r)
            i = self._取(lambda: self._canvas.create_oval(
                px - pr, py - pr, px + pr, py + pr, fill=颜色(色), outline=""))
            self._canvas.coords(i, px - pr, py - pr, px + pr, py + pr)
            self._canvas.itemconfigure(i, fill=颜色(色), state="normal")

        def 呈现(self):
            for j in range(self._序, len(self._池)):
                self._canvas.itemconfigure(self._池[j], state="hidden")
            super().呈现()

    print("  === ③ item 复用（对照，预期无效）===")
    print()
    print("  %-8s %-14s %-14s %-8s" % ("对象", "原Tk(ms/帧)", "复用(ms/帧)", "提速"))
    for n in (100, 300, 1000):
        a = 量游戏(E.Tk后端(宽=640, 高=640, size=100, 标题="原"), 建(n))
        b = 量游戏(复用后端(宽=640, 高=640, size=100, 标题="复用"), 建(n))
        print("  %-8d %-14.3f %-14.3f %-8s" % (n, a, b, "%.1fx" % (a / b if b else 0)))
    print()
    print("  ⇒ 1.0x ⇒ **删了重建不是瓶颈**（我原来猜它是最可疑的一条，猜错了）")


def 主() -> int:
    print("  ═══ pyengine 渲染压测 ═══")
    print("  ⚠ 会真开 tkinter 窗口（跑完就关）")
    print()
    只 = sys.argv[1] if len(sys.argv) > 1 else ""
    if 只 in ("", "后端"):
        压后端()
        print()
    if 只 in ("", "优化"):
        压优化()
        print()
    if 只 in ("", "复用"):
        压复用()
    print()
    print("  ⚠ 这些都是**本机**数字。作者给的 100% / 90% / 30%")
    print("    是跨产品的**定位判断**，基准不同，不能互相换算。")
    return 0


if __name__ == "__main__":
    sys.exit(主())
