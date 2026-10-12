# 编译型四条线（C / C++ / Java / C#）的**统一验证器**。
#
# ════════════════════════════════════════════════════════════════════
#  为什么要统一
# ════════════════════════════════════════════════════════════════════
#  原来 `_verify_c.py`、`_verify_cpp.py` 各写一份，缺点有三：
#
#   ① 两条线的逻辑一样，改一处要记得改另一处（我漏过一次）
#   ② **Java 和 C# 根本没有验证器** ⇒ 那两条线的课没人验过
#   ③ ⚠ 更要命的：那两个脚本的正则写死了 `\[课程\(`
#      —— 而 **Java 用的是 `[Java课程(`**，正则匹配不到，
#      于是 Java 课会被静默跳过（"看起来验证过了，其实一节没验"）。
#
#  ⚠ 这就是我一直在踩的同一类坑：**判据的覆盖面比它看起来的窄**。
#    所以这个统一版第一件事就是：**报告它实际验了几课、跳过了几课**，
#    数字对不上时直接报错退出 —— 不允许"静默跳过"。
#
#  用法：
#      python _build/_verify_compiled.py            # 四条线都验
#      python _build/_verify_compiled.py java c     # 只验这两条
import io
import os
import re
import shutil
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

线表 = {
    "c":      dict(工程="c-lessons",    答案="c-answers",    特性="课程"),
    "cpp":    dict(工程="cpp-lessons",  答案="cpp-answers",  特性="课程"),
    "java":   dict(工程="java-lessons", 答案="java-answers", 特性="Java课程"),
    "csharp": dict(工程="cs-lessons",   答案="cs-answers",   特性="课程"),
}

env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"
env["PYTHONIOENCODING"] = "utf-8"
# ⚠⚠ 把 C++ 编译的临时目录指到一个**私有**的 TEMP（ASCII 路径）。
#   两个原因，都是实测踩出来的：
#     ① `CppRunner.清理旧目录` 会删掉**别的进程**留下的 run* 目录 ——
#        这台机器上同时有好几个助手各自跑课程检查，于是刚建出来的编译目录
#        会被别人的检查顺手删掉，g++ 报
#           ld.exe: cannot open output file …prog.exe: No such file or directory
#        学生看到的是"编译没能通过"，其实他的代码一个字都没错。
#     ② MinGW 的 ld.exe **无法把 exe 写进含中文的路径**（项目根目录里就有中文），
#        同一个错误换到这里来。
私有TEMP = r"C:\lc_tmpx"
try:
    os.makedirs(私有TEMP, exist_ok=True)
    env["TEMP"] = 私有TEMP
    env["TMP"] = 私有TEMP
except OSError:
    pass


def 验一条(键: str) -> tuple[int, int, int]:
    """返回 (通过数, 总数, 跳过数)。"""
    参 = 线表[键]
    Lessons = os.path.join(ROOT, "csharp", 参["工程"], "Lessons")
    ANS = os.path.join(ROOT, 参["答案"])
    if not os.path.isdir(Lessons):
        print("  %-7s ✗ 没有 %s" % (键, Lessons))
        return (0, 0, 0)

    print()
    print("═" * 62)
    print("  %s（%s）" % (键, 参["工程"]))
    print("═" * 62)

    通过数, 总数, 跳过 = 0, 0, 0
    文件们 = sorted(f for f in os.listdir(Lessons) if f.endswith(".cs"))
    for f in 文件们:
        t = io.open(os.path.join(Lessons, f), encoding="utf-8").read()
        # ⚠ 特性名按语言取：Java 是 [Java课程(，其余是 [课程(
        m = re.search(r'\[%s\("(\d+)"\s*,\s*"((?:[^"\\]|\\.)*)"' % 参["特性"], t)
        # ⚠⚠ 这个正则要同时满足三件事（我前两版各栽过一次）：
        #    ① **在第一个 `"""` 就收住** —— 否则参考答案末尾多一个空行时，
        #       `.*?` 会一路吃到**下一个** `"""`，把后面的 C# 代码也当答案
        #       喂进编译器 ⇒ 报「编译没能通过」，而报错行号指向模板，
        #       看着完全不相干。实测 C 的第 09、11 课就是这样挂的。
        #    ② **`"""` 后面不能强制要求 `\n`** —— 有一批课的答案是
        #       `参考答案 => """` 紧跟代码（同一行换行都没有）。
        #       我第一版改成 `"""\n(.*?)\s*""";` 就漏掉了这些课，
        #       表现为「解析不出参考答案（没验到）」，C++ 04/05/16 全中。
        #    ③ 结尾的空白要去掉（`(.*?)` 收在 `"""` 前，可能带回车）。
        #    ⇒ 用最朴素的 `"""(.*?)""";` 再 `.strip()`。C# 原始字符串里
        #      本来就不会出现三个连续双引号，所以这样是安全的。
        ans = re.search(r'public static string 参考答案 => """(.*?)""";', t, re.S)
        答案文 = ans.group(1).strip() if ans else ""
        if not (m and 答案文):
            # ⚠ **不静默跳过** —— 明确报出来，否则会误以为"验过了"
            跳过 += 1
            print("  ??  %-30s 解析不出编号/参考答案（**没验到**）" % f[:30])
            continue
        总数 += 1
        编号 = m.group(1)
        标题 = m.group(2).replace('\\"', '"').replace("\\\\", "\\")

        # 先让课程程序建好答题文件（它用的是**真实**的文件名约定），
        # 再用参考答案覆盖 —— 不自己拼文件名（踩过：标题里的引号会让拼出来的名字不一致）
        shutil.rmtree(ANS, ignore_errors=True)
        os.makedirs(ANS, exist_ok=True)
        subprocess.run(["dotnet", "run", "--project", "csharp/" + 参["工程"],
                        "检查", 编号],
                       cwd=ROOT, env=env, capture_output=True, timeout=300)
        文件 = [x for x in os.listdir(ANS) if x.startswith(编号)]
        if not 文件:
            跳过 += 1
            print("  ??  %s 建不出答题文件（**没验到**）" % 编号)
            continue

        # ⚠⚠ 答案写进去之后**要确认它还在**，否则重写。
        #
        #   为什么：这个脚本和别的验证脚本会**共用同一个 answers 目录**，
        #   而它们都会 `rmtree(ANS)` 清场。两个脚本同时跑时会出现这个时序：
        #       我：建目录 → 让课程程序生成模板 → 写好参考答案
        #       别人：rmtree(ANS)                       ← 把我的答案删了
        #       我：跑检查 → 课程程序发现文件不存在 → 重建**模板** → 编译失败
        #   症状是「参考答案跑不通」，而**参考答案本身没问题** ——
        #   实测就是这个：一个写课的子代理报 `_verify_compiled.py` 连续三次
        #   报 31/41、31/41、27/41，失败点每次都漂移，而同一批课单独重跑全过。
        #
        #   ⇒ 写完读回来对一下：内容不对就重建目录、重写一次（最多 3 轮）。
        def 写答案():
            io.open(os.path.join(ANS, 文件[0]), "w", encoding="utf-8",
                    newline="").write(答案文 + "\n")

        写答案()
        for _ in range(3):
            try:
                回读 = io.open(os.path.join(ANS, 文件[0]),
                               encoding="utf-8").read().strip()
            except OSError:
                回读 = ""
            if 回读 == 答案文.strip():
                break
            # 被别人删了或覆盖了 —— 重建再写
            os.makedirs(ANS, exist_ok=True)
            subprocess.run(["dotnet", "run", "--project", "csharp/" + 参["工程"],
                            "检查", 编号],
                           cwd=ROOT, env=env, capture_output=True, timeout=300)
            新文件 = [x for x in os.listdir(ANS) if x.startswith(编号)]
            if 新文件:
                文件 = 新文件
            写答案()

        r = subprocess.run(["dotnet", "run", "--project", "csharp/" + 参["工程"],
                            "检查", 编号],
                           cwd=ROOT, env=env, capture_output=True, timeout=600)
        out = (r.stdout + r.stderr).decode("utf-8", "replace")
        过 = "全部通过" in out
        通过数 += 1 if 过 else 0
        n过 = out.count("✓")
        n否 = out.count("✗") - (1 if "上面 ✗ 的部分" in out else 0)
        print("  %s %s  %-32s ✓%d ✗%d"
              % ("OK " if 过 else "XX ", 编号, 标题[:32], n过, max(0, n否)))
        if not 过:
            for line in out.splitlines():
                s = line.strip()
                if s.startswith("✗") or "应当是" in s or "应该是" in s:
                    print("         " + s[:104])

    print()
    print("  %s：%d/%d 课用参考答案跑通%s"
          % (键, 通过数, 总数, ("（另有 %d 个文件没验到 ⚠）" % 跳过) if 跳过 else ""))
    return (通过数, 总数, 跳过)


目标 = sys.argv[1:] or list(线表)
总通过, 总课, 总跳 = 0, 0, 0
for 键 in 目标:
    if 键 not in 线表:
        print("  ! 不认识的线：%s（可选：%s）" % (键, " ".join(线表)))
        sys.exit(2)
    a, b, c = 验一条(键)
    总通过 += a
    总课 += b
    总跳 += c

for 键 in 目标:
    shutil.rmtree(os.path.join(ROOT, 线表[键]["答案"]), ignore_errors=True)

print()
print("═" * 62)
print("  合计：%d/%d 课用参考答案跑通%s"
      % (总通过, 总课, ("，**%d 个文件没验到**" % 总跳) if 总跳 else ""))
print("═" * 62)
# ⚠ 有文件没验到也算失败 —— 不允许"静默跳过"
sys.exit(0 if 总课 and 总通过 == 总课 and 总跳 == 0 else 1)
