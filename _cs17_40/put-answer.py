# 把某课的「参考答案」从课程文件里抠出来，写进沙箱的答题文件。
#
# 为什么要脚本：参考答案就在课程文件的 `参考答案 => """…"""` 里，
# 手抄一份进答题文件会和课程里的那份**慢慢不一致** —— 而"参考答案必须能过检查"
# 这条要求针对的是课程文件里那一份。所以让脚本去抠，只有一份真相。
#
# 用法：
#   python _cs17_40\put-answer.py C:\lc_box1 18
#
# ⚠ 答题文件必须先存在 —— 先跑一次 `dotnet run --project csharp/cs-lessons 检查 18`
#   （第一次会把答题文件建出来，并把路径打印给你看）。
import glob
import io
import os
import re
import sys

root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

if len(sys.argv) < 3:
    print("用法：python _cs17_40\\put-answer.py <沙箱目录> <课号>")
    sys.exit(2)

box, num = sys.argv[1], sys.argv[2]
命中 = glob.glob(os.path.join(root, "csharp", "cs-lessons", "Lessons", "第%s课_*.cs" % num))
if not 命中:
    print("找不到课程文件：第%s课_*.cs" % num)
    sys.exit(1)

文 = io.open(命中[0], encoding="utf-8").read()
m = re.search(r'参考答案 => """(.*?)""";', 文, re.S)
if not m:
    print("课程文件里解析不出 参考答案 => \"\"\"…\"\"\"：%s" % 命中[0])
    sys.exit(1)
答案 = m.group(1).strip()

目录 = os.path.join(box, "cs-answers")
if not os.path.isdir(目录):
    print("沙箱里还没有 cs-answers —— 先跑一次 检查 %s 把答题文件建出来" % num)
    sys.exit(1)
文件 = [f for f in os.listdir(目录) if f.startswith(num)]
if not 文件:
    print("cs-answers 里没有 %s_*.cs —— 先跑一次 检查 %s" % (num, num))
    sys.exit(1)

路径 = os.path.join(目录, 文件[0])
io.open(路径, "w", encoding="utf-8", newline="").write(答案 + "\n")
print("已写入 %s（%d 行）" % (路径, 答案.count("\n") + 1))
