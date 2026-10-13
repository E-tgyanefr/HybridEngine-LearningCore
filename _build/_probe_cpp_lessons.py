# 只读探针：把 C++ 每课的「编号 / 标题 / 检查项 / 检查方法名」列出来。
#
# ⚠ 这是个**只读**脚本：不写任何课程文件、不跑 dotnet、不生成答题文件。
#   目的只有一个 —— 写反面验证脚本前，先看清楚每一课到底在查什么。
#
# 用法： python _build/_probe_cpp_lessons.py [编号...]
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LESSONS = os.path.join(ROOT, "csharp", "cpp-lessons", "Lessons")

只 = sys.argv[1:]

课 = re.compile(r'\[课程\("(\d+)",\s*"((?:[^"\\]|\\.)*)"')
检查 = re.compile(
    r'\[检查说明\("((?:[^"\\]|\\.)*)"\)\]\s*'
    r'public\s+static\s+CheckResult\s+(检查_\w+)\s*\(', re.S)

总检查 = 0
for f in sorted(os.listdir(LESSONS)):
    if not f.endswith(".cs"):
        continue
    文 = io.open(os.path.join(LESSONS, f), encoding="utf-8").read()
    m = 课.search(文)
    if not m:
        print("!! 解析不出编号：%s" % f)
        continue
    编号 = m.group(1)
    if 只 and 编号 not in 只:
        continue
    标题 = m.group(2).replace('\\"', '"').replace("\\\\", "\\")
    cs = 检查.findall(文)
    总检查 += len(cs)
    print("%s  %s  （%d 项检查）" % (编号, 标题, len(cs)))
    for 说明, 方法 in cs:
        说明 = 说明.replace('\\"', '"')
        print("      . %-34s %s" % (说明, 方法))
print()
print("== 共 %d 课（筛选后 %s），检查项合计 %d =="
      % (len(os.listdir(LESSONS)), 只 or "全部", 总检查))
