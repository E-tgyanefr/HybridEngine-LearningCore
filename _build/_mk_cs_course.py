import io
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
LIB = os.path.join(ROOT, "csharp", "HybridEngineLight")
C = os.path.join(ROOT, "csharp", "c-lessons")  # 只用来读一个样板
CS = os.path.join(ROOT, "csharp", "cs-lessons")
os.makedirs(os.path.join(CS, "Lessons"), exist_ok=True)

# ══════════════════════ ① CsCourse.cs（课程库） ══════════════════════
io.open(os.path.join(LIB, "CsCourse.cs"), "w", encoding="utf-8", newline="\r\n").write(
    '''// HybridEngineLight.CsCourse —— C# 课程的定义与运行。
//
// ══ 为什么 C# 课程能"看起来像不需要编译器" ══
//
// C# 与其他编译语言不同：只要机器上有 .NET SDK，`dotnet` 自己就是编译器。
// 所以 C# 课的"跑学员代码"**不需要额外装任何编译器** ——
// `CSharpRunner` 现场把学员的代码编译成一个临时程序再跑。
//
// ⚠ 这一点值得对学员讲清楚：C# 是**编译型**语言，
//   只是因为 .NET SDK 同时带了编译器，才没有 C++ / Java 那种"还要装 g++/JDK"的墙。
//   课程里会把这件事说出来 —— 否则学员会以为"编译型语言都不用装东西"。
using System;
using System.IO;
using System.Text;

namespace HybridEngineLight;

public static class CsCourse
{
    /// <summary>C# 课答题文件所在目录（相对仓库根）。</summary>
    public const string 答题目录 = "cs-answers";

    /// <summary>某一课的答题文件路径。</summary>
    public static string 答案路径(string 根目录, CourseDef def) =>
        Path.Combine(根目录, 答题目录, $"{def.编号}_{清理(def.标题)}.cs");

    /// <summary>从标题里取一段干净的名字做文件名（与 CppCourse / CCourse 同一套规则）。</summary>
    private static string 清理(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s)
            if (char.IsLetterOrDigit(c)) sb.Append(c);
        return sb.Length > 0 ? sb.ToString() : "作业";
    }

    /// <summary>第一次检查时自动生成答题模板。</summary>
    public static string 生成模板(CourseDef def)
    {
        if (def.模板.Length > 0) return def.模板;
        var sb = new StringBuilder();
        sb.Append("// C#\\n");
        sb.Append("// ").Append(def.标题).Append('\\n');
        sb.Append("//\\n// 要做：\\n");
        foreach (var l in def.要做.Split('\\n'))
            sb.Append("//   ").Append(l.TrimEnd()).Append('\\n');
        sb.Append("//\\n// 写完之后运行：\\n");
        sb.Append("//   dotnet run --project csharp/cs-lessons 检查 ").Append(def.编号).Append('\\n');
        sb.Append("//\\n// ── 在下面写你的代码 ──\\n\\n");
        return sb.ToString();
    }
}
''')
print("  (1) HybridEngineLight/CsCourse.cs")

# ══════════════════════ ② cs-lessons 工程 ══════════════════════
io.open(os.path.join(CS, "CsLessons.csproj"), "w", encoding="utf-8", newline="\r\n").write(
    '''<Project Sdk="Microsoft.NET.Sdk">
  <!-- C# 课程：课程定义（.cs，装检查逻辑）+ 学生作业（.cs，装学生代码）。
       ⚠ 学生作业在这里是**字符串**（写在答题文件里），由 CSharpRunner
         现场编译成临时程序再跑 —— 所以本工程不需要在编译期看到它们。 -->
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <AssemblyName>HybridEngineLight.CsLessons</AssemblyName>
    <RootNamespace>HybridEngineLight.CsLessons</RootNamespace>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\\HybridEngineLight\\HybridEngineLight.csproj" />
  </ItemGroup>
</Project>
''')
print("  (2) cs-lessons/CsLessons.csproj")

# ══════════════════════ ③ Program.cs（运行器） ══════════════════════
io.open(os.path.join(CS, "Program.cs"), "w", encoding="utf-8", newline="\r\n").write(
    '''// C# 课程的运行器 —— `dotnet run --project csharp/cs-lessons`
//
// ══ 为什么课程是「自动发现」而不是「手动注册」══
//
// 课程定义散在 Lessons/*.cs 里，一人写一课。
// 如果每加一课都要在某个中心文件里登记一行，那多人同时写课**必然改同一个文件**。
// ⇒ `[课程]` 特性 + 反射扫描，加一课不用碰任何中心文件。
using System;
using System.IO;
using System.Reflection;
using HybridEngineLight;

namespace HybridEngineLight.CsLessons;

/// <summary>标在课程类型上，让运行器能发现它。</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class 课程Attribute : Attribute
{
    public string 编号;
    public string 标题;
    /// <summary>「要做」段落。</summary>
    public string 要做;
    public 课程Attribute(string 编号, string 标题, string 要做)
    {
        this.编号 = 编号; this.标题 = 标题; this.要做 = 要做;
    }
}

internal static class Program
{
    private static int Main(string[] argv)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        发现课程();

        if (CppCourse.全部.Count == 0)
        {
            Console.WriteLine("\\n  还没有任何 C# 课程。\\n");
            return 1;
        }
        CppCourse.全部.Sort((a, b) => string.CompareOrdinal(a.Def.编号, b.Def.编号));

        if (argv.Length == 0) return 列表();

        string cmd = argv[0];
        if (cmd is "检查" or "check")
            return argv.Length > 1 ? 检查(argv[1]) : 列表();
        if (cmd is "答案" or "answer")
            return argv.Length > 1 ? 看答案(argv[1]) : 列表();

        return 看课程(cmd);
    }

    private static void 发现课程()
    {
        foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
        {
            var attr = t.GetCustomAttribute<课程Attribute>();
            if (attr == null) continue;
            var def = new CourseDef(attr.编号, attr.标题, attr.要做);

            var p = t.GetProperty("参考答案", BindingFlags.Public | BindingFlags.Static);
            if (p != null && p.PropertyType == typeof(string))
                def.参考答案 = (string)p.GetValue(null) ?? "";

            var p2 = t.GetProperty("模板", BindingFlags.Public | BindingFlags.Static);
            if (p2 != null && p2.PropertyType == typeof(string))
                def.模板 = (string)p2.GetValue(null) ?? "";

            CppCourse.注册(def, t);
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

    private static int 列表()
    {
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine("  C# 课程");
        Console.WriteLine("════════════════════════════════════════════════════════");
        foreach (var (def, t) in CppCourse.全部)
        {
            int n = CppCourse.检查列表(t).Count;
            Console.WriteLine($"  {def.编号}   {def.标题}   （{n} 项检查）");
        }
        Console.WriteLine();
        Console.WriteLine("  看某一课：   dotnet run --project csharp/cs-lessons 01");
        Console.WriteLine("  检查答案：   dotnet run --project csharp/cs-lessons 检查 01");
        Console.WriteLine("  参考答案：   dotnet run --project csharp/cs-lessons 答案 01");
        Console.WriteLine();
        Console.WriteLine($"  你的答案文件在 {CsCourse.答题目录}/ 目录下（第一次检查时自动生成）。");
        Console.WriteLine();
        Console.WriteLine("  ⚠ C# 也是**编译型**语言 —— 只是 .NET SDK 自带编译器，");
        Console.WriteLine("    所以不用像 C++ / Java 那样另外装 g++ / JDK。");
        Console.WriteLine();
        return 0;
    }

    private static int 看课程(string 编号)
    {
        var f = CppCourse.找(编号);
        if (f == null) { Console.WriteLine($"\\n  没有找到「{编号}」这一课。\\n"); return 1; }
        var (def, t) = f.Value;

        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine($"  {def.标题}");
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine(def.要做);
        Console.WriteLine();
        Console.WriteLine("────────────────────────────────────────────────────────");
        Console.WriteLine($"  这一课有 {CppCourse.检查列表(t).Count} 项检查：");
        foreach (var c in CppCourse.检查列表(t)) Console.WriteLine($"    · {c.名字}");
        Console.WriteLine();
        Console.WriteLine($"  答题文件：{CsCourse.答案路径(根目录(), def)}");
        Console.WriteLine($"  （第一次运行「检查 {def.编号}」时会自动生成）");
        Console.WriteLine();
        return 0;
    }

    private static int 检查(string 编号)
    {
        var f = CppCourse.找(编号);
        if (f == null) { Console.WriteLine($"\\n  没有找到「{编号}」这一课。\\n"); return 1; }
        var (def, t) = f.Value;

        string path = CsCourse.答案路径(根目录(), def);
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, CsCourse.生成模板(def), new System.Text.UTF8Encoding(false));
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
            Console.WriteLine($"\\n  答题文件还是空的：{path}\\n  打开它写点东西再来检查。\\n");
            return 1;
        }

        Console.WriteLine($"\\n  正在编译运行你的代码…（第一次会慢一两秒）");
        var (通过, 总数, 明细) = CppCourse.运行(t, 代码);
        Console.Write(CppCourse.报告(def, 通过, 总数, 明细));
        return 0;
    }

    private static int 看答案(string 编号)
    {
        var f = CppCourse.找(编号);
        if (f == null) { Console.WriteLine($"\\n  没有找到「{编号}」这一课。\\n"); return 1; }
        var (def, _) = f.Value;
        if (def.参考答案.Length == 0)
        {
            Console.WriteLine($"\\n  这一课还没有参考答案。\\n");
            return 1;
        }
        Console.WriteLine();
        Console.WriteLine("── 参考答案 ────────────────────────────────────────────");
        Console.WriteLine("  （先自己写一遍再看。抄一遍不如写错一遍学得多。）");
        Console.WriteLine(new string('─', 56));
        Console.WriteLine(def.参考答案);
        return 0;
    }
}
''')
print("  (3) cs-lessons/Program.cs")
print("  完成 课程库 / 工程 / 运行器")
