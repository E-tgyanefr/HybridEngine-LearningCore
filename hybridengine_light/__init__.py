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

from .stage import Stage, color_of
from .explain import explain, friendly, run_safely
from .coach import Coach, 诊断

# ⚠ 版本号要与 GitHub Release 的 tag 对应 —— 实测踩到：
#   我发 Release 时自己编了 v1.0.0~v1.0.3，而这里写的是 0.1.0，
#   **两者不一致**（而且 1.0.0 暗示"已稳定发布"，与项目实际状态不符）。
#   版本号是**唯一来源**：改这里 + 打同名 tag，不要各写各的。
# 独立于引擎版本：引擎（专业版）是 0.5.0（见 HybridEngine/CMakeLists.txt），
#   轻量版是**自己一条线**，不跟着走。
__version__ = "0.1.4"
__all__ = ["Stage", "color_of", "explain", "friendly", "run_safely",
           "Coach", "诊断", "__version__"]
