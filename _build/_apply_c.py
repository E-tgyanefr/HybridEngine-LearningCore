"""把 `_c_模板扩写.md` 里的 20 课模板落盘到 C 的数据文件。

⚠⚠ 为什么用"按课块替换"而不是全局替换：
  旧的模板文本在不同课里**可能有相同的行**（比如 `// ── 在下面写你的函数 ──`），
  全局替换会串课。按 `@@@ NN_` 切块、只在那一块里替换，才不会串。

⚠ 写完必须立刻跑 `_gen_c16.py` + `_verify_compiled.py c` ——
  "看着差不多就交"在这个项目里已经害我几次了。
"""
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
B = os.path.join(LC, "_build")

原文 = io.open(os.path.join(B, "_c_模板扩写.md"), encoding="utf-8").read()
新模板 = dict(re.findall(
    r"^=====\s*(\d\d)\s*=====\s*\n<<<模板开始>>>\n(.*?)\n<<<模板结束>>>\s*$",
    原文, re.M | re.S))
print("  新模板：%d 课" % len(新模板))

文件们 = ("_c00.txt", "_c16.txt", "_c_extra.txt")
统计 = []
for f in 文件们:
    P = os.path.join(B, f)
    行们 = io.open(P, encoding="utf-8").read().split("\n")
    出 = []
    当前 = None
    改了 = 0
    i = 0
    while i < len(行们):
        l = 行们[i]
        if l.startswith("@@@ "):
            当前 = l[4:6]
            出.append(l)
            i += 1
            continue
        if 当前 in 新模板 and l.startswith("模板|"):
            # 吃掉这一课**连续**的所有 `模板|` 行，换成新的
            while i < len(行们) and 行们[i].startswith("模板|"):
                i += 1
            出 += ["模板|" + x for x in 新模板[当前].split("\n")]
            统计.append((当前, f))
            改了 += 1
            当前 = None          # ⚠ 只替换一次，避免同一课里二次命中
            continue
        出.append(l)
        i += 1
    io.open(P, "w", encoding="utf-8", newline="").write("\n".join(出))
    print("    %-16s 替换 %d 课" % (f, 改了))

print()
print("  替了 %d 课：%s" % (len(统计), " ".join(k for k, _ in 统计)))
漏 = sorted(set(新模板) - {k for k, _ in 统计})
print("  没替上的：%s" % (漏 or "（无）"))
