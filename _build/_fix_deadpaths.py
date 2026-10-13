# -*- coding: utf-8 -*-
r"""把 `_build/` 与 `tests/` 里写死的**老仓库绝对路径**改成从 `__file__` 推。

════════════════════════════════════════════════════════════════
  为什么（这是真缺口，不是洁癖）
════════════════════════════════════════════════════════════════

仓库改过名：`Project\LearnCode` → `Project\引擎\LearnCode`
→ `Project\引擎\HybridEngine-LearningCore`。

而二十来个一次性脚本把**当时的绝对路径**写死在源码里。改名之后那些字面量
指向两个**都不存在**的目录：

    D:\Users\etgya\Desktop\Project\引擎\LearnCode      （不存在）
    D:\Users\etgya\Desktop\Project\HybridEngine-light  （不存在）

⚠ 危险的地方在于它们**坏得很安静**：`os.walk` 一个不存在的目录**返回空**
  而**不报错** ⇒ 脚本「跑完了、没报错、什么都没做」。
  这正是 `环境坑与教训.md` 第 22 条那一类（"跑绿了" ≠ "全验过了"）。

════════════════════════════════════════════════════════════════
  四条纪律
════════════════════════════════════════════════════════════════

  ① **先校验，再写**（第 6 条）：所有文件的所有锚点**全部**检查完、
     收集完问题，**再**统一落盘。校验失败时一个字都不写。
  ② **不手抄老路径**：旧行用正则**在文件里现匹配**（含中文的原始字符串
     手抄一次就可能差一个字符，而差一个字符的表现是"锚点没找到"，
     看起来像"这文件不用改" —— 那是**假阴性**）。命中数必须**恰好 1**。
  ③ **整行锚定**：`^…$` + `re.M`，不做子串替换 —— 子串替换会命中
     **注释里提到老路径的句子**（本项目里有一堆），把历史证据改掉。
  ④ **同文件多处改动要累积**：第一版对每个规则都拿**原始**文本做切片，
     于是第二个规则把第一个规则的改动**吃掉了**（`_fix_cs_entry.py`
     同时有 `P` 和 `S` 两处）。这里改成按文件收集、**从后往前**应用，
     偏移量才不会被前一次改动带偏。
     ⚠ 而且 `LC = …` 必须插在**它第一次被用之前** —— `_fix_cs_entry.py`
       的 `P` 在第 9 行、`S` 在第 73 行，把 `LC` 插在 `S` 那里的话
       `P` 就成了未定义名（NameError），一跑就炸。

════════════════════════════════════════════════════════════════
  分类（这是本脚本唯一需要判断的地方）
════════════════════════════════════════════════════════════════

改路径是**呈现层**的修复，不动任何课程内容。但改完之后**能不能真跑**，
取决于脚本本来干什么：

    可跑   —— 只读/诊断类。改完真跑，记退出码。
    不可跑 —— **会写课程文件**（`lessons/` `csharp/*-lessons/` 等）或
              **生成器**（产物已在仓库里）。跑它 = 改教学内容 ⇒
              硬要求不允许。只做静态验证，并如实标注"没跑"。

⚠ 判据仍是那条：**「没跑到」不能算「通过」**。
  所以"不可跑"的那些**不计入"已验证可跑"**，在报告里单列。

用法：
    python _build/_fix_deadpaths.py            # 只检查，不改（默认）
    python _build/_fix_deadpaths.py --go       # 检查通过后落盘
"""
import ast
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

#: 老仓库根的两种写法
_老根 = r'D:\\Users\\etgya\\Desktop\\Project\\引擎\\LearnCode'

#: 从 `__file__` 推仓库根 —— 所有脚本都在 `<仓库>/_build/` 里。
推根 = r'os.path.dirname(os.path.dirname(os.path.abspath(__file__)))'
LC行 = "LC = " + 推根 + "   # 仓库根（从 __file__ 推，别再写死）"

#: 只含一行 `LC = r"…"` 的诊断/工具脚本，以及它们能不能真跑。
#: 「不可跑」的判据见文件头：它们会写课程文件，或是产物已在仓库里的一次性生成器。
仅LC = {
    "_apply_any": "不可跑",            # 把稿子写进 <线>/Lessons/*.cs
    "_audit_drift": "可跑",            # 只比内容，无写操作
    "_cmp_discover": "可跑",
    "_cmp_discover2": "可跑",
    "_cmp_thread": "可跑",
    "_cold": "可跑",                   # 只往 TEMP 写
    "_diff_cpp": "可跑",
    "_gen_c16": "不可跑",              # C 课生成器，会重写 lessons 产物
    "_gen_compiled": "不可跑",         # 四条线的统一生成器，同上
    "_gesp_reference": "可跑",         # 只往 lessons/answers/（gitignore 的答题区）写
    "_mk_cs_lessons": "不可跑",        # 重写 cs-lessons + cs-solutions
    "_peek2": "可跑",
    "_peek_templates": "可跑",
    "_peek_templates2": "可跑",        # 删掉答题模板强制重建（答题区，非课程）
    "_peek_templates3": "可跑",
    "_rebuild_versions": "不可跑",     # git worktree + 打 zip，会动 .git 状态
    "_shot": "可跑",
    "_test_progress_gui": "可跑",
    "_time_discover": "可跑",
    "_trace2": "可跑",
    "_trace_gui": "可跑",
    "_where": "可跑",
    "_where2": "可跑",
}

#: 老路径锚点的正则（整行锚定）
_LC锚 = r'(?m)^LC = r"' + _老根 + r'"$'

规则 = []          # (文件, 锚点正则, 新文本)

for n, _类 in sorted(仅LC.items()):
    规则.append(("_build/%s.py" % n, _LC锚, LC行))

#: `ROOT = r"…\LearnCode"`（`_mk_cs_course.py` 用的名字是 ROOT）
规则.append(("_build/_mk_cs_course.py",
            r'(?m)^ROOT = r"' + _老根 + r'"$', LC行.replace("LC", "ROOT", 1)))

#: `S = r"…\LearnCode\csharp\cs-lessons\Lessons"`
规则.append(("_build/_fix_cs_class_public.py",
            r'(?m)^S = r"' + _老根 + r'\\csharp\\cs-lessons\\Lessons"$',
            'S = os.path.join(LC, "csharp", "cs-lessons", "Lessons")'))
规则.append(("_build/_fix_cs_entry.py",
            r'(?m)^S = r"' + _老根 + r'\\csharp\\cs-lessons\\Lessons"$',
            'S = os.path.join(LC, "csharp", "cs-lessons", "Lessons")'))
规则.append(("_build/_fix_cs_type_scan.py",
            r'(?m)^S = r"' + _老根 + r'\\csharp\\cs-lessons\\Lessons"$',
            'S = os.path.join(LC, "csharp", "cs-lessons", "Lessons")'))
规则.append(("_build/_unify_cs_entry.py",
            r'(?m)^S = r"' + _老根 + r'\\csharp\\cs-lessons\\Lessons"$',
            'S = os.path.join(LC, "csharp", "cs-lessons", "Lessons")'))

#: `P = r"…\LearnCode\csharp\HybridEngineLight\CppLesson.cs"`
_P锚 = r'(?m)^P = r"' + _老根 + r'\\csharp\\HybridEngineLight\\CppLesson\.cs"$'
_P新 = 'P = os.path.join(LC, "csharp", "HybridEngineLight", "CppLesson.cs")'
规则.append(("_build/_fix_cs_entry.py", _P锚, _P新))
规则.append(("_build/_fix_cs_class_public.py", _P锚, _P新))

#: `PKG = r"…\LearnCode\hybridengine_light"`
规则.append(("_build/_unify_cmdname.py",
            r'(?m)^PKG = r"' + _老根 + r'\\hybridengine_light"$',
            'PKG = os.path.join(LC, "hybridengine_light")'))

#: `_mkjava.py`：`P = (r"…\_build"` + 续行 `r"\_java_模板扩写.md")`
#:  ⚠ 这一段跨两行，原路径是 `<仓库>\_build\_java_模板扩写.md`。
#:    第一版把 `_build` 那一节**漏掉了**（写成了 `<仓库>\_java_模板扩写.md`）——
#:    是 `_verify_fixed_paths.py` 静态验出来的（"P → … **不存在**"）。
#:    这就是为什么"改完要验"不是形式：漏一节的表现和"改对了"长得一模一样。
规则.append(("_build/_mkjava.py",
            r'(?m)^P = \(r"' + _老根 + r'\\_build"\n     r"\\_java_模板扩写\.md"\)$',
            'P = os.path.join(LC, "_build", "_java_模板扩写.md")'))

#: `tests/验收.py`：`ROOT = r"…\Project\HybridEngine-light"`
规则.append(("tests/验收.py",
            r'(?m)^ROOT = r"D:\\Users\\etgya\\Desktop\\Project\\HybridEngine-light"$',
            LC行.replace("LC", "ROOT", 1)))

#: 引用 `LC` 却**没有** `LC = …` 的文件 ⇒ 需要先插一行。
#: （`_mkjava.py` 连 `import os` 都没有。）
需插LC = {"_build/_fix_cs_class_public.py", "_build/_fix_cs_entry.py",
          "_build/_fix_cs_type_scan.py", "_build/_unify_cs_entry.py",
          "_build/_unify_cmdname.py", "_build/_mkjava.py"}
需补os = {"_build/_mkjava.py"}

#: ⚠ **故意不改**的：`_fix00j.py`
#    · 它**本来就语法错误**（`ast.parse` 直接 SyntaxError）—— 从写下来那天起
#      **从来没跑成过**，和路径写不写死无关。
#    · 它要写的是 `csharp\java-lessons\Lessons\第00课…cs` —— **课程文件**。
#    · 它想补的「常见坑」在 Java **整条线**上是 0/41，不是"第 00 课漏了"。
#    ⇒ 只报告，不动。改它等于把一把坏了的枪擦亮。
不改 = {"_build/_fix00j.py": "语法错误（从未跑成）+ 会写课程文件 + 意图已过期"}


def 读(P):
    return io.open(P, encoding="utf-8", errors="replace").read()


def 插入点(文):
    """最后一个**顶层** import 之后的字符偏移；找不到返回 None。

    ⚠⚠ 第一版是拿正则找 `^(?:import|from)\\s` 的最后一条命中，结果在
      `_unify_cmdname.py` 上栽了：那个文件第 87 行**函数体里**有一条真的
      `import ast`，而第 88 行属于「③ 语法检查」那一段。于是 `LC = …`
      被插到了第 88 行 —— **在它第 17 行第一次被用之后** ⇒ 一跑就 NameError。
      正则看不见"在不在顶层"。
      ⇒ 改用 `ast` 取顶层 Import / ImportFrom 节点。这是同一类坑的第二次
        （第 10 条：正则剥注释的顺序会咬人）—— 能上语法树就别用正则。
    """
    try:
        树 = ast.parse(文)
    except SyntaxError:
        return None
    行 = 文.split("\n")
    最后 = None
    for 节 in 树.body:
        if isinstance(节, (ast.Import, ast.ImportFrom)):
            最后 = 节.end_lineno
    if 最后 is None:
        return None
    #  插在最后一个顶层 import 那一行的**下一行**行首
    return sum(len(x) + 1 for x in 行[:最后])


def main():
    go = "--go" in sys.argv[1:]
    问题 = []
    计划 = {}

    for rel, 锚, 新 in 规则:
        P = os.path.join(ROOT, rel.replace("/", os.sep))
        if not os.path.isfile(P):
            问题.append("%s 不存在" % rel)
            continue
        文 = 读(P)
        命中 = list(re.finditer(锚, 文))
        if len(命中) != 1:
            #  0 次 = 已经不是老样子（别人改过 / 我记错）；
            #  >1 次 = 规则不够专一，替换会落错地方。两种都停。
            问题.append("%s：锚点命中 %d 次（要求恰好 1 次）"
                        % (rel, len(命中)))
            continue
        if "import os" not in 文 and rel not in 需补os:
            #  改了之后要用 `os.path` 却没有 `import os` ⇒ 一跑就 NameError。
            问题.append("%s：没有 `import os`（改了会 NameError）" % rel)
            continue
        计划.setdefault(rel, {"文": 文, "改": []})["改"].append(
            (命中[0].start(), 命中[0].end(), 新))

    for rel in sorted(需插LC):
        P = os.path.join(ROOT, rel.replace("/", os.sep))
        if not os.path.isfile(P):
            问题.append("%s 不存在（需插 LC）" % rel)
            continue
        文 = 读(P)
        #  ⚠ 只有当文件里**确实没有**顶层 `LC = ` 时才插 ——
        #    否则会插出第二个 LC（后面的覆盖前面的，看着没错，实际难查）。
        if re.search(r'(?m)^LC = ', 文):
            问题.append("%s：已经有顶层 `LC = `，需插LC 表过期" % rel)
            continue
        if 插入点(文) is None:
            问题.append("%s：找不到可以插 `LC = ` 的 import 锚点" % rel)
            continue
        计划.setdefault(rel, {"文": 文, "改": []})["插LC"] = True

    for rel in sorted(需补os):
        P = os.path.join(ROOT, rel.replace("/", os.sep))
        if os.path.isfile(P) and "import os" in 读(P):
            问题.append("%s：已经在 import os 了，需补os 表过期" % rel)

    print("═" * 70)
    print("  死路径修复：写死的老仓库绝对路径 → 从 __file__ 推")
    print("═" * 70)
    print("  仓库根：%s" % ROOT)
    print("  规则 %d 条 / 涉及 %d 个文件   故意不改 %d 个"
          % (len(规则), len({r[0] for r in 规则}), len(不改)))
    for rel, 为什么 in sorted(不改.items()):
        print("    -- %s：%s" % (rel, 为什么))
    print()

    if 问题:
        print("  **校验没过，一个字都不写**（先校验再写 —— 第 6 条）：")
        for p in 问题:
            print("    · " + p)
        return 2

    类别 = {("_build/%s.py" % n): c for n, c in 仅LC.items()}
    类别["_build/_mk_cs_course.py"] = "不可跑"
    for rel in ("_build/_fix_cs_class_public.py", "_build/_fix_cs_entry.py",
                "_build/_fix_cs_type_scan.py", "_build/_unify_cs_entry.py",
                "_build/_unify_cmdname.py"):
        类别[rel] = "不可跑"
    类别["_build/_mkjava.py"] = "可跑"
    类别["tests/验收.py"] = "可跑"

    print("  校验通过：%d 个文件、%d 处锚点，都恰好命中 1 次。"
          % (len(计划), sum(len(d["改"]) for d in 计划.values())))
    print()
    可跑 = [r for r in sorted(计划) if 类别.get(r) == "可跑"]
    不可跑 = [r for r in sorted(计划) if 类别.get(r) != "可跑"]
    print("  【可跑】改完真跑，记退出码（%d 个）：" % len(可跑))
    for r in 可跑:
        print("    " + r)
    print("  【不可跑】会写课程文件/一次性生成器 ⇒ 只做静态验证，报告里单列（%d 个）："
          % len(不可跑))
    for r in 不可跑:
        print("    " + r)
    print()

    if not go:
        print("  （这是干跑。加 --go 才真写。）")
        return 0

    for rel, d in sorted(计划.items()):
        出 = d["文"]
        #  ⚠ 从后往前应用：偏移量才不会被前一次改动带偏。
        for s, e, 新 in sorted(d["改"], reverse=True):
            出 = 出[:s] + 新 + 出[e:]
        if d.get("插LC"):
            #  ⚠ 插在**最后一个 import 之后** —— 保证在任何 `LC` 的用处之前。
            i = 插入点(出)
            出 = 出[:i] + LC行 + "\n" + 出[i:]
        if rel in 需补os and "import os" not in 出:
            i = 出.find("\nimport ")
            出 = 出[:i + 1] + "import os\n" + 出[i + 1:]
        io.open(os.path.join(ROOT, rel.replace("/", os.sep)), "w",
                encoding="utf-8", newline="").write(出)
    print("  已写 %d 个文件。" % len(计划))
    return 0


if __name__ == "__main__":
    sys.exit(main())
