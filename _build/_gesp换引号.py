# 把 35~46 课**正文注释**里的英文双引号换成「」。
#
# ⚠ 只动注释行（`#` 开头），代码一行不碰；一行里引号个数是奇数的话**跳过并报出来**，
#   让人自己看一眼（可能是跨行的引用，机械替换会换错）。
import glob
import io
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
根 = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
课们 = sorted(p for p in glob.glob(os.path.join(根, "lessons", "[0-9][0-9]_*.py"))
             if "35" <= os.path.basename(p)[:2] <= "46")

改 = 0
奇 = []
for p in 课们:
    行 = io.open(p, encoding="utf-8").read().split("\n")
    动 = False
    for i, l in enumerate(行):
        if not l.lstrip().startswith("#") or '"' not in l:
            continue
        if l.count('"') % 2:
            奇.append("%s:%d  %s" % (os.path.basename(p), i + 1, l.strip()[:70]))
            continue
        开 = True
        新 = []
        for ch in l:
            if ch == '"':
                新.append("「" if 开 else "」")
                开 = not 开
            else:
                新.append(ch)
        行[i] = "".join(新)
        改 += 1
        动 = True
    if 动:
        io.open(p, "w", encoding="utf-8").write("\n".join(行))

print("  改过的注释行：%d" % 改)
if 奇:
    print("  ⚠ 引号个数是奇数的行（没动，要手工看）：")
    for s in 奇:
        print("     " + s)
sys.exit(1 if 奇 else 0)
