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
        数据=["_build/_cpp00.txt", "_build/_cpp09.txt", "_build/_cpp_extra.txt"],
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

    # ⚠⚠ 第 00 课（工具链）要**多一条前置检查**：工具装好了没有。
    #   为什么必须插在最前面：没装 gcc 时，这条"编译运行"的检查会失败，
    #   而它的报错是「程序没能正常运行」—— 学员会以为是自己代码的问题。
    #   先过一条明说"这不是你代码的问题"的检查，方向才对。
    #
    #   ⚠ 我加这段的时候把 `return` 挪进了 `if` 里面 ⇒
    #     非 00 课的函数直接掉出去返回 **None**，
    #     生成时报 `TypeError: sequence item 0: expected str instance, NoneType found`。
    #     ⇒ 教训：加"特殊情况"时，**普通情况的出口不能被吃掉**。
    前缀 = ""
    序号 = f"检查_{序}"
    if 课号 == "00":
        前缀 = (头("工具链就绪（gcc 与 dotnet 都在）", f"检查_{序}a") +
                "        return {L}.工具链就绪(代码);\n" + 尾)
        序号 = f"检查_{序}b"

    return 前缀 + 头(说明, 序号) + (
        "        var run = {K}.直接运行(代码);\n"
        "        if (!run.编译通过) return {K}.编译失败说明(run);\n"
        "        string[] 期望 = {\n" + 期望 + "        };\n"
        "        var 实际 = run.行;\n"
        "        if (实际.Count != 期望.Length)\n"
        '            return {R}.不过($"应该有 {期望.Length} 行输出，实际 {实际.Count} 行。" +\n'
        '                "\\n      打印出来的：\\n        " + string.Join("\\n        ", 实际));\n'
        "        for (int i = 0; i < 期望.Length; i++)\n"
        "            if (实际[i].Trim() != 期望[i])\n"
        "                return {R}.不过(\n"
        '                    $"第 {i + 1} 行应当是「{期望[i]}」，实际是「{实际[i].Trim()}」。");\n'
        '        return {R}.过($"{期望.Length} 行输出全对");\n') + 尾


def 形_输出不含(说明, 参, 序):
    片段 = 参[0]
    return 头(说明, f"检查_{序}") + (
        "        var run = {K}.直接运行(代码);\n"
        "        if (!run.编译通过) return {K}.编译失败说明(run);\n"
        '        string 全部 = string.Join("\\n", run.行);\n'
        f"        if (全部.Contains({C(片段)}))\n"
        f"            return {R}.不过({C('输出里出现了「' + 片段 + '」—— 换行要用 \\n 转义。')});\n"
        "        return {R}.过();\n") + 尾


def 形_代码包含(说明, 参, 序):
    片段 = 参[0]
    return 头(说明, f"检查_{序}") + (
        "        string 去注释 = 去掉注释(代码);\n"
        f"        if (!去注释.Contains({C(片段)}))\n"
        f"            return {R}.不过({C('没找到「' + 片段 + '」—— 这一课要求用到它。')});\n"
        "        return {R}.过();\n") + 尾


def 形_代码不含(说明, 参, 序):
    片段 = 参[0]
    return 头(说明, f"检查_{序}") + (
        "        string 去注释 = 去掉注释(代码);\n"
        f"        if (去注释.Contains({C(片段)}))\n"
        f"            return {R}.不过({C('出现了不该有的写法：' + 片段)});\n"
        "        return {R}.过();\n") + 尾


def 形_写了函数(说明, 参, 序):
    名, 调用 = 参[0], 参[1]
    # ⚠ 不能把"调用表达式"当**值**打印：printf("%d", 加倍(0)) 对 void 函数会报
    #   void value not ignored。带参数的（如 判等级(95)）说明要看返回值。
    带参 = "(" in 调用 and 调用.split("(", 1)[1].rstrip(")") != ""
    # ⚠ `%d` 在 Python 的 `%` 格式化里是占位符 ⇒ 要写成 `%%d`
    #   （这一行原来直接写 `%d`，跑到这里报 "TypeError: %d format: a real number is required"）
    语句 = ('    printf("%%d\\n", %s);\n' % 调用) if 带参 else ("    %s;\n" % 调用)
    return 头(说明, f"检查_{序}") + 跑骨架(语句) + (
        "        if (!run.编译通过)\n"
        "        {\n"
        f"            if (run.编译信息.Contains({C(名)}))\n"
        f"                return {R}.不过({C('没找到「' + 名 + '」这个函数 —— 或者签名和题目要求不一致。')});\n"
        "            return {K}.编译失败说明(run);\n"
        "        }\n"
        f"        return {R}.过({C('「' + 名 + '」可以正常调用')});\n") + 尾


def 形_函数用例(说明, 参, 序):
    名 = 参[0]
    对 = list(zip(参[1::2], 参[2::2]))
    打印 = "".join('    printf("%%d\\n", %s(%s));\n' % (名, 入) for 入, _ in 对)
    return 头(说明, f"检查_{序}") + 跑骨架(打印) + (
        "        if (!run.编译通过) return {K}.编译失败说明(run);\n" +
        逐行比对([期 for _, 期 in 对], [入 for 入, _ in 对],
                "      ⚠ 多组输入都对才算对 —— 只对一组可能是碰巧。")) + 尾


def 形_字符串用例(说明, 参, 序):
    """和 `函数用例` 一样，但**函数的返回值是 C 字符串**（`char*`）。

    ⚠⚠ 为什么必须分开：`函数用例` 生成的是 `printf("%d\n", f(x))` ——
      **`%d` 只能打整数**。而返回 `char*` 的函数这么打会打印出一个
      毫无意义的数字（指针的值），或者编译器直接警告/报错。

      我写第 17 课（switch 返回 "春"/"夏"…）时差点直接用 `函数用例`，
      那会让**正确答案被判错**。⇒ 按返回类型选 printf 的格式符。

      这一条也是"检查冤枉正确答案"的典型来源，所以宁可多一个形态。
    """
    名 = 参[0]
    对 = list(zip(参[1::2], 参[2::2]))
    打印 = "".join('    printf("%%s\\n", %s(%s));\n' % (名, 入) for 入, _ in 对)
    return 头(说明, f"检查_{序}") + 跑骨架(打印) + (
        "        if (!run.编译通过) return {K}.编译失败说明(run);\n" +
        逐行比对([期 for _, 期 in 对], [入 for 入, _ in 对],
                "      ⚠ 返回的是 C 字符串（双引号），不是字符（单引号）。")) + 尾


def 形_指针用例(说明, 参, 序):
    名 = 参[0]
    对 = list(zip(参[1::2], 参[2::2]))
    打印 = "".join(
        '    int __v%d = %s;\n    %s(&__v%d);\n    printf("%%d\\n", __v%d);\n'
        % (i, 入, 名, i, i) for i, (入, _) in enumerate(对))
    return 头(说明, f"检查_{序}") + 跑骨架(打印) + (
        "        if (!run.编译通过) return {K}.编译失败说明(run);\n" +
        逐行比对([期 for _, 期 in 对], [入 for 入, _ in 对],
                "      ⚠ 要写 *p = ... 才改到外面的变量；只写 p = ... 改的是指针自己。")) + 尾


def 形_数组合用例(说明, 参, 序):
    """数组类函数。**必须显式说明**是哪种输出形态：
         返回   —— 函数返回一个值 ⇒ 打印 f(数组, 个数)
         改数组 —— 函数改数组本身 ⇒ 打印改完之后的整个数组
    （我在 09 / 15 之间来回改了六轮，根因就是这两种形态混在一个分支里。）
    """
    模式 = "改数组"
    if 参 and 参[0] in ("返回", "改数组"):
        模式 = 参[0]
        参 = 参[1:]
    名 = 参[0]
    对 = list(zip(参[1::2], 参[2::2]))
    块, 有效 = [], []
    for 数, 期 in 对:
        元素 = [x for x in 数.split(",") if x != ""]
        # ⚠ 空数组在这套骨架里造不出合法调用（f(0, 0) 类型错）⇒ 跳过。
        #   不测也比"造出编不过的代码"强。
        if not 元素:
            continue
        i = len(有效)
        有效.append((数, 期))
        块.append("    int __a%d[] = {%s};\n" % (i, ", ".join(元素)))
        if 模式 == "返回":
            块.append('    printf("%%d\\n", %s(__a%d, %d));\n' % (名, i, len(元素)))
        else:
            块.append("    %s(__a%d, %d);\n" % (名, i, len(元素)))
            块.append('    for (int __i = 0; __i < %d; __i++) printf("%%d,", __a%d[__i]);\n'
                      % (len(元素), i))
            块.append('    printf("\\n");\n')
    return 头(说明, f"检查_{序}") + 跑骨架("".join(块)) + (
        "        if (!run.编译通过) return {K}.编译失败说明(run);\n" +
        逐行比对([期 for _, 期 in 有效], [数 for 数, _ in 有效],
                "      ⚠ 函数拿到的是地址 ⇒ 改的就是原数组（或返回值要对）。")) + 尾


def 形_统计用例(说明, 参, 序):
    """void f(int a[], int n, int *大, int *小) —— 两个指针输出。"""
    对 = list(zip(参[0::3], 参[1::3], 参[2::3]))
    块, 输入列, 期望列 = [], [], []
    for 数, 大, 小 in 对:
        元素 = [x for x in 数.split(",") if x != ""]
        if not 元素:
            continue
        j = len(输入列)
        输入列.append(数)
        期望列.append("%s %s" % (大, 小))
        块.append("    int __a%d[] = {%s};\n" % (j, ", ".join(元素)))
        块.append("    int __d%d = -999, __x%d = 999;\n" % (j, j))
        块.append("    统计(__a%d, %d, &__d%d, &__x%d);\n" % (j, len(元素), j, j))
        块.append('    printf("%%d %%d\\n", __d%d, __x%d);\n' % (j, j))
    return 头(说明, f"检查_{序}") + 跑骨架("".join(块)) + (
        "        if (!run.编译通过) return {K}.编译失败说明(run);\n" +
        逐行比对(期望列, 输入列,
                "      ⚠ 两个坑：① 初值要从第一个元素起 ② 空数组要特判")) + 尾


def 形_连调用(说明, 参, 序):
    """同一个函数**连着调多次**，看它记不记得住上一次（`static` 局部变量）。

    ⚠⚠ 为什么不能复用 `函数用例`：那个形态每个用例都**重新跑一次完整骨架**，
      于是 `static` 变量每次都重新初始化，永远返回 1 ——
      **正确答案会被判错**。

      这一条要的是"在**同一个程序**里连着调"，所以才需要单独一个形态。

    参数：`连调用|函数名|第1次期望|第2次期望|…`
    """
    名 = 参[0]
    期望列 = 参[1:]
    # ⚠ 骨架里连着调 N 次，每次打印一个数
    打印 = "".join('    printf("%%d\\n", %s());\n' % 名 for _ in 期望列)
    return 头(说明, f"检查_{序}") + 跑骨架(打印) + (
        "        if (!run.编译通过) return {K}.编译失败说明(run);\n" +
        逐行比对(期望列, ["第 %d 次调用" % (i + 1) for i in range(len(期望列))],
                "      ⚠ 这一条要求**同一个程序里连着调**的结果 —— "
                "如果你用普通局部变量，它每次都会回到初值。")) + 尾


def 形_可编译(说明, 参, 序):
    """裸函数 + **空 main**（只为让它能编过）。"""
    return 头(说明, f"检查_{序}") + 跑骨架("") + (
        "        if (!run.编译通过) return {K}.编译失败说明(run);\n"
        "        return {R}.过();\n") + 尾


def 形_带main就能编译(说明, 参, 序):
    """裸函数 + 一个**调用它**的 main。参[0] 是用 ; 分开的完整语句序列。"""
    语句 = ""
    for 条 in (参[0].split(";") if 参 else []):
        条 = 条.strip()
        if 条:
            语句 += "    " + 条 + ";\n"
    return 头(说明, f"检查_{序}") + 跑骨架(语句) + (
        "        if (!run.编译通过) return {K}.编译失败说明(run);\n"
        "        return {R}.过();\n") + 尾


def 形_需要输入(说明, 参, 序):
    return 头(说明, f"检查_{序}") + (
        "        var run = {K}.直接运行(代码);\n"
        "        if (run.Raw.CompileFailed) return {K}.编译失败说明(run);\n"
        f"        return {R}.过({C('能编译运行（这一课的程序要读输入）')});\n") + 尾


def 形_顺序(说明, 参, 序):
    名 = 参[0]
    return 头(说明, f"检查_{序}") + (
        "        string 去注释 = 去掉注释(代码);\n"
        f"        int 主 = 去注释.IndexOf({C('int main')});\n"
        "        if (主 < 0)\n"
        f"            return {R}.不过({C('这一课要写完整的 main —— 找不到 int main。')});\n"
        "        string 前 = 去注释.Substring(0, 主);\n"
        "        string 后 = 去注释.Substring(主);\n"
        f"        if (!前.Contains({C('int ' + 名)}))\n"
        f"            return {R}.不过({C('声明要写在 main **前面**，否则编译器读不到它。')});\n"
        f"        if (!后.Contains({C('int ' + 名)}))\n"
        f"            return {R}.不过({C('函数定义要写在 main **后面** —— 这一课练的就是这个顺序。')});\n"
        f"        return {R}.过({C('声明在 main 前、定义在 main 后 —— 顺序对了')});\n") + 尾


形态表 = {
    "输出逐行": 形_输出逐行,
    "输出不含": 形_输出不含,
    "代码包含": 形_代码包含,
    "代码不含": 形_代码不含,
    "写了函数": 形_写了函数,
    "函数用例": 形_函数用例,
    "连调用": 形_连调用,
    "字符串用例": 形_字符串用例,
    "指针用例": 形_指针用例,
    "数组合用例": 形_数组合用例,
    "统计用例": 形_统计用例,
    "可编译": 形_可编译,
    "带main就能编译": 形_带main就能编译,
    "需要输入": 形_需要输入,
    "顺序": 形_顺序,
}


# ⚠ 形态函数里写的 `{K}` / `{L}` / `{R}` 是**占位符**，生成时换成本线真正的类名。
#   为什么用 .replace 而不是 f-string：生成的 C# 代码里有大量 `{ }`
#   （C# 的花括号），f-string 会把它们当占位符 ⇒ 一堆 KeyError。
#   用三个**独特的** token 做替换就绝不会撞。
def 换名(文: str) -> str:
    return 文.replace("{K}", K).replace("{L}", L).replace("{R}", R)



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
        # ⚠ 每条形态的输出都要过一遍 `换名()` —— 形态函数里写的是
        #   `{K}.不过(...)` / `{L}.工具链就绪(...)`，这里才换成真正的类名。
        检查们 = "".join(
            换名(形态表[规.split("|")[1]](规.split("|")[0], 规.split("|")[2:], i))
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
