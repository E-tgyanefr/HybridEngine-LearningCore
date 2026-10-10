using System;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("03", "第 03 课：条件判断 —— 让程序做选择",
    "写一个函数 `const char* 判等级(int 分)`：\n" +
    "  ≥90 返回 \"优秀\"、≥60 返回 \"及格\"、否则 \"不及格\"\n" +
    "（用 if / else if / else）")]
public static class 第03课
{
    public static string 模板 => """
// 第 03 课：条件判断 —— 让程序做选择
//
// 要做：写一个函数
//
//     const char* 判等级(int 分);
//
//   · 分 >= 90  → 返回 "优秀"
//   · 分 >= 60  → 返回 "及格"
//   · 其他      → 返回 "不及格"
//
// ⚠ 三个容易踩的地方：
//   1. 返回值类型是 `const char*`（**文字用双引号**，是字符串不是单字符）
//      单引号 '优' 是**字符**，类型不匹配 —— 这是 C 里最常见的报错之一
//   2. `>= 90` 不能写成 `> 90`（90 分本身也是优秀）
//   3. 顺序要从高到低判断；反过来写（先 >= 60）会让 95 分也被判成"及格"
//
// 写完后运行：dotnet run --project csharp/c-lessons 检查 03

// ── 在下面写你的函数 ──
// 这一课**不用**写 main（本课只考你写函数，main 由课程提供）。

""";

    public static string 参考答案 => """
const char* 判等级(int 分) {
    if (分 >= 90) return "优秀";
    else if (分 >= 60) return "及格";
    else return "不及格";
}
""";

    [检查说明("编译通过，且函数签名对")]
    public static CheckResult 检查_能编译(string 代码)
    {
        // ⚠ 这里传的必须是**表达式**，不是一条完整语句。
        //   我第一版传了 `printf("%s\n", 判等级(95));`（带分号），
        //   而 `写了函数` 会把它当"要打印的值"包一层 ⇒ 生成
        //       printf("%d\n", (int)(printf("%s\n", 判等级(95));));
        //   两处 printf 叠在一起，编译器报 expected ')' before ';' ——
        //   而那是**课程自己的错**，学生却会以为是自己写错了。
        //   ⇒ 只传调用表达式，打印方式交给检查机制（它按语言选 %d / std::cout）。
        //   ⚠ 判等级返回字符串，所以这一项只验证「函数存在 + 能编译」，
        //     返回值对不对由下面那项「边界值」检查。
        var run = CppCheck.写了函数(代码, "判等级", "判等级(95) != 0");
        if (!run.Ok) return run;
        return CheckResult.过();
    }

    [检查说明("边界值：95 / 90 / 60 / 59 都对")]
    public static CheckResult 检查_边界(string 代码)
    {
        var run = CppCheck.逐个用例(代码,
            new CppCheck.用例("95 分", "判等级(95) == NULL ? 0 : 0", ""),
            new CppCheck.用例("dummy", "0", ""));
        // 上面那种写法不适用于字符串；改用自定义骨架一次跑完所有边界
        return 边界检查(代码);
    }

    private static CheckResult 边界检查(string 代码)
    {
        var 骨架 = """
#include <stdio.h>
#include <string.h>

/*学生代码*/

int main(void) {
    printf("%s\n", 判等级(95));
    printf("%s\n", 判等级(90));
    printf("%s\n", 判等级(60));
    printf("%s\n", 判等级(59));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        var 行 = run.行;
        string[] 期望 = { "优秀", "优秀", "及格", "不及格" };
        if (行.Count < 4)
            return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
        {
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例错了：应该是「{期望[i]}」，实际是「{行[i].Trim()}」。\n" +
                    "      四个用例是 95 / 90 / 60 / 59 —— 重点看 **90 和 60 这两个边界**。\n" +
                    "      （`> 90` 和 `>= 90` 的差别就在这里。）");
        }
        return CheckResult.过("95→优秀 · 90→优秀 · 60→及格 · 59→不及格");
    }

    [检查说明("返回值是字符串（双引号），不是字符（单引号）")]
    public static CheckResult 检查_双引号(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains("'优秀'") || 去注释.Contains("'及格'") || 去注释.Contains("'不及格'"))
            return CheckResult.不过(
                "用了**单引号**包文字。\n" +
                "      单引号在 C 里是**单个字符**（'A'），多个字符要用**双引号**（\"优秀\"）。\n" +
                "      `'优秀'` 这种写法编译器会报 multi-character character constant。");
        return CheckResult.过();
    }

    [检查说明("用的是 if / else if（不是三个独立的 if）")]
    public static CheckResult 检查_用了elseif(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        int if数 = 0;
        for (int i = 0; i + 1 < 去注释.Length; i++)
            if (去注释[i] == 'i' && 去注释.Substring(i).StartsWith("if")) if数++;
        if (if数 < 2)
            return CheckResult.不过("只看到 1 个 if。三个分支要用 if / else if / else。");
        if (!去注释.Contains("else"))
            return CheckResult.不过("没看到 else。要写成 if / else if / else。");
        return CheckResult.过();
    }

    internal static string 去掉注释(string s)
    {
        var o = new System.Text.StringBuilder();
        bool 块 = false;
        for (int i = 0; i < s.Length; i++)
        {
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '/')
            { while (i < s.Length && s[i] != '\n') i++; o.Append('\n'); continue; }
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') { 块 = true; i++; continue; }
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') { 块 = false; i++; continue; }
            if (!块) o.Append(s[i]);
        }
        return o.ToString();
    }
}

