# 编译型四条线（C / C++ / Java / C#）的**统一生成器**。
#
# ════════════════════════════════════════════════════════════════════
#  为什么泛化
# ════════════════════════════════════════════════════════════════════
#  原来的 `_gen_c16.py` 是给 C 一条线写的。四条线的课程文件结构**几乎一样**：
#
#      C / C++ / C#   [课程("01", "标题", "要做")]
#                     public static CheckResult 检查_xxx(string 代码)
#                     用 CppCheck / CppLesson
#
#      Java           [Java课程("01", "标题", "要做")]
#                     public static JavaCheckResult 检查_xxx(string 代码)
#                     用 JavaCheck / JavaLesson
#
#  差别只有：特性名、返回类型、helper 类名、命名空间。
#  ⇒ 把它们做成一张参数表，四条线共用同一套形态生成逻辑。
#    这样加一课只写数据文件，不用碰代码。
#
# ════════════════════════════════════════════════════════════════════
#  数据文件格式
# ════════════════════════════════════════════════════════════════════
#      @@@ <编号>_<名字>
#      标题|<第 NN 课：...>
#      要求|<一条>
#      模板|<一行>
#      答案|<一行>
#      检查|<说明>|<形态>|<参数…>
import io
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"

#: 四条线的参数表
线表 = {
    "c": dict(
        数据=["_build/_c00.txt", "_build/_c16.txt"],
        产物目录="csharp/c-lessons/Lessons",
        特性="课程",
        返回类型="CheckResult",
        检查类="CppCheck",
        课程类="CppLesson",
        命名空间="HybridEngineLight.CLessons",
        库命名空间="HybridEngineLight",
        前缀="第",
    ),
    "cpp": dict(
        数据=["_build/_cpp00.txt"],
        产物目录="csharp/cpp-lessons/Lessons",
        特性="课程",
        返回类型="CheckResult",
        检查类="CppCheck",
        课程类="CppLesson",
        命名空间="HybridEngineLight.CppLessons",
        库命名空间="HybridEngineLight",
        前缀="第",
    ),
    "java": dict(
        数据=["_build/_java00.txt"],
        产物目录="csharp/java-lessons/Lessons",
        特性="Java课程",
        返回类型="JavaCheckResult",
        检查类="JavaCheck",
        课程类="JavaLesson",
        命名空间="HybridEngineLight.JavaLessons",
        库命名空间="HybridEngineLight",
        前缀="第",
    ),
    "csharp": dict(
        数据=[],
        产物目录="csharp/cs-lessons/Lessons",
        特性="课程",
        返回类型="CheckResult",
        检查类="CppCheck",
        课程类="CppLesson",
        命名空间="HybridEngineLight.CSLessons",
        库命名空间="HybridEngineLight",
        前缀="第",
    ),
}


def C(文: str) -> str:
    """Python 字符串 → C# 字符串字面量。

    ⚠ 必须转义**换行**：数据文件里读来的多行文字若原样塞进去，
      C# 会报 CS1010: Newline in constant（栽过两次）。
    """
    出 = (文.replace("\\", "\\\\").replace('"', '\\"')
          .replace("\r\n", "\\n").replace("\n", "\\n")
          .replace("\r", "\\r").replace("\t", "\\t"))
    return '"' + 出 + '"'


#: 当前线的参数（生成时设置）
线 = None
#: 当前正在生成的课号
课号 = ""
#: 结果类名（`CheckResult` 或 `JavaCheckResult`）
R = "CheckResult"
#: 检查 helper 类名（`CppCheck` 或 `JavaCheck`）
K = "CppCheck"
#: 课程 helper 类名（`CppLesson` 或 `JavaLesson`）
L = "CppLesson"

骨架头 = '#include <stdio.h>\n\n/*学生代码*/\n\nint main(void) {\n'
尾 = "    }\n"


def 头(说明: str, 名: str) -> str:
    # Java 用 `[Java检查说明]`，C / C++ / C# 用 `[检查说明]`
    特性 = "Java检查说明" if 线["特性"] == "Java课程" else "检查说明"
    return (f'\n    [{特性}({C(说明)})]\n'
            f'    public static {R} {名}(string 代码)\n    {{\n')


def 过(说明: str = "") -> str:
    return f"        return {R}.过({C(说明)});\n" if 说明 else f"        return {R}.过();\n"


def 不过(说明: str) -> str:
    return f"        return {R}.不过({说明});\n"


def 跑骨架(语句: str) -> str:
    return ('        var 骨架 = """\n' + 骨架头 + 语句 + '    return 0;\n}\n""";\n'
            f'        var run = {K}.编译并运行(代码, 骨架);\n')


def 逐行比对(期望列, 输入列, 提示: str) -> str:
    """逐行比对的 C# 代码。

    ⚠ 两处细节都踩过坑：
      · **C# 不允许尾随逗号**（`f(a, b,)` 报 CS1525）⇒ 用 join，不留尾逗号
      · 数组类骨架打印的是 "%d,"（带尾逗号），而期望写的是 2,3,4
        ⇒ 比较前 **TrimEnd(',')**
    """
    期望 = ",\n".join("            " + C(x) for x in 期望列)
    输入 = ",\n".join("            " + C(x) for x in 输入列)
    return (
        f'        string[] 期望 = {{\n{期望}\n        }};\n'
        f'        string[] 输入 = {{\n{输入}\n        }};\n'
        '        for (int i = 0; i < 期望.Length; i++)\n'
        '        {\n'
        '            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号\n'
        '            string 实际行 = i < run.行.Count\n'
        "                ? run.行[i].Trim().TrimEnd(',') : \"\";\n"
        '            if (i >= run.行.Count || 实际行 != 期望[i])\n'
        '            {\n'
        '                string 这是 = i < 输入.Length ? 输入[i] : \"\";\n'
        '                return ' + R + '.不过(\n'
        '                    $"输入 [{这是}] 应当是 [{期望[i]}]，" +\n'
        '                    $"实际是 [{(i < run.行.Count ? 实际行 : \"（没输出）\")}]。\\n" +\n'
        '                    ' + C(提示) + ');\n'
        '            }\n'
        '        }\n'
        '        return ' + R + '.过("每一组都对");\n')


def 形_输出逐行(说明, 参, 序):
    """学生代码是**完整程序**（自带 main）⇒ 原样编译运行，逐行比。"""
    期望 = "".join(f"            {C(x)},\n" for x in 参)
    # ⚠ 第 00 课要先查"工具链装好了没有"：没装时这条会失败，
    #   而它的报错像"你代码写错了"，方向就错了。
    前缀 = ""
    if 课号 == "00" and L == "CppLesson":
        前缀 = (头("工具链就绪（编译器与 dotnet 都在）", f"检查_{序}a") +
                f"        return {L}.工具链就绪(代码);\n" + 尾)
    elif 课号 == "00" and L == "JavaLesson":
        前缀 = (头("工具链就绪（javac / java / dotnet 都在）", f"检查_{序}a") +
                f"        return {L}.工具链就绪(代码);\n" + 尾)
    return 前缀 + 头(说明, f"检查_{序}b" if 前缀 else f"检查_{序}") + (
        f"        var run = {K}.直接运行(代码);\n"
        "        if (!run.编译通过) return " + K + ".编译失败说明(run);\n"
        "        string[] 期望 = {\n" + 期望 + "        };\n"
        "        var 实际 = run.行;\n"
        "        if (实际.Count != 期望.Length)\n"
        '            return ' + R + '.不过($"应该有 {期望.Length} 行输出，实际 {实际.Count} 行。" +\n'
        '                "\\n      打印出来的：\\n        " + string.Join("\\n        ", 实际));\n'
        "        for (int i = 0; i < 期望.Length; i++)\n"
        "            if (实际[i].Trim() != 期望[i])\n"
        "                return " + R + ".不过(\n"
        '                    $"第 {i + 1} 行应当是「{期望[i]}」，实际是「{实际[i].Trim()}」。");\n'
        '        return ' + R + '.过($"{期望.Length} 行输出全对");\n') + 尾


def 形_代码包含(说明, 参, 序):
    片段 = 参[0]
    return 头(说明, f"检查_{序}") + (
        "        string 去注释 = 去掉注释(代码);\n"
        f"        if (!去注释.Contains({C(片段)}))\n"
        f"            return {R}.不过({C('没找到「' + 片段 + '」—— 这一课要求用到它。')});\n"
        + 过()) + 尾


def 形_代码不含(说明, 参, 序):
    片段 = 参[0]
    return 头(说明, f"检查_{序}") + (
        "        string 去注释 = 去掉注释(代码);\n"
        f"        if (去注释.Contains({C(片段)}))\n"
        f"            return {R}.不过({C('出现了不该有的写法：' + 片段)});\n"
        + 过()) + 尾


形态表 = {
    "输出逐行": 形_输出逐行,
    "代码包含": 形_代码包含,
    "代码不含": 形_代码不含,
}


# ════════════════════════════════════════════════════════════════════
#  生成一课
# ════════════════════════════════════════════════════════════════════
def 生成一线(线键: str) -> int:
    global 线, R, K, L, 课号
    线 = 线表[线键]
    R, K, L = 线["返回类型"], 线["检查类"], 线["课程类"]
    线目录 = os.path.join(LC, 线["产物目录"])

    课, 当前 = {}, None
    for 相对 in 线["数据"]:
        for 行 in io.open(os.path.join(LC, 相对), encoding="utf-8").read().split("\n"):
            if 行.startswith("@@@ "):
                当前 = 行[4:].strip()
                课[当前] = {"标题": "", "要求": [], "模板": [], "答案": [], "检查": []}
                continue
            if 当前 is None or not 行.strip() or "|" not in 行:
                continue
            键, _, 值 = 行.partition("|")
            if 键 == "标题":
                课[当前]["标题"] = 值
            elif 键 in ("要求", "模板", "答案", "检查"):
                课[当前][键].append(值)

    if not 课:
        print("  %-7s （没有数据文件，跳过）" % 线键)
        return 0

    错 = []
    for 名, k in 课.items():
        for 字段 in ("标题", "答案", "模板"):
            if not k[字段]:
                错.append("%s 没有%s" % (名, 字段))
        for i, 规 in enumerate(k["检查"], 1):
            片 = 规.split("|")
            if len(片) < 2 or 片[1] not in 形态表:
                错.append("%s 第 %d 条检查的形态不认识：%r" % (名, i, 片[1] if len(片) > 1 else None))
    if 错:
        print("  XX %s 的数据有问题，**不生成**：" % 线键)
        for e in 错:
            print("     " + e)
        return -1

    os.makedirs(线目录, exist_ok=True)
    # ⚠⚠ 这里原本想在覆盖前删掉旧文件，结果**把没在本次数据里的课也删了**：
    #   C 的 `_c16.txt`（01~16）和 `_c00.txt`（00）是**两个**数据文件，
    #   而我只跑 `_gen_compiled.py c` 时 `要生成` 算错了集合
    #   ⇒ 01~16 那 16 个文件被当成垃圾清掉，`Lessons/` 里只剩第 00 课。
    #
    #   教训：**"清理"要非常小心**。这里根本不需要清理 ——
    #   写文件本来就会覆盖同名文件。
    #   真正需要删的只有"改了名字的旧课"，而那属于人工决定，
    #   不该由一个通用生成器去猜。
    #   ⇒ 直接删掉这段逻辑。
    pass

    for 编号 in sorted(课):
        课号 = 编号[:2]
        k = 课[编号]
        要求 = '"""\n' + "\n".join(k["要求"]) + '\n"""'
        检查们 = "".join(形态表[规.split("|")[1]](规.split("|")[0], 规.split("|")[2:], i)
                        for i, 规 in enumerate(k["检查"], 1))
        体 = f'''using System;
using System.Linq;
using {线["库命名空间"]};
using {线["命名空间"]};

[{线["特性"]}("{课号}", "{k['标题']}",
    {要求})]
public static class {线["前缀"]}{课号}课
{{
    public static string 模板 => """
{chr(10).join(k["模板"])}
""";

    public static string 参考答案 => """
{chr(10).join(k["答案"])}
""";

{检查们}
    private static string 去掉注释(string s)
    {{
        var o = new System.Text.StringBuilder();
        bool 块 = false;
        for (int i = 0; i < s.Length; i++)
        {{
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '/')
            {{ while (i < s.Length && s[i] != '\\n') i++; o.Append('\\n'); continue; }}
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') {{ 块 = true; i++; continue; }}
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') {{ 块 = false; i++; continue; }}
            if (!块) o.Append(s[i]);
        }}
        return o.ToString();
    }}
}}
'''
        io.open(os.path.join(线目录, f"第{课号}课_{编号[3:]}.cs"), "w",
                encoding="utf-8", newline="\r\n").write(体)

    实际 = sorted(f for f in os.listdir(线目录) if f.endswith(".cs"))
    print("  %-7s 生成 %d 课（目录里现有 %d 个 .cs）" % (线键, len(课), len(实际)))
    return len(课)


if __name__ == "__main__":
    目标 = sys.argv[1:] or list(线表)
    总 = 0
    for 键 in 目标:
        n = 生成一线(键)
        if n < 0:
            sys.exit(1)
        总 += n
    print("  合计 %d 课" % 总)
