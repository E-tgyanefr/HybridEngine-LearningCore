# 逐条计：M.发现(键) 各花多少时间，慢在哪一步。
import os
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
sys.path.insert(0, LC)
os.chdir(LC)

from hybridengine_light import learnmodel as M                  # noqa: E402

for 键 in M.编译型轨道:
    t0 = time.time()
    课 = M.发现(键)
    print("  %-8s %2d 课  %.2f 秒" % (键, len(课), time.time() - t0))

print()
print("  === 编译型轨道的 工程名/答案名（c 是不是配错了）===")
for 名 in ("_发现编译型", "工程名", "答案名"):
    print("  " + 名 + ":")
import io
import re
t = io.open(os.path.join(LC, "hybridengine_light", "learnmodel.py"), encoding="utf-8").read()
m = re.search(r"def _发现编译型.*?(?=\ndef |\nclass )", t, re.S)
if m:
    for l in m.group(0).split("\n")[:46]:
        print("    " + l.rstrip()[:104])
