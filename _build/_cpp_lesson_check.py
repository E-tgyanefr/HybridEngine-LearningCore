# 给某一课灌一份答案，跑检查。造出来是给我自己验收用的，不是交付物。
#
#   python _build/_cpp_lesson_check.py 16                  # 灌这一课的参考答案
#   python _build/_cpp_lesson_check.py 16 某个文件.cpp       # 灌指定的代码
import io
import os
import re
import shutil
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"
env["PYTHONIOENCODING"] = "utf-8"

编号 = sys.argv[1]
ANS = os.path.join(ROOT, "cpp-answers")
LESSONS = os.path.join(ROOT, "csharp", "cpp-lessons", "Lessons")


def 跑(*args, **kw):
    r = subprocess.run(["dotnet", "run", "--project", "csharp/cpp-lessons", "--no-build"] + list(args),
                       cwd=ROOT, env=env, capture_output=True, timeout=900, **kw)
    return (r.stdout + r.stderr).decode("utf-8", "replace")


def 建答题文件():
    os.makedirs(ANS, exist_ok=True)
    for f in os.listdir(ANS):
        if f.startswith(编号):
            os.remove(os.path.join(ANS, f))
    out = 跑("检查", 编号)
    文件 = [x for x in os.listdir(ANS) if x.startswith(编号)]
    if not 文件:
        print(out)
        raise SystemExit("建不出答题文件")
    return os.path.join(ANS, 文件[0])


if len(sys.argv) > 2:
    源代码 = io.open(sys.argv[2], encoding="utf-8").read()
else:
    源码文件 = None
    for f in sorted(os.listdir(LESSONS)):
        if not f.endswith(".cs"):
            continue
        t = io.open(os.path.join(LESSONS, f), encoding="utf-8").read()
        m = re.search(r'\[课程\("(\d+)"', t)
        if m and m.group(1) == 编号:
            源码文件 = os.path.join(LESSONS, f)
            break
    if not 源码文件:
        raise SystemExit("找不到第 %s 课的课程文件" % 编号)
    t = io.open(源码文件, encoding="utf-8").read()
    m = re.search(r'public static string 参考答案 => """\n(.*?)\s*""";', t, re.S)
    源代码 = m.group(1) + "\n"

路径 = 建答题文件()
io.open(路径, "w", encoding="utf-8", newline="").write(源代码)
out = 跑("检查", 编号)
print(out)
print("通过" if "全部通过" in out else "**没全过**")
sys.exit(0 if "全部通过" in out else 1)
