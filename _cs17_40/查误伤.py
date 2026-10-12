# 找出自动补引号时**误伤**的地方：`「...」` 里包的是两个字符串字面量之间的胶水。
#
# 判据：`「」` 里的内容**一个中文字都没有**（而且很短）——
#   正常的正文引用（「长度从哪儿问」）一定含中文；
#   而 `"稳定甲", "甲,乙,丙"` 这种**相邻字面量之间的 `, `** 被包起来就是误伤。
#
# 用法：python _cs17_40\查误伤.py
import glob
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
根 = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
模式 = re.compile(r"「([^」\n]{0,8})」")

for 路径 in sorted(glob.glob(os.path.join(根, "csharp", "cs-lessons", "Lessons", "第*.cs"))):
    名 = os.path.basename(路径)
    if not re.match(r"第\d\d课", 名):
        continue
    for i, 行 in enumerate(io.open(路径, encoding="utf-8").read().split("\n"), 1):
        for m in 模式.finditer(行):
            内容 = m.group(1)
            if re.search(r"[\u4e00-\u9fff]", 内容):
                continue
            print("%s:%d  「%s」   %s" % (名, i, 内容, 行.strip()[:90]))
            break
