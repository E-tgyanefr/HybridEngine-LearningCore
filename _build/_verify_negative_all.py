r"""一条命令跑完所有**留存的**反面验证 —— 并报出「覆盖账本」。

════════════════════════════════════════════════════════════════
  为什么需要这条命令
════════════════════════════════════════════════════════════════

`docs/终审清单.md` 第三节写着：反面验证**比正验证更重要** ——
只验"参考答案能过"，防不住"检查根本没在管事"（永远返回通过的检查，
同样能"参考答案全过"）。

但那一节一直是**四行手工命令**，而且当时只剩抽查。于是有一个诚实的缺口：

    「全量反验证没有统一命令（抽查跑过）」   ← docs/任务清单.md 第四节第 2 条

这条命令补的就是它。它做三件事：

  ① **串起来跑**：把留存的反面验证脚本按顺序跑一遍（**串行** ——
     理由见下），逐个记退出码和结论行。
  ② **报覆盖账本**：哪几条线的哪些课**真的有**留存的反面验证，
     哪些课**没有**。账本从各脚本自己的错例表里读出来，不是我手写的常量。
     ⇒ "没覆盖"这件事必须**看得见**，否则"跑绿了"会被误读成"全验过了"。
  ③ **退出码能当判据**：任何一条失败 ⇒ 1。

⚠⚠ 「没跑到」和「没过」必须分开（这个项目踩过很多次）：
   · 脚本**一个结论行都没打出来** ⇒ 判为"没跑到"，**算失败**
   · 脚本报 0 条错例 ⇒ 也判为"没跑到"，**算失败**
   两条都不是"通过"。

⚠ **必须串行**：这些脚本都在写同一批临时答题文件
  （`lessons/answers/`、`c-answers/`、`cs-answers/`），
  而且有的会**整个清空** `lessons/answers/`。并行跑会互相踩，
  症状是"一条 ✓ 和 ✗ 都没有"（看着像检查坏了，其实是答案被清走了）。
  实测记录见 `_verify_gesp_negative.py` 的 `跑一课()` 注释。

⚠ **覆盖缺口不判失败**：账本会把**没有**留存反面验证的课标成 ⚠ 并逐课列出来，
  但**退出码仍是 0** —— 否则这条命令永远红，就没人看了。
  缺口要的是"被看见"，不是"把闸门焊死"。

  ⚠⚠ 这里原来写的是「**C++ / Java 目前没有留存的反面验证脚本**」。
     那句话到后来**不成立了**：C++（`_verify_cpp_negative.py`）和
     Java（`_verify_java_negative.py`）两条都补上了。
     留这一行说明是为了记住教训 —— **"哪条线还没有"这类断言最容易过期**，
     它会让下一个读的人直接判断错。加了新线记得回来改这里。

════════════════════════════════════════════════════════════════
  用法
════════════════════════════════════════════════════════════════

    python _build/_verify_negative_all.py             # 全跑（几分钟）
    python _build/_verify_negative_all.py --列         # 只列脚本和覆盖，不跑
    python _build/_verify_negative_all.py --只跑 c-neg # 只跑名字里含它的

════════════════════════════════════════════════════════════════
  路径
════════════════════════════════════════════════════════════════

⚠ **不写死绝对路径**：脚本在 `LearnCode/_build/` ⇒ 上一级就是仓库根。
  同一课里已经吃过一次这个亏（`_verify_c.py` 开头那段注释：
  仓库从 `Project\LearnCode` 挪到 `Project\引擎\LearnCode`，
  写死路径的脚本全废）。`_verify_*.py` 里有几个仍然写死了 ——
  那是**它们的问题**，这里不跟着学。
"""
import io
import os
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

#: (键, 脚本文件名, 线, 这一条验的是什么)
#: ⚠ 顺序就是**执行顺序**，别随手调 —— 先跑会清 `lessons/answers/` 的。
脚本表 = [
    ("py-neg2", "_verify_py_neg2.py", "python",
     "Python 01~16 错答案（会清空 lessons/answers/）"),
    ("py-25-34", "_verify_py_25_34.py", "python",
     "Python 25~34 正验证 + 反验证两段"),
    ("gesp", "_verify_gesp_negative.py", "python",
     "Python 35~46（GESP 三~八级）错答案"),
    ("py-neg3", "_verify_py_neg3.py", "python",
     "Python 00/02/06/17~24/47~54 错答案（补齐账本里缺的 19 课）"),
    ("c-neg", "_verify_c_neg.py", "c",
     "C 01~12/15~19 错答案"),
    ("c-neg2", "_verify_c_neg2.py", "c",
     "C 00/13/14/20~40 错答案（补齐账本里缺的 24 课）"),
    ("cs-neg", "_verify_cs_negative.py", "csharp",
     "C# 01~08 错答案"),
    ("cs-neg2", "_verify_cs_neg2.py", "csharp",
     "C# 00/09~40 错答案（补齐账本里缺的 33 课）"),
    # ⚠ 加在**最后**：这条自己会清 `cpp-answers/`，而且编译很慢
    #   （41 课 × 一两次 g++，实测 4 分多钟）。放前面会让别的线等它。
    ("cpp-neg", "_verify_cpp_negative.py", "cpp",
     "C++ 00~40 错答案（每课一条）"),
    # ⚠ 同理再往后排：这条清 `java-answers/`，每课要跑一次 javac + java。
    #   和 cpp-neg 的目录**不重叠**（cpp-answers / java-answers 各一个），
    #   但两条都是编译型，串行跑才不会互相抢机器。
    ("java-neg", "_verify_java_negative.py", "java",
     "Java 00~40 错答案（每课一条）"),
    # ⚠ 这一条是**回归探针**，不进覆盖账本（它打的两课 java-neg 已经覆盖了）：
    #   打的是 Java 第 19 / 22 课那两条**曾经是弱检查**的地方 ——
    #   2026-10-13 已按作者的决定修好（19 改数据、22 改判据），
    #   所以它现在「放过」也判失败（回归信号），不再是"已知缺口只记录"。
    #   7 秒左右，放最后。
    ("java-probe", "_probe_weakchecks.py", "java",
     "Java 19/22 两条弱检查的**回归**探针（放过就失败）"),
]

#: 认得但**仍然没有**反面验证的线 —— 账本里要显式列出来，不能装作看不见。
#: ⚠ 现在是空的（五条主线都有了）。哪条线被拿掉脚本，记得把名字加回来。
没有反面脚本的线 = []

#: 留了但被同名新脚本取代的（跑了只是浪费时间，不跑，但要说明）
被取代的 = [("_verify_py_negative.py", "被 _verify_py_neg2.py 取代（同覆盖 01~16）")]

#: 只关心这五条课程线（另有 games/ai/embedded/raspi/enginecourse 五条小线）
主线 = ["python", "c", "cpp", "java", "csharp"]

汇总行 = re.compile(r"^\s*==\s*(.+?)\s*==\s*$", re.M)
#: 错例表里的课号。
#  ⚠ 必须要求「两位数字后面紧跟逗号」—— 这是错例行 `("01", '''...` 的形状。
#    只写 `\s*"(\d\d)"` 的话，**散文里提到课号的句子**也会被数进去
#    （`("第 01 课：…")` 这种），于是账本报的覆盖比真实的大。
#    这条脚本自己踩过：C 那条线报 19 课，实际错例表只有 17 课。
错例课号 = re.compile(r'^\s*\(\s*"(\d\d)",', re.M)


def 数课():
    """{线: 课数} —— 真值来自 learnmodel，不是写死的常量。"""
    sys.path.insert(0, ROOT)
    from hybridengine_light import learnmodel as M
    return {k: len(M.发现(k)) for k in M.轨道}


def 反面覆盖():
    """{线: set(课号)} —— 从各脚本自己的错例表里读，不手写。"""
    出 = {}
    for _, 文件, 线, _ in 脚本表:
        P = os.path.join(ROOT, "_build", 文件)
        if not os.path.isfile(P):
            continue
        文 = io.open(P, encoding="utf-8", errors="replace").read()
        出.setdefault(线, set()).update(错例课号.findall(文))
    return 出


def 打印账本():
    print()
    print("  ═══ 覆盖账本：哪几条线的哪些课**真的有**留存的反面验证 ═══")
    print("      （课号真值来自 learnmodel；覆盖来自各脚本的错例表）")
    print()
    课数 = 数课()
    覆盖 = 反面覆盖()
    for 线 in 主线:
        总 = 课数.get(线, 0)
        有 = 覆盖.get(线, set())
        缺 = sorted(set("%02d" % i for i in range(总)) - 有)
        标 = "OK" if not 缺 else ("**一个都没有**" if not 有 else "")
        print("    %-7s 共 %2d 课 · 反面覆盖 %2d 课  %s" % (线, 总, len(有), 标))
        if 缺:
            print("            反面**没覆盖**的课号：%s%s"
                  % (" ".join(缺[:26]),
                     "  …（共 %d 课）" % len(缺) if len(缺) > 26 else ""))
    print()
    还缺 = []
    for 线 in 主线:
        总 = 课数.get(线, 0)
        有 = 覆盖.get(线, set())
        还缺 += sorted(set("%02d" % i for i in range(总)) - 有)
    if 还缺:
        print("    ⇒ ⚠ 上面这些缺口是**如实的**：`docs/终审清单.md` 早就写明")
        print("       「我这边没有一条覆盖全部 219 课的反验证命令」。")
        print("       这份账本让缺口**逐课可见**，但它**没有**把缺口补上。")
    else:
        print("    ⇒ 五条主线**都被反面验证覆盖到了**（219/219）。")
        print("       ⚠⚠ 但「覆盖」这个词**只到这个程度**，别读大：")
        print("          · 它说的是「**这一课至少有一条检查**能拒掉**至少一个**错答案」；")
        print("          · 它**不是**「这一课的每条检查都有用」，也不是「多种错法都试过」；")
        print("          · 每条线留存的错例数**不一样**（有的脚本一课一条，有的多条），")
        print("            所以「219 课全覆盖」**不能**拿来互相比较各条线的强度。")
        print("          · 已验证「会空转」的检查另有记录（见 `引擎\\LearningCore-检查可疑处.md`）")


def 跑一条(键, 文件, 说明):
    P = os.path.join(ROOT, "_build", 文件)
    if not os.path.isfile(P):
        print("  XX %-12s 脚本不在：%s" % (键, 文件))
        return None
    env = dict(os.environ, PYTHONIOENCODING="utf-8", PYTHONUNBUFFERED="1")
    print()
    print("  ── 跑 %s（%s）──" % (键, 说明))
    sys.stdout.flush()
    # ⚠ 串行 + 不设 capture 上限：这些脚本自己会打报告，这里要完整输出
    r = subprocess.run([sys.executable, P], cwd=ROOT, env=env,
                       capture_output=True, timeout=7200)
    出 = (r.stdout + r.stderr).decode("utf-8", "replace")
    # 只打结论行和失败的明细 —— 逐条 OK 行太吵，失败了才需要看
    行们 = 出.splitlines()
    for l in 行们:
        s = l.rstrip()
        if (汇总行.match(s) or s.strip().startswith("XX")
                or "**" in s or "Traceback" in s or "Error" in s):
            print("    " + s.strip()[:118])
    结论 = 汇总行.findall(出)
    if not 结论:
        print("    **一条结论行都没有 —— 判为「没跑到」（不是「没过」）**")
        for l in 行们[-6:]:
            print("    | " + l.rstrip()[:112])
    return {"键": 键, "退出码": r.returncode,
            "结论": 结论[-1] if 结论 else None}


def main():
    参 = sys.argv[1:]
    if "--列" in 参:
        打印账本()
        return 0
    筛选 = None
    if "--只跑" in 参:
        i = 参.index("--只跑")
        筛选 = 参[i + 1] if i + 1 < len(参) else ""

    print("═" * 64)
    print("  反面验证（统一入口）—— 错答案必须被拒")
    print("═" * 64)
    print("  仓库根：%s" % ROOT)
    print("  ⚠ 下面这些脚本会写/清 `lessons/answers/` 等临时答题区 —— 串行跑。")

    结果 = []
    for 键, 文件, _, 说明 in 脚本表:
        if 筛选 and 筛选 not in 键:
            continue
        结果.append(跑一条(键, 文件, 说明))

    print()
    print("  ═══ 逐个结论 ═══")
    全过 = True
    for x in 结果:
        if x is None:
            全过 = False
            continue
        好 = (x["退出码"] == 0) and (x["结论"] is not None)
        全过 = 全过 and 好
        print("    %s %-10s 退出码 %-3s %s"
              % ("OK " if 好 else "XX ", x["键"], x["退出码"],
                 ("结论：" + x["结论"]) if x["结论"] else "**没跑到**"))

    for 文件, 为什么 in 被取代的:
        if os.path.isfile(os.path.join(ROOT, "_build", 文件)):
            print("    --  %-10s 没跑：%s" % (文件, 为什么))

    打印账本()

    print()
    print("═" * 64)
    print("  结论：%s" % ("跑到的都过了 ✅" if 全过 else "有失败 ❌"))
    if 全过:
        print("  ⚠ 但这**不等于**「全量反面验证过了」—— 覆盖缺口见上面的账本，")
        print("    缺的课**一次都没被反面验过**。")
    print("═" * 64)
    return 0 if 全过 else 1


if __name__ == "__main__":
    sys.exit(main())
