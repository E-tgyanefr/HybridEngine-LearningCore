"""原生组件 —— 引擎自带的零件。**学员的作品就是用这些拼起来的。**

## 为什么"原生组件"是这套架构的重点

组件架构本身不难写；难的是**让人真的用它**。
如果挂一个"能画圆"的组件要自己写 30 行，学员还是会回去写 if-else。

所以这里备好最常用的那些：画东西的、动的、被按键驱动的、跟随的、计时的、计分的。
学员第一步是**装配**（拼现成的），第二步才是**自己写组件**（继承 `组件`）——
而第二步在那时已经很自然了，因为他已经看过十几个组件长什么样。

## 约定

每个组件都只干一件事，而且**能单独测**：
`组件.每帧(dt)` 与 `组件.绘制(画笔)` 都不碰窗口 ——
所以判据可以用 `录制后端` 抓它画了什么。
"""
from __future__ import annotations

import math

from .scene import 组件, 场景对象, 向量


# ══════════════════════ 视觉 ══════════════════════

class 圆点(组件):
    """在宿主位置画一个实心圆。**最常用的那一个。**"""

    def __init__(self, 半径: float = 3.0, 颜色: str = "红") -> None:
        self.半径 = 半径
        self.颜色 = 颜色
        #: 相对宿主的偏移（做"炮口""眼睛"这类挂件用）
        self.偏移x = 0.0
        self.偏移y = 0.0

    def 绘制(self, 画笔) -> None:
        p = self.宿主.世界位置
        画笔.圆(p.x + self.偏移x, p.y + self.偏移y, self.半径, self.颜色)


class 方块(组件):
    """在宿主位置画一个矩形（锚点在**左下角**，与 `Stage.box` 一致）。"""

    def __init__(self, 宽: float = 4.0, 高: float = 4.0, 颜色: str = "蓝",
                 居中: bool = False) -> None:
        self.宽, self.高, self.颜色, self.居中 = 宽, 高, 颜色, 居中

    def 绘制(self, 画笔) -> None:
        p = self.宿主.世界位置
        x = p.x - (self.宽 / 2 if self.居中 else 0)
        y = p.y - (self.高 / 2 if self.居中 else 0)
        画笔.矩形(x, y, self.宽, self.高, self.颜色)


class 标签(组件):
    """在宿主位置写一段字（分数、提示都靠它）。"""

    def __init__(self, 内容: str = "", 颜色: str = "黑", 字号: int = 14) -> None:
        self.内容, self.颜色, self.字号 = 内容, 颜色, 字号

    def 绘制(self, 画笔) -> None:
        p = self.宿主.世界位置
        画笔.文字(p.x, p.y, self.内容, self.颜色, self.字号)


class 线框(组件):
    """把宿主的子对象连起来画（调试用：看层次结构对不对）。"""

    def __init__(self, 颜色: str = "灰", 粗: float = 1.0) -> None:
        self.颜色, self.粗 = 颜色, 粗

    def 绘制(self, 画笔) -> None:
        p = self.宿主.世界位置
        for 子 in self.宿主.子对象:
            q = 子.世界位置
            画笔.线(p.x, p.y, q.x, q.y, self.颜色, self.粗)


# ══════════════════════ 运动 ══════════════════════

class 匀速移动(组件):
    """按固定速度走，撞到边界就反弹（做"弹球""巡逻怪"用）。"""

    def __init__(self, vx: float = 10.0, vy: float = 10.0,
                 边界: tuple[float, float] | None = None) -> None:
        self.vx, self.vy = vx, vy
        #: (最小, 最大) —— 到边界就反向；None = 不反弹
        self.边界 = 边界

    def 每帧(self, dt: float) -> None:
        t = self.宿主.变换
        t.位置 = 向量(t.位置.x + self.vx * dt, t.位置.y + self.vy * dt)
        if self.边界 is not None:
            小, 大 = self.边界
            # ⚠ 判据是 `<=` / `>=`，**不是** `<` / `>`。
            #   实测踩到：一步恰好走到 x == 大 时，`>` 不成立 ⇒ 不反弹；
            #   下一步才反 —— 表现为"贴著边界停一拍"，手感很怪。
            #   碰着就反更可预期（也是新手会预期的）。
            if t.位置.x <= 小:
                t.位置 = 向量(小, t.位置.y)
                self.vx = abs(self.vx)
            elif t.位置.x >= 大:
                t.位置 = 向量(大, t.位置.y)
                self.vx = -abs(self.vx)
            if t.位置.y <= 小:
                t.位置 = 向量(t.位置.x, 小)
                self.vy = abs(self.vy)
            elif t.位置.y >= 大:
                t.位置 = 向量(t.位置.x, 大)
                self.vy = -abs(self.vy)


class 按键驱动(组件):
    """按 WASD / 方向键移动宿主。**"我能控制它了"的那一瞬间。**"""

    def __init__(self, 速度: float = 30.0, 输入=None, 边界: tuple[float, float] | None = None) -> None:
        self.速度 = 速度
        self.输入 = 输入              # None = 用宿主所在场景的服务"输入"
        self.边界 = 边界

    def _输入(self):
        if self.输入 is not None:
            return self.输入
        if self.宿主 is not None and self.宿主._场景 is not None:
            return self.宿主._场景.取服务("输入")
        return None

    def 每帧(self, dt: float) -> None:
        输 = self._输入()
        if 输 is None:
            return
        方向 = 输.方向()
        if 方向.x == 0 and 方向.y == 0:
            return
        t = self.宿主.变换
        新 = t.位置 + 方向 * (self.速度 * dt)
        if self.边界 is not None:
            小, 大 = self.边界
            新 = 向量(min(max(新.x, 小), 大), min(max(新.y, 小), 大))
        t.位置 = 新


class 重力(组件):
    """给宿主一个向下的加速度（跳跃手感的基础）。"""

    def __init__(self, 加速度: float = -60.0, 地面: float = 0.0) -> None:
        self.加速度 = 加速度
        self.地面 = 地面
        self.vy = 0.0
        self.在地面 = True

    def 每帧(self, dt: float) -> None:
        t = self.宿主.变换
        self.vy += self.加速度 * dt
        y = t.位置.y + self.vy * dt
        if y <= self.地面:
            y = self.地面
            self.vy = 0.0
            self.在地面 = True
        else:
            self.在地面 = False
        t.位置 = 向量(t.位置.x, y)

    def 跳(self, 初速: float = 22.0) -> bool:
        """跳一下。⚠ 返回是否真的跳了（不在地上的话不跳）—— 避免"空中连跳"。"""
        if not self.在地面:
            return False
        self.vy = 初速
        self.在地面 = False
        return True


# ══════════════════════ 相机 ══════════════════════

class 跟随相机(组件):
    """让场景的相机跟着宿主走。**一个组件就搞定了"镜头跟着我"。**"""

    def __init__(self, 平滑: float = 0.0, 提前量: float = 0.0) -> None:
        self.平滑 = 平滑
        self.提前量 = 提前量

    def 挂上时(self) -> None:
        s = self.宿主._场景
        if s is not None:
            相 = s.取服务("相机")
            if 相 is not None:
                相.跟随 = self.宿主
                相.平滑 = self.平滑


class 世界边界(组件):
    """给相机设世界边界（不让画面跑出地图外）。"""

    def __init__(self, 左: float, 下: float, 右: float, 上: float) -> None:
        self.边界 = (左, 下, 右, 上)

    def 挂上时(self) -> None:
        s = self.宿主._场景
        if s is not None:
            相 = s.取服务("相机")
            if 相 is not None:
                相.边界 = self.边界


# ══════════════════════ 玩法零件 ══════════════════════

class 计时器(组件):
    """每 `间隔` 秒触发一次（生成敌人、掉血、闪烁都用它）。"""

    def __init__(self, 间隔: float = 1.0, 回调=None, 立即开始: bool = False) -> None:
        self.间隔 = 间隔
        self.回调 = 回调
        self.累计 = 0.0 if 立即开始 else 0.0
        self.次数 = 0

    def 每帧(self, dt: float) -> None:
        self.累计 += dt
        while self.累计 >= self.间隔:
            self.累计 -= self.间隔
            self.次数 += 1
            if self.回调 is not None:
                self.回调(self.宿主, self.次数)


class 分数(组件):
    """计分 + 自己画在屏幕左上角（不用学员自己摆位置）。"""

    def __init__(self, 初值: int = 0, 颜色: str = "黑") -> None:
        self.值 = 初值
        self.颜色 = 颜色
        #: 显示位置（世界坐标；通常配一个不动的 HUD 对象）
        self.显示x = 3.0
        self.显示y = 96.0

    def 加(self, n: int = 1) -> None:
        self.值 += n

    def 绘制(self, 画笔) -> None:
        画笔.文字(self.显示x, self.显示y, f"分数 {self.值}", self.颜色, 14)


class 生命周期(组件):
    """活够 `寿命` 秒就自动卸载宿主（子弹、粒子、飘字都用它）。

    ⚠ `卸载时` 里**不能**去改父对象的列表 —— 那是遍历中改容器，
      经典崩溃点。所以这里只把自己标记成"死了"，由场景在帧末统一清理。
    """

    def __init__(self, 寿命: float = 1.0, 到时=None) -> None:
        self.寿命 = 寿命
        self.到时 = 到时
        self.已活 = 0.0

    def 每帧(self, dt: float) -> None:
        self.已活 += dt
        if self.已活 >= self.寿命:
            self.宿主.活着 = False
            if self.到时 is not None:
                self.到时(self.宿主)


class 圆形碰撞(组件):
    """圆形碰撞体。**只做判定，不做物理** —— 物理有需要再加。"""

    def __init__(self, 半径: float = 3.0) -> None:
        self.半径 = 半径

    def 碰到(self, 别的: "圆形碰撞") -> bool:
        a = self.宿主.世界位置
        b = 别的.宿主.世界位置
        return math.hypot(a.x - b.x, a.y - b.y) <= (self.半径 + 别的.半径)


class 闪烁(组件):
    """按固定节奏一闪一闪（无敌时间、警示都用它）。"""

    def __init__(self, 周期: float = 0.2, 颜色a: str = "红", 颜色b: str = "白") -> None:
        self.周期, self.颜色a, self.颜色b = 周期, 颜色a, 颜色b
        self.累计 = 0.0
        self.当前 = 颜色a

    def 每帧(self, dt: float) -> None:
        self.累计 += dt
        if self.累计 >= self.周期:
            self.累计 -= self.周期
            self.当前 = self.颜色b if self.当前 == self.颜色a else self.颜色a
            # 顺手把兄弟"圆点"组件的颜色也换掉（最常见用法）
            for c in self.宿主.组件们:
                if isinstance(c, 圆点):
                    c.颜色 = self.当前


class 尾迹(组件):
    """移动时留下会消失的小点（拖尾效果）。"""

    def __init__(self, 间隔: float = 0.05, 寿命: float = 0.4,
                 颜色: str = "灰", 半径: float = 1.5) -> None:
        self.间隔, self.寿命, self.颜色, self.半径 = 间隔, 寿命, 颜色, 半径
        self.累计 = 0.0

    def 每帧(self, dt: float) -> None:
        self.累计 += dt
        if self.累计 < self.间隔:
            return
        self.累计 -= self.间隔
        s = self.宿主._场景
        if s is None:
            return
        p = self.宿主.世界位置
        o = 场景对象(f"{self.宿主.名字}尾迹", p.x, p.y)
        o.加(圆点(半径=self.半径, 颜色=self.颜色), 生命周期(寿命=self.寿命))
        # ⚠ 用 `延后加根` 而**不是** `加根` —— 这里正在 `每帧` 里，
        #   而 `每帧` 是在遍历 `场景.根` 的过程中被调的。
        #   遍历中改那个列表会跳过元素（或抛错）。详见 `场景._待加`。
        s.延后加根(o)
