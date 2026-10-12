# 从课程 .cs 文件里把「参考答案」抽出来，写进 <线>-answers\NN_*.java|.cs
# 用法：python _build\_answer.py java            （全部 java 课）
#       python _build\_answer.py java 17 18      （只写这几课）
import io
import os
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
线表 = {
    "c": dict(工程="c-lessons", 答案="c-answers", 特性="课程", 后缀=".c"),
    "cpp": dict(工程="cpp-lessons", 答案="cpp-answers", 特性="课程", 后缀=".cpp"),
    "java": dict(工程="java-lessons", 答案="java-answers", 特性="Java课程", 后缀=".java"),
    "csharp": dict(工程="cs-lessons", 答案="cs-answers", 特性="课程", 后缀=".cs"),
}

env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"


def 写(键, 只要=None):
    参 = 线表[键]
    Lessons = os.path.join(ROOT, "csharp", 参["工程"], "Lessons")
    ANS = os.path.join(ROOT, 参["答案"])
    os.makedirs(ANS, exist_ok=True)
    好, 坏 = [], []
    for f in sorted(os.listdir(Lessons)):
        if not f.endswith(".cs"):
            continue
        t = io.open(os.path.join(Lessons, f), encoding="utf-8").read()
        m = re.search(r'\[%s\("(\d+)"' % 参["特性"], t)
        if not m:
            continue
        编号 = m.group(1)
        if 只要 and 编号 not in 只要:
            continue
        答案 = re.search(r'public static string 参考答案 => """(.*?)""";', t, re.S)
        if not 答案:
            坏.append((编号, f, "解析不出参考答案"))
            continue
        文 = 答案.group(1).strip() + "\n"
        # 先让课程程序用真实文件名约定建好文件，再覆盖
        r = subprocess.run(["dotnet", "run", "--project", "csharp/" + 参["工程"], "检查", 编号],
                           cwd=ROOT, env=env, capture_output=True, timeout=300)
        out = (r.stdout + r.stderr).decode("utf-8", "replace")
        文件们 = [x for x in os.listdir(ANS) if x.startswith(编号)]
        if not 文件们:
            坏.append((编号, f, "课程程序没建出答题文件：" + out.strip()[-200:]))
            continue
        p = os.path.join(ANS, 文件们[0])
        io.open(p, "w", encoding="utf-8", newline="").write(文)
        好.append((编号, os.path.basename(p), 文.count("\n")))
    for 编号, 名, n in 好:
        print("  OK  %s  %-40s %d 行" % (编号, 名, n))
    for 编号, f, 为什么 in 坏:
        print("  XX  %s  %s  —— %s" % (编号, f, 为什么))
    print("\n  %s：写好 %d 课，失败 %d 课" % (键, len(好), len(坏)))
    return len(坏)


坏 = 0
if len(sys.argv) < 2:
    print("用法：python _build/_answer.py <线> [编号...]")
    sys.exit(2)
键 = sys.argv[1]
if 键 not in 线表:
    print("不认识的线：%s" % 键)
    sys.exit(2)
坏 = 写(键, set(sys.argv[2:]) or None)
sys.exit(1 if 坏 else 0)
