# -*- coding: utf-8 -*-
r"""定点探针：Java 第 19 / 22 课那两条**已知弱检查**，到底抓不抓得住错答案。

════════════════════════════════════════════════════════════════
  为什么要单独写这条（它和 `_verify_java_negative.py` 的分工）
════════════════════════════════════════════════════════════════

`_verify_java_negative.py` 报「41/41 被拒」，但它的**注释里自己写明了**：
第 19 课和第 22 课那两条检查各有盲区，所以它**故意绕开盲区**，
换了一处能被抓住的地方下手（见那个文件 L280-282、L307-312）。

⇒ 那句「41/41」**不能**被读成「这 41 课的每条检查都有用」。
   它证明的是「每课至少有一条检查抓得住某个错答案」。

这条探针干的就是补上那块**被绕开的**证据：
针对那两条盲区，造**能编译**的错答案，直接看目标检查会不会 ✗。

════════════════════════════════════════════════════════════════
  两条盲区（都已在课程文件里逐字核实，课程文件只读、不动）
════════════════════════════════════════════════════════════════

  ① 第 19 课 `检查_比较器`（第19课_排序稳定性与Comparator.cs:346）
     检查喂的数据：`Arrays.asList("java","c","abc","ab","zz")`（同文件 :386）
     期望输出：`[c, ab, zz, abc, java]`

     ⇒ 这组数据里**等长的只有 `ab` / `zz` 两个**，而它们在输入里
       **本来就是字典序**（ab 在 zz 前面）。`List.sort` 是稳定排序。
       ⇒「只比长度」和「先比长度再比字典序」输出**完全相同**，
         这组数据分不出这两种实现。

  ② 第 22 课 `检查_用了trywithresources`（第22课_文件读写.cs:310）
     判据原文（同文件 :318）：
         `if (!体.Contains("try") || !体.Contains("("))`
     后面还要求 `体.Contains("readLine")`（:330）。
     ⇒ 它只问「**`try` 这个词出现过吗？`(` 出现过吗？readLine 出现过吗？**」
       **没有核对资源是不是真的写在 `try (` 的圆括号里**，也不要求 close。

════════════════════════════════════════════════════════════════
  判据（「没跑到」必须算失败 —— 这是本项目的硬规矩）
════════════════════════════════════════════════════════════════

  每条探针跑**两次**：

    · **正对照**：把这一课自己的参考答案喂进去 ⇒ 必须「全部通过」。
      正对照不过 ⇒ 说明探针/环境有问题，**结论无效**（判失败），
      不能拿它去说"检查抓不住"。
    · **反探针**：喂能编译的错答案 ⇒ 看目标检查那一行是 ✓ 还是 ✗。

  结论三种：

    · 目标检查 **✗** ⇒「抓住了」，盲区在这条数据上不成立。
    · 目标检查 **✓** ⇒「**放过了**」⇒ 盲区坐实（这是本脚本要找的东西）。
    · 一个 ✓/✗ 都没有 ⇒「**没跑到**」⇒ **判失败**，不算任何结论。

  退出码：
    0 = 每条探针都跑到了、正对照都过、结论明确（**不管结论是抓还是放**）
    1 = 有「没跑到」或正对照失败 ⇒ 结论不可信
    2 = 前置自检不过（教材改了 ⇒ 变换的锚点没了）⇒ 一条都不跑，不动 java-answers/

⚠ 已知弱检查**不判失败**：理由是项目里那条一致的取舍 ——
  「缺口要的是**被看见**，不是把闸门焊死」（否则这条命令永远红，就没人看了）。
  但它会**用显眼的框**打出来，而且不会被算进"讲过没问题"的那一堆。

用法：
    python _build/_probe_weakchecks.py          # 跑两条探针
    python _build/_probe_weakchecks.py --列     # 只列探针，不跑

⚠ 路径从 `__file__` 推。⚠ `java-answers/` 同一时刻只能一个进程写 —— 串行跑。
"""
import io
import os
import re
import shutil
import subprocess
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
课程目录 = os.path.join(ROOT, "csharp", "java-lessons", "Lessons")
ANS = os.path.join(ROOT, "java-answers")

env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"
env["PYTHONIOENCODING"] = "utf-8"
env["PYTHONUNBUFFERED"] = "1"

#: ⚠ `--no-build`：先自己 build 一次（见下），避免每条探针都摸一遍 obj/。
#:   这也是 `_verify_java_negative.py` 的做法（那边解释了为什么：build 很慢，
#:   而且 cpp-lessons / java-lessons 引用同一个 HybridEngineLight 工程，
#:   并行 build 会抢 MSBuild 的中间目录）。
运行 = ["dotnet", "run", "--no-build", "--project", "csharp/java-lessons", "检查"]

_课 = re.compile(r'\[Java课程\(\s*"(\d\d)"\s*,\s*"((?:[^"\\]|\\.)*)"')
_检查 = re.compile(r'\[Java检查说明\(\s*"((?:[^"\\]|\\.)*)"\s*\)\]')
_参考 = re.compile(r'public\s+static\s+string\s+参考答案\s*=>\s*"""(.*?)""";', re.S)

# ════════════════════════════════════════════════════════════════
#  探针表
#
#  每项：(课号, 期望被观察的检查名, [(旧, 新)…], 错答案错在哪, 这条盲区的机制)
#
#  ⚠ 变换里的 `旧` 一律挑**纯代码片段**（不带行尾注释）——
#    作者哪天顺手改一句注释，这里就假失败了。
#    （`_verify_java_negative.py` 的错例表上写着同一条规矩。）
# ════════════════════════════════════════════════════════════════

探针 = [
    ("19",
     "★ 比较器是两级比较：长度相同的还要按字典序排",
     [("        if (a.length() != b.length()) {\n"
       "            return a.length() - b.length();\n"
       "        }\n"
       "        return a.compareTo(b);",
       "        return a.length() - b.length();")],
     "两级比较器只做第一级（只比长度、不比字典序）—— 能编译、签名对、返回的确实是 Comparator<String>",
     "数据里等长的只有 ab/zz，且它们在输入里本来就是字典序 ⇒ 只比长度 = 先长度再字典序"),

    ("22",
     "★ 数行数 用的是 try-with-resources（资源写在 try 后面的圆括号里）",
     [("    int 行数 = 0;\n"
       "    try (BufferedReader 读 = new BufferedReader(new FileReader(路径))) {\n"
       "        String 一行;\n"
       "        while ((一行 = 读.readLine()) != null) {\n"
       "            行数 = 行数 + 1;\n"
       "        }\n"
       "    }",
       "    BufferedReader 读 = new BufferedReader(new FileReader(路径));\n"
       "    int 行数 = 0;\n"
       "    try {\n"
       "        String 一行;\n"
       "        while ((一行 = 读.readLine()) != null) {\n"
       "            行数 = 行数 + 1;\n"
       "        }\n"
       "    } finally {\n"
       "    }")],
     "资源在 try **外面** new、进了 try 也从不 close（只有空 finally）—— 能编译，"
     "但这一课的正题（try-with-resources）一个字都没做",
     "判据只问「try / ( / readLine 这三个词出现过吗」，没核对资源在不在 try 的圆括号里"),
]


def 读课程():
    """{课号: {"标题":…, "参考":…, "检查名":[…]}} —— 全部从课程文件里读，不手抄。"""
    出 = {}
    for f in sorted(os.listdir(课程目录)):
        if not f.endswith(".cs"):
            continue
        文 = io.open(os.path.join(课程目录, f), encoding="utf-8").read()
        m = _课.search(文)
        if not m:
            continue
        r = _参考.search(文)
        出[m.group(1)] = {
            "标题": m.group(2),
            "参考": (r.group(1).strip() if r else ""),
            "检查名": [x for x in _检查.findall(文)],
            "文件": f,
        }
    return 出


def _清理(标题):
    """把标题变成答题文件名里那一段 —— 与 Program.cs 的 清理() 逐字对应。

    ⚠ 这段是从 `_verify_java_negative.py` **原样抄来**的。
      不 import 那个文件是因为它一被 import 就会执行整个 41 课的流程。
      两处若有一天不一致，文件名对不上 ⇒ 会直接判「没跑到」，不会静默变绿。
    """
    i = 标题.find("：")
    t = 标题[i + 1:] if i > 0 else 标题
    j = t.find("—")
    if j > 0:
        t = t[:j]
    return "".join(c for c in t if c.isalnum()) or "作业"


def 应用变换(参考, 变换):
    出 = 参考
    for 旧, 新 in 变换:
        出 = 出.replace(旧, 新, 1)
    return 出


课程 = 读课程()

# ── 前置自检：**先校验，再动手**（校验放写之后，失败时改动已落盘）────────
#    ⚠ 这里校验的是「教材有没有改掉这些锚点」——
#      锚点没了就说明这条探针验的是**已经不存在的代码**，
#      那时候它报什么都不能信。一条都不跑，也不碰 java-answers/。
def 校验探针():
    问题 = []
    if not 探针:
        问题.append("探针表是空的")
    for 编, 期望, 变换, 说明, 机制 in 探针:
        if 编 not in 课程:
            #  `_verify_java_negative.py` 是按顺序扫文件的，这里也一样：
            #  课号不在课程表里 ⇒ 教材结构变了。
            问题.append("第 %s 课不在课程表里" % 编)
            continue
        课 = 课程[编]
        if not 课["参考"].strip():
            #  参考答案的格式是 `public static string 参考答案 => """…""";`
            #  格式变了 ⇒ 这个探针失去输入来源。
            问题.append("第 %s 课取不到参考答案（`参考答案 => \"\"\"…\"\"\";` 的格式变了吗）" % 编)
            continue
        if not any(期望.strip() == n.strip() for n in 课["检查名"]):
            #  ⚠ 抄错一个标点都会在这里被拦下 —— 否则探针会去找一行
            #    永远不存在的 ✗，把"检查放过了"和"我抄错了检查名"混成一件事。
            问题.append("第 %s 课的检查名 %r 在课程文件里不存在（共 %d 条）"
                        % (编, 期望, len(课["检查名"])))
        for 旧, 新 in 变换:
            n = 课["参考"].count(旧)
            if n != 1:
                #  出现 0 次 = 作者改了参考答案；出现多次 = 我挑的片段不唯一，
                #  替换会落到别处 ⇒ 两种都必须在这里停下。
                问题.append("第 %s 课：变换的锚点出现 %d 次（要求恰好 1 次）—— %r"
                            % (编, n, 旧.strip()[:60]))
    return 问题


表问题 = 校验探针()
if 表问题:
    print("  **探针表没通过自检，一条都不跑（不动 java-answers/）**")
    for p in 表问题:
        print("    · " + p)
    sys.exit(2)

if "--列" in sys.argv[1:]:
    print("  探针：%d 条（自检已通过）" % len(探针))
    for 编, 期望, 变换, 说明, 机制 in 探针:
        print("    %s  %s" % (编, 说明))
        print("         期望被观察的检查：%s" % 期望)
        print("         盲区机制：%s" % 机制)
    sys.exit(0)


def 答题文件(编):
    """先按课程程序的命名规则算；算不出来再让课程程序自己建（它才是权威）。"""
    名 = "%s_%s.java" % (编, _清理(课程[编]["标题"]))
    路 = os.path.join(ANS, 名)
    if os.path.isfile(路):
        return 路
    os.makedirs(ANS, exist_ok=True)
    跑检查(编)
    候选 = [x for x in os.listdir(ANS) if x.startswith(编)]
    return os.path.join(ANS, 候选[0]) if 候选 else None


def 跑检查(编, 超时=600):
    r = subprocess.run([*运行, 编], cwd=ROOT, env=env,
                       capture_output=True, timeout=超时)
    return (r.stdout + r.stderr).decode("utf-8", "replace")


def 判(out):
    """(全过?, 目标检查的记号, 勾数, 叉数, 该检查那一行) —— 判据见文件头。"""
    行 = [l.strip() for l in out.splitlines()]
    全过 = any("全部通过" in l for l in 行)
    勾 = [l for l in 行 if l.startswith("✓")]
    叉 = [l for l in 行 if l.startswith("✗")]
    return 全过, len(勾), len(叉)


def 目标那一行(out, 期望):
    """找出目标检查名那一行的记号（✓ / ✗ / None=没出现）。"""
    for l in out.splitlines():
        s = l.strip()
        if 期望.strip() in s:
            if s.startswith("✓"):
                return "✓"
            if s.startswith("✗"):
                return "✗"
    return None


def 喂并跑(编, 内容, 次数=3):
    """把内容写进答题文件再跑检查。返回 (out, 文件)。"""
    文件 = None
    out = ""
    for _ in range(次数):
        文件 = 答题文件(编)
        if 文件 is None:
            continue
        # newline="" —— 与课程程序写文件的方式一致，别让 Python 改行尾
        io.open(文件, "w", encoding="utf-8", newline="").write(内容)
        out = 跑检查(编)
        if out.count("✓") or out.count("✗"):
            break
    return out, 文件


print("═" * 68)
print("  定点探针：Java 第 19 / 22 课的两条弱检查")
print("═" * 68)
print("  仓库根：%s" % ROOT)
print("  探针：%d 条（每条 = 正对照 + 反探针）" % len(探针))
print()

# ── 先把课程程序编好，后面全用 --no-build ───────────────────────────────
print("  先编译一次 csharp/java-lessons …")
构建 = subprocess.run(["dotnet", "build", "csharp/java-lessons", "-v", "q", "--nologo"],
                     cwd=ROOT, env=env, capture_output=True, timeout=600)
if 构建.returncode != 0:
    print("  **课程程序编译不过，一条都不跑**（退出码 %d）" % 构建.returncode)
    print((构建.stdout + 构建.stderr).decode("utf-8", "replace")[-2000:])
    sys.exit(2)
print("  编译通过。")
print()

t0 = time.time()
放过了 = []          # 目标检查判 ✓ ⇒ 盲区坐实
抓住了 = []          # 目标检查判 ✗ ⇒ 盲区不成立
没跑到 = []          # 一个 ✓/✗ 都没有 ⇒ 失败
对照坏 = []          # 正对照没「全部通过」⇒ 结论无效

# 先清一次场，保证答题文件是课程程序自己建的
shutil.rmtree(ANS, ignore_errors=True)
os.makedirs(ANS, exist_ok=True)

for 编, 期望, 变换, 说明, 机制 in 探针:
    参考 = 课程[编]["参考"]
    错 = 应用变换(参考, 变换)

    # ── 正对照 ──────────────────────────────────────────────────
    out, 文件 = 喂并跑(编, 参考)
    if 文件 is None or not (out.count("✓") or out.count("✗")):
        没跑到.append(编 + "(正对照)")
        print("  !! %s 正对照**没跑到**（一条 ✓/✗ 都没有）—— 结论无效" % 编)
        continue
    全过, 勾, 叉 = 判(out)
    if not 全过:
        对照坏.append(编)
        print("  XX %s 正对照**没通过**（%d✓ %d✗）—— 参考答案都被拒了，"
              "说明探针/环境有问题，下面的结论无效" % (编, 勾, 叉))
        for l in out.splitlines():
            if l.strip().startswith("✗"):
                print("       | " + l.strip()[:104])
        continue
    print("  OK %s 正对照：参考答案 %d✓ %d✗ 全部通过" % (编, 勾, 叉))

    # ── 反探针 ──────────────────────────────────────────────────
    out, 文件 = 喂并跑(编, 错)
    if 文件 is None or not (out.count("✓") or out.count("✗")):
        没跑到.append(编 + "(反探针)")
        print("  !! %s 反探针**没跑到**（一条 ✓/✗ 都没有）—— 不算任何结论" % 编)
        continue
    全过, 勾, 叉 = 判(out)
    记号 = 目标那一行(out, 期望)
    if 记号 is None:
        没跑到.append(编 + "(目标行没出现)")
        print("  !! %s 反探针跑了，但输出里**没有**目标检查「%s」那一行 —— "
              "不算任何结论" % (编, 期望))
        continue
    print("  ── %s 反探针：%s" % (编, 说明))
    print("       目标检查：%s %s" % (记号, 期望))
    print("       整轮：%d✓ %d✗%s" % (勾, 叉, "   ！全部通过" if 全过 else ""))
    if 记号 == "✓":
        放过了.append(编)
        print("       ⇒ **检查放过了这个错答案** —— 盲区坐实")
    else:
        抓住了.append(编)
        print("       ⇒ 检查抓住了这个错答案 —— 盲区在这条数据上**不成立**")
    print("       机制：%s" % 机制)
    print()

print("═" * 68)
if 放过了:
    print("  ⚠⚠ 目标检查**放过了**错答案的课：%s" % " ".join(放过了))
    print("      ⇒ 这两条检查的判断力**低于它的名字所声称的**。")
    print("      ⇒ 课程文件**只读**：这里只记录，不改。要改得先问作者。")
if 抓住了:
    print("  -- 目标检查抓住了错答案的课：%s" % " ".join(抓住了))
if 没跑到:
    print("  **没跑到 %d 条（不算结论、判失败）**：%s"
          % (len(没跑到), " ".join(没跑到)))
if 对照坏:
    print("  **正对照失败（结论无效、判失败）**：%s" % " ".join(对照坏))
print("  == 探针 %d 条：放过 %d · 抓住 %d · 没跑到 %d · 正对照坏 %d =="
      % (len(探针), len(放过了), len(抓住了), len(没跑到), len(对照坏)))
print("  用时 %.0f 秒" % (time.time() - t0))
print("═" * 68)

# 收尾把 java-answers/ 删掉（和四条姊妹脚本一致：留下错答案的伤害比删掉大得多）
shutil.rmtree(ANS, ignore_errors=True)

# ⚠ 退出码必须反映结论的可信度：
#   · 「没跑到」和「正对照坏」⇒ 判失败（结论不可信）
#   · 「放过了」**不判失败** —— 缺口要的是被看见，不是把闸门焊死。
#     但它已经用显眼的框打在上面了，不会被当成"没问题"。
sys.exit(1 if (没跑到 or 对照坏) else 0)
