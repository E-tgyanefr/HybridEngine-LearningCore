# -*- coding: utf-8 -*-
r"""审计：哪些脚本里**还写着会执行的**硬编码仓库路径。

════════════════════════════════════════════════════════════════
  为什么需要这一条
════════════════════════════════════════════════════════════════

仓库改过名（`Project\LearnCode` → `Project\引擎\LearnCode`
→ `Project\引擎\HybridEngine-LearningCore`），而 `_build\` 里有二十来个
一次性脚本**把当时的绝对路径写死在源码里**。改名之后那些路径
**指向两个都不存在的目录**：

    D:\Users\etgya\Desktop\Project\引擎\LearnCode      （不存在）
    D:\Users\etgya\Desktop\Project\HybridEngine-light  （不存在）

⇒ 它们**现在就是坏的**，但坏得**很安静**：`os.walk` 一个不存在的目录
  返回**空**（不报错），于是脚本「跑完了、没报错、什么都没做」。
  这正是项目里反复写的那一类坑（`环境坑与教训.md` 第 22 条）。

⚠ 本脚本只**审计**，不改任何东西。它把「哪些是真坏」和「哪些只是
  **注释里提到**老路径」分开 —— 后者不用改，改了反而丢历史。

用法：
    python _build/_audit_deadpaths.py            # 人类可读报告
    python _build/_audit_deadpaths.py --tsv      # 机器可读
退出码：0 = 没有「会执行的死路径」；1 = 还有。
"""
import ast
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

#: 老仓库路径的字面量（反斜杠 / 正斜杠两种写法都算）
老路径 = re.compile(r"D:[\\/]Users[\\/]etgya[\\/]Desktop[\\/]Project"
                    r"(?:[\\/]引擎)?[\\/](?:LearnCode|HybridEngine-light)")

#: 这个文件**故意**留一个不存在的绝对路径当测试夹具（验"路径不存在时的提示"），
#: 不是死路径 —— 列出来免得下一个人"修"掉它。
白名单 = {
    "_build/_verify_stub_templates.py": "文件里那行是**测试夹具**（模拟 exe 路径），不是仓库路径",
    #: 本文件自己的文档字符串里就写着那两条老路径当例子 —— 别把自己算进去。
    "_build/_audit_deadpaths.py": "本审计脚本自己的说明文字",
    #: 修复脚本里的老路径是**匹配用的正则**（`_老根`）和文档例子，
    #:  不是拿去打开文件的路径 ⇒ 不是死路径。
    "_build/_fix_deadpaths.py": "老路径在这里是匹配正则 + 说明文字，不是运行期路径",
}


def py文件():
    出 = []
    for 子 in ("_build", "tests", "hybridengine_light", "tools"):
        d = os.path.join(ROOT, 子)
        if not os.path.isdir(d):
            continue
        for 根, _, 名们 in os.walk(d):
            if "__pycache__" in 根:
                continue
            for 名 in 名们:
                if 名.endswith(".py"):
                    出.append(os.path.relpath(os.path.join(根, 名), ROOT)
                              .replace("\\", "/"))
    return sorted(出)


def 真命中的行(P):
    """(行号, 行内容, 是不是注释里) —— 只看含老路径的行。"""
    出 = []
    for i, 行 in enumerate(io.open(P, encoding="utf-8", errors="replace"), 1):
        if not 老路径.search(行):
            continue
        出.append((i, 行.rstrip()))
    return 出


def 是字符串常量(P, 行号):
    """这一行的老路径出现在**字符串字面量**里吗？

    ⚠ 不能用「行首是 #」来判断注释 —— 项目里有大量
      「代码 + 行尾注释」和「三引号文档字符串里提到老路径」。
      这里用 `ast` 拿所有字符串常量的位置，比正则靠谱
      （`环境坑与教训.md` 第 10 条：正则剥注释的顺序会咬人）。
    """
    try:
        树 = ast.parse(io.open(P, encoding="utf-8", errors="replace").read())
    except SyntaxError:
        return None                       # 解析不了 ⇒ 交给调用方当"存疑"
    for 节 in ast.walk(树):
        if isinstance(节, ast.Constant) and isinstance(节.value, str):
            if 老路径.search(节.value):
                if 节.lineno <= 行号 <= (节.end_lineno or 节.lineno):
                    return True
    return False


def main():
    tsv = "--tsv" in sys.argv
    死 = []           # 会在运行时用到老路径的
    存疑 = []
    注释 = []
    for rel in py文件():
        if not rel.endswith(".py"):
            continue
        P = os.path.join(ROOT, rel)
        行们 = 真命中的行(P)
        if not 行们:
            continue
        if rel in 白名单:
            注释.append((rel, 白名单[rel]))
            continue
        字面 = 是字符串常量(P, 行们[0][0])
        if 字面 is None:
            存疑.append((rel, 行们, "文件解析不了（SyntaxError）"))
        elif 字面:
            死.append((rel, 行们))
        else:
            注释.append((rel, "老路径只在注释里"))

    if tsv:
        for rel, 行们 in 死:
            for i, 行 in 行们:
                print("%s\t%d\t%s" % (rel, i, 行.strip()[:110]))
        return 1 if 死 else 0

    print("═" * 70)
    print("  死路径审计 —— 会**执行**的老仓库绝对路径")
    print("═" * 70)
    print("  老路径：%s" % 老路径.pattern)
    print()
    if not 死:
        print("  OK  没有一个脚本还在**运行期**用老路径。")
    else:
        print("  **%d 个脚本的字符串字面量里还有老路径**（运行时必然落空）：" % len(死))
        for rel, 行们 in 死:
            print("    %s" % rel)
            for i, 行 in 行们:
                print("        L%-4d %s" % (i, 行.strip()[:100]))
    print()
    if 存疑:
        print("  ?? 解析不了、需要人看：")
        for rel, 行们, 为什么 in 存疑:
            print("    %-46s %s" % (rel, 为什么))
    print()
    print("  -- 只在注释 / 文档字符串里提到老路径的（**不用改**，那是历史）：")
    for rel, 为什么 in 注释:
        print("    %-46s %s" % (rel, 为什么))
    print()
    print("  == 死路径 %d 处 / 存疑 %d 处 / 仅注释 %d 处 =="
          % (len(死), len(存疑), len(注释)))
    return 1 if 死 else 0


if __name__ == "__main__":
    sys.exit(main())
