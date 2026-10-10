"""渲染后端抽象 —— 引擎不直接依赖 tkinter。

## 为什么要有这一层

引擎的核心（场景、组件、循环）**不该知道窗口是怎么来的**。分开之后：

  · `录制后端`   —— 只把绘制指令记进列表，**不建窗口**
    ⇒ 场景/组件/变换/相机这些核心逻辑**可以在无头环境里测**
      （这是本项目一直的做法：能让判据覆盖的就不留给肉眼）
  · `Tk后端`     —— 现在用的：tkinter Canvas
  · `引擎后端`   —— 有 `hybridengine.dll` 时改用它（留接口，见 `可用引擎后端`）

## 坐标约定（与 `Stage` 一致，学员不用学第二套）

虚拟坐标 `0..size`，**原点在左下角**（和数学课一样）。
后端负责把它翻成屏幕像素。

⚠ 两个轴必须**各用各的比例**。这里踩过一次：y 的缩放原来用了宽度比例，
  方形画布看不出问题，一旦 `width ≠ height` 就错位
  （500×250 时 y=50 会算成 0 而不是 125）。
"""
from __future__ import annotations

import time
from typing import Protocol


class 后端(Protocol):
    """渲染后端要提供的东西。"""

    def 清屏(self, 颜色: str) -> None: ...
    def 画圆(self, x: float, y: float, r: float, 颜色: str) -> None: ...
    def 画矩形(self, x: float, y: float, w: float, h: float, 颜色: str) -> None: ...
    def 画线(self, x1: float, y1: float, x2: float, y2: float,
             颜色: str, 粗: float) -> None: ...
    def 画文字(self, x: float, y: float, 内容: str, 颜色: str, 字号: int) -> None: ...
    def 呈现(self) -> None: ...
    def 关闭(self) -> None: ...
    @property
    def 还开着(self) -> bool: ...


class 录制后端:
    """只记录指令，不建窗口。**判据用它。**

    ⚠ 这不是"假后端"——它就是引擎的一个正经后端（离屏）。
      有了它，场景/组件/变换/相机的正确性**全都能写成判据**，
      而不用去截屏比对像素。
    """

    def __init__(self) -> None:
        self.指令: list[tuple] = []
        self.帧数 = 0
        #: 每一帧的指令快照（`呈现` 时存一份）。
        #  ⚠ 为什么要有它：`指令` 每帧清空（与真实后端语义一致），
        #    但判据有时要看"上几帧画了什么"（比如确认某个对象消失）。
        self.帧历史: list[list[tuple]] = []

    def 清屏(self, 颜色: str) -> None:
        self.指令.append(("清屏", 颜色))

    def 画圆(self, x, y, r, 颜色) -> None:
        self.指令.append(("圆", x, y, r, 颜色))

    def 画矩形(self, x, y, w, h, 颜色) -> None:
        self.指令.append(("矩形", x, y, w, h, 颜色))

    def 画线(self, x1, y1, x2, y2, 颜色, 粗) -> None:
        self.指令.append(("线", x1, y1, x2, y2, 颜色, 粗))

    def 画文字(self, x, y, 内容, 颜色, 字号) -> None:
        self.指令.append(("文字", x, y, 内容, 颜色, 字号))

    def 呈现(self) -> None:
        # ⚠⚠ 必须在**这一帧结束时清空指令**。
        #   我第一版只是 `帧数 += 1`，指令一直累积 ——
        #   于是判据读 `有文字()` 拿到的是**第一帧**的内容，
        #   改了内容也看不出来（实测就这么红的）。
        #   真实后端本来每帧重画（tkinter 会 `delete("all")`），
        #   录制后端必须与它**语义一致**，否则拿它写的判据都是假的。
        self.帧数 += 1
        self.帧历史.append(list(self.指令))
        self.指令.clear()

    def 关闭(self) -> None:
        pass

    @property
    def 还开着(self) -> bool:
        return True

    # ── 判据用的查询 ──
    # ⚠ 这些查的是**最后一帧**（不是 `指令` —— 它在 `呈现` 时被清空了）。
    #   真实后端无法"回看上一帧"，所以这是录制后端特有的能力，
    #   专给判据用：它替代的是"截屏之后数像素"。
    def 本帧(self, 第几帧: int = -1) -> list[tuple]:
        if not self.帧历史:
            return []
        return self.帧历史[第几帧]

    def _本帧某类(self, 类: str) -> list[tuple]:
        return [d for d in self.本帧() if d[0] == 类]

    def 有圆(self) -> list[tuple]:
        return self._本帧某类("圆")

    def 有矩形(self) -> list[tuple]:
        return self._本帧某类("矩形")

    def 有文字(self) -> list[tuple]:
        return self._本帧某类("文字")

    def 有线(self) -> list[tuple]:
        return self._本帧某类("线")

    def 清空(self) -> None:
        self.指令.clear()
        self.帧历史.clear()
        self.帧数 = 0


def 颜色(名: str) -> str:
    """颜色名 → tkinter 认的字符串。未知名字原样返回（交给底下提示）。

    ⚠⚠ 这里踩了两个坑，都记下来：

    ① **相对层级数错**：本文件在 `pyengine/` 里，而 `stage.py` 在**上一级**。
       我第一版写 `from .stage import color_of` ⇒ 去找 `pyengine.stage`，
       ImportError。

    ② **`except Exception` 把它吞了** —— 于是 `颜色("黑")` 静默返回 `"黑"`，
       一路传到 tkinter 才炸 `unknown color name "黑"`。
       而报错现场离真因隔了四层调用，非常难查。

    ⇒ 现在**不吞异常**：import 放在函数外（模块级），
      真出问题就在加载时报，一眼看到是谁。
      也**不再有 try/except** —— 吞异常换来的"稳"是假的：
      它把"颜色没生效"变成"运行时莫名报错"。
    """
    # ⚠ 延迟到模块级 import 完成后再引用（`stage` 会 import tkinter，
    #   无头环境可能没有 —— 所以这里**保留**一次容错，但只针对 ImportError，
    #   而且失败时明确说清颜色会原样传下去）。
    from ..stage import color_of
    return color_of(名)


class Tk后端:
    """tkinter Canvas 后端。**只在真的要用窗口时才建 Tk。**"""

    def __init__(self, 宽: int, 高: int, size: int, 标题: str = "HybridEngine",
                 背景: str = "白") -> None:
        import tkinter as tk
        self.宽, self.高, self.size = 宽, 高, size
        self._tk = tk
        self._root = tk.Tk()
        self._root.title(标题)
        self._root.resizable(False, False)
        self._canvas = tk.Canvas(self._root, width=宽, height=高, highlightthickness=0)
        self._canvas.pack()
        self._canvas.configure(bg=颜色(背景))
        self._关 = False
        self._root.protocol("WM_DELETE_WINDOW", self.关闭)

    # 坐标换算：虚拟坐标 → 屏幕像素（**两个轴各用各的比例**）
    def _xy(self, x: float, y: float) -> tuple[float, float]:
        kx = self.宽 / self.size
        ky = self.高 / self.size
        return (x * kx, self.高 - y * ky)

    def _r(self, r: float) -> float:
        return r * (self.宽 / self.size)

    def 清屏(self, 色: str) -> None:
        self._canvas.delete("all")
        try:
            self._canvas.configure(bg=颜色(色))
        except Exception:
            pass

    def 画圆(self, x, y, r, 色) -> None:
        px, py = self._xy(x, y)
        pr = self._r(r)
        self._canvas.create_oval(px - pr, py - pr, px + pr, py + pr,
                                 fill=颜色(色), outline="")

    def 画矩形(self, x, y, w, h, 色) -> None:
        x1, y1 = self._xy(x, y + h)          # 左下角在屏幕上是"下方"
        x2, y2 = self._xy(x + w, y)
        self._canvas.create_rectangle(x1, y1, x2, y2, fill=颜色(色), outline="")

    def 画线(self, x1, y1, x2, y2, 色, 粗) -> None:
        a = self._xy(x1, y1)
        b = self._xy(x2, y2)
        self._canvas.create_line(a[0], a[1], b[0], b[1],
                                 fill=颜色(色), width=max(1, int(self._r(粗))))

    def 画文字(self, x, y, 内容, 色, 字号) -> None:
        px, py = self._xy(x, y)
        self._canvas.create_text(px, py, text=str(内容), fill=颜色(色),
                                 font=("Microsoft YaHei UI", int(字号)))

    def 呈现(self) -> None:
        if self._关:
            return
        try:
            self._root.update()
        except self._tk.TclError:
            self._关 = True

    def 关闭(self) -> None:
        if self._关:
            return
        self._关 = True
        try:
            self._root.destroy()
        except Exception:
            pass

    @property
    def 还开着(self) -> bool:
        return not self._关

    # ── 输入（tkinter 事件 → 引擎要的"当前按键集合"）──
    def 装输入(self, 按键表: set[str], 按下回调=None, 抬起回调=None) -> None:
        def 规整(ev):
            # tkinter 给的是 keysym；统一成小写单字符或名字
            k = (ev.keysym or "").lower()
            return k
        def 按(ev):
            k = 规整(ev)
            按键表.add(k)
            if 按下回调:
                按下回调(k)
        def 放(ev):
            k = 规整(ev)
            按键表.discard(k)
            if 抬起回调:
                抬起回调(k)
        self._root.bind("<KeyPress>", 按)
        self._root.bind("<KeyRelease>", 放)
        self._root.focus_force()


#: 有没有可用的引擎后端（`hybridengine.dll`）。
#  ⚠ 留成函数而不是常量：DLL 可能中途被加上/移除。
def 可用引擎后端() -> bool:
    import os
    return bool(os.environ.get("HYBRIDENGINE_DLL")) and os.path.isfile(
        os.environ["HYBRIDENGINE_DLL"])
