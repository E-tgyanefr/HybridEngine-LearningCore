# -*- coding: utf-8 -*-
"""**替换我自己那几课**（31~40）在共享数据文件里的块。

⚠ 和 `追加.py` 的区别：
   追加 = 只往后面加，不碰任何已有字节（第一次交付用的就是这个）。
   改块 = 我自己的课写完又改了（比如第 40 课的边表顺序），要换掉。

⚠⚠ 改块比追加危险得多，所以这里做三道保险：
     ① **每次重新读文件**（不缓存）
     ② 只替换「要改的那几课」那一段，**别的一律原样拼回去**
     ③ 拼完以后逐段核对：**除了这几课，其他课的每一块都必须和原文一字不差**。
        对不上就拒绝写入 —— 宁可什么都不做，也不能把别人的课弄丢。
"""
import io
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
D = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode\_build"
目标 = os.path.join(D, "_c20_40.txt")
草稿 = [os.path.join(D, "_c31_40_试验", "draft_31_35.txt"),
        os.path.join(D, "_c31_40_试验", "draft_36_40.txt")]
我的 = [str(i) for i in range(31, 41)]


def 切块(文本):
    """→ (头, [(课号, 块文本)], 尾)。块文本不含首尾多余空行。"""
    lines = 文本.split("\n")
    头, 块们, 当前, 当前行 = [], [], None, []
    for i, 行 in enumerate(lines):
        if 行.startswith("@@@ "):
            if 当前 is None:
                头 = lines[:i]
            else:
                块们.append((当前, "\n".join(当前行)))
            当前, 当前行 = 行[4:].strip()[:2], [行]
        elif 当前 is not None:
            当前行.append(行)
    if 当前 is not None:
        块们.append((当前, "\n".join(当前行)))
    return 头, 块们


新块 = {}
for f in 草稿:
    _, 块们 = 切块(io.open(f, encoding="utf-8-sig").read())
    for 号, 块 in 块们:
        新块[号] = 块.rstrip("\n")

原文 = io.open(目标, encoding="utf-8").read()
原文尾 = "\n" if 原文.endswith("\n") else ""
头, 块们 = 切块(原文.rstrip("\n"))
print("  目标现状：%d 课 → %s" % (len(块们), " ".join(号 for 号, _ in 块们)))

改了 = []
出 = []
for 号, 块 in 块们:
    if 号 in 我的 and 号 in 新块 and 新块[号] != 块.rstrip("\n"):
        出.append(新块[号]); 改了.append(号)
    else:
        出.append(块)
新文 = "\n".join(头 + 出) + 原文尾
if not 改了:
    print("  我的课一个字都没变，不做任何写入。")
    sys.exit(0)
print("  要替换的课：%s" % " ".join(改了))

# ── 保险：除了这几课，其他块必须一字不差 ──
_, 新块们 = 切块(新文)
旧表 = dict(块们)
新表 = dict(新块们)
if [号 for 号, _ in 块们] != [号 for 号, _ in 新块们]:
    print("  XX 拒绝写入：课号列表变了")
    sys.exit(1)
for 号, 块 in 旧表.items():
    if 号 in 改了:
        continue
    if 新表[号] != 块:
        print("  XX 拒绝写入：第 %s 课被动到了" % 号)
        sys.exit(1)
print("  自检：其他 %d 课一字未动 ✓" % (len(块们) - len(改了)))

io.open(目标, "w", encoding="utf-8", newline="").write(新文)
回读 = io.open(目标, encoding="utf-8").read()
if 回读.rstrip("\n") != 新文.rstrip("\n"):
    print("  XX 回读不一致")
    sys.exit(1)
_, 回读块 = 切块(回读.rstrip("\n"))
print("  写入完成：%d 课 → %s" % (len(回读块), " ".join(号 for 号, _ in 回读块)))
