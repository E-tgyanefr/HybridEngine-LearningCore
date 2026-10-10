"""hybridengine_light.cmd_exam —— 「考试」子命令。

单独一个文件的理由：run.py 已经管着课程的三条命令，再把考试塞进去会变成一个
谁都不想读的长函数。考试有自己的选项（模式/主题/题量/限时），值得独立。

════ 用法 ════

    python -m hybridengine_light 考试                  随机抽 10 题，练习模式
    python -m hybridengine_light 考试 --考试            考试模式（不即时提示、限时）
    python -m hybridengine_light 考试 --主题 循环        只考某个主题
    python -m hybridengine_light 考试 --题量 20
    python -m hybridengine_light 考试 --限时 10          限时 10 分钟
    python -m hybridengine_light 考试 --成绩             看历史与弱项
    python -m hybridengine_light 考试 --题目             看看题库里有什么
"""

from __future__ import annotations

import random
import sys
from pathlib import Path

from .exam import load_banks, Progress
from .examrun import interactive_available, render_report, take_exam

__all__ = ["cmd_exam"]


def _parse(argv: list[str]) -> dict:
    """极简参数解析。不引入 argparse 以外的东西 —— 其实 argparse 也行，
    但这些参数很少，手写反而更好读。"""
    o = {"mode": "practice", "topic": None, "n": 10, "limit": None,
         "show_scores": False, "list": False}
    i = 0
    while i < len(argv):
        a = argv[i]
        if a in ("--考试", "--exam"):
            o["mode"] = "exam"
        elif a in ("--练习", "--practice"):
            o["mode"] = "practice"
        elif a in ("--主题", "--topic") and i + 1 < len(argv):
            i += 1
            o["topic"] = argv[i]
        elif a in ("--题量", "--n") and i + 1 < len(argv):
            i += 1
            try:
                o["n"] = max(1, int(argv[i]))
            except ValueError:
                pass
        elif a in ("--限时", "--limit") and i + 1 < len(argv):
            i += 1
            try:
                o["limit"] = max(1.0, float(argv[i]) * 60.0)     # 参数单位是分钟
            except ValueError:
                pass
        elif a in ("--成绩", "--scores"):
            o["show_scores"] = True
        elif a in ("--题目", "--list"):
            o["list"] = True
        i += 1
    return o


def cmd_exam(root: Path, argv: list[str]) -> int:
    opts = _parse(argv)
    bank, metas = load_banks(root)

    if not bank:
        print()
        print("  题库是空的（exams/ 目录下没有题目）。")
        print("  题库格式见 hybridengine_light/exam.py 顶部说明。")
        print()
        return 1

    progress = Progress(root / "exams" / "成绩记录.json")

    if opts["list"]:
        return _list_bank(bank)

    if opts["show_scores"]:
        return _show_scores(progress, bank)

    # ── 抽题 ──
    pool = bank
    if opts["topic"]:
        pool = [q for q in pool if opts["topic"] in q.topic]
        if not pool:
            topics = sorted({q.topic for q in bank})
            print(f"\n  没有「{opts['topic']}」这个主题。现有主题：{'、'.join(topics)}\n")
            return 1

    n = min(opts["n"], len(pool))
    picked = random.sample(pool, n)

    # code 题需要交互输入多行代码 —— 管道里没法做，直接剔除并说明
    if not interactive_available():
        dropped = [q for q in picked if q.type == "code"]
        if dropped:
            picked = [q for q in picked if q.type != "code"]
            print(f"\n  （当前不是交互终端，跳过了 {len(dropped)} 道需要写代码的题）")
        if not picked:
            print("\n  剩下的题都需要交互输入。请在终端里直接运行。\n")
            return 1

    limit = opts["limit"]
    if opts["mode"] == "exam" and limit is None:
        limit = max(60.0, n * 60.0)        # 考试模式默认每题 1 分钟

    title = "考试" if opts["mode"] == "exam" else "练习"
    if opts["topic"]:
        title += f"（{opts['topic']}）"

    att, answers = take_exam(picked, mode=opts["mode"], limit_seconds=limit, title=title)
    print(render_report(att, answers, title=title))
    progress.record(att)

    weak = progress.weak_topics()
    if weak:
        print("  累计弱项（历史正确率低于 70% 的主题）：")
        for t, rate, cnt in weak:
            print(f"    {t}   {rate * 100:.0f}%   （共做过 {cnt} 题）")
        print()
        print("  建议：回到对应课程重看一遍，再来考这个主题。")
        print()
    return 0


def _list_bank(bank) -> int:
    by_topic: dict[str, list] = {}
    for q in bank:
        by_topic.setdefault(q.topic, []).append(q)
    print()
    print("═" * 56)
    print(f"  题库：共 {len(bank)} 题，{len(by_topic)} 个主题")
    print("═" * 56)
    for t, qs in sorted(by_topic.items()):
        kinds: dict[str, int] = {}
        for q in qs:
            kinds[q.type] = kinds.get(q.type, 0) + 1
        kind_s = "  ".join(f"{k}×{v}" for k, v in sorted(kinds.items()))
        print(f"  {t:<12} {len(qs):>3} 题    {kind_s}")
    print()
    print("  只考某个主题： python -m hybridengine_light 考试 --主题 循环")
    print()
    return 0


def _show_scores(progress: Progress, bank) -> int:
    print()
    print("═" * 56)
    print("  成绩历史")
    print("═" * 56)
    if not progress.attempts:
        print("  还没有考过。先来一场：python -m hybridengine_light 考试")
        print()
        return 0

    import time as _t
    for a in progress.attempts[-10:]:
        when = _t.strftime("%m-%d %H:%M", _t.localtime(a.ts))
        pct = (a.correct / a.total * 100) if a.total else 0
        print(f"  {when}  {a.mode:<8} {a.correct:>2}/{a.total:<2}  {pct:>3.0f}%  "
              f"{int(a.seconds // 60)}分{int(a.seconds % 60):02d}秒")
    print()

    stats = progress.topic_stats()
    if stats:
        print("  累计按主题：")
        for t, (c, n) in sorted(stats.items(), key=lambda kv: kv[1][0] / kv[1][1]):
            rate = c / n
            bar = "█" * int(rate * 10) + "·" * (10 - int(rate * 10))
            print(f"    {t:<12} {bar} {c}/{n}")
        print()

    weak = progress.weak_topics()
    if weak:
        print("  需要补的（正确率 < 70%）：")
        for t, rate, cnt in weak:
            print(f"    {t}   {rate * 100:.0f}%")
    else:
        print("  没有明显弱项。")
    print()
    return 0
