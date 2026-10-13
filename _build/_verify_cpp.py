# 用「参考答案」跑全部 C++ 课的检查 —— 和 `_verify_c.py` 同一个思路。
#
# ⚠ 路径不写死：脚本在 LearnCode\_build\ ⇒ 上一级就是仓库根。
import io
import os
import re
import shutil
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LESSONS = os.path.join(ROOT, "csharp", "cpp-lessons", "Lessons")
ANS = os.path.join(ROOT, "cpp-answers")
env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"
env["PYTHONIOENCODING"] = "utf-8"

os.makedirs(ANS, exist_ok=True)
结果 = {}
for f in sorted(os.listdir(LESSONS)):
    if not f.endswith(".cs"):
        continue
    t = io.open(os.path.join(LESSONS, f), encoding="utf-8").read()
    m = re.search(r'\[课程\("(\d+)",\s*"((?:[^"\\]|\\.)*)"', t)
    ans = re.search(r'public static string 参考答案 => """\n(.*?)\n""";', t, re.S)
    if not (m and ans):
        print("  %-34s ✗ 解析不出编号/参考答案" % f)
        continue
    编号 = m.group(1)
    # 先让课程程序建好答题文件（它会用**真实**的文件名约定），再覆盖成参考答案
    shutil.rmtree(ANS, ignore_errors=True)
    os.makedirs(ANS, exist_ok=True)
    subprocess.run(["dotnet", "run", "--project", "csharp/cpp-lessons", "--no-build",
                    "检查", 编号], cwd=ROOT, env=env, capture_output=True, timeout=300)
    文件 = [x for x in os.listdir(ANS) if x.startswith(编号)]
    if not 文件:
        print("  %-34s ✗ 建不出答题文件" % f)
        continue
    io.open(os.path.join(ANS, 文件[0]), "w", encoding="utf-8",
            newline="").write(ans.group(1) + "\n")

    r = subprocess.run(["dotnet", "run", "--project", "csharp/cpp-lessons", "--no-build",
                        "检查", 编号], cwd=ROOT, env=env, capture_output=True, timeout=600)
    out = (r.stdout + r.stderr).decode("utf-8", "replace")
    通过 = "全部通过" in out
    结果[f] = 通过
    n过 = out.count("✓")
    n否 = out.count("✗") - (1 if "上面 ✗ 的部分" in out else 0)
    标题 = m.group(2).replace('\\"', '"').replace("\\\\", "\\")
    print("  %s %s  %-34s ✓%d ✗%d" % ("OK " if 通过 else "XX ", 编号, 标题[:34], n过, max(0, n否)))
    if not 通过:
        for line in out.splitlines():
            if line.strip().startswith("✗"):
                print("         " + line.strip()[:100])

print()
好 = sum(1 for v in 结果.values() if v)
print("  == %d/%d 课用参考答案跑通 ==" % (好, len(结果)))
shutil.rmtree(ANS, ignore_errors=True)
sys.exit(0 if 结果 and 好 == len(结果) else 1)
