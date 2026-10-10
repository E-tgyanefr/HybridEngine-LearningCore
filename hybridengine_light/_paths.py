"""内容根的**单一真值**（缺口：打包成 exe 后课程不该藏在 `_internal/` 里）。

## 为什么需要它

实测：本包内 **5 处**都各自用 `Path(__file__).resolve().parent.parent` 找"项目根"：
  `run.py` 的 `_root()` · `coach.py` · `ctl.py` · `exam.py` · `gamekit.py`

开发时那样没问题（包就在仓库里）。但打包成 exe 后：
  · PyInstaller 把包放进 `_internal/`，`__file__` 指向那里
  · ⇒ 课程数据只能落在 `_internal/`（学员看到的是个黑盒，
    而"答题文件"是他们**每天要改**的东西 —— 不该藏在那种地方）

## 做法

`内容根()` 优先读环境变量 `HYBRIDENGINE_LIGHT_ROOT`；没设就退回原来的
`parent.parent`。⇒ **没设环境变量时行为与改动前逐位相同**（既有用法零影响）。
"""
from __future__ import annotations

import os
from pathlib import Path

#: 打包产物用它把"课程内容"指到 exe 旁边（见 `_build/build_exe.py`）
ROOT_ENV = "HYBRIDENGINE_LIGHT_ROOT"


def 内容根() -> Path:
    """课程内容所在目录（`lessons/` `games/` `csharp/` … 的父目录）。

    ⚠ 没设环境变量时 = 包目录的上一级（即改动前的行为）。
    """
    env = os.environ.get(ROOT_ENV)
    if env:
        return Path(env).resolve()
    return Path(__file__).resolve().parent.parent


def 冻结() -> bool:
    """当前是不是打包产物（PyInstaller 会设 `sys._MEIPASS`）。"""
    return hasattr(__import__("sys"), "_MEIPASS")


def 命令名() -> str:
    """用法提示里该显示的命令 —— 打包成 exe 与 `python -m` 用法**不同**。

    ⚠ 不区分的话：exe 用户照着提示打 `python -m hybridengine_light`
      会失败（他们机器上可能根本没 Python），而提示文字是新手唯一的线索。

    ⚠ 放在这里（而不是 run.py）是因为**每个模块**都要用它：
      实测 aicourse / embedded / cmd_exam / raspi_course 里各有若干处提示，
      只改一处的话，学员换个课程线又会看到错的命令。
    """
    import sys as _sys
    if getattr(_sys, "frozen", False):
        import os as _os
        return _os.path.basename(_sys.executable)      # 例如 HybridEngine-light.exe
    return "python -m hybridengine_light"
