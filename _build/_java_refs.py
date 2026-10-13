# -*- coding: utf-8 -*-
"""临时：把 41 个 Java 参考答案**去掉注释行**后打出来，供人读。
只读课程文件。用完即删。"""
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
D = os.path.join(ROOT, "csharp", "java-lessons", "Lessons")
参考 = re.compile(r'public\s+static\s+string\s+参考答案\s*=>\s*"""(.*?)""";', re.S)
课号 = re.compile(r'\[Java课程\(\s*"(\d\d)"\s*,')
出 = []
for f in sorted(os.listdir(D)):
    if not f.endswith(".cs"):
        continue
    文 = io.open(os.path.join(D, f), encoding="utf-8").read()
    m = 课号.search(文)
    if not m:
        continue
    r = 参考.search(文)
    码 = r.group(1).strip() if r else ""
    # 去掉整行注释
    净 = "\n".join(l for l in 码.split("\n") if not l.strip().startswith("//"))
    # 去掉连续空行
    净 = re.sub(r"\n{2,}", "\n", 净)
    出.append((m.group(1), f, 净, len(净)))
出.sort()
行 = ["共 %d 课" % len(出)]
for 编, f, 净, n in 出:
    行.append("")
    行.append("─" * 66)
    行.append("### %s   (%d 字符，去注释后)" % (编, n))
    行.append("─" * 66)
    行.append(净)
io.open(os.path.join(ROOT, "_build", "_java_refs.txt"),
        "w", encoding="utf-8", newline="\n").write("\n".join(行) + "\n")
for 编, f, 净, n in 出:
    print("  %s  %5d 字符" % (编, n))
