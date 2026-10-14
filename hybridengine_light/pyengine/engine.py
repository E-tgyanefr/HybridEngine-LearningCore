"""游戏循环、输入、相机 —— 引擎的"驱动"部分。

## 三条设计约束

### ① 逻辑用**固定步长**，绘制不限

    while 还在跑:
        累积 += 真实经过的秒数
        while 累积 >= 步长:
            场景.更新(步长)        # ← 逻辑永远用同一个 dt
            累积 -= 步长
        场景.绘制(...)              # ← 画多少帧取决于机器快慢

⚠ 为什么：如果逻辑直接用"这帧花了多久"，那么同一份代码在快机器与慢机器上
  **结果不同**（移动距离 = 速度 × dt，dt 抖动就漂移）。
  学员写出的"我的游戏在同学电脑上不一样快"就是这么来的。

### ② 输入要能"查当前状态"，不只是"回调"

    按键("a")      —— 现在按着吗（持续移动用这个）
    刚按下("a")    —— 这一帧刚按下吗（跳跃/开火用这个，避免连发）

⚠ 只有回调的话，"按住方向键持续走"要自己维护一堆状态；
  只有状态的话，"按一下跳一次"会在按住时连跳。

### ③ 相机是**服务**，不是偷偷改坐标

相机放在场景服务里，`画笔接口` 渲染时问它要变换。
⚠ 我踩过这个坑：上一版把相机偏移写成"在别处赋值 + 注释说应用了"，
  实际**根本没应用**（补丁只改了赋值）。现在它**只有一个入口**。
"""
from __future__ import annotations

import time

from . import render as R
from .scene import 场景, 场景对象, 画笔接口, 向量

#: ⚠ 模块级别的别名：`游戏.__init__` 的参数叫 `场景`，会遮住同名的类。
#   这里留一个不会被遮住的名字，让"不传场景就自己造一个"能工作。
_场景类 = 场景


class 相机2D:
    """跟谁看、看多远。世界坐标 → 屏幕坐标。"""

    def __init__(self, x: float = 0.0, y: float = 50.0, 缩放: float = 1.0,
                 视口: tuple[float, float] = (100.0, 100.0)) -> None:
        self.x, self.y = float(x), float(y)
        self.缩放 = float(缩放)
        self.视口宽, self.视口高 = 视口
        #: 要跟随的对象（None = 不跟随）
        self.跟随: 场景对象 | None = None
        #: 平滑（0 = 立刻跟上；越大越"懒"）
        self.平滑 = 0.0
        #: 世界边界 (左, 下, 右, 上) —— 给了就不让画面跑出去
        self.边界: tuple[float, float, float, float] | None = None

    def 世界转屏幕(self, x: float, y: float) -> tuple[float, float]:
        # 相机把它的中心点移到屏幕中心
        sx = (x - self.x) * self.缩放 + self.视口宽 / 2
        sy = (y - self.y) * self.缩放 + self.视口高 / 2
        return (sx, sy)

    def 屏幕转世界(self, sx: float, sy: float) -> tuple[float, float]:
        x = (sx - self.视口宽 / 2) / self.缩放 + self.x
        y = (sy - self.视口高 / 2) / self.缩放 + self.y
        return (x, y)

    def 每帧(self, dt: float) -> None:
        if self.跟随 is None:
            return
        目标 = self.跟随.世界位置
        if self.平滑 <= 0:
            self.x, self.y = 目标.x, 目标.y
        else:
            # ⚠ 与帧率无关的追赶：用 1-exp(-dt/tau) 而不是 `* 0.9`。
            #   `* 0.9` 这类写法在 120fps 下比 60fps 快一倍 ——
            #   学员会看到"我的相机在别人电脑上不一样"。
            系数 = 1.0 - pow(2.718281828, -dt / max(1e-6, self.平滑))
            self.x += (目标.x - self.x) * 系数
            self.y += (目标.y - self.y) * 系数
        if self.边界 is not None:
            左, 下, 右, 上 = self.边界
            半宽 = self.视口宽 / (2 * self.缩放)
            半高 = self.视口高 / (2 * self.缩放)
            # 视口比世界还大时就居中（夹取会算反）
            self.x = (左 + 右) / 2 if (右 - 左) <= 2 * 半宽 else min(max(self.x, 左 + 半宽), 右 - 半宽)
            self.y = (下 + 上) / 2 if (上 - 下) <= 2 * 半高 else min(max(self.y, 下 + 半高), 上 - 半高)


class 输入:
    """按键/鼠标状态。**由窗口后端填充，引擎只读。**"""

    def __init__(self) -> None:
        self._按下: set[str] = set()
        self._这帧按下: set[str] = set()
        self._这帧抬起: set[str] = set()
        self.鼠标x = 0.0
        self.鼠标y = 0.0

    # ── 后端调用 ──
    def _按(self, 键: str) -> None:
        # ⚠ 存入时也必须转小写：查询那边转了小写，存入这边不转
        #   ⇒ `按键("A")` 存成 "A"，而 `按键("a")` 查 "a" ⇒ **查不到**。
        #   实测就是这个 bug（判据 `大小写不敏感` 抓到的）。
        键 = 键.lower()
        if 键 not in self._按下:
            self._这帧按下.add(键)
        self._按下.add(键)

    def _放(self, 键: str) -> None:
        键 = 键.lower()
        self._按下.discard(键)
        self._这帧抬起.add(键)

    def 帧末清理(self) -> None:
        """每帧末尾清掉"刚按下/刚抬起"——否则会一直为真。"""
        self._这帧按下.clear()
        self._这帧抬起.clear()

    # ── 给学员用 ──
    def 按键(self, 键: str) -> bool:
        """**现在按着吗**（持续移动用）。"""
        return 键.lower() in self._按下

    def 刚按下(self, 键: str) -> bool:
        """**这一帧刚按下吗**（跳跃/开火用，按住不会连发）。"""
        return 键.lower() in self._这帧按下

    def 刚抬起(self, 键: str) -> bool:
        return 键.lower() in self._这帧抬起

    def 方向(self) -> 向量:
        """把 WASD / 方向键合成一个方向向量（长度已归一，斜着走不会更快）。"""
        x = (1 if (self.按键("d") or self.按键("right")) else 0) - \
            (1 if (self.按键("a") or self.按键("left")) else 0)
        y = (1 if (self.按键("w") or self.按键("up")) else 0) - \
            (1 if (self.按键("s") or self.按键("down")) else 0)
        v = 向量(x, y)
        n = v.长度()
        return 向量(0, 0) if n == 0 else 向量(v.x / n, v.y / n)


class 游戏:
    """一个游戏 = 一个场景 + 一个循环。

    典型用法::

        游戏 = 引擎.游戏(标题="我的游戏")
        s = 游戏.场景
        玩家 = s.造("玩家", 50, 50)
        玩家.加(原生.圆点(颜色="红", 半径=3))
        s.设服务("相机", 相机2D(跟随=玩家))
        游戏.跑()                      # 一直跑到关窗口
    """

    #: 逻辑步长（秒）—— 1/60
    步长 = 1.0 / 60.0

    def __init__(self, 场景: 场景 | None = None, 宽: int = 640, 高: int = 640,
                 尺寸: int = 100, 标题: str = "HybridEngine（Python 版）",
                 后端=None, 背景: str = "白") -> None:
        # ⚠⚠ 参数名 `场景` 把**同名的类**遮住了。
        #
        #   我原来写的是：
        #       self.场景 = 场景 if 场景 is not None else 场景()
        #   在函数体里，`场景` 指的是**参数**（None），不是那个类
        #   ⇒ `None()` ⇒ `TypeError: 'NoneType' object is not callable`。
        #
        #   而且**只在"不传场景"时炸** —— 自带贪吃蛇总是传 `场景=...`，
        #   所以一直没暴露；第一次有人 `E.游戏(后端=后)` 就撞上了。
        #   （判据也没覆盖这条路径 —— 现在有了。）
        #
        #   ⇒ 用模块级别的别名，别让参数名遮住类名。
        self.场景 = 场景 if 场景 is not None else _场景类()
        self.宽, self.高, self.尺寸, self.标题, self.背景 = 宽, 高, 尺寸, 标题, 背景
        self.输入 = 输入()
        self.相机 = self.场景.取服务("相机")
        if self.相机 is None:
            # ⚠ 默认相机放在**视口正中** —— 这样"世界坐标 = 屏幕坐标"，
            #   学员写 `圆点(x=30, y=40)` 就真的画在 (30,40)。
            #   我第一版放在 (0, size/2)，于是默认带了一个偏移：
            #   世界 (30,40) 画到了屏幕 (80,40) —— **离新手预期 50 像素**，
            #   而他会以为是自己算错了。
            #   （相机本身仍然要有，否则 `跟随相机` 组件没有服务可挂。）
            self.相机 = 相机2D(x=尺寸 / 2, y=尺寸 / 2, 视口=(尺寸, 尺寸))
            self.场景.设服务("相机", self.相机)
        self._后端 = 后端
        self._跑 = False
        self.帧数 = 0
        #: 每帧回调（给"不想写组件"的简单脚本用）
        self.每帧回调: list = []

    # ── 后端 ──
    @property
    def 后端(self):
        if self._后端 is None:
            # ⚠⚠ **只改了这一处**：从"写死 `Tk后端`"改成"问一句默认后端是谁"。
            #   `HBLIGHT_RENDER=1` 且 C++ 后端的库在 ⇒ `Cpp后端`；否则**还是 `Tk后端`**。
            #
            # ⚠ import 放在**函数体内**（延迟导入）—— 这是循环导入的**唯一**破法：
            #   `pyengine.engine` → `pyengine_c.backend`，而 `pyengine_c` 那边也有
            #   指向 `pyengine` 的引用；写成模块级 import 会成环。
            #   ⚠ 这条是**想清楚**的，不是试出来的。
            #
            # ⚠ 拿不到就退回 `Tk后端` —— `pyengine_c` **绝不许**变成硬依赖
            #   （手机版 Chaquopy 上根本没有它，而且它只有 Windows）。
            try:
                from ..pyengine_c.backend import 默认后端 as _默认后端
                self._后端 = _默认后端(self.宽, self.高, self.尺寸, self.标题, self.背景)
            except Exception:
                self._后端 = R.Tk后端(self.宽, self.高, self.尺寸, self.标题, self.背景)

            # ⚠⚠ **下面这一段一个字都不许删。**
            #   它是**键盘输入唯一的接线点**：`Tk后端.装输入` 把 tkinter 的
            #   `KeyPress`/`KeyRelease` 接到 `输入` 上。
            #   丢了它的症状是 **GUI 里键盘静默失灵**，而
            #   `_engine_fingerprint` **看不见**（无头判据不按键）⇒ 判据会全绿。
            #   ⇒ 所以 `Cpp后端` 也提供一个**明确 no-op** 的 `装输入`
            #     （带 `_输入没接` 状态位）：让"没接"是**可断言的**，不是"看不出来"。
            if hasattr(self._后端, "装输入"):
                self._后端.装输入(self.输入._按下, self.输入._按, self.输入._放)
        return self._后端

    # ── 一帧 ──
    def 一步(self, dt: float | None = None) -> None:
        """跑**一个逻辑帧**并绘制。测试里直接调它，不用开循环。"""
        dt = self.步长 if dt is None else float(dt)
        self.场景.更新(dt)
        self.相机.每帧(dt)
        self.绘制()
        self.输入.帧末清理()
        self.帧数 += 1

    def 绘制(self) -> None:
        后 = self.后端
        后.清屏(self.背景)
        笔 = 画笔接口(后, self.相机)
        self.场景.绘制(笔)
        后.呈现()

    # ── 循环 ──
    def 跑(self, 帧数: int = -1, 最长秒数: float | None = None) -> int:
        """跑到窗口关掉（或跑够 `帧数` 帧 / `最长秒数` 秒）。

        ⚠ 逻辑用固定步长，绘制跟着机器 —— 见文件头说明。
        """
        self._跑 = True
        上次 = time.perf_counter()
        累积 = 0.0
        开始 = 上次
        跑过 = 0
        while self._跑:
            now = time.perf_counter()
            经过 = now - 上次
            上次 = now
            # 卡顿保护：一次最多补 8 帧，否则"卡一下"会引发雪崩式补帧
            累积 = min(累积 + 经过, self.步长 * 8)
            while 累积 >= self.步长:
                self.场景.更新(self.步长)
                self.相机.每帧(self.步长)
                for cb in list(self.每帧回调):
                    cb(self.步长)
                self.输入.帧末清理()
                self.帧数 += 1
                跑过 += 1
                累积 -= self.步长
            self.绘制()
            if not self.后端.还开着:
                break
            if 帧数 >= 0 and self.帧数 >= 帧数:
                break
            if 最长秒数 is not None and (now - 开始) >= 最长秒数:
                break
        return self.帧数

    def 停(self) -> None:
        self._跑 = False

    def 关(self) -> None:
        self._跑 = False
        try:
            self.后端.关闭()
        except Exception:
            pass
