"""`引擎` 命令 —— 跑 Python 引擎写的游戏。

## 为什么要有这个入口

引擎能力如果只能"自己写代码才看得到"，那就白做了：
**得让人一条命令就能看见它在动。**

    python -m hybridengine_light 引擎            # 列出现成的游戏
    python -m hybridengine_light 引擎 贪吃蛇      # 跑起来

⚠ 这里**不做成课程**（那是 `engine_course.py` 的事）：
  这是"看一眼它能干什么"的门，课程的入口是"学怎么用它"。
"""
from __future__ import annotations

import sys
from pathlib import Path

from ._paths import 命令名

#: 现成的样例游戏：键 -> (显示名, 模块路径, 说明)
样例 = {
    "贪吃蛇": ("贪吃蛇", "hybridengine_light.pyengine.games.snake",
             "用 WASD / 方向键控制，吃红点加分"),
}


def _列游戏() -> int:
    print()
    print("═" * 56)
    print("  Python 引擎 · 现成的样例游戏")
    print("═" * 56)
    for 键, (显示, _, 说明) in 样例.items():
        print(f"  {键:8s} {说明}")
    print()
    print(f"  跑一个：     {命令名()} 引擎 贪吃蛇")
    print(f"  看看引擎能干什么：{命令名()} 引擎 说明")
    print()
    return 0


def _说明() -> int:
    print("""
════════════════════════════════════════════════════════
  Python 引擎 —— 用组件拼游戏
════════════════════════════════════════════════════════

它不是"画几个图形"，而是一套**和专业版同形状的架构**：

    场景图（父子变换）      子对象跟着父走，不用手算坐标
    组件体系（可插拔）      加功能 = 挂一个组件，不改别处代码
    固定步长循环            同一份代码在快/慢机器上结果相同
    输入 / 相机             按键查询 + 跟随相机（一个组件搞定）
    渲染后端可换            tkinter / 真引擎 / 无头录制

════════ 一段完整的游戏是这样写出来的 ════════

    from hybridengine_light import pyengine as E
    from hybridengine_light.pyengine import components as C

    游戏 = E.游戏(标题="我的游戏")
    场景 = 游戏.场景
    场景.设服务("输入", 游戏.输入)

    玩家 = 场景.造("玩家", 50, 50)
    玩家.加(C.圆点(半径=3, 颜色="红"),
           C.按键驱动(速度=40),
           C.跟随相机())          # ← 加"镜头跟着我"只是**挂一个组件**

    游戏.跑()

════════ 加功能 = 挂组件（这才是模块化）════════

    玩家.加(C.重力(地面=0))          # 会掉了
    玩家.加(C.尾迹(颜色="橙"))        # 有拖尾了
    玩家.加(自己写的组件())            # 继承 `组件`，实现 `每帧`

**没有一处需要"去别的地方插一个 if"。**

════════ 自己写一个组件（最短的例子）════════

    class 转圈(E.组件):
        def 每帧(self, dt):
            自转 = self.宿主.取(C.圆点)
            if 自转:
                自转.偏移x = 3 * math.cos(总时间)

    class 我的一点想法(E.组件):
        def 挂上时(self):    ...   # 挂上来时做一次
        def 每帧(self, dt):  ...   # 每个逻辑帧
        def 绘制(self, 画笔): ...  # 每帧画（默认什么都不画）
        def 卸载时(self):    ...   # 被移除时收尾
""")
    return 0


def cmd_引擎(root: Path, argv: list[str]) -> int:
    """`引擎` 子命令。"""
    if not argv:
        return _列游戏()
    首 = argv[0]
    if 首 in ("说明", "help", "-h", "--help", "引擎"):
        return _说明()
    if 首 not in 样例:
        print(f"\n  没有「{首}」这个样例。用 `{命令名()} 引擎` 看有哪些。\n")
        return 1
    _, 模块路径, _ = 样例[首]
    try:
        m = __import__(模块路径, fromlist=["主"])
    except Exception as e:
        print(f"\n  加载「{首}」失败：{type(e).__name__}: {e}")
        print("  （这多半是打包缺文件，请把这个信息发出来。）\n")
        return 1
    return int(m.主())
