# 找**代码行**里"引号插在中文中间"的地方 —— 那几乎一定是 C# 字符串字面量被劈开了。
#
# 为什么单独一个脚本：我连着几课都犯同一个错 —— 在检查的提示文字里顺手写
#     "…再"能接就接"：…"
# 于是那行 C# 变成 `"…再" + 能接就接 + "：…"`，编译直接报 CS1003。
# 判据：非注释行里出现「中文 " 中文」（`[\u4e00-\u9fff]"[^\s]` 这种）——
#   正常的字符串里，闭引号后面跟的是 \n / + / ) / , / ; / " 或者行尾，
#   绝不会紧跟一个中文字。
#
# 用法：python _cs17_40\lint-quotes.py <文件> ...
import io
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

坏 = 0
for path in sys.argv[1:]:
    for i, line in enumerate(io.open(path, encoding="utf-8").read().split("\n"), 1):
        if line.lstrip().startswith("//"):
            continue
        for m in re.finditer(r'[\u4e00-\u9fff]"[\u4e00-\u9fff]', line):
            print("%s:%d  %s" % (path.split("\\")[-1], i, line.strip()[:100]))
            坏 += 1
            break
print("共 %d 处" % 坏)
