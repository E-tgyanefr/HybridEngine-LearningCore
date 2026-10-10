using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("01", "第 01 课：输出与变量 —— 让程序开口说话",
    "写一个方法 `static void 打招呼()`：\n" +
    "  1. 打印一行：你好，C#\n" +
    "  2. 定义 int 变量 甲 = 6、乙 = 7\n" +
    "  3. 打印它们的和（用变量相加，不要写死 13）")]
public static class 第01课
{
    public static string 模板 => """
// 第 01 课：输出与变量
//
// 要做：
//   1. 打印一行：你好，C#
//   2. 定义 int 变量 甲 = 6、int 变量 乙 = 7
//   3. 打印它们的和（用变量相加，不要写死 13）
//
// 记住三件事：
//   · 每条语句末尾要有分号 ;
//   · 打印用 Console.WriteLine(...)
//   · 字符串用双引号 "..."，单个字符才用单引号 '...'
//
// ⚠ C# 是**编译型**语言 —— 但 .NET SDK 自带编译器，
//   所以不用像 C++ / Java 那样另外装 g++ / JDK。
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 01

// ── 在下面写你的方法 ──
// 这一课**不用**写 Main（本课只考你写方法，Main 由课程提供）。

""";

    public static string 参考答案 => """
static void 打招呼()
{
    Console.WriteLine("你好，C#");

    int 甲 = 6;
    int 乙 = 7;
    Console.WriteLine(甲 + 乙);
}
""";

    [检查说明("编译通过，且方法签名对")]
    public static CheckResult 检查_能编译(string 代码)
    {
        var r = CppCheck.写了函数(代码, "打招呼", "打招呼()");
        if (!r.Ok) return r;
        return CheckResult.过();
    }

    [检查说明("打印了「你好，C#」这一行")]
    public static CheckResult 检查_第一行文字(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        var 行 = run.行;
        if (行.Count < 1) return CheckResult.不过("一行都没打印。");
        if (!行[0].Contains("你好，C#"))
            return CheckResult.不过(
                $"第一行应该是「你好，C#」，实际是「{行[0]}」。\n" +
                "      注意：逗号是中文逗号，C# 是大写 S（C# 里 # 就是普通字符）。");
        return CheckResult.过($"第一行 = {行[0]}");
    }

    [检查说明("第二行是 13（= 6 + 7）")]
    public static CheckResult 检查_和(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        var 行 = run.行;
        if (行.Count < 2)
            return CheckResult.不过($"应该有 2 行输出，实际 {行.Count} 行 —— 是不是忘了打印和？");
        if (行[1].Trim() != "13")
            return CheckResult.不过(
                $"第二行应该是 13，实际是「{行[1].Trim()}」。\n" +
                "      检查：变量是 6 和 7 吗？真的用 + 相加了吗？");
        return CheckResult.过();
    }

    [检查说明("真的用了变量相加（不是写死 13）")]
    public static CheckResult 检查_不是硬写(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains("13"))
            return CheckResult.不过(
                "代码里出现了字面量 13 —— 那是把答案写死了。\n" +
                "      要求用两个变量相加：Console.WriteLine(甲 + 乙);");
        if (!去注释.Contains("+"))
            return CheckResult.不过("没看到加法运算。要用 甲 + 乙，不是直接打印一个数。");
        return CheckResult.过();
    }

    [检查说明("用了 Console.WriteLine（不是 Write）")]
    public static CheckResult 检查_用对了打印(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("Console"))
            return CheckResult.不过("没看到 Console —— C# 的打印是 Console.WriteLine(...)");
        // ⚠ Write 不换行 ⇒ 两行输出会挤成一行。新手常犯。
        if (去注释.Contains("Console.Write(") && !去注释.Contains("Console.WriteLine("))
            return CheckResult.不过(
                "你用的是 `Console.Write`（不换行）—— 两行输出会挤成一行。\n" +
                "      要换行请用 `Console.WriteLine`。");
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
