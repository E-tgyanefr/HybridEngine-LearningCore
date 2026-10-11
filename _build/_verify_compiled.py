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
        ans = re.search(r'public static string 参考答案 => """\n(.*?)\n""";', t, re.S)
        if not (m and ans):
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
                        "--no-build", "检查", 编号],
                       cwd=ROOT, env=env, capture_output=True, timeout=300)
        文件 = [x for x in os.listdir(ANS) if x.startswith(编号)]
        if not 文件:
            跳过 += 1
            print("  ??  %s 建不出答题文件（**没验到**）" % 编号)
            continue
        io.open(os.path.join(ANS, 文件[0]), "w", encoding="utf-8",
                newline="").write(ans.group(1) + "\n")

        r = subprocess.run(["dotnet", "run", "--project", "csharp/" + 参["工程"],
                            "--no-build", "检查", 编号],
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
