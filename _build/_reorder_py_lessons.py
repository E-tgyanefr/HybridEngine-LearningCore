# 把课程正文的顺序改成「先介绍 → 再布置任务」。
#
# ## 作者的原话
#   「教程可以按照网上现有的来写，先介绍，然后在让学员进行编写」
#
# ## 为什么这个顺序更对
#   原来的顺序是「要做 → 为什么」：学员一打开就先被 4 条要求砸到，
#   而"这些要求为什么这样提"在他**动笔之后**才读到。
#   那时他很可能已经写成了错的样子（比如边遍历边删），
#   再回头看讲解 —— 于是讲解变成了"事后纠错"，而不是"事先铺路"。
#
#   网上成熟的教程（MDN、Python 官方教程、菜鸟教程等）都是这个顺序：
#       ① 这是什么 / 为什么需要它
#       ② 基本用法（配例子）
#       ③ 常见坑
#       ④ **现在轮到你写** ← 练习在最后
#
#   所以这里把「要做」整块搬到讲解之后，并加一个「轮到你了」的标题，
#   让它明显是"读完了，该动手了"。
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
TXT = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode\_build\_py_lessons.txt"

文本 = io.open(TXT, encoding="utf-8").read()

块 = {}
当前 = None
缓冲 = []
for 行 in 文本.split("\n"):
    if 行.startswith("@@@ "):
        if 当前:
            块[当前] = "\n".join(缓冲)
        当前 = 行[4:].strip()
        缓冲 = []
    else:
        缓冲.append(行)
if 当前:
    块[当前] = "\n".join(缓冲)


def 重排(体: str) -> str:
    """把「要做」那块从最前搬到讲解之后。

    只处理**课程文件**（`sol/` 开头的是参考答案，不重排）。
    """
    行们 = 体.split("\n")
    # 找「要做」段的起止：从含「要做」的那行起，到第一个不是注释空行/续行的位置
    起 = None
    for i, l in enumerate(行们):
        if l.startswith("#") and "要做" in l:
            起 = i
            break
    if 起 is None:
        return 体
    # 「要做」段一直延续到"分隔线"（整行是 # ─────）或"标题段"（#  ───）
    止 = None
    for i in range(起 + 1, len(行们)):
        s = 行们[i].strip()
        if s.startswith("# ──") or s.startswith("# ─────"):
            止 = i
            break
        if s == "" or not s.startswith("#"):
            止 = i
            break
    if 止 is None:
        return 体
    要做块 = 行们[起:止]
    其余 = 行们[:起] + 行们[止:]

    # 把「要做：」改成更明确的标题
    新块 = []
    for l in 要做块:
        if "要做" in l and l.startswith("#"):
            新块.append("# ─────────────────────────────────────────────────────")
            新块.append("#  轮到你了 —— 在下面的答题文件里写这几件事")
            新块.append("# ─────────────────────────────────────────────────────")
            新块.append("#")
        else:
            新块.append(l)

    # 找"怎么自己测"那一节（它在正文最后）—— 插在它**前面**
    插 = len(其余)
    for i, l in enumerate(其余):
        if l.startswith("#") and "怎么自己测" in l:
            插 = i
            break
    # 去掉 插点前面的多余空注释行
    出 = 其余[:插] + ["#"] + 新块 + ["#"] + 其余[插:]
    return "\n".join(出)


改 = 0
for 名 in list(块):
    if 名.startswith("sol/"):
        continue
    新 = 重排(块[名])
    if 新 != 块[名]:
        块[名] = 新
        改 += 1
        print("  重排 %s" % 名)
print("  共重排 %d 课" % 改)

# 写回源文件
出 = []
for 名, 体 in 块.items():
    出.append("@@@ " + 名)
    出.append(体.rstrip("\n"))
io.open(TXT, "w", encoding="utf-8", newline="\n").write("\n".join(出) + "\n")
print("  已写回 _py_lessons.txt")
