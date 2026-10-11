"""把 `_cs_模板扩写.md` 的 7 课模板落盘到 C# 课文件。

⚠ 替换的是 `模板 => """…""";` 里**两个定界符之间**的内容。
  定界符本身要留着（`"""` 换行 + 内容 + 换行 + `"""`），
  所以新内容前后各补一个换行。

⚠ 写完必须编译 + 跑验证 —— "看着差不多就交"在这个项目里已经害我几次。
"""
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
B = os.path.join(LC, "_build")
D = os.path.join(LC, "csharp", "cs-lessons", "Lessons")

原文 = io.open(os.path.join(B, "_cs_模板扩写.md"), encoding="utf-8").read()
新模板 = dict(re.findall(
    r"^=====\s*(\d\d)\s*=====\s*\n<<<模板开始>>>\n(.*?)\n<<<模板结束>>>\s*$",
    原文, re.M | re.S))
print("  新模板 %d 课：%s" % (len(新模板), " ".join(sorted(新模板))))

for 号, 文 in sorted(新模板.items()):
    候选 = [f for f in os.listdir(D) if f.startswith("第" + 号 + "课")]
    if len(候选) != 1:
        print("  XX %s：找到 %d 个文件" % (号, len(候选)))
        continue
    P = os.path.join(D, 候选[0])
    t = io.open(P, encoding="utf-8").read()
    m = re.search(r'(模板 => """)(.*?)(""";)', t, re.S)
    if not m:
        print("  XX %s：找不到 模板 定界符" % 号)
        continue
    旧行 = len([x for x in m.group(2).split("\n") if x.strip()])
    t = t[:m.start(2)] + "\n" + 文.strip("\n") + "\n" + t[m.end(2):]
    io.open(P, "w", encoding="utf-8", newline="").write(t)
    新行 = len([x for x in 文.split("\n") if x.strip()])
    print("    %s  %-40s %3d → %3d 行" % (号, 候选[0][:38], 旧行, 新行))
