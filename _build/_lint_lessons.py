"""新课自检：行数、注释里的英文双引号、轮到你了的编号格式。

用法：  python _build/_lint_lessons.py 18 19 20 21 22 23 24
"""
import glob
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

课们 = sys.argv[1:] or ["18", "19", "20", "21", "22", "23", "24"]
坏 = 0
for 编 in 课们:
    命中 = glob.glob(os.path.join(LC, "lessons", 编 + "_*.py"))
    if not 命中:
        print("  XX %s: 找不到课文件" % 编)
        坏 += 1
        continue
    p = 命中[0]
    文 = io.open(p, encoding="utf-8").read()
    L = 文.splitlines()
    行数 = len(L)
    # ⚠ 只有**开头那段注释**会被搬进答题模板（见 run._stub_for）：
    #   它收集开头的连续 `#` 行，碰到第一行代码就停。
    注释段行数 = 0
    for l in L:
        if l.startswith("#"):
            注释段行数 += 1
        elif not l.strip() and 注释段行数:
            continue
        else:
            break
    问题 = []
    if not (100 <= 行数 <= 160):
        问题.append("行数 %d 不在 100~160" % 行数)
    if not L[0].startswith("# 第"):
        问题.append("第一行不是标题")
    if not re.search(r"^# \d\.", 文, re.M):
        问题.append("没有 `# 1.` 形态的条目")
    引号 = [(i, l) for i, l in enumerate(L, 1)
            if l.startswith("#") and '"' in l]
    if 引号:
        问题.append("注释里有英文双引号 %d 处" % len(引号))
    if '"""' in 文.split("\ndef ", 1)[0] or "'''" in 文.split("\ndef ", 1)[0]:
        问题.append("注释段里有三引号（会炸掉答题模板）")
    检查 = re.findall(r"^def (CHECK_\w+)", 文, re.M)
    if not (3 <= len(检查) <= 5):
        问题.append("CHECK_ 函数 %d 个（要 3~5）" % len(检查))
    print("  %s %s  %3d 行（注释 %d + 代码 %d）  检查 %d 个  %s"
          % ("OK " if not 问题 else "XX ", os.path.basename(p), 行数, 注释段行数,
             行数 - 注释段行数, len(检查), "；".join(问题)))
    if 引号:
        for i, l in 引号[:8]:
            print("        注释第 %d 行: %s" % (i, l[:78]))
    for n in 检查:
        print("        · " + n)
    if 问题:
        坏 += 1
print()
print("  == %d/%d 课通过自检 ==" % (len(课们) - 坏, len(课们)))
sys.exit(0 if 坏 == 0 else 1)
