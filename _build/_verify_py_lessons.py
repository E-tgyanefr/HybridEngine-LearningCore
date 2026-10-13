# 用「参考答案」跑全部 Python 基础课的检查。
#
# ## 为什么必须这么做（第四次了，教训一致）
#   课程里的检查是我写的、参考答案也是我写的。
#   **两边都错时检查会"通过"，而课程是错的。**
#   · 加 C 课时抓到 3 个 · 加引擎课程抓到 1 个
#   · 加 C# 课抓到 12 个（一路撞到能跑）
#   ⇒ 这一步不是走形式。
#
# ## 两种形态都要支持
#   · 单文件课：`lessons/answers/<名>.py`
#   · 多文件课（02_模块化）：`lessons/answers/<名>/` 整个目录
import io
import os
import shutil
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
# ⚠ 路径**从 `__file__` 推**，不写死绝对路径（原来写死 `…\引擎\LearnCode`，
#   而仓库后来搬到了 `…\引擎\HybridEngine-LearningCore` ⇒ 那是个空壳目录）。
def _仓库根():
    """从本文件的所在目录往上找仓库根（含 `lessons/` 的那一级）。"""
    d = os.path.dirname(os.path.abspath(__file__))          # <仓库>\_build
    for _ in range(3):
        if os.path.isdir(os.path.join(d, "lessons")):
            return d
        d = os.path.dirname(d)
    return os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 兜底：不变

LC = _仓库根()
print("  仓库根：%s" % LC)
lessons = os.path.join(LC, "lessons")
sol = os.path.join(lessons, "solutions")
ans = os.path.join(lessons, "answers")

env = dict(os.environ)
env["PYTHONIOENCODING"] = "utf-8"

课们 = sorted(f for f in os.listdir(lessons)
              if f.endswith(".py") and len(f) > 2 and f[:2].isdigit())
if not 课们:
    print("  XX lessons/ 里没有课程文件")
    sys.exit(1)
print("  课程：", " ".join(f[:2] for f in 课们))

# 把参考答案放进答题位置
shutil.rmtree(ans, ignore_errors=True)
os.makedirs(ans, exist_ok=True)
放好 = 0
for f in 课们:
    名 = f[:-3]
    # 多文件形态？
    源目录 = os.path.join(sol, 名)
    源文件 = os.path.join(sol, f)
    if os.path.isdir(源目录):
        shutil.copytree(源目录, os.path.join(ans, 名))
        放好 += 1
    elif os.path.isfile(源文件):
        shutil.copyfile(源文件, os.path.join(ans, f))
        放好 += 1
    else:
        print("  ! %s: 找不到参考答案" % f)
print("  已放入参考答案：%d 个" % 放好)

好 = 0
for f in 课们:
    编 = f[:2]
    r = subprocess.run([sys.executable, "-m", "hybridengine_light", "检查", 编],
                       cwd=LC, env=env, capture_output=True, timeout=300)
    out = (r.stdout + r.stderr).decode("utf-8", "replace")
    过 = "全部通过" in out
    勾 = out.count("√") + out.count("✓")
    叉 = out.count("✗") + out.count("×")
    好 += 1 if 过 else 0
    print("  %s %s  ✓%-3d ✗%-3d %s" % ("OK " if 过 else "XX ", 编, 勾, 叉, f[3:-3]))
    if not 过:
        for line in out.splitlines():
            s = line.strip()
            if s.startswith(("✗", "×")) or "应当" in s or "没有" in s:
                print("        " + s[:104])

print()
print("  == %d/%d 课用参考答案跑通 ==" % (好, len(课们)))
shutil.rmtree(ans, ignore_errors=True)
print("  lessons/answers 已清理:", not os.path.exists(ans))
sys.exit(0 if 好 == len(课们) else 1)
