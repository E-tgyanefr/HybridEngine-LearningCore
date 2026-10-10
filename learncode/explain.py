"""learncode.explain —— 把 Python 报错翻译成人话。

════ 为什么这是核心功能，而不是附属功能 ════

新手放弃编程最常见的时刻不是"写不出来"，而是**报错看不懂**。
刚学一天的人看到下面这行，唯一能做的就是关掉窗口：

    TypeError: unsupported operand type(s) for +: 'int' and 'str'

它说的其实是"你拿数字去加了文字"。但这句话里没有一个词是新手认识的。

所以这个模块做的事很简单：**认出常见的错误 → 说清哪里错了 → 给出下一步**。
每条解释都遵循同一个结构：

    发生了什么      （一句话，不含术语）
    出在哪一行      （直接指出）
    怎么改          （给能直接用的写法）
    顺便记住        （一条可迁移的知识，而不是就事论事）

最后一条是它区别于"搜索引擎"的地方：新手查一次报错应该**学到一点东西**，
而不是只把这一个坑填上。

════ 不做什么 ════

· 不猜测意图去自动改代码 —— 那样新手学不到东西，而且会改错。
· 不吞掉原始报错 —— 原始信息始终保留在末尾（老师/搜索要用）。
"""

from __future__ import annotations

import re
import traceback

__all__ = ["explain", "friendly", "run_safely"]


# ── 每条规则：先认出错误类型与消息，再给出人话解释 ──
# 写成"按顺序匹配"，所以**具体的放前面、笼统的放后面**。
def _rules(exc: BaseException, tb_text: str) -> list[str]:
    name = type(exc).__name__
    msg = str(exc)
    out: list[str] = []

    if name == "NameError":
        m = re.search(r"name '(\w+)' is not defined", msg)
        who = m.group(1) if m else "某个名字"
        out += [
            f"你用了「{who}」，但 Python 不知道它是什么。",
            "",
            "三种常见原因，对照一下：",
            f"  1. 拼错了 —— 检查大小写，Python 区分 Big 和 big。",
            f"  2. 用在了定义之前 —— 变量要先赋值再用。",
            f"  3. 忘了引号 —— 想写文字要加引号：\"{who}\"，不加引号 Python 会当成变量名去查。",
            "",
            "怎么改：想清楚「这是个变量，还是一段文字」。",
            "  是文字 → 写成 \"%s\"（带引号）" % who,
            "  是变量 → 往上找它在哪里赋值的；没赋值过就先赋值。",
            "",
            "顺便记住：Python 里**名字必须先用等号赋过值**才存在。",
        ]

    elif name == "SyntaxError":
        out += [
            "Python 读不懂这行，所以整个文件都没运行。",
            "",
            "最常见的是这几处：",
            "  · 少了冒号 —— if / for / while / def 那一行的末尾要有 :",
            "  · 括号没配对 —— ( [ { 各有各的闭合符号",
            "  · 用了中文标点 —— 中文的 （）  “”  ： 在代码里都不合法",
            "",
            "怎么改：看报错指出的那一行，以及**它的上一行**（漏冒号常报到下一行）。",
            "",
            "顺便记住：语法错误是唯一会阻止整个文件运行的错误，其它错误都是运行到才发作。",
        ]

    elif name == "IndentationError":
        out += [
            "缩进不对。Python 靠**行首的空格**来判断代码的层级。",
            "",
            "规则就两条：",
            "  · 属于 if / for / while / def 内部的代码，要往右缩进",
            "  · 同一个块里，每行缩进的空格数必须一致（推荐统一 4 个空格）",
            "",
            "怎么改：把报错那行和它上一行的缩进对齐。",
            "注意：**不要混用 Tab 和空格**——肉眼看不出差别，Python 会报错。",
            "",
            "顺便记住：别的语言用大括号 {} 划分代码块，Python 用缩进。",
        ]

    elif name == "TypeError" and "unsupported operand" in msg:
        m = re.search(r"for \+: '(\w+)' and '(\w+)'", msg)
        if m:
            a, b = m.group(1), m.group(2)
            out += [
                f"你拿 {a} 和 {b} 相加了 —— 这两样东西不能加。",
                "",
                "Python 里加号有两种意思：",
                "  数字 + 数字 → 算加法（1 + 2 得 3）",
                "  文字 + 文字 → 拼起来（\"a\" + \"b\" 得 \"ab\"）",
                "但**数字和文字不能相加**，因为 Python 不知道该算加法还是拼起来。",
                "",
                "怎么改（挑一个）：",
                "  · 想拼成文字 → 先把数字转成文字：\"你考了\" + str(95) + \"分\"",
                "  · 想用更省事的写法 → f\"你考了{95}分\"   （推荐，这叫 f-string）",
                "  · 想算数学 → 确认两边都是数字（把 input() 的结果用 int() 转一下）",
                "",
                "顺便记住：**input() 拿到的永远是文字**，哪怕输入的是 100。要算数先 int()。",
            ]
        else:
            out += [
                "这两种类型的东西不能用这个运算符。",
                "",
                "怎么改：确认两边的类型一致。用 type(x) 打印看看它到底是什么。",
            ]

    elif name == "TypeError" and "can only concatenate str" in msg:
        out += [
            "你想把文字和别的东西用 + 拼起来，但 + 只认两边都是文字。",
            "",
            "怎么改：优先用 f-string，比手动转好写也不容易错：",
            "  ❌ \"结果是 \" + 结果",
            "  ✅ f\"结果是 {结果}\"",
            "",
            "顺便记住：f\"...{变量}...\" 会把变量自动变成文字，是 Python 里最常用的拼接方式。",
        ]

    elif name == "TypeError" and "takes" in msg and "positional argument" in msg:
        out += [
            "你调用函数时，给的参数个数不对。",
            "",
            "怎么改：看函数定义那一行，数清它要几个参数，再数你给了几个。",
            "常见情况：定义写了 def f(a, b)，调用却只写了 f(1)。",
            "",
            "顺便记住：报错里的 \"takes 2 positional arguments but 3 were given\" 意思是"
            "「要 2 个但你给了 3 个」。",
        ]

    elif name == "IndexError":
        out += [
            "你要的位置超出了范围。",
            "",
            "关键在于：**Python 的位置从 0 开始数**。",
            "  一个有 3 个元素的列表，合法位置是 0、1、2 —— 没有 3。",
            "",
            "怎么改：先打印长度看看 len(x)，最后一个元素的写法是 x[-1]（不用算下标）。",
            "",
            "顺便记住：x[-1] 是最后一个，x[-2] 是倒数第二个；这个写法不会算错。",
        ]

    elif name == "KeyError":
        out += [
            "字典里没有这个键。",
            "",
            "怎么改：",
            "  · 先看看有哪些键：print(d.keys())",
            "  · 不确定有没有 → 用 d.get(\"键\", 默认值)，没有也不会崩",
            "  · 注意**键是区分大小写、也要带引号**的",
            "",
            "顺便记住：d[\"x\"] 取不到会报错，d.get(\"x\") 取不到给 None —— 后者更安全。",
        ]

    elif name == "ZeroDivisionError":
        out += [
            "除数是 0。数学上除以 0 没有意义，Python 也一样。",
            "",
            "怎么改：除之前先判断一下：",
            "  if 除数 != 0:",
            "      结果 = 被除数 / 除数",
            "",
            "顺便记住：报错说“除数”，不是“被除数”。a / b 里 b 是除数。",
        ]

    elif name == "FileNotFoundError":
        out += [
            "找不到这个文件。",
            "",
            "怎么改：注意两件事 ——",
            "  · 文件名和路径是否拼对了（含扩展名 .txt / .csv）",
            "  · **程序的工作目录**是哪里。相对路径是相对“你运行程序时所在的目录”，"
            "不是相对这个 .py 文件。",
            "",
            "顺便记住：不确定就先打印 import os; print(os.getcwd())。",
        ]

    elif name == "AttributeError":
        m = re.search(r"'(\w+)' object has no attribute '(\w+)'", msg)
        if m:
            out += [
                f"你把 {m.group(1)} 当成了有 {m.group(2)} 这个东西的对象，但它没有。",
                "",
                "怎么改：",
                "  · 拼错了？Python 区分大小写。",
                "  · 类型不对？用 type(x) 看看它到底是什么。",
                "    常见坑：字符串没有 append（那是列表的方法）；",
                "    列表没有 upper（那是字符串的方法）。",
                "",
                "顺便记住：报错里的 \"object has no attribute\" 就是「这个东西没有这个功能」。",
            ]
        else:
            out += ["你调用了某个对象没有的功能。用 dir(x) 可以列出它到底有什么。"]

    elif name in ("ModuleNotFoundError", "ImportError"):
        m = re.search(r"No module named '([\w\.]+)'", msg)
        who = m.group(1) if m else "某个模块"
        out += [
            f"没有找到叫 {who} 的模块。",
            "",
            "怎么改：",
            f"  · 如果要装它 → 在终端运行：pip install {who}",
            "  · 已经装了还报错 → 确认你用的 python 和装它的那个是同一个",
            "  · 是你自己写的文件？确认文件名拼对了、和这个 .py 在同一个目录",
            "",
            "顺便记住：import 失败几乎都是「没装」或「装到了另一个 Python 里」。",
        ]

    elif name == "ValueError" and "invalid literal for int()" in msg:
        out += [
            "你想把一段文字转成整数，但那段文字不是数字。",
            "",
            "最常见的来源：input()。用户输入了字母、或者空着直接回车。",
            "",
            "怎么改：先确认拿到的是不是数字：",
            "  用 int(input(\"...\")) 之前，先试试 x = input(); print(repr(x)) 看它到底是什么。",
            "  要允许小数 → 用 float() 代替 int()。",
            "",
            "顺便记住：int(\"3.5\") 也会报这个错（小数点不算整数），要用 float(\"3.5\")。",
        ]

    return out


def explain(exc: BaseException, filename: str | None = None) -> str:
    """把一个异常变成一段给人看的解释。返回可直接 print 的字符串。"""
    tb_text = "".join(traceback.format_exception(type(exc), exc, exc.__traceback__))

    # 找出"用户代码"里出错的行号与内容 —— 这是最能帮上忙的一条信息
    where = ""
    for fr in reversed(traceback.extract_tb(exc.__traceback__)):
        if filename and fr.filename != filename:
            continue
        where = f"  第 {fr.lineno} 行： {fr.line}\n"
        break

    lines = [
        "",
        "─" * 56,
        f"出了点问题：{type(exc).__name__}",
        "─" * 56,
    ]
    if where:
        lines += ["出在这里：", where.rstrip("\n"), ""]

    body = _rules(exc, tb_text)
    if body:
        lines += body
    else:
        # 没认出来 —— 那就诚实地给通用建议，而不是编一段像模像样的解释
        lines += [
            "这个错误我没有内置解释（我的规则表里没有它）。",
            "",
            "通用的排查办法：",
            "  1. 看报错最后一行 —— 那才是错误类型和原因（前面的都是调用路径）",
            "  2. 在出错那行前面 print() 一下相关变量，看它们到底是什么",
            "  3. 把报错原文复制去搜索（下面就是原文，方便复制）",
        ]

    lines += ["", "原始报错（老师或搜索需要用到的原文）：", "─" * 56, tb_text.rstrip()]
    return "\n".join(lines)


def friendly(fn):
    """装饰器：让这个函数抛出的异常自动被翻译成人话再抛出。

    用法::

        @friendly
        def main():
            ...

        main()          # 出错时打印人话，而不是天书
    """
    def wrapper(*a, **kw):
        try:
            return fn(*a, **kw)
        except Exception as e:                       # noqa: BLE001 —— 这里就是要兜住全部
            print(explain(e))
            raise SystemExit(1) from None
    wrapper.__name__ = getattr(fn, "__name__", "wrapper")
    wrapper.__doc__ = fn.__doc__
    return wrapper


def run_safely(fn, *a, **kw):
    """跑一个函数；出错就打印人话解释并返回 None（不抛异常）。

    给"课程/练习"用：新手做错时看到解释，而不是一串栈。
    """
    try:
        return fn(*a, **kw)
    except Exception as e:                           # noqa: BLE001
        print(explain(e, filename=getattr(fn, "__code__", None) and fn.__code__.co_filename))
        return None
