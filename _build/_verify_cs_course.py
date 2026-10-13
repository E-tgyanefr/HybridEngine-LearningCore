# 用「参考答案」跑全部 C# 课程的检查。
#
# ## 为什么必须这么做（第三次了，教训一致）
#   课程里的检查是我写的、参考答案也是我写的。
#   **两边都错时检查会"通过"，而课程是错的。**
#
#   · 加 C 课时：抓到 3 个课程自己的 bug
#   · 加引擎课程时：抓到 1 个引擎 bug（参数名遮住类名）
#   ⇒ 这一步不是走形式。
import io
import os
import re
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
ANS = os.path.join(ROOT, "cs-answers")
SOL = os.path.join(ROOT, "cs-solutions")
LESSONS = os.path.join(ROOT, "csharp", "cs-lessons", "Lessons")

env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"
env["PYTHONIOENCODING"] = "utf-8"

# ① 问 C# 侧要真实的答题文件路径（不自己拼 —— 文件名含标题清理规则）
def 真实路径(编号: str) -> str | None:
    r = subprocess.run(["dotnet", "run", "--project", "csharp/cs-lessons",
                        "--no-build", "检查", 编号],
                       cwd=ROOT, env=env, capture_output=True, timeout=300)
    t = (r.stdout + r.stderr).decode("utf-8", "replace")
    m = re.search(r"给你建好了答题文件：(.+?)\s*$", t, re.M)
    return m.group(1).strip() if m else None


if not os.path.isdir(SOL):
    print("  XX cs-solutions/ 不存在")
    sys.exit(1)

# ⚠ 文件名是 `第01课_输出.cs` —— 编号是**中间那两位数字**，
#   不是 `f[:2]`（那是"第0"）。我第一版就错在这里。
编号们 = sorted(re.search(r"第(\d\d)课", f).group(1)
                for f in os.listdir(SOL) if f.endswith(".cs"))
print("  参考答案：", " ".join(编号们))

# ⚠ 先让课程程序建出答题文件（拿到真实文件名），再覆盖成参考答案
shutil.rmtree(ANS, ignore_errors=True)
真实名 = {}
for 编 in 编号们:
    路径 = 真实路径(编)
    if not 路径:
        print(f"  XX {编}: 问不到答题文件路径")
        continue
    真实名[编] = 路径
    解 = [f for f in os.listdir(SOL) if re.search(r"第" + 编 + r"课", f)]
    if 解:
        shutil.copyfile(os.path.join(SOL, 解[0]), 路径)
    else:
        print(f"  ! {编}: 找不到对应的参考答案文件")
print("  已放入参考答案：", len(真实名), "个")

# ② 逐课跑检查
好 = 0
for 编 in 编号们:
    if 编 not in 真实名:
        continue
    r = subprocess.run([sys.executable, "-m", "hybridengine_light", "引擎课程", "检查", "00"],
                       capture_output=True)  # 无害调用，保持导入
    r = subprocess.run(["dotnet", "run", "--project", "csharp/cs-lessons", "--no-build", "检查", 编],
                       cwd=ROOT, env=env, capture_output=True, timeout=600)
    out = (r.stdout + r.stderr).decode("utf-8", "replace")
    过 = "全部通过" in out
    勾 = out.count("✓")
    叉 = out.count("✗")
    if 过:
        好 += 1
    print(f"  {'OK ' if 过 else 'XX '} {编}  ✓{勾} ✗{叉}")
    if not 过:
        for line in out.splitlines():
            s = line.strip()
            if s.startswith("✗") or "应当" in s or "没找到" in s or "错" in s:
                print("        " + s[:108])

print()
print(f"  == {好}/{len(编号们)} 课用参考答案跑通 ==")
shutil.rmtree(ANS, ignore_errors=True)
print(f"  cs-answers 已清理: {not os.path.exists(ANS)}")
sys.exit(0 if 好 == len(编号们) and 编号们 else 1)
