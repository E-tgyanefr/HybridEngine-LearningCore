"""hybridengine_light —— 用最少的代码，让你第一次运行就看到东西。

目标很小：**从 pip install 到屏幕上有画面，不超过 3 行代码。**

    from hybridengine_light import Stage
    Stage().dot(50, 50, r=20, color="红").wait()

为什么这么设计：

· **零依赖** —— 只用 Python 自带的 tkinter。学编程最不该卡在装环境上。
· **不用像素坐标** —— 画布是 100×100，原点在左下角（和数学课一样）。
  你不需要知道窗口多宽，也不会被"屏幕上 y 轴朝下"坑一次。
· **颜色可以直接写中文** —— dot(50, 50, color="红")。
· **报错说人话** —— 看不懂的报错会被翻译成"哪里错了 + 怎么改 + 该记住什么"。
"""

# ⚠⚠ `.stage`（以及 `learn_gui`/`anim`/`run`）**都 import tkinter**，
#   而 tkinter **不是到处都有**：
#     · Linux 上要单独装 `python3-tk`，精简系统/容器里常常没有
#     · **Android 上根本没有** —— CPython 官方就不支持
#       （见 PEP 738：`tkinter` 需要自行构建 Tk，"not officially supported"）
#
#   原来这里是**顶层直接 import**，于是只要碰这个包就必然加载 tkinter：
#
#       import hybridengine_light.ctl        ← 安卓上崩在这一行
#
#   因为 Python 的包导入语义是"**先执行 `__init__.py`**"，
#   哪怕你只想要命令行那部分，也会先把 GUI 拉进来。
#
#   ⇒ 改成**惰性导入**：用到 `Stage` 才去加载 tkinter。
#     好处不只是安卓 —— **没装 python3-tk 的 Linux 上，命令行也能用了**
#     （原来会 ImportError 直接退出，而错误信息还指向 tkinter，与用户想做的事无关）。
#
#   ⚠ 实现上不能用模块级 `__getattr__` 之外的花样：`from hybridengine_light import Stage`
#     会走 `__getattr__`，而 `from hybridengine_light.stage import Stage` 不会 ——
#     两种写法都得能用，所以 `__getattr__` 是正解（PEP 562，3.7+）。
from .explain import explain, friendly, run_safely      # 不依赖 tkinter
from .coach import Coach, 诊断                          # 不依赖 tkinter

# ⚠ 版本号要与 GitHub Release 的 tag 对应 —— 实测踩到：
#   我发 Release 时自己编了 v1.0.0~v1.0.3，而这里写的是 0.1.0，
#   **两者不一致**（而且 1.0.0 暗示"已稳定发布"，与项目实际状态不符）。
#   版本号是**唯一来源**：改这里 + 打同名 tag，不要各写各的。
# 独立于引擎版本：引擎（专业版）是 0.5.0（见 HybridEngine/CMakeLists.txt），
#   轻量版是**自己一条线**，不跟着走。
__version__ = "0.8.0"

#: 惰性成员 → 它所在的模块。用到才 import。
_惰性 = {
    "Stage": ".stage",
    "color_of": ".stage",
}

__all__ = ["Stage", "color_of", "explain", "friendly", "run_safely",
           "Coach", "诊断", "__version__"]


def __getattr__(名字: str):
    """PEP 562 的模块级 `__getattr__` —— 只有**真的取这个属性**时才加载 tkinter。"""
    模块 = _惰性.get(名字)
    if 模块 is None:
        raise AttributeError("module %r has no attribute %r" % (__name__, 名字))
    import importlib
    m = importlib.import_module(模块, __name__)
    值 = getattr(m, 名字)
    globals()[名字] = 值          # 缓存：下次不再走 __getattr__
    return 值


def __dir__():
    return sorted(set(globals()) | set(_惰性))
