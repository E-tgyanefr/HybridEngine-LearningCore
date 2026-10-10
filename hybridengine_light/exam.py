"""hybridengine_light.exam —— 考试系统。

════ 和「课程检查」的区别 ════

课程检查（`hybridengine_light check`）是**练习**：开放式作答，不限时，错了可以反复改。
考试系统是**评估**：随机抽题、限时、一次成绩、记录历史。两者的用途完全不同，
所以是两套东西，不是同一套的两个模式。

════ 为什么要有考试 ════

自学最大的问题不是没材料，是**不知道自己到底会不会**。
"看懂了"和"能写出来"之间隔得很远，而只有后者算数。

所以这个考试系统的设计目标是**暴露盲点**，不是给分数：
  · 出分之后按知识主题给出正确率 —— 哪一块弱一眼看得见
  · 记录历史 —— 同一个主题反复错，那就是没学会，不是手滑
  · 代码题必须**真的能跑通**才算对，不猜选项、不糊弄

════ 题型 ════

    choice    选择题      给几个选项，选一个
    output    预测输出    给一段代码，问它会打印什么
    fill      填空        补全一行代码
    code      写代码      写一个函数，用隐藏的测试用例判定对错

code 题的判定方式是**真的执行学生的函数**并跑测试用例 ——
这是唯一能防止"看起来会了"的方式。

════ 题库格式 ════

题库放在 exams/ 目录下，一个主题一个文件（`01_基础.py` 这样编号）。
每个文件里定义 QUESTIONS 列表，每一项是一个 dict：

    {
        "id": "基础-001",
        "type": "choice",              # choice / output / fill / code
        "topic": "变量",                # 用于成绩按主题聚合
        "level": 1,                    # 1=入门 2=进阶 3=提高
        "q": "题目文字",
        "options": ["A", "B", "C"],    # choice 用
        "answer": 0,                   # choice 用（正确选项的下标）
        "expect": "答案文字",           # output / fill 用（会做归一化后比较）
        "starter": "def f():\n    ...",  # code 用的起始代码
        "tests": [("输入描述", lambda f: ...)],   # code 用：每个返回 (是否通过, 说明)
        "explain": "为什么是这个答案",   # 答错时给他看
    }

为什么要 `explain` 字段：考试的价值不在分数，在**订正**。
答错却不知道错在哪，这次考试就白考了。
"""

from __future__ import annotations

import importlib.util
import json
import random
import re
import sys
import time
from dataclasses import dataclass, field
from pathlib import Path

__all__ = ["Question", "BANK", "load_banks", "Exam", "Progress", "normalize"]


def normalize(s: str) -> str:
    """答案比较前的归一化。

    为什么需要：学生答 `"hello"` 和 `hello`、`print("x")` 和 `print( 'x' )`
    在"意思"上是一样的。考试不该考标点洁癖。
    但仍然保留大小写与内容差异 —— 那才是要考的东西。
    """
    if not isinstance(s, str):
        s = str(s)
    s = s.strip()
    s = s.replace("“", '"').replace("”", '"').replace("‘", "'").replace("’", "'")
    s = re.sub(r"\s+", " ", s)                  # 连续空白当一个空格
    s = re.sub(r"\s*([(),:=+\-*/\[\]{}])\s*", r"\1", s)   # 符号周围空白去掉
    return s.strip()


@dataclass
class ExamMeta:
    """一个题库文件描述的是哪门考试。

    为什么要这个：课程习题（考「学没学会」）和考级题库（考「能不能过那门试」）
    是两种东西 —— 后者有明确的考试名、级别、分值分布，学生需要知道自己在准备什么。
    没有这层元数据，题库就只是一堆散题。
    """
    name: str                      # 考试名，例如「全国计算机等级考试 二级 Python」
    short: str = ""                # 简称，命令里用
    level: str = ""                # 级别说明
    source: str = ""              # 依据（大纲/样卷；不抄原题，只对齐考纲）
    duration_min: int = 0          # 建议时长（分钟）；0=不限


@dataclass
class Question:
    id: str
    type: str
    topic: str
    level: int
    q: str
    explain: str = ""
    options: list[str] = field(default_factory=list)
    answer: int = -1
    expect: str = ""
    starter: str = ""
    tests: list = field(default_factory=list)
    exam: str = ""                 # 属于哪门考试（取 ExamMeta.short；空 = 通用课程题）
    points: float = 1.0            # 分值（考级题库里不同题型分值不同）


def load_banks(root: Path | None = None) -> tuple[list[Question], list[ExamMeta]]:
    """加载 exams/ 下所有题库。返回 (所有题目, 所有考试元数据)。

    文件本身出错就让异常冒出来 —— 那是我们自己的 bug，不该藏起来
    （题库语法错的时候，学生看到"题库加载失败"比看到一堆题更糟）。
    """
    d = (root or Path(__file__).resolve().parent.parent) / "exams"
    if not d.is_dir():
        return [], []
    out: list[Question] = []
    metas: list[ExamMeta] = []
    for p in sorted(d.glob("*.py")):
        if p.name.startswith("_"):
            continue
        spec = importlib.util.spec_from_file_location(f"bank_{p.stem}", p)
        if spec is None or spec.loader is None:
            continue
        mod = importlib.util.module_from_spec(spec)
        sys.modules[spec.name] = mod
        spec.loader.exec_module(mod)
        raw_meta = getattr(mod, "EXAM", None)
        # 题库里写的是普通 dict（内容文件不该要求 import 我们的类型），这里转成 ExamMeta
        meta: ExamMeta | None = None
        if isinstance(raw_meta, dict):
            meta = ExamMeta(
                name=raw_meta.get("name", ""),
                short=raw_meta.get("short", ""),
                level=raw_meta.get("level", ""),
                source=raw_meta.get("source", ""),
                duration_min=int(raw_meta.get("duration_min", 0)),
            )
        elif isinstance(raw_meta, ExamMeta):
            meta = raw_meta
        bank_key = meta.short if meta else ""
        if meta:
            metas.append(meta)
        for raw in getattr(mod, "QUESTIONS", []):
            out.append(Question(
                id=raw.get("id", p.stem),
                type=raw.get("type", "choice"),
                topic=raw.get("topic", "未分类"),
                level=int(raw.get("level", 1)),
                q=raw.get("q", ""),
                explain=raw.get("explain", ""),
                options=list(raw.get("options", [])),
                answer=int(raw.get("answer", -1)),
                expect=raw.get("expect", ""),
                starter=raw.get("starter", ""),
                tests=list(raw.get("tests", [])),
                exam=raw.get("exam", bank_key),
                points=float(raw.get("points", 1.0)),
            ))
    return out, metas


def validate_bank(bank: list[Question]) -> list[str]:
    """题库自检 —— 返回问题清单（空列表=没问题）。

    ⚠ 为什么需要它：题库是**数据**，写错了不会立刻暴露。
      我自己就踩过：题库文件里一个引号没转义，直到学生运行
      `python -m hybridengine_light 考试` 才炸出来 —— 那时看到的是
      "SyntaxError: Perhaps you forgot a comma?"，而学生根本不知道那是什么。
      题库是内容，内容必须先自检再发布。
    """
    problems: list[str] = []
    seen: set[str] = set()
    for q in bank:
        tag = f"[{q.id}]"
        if q.id in seen:
            problems.append(f"{tag} 题号重复")
        seen.add(q.id)
        if not q.q.strip():
            problems.append(f"{tag} 题目文字为空")
        if q.type == "choice":
            if len(q.options) < 2:
                problems.append(f"{tag} 选择题至少要有 2 个选项")
            if not (0 <= q.answer < len(q.options)):
                problems.append(f"{tag} 选择题的 answer={q.answer} 超出了选项范围"
                                f"（有 {len(q.options)} 个选项，应该是 0..{len(q.options) - 1}）")
        elif q.type in ("output", "fill"):
            if not q.expect.strip():
                problems.append(f"{tag} {q.type} 题没有 expect（标准答案）")
        elif q.type == "code":
            if not q.tests:
                problems.append(f"{tag} 代码题没有 tests —— 没法判对错")
            for i, item in enumerate(q.tests):
                if not (isinstance(item, tuple) and len(item) == 2 and callable(item[1])):
                    problems.append(f"{tag} 第 {i + 1} 个测试用例格式不对"
                                    f"（应为 (说明, lambda ns: ...)）")
        else:
            problems.append(f"{tag} 未知题型 {q.type!r}")
        if not q.explain.strip():
            problems.append(f"{tag} 缺 explain —— 学生答错后看不懂为什么，这题等于白做")
    return problems


BANK: list[Question] = []          # 由 run.py 载入后填充（避免 import 时就扫盘）


# ══════════════════════════════════════════════════════════
#  成绩记录
# ══════════════════════════════════════════════════════════
@dataclass
class Attempt:
    ts: float
    mode: str                  # practice / exam
    total: int
    correct: int
    seconds: float
    by_topic: dict[str, list[int]] = field(default_factory=dict)   # 主题 → [对, 总]
    wrong_ids: list[str] = field(default_factory=list)


class Progress:
    """成绩历史。存 JSON —— 便于学生自己看、也便于老师收。"""

    def __init__(self, path: Path):
        self.path = path
        self.attempts: list[Attempt] = []
        self.load()

    def load(self):
        if not self.path.exists():
            return
        try:
            raw = json.loads(self.path.read_text(encoding="utf-8"))
        except (OSError, ValueError):
            return                    # 文件坏了就当没有 —— 不能因为历史记录丢了就崩
        for a in raw.get("attempts", []):
            self.attempts.append(Attempt(
                ts=a.get("ts", 0), mode=a.get("mode", "?"),
                total=a.get("total", 0), correct=a.get("correct", 0),
                seconds=a.get("seconds", 0),
                by_topic={k: list(v) for k, v in a.get("by_topic", {}).items()},
                wrong_ids=list(a.get("wrong_ids", [])),
            ))

    def save(self):
        self.path.parent.mkdir(parents=True, exist_ok=True)
        data = {"attempts": [
            {"ts": a.ts, "mode": a.mode, "total": a.total, "correct": a.correct,
             "seconds": round(a.seconds, 1), "by_topic": a.by_topic,
             "wrong_ids": a.wrong_ids}
            for a in self.attempts[-100:]        # 只留最近 100 次
        ]}
        self.path.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")

    def record(self, a: Attempt):
        self.attempts.append(a)
        self.save()

    # ── 统计 ──
    def topic_stats(self) -> dict[str, list[int]]:
        """把历次成绩按主题累加 → {主题: [对, 总]}。这是"哪里弱"的依据。"""
        agg: dict[str, list[int]] = {}
        for a in self.attempts:
            for t, (c, n) in a.by_topic.items():
                cur = agg.setdefault(t, [0, 0])
                cur[0] += c
                cur[1] += n
        return agg

    def weak_topics(self, threshold: float = 0.7, min_n: int = 3) -> list[tuple[str, float, int]]:
        """正确率低于 threshold 且样本够多的主题。样本太少不下结论。"""
        out = []
        for t, (c, n) in self.topic_stats().items():
            if n < min_n:
                continue
            rate = c / n
            if rate < threshold:
                out.append((t, rate, n))
        out.sort(key=lambda x: x[1])
        return out
