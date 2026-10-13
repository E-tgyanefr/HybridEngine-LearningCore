// HybridEngineLight.CsCourse —— C# 课程的定义与运行。
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
        sb.Append("// C#\n");
        sb.Append("// ").Append(def.标题).Append('\n');
        sb.Append("//\n// 要做：\n");
        foreach (var l in def.要做.Split('\n'))
            sb.Append("//   ").Append(l.TrimEnd()).Append('\n');
        sb.Append("//\n// 写完之后运行：\n");
        sb.Append("//   dotnet run --project csharp/cs-lessons 检查 ").Append(def.编号).Append('\n');
        sb.Append("//\n// ── 在下面写你的代码 ──\n\n");
        return sb.ToString();
    }
}
