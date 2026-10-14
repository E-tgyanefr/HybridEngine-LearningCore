"""pyengine_c —— `libhblight` 的 ctypes 绑定（C++ 内核 ⇄ Python 门面）。

## 定位

`hybridengine_light/pyengine/` 里那份 **Python 实现一行都没删**，它是**回退**。
本包在 **C++ 内核在** 的时候把其中一部分**数值内核**换掉：

     向量        纯值类型，无身份要求     ← 已接手（逐位对账过）
     变换        父子链世界变换           ← 已接手（逐位对账过）
     相机2D / 输入 / 游戏循环 / 15 个组件   ← **尚未接手**（下一层）

## ⚠ 为什么只换这两个

`tests/test_pyengine.py`（649 行，**一个字都不许改**）要求：

    self.assertIs(c.宿主, o)              # 对象身份
    class 探针(E.组件): ...                # 学员在 Python 里子类化组件
    o.取(C.圆点)                           # isinstance 语义
    a.子对象[0]                            # 真的是个 list

⇒ **对象模型必须留在 Python**，能搬的只有"算得多的那些数字"。

## ⚠ 为什么 `向量` 和 `变换` 必须**一起**换

判据里有 `assertEqual(子.世界位置, E.向量(15, 10))`。
`assertEqual` 走的是左边那个的 `__eq__`，而它内部 `isinstance(o, 向量)`
指的是**定义它的那个模块里的 `向量`**。
⇒ 只换一半 ⇒ 两个不同的类 ⇒ `isinstance` 为假 ⇒ **判据红**。
   所以 `安装()` 把 `场景/变换/向量` 在**所有**引用它们的模块里一起换掉。

## 开关

    HBLIGHT_ENABLE=1    ⇒ 启用 C++ 内核（**必须显式给**）
    什么都不设           ⇒ **默认走 Python 版** ← 这是默认值
    HBLIGHT_LAYER=1|2   ⇒ 只用来做成本实测：1=只向量+变换，2=再加相机2D

⚠⚠ **为什么默认是关的**（2026-10-14 实测，别改成开）：
   在"逐操作 ctypes"这个粒度下，开内核比 Python 版**慢 4.5 倍** ——
       ① Python 版          0.7335 ms/帧
       ② 第1层(向量+变换)    2.9047 ms/帧   (+296.0%)
       ③ 第2层(+相机2D)      3.3097 ms/帧   (+351.2%)
   复现口径：`native/hblight/tests/paths_and_cost.py`（1000 对象 · 300 帧 · 录制后端）。
   ⇒ **"C++ 内核"不是错，错的是接口粒度**：每个 `向量` 运算符、每个
     `变换.位置` 的 getter/setter 都是一次跨语言往返。
     要真快得**一次调用干完一整帧**（C++ 侧持有场景，Python 只发一条
     `hb_scene_step(scene, dt)`）—— 那是另一个 ABI 设计，**尚未实现**。

⚠ **没有** `HBLIGHT_USE=1` 这种"强制用内核、加载不了就报错"的模式 ——
  那是写本文档时的设想，**没有实现**，别照着它写脚本。
  想知道当前到底走了哪条路：`pyengine_c.状态()`，或看
  `pyengine.向量.__module__`（`...pyengine_c` = 内核，`...pyengine.scene` = Python）。
  ⚠ **必须看这条**：内核加载失败会**按设计静默退回** Python
  （手机版 Chaquopy 就靠它）⇒ **"没生效"和"生效了"的判据都是绿的**。
  本轮实测踩过一次假绿，就是靠这一条抓出来的。

⚠ 判据要能**分别**跑通两条路（Python 路 / C++ 路），否则分不清红是谁的。
"""
from __future__ import annotations

import ctypes
import os
import sys

__all__ = ["可用", "版本", "库路径", "向量", "变换", "安装", "已接手", "状态"]

# ══════════════════════════════════════════════════════════════════════════
# ① 找库
# ══════════════════════════════════════════════════════════════════════════

_候选 = (
    ("libhblight.dll", "hblight.dll") if sys.platform.startswith("win")
    else (("libhblight.dylib",) if sys.platform == "darwin" else ("libhblight.so",))
)


def _找库() -> str | None:
    显式 = os.environ.get("HBLIGHT_DLL")
    if 显式:
        return 显式 if os.path.isfile(显式) else None
    这里 = os.path.dirname(os.path.abspath(__file__))
    for 名 in _候选:
        p = os.path.join(这里, 名)
        if os.path.isfile(p):
            return p
    return None


_lib = None
_加载错误 = ""


def _载入():
    """惰性加载。⚠ 失败**不是异常** —— 内核不在是**正常情况**（回退 Python）。"""
    global _lib, _加载错误
    if _lib is not None or _加载错误:
        return _lib
    p = _找库()
    if p is None:
        _加载错误 = "找不到 %s（跑 python _build/build_hblight.py --go 构建）" % " / ".join(_候选)
        return None
    try:
        _lib = ctypes.CDLL(p)
    except OSError as e:                       # pragma: no cover - 平台相关
        _加载错误 = "加载 %s 失败：%s" % (p, e)
        return None
    _声明接口(_lib)
    return _lib


def 库路径() -> str | None:
    return _找库()


def 可用() -> bool:
    """内核**能不能用**（不看环境变量）。"""
    return _载入() is not None


def 版本() -> str:
    lib = _载入()
    if lib is None:
        return "(内核不可用) " + _加载错误
    v = lib.hb_version()
    return v.decode("utf-8") if isinstance(v, bytes) else str(v)


# ══════════════════════════════════════════════════════════════════════════
# ② C ABI 声明
# ══════════════════════════════════════════════════════════════════════════

class _Vec2(ctypes.Structure):
    _fields_ = [("x", ctypes.c_double), ("y", ctypes.c_double)]


def _声明接口(lib) -> None:
    D = ctypes.c_double
    P = ctypes.c_void_p

    lib.hb_version.restype = ctypes.c_char_p
    lib.hb_version.argtypes = []

    # 向量（按值进出）
    for 名 in ("hb_vec_add", "hb_vec_sub"):
        getattr(lib, 名).argtypes = [_Vec2, _Vec2]
        getattr(lib, 名).restype = _Vec2
    lib.hb_vec_mul.argtypes = [_Vec2, D]
    lib.hb_vec_mul.restype = _Vec2
    lib.hb_vec_len.argtypes = [_Vec2]
    lib.hb_vec_len.restype = D
    lib.hb_vec_norm.argtypes = [_Vec2]
    lib.hb_vec_norm.restype = _Vec2
    lib.hb_vec_rot.argtypes = [_Vec2, D]
    lib.hb_vec_rot.restype = _Vec2

    # 变换
    lib.hb_transform_new.argtypes = [D, D, D, D]
    lib.hb_transform_new.restype = P
    lib.hb_transform_free.argtypes = [P]
    lib.hb_transform_free.restype = None
    lib.hb_transform_set_parent.argtypes = [P, P]
    lib.hb_transform_set_parent.restype = None
    lib.hb_transform_set_pos.argtypes = [P, D, D]
    lib.hb_transform_set_pos.restype = None
    lib.hb_transform_pos.argtypes = [P]
    lib.hb_transform_pos.restype = _Vec2
    lib.hb_transform_set_scale.argtypes = [P, D]
    lib.hb_transform_set_scale.restype = None
    lib.hb_transform_scale.argtypes = [P]
    lib.hb_transform_scale.restype = D
    lib.hb_transform_set_rot.argtypes = [P, D]
    lib.hb_transform_set_rot.restype = None
    lib.hb_transform_rot.argtypes = [P]
    lib.hb_transform_rot.restype = D
    lib.hb_transform_world_pos.argtypes = [P]
    lib.hb_transform_world_pos.restype = _Vec2
    lib.hb_transform_world_rot.argtypes = [P]
    lib.hb_transform_world_rot.restype = D
    lib.hb_transform_world_scale.argtypes = [P]
    lib.hb_transform_world_scale.restype = D
    lib.hb_transform_local_to_world.argtypes = [P, D, D]
    lib.hb_transform_local_to_world.restype = _Vec2

    # 相机2D
    lib.hb_camera_new.argtypes = [D, D, D, D, D]
    lib.hb_camera_new.restype = P
    lib.hb_camera_free.argtypes = [P]
    lib.hb_camera_free.restype = None
    lib.hb_camera_set_pos.argtypes = [P, D, D]
    lib.hb_camera_set_pos.restype = None
    lib.hb_camera_pos.argtypes = [P]
    lib.hb_camera_pos.restype = _Vec2
    lib.hb_camera_set_scale.argtypes = [P, D]
    lib.hb_camera_set_scale.restype = None
    lib.hb_camera_scale.argtypes = [P]
    lib.hb_camera_scale.restype = D
    lib.hb_camera_set_viewport.argtypes = [P, D, D]
    lib.hb_camera_set_viewport.restype = None
    lib.hb_camera_viewport.argtypes = [P, ctypes.POINTER(D), ctypes.POINTER(D)]
    lib.hb_camera_viewport.restype = None
    lib.hb_camera_set_target.argtypes = [P, ctypes.c_int, D, D]
    lib.hb_camera_set_target.restype = None
    lib.hb_camera_set_smooth.argtypes = [P, D]
    lib.hb_camera_set_smooth.restype = None
    lib.hb_camera_smooth.argtypes = [P]
    lib.hb_camera_smooth.restype = D
    lib.hb_camera_set_bounds.argtypes = [P, ctypes.c_int, D, D, D, D]
    lib.hb_camera_set_bounds.restype = None
    lib.hb_camera_world_to_screen.argtypes = [P, D, D]
    lib.hb_camera_world_to_screen.restype = _Vec2
    lib.hb_camera_screen_to_world.argtypes = [P, D, D]
    lib.hb_camera_screen_to_world.restype = _Vec2
    lib.hb_camera_step.argtypes = [P, D]
    lib.hb_camera_step.restype = None


# ══════════════════════════════════════════════════════════════════════════
# ③ 向量 —— 门面与 Python 版**同名同形**（判据才认）
# ══════════════════════════════════════════════════════════════════════════

class 向量:
    """与 `scene.向量` 同名同形的 C++ 版（值语义，走 ctypes 调内核）。"""

    __slots__ = ("x", "y")

    def __init__(self, x: float = 0.0, y: float = 0.0) -> None:
        self.x = float(x)
        self.y = float(y)

    def _c(self) -> _Vec2:
        return _Vec2(self.x, self.y)

    def __add__(self, o):
        r = _lib.hb_vec_add(self._c(), _Vec2(o.x, o.y))
        return 向量(r.x, r.y)

    def __sub__(self, o):
        r = _lib.hb_vec_sub(self._c(), _Vec2(o.x, o.y))
        return 向量(r.x, r.y)

    def __mul__(self, k):
        r = _lib.hb_vec_mul(self._c(), float(k))
        return 向量(r.x, r.y)

    __rmul__ = __mul__

    def 长度(self) -> float:
        return _lib.hb_vec_len(self._c())

    def 归一(self):
        r = _lib.hb_vec_norm(self._c())
        return 向量(r.x, r.y)

    def 旋转(self, 弧度: float):
        r = _lib.hb_vec_rot(self._c(), float(弧度))
        return 向量(r.x, r.y)

    def 元组(self):
        return (self.x, self.y)

    def __repr__(self) -> str:
        return "向量(%g, %g)" % (self.x, self.y)

    def __eq__(self, o) -> bool:
        return isinstance(o, 向量) and self.x == o.x and self.y == o.y

    def __hash__(self):
        return hash((self.x, self.y))


# ══════════════════════════════════════════════════════════════════════════
# ④ 变换 —— 父子链由内核持有；**父的 Python 壳也被引用住**（防悬垂指针）
# ══════════════════════════════════════════════════════════════════════════

class 变换:
    """与 `scene.变换` 同名同形的 C++ 版。

    ⚠⚠ 两条生命周期上的讲究（不然就是**悬垂指针**，而且是随机的）：
      ① `__del__` 里 free 内核对象，用一个哨兵防止重复 free；
      ② **父的 Python 壳被自引用住**（`_父对象`）——
         内核里子是拿父的**裸指针**的，父若被 GC 掉，子再算世界位置就是野指针。
         Python 版没这个问题（对象之间互相引用），搬进 C++ 就必须自己保住。
    """

    __slots__ = ("_h", "_父对象", "_活了")

    def __init__(self, x: float = 0.0, y: float = 0.0,
                 缩放: float = 1.0, 旋转: float = 0.0) -> None:
        self._h = _lib.hb_transform_new(float(x), float(y), float(缩放), float(旋转))
        self._父对象 = None
        self._活了 = True

    # ── 位置 / 缩放 / 旋转 ──
    @property
    def 位置(self):
        r = _lib.hb_transform_pos(self._h)
        return 向量(r.x, r.y)

    @位置.setter
    def 位置(self, v):
        _lib.hb_transform_set_pos(self._h, float(v.x), float(v.y))

    @property
    def 缩放(self) -> float:
        return _lib.hb_transform_scale(self._h)

    @缩放.setter
    def 缩放(self, v) -> None:
        _lib.hb_transform_set_scale(self._h, float(v))

    @property
    def 旋转(self) -> float:
        return _lib.hb_transform_rot(self._h)

    @旋转.setter
    def 旋转(self, v) -> None:
        _lib.hb_transform_set_rot(self._h, float(v))

    # ── 父链 ──
    @property
    def _父(self):
        return self._父对象

    @_父.setter
    def _父(self, t):
        self._父对象 = t
        _lib.hb_transform_set_parent(self._h, t._h if t is not None else None)

    def 世界位置(self):
        r = _lib.hb_transform_world_pos(self._h)
        return 向量(r.x, r.y)

    def 世界旋转(self) -> float:
        return _lib.hb_transform_world_rot(self._h)

    def 世界缩放(self) -> float:
        return _lib.hb_transform_world_scale(self._h)

    def 局部转世界(self, 局部x: float, 局部y: float):
        r = _lib.hb_transform_local_to_world(self._h, float(局部x), float(局部y))
        return 向量(r.x, r.y)

    def __del__(self):
        # ⚠ 解释器关闭时 `_lib` 可能已被清掉 ⇒ 用 getattr 兜底，别在 __del__ 里抛。
        if getattr(self, "_活了", False):
            self._活了 = False
            lib = globals().get("_lib")
            if lib is not None:
                try:
                    lib.hb_transform_free(self._h)
                except Exception:
                    pass


# ══════════════════════════════════════════════════════════════════════════
# ⑤ 相机2D —— 与 `engine.相机2D` 同名同形（纯算术，全部搬进内核）
# ══════════════════════════════════════════════════════════════════════════

class 相机2D:
    """平滑跟随 / 边界夹取 / 世界↔屏幕 —— 算术在 C++，门面在 Python。

    ⚠ `跟随` 是**场景对象**（Python），所以每帧要先把它的 `世界位置`
      取出来喂给内核（`hb_camera_set_target`），而不是让内核去持 Python 对象。
      语义一样，但避免了每帧跨语言查属性。
    """

    __slots__ = ("_h", "_跟随", "_边界", "_活了")

    def __init__(self, x: float = 0.0, y: float = 50.0, 缩放: float = 1.0,
                 视口: tuple = (100.0, 100.0)) -> None:
        self._h = _lib.hb_camera_new(float(x), float(y), float(缩放),
                                     float(视口[0]), float(视口[1]))
        self._跟随 = None
        self._边界 = None
        self._活了 = True

    # ── 位置（⚠ x/y 分开设，设一个不能把另一个冲掉）──
    @property
    def x(self) -> float:
        return _lib.hb_camera_pos(self._h).x

    @x.setter
    def x(self, v) -> None:
        p = _lib.hb_camera_pos(self._h)
        _lib.hb_camera_set_pos(self._h, float(v), p.y)

    @property
    def y(self) -> float:
        return _lib.hb_camera_pos(self._h).y

    @y.setter
    def y(self, v) -> None:
        p = _lib.hb_camera_pos(self._h)
        _lib.hb_camera_set_pos(self._h, p.x, float(v))

    @property
    def 缩放(self) -> float:
        return _lib.hb_camera_scale(self._h)

    @缩放.setter
    def 缩放(self, v) -> None:
        _lib.hb_camera_set_scale(self._h, float(v))

    def _视口(self):
        w = ctypes.c_double(0.0)
        h = ctypes.c_double(0.0)
        _lib.hb_camera_viewport(self._h, ctypes.byref(w), ctypes.byref(h))
        return (w.value, h.value)

    @property
    def 视口宽(self) -> float:
        return self._视口()[0]

    @视口宽.setter
    def 视口宽(self, v) -> None:
        _lib.hb_camera_set_viewport(self._h, float(v), self._视口()[1])

    @property
    def 视口高(self) -> float:
        return self._视口()[1]

    @视口高.setter
    def 视口高(self, v) -> None:
        _lib.hb_camera_set_viewport(self._h, self._视口()[0], float(v))

    # ── 跟随 / 平滑 / 边界 ──
    @property
    def 跟随(self):
        return self._跟随

    @跟随.setter
    def 跟随(self, o) -> None:
        self._跟随 = o

    @property
    def 平滑(self) -> float:
        return _lib.hb_camera_smooth(self._h)

    @平滑.setter
    def 平滑(self, v) -> None:
        _lib.hb_camera_set_smooth(self._h, float(v))

    @property
    def 边界(self):
        return self._边界

    @边界.setter
    def 边界(self, v) -> None:
        self._边界 = v
        if v is None:
            _lib.hb_camera_set_bounds(self._h, 0, 0.0, 0.0, 0.0, 0.0)
        else:
            左, 下, 右, 上 = v
            _lib.hb_camera_set_bounds(self._h, 1, float(左), float(下), float(右), float(上))

    # ── 坐标换算 ──
    def 世界转屏幕(self, x: float, y: float):
        r = _lib.hb_camera_world_to_screen(self._h, float(x), float(y))
        return (r.x, r.y)

    def 屏幕转世界(self, sx: float, sy: float):
        r = _lib.hb_camera_screen_to_world(self._h, float(sx), float(sy))
        return (r.x, r.y)

    def 每帧(self, dt: float) -> None:
        if self._跟随 is None:
            return
        目标 = self._跟随.世界位置
        _lib.hb_camera_set_target(self._h, 1, float(目标.x), float(目标.y))
        _lib.hb_camera_step(self._h, float(dt))

    def __del__(self):
        if getattr(self, "_活了", False):
            self._活了 = False
            lib = globals().get("_lib")
            if lib is not None:
                try:
                    lib.hb_camera_free(self._h)
                except Exception:
                    pass


# ══════════════════════════════════════════════════════════════════════════
# ⑥ 安装（把内核接手的类换进 pyengine）
# ══════════════════════════════════════════════════════════════════════════

#: 第 1 层：`向量` + `变换`（判据要求这两个**必须一起换**，见本文件抬头）
第一层 = ("向量", "变换")
#: 第 2 层：`相机2D`（纯算术：平滑 / 边界夹取 / 正反变换）
第二层 = ("相机2D",)


def _层() -> int:
    """`HBLIGHT_LAYER` —— 用来把"第 1 层"和"第 2 层"当**两个可测配置**跑。

    ⚠ 加这个开关是为了满足 Lead 的硬要求：成本实测要给 ①Python ②第1层 ③第2层
      三组数。没有它就只能量两个点，"多搬这一层值不值"就答不上来。
    """
    try:
        return int(os.environ.get("HBLIGHT_LAYER", "99"))
    except ValueError:
        return 99


def 接手了() -> tuple:
    """**本次运行**实际会接手哪些（内核在不在 + `HBLIGHT_LAYER`）。"""
    return (第一层 + 第二层) if _层() >= 2 else 第一层


def 状态() -> dict:
    return {
        "库路径": 库路径(),
        "可用": 可用(),
        "版本": 版本() if 可用() else None,
        "加载错误": _加载错误,
        "层": _层(),
        "接手": list(接手了()),
    }


def 已接手() -> bool:
    return bool(getattr(sys.modules.get(__name__), "_已安装", False))


def 安装() -> bool:
    """把 `向量` / `变换` 换到内核实现（在**所有**引用它们的模块里一起换）。

    返回是否真的换了。**换不了就原样返回 False**，绝不让调用方炸。
    """
    if not 可用():
        return False
    # ⚠⚠ **默认关**：实测在"逐操作 ctypes"这个粒度下，开着它比 Python 版
    #    **慢 4.5 倍**（1000 对象 · 300 帧 · 录制后端）：
    #        ① Python 版          0.7335 ms/帧
    #        ② 第1层(向量+变换)    2.9047 ms/帧   (+296.0%)
    #        ③ 第2层(+相机2D)      3.3097 ms/帧   (+351.2%)
    #    根因：`向量` 每个运算符、`变换.位置` 的 getter/setter 都是**一次 ctypes 调用**，
    #    1000 对象 × 300 帧 = 几百万次跨语言往返。
    #    ⇒ 所以默认**不能**开：开了就是把产品变慢 4.5 倍。
    #      要真快，得换粒度（**一次调用干完一整帧**），那是另一个 ABI 设计（本轮不做）。
    #    ⇒ 口径与复现：`native/hblight/tests/paths_and_cost.py`
    if os.environ.get("HBLIGHT_ENABLE") != "1":
        return False

    包 = __name__.rsplit(".", 1)[0]           # hybridengine_light.pyengine_c → hybridengine_light
    try:
        import importlib
        模块们 = [
            importlib.import_module(包 + ".pyengine"),
            importlib.import_module(包 + ".pyengine.scene"),
            importlib.import_module(包 + ".pyengine.engine"),
            importlib.import_module(包 + ".pyengine.components"),
        ]
    except Exception:
        return False

    # ⚠ `向量` 与 `变换` 必须**一起**换、且**处处一致** ——
    #   只换一半会让 `assertEqual(子.世界位置, E.向量(15, 10))` 因为
    #   两个不同的类而 `isinstance` 为假（见本文件抬头）。
    for m in 模块们:
        if hasattr(m, "向量"):
            m.向量 = 向量
        if hasattr(m, "变换"):
            m.变换 = 变换
        if _层() >= 2 and hasattr(m, "相机2D"):
            # `相机2D` 只定义在 engine.py（`scene.py`/`components.py` 没这个名字）
            m.相机2D = 相机2D

    sys.modules[__name__]._已安装 = True
    sys.modules[__name__]._本次接手 = 接手了()
    return True
