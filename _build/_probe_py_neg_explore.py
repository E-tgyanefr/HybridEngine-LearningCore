# -*- coding: utf-8 -*-
r"""探索器：给 Python 线**没被反面验证覆盖**的课，自动找出「能被抓住的错答案」。

════════════════════════════════════════════════════════════════
  为什么要有这一步（而不是直接手写错答案）
════════════════════════════════════════════════════════════════

`_verify_negative_all.py` 的账本里 Python 线还缺 19 课
（00 02 06 17 18 19 20 21 22 23 24 47~54）。要补上，每一课都得有
**一个能被检查拒绝的错答案**，而且不能是"让程序整个跑不起来"
那种（那叫**弱捕获**：每条检查都报 ×，什么也没证明）。

手写 19 个错答案意味着要先读 19 份课程文件（几千行）。
先跑这个探索器，**让机器去找**哪些改动真的能被抓住、
以及被哪一条检查抓住 —— 然后人只从候选里挑一个像学生的错。

════════════════════════════════════════════════════════════════
  它怎么工作
════════════════════════════════════════════════════════════════

  ① 拿每一课的**参考答案**（`lessons/solutions/NN_*.py`，只读）当基线。
  ② 用 `ast` 定位可改的地方（**只在真代码里改，不碰字符串/注释**）：
       · 整数常量      n → n±1 / 0 / 1 / 2
       · 比较运算符    <  <=  >  >=  ==  !=  互换
       · 算术运算符    +  -  *  //  %         互换
       · 布尔运算符    and  or                互换
       · 删掉一条语句  （"忘了写那一步"这种错）
  ③ 每条变异跑一遍**真正的检查**（`hybridengine_light` 的 `检查` 通路），
     记下：几个 √、几个 ×、失败的检查名。
  ④ 排序输出候选：优先「恰好一条 ×、其余全 √」——
     那说明**这一条检查精确地抓住了这个错**，而不是整个程序崩了。

════════════════════════════════════════════════════════════════
  ⚠ 它不写 `lessons/answers/`（这是它能和别的进程并行跑的原因）
════════════════════════════════════════════════════════════════

  它把 `hybridengine_light.run.ANSWERS_DIR` 改成一个**绝对路径的临时目录**
  （`<temp>/<stem>.py` 或 `<temp>/<stem>/main.py`），检查通路一个字没改，
  但学生的答题区**一个文件都不动**。
  ⇒ 不会和 `_verify_py_neg2.py` / `run_tests.py` 那些写 `lessons/answers/`
    的脚本互相清场（那个坑见 `引擎\环境坑与教训.md` 第 0b 条）。

用法：
    python _build/_probe_py_neg_explore.py            # 跑全部缺的课
    python _build/_probe_py_neg_explore.py 17 18      # 只跑这两课

⚠ 这是**探索工具**，不是判据：它的结果要人工挑，挑完写进
  `_verify_py_neg3.py`（那个才是进 `_verify_negative_all.py` 的正式脚本）。
"""
import ast
import io
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, ROOT)

#: 账本里 Python 线**没有**反面验证的课（真值来自 _verify_negative_all.py --列）
缺的课 = ["00", "02", "06", "17", "18", "19", "20", "21", "22", "23", "24",
          "47", "48", "49", "50", "51", "52", "53", "54"]

解目录 = os.path.join(ROOT, "lessons", "solutions")

#: 子进程外壳：把 ANSWERS_DIR 指到临时目录，然后照常走「检查」通路。
外壳 = r'''
import io, json, os, sys, contextlib
sys.stdout.reconfigure(encoding="utf-8", errors="replace")
sys.path.insert(0, r"%(root)s")
import hybridengine_light.run as R
R.ANSWERS_DIR = os.environ["PYNEG_ANS"]
计划 = json.load(io.open(sys.argv[1], encoding="utf-8"))
出 = []
好 = 0
for 项 in 计划:
    p = R._find(项["编号"])
    if p is None:
        出.append(dict(项, out="**找不到这一课**"))
        continue
    路 = R._答题路径(p)
    路.parent.mkdir(parents=True, exist_ok=True)
    路.write_text(项["源码"], encoding="utf-8")
    缓 = io.StringIO()
    with contextlib.redirect_stdout(缓):
        R._cmd_check(项["编号"])
    o = 缓.getvalue()
    出.append(dict(项, out=o))
    # ⚠ 够用就停：目标是给每课找**一个**能用的错答案，
    #   不是把所有变异都跑完（第一版跑全量，某一课转不停，整批白跑）。
    勾 = sum(1 for l in o.splitlines() if l.strip().startswith("√ "))
    叉 = sum(1 for l in o.splitlines() if l.strip().startswith("× "))
    if 叉 == 1 and 勾 >= 1:
        好 += 1
        if 好 >= 3:
            break
json.dump(出, io.open(sys.argv[2], "w", encoding="utf-8"), ensure_ascii=False)
'''


# ══════════════════════════════════════════════════════════════
#  变异生成（只动真代码：位置全部来自 ast）
# ══════════════════════════════════════════════════════════════

比较互换 = {"Lt": "GtE", "LtE": "Gt", "Gt": "LtE", "GtE": "Lt",
            "Eq": "NotEq", "NotEq": "Eq", "Is": "IsNot", "IsNot": "Is",
            "In": "NotIn", "NotIn": "In"}
比较写法 = {"Lt": "<", "LtE": "<=", "Gt": ">", "GtE": ">=",
            "Eq": "==", "NotEq": "!=", "Is": "is", "IsNot": "is not",
            "In": "in", "NotIn": "not in"}
算术互换 = {"Add": "Sub", "Sub": "Add", "Mult": "FloorDiv",
            "FloorDiv": "Mult", "Mod": "FloorDiv", "Div": "FloorDiv"}
算术写法 = {"Add": "+", "Sub": "-", "Mult": "*", "FloorDiv": "//",
            "Mod": "%", "Div": "/"}


def _位置(源码, 节):
    """把 (行, 列) 换算成源码里的**字节**偏移。

    ⚠ ast 的 col_offset 是 **UTF-8 字节**偏移，不是字符偏移 ——
      这个仓库通篇是中文标识符（`购物 = ["苹果"]`），
      按字符切会切在半个汉字上（第一次就是这么错的）。
    """
    字节行 = [x.encode("utf-8") for x in 源码.splitlines(keepends=True)]
    起 = sum(len(x) for x in 字节行[:节[0] - 1]) + 节[1]
    止 = sum(len(x) for x in 字节行[:节[2] - 1]) + 节[3]
    return 起, 止


def 生成变异(源码):
    """[(描述, 变异后的源码)] —— 每条只改**一处**（环境坑与教训 第 23 条）。"""
    出 = []
    字节源 = 源码.encode("utf-8")
    try:
        树 = ast.parse(源码)
    except SyntaxError:
        return 出

    def 替换(起, 止, 新):
        变 = (字节源[:起] + 新.encode("utf-8") + 字节源[止:]).decode("utf-8")
        if 变 == 源码:
            return
        try:
            ast.parse(变)
        except SyntaxError:
            return
        出.append(变)

    for 节 in ast.walk(树):
        # ── 整数常量 ────────────────────────────────────────────
        if (isinstance(节, ast.Constant) and isinstance(节.value, int)
                and not isinstance(节.value, bool)):
            n = 节.value
            a, b = _位置(源码, (节.lineno, 节.col_offset,
                                节.end_lineno, 节.end_col_offset))
            原文 = 字节源[a:b].decode("utf-8", "replace")
            if not 原文.strip().lstrip("-").isdigit():
                continue
            for 新 in (n + 1, n - 1, 0, 1, 2, 3):
                if 新 == n or 新 < 0:
                    continue
                替换(a, b, str(新))
        # ── 比较运算符 ──────────────────────────────────────────
        elif isinstance(节, ast.Compare) and 节.ops:
            名 = type(节.ops[0]).__name__
            if 名 in 比较互换:
                左, 右 = 节.left, 节.comparators[0]
                a, b = _位置(源码, (左.end_lineno, 左.end_col_offset,
                                    右.lineno, 右.col_offset))
                替换(a, b, " %s " % 比较写法[比较互换[名]])
        # ── 算术运算符 ──────────────────────────────────────────
        elif isinstance(节, ast.BinOp):
            名 = type(节.op).__name__
            if 名 in 算术互换:
                a, b = _位置(源码, (节.left.end_lineno, 节.left.end_col_offset,
                                    节.right.lineno, 节.right.col_offset))
                替换(a, b, " %s " % 算术写法[算术互换[名]])
        # ── 布尔运算符 ──────────────────────────────────────────
        elif isinstance(节, ast.BoolOp):
            名 = "And" if isinstance(节.op, ast.And) else "Or"
            新 = "Or" if 名 == "And" else "And"
            值 = 节.values
            for i in range(len(值) - 1):
                a, b = _位置(源码, (值[i].end_lineno, 值[i].end_col_offset,
                                    值[i + 1].lineno, 值[i + 1].col_offset))
                替换(a, b, " %s " % ("or" if 新 == "Or" else "and"))

    # ── 删掉一条单行语句（"忘了写那一步"）──────────────────────
    删除 = []
    for 节 in ast.walk(树):
        for 子 in ast.iter_child_nodes(节):
            if isinstance(子, ast.stmt) and not isinstance(
                    子, (ast.FunctionDef, ast.ClassDef, ast.Return,
                         ast.Import, ast.ImportFrom, ast.Expr)):
                if 子.lineno == 子.end_lineno and not isinstance(子, ast.Pass):
                    删除.append(子)
    for 节 in 删除[:14]:
        行们 = 源码.splitlines(keepends=True)
        if 节.lineno - 1 >= len(行们):
            continue
        变 = "".join(行们[:节.lineno - 1] + 行们[节.end_lineno:])
        try:
            ast.parse(变)
        except SyntaxError:
            continue
        if 变 != 源码:
            出.append(变)
    return 出


def 变异描述(旧, 新):
    """一句话说清改了什么（给输出用）。"""
    a = [x for x in 旧.splitlines() if x.strip()]
    b = [x for x in 新.splitlines() if x.strip()]
    for i in range(max(len(a), len(b))):
        左 = a[i] if i < len(a) else "（没有）"
        右 = b[i] if i < len(b) else "（没有）"
        if 左 != 右:
            return "%s  ⇒  %s" % (左.strip()[:52], 右.strip()[:52])
    return "（内容有变，但逐行看不出差别）"


def 判(out):
    """(全过?, [√的检查名], [×的检查名])。"""
    过, 叉 = [], []
    for l in out.splitlines():
        s = l.strip()
        if s.startswith("√ "):
            过.append(s[2:].strip())
        elif s.startswith("× "):
            叉.append(s[2:].strip())
    return ("全部通过" in out), 过, 叉


def 主(选):
    计划 = []
    基线 = {}
    for 编 in 选:
        匹配 = [f for f in os.listdir(解目录)
                if f.startswith(编 + "_") and f.endswith(".py")]
        if not 匹配:
            print("  !! %s 没有参考答案，跳过" % 编)
            continue
        原文 = io.open(os.path.join(解目录, 匹配[0]), encoding="utf-8").read()
        基线[编] = 原文
        变异们 = 生成变异(原文)
        print("  %s 生成 %d 条候选变异" % (编, len(变异们)))
        for i, 变 in enumerate(变异们[:30]):
            计划.append({"编号": 编, "序号": i, "源码": 变,
                         "描述": 变异描述(原文, 变)})

    if not 计划:
        print("  **一条候选都没有**")
        return 1

    临时 = tempfile.mkdtemp(prefix="pyneg_")
    壳路 = os.path.join(临时, "_shell.py")
    io.open(壳路, "w", encoding="utf-8").write(外壳 % {"root": ROOT})
    每课计划 = {}
    for 项 in 计划:
        每课计划.setdefault(项["编号"], []).append(项)

    # ⚠ **一课一个子进程**，各有超时。
    #   第一版是 19 课全塞进一个子进程、给 30 分钟总超时 ——
    #   结果某条变异让某一课**转不停**（算法课很容易），整批全废，
    #   而且连"跑到哪一课了"都看不出来。分课跑：挂只挂那一课。
    print()
    print("═" * 70)
    没跑到 = []
    try:
        for 编 in [c for c in 选 if c in 每课计划]:
            计划路 = os.path.join(临时, "计划_%s.json" % 编)
            出路 = os.path.join(临时, "结果_%s.json" % 编)
            io.open(计划路, "w", encoding="utf-8").write(
                json.dumps(每课计划[编], ensure_ascii=False))
            结果 = None
            try:
                子 = subprocess.run([sys.executable, 壳路, 计划路, 出路],
                                    cwd=ROOT, timeout=150,
                                    env={**os.environ, "PYNEG_ANS": 临时,
                                         "PYTHONIOENCODING": "utf-8"})
                if os.path.isfile(出路):
                    结果 = json.load(io.open(出路, encoding="utf-8"))
                else:
                    print("  !! 第 %s 课：子进程没出结果（退出码 %d）"
                          % (编, 子.returncode))
            except subprocess.TimeoutExpired:
                print("  !! 第 %s 课：150 秒没跑完（多半是某条变异转不停）"
                      "—— 这一课要人工看" % 编)
            候选 = []
            for 项 in (结果 or []):
                全过, 过, 叉 = 判(项["out"])
                候选.append(dict(项, 全过=全过, 过=过, 叉=叉))
            if not 候选:
                没跑到.append(编)
                continue
            好 = [c for c in 候选 if not c["全过"]]
            # 排序：恰好一条 × 的最干净 → √ 多的 → 描述短的
            好.sort(key=lambda c: (len(c["叉"]) != 1, -len(c["过"]), len(c["描述"])))
            print()
            print("  ── 第 %s 课：%d 条候选，%d 条能被拒（%d 条全过）"
                  % (编, len(候选), len(好), len(候选) - len(好)))
            if not 好:
                print("     ⚠ 一条都拒绝不了 —— 这一课要人工写")
            for c in 好[:5]:
                print("     [%d√ %d×] %s" % (len(c["过"]), len(c["叉"]), c["描述"]))
                print("            被拒于：%s" % " · ".join(c["叉"][:3]))
                if len(c["过"]) == 0:
                    print("            ⚠ 弱捕获（一个 √ 都没有）—— 不要用这条")
                if not c["过"] and not c["叉"]:
                    摘 = [x.strip() for x in c["out"].splitlines() if x.strip()][:3]
                    print("            ⚠ 连 √/× 都没有（检查没跑到）：%s"
                          % " | ".join(摘))
    finally:
        shutil.rmtree(临时, ignore_errors=True)

    if 没跑到:
        print()
        print("  **没跑到的课**：%s" % " ".join(没跑到))
    return 0


if __name__ == "__main__":
    参 = [a for a in sys.argv[1:] if not a.startswith("-")]
    sys.exit(主(参 or 缺的课))
