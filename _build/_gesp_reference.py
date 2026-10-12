# 用**参考答案**跑 35~46 的检查（只动自己这几个课号的答题文件，不碰别人的）。
#
#   python _build/_gesp_reference.py
#
# ## 为什么必须这么做（和第 17 课那次同一个教训）
#   课程里的检查是我写的、参考答案也是我写的。
#   **两边都错时检查会"通过"，而课程是错的。**
#   ⇒ 这一轮专门用来撞出"检查写错了"（实测撞到过：分解质因数(0) 被判错、
#     「选中的区间」里 list.remove 抛 ValueError、归并排序的期望值写成了 [1,2]*500）。
import glob
import os
import shutil
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
S = os.path.join(LC, "lessons")
ANS = os.path.join(S, "answers")
env = dict(os.environ, PYTHONIOENCODING="utf-8")

课们 = [os.path.basename(p) for p in sorted(glob.glob(os.path.join(S, "[0-9][0-9]_*.py")))
        if "35" <= os.path.basename(p)[:2] <= "46"]
print("  课程：", " ".join(f[:2] for f in 课们))

缺 = [f for f in 课们 if not os.path.isfile(os.path.join(S, "solutions", f))]
if 缺:
    print("  ** 缺参考答案：", 缺)

好 = 0
for f in 课们:
    编 = f[:2]
    解 = os.path.join(S, "solutions", f)
    if not os.path.isfile(解):
        continue
    os.makedirs(ANS, exist_ok=True)
    shutil.copyfile(解, os.path.join(ANS, f))
    r = subprocess.run([sys.executable, "-m", "hybridengine_light", "检查", 编],
                       cwd=LC, env=env, capture_output=True, timeout=600)
    out = (r.stdout + r.stderr).decode("utf-8", "replace")
    过 = "全部通过" in out
    好 += 1 if 过 else 0
    print("  %s %s  √%-3d ×%-3d %s" % ("OK " if 过 else "XX ", 编,
                                       out.count("√"), out.count("×"), f[3:-3]))
    if not 过:
        for l in out.splitlines():
            s = l.strip()
            if s.startswith(("×", "✗")) or "应当" in s or "没有定义" in s:
                print("        " + s[:100])

print()
print("  == %d/%d 课用参考答案全过 ==" % (好, len(课们)))
sys.exit(0 if 好 == len(课们) else 1)
