# -*- coding: utf-8 -*-
r"""实测：`run_tests.py` 会不会把 `__pycache__` 写进**产品根**，以及那条修复管不管用。

════════════════════════════════════════════════════════════════
  要回答的问题
════════════════════════════════════════════════════════════════

`HybridEngine-SampleCore` 是**生成**的，生成器收尾时清过 `__pycache__`。
但实测：**在发行物里跑一次 `run_tests.py`，`.pyc` 就全回来了**
（146 个，时间戳全部晚于生成器写 README 的时刻）。

⇒ 症状是「生成时明明排掉了，跑完测试又回来了」。

**机制假设**：`run_tests.py` 把子进程 cwd 设成产品根，测试会
`import hybridengine_light` / `import tests` ⇒ CPython **就地**写 `.pyc`。

**这条脚本不靠"读代码觉得对"**，而是把假设**证伪一遍**：

    ① 先证明"不设开关就会写"（否则后面那条"设了就不写"什么都没证明）
    ② 再证明"设了 `PYTHONDONTWRITEBYTECODE=1` 就不写"
    ③ 最后跑**真的** `run_tests.py`，看整个过程中有没有新增 `.pyc`

⚠ 只动 `hybridengine_light/__pycache__`（`.gitignore` 里的字节码缓存，
  删了会自动重建）—— **不碰任何课程文件**。

用法：
    python _build/_verify_no_pyc.py
退出码：0 = 三条都如预期；1 = 有哪条不符合 ⇒ 结论不成立。
"""
import io
import os
import shutil
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
包目录 = os.path.join(ROOT, "hybridengine_light")
缓存 = os.path.join(包目录, "__pycache__")

env = dict(os.environ, PYTHONIOENCODING="utf-8", PYTHONUNBUFFERED="1")
DOTNET = dict(env, DOTNET_CLI_UI_LANGUAGE="en")

结论 = []


def 跑(命令, 环境, 秒=600):
    r = subprocess.run(命令, cwd=ROOT, env=环境, capture_output=True, timeout=秒)
    return r.returncode, (r.stdout + r.stderr).decode("utf-8", "replace")


def 缓存里有几个():
    if not os.path.isdir(缓存):
        return 0
    return len([f for f in os.listdir(缓存) if f.endswith(".pyc")])


def 清缓存():
    shutil.rmtree(缓存, ignore_errors=True)


print("═" * 70)
print("  实测：测试会不会把 __pycache__ 写进产品根")
print("═" * 70)
print("  仓库根：%s" % ROOT)
print("  观察点：%s" % 缓存)
print()

# ── ① 证伪对照：**不设**开关 ⇒ 应该会写 ────────────────────────────────
清缓存()
码, 出 = 跑([sys.executable, "-c", "import hybridengine_light.learnmodel"], env)
写没写 = 缓存里有几个() > 0
print("  ① 不设 PYTHONDONTWRITEBYTECODE（对照）：")
print("     import hybridengine_light.learnmodel ⇒ 退出码 %d，"
      "__pycache__ 里 %d 个 .pyc" % (码, 缓存里有几个()))
print("     ⇒ %s" % ("**会写**（机制确认）" if 写没写 else "**没写**（机制不成立！）"))
结论.append(("① 不设开关会写 .pyc", 写没写))

# ── ② 实验组：设了开关 ⇒ 不该写 ────────────────────────────────────────
清缓存()
env2 = dict(env, PYTHONDONTWRITEBYTECODE="1")
码2, 出2 = 跑([sys.executable, "-c", "import hybridengine_light.learnmodel"], env2)
写没写2 = 缓存里有几个() > 0
print()
print("  ② 设 PYTHONDONTWRITEBYTECODE=1：")
print("     import hybridengine_light.learnmodel ⇒ 退出码 %d，"
      "__pycache__ 里 %d 个 .pyc" % (码2, 缓存里有几个()))
print("     ⇒ %s" % ("**没写**（修复有效）" if not 写没写2 else "**还是写了**（修复无效！）"))
结论.append(("② 设了开关不写 .pyc", not 写没写2))

# ── ③ 集成：跑真的 run_tests.py，看有没有新增 .pyc ─────────────────────
清缓存()
全仓pyc前 = {os.path.join(r, f)
             for r, _, fs in os.walk(ROOT) for f in fs if f.endswith(".pyc")}
t0 = os.path.getmtime(__file__)          # 只用来给报告一个"跑之前"的时间感
起 = __import__("time").time()
码3, 出3 = 跑([sys.executable, os.path.join(ROOT, "_build", "run_tests.py")], DOTNET)
止 = __import__("time").time()
新增 = []
for r, _, fs in os.walk(ROOT):
    for f in fs:
        if not f.endswith(".pyc"):
            continue
        p = os.path.join(r, f)
        if p not in 全仓pyc前 and os.path.getmtime(p) >= 起 - 2:
            新增.append(os.path.relpath(p, ROOT))
print()
print("  ③ 真跑 _build/run_tests.py（跑完那个 import 的对照又清过一次缓存）：")
print("     退出码 %d   用时 %.1f 秒" % (码3, 止 - 起))
for l in 出3.splitlines():
    s = l.strip()
    if s.startswith(("Ran ", "OK", "FAILED", "结论")):
        print("       | " + s[:100])
print("     新增 .pyc：%d 个" % len(新增))
for p in 新增[:12]:
    print("       + " + p)
if len(新增) > 12:
    print("       …（共 %d 个）" % len(新增))
print("     ⇒ %s" % ("**没有再往产品根里写 .pyc**" if not 新增
                     else "**仍然写了 %d 个**" % len(新增)))
结论.append(("③ run_tests.py 全程不新增 .pyc", not 新增))
结论.append(("③ run_tests.py 退出码 0", 码3 == 0))

print()
print("═" * 70)
print("  逐条结论：")
for 名, 过 in 结论:
    print("    %s %s" % ("OK" if 过 else "XX", 名))
print("  == %d/%d 条符合预期 ==" % (sum(1 for _, x in 结论 if x), len(结论)))
print("═" * 70)

# 收尾：把这次实验产生的缓存清掉，别给后面的人留噪音
清缓存()
sys.exit(0 if all(x for _, x in 结论) else 1)
