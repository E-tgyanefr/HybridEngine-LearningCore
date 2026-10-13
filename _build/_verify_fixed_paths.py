# -*- coding: utf-8 -*-
r"""静态验证：改过死路径的脚本，路径**真的**指到了存在的目标。

════════════════════════════════════════════════════════════════
  为什么需要一条"静态"的
════════════════════════════════════════════════════════════════

`_run_fixed_scripts.py` 把**能跑的**那批真跑一遍（退出码 + 正向信号）。
但另外一批**不能跑**：

    · 会写 `lessons/` / `csharp/*-lessons/` 的（跑它 = 改教学内容）
    · 一次性生成器（产物已经在仓库里，重跑会把后来的手改冲掉）
    · 会动 git 状态的

⚠ 对它们**不能**说"跑过了" —— 那是假话。但也不能就写"没验"：
  路径修复本身是**可以静态验的**，而且验的正是这次的 bug 本身：

    ① **老字面量没了**：用 `ast` 取**字符串常量**里的老路径 ⇒ 必须 0 处。
       （只看常量、不看注释 —— 注释里提到老路径是历史，应当保留。）
    ② **新表达式能算出真目录**：把模块级的 `LC = …` / `S = …` / `P = …`
       那一条赋值**单独**拿出来 `eval`（`__file__` 绑定成这个文件的真路径），
       看算出来的东西**在磁盘上存不存在**，而且**在仓库根里面**。
       ⇒ 这条直接回答"改了之后它还指不指得到东西"。
    ③ 顺带确认它在**被用到之前**定义（`_fix_cs_entry.py` 的坑：
       `LC` 若插在 `S` 那一行（第 73 行），第 9 行的 `P` 就成了未定义名）。

⚠ 这三条**不能**替代"真跑"。本脚本打完会把「静态验过 / 真跑过」分开算，
  而且**明白写出来**：静态验过 ≠ 跑过。

用法：
    python _build/_verify_fixed_paths.py
退出码：0 = 31 个改了路径的文件全部通过静态验证；1 = 有文件没通过。
"""
import ast
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

_老路径 = re.compile(r"D:[\\/]Users[\\/]etgya[\\/]Desktop[\\/]Project"
                     r"(?:[\\/]引擎)?[\\/](?:LearnCode|HybridEngine-light)")

#: 只验这些模块级名字（都是这一轮改过的）
路径名 = {"LC", "ROOT", "S", "P", "PKG"}

#: 真跑过的（`_run_fixed_scripts.py` 里那批）—— 用来把两栏分开算，不混一起。
真跑过 = {
    "_build/_audit_drift.py", "_build/_cmp_discover.py", "_build/_cmp_discover2.py",
    "_build/_cmp_thread.py", "_build/_cold.py", "_build/_diff_cpp.py",
    "_build/_gesp_reference.py", "_build/_mkjava.py", "_build/_peek2.py",
    "_build/_peek_templates.py", "_build/_peek_templates2.py",
    "_build/_peek_templates3.py", "_build/_shot.py",
    "_build/_test_progress_gui.py", "_build/_time_discover.py",
    "_build/_trace2.py", "_build/_trace_gui.py", "_build/_where.py",
    "_build/_where2.py", "tests/验收.py",
}

#: 这一轮改过路径的文件（由 `_fix_deadpaths.py` 的报告给出）
改了 = sorted(真跑过 | {
    "_build/_apply_any.py", "_build/_fix_cs_class_public.py",
    "_build/_fix_cs_entry.py", "_build/_fix_cs_type_scan.py",
    "_build/_gen_c16.py", "_build/_gen_compiled.py",
    "_build/_mk_cs_course.py", "_build/_mk_cs_lessons.py",
    "_build/_rebuild_versions.py", "_build/_unify_cmdname.py",
    "_build/_unify_cs_entry.py",
})


def 常量里的老路径(树):
    出 = []
    for 节 in ast.walk(树):
        if isinstance(节, ast.Constant) and isinstance(节.value, str):
            if _老路径.search(节.value):
                出.append((节.lineno, 节.value.strip()[:70]))
    return 出


#: `os.path` 里"只算路径、没有副作用"的函数 —— 只有这些允许被求值。
#:  ⚠⚠ 为什么必须白名单：模块顶层还有 `os.makedirs(...)` 这种**会写盘**的语句。
#:    第一版对所有模块级赋值一律 `eval` —— 那等于把生成器真的跑了一部分。
#:    在这一轮的硬要求下（不许改课程文件）那是**不能接受**的。
#:    能上语法树判断就别靠"我觉得它没副作用"。
_纯函数 = {"join", "dirname", "abspath", "basename", "normpath", "realpath",
           "normcase", "split", "splitext"}


def 纯(节):
    """这个表达式是不是"只算字符串路径"？—— 白名单式判断，宁严勿宽。"""
    if isinstance(节, ast.Constant):
        return isinstance(节.value, str)
    if isinstance(节, ast.Name):
        return True
    if isinstance(节, ast.Attribute):
        return 纯(节.value)
    if isinstance(节, ast.BinOp) and isinstance(节.op, ast.Add):
        return 纯(节.left) and 纯(节.right)
    if isinstance(节, ast.Call):
        f = 节.func
        if not isinstance(f, ast.Attribute) or f.attr not in _纯函数:
            return False
        if not 纯(f.value):
            return False
        return all(纯(a) for a in 节.args) and not 节.keywords
    return False


def 模块级赋值(树):
    """源码顺序的 [(名字, 行号, 值节点, 纯不纯)]（只收单目标 `Name = …`）。"""
    出 = []
    for 节 in 树.body:
        if (isinstance(节, ast.Assign) and len(节.targets) == 1
                and isinstance(节.targets[0], ast.Name)):
            出.append((节.targets[0].id, 节.lineno, 节.value, 纯(节.value)))
    return 出


def main():
    print("═" * 72)
    print("  静态验证：改过路径的脚本 —— 老字面量没了、新路径指得到真东西")
    print("═" * 72)
    print("  仓库根：%s" % ROOT)
    print("  文件：%d 个（其中 %d 个另外真跑过）" % (len(改了), len(真跑过)))
    print()

    坏 = []
    旧字面量还在 = []
    解析不了 = []
    明细 = []

    for rel in 改了:
        P = os.path.join(ROOT, rel.replace("/", os.sep))
        if not os.path.isfile(P):
            坏.append((rel, "文件不存在"))
            continue
        文 = io.open(P, encoding="utf-8", errors="replace").read()
        try:
            树 = ast.parse(文)
        except SyntaxError as e:
            解析不了.append((rel, "L%s %s" % (e.lineno, e.msg)))
            坏.append((rel, "语法错误：L%s %s" % (e.lineno, e.msg)))
            continue

        # ① 字符串常量里不许再有老路径
        常量 = 常量里的老路径(树)
        if 常量:
            旧字面量还在.append((rel, 常量))
            坏.append((rel, "字符串常量里还有老路径 %d 处" % len(常量)))

        # ②③ 新表达式算出的东西要存在、在仓库内、且在被用之前定义
        #
        #  ⚠⚠ 必须**按源码顺序**把它们算出来，并把结果放回同一个命名空间：
        #     `S = os.path.join(LC, …)` 依赖前面的 `LC = …`，
        #     而 `_mk_cs_lessons.py` 的 `S` 还依赖 `CS`。
        #     第一版每个表达式都在**空**命名空间里求值 ⇒ 一片
        #     `NameError: name 'LC' is not defined` —— 那是**我的探针的假阳性**，
        #     不是仓库的问题。（"看着像失败"和"真的失败"要分得开。）
        赋 = {名: 行 for 名, 行, _, _ in 模块级赋值(树)}
        域 = {"os": os, "__file__": P, "__name__": "探针"}
        算得出 = []
        跳过 = []
        for 名, 行, 值, 是纯 in 模块级赋值(树):
            if not 是纯:
                #  有副作用的（`os.makedirs(...)`、`io.open(...)`）一律不求值。
                跳过.append((名, 行))
                continue
            源 = ast.get_source_segment(文, 值)
            if 源 is None:
                跳过.append((名, 行, "取不到源码片段"))
                continue
            try:
                值算 = eval(源, 域)                      # noqa: S307 —— 只对白名单纯表达式求值
            except Exception as e:
                跳过.append((名, 行, "%s: %s" % (type(e).__name__, e)))
                continue
            域[名] = 值算
            if 名 not in 路径名:
                continue
            存在 = os.path.exists(值算)
            在仓内 = os.path.normcase(os.path.abspath(str(值算))).startswith(
                os.path.normcase(ROOT))
            算得出.append((名, 行, str(值算), 存在, 在仓内))
            if not 存在:
                坏.append((rel, "%s → %s **不存在**" % (名, 值算)))
            if not 在仓内:
                坏.append((rel, "%s → %s **不在仓库里**" % (名, 值算)))
        if not 算得出:
            坏.append((rel, "没有任何 %s 形式的模块级路径赋值" % "/".join(sorted(路径名))))
        if 跳过:
            #  如实标出"有几个表达式我没敢求值" —— 静态验证的边界要说清。
            算得出.append(("（另有 %d 个非纯表达式未求值）" % len(跳过), 0, "", True, True))

        #  ③ 路径变量必须定义在第一次被用到的行**之前**（`_fix_cs_entry.py` 踩过：
        #     `P` 在第 9 行用了 `LC`，而 `LC` 被插到了第 73 行 ⇒ 一跑就 NameError）
        for 名 in ("LC", "ROOT"):
            if 名 not in 赋:
                continue
            定义行 = 赋[名]
            首次用 = None
            for 节 in ast.walk(树):
                if (isinstance(节, ast.Name) and 节.id == 名
                        and isinstance(节.ctx, ast.Load)):
                    if 首次用 is None or 节.lineno < 首次用:
                        首次用 = 节.lineno
            if 首次用 is not None and 首次用 < 定义行:
                坏.append((rel, "`%s` 在第 %d 行就被用了，却到第 %d 行才定义（NameError）"
                           % (名, 首次用, 定义行)))

        明细.append((rel, 算得出))

    在仓内 = [x for x in 明细 if x[0] in 真跑过]
    仅静态 = [x for x in 明细 if x[0] not in 真跑过]

    for 标, 组 in (("真跑过（另见 _run_fixed_scripts.py）", 在仓内),
                   ("**没跑**（会写课程文件/一次性生成器）—— 只有静态验证", 仅静态)):
        print("  ── %s ──" % 标)
        for rel, 算得出 in 组:
            print("    %s" % rel)
            for 名, 行, 值, 存在, 内 in 算得出:
                print("        L%-4d %-4s → %s   %s%s"
                      % (行, 名, 值.replace(ROOT, "<仓库>"),
                         "存在" if 存在 else "**不存在**",
                         "" if 内 else "  **不在仓库内**"))
        print()

    #  ⚠ 第一版把 `坏` 收集了却**只打印**了「老字面量」和「解析不了」两类，
    #    于是报告尾部写着 23/31，中间却一条失败原因都没有 ——
    #    「有失败」和「失败在哪」长得不一样，等于没说。这里全打出来。
    if 坏:
        print("  **不通过的 %d 个文件**：" % len({x[0] for x in 坏}))
        for rel, 为什么 in 坏:
            print("    %-40s %s" % (rel, 为什么))
        print()
    if 旧字面量还在:
        print("  **仍有可执行的老路径字面量**：")
        for rel, 常量 in 旧字面量还在:
            for 行, 文 in 常量:
                print("    %s L%d  %s" % (rel, 行, 文))
        print()
    if 解析不了:
        print("  **解析不了**：")
        for rel, 为什么 in 解析不了:
            print("    %-40s %s" % (rel, 为什么))
        print()

    print("═" * 72)
    print("  == %d/%d 个文件静态验证通过 ==" % (len(改了) - len({x[0] for x in 坏}), len(改了)))
    print("  ⚠ 静态验证只回答「路径指得到真目录」。它**不等于**跑过 ——")
    print("     真跑过的只有 %d 个（见上）；另外 %d 个**没跑**，"
          % (len(真跑过), len(改了) - len(真跑过)))
    print("     理由是跑它们会改课程文件（硬要求不允许），如实列出。")
    print("═" * 72)
    return 0 if not 坏 else 1


if __name__ == "__main__":
    sys.exit(main())
