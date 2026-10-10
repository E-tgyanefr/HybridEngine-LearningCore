// LearnCode.BankLoader —— 从磁盘加载题库文件。
//
// ══ 为什么题库在文件里而不是编译进程序集 ══
//   · 题库是**内容**，会频繁增删改；编译进去的话改一道题就要重新发布
//   · 学生和老师可以自己加题（写个 .cs 丢进 exams/ 就行）
//   · 但 C# 编译语言 —— 所以题库要**在运行时编译**
//
// ══ 怎么做到"运行时加载 C# 题库" ══
// 与跑学生代码同一招：生成临时工程 + dotnet run。
// 题库文件里的 Dump() 负责把题目打印成**行协议**，父进程解析。
// 于是题库可以是正常 C# 代码（有注释、能换行、读得懂），而解析只面对一个简单协议。
//
// ══ 行协议 ══
//   E <名称> <简称> <级别> <依据> <时长分钟>
//   Q <id> <类型> <主题> <难度> <题干> <解释> <答案>
//   K <入口方法名> <题干> <解释>                ← 写代码题
//   T <入口方法名> <说明> <调用表达式> <期望输出>   ← 写代码题的测试用例
//
// 字段里换行→\n、制表符→\t（题库侧 Esc 负责），免得破坏按行解析。
//
// ⚠ 为什么写代码题要单独的 K/T 两行，而不是塞进 Q 里：
//   我第一版想用"按位置对应的数组"表达测试用例 —— 那种写法**插一道题就错位**，
//   而且错了不报错、只是判分变乱，极难发现。显式发一行，读和改都不会错。

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LearnCode;

public static class BankLoader
{
    /// <summary>加载 exams/ 目录下所有 .cs 题库。</summary>
    public static (ExamMeta? Meta, List<Question> Questions) LoadAll(string examsDir)
    {
        ExamMeta? meta = null;
        var list = new List<Question>();
        if (!Directory.Exists(examsDir)) return (null, list);

        foreach (var f in Directory.GetFiles(examsDir, "*.cs"))
        {
            if (Path.GetFileName(f).StartsWith("_", StringComparison.Ordinal)) continue;
            var (m, qs) = LoadOne(f);          // 坏了就让异常冒出来（见下）
            if (m != null) meta = m;
            list.AddRange(qs);
        }
        return (meta, list);
    }

    /// <summary>
    /// 加载单个题库文件。
    /// ⚠ 这里**不吞异常** —— 题库语法错必须立刻炸出来。
    ///   我踩过：题库里一个引号没转义，直到学生运行考试才暴露，
    ///   而他看到的是编译器的一串英文报错，根本不知道那是题库的问题。
    ///   宁可让发布前就失败。
    /// </summary>
    private static (ExamMeta?, List<Question>) LoadOne(string path)
    {
        string body = File.ReadAllText(path, Encoding.UTF8);

        // ⚠ 入口类名必须从文件里认出来，不能硬编码。
        //   我一开始写的是 "CSharpBank.Dump" —— 加 C++ 题库时它静默失效：
        //   不报错，只是**题目数量悄悄变少**。这类"少了东西但不报错"的故障最难发现。
        var m = Regex.Match(body, @"public\s+static\s+class\s+(\w+)");
        if (!m.Success)
            throw new InvalidOperationException(
                $"题库 {Path.GetFileName(path)} 里找不到 public static class —— 无法确定 Dump 在哪。");
        string entry = m.Groups[1].Value + ".Dump";

        var r = CSharpRunner.DumpOutput(body, entry);
        if (r.Error != null)
            throw new InvalidOperationException(
                $"题库 {Path.GetFileName(path)} 加载失败：{r.Error}\n{r.Raw}");

        ExamMeta? meta = null;
        var qs = new List<Question>();
        var codeByEntry = new Dictionary<string, Question>();

        foreach (var raw in r.RawLines)
        {
            var f = raw.Split('\t');
            switch (f[0])
            {
                case "E" when f.Length >= 6:
                    meta = new ExamMeta
                    {
                        Name = Unesc(f[1]), Short = Unesc(f[2]), Level = Unesc(f[3]),
                        Source = Unesc(f[4]),
                        DurationMin = int.TryParse(f[5], out var d) ? d : 0,
                    };
                    break;

                case "Q" when f.Length >= 8:
                    qs.Add(new Question
                    {
                        Id = Unesc(f[1]), Type = Unesc(f[2]), Topic = Unesc(f[3]),
                        Level = int.TryParse(f[4], out var lv) ? lv : 1,
                        Q = Unesc(f[5]), Explain = Unesc(f[6]),
                        // choice 的答案是选项序号（1 起），这里转成 0 起的下标
                        Answer = Unesc(f[2]) == "choice" && int.TryParse(Unesc(f[7]), out var a) ? a - 1 : -1,
                        Expect = Unesc(f[2]) != "choice" ? Unesc(f[7]) : "",
                        Exam = meta?.Short ?? "",
                        Points = Unesc(f[2]) == "choice" ? 1 : 2,
                    });
                    break;

                case "K" when f.Length >= 4:
                {
                    var q = new Question
                    {
                        Id = "C#-code-" + Unesc(f[1]),
                        Type = "code",
                        Topic = "写代码",
                        Level = 2,
                        Q = Unesc(f[2]),
                        Explain = Unesc(f[3]),
                        Entry = Unesc(f[1]),
                        Exam = meta?.Short ?? "",
                        Points = 10,
                    };
                    qs.Add(q);
                    codeByEntry[q.Entry] = q;
                    break;
                }

                case "W" when f.Length >= 3:
                {
                    // W <入口名> <程序骨架的 Base64>
                    // 为什么是 Base64：见 30_Cpp.cs 里 W 行旁边的说明 ——
                    // 骨架里的 C++ 换行符和协议层的转义会互相干扰，Base64 能彻底避开。
                    if (codeByEntry.TryGetValue(Unesc(f[1]), out var q))
                    {
                        try
                        {
                            q.Wrapper = Encoding.UTF8.GetString(Convert.FromBase64String(f[2]));
                        }
                        catch (FormatException e)
                        {
                            throw new InvalidOperationException(
                                $"题库里的 W 行（{Unesc(f[1])} 的程序骨架）不是合法的 Base64：{e.Message}");
                        }
                    }
                    break;
                }

                case "T" when f.Length >= 5:
                {
                    // T <入口> <说明> <调用表达式> <期望输出>
                    if (codeByEntry.TryGetValue(Unesc(f[1]), out var q))
                        q.Tests.Add((Unesc(f[2]), Unesc(f[3]), Unesc(f[4])));
                    break;
                }
            }
        }
        return (meta, qs);
    }

    private static string Unesc(string s) =>
        s.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\\", "\\");
}
