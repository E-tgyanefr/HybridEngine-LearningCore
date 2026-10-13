"""场景图与组件 —— 这个 Python 引擎的**架构核心**。

## 这一层解决什么问题

学员写的 Python 代码一长就乱：变量到处飞、逻辑缠在一起、想加个新东西要改十处。
**模块化编程**不是"把函数拆小"，而是「**把会变的东西装进可插拔的块里**」。

这个引擎照专业版的架构给两样东西：

    SceneObject（场景对象）  —— 一个"东西"：位置、名字、子对象
    Component（组件）        —— 一块**能力**：能画、能跟随、能计分、能被按键驱动

于是"给玩家加上跟随相机"不是改玩家的代码，而是**挂一个组件**：

    player.add(Camera2D(跟随=player))
    player.add(计分器())
    player.add(自定义的组件())

⇒ 学员要学的不是"引擎怎么实现"，而是**"怎么把一件事装进一个组件里"**。
  这是模块化思维的最小可操作形状。

## 设计约束（都是踩出来的）

· **变换要支持父子** —— 不然"把一堆东西一起移动"要么手算、要么写不通用
· **组件的生命周期要明确**（挂上 / 每帧 / 卸载）—— 否则"什么时候该初始化"
  会变成玄学，学员只能靠试
· **遍历顺序必须确定**（按挂载顺序，不按字典序）—— 否则同一份代码
  在不同机器上表现不同，而"确定性"是这个项目一贯的要求
"""
from __future__ import annotations

import math
from typing import Any, Callable, Iterable


# ══════════════════════ 数学 ══════════════════════

class 向量:
    """一个二维向量。**用不可变对象**（避免"改了一个地方别处也变了"这类难查的 bug）。"""

    __slots__ = ("x", "y")

    def __init__(self, x: float = 0.0, y: float = 0.0) -> None:
        self.x = float(x)
        self.y = float(y)

    def __add__(self, o: "向量") -> "向量":
        return 向量(self.x + o.x, self.y + o.y)

    def __sub__(self, o: "向量") -> "向量":
        return 向量(self.x - o.x, self.y - o.y)

    def __mul__(self, k: float) -> "向量":
        return 向量(self.x * k, self.y * k)

    __rmul__ = __mul__

    def 长度(self) -> float:
        return math.hypot(self.x, self.y)

    def 归一(self) -> "向量":
        n = self.长度()
        return 向量(0.0, 0.0) if n == 0 else 向量(self.x / n, self.y / n)

    def 旋转(self, 弧度: float) -> "向量":
        c, s = math.cos(弧度), math.sin(弧度)
        return 向量(self.x * c - self.y * s, self.x * s + self.y * c)

    def 元组(self) -> tuple[float, float]:
        return (self.x, self.y)

    def __repr__(self) -> str:
        return f"向量({self.x:g}, {self.y:g})"

    def __eq__(self, o: object) -> bool:
        return isinstance(o, 向量) and self.x == o.x and self.y == o.y


class 变换:
    """位置 / 缩放 / 旋转，**支持父子继承**。

    ⚠ 父子变换是"能不能做出层次结构"的分水岭：
      没有它，"让一个整体的所有部件一起动"就得手算每个部件 ——
      而那正是学员写出乱代码的起点。

    世界变换 = 父的世界变换 ∘ 自己的局部变换。
    """

    __slots__ = ("位置", "缩放", "旋转", "_父")

    def __init__(self, x: float = 0.0, y: float = 0.0,
                 缩放: float = 1.0, 旋转: float = 0.0) -> None:
        self.位置 = 向量(x, y)
        self.缩放 = float(缩放)
        self.旋转 = float(旋转)
        self._父: 变换 | None = None

    # ── 世界坐标（递归到根）──
    def 世界位置(self) -> 向量:
        p = self.位置
        t: 变换 | None = self._父
        while t is not None:
            # 父的旋转/缩放要作用到子的位置上
            p = t.位置 + p.旋转(t.旋转) * t.缩放
            t = t._父
        return p

    def 世界旋转(self) -> float:
        r = self.旋转
        t = self._父
        while t is not None:
            r += t.旋转
            t = t._父
        return r

    def 世界缩放(self) -> float:
        s = self.缩放
        t = self._父
        while t is not None:
            s *= t.缩放
            t = t._父
        return s

    def 局部转世界(self, 局部x: float, 局部y: float) -> 向量:
        """把一个局部点变成世界点（含父链）。"""
        v = 向量(局部x, 局部y) * self.缩放
        v = v.旋转(self.旋转) + self.位置
        t = self._父
        while t is not None:
            v = v * t.缩放
            v = v.旋转(t.旋转) + t.位置
            t = t._父
        return v


# ══════════════════════ 组件 ══════════════════════

class 组件:
    """一块能力。**继承它，然后挂到场景对象上。**

    生命周期（四个钩子，按顺序）：

        挂上时()     —— 挂到这个对象上时调一次
        每帧(dt)     —— 每个逻辑帧调一次
        绘制(画笔)   —— 每帧绘制时调（默认什么都不画）
        卸载时()     —— 被移除或整个场景销毁时调一次

    ⚠ 为什么把"逻辑"和"绘制"分成两个钩子：
      逻辑要按固定步长走（否则物理/计分随帧率变），
      绘制要在能画的时候画（无头测试时根本没有窗口）。
      合成一个钩子的话，"无头跑逻辑"就做不到了。
    """

    #: 挂载后由 `场景对象.add()` 填上
    宿主: "场景对象 | None" = None

    def 挂上时(self) -> None:
        pass

    def 每帧(self, dt: float) -> None:
        pass

    def 绘制(self, 画笔: "画笔接口") -> None:
        pass

    def 卸载时(self) -> None:
        pass

    def __repr__(self) -> str:
        return f"<{type(self).__name__}>"


class 画笔接口:
    """给组件用的绘制接口（虚拟坐标，原点左下）。

    ⚠ 组件拿到的是**这个接口**，不是后端本身 ——
      于是"换后端"（tkinter / 真引擎 / 无头录制）对组件完全透明。
    """

    def __init__(self, 后端, 相机=None) -> None:
        self._后端 = 后端
        self._相机 = 相机

    def _世界转屏幕(self, x: float, y: float) -> tuple[float, float]:
        if self._相机 is None:
            return (x, y)
        return self._相机.世界转屏幕(x, y)

    def 圆(self, x: float, y: float, r: float, 颜色: str = "黑") -> None:
        sx, sy = self._世界转屏幕(x, y)
        self._后端.画圆(sx, sy, r * (self._相机.缩放 if self._相机 else 1.0), 颜色)

    def 矩形(self, x: float, y: float, w: float, h: float, 颜色: str = "蓝") -> None:
        sx, sy = self._世界转屏幕(x, y)
        k = self._相机.缩放 if self._相机 else 1.0
        self._后端.画矩形(sx, sy, w * k, h * k, 颜色)

    def 线(self, x1, y1, x2, y2, 颜色: str = "黑", 粗: float = 1.0) -> None:
        a = self._世界转屏幕(x1, y1)
        b = self._世界转屏幕(x2, y2)
        self._后端.画线(a[0], a[1], b[0], b[1], 颜色, 粗)

    def 文字(self, x: float, y: float, 内容: str, 颜色: str = "黑", 字号: int = 16) -> None:
        sx, sy = self._世界转屏幕(x, y)
        self._后端.画文字(sx, sy, 内容, 颜色, 字号)


# ══════════════════════ 场景对象 ══════════════════════

class 场景对象:
    """场景里的一个"东西"：名字 + 变换 + 组件 + 子对象。"""

    def __init__(self, 名字: str = "对象", x: float = 0.0, y: float = 0.0) -> None:
        self.名字 = 名字
        self.变换 = 变换(x, y)
        self.组件们: list[组件] = []
        self.子对象: list[场景对象] = []
        self.父亲: 场景对象 | None = None
        self.活着 = True
        self._场景 = None

    # ── 树 ──
    def 加子(self, 子: "场景对象") -> "场景对象":
        if 子.父亲 is not None:
            子.父亲.子对象.remove(子)
        子.父亲 = self
        子.变换._父 = self.变换
        self.子对象.append(子)
        子._传播场景(self._场景)
        return 子

    def _传播场景(self, 场景) -> None:
        self._场景 = 场景
        for c in self.组件们:
            c.宿主 = self
        for 子 in self.子对象:
            子._传播场景(场景)

    def 找(self, 名字: str) -> "场景对象 | None":
        """按名字找后代（深度优先，**顺序确定**）。"""
        for 子 in self.子对象:
            if 子.名字 == 名字:
                return 子
            r = 子.找(名字)
            if r is not None:
                return r
        return None

    def 遍历(self) -> Iterable["场景对象"]:
        """自己 + 所有后代，**按挂载顺序**（不是字典序 —— 要确定性）。"""
        yield self
        for 子 in self.子对象:
            yield from 子.遍历()

    # ── 组件 ──
    def 加(self, *组件们: 组件) -> "场景对象":
        for c in 组件们:
            c.宿主 = self
            self.组件们.append(c)
            c.挂上时()
        return self

    def 取(self, 类型: type) -> 组件 | None:
        """取**第一个**该类型的组件（挂载顺序）。"""
        for c in self.组件们:
            if isinstance(c, 类型):
                return c
        return None

    def 全取(self, 类型: type) -> list[组件]:
        return [c for c in self.组件们 if isinstance(c, 类型)]

    def 移除(self, c: 组件) -> None:
        if c in self.组件们:
            self.组件们.remove(c)
            c.卸载时()
            c.宿主 = None

    # ── 每帧 ──
    def 更新(self, dt: float) -> None:
        for c in list(self.组件们):      # 复制：组件可能在遍历中被移除
            c.每帧(dt)
        for 子 in list(self.子对象):
            子.更新(dt)

    def 绘制(self, 画笔: "画笔接口") -> None:
        for c in self.组件们:
            c.绘制(画笔)
        for 子 in self.子对象:
            子.绘制(画笔)

    @property
    def 世界位置(self) -> 向量:
        return self.变换.世界位置()

    def __repr__(self) -> str:
        return f"<{self.名字} @{self.变换.位置} 组件×{len(self.组件们)} 子×{len(self.子对象)}>"


class 场景:
    """一个场景 = 若干根对象 + 全局服务。

    ⚠ 为什么要有"场景"这一层，而不是直接一个对象列表：
      学员需要"重开一局"「切换到另一个画面」这类操作 ——
      那是**整个世界的替换**，不是一个对象的事。
    """

    def __init__(self, 名字: str = "主场景") -> None:
        self.名字 = 名字
        self.根: list[场景对象] = []
        #: 全局服务（相机、输入、资源、计分…）—— 按名字取，**顺序确定**
        self.服务: dict[str, Any] = {}
        #: 帧末才加进去的对象。
        #  ⚠⚠ 为什么不能直接 `加根`：组件在 `每帧` 里造新对象
        #    （尾迹、子弹、粒子）时，正好处于 `for r in 根` 的遍历中 ——
        #    **遍历中改容器**是经典崩溃点（会跳过元素或抛错）。
        #    ⇒ 先放进待加队列，`更新` 结束时统一并入。
        self._待加: list[场景对象] = []

    def 加根(self, *对象们: 场景对象) -> 场景对象:
        for o in 对象们:
            o._传播场景(self)
            self.根.append(o)
        return 对象们[0] if 对象们 else None    # type: ignore[return-value]

    def 延后加根(self, *对象们: 场景对象) -> 场景对象:
        """**帧末**再加。组件在 `每帧` 里造东西时用它（见 `_待加` 的说明）。"""
        for o in 对象们:
            self._待加.append(o)
        return 对象们[0] if 对象们 else None    # type: ignore[return-value]

    def _并入延后的(self) -> None:
        if not self._待加:
            return
        批, self._待加 = self._待加, []
        for o in 批:
            o._传播场景(self)
            self.根.append(o)

    def 造(self, 名字: str = "对象", x: float = 0.0, y: float = 0.0) -> 场景对象:
        """建一个对象并加到根上（省得每次两行）。"""
        o = 场景对象(名字, x, y)
        self.加根(o)
        return o

    def 找(self, 名字: str) -> 场景对象 | None:
        for r in self.根:
            if r.名字 == 名字:
                return r
            h = r.找(名字)
            if h is not None:
                return h
        return None

    def 遍历(self) -> Iterable[场景对象]:
        for r in self.根:
            yield from r.遍历()

    def 取服务(self, 名字: str, 默认=None):
        return self.服务.get(名字, 默认)

    def 设服务(self, 名字: str, 值) -> None:
        self.服务[名字] = 值

    def 更新(self, dt: float) -> None:
        for r in list(self.根):
            r.更新(dt)
        # ⚠ 清理"死了"的对象（`生命周期` 组件只会标记，不自己动手 —— 见那里的说明）
        if any(not r.活着 for r in self.根):
            self.根 = [r for r in self.根 if r.活着]
        self._并入延后的()

    def 绘制(self, 画笔: "画笔接口") -> None:
        for r in self.根:
            r.绘制(画笔)

    def 销毁(self) -> None:
        for o in list(self.遍历()):
            for c in list(o.组件们):
                c.卸载时()
                c.宿主 = None
            o.组件们.clear()
        self.根.clear()

    def __repr__(self) -> str:
        总数 = sum(1 for _ in self.遍历())
        return f"<场景 {self.名字} 对象×{总数}>"
