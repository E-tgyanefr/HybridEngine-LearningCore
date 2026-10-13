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
    return 弹簧(t, 过冲=1.7, 次数=1)


def 弹簧(t: float, 过冲: float = 2.2, 次数: int = 2) -> float:
    """带**过冲**的缓出 —— 冲过目标再荡回来。

    ⚠⚠ 这是"非线性"手感的主力。普通的 `缓出` 只是"先快后慢"，
      而**过冲**才有"东西真的到了"的物理感 ——
      大脑对"速度突然归零"和"冲过去再回来"的反应完全不同，
      后者看起来像有质量。

    `过冲` 越大荡得越远（一般 1.2~3.0）；`次数` 是振荡频率（1~3）。
    ⚠ 别调太大：教学界面里夸张的弹跳会显得廉价，
      而且会让"显示的是最终值"变得不确定（学员会怀疑数字停稳没有）。

    ⚠ 边界是**严格**的：`t<=0` 返回 0、`t>=1` 返回 1。
      不严格的话，动画会在最后一帧停在 0.98 那种值上 —— 那看起来像**卡住了**。
    """
    if t <= 0:
        return 0.0
    if t >= 1:
        return 1.0
    w = 次数 * math.pi
    return 1 - math.exp(-6.0 * t) * math.cos(w * t) * (1 + (过冲 - 1) * (1 - t))


def 缓出过冲(t: float) -> float:
    """`缓出` + 一点点回弹 —— 给"到位"这种动作（框弹一下、色块滑到位）。"""
    return 弹簧(t, 过冲=1.5, 次数=1)


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


class 淡入:
    """把一批**标签**依次淡入 —— 给界面"活起来"的感觉。

    ⚠⚠ 用户原话：「这个软件看起来好塑料，加一些非线性动画」。

      "塑料感"的**技术原因**是：界面**一次性全部出现** ——
      没有任何东西在动，大脑读到的就是"一张静态图"。

      ⇒ 最便宜、最有效的解药是**入场**，而且要**错开**（stagger）——
        全部同时出现反而更像一个整体在闪。

    ⚠⚠ 三条约束（都是"别让动效变成负担"）：

      · **总时长 <= 450ms**：教学软件里等动画就是浪费生命。
      · **只动一次**：切课不重播 —— 每次都播会变成噪音，
        而噪音会让人不再注意它（那这个动效就白做了）。
      · **`after_cancel` 要清干净**：窗口关掉后那些待播的 `after`
        会打在已销毁的控件上 ⇒ `TclError` 刷屏。**实测踩过。**

    ⚠⚠ 实现上踩过一个坑（记下来）：
      第一版我写的是"到点了再 `tag_add`" —— 那行会**先按默认样式显示**，
      然后"啪"地变色。那不是淡入，那是**延迟变色**。
      第二版我想用"加一层遮罩标签" —— 太绕，而且 `tag_raise` 的
      优先级很难控对。

      ⇒ 这一版：**直接把标签的前景/背景插值成底色 → 真色**。
        `tag_configure` 改一次颜色就会触发重绘，所以这是**真渐变**，
        而且不涉及任何布局重排（Tk 的 `pack`/`place` 做不到平滑改位置，
        硬凑会在中途重排、看起来在抖）。
    """

    def __init__(self, 控件: tk.Widget):
        self.控件 = 控件
        self._句柄 = []
        self._原 = {}          #: 标签名 → (前景, 背景)，结束后要还原

    def 停(self) -> None:
        for h in self._句柄:
            try:
                self.控件.after_cancel(h)
            except Exception:
                pass
        self._句柄 = []

    def 跑(self, 文本控件: tk.Text, 组: list[str], 底色: str,
           每段毫秒: int = 26, 单段毫秒: int = 180, 总预算毫秒: int = 460) -> None:
        """`组` 里的标签依次从"看不见"渐变到"正常"。

        ⚠ 延迟会**按总预算自动收缩** —— 组多了之后不能一直线性累加，
          否则一篇长课要等好几秒。
        """
        self.停()
        if not 组:
            return
        延迟 = max(6, min(每段毫秒, 总预算毫秒 // max(1, len(组))))
        单段 = max(60, min(单段毫秒, 延迟 * 6))

        for i, 名 in enumerate(组):
            try:
                真前 = 文本控件.tag_cget(名, "foreground") or ""
                真后 = 文本控件.tag_cget(名, "background") or ""
                if 名 not in self._原:
                    self._原[名] = (真前, 真后)
                #: ⚠⚠ **只把"真有背景"的标签也插值背景** ——
                #:   本来没有背景的（正文、节标题）如果被填上底色，
                #:   动画结束后它们会**一直带着一块底色**，
                #:   看起来像"这几行被选中了"。实测踩到过。
                文本控件.tag_configure(名, foreground=底色)
                if 真后:
                    文本控件.tag_configure(名, background=底色)
                真前色 = self._原[名][0] or "#ffffff"
                真后色 = self._原[名][1]
                self._渐(文本控件, 名, 底色, 真前色, 真后色,
                        i * 延迟, 单段)
            except tk.TclError:
                pass

    def _渐(self, 文本控件: tk.Text, 名: str, 起色: str, 止色: str,
            后色: str, 延迟: int, 毫秒: int):
        """把 `名` 的前景从 `起色` 插值到 `止色`。"""
        动 = 动效(self.控件)
        self._动们 = getattr(self, "_动们", [])
        self._动们.append(动)

        def 开始():
            def 帧(t):
                p = 缓出(min(1.0, t / 毫秒))
                色 = _混(起色, 止色, p)
                try:
                    文本控件.tag_configure(名, foreground=色)
                    if 后色:
                        文本控件.tag_configure(
                            名, background=_混(起色, 后色, p))
                    if p >= 1.0:
                        #: 收尾时写回**原值** —— 插值出来的颜色可能有
                        #: 一两位误差，不还原的话主题就不是 theme 里那个数了
                        文本控件.tag_configure(名, foreground=止色)
                        if 后色:
                            文本控件.tag_configure(名, background=后色)
                        动.停()
                except tk.TclError:
                    动.停()
            动.跑(16, 帧)
        try:
            self._句柄.append(self.控件.after(延迟, 开始))
        except tk.TclError:
            pass


def _混(甲: str, 乙: str, p: float) -> str:
    """两个 `#rrggbb` 之间按 `p` 线性插值。

    ⚠ 插值要在 **sRGB 数值上**做（简单）而不是线性光空间（正确）——
      对 200ms 的淡入，肉眼分不出区别，而 sRGB 版本不用做 gamma 变换。
    """
    try:
        a = 甲.lstrip("#")
        b = 乙.lstrip("#")
        if len(a) != 6 or len(b) != 6:
            return 乙
        p = max(0.0, min(1.0, p))
        出 = []
        for i in (0, 2, 4):
            x = int(a[i:i + 2], 16)
            y = int(b[i:i + 2], 16)
            出.append(int(round(x + (y - x) * p)))
        return "#%02x%02x%02x" % tuple(出)
    except Exception:
        return 乙


class 数字滚动:
    """让一个数字**平滑地**从 A 变到 B —— 用在进度百分比上。

    ⚠ 为什么不直接把新数字写上去：进度从 `1/240` 变到 `2/240`，
      数字是"跳"的。而**滚动**能让人看到"它在涨"，
      这个"看到在涨"比数字本身更有激励作用。
    ⚠ 用缓出曲线（先快后慢）—— 线性滚动的结尾会显得很"硬"。
    """

    def __init__(self, 控件: tk.Widget, 设值):
        self.控件 = 控件
        self.设值 = 设值
        self._动 = 动效(控件)

    def 到(self, 起: float, 止: float, 毫秒: int = 500, 取整=True):
        self._动.停()

        def 帧(t):
            p = 缓出(min(1.0, t / 毫秒))
            v = 起 + (止 - 起) * p
            self.设值(int(round(v)) if 取整 else v)
        self._动.跑(16, 帧)


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


class 定值进度条:
    """**确定百分比**的进度条 —— 显示"学过多少 / 一共多少"。

    ⚠ 和上面那个 `进度条`（不确定、来回滑）是**两件事**，别混：
      · 不知道要多久 ⇒ 用 `进度条`（滑动，表示"在忙"）
      · 知道 12/55     ⇒ 用这个（填充，表示"走到哪了"）

    ⚠ 为什么值得单独做一个（而不是给 `进度条` 加个参数）：
      两者的**语义完全不同** —— 一个是"我在忙，等着"，
      一个是"你已经走了这么远"。混成一个类之后，
      调用方每次都要想"我现在是哪种"，而那正是出错的地方。

    ⚠ 增长要有动画：学员刚读完一课，回来看到它**长了一截** ——
      那一下"长"本身才是奖励。直接跳到位就没这个效果了。
    """

    def __init__(self, 画布: tk.Canvas, 宽: int, 高: int = 6,
                 槽色: str | None = None, 前景: str = 强调):
        #: ⚠ 槽色默认取 `暗`（这个模块里的名字）—— 而 `learn_gui` 里同一个
        #:   颜色叫 `暗文字`。两个模块各有一套常量名，**默认值只能引用自己
        #:   模块里的那个**，否则 import 期就 NameError。
        #: 我第一版写了 `底: str = 暗文字`，就是这个错。
        self.画布, self.宽, self.高 = 画布, 宽, 高
        槽色 = 槽色 or 暗
        self._槽 = 画布.create_rectangle(0, 0, 宽, 高, fill=槽色, outline="")
        self._条 = 画布.create_rectangle(0, 0, 0, 高, fill=前景, outline="")
        self._值 = 0.0
        self._动 = 动效(画布)

    def 设(self, 比: float, 动画: bool = True) -> None:
        """`比` 取 0.0~1.0。"""
        比 = max(0.0, min(1.0, float(比)))
        if not 动画:
            self._值 = 比
            self._画(比)
            return
        起 = self._值
        self._动.停()
        #: 500ms —— 和手机版那个一致。再长就开始"拖"了
        self._动.跑(16, lambda t: self._画(起 + (比 - 起) * min(1.0, t / 500.0)))
        self._值 = 比

    def _画(self, 比: float) -> None:
        try:
            self.画布.coords(self._条, 0, 0, self.宽 * 比, self.高)
        except tk.TclError:
            pass

    def 停(self) -> None:
        self._动.停()
