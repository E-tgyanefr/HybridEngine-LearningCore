"""hybridengine_light.check —— 自动检查学生的答案。

════ 为什么这是课程的核心，不是附属 ════

编程课最难的不是讲知识，是**给反馈**：
  · 学生写完代码，没人告诉他"对不对" → 卡住 → 放弃
  · 老师逐个看作业 → 不可规模化

这个模块让**课程文件自己带检查**。学生在 solution.py 里写答案，
运行 `python -m hybridengine_light 检查 01`，就能看到：

    √ 名字变量存在
    √ 名字是文字类型
    × 打招呼() 应该返回「你好，小明」这种格式
       实际返回：你好
       提示：用 f"你好，{名字}" 把变量放进字符串里

每条检查都给出：**过了什么、错在哪、怎么改**。

════ 设计约定 ═══

课程文件里定义 `CHECK_*` 函数，每个返回 (是否通过, 说明文字)：

    def CHECK_名字存在(答案):
        if not hasattr(答案, "名字"):
            return False, "还没有定义「名字」这个变量"
        return True, f"名字 = {答案.名字!r}"

· 检查函数**只读不写** —— 不能改学生的代码，那样学生学不到东西
· 检查失败要给出**具体**的提示（实际值是什么、期望什么），不能只说"错了"
· 一个课程可以有任意多条检查，全部列出（不只报第一条）
"""

from __future__ import annotations

import importlib.util
import inspect
import sys
import traceback
from dataclasses import dataclass, field
from pathlib import Path


def _确保输出编码():
    """把输出流设成 UTF-8，并让编码错误降级成替换字符。

    ══ 为什么需要 ══
    中文 Windows 上，stdout **不是交互式终端**时 Python 会用 GBK。
    而报告里的 √ × 这些字符 GBK 里没有 → UnicodeEncodeError → **整份报告打不出来**。

    实测：`python -m hybridengine_light 检查 01 > out.txt` 在没设编码时直接崩，
    退出码 1、输出为空。学生想把结果贴给别人看都做不到。

    errors="replace" 是关键：宁可把个别字符显示成 ?，
    也不能因为一个字符让整份报告消失。
    """
    for 流名 in ("stdout", "stderr"):
        流 = getattr(sys, 流名, None)
        if 流 is None:
            continue
        try:
            流.reconfigure(encoding="utf-8", errors="replace")
        except Exception:
            # 有些环境（stdout 被别的库包过）没法 reconfigure —— 不是致命问题，
            # 下面的字符替换是第二道防线。
            pass


_确保输出编码()



__all__ = ["Result", "run_checks", "load_module", "report"]


@dataclass
class Result:
    name: str
    ok: bool
    detail: str = ""


@dataclass
class Report:
    lesson: str
    results: list[Result] = field(default_factory=list)
    error: str | None = None          # 学生的代码本身跑不起来时的信息

    @property
    def passed(self) -> int:
        return sum(1 for r in self.results if r.ok)

    @property
    def total(self) -> int:
        return len(self.results)

    @property
    def all_ok(self) -> bool:
        return self.error is None and self.total > 0 and self.passed == self.total


def load_module(path: Path, name: str | None = None):
    """把某个 .py 文件当模块加载。失败会抛出原始异常（由调用方决定怎么讲人话）。"""
    name = name or path.stem
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise ImportError(f"无法加载 {path}")
    mod = importlib.util.module_from_spec(spec)
    sys.modules[name] = mod
    spec.loader.exec_module(mod)
    return mod


def run_checks(lesson_mod, answer_mod, 秒上限: float | None = None) -> Report:
    """跑课程模块里所有的 CHECK_* 函数。

    ⚠⚠ **每条检查都有超时保护**（`watchdog.看门狗`）—— 这**不是**防御性编程：

        检查函数会**调用学员写的代码**。学员写个 `while True` 就永远不返回。
        桌面版一直有兜底（`learnmodel.运行` 用 `subprocess.run(timeout=20)`），
        ⚠ **但手机版没有**：安卓不允许执行应用自己的可执行文件（PEP 738）
        ⇒ 起不了子进程 ⇒ **没有"杀掉那个进程"这个选项**，
        而 Python 就嵌在应用主进程里（`Py_Initialize`），
        桥的方法还跑在 WebView 的 JS 线程上。

        ⇒ **学员在手机上写一个死循环，整个应用就卡死了 —— 只能强杀。**

    ⚠ 超时后**继续跑剩下的检查**（不是整个放弃）——
      这样学员一次就能看到"哪几条有问题"，而不是修一条跑一次。

    ⚠ 为什么"每条检查"各给一份预算，而不是整个判分给一份：
      课程里有 3~8 条检查，正常总共几十毫秒。**按整体给预算**的话，
      一条死循环会把后面所有检查的预算烧光 ⇒ 学员看到一堆"超时"，
      分不清是哪一条的锅。
    """
    from .watchdog import 超时 as _超时, 看门狗 as _看门狗, 默认秒 as _默认秒

    上限 = _默认秒 if 秒上限 is None else float(秒上限)
    rep = Report(lesson=lesson_mod.__name__)

    checks = [(n, f) for n, f in vars(lesson_mod).items()
              if n.startswith("CHECK_") and callable(f)]
    # 按定义顺序（vars 的顺序就是文件里的顺序，Python 3.7+ 保证）
    if not checks:
        rep.error = "这个课程没有定义任何检查（CHECK_ 开头的函数）"
        return rep

    for name, fn in checks:
        title = name[len("CHECK_"):]
        try:
            #: ⚠ 看门狗只包 `fn(...)` 这一句 ——
            #:   别把 `rep.results.append` 也包进去（那是我们自己的代码，不需要看）。
            with _看门狗(秒=上限):
                out = fn(answer_mod)
        except _超时 as e:
            #: ⚠ 单独一条分支：**"停不下来"和"报了个错"是两件事**，
            #:   给学员的提示语完全不同。混在一起他会去找一个不存在的语法错误。
            rep.results.append(Result(title, False, "停不下来：%s" % e))
            continue
        except Exception as e:                       # noqa: BLE001
            # 检查函数自己崩了 —— 说明学生的代码让检查没法进行（例如缺函数导致 AttributeError）
            rep.results.append(Result(title, False, f"检查无法进行：{type(e).__name__}: {e}"))
            continue
        if isinstance(out, tuple) and len(out) == 2:
            ok, detail = out
        else:
            ok, detail = bool(out), ""
        rep.results.append(Result(title, bool(ok), str(detail)))
    return rep


def report(rep: Report, title: str = "") -> str:
    """把结果渲染成给学生看的文字。"""
    lines: list[str] = []
    lines.append("")
    lines.append("─" * 56)
    lines.append(f"  检查结果：{title or rep.lesson}")
    lines.append("─" * 56)

    if rep.error:
        lines.append("")
        lines.append("  检查没能进行：")
        lines.append(f"    {rep.error}")
        return "\n".join(lines)

    for r in rep.results:
        lines.append(f"  {'√' if r.ok else '×'} {r.name}")
        if r.detail:
            # 说明文字可能多行（提示往往要分几行讲清），逐行缩进
            for dl in r.detail.splitlines():
                lines.append(f"      {dl}")

    lines.append("")
    if rep.all_ok:
        lines.append(f"  全部通过（{rep.passed}/{rep.total}）—— 这一课过了！")
        lines.append("  继续下一课吧。")
    else:
        lines.append(f"  通过 {rep.passed}/{rep.total}。")
        lines.append("  上面 × 的部分就是还要改的地方 —— 按提示改完再跑一次。")
        lines.append("")
        lines.append("  卡住了？把 × 那几行连同你的代码发出去问，比空想快得多。")
    lines.append("")
    return "\n".join(lines)
