"""C++ 渲染后端（`libhblight_embed` + `WS_CHILD` 子窗口）—— 第一小步。

## 这一版**只做**四件事

    清屏 / 画圆 / 画矩形 / 呈现          ← 真的实现了
    画线 / 画文字 / 关闭 / 还开着        ← 画文字明确 `NotImplementedError`，其余最简

⚠ **对照台（Tk vs C++ 的 ms/帧）不在这一版里** —— 那是"带仪器的测量"，单独一轮。

## ⚠⚠ 三个"照抄"（不照抄就是两套规则，判据会红而你不知道为什么）

① **坐标换算：两个轴各用各的比例**。照 `render.py::Tk后端._xy`：
       kx = 宽 / size ;  ky = 高 / size ;  py = 高 - y * ky
   半径用 `宽/size`（`_r`）。⚠ `render.py` 头部就记着这个坑：
   y 原来用了宽度比例，**方形画布看不出来**，一旦 500×250 就错位（y=50 算成 0 而不是 125）。

② **裁剪**照 `render.py::Tk后端._在视野内`：
       允许裁剪 = True ； m = MARGIN * (宽 / size)
       -m <= sx <= 宽+m  且  -m <= sy <= 高+m
   ⚠ `MARGIN = 10.0` 是**虚拟单位**，必须乘 `宽/size` 才变成像素 ——
     不乘的话，非 1:1 的画布上"放宽多少"就变了。

③ **颜色**走 `pyengine.render.颜色(名)` → `"#rrggbb"` → `int(hex, 16)`。
   中文名 / 十六进制 / 未知名的约定全归它管，**不要自己写第二张表**。

## 默认关

`HBLIGHT_RENDER=1` 才启用（`默认后端()` 才返回本类）。理由两条：
  · **手机版 Chaquopy 靠 tkinter 回退**（那边没有 `.so`，也没有 Win32）
  · 本后端**只有 Windows**（`window_embed.cpp` 在别的平台是 `"null"`）

## ⚠⚠ 消息泵：**一个都不泵**（写死）

同线程的所有窗口**共用一个消息队列**。我们若 `PeekMessage(PM_REMOVE)` 而不派发，
就会把 **Tk 自己的消息一起捞走** —— `embed_probe.py --eat` 实测：
`BUTTON_CLICKS=0`，Tk 的按钮**再也不响应**。
⇒ 本后端**完全不碰消息队列**：画面是 `StretchDIBits` **直写子窗口 DC**，
  不依赖任何消息（`--no-pump` 实测 ①②③ 全过）。
⇒ **规矩：要么不泵，要么完整 `TranslateMessage` + `DispatchMessage`。这里选"不泵"。**
"""
from __future__ import annotations

import ctypes
import os
import sys

__all__ = ["Cpp后端", "可用", "默认后端", "后端名"]

# ── 找库 ──
# ⚠⚠ **不要在这里再写一遍 `.dll` / `.so` / `.dylib` 字面量。**
#   平台名的**唯一来源**是 `pyengine_c/__init__.py` 的 `_候选`（已经过核、登记过）。
#   这里只把 `hblight` 换成 `hblight_embed` 派生出嵌入库的名字。
#   ⇒ 好处有两条，都不是洁癖：
#     ① `_check_platform.py` 的「.dll 字面量」清单**不会再多一处**（能修就别登记）
#     ② 命名规则只有**一处** —— 以后改前缀/后缀不会两边跑偏
#   ⚠ 第一次我是直接抄了一份候选名，`_check_platform` 当场就红了（L50）——
#     这正是那条自检存在的意义。
from . import _候选 as _主候选  # noqa: E402

候选名 = tuple(名.replace("hblight", "hblight_embed") for 名 in _主候选)

_lib = None
_加载错误 = ""


def _这里():
    return os.path.dirname(os.path.abspath(__file__))


def _找库():
    for 名 in 候选名:
        p = os.path.join(_这里(), 名)
        if os.path.isfile(p):
            return p
    return None


def _载入():
    """惰性。⚠ 失败**不是异常** —— 库不在是**正常情况**（回退 tkinter）。"""
    global _lib, _加载错误
    if _lib is not None or _加载错误:
        return _lib
    p = _找库()
    if p is None:
        _加载错误 = "找不到 %s（跑 python _build/build_hblight.py --go）" % " / ".join(候选名)
        return None
    try:
        lib = ctypes.CDLL(p)
    except OSError as e:
        _加载错误 = "加载 %s 失败：%s" % (p, e)
        return None
    P, I = ctypes.c_void_p, ctypes.c_int
    lib.hb_fb_new.argtypes = [I, I]; lib.hb_fb_new.restype = P
    lib.hb_fb_free.argtypes = [P]
    lib.hb_fb_pixels.argtypes = [P]; lib.hb_fb_pixels.restype = P
    lib.hb_fb_clear.argtypes = [P, ctypes.c_uint32]
    lib.hb_fb_fill_circle.argtypes = [P, ctypes.c_double, ctypes.c_double, ctypes.c_double, ctypes.c_uint32]
    lib.hb_fb_fill_rect.argtypes = [P, ctypes.c_double, ctypes.c_double, ctypes.c_double,
                                    ctypes.c_double, ctypes.c_uint32]
    lib.hb_fb_line.argtypes = [P, ctypes.c_double, ctypes.c_double, ctypes.c_double,
                               ctypes.c_double, ctypes.c_uint32, ctypes.c_double]
    lib.hb_fb_written_pixels.restype = ctypes.c_longlong
    lib.hb_child_backend.restype = ctypes.c_char_p
    lib.hb_child_new.argtypes = [P, I, I, I, I]; lib.hb_child_new.restype = P
    lib.hb_child_free.argtypes = [P]
    lib.hb_child_present.argtypes = [P, P, I, I]; lib.hb_child_present.restype = I
    lib.hb_child_presented.argtypes = [P]; lib.hb_child_presented.restype = ctypes.c_longlong
    lib.hb_child_visible.argtypes = [P]; lib.hb_child_visible.restype = I
    _lib = lib
    return lib


def 可用() -> bool:
    """库能不能加载（**不看** `HBLIGHT_RENDER`）。"""
    return _载入() is not None


def 后端名() -> str:
    lib = _载入()
    if lib is None:
        return "(不可用) " + _加载错误
    v = lib.hb_child_backend()
    return v.decode("utf-8") if isinstance(v, bytes) else str(v)


def 启用了吗() -> bool:
    return os.environ.get("HBLIGHT_RENDER") == "1"


def 默认后端(*args, **kw):
    """`HBLIGHT_RENDER=1` **且**库可用 ⇒ `Cpp后端`；否则 `Tk后端`。

    ⚠ 为什么默认是 tkinter：手机版（Chaquopy）靠它，而本后端只有 Windows。
      判据里那条"**默认必须退回 tkinter**"就是钉这个的。
    """
    if 启用了吗() and 可用():
        try:
            return Cpp后端(*args, **kw)
        except Exception:
            pass                       # ⚠ 起不来就退回 —— 绝不因为后端坏了让引擎起不来
    from ..pyengine.render import Tk后端
    return Tk后端(*args, **kw)


def _颜色转整数(名: str) -> int:
    """照抄 `render.颜色` 的约定，然后转 0xRRGGBB。"""
    from ..pyengine.render import 颜色 as 颜色函数
    s = 颜色函数(名)
    if isinstance(s, str) and len(s) == 7 and s.startswith("#"):
        try:
            return int(s[1:], 16)
        except ValueError:
            pass
    # ⚠ 不静默吞掉：未知颜色名在 Tk 那边会由 explain.py 翻译成人话，
    #   这边也得**当场说清是哪个名字**（否则会变成"画面莫名少了一块"）。
    raise ValueError("颜色名不认识：%r（`pyengine.render.颜色` 只认中英文色名与 #rrggbb）" % (名,))


class Cpp后端:
    """`pyengine` 的后端协议（`清屏/画圆/画矩形/画线/画文字/呈现/关闭/还开着`）。

    ⚠ `允许裁剪 = True` —— 照 `Tk后端` 的语义，理由：本后端是**直写像素**，
      裁剪的代价与 Tk 不同（Tk 省的是建 item，这里省的是填像素），
      但**语义要和 Tk 一致**，否则同一份场景在两条后端上"画布上 item 数"对不上。
    """

    #: 照 `render.py::Tk后端` —— 视野外跳过不画
    允许裁剪 = True
    #: 裁剪外扩（**虚拟单位**，用时必须乘 `宽/size`）
    MARGIN = 10.0

    def __init__(self, 宽: int, 高: int, size: int, 标题: str = "HybridEngine",
                 背景: str = "白", 父窗口=None) -> None:
        lib = _载入()
        if lib is None:
            raise RuntimeError("C++ 后端不可用：" + _加载错误)
        self._lib = lib
        self.宽, self.高, self.size = int(宽), int(高), int(size)
        self.标题, self.背景 = 标题, 背景
        self._关 = False
        self._fb = lib.hb_fb_new(self.宽, self.高)
        if not self._fb:
            raise RuntimeError("framebuffer 分配失败（%dx%d）" % (self.宽, self.高))
        self._子 = None
        if 父窗口 is not None:
            # ⚠ 子窗口铺满整个客户区（0,0,宽,高）—— 本后端不做布局，位置由宿主定
            self._子 = lib.hb_child_new(ctypes.c_void_p(int(父窗口)), 0, 0, self.宽, self.高)
            if not self._子:
                raise RuntimeError("子窗口创建失败（父窗口句柄=%r）" % (父窗口,))
        self.画过的图元数 = 0

    # ── 坐标换算（⚠ 照抄 render.py::Tk后端._xy / ._r）──
    def _xy(self, x: float, y: float):
        kx = self.宽 / self.size
        ky = self.高 / self.size
        return (x * kx, self.高 - y * ky)

    def _r(self, r: float) -> float:
        return r * (self.宽 / self.size)

    def _在视野内(self, sx: float, sy: float) -> bool:
        if not self.允许裁剪:
            return True
        m = self.MARGIN * (self.宽 / self.size)      # ⚠ MARGIN 是虚拟单位，乘比例
        return -m <= sx <= self.宽 + m and -m <= sy <= self.高 + m

    # ── 后端协议 ──
    def 清屏(self, 色: str) -> None:
        self._lib.hb_fb_clear(self._fb, _颜色转整数(色))

    def 画圆(self, x, y, r, 色, tags=None) -> None:
        px, py = self._xy(x, y)
        if not self._在视野内(px, py):
            return
        self._lib.hb_fb_fill_circle(self._fb, px, py, self._r(r), _颜色转整数(色))
        self.画过的图元数 += 1

    def 画矩形(self, x, y, w, h, 色, tags=None) -> None:
        # ⚠ 照 Tk后端：左下角在屏幕上是"下方"，用 (x, y+h) 与 (x+w, y) 换算后取中心判裁剪
        x1, y1 = self._xy(x, y + h)
        x2, y2 = self._xy(x + w, y)
        if not self._在视野内((x1 + x2) / 2, (y1 + y2) / 2):
            return
        left, top = min(x1, x2), min(y1, y2)
        self._lib.hb_fb_fill_rect(self._fb, left, top, abs(x2 - x1), abs(y2 - y1),
                                  _颜色转整数(色))
        self.画过的图元数 += 1

    def 画线(self, x1, y1, x2, y2, 色, 粗, tags=None) -> None:
        a = self._xy(x1, y1)
        b = self._xy(x2, y2)
        if not (self._在视野内(a[0], a[1]) or self._在视野内(b[0], b[1])):
            return
        self._lib.hb_fb_line(self._fb, a[0], a[1], b[0], b[1], _颜色转整数(色), self._r(粗))
        self.画过的图元数 += 1

    def 画文字(self, x, y, 内容, 色, 字号, tags=None) -> None:
        # ⚠⚠ 这一版**没有字形光栅化**。`标签` / `分数` 组件会走到这里。
        #   ⇒ 明确报错，**不静默画个空白** —— 静默的话，症状是"文字不见了"，
        #     而原因在四层之外（本仓在颜色那件事上正是这么栽的）。
        raise NotImplementedError(
            "C++ 后端这一版没有文字（`标签` / `分数` 组件会走到它）。"
            "要文字请用 Tk后端（默认），或等字形光栅化那一版。")

    def 呈现(self) -> None:
        # ⚠ 这里**不泵消息**（见模块抬头）—— 只把位图写进子窗口的 DC。
        if self._子:
            self._lib.hb_child_present(self._子, self._fb, self.宽, self.高)

    def 关闭(self) -> None:
        if self._关:
            return
        self._关 = True
        if self._子:
            self._lib.hb_child_free(self._子)
            self._子 = None
        if self._fb:
            self._lib.hb_fb_free(self._fb)
            self._fb = None

    @property
    def 还开着(self) -> bool:
        return not self._关

    # ── 判据用（证"真的画了"，而不是"跑完了"）──
    def 写过的像素(self) -> int:
        return int(self._lib.hb_fb_written_pixels())

    def 呈现过的帧(self) -> int:
        return int(self._lib.hb_child_presented(self._子)) if self._子 else 0

    def 子窗口可见(self) -> bool:
        return bool(self._lib.hb_child_visible(self._子)) if self._子 else False
