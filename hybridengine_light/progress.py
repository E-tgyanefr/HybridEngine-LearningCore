"""「哪几课读过了」—— 学到哪了的记录。

⚠⚠ 为什么单独一个模块，而不是塞进 `learnmodel`：

  `learnmodel` 是**纯的** —— 它只负责"去哪找课程、答案路径怎么算、
  检查怎么调"，所以它能被单元测试直接调（`_build/run_tests.py` 里
  那 320 个断言靠的就是这一点）。
  **"读过哪几课"是状态，不是模型** —— 混进去之后 `learnmodel` 就
  开始有副作用了，测试也不好写。

  ⇒ 分层：`进度` 只管"记什么、存在哪"，`learn_gui` 只管"什么时候记"。

═══ 为什么桌面端也要这个 ═══

  手机版加进度可视化时想明白的一件事：**可见的进度是坚持下来的关键**。
  而桌面端原来**什么进度都没有** —— 219 课列在树里，
  学员看不到自己走过多少、还剩多少。

⚠ 存在哪：**项目根**下的 `.学习进度.json`。

  为什么不用用户主目录：
  · 这个项目的课程是**跟着仓库走**的（学员 clone 下来就是一套课）
  · 放在主目录的话，换一份课程副本进度就串了
  · 而放在项目根，`.gitignore` 掉就行，一看就知道是本地状态

⚠ 格式故意做得很松：读坏了就当空的（宁可从零开始，也不能因为
  一个坏文件让界面起不来）。
"""
from __future__ import annotations

import json
import os
from pathlib import Path

文件名 = ".学习进度.json"

#: 版本号 —— 以后要改格式时靠它判断
格式版本 = 1


def _路径(根: Path | None = None) -> Path:
    if 根 is None:
        #: ⚠ 这个文件在 `hybridengine_light/` 下 ⇒ 上一级就是项目根
        根 = Path(__file__).resolve().parent.parent
    return 根 / 文件名


def 读(根: Path | None = None) -> dict[str, float]:
    """返回 {课号: 时间戳}。读不出来就当空的。"""
    p = _路径(根)
    try:
        数据 = json.loads(p.read_text(encoding="utf-8"))
    except Exception:
        return {}
    if not isinstance(数据, dict) or 数据.get("版本") != 格式版本:
        return {}
    课 = 数据.get("读过")
    if not isinstance(课, dict):
        return {}
    出: dict[str, float] = {}
    for k, v in 课.items():
        try:
            出[str(k)] = float(v)
        except (TypeError, ValueError):
            continue
    return 出


def 写(读过: dict[str, float], 根: Path | None = None) -> bool:
    """存回去。⚠ 失败**不抛异常** —— 记进度失败不该让界面崩。"""
    p = _路径(根)
    try:
        p.write_text(
            json.dumps({"版本": 格式版本, "读过": 读过},
                       ensure_ascii=False, indent=1, sort_keys=True),
            encoding="utf-8")
        return True
    except Exception:
        return False


def 记(课号: str, 根: Path | None = None) -> bool:
    """记一课"读过了"。返回**是不是新记的**（用来决定要不要播反馈动画）。

    ⚠ 返回值有意义：界面靠它判断"要不要播那个'刚读完'的动画"。
      每次都播 = 噪音，而噪音会让人不再注意它（那反馈就白做了）。
    """
    读过 = 读(根)
    if 课号 in 读过:
        return False
    import time
    读过[课号] = time.time()
    return 写(读过, 根)
