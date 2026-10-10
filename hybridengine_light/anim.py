"""界面动效的原语（纯 tkinter Canvas，无第三方依赖）。

## 为什么单独一层

动效最容易写成一团 `after()` 回调互相打架 —— 那种东西**改一次就坏**。
这里把"怎么动"和"动什么"分开：

  · `缩放值(t, 起, 止, 缓动)` —— **纯函数**，能写判据（无头可测）
  · `动效` —— 一个 `after()` 循环，按时刻求值再画
  · `旋转弧 / 脉冲圆 / 计数条` —— 三个真正用到的控件

⇒ 缓动曲线是纯函数，所以"弹一下"的手感**能用判据固定住**
  （不然每次改都靠肉眼，改坏了看不出来）。

## ⚠ 一条原则：动效**不能挡住操作**

所有动画都必须能被"立刻取消"（`停()`），而且不阻塞输入。
旋转指示器在跑后台任务时转，任务一回来就停 —— 学员不用等动画演完。
"""
from __future__ import annotations

import math
import tkinter as tk

# ── 缓动（纯函数，可测）──────────────────────────────────────────────────

def 线性(t: float) -> float:
    return t


def 缓出(t: float) -> float:
    """先快后慢 —— 大部分"出现"类动效用这个最自然。"""
    return 1 - (1 - t) ** 3


def 缓入(t: float) -> float:
    return t ** 3


def 回弹(t: float) -> float:
    """末尾轻微过冲再收回 —— "弹一下"的手感。

    ⚠ 过冲量刻意很小（约 4%）：教学界面里夸张的回弹会显得廉价，
      而且会让"显示的是最终值"这件事变得不确定。
    """
    if t <= 0:
        return 0.0
    if t >= 1:
        return 1.0
    c = 1.70158 * 0.55
    return 1 + (c + 1) * (t - 1) ** 3 + c * (t - 1) ** 2


def 缩放值(进度: float, 起: float, 止: float, 缓动=缓出) -> float:
    """把 0..1 的进度映射成 起..止 的值。

    ⚠ 判据落在这里：它必须是**单调**的（除回弹），而且
      `进度<=0` 时严格等于 `起`、`进度>=1` 时严格等于 `止` ——
      否则动画结束时会停在"差一点"的位置上，看着像卡住了。
    """
    if 进度 <= 0:
        return 起
    if 进度 >= 1:
        return 止
    return 起 + (止 - 起) * 缓动(进度)


# ── 动画控件 ─────────────────────────────────────────────────────────────

面板 = "#2b2d30"
文字 = "#d6d6d6"
暗 = "#8a8f98"
强调 = "#4a9eff"
好 = "#4ec9b0"
坏 = "#f48771"


class 动效:
    """一个 `after()` 驱动的动画循环。

    ⚠ 必须能 `停()` —— 动画还在跑时窗口被销毁的话，
      `after` 回调会打到已销毁的控件上，Tkinter 抛 TclError。
      实测这类错很难查（栈里只有 Tk 的内部帧）。
    """

    def __init__(self, 控件: tk.Widget):
        self.控件 = 控件
        self._在跑 = False
        self._句柄 = None

    def 跑(self, 毫秒: int, 每帧) -> None:
        """每 `毫秒` 调一次 `每帧(经过毫秒)`，直到 `停()`。"""
        self.停()
        self._在跑 = True
        self._起 = None

        def 循环():
            if not self._在跑:
                return
            try:
                import time
                now = time.monotonic() * 1000
                if self._起 is None:
                    self._起 = now
                每帧(now - self._起)
                self._句柄 = self.控件.after(毫秒, 循环)
            except tk.TclError:
                # 控件没了（窗口关了）—— 静默停，不要往外抛
                self._在跑 = False
        循环()

    def 停(self) -> None:
        self._在跑 = False
        if self._句柄 is not None:
            try:
                self.控件.after_cancel(self._句柄)
            except Exception:
                pass
            self._句柄 = None


class 旋转弧:
    """转圈的弧 —— 替掉"正在运行…"那种死文字。

    ⚠ 为什么值得做：`dotnet run` 第一次编译要十几秒。
      纯文字的话学员会以为卡死了；转圈是**通用**的"还在干活"信号。
    """

    def __init__(self, 画布: tk.Canvas, x: int, y: int, r: int = 8, 颜色: str = 强调):
        self.画布, self.x, self.y, self.r, self.颜色 = 画布, x, y, r, 颜色
        self._弧 = 画布.create_arc(x - r, y - r, x + r, y + r,
                                 start=0, extent=90, style="arc",
                                 outline=颜色, width=2)
        self._扇 = 0
        self._动 = 动效(画布)

    def 开始(self) -> None:
        self.画布.itemconfigure(self._弧, state="normal")

        def 帧(t: float):
            self._扇 = (self._扇 + 12) % 360
            self.画布.itemconfigure(self._弧, start=self._扇)
            # 弧长也呼吸一下，比匀速转更像"在忙"
            长 = 60 + 40 * (0.5 + 0.5 * math.sin(t / 260))
            self.画布.itemconfigure(self._弧, extent=长)
        self._动.跑(50, 帧)

    def 停(self) -> None:
        self._动.停()
        try:
            self.画布.itemconfigure(self._弧, state="hidden")
        except tk.TclError:
            pass


class 结果徽标:
    """在画布上画一个"弹出来"的 ✓ / ✗。

    ⚠ 用**动画**而不是一行文字的原因：检查结果是学员最关心的反馈，
      一闪而过的文字容易被忽略；弹出来的徽标**抢到注意力**，
      而且"弹"这个动作本身暗示"有新消息"。
    """

    def __init__(self, 画布: tk.Canvas, x: int, y: int, 大小: int = 16):
        self.画布, self.x, self.y, self.大小 = 画布, x, y, 大小
        self._件: list[int] = []
        self._动 = 动效(画布)

    def 显示(self, 成功: bool, 文字: str = "") -> None:
        self.清()
        色 = 好 if 成功 else 坏

        def 帧(t: float):
            s = 缩放值(min(1.0, t / 320), 0.4, 1.0, 回弹)
            self.清()
            r = self.大小 * s
            x, y = self.x, self.y
            if 成功:
                # 对勾：两段折线
                self._件.append(self.画布.create_line(
                    x - r * 0.6, y + r * 0.05, x - r * 0.15, y + r * 0.5,
                    x + r * 0.65, y - r * 0.45, fill=色, width=max(2, int(3 * s)),
                    capstyle="round", joinstyle="round", smooth=False))
            else:
                # 叉：两条对角线
                w = max(2, int(3 * s))
                self._件.append(self.画布.create_line(
                    x - r * 0.5, y - r * 0.5, x + r * 0.5, y + r * 0.5,
                    fill=色, width=w, capstyle="round"))
                self._件.append(self.画布.create_line(
                    x + r * 0.5, y - r * 0.5, x - r * 0.5, y + r * 0.5,
                    fill=色, width=w, capstyle="round"))
            if 文字:
                self._件.append(self.画布.create_text(
                    x + r + 6, y, text=文字, anchor="w", fill=色,
                    font=("Segoe UI", 10, "bold")))
        self._动.跑(16, 帧)

    def 清(self) -> None:
        for i in self._件:
            try:
                self.画布.delete(i)
            except tk.TclError:
                pass
        self._件 = []

    def 停(self) -> None:
        self._动.停()


class 进度条:
    """不确定进度用的滑动条（不知道要多久时用它而不是百分比）。"""

    def __init__(self, 画布: tk.Canvas, 宽: int, 高: int = 3, 颜色: str = 强调):
        self.画布, self.宽, self.高, self.颜色 = 画布, 宽, 高, 颜色
        self._条 = 画布.create_rectangle(0, 0, 0, 高, fill=颜色, outline="")
        self._动 = 动效(画布)

    def 开始(self) -> None:
        def 帧(t: float):
            # 来回滑的块
            w = self.宽 * 0.3
            p = 0.5 - 0.5 * math.cos(t / 420)
            x = (self.宽 - w) * p
            self.画布.coords(self._条, x, 0, x + w, self.高)
        self._动.跑(33, 帧)

    def 停(self) -> None:
        self._动.停()
        try:
            self.画布.coords(self._条, 0, 0, 0, self.高)
        except tk.TclError:
            pass
