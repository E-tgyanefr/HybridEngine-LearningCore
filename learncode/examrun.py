"""learncode.examrun —— 考试的运行与评分。

与 exam.py 的分工：exam.py 是**数据结构**（题、成绩、统计），
这里是**交互与判定**（怎么问、怎么判、怎么出报告）。

════ 判分原则 ════

· choice：选对才算对。不猜分、不倒扣 —— 那是应试技巧，不是编程能力。
· output / fill：归一化后比较（空白、引号样式不扣分），但内容与大小写要一致。
· code：**真的执行**学生的函数，跑隐藏测试用例。这是唯一无法糊弄的题型。

════ 报告的重点是订正，不是分数 ════

出分之后逐题回放出错的地方 + 正确答案 + 为什么。
只给分数不给订正的考试，对学生没有价值。
"""

from __future__ import annotations

import random
import sys
import time
from dataclasses import dataclass, field

from .exam import Attempt, Progress, Question, normalize


def _确保输出编码():
    """把输出流设成 UTF-8，并让编码错误降级成替换字符。

    ══ 为什么需要（实测）══
    中文 Windows 上，stdout **不是交互式终端**时 Python 用 GBK。
    报告里的 √ × 这些字符 GBK 里没有 → UnicodeEncodeError → **整份报告一个字都打不出来**。

        python -m learncode 检查 01 > out.txt     # 没兜底时：退出码 1，输出为空

    学生最常见的求助方式就是「把输出复制给我看」——
    如果重定向到文件就崩，这条路直接断掉。

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
            # stdout 被别的库包过时没法 reconfigure —— 不是致命问题，
            # 下面的字符替换是第二道防线。
            pass


_确保输出编码()



__all__ = ["take_exam", "render_report", "interactive_available"]


def interactive_available() -> bool:
    """有没有真正的终端可以交互。管道/重定向时 stdin 不是终端，这时不能出交互题。"""
    try:
        return sys.stdin.isatty()
    except Exception:                                 # noqa: BLE001
        return False


@dataclass
class Answered:
    q: Question
    given: str = ""
    ok: bool = False
    note: str = ""            # code 题的测试结果说明


def _ask_choice(q: Question) -> str:
    print()
    print(f"  {q.q}")
    print()
    for i, opt in enumerate(q.options):
        print(f"    {i + 1}. {opt}")
    while True:
        raw = input("\n  选一个（输入序号）：").strip()
        if raw.isdigit() and 1 <= int(raw) <= len(q.options):
            return str(int(raw) - 1)
        print("  请输入选项前面的序号。")


def _ask_text(q: Question, prompt: str = "\n  你的答案：") -> str:
    print()
    print(f"  {q.q}")
    return input(prompt)


def _ask_code(q: Question) -> str:
    print()
    print(f"  {q.q}")
    print()
    if q.starter:
        for line in q.starter.splitlines():
            print(f"    {line}")
    print()
    print("  写完后用一个空行结束（每行按回车，最后多按一次回车）。")
    print("  提示：直接写函数体即可，不用管输入输出。")
    print()
    lines: list[str] = []
    while True:
        try:
            line = input("    ")
        except EOFError:
            break
        if line == "" and lines:
            break
        lines.append(line)
    return "\n".join(lines)


def _grade_code(q: Question, src: str) -> tuple[bool, str]:
    """执行学生的代码，跑隐藏测试。这是考试里唯一无法猜的题型。"""
    if not src.strip():
        return False, "没有写任何代码"
    ns: dict = {}
    try:
        exec(compile(src, "<你的答案>", "exec"), ns)      # noqa: S102 —— 这是本地教学工具，学生写的就是要跑的
    except Exception as e:                                # noqa: BLE001
        return False, f"代码跑不起来：{type(e).__name__}: {e}"

    if not q.tests:
        # 没有测试用例时退化为"能跑通且定义了东西"
        fn = [k for k, v in ns.items() if callable(v) and not k.startswith("_")]
        return bool(fn), ("定义了：" + ", ".join(fn)) if fn else "没有定义任何函数"

    passed = 0
    notes = []
    for item in q.tests:
        desc, fn = item[0], item[1]
        try:
            ok = bool(fn(ns))
        except Exception as e:                            # noqa: BLE001
            ok = False
            notes.append(f"{desc} → 出错 {type(e).__name__}: {e}")
            continue
        if ok:
            passed += 1
        else:
            notes.append(f"{desc} → 不对")
    allok = passed == len(q.tests)
    return allok, ("全部通过" if allok else f"通过 {passed}/{len(q.tests)}；" + "；".join(notes))


def take_exam(questions: list[Question], mode: str = "practice",
              limit_seconds: float | None = None, title: str = "") -> tuple[Attempt, list[Answered]]:
    """跑一场考试。返回 (成绩记录, 每题作答明细)。

    ⚠ 返回的是元组不是单个 Attempt —— 我第一版把标注写错了（写成了 Attempt），
      因为出报告需要**逐题明细**才能订正，只给汇总分数对学生没有价值。
    """
    print()
    print("═" * 56)
    print(f"  {title or ('练习' if mode == 'practice' else '考试')}")
    print("═" * 56)
    print(f"  共 {len(questions)} 题", end="")
    if limit_seconds:
        print(f"，限时 {int(limit_seconds // 60)} 分钟", end="")
    print()
    if mode == "practice":
        print("  （练习模式：答完每题都会告诉你对不对）")
    else:
        print("  （考试模式：全部答完才出结果，中途不提示对错）")

    answers: list[Answered] = []
    t0 = time.time()

    for idx, q in enumerate(questions, 1):
        print()
        print("─" * 56)
        print(f"  第 {idx}/{len(questions)} 题   [{q.topic}]", end="")
        if limit_seconds:
            used = time.time() - t0
            left = limit_seconds - used
            if left <= 0:
                print("   ! 时间到")
                break
            print(f"   剩余 {int(left // 60)}:{int(left % 60):02d}", end="")
        print()

        if q.type == "choice":
            given = _ask_choice(q)
            ok = given == str(q.answer)
        elif q.type == "code":
            given = _ask_code(q)
            ok, note = _grade_code(q, given)
            answers.append(Answered(q, given, ok, note))
            if mode == "practice":
                _feedback(ok, note)
            continue
        else:                                   # output / fill
            given = _ask_text(q)
            ok = normalize(given) == normalize(str(q.expect))

        answers.append(Answered(q, given, ok))
        if mode == "practice":
            _feedback(ok, "")

    seconds = time.time() - t0

    # ── 汇总 ──
    by_topic: dict[str, list[int]] = {}
    for a in answers:
        cur = by_topic.setdefault(a.q.topic, [0, 0])
        cur[1] += 1
        if a.ok:
            cur[0] += 1

    return Attempt(
        ts=time.time(), mode=mode, total=len(answers),
        correct=sum(1 for a in answers if a.ok), seconds=seconds,
        by_topic=by_topic, wrong_ids=[a.q.id for a in answers if not a.ok],
    ), answers


def _feedback(ok: bool, note: str):
    if ok:
        print("\n  √ 对")
    else:
        print(f"\n  × 不对{('：' + note) if note else ''}")


def render_report(att: Attempt, answers: list[Answered], title: str = "") -> str:
    """出报告：分数 + 逐题订正 + 弱项。"""
    lines: list[str] = []
    A = lines.append

    A("")
    A("═" * 56)
    A(f"  成绩：{title or '本次'}")
    A("═" * 56)
    pct = (att.correct / att.total * 100) if att.total else 0.0
    A(f"  得分    {att.correct}/{att.total}   （{pct:.0f}%）")
    A(f"  用时    {int(att.seconds // 60)} 分 {int(att.seconds % 60)} 秒")
    A("")

    # 按主题
    if att.by_topic:
        A("  按知识主题：")
        for t, (c, n) in sorted(att.by_topic.items(), key=lambda kv: kv[1][0] / kv[1][1]):
            rate = c / n
            bar = "█" * int(rate * 10) + "·" * (10 - int(rate * 10))
            mark = "  ← 这块要补" if rate < 0.7 else ""
            A(f"    {t:<10} {bar} {c}/{n}{mark}")
        A("")

    # 订正 —— 这才是重点
    wrong = [a for a in answers if not a.ok]
    if wrong:
        A("─" * 56)
        A("  订正（这部分比分数重要）")
        A("─" * 56)
        for a in wrong:
            A("")
            A(f"  [{a.q.topic}] {a.q.q}")
            if a.q.type == "choice":
                A(f"    你选了：{a.q.options[int(a.given)] if a.given.isdigit() and int(a.given) < len(a.q.options) else a.given}")
                if 0 <= a.q.answer < len(a.q.options):
                    A(f"    正确答案：{a.q.options[a.q.answer]}")
            else:
                A(f"    你写的：{a.given!r}" if a.given else "    你写的：（空）")
                if a.q.expect:
                    A(f"    正确答案：{a.q.expect!r}")
                if a.note:
                    A(f"    测试结果：{a.note}")
            if a.q.explain:
                A(f"    为什么：{a.q.explain}")
    else:
        A("  全对 —— 没有要订正的。")
    A("")
    return "\n".join(lines)
