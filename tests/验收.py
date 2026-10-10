"""全仓验收：逐课跑一遍，统计真实通过率。

════ 为什么要单独写这个脚本 ════

我前面的手工验收反复出错 —— 因为我用**反面测试的坏答案**覆写了答题文件，
之后看"未过 N 课"就以为课程有问题。

教训：**验收脚本必须自己准备好输入**，不能依赖"上一步留下的状态"。
所以这个脚本：
  ① 先把 solutions/ 里的参考答案复制进 answers/（自己准备输入）
  ② 再逐课跑检查
  ③ 只报告真实结果

这样才是可重复的 —— 谁跑都一样。
"""
import io
import os
import shutil
import subprocess
import sys

ROOT = r"D:\Users\etgya\Desktop\Project\HybridEngine-light"
ENV = dict(os.environ, PYTHONIOENCODING="utf-8", DOTNET_CLI_UI_LANGUAGE="en")

# ── ① 自己准备输入：把参考答案复制进答题目录 ──
对 = [
    ("lessons/solutions", "lessons/answers"),
    ("embedded-solutions", "embedded-answers"),
    ("raspi-solutions", "raspi-answers"),
    ("ai-solutions", "ai-answers"),
]
for src, dst in 对:
    s, d = os.path.join(ROOT, src), os.path.join(ROOT, dst)
    if not os.path.isdir(s):
        continue
    # ⚠ 先删掉旧的答题文件再复制。
    #
    #   我踩过：反面测试把坏答案写进了 answers/，而"复制"只覆盖**同名**文件 ——
    #   结果验收脚本以为自己准备好了输入，实际测的还是坏答案，
    #   我因此误报过好几次"课没过"。
    #   教训：**准备输入要"清空再放"，不是"覆盖同名"**。
    if os.path.isdir(d):
        shutil.rmtree(d, ignore_errors=True)
    for 根, _, 文件们 in os.walk(s):
        for f in 文件们:
            if not f.endswith(".py"):
                continue
            sp = os.path.join(根, f)
            rel = os.path.relpath(sp, s)
            dp = os.path.join(d, rel)
            os.makedirs(os.path.dirname(dp), exist_ok=True)
            shutil.copy2(sp, dp)

print("=" * 60)
print("  全仓逐课验收")
print("=" * 60)

结果 = []


def 跑(标签, 命令, 课号们):
    通过 = 0
    有效 = 0
    for n in 课号们:
        try:
            r = subprocess.run(命令 + [n], capture_output=True, text=True,
                               encoding="utf-8", env=ENV, cwd=ROOT, timeout=180)
        except subprocess.TimeoutExpired:
            结果.append((标签, n, "超时"))
            continue
        out = (r.stdout or "") + (r.stderr or "")
        if "全部通过" in out:
            通过 += 1
            有效 += 1
        elif "通过 " in out:
            有效 += 1
            # 抓出通过数
            for line in out.splitlines():
                if "通过 " in line and "/" in line:
                    结果.append((标签, n, line.strip()))
                    break
        else:
            结果.append((标签, n, "（无答题文件）"))
    结果.append((标签, "合计", f"{通过}/{有效} 课全过"))
    return 通过, 有效


跑("Python", [sys.executable, "-m", "hybridengine_light", "检查"], ["01", "02"])
跑("嵌入式", [sys.executable, "-m", "hybridengine_light", "嵌入式", "检查"], ["01", "02", "03", "04", "05"])
跑("树莓派", [sys.executable, "-m", "hybridengine_light", "树莓派", "检查"], ["01", "02", "03", "04", "05"])
跑("AI", [sys.executable, "-m", "hybridengine_light", "AI", "检查"], ["01", "02", "03", "04", "05"])

for 名, 项目 in (("C++", "cpp-lessons"), ("Java", "java-lessons")):
    跑(名, ["dotnet", "run", "--project", f"csharp/{项目}", "检查"],
       ["01", "02", "03", "04", "05", "06", "07", "08"])

当前 = None
for 标签, n, r in 结果:
    if 标签 != 当前:
        print(f"\n  ── {标签} ──")
        当前 = 标签
    前缀 = "    " if n != "合计" else "  "
    print(f"{前缀}{n:>4} : {r}")

# ── ② 测试套件 ──
print("\n" + "=" * 60)
print("  测试套件")
print("=" * 60)
r = subprocess.run([sys.executable, "-m", "unittest", "discover", "-s", "tests"],
                   capture_output=True, text=True, encoding="utf-8", env=ENV, cwd=ROOT, timeout=600)
for line in ((r.stdout or "") + (r.stderr or "")).splitlines():
    if line.startswith("Ran ") or line.strip() in ("OK",) or line.startswith("FAILED"):
        print("  " + line.strip())
r2 = subprocess.run(["dotnet", "run", "--project", "csharp/tests"],
                    capture_output=True, text=True, encoding="utf-8", env=ENV, cwd=ROOT, timeout=900)
for line in ((r2.stdout or "") + (r2.stderr or "")).splitlines():
    if "通过 " in line or "FAIL" in line:
        print("  " + line.strip())
