# 找出我这几课里**非注释代码行**里的「」—— 那些几乎都是自动补引号的误伤。
#
# 正常的 C# 代码行里不该出现「」（它是正文用的引号）；
# 出现了就说明有一对字符串字面量被包成了「…」，编译可能还过得去，语义已经错了
# （第 28 课的 "汉" + n + "数" 就是这么变成 "汉「 + n + 」数" 的，检查永远找不到标记）。
#
# 用法：python _cs17_40\查代码里的引号.py
import io
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
根 = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
目录 = os.path.join(根, "csharp", "cs-lessons", "Lessons")

坏 = 0
for 编 in ["%02d" % i for i in range(17, 41)]:
    for f in os.listdir(目录):
        if not (f.startswith("第%s课" % 编) and f.endswith(".cs")):
            continue
        for i, line in enumerate(io.open(os.path.join(目录, f), encoding="utf-8").read().split("\n"), 1):
            s = line.lstrip()
            if s.startswith("//"):
                continue
            if "「" in line or "」" in line:
                print("%s:%d  %s" % (f, i, line.strip()[:110]))
                坏 += 1
print("共 %d 处" % 坏)
