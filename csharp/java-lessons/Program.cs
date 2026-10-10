// Java 课程的运行器 —— `dotnet run --project csharp/java-lessons`
//
// 用法与 C++ 课程完全一致（同一个 CLI 形状，学生不用学两套）：
//
//   dotnet run --project csharp/java-lessons                 列出所有课程
//   dotnet run --project csharp/java-lessons 01              看第 1 课的要求
//   dotnet run --project csharp/java-lessons 检查 01         检查你的答案
//   dotnet run --project csharp/java-lessons 答案 01         看参考答案
//
// ══ 为什么和 C++ 课程保持一模一样的形状 ══
//
// 学生学第二门语言时，最不该把精力花在"这门的工具怎么用"上。
// 命令一样、答题文件位置一样、检查报告一样 —— 他只需要专注语言本身。
// （这也是我刻意不去做"统一的多语言运行器"的原因：那种抽象会让每个语言
//   都变得不像自己，而学生要学的恰恰是"这门语言特有的东西"。）

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HybridEngine-light;

namespace HybridEngineLight.JavaLessons;

/// <summary>标在课程类型上，让运行器能发现它。</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class Java课程Attribute : Attribute
{
    public string 编号;
    public string 标题;
    public string 要做;
    public Java课程Attribute(string 编号, string 标题, string 要做)
    {
        this.编号 = 编号; this.标题 = 标题; this.要做 = 要做;
    }
}

public sealed class JavaCourseDef
{
    public string 编号 = "", 标题 = "", 要做 = "";
    public string 模板 = "", 参考答案 = "";
}

internal static class Program
{
    private static readonly List<(JavaCourseDef Def, Type 类型)> 全部 = new();

    private static int Main(string[] argv)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // 先确认 Java 工具链在不在 —— 不在的话后面每个检查都会失败，
        // 不如一开始就说清楚怎么装。
        if (JavaRunner.Compiler == null)
        {
            Console.WriteLine();
            Console.WriteLine("  " + JavaRunner.InstallHint.Replace("\n", "\n  "));
            Console.WriteLine();
            return 1;
        }

        发现课程();
        if (全部.Count == 0) { Console.WriteLine("\n  还没有任何 Java 课程。\n"); return 1; }
        全部.Sort((a, b) => string.CompareOrdinal(a.Def.编号, b.Def.编号));

        if (argv.Length == 0) return 列表();
        string cmd = argv[0];
        if (cmd is "检查" or "check") return argv.Length > 1 ? 检查(argv[1]) : 列表();
        if (cmd is "答案" or "answer") return argv.Length > 1 ? 看答案(argv[1]) : 列表();
        return 看课程(cmd);
    }

    private static void 发现课程()
    {
        foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
        {
            var attr = t.GetCustomAttribute<Java课程Attribute>();
            if (attr == null) continue;
            var def = new JavaCourseDef { 编号 = attr.编号, 标题 = attr.标题, 要做 = attr.要做 };

            var p = t.GetProperty("参考答案", BindingFlags.Public | BindingFlags.Static);
            if (p != null && p.PropertyType == typeof(string))
                def.参考答案 = (string?)p.GetValue(null) ?? "";

            var p2 = t.GetProperty("模板", BindingFlags.Public | BindingFlags.Static);
            if (p2 != null && p2.PropertyType == typeof(string))
                def.模板 = (string?)p2.GetValue(null) ?? "";

            全部.Add((def, t));
        }
    }

    private static string 根目录()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null)
        {
            if (File.Exists(Path.Combine(d.FullName, "README.md"))) return d.FullName;
            d = d.Parent;
        }
        return Directory.GetCurrentDirectory();
    }

    private static (JavaCourseDef, Type)? 找(string 编号)
    {
        foreach (var c in 全部)
            if (c.Def.编号 == 编号 || c.Def.编号.EndsWith(编号, StringComparison.Ordinal))
                return c;
        return null;
    }

    private static string 答案路径(JavaCourseDef def)
    {
        string 名 = 清理(def.标题);
        return Path.Combine(根目录(), "java-answers", $"{def.编号}_{名}.java");
    }

    private static string 清理(string s)
    {
        int i = s.IndexOf('：');
        string t = i > 0 ? s.Substring(i + 1) : s;
        int j = t.IndexOf('—');
        if (j > 0) t = t.Substring(0, j);
        var sb = new System.Text.StringBuilder();
        foreach (var c in t) if (char.IsLetterOrDigit(c)) sb.Append(c);
        return sb.Length > 0 ? sb.ToString() : "作业";
    }

    private static string 生成模板(JavaCourseDef def)
    {
        if (def.模板.Length > 0) return def.模板;
        var sb = new System.Text.StringBuilder();
        sb.Append("// ").Append(def.标题).Append('\n');
        sb.Append("//\n// 要做：\n");
        foreach (var l in def.要做.Split('\n')) sb.Append("//   ").Append(l.TrimEnd()).Append('\n');
        sb.Append("//\n// 写完后运行：\n");
        sb.Append("//   dotnet run --project csharp/java-lessons 检查 ").Append(def.编号).Append('\n');
        sb.Append("//\n// ⚠ Java 的公共类名必须和文件名一致。这个文件叫 ")
          .Append(Path.GetFileName(答案路径(def)))
          .Append("，\n//   所以你的类名要用 Main（JavaRunner 固定按 Main.java 编译）。\n");
        sb.Append("//\n// ── 在下面写你的代码 ──\n\npublic class Main {\n\n}\n");
        return sb.ToString();
    }

    private static int 列表()
    {
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine("  Java 课程");
        Console.WriteLine("════════════════════════════════════════════════════════");
        foreach (var (def, t) in 全部)
            Console.WriteLine($"  {def.编号}   {def.标题}   （{JavaLesson.收集(t).Count} 项检查）");
        Console.WriteLine();
        Console.WriteLine("  看某一课：   dotnet run --project csharp/java-lessons 01");
        Console.WriteLine("  检查答案：   dotnet run --project csharp/java-lessons 检查 01");
        Console.WriteLine("  参考答案：   dotnet run --project csharp/java-lessons 答案 01");
        Console.WriteLine();
        return 0;
    }

    private static int 看课程(string 编号)
    {
        var f = 找(编号);
        if (f == null) { Console.WriteLine($"\n  没有找到「{编号}」这一课。\n"); return 1; }
        var (def, t) = f.Value;
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine($"  {def.标题}");
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine(def.要做);
        Console.WriteLine();
        Console.WriteLine("────────────────────────────────────────────────────────");
        Console.WriteLine($"  这一课有 {JavaLesson.收集(t).Count} 项检查：");
        foreach (var c in JavaLesson.收集(t)) Console.WriteLine($"    · {c.名字}");
        Console.WriteLine();
        Console.WriteLine($"  答题文件：{答案路径(def)}");
        Console.WriteLine();
        return 0;
    }

    private static int 检查(string 编号)
    {
        var f = 找(编号);
        if (f == null) { Console.WriteLine($"\n  没有找到「{编号}」这一课。\n"); return 1; }
        var (def, t) = f.Value;

        string path = 答案路径(def);
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, 生成模板(def), new System.Text.UTF8Encoding(false));
            Console.WriteLine();
            Console.WriteLine(new string('─', 56));
            Console.WriteLine($"  给你建好了答题文件：{path}");
            Console.WriteLine("  打开它，按里面的说明写代码，然后再跑一次这条命令。");
            Console.WriteLine(new string('─', 56));
            Console.WriteLine();
            return 1;
        }

        string 代码 = File.ReadAllText(path);
        if (代码.Trim().Length == 0)
        {
            Console.WriteLine($"\n  答题文件还是空的：{path}\n");
            return 1;
        }

        Console.WriteLine("\n  正在编译运行你的代码…（第一次会慢一两秒）");
        var 明细 = new List<(string, JavaCheckResult)>();
        int 通过 = 0;
        foreach (var c in JavaLesson.收集(t))
        {
            JavaCheckResult r;
            try { r = c.检查(代码); }
            catch (Exception e) { r = JavaCheckResult.不过($"检查无法进行：{e.GetType().Name}: {e.Message}"); }
            if (r.Ok) 通过++;
            明细.Add((c.名字, r));
        }

        Console.WriteLine();
        Console.WriteLine(new string('─', 56));
        Console.WriteLine($"  检查结果：{def.标题}");
        Console.WriteLine(new string('─', 56));
        foreach (var (名字, 结果) in 明细)
        {
            Console.WriteLine($"  {(结果.Ok ? "✓" : "✗")} {名字}");
            if (结果.Detail.Length > 0)
                foreach (var l in 结果.Detail.Split('\n'))
                    Console.WriteLine("      " + l);
        }
        Console.WriteLine();
        if (明细.Count > 0 && 通过 == 明细.Count)
        {
            Console.WriteLine($"  全部通过（{通过}/{明细.Count}）—— 这一课过了！");
            Console.WriteLine("  继续下一课吧。");
        }
        else
        {
            Console.WriteLine($"  通过 {通过}/{明细.Count}。");
            Console.WriteLine("  上面 ✗ 的部分就是还要改的地方 —— 按提示改完再跑一次。");
        }
        Console.WriteLine();
        return 0;
    }

    private static int 看答案(string 编号)
    {
        var f = 找(编号);
        if (f == null) { Console.WriteLine($"\n  没有找到「{编号}」这一课。\n"); return 1; }
        var (def, _) = f.Value;
        if (def.参考答案.Length == 0) { Console.WriteLine("\n  这一课还没有参考答案。\n"); return 1; }
        Console.WriteLine();
        Console.WriteLine("── 参考答案 ─────────────────────────────────────────");
        Console.WriteLine("  （先自己写一遍再看。抄一遍不如写错一遍学得多。）");
        Console.WriteLine(new string('─', 56));
        Console.WriteLine(def.参考答案);
        return 0;
    }
}
