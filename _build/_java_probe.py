# -*- coding: utf-8 -*-
"""临时：把几课的参考答案里我要改的那几行按 repr 打出来，好把「旧」写准。
只读课程文件。用完即删。"""
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
D = os.path.join(ROOT, "csharp", "java-lessons", "Lessons")
_课 = re.compile(r'\[Java课程\(\s*"(\d\d)"\s*,')
_参考 = re.compile(r'public\s+static\s+string\s+参考答案\s*=>\s*"""(.*?)""";', re.S)

想看 = {
    "15": ["finally", "这行总会执行"],
    "16": ['get("小刚")'],
    "20": ["循环求和", "和 = 和 + i"],
    "25": ["是质数[0]", "是质数[1]"],
    "39": ["图.get(乙)"],
}
出 = []
for f in sorted(os.listdir(D)):
    if not f.endswith(".cs"):
        continue
    文 = io.open(os.path.join(D, f), encoding="utf-8").read()
    m = _课.search(文)
    if not m or m.group(1) not in 想看:
        continue
    r = _参考.search(文)
    参考 = r.group(1).strip()
    出.append("=" * 60)
    出.append("### %s   %s" % (m.group(1), f))
    for 键 in 想看[m.group(1)]:
        出.append("  --- 含 %r 的行 ---" % 键)
        for i, line in enumerate(参考.split("\n")):
            if 键 in line:
                出.append("   %3d %r" % (i, line))
    # 顺便数一数
    for 键 in 想看[m.group(1)]:
        出.append("  出现次数 %r = %d" % (键, 参考.count(键)))

io.open(os.path.join(ROOT, "_build", "_java_probe.txt"),
        "w", encoding="utf-8", newline="\n").write("\n".join(出) + "\n")
print("写出 %d 行" % len(出))
