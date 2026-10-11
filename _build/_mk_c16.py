import io
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
TXT = os.path.join(LC, "_build", "_c16.txt")
DST = os.path.join(LC, "csharp", "c-lessons", "Lessons")

# ⚠⚠ 生成「要求」串时踩了一个坑，记在这里：
#   我原来用 C_字符串(x + "\n") 拼，得到的是 `"1. …\n"` —— 但那个 \n 在
#   Python 里已经**真的变成了换行符**，于是 C# 源码里出现了
#   `"1. 打印一行文字：你好，C` + 换行 + `"` ⇒
#       error CS1010: Newline in constant
#   ⇒ 改用 C# **原始字符串** `"""…"""`：换行就是换行，不用转义、不用拼。
#     这也和课程里 `模板` / `参考答案` 的写法一致。


def C_字符串(s: str) -> str:
    """把一段文字变成合法的 C# 字符串字面量。

    ⚠⚠ 必须把**换行**转义成 `\n`。
      我第一版只处理了反斜杠和引号，于是从数据文件里读来的多行文字
      （错误提示里我常写好几行）被**原样**塞进了 C# 字符串 ⇒
          error CS1525: Invalid expression term
      而且报错行号指向的是那一行的中间，很难看出是"字符串里有个真换行"。
      （前面「要求」那处也栽在同一个坑上，那次用的是原始字符串绕过去的。）
    """
    出 = (s.replace("\\", "\\\\")
           .replace('"', '\\"')
           .replace("\r\n", "\\n")
           .replace("\n", "\\n")
           .replace("\r", "\\r")
           .replace("\t", "\\t"))
    return '"' + 出 + '"'


def 生成检查(说明: str, 规格: str, 序: int) -> str:
    # ⚠⚠ 规格的格式是 **`说明|形态|参数…`** —— 说明在最前面！
    #   我第一版把 `片[0]` 当成了形态 ⇒ 每条都落到"不认识的形态"分支。
    #   而那个分支**也返回一个 CheckResult.不过**，所以症状是
    #   "16 课全红、每条都说生成器不认识" —— 报错信息里带着整句说明，
    #   我看了两轮才反应过来那是**说明**不是形态名。
    #   ⇒ 教训：**自己定的格式，解析时要对着格式写**，别凭印象取下标。
    片 = 规格.split("|")
    形 = 片[1]
    参 = 片[2:]
    # ⚠⚠ 方法名**必须**以 `检查_` 开头 —— 收集器（CppLesson.收集）就是按这个前缀
    #   反射找的。我第一版生成的是 `检查1`，于是「通过 0/0」：
    #   一个检查都没装上，而**程序不报错**，看起来像"检查全过"。
    #   这是最坏的一类失败：静默地什么都没检查。
    方法名 = f"检查_{序}"

    头 = f'\n    [检查说明({C_字符串(说明)})]\n    public static CheckResult {方法名}(string 代码)\n    {{\n'
    尾 = "    }\n"
    S = C_字符串

    if 形 in ("能编译", "辅助"):
        # ⚠⚠ 用 `能编译` 会**套一层 main 骨架**，而整程序课的参考答案自己就有 main
        #   ⇒  gcc: error: redefinition of 'main'
        #   实测就是这么连红 6 课的。整程序课一律走 `直接运行`（原样编译）。
        return 头 + (
            "        var run = CppCheck.直接运行(代码);\n"
            "        if (!run.编译通过) return CppCheck.编译失败说明(run);\n"
            "        return CheckResult.过();\n") + 尾

    if 形 == "输出逐行":
        # ⚠ 不能用 `CppCheck.输出逐行` —— 它内部会套 main 骨架（见上面 (1) 的说明）。
        #   自己拿 `直接运行` 再逐行比。
        # ⚠⚠ 另外：**C# 不允许尾随逗号**（`f(a, b,)` 报 CS1525），
        #   我按 Python 的习惯留过 ⇒ 又是 6 课编译不过。
        期望 = ",\n".join(f"            {S(x)}" for x in 参)
        return 头 + (
            "        var run = CppCheck.直接运行(代码);\n"
            "        if (!run.编译通过) return CppCheck.编译失败说明(run);\n"
            "        string[] 期望 = {\n" + 期望 + "\n        };\n"
            "        var 实际 = run.行;\n"
            "        if (实际.Count != 期望.Length)\n"
            '            return CheckResult.不过($"应该有 {期望.Length} 行输出，实际 {实际.Count} 行。" +\n'
            '                "\\n      打印出来的：\\n        " + string.Join("\\n        ", 实际));\n'
            "        if (!run.编译通过)\n"
            "        {\n"
            "            string 信 = run.编译信息;\n"
            f"            if (信.Contains({S(名)}))\n"
            f"                return CheckResult.不过({S('没找到「' + 名 + '」这个函数 —— 或者它的签名和题目要求不一致。')});\n"
            "            return CppCheck.编译失败说明(run);\n"
            "        }\n"

    if 形 == "可编译":
        # ⚠⚠ 这一形态是为**裸函数课**准备的（09 / 12 / 15 / 16）。
        #
        #   为什么不能用「能编译」（那走 `直接运行`）：
        #     `直接运行` 把学生代码**原样**当完整程序编译 —— 它**不套 main**。
        #     而裸函数课的参考答案只有函数、没有 main ⇒
        #         error: undefined reference to `main'
        #   （反过来说，整程序课**自带 main**，套骨架又会
        #     `redefinition of 'main'` —— 两种课要用两种检查，这就是原因。）
        #
        #   ⚠ 骨架里的 main 必须**真的调用**那个函数，否则 gcc 可能把
        #     没被引用的静态函数优化掉，或者给"定义了但没用"的警告；
        #     调用一下最稳。
        return 头 + (
            '        var 骨架 = """\n'
            "#include <stdio.h>\n\n/*学生代码*/\n\nint main(void) {\n"
            "    return 0;\n}\n"
            '""";\n'
            "        var run = CppCheck.编译并运行(代码, 骨架);\n"
            "        if (!run.编译通过) return CppCheck.编译失败说明(run);\n"
            "        return CheckResult.过();\n") + 尾

    if 形 == "带main就能编译":
        # ⚠⚠ 三种「编译一下」的形态，对应**三种学生代码**，别混：
        #
        #   · `直接运行`      —— 学生代码是**完整程序**（自带 #include 与 main）
        #                        C 课里的 01~05、13、14 是这种
        #   · `可编译`        —— 学生代码是**裸函数**，需要 #include + 一个空 main
        #                        09 的求和 / 12 的结构体 / 16 的统计
        #   · `带main就能编译` —— 学生代码是**裸函数**，需要 #include + **调用它**的 main
        #                        11 的加倍 / 15 的全部加一
        #                        （为什么要调用：gcc 对"定义了但没用到"的函数
        #                          在 -Wall 下会警告，而课程把警告也当编译信息看）
        #
        #   我在这三种之间来回改错过好几轮。**判据是"学生要写什么"**，
        #   不是"我想检查什么"。
        # ⚠⚠ `参[0]` 是**整条要放进去的语句**（含变量声明），不是"一个表达式"。
        #   我第一版拿它当表达式的参数用 ⇒ 生成 `加倍(&__v);` 而 `__v`
        #   从来没被声明过：
        #       error: '__v' was not declared in this scope
        #   ⇒ 调用方要写**完整的语句序列**，用 `;` 分隔多条。
        # ⚠⚠ 参数是**一串用 `;` 分开的语句**，
        #   不能整条直接贴进去 —— 我第一版生成了
        #       int __a[] = {1, 2, 3}; 全部加一(__a, 3);     ← 挤在一行（能编译但难看）
        #   而另一处更糟：参数里没带分号时生成了
        #       int __a[] = {1, 2, 3} 全部加一(__a, 3);     ← 缺分号，编不过
        #   ⇒ 按 `;` 切开、逐条补分号、逐条换行。
        语句 = ""
        for 条 in (参[0].split(";") if 参 else []):
            条 = 条.strip()
            if 条:
                语句 += "    " + 条 + ";\n"
        if "return 0" not in 语句:
            语句 += "    return 0;\n"
        return 头 + (
            '        var 骨架 = """\n'
            "#include <stdio.h>\n\n/*学生代码*/\n\nint main(void) {\n"
            + 语句 +
            "    return 0;\n}\n"
            '""";\n'
            "        var run = CppCheck.编译并运行(代码, 骨架);\n"
            "        if (!run.编译通过) return CppCheck.编译失败说明(run);\n"
            "        return CheckResult.过();\n") + 尾

    if 形 == "输出不含":
        片段 = 参[0]
        return 头 + (
            "        var run = CppCheck.直接运行(代码);\n"
            "        if (!run.编译通过) return CppCheck.编译失败说明(run);\n"
            '        string 全部 = string.Join("\\n", run.行);\n'
            f"        if (全部.Contains({S(片段)}))\n"
            f"            return CheckResult.不过({S('输出里出现了「' + 片段 + '」这几个字 —— 换行要用 \\n 转义，不能真的敲两下')});\n"
            "        return CheckResult.过();\n") + 尾

    if 形 == "代码包含":
        片段 = 参[0]
        return 头 + (
            "        string 去注释 = 去掉注释(代码);\n"
            f"        if (!去注释.Contains({S(片段)}))\n"
            f"            return CheckResult.不过({S('没找到「' + 片段 + '」—— 这一课要求用到它。')});\n"
            "        return CheckResult.过();\n") + 尾

    if 形 == "代码不含":
        片段 = 参[0]
        return 头 + (
            "        string 去注释 = 去掉注释(代码);\n"
            f"        if (去注释.Contains({S(片段)}))\n"
            f"            return CheckResult.不过({S('出现了不该有的写法：' + 片段)});\n"
            "        return CheckResult.过();\n") + 尾

    if 形 == "写了函数":
        名, 调用 = 参[0], 参[1]
        # ⚠ 不能用 `CppCheck.写了函数` —— 它会把调用表达式当**值**打印
        #   （`printf("%d", 加倍(0))`），而 void 函数这么用会报
        #       error: void value not ignored as it ought to be
        #   ⇒ 自己写骨架：只**调用**，不打印返回值。
        #     调用表达式里带参数的（如 `判等级(95)`）说明要看返回值，那就打印。
        带参 = "(" in 调用 and 调用.split("(", 1)[1].rstrip(")") != ""
        语句 = (f"printf(\"%d\\n\", {调用});" if 带参 else f"{调用};")
        return 头 + (
            '        var 骨架 = """\n'
            "#include <stdio.h>\n\n/*学生代码*/\n\nint main(void) {\n"
            "    " + 语句 + "\n    return 0;\n}\n"
            '""";\n'
            "        var run = CppCheck.编译并运行(代码, 骨架);\n"
            "        if (!run.编译通过)\n"
            "        for (int i = 0; i < 期望.Length; i++)\n"
            // ⚠ 骨架打印的是 "%d,"（每个元素后都带逗号），
            //   而期望写的是 "2,3,4" ⇒ 比较前去掉尾逗号。
            "            if (i >= run.行.Count || run.行[i].Trim().TrimEnd(',') != 期望[i])\n"
            "                return CheckResult.不过(\n"
            "            return CppCheck.编译失败说明(run);\n"
            "        }\n"
            f"        return CheckResult.过({S('「' + 名 + '」可以正常调用')});\n") + 尾

    if 形 == "函数用例":
        名 = 参[0]
        对 = list(zip(参[1::2], 参[2::2]))
        打印 = "".join(f'        printf("%d\\n", {名}({入}));\n' for 入, _ in 对)
        期望 = "".join(f"            {S(期)},\n" for _, 期 in 对)
        输入 = "".join(f"            {S(入)},\n" for 入, _ in 对)
        return 头 + (
            '        var 骨架 = """\n'
            "#include <stdio.h>\n\n/*学生代码*/\n\nint main(void) {\n" + 打印 + "    return 0;\n}\n"
            '""";\n'
            "        var run = CppCheck.编译并运行(代码, 骨架);\n"
            "        if (!run.编译通过) return CppCheck.编译失败说明(run);\n"
            "        string[] 期望 = {\n" + 期望 + "        };\n"
            "        string[] 输入 = {\n" + 输入 + "        };\n"
            "        if (run.行.Count < 期望.Length)\n"
            '            return CheckResult.不过($"应该打印 {期望.Length} 行，实际 {run.行.Count} 行。");\n'
            "        for (int i = 0; i < 期望.Length; i++)\n"
            // ⚠ 骨架打印的是 "%d,"（每个元素后都带逗号），
            //   而期望写的是 "2,3,4" ⇒ 比较前去掉尾逗号。
            "            if (i >= run.行.Count || run.行[i].Trim().TrimEnd(',') != 期望[i])\n"
            "                return CheckResult.不过(\n"
            f'                    $"{名}({{输入[i]}}) 应该是 {{期望[i]}}，实际是「{{run.行[i].Trim()}}」。\\n" +\n'
            f"                    {S('      ⚠ 多组输入都对才算对 —— 只对一组可能是碰巧。')});\n"
            '        return CheckResult.过($"{期望.Length} 组输入全对");\n') + 尾

    if 形 == "指针用例":
        名 = 参[0]
        对 = list(zip(参[1::2], 参[2::2]))
        打印 = "".join(
            f"    int __v{i} = {入};\n    {名}(&__v{i});\n    printf(\"%d\\n\", __v{i});\n"
            for i, (入, _) in enumerate(对))
        期望 = "".join(f"            {S(期)},\n" for _, 期 in 对)
        输入 = "".join(f"            {S(入)},\n" for 入, _ in 对)
        return 头 + (
            '        var 骨架 = """\n'
            "#include <stdio.h>\n\n/*学生代码*/\n\nint main(void) {\n" + 打印 + "    return 0;\n}\n"
            '""";\n'
            "        var run = CppCheck.编译并运行(代码, 骨架);\n"
            "        if (!run.编译通过) return CppCheck.编译失败说明(run);\n"
            "        string[] 期望 = {\n" + 期望 + "        };\n"
            "        string[] 输入 = {\n" + 输入 + "        };\n"
            "        for (int i = 0; i < 期望.Length; i++)\n"
            "        {\n            // ⚠ 骨架打印的是 \"%d,\"（每个元素后都带逗号），而期望写的是 \"2,3,4\"
            //   ⇒ 比较前**去掉尾逗号**，否则永远差一个字符。
            string 实际行 = i < run.行.Count ? run.行[i].Trim().TrimEnd(',') : "";\n            if (i >= run.行.Count || 实际行 != 期望[i])\n"
            "                return CheckResult.不过(\n"
            '                    $"传入 {输入[i]} 之后应当是 {期望[i]}，" +\n'
            '                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\\n" +\n'
            f"                    {S('      ⚠ 要在函数里写 *p = ...，才会改到外面的变量；只写 p = ... 改的是指针自己。')});\n"
            '        return CheckResult.过("每一组都真的改到了外面的变量");\n') + 尾

    if 形 == "数组合用例":
        # ⚠⚠ 数组类函数有**两种**输出形态，我反复弄错就是因为没把它们分开：
        #   · `返回`   —— 函数**返回一个值**（如 求和）
        #                ⇒ 打印 `求和(数组, 个数)`，期望是**一个数**
        #   · `改数组` —— 函数**修改数组本身**（如 全部加一）
        #                ⇒ 打印改完之后的**整个数组**
        #   ⇒ 参数里**明写**哪一种，不要让生成器猜。
        #     （我在 09 / 15 两课之间来回改了六轮，每次修好一个另一个就坏 ——
        #       根因就是这两种形态被混在一个分支里。）
        模式 = "改数组"
        if 参 and 参[0] in ("返回", "改数组"):
            模式 = 参[0]
            参 = 参[1:]
        名 = 参[0]
        对 = list(zip(参[1::2], 参[2::2]))
        块 = []
        用例们 = []
        for 数, 期 in 对:
            元素 = [x for x in 数.split(",") if x != ""]
            # ⚠⚠ 空数组在这套骨架里**造不出合法的调用**：
            #   `求和(0, 0)`        ⇒ error: invalid conversion from 'int' to 'int*'
            #   `int a[]={0}; 求和(a, 0)` ⇒ 元素个数与传的长度不一致
            #   ⇒ **跳过这一组**。不测也比造出编不过的代码强。
            if not 元素:
                continue
            i = len(用例们)
            用例们.append((数, 期))
            块.append("    int __a%d[] = {%s};\n" % (i, ", ".join(元素)))
            if 模式 == "返回":
                块.append('    printf("%%d\\n", %s(__a%d, %d));\n' % (名, i, len(元素)))
            else:
                块.append("    %s(__a%d, %d);\n" % (名, i, len(元素)))
                块.append("    for (int __i = 0; __i < %d; __i++) printf(\"%%d,\", __a%d[__i]);\n"
                            % (len(元素), i))
                块.append('    printf("\\n");\n')
        期望 = "".join("            %s,\n" % S(期) for _, 期 in 用例们)
        输入 = "".join("            %s,\n" % S(数) for 数, _ in 用例们)
        return 头 + (
            '        var 骨架 = """\n'
            "#include <stdio.h>\n\n/*学生代码*/\n\nint main(void) {\n" + "".join(块) + "    return 0;\n}\n"
            '""";\n'
            "        var run = CppCheck.编译并运行(代码, 骨架);\n"
            "        if (!run.编译通过) return CppCheck.编译失败说明(run);\n"
            "        string[] 期望 = {\n" + 期望 + "        };\n"
            "        string[] 输入 = {\n" + 输入 + "        };\n"
            "        for (int i = 0; i < 期望.Length; i++)\n"
            "            if (i >= run.行.Count || run.行[i].Trim() != 期望[i])\n"
            "                return CheckResult.不过(\n"
            '                    $"数组 [{输入[i]}] 处理之后应当是 [{期望[i]}]\uff0c" +\n'
            '                    $"实际是 [{(i < run.行.Count ? run.行[i].Trim() : "（没输出）")}]。\\n" +\n'
            f"                    {S('      ⚠ 函数拿到的是地址 ⇒ 改的就是原数组（或返回值要对）。')});\n"
            '        return CheckResult.过("每一组都对");\n') + 尾

    if 形 == "统计用例":
        对 = list(zip(参[0::3], 参[1::3], 参[2::3]))
        块 = []
        for i, (数, _, _) in enumerate(对):
            元素 = [x for x in 数.split(",") if x != ""]
            块.append("    int __a%d[] = {%s};\n" % (i, ", ".join(元素) if 元素 else "0"))
            块.append("    int __d%d = -999, __x%d = 999;\n" % (i, i))
            块.append("    统计(__a%d, %d, &__d%d, &__x%d);\n" % (i, len(元素), i, i))
            块.append("    printf(\"%%d %%d\\n\", __d%d, __x%d);\n" % (i, i))
        期望 = "".join("            %s,\n" % S("%s %s" % (大, 小)) for _, 大, 小 in 对)
        输入 = "".join(f"            {S(数)},\n" for 数, _, _ in 对)
        return 头 + (
            '        var 骨架 = """\n'
            "#include <stdio.h>\n\n/*学生代码*/\n\nint main(void) {\n" + "".join(块) + "    return 0;\n}\n"
            '""";\n'
            "        var run = CppCheck.编译并运行(代码, 骨架);\n"
            "        if (!run.编译通过) return CppCheck.编译失败说明(run);\n"
            "        string[] 期望 = {\n" + 期望 + "        };\n"
            "        string[] 输入 = {\n" + 输入 + "        };\n"
            "        for (int i = 0; i < 期望.Length; i++)\n"
            "            if (i >= run.行.Count || run.行[i].Trim() != 期望[i])\n"
            "                return CheckResult.不过(\n"
            '                    $"数组 [{输入[i]}] 的最大/最小应当是 {期望[i]}，" +\n'
            '                    $"实际是「{(i < run.行.Count ? run.行[i].Trim() : "（没输出）")}」。\\n" +\n'
            f"                    {S('      ⚠ 两个坑：① 初值要从第一个元素起（写 0 在全负数时就会错）② 空数组要特判')});\n"
            '        return CheckResult.过("含全负数、单元素、空数组 都对");\n') + 尾

    if 形 == "需要输入":
        return 头 + (
            "        var run = CppCheck.直接运行(代码);\n"
            "        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);\n"
            f"        return CheckResult.过({S('能编译运行（这一课的程序要读输入，检查会喂一个数给它）')});\n") + 尾

    if 形 == "顺序":
        名 = 参[0]
        return 头 + (
            "        string 去注释 = 去掉注释(代码);\n"
            f"        int 主 = 去注释.IndexOf({S('int main')});\n"
            "        if (主 < 0)\n"
            f"            return CheckResult.不过({S('这一课要求写完整的 main —— 找不到 int main。')});\n"
            "        string 前 = 去注释.Substring(0, 主);\n"
            "        string 后 = 去注释.Substring(主);\n"
            f"        if (!前.Contains({S('int ' + 名)}))\n"
            f"            return CheckResult.不过({S('声明要写在 main **前面**（写法：int ' + 名 + '(int x); 末尾有分号），否则编译器读不到它。')});\n"
            f"        if (!后.Contains({S('int ' + 名)}))\n"
            f"            return CheckResult.不过({S('函数定义要写在 main **后面** —— 这一课练的就是这个顺序。')});\n"
            f"        return CheckResult.过({S('声明在 main 前、定义在 main 后 —— 顺序对了')});\n") + 尾

    return 头 + f"        return CheckResult.不过({S('（生成器不认识这种检查形态：' + 形 + '）')});\n" + 尾


文本 = io.open(TXT, encoding="utf-8").read()
课 = {}
当前 = None
for 行 in 文本.split("\n"):
    if 行.startswith("@@@ "):
        当前 = 行[4:].strip()
        课[当前] = {"标题": "", "要求": [], "模板": [], "答案": [], "检查": []}
        continue
    if 当前 is None or not 行.strip() or "|" not in 行:
        continue
    if 行.startswith("template_marker"):
        continue
    键, _, 值 = 行.partition("|")
    if 键 in ("标题", "要求", "模板", "答案", "检查"):
        if 键 in ("要求", "模板", "答案", "检查"):
            课[当前][键].append(值)
        else:
            课[当前][键] = 值

print("  解析到 %d 课" % len(课))
os.makedirs(DST, exist_ok=True)
for f in sorted(os.listdir(DST)):
    if f.endswith(".cs"):
        os.remove(os.path.join(DST, f))

for 编号 in sorted(课):
    k = 课[编号]
    # ⚠ 用 C# **原始字符串**写「要求」—— 换行就是换行，不拼、不转义
    要求 = '"""\n' + "\n".join(k["要求"]) + '\n"""'
    检查们 = "".join(生成检查(规.split("|", 1)[0], 规, i)
                    for i, 规 in enumerate(k["检查"], 1))
    体 = f'''using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("{编号[:2]}", "{k['标题']}",
    {要求})]
public static class 第{编号[:2]}课
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
    io.open(os.path.join(DST, f"第{编号[:2]}课_{编号[3:]}.cs"), "w",
            encoding="utf-8", newline="\r\n").write(体)
print("  已生成 16 个 .cs")
