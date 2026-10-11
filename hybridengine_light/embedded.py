"""hybridengine_light.embedded —— 嵌入式/单片机课程运行器。

════ 为什么嵌入式单独一套命令 ════

因为它和其他四门语言的**验证方式根本不同**：

  Python / C# / C++ / Java：编译运行 → 对比输出 → 客观对错
  嵌入式：代码跑在**真实硬件**上，输出是"LED 亮了"

没有真板子，唯一的验证途径是**模拟器**。所以这里的检查都在问
"在模拟的环境里，你的代码做对事了吗"，而不是"输出对不对"。

════ ⚠ 模拟器保证什么、保证不了什么 ════

    能保证：**程序逻辑**（引脚编号、状态机、时序、边界）
    保证不了：**电路**（限流电阻、正负极、接线位置、电压）

所以每条检查的说明里都会带上这句提醒 —— 学生真的插上板子时，
才不会以为"检查通过了就一定能亮"。

用法::

    python -m hybridengine_light 嵌入式            列出课程
    python -m hybridengine_light 嵌入式 01         看第 1 课
    python -m hybridengine_light 嵌入式 检查 01     检查你的答案
    python -m hybridengine_light 嵌入式 答案 01     参考答案
"""

from __future__ import annotations
from ._paths import 命令名
from ._stub import 答题模板

import importlib.util
import sys
from pathlib import Path

__all__ = ["cmd_embedded"]


def _课程目录(root: Path) -> Path:
    return root / "embedded"


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
    print("  嵌入式 / 单片机课程")
    print("═" * 56)
    if not ls:
        print("  （还没有课程。嵌入式课程放在 embedded/ 目录下。）")
        return 0
    for p in ls:
        print(f"  {p.stem[:2]}   {_标题(p)}")
    print()
    print(f"  看某一课：   {命令名()} 嵌入式 01")
    print(f"  检查答案：   {命令名()} 嵌入式 检查 01")
    print(f"  参考答案：   {命令名()} 嵌入式 答案 01")
    print()
    print("  ⚠ 这套课程用**模拟器**跑 —— 不需要真的买板子。")
    print("    模拟器能验证程序逻辑，但**验证不了电路**（限流电阻、接线、正负极）。")
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

    答案目录 = root / "embedded-answers"
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
        answer = _载入(sol, name=f"emb_ans_{p.stem}")
    except Exception as e:                            # noqa: BLE001
        print(explain(e, filename=str(sol)))
        return 1

    try:
        lesson = _载入(p, name=f"emb_lesson_{p.stem}")
    except Exception as e:                            # noqa: BLE001
        print("\n  课程文件本身有问题（这不是你的错）：")
        print(explain(e, filename=str(p)))
        return 1

    print(report(run_checks(lesson, answer), title=_标题(p)))
    print("  ⚠ 提醒：检查通过只说明**程序逻辑**对。")
    print("    限流电阻、接线位置、LED 正负极这些**电路问题**模拟器发现不了。")
    print()
    return 0


def _生成模板(p: Path) -> str:
    """这一课的答题文件 —— **它本身就是一篇教程**（做法见 `_stub.答题模板`）。

    ⚠ 原来只抄「要做」那一块，学员打开看到的是「三行说明 + 一大片空白」，
      而嵌入式这一课的坑（上下拉、悬空输入、模拟器管不了电路）全写在讲解里 ——
      他写代码时恰恰最需要那些。⇒ 现在整块讲解都搬进来，最后才是「轮到你了」。
    """
    return 答题模板(
        p,
        命令="嵌入式",
        提示="你写的应该是**函数** —— 检查会调用它，然后看**模拟板子**的状态"
             "（引脚模式、电平、亮灭过程、时间推进）对不对。",
    )


def _答案(root: Path, 编号: str) -> int:
    p = _找(root, 编号)
    if p is None:
        print(f"\n  没有找到「{编号}」这一课。\n")
        return 1
    sol = root / "embedded-solutions" / p.name
    if not sol.exists():
        print(f"\n  这一课还没有参考答案（{sol}）。\n")
        return 1
    print()
    print("── 参考答案 ─────────────────────────────────────────")
    print("  （先自己写一遍再看。抄一遍不如写错一遍学得多。）")
    print("─" * 56)
    print(sol.read_text(encoding="utf-8"))
    return 0


def cmd_embedded(root: Path, argv: list[str]) -> int:
    if not argv:
        return _列课程(root)
    cmd = argv[0]
    if cmd in ("检查", "check"):
        return _检查(root, argv[1]) if len(argv) > 1 else _列课程(root)
    if cmd in ("答案", "answer"):
        return _答案(root, argv[1]) if len(argv) > 1 else _列课程(root)
    return _看课程(root, cmd)
