# 用**参考答案**跑某几课的检查，输出压到最小（只留结论和 ✗ 的行）。
#
# 用法：
#   python _cs17_40\check.py 17 18 19
#
# 干的事：① 先跑一次「检查 NN」把答题文件建出来（沙箱/真工程都会自动建）
#        ② 把课程文件里的参考答案写进答题文件
#        ③ 再跑一次「检查 NN」，只打印结论
# ⚠ 必须在真工程根目录下跑（cs-answers 会被这个脚本反复覆盖 —— 它就是这个用途）。
import io
import os
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ANS = os.path.join(ROOT, "cs-answers")
LESSONS = os.path.join(ROOT, "csharp", "cs-lessons", "Lessons")

env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"
env.setdefault("TEMP", r"C:\lc_cs_tmp")
env.setdefault("TMP", r"C:\lc_cs_tmp")


def 跑(编号):
    r = subprocess.run(["dotnet", "run", "--project", "csharp/cs-lessons", "检查", 编号],
                       cwd=ROOT, env=env, capture_output=True, timeout=900)
    return (r.stdout + r.stderr).decode("utf-8", "replace")


def 一提(编号, 详细=False):
    命中 = [f for f in os.listdir(LESSONS)
            if f.startswith("第%s课" % 编号) and f.endswith(".cs")]
    if not 命中:
        print("  ?? %s 找不到课程文件" % 编号)
        return
    out = 跑(编号)
    if not os.path.isdir(ANS):
        os.makedirs(ANS, exist_ok=True)
    文件 = sorted(f for f in os.listdir(ANS) if f.startswith(编号))
    if not 文件:
        print("  ?? %s 建不出答题文件" % 编号)
        return
    文 = io.open(os.path.join(LESSONS, 命中[0]), encoding="utf-8").read()
    m = re.search(r'参考答案 => """(.*?)""";', 文, re.S)
    if not m:
        print("  ?? %s 解析不出参考答案" % 编号)
        return
    io.open(os.path.join(ANS, 文件[0]), "w", encoding="utf-8", newline="").write(m.group(1).strip() + "\n")

    out = 跑(编号)
    行 = [l.strip() for l in out.splitlines()]
    总结 = [l for l in 行 if "全部通过" in l or l.startswith("通过 ")]
    叉 = [l for l in 行 if l.startswith("✗")]
    勾 = len([l for l in 行 if l.startswith("✓")])
    总结 = 总结[0] if 总结 else "(没有结论行)"
    print("  %s %s  ✓%d  %s" % ("OK " if "全部通过" in out else "XX ", 编号, 勾, 总结))
    if "全部通过" not in out:
        for l in 叉[:6]:
            print("        " + l[:110])
        if 详细:
            for l in 行:
                if l.startswith("得到") or "应该是" in l or "应当是" in l:
                    print("          " + l[:110])


for 编号 in sys.argv[1:]:
    一提(编号, 详细=True)
