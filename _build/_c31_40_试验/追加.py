# -*- coding: utf-8 -*-
"""把课堂草稿里的课**追加**到共享数据文件 `_build/_c20_40.txt`。

⚠⚠ 这个脚本存在的唯一理由是「**绝不覆盖别人写的东西**」：
   另一个作者正在往同一个文件里追加第 21~30 课，两边都写同一个文件
   就会互相覆盖。所以这里的规矩是：

     ① **每次运行都重新读一遍**目标文件（不缓存、不用上次读到的内容）
     ② 把原文的每一个字符**原样**放回输出（逐字校验，不一致就拒绝写入）
     ③ 只在自己那份内容**后面追加**；已经存在的课号**跳过**，不重复加
     ④ 用 Python 直接 `open(..., "w", encoding="utf-8", newline="")` 写，
        不用 PowerShell 的 Set-Content / -replace（那会毁掉编码）
"""
import io
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
D = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode\_build"
目标 = os.path.join(D, "_c20_40.txt")
草稿 = [os.path.join(D, "_c31_40_试验", "draft_31_35.txt"),
        os.path.join(D, "_c31_40_试验", "draft_36_40.txt")]

想要 = sys.argv[1:] or [str(i) for i in range(31, 41)]


def 读课们(路径):
    """→ [(课号, 整块文本)]，块尾不留空行。"""
    出, 当前, 行们 = [], None, []
    for 行 in io.open(路径, encoding="utf-8-sig").read().split("\n"):
        if 行.startswith("@@@ "):
            if 当前 is not None:
                出.append((当前, "\n".join(行们).rstrip("\n")))
            当前, 行们 = 行[4:].strip()[:2], [行]
        elif 当前 is not None:
            行们.append(行)
    if 当前 is not None:
        出.append((当前, "\n".join(行们).rstrip("\n")))
    return 出


候选 = {}
for f in 草稿:
    for 号, 块 in 读课们(f):
        候选[号] = 块

# ── ① 重新读一遍目标文件（每次都读，不缓存）──
原文 = io.open(目标, encoding="utf-8").read()
已有 = [行[4:].strip()[:2] for 行 in 原文.split("\n") if 行.startswith("@@@ ")]
print("  目标文件现状：%d 课 → %s" % (len(已有), " ".join(已有)))
print("  原文 %d 字，末行是 %r" % (len(原文), 原文.split("\n")[-1][:40]))

# ── ② 决定要追加哪些 ──
要加 = [号 for 号 in 想要 if 号 not in 已有]
跳过 = [号 for 号 in 想要 if 号 in 已有]
if 跳过:
    print("  ⚠ 这些课已经在文件里了，**跳过不重复加**：%s" % " ".join(跳过))
if not 要加:
    print("  没有要加的课。什么都不做。")
    sys.exit(0)
缺 = [号 for 号 in 要加 if 号 not in 候选]
if 缺:
    print("  XX 草稿里找不到这些课：%s" % " ".join(缺))
    sys.exit(1)

追加 = "\n".join(候选[号] for 号 in 要加)
新文 = 原文.rstrip("\n") + "\n" + 追加 + "\n"

# ── ③ 动笔之前先逐字校验：原文必须一字不差地在里面 ──
去尾 = 原文.rstrip("\n")
if not 新文.startswith(去尾):
    print("  XX 拒绝写入：新内容的前缀和原文对不上（原文没有原样保留）")
    sys.exit(1)
去掉我加的 = 新文[len(去尾):]
for 号 in 要加:
    if 候选[号] not in 去掉我加的:
        print("  XX 拒绝写入：第 %s 课没进到新内容里" % 号)
        sys.exit(1)
print("  自检：原文 %d 字原样保留 ✓，新加 %d 课，各块都在 ✓" % (len(去尾), len(要加)))

# ── ④ 写 ──
io.open(目标, "w", encoding="utf-8", newline="").write(新文)

# ── ⑤ 写完回读，再核一遍 ──
回读 = io.open(目标, encoding="utf-8").read()
回读课 = [行[4:].strip()[:2] for 行 in 回读.split("\n") if 行.startswith("@@@ ")]
if not 回读.startswith(去尾):
    print("  XX 回读发现原文变了！")
    sys.exit(1)
if 回读.rstrip("\n") != 新文.rstrip("\n"):
    print("  XX 回读和写出去的不一致")
    sys.exit(1)
print("  写入完成：%d 课 → %s" % (len(回读课), " ".join(回读课)))
print("  这次加了：%s" % " ".join(要加))
