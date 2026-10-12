# -*- coding: utf-8 -*-
"""逐课验证我加的 31~40 课（对 c-answers 竞态免疫）。

⚠ 为什么不直接用 `_verify_compiled.py c` 一把跑：
  它和**另一个作者**同时跑的同一个脚本共用 `c-answers\\` 目录，
  而两边都会 rmtree(答案目录) 清场（实测：40 秒内目录被清空 11 次）。
  于是「刚写好答案 → 别人把它删了 → 检查跑在空模板上」这种时序时不时发生，
  症状是某几课报 ✓0 ✗0（一节检查都没跑）。

⇒ 这里一课一课来，**只要这一课没真的跑出结果就重试**（最多 6 次）。
  重试不改答案内容 —— 只保证检查真的执行到了。
"""
import io
import os
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
BASE = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
ANS = os.path.join(BASE, "c-answers")
env = dict(os.environ, PYTHONIOENCODING="utf-8", DOTNET_CLI_UI_LANGUAGE="en")

课程 = ["第31课_栈与队列.cs", "第32课_链表.cs", "第33课_树与遍历.cs",
        "第34课_DFS与BFS.cs", "第35课_一维DP.cs", "第36课_数学库.cs",
        "第37课_二维DP.cs", "第38课_图与遍历.cs", "第39课_排列组合.cs",
        "第40课_最短路.cs"]


def 跑检查(号):
    r = subprocess.run(["dotnet", "run", "--project", "csharp/c-lessons", "检查", 号],
                       cwd=BASE, env=env, capture_output=True, timeout=600)
    return (r.stdout + r.stderr).decode("utf-8", "replace")


全过 = True
for 文件 in 课程:
    号 = re.search(r"\d+", 文件).group(0)
    源 = os.path.join(BASE, "csharp", "c-lessons", "Lessons", 文件)
    t = io.open(源, encoding="utf-8").read()
    答案 = re.search(r'public static string 参考答案 => """(.*?)""";', t, re.S).group(1).strip()

    成 = False
    for 试 in range(1, 7):
        跑检查(号)                                     # 先让课程程序建出答题文件
        文件们 = [x for x in os.listdir(ANS) if x.startswith(号)] if os.path.isdir(ANS) else []
        if not 文件们:
            continue
        路径 = os.path.join(ANS, 文件们[0])
        io.open(路径, "w", encoding="utf-8", newline="").write(答案 + "\n")
        out = 跑检查(号)
        if "全部通过" in out:
            print("%s  OK   ✓%d ✗%d   （第 %d 次尝试）"
                  % (号, out.count("✓"), out.count("✗"), 试))
            成 = True
            break
    if not 成:
        print("%s  XX   6 次都没跑出结果" % 号)
        全过 = False

print("---- 31~40 %s ----" % ("全部通过" if 全过 else "有问题"))
sys.exit(0 if 全过 else 1)
