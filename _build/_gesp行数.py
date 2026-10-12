# GESP 新课（35~46）行数体检：硬要求 110~180 行。
#
#   python _build/_gesp行数.py            # 汇总
#   python _build/_gesp行数.py 44         # 细看某一课（注释行 / 代码行 / 每个 def 多长）
import glob
import io
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
根 = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
细看 = sys.argv[1:] if len(sys.argv) > 1 else []

课们 = sorted(glob.glob(os.path.join(根, "lessons", "[0-9][0-9]_*.py")))
差 = 0
for f in 课们:
    名 = os.path.basename(f)
    if 名[:2] < "35" or (细看 and 名[:2] not in 细看):
        continue
    行 = io.open(f, encoding="utf-8").read().splitlines()
    n = len(行)
    注 = sum(1 for l in 行 if l.lstrip().startswith("#"))
    好 = 110 <= n <= 180
    差 += 0 if 好 else 1
    print("  %s %s  %d 行（注释 %d / 代码 %d）" % ("OK " if 好 else "XX ", 名, n, 注, n - 注))
    if 细看 and 名[:2] in 细看:
        位置 = [(i, l) for i, l in enumerate(行) if l.startswith("def ")]
        for k, (i, l) in enumerate(位置):
            末 = 位置[k + 1][0] if k + 1 < len(位置) else n
            print("        %-30s 行 %d~%d  共 %d" % (l[:30], i + 1, 末, 末 - i))
        # 正文（注释）里最长的几段：找出连续注释块
        块 = []
        起 = None
        for i, l in enumerate(行):
            if l.lstrip().startswith("#"):
                if 起 is None:
                    起 = i
            else:
                if 起 is not None:
                    块.append((起, i))
                    起 = None
        if 起 is not None:
            块.append((起, n))
        for a, b in sorted(块, key=lambda x: x[0] - x[1]):
            if b - a >= 12:
                print("        注释块 行 %d~%d 共 %d：%s" % (a + 1, b, b - a, 行[a][:46]))
print()
print("  == %s ==" % ("全部在 110~180 行内" if not 差 else "%d 课超范围" % 差))
sys.exit(1 if 差 else 0)
