// HybridEngineLight.CppRunner —— 编译并运行学生写的 C++ 代码。
//
// ══ 与 C# 侧最大的不同：C++ 要外部编译器 ══
//
// Python 自带解释器，C# 有 dotnet —— 两者都不需要学生额外装东西。
// **C++ 不一样**：它必须有 g++ / clang++ 这样的编译器，而那是独立安装的。
//
// 所以这里做两件事：
//   ① 找不到编译器时给**明确的安装指引**，而不是抛一个"系统找不到文件"
//      —— 对刚学编程的人来说，"怎么装编译器"本身就是一道墙，
//         把他撞在墙上又不说怎么翻，是最坏的做法。
//   ② 编译错误**必须翻译**。C++ 的报错是出了名的难懂：
//         error: expected ',' or ';' before 'std'
//      真正的原因往往是**上一行漏了分号**。这个信息编译器不会说，
//      但新手不知道，就会盯着报错那一行改半天。
//
// ══ 安全说明 ══
// 学生代码会以**当前用户权限**在本机编译并运行 —— 与他自己在终端
// `g++ a.cpp && a.exe` 完全等价。所以：**不要拿它跑来路不明的代码。**

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace HybridEngineLight;

public sealed class CppResult
{
    /// <summary>程序打印的每一行。</summary>
    public System.Collections.Generic.List<string> Lines { get; } = new();
    /// <summary>失败信息（null = 成功）。</summary>
    public string? Error { get; set; }
    /// <summary>
    /// 编译器的原始输出（错误 + 警告）。
    /// ⚠ 和 RunOutput 分开存 —— 见下面的说明。
    /// </summary>
    public string CompileOutput { get; set; } = "";
    /// <summary>程序运行时的输出（stdout + stderr）。</summary>
    public string RunOutput { get; set; } = "";
    /// <summary>两者的合并 —— 给"我不知道该看哪个"的调用方用。</summary>
    public string Raw => (CompileOutput + "\n" + RunOutput).Trim();
    /// <summary>区分是"编译失败"还是"运行出错" —— 两者的解释方向完全不同。</summary>
    public bool CompileFailed { get; set; }
    /// <summary>
    /// 编译时出现了**影响正确性**的警告（未初始化变量等）。
    /// 为什么单独拎出来：这类代码在 C++ 里是未定义行为，可能碰巧跑对 ——
    /// 判它通过等于奖励了一个真实的 bug。
    /// </summary>
    public bool HasCorrectnessWarning { get; set; }
}

public static class CppRunner
{
    private static int _seq;
    private static string? _compiler;
    private static bool _searched;
    private static readonly object Gate = new();

    /// <summary>找到的 C++ 编译器路径。null = 没找到。</summary>
    public static string? Compiler
    {
        get { lock (Gate) { if (!_searched) { _searched = true; _compiler = Find(); } return _compiler; } }
    }

    /// <summary>没找到编译器时给学生看的说明。</summary>
    public static string InstallHint =>
        "没有找到 C++ 编译器。\n\n" +
        "C++ 和 Python、C# 不一样 —— 它需要一个**单独安装的编译器**：\n\n" +
        "  Windows  装 MinGW-w64（推荐用 winget）：\n" +
        "             winget install BrechtSanders.WinLibs.POSIX.UCRT\n" +
        "           或者装 Visual Studio 时勾选「使用 C++ 的桌面开发」\n\n" +
        "  macOS    在终端运行：  xcode-select --install\n\n" +
        "  Linux    用包管理器装 g++：\n" +
        "             Ubuntu/Debian:  sudo apt install g++\n" +
        "             Fedora:         sudo dnf install gcc-c++\n\n" +
        "装完**重开一个终端**（让 PATH 生效），再运行这条命令。";

    private static string? Find()
    {
        foreach (var name in new[] { "g++", "g++.exe", "clang++", "clang++.exe", "c++" })
        {
            try
            {
                var psi = new ProcessStartInfo(name, "--version")
                {
                    RedirectStandardOutput = true, RedirectStandardError = true,
                    UseShellExecute = false, CreateNoWindow = true,
                };
                using var p = Process.Start(psi);
                if (p == null) continue;
                p.StandardOutput.ReadToEnd();
                p.WaitForExit(5000);
                if (p.ExitCode == 0) return name;
            }
            catch { /* 这个没有，试下一个 */ }
        }
        return null;
    }

    /// <summary>
    /// 把完整的 C++ 源码写下来、编译、运行，收取输出。
    /// <paramref name="source"/> 必须是**完整程序**（含 #include 和 main）。
    /// </summary>
    public static CppResult BuildAndRun(string source, int timeoutMs = 60_000)
    {
        var r = new CppResult();
        string? cc = Compiler;
        if (cc == null) { r.Error = InstallHint; return r; }

        // ⚠ 每次一个**全新目录**，不复用上一次的。
        //
        // 我踩过：同一路径上连续编译两次，工具链复用了旧产物 ——
        // 于是「错误解法」跑出来的是「上一次正确解法」的结果。
        // 对教学工具来说这是致命的：反馈必须是关于**你刚写的这段代码**的。
        // 与其跟增量构建的规则较劲，不如从根上让复用无从发生。
        int seq = System.Threading.Interlocked.Increment(ref _seq);
        // ⚠ 目录名**必须带进程号**。
        //
        // 踩过的坑（同事 C 在验收时撞了 2 次）：`_seq` 是**进程内**静态字段，
        // 每次 `dotnet run` 都从 1 重新开始 —— 于是 run1/run2/… 被**跨进程复用**，
        // 上一个进程留下的 prog.exe 就在那儿。下面那句"保险"的删除于是真去删它，
        // Windows 偶尔还锁着（杀软/句柄延迟）就抛异常，**正确的代码被判成 ✗**。
        //
        // 带进程号之后，"新目录本来就是空的"这个前提重新成立。
        string dir = Path.Combine(Path.GetTempPath(), "hybridengine_light_cpp",
                                  $"run{Environment.ProcessId}_{seq}");
        Directory.CreateDirectory(dir);
        string src = Path.Combine(dir, "main.cpp");
        // Windows 上可执行文件带 .exe，别的平台不带 —— 用运行时判断而不是编译期
        string exe = Path.Combine(dir, OperatingSystem.IsWindows() ? "prog.exe" : "prog");

        try { File.WriteAllText(src, source, new UTF8Encoding(false)); }
        catch (Exception e) { r.Error = "无法写入源文件：" + e.Message; return r; }

        // ⚠ 必须先把上一轮的可执行文件删掉。
        //
        // 我踩过这个坑：判断"编译成功"用的是 File.Exists(exe)，但上一轮编译出的
        // prog.exe 还在那儿 —— 于是**编译失败被误判成成功，跑的是上一次的旧程序**。
        // 学生看到的是「我改的代码没生效」或者更糟「明明报错了却跑出了结果」。
        // 这类 bug 在别处只是脏，在教学工具里是致命的：它让反馈变成谎言。
        // 这条只是保险（新目录本来是空的），保留着防"跑了旧程序"。
        //
        // ⚠ 但**删不掉不能再判失败**。原来这里 `return r` 会把整次检查判成错误 ——
        //   而"清理临时文件失败"和"学生代码有问题"是两回事，不该混为一谈。
        //   实测：正确代码因此被判 ✗。
        //   现在重试几次，实在不行就忽略 —— 后面有全新目录兜底。
        尽力删除(exe);

        清理旧目录(seq);

        // ── 编译 ──
        // -std=c++17：教学用足够，而且比最新标准宽容一点（新手少撞语言特性问题）
        // -Wall：把警告也打开 —— 初学者最该看到的往往是警告而不是错误
        var args = new[] { "-std=c++17", "-O1", "-Wall", "-o", exe, src };
        var (cout, cerr, timedOut, _) = Exec(cc, args, timeoutMs);
        r.CompileOutput = (cerr + "\n" + cout).Trim();
        if (timedOut) { r.Error = "编译超时 —— 这通常不是你的代码问题，检查一下编译器是否正常"; return r; }
        if (!File.Exists(exe))
        {
            r.CompileFailed = true;
            r.Error = FirstError(r.CompileOutput);
            return r;
        }

        // ⚠ 警告检测必须读 CompileOutput。
        //
        // 我踩过：这里原来读的是 r.Raw，而 r.Raw 在运行阶段会被程序输出覆盖 ——
        // 于是检测发生在编译后（看得到警告），解释发生在运行后（看不到警告），
        // 结果就是「检测到了却解释不出来」，学生只看到一句兜底提示。
        // 一个字段承担两个阶段的输出，迟早会出这种事。

        // ⚠ 影响正确性的警告要当成问题。
        //
        // 实测：学生写 `int s;` 然后累加，程序"碰巧"跑对了（读到 0），
        // 于是被判通过 —— 而这是 C++ 里最经典的未定义行为。
        // 奖励一个真实的 bug 是最坏的教学反馈，所以这里拦下来。
        // 只拦真正影响正确性的那几类；"定义了没用"这种无关紧要的不拦。
        foreach (var w in new[] { "uninitialized", "may be used uninitialized",
                                  "return-local-addr", "array-bounds" })
        {
            if (r.CompileOutput.Contains(w)) { r.HasCorrectnessWarning = true; break; }
        }

        // ── 运行 ──
        var (rout, rerr, rtimeout, exitCode) = Exec(exe, Array.Empty<string>(), timeoutMs);
        r.RunOutput = (rerr + "\n" + rout).Trim();
        if (rtimeout)
        {
            r.Error = "程序运行超时（超过 " + timeoutMs / 1000 + " 秒）—— 检查有没有死循环";
            return r;
        }
        foreach (var line in rout.Split('\n'))
        {
            string s = line.TrimEnd('\r');
            if (s.Length > 0) r.Lines.Add(s);
        }

        // ⚠ 必须看**退出码**。
        //
        // 我踩过：整数除以 0 时进程直接崩了，但 Windows 上**既不输出也不打印任何错误**，
        // 于是 rerr 是空的、Lines 也是空的 —— 程序被当成"成功但没输出"。
        // 学生看到的是"我的程序什么都没干"，而真实原因是崩溃。
        // 这类静默失败在教学工具里最要命：反馈必须是准的，哪怕它只是"我不知道为什么，但它崩了"。
        if (exitCode != 0)
        {
            string 详情 = rerr.Trim().Length > 0
                ? "\n" + ShortErr(rerr)
                : "\n（系统没有给出具体原因）";
            r.Error = $"程序崩溃了（退出码 {exitCode}）{详情}";
            // 把崩溃信息塞进 Raw，让 ExplainCpp 的段错误/除零规则能接上
            r.RunOutput = (rerr + "\n" + rout + $"\nexit code {exitCode}").Trim();
            return r;
        }

        // 输出为空但 stderr 有内容 —— 通常是运行时警告之类
        if (r.Lines.Count == 0 && rerr.Trim().Length > 0)
        {
            r.Error = "程序没有输出，而且报了错：\n" + ShortErr(rerr);
        }
        return r;
    }

    private static (string Out, string Err, bool TimedOut, int ExitCode) Exec(
        string exe, string[] args, int timeoutMs)
    {
        var psi = new ProcessStartInfo(exe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        var outSb = new StringBuilder();
        var errSb = new StringBuilder();
        try
        {
            using var p = Process.Start(psi);
            if (p == null) return ("", "无法启动 " + exe, false, -1);
            var t1 = new Thread(() => { string? l; while ((l = p.StandardOutput.ReadLine()) != null) outSb.AppendLine(l); });
            var t2 = new Thread(() => { string? l; while ((l = p.StandardError.ReadLine()) != null) errSb.AppendLine(l); });
            t1.IsBackground = t2.IsBackground = true;
            t1.Start(); t2.Start();
            if (!p.WaitForExit(timeoutMs))
            {
                try { p.Kill(true); } catch { /* 已经退了 */ }
                return (outSb.ToString(), errSb.ToString(), true, -1);
            }
            t1.Join(3000); t2.Join(3000);
            return (outSb.ToString(), errSb.ToString(), false, p.ExitCode);
        }
        catch (Exception e)
        {
            return ("", "启动失败：" + e.Message, false, -1);
        }
    }

    /// <summary>
    /// 加载 C++ 题库：题库文件本身是 **C# 写的元数据**（描述 C++ 题目），
    /// 所以用 CSharpRunner 编译它、调它的 Dump()，把行协议收回来。
    ///
    /// 为什么不直接用 CppRunner 编译它：题库不是 C++ 代码，是题目数据。
    /// 用 C# 写数据的好处是有类型检查、能写注释、好维护 —— 见 30_Cpp.cs。
    /// </summary>
    public static CSharpResult DumpBank(string bankSource) =>
        CSharpRunner.DumpOutput(bankSource, "CppBank.Dump");
    /// <summary>
    /// 只保留最近几个运行目录 —— 每次编译一个目录会越堆越多。
    /// 清理失败不算错（可能有别的进程占着），静默忽略。
    /// </summary>
    private static void 清理旧目录(int current)
    {
        try
        {
            string root = Path.Combine(Path.GetTempPath(), "hybridengine_light_cpp");
            if (!Directory.Exists(root)) return;
            foreach (var d in Directory.GetDirectories(root))
            {
                string name = Path.GetFileName(d);
                if (!name.StartsWith("run", StringComparison.Ordinal)) continue;
                // 两种命名都要认：新的 run{进程号}_{序号}，和旧的 run{序号}
                var 段 = name.Substring(3).Split('_');
                int 序号 = 0;
                if (段.Length == 2) int.TryParse(段[1], out 序号);
                else int.TryParse(段[0], out 序号);
                // ⚠ 两条规则分开写，别合成一个复合条件 ——
                //   我第一版就是那么写的（`!本进程的 && 段.Length == 1 && …`），
                //   而新命名的段长是 2，那个条件恒为假，于是**本进程正在用的目录被删掉**，
                //   g++ 报 `main.cpp: No such file or directory`，测试全线崩。
                //
                //   规则一：本进程的目录**只删很旧的**（还在用的绝不能碰）
                //   规则二：别的进程留下的一律可清（那些再也不会被用到）
                bool 本进程的 = 段.Length == 2 && 段[0] == Environment.ProcessId.ToString();
                if (本进程的 && 序号 > current - 6) continue;
                try { Directory.Delete(d, recursive: true); } catch { /* 占着就算了 */ }
            }
        }
        catch { /* 清理是尽力而为 */ }
    }

    /// <summary>
    /// 删一个文件，失败就重试几次，最终失败也**不抛异常**。
    ///
    /// 为什么不让它失败：清理临时文件是"尽力而为"的事。
    /// 把它当成错误会让**正确的代码被判错** —— 我实测撞到过（Windows 文件锁）。
    /// 而且新目录本来是空的，删不掉也不影响"不会跑到旧程序"这件事。
    /// </summary>
    private static void 尽力删除(string path)
    {
        for (int i = 0; i < 3; i++)
        {
            try
            {
                if (!File.Exists(path)) return;
                File.Delete(path);
                return;
            }
            catch (IOException)
            {
                System.Threading.Thread.Sleep(50 * (i + 1));   // 可能是杀软在扫，等一下
            }
            catch (UnauthorizedAccessException)
            {
                System.Threading.Thread.Sleep(50 * (i + 1));
            }
            catch
            {
                return;      // 别的异常也不该让检查失败
            }
        }
        // 三次都不行就算了 —— 新目录兜底，不会跑到旧程序
    }

    /// <summary>从编译输出里挑出第一条 error（不是 warning）。</summary>
    public static string FirstError(string raw)
    {
        foreach (var line in raw.Split('\n'))
        {
            string s = line.Trim();
            int i = s.IndexOf("error:", StringComparison.Ordinal);
            if (i < 0) continue;
            int bar = s.IndexOf(" | ", StringComparison.Ordinal);      // 去掉源码片段
            if (bar > 0) s = s.Substring(0, bar);
            return s.Trim();
        }
        var ls = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return ls.Length > 0 ? ls[0].Trim() : "未知错误";
    }

    private static string ShortErr(string s)
    {
        var ls = s.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return ls.Length > 0 ? ls[0].Trim() : s.Trim();
    }
}
