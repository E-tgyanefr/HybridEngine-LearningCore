# -*- coding: utf-8 -*-
r"""探索器：给 **C# 课线**没被反面验证覆盖的 24 课，自动找「能被抓住的错答案」。

思路和 `_probe_py_neg_explore.py` 一样（那一份补的是 Python 线）：
拿每一课的**参考答案**当基线，只改**一处**，跑真正的检查，看它抓不抓得住。

⚠ 与 Python 那份的两个不同：
  · C# 的参考答案在课程文件里（`csharp/cs-lessons/Lessons/第NN课*.cs` 的
    `public static string 参考答案 => '''…''';` 那种原始字符串），不是单独的 solutions 目录。
  · 检查要起 .NET 进程（C# 课程工程）（`dotnet run --no-build --project csharp/cs-lessons 检查 NN`），
    每次 1 秒上下 ⇒ 每课的候选数要**截断**，而且**够用就停**。

⚠ 它写 `cs-answers/`（C# 那条线的答题区）—— **同一时刻只能一个进程写**。
  跑之前先确认没有别的 `_verify` / `run_tests` 在跑。

用法：
    python -u _build/_probe_c_neg_explore.py            # 全部 24 课
    python -u _build/_probe_c_neg_explore.py 20 21      # 只跑这两课
"""
import io
import os
import re
import shutil
import subprocess
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
课目录 = os.path.join(ROOT, "csharp", "cs-lessons", "Lessons")
ANS = os.path.join(ROOT, "cs-answers")

env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"
env["PYTHONIOENCODING"] = "utf-8"

运行 = ["dotnet", "run", "--no-build", "--project", "csharp/cs-lessons", "检查"]

缺的课 = ["00",
          "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19",
          "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30",
          "31", "32", "33", "34", "35", "36", "37", "38", "39", "40"]

_参考 = re.compile(r'public\s+static\s+string\s+参考答案\s*=>\s*"""(.*?)""";', re.S)
_检查名 = re.compile(r'\[检查说明\(\s*"((?:[^"\\]|\\.)*)"\s*\)\]')

#: 运算符互换（保守：只碰这些，别搞出 `<<` / `->` 那种）
换 = {
    "<=": ["<"], ">=": [">"],
    "==": ["!="], "!=": ["=="],
    "&&": ["||"], "||": ["&&"],
    "<": ["<="], ">": [">="],
}
词 = re.compile(r'[A-Za-z_\u4e00-\u9fff][A-Za-z0-9_\u4e00-\u9fff]*'
                r'|<=|>=|==|!=|&&|\|\||\+\+|--|<<|>>'
                r'|[0-9]+|[<>+\-*/%]')


def 取词(源码):
    """[(起, 止, 词)] —— **跳过字符串、字符字面量和注释**里的东西。"""
    出 = []
    i, n = 0, len(源码)
    while i < n:
        c = 源码[i]
        if c == "/" and i + 1 < n and 源码[i + 1] == "/":
            j = 源码.find("\n", i)
            i = n if j < 0 else j
        elif c == "/" and i + 1 < n and 源码[i + 1] == "*":
            j = 源码.find("*/", i + 2)
            i = n if j < 0 else j + 2
        elif c in "\"'":
            引, j = c, i + 1
            while j < n:
                if 源码[j] == "\\":
                    j += 2
                    continue
                if 源码[j] == 引:
                    break
                j += 1
            i = j + 1
        else:
            m = 词.match(源码, i)
            if m:
                出.append((m.start(), m.end(), m.group(0)))
                i = m.end()
            else:
                i += 1
    return 出


def 生成变异(源码):
    """[(描述, 变异后的源码)] —— 每条只改一处。"""
    出, 见过 = [], set()
    for 起, 止, t in 取词(源码):
        候选 = []
        if t.isdigit():
            n = int(t)
            for 新 in (n + 1, n - 1, 0, 1, 2):
                if 新 != n and 新 >= 0:
                    候选.append(str(新))
        elif t in 换:
            # ⚠ C# 里 `=>` 到处都是（表达式体成员），把 `>` 改成 `>=` 只会把
            #   箭头改坏 —— 那种候选一跑就是编译错误，纯噪音。
            if t == ">" and 起 and 源码[起 - 1] == "=":
                continue
            候选 = 换[t]
        # `-` 只在两侧都是空白/数字/标识符时换（避开 -> 和负数）
        elif t == "-":
            前 = 源码[起 - 1] if 起 else " "
            后 = 源码[止] if 止 < len(源码) else " "
            if 前 not in "-<>" and 后 not in "->":
                候选 = ["+"]
        for 新 in 候选:
            变 = 源码[:起] + 新 + 源码[止:]
            if 变 in 见过 or 变 == 源码:
                continue
            见过.add(变)
            行号 = 源码[:起].count("\n") + 1
            原文行 = 源码.splitlines()[行号 - 1].strip()
            新行 = 变.splitlines()[行号 - 1].strip()
            # ⚠ 描述里带上**整行**：正式错例表要用「整行」当锚点，
            #   只写 `5 行：2 ⇒ 3` 的话，回头还得再去翻一遍参考答案。
            出.append(("第%d行：%s  ⇒  %s   ｜ %s" % (行号, t, 新, 原文行),
                       变, 原文行, 新行))
    return 出


def 读课(编):
    匹配 = [f for f in os.listdir(课目录)
            if f.startswith("第%s课" % 编) and f.endswith(".cs")]
    if not 匹配:
        return None
    文 = io.open(os.path.join(课目录, 匹配[0]), encoding="utf-8").read()
    m = _参考.search(文)
    if not m:
        return None
    return {"参考": m.group(1).strip(), "检查名": _检查名.findall(文), "文件": 匹配[0]}


def 判(out):
    过, 叉 = [], []
    for l in out.splitlines():
        s = l.strip()
        if s.startswith("✓ ") or s.startswith("√ "):
            过.append(s[2:].strip())
        elif s.startswith("✗ ") or s.startswith("× "):
            叉.append(s[2:].strip())
    return ("全部通过" in out), 过, 叉


def 答题文件(编):
    """先让课程程序自己建模板，再返回它建出来的那个文件。"""
    文件 = [f for f in os.listdir(ANS) if f.startswith(编)] if os.path.isdir(ANS) else []
    if 文件:
        return os.path.join(ANS, 文件[0])
    os.makedirs(ANS, exist_ok=True)
    subprocess.run([*运行, 编], cwd=ROOT, env=env, capture_output=True, timeout=300)
    文件 = [f for f in os.listdir(ANS) if f.startswith(编)]
    return os.path.join(ANS, 文件[0]) if 文件 else None


def 跑检查(编, 源码, 路):
    """跑一次检查。

    ⚠⚠ 这里踩过一次：某条变异让**学员程序死循环**了。
      `subprocess.run(timeout=...)` 只杀得掉直接子进程（`dotnet run`），
      而真正在转的是它拉起来的孙进程（`%TEMP%\\hybridengine_light_c\\prog.exe`）——
      孙进程攥着 stdout 管道不放 ⇒ `communicate()` **一直阻塞**，
      连 timeout 都等不到尽头（实测卡了 11 分钟，CPU 一点没涨）。
      ⇒ 用 `taskkill /F /T`（连**整棵进程树**）杀，而且单次超时压到 25 秒。
    """
    if 路 is None:
        return ""
    io.open(路, "w", encoding="utf-8", newline="").write(源码)
    p = subprocess.Popen([*运行, 编], cwd=ROOT, env=env,
                         stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    try:
        out, err = p.communicate(timeout=25)
    except subprocess.TimeoutExpired:
        subprocess.run(["taskkill", "/F", "/T", "/PID", str(p.pid)],
                       capture_output=True)
        try:
            out, err = p.communicate(timeout=10)
        except Exception:                                   # noqa: BLE001
            out, err = b"", b""
        return "**这一条变异跑了 25 秒没停（多半是死循环）—— 跳过**"
    return (out + err).decode("utf-8", "replace")


def main():
    选 = [a for a in sys.argv[1:] if not a.startswith("-")] or 缺的课
    print("═" * 70)
    print("  C# 线探索器：%d 课" % len(选))
    print("═" * 70)
    if not os.path.isdir(ANS):
        os.makedirs(ANS, exist_ok=True)
    print("  先编译一次 csharp/cs-lessons …")
    构建 = subprocess.run(["dotnet", "build", "csharp/cs-lessons", "-v", "q", "--nologo"],
                          cwd=ROOT, env=env, capture_output=True, timeout=900)
    if 构建.returncode != 0:
        print("  **编译不过，一条都不跑**")
        print((构建.stdout + 构建.stderr).decode("utf-8", "replace")[-2000:])
        return 2
    print("  编译通过。")
    t0 = time.time()
    没跑到 = []
    for 编 in 选:
        课 = 读课(编)
        if 课 is None:
            print("  !! %s 取不到参考答案" % 编)
            没跑到.append(编)
            continue
        路 = 答题文件(编)
        变异们 = 生成变异(课["参考"])[:14]
        print()
        print("  ── 第 %s 课（%s）候选 %d 条；检查名：%s"
              % (编, 课["文件"], len(变异们), " · ".join(课["检查名"])))
        好 = 0
        for 项 in 变异们:
            描述, 变 = 项[0], 项[1]
            out = 跑检查(编, 变, 路)
            全过, 过, 叉 = 判(out)
            if 全过:
                continue
            if not (过 or 叉):
                print("     [没跑到] %s" % 描述)
                continue
            标 = "★" if (len(叉) == 1 and 过) else " "
            print("     %s [%d√ %d×] %s   被拒于：%s"
                  % (标, len(过), len(叉), 描述, " · ".join(叉[:3])))
            if len(叉) == 1 and 过:
                好 += 1
                if 好 >= 3:
                    break
        if 好 == 0:
            print("     ⚠ 前 %d 条里没有「恰好一条 ×、其余 √」的候选" % len(变异们))
    print()
    print("  用时 %.0f 秒" % (time.time() - t0))
    if 没跑到:
        print("  **没跑到的课**：%s" % " ".join(没跑到))
    print("  ⚠ 收尾**不删** c-answers/：下一课还要用（正式脚本才负责清场）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
