# 最后一道自检：找 17~40 课里**代码行**上可疑的「」残留。
#
# 可疑 = 「」里没有中文，而且里面有括号或引号 —— 那多半是一对字符串字面量
# （含函数调用/构造）被包起来了，比如第 19 课那个探针：
#     new 成绩("甲", 1), new 成绩("乙", 2)   ⇒   new 成绩("甲「, 1), new 成绩(」乙", 2)
# 这种残留编译得过，但语义已经错了（检查会莫名其妙地失败）。
#
# 用法：python _cs17_40\查残留.py
import glob
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
根 = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

坏 = 0
for f in sorted(glob.glob(os.path.join(根, "csharp", "cs-lessons", "Lessons", "第*.cs"))):
    名 = os.path.basename(f)
    m = re.search(r"第(\d\d)课", 名)
    if not m or not (17 <= int(m.group(1)) <= 40):
        continue
    for i, line in enumerate(io.open(f, encoding="utf-8").read().split("\n"), 1):
        if line.lstrip().startswith("//"):
            continue
        for mm in re.finditer(r"「([^」\n]*)」", line):
            内 = mm.group(1)
            if re.search(r"[\u4e00-\u9fff]", 内):
                continue                      # 正文引用，正常
            if "(" in 内 or '"' in 内 or "," in 内 or "+" in 内:
                print("%s:%d  %s" % (名, i, line.strip()[:110]))
                坏 += 1
print("可疑残留 %d 处" % 坏)
