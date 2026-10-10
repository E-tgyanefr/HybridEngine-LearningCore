// 题库编译验证 —— 每个题库文件都必须能被独立编译、并 dump 出合法协议。
//
// ══ 为什么单独做这一组测试 ════
//
// 题库是**数据文件**，写错了不会立刻暴露：
//   · C# 语法错（例如字符串里内嵌了没转义的引号）→ 整个题库加载失败，题数变少
//   · 协议字段缺失（例如 code 题忘了 W 行骨架）→ 那道题永远判不了分
//
// 而"题数变少"这种故障**不会报错**，只是悄悄少了 —— 最难发现的一类问题。
//
// 我在这一个坑里摔了三次（Python 题库、C# 题库、Java 题库都是内嵌引号没转义）。
// 三次都是靠人手工撞出来的。第四次不该还这样。
//
// 所以：**每个题库文件都要在这里过一遍编译 + 协议完整性检查。**

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LearnCode;

internal static class BankTests
{
    public static void Run(Action<bool, string> check, string examsDir)
    {
        Console.WriteLine("\n=== 题库编译与协议 ===");

        if (!Directory.Exists(examsDir))
        {
            check(false, $"题库目录不存在：{examsDir}");
            return;
        }

        var files = Directory.GetFiles(examsDir, "*.cs")
                             .Where(f => !Path.GetFileName(f).StartsWith("_", StringComparison.Ordinal))
                             .OrderBy(f => f, StringComparer.Ordinal)
                             .ToList();
        check(files.Count >= 3, $"题库文件数量（实际 {files.Count} 个：{string.Join(", ", files.Select(Path.GetFileName))}）");

        foreach (var f in files)
        {
            string name = Path.GetFileName(f);
            Console.WriteLine($"  ── {name}");

            // ① 认出入口类名（不能硬编码 —— 每个题库的类名不同）
            string body = File.ReadAllText(f);
            var m = Regex.Match(body, @"public\s+static\s+class\s+(\w+)");
            if (!m.Success)
            {
                check(false, $"{name}: 找不到 public static class，无法确定 Dump 在哪");
                continue;
            }

            // ② 编译并 dump —— 语法错会在这里暴露
            var r = CSharpRunner.DumpOutput(body, m.Groups[1].Value + ".Dump");
            if (r.Error != null)
            {
                check(false, $"{name}: 题库加载失败 —— {r.Error}");
                continue;
            }

            // ③ 协议完整性
            int q = 0, k = 0, w = 0, t = 0, e = 0;
            var 代码题入口 = new HashSet<string>();
            var 有骨架的 = new HashSet<string>();
            var 有测试的 = new Dictionary<string, int>();
            var 问题 = new List<string>();

            foreach (var line in r.RawLines)
            {
                var f2 = line.Split('\t');
                if (f2.Length == 0) continue;
                switch (f2[0])
                {
                    case "E":
                        e++;
                        if (f2.Length < 6) 问题.Add("E 行字段不足（应为 6 个）");
                        break;
                    case "Q":
                        q++;
                        if (f2.Length < 8) 问题.Add($"Q 行字段不足：{f2[1]}");
                        break;
                    case "K":
                        k++;
                        if (f2.Length < 4) 问题.Add($"K 行字段不足（应为 4 个）");
                        else 代码题入口.Add(f2[1]);
                        break;
                    case "W":
                        w++;
                        if (f2.Length < 3) 问题.Add("W 行字段不足");
                        else 有骨架的.Add(f2[1]);
                        break;
                    case "T":
                        t++;
                        if (f2.Length < 5) 问题.Add("T 行字段不足（应为 5 个）");
                        else
                        {
                            有测试的.TryGetValue(f2[1], out int c);
                            有测试的[f2[1]] = c + 1;
                        }
                        break;
                }
            }

            // 每个代码题都必须有骨架和测试 —— 少一个，那道题就判不了分
            foreach (var entry in 代码题入口)
            {
                if (!有骨架的.Contains(entry))
                    问题.Add($"代码题「{entry}」缺 W 行（程序骨架）—— 它永远判不了分");
                if (!有测试的.ContainsKey(entry) || 有测试的[entry] == 0)
                    问题.Add($"代码题「{entry}」缺 T 行（测试用例）—— 它永远判不了分");
            }

            check(e > 0, $"{name}: 有考试元数据（E 行）");
            check(q + k > 0, $"{name}: 有题目（选择/填空 {q}，代码 {k}）");
            check(问题.Count == 0, $"{name}: 协议完整{(问题.Count > 0 ? " —— " + string.Join("；", 问题) : "")}");
        }
    }
}
