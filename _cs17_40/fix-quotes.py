# 把课程文件里**散文注释**的英文双引号换成「」。
#
# 为什么：课程的答题模板就是学生读的正文，里面混 ASCII 引号既不好看也容易
# 在后续手工改的时候把 `"""` 原始字符串弄炸。规则是「一律用「」」。
#
# ⚠ 只动**注释行**，而且**跳过代码示例行**（那些引号是 C# 字面量，换了就不是代码了）。
#   判定：注释行里出现 new / Console. / .Compare / => / error CS / Math.Sign /
#   List< / int[ / record 这些记号，就当成代码示例，放过。
#
# 用法：python _cs17_40\fix-quotes.py <文件> [<文件> ...]
import io
import re
import sys

代码记号 = ("new ", "Console.", ".Compare", "=>", "error CS", "Math.Sign",
        "List<", "int[", "record ", "using ", "IComparer<", "return ")

改 = 0
for path in sys.argv[1:]:
    text = io.open(path, encoding="utf-8").read()
    行们 = text.split("\n")
    for i, line in enumerate(行们):
        s = line.lstrip()
        if not s.startswith("//"):
            continue
        if any(m in line for m in 代码记号):
            continue
        if '"' not in line:
            continue
        新 = re.sub(r'"([^"]*)"', lambda m: "「" + m.group(1) + "」", line)
        if 新 != line:
            行们[i] = 新
            改 += 1
    io.open(path, "w", encoding="utf-8", newline="").write("\n".join(行们))
    print("处理好 %s" % path)
print("共改 %d 行" % 改)
