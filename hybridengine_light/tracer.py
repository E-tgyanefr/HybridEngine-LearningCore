"""把一段代码**逐行走一遍**，录下每一步的行号和变量 —— 互动动画的引擎。

═══ 用户原话：「课程内容直接看文字会太干，加一些互动性动画」 ═══

⚠⚠ 什么叫"互动性动画"（我想清楚才动手的）：

  ❌ 不是"排版好看的静态文字" —— 那是上一轮做的，不够。
  ❌ 不是"装饰性动效"（淡入、转圈）—— 那是"看起来在动"，不是"看懂"。

  ✅ 是**让程序执行过程可见**：学员按一下，看到
       · 现在跑到哪一行（高亮那一行）
       · 变量现在是什么值（并看到它变）
       · 走到了哪个分支 / 转了几圈
     这才是"辅助学习"，而不是"看起来不干"。

  ⚠ 为什么这个能把"干"治好：看文字是**被动的**，
    而看变量在眼前变化是**主动的推理** —— 学员会自己预测
    "下一步会走 else 吧"，然后按一下验证。那个"预测→验证"的循环
    就是学习本身。

═══ 实现怎么做（只用标准库）═══

  `sys.settrace` + `frame.f_lineno` —— 标准库里唯一的逐行钩子。
  路径：编译 → 起追踪 → 执行 → 每一步记一行 → 收工。

  ⚠⚠ 四条硬约束（都是这类"跑别人代码"的功能必须有的）：

    ① **必须有步数上限**。学员的代码里可能有 `while True:` ——
       没有上限的话界面直接冻死（`settrace` 让每行都回到 Python 层，
       比原速慢几十倍）。
    ② **必须捕获 stdout**。课程代码里有 `print`，不拦的话会打到
       控制台，而这个界面根本没有控制台。
    ③ **必须捕异常**。课程里的示例**故意**写了错的（`if 分数 = 60`
       那种 ❌ 的例子），那是教学内容，不是 bug。要录成"这一步抛了
       什么错"，而不是让录制崩掉。
    ④ **只录自己的帧**。`settrace` 会把**所有**被调用函数的每一行
       都喂过来（包括 `print` 内部的几千行）。
       ⇒ 要按 `frame.f_code.co_filename` 过滤，只留学员那段。
       ⚠ 这一条不做的后果很具体：`print("x")` 一步会变成几百步。
"""
from __future__ import annotations

import io
import sys
import types

#: 一次录制最多多少步。⚠ 超了就停下并**如实告诉学员**，
#: 而不是假装跑完了 —— 那个"没跑完"本身是有用信息（"你这循环太大"）。
最多步 = 2000


class 一步:
    """一个执行步：第几行、当时的变量、当时的输出。"""

    __slots__ = ("行", "变量", "输出", "事件", "错误")

    def __init__(self, 行: int, 变量: dict, 输出: str, 事件: str = "line",
                 错误: str = ""):
        self.行 = 行
        self.变量 = 变量
        self.输出 = 输出
        self.事件 = 事件
        self.错误 = 错误

    def __repr__(self):
        return "<步 行%d %s>" % (self.行, self.变量)


class 录制:
    """一段代码的执行轨迹。"""

    def __init__(self):
        self.步们: list[一步] = []
        self.源码 = ""
        self.行们: list[str] = []
        self.完整 = True          #: 是不是真跑完了（没撞步数上限）
        self.错 = ""              #: 顶层异常（课程里的 ❌ 例子会有）

    def 行号范围(self):
        return (1, len(self.行们))


def 录(源码: str, 最多: int = 最多步) -> 录制:
    """把 `源码` 跑一遍，录下每一步。**不抛异常**。

    ⚠ 返回值里 `完整=False` 表示撞了步数上限（大概率是死循环），
      调用方要把这件事**显示出来** —— 那是给学员的信号，不是错误。
    """
    果 = 录制()
    果.源码 = 源码
    果.行们 = 源码.split("\n")

    try:
        码 = compile(源码, "<互动>", "exec")
    except SyntaxError as e:
        果.完整 = True
        果.错 = "%s（第 %s 行）" % (e.msg, e.lineno)
        return 果

    局 = {"__name__": "__main__", "__builtins__": __builtins__}
    缓冲 = io.StringIO()
    步数 = [0]
    停 = [False]

    def 追(frame, event, arg):
        #: ⚠ 只认这一段代码的帧（见 docstring 的约束 ④）
        if frame.f_code.co_filename != "<互动>":
            return None
        if event != "line":
            return 追
        if 停[0]:
            return None
        步数[0] += 1
        if 步数[0] > 最多:
            停[0] = True
            果.完整 = False
            raise _超了()
        #: 变量快照：排除模块自带的几个
        变量 = {}
        for k, v in frame.f_locals.items():
            if k.startswith("__"):
                continue
            变量[k] = _好看(v)
        果.步们.append(一步(frame.f_lineno, 变量, 缓冲.getvalue()))
        return 追

    class _超了(Exception):
        pass

    原来 = sys.stdout
    sys.stdout = 缓冲
    sys.settrace(追)
    try:
        执行 = types.FunctionType(码, 局)
        try:
            执行()
        except _超了:
            pass
        except BaseException as e:
            果.错 = "%s: %s" % (type(e).__name__, e)
    finally:
        sys.settrace(None)
        sys.stdout = 原来

    #: 最后补一步"结束" —— 界面上要能看到终值
    终 = {k: _好看(v) for k, v in 局.items() if not k.startswith("__")}
    果.步们.append(一步(len(果.行们), 终, 缓冲.getvalue(), 事件="结束"))
    return 果


def _好看(v, 深: int = 0) -> str:
    """把值转成**短**字符串 —— 界面上那一栏放不下长东西。

    ⚠ 三条：
      · 字符串带引号（和代码里的字面量一致，学员好对照）
      · 容器截断到 6 个元素（`range(1000)` 不能原样显示）
      · 有递归深度上限（自引用的 list 会无限展开）
    """
    if 深 > 2:
        return "…"
    if isinstance(v, str):
        s = v if len(v) <= 24 else v[:21] + "…"
        return "'%s'" % s
    if isinstance(v, bool) or v is None or isinstance(v, (int, float)):
        return repr(v)
    if isinstance(v, (list, tuple, set, frozenset, dict)):
        左, 右 = ("[", "]") if isinstance(v, list) else \
                 ("(", ")") if isinstance(v, tuple) else \
                 ("{", "}") if isinstance(v, (set, frozenset)) else ("{", "}")
        项 = list(v.items()) if isinstance(v, dict) else list(v)
        出 = []
        for i, x in enumerate(项[:6]):
            if isinstance(v, dict):
                出.append("%s: %s" % (_好看(x[0], 深 + 1), _好看(x[1], 深 + 1)))
            else:
                出.append(_好看(x, 深 + 1))
        if len(项) > 6:
            出.append("…共%d个" % len(项))
        return 左 + ", ".join(出) + 右
    if callable(v):
        return "<函数 %s>" % getattr(v, "__name__", "?")
    s = repr(v)
    return s if len(s) <= 24 else s[:21] + "…"
