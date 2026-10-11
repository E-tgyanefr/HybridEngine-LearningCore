// HybridEngineLight.CSharpRunner —— 真的编译并运行学生写的 C# 代码，逐条跑测试用例。
//
// ══ 为什么是"传测试用例进去"而不是"反射调用" ══
//
// 踩过两个版本才定下来：
//
// ① 第一版：父进程用反射拿学生的对象，自己断言。
//    行不通 —— 学生代码在**另一个进程**里编译运行，跨进程只能传字符串，
//    拿不到真正的对象。（而且把学生代码加载进本进程 = 更危险。）
//
// ② 第二版：父进程让子进程调用入口方法、打印返回值。
//    也错了 —— 学生的 `是偶数(int n)` **需要参数**，而骨架里写的是
//    `Console.WriteLine(学生代码.是偶数())` → 编译报 "no argument given"。
//    这个错误只在真跑一次时才会暴露，光看代码看不出来。
//
// 现在：**把测试用例（调用表达式 + 期望输出）直接传给子进程**，
// 由子进程生成 `Console.WriteLine(学生代码.是偶数(4));` 这样的调用，
// 每条结果打一行，父进程逐行比对。
//
// ══ 零依赖怎么做到的 ══
// C# 是编译语言，没有 exec。跑学生现写的代码只有两条路：
//   ① 引入 Roslyn（Microsoft.CodeAnalysis.CSharp）—— 第三方包，破"零依赖"
//   ② 生成临时工程 + 用系统自带的 dotnet 编译运行 —— 不引入依赖，慢 1~2 秒
// 选 ②。这是**考试**不是交互练习，一道题慢两秒完全可以接受；
// 而"零依赖"是轻量版的核心承诺，不该为了快两秒破掉。
//
// ══ 安全说明（写在明处比假装安全有用）══
// 学生代码以**当前用户权限**在本机运行 —— 与他自己在终端 dotnet run 等价。
// 所以：**不要拿这个系统跑来路不明的代码。**

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace HybridEngineLight;

public sealed class CSharpResult
{
    /// <summary>每条测试的结果：说明 → 实际输出（null = 该条调用出错）。</summary>
    public Dictionary<string, string?> Results { get; } = new();
    /// <summary>编译/运行失败（null = 成功）。</summary>
    public string? Error { get; set; }
    /// <summary>编译器或运行时的原始输出 —— 给 Explain 翻译用。</summary>
    public string Raw { get; set; } = "";
    /// <summary>子进程打印的每一行（DumpOutput 用；Run 用 Results）。</summary>
    public List<string> RawLines { get; } = new();
}

public static class CSharpRunner
{
    /// <summary>一条测试：说明、调用表达式、期望输出。</summary>
    public readonly record struct Case(string Desc, string Call, string Expect);

    private static string? _dir;
    private static readonly object Gate = new();

    private static string EnsureProject()
    {
        lock (Gate)
        {
            if (_dir != null) return _dir;
            string dir = Path.Combine(Path.GetTempPath(), "hybridengine_light_cs");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "run.csproj"), """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <AssemblyName>hybridengine_light_cs</AssemblyName>
    <InvariantGlobalization>true</InvariantGlobalization>
    <!-- 初学者常见的"定义了没用"不该变成错误刷屏 -->
    <NoWarn>CS0168;CS0219;CS8321;CS0162;CS0164</NoWarn>
  </PropertyGroup>
</Project>
""", Encoding.UTF8);
            _dir = dir;
            return dir;
        }
    }

    /// <summary>
    /// 编译学生写的 <paramref name="body"/>（方法定义等），
    /// 然后逐条执行 <paramref name="cases"/> 里的调用并收集输出。
    /// </summary>
    public static CSharpResult Run(string body, IReadOnlyList<Case> cases)
    {
        var r = new CSharpResult();
        string dir;
        try { dir = EnsureProject(); }
        catch (Exception e) { r.Error = "无法创建运行目录：" + e.Message; return r; }

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Text;");
        // ⚠⚠ 这一行是第 13 课（using / IDisposable）要的：`StringWriter` 在 System.IO 里。
        //   我原来只给 `CppLesson.默认骨架CSharp` 加了 `using System.IO;` ——
        //   **但那个骨架根本没被用上**：真正拼 using 前导块的是**这里**。
        //   ⇒ 症状是 `CS0246: 未能找到类型或命名空间名"StringWriter"`，
        //     而报错位置指向**学员的代码**（学员会以为是自己写错了）。
        //   ⚠ 教训：**同一个"前导块"有多处实现**（这个文件里就有两处），
        //     改一处永远不够 —— 加 using 时要 grep 一遍。
        sb.AppendLine("using System.IO;");
        sb.AppendLine();
        sb.AppendLine("public static class 学生代码");
        sb.AppendLine("{");
        sb.AppendLine(body);
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("public static class 入口");
        sb.AppendLine("{");
        sb.AppendLine("    public static void Main()");
        sb.AppendLine("    {");
        for (int i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            // 每条测试独立 try —— 一条崩了不该让后面的看不到结果
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            sb.AppendLine($"            var __v{i} = 学生代码.{c.Call};");
            sb.AppendLine($"            Console.WriteLine(\"@@{i}@@\" + (__v{i} == null ? \"<null>\" : __v{i}.ToString()));");
            sb.AppendLine("        }");
            sb.AppendLine("        catch (Exception __e)");
            sb.AppendLine("        {");
            sb.AppendLine($"            Console.WriteLine(\"@@{i}@@\" + \"@@EX@@\" + __e.GetType().Name);");
            sb.AppendLine("        }");
        }
        sb.AppendLine("        Console.WriteLine(\"@@DONE@@\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        try { File.WriteAllText(Path.Combine(dir, "Program.cs"), sb.ToString(), new UTF8Encoding(false)); }
        catch (Exception e) { r.Error = "无法写入源代码：" + e.Message; return r; }

        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = dir,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in new[] { "run", "-v", "q", "--project", dir })
            psi.ArgumentList.Add(a);

        var outSb = new StringBuilder();
        var errSb = new StringBuilder();
        try
        {
            using var p = Process.Start(psi);
            if (p == null) { r.Error = "无法启动 dotnet"; return r; }
            var t1 = new Thread(() => { string? l; while ((l = p.StandardOutput.ReadLine()) != null) outSb.AppendLine(l); });
            var t2 = new Thread(() => { string? l; while ((l = p.StandardError.ReadLine()) != null) errSb.AppendLine(l); });
            t1.IsBackground = t2.IsBackground = true;
            t1.Start(); t2.Start();
            if (!p.WaitForExit(180_000))
            {
                try { p.Kill(true); } catch { /* 已经退了 */ }
                r.Error = "运行超时（超过 3 分钟）—— 检查有没有死循环";
                return r;
            }
            t1.Join(3000); t2.Join(3000);
        }
        catch (Exception e)
        {
            r.Error = "启动 dotnet 失败：" + e.Message;
            return r;
        }

        string outText = outSb.ToString();
        string errText = errSb.ToString();
        r.Raw = (errText + "\n" + outText).Trim();

        if (!outText.Contains("@@DONE@@"))
        {
            // 没跑到结尾 —— 基本就是编译失败。原话留着给 Explain 翻译。
            r.Error = ShortError(r.Raw);
            return r;
        }

        // 解析每条测试的结果
        for (int i = 0; i < cases.Count; i++)
        {
            var m = Regex.Match(outText, $@"^@@{i}@@(.*)$", RegexOptions.Multiline);
            if (!m.Success) { r.Results[cases[i].Desc] = null; continue; }
            string val = m.Groups[1].Value.Trim();
            r.Results[cases[i].Desc] = val.StartsWith("@@EX@@", StringComparison.Ordinal)
                ? null                                   // 调用抛异常
                : val;
        }
        return r;
    }

    /// <summary>
    /// 编译 <paramref name="body"/> 并调用它的 <paramref name="entry"/> 方法，
    /// **把方法打印的内容原样收回来**（不比对、不判分）。
    ///
    /// 用途：加载题库。题库的 Dump() 就是把题目打成行协议 —— 那是纯输出，
    /// 没有"测试用例"可言，所以走这条更直接的路径。
    /// </summary>
    public static CSharpResult DumpOutput(string body, string entry)
    {
        var r = new CSharpResult();
        string dir;
        try { dir = EnsureProject(); }
        catch (Exception e) { r.Error = "无法创建运行目录：" + e.Message; return r; }

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Text;");
        // ⚠⚠ 这一行是第 13 课（using / IDisposable）要的：`StringWriter` 在 System.IO 里。
        //   我原来只给 `CppLesson.默认骨架CSharp` 加了 `using System.IO;` ——
        //   **但那个骨架根本没被用上**：真正拼 using 前导块的是**这里**。
        //   ⇒ 症状是 `CS0246: 未能找到类型或命名空间名"StringWriter"`，
        //     而报错位置指向**学员的代码**（学员会以为是自己写错了）。
        //   ⚠ 教训：**同一个"前导块"有多处实现**（这个文件里就有两处），
        //     改一处永远不够 —— 加 using 时要 grep 一遍。
        sb.AppendLine("using System.IO;");
        sb.AppendLine();
        sb.AppendLine(body);
        sb.AppendLine();
        sb.AppendLine("public static class 入口");
        sb.AppendLine("{");
        sb.AppendLine("    public static void Main()");
        sb.AppendLine("    {");
        sb.AppendLine("        try { " + entry + "(); }");
        sb.AppendLine("        catch (Exception __e) { Console.WriteLine(\"@@EX@@\" + __e.GetType().Name + \": \" + __e.Message); }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        try { File.WriteAllText(Path.Combine(dir, "Program.cs"), sb.ToString(), new UTF8Encoding(false)); }
        catch (Exception e) { r.Error = "无法写入源代码：" + e.Message; return r; }

        var (outText, errText, timedOut) = Compile(dir);
        if (timedOut) { r.Error = "运行超时"; return r; }
        r.Raw = (errText + "\n" + outText).Trim();

        if (outText.Contains("@@EX@@"))
        {
            var m = Regex.Match(outText, @"@@EX@@(.*)");
            r.Error = "题库自己出错了：" + m.Groups[1].Value.Trim();
            return r;
        }
        foreach (var line in outText.Split('\n'))
        {
            string s = line.TrimEnd('\r');
            if (s.Length > 0) r.RawLines.Add(s);
        }
        return r;
    }

    /// <summary>编译并运行临时工程，返回 (stdout, stderr, 是否超时)。</summary>
    private static (string Out, string Err, bool TimedOut) Compile(string dir)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = dir,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in new[] { "run", "-v", "q", "--project", dir })
            psi.ArgumentList.Add(a);

        var outSb = new StringBuilder();
        var errSb = new StringBuilder();
        try
        {
            using var p = Process.Start(psi);
            if (p == null) return ("", "无法启动 dotnet", false);
            var t1 = new Thread(() => { string? l; while ((l = p.StandardOutput.ReadLine()) != null) outSb.AppendLine(l); });
            var t2 = new Thread(() => { string? l; while ((l = p.StandardError.ReadLine()) != null) errSb.AppendLine(l); });
            t1.IsBackground = t2.IsBackground = true;
            t1.Start(); t2.Start();
            if (!p.WaitForExit(180_000))
            {
                try { p.Kill(true); } catch { /* 已经退了 */ }
                return ("", "", true);
            }
            t1.Join(3000); t2.Join(3000);
        }
        catch (Exception e)
        {
            return ("", "启动 dotnet 失败：" + e.Message, false);
        }
        return (outSb.ToString(), errSb.ToString(), false);
    }

    /// <summary>
    /// 从编译器输出里挑出最关键的那一行，并去掉噪音。
    ///
    /// 为什么要处理：编译器的原话长这样 ——
    ///   C:\Users\x\AppData\Local\Temp\hybridengine_light_cs\Program.cs(10,22): error CS1002: 应输入 ;
    /// 那个临时路径对学生毫无意义，还容易让人以为"我的代码跑到了奇怪的地方"。
    /// 这里只留「CS编号 + 说明」。（原始输出仍保留在 Raw 里，Explain 需要时能拿到。）
    /// </summary>
    private static string ShortError(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            string s = line.Trim();
            int i = s.IndexOf("error CS", StringComparison.Ordinal);
            if (i < 0) continue;
            string core = s.Substring(i);
            int br = core.IndexOf(" [", StringComparison.Ordinal);
            if (br > 0) core = core.Substring(0, br);
            return core.Replace("error ", "");
        }
        var ls = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return ls.Length > 0 ? ls[0].Trim() : "未知错误";
    }
}
