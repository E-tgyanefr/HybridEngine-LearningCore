# GESP 新课（35~46）体检：形状对不对（行数、CHECK 数、正文引号、分条要求、标题）。
#
#   python _build/_gesp体检.py
import glob
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
根 = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
课们 = sorted(p for p in glob.glob(os.path.join(根, "lessons", "[0-9][0-9]_*.py"))
             if "35" <= os.path.basename(p)[:2] <= "46")

问题 = 0
for p in 课们:
    名 = os.path.basename(p)
    行 = io.open(p, encoding="utf-8").read().splitlines()
    正文 = [l for l in 行 if l.lstrip().startswith("#")]
    源码 = "\n".join(行)
    检查们 = re.findall(r"^def (CHECK_\w+)", 源码, re.M)
    错 = []
    if not (110 <= len(行) <= 180):
        错.append("行数 %d 不在 110~180" % len(行))
    if not (3 <= len(检查们) <= 5):
        错.append("CHECK_ 有 %d 个（要 3~5）" % len(检查们))
    if not 行[0].startswith("# 第"):
        错.append("第一行不是「# 第 …」")
    if not any("轮到你了" in l for l in 正文):
        错.append("没有「轮到你了」")
    if not re.search(r"^#\s+\d\.", 源码, re.M):
        错.append("要求没有用 `# 1.` 分条")
    for l in 正文:
        if '"' in l:
            错.append("正文里有英文双引号：%s" % l.strip()[:40])
            break
    if not all(s in 源码 for s in ("这一课你会学到", "需要先会", "怎么读这一课",
                                  "这一课只讲这一件事", "怎么自己测")):
        错.append("少了骨架里的某个小节")
    问题 += 1 if 错 else 0
    print("  %s %s  %d 行  CHECK %d 个  %s" % ("OK " if not 错 else "XX ", 名, len(行),
                                              len(检查们), "；".join(错) if 错 else ""))
print()
print("  == %s ==" % ("12 课形状全对" if not 问题 else "%d 课有问题" % 问题))
sys.exit(1 if 问题 else 0)
