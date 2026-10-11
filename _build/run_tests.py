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
