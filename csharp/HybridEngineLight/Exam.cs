// HybridEngineLight.Exam —— 考试系统（C# 版）
//
// 与 Python 侧（hybridengine_light/exam.py + examrun.py）共用同一套设计：
//   题库是数据 → 抽题 → 作答 → 判分 → 按主题看弱项 → 记录历史
//
// ══ 题型 ══
//   choice  选择题
//   output  预测输出（比较归一化后的文字）
//   code    写代码 —— **真的执行**学生的函数并跑测试用例
//
// code 题的判定方式是这套系统唯一的"防糊弄"手段：
// 选择题可以猜，写代码不能。所以判分逻辑里它最重。
//
// ══ 为什么要考试 ══
// 自学最大的问题不是没材料，是**不知道自己到底会不会**。
// "看懂了"和"能写出来"之间隔得很远，只有后者算数。
// 所以出分之后按主题给正确率 —— 哪一块弱一眼看得见，而不是只给一个总分。

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HybridEngineLight;

/// <summary>一道题。题库是纯数据（见 exams/ 下的 .cs 文件）。</summary>
public sealed class Question
{
    public string Id = "";
    public string Type = "choice";       // choice / output / code
    public string Topic = "";
    public int Level = 1;
    public string Q = "";
    public string Explain = "";
    public List<string> Options = new();
    public int Answer = -1;
    public string Expect = "";
    public string Starter = "";
    /// <summary>
    /// code 题：每项是 (说明, 期望输出)。学生代码跑起来后打印的内容要**按行完全一致**。
    /// ⚠ 为什么不是"检查函数"：学生代码在**另一个进程**里编译运行，跨进程只能传字符串，
    ///   拿不到真正的对象去做断言。约定"打印出什么"是唯一可跨进程验证的方式，
    ///   而且对入门教学完全够用 —— 初学者本来就在跟输出打交道。
    /// </summary>
    /// <summary>code 题：每条测试 = (说明, 调用表达式, 期望输出)。</summary>
    public List<(string Desc, string Call, string ExpectLine)> Tests = new();
    public string Exam = "";
    public double Points = 1.0;
    /// <summary>code 题的入口方法名（无参）。</summary>
    public string Entry = "";
    /// <summary>
    /// C++ 代码题的程序骨架：里面有一个 /*学生函数*/ 占位符，
    /// 判分时把学生写的函数替换进去，再整体编译。
    /// 为什么需要：学生写的是函数，而 C++ 必须要有 main() 才能编译。
    /// </summary>
    public string Wrapper = "";
    /// <summary>true = 不打印返回值，只看学生自己 Console.WriteLine 的内容。</summary>
    public bool Silent;
}

/// <summary>一门考试的元数据。</summary>
public sealed class ExamMeta
{
    public string Name = "";
    public string Short = "";
    public string Level = "";
    public string Source = "";
    public int DurationMin;
}

/// <summary>一次作答的明细 —— 出报告要逐题订正，只给总分对学生没价值。</summary>
public sealed class Answered
{
    public Question Q = null!;
    public string Given = "";
    public bool Ok;
    public string Note = "";
}

/// <summary>一次考试的成绩。</summary>
public sealed class Attempt
{
    public string Mode = "practice";
    public int Total;
    public int Correct;
    public double Seconds;
    public Dictionary<string, int[]> ByTopic = new();     // 主题 → [对, 总]
    public List<string> WrongIds = new();
}

public static class Exam
{
    // ══════════════════ 题库 ══════════════════
    public static readonly List<Question> Bank = new();
    public static ExamMeta? Meta;

    /// <summary>从给定目录加载题库（只加载一次）。</summary>
    public static void LoadFrom(string examsDir)
    {
        if (Bank.Count > 0) return;
        var (m, qs) = BankLoader.LoadAll(examsDir);
        Meta = m;
        Bank.AddRange(qs);
    }

    // ══════════════════ 归一化 ══════════════════
    /// <summary>
    /// 答案比较前的归一化。学生的空白、引号样式不该扣分 ——
    /// 那考的是标点洁癖，不是编程。但大小写与内容必须一致。
    /// </summary>
    public static string Normalize(string s)
    {
        if (s == null) return "";
        s = s.Trim()
             .Replace('\u201c', '"').Replace('\u201d', '"')    // 中文引号
             .Replace('\u2018', '\'').Replace('\u2019', '\'');
        s = Regex.Replace(s, @"\s+", " ");
        s = Regex.Replace(s, @"\s*([(),:=+\-*/\[\]{}])\s*", "$1");
        return s.Trim();
    }

    // ══════════════════ 判分 ══════════════════
    /// <summary>
    /// 判 code 题：**真的编译并运行**学生写的代码，逐条跑测试用例。
    /// 这是整套系统唯一的「防糊弄」手段 —— 选择题能猜，写代码不能。
    /// </summary>
    public static (bool Ok, string Note) GradeCode(Question q, string src)
    {
        if (string.IsNullOrWhiteSpace(src)) return (false, "没有写任何代码");
        if (q.Tests.Count == 0) return (false, "这道题没有测试用例（题库的问题，不是你的）");

        // ── 按语言走不同的运行路径 ──
        //
        // C# 和 C++ 都是编译语言，但用的编译器和骨架完全不同：
        //   C#  → CSharpRunner（dotnet run 一个临时工程）
        //   C++ → CppRunner（g++ 编译一个临时 main.cpp）
        // 所以 code 题的判分必须分派，不能只有一条路。
        if (q.Exam == "C++")
            return GradeCpp(q, src);
        if (q.Exam == "Java")
            return GradeJava(q, src);

        var cases = q.Tests
            .Select(t => new CSharpRunner.Case(t.Desc, t.Call, t.ExpectLine))
            .ToList();

        var r = CSharpRunner.Run(src, cases);
        if (r.Error != null) return (false, r.Error);

        int passed = 0;
        var notes = new List<string>();
        foreach (var c in cases)
        {
            if (!r.Results.TryGetValue(c.Desc, out var got))
            {
                notes.Add($"{c.Desc} → 没跑到");
                continue;
            }
            if (got == null) { notes.Add($"{c.Desc} → 调用时抛异常"); continue; }
            if (Normalize(got) == Normalize(c.Expect)) passed++;
            else notes.Add($"{c.Desc} → 得到 {Show(got)}，期望 {Show(c.Expect)}");
        }
        bool all = passed == cases.Count;
        return (all, all ? "全部通过" : $"通过 {passed}/{cases.Count}；" + string.Join("；", notes));
    }

    /// <summary>
    /// 判 C++ 代码题：把学生的函数拼进一个完整的 C++ 程序，编译运行，比对输出。
    ///
    /// 为什么要拼：学生写的是**一个函数**，不是完整程序。
    /// 而 C++ 必须有 main() 才能编译，所以骨架（#include、main、调用点）
    /// 由题库提供（协议里的 W 行），我们把学生的函数塞进去。
    ///
    /// ⚠ 编译失败时要走 ExplainCpp 翻译 —— C++ 的报错新手读不懂，
    ///   而这条路径正是学生最常见的卡点。
    /// </summary>
    private static (bool Ok, string Note) GradeCpp(Question q, string src)
    {
        string 骨架 = q.Wrapper;
        if (string.IsNullOrWhiteSpace(骨架))
            return (false, "这道 C++ 题没有提供程序骨架（题库的问题，不是你的）");

        // 学生可能只写了函数，也可能写了完整程序。只取函数部分拼进去。
        string 程序 = 骨架.Replace("/*学生函数*/", src);

        var r = CppRunner.BuildAndRun(程序);
        if (r.Error != null)
        {
            if (r.CompileFailed)
            {
                // 编译失败要给出**完整解释** —— C++ 的报错新手读不懂，
                // 而这条路径正是学生最常见的卡点。摘要没用，要给翻译。
                return (false, "编译没能通过 —— 下面是编译器给的解释：\n"
                               + ExplainCpp.For(r.CompileOutput, 程序, true));
            }
            return (false, r.Error);
        }

        string got = string.Join("\n", r.Lines).Trim();
        string want = string.Join("\n", q.Tests.Select(t => t.ExpectLine)).Trim();
        bool 输出对 = Normalize(got) == Normalize(want);

        // 输出对但编译器警告了未初始化变量 —— 这次碰巧对了，下次不会。
        // 判它通过等于奖励一个真实的 bug，所以拦下来并讲清楚。
        if (输出对 && r.HasCorrectnessWarning)
        {
            return (false, "输出虽然对了，但编译器发现了一个**真实的隐患**：\n"
                           + ExplainCpp.ExplainWarning(r.CompileOutput)
                           + "\n      （这次碰巧算对了，换台机器或换个编译器就可能不对。）");
        }

        if (输出对) return (true, "输出正确，测试全过");
        return (false, $"期望输出：{Show(want)}\n        实际输出：{Show(got)}");
    }

    /// <summary>
    /// 判 Java 代码题：把学生的函数拼进骨架（题库提供），编译运行，比对输出。
    ///
    /// 与 C++ 路径的关键差别：**通过之后还要检查 main 的写法**。
    /// 实测（JDK 25）：漏 static、漏 public、没参数的 main 都能编译能跑，
    /// 所以这类错误在这台机器上完全不暴露 —— 学生换台机器才撞墙。
    /// 只提醒，不判错（它确实能跑）。
    /// </summary>
    private static (bool Ok, string Note) GradeJava(Question q, string src)
    {
        if (string.IsNullOrWhiteSpace(q.Wrapper))
            return (false, "这道 Java 题没有提供程序骨架（题库的问题，不是你的）");

        // ⚠ Java 的公共类名必须和文件名一致，而 JavaRunner 固定用 Main.java。
        //   所以骨架里的类名必须是 Main —— 题库负责这一点，这里只做拼装。
        string 程序 = q.Wrapper.Replace("/*学生代码*/", src);

        var r = JavaRunner.BuildAndRun(程序);
        if (r.Error != null)
        {
            if (r.CompileFailed)
                return (false, "编译没能通过 —— 下面是编译器给的解释：\n"
                               + ExplainJava.For(r.CompileOutput, 程序, true));
            return (false, r.Error);
        }

        string got = string.Join("\n", r.Lines).Trim();
        string want = string.Join("\n", q.Tests.Select(t => t.ExpectLine)).Trim();
        if (Normalize(got) == Normalize(want))
        {
            // 输出对，但 main 的写法可能不标准（见方法头说明）。
            // 不判错，只提醒 —— 学生需要知道"这只在特定 JDK 上能跑"。
            string 提醒 = ExplainJava.MainSignatureNote(程序);
            if (提醒.Length > 0)
                return (true, "输出正确；但要提醒你一件事：\n" + 提醒);
            return (true, "输出正确，测试全过");
        }
        return (false, $"期望输出：{Show(want)}\n        实际输出：{Show(got)}");
    }

    private static string Show(string s) =>
        string.IsNullOrEmpty(s) ? "（空）" : s.Replace("\n", " ⏎ ");
    // ══════════════════ 考试流程 ══════════════════
    public static (Attempt, List<Answered>) Take(List<Question> questions, string mode = "practice",
                                                  double? limitSeconds = null, string title = "")
    {
        Line('═');
        Console.WriteLine($"  {(string.IsNullOrEmpty(title) ? (mode == "practice" ? "练习" : "考试") : title)}");
        Line('═');
        Console.Write($"  共 {questions.Count} 题");
        if (limitSeconds is double ls) Console.Write($"，限时 {ls / 60:F0} 分钟");
        Console.WriteLine();
        Console.WriteLine(mode == "practice"
            ? "  （练习模式：答完每题都会告诉你对不对）"
            : "  （考试模式：全部答完才出结果，中途不提示对错）");

        var answers = new List<Answered>();
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            Console.WriteLine();
            Line('─');
            Console.Write($"  第 {i + 1}/{questions.Count} 题   [{q.Topic}]");
            if (limitSeconds is double lim)
            {
                double left = lim - sw.Elapsed.TotalSeconds;
                if (left <= 0) { Console.WriteLine("   ⏰ 时间到"); break; }
                Console.Write($"   剩余 {left / 60:F0}:{left % 60:00}");
            }
            Console.WriteLine();

            var a = new Answered { Q = q };
            if (q.Type == "choice")
            {
                a.Given = AskChoice(q);
                a.Ok = a.Given == q.Answer.ToString(CultureInfo.InvariantCulture);
            }
            else if (q.Type == "code")
            {
                a.Given = AskCode(q);
                var (ok, note) = GradeCode(q, a.Given);
                a.Ok = ok; a.Note = note;
                if (mode == "practice") Feedback(ok, note);
                answers.Add(a);
                continue;
            }
            else
            {
                a.Given = AskText(q);
                a.Ok = Normalize(a.Given) == Normalize(q.Expect);
            }
            answers.Add(a);
            if (mode == "practice") Feedback(a.Ok, "");
        }
        sw.Stop();

        var byTopic = new Dictionary<string, int[]>();
        foreach (var a in answers)
        {
            if (!byTopic.TryGetValue(a.Q.Topic, out var v)) { v = new[] { 0, 0 }; byTopic[a.Q.Topic] = v; }
            v[1]++;
            if (a.Ok) v[0]++;
        }

        var att = new Attempt
        {
            Mode = mode,
            Total = answers.Count,
            Correct = answers.Count(x => x.Ok),
            Seconds = sw.Elapsed.TotalSeconds,
            ByTopic = byTopic,
            WrongIds = answers.Where(x => !x.Ok).Select(x => x.Q.Id).ToList(),
        };
        return (att, answers);
    }

    private static string AskChoice(Question q)
    {
        Console.WriteLine();
        Console.WriteLine("  " + q.Q);
        Console.WriteLine();
        for (int i = 0; i < q.Options.Count; i++) Console.WriteLine($"    {i + 1}. {q.Options[i]}");
        while (true)
        {
            Console.Write("\n  选一个（输入序号）：");
            string raw = (Console.ReadLine() ?? "").Trim();
            if (int.TryParse(raw, out int n) && n >= 1 && n <= q.Options.Count)
                return (n - 1).ToString(CultureInfo.InvariantCulture);
            Console.WriteLine("  请输入选项前面的序号。");
        }
    }

    private static string AskText(Question q)
    {
        Console.WriteLine();
        Console.WriteLine("  " + q.Q);
        Console.Write("\n  你的答案：");
        return (Console.ReadLine() ?? "").Trim();
    }

    private static string AskCode(Question q)
    {
        Console.WriteLine();
        Console.WriteLine("  " + q.Q);
        Console.WriteLine();
        if (!string.IsNullOrEmpty(q.Starter))
            foreach (var line in q.Starter.Split('\n')) Console.WriteLine("    " + line.TrimEnd());
        Console.WriteLine();
        Console.WriteLine("  每行按回车，写完输入一个单独的空行结束。");
        Console.WriteLine();
        var sb = new StringBuilder();
        while (true)
        {
            Console.Write("    ");
            string line = Console.ReadLine() ?? "";
            if (line.Length == 0 && sb.Length > 0) break;
            if (line.Length == 0 && sb.Length == 0) continue;
            sb.AppendLine(line);
        }
        return sb.ToString();
    }

    private static void Feedback(bool ok, string note)
    {
        Console.WriteLine(ok ? "\n  ✓ 对" : $"\n  ✗ 不对{(note.Length > 0 ? "：" + note : "")}");
    }

    // ══════════════════ 报告 ══════════════════
    public static string RenderReport(Attempt att, List<Answered> answers, string title = "")
    {
        var sb = new StringBuilder();
        sb.Append('\n'); sb.Append('═', 56).Append('\n');
        sb.Append($"  成绩：{(string.IsNullOrEmpty(title) ? "本次" : title)}\n");
        sb.Append('═', 56).Append('\n');
        double pct = att.Total > 0 ? att.Correct * 100.0 / att.Total : 0;
        sb.Append($"  得分    {att.Correct}/{att.Total}   （{pct:F0}%）\n");
        sb.Append($"  用时    {(int)(att.Seconds / 60)} 分 {(int)(att.Seconds % 60)} 秒\n\n");

        if (att.ByTopic.Count > 0)
        {
            sb.Append("  按知识主题：\n");
            foreach (var kv in att.ByTopic.OrderBy(k => k.Value[0] / (double)Math.Max(1, k.Value[1])))
            {
                double rate = kv.Value[0] / (double)Math.Max(1, kv.Value[1]);
                int full = (int)(rate * 10);
                string bar = new string('█', full) + new string('·', 10 - full);
                string mark = rate < 0.7 ? "  ← 这块要补" : "";
                sb.Append($"    {kv.Key,-12} {bar} {kv.Value[0]}/{kv.Value[1]}{mark}\n");
            }
            sb.Append('\n');
        }

        // 订正 —— 比分数重要
        var wrong = answers.Where(x => !x.Ok).ToList();
        if (wrong.Count > 0)
        {
            sb.Append('─', 56).Append('\n');
            sb.Append("  订正（这部分比分数重要）\n");
            sb.Append('─', 56).Append('\n');
            foreach (var a in wrong)
            {
                sb.Append($"\n  [{a.Q.Topic}] {a.Q.Q}\n");
                if (a.Q.Type == "choice")
                {
                    bool hasIdx = int.TryParse(a.Given, out int gi) && gi >= 0 && gi < a.Q.Options.Count;
                    sb.Append($"    你选了：{(hasIdx ? a.Q.Options[int.Parse(a.Given)] : a.Given)}\n");
                    if (a.Q.Answer >= 0 && a.Q.Answer < a.Q.Options.Count)
                        sb.Append($"    正确答案：{a.Q.Options[a.Q.Answer]}\n");
                }
                else
                {
                    sb.Append($"    你写的：{(a.Given.Length > 0 ? a.Given.Trim() : "（空）")}\n");
                    if (!string.IsNullOrEmpty(a.Q.Expect)) sb.Append($"    正确答案：{a.Q.Expect}\n");
                    if (!string.IsNullOrEmpty(a.Note)) sb.Append($"    测试结果：{a.Note}\n");
                }
                if (!string.IsNullOrEmpty(a.Q.Explain)) sb.Append($"    为什么：{a.Q.Explain}\n");
            }
        }
        else sb.Append("  全对 —— 没有要订正的。\n");
        sb.Append('\n');
        return sb.ToString();
    }

    // ══════════════════ 成绩历史 ══════════════════
    private static string ProgressPath => Path.Combine(AppContext.BaseDirectory, "HybridEngine-light-成绩记录.json");

    public static void SaveAttempt(Attempt a)
    {
        try
        {
            var sb = new StringBuilder();
            sb.Append("{\"attempts\":[");
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "{{\"ts\":{0:F0},\"mode\":\"{1}\",\"total\":{2},\"correct\":{3},\"seconds\":{4:F1}}}",
                DateTimeOffset.UtcNow.ToUnixTimeSeconds(), a.Mode, a.Total, a.Correct, a.Seconds));
            // 简单追加式：读旧内容、去掉结尾、拼新的。够用且不引入 JSON 依赖。
            if (File.Exists(ProgressPath))
            {
                string old = File.ReadAllText(ProgressPath, Encoding.UTF8).Trim();
                int end = old.LastIndexOf("]}", StringComparison.Ordinal);
                if (end > 0)
                {
                    string inner = old.Substring(old.IndexOf('[', StringComparison.Ordinal) + 1, end - old.IndexOf('[', StringComparison.Ordinal) - 1).Trim();
                    if (inner.Length > 0)
                    {
                        sb.Clear();
                        sb.Append("{\"attempts\":[").Append(inner).Append(',');
                        sb.Append(string.Format(CultureInfo.InvariantCulture,
                            "{{\"ts\":{0:F0},\"mode\":\"{1}\",\"total\":{2},\"correct\":{3},\"seconds\":{4:F1}}}",
                            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), a.Mode, a.Total, a.Correct, a.Seconds));
                    }
                }
            }
            sb.Append("]}");
            File.WriteAllText(ProgressPath, sb.ToString(), Encoding.UTF8);
        }
        catch (Exception)
        {
            // 历史记录写不进去不该影响考试本身 —— 静默失败
        }
    }

    private static void Line(char c) => Console.WriteLine(new string(c, 56));
}
