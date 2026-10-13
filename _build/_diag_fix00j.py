# -*- coding: utf-8 -*-
"""临时诊断：`_fix00j.py` 到底哪一行坏 + Java 各课的「常见坑」覆盖（只读）。"""
import io
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

P = os.path.join(ROOT, "_build", "_fix00j.py")
L = io.open(P, encoding="utf-8").read().split("\n")
print("=== _fix00j.py 第 20-26 行的 repr ===")
for i in range(19, 26):
    print("  %2d| %s" % (i + 1, repr(L[i])))

print()
print("=== Java 41 课的「常见坑」覆盖 ===")
D = os.path.join(ROOT, "csharp", "java-lessons", "Lessons")
缺 = []
有 = 0
for f in sorted(os.listdir(D)):
    if not f.endswith(".cs"):
        continue
    t = io.open(os.path.join(D, f), encoding="utf-8").read()
    if "常见坑" in t:
        有 += 1
    else:
        缺.append(f[:3])
print("  有：%d 课   没有：%d 课 %s" % (有, len(缺), " ".join(缺)))

print()
print("=== 对照：C / C++ / C# 三条线的「常见坑」 ===")
for 线 in ("c-lessons", "cpp-lessons", "cs-lessons"):
    D2 = os.path.join(ROOT, "csharp", 线, "Lessons")
    n = 0
    tot = 0
    for f in sorted(os.listdir(D2)):
        if not f.endswith(".cs"):
            continue
        tot += 1
        if "常见坑" in io.open(os.path.join(D2, f), encoding="utf-8").read():
            n += 1
    print("  %-12s %d/%d" % (线, n, tot))
