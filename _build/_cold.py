# 冷启动实测：从进程开始到四条轨道全部发现完，用户要等多久。
import os
import subprocess
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）
代码 = r'''
import os, sys, time
sys.path.insert(0, r"{lc}")
os.chdir(r"{lc}")
sys.stdout.reconfigure(encoding="utf-8", errors="replace")
from hybridengine_light import learnmodel as M
t0 = time.time()
for k in M.编译型轨道:
    a = time.time()
    n = len(M.发现(k))
    print("  %-8s %2d 课  单条 %.2f 秒" % (k, n, time.time() - a))
print("  ---- 全部发现完：%.2f 秒 ----" % (time.time() - t0))
'''.format(lc=LC)

open(os.path.join(os.environ["TEMP"], "_cold.py"), "w", encoding="utf-8").write(代码)
for i in (1, 2):
    print("  === 第 %d 次（冷启动）===" % i)
    r = subprocess.run([sys.executable, os.path.join(os.environ["TEMP"], "_cold.py")],
                       capture_output=True, timeout=300)
    print((r.stdout + r.stderr).decode("utf-8", "replace").rstrip())
    print()
