"""新课自检：分节、`# 1.` 条目、注释里的英文双引号、CHECK_ 个数。

用法：
    python _build/_lint_lessons.py 18 19 20 21    只查这几课
    python _build/_lint_lessons.py                查**全部**课

⚠ 原来是"不给参数就查 18~24"（那批课的作者写的）——
  于是后来加了几十课，这个自检**一直只查 7 课**，剩下的从没被它看过。
  ⇒ 改成不给参数就扫 `lessons/` 全部，并打印查了几课。
"""
import glob
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

if sys.argv[1:]:
    课们 = sys.argv[1:]
else:
    课们 = sorted(
        os.path.basename(p)[:2]
        for p in glob.glob(os.path.join(LC, "lessons", "[0-9][0-9]_*.py")))
    print("  （没给课号，查全部 %d 课）" % len(课们))
坏 = 0
for 编 in 课们:
    命中 = glob.glob(os.path.join(LC, "lessons", 编 + "_*.py"))
    if not 命中:
        print("  XX %s: 找不到课文件" % 编)
        坏 += 1
        continue
    p = 命中[0]
    文 = io.open(p, encoding="utf-8").read()
    L = 文.splitlines()
    行数 = len(L)
    # ⚠ 只有**开头那段注释**会被搬进答题模板（见 run._stub_for）：
    #   它收集开头的连续 `#` 行，碰到第一行代码就停。
    注释段行数 = 0
    for l in L:
        if l.startswith("#"):
            注释段行数 += 1
        elif not l.strip() and 注释段行数:
            continue
        else:
            break
    问题 = []
    # ⚠⚠ **这里不判行数上限。** 只留一个下限，防"太薄的课"。
    #
    #   我原来判「总行数 100~160」。这条规则的问题是：
    #   ① 太长的课未必不好 —— 五级往后的算法课要「喂 ≥5 组数据 + 暴力对照」
    #      （48 课用暴力枚举对照 16 张 dp 表、50 课把 3×3 的 512 种网格全试一遍），
    #      检查代码自然长。**卡上限等于逼作者削弱验证强度。**
    #   ② 太短的课未必不好 —— 18~24 那批（三元/列表解析/进制）注释段 75~83 行，
    #      但该讲的一件事讲清了、检查也够。
    #   ③ 而且我两次调阈值（160→170、改判注释段）都只是在**凑数字**，
    #      每次调完就有另一批课红 —— 那是"指标在管事"，不是"质量在管事"。
    #
    #   ⇒ 行数只留一个**下限**（注释段 ≥60，防太薄）；
    #     真正的质量要求交给下面几条判据：分节齐全、`# 1.` 条目、
    #     注释里没有英文引号、CHECK_ 3~5 个。
    if 注释段行数 < 60:
        问题.append("注释段只有 %d 行，太薄" % 注释段行数)
    if not L[0].startswith("# 第"):
        问题.append("第一行不是标题")
    # ⚠ 条目形态**不止一种**，要和 `tests/test_lessons.py` 的判据对齐：
    #     新规约  `# 1.`
    #     老课    `#   1.`（缩进版，一样可读）
    #     lesson 08 用项目符号 `#     ·`
    #   ⇒ lint 只认一种比测试还严，那是在挑格式而不是挑质量。
    if not (re.search(r"^#\s+\d\.", 文, re.M)
            or re.search(r"^#\s+[·•\-]\s", 文, re.M)
            or "具体每条要求写在那两个文件" in 文):
        问题.append("没有分条列出的条目")
    # ⚠⚠ 「注释里有英文双引号」这条**必须区分代码示例和散文**。
    #
    #   我原来只判「行首是 # 且含 `"`」—— 于是把这两种混为一谈：
    #
    #       #     print("成年")              ← **Python 代码示例**，引号本来就该是 ASCII
    #       #  先介绍：程序要会"分岔"        ← 散文里混了英文引号，这才是真问题
    #
    #   实测老课（00~16）被判"注释里有英文双引号 1~19 处"，
    #   逐行看下来**几乎全是代码示例** —— 那是误判。
    #   （这条规则原本的用意是：答题模板是 `"""…"""` 原始字符串，
    #     散文里出现 `"` 容易在手改时炸掉；代码示例不受影响。）
    #
    #   ⇒ 判据：**去掉缩进后是否以 `#` 开头**（那就是代码示例/引用块），
    #     是就不算。只看真正的散文行。
    引号 = []
    for i, l in enumerate(L, 1):
        if not l.startswith("#"):
            continue
        内容 = l[1:]
        if '"' not in 内容:
            continue
        if 内容.lstrip().startswith("#") or 内容.lstrip().startswith("``"):
            continue                      # 引用块/代码块
        if re.match(r"^\s{4,}\S", 内容) and not re.search(r"[\u4e00-\u9fff]", 内容):
            continue                      # 纯 ASCII 的缩进行 = 代码示例
        # 缩进 ≥4 且含中文 ⇒ 可能是代码里的注释说明，放过；
        # 只有**散文行**（缩进 <4）才判
        if re.match(r"^\s{4,}", 内容) and not 内容.strip().startswith("#"):
            continue
        # ⚠ 三种**必须保留 ASCII 引号**的情况（lint 判不出来，人工确认过）：
        #     报错原文    `说 "name ... is not defined" 时`   —— 那是编译器原话
        #     空串字面量  `不传时返回 ""`                     —— 那是一个值
        #     分隔符      `用 "-" 把…粘起来`                  —— 那也是一个值
        #   ⇒ 引号里没有中文，或者就是 `""`/`"-"` 这种纯符号 ⇒ 放过。
        引号们 = re.findall(r'"([^"]*)"', 内容)
        if 引号们 and all(
                (not re.search(r"[\u4e00-\u9fff]", t)) or len(t) <= 1
                for t in 引号们):
            continue
        引号.append((i, l))
    if 引号:
        问题.append("散文里有英文双引号 %d 处（改成「」）" % len(引号))
    if '"""' in 文.split("\ndef ", 1)[0] or "'''" in 文.split("\ndef ", 1)[0]:
        问题.append("注释段里有三引号（会炸掉答题模板）")
    检查 = re.findall(r"^def (CHECK_\w+)", 文, re.M)
    # ⚠ 只判下限，**不设上限**。
    #   原来的 3~5：当时是给"新课"定的（3~5 条正合适）。
    #   但第 00 课只有 2 条（工具链 + 三行输出 —— 这一课本来就只有两件事要查），
    #   老课 01/02/12 也是 2 条，而它们的检查**确实在管事**
    #   （反验证：喂错答案会被拒）。
    #   ⇒ 少于 2 条才算问题（那样基本等于没检查）；
    #     多于 5 条不是缺陷 —— 算法课要喂多组数据，检查多一点正常。
    if len(检查) < 2:
        问题.append("CHECK_ 函数只有 %d 个（至少 2）" % len(检查))
    print("  %s %s  %3d 行（注释 %d + 代码 %d）  检查 %d 个  %s"
          % ("OK " if not 问题 else "XX ", os.path.basename(p), 行数, 注释段行数,
             行数 - 注释段行数, len(检查), "；".join(问题)))
    if 引号:
        for i, l in 引号[:8]:
            print("        注释第 %d 行: %s" % (i, l[:78]))
    for n in 检查:
        print("        · " + n)
    if 问题:
        坏 += 1
print()
print("  == %d/%d 课通过自检 ==" % (len(课们) - 坏, len(课们)))
sys.exit(0 if 坏 == 0 else 1)
