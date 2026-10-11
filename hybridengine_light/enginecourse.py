"""`引擎` 课程线 —— 教"用组件拼游戏"，也就是**模块化编程**。

## 这条线为什么值得有

前面五门课教的是**语言的语法**（变量、循环、函数、类）。
但"会写函数"和"能做出一个东西"之间隔着一件事：**怎么把功能拆开、装成可插拔的块**。

那件事就是模块化 —— 而它光讲没用，得有个**真的架构**让人练：

    场景对象（一个"东西"） + 组件（一块"能力"）

于是"给玩家加个跟随相机"不是改玩家的代码，而是**挂一个组件**。
学员练的就是这个动作，一次一次，直到它变成本能。

## ⚠ 检查怎么做到"无头"

课程的 `CHECK_*` 用 `录制后端`（不开窗口）跑学员的场景，
然后问"画了几个圆、在哪个位置"。**比截屏比像素稳得多**，
而且学员在自己机器上跑出的结果和判分一致。

## 与其它课程线同一套契约

    engine/           课程（`CHECK_xxx(答案)` 返回 `(过没过, 说明)`）
    engine-answers/   学员的答题文件（首次检查时生成模板）
    engine-solutions/ 参考答案（只读）
"""
from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

from ._paths import 命令名
from ._stub import 答题模板

__all__ = ["cmd_引擎课程"]

课程目录名 = "engine"
答案目录名 = "engine-answers"
参考目录名 = "engine-solutions"

# ⚠ 这两个目录必须分开，理由和其它课程线一样：
#   放一起的话，"参考答案"读到的会是学生写的文件 —— 学生写错了，参考答案就是错的。


def _课程目录(root: Path) -> Path:
    return root / 课程目录名


def _载入(p: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, p)
    if spec is None or spec.loader is None:
        raise ImportError(f"无法加载 {p}")
    mod = importlib.util.module_from_spec(spec)
    sys.modules[name] = mod
    spec.loader.exec_module(mod)
    return mod


def _课程们(root: Path) -> list[Path]:
    d = _课程目录(root)
    if not d.is_dir():
        return []
    return sorted(p for p in d.glob("[0-9][0-9]_*.py"))


def _找(root: Path, 编号: str) -> Path | None:
    for p in _课程们(root):
        if p.stem.startswith(编号):
            return p
    return None


def _标题(p: Path) -> str:
    for line in p.read_text(encoding="utf-8").splitlines():
        s = line.strip()
        if s.startswith("#"):
            return s.lstrip("# ").strip()
    return p.stem


def _列课程(root: Path) -> int:
    ls = _课程们(root)
    print()
    print("═" * 56)
    print("  引擎入门 —— 用组件拼游戏")
    print("═" * 56)
    if not ls:
        print("  （还没有课程）")
        print()
        return 1
    for p in ls:
        print(f"  {p.stem[:2]}   {_标题(p)}")
    print()
    print(f"  看某一课：   {命令名()} 引擎课程 01")
    print(f"  检查答案：   {命令名()} 引擎课程 检查 01")
    print(f"  参考答案：   {命令名()} 引擎课程 答案 01")
    print()
    print("  ⚠ 这里教的是**模块化编程**：把一个功能装进一个组件，")
    print("    然后像插积木一样拼起来 —— 而不是把所有代码写成一长串。")
    print()
    return 0


def _看课程(root: Path, 编号: str) -> int:
    p = _找(root, 编号)
    if p is None:
        print(f"\n  没有找到「{编号}」这一课。\n")
        return 1
    print()
    print(p.read_text(encoding="utf-8"))
    return 0


def _检查(root: Path, 编号: str) -> int:
    from .check import run_checks, report
    from .explain import explain

    p = _找(root, 编号)
    if p is None:
        print(f"\n  没有找到「{编号}」这一课。\n")
        return 1

    答案目录 = root / 答案目录名
    sol = 答案目录 / p.name
    if not sol.exists():
        答案目录.mkdir(parents=True, exist_ok=True)
        sol.write_text(_生成模板(p), encoding="utf-8")
        print()
        print("─" * 56)
        print(f"  给你建好了答题文件：{sol}")
        print("  打开它，按里面的说明写代码，然后再跑一次这条命令。")
        print("─" * 56)
        print()
        return 1

    try:
        answer = _载入(sol, name=f"eng_ans_{p.stem}")
    except Exception as e:                            # noqa: BLE001
        print(explain(e, filename=str(sol)))
        return 1

    try:
        lesson = _载入(p, name=f"eng_lesson_{p.stem}")
    except Exception as e:                            # noqa: BLE001
        print("\n  课程文件本身有问题（这不是你的错）：")
        print(explain(e, filename=str(p)))
        return 1

    print(report(run_checks(lesson, answer), title=_标题(p)))
    print("  ⚠ 检查用的是**无头后端**（不开窗口）—— 所以你不用等窗口，")
    print("    它直接看你搭出来的场景对不对。")
    print()
    return 0


def _生成模板(p: Path) -> str:
    """这一课的答题文件 —— **它本身就是一篇教程**（做法见 `_stub.答题模板`）。

    ⚠ 原来只抄「要做」那一块，学员打开看到的是「三行说明 + 一大片空白」。
      这条线的讲解只有三四十行，全是**为什么要这样搭**（组件、能力、每帧）——
      整块搬进来代价很小，而它恰好是答题时唯一能对照的东西。
      ⇒ 现在整块讲解都搬进来，最后才是「轮到你了」。
    """
    return 答题模板(
        p,
        命令="引擎课程",
        提示="你写的应该是**函数** —— 它把搭好的场景/对象**返回**出来，"
             "检查拿这个返回值去跑（用的是无头后端，不开窗口）。",
        末尾代码=(
             "from hybridengine_light import pyengine as E\n"
             "from hybridengine_light.pyengine import components as C"
         ),
    )


def _答案(root: Path, 编号: str) -> int:
    p = _找(root, 编号)
    if p is None:
        print(f"\n  没有找到「{编号}」这一课。\n")
        return 1
    sol = root / 参考目录名 / p.name
    if not sol.exists():
        print(f"\n  这一课还没有参考答案（{sol}）。\n")
        return 1
    print()
    print("── 参考答案 ─────────────────────────────────────────")
    print("  （先自己写一遍再看。抄一遍不如写错一遍学得多。）")
    print("─" * 56)
    print(sol.read_text(encoding="utf-8"))
    return 0


def cmd_引擎课程(root: Path, argv: list[str]) -> int:
    if not argv:
        return _列课程(root)
    cmd = argv[0]
    if cmd in ("检查", "check"):
        return _检查(root, argv[1]) if len(argv) > 1 else _列课程(root)
    if cmd in ("答案", "answer"):
        return _答案(root, argv[1]) if len(argv) > 1 else _列课程(root)
    return _看课程(root, cmd)
