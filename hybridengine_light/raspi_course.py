"""hybridengine_light.raspi_course —— 树莓派课程运行器。

与嵌入式课程同构（同一套 CHECK_* 框架、同样的报告格式），
但**课程目录和答案目录不同**，而且强调的重点不一样：

    嵌入式课：主循环、引脚、时序、状态机
    树莓派课：小电脑思维 —— 传感器读数、数据记录、长期运行

════ ⚠ 和嵌入式一样：验证不了电路 ════

限流电阻、正负极、接线位置、I2C 地址冲突 —— 模拟器都发现不了。
每个检查结果后面都会提醒。
"""

from __future__ import annotations
from ._paths import 命令名
from ._stub import 答题模板

import importlib.util
import sys
from pathlib import Path

__all__ = ["cmd_raspi"]


def _载入(p: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, p)
    if spec is None or spec.loader is None:
        raise ImportError(f"无法加载 {p}")
    mod = importlib.util.module_from_spec(spec)
    sys.modules[name] = mod
    spec.loader.exec_module(mod)
    return mod


def _课程们(root: Path) -> list[Path]:
    d = root / "raspi"
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
    print("  树莓派课程")
    print("═" * 56)
    if not ls:
        print("  （还没有课程。树莓派课程放在 raspi/ 目录下。）")
        return 0
    for p in ls:
        print(f"  {p.stem[:2]}   {_标题(p)}")
    print()
    print(f"  看某一课：   {命令名()} 树莓派 01")
    print(f"  检查答案：   {命令名()} 树莓派 检查 01")
    print(f"  参考答案：   {命令名()} 树莓派 答案 01")
    print()
    print("  ⚠ 和嵌入式一样用模拟器 —— 不需要真的买板子。")
    print("    模拟器管得了程序逻辑，**管不了电路**（电阻、接线、I2C 地址）。")
    print()
    print("  树莓派和单片机最大的不同：它跑的是**完整的 Linux**，")
    print("  你的程序是一个普通进程 —— 有开始有结束，不是永远在转的 loop。")
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

    答案目录 = root / "raspi-answers"
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
        answer = _载入(sol, name=f"rpi_ans_{p.stem}")
    except Exception as e:                            # noqa: BLE001
        print(explain(e, filename=str(sol)))
        return 1

    try:
        lesson = _载入(p, name=f"rpi_lesson_{p.stem}")
    except Exception as e:                            # noqa: BLE001
        print("\n  课程文件本身有问题（这不是你的错）：")
        print(explain(e, filename=str(p)))
        return 1

    print(report(run_checks(lesson, answer), title=_标题(p)))
    print("  ⚠ 提醒：检查通过只说明**程序逻辑**对。")
    print("    限流电阻、接线位置、I2C 地址冲突这些**电路/硬件问题**，模拟器发现不了。")
    print()
    return 0


def _生成模板(p: Path) -> str:
    """这一课的答题文件 —— **它本身就是一篇教程**（做法见 `_stub.答题模板`）。

    ⚠ 原来只抄「要做」那一块，学员打开看到的是「三行说明 + 一大片空白」。
      树莓派这条线的讲义最长（动不动一百多行），而它讲的全是**为什么**这么写
      （程序形态、引脚不复位、I2C 地址）—— 那些正是动手时要对照的东西。
      ⇒ 现在整块讲解都搬进来，最后才是「轮到你了」。
    """
    return 答题模板(
        p,
        命令="树莓派",
        提示="你写的应该是**函数** —— 检查会调用它，然后看**模拟树莓派**的状态"
             "（引脚、传感器读数、写出去的文件、时间推进）对不对。",
    )


def _答案(root: Path, 编号: str) -> int:
    p = _找(root, 编号)
    if p is None:
        print(f"\n  没有找到「{编号}」这一课。\n")
        return 1
    sol = root / "raspi-solutions" / p.name
    if not sol.exists():
        print(f"\n  这一课还没有参考答案（{sol}）。\n")
        return 1
    print()
    print("── 参考答案 ─────────────────────────────────────────")
    print("  （先自己写一遍再看。抄一遍不如写错一遍学得多。）")
    print("─" * 56)
    print(sol.read_text(encoding="utf-8"))
    return 0


def cmd_raspi(root: Path, argv: list[str]) -> int:
    if not argv:
        return _列课程(root)
    cmd = argv[0]
    if cmd in ("检查", "check"):
        return _检查(root, argv[1]) if len(argv) > 1 else _列课程(root)
    if cmd in ("答案", "answer"):
        return _答案(root, argv[1]) if len(argv) > 1 else _列课程(root)
    return _看课程(root, cmd)
