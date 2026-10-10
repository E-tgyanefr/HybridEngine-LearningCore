// HybridEngineLight.JavaLesson —— 写 Java 课程时的检查辅助。
//
// 与 CppLesson 同一套设计，只是把"编译运行学生代码"换成 Java 的。
// 为什么每个语言都要一份而不是做成通用的：
//   通用版要么靠字符串拼语言差异（脆弱），要么靠反射（读不懂）。
//   而每份只有一两百行、写清该语言特有的坑，比一个抽象层更有用 ——
//   学生看到的是"Java 的错在哪"，不是"某个泛化抽象说了什么"。

using System;
using System.Collections.Generic;
using System.Text;

namespace HybridEngineLight;

/// <summary>一次学生 Java 代码的运行结果。</summary>
public sealed class JavaRun
{
    public JavaResult Raw = null!;
    public bool 编译通过 => !Raw.CompileFailed && Raw.Error == null;
    public IReadOnlyList<string> 行 => Raw.Lines;
    public string 全部输出 => string.Join("\n", Raw.Lines);
    public string 编译信息 => Raw.CompileOutput;
}

/// <summary>一条检查的结果。</summary>
public readonly struct JavaCheckResult
{
    public readonly bool Ok;
    public readonly string Detail;
    public JavaCheckResult(bool ok, string detail) { Ok = ok; Detail = detail; }
    public static JavaCheckResult 过(string detail = "") => new(true, detail);
    public static JavaCheckResult 不过(string detail) => new(false, detail);
}

public static class JavaCheck
{
    /// <summary>
    /// 学生代码的默认骨架。
    ///
    /// ⚠ Java 的**公共类名必须和文件名一致**。JavaRunner 固定用 Main.java，
    ///   所以骨架里的类名必须是 Main —— 这一点和 C++ 不同（C++ 无所谓）。
    ///   课程作者如果自己写骨架，也要守这条。
    /// </summary>
    public const string 默认骨架 = """
public class Main {
    /*学生代码*/

    public static void main(String[] args) {
        /*调用点*/
    }
}
""";

    /// <summary>把学生代码塞进骨架并编译运行。</summary>
    public static JavaRun 编译并运行(string 学生代码, string? 骨架 = null, string 调用点 = "")
    {
        string 用骨架 = 骨架 ?? 默认骨架;
        string 程序 = 用骨架.Replace("/*学生代码*/", 学生代码).Replace("/*调用点*/", 调用点);
        return new JavaRun { Raw = JavaRunner.BuildAndRun(程序) };
    }

    /// <summary>
    /// 直接编译运行学生代码，**不套骨架**。
    /// 学生自己写了完整类（含 main）的课用这个。
    /// </summary>
    public static JavaRun 直接运行(string 学生代码)
    {
        return new JavaRun { Raw = JavaRunner.BuildAndRun(学生代码) };
    }

    /// <summary>一组测试用例：一段调用代码 + 它应该打印出的结果。</summary>
    public readonly record struct 用例(string 说明, string 调用, string 期望);

    /// <summary>一次编译，跑完所有用例，逐条比对。</summary>
    public static JavaCheckResult 逐个用例(string 学生代码, params 用例[] 用例们)
    {
        if (用例们.Length == 0) return JavaCheckResult.不过("这道题没有测试用例（课程的问题）");

        var 调用点 = new StringBuilder();
        for (int i = 0; i < 用例们.Length; i++)
            调用点.Append("        System.out.println(").Append(用例们[i].调用).Append(");\n");

        var run = 编译并运行(学生代码, null, 调用点.ToString());
        if (!run.编译通过) return 编译失败说明(run);

        var 实际 = run.行;
        if (实际.Count != 用例们.Length)
            return JavaCheckResult.不过(
                $"程序应该打印 {用例们.Length} 行（每个用例一行），但打印了 {实际.Count} 行。");

        var 错的 = new List<string>();
        for (int i = 0; i < 用例们.Length; i++)
        {
            string 得到 = 实际[i].Trim();
            string 期望 = 用例们[i].期望.Trim();
            if (得到 != 期望)
                错的.Add($"      {用例们[i].说明}：得到 {显示(得到)}，应该是 {显示(期望)}");
        }
        if (错的.Count == 0) return JavaCheckResult.过($"全部 {用例们.Length} 个用例都正确");

        return JavaCheckResult.不过(
            $"通过 {用例们.Length - 错的.Count}/{用例们.Length}：\n" +
            string.Join("\n", 错的) + "\n" +
            "      对每个用例都想一遍：你的方法算出来的是什么？");
    }

    public static JavaCheckResult 输出逐行(string 学生代码, params string[] 期望行)
    {
        var run = 编译并运行(学生代码);
        if (!run.编译通过) return 编译失败说明(run);
        var 实际 = run.行;
        if (实际.Count == 期望行.Length)
        {
            bool 全同 = true;
            for (int i = 0; i < 实际.Count; i++)
                if (实际[i].Trim() != 期望行[i].Trim()) { 全同 = false; break; }
            if (全同) return JavaCheckResult.过($"输出正确：{string.Join(" / ", 实际)}");
        }
        return JavaCheckResult.不过(输出不符说明(期望行, 实际));
    }

    public static JavaCheckResult 输出包含(string 学生代码, string 片段)
    {
        var run = 编译并运行(学生代码);
        if (!run.编译通过) return 编译失败说明(run);
        if (run.全部输出.Contains(片段)) return JavaCheckResult.过($"输出里有「{片段}」");
        return JavaCheckResult.不过(
            $"输出里应该有「{片段}」，但实际是：\n      {显示(run.全部输出)}");
    }

    public static JavaCheckResult 能编译(string 学生代码)
    {
        var run = 编译并运行(学生代码);
        if (run.Raw.CompileFailed) return 编译失败说明(run);
        return JavaCheckResult.过("编译通过");
    }

    public static JavaCheckResult 编译失败说明(JavaRun run)
    {
        if (run.Raw.CompileFailed)
            return JavaCheckResult.不过(
                ExplainJava.For(run.Raw.CompileOutput, run.Raw.CompileOutput, true).TrimEnd());
        return JavaCheckResult.不过("程序没能正常运行：\n      " + (run.Raw.Error ?? "未知原因"));
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

/// <summary>给一条检查写一句给人看的说明。</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class Java检查说明Attribute : Attribute
{
    public string 文字;
    public Java检查说明Attribute(string 文字) { this.文字 = 文字; }
}

public sealed record JavaCheckItem(string 名字, Func<string, JavaCheckResult> 检查);

public static class JavaLesson
{
    /// <summary>收集「检查_」开头的静态方法（签名固定：string → JavaCheckResult）。</summary>
    public static List<JavaCheckItem> 收集(Type 课程类型)
    {
        var list = new List<JavaCheckItem>();
        foreach (var m in 课程类型.GetMethods(System.Reflection.BindingFlags.Public
                                              | System.Reflection.BindingFlags.Static
                                              | System.Reflection.BindingFlags.DeclaredOnly))
        {
            if (!m.Name.StartsWith("检查_", StringComparison.Ordinal)) continue;
            if (m.ReturnType != typeof(JavaCheckResult)) continue;
            var ps = m.GetParameters();
            if (ps.Length != 1 || ps[0].ParameterType != typeof(string)) continue;

            var attr = (Java检查说明Attribute?)Attribute.GetCustomAttribute(m, typeof(Java检查说明Attribute));
            string 名字 = attr?.文字 ?? m.Name.Substring(3);
            var fn = (Func<string, JavaCheckResult>)(代码 =>
                (JavaCheckResult)m.Invoke(null, new object[] { 代码 })!);
            list.Add(new JavaCheckItem(名字, fn));
        }
        return list;
    }
}
