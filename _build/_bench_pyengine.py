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

═══ ④ 再往下压：静态 item 合并成**一张位图**（2026-10-13 实测，见 `压合图()`）═══

我原来推断"烘焙之后静态 item 就不再花钱了" —— **实测推翻了**：

    总对象   原Tk(ms/帧)  烘焙(ms/帧)  合图(ms/帧)   合图/烘焙
    300     5.229       4.836      1.615        0.33x
    1000    18.529      15.760     4.598        0.29x
    3000    54.737      45.681     11.407       0.25x

（"合图" = 静态层只剩 **1 个 image item**，铺满整张画布；它会随损坏区域一起被重画，
  所以这是**最有利于"合并"**的假设 —— 结论只会更保守。）

⇒ **静态 item 每帧仍在被重画**（动态 item 移动时损坏的区域压着它们），
  把几十上百个静态 item 换成 1 张位图还能再快 **3~4 倍**。

⚠⚠ **但它没有落地成产品**，因为纯 tkinter **没有光栅化器**：
  要把 oval / line / 文字画进位图，得有 PIL（依赖）或 Ghostscript（postscript 那条路），
  而本项目的一条硬约束是**学员侧零依赖**。这件事已经报给 lead 定方向；
  在那之前这个模式只作为**上限实验**留着（`python _build/_bench_pyengine.py 合图`）。

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
    """用**真·`Tk后端(烘焙=True)`** 量（不是打补丁的原型）。

    ⚠⚠ 这一条是**自我纠正**：我第一版这里猴子补丁了 `画圆`/`清屏`，
      然后拿那个数字当"产品能到多少"。那是**不成立的** ——
      原型和落地实现是两份代码，原型快不等于产品快。

    ⚠ 还有一条：`造()` **已经**把对象加到根上了，
      所以场景里只能 `造()`，**不能再 `加根()`**（我第一版两条都用了 ⇒
      每个对象被加两遍 ⇒ 所有对象数都是翻倍的，压测数字全偏高）。
    """
    #: 静态那部分：`tags=()` 画进画布（不打 动态 标签 ⇒ 永不被删）
    def 画静(后):
        for i in range(int(n * (1 - 动比))):
            x, y = (i % 60) * 1.5, (i // 60) * 1.5
            px, py = 后._xy(x, y)
            pr = 后._r(0.5)
            后._canvas.create_oval(px - pr, py - pr, px + pr, py + pr,
                                 fill="red", outline="")

    后 = E.Tk后端(宽=640, 高=640, size=100, 标题="烘焙", 烘焙=True)
    后.烘焙静态(画静)
    #: 场景里**只放会动的**那些
    return 量游戏(后, 建(int(n * 动比), 1.0), 帧数)


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


def 量烘焙图(n: int, 动比: float = 0.2, 帧数: int = 25) -> float:
    """把静态层**合并成一张位图**（1 个 image item）—— 量它的上限。

    ⚠⚠ 这是"合并静态图为一张 PhotoImage"那条优化**能不能有收益**的上限实验：
      它不是产品实现（纯 tkinter 里**没有光栅化器** —— 把几十个 oval/文字
      画成位图需要 PIL 或 Ghostscript），只是把"静态层只剩 1 个 item"
      这件事**做出来**，看每帧能快多少。
      · 如果这里**明显更快** ⇒ 那条优化值得为它引入依赖；
      · 如果这里**和烘焙差不多** ⇒ 瓶颈不在静态 item 的数量上，那条优化**无效**，
        不用去折腾光栅化。

    ⚠ 图铺满整张画布（最"重"的那种静态层：每一块被损坏的区域都压着它），
      这对"合并"是**最有利**的假设 —— 结论只会更保守。
    """
    后 = E.Tk后端(宽=640, 高=640, size=100, 标题="合图", 烘焙=True)
    图 = 后._tk.PhotoImage(width=640, height=640)
    图.put("red", to=(0, 0, 640, 640))
    后._canvas.create_image(0, 0, anchor="nw", image=图, tags=())
    后._合图 = 图                       # 别被 GC 回收
    return 量游戏(后, 建(int(n * 动比), 1.0), 帧数)


def 压合图():
    """④ 静态层合并成一张位图（上限实验）——判断那条优化值不值得做。"""
    print("  === ④ 静态层合并成**一张位图**（上限实验：静态 item 从 N 个变 1 个）===")
    print("  ⚠ 纯 tkinter 没有光栅化器 —— 这里只量「能快多少」，不是产品实现")
    print()
    print("  %-8s %-14s %-14s %-14s %-8s" % ("总对象", "原Tk(ms/帧)", "烘焙(ms/帧)",
                                            "合图(ms/帧)", "合图/烘焙"))
    for n in (300, 1000, 3000):
        a = 量游戏(E.Tk后端(宽=640, 高=640, size=100, 标题="原"), 建(n))
        b = 量烘焙(n)
        c = 量烘焙图(n)
        print("  %-8d %-14.3f %-14.3f %-14.3f %-8s"
              % (n, a, b, c, "%.2fx" % (c / b if b else 0)))
    print()
    print("  ⇒ 这一列的读法：**越接近 1.0x，说明静态 item 的数量越不是瓶颈**。")
    print("     接近 1.0 ⇒ 「合并成一张图」不值得做（还要为此引入光栅化依赖）。")


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
    if 只 in ("", "合图"):
        压合图()
        print()
    print("  ⚠ 这些都是**本机**数字。作者给的 100% / 90% / 30%")
    print("    是跨产品的**定位判断**，基准不同，不能互相换算。")
    return 0


if __name__ == "__main__":
    sys.exit(主())
