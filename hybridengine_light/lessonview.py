"""把课程正文**渲染**成有层次的界面（不是把原始文本倒进 Text）。

═══ 用户原话：「课程部分只有文字，太干了」 ═══

⚠⚠ **根因**：桌面版把 `M.看课()` 的输出**原样**塞进 `tk.Text`，而那份输出是
   带 `# ` 前缀的原始文件格式：

       # 第 00 课：跑通第一个程序 —— 先确认你能把程序跑起来
       # ─────────────────────────────────────────────────────
       #  这一课你会学到
       #    怎么跑一个 Python 程序，以及 print 和它的括号引号
       #  需要先会
       #    没有 —— 这是第一课

   实测确认：正文控件的 `tag_names()` 只有 `('sel',)` —— **一个样式标签都没有**。
   于是 154 行、4296 字全是**同一种字、同一种颜色、同一种粗细**。
   那不是"朴素"，那是**没有排版**。

⚠ 对比：手机版早就把它渲染成干净正文了（`make_course_data.py` 里的
  `去掉注释前缀`）。桌面版一直没跟上 —— 所以同一个课程，手机上比电脑上好看。

═══ 这一层做什么 ═══

**只换呈现，一个字不改**（作者的硬要求）。把正文按行分类，交给界面打标签：

    分节标题   `--- 这一课你会学到 ---` 之间的那行   ⇒ 强调色 + 加粗 + 上方留白
    分隔线     `──────` → 一条 1px 细线            ⇒ 而不是一串长破折号
    代码行     缩进 >= 4 格                          ⇒ 等宽 + 深底 + 左侧色条
    坑         `⚠` 开头                              ⇒ 左边黄条 + 淡黄底
    步骤       `1.` / `2.` 开头                       ⇒ 编号加重
    行内代码   `` `if` ``                            ⇒ 等宽 + 底色
    加粗       `**缩进**`                             ⇒ 真加粗
    成功/失败  `✅` / `❌`                            ⇒ 绿 / 红

⚠ 为什么按**缩进**判代码行：这是课程作者本来的排版方式
  （`#     if 条件:` 里的 4 个空格就是"这是代码"）。
  量过数据：正文里缩进 >= 4 格的有 35 行 / 154 行，那些确实都是代码。
  ⚠ 但**同一份文本在手机版上不能这么判** —— 手机版量出"缩进 >= 4 的行里
    有 2940 行像散文"，所以那边整篇用等宽。
    两边判断不同是**对的**：桌面有 `Text` 的标签能力，可以把代码
    "框出来"；手机做不到，只能整篇等宽。**同一个判断在两种渲染能力下
    结论不同，这不是不一致。**
"""
from __future__ import annotations

import re
import tkinter as tk

from . import theme as T

#: `# ─────` 这种分隔线（去掉 `# ` 之后是一串 `─`）
_分隔线 = re.compile(r"^[─\-=—_]{3,}\s*$")
#: 行首的 `#` 注释符（课程文件里每行都有）
_注释符 = re.compile(r"^#\s?")
#: `**加粗**`
_加粗 = re.compile(r"\*\*(.+?)\*\*")
#: 行内 `` `代码` ``
_行内码 = re.compile(r"`([^`]+)`")
#: 编号条：`1. 打印…` / `第 1 步`
_编号 = re.compile(r"^(\d+[.、])\s*")


def 去注释(行: str) -> str:
    """去掉行首的 `# `，**保留缩进**。

    ⚠ 只去掉注释符本身（`#` 和紧跟的一个空格），**一个字都不删**。
      缩进必须留着 —— 它是这份文本的排版手段（见模块 docstring）。
    """
    return _注释符.sub("", 行)


def 分类(行: str) -> tuple[str, str]:
    """返回 (类别, 去掉注释符之后的文本)。

    类别：`空` / `线` / `节标题` / `坑` / `码` / `步骤` / `正文` / `标题`
    """
    净 = 去注释(行).rstrip()
    身 = 净.strip()
    if not 身:
        return "空", ""
    if _分隔线.match(身):
        return "线", ""
    #: 第一行是课名（`第 07 课：…`），单独给个"标题"类别
    if re.match(r"^第\s*\d+\s*课", 身) and len(身) < 60:
        return "标题", 身
    if 身.startswith("⚠"):
        return "坑", 净
    缩 = len(净) - len(净.lstrip(" "))
    if 缩 >= 4:
        return "码", 净[4:] if 净.startswith("    ") else 净.lstrip()
    if _编号.match(身):
        return "步骤", 净
    return "正文", 净


def 建标签(控件: tk.Text):
    """给一个 `Text` 控件建好这套标签。重复调用是安全的（幂等）。

    ⚠ 标签名用**中文短名**（`节` / `码` / `坑`…）—— 这个文件里到处是中文标识符，
      保持一致。Tk 的标签名只要不是保留字就行。
    """
    码字 = T.等宽字体(T.字_正文)
    正文 = T.界面字体(T.字_正文)

    控件.tag_configure("标题", font=T.界面字体(T.字_标题, 粗=True),
                     foreground=T.文字, spacing3=6)
    控件.tag_configure("节", font=T.界面字体(T.字_正文, 粗=True),
                     foreground=T.强调, spacing1=14, spacing3=4)
    #: 小标题（`需要先会` 这种分组名）—— 比正文亮一点、粗一点，
    #: 但**不用强调色**：那是留给分节标题的，用两次就不分主次了
    控件.tag_configure("小标题", font=T.界面字体(T.字_正文, 粗=True),
                     foreground=T.文字次, spacing1=8, spacing3=3)
    控件.tag_configure("正文", font=正文, foreground=T.文字, spacing3=3)
    控件.tag_configure("坑", font=正文, foreground=T.警示,
                     background=T.坑底, lmargin1=8, lmargin2=8,
                     rmargin=6, spacing1=4, spacing3=4)
    控件.tag_configure("码", font=码字, foreground=T.码_其余,
                     background=T.码底, lmargin1=14, lmargin2=14,
                     spacing1=1, spacing3=1)
    控件.tag_configure("步骤", font=正文, foreground=T.文字次,
                     lmargin1=12, lmargin2=22, spacing3=2)
    控件.tag_configure("行内码", font=码字, foreground=T.码_字符串,
                     background=T.浮起)
    控件.tag_configure("粗", font=T.界面字体(T.字_正文, 粗=True),
                     foreground=T.文字)
    控件.tag_configure("好", foreground=T.好)
    控件.tag_configure("坏", foreground=T.坏)
    控件.tag_configure("空", spacing3=8)
    #: 分节线：一行 1px 的横线。Text 里只能靠"下划线 + 背景"近似 ——
    #: ⚠ 这个技巧值得记：`Text` 没有 `hr`，但可以用一个
    #:   `elide=False` 的极短标记行 + `underline` 做出分隔线。
    控件.tag_configure("分节线", font=T.界面字体(1), foreground=T.线,
                     spacing1=6, spacing3=6, underline=True)


def _插行内(控件: tk.Text, 文本: str, 基类: str):
    """把一行的内容插进去，同时处理 `**加粗**` / `` `代码` `` / ✅❌。

    ⚠ 为什么要逐个片段插：`Text` 的标签是**区间**的，
      一行里"加粗两个字"意味着这一行要拆成三段插入。
      直接把整行 `insert(..., tags=...)` 做不到这件事。
    """
    位 = 0
    #: 用一个正则一次扫出所有"要特殊处理"的片段
    模式 = re.compile(r"\*\*(.+?)\*\*|`([^`]+)`|([✅❌])")
    for m in 模式.finditer(文本):
        if m.start() > 位:
            控件.insert("end", 文本[位:m.start()], 基类)
        if m.group(1) is not None:
            控件.insert("end", m.group(1), (基类, "粗"))
        elif m.group(2) is not None:
            控件.insert("end", m.group(2), "行内码")
        else:
            控件.insert("end", m.group(3), (基类, "好" if m.group(3) == "✅" else "坏"))
        位 = m.end()
    if 位 < len(文本):
        控件.insert("end", 文本[位:], 基类)


def 渲染(控件: tk.Text, 正文: str):
    """把课程正文渲染进 `控件`。**原样清空后重画**。"""
    建标签(控件)
    控件.configure(state="normal")
    控件.delete("1.0", "end")

    行们 = 正文.split("\n")
    #: `---` 之间的那一行是"节标题" —— 先扫一遍标出来
    #:
    #: ⚠ 为什么需要这一遍：正文里 `─` 分隔线**成对出现**，夹着节名：
    #:       # ─────────────────
    #:       #  这一课你会学到
    #:       # ─────────────────
    #:   单看一行分不出"这是节标题"还是"普通正文"。要看它上下是不是线。
    是节 = [False] * len(行们)
    for i in range(1, len(行们) - 1):
        if 分类(行们[i])[0] == "正文":
            if 分类(行们[i - 1])[0] == "线" and 分类(行们[i + 1])[0] == "线":
                是节[i] = True

    #: 还有一类"小标题"：**缩进 2 格的短行**，而且它下面**缩得更深**。
    #:
    #: ⚠ 为什么需要：作者的行文格式是
    #:       #  需要先会              ← 缩进 2，短，是小标题
    #:       #    第 03 课：比较那套写法  ← 缩进 4，是它的内容
    #:   实测：不判它的话，「需要先会」会混在正文里，
    #:   层次就少了一级（而这一课里这种小标题有 5 个）。
    #:
    #: ⚠ 三条都要满足，少一条就误判：短（<= 16）、不以句号/冒号结尾、
    #:   下一行缩进更深。只看"短"的话，「没有 —— 这是第一课」也会被当成标题。
    是小标题 = [False] * len(行们)
    for i in range(len(行们) - 1):
        净 = 去注释(行们[i]).rstrip()
        身 = 净.strip()
        if not 身 or len(身) > 16:
            continue
        if 身.endswith(("。", "！", "？", ":", "：")):
            continue
        if not (净.startswith("  ") and not 净.startswith("    ")):
            continue
        下 = 去注释(行们[i + 1]).rstrip()
        if 下.strip() and (len(下) - len(下.lstrip(" "))) > 2:
            是小标题[i] = True

    上一类 = None
    for i, 行 in enumerate(行们):
        类, 文 = 分类(行)
        if 类 == "空":
            if 上一类 not in (None, "空"):
                控件.insert("end", "\n", "空")
            上一类 = "空"
            continue
        if 类 == "线":
            #: ⚠ 连续的线只画一条（节标题上下那两条线，只留下面那条当分隔）
            if 上一类 != "线":
                控件.insert("end", "\n", "分节线")
            上一类 = "线"
            continue
        if 是节[i]:
            控件.insert("end", "▎", "节")
            控件.insert("end", 文 + "\n", "节")
            上一类 = "节"
            continue
        if 是小标题[i]:
            控件.insert("end", 文 + "\n", "小标题")
            上一类 = "小标题"
            continue
        if 类 == "标题":
            控件.insert("end", 文 + "\n", "标题")
        elif 类 == "码":
            控件.insert("end", 文 + "\n", "码")
        elif 类 == "坑":
            _插行内(控件, 文, "坑")
            控件.insert("end", "\n")
        elif 类 == "步骤":
            m = _编号.match(文)
            if m:
                控件.insert("end", m.group(1) + " ", ("步骤", "粗"))
                _插行内(控件, 文[m.end():], "步骤")
            else:
                _插行内(控件, 文, "步骤")
            控件.insert("end", "\n")
        else:
            _插行内(控件, 文, "正文")
            控件.insert("end", "\n")
        上一类 = 类

    #: 只读，但**可以选中复制**（学员要照着敲）
    控件.configure(state="disabled")
