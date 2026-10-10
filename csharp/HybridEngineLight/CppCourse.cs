// HybridEngineLight.CppCourse —— C++ 课程的定义与运行。
//
// ══ 为什么课程是 .cs 而作业是 .cpp ══
//
//   课程文件（.cs）  装的是**检查逻辑** —— "怎么判断学生写对了"。
//                    检查要编译、要运行、要报错翻译，这些是程序。
//   学生作业（.cpp） 装的是**学生写的 C++ 代码** —— 就一个函数或一小段程序。
//
// 这个分工是有意的：学生只面对一个 .cpp 文件，不用管检查怎么实现的。
//
// ══ 课程作者要写的三样东西 ══
//
//   1. static CourseDef 课程 => new(...)      课程的标题、要求、答题模板
//   2. public static CheckResult 检查_xxx(string 学生代码)   若干条检查
//   3. （可选）static string 参考答案 => "..."   参考答案
//
// 运行器会自动收集所有「检查_」开头的方法，按定义顺序执行。

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HybridEngine-light;

/// <summary>一个 C++ 课程的定义（标题、要求、答题模板）。</summary>
public sealed class CourseDef
{
    public string 编号;
    public string 标题;
    /// <summary>「要做」段落 —— 会写进答题模板供学生照着写。</summary>
    public string 要做;
    /// <summary>答题模板（.cpp 的初始内容）。留空则自动生成一个带说明的空模板。</summary>
    public string 模板 = "";
    /// <summary>参考答案（学生卡住时看）。</summary>
    public string 参考答案 = "";

    public CourseDef(string 编号, string 标题, string 要做)
    {
        this.编号 = 编号;
        this.标题 = 标题;
        this.要做 = 要做;
    }
}

public static class CppCourse
{
    /// <summary>所有课程（由 Runner 从 cpp-lessons 程序集里自动发现）。</summary>
    public static readonly List<(CourseDef Def, Type 类型)> 全部 = new();

    public static void 注册(CourseDef def, Type 类型) => 全部.Add((def, 类型));

    public static (CourseDef, Type)? 找(string 编号)
    {
        foreach (var c in 全部)
            if (c.Def.编号 == 编号 || c.Def.编号.EndsWith(编号, StringComparison.Ordinal))
                return c;
        return null;
    }

    // ══════════════════ 检查 ══════════════════

    /// <summary>对一段学生代码跑完某课的所有检查。</summary>
    public static List<CppCheckItem> 检查列表(Type 类型) => CppLesson.收集(类型);

    public static (int 通过, int 总数, List<(string 名字, CheckResult 结果)> 明细)
        运行(Type 类型, string 学生代码)
    {
        var 明细 = new List<(string, CheckResult)>();
        int 通过 = 0;
        foreach (var c in 检查列表(类型))
        {
            CheckResult r;
            try { r = c.检查(学生代码); }
            catch (Exception e)
            {
                r = CheckResult.不过($"检查无法进行：{e.GetType().Name}: {e.Message}");
            }
            if (r.Ok) 通过++;
            明细.Add((c.名字, r));
        }
        return (通过, 明细.Count, 明细);
    }

    // ══════════════════ 报告 ══════════════════

    public static string 报告(CourseDef def, int 通过, int 总数,
                              List<(string 名字, CheckResult 结果)> 明细)
    {
        var sb = new StringBuilder();
        sb.Append('\n').Append('─', 56).Append('\n');
        sb.Append($"  检查结果：{def.标题}\n");
        sb.Append('─', 56).Append('\n');
        foreach (var (名字, 结果) in 明细)
        {
            sb.Append($"  {(结果.Ok ? "✓" : "✗")} {名字}\n");
            if (结果.Detail.Length > 0)
                foreach (var l in 结果.Detail.Split('\n'))
                    sb.Append("      ").Append(l).Append('\n');
        }
        sb.Append('\n');
        if (总数 > 0 && 通过 == 总数)
        {
            sb.Append($"  全部通过（{通过}/{总数}）—— 这一课过了！\n");
            sb.Append("  继续下一课吧。\n");
        }
        else
        {
            sb.Append($"  通过 {通过}/{总数}。\n");
            sb.Append("  上面 ✗ 的部分就是还要改的地方 —— 按提示改完再跑一次。\n\n");
            sb.Append("  卡住了？把 ✗ 那几行连同你的代码发出去问，比空想快得多。\n");
        }
        sb.Append('\n');
        return sb.ToString();
    }

    // ══════════════════ 答题文件的读写 ══════════════════

    public static string 答案路径(string 根目录, CourseDef def) =>
        Path.Combine(根目录, "cpp-answers", $"{def.编号}_{清理(定义名(def.标题))}.cpp");

    private static string 定义名(string 标题)
    {
        // 标题形如「第 01 课：输出 —— 让程序开口说话」，取冒号前那一段做文件名更清爽
        int i = 标题.IndexOf('：');
        string s = i > 0 ? 标题.Substring(i + 1) : 标题;
        int j = s.IndexOf('—');
        if (j > 0) s = s.Substring(0, j);
        return 清理(s.Trim());
    }

    private static string 清理(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s)
            if (char.IsLetterOrDigit(c)) sb.Append(c);
        return sb.Length > 0 ? sb.ToString() : "作业";
    }

    /// <summary>第一次检查时自动生成答题文件 —— 学生得有个地方下手。</summary>
    public static string 生成模板(CourseDef def)
    {
        if (def.模板.Length > 0) return def.模板;
        var sb = new StringBuilder();
        sb.Append("// ").Append(def.标题).Append('\n');
        sb.Append("//\n");
        sb.Append("// 要做：\n");
        foreach (var l in def.要做.Split('\n'))
            sb.Append("//   ").Append(l.TrimEnd()).Append('\n');
        sb.Append("//\n");
        sb.Append("// 写完之后运行：\n");
        sb.Append("//   dotnet run --project csharp/cpp-lessons 检查 ").Append(def.编号).Append('\n');
        sb.Append("//\n");
        sb.Append("// ── 在下面写你的代码 ──\n\n");
        return sb.ToString();
    }
}
