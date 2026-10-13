# 用「参考答案」跑全部引擎课程的检查。
#
# ## 为什么必须这么做（上一轮的教训）
#   课程里的检查是我写的、参考答案也是我写的。
#   **两边都错时检查会"通过"，而课程是错的。**
#   上一轮加 C 课时，就是这么抓出 3 个课程自己的 bug
#   （传了带分号的语句、"写了函数"里硬编码 std::cout、期望值算错）。
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

ROOT = _仓库根()
print("  仓库根：%s" % ROOT)
ANS = os.path.join(ROOT, "engine-answers")

env = dict(os.environ)
env["PYTHONIOENCODING"] = "utf-8"

# ① 把参考答案复制到答题位置
shutil.rmtree(ANS, ignore_errors=True)
os.makedirs(ANS, exist_ok=True)
sol = os.path.join(ROOT, "engine-solutions")
编号们 = []
for f in sorted(os.listdir(sol)):
    if f.endswith(".py"):
        shutil.copyfile(os.path.join(sol, f), os.path.join(ANS, f))
        编号们.append(f[:2])
print("  已放入参考答案：", " ".join(编号们))

# ② 逐课跑检查
好 = 0
明细 = []
for 编 in 编号们:
    r = subprocess.run([sys.executable, "-m", "hybridengine_light", "引擎课程", "检查", 编],
                       cwd=ROOT, env=env, capture_output=True, timeout=600)
    out = (r.stdout + r.stderr).decode("utf-8", "replace")
    过 = "全部通过" in out
    勾 = out.count("√") + out.count("✓")
    叉 = out.count("✗")
    明细.append((编, 过, 勾, 叉))
    print(f"  {'OK ' if 过 else 'XX '} {编}  ✓{勾} ✗{叉}")
    if 过:
        好 += 1
    else:
        for line in out.splitlines():
            s = line.strip()
            if s.startswith("✗") or "应当" in s or "没有" in s or "错" in s:
                print("        " + s[:104])

print()
print(f"  == {好}/{len(编号们)} 课用参考答案跑通 ==")
shutil.rmtree(ANS, ignore_errors=True)
print(f"  engine-answers 已清理: {not os.path.exists(ANS)}")
sys.exit(0 if 好 == len(编号们) and 编号们 else 1)
