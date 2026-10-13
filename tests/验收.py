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

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）
#: ⚠ 同 `_build/run_tests.py`：子进程的 cwd 就是**产品根**，别把 `__pycache__`
#:   就地写进去（发行物里会因为"跑过一次测试"而多出上百个 `.pyc`）。
#:   ⚠ 这一条**挡不住答题模板** —— 本脚本自己就会清并重建
#:   `lessons/answers` 等答题区（那是它的活）。发行物要干净得靠生成器收尾。
ENV = dict(os.environ, PYTHONIOENCODING="utf-8", DOTNET_CLI_UI_LANGUAGE="en",
           PYTHONDONTWRITEBYTECODE="1")

# ── ① 自己准备输入：把参考答案复制进答题目录 ──
对 = [
    ("lessons/solutions", "lessons/answers"),
    ("embedded-solutions", "embedded-answers"),
    ("raspi-solutions", "raspi-answers"),
    ("ai-solutions", "ai-answers"),
]

# ⚠⚠ 跑完必须**恢复原状** —— 这是 2026-10-13 实测踩出来的
#
#   本脚本会把 `solutions/` 复制进 `*-answers/`。而 `*-answers/` 是**运行期答题区**：
#   `learnmodel.读答案()` 只要看到文件就**直接返回它**，不再生成模板。
#
#   ⇒ 跑完不清场的话，紧接着跑 `_build/run_tests.py` 会**挂**：
#
#         FAIL: test_每条线都能生成非空的答题模板
#               python：模板只有 9 行非空（像没生成）
#
#     （第 00 课的**参考答案**只有 9 行非空 —— 它是答案，不是模板。
#       把答案放进了答题区，测试读到的自然是答案。）
#
#   ⇒ 症状看着像"模板生成坏了"，其实是**上一条命令留下的状态**。
#     这是本文件开头那条教训的**另一面**：验收脚本要自己准备输入，
#     **也要自己收拾干净** —— 不然"我准备输入"就变成了"污染下一个人"。
#
#   ⚠ 顺序记一笔：跑 `run_tests.py` **之前**先跑本脚本，只有在
#     本脚本会恢复原状的前提下才安全。
#   ⚠ 旧的是 `shutil.move` 搬走而不是删：学员真写过的东西，脚本没权力替他决定。
备份根 = os.path.join(__import__("tempfile").gettempdir(), "lc_yanshou_答题区备份")
if os.path.isdir(备份根):
    shutil.rmtree(备份根, ignore_errors=True)
os.makedirs(备份根, exist_ok=True)
还原 = []          # [(答题目录, 备份目录 or None)]

#  ⚠ 用 `atexit` 而不是只在末尾调一次：中途抛异常 / 提前 return 也要还回去，
#    否则"脚本挂了"会连带把下一个人的 `run_tests.py` 也搞挂（见上面那段）。
import atexit                                                            # noqa: E402
已还原 = [False]


def _还原答题区():
    if 已还原[0]:
        return
    已还原[0] = True
    for d, b in 还原:
        try:
            if os.path.isdir(d):
                shutil.rmtree(d, ignore_errors=True)
            if b and os.path.isdir(b):
                shutil.move(b, d)
        except OSError as e:                                             # noqa: PERF203
            #  ⚠ 还原失败要**喊出来**并指出备份在哪 —— 静默失败就等于把
            #    学员的答题文件弄丢了。
            print("  !! 还原答题区失败：%s（备份还在 %s）—— %s" % (d, b, e))
    shutil.rmtree(备份根, ignore_errors=True)


atexit.register(_还原答题区)

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
    安全名 = dst.replace("/", "__")
    if os.path.isdir(d):
        shutil.move(d, os.path.join(备份根, 安全名))
        还原.append((d, os.path.join(备份根, 安全名)))
    else:
        还原.append((d, None))
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

# ── ③ 收拾干净：把答题区还原成跑之前的样子 ──
#   ⚠ 这一步不是"顺手做的"，是**必须的**：不还原的话，紧接着跑
#     `_build/run_tests.py` 会因为答题区里放的是**参考答案**而挂
#     （见文件开头 ① 之前那段）。`atexit` 已经登记过，这里显式再调一次
#     只是为了让"正常路径"上也真的执行到（函数自身是幂等的）。
_还原答题区()
print("\n  答题区已还原成跑之前的样子。")
