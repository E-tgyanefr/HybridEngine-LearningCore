# 只读探针：把 C++ 每课的「参考答案」抽出来，方便逐课核对错答案该长什么样。
#
# ⚠ 只读：不写仓库里的任何文件。默认写到一个**仓库外**的临时目录（可用第二个参数改）。
#   它不跑 dotnet、不生成答题文件、不碰 cpp-answers/。
#
# 用法：
#   python _build/_probe_cpp_answers.py 21 25          # 只打印这两课
#   python _build/_probe_cpp_answers.py --抽 21 25 30  # 抽到临时目录，返回路径
import io
import os
import re
import sys
import tempfile

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LESSONS = os.path.join(ROOT, "csharp", "cpp-lessons", "Lessons")

课 = re.compile(r'\[课程\("(\d+)",\s*"((?:[^"\\]|\\.)*)"')
# ⚠ 与 `_verify_compiled.py` 用**同一条**正则（那个脚本为这条正则栽过两版）
答案 = re.compile(r'public static string 参考答案 => """(.*?)""";', re.S)

抽 = "--抽" in sys.argv
参 = [a for a in sys.argv[1:] if a != "--抽"]

出目录 = None
if 抽:
    出目录 = os.path.join(tempfile.gettempdir(), "lc_cpp_ref")
    os.makedirs(出目录, exist_ok=True)

for f in sorted(os.listdir(LESSONS)):
    if not f.endswith(".cs"):
        continue
    文 = io.open(os.path.join(LESSONS, f), encoding="utf-8").read()
    m = 课.search(文)
    if not m:
        continue
    编号 = m.group(1)
    if 参 and 编号 not in 参:
        continue
    a = 答案.search(文)
    if not a:
        print("!! %s 抽不到参考答案" % 编号)
        continue
    标题 = m.group(2).replace('\\"', '"').replace("\\\\", "\\")
    答案文 = a.group(1).strip()
    if 抽:
        路 = os.path.join(出目录, "%s.cpp" % 编号)
        io.open(路, "w", encoding="utf-8", newline="").write(答案文 + "\n")
        print("== %s %s  ->  %s（%d 行）" % (编号, 标题, 路, 答案文.count("\n") + 1))
    else:
        print("=" * 72)
        print("== %s  %s" % (编号, 标题))
        print("=" * 72)
        print(答案文)
        print()
