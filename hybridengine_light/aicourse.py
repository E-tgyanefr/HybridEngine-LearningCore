"""hybridengine_light.aicourse —— 人工智能入门课程运行器。

════ 为什么 AI 课要单独一套命令 ════

因为它的**验证对象**和别的课程不一样：

  Python 课：写一个函数 → 比输出 → 对错分明
  AI 课：写一个**学习算法** → 喂它数据 → 看它有没有真的"学"出来

后者不能靠比对输出。学生完全可以 `return 2.5` 把答案写死，
输出看着一模一样 —— 但他一行算法都没写。

所以这里的每条核心检查都用了同一个手法：

    换一批数据再问一次。真的算出来的会跟着变，写死的不会。

（这也正是 README 里写的"核心检查点"手法：不检查"看起来对不对"，
  而是**改掉依赖的东西，看结果会不会跟着变**。）

════ 为什么这套课一个第三方库都不用 ════

实测这台机器上没有 numpy / sklearn / torch / tensorflow / PIL / matplotlib。
但这不是将就 —— 对**教原理**来说反而是优势：

    sklearn.linear_model.LinearRegression().fit(X, y)   ← 一行搞定，也一行都不懂
    自己写最小二乘，用 list 和 for 循环                       ← 慢，但每一步都看得见

学 AI 最要紧的不是"会调 API"，是知道**里面在干什么**。
所以这套课的所有算法都用纯 Python 从零实现，一个 import 都不需要。

用法::

    python -m hybridengine_light AI            列出课程
    python -m hybridengine_light AI 01         看第 1 课
    python -m hybridengine_light AI 检查 01     检查你的答案
    python -m hybridengine_light AI 答案 01     参考答案
"""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

__all__ = ["cmd_ai"]


# 目录名放在这里，而不是散在各处 ——
# 学生答题文件写错地方（写进 ai-solutions/）时，改一处就能搬家。
课程目录名 = "ai"
答案目录名 = "ai-answers"        # 学生的工作文件（首次检查时自动生成模板）
参考目录名 = "ai-solutions"      # 参考答案（课程作者提供，只读）

# ⚠ 这两个目录必须分开，理由和 lessons/ 那边一样：
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
    print("  人工智能入门课程")
    print("═" * 56)
    if not ls:
        print("  （还没有课程。AI 课程放在 ai/ 目录下。）")
        return 0
    for p in ls:
        print(f"  {p.stem[:2]}   {_标题(p)}")
    print()
    print("  看某一课：   python -m hybridengine_light AI 01")
    print("  检查答案：   python -m hybridengine_light AI 检查 01")
    print("  参考答案：   python -m hybridengine_light AI 答案 01")
    print()
    print("  ⚠ 这套课**一个第三方库都不用** —— 不需要 numpy / sklearn / torch。")
    print("    所有算法都用 Python 的 list 和 for 循环从零写出来。")
    print("    用 sklearn 一行能跑完，但那样你学不到任何东西。")
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
        answer = _载入(sol, name=f"ai_ans_{p.stem}")
    except Exception as e:                            # noqa: BLE001
        print(explain(e, filename=str(sol)))
        return 1

    try:
        lesson = _载入(p, name=f"ai_lesson_{p.stem}")
    except Exception as e:                            # noqa: BLE001
        print("\n  课程文件本身有问题（这不是你的错）：")
        print(explain(e, filename=str(p)))
        return 1

    print(report(run_checks(lesson, answer), title=_标题(p)))
    print("  ⚠ 提醒：检查会**换一批数据**再问你的函数一次。")
    print("    所以把答案写死（比如直接 return 2.5）是过不了的 ——")
    print("    这一课的算法必须真的从数据里算出来。")
    print()
    return 0


def _生成模板(p: Path) -> str:
    text = p.read_text(encoding="utf-8")
    lines = [
        f'"""第 {p.stem[:2]} 课的答案 —— 在这里写你的代码。',
        "",
        "改完运行：  python -m hybridengine_light AI 检查 " + p.stem[:2],
        "卡住了：    python -m hybridengine_light AI 答案 " + p.stem[:2],
        "",
        "提示：你写的应该是一个**函数**（或几个），检查会拿不同的数据调用它。",
        "      所以别把某一次的结果写死 —— 换个数据它就错了。",
        '"""',
        "",
    ]
    # 只抄「# 要做：」这一整块，**遇到第一个空行就停**。
    #
    # ⚠ 这里和 embedded.py 的写法有意不同：那边会一直抄到第一行不是注释的代码为止。
    #   AI 课的讲义比嵌入式长得多，那样抄的结果是答题文件里塞了两百行讲义，
    #   学生打开文件要翻半天才找到"我该写什么"。
    #   讲义本身用 `python -m hybridengine_light AI 01` 看就行。
    grab = False
    for line in text.splitlines():
        s = line.strip()
        if s.startswith("# 要做"):
            grab = True
            continue
        if grab:
            if not s:
                break
            if s.startswith("#"):
                lines.append("# " + s.lstrip("# ").rstrip())
            else:
                break
    lines.append("")
    return "\n".join(lines)


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


def cmd_ai(root: Path, argv: list[str]) -> int:
    if not argv:
        return _列课程(root)
    cmd = argv[0]
    if cmd in ("检查", "check"):
        return _检查(root, argv[1]) if len(argv) > 1 else _列课程(root)
    if cmd in ("答案", "answer"):
        return _答案(root, argv[1]) if len(argv) > 1 else _列课程(root)
    return _看课程(root, cmd)
