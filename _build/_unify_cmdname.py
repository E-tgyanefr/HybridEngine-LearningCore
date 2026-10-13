# 把「用法提示里的命令名」统一到 _paths.py（单一真值），并修掉所有面向学员的
# 硬编码 `python -m hybridengine_light`。
#
# ## 为什么必须做
#   打包成 exe 后，学员看到的是：
#      看某一课：   python -m hybridengine_light 嵌入式 01
#   而他们机器上**没有 Python** —— 照抄就失败。
#   而新手唯一的线索就是这些提示文字。
#   ⚠ 实测：`run.py` 里改了，但 aicourse / embedded / cmd_exam / raspi_course
#     里还有 11 处没改（我只改了当时看到的那一个模块）。
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）
PKG = os.path.join(LC, "hybridengine_light")

# ── ① 把 命令名() 放进 _paths.py（单一真值）───────────────────────────
P = os.path.join(PKG, "_paths.py")
t = io.open(P, encoding="utf-8").read()
if "def 命令名(" not in t:
    助手 = '''

def 命令名() -> str:
    """用法提示里该显示的命令 —— 打包成 exe 与 `python -m` 用法**不同**。

    ⚠ 不区分的话：exe 用户照着提示打 `python -m hybridengine_light`
      会失败（他们机器上可能根本没 Python），而提示文字是新手唯一的线索。

    ⚠ 放在这里（而不是 run.py）是因为**每个模块**都要用它：
      实测 aicourse / embedded / cmd_exam / raspi_course 里各有若干处提示，
      只改一处的话，学员换个课程线又会看到错的命令。
    """
    import sys as _sys
    if getattr(_sys, "frozen", False):
        import os as _os
        return _os.path.basename(_sys.executable)      # 例如 HybridEngine-light.exe
    return "python -m hybridengine_light"
'''
    t = t.rstrip("\n") + "\n" + 助手
    io.open(P, "w", encoding="utf-8", newline="").write(t)
    print("  ① _paths.py: 已加 命令名()")
else:
    print("  ① _paths.py: 已有 命令名()")

# ── ② 各模块：import 它，并把 print 里的硬编码换成 {命令名()} ─────────
目标 = ["aicourse.py", "cmd_exam.py", "embedded.py", "raspi_course.py",
        "coach.py", "ctl.py", "examrun.py", "check.py", "__main__.py", "exam.py"]

for f in 目标:
    p = os.path.join(PKG, f)
    if not os.path.isfile(p):
        continue
    src = io.open(p, encoding="utf-8").read()
    原 = src

    # 只在 **print(...)** 行里替换（docstring / 注释不动 —— 那里的包名是对的）
    出 = []
    n = 0
    for line in src.split("\n"):
        if "python -m hybridengine_light" in line and line.lstrip().startswith("print("):
            new = line.replace("python -m hybridengine_light", "{命令名()}")
            if 'f"' not in new and "f'" not in new:
                new = new.replace('print("', 'print(f"').replace("print('", "print(f'")
            出.append(new)
            n += 1
        else:
            出.append(line)
    src = "\n".join(出)

    if n == 0:
        continue

    # 补 import（在文件头第一个 import 之后，且要在 __future__ 之后）
    行 = src.split("\n")
    if not any("命令名" in l and "import" in l for l in 行):
        未来 = [i for i, l in enumerate(行) if l.startswith("from __future__ import")]
        位置 = 未来[0] + 1 if 未来 else 0
        行.insert(位置, "from ._paths import 命令名")
        src = "\n".join(行)

    io.open(p, "w", encoding="utf-8", newline="").write(src)
    print(f"  {f}: 改了 {n} 处提示 + 加 import")

# ── ③ 语法检查 ────────────────────────────────────────────────────────
import ast
坏 = 0
for f in sorted(os.listdir(PKG)):
    if not f.endswith(".py"):
        continue
    try:
        ast.parse(io.open(os.path.join(PKG, f), encoding="utf-8").read())
    except SyntaxError as e:
        print(f"  XX {f} 第 {e.lineno} 行: {e.msg}")
        坏 += 1
print(f"  ③ 语法检查：{坏} 个文件有问题")
