// HybridEngineLight.JavaRunner —— 编译并运行学生写的 Java 代码。
//
// ══ 与 C# 版最大的不同：Java 要外部 JDK ══
//
// C# 有 dotnet 就有了一切。Java 不行 —— 编译要 javac、运行要 java，
// 它们来自**单独安装的 JDK**（注意：只有 JRE 是不够的，JRE 里没有 javac）。
// 所以这里和 CppRunner 一样，第一件事是：找不到工具链就给**安装指引**，
// 而不是抛一个"系统找不到文件 javac"。
//
// ══ 约定：文件名固定 Main.java ══
//
// Java 有一条别的语言没有的硬规则：**public 类的名字必须和文件名一致**。
// 学生写的是完整程序（`public class Main { ... }`），所以这里把源文件
// **固定写成 Main.java**，并要求学生的公共类叫 Main。
// 写别的名字不会"悄悄失败"，javac 会明确报：
//     class Hello is public, should be declared in a file named Hello.java
// 这条报错由 ExplainJava 翻译 —— 这是 Java 新手第一个撞上的墙。
//
// 于是运行入口也固定：`java Main`（必须在 Main.java 所在的目录里跑）。
//
// ══ 踩过的坑（从 CppRunner 继承的两条 + 本机实测的三条）══
//
// ① 复用编译目录 = 跑出上一次的结果（CppRunner 踩过）。
//    C++ 那边是链接器复用旧 .o / .exe。Java 这边同样会：
//    如果目录里躺着上一次的 Main.class，而这次 javac **编译失败了**，
//    `java Main` 照样能把那个旧 class 跑起来 ——
//    学生看到的就是「我明明写错了，却跑出了正确结果」。教学工具里这是致命的：
//    反馈必须说的是**你刚写的这段代码**。
//    做法和 CppRunner 一样：每次一个全新目录 run{N}，另外再显式删一次
//    Main.class 兜底（见 BuildAndRun 里的说明）。
//
// ② 崩溃了却被当成"成功但没输出"（CppRunner 踩过）。
//    C++ 的整数除零在 Windows 上什么都不打印，只能靠退出码认出来。
//    Java 好一些：未捕获的异常会打到 stderr 并把退出码置成 1，
//    但**退出码检查照样不能省** —— System.exit(n)、被 kill、
//    以及下面第 ③ 条那种"没抛异常但也没跑成"的情况，都只有退出码能发现。
//
// ③ **javac 的报错是跟着系统语言走的**（本机实测，这条最坑）。
//    这台机器上 `javac Main.java` 的默认输出是：
//        Main.java:3: 错误: 需要';'
//    而且是 **GBK 编码**的字节流（.NET 侧没有 GBK 解码器 ——
//    Encoding.GetEncoding(936) 在这里直接抛 NotSupportedException，
//    要它就得引入 System.Text.Encoding.CodePages 包，破"零依赖"）。
//    所以什么都不做的话，学生看到的是一串 `����: ��Ҫ';'`。
//
//    做法：**只钉编码，不动语言** —— 报错保持中文，字节变成 UTF-8。
//        -Dstdout.encoding=UTF-8 -Dstderr.encoding=UTF-8   ← JDK 18+ 认这两个
//        -Dfile.encoding=UTF-8                             ← 更老的 JDK 只认这个
//    （javac 要用 -J 前缀把 -D 转交给它背后的 JVM。）
//    实测加上之后抓回来就是正确的中文：
//        Main.java:3: 错误: 需要';'
//
//    ⚠ 另一条路是设 JAVA_TOOL_OPTIONS 环境变量（效果一样，JDK 8 也认），
//      但每跑一次 java / javac 都会往输出里多打一行
//          Picked up JAVA_TOOL_OPTIONS: -Dfile.encoding=UTF-8 ...
//      那行会混进学生的程序输出里。用命令行参数就没这个噪音，所以这里选命令行。
//      （不过学生自己的机器上很可能早就设了这个变量，所以 BuildAndRun
//        仍然会把那行滤掉 —— 见 去掉Jvm噪音 。）
//
//    ⚠ 报错**故意保持中文**：学生看到的中文报错，和我们讲给他的中文解释是同一套词
//      （"找不到符号" 对应 "编译器不认识这个名字"），比英文原文好懂。
//      但 ExplainJava 的规则表**两种语言都认** —— 因为英文系统的机器、
//      或者学生自己从终端贴过来的原文，都可能是英文的。
//
// ④ **CLASSPATH 环境变量会把 `java Main` 指到别的目录去**（本机实测）。
//    实测：在 A 目录里 `java Main`，但 CLASSPATH 指向 B 目录 ——
//    跑起来的**是 B 目录里的那个 Main**，输出完全来自另一个程序。
//    这就是第 ① 条那个"跑了别的东西"的坑，换了个入口又出现一次。
//    所以运行和编译都显式带 `-cp .`，把"去哪儿找类"钉死在当前目录。
//
// ⑤ **javac 和 java 版本不一致会炸在运行期**（本机实测）。
//    用 JDK 25 的 javac 编译、用 JDK 21 的 java 运行：
//        java.lang.UnsupportedClassVersionError: Main has been compiled by
//        a more recent version of the Java Runtime (class file version 69.0),
//        this version of the Java Runtime only recognizes class file versions up to 65.0
//    编译这一步完全成功，失败发生在运行 —— 报错还看不出跟"两个 JDK"有关。
//    所以找工具链时**优先用和 javac 同一个目录下的 java**（同一套 JDK 里的一对），
//    只有那里没有才退回 PATH 上的 java。
//
// ⑥ 命令行参数与标准输入：学生程序里写 `args[0]` 就会在**没传参数**时越界
//    （实测 Index 0 out of bounds for length 0），这是 ExplainJava 里的一条。
//    另外这里明确把子进程的 stdin **重定向后立刻关掉**：
//    学生在课里写的 Scanner 程序不可能有人在旁边敲键盘，
//    不关掉的话它会一直等输入直到超时，反馈就变成"超时"而不是
//    "你用了 Scanner 但这里没有输入" —— 后者才是他该看到的信息。
//
// ══ 安全说明 ══
// 学生代码以**当前用户权限**在本机编译运行 —— 与他自己在终端
// `javac Main.java && java Main` 完全等价。所以：**不要拿它跑来路不明的代码。**

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace HybridEngine-light;

public sealed class JavaResult
{
    /// <summary>程序打印的每一行（只有 stdout，和 CppResult.Lines 一致）。</summary>
    public List<string> Lines { get; } = new();
    /// <summary>失败信息（null = 成功）。</summary>
    public string? Error { get; set; }
    /// <summary>
    /// 编译器的原始输出（错误 + 警告）。
    /// ⚠ 和 RunOutput 分开存：CppRunner 踩过"一个字段承担两个阶段的输出"的坑 ——
    /// 警告检测发生在编译后、解释发生在运行后，读到的是被覆盖过的内容，
    /// 于是"检测到了却解释不出来"。这里从字段上就分开。
    /// </summary>
    public string CompileOutput { get; set; } = "";
    /// <summary>程序运行时的输出（stdout + stderr，异常堆栈就在里面）。</summary>
    public string RunOutput { get; set; } = "";
    /// <summary>两者的合并 —— 给"我不知道该看哪个"的调用方用。</summary>
    public string Raw => (CompileOutput + "\n" + RunOutput).Trim();
    /// <summary>区分"编译失败"和"运行出错" —— 两者的解释方向完全不同。</summary>
    public bool CompileFailed { get; set; }
}

public static class JavaRunner
{
    /// <summary>目录里最多留几个运行目录。</summary>
    private const int 保留目录数 = 5;

    private static int _seq = -1;                  // -1 = 还没初始化（见 NextSeq）
    private static string? _compiler;              // javac
    private static string? _runtime;               // java
    private static bool _searched;
    private static readonly object Gate = new();

    /// <summary>javac 的路径。null = 没找到。</summary>
    public static string? Compiler
    {
        get { lock (Gate) { EnsureSearched(); return _compiler; } }
    }

    /// <summary>
    /// java（运行期）的路径。
    /// 单独暴露是因为**编译和运行可以是两套 JDK** —— 那正是
    /// UnsupportedClassVersionError 的来源，排查时要能分别看到两个路径。
    /// </summary>
    public static string? Runtime
    {
        get { lock (Gate) { EnsureSearched(); return _runtime; } }
    }

    /// <summary>没找到 JDK 时给学生看的说明。</summary>
    public static string InstallHint =>
        "没有找到 Java 编译器（javac）。\n\n" +
        "Java 和 Python、C# 不一样 —— 它需要**单独安装 JDK**：\n" +
        "⚠ 注意是 JDK，不是 JRE。JRE 只能运行 Java 程序，里面没有 javac，编译不了。\n\n" +
        "  Windows  推荐用 winget 装（任选一个）：\n" +
        "             winget install Microsoft.OpenJDK.21\n" +
        "             winget install EclipseAdoptium.Temurin.21.JDK\n\n" +
        "  macOS    在终端运行：\n" +
        "             brew install openjdk@21\n" +
        "           （或者去 adoptium.net 下 .pkg 安装包）\n\n" +
        "  Linux    用包管理器装（注意带 -jdk / -devel，别装成 jre）：\n" +
        "             Ubuntu/Debian:  sudo apt install openjdk-21-jdk\n" +
        "             Fedora:         sudo dnf install java-21-openjdk-devel\n\n" +
        "装完**重开一个终端**（让 PATH 生效），然后确认这两条都能打印版本号：\n" +
        "    javac -version\n" +
        "    java  -version\n\n" +
        "如果两条的版本号不一样（比如 javac 是 25、java 是 21），\n" +
        "编译能过但运行会报 UnsupportedClassVersionError —— 把两个都指向同一套 JDK 就好。";

    private static void EnsureSearched()
    {
        if (_searched) return;
        _searched = true;
        (_compiler, _runtime) = Find();
    }

    /// <summary>
    /// 找 javac 和 java。
    ///
    /// 顺序：**PATH 优先**（学生自己在终端敲 javac 用的是哪个，这里就用哪个 ——
    /// 报错才对得上他的实际操作），PATH 里找不到再看 JAVA_HOME。
    /// 找到 javac 之后，优先配**同一个 bin 目录里的 java**（见文件头第 ⑤ 条）。
    /// </summary>
    private static (string? Javac, string? Java) Find()
    {
        foreach (var dir in SearchDirs())
        {
            string? javac = FirstExisting(dir, "javac");
            if (javac == null) continue;
            if (!Usable(javac, "-version")) continue;

            string? 同目录的java = FirstExisting(dir, "java");
            if (同目录的java != null && Usable(同目录的java, "-version"))
                return (javac, 同目录的java);

            // 同目录没有 java：退回 PATH 上那个 —— 版本可能对不上，
            // 但那属于"该报的错要报出来"，而不是这里替它决定。
            return (javac, Usable("java", "-version") ? "java" : null);
        }

        // PATH 里没有能找到的实体文件（可能是某种包装脚本），最后再按名字试一次
        if (Usable("javac", "-version")) return ("javac", "java");
        return (null, null);
    }

    /// <summary>PATH 里的每个目录，再加上 JAVA_HOME\bin（PATH 里都没有时才轮到它）。</summary>
    private static IEnumerable<string> SearchDirs()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(path))
        {
            foreach (var d in path.Split(Path.PathSeparator))
            {
                string s = d.Trim().Trim('"');
                if (s.Length == 0) continue;
                if (seen.Add(s)) yield return s;
            }
        }
        string? home = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrEmpty(home))
        {
            string bin = Path.Combine(home.Trim().Trim('"'), "bin");
            if (seen.Add(bin)) yield return bin;
        }
    }

    /// <summary>
    /// 这个目录里有没有 javac / java。
    /// Windows 上是 .exe；别的平台没有扩展名 —— 两种都试，用运行时判断而不是编译期。
    /// </summary>
    private static string? FirstExisting(string dir, string name)
    {
        foreach (var file in OperatingSystem.IsWindows()
                     ? new[] { name + ".exe", name }
                     : new[] { name, name + ".exe" })
        {
            try
            {
                string p = Path.Combine(dir, file);
                if (File.Exists(p)) return p;
            }
            catch { /* 路径里有非法字符之类，跳过 */ }
        }
        return null;
    }

    /// <summary>能不能跑起来（`-version` 返回 0）。</summary>
    private static bool Usable(string exe, string arg)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, arg)
            {
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p == null) return false;
            p.StandardOutput.ReadToEnd();
            p.StandardError.ReadToEnd();
            p.WaitForExit(5000);
            return p.ExitCode == 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// 把完整的 Java 源码写下来、编译、运行，收取输出。
    ///
    /// <paramref name="source"/> 必须是**完整程序**，而且要满足这个约定：
    ///   · 公共类叫 <c>Main</c>（因为文件名固定是 Main.java，见文件头）
    ///   · 有 <c>public static void main(String[] args)</c>
    /// 违反约定不会静默失败，会在编译或启动时报错，交给 ExplainJava 翻译。
    /// </summary>
    public static JavaResult BuildAndRun(string source, int timeoutMs = 60_000)
    {
        var r = new JavaResult();
        string? javac = Compiler;
        if (javac == null) { r.Error = InstallHint; return r; }
        string java = Runtime ?? "java";

        // ⚠ 每次一个**全新目录**（见文件头第 ① 条）。
        int seq = NextSeq();
        // ⚠ 目录名**必须带进程号**。
        //
        //   同事实测复现过：并行跑两条检查，NextSeq 在两个进程里**同时**扫到
        //   同一个最大序号，于是都返回同一个 run{N} —— 撞在一起，
        //   其中一个报 "Main.java because it is being used by another process"，
        //   **那次检查假失败**（学生代码没问题，是工具自己的问题）。
        //
        //   带进程号之后，两个进程的目录永远不同；序号只用于同一个进程内的多次调用，
        //   而那部分有 lock (Gate) 保护，本来就安全。
        string dir = Path.Combine(Path.GetTempPath(), "hybridengine_light_java",
                                  $"run{Environment.ProcessId}_{seq}");
        Directory.CreateDirectory(dir);
        string src = Path.Combine(dir, "Main.java");
        string cls = Path.Combine(dir, "Main.class");

        try { File.WriteAllText(src, source, new UTF8Encoding(false)); }
        catch (Exception e) { r.Error = "无法写入源文件：" + e.Message; return r; }

        // ⚠ 编译之前先把上一轮的 Main.class 删掉。
        //
        // 新目录本来应该是空的，但这条保险必须留：一旦有人把目录改回固定路径，
        // 或者两次进程撞上了同一个 run{N}（NextSeq 只在**首次**扫描一次已有目录，
        // 两个 HybridEngine-light 同时启动仍可能撞），
        // 那么 javac 编译失败时这里会留下**上一个程序**的 Main.class ——
        // `java Main` 照样能跑起来，学生看到的是"我写错了却跑出了结果"。
        // 这一行就是挡住它的东西。（CppRunner 里有同样的保险，原因一模一样。）
        try { if (File.Exists(cls)) File.Delete(cls); }
        catch (Exception e) { r.Error = "无法清理上一轮的 class 文件：" + e.Message; return r; }

        清理旧目录(seq);

        // ── 编译 ──
        // -encoding UTF-8   源码是我们自己按 UTF-8 写的；不写的话老 JDK 会按系统
        //                   编码读，中文串会**悄悄变成乱码**（新版 JDK 默认已是 UTF-8，
        //                   但这里不能赌学生的 JDK 是哪个版本）。
        // -g                保留局部变量名。实测差别很大：
        //                     不加 因为 "<local1>" 是 null
        //                     加   因为 "s" 是 null
        //                   对新手来说，能说出变量名的 NPE 提示价值天差地别。
        // -cp .             钉死"去哪儿找类"，防 CLASSPATH 劫持（见文件头第 ④ 条）。
        // -J-D...           转交给 javac 背后的 JVM：字节用 UTF-8，报错保持中文（第 ③ 条）。
        //                   加了 -J-Dfile.encoding 是为了老 JDK（18 之前不认 stdout.encoding）。
        // 不加 -Xlint:all   实测 `List xs = new ArrayList()` 这种完全合法的新手写法
        //                   会被 rawtypes / unchecked 刷 3 条警告；新版 JDK 还会警告
        //                   this-escape 之类跟初学者毫无关系的东西。
        //                   Java 的编译器在真正要紧的地方（未初始化变量等）本来就是**报错**，
        //                   不需要像 C++ 那样靠 -Wall 补。
        var javacArgs = new[]
        {
            "-encoding", "UTF-8",
            "-g",
            "-cp", ".",
            "-J-Dstdout.encoding=UTF-8", "-J-Dstderr.encoding=UTF-8", "-J-Dfile.encoding=UTF-8",
            "Main.java",
        };
        var (cout, cerr, cTimedOut, cExit) = Exec(javac, javacArgs, dir, timeoutMs, closeStdin: false);
        cout = 去掉Jvm噪音(cout);
        cerr = 去掉Jvm噪音(cerr);
        r.CompileOutput = (cerr + "\n" + cout).Trim();
        if (cTimedOut)
        {
            r.Error = "编译超时 —— 这通常不是你的代码问题，检查一下 JDK 是否正常";
            return r;
        }

        // 判"编译成功"看**退出码** —— 这是最权威的信号。
        //
        // 为什么不把"Main.class 在不在"也算进来（CppRunner 是看文件存在与否的）：
        // javac 的退出码比 C++ 链接器可靠得多，而"文件不在"有一种**合法**情况 ——
        // 学生写了 `package foo;` 时 class 会被放进 foo/Main.class（实测）。
        // 那不是编译失败，硬判成失败反而会把真正有用的报错吞掉
        // （运行期那句「找不到或无法加载主类 Main」+ wrong name: foo/Main 才是答案）。
        // 至于"旧产物"那个坑：编译前已经显式删过 Main.class，目录也是全新的，
        // 而且运行时带着 -cp .，跑不到别处去。三道保险都在。
        if (cExit != 0)
        {
            r.CompileFailed = true;
            r.Error = FirstError(r.CompileOutput);
            // 编译没过就**不要运行** —— 跑了也只能是旧产物
            return r;
        }

        // ── 运行 ──
        // -cp . 同理，而且这里更要紧：CLASSPATH 一变，跑的就是别人的 Main。
        // 另外注意这里写的是类名 `Main`，不是文件名 `Main.java`。
        var javaArgs = new[]
        {
            "-Dstdout.encoding=UTF-8", "-Dstderr.encoding=UTF-8", "-Dfile.encoding=UTF-8",
            "-cp", ".", "Main",
        };
        var (rout, rerr, rTimedOut, rExit) = Exec(java, javaArgs, dir, timeoutMs, closeStdin: true);
        // ⚠ 先滤噪音再往下走 —— 下面的 Lines 是"程序打印的每一行"，
        // 混进一行 Picked up JAVA_TOOL_OPTIONS 会让输出比对全部失败（见 去掉Jvm噪音）。
        rout = 去掉Jvm噪音(rout);
        rerr = 去掉Jvm噪音(rerr);
        r.RunOutput = (rerr + "\n" + rout).Trim();

        if (rTimedOut)
        {
            r.Error = "程序运行超时（超过 " + timeoutMs / 1000 + " 秒）—— 检查有没有死循环";
            return r;
        }

        // 先把已经打出来的行收进来，**再看退出码**。
        // 顺序是有意的：学生程序常常"打印了几行然后崩了"，
        // 那几行正是最有力的线索（例如数组越界前打印的 "before"）。
        foreach (var line in rout.Split('\n'))
        {
            string s = line.TrimEnd('\r');
            if (s.Length > 0) r.Lines.Add(s);
        }

        // ⚠ 必须看退出码（文件头第 ② 条）。
        //
        // Java 的未捕获异常会走 stderr 并把退出码置 1，但**不能只靠 stderr**：
        //   · 有的失败什么都不打印（例如 System.exit(1)）
        //   · Error: Main method not found ... 这行也不是异常堆栈，
        //     靠 "Exception in thread" 之类的特征去找会漏掉
        // 退出码是唯一每次都准的信号。CppRunner 就是在这上面栽过：
        // 程序崩了既不输出也不报错，被当成"成功但没输出"。
        if (rExit != 0)
        {
            string 详情 = rerr.Trim().Length > 0
                ? "\n" + ShortErr(rerr)
                : "\n（系统没有给出具体原因）";
            r.Error = $"程序没有正常结束（退出码 {rExit}）{详情}";
            // 把退出码也塞进 Raw，让 ExplainJava 的规则有据可依
            r.RunOutput = (rerr + "\n" + rout + $"\nexit code {rExit}").Trim();
            return r;
        }

        // 输出为空但 stderr 有内容 —— 通常是运行时警告之类
        if (r.Lines.Count == 0 && rerr.Trim().Length > 0)
            r.Error = "程序没有输出，而且报了错：\n" + ShortErr(rerr);

        return r;
    }

    /// <summary>
    /// 下一个运行目录序号。
    ///
    /// ⚠ 第一次用的时候**先扫一遍已有的 run{N}，接着最大的往下编号**。
    /// 不这么做的话，新进程又从 run1 开始，正好撞上前一次进程留下的同名目录
    /// （CppRunner 就是这样：序号从 0 起，上一个进程的 run1..run5 谁也清不掉，
    ///  因为清理规则只删"比自己小的"）。从最大号接着走，两件事一起解决：
    /// 不会踩旧目录，旧目录也能被正常清理掉。
    /// </summary>
    private static int NextSeq()
    {
        lock (Gate)
        {
            if (_seq < 0)
            {
                _seq = 0;
                try
                {
                    string root = Path.Combine(Path.GetTempPath(), "hybridengine_light_java");
                    if (Directory.Exists(root))
                        foreach (var d in Directory.GetDirectories(root))
                        {
                            string name = Path.GetFileName(d);
                            if (name.StartsWith("run", StringComparison.Ordinal)
                                && int.TryParse(name.Substring(3), out int n) && n > _seq)
                                _seq = n;
                        }
                }
                catch { /* 扫不动就从 0 开始，最多是目录变多，不影响正确性 */ }
            }
            return ++_seq;
        }
    }

    /// <summary>
    /// 只保留最近几个运行目录 —— 每次编译一个目录会越堆越多。
    ///
    /// ⚠ 只删**序号比当前小**的目录。序号更大的可能是**另一个 HybridEngine-light 实例**
    /// 正在用的运行目录，删了会把别人正在跑的编译搅坏 ——
    /// 少清几个目录只是脏，删错别人的目录是错。
    /// 清理失败不算错（文件可能被占用），静默忽略。
    /// </summary>
    private static void 清理旧目录(int current)
    {
        try
        {
            string root = Path.Combine(Path.GetTempPath(), "hybridengine_light_java");
            if (!Directory.Exists(root)) return;
            foreach (var d in Directory.GetDirectories(root))
            {
                string name = Path.GetFileName(d);
                if (!name.StartsWith("run", StringComparison.Ordinal)) continue;
                if (!int.TryParse(name.Substring(3), out int n)) continue;
                if (n > current - 保留目录数) continue;
                try { Directory.Delete(d, recursive: true); } catch { /* 占着就算了 */ }
            }
        }
        catch { /* 清理是尽力而为 */ }
    }

    /// <summary>
    /// 起一个子进程、收它的输出、必要时超时杀掉。
    /// <paramref name="closeStdin"/> = true 时把标准输入重定向后立刻关掉：
    /// 学生写的 Scanner 程序在这里没有人在旁边敲键盘，与其挂到超时，
    /// 不如让它马上读到 EOF 抛 NoSuchElementException —— 那才是能解释的信息。
    /// </summary>
    private static (string Out, string Err, bool TimedOut, int ExitCode) Exec(
        string exe, string[] args, string workDir, int timeoutMs, bool closeStdin)
    {
        var psi = new ProcessStartInfo(exe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = closeStdin,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workDir,
            // 输出编码固定按 UTF-8 读 —— 子进程那边也用 -Dstdout.encoding=UTF-8 钉死了，
            // 两头一致才不会出现"中文变问号"。
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
            if (closeStdin) { try { p.StandardInput.Close(); } catch { /* 已经关了 */ } }

            if (!p.WaitForExit(timeoutMs))
            {
                // Kill(true) = 连子进程一起杀。实测死循环的 java 能被干净杀掉，
                // 不留残余进程（java.exe 本身就是 JVM，不会再套一层）。
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
    /// 去掉 JVM 自己往输出里塞的噪音行。
    ///
    /// 实测：只要环境里有 JAVA_TOOL_OPTIONS（很多开发机上都设着），
    /// 每次 java / javac 都会先打一行
    ///     Picked up JAVA_TOOL_OPTIONS: -Dfile.encoding=UTF-8 ...
    /// 这行**混在学生的程序输出里**。后果不是"多一行难看"这么轻 ——
    /// 它会让「程序输出和预期比对」全部失败：程序明明打印对了，可比对时
    /// 第一行是这行噪音，于是判成错。所以必须在收输出的时候就滤掉。
    ///
    /// 顺手把 JDK_JAVA_OPTIONS 的那行也滤掉（同一类东西，写法不同）。
    /// 注意：我们自己**不设**这些环境变量（见文件头第 ③ 条，
    /// 这里用命令行参数替代了），这一层防的是学生机器上本来就有的设置。
    /// </summary>
    private static string 去掉Jvm噪音(string text)
    {
        var sb = new StringBuilder();
        foreach (var line in text.Split('\n'))
        {
            string s = line.TrimEnd('\r');
            if (s.StartsWith("Picked up JAVA_TOOL_OPTIONS:", StringComparison.Ordinal)) continue;
            if (s.StartsWith("Picked up JDK_JAVA_OPTIONS:", StringComparison.Ordinal)) continue;
            if (s.StartsWith("NOTE: Picked up JDK_JAVA_OPTIONS:", StringComparison.Ordinal)) continue;
            sb.Append(s).Append('\n');
        }
        return sb.ToString().Trim();
    }

    /// <summary>
    /// 从输出里挑出第一条 error（不是 warning、不是 Note）。
    ///
    /// javac 的一行长这样：  Main.java:5: 错误: 找不到符号     （中文环境）
    ///                 或    Main.java:5: error: cannot find symbol （英文环境）
    /// 这里只去掉 "错误:" / "error:" 这个词，**文件名和行号留着** ——
    /// 它们是学生唯一能自己定位的线索。
    /// （临时目录路径不会出现在输出里，因为我们传的是相对文件名 "Main.java"，
    ///  所以不需要像 CSharpRunner 那样去剥路径。）
    /// </summary>
    public static string FirstError(string raw)
    {
        foreach (var line in raw.Split('\n'))
        {
            string s = line.Trim();
            if (IsErrorLine(s)) return StripErrorWord(s);
        }
        // 没有 error 行：可能是运行期的失败（Main method not found / 异常）
        foreach (var line in raw.Split('\n'))
        {
            string s = line.Trim();
            if (s.Contains("Exception in thread", StringComparison.Ordinal)
                || s.Contains("Error:", StringComparison.Ordinal)
                || s.Contains("错误:", StringComparison.Ordinal)) return s;
        }
        var ls = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return ls.Length > 0 ? ls[0].Trim() : "未知错误";
    }

    /// <summary>
    /// 这一行是不是编译器报的 error。
    /// 用正则卡住 "文件.java:行号: error:" 的形状，而不是简单 Contains("error:") ——
    /// 学生自己的源码会被原样回显在报错下面，
    /// <c>System.out.println("error: 说明")</c> 这种行不该被当成错误行。
    /// 中文的 "错误:" 也认，因为规则不该依赖"我们一定成功地把 javac 调成了英文"。
    /// </summary>
    internal static bool IsErrorLine(string line) =>
        Regex.IsMatch(line, @"\.java:\d+:\s*(error|错误)\s*[:：]")
        || Regex.IsMatch(line, @"^\s*(error|错误)\s*[:：]");

    /// <summary>把 "Main.java:5: error: 说明" 变成 "Main.java:5: 说明"。</summary>
    private static string StripErrorWord(string s) =>
        Regex.Replace(s, @"\b(error|错误)\s*[:：]\s*", "", RegexOptions.None);

    private static string ShortErr(string s)
    {
        var ls = s.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return ls.Length > 0 ? ls[0].Trim() : s.Trim();
    }
}
