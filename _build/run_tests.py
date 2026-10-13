"""跑全套测试 —— **分两批**。

════════════════════════════════════════════════════════════════
  ⚠⚠ 为什么必须分两个进程
════════════════════════════════════════════════════════════════

`tests/test_gui_flow.py` 是唯一一个**真的建 Tk 窗口、真的跑 mainloop()**
的测试（这是刻意的 —— 见那个文件的开头：Tk 的线程检查条件是
"主线程在不在 mainloop 里"，用 `update()` 轮询会得到假结论）。

实测结果：

    python -m unittest tests.test_gui_flow                     ✅ 7 个测试全过
    python -m unittest <其他任意一个文件> tests.test_gui_flow    ✅ 也过
    python -m unittest <前面 6 个文件> tests.test_gui_flow       ❌ **解释器硬崩**
                                           退出码 2147483651（0x80000003）

  崩的是**解释器本身**，不是某个断言失败 —— 所以 `unittest discover`
  跑完连 "Ran N tests" 那一行都打不出来，看起来像"测试全挂了"，
  而实际上是 Tk 在多模块共用一个进程时状态被污染。

  ⚠ 这个崩溃**在改动之前就有**（拿 HEAD 版的 tests 试过，一样崩），
    而且不稳定 —— 同一个命令有时绿有时崩。它不是我这次改出来的，
    但既然查清楚了，就顺手把它固定成"永远不会再遇到"的样子。

════════════════════════════════════════════════════════════════
  用法
════════════════════════════════════════════════════════════════

    python _build/run_tests.py

  等价于两步（想手动跑的时候用这两条）：

    python -m unittest discover -s tests -p "test_*.py" \\
           --ignore=tests/test_gui_flow.py
    python -m unittest tests.test_gui_flow
"""
import os
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
env = dict(os.environ)
env["PYTHONIOENCODING"] = "utf-8"
env["PYTHONUNBUFFERED"] = "1"
# ⚠⚠ 别把 `__pycache__` 写进要交付的目录（2026-10-13 实测）
#
#   本脚本把子进程的 cwd 设成 `LC` —— 也就是**产品根**。而测试会 import
#   `hybridengine_light` 和 `tests` ⇒ 默认会在产品根里**就地编译**，
#   生成一大堆 `.pyc`。
#
#   症状（实测）：`make_samplecore.py` 收尾时明明清过 `__pycache__`，
#   但只要有人在**发行物里**跑一次本脚本，**146 个 `.pyc` 立刻回来**，
#   时间戳全部晚于生成器写 README 的时刻。
#   ⇒ 「**生成时**排掉了」和「**发行时**是干净的」是两件事。
#      谁跑一次测试，第一件就白做了。
#
#   `PYTHONDONTWRITEBYTECODE=1` 是 Python 的官方开关，对**子进程同样生效**。
#
# ⚠ **这条挡不住答题模板**（`*-answers/`、`lessons/answers/`）：
#   那些是**产品按设计**在内容根旁边生成的（见 `hybridengine_light/_paths.py`
#   的 `内容根()` —— 它由**包的位置**决定，**不是 cwd**）。
#   ⇒ 「换个 cwd 跑」**解决不了**答题模板，只会把相对路径搞坏。
#      要发行物干净，得在**生成器那侧**收尾（或按"生成 → 跑测试 → 再生成一次"）。
env["PYTHONDONTWRITEBYTECODE"] = "1"

批次 = [
    ("第一批：除界面全流程之外的全部测试",
     # ⚠ 这里**不能用 `discover --ignore`** —— 这个 Python 版本的
     #   `unittest discover` 不认 `--ignore`（报 unrecognized arguments，
     #   退出码 2，而输出里只有一句 argparse 的抱怨，看起来像测试挂了）。
     #   ⇒ 显式列出模块。反正就这 15 个，列出也更清楚。
     [sys.executable, "-m", "unittest"] + [
         "tests.test_ai", "tests.test_anim", "tests.test_courselines",
         "tests.test_editor", "tests.test_exam", "tests.test_games",
         "tests.test_gui_model", "tests.test_hybridengine_light",
         "tests.test_lessons", "tests.test_mcu", "tests.test_pause",
         "tests.test_pyengine", "tests.test_raspi", "tests.test_startup",
         #: ⚠ 会**真开 tkinter 窗口**（每个测试立刻关掉）。
         #:   无显示环境时它自己 skip（不算失败、也不算通过）。
         "tests.test_tk_hot_backend",
         "tests.test_version",
     ]),
    ("第二批：界面全流程（必须独占一个进程）",
     [sys.executable, "-m", "unittest", "tests.test_gui_flow"]),
]

总过 = True
for 名, 命令 in 批次:
    print()
    print("═" * 60)
    print("  " + 名)
    print("═" * 60)
    r = subprocess.run(命令, cwd=LC, env=env, capture_output=True)
    出 = (r.stdout + r.stderr).decode("utf-8", "replace")
    # ⚠ Tk 在窗口销毁后触发定时器会产生一堆 `invalid command name …`，
    #   那是**噪音不是失败** —— 只保留有意义的那几行。
    for l in 出.split("\n"):
        s = l.strip()
        if (s.startswith(("Ran ", "OK", "FAILED", "FAIL:", "ERROR:", "----"))
                or "Traceback" in s or "Error" in s and "invalid command" not in s):
            print("  " + s[:110])
    if r.returncode != 0:
        总过 = False
        print("  **退出码 %s**" % r.returncode)

print()
print("═" * 60)
print("  结论：%s" % ("两批都通过 ✅" if 总过 else "有失败 ❌"))
print("═" * 60)
sys.exit(0 if 总过 else 1)
