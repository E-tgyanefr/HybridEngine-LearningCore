# -*- coding: utf-8 -*-
"""临时：把 41 个 Java 课程的「检查名 + 参考答案」摘出来，给人（我）读。
只读课程文件，不改任何东西。用完即删。"""
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
D = os.path.join(ROOT, "csharp", "java-lessons", "Lessons")

课号 = re.compile(r'\[Java课程\(\s*"(\d\d)"\s*,\s*"((?:[^"\\]|\\.)*)"')
检查 = re.compile(r'\[Java检查说明\(\s*"((?:[^"\\]|\\.)*)"\s*\)\]')
参考 = re.compile(r'public\s+static\s+string\s+参考答案\s*=>\s*"""(.*?)""";', re.S)

出 = []
for f in sorted(os.listdir(D)):
    if not f.endswith(".cs"):
        continue
    文 = io.open(os.path.join(D, f), encoding="utf-8").read()
    m = 课号.search(文)
    if not m:
        continue
    参考m = 参考.search(文)
    出.append({
        "编": m.group(1),
        "题": m.group(2),
        "文件": f,
        "检查": 检查.findall(文),
        "参考": (参考m.group(1).strip() if 参考m else "(没有参考答案)"),
    })

出.sort(key=lambda x: x["编"])
print("共 %d 课" % len(出))
for x in 出:
    print()
    print("=" * 70)
    print("### %s  %s   [%s]" % (x["编"], x["题"], x["文件"]))
    print("检查(%d)：" % len(x["检查"]))
    for i, c in enumerate(x["检查"]):
        print("   [%d] %s" % (i, c))
    print("--- 参考答案 ---")
    print(x["参考"])
