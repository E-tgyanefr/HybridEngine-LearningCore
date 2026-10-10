"""learncode.stage —— 给新手的第一块画布。

设计原则（每一条都是为了减少"新手卡住"）：

1. **不需要装任何东西。** 只用标准库的 tkinter。pip install 之后第一行代码就能出画面。

2. **不用像素坐标。** 画布默认 100×100 的**虚拟坐标**，原点在左下角。
   新手不用去想"我的窗口多宽"，也不用被"y 轴向下"坑一次
   （屏幕上往下的 y 变大，和数学课相反 —— 这是初学者最常见的挫败点之一）。

3. **颜色可以直接写名字。** 调 color("红") 就行，不必查 #FF0000。

4. **画面与逻辑分离。** 所有绘图指令先记进一个列表，`show()` 才真正画出来。
   这样测试不需要显示器也能验证（见同目录的 tests/），
   也让"动画"变成一件自然的事：改状态 → show() → 睡一下 → 再改。
"""

from __future__ import annotations

import time
import tkinter as tk

__all__ = ["Stage", "colors", "color_of"]

# 新手最常用的几种颜色，中英文都能写。
# 为什么不直接要求写 #RRGGBB：初学者记不住十六进制，而且一个拼错的颜色名
# 会让整个程序崩掉 —— 那是最没有教育意义的失败。
colors = {
    "黑": "#000000", "白": "#ffffff", "红": "#e5484d", "绿": "#30a46c",
    "蓝": "#0091ff", "黄": "#ffd60a", "橙": "#ff8b00", "紫": "#8e4ec6",
    "灰": "#8b8d98", "粉": "#e93d82", "青": "#00b8d9", "棕": "#a18072",
    "black": "#000000", "white": "#ffffff", "red": "#e5484d", "green": "#30a46c",
    "blue": "#0091ff", "yellow": "#ffd60a", "orange": "#ff8b00", "purple": "#8e4ec6",
    "gray": "#8b8d98", "grey": "#8b8d98", "pink": "#e93d82", "cyan": "#00b8d9",
    "brown": "#a18072",
}


def color_of(name: str) -> str:
    """把颜色名（中/英）或 #RRGGBB 统一成 tkinter 认的字符串。

    未知名字**不报错**，而是原样返回 —— 交给 tkinter 提示。
    但我们会把 tkinter 的报错翻译成人话（见 explain.py）。
    """
    if not isinstance(name, str):
        return name
    key = name.strip().lower()
    return colors.get(name.strip(), colors.get(key, name))


class Stage:
    """一块画布。约定：坐标是 0..size 的虚拟坐标，原点在**左下角**。

    典型用法（新手第一个程序）::

        from learncode import Stage
        s = Stage()
        s.dot(50, 50, r=20, color="红")
        s.wait()
    """

    def __init__(self, size: int = 100, width: int = 640, height: int = 640,
                 title: str = "learncode"):
        self.size = size
        self.width = width
        self.height = height
        self.title = title
        self._shapes: list[tuple] = []      # 待绘制的指令（与显示无关 —— 便于测试）
        self._root: tk.Tk | None = None
        self._canvas: tk.Canvas | None = None
        self.background = "白"

    # ── 坐标换算：虚拟坐标 → 屏幕像素 ──
    #
    # ⚠ 这里踩过一次：y 的缩放原来写的是 `self.height - y * k`，其中 k 用的是**宽度**比例。
    #   方形画布看不出问题，一旦 width ≠ height 就错位（500×250 时 y=50 会算成 0 而不是 125）。
    #   两个轴必须各用各的比例。
    def _xy(self, x: float, y: float) -> tuple[float, float]:
        kx = self.width / self.size
        ky = self.height / self.size
        return (x * kx, self.height - y * ky)    # y 翻转：数学坐标 → 屏幕坐标
    def _r(self, r: float) -> float:
        return r * (self.width / self.size)

    # ── 绘图指令（只记录，不画）──
    def dot(self, x: float, y: float, r: float = 5, color: str = "黑"):
        """在 (x, y) 画一个半径 r 的实心圆。"""
        self._shapes.append(("dot", x, y, r, color))
        return self

    def box(self, x: float, y: float, w: float, h: float, color: str = "蓝"):
        """以 (x, y) 为**左下角**画一个 w×h 的实心矩形。"""
        self._shapes.append(("box", x, y, w, h, color))
        return self

    def line(self, x1: float, y1: float, x2: float, y2: float,
             color: str = "黑", thick: float = 1):
        """从 (x1,y1) 到 (x2,y2) 画一条线。"""
        self._shapes.append(("line", x1, y1, x2, y2, color, thick))
        return self

    def text(self, x: float, y: float, s: str, color: str = "黑", font_size: int = 16):
        """在 (x, y) 写一段字。"""
        self._shapes.append(("text", x, y, s, color, font_size))
        return self

    def clear(self):
        """擦掉画布上所有东西（下一次 show 生效）。"""
        self._shapes.clear()
        self._pending_clear = True
        return self

    # ── 游戏需要的输入 ──
    #
    # 为什么放在 Stage 上而不是让游戏自己开窗口：
    # 游戏用到的每一个能力（画、按键、等）都该来自同一处，
    # 否则学生每写一个游戏就要学一套新的窗口代码 —— 那正好背离"少点干扰"的初衷。
    def key(self) -> str:
        """等一个按键，返回它的名字。

        返回的是**方向名或字符**，不是系统键码 —— 学生不用查对照表。
        想退出就按 Esc，返回 "Escape"。
        """
        _NAMES = {
            "Up": "上", "Down": "下", "Left": "左", "Right": "右",
            "space": "空格", "Return": "回车", "Escape": "Escape",
            "w": "上", "s": "下", "a": "左", "d": "右",   # WASD 也当方向键
        }
        self._ensure_window()
        got: list[str] = []

        def on_press(e):
            got.append(_NAMES.get(e.keysym, e.keysym))

        self._root.bind("<Key>", on_press)
        self._root.focus_force()
        while not got:
            try:
                self._root.update()
            except tk.TclError:
                return "Escape"          # 窗口被关了就当作退出
            time.sleep(0.01)
        self._root.unbind("<Key>")
        return got[0]

    def hint(self, s: str):
        """在窗口标题上显示一句提示（不占画面）。

        为什么用标题栏而不是画在画布上：提示文字不该挤占游戏画面，
        而标题栏本来就空着 —— 这是"不干扰玩法"的顺手做法。
        """
        self._ensure_window()
        try:
            self._root.title(f"{self.title}  ——  {s}")
        except tk.TclError:
            pass
        return self

    # ── 真正显示 ──
    def _ensure_window(self):
        if self._root is not None:
            return
        self._root = tk.Tk()
        self._root.title(self.title)
        self._root.resizable(False, False)
        self._canvas = tk.Canvas(self._root, width=self.width, height=self.height,
                                 highlightthickness=0)
        self._canvas.pack()

    def show(self):
        """把这一轮记录下来的图形真正画到屏幕上。"""
        self._ensure_window()
        self._canvas.delete("all")
        self._canvas.configure(bg=color_of(self.background))
        for sh in self._shapes:
            self._draw_one(sh)
        self._root.update()          # 立即刷新，不然新手看不到"动画"
        return self

    def _draw_one(self, sh: tuple):
        c = self._canvas
        kind = sh[0]
        if kind == "dot":
            _, x, y, r, col = sh
            px, py = self._xy(x, y)
            pr = self._r(r)
            c.create_oval(px - pr, py - pr, px + pr, py + pr,
                          fill=color_of(col), outline="")
        elif kind == "box":
            _, x, y, w, h, col = sh
            x1, y1 = self._xy(x, y + h)          # 左下角在屏幕上是"下方"，取 y+h
            x2, y2 = self._xy(x + w, y)
            c.create_rectangle(x1, y1, x2, y2, fill=color_of(col), outline="")
        elif kind == "line":
            _, x1, y1, x2, y2, col, th = sh
            p1 = self._xy(x1, y1); p2 = self._xy(x2, y2)
            c.create_line(p1[0], p1[1], p2[0], p2[1],
                          fill=color_of(col), width=max(1, int(self._r(th))))
        elif kind == "text":
            _, x, y, s, col, fs = sh
            px, py = self._xy(x, y)
            c.create_text(px, py, text=s, fill=color_of(col),
                          font=("Microsoft YaHei UI", fs))

    def wait(self, seconds: float | None = None):
        """停住画面让人看。不传参数就一直等到你关掉窗口。"""
        self.show()
        if self._root is None:
            return self
        if seconds is None:
            self._root.mainloop()
        else:
            self._root.update()
            time.sleep(seconds)
        return self

    def close(self):
        """关掉窗口。程序结束时一般不用管；测试里要记得调。"""
        if self._root is not None:
            try:
                self._root.destroy()
            finally:
                self._root = None
                self._canvas = None
        return self

    # ── 给测试用：不依赖显示器的形状快照 ──
    def snapshot(self) -> list[tuple]:
        """返回当前记录的图形指令。测试用 —— 不需要显示器、不需要窗口。"""
        return list(self._shapes)
