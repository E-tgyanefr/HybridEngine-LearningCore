// LearnCode.CppLesson —— 写 C++ 课程时的检查辅助。
//
// ══ 这个文件是「课程作者」的接口 ══
//
// 每一课是一个 .cs 文件，里面写若干检查。检查要验证的是**学生写的 C++ 代码**，
// 所以每一条检查本质上都是：
//
//     把学生的 C++ 代码编译运行 → 看输出对不对 → 不对就说清楚哪不对
//
// 这件事每次重写一遍既啰嗦又容易错，所以收在这里。课程作者只需要用这几个方法：
//
//     CppCheck.编译并运行(学生代码)            → CppRun
//     CppCheck.输出等于(学生代码, "42")        → CheckResult
//     CppCheck.输出逐行(学生代码, "1", "2")    → CheckResult
//     CppCheck.能编译(学生代码)                → CheckResult
//     CppCheck.写了函数(学生代码, "平方")       → CheckResult
//     CppCheck.运行后(学生代码, 检查函数)       → CheckResult
//
// ══ 为什么课程作者不该自己拼「检查框架」 ══
//
// 因为**检查写错的代价不对称**：
//   · 检查太松 → 学生抄一遍就过，那一课白上
//   · 检查太严 → 正确解法被判错，学生莫名卡住然后放弃
// 两种错都不会自己暴露。集中在这里，才有可能被统一测试覆盖。

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LearnCode;

/// <summary>一次学生代码的运行结果（编译 + 执行）。</summary>
public sealed class CppRun
{
    public CppResult Raw = null!;
    /// <summary>学生代码本身有没有编译通过。</summary>
    public bool 编译通过 => !Raw.CompileFailed && Raw.Error == null;
    /// <summary>程序打印的所有行。</summary>
    public IReadOnlyList<string> 行 => Raw.Lines;
    /// <summary>合并的输出（用于查找关键字）。</summary>
    public string 全部输出 => string.Join("\n", Raw.Lines);
    /// <summary>编译输出（错误 + 警告）。</summary>
    public string 编译信息 => Raw.CompileOutput;
    /// <summary>编译失败或有正确性警告 —— 这两种情况都不该判通过。</summary>
    public bool 有问题 => Raw.CompileFailed || Raw.Error != null || Raw.HasCorrectnessWarning;
}

/// <summary>一条检查的结果：过没过 + 给学生的说明。</summary>
public readonly struct CheckResult
{
    public readonly bool Ok;
    public readonly string Detail;
    public CheckResult(bool ok, string detail) { Ok = ok; Detail = detail; }
    public static CheckResult 过(string detail = "") => new(true, detail);
    public static CheckResult 不过(string detail) => new(false, detail);
}

public static class CppCheck
{
    /// <summary>
    /// 学生代码的**默认程序骨架**。
    /// 里面有一个 /*学生代码*/ 占位符，会被替换成学生写的内容。
    ///
    /// 为什么需要：学生写的通常只是**一个函数**，而 C++ 必须有 main() 才能编译。
    /// 骨架由课程提供，学生就不用管 #include、不用管 main —— 少点干扰。
    /// 需要自定义输出时（比如打印多个结果），传自己的骨架。
    /// </summary>
    public const string 默认骨架 = """
#include <iostream>
#include <string>
#include <vector>
#include <cmath>
#include <algorithm>

/*学生代码*/

int main() {
    /*调用点*/
    return 0;
}
""";

    /// <summary>把学生代码塞进骨架并编译运行。</summary>
    public static CppRun 编译并运行(string 学生代码, string? 骨架 = null, string 调用点 = "")
    {
        string 用骨架 = 骨架 ?? 默认骨架;
        string 程序 = 用骨架.Replace("/*学生代码*/", 学生代码).Replace("/*调用点*/", 调用点);
        return new CppRun { Raw = CppRunner.BuildAndRun(程序) };
    }

    // ══════════════════ 常用检查 ══════════════════

    /// <summary>
    /// 直接编译运行学生代码，**不套任何骨架**。
    ///
    /// 什么时候用它：学生写的就是**完整程序**（自己带 main）的那种课 ——
    /// 比如第一课"让程序开口说话"，学生必须自己写 #include 和 main 才算学会。
    ///
    /// ⚠ 用错会出很迷惑的错误：学生写了 main，而骨架里也有 main 时，
    ///   GCC 报的是 "redefinition of 'int main()'" —— 学生会以为是自己写错了。
    ///   所以「这一课学生写完整程序、还是只写一个函数」必须由课程作者明确选。
    /// </summary>
    public static CppRun 直接运行(string 学生代码)
    {
        return new CppRun { Raw = CppRunner.BuildAndRun(学生代码) };
    }

    /// <summary>
    /// 一组测试用例：一段调用代码 + 它应该打印出的结果。
    ///
    /// 为什么需要：只拼一个调用点的检查测不了**带参数的函数** ——
    /// 而"同一个函数对不同输入给出不同结果"恰恰是函数这一课要考的东西。
    /// 一个用例写死输出、蒙对就过；一组用例蒙不过去。
    /// </summary>
    public readonly record struct 用例(string 说明, string 调用, string 期望);

    /// <summary>一次编译，跑完所有用例，逐条比对。</summary>
    public static CheckResult 逐个用例(string 学生代码, params 用例[] 用例们)
    {
        if (用例们.Length == 0) return CheckResult.不过("这道题没有测试用例（课程的问题）");

        // 一次编译，把所有用例的调用都塞进 main —— 编译一遍能省一两秒
        var 调用点 = new System.Text.StringBuilder();
        for (int i = 0; i < 用例们.Length; i++)
            调用点.Append("    std::cout << ").Append(用例们[i].调用).Append(" << \"\\n\";\n");

        var run = 编译并运行(学生代码, null, 调用点.ToString());
        if (!run.编译通过) return 编译失败说明(run);

        var 实际 = run.行;
        if (实际.Count != 用例们.Length)
            return CheckResult.不过(
                $"程序应该打印 {用例们.Length} 行（每个用例一行），但打印了 {实际.Count} 行。\n" +
                "      检查一下你的函数是不是对每个输入都打印了一次结果。");

        var 错的 = new List<string>();
        for (int i = 0; i < 用例们.Length; i++)
        {
            string 得到 = 实际[i].Trim();
            string 期望 = 用例们[i].期望.Trim();
            if (得到 != 期望)
                错的.Add($"      {用例们[i].说明}：得到 {显示(得到)}，应该是 {显示(期望)}");
        }
        if (错的.Count == 0)
            return CheckResult.过($"全部 {用例们.Length} 个用例都正确");

        return CheckResult.不过(
            $"通过 {用例们.Length - 错的.Count}/{用例们.Length}：\n" +
            string.Join("\n", 错的) + "\n" +
            "      对每个用例都想一遍：你的函数算出来的是什么？");
    }

    /// <summary>要求输出正好是这些行（逐行比较，忽略首尾空白）。</summary>
    public static CheckResult 输出逐行(string 学生代码, params string[] 期望行)
    {
        var run = 编译并运行(学生代码);
        if (!run.编译通过) return 编译失败说明(run);
        var 实际 = run.行;
        if (实际.Count == 期望行.Length)
        {
            bool 全同 = true;
            for (int i = 0; i < 实际.Count; i++)
                if (实际[i].Trim() != 期望行[i].Trim()) { 全同 = false; break; }
            if (全同) return CheckResult.过($"输出正确：{string.Join(" / ", 实际)}");
        }
        return CheckResult.不过(输出不符说明(期望行, 实际));
    }

    /// <summary>要求输出里包含某个片段（不关心别的输出）。</summary>
    public static CheckResult 输出包含(string 学生代码, string 片段)
    {
        var run = 编译并运行(学生代码);
        if (!run.编译通过) return 编译失败说明(run);
        if (run.全部输出.Contains(片段))
            return CheckResult.过($"输出里有「{片段}」");
        return CheckResult.不过(
            $"输出里应该有「{片段}」，但实际输出是：\n" +
            $"      {显示(run.全部输出)}\n" +
            $"      检查一下打印的内容和格式（空格、标点都算）。");
    }

    /// <summary>只要求能编译通过（不检查运行结果）。</summary>
    public static CheckResult 能编译(string 学生代码)
    {
        var run = 编译并运行(学生代码);
        if (run.Raw.CompileFailed) return 编译失败说明(run);
        if (run.Raw.HasCorrectnessWarning)
            return CheckResult.不过(
                "能编译，但编译器发现了隐患：\n" +
                ExplainCpp.ExplainWarning(run.编译信息));
        return CheckResult.过("编译通过，没有警告");
    }

    /// <summary>
    /// 要求源码里定义了指定的函数。
    ///
    /// 怎么验：写一个小程序去**调用**它。调用不了（编译不过）就说明没定义。
    /// 比用正则匹配源码可靠得多 —— 正则会被注释和字符串骗过去。
    /// </summary>
    public static CheckResult 写了函数(string 学生代码, string 函数名, string 调用表达式 = "")
    {
        string 调用 = 调用表达式.Length > 0 ? 调用表达式 : 函数名 + "()";
        string 骨架 = 默认骨架.Replace("/*调用点*/", $"std::cout << {调用} << \"\\n\";");
        var run = 编译并运行(学生代码, 骨架);
        if (!run.编译通过)
        {
            // 区分"没写这个函数"和"函数写了但里面有别的编译错误"
            if (run.编译信息.Contains(函数名) &&
                (run.编译信息.Contains("was not declared") || run.编译信息.Contains("not declared")))
                return CheckResult.不过(
                    $"没有找到「{函数名}」这个函数。\n" +
                    $"      写法：\n" +
                    $"          int {函数名}(...) {{\n" +
                    $"              ...\n" +
                    $"          }}\n" +
                    $"      注意：C++ 里函数的**返回类型必须写**，不能像 Python 那样只写 def。");
            return 编译失败说明(run);
        }
        return CheckResult.过($"「{函数名}」可以正常调用，输出 {显示(run.全部输出)}");
    }

    /// <summary>给需要看「运行结果对象」的复杂检查用。</summary>
    public static CheckResult 运行后(string 学生代码, Func<CppRun, CheckResult> 检查,
                                     string? 骨架 = null, string 调用点 = "")
    {
        var run = 编译并运行(学生代码, 骨架, 调用点);
        if (!run.编译通过) return 编译失败说明(run);
        return 检查(run);
    }

    // ══════════════════ 说明文字 ══════════════════

    /// <summary>编译失败时给学生的说明 —— 直接把翻译结果贴上去。</summary>
    public static CheckResult 编译失败说明(CppRun run)
    {
        if (run.Raw.CompileFailed)
            return CheckResult.不过(ExplainCpp.For(run.Raw.CompileOutput, run.Raw.CompileOutput, true)
                                    .TrimEnd());
        return CheckResult.不过("程序没能正常运行：\n      " + (run.Raw.Error ?? "未知原因"));
    }

    public static string 输出不符说明(IReadOnlyList<string> 期望, IReadOnlyList<string> 实际)
    {
        var sb = new StringBuilder();
        sb.Append("输出不对。\n");
        sb.Append("      期望（").Append(期望.Count).Append(" 行）：\n");
        foreach (var l in 期望) sb.Append("          ").Append(l).Append('\n');
        sb.Append("      实际（").Append(实际.Count).Append(" 行）：\n");
        if (实际.Count == 0) sb.Append("          （没有任何输出）\n");
        foreach (var l in 实际) sb.Append("          ").Append(l).Append('\n');
        sb.Append("      逐行对照一下，注意**数量**和**顺序**也要一致。");
        return sb.ToString();
    }

    private static string 显示(string s) => s.Length == 0 ? "（空）" : s.Replace("\n", " ⏎ ");
}

/// <summary>
/// 课程文件里的一条检查：名字 + 检查函数。
/// 课程作者用 CppLesson.收集(this) 把本文件里的检查交给运行器。
/// </summary>
/// <summary>
/// 给一条检查写一句**给人看的**说明。
///
/// 为什么不直接用函数名：函数名是为代码服务的（「检查_变量真的在用」），
/// 而列给学生看的应该是完整的话（「变量是不是真的参与了计算」）。
/// 两者要求不同，所以分开写。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class 检查说明Attribute : Attribute
{
    public string 文字;
    public 检查说明Attribute(string 文字) { this.文字 = 文字; }
}

public sealed record CppCheckItem(string 名字, Func<string, CheckResult> 检查);

public static class CppLesson
{
    /// <summary>
    /// 从一个「课程定义对象」里收集检查。
    ///
    /// 约定：课程文件里定义一堆 **public static CheckResult 检查_xxx(string 学生代码)**，
    /// 这个方法会自动把它们装上（按定义顺序）。
    /// 前缀用中文「检查_」而不是 CHECK_ —— 学生一眼能看出那是检查，而不是要写的代码。
    /// </summary>
    public static List<CppCheckItem> 收集(Type 课程类型)
    {
        var list = new List<CppCheckItem>();
        foreach (var m in 课程类型.GetMethods(System.Reflection.BindingFlags.Public
                                              | System.Reflection.BindingFlags.Static
                                              | System.Reflection.BindingFlags.DeclaredOnly))
        {
            if (!m.Name.StartsWith("检查_", StringComparison.Ordinal)) continue;
            if (m.ReturnType != typeof(CheckResult)) continue;
            var ps = m.GetParameters();
            if (ps.Length != 1 || ps[0].ParameterType != typeof(string)) continue;

            // 优先用 [检查说明] 里那句给人看的话；没写就退回函数名
            var attr = (检查说明Attribute?)Attribute.GetCustomAttribute(m, typeof(检查说明Attribute));
            string 名字 = attr?.文字 ?? m.Name.Substring(3);
            var fn = (Func<string, CheckResult>)(代码 => (CheckResult)m.Invoke(null, new object[] { 代码 })!);
            list.Add(new CppCheckItem(名字, fn));
        }
        return list;
    }
}
