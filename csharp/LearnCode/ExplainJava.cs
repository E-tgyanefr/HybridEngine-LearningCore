// LearnCode.ExplainJava —— 把 javac 的报错和 Java 的运行时异常翻译成人话。
//
// ══ 这一版和 ExplainCpp 最大的不同 ══
//
// C++ 的难处是"**报错行经常是错的**"（真凶在上一行），所以 ExplainCpp 的第一条规则
// 就是教人往上看一行。
// Java 不一样：javac 的语法分析很稳，**报错行基本就是真凶那一行**，
// 而且它还会把符号名、类型名直接写在错误信息里（symbol / location），
// 信息量比 C++ 大得多。
//
// 所以这里的重心不在"猜位置"，而在两件事：
//
//   ① **把 Java 特有的那几条规则讲透**。新手在 Java 上卡住的地方高度集中：
//      公共类名必须和文件名一致、局部变量必须先赋初值、不做隐式窄化、
//      忘了 static、cannot find symbol、NPE……
//      每一条都要说清"怎么改"和"为什么 Java 要这么设计"。
//
//   ② **区分"会报错的坑"和"不报错但算错的坑"**。
//      整数除法、用 == 比字符串、把 double 隐式截断 —— 这几个 Java **一声不吭**，
//      结果就是错的。会报错的坑学生自己能查，静默的坑只能靠这里提醒。
//      （规则表最后一段专门讲这个，只在"程序跑完了但结果不对"时出现。）
//
// ══ 关于语言 ══
//
// JavaRunner 给 javac / java 都传了 -Duser.language=en，所以我们拿到的是英文报错。
// 但规则表**同时认中文**（"错误:" / "找不到符号" / "需要';'"）——
// 因为学生自己也可能在终端里跑 javac 然后把中文报错贴进来，
// 而且"一定成功地让 JVM 说英文"这件事本身不该被当成前提。
//
// ══ 结构（三个语言版本统一）══
//     发生了什么 → 出在哪一行 → 怎么改 → 顺便记住
//
// ══ 不做什么 ══
//   · 不自动改代码 —— 新手学不到东西，而且会改错
//   · 不吞掉原始报错 —— 原文始终保留在末尾
//   · 认不出来就说认不出来，不编一个"大概是……"糊弄过去

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LearnCode;

public static class ExplainJava
{
    /// <summary>把一次编译/运行失败变成一段给人看的解释。</summary>
    /// <param name="raw">编译器或程序的原始输出</param>
    /// <param name="source">学生写的源码（用来判断上下文，例如 main 是怎么写的）</param>
    /// <param name="compileFailed">true=编译失败；false=运行时出错</param>
    public static string For(string raw, string source, bool compileFailed)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append('\n').Append('─', 56).Append('\n');
        sb.Append(compileFailed ? "编译没能通过\n" : "程序运行时出错了\n");
        sb.Append('─', 56).Append('\n');

        var (行号, 消息) = PickError(raw, source);
        if (行号 > 0)
        {
            sb.Append($"出在这里：第 {行号} 行");
            var 源码行 = SourceLine(source, 行号);
            if (源码行.Length > 0) sb.Append($"\n    {源码行}");
            sb.Append("\n\n");
        }

        var body = Rules(消息, raw, source, 行号, compileFailed);
        if (body.Count > 0)
        {
            foreach (var l in body) sb.Append(l).Append('\n');
        }
        else
        {
            sb.Append("这个错误我没有内置解释（规则表里没有它）。\n\n");
            sb.Append("通用的排查办法：\n");
            sb.Append("  1. 先看**第一条** error —— 后面的往往只是它引起的连锁反应\n");
            sb.Append("  2. 括号 {} 有没有配对、语句末尾有没有分号\n");
            sb.Append("  3. 把报错原文复制去搜索（下面就是原文，方便复制）\n");
        }

        sb.Append('\n').Append("原始报错（老师或搜索需要用到的原文）：\n");
        sb.Append('─', 56).Append('\n');
        // 原始输出可能很长（运行时异常前面会有几十行 JDK 内部的堆栈帧），
        // 截断一下，但保留第一条 error / 第一行异常。
        var lines = raw.Split('\n');
        int shown = 0;
        foreach (var l in lines)
        {
            sb.Append(l.TrimEnd()).Append('\n');
            if (++shown >= 24) { sb.Append("（…后面的输出省略）\n"); break; }
        }
        return sb.ToString();
    }

    // ══════════════════ 定位 ══════════════════

    /// <summary>
    /// 从输出里取 (行号, 消息)。
    /// 三种来源，按可靠性排序：
    ///   ① javac 的编译错误    Main.java:5: error: cannot find symbol
    ///   ② 运行时异常堆栈       Exception in thread "main" java.lang.NullPointerException: ...
    ///   ③ 启动期错误（不是异常）Error: Main method not found in class Main, ...
    /// 第 ③ 种最容易被漏掉：它既不是编译错误，也不是异常，只在退出码上是失败。
    /// </summary>
    private static (int Line, string Msg) PickError(string raw, string source)
    {
        // ① 启动期错误**先查**：它既不是编译错误也不是异常，只有退出码是失败。
        //
        // ⚠ 顺序很重要：中文版这行长这样
        //        错误: 在类 Main 中找不到 main 方法, 请将 main 方法定义为:
        //    它也以 "错误:" 开头，要是让下面的编译错误循环先抓到，
        //    就会被当成一条"行号 0 的编译错误"，后面的规则全都对不上号。
        //    （实测：中文的措辞是「找不到 main 方法」，不是「找不到主方法」——
        //      这两个都得认。英文环境是 "Main method not found"。）
        if (Has(raw, "Main method not found", "找不到 main 方法", "找不到主方法",
                "Could not find or load main class", "找不到或无法加载主类"))
        {
            // 这类错误没有堆栈，指不出行号 —— 那就自己从源码里找一行顶上。
            // 要找的**不是**"main 在哪"，而是"学生该改哪一行"：
            //   · 多半是 main 的签名写错了 → 指 main 那一行
            //   · 但如果是 class 找不到（package 声明惹的祸）→ 该指 package 那一行
            // 指错行会让人白改半天，所以这两种分开处理。
            bool 是找不到类 = Has(raw, "Could not find or load main class", "找不到或无法加载主类");
            int 行 = (是找不到类 ? PackageLine(source) : 0);
            if (行 == 0) 行 = MainLine(source);
            return (行, FirstMeaningfulLine(raw));
        }

        // ② 编译错误
        foreach (var line in raw.Split('\n'))
        {
            string s = line.Trim();
            if (!JavaRunner.IsErrorLine(s)) continue;
            int n = 0;
            var lm = Regex.Match(s, @"\.java:(\d+):");
            if (lm.Success) n = int.Parse(lm.Groups[1].Value);
            // 去掉 "路径:行号: 错误:" 这一段，只留说明本身
            string msg = Regex.Replace(s, @"^.*?\b(error|错误)\s*[:：]\s*", "");
            return (n, msg.Trim());
        }

        // ③ 运行时异常
        // ⚠ 只认 java.lang.XXXException 这种**类名**，不靠 "Exception in thread" 的中文译法 ——
        //   实测这行头在中文语言环境下**也是英文**（"Exception in thread "main" " 原样输出），
        //   但类名是永远不本地化的，用它最稳。
        var ex = Regex.Match(raw, "Exception in thread \"[^\"]*\"\\s+([\\w.$]+)(?::\\s*([^\\n]*))?");
        if (ex.Success)
        {
            string type = ex.Groups[1].Value;
            int dot = type.LastIndexOf('.');
            if (dot >= 0) type = type.Substring(dot + 1);          // java.lang.NullPointerException → NullPointerException
            string detail = ex.Groups[2].Success ? ex.Groups[2].Value.Trim() : "";
            return (FrameLine(raw), detail.Length > 0 ? type + ": " + detail : type);
        }

        // ④ 什么特征都没有，只有退出码（CppRunner 里那个"崩了却什么都不说"的情况）
        if (raw.Contains("exit code", StringComparison.Ordinal))
        {
            var m = Regex.Match(raw, @"exit code (-?\d+)");
            return (0, "程序异常退出（退出码 " + (m.Success ? m.Groups[1].Value : "?") + "）");
        }

        return (0, "");
    }

    /// <summary>
    /// 异常堆栈里**学生自己那一帧**的行号。
    ///
    /// 为什么不能取第一帧：NumberFormatException 的堆栈前 3 帧全是 JDK 内部
    ///   at java.base/java.lang.Integer.parseInt(Integer.java:565)
    ///   at java.base/java.lang.Integer.parseInt(Integer.java:662)
    ///   at Main.main(Main.java:4)          ← 只有这一帧跟学生有关
    /// 直接报 Integer.java:565 会把学生带到 JDK 源码里去。
    /// </summary>
    private static int FrameLine(string raw)
    {
        var m = Regex.Match(raw, @"\(Main\.java:(\d+)\)");
        if (m.Success) return int.Parse(m.Groups[1].Value);
        m = Regex.Match(raw, @"\((\w+)\.java:(\d+)\)");
        return m.Success ? int.Parse(m.Groups[2].Value) : 0;
    }

    /// <summary>
    /// 源码里 main 声明在第几行（用来给"没有堆栈"的启动错误指个位置）。
    ///
    /// ⚠ 必须**忽略大小写**：这一条最常服务的场景恰恰是学生把 main 写成了 Main，
    /// 严格匹配小写的话就正好找不到那一行 —— 而那一行就是他要改的地方。
    /// </summary>
    private static int MainLine(string source)
    {
        var ls = source.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < ls.Length; i++)
            if (Regex.IsMatch(ls[i], @"\bmain\s*\(", RegexOptions.IgnoreCase)) return i + 1;
        return 0;
    }

    /// <summary>
    /// 源码里 `package xxx;` 在第几行（没有就返回 0）。
    /// 「找不到主类」那种错误要找的是**该删掉的那一行** —— 也就是这个。
    /// </summary>
    private static int PackageLine(string source)
    {
        var ls = source.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < ls.Length; i++)
            if (Regex.IsMatch(ls[i], @"^\s*package\s+[\w.]+\s*;")) return i + 1;
        return 0;
    }

    private static string FirstMeaningfulLine(string raw)
    {
        foreach (var l in raw.Split('\n'))
        {
            string s = l.Trim();
            if (s.Length > 0) return s;
        }
        return "";
    }

    private static string SourceLine(string source, int lineNo)
    {
        var ls = source.Replace("\r\n", "\n").Split('\n');
        if (lineNo >= 1 && lineNo <= ls.Length) return ls[lineNo - 1].TrimEnd();
        return "";
    }

    /// <summary>有没有命中这批关键词里的任意一个（英文 + 中文写法都要给）。</summary>
    private static bool Has(string hay, params string[] needles)
    {
        foreach (var n in needles)
            if (hay.Contains(n, StringComparison.Ordinal)) return true;
        return false;
    }

    // ══════════════════ main 写法检查 ══════════════════

    /// <summary>源码里是不是标准的 <c>public static void main(String[] args)</c>。</summary>
    private static bool StandardMain(string source) =>
        Regex.IsMatch(source, @"public\s+static\s+void\s+main\s*\(\s*String\s*(\[\s*\]\s*\w+|\w+\s*\[\s*\])\s*\)");

    /// <summary>
    /// 《main 写法不标准》的提醒。标准写法返回空串。
    ///
    /// ⚠ 为什么要把这件事单独拎出来：**JDK 25 放宽了 main 的写法**。
    /// 本机实测（openjdk 25.0.3）下面这三种**都能编译、都能跑出结果**：
    ///     public void main(String[] args)      ← 漏了 static
    ///     static void main(String[] args)      ← 漏了 public
    ///     public static void main()            ← 没有参数
    /// 也就是说，学生漏写 static 时**不会看到任何报错** ——
    /// 而同样的代码在 JDK 21 或更早版本上会报
    ///     Error: Main method not found in class Main, please define ...
    /// 这是一条"在你这台机器上是对的、换个环境就错"的规则，
    /// 只能靠调用方主动问一句，编译器不会说。
    /// 用法：判分通过之后也调一下它，非空就提示学生。
    /// （这属于"建议"，不是"错误" —— 所以这里不改 JavaResult.Error。）
    /// </summary>
    public static string MainSignatureNote(string source)
    {
        if (StandardMain(source)) return "";

        return
            "提醒：你的 main 不是标准的写法。\n"
            + "\n"
            + "标准写法只有这一种（四个部分缺一不可）：\n"
            + "    public static void main(String[] args) {\n"
            + "    }\n"
            + "\n"
            + "    public   JVM 要从外面调用它，所以必须是公开的\n"
            + "    static   程序刚启动时还没有任何对象，JVM 只能靠\"类名.方法名\"找入口\n"
            + "    void     入口的返回值没人接收，所以固定是 void\n"
            + "    String[] args  命令行参数（这里用不到也必须写上）\n"
            + "\n"
            + "⚠ 这条提醒重要，是因为**你现在的 JDK 可能不会报错**：\n"
            + "  新版 JDK（25 起）放宽了 main 的写法，漏写 static、漏写 public、\n"
            + "  甚至写成没参数的 main() 都能跑起来。\n"
            + "  但换到 JDK 21 或更早、或者把作业交到别的环境里，就会报\n"
            + "      Error: Main method not found in class Main, please define the main method as:\n"
            + "         public static void main(String[] args)\n"
            + "\n"
            + "所以：能跑不等于对。按标准写法写，代码才到哪都能跑。";
    }

    // ══════════════════ 规则 ══════════════════

    private static List<string> Rules(string msg, string raw, string source, int lineNo, bool compileFailed)
    {
        var o = new List<string>();

        // ── ① 公共类名必须和文件名一致 ──
        // 这是 Java 新手撞的第一堵墙，也是**本课最需要解释的一条**，
        // 因为这个教学项目把文件名固定成了 Main.java（见 JavaRunner 文件头）。
        // 实测原文： Main.java:1: error: class Hello is public, should be declared
        //                              in a file named Hello.java
        if (Has(raw, "should be declared in a file named", "是公共的", "应在名为"))
        {
            var m = Regex.Match(raw, @"class\s+(\w+)\s+is\s+public");
            if (!m.Success) m = Regex.Match(raw, @"类\s+(\w+)\s+是公共的");
            string 类名 = m.Success ? m.Groups[1].Value : "这个类";

            o.Add($"你把类名写成了 {类名}，但源文件叫 Main.java —— Java 不允许这样。");
            o.Add("");
            o.Add("Java 有一条别的语言都没有的硬规矩：");
            o.Add("**一个文件里，public 类的名字必须和文件名一模一样（大小写也要一致）。**");
            o.Add("原因不神秘：Java 要能在只知道类名的情况下找到对应的文件，");
            o.Add("所以它干脆要求两者绑定在一起，省掉一切猜测。");
            o.Add("");
            o.Add("怎么改（改类名，不是改文件名）：");
            o.Add($"    public class {类名} {{        ← 现在的写法");
            o.Add("    public class Main {         ← 改成这样");
            o.Add("");
            o.Add("本课的文件名固定是 Main.java，所以你的公共类必须叫 Main。");
            o.Add("");
            o.Add("顺便记住：");
            o.Add("  · 一个 .java 文件里可以有多个类，但**最多只能有一个 public 类**");
            o.Add("  · 不写 public 的类可以叫别的名字（比如 class 工具），");
            o.Add("    但它不能被别的文件直接使用 —— 小练习里没必要这么干");
            o.Add("  · 大小写也算名字的一部分：main 和 Main 是两个不同的类");
            return o;
        }

        // ── ② 忘了 static：从静态上下文里调用了非静态方法 ──
        // 实测原文： Main.java:3: error: non-static method dbl(int) cannot be referenced
        //                              from a static context
        if (Has(msg, "cannot be referenced from a static context", "无法从静态上下文"))
        {
            var m = Regex.Match(raw, @"non-static method\s+(\w+)\s*\(");
            if (!m.Success) m = Regex.Match(raw, @"非静态\s*方法\s+(\w+)\s*\(");
            string 方法 = m.Success ? m.Groups[1].Value : "这个方法";

            o.Add($"你在 main 里直接调用了 {方法}()，但 {方法}() 不是 static 的。");
            o.Add("");
            o.Add("这是 Java 新手最常困惑的一条，值得讲清楚：");
            o.Add("  · **static 的方法属于「类」**，不用创建对象就能调用");
            o.Add("  · **不写 static 的方法属于「对象」**，必须先 new 出一个对象才能调用");
            o.Add("  · main 本身是 static 的，它里面直接写名字调用的方法，也只能是 static 的");
            o.Add("");
            o.Add("怎么改（挑一个）：");
            o.Add($"    ① 给方法加上 static —— 纯计算的函数就该这样：");
            o.Add($"           static int {方法}(int x) {{ ... }}");
            o.Add($"    ② 或者先创建对象，再用对象去调：");
            o.Add($"           Main m = new Main();");
            o.Add($"           System.out.println(m.{方法}(21));");
            o.Add("");
            o.Add("顺便记住：**main 为什么必须是 static** —— 程序刚启动的那一刻，");
            o.Add("一个对象都还不存在。JVM 没有对象可以依附，只能靠「类名.方法名」");
            o.Add("找到入口，所以入口必须属于类，而不是属于某个对象。");
            return o;
        }

        // ── ③ main 的写法不对（编译能过，启动时才失败）──
        // 实测： public void main(String[] args) / public static void main() /
        //       public static void Main(...) / public static int main(...)
        //       在 JDK 25 上的表现分两种 —— 前两种居然能跑（见 MainSignatureNote），
        //       后两种报下面这条。
        if (Has(raw, "Main method not found", "找不到 main 方法", "找不到主方法"))
        {
            o.Add("编译器认为你的代码没问题，但**运行时找不到入口方法 main**。");
            o.Add("");
            o.Add("Java 要求的入口只有这一种写法：");
            o.Add("    public static void main(String[] args) {");
            o.Add("    }");
            o.Add("四个部分一个都不能少，顺序也不能换。");
            o.Add("");

            // 按源码里实际写了什么，指出最可能错在哪 —— 比泛泛列一遍有用得多
            string 具体 = 诊断main(source);
            if (具体.Length > 0) { o.Add(具体); o.Add(""); }

            o.Add("顺便记住：main 的这个签名是 JVM **写死**的约定。");
            o.Add("「方法名 + 参数类型」合起来才算一个方法，所以 java 命令只认");
            o.Add("public static void main(String[])，参数写成 String args（少了方括号）就不认。");
            o.Add("");
            o.Add("⚠ 还有一件事必须提醒：**新版 JDK 放宽了 main 的写法** ——");
            o.Add("本机实测 JDK 25 上，漏写 static、漏写 public、甚至写成没参数的 main()");
            o.Add("都能正常跑起来。所以这个错误在有些机器上不出现，");
            o.Add("但那不代表写对了，只代表这台机器的 JDK 比较新。按标准写法写。");
            return o;
        }

        // ── ③b 类编译出来了，但运行时找不到它 ──
        // 实测：源码第一行写 `package foo;` 时，javac **成功**（class 落到了 foo/Main.class），
        // 然后 java 报：
        //     错误: 找不到或无法加载主类 Main
        //     原因: java.lang.NoClassDefFoundError: Main (wrong name: foo/Main)
        // 新手常常是从网上抄代码时把 package 那行一起抄进来的。
        if (Has(raw, "Could not find or load main class", "找不到或无法加载主类")
            || (raw.Contains("NoClassDefFoundError", StringComparison.Ordinal)
                && Has(raw, "wrong name", "名称错误")))
        {
            o.Add("编译是成功的，但运行时 Java **找不到 Main 这个类**。");
            o.Add("");
            o.Add("如果报错里出现 `wrong name: xxx/Main` 这样的字眼，那原因基本可以确定：");
            o.Add("**你在文件最上面写了 package 声明。**");
            o.Add("");
            o.Add("    package foo;          ← 这一行");
            o.Add("");
            o.Add("package 会把类放进一个\"文件夹\"里（foo/Main.class），");
            o.Add("于是运行时的名字就不再是 Main，而是 foo.Main —— 找不到 Main 是必然的。");
            o.Add("");
            o.Add("怎么改：**把 package 那一行删掉。**");
            o.Add("单文件的练习题不需要 package —— 它是给大型项目组织很多类用的，");
            o.Add("现在还不到用它的时候。（网上抄来的代码经常带着它，记得删。）");
            o.Add("");
            o.Add("顺便记住：`import` 和 `package` 是两件不同的事 ——");
            o.Add("  · import  告诉编译器\"我要用的类在哪儿\"，练习里经常需要");
            o.Add("  · package 声明\"我这个类属于哪个包\"，练习里不需要");
            return o;
        }

        // ── ④ 局部变量没初始化 ──
        //
        // 这条要专门讲"Java 和 C++ 不一样"，因为它是**一个好消息**：
        // C++ 里同样的代码是"未定义行为"，未初始化的变量里是随机值，
        // 这次碰巧是 0 就算对了（CppRunner 里为此专门做了警告拦截）；
        // Java 直接编译不过，等于把这个坑替你填了。
        // 实测原文： Main.java:4: 错误: 可能尚未初始化变量s
        if (Has(msg, "might not have been initialized", "可能尚未初始化", "尚未初始化"))
        {
            var m = Regex.Match(msg, @"variable\s+(\w+)\s+might not");
            if (!m.Success) m = Regex.Match(msg, @"变量\s*(\w+)");
            string 变量 = m.Success ? m.Groups[1].Value : "这个变量";

            o.Add($"你用了变量 {变量}，但在那之前从来没有给它赋过初值。");
            o.Add("");
            o.Add("**这是 Java 一个非常好的设计，值得你知道**：");
            o.Add("Java 的局部变量**必须先赋初值才能使用**，而且这不是警告，是编译错误 ——");
            o.Add("程序根本跑不起来，你不可能拿到一个错误的结果。");
            o.Add("");
            o.Add("对比一下 C++：同样的代码**能编译能运行**，因为未初始化的局部变量里");
            o.Add("装的是内存里的随机值。这次碰巧是 0，你的累加就\"算对了\" ——");
            o.Add("换个编译器、换个运行顺序，它会变成另一个数。");
            o.Add("那才是最难查的 bug：**结果时对时错，代码一行没改。**");
            o.Add("Java 用一条编译错误，把这类 bug 从你身上拿走了。");
            o.Add("");
            o.Add("怎么改：声明的时候就给初值。");
            o.Add($"    int {变量};          ← 现在这样，隐患");
            o.Add($"    int {变量} = 0;      ← 改成这样");
            o.Add("");
            o.Add("顺便记住：Java 里只有**局部变量**有这个要求。");
            o.Add("写在类里、方法外面的**成员变量**会被自动赋默认值（int→0，boolean→false，");
            o.Add("对象→null），不写初值也能用。错误信息里说的是 variable（局部变量），");
            o.Add("这也是为什么它挑得出毛病。");
            return o;
        }

        // ── ⑤ 窄化转换：double → int 必须显式写 cast ──
        // 实测原文： Main.java:4: error: incompatible types:
        //           possible lossy conversion from double to int
        //
        // 注意这条必须排在 ⑥「incompatible types」**前面**：
        // 它的消息里也含 "incompatible types:"，顺序反了就永远走不到这里。
        if (Has(msg, "possible lossy conversion", "可能会有损失", "可能有损失"))
        {
            var m = Regex.Match(msg, @"from\s+(\w+)\s+to\s+(\w+)");
            if (!m.Success) m = Regex.Match(msg, @"从\s*(\w+)\s*转换到\s*(\w+)");
            string 从 = m.Success ? m.Groups[1].Value : "double";
            string 到 = m.Success ? m.Groups[2].Value : "int";

            o.Add($"你想把 {从} 装进 {到}，Java 说这**可能会丢信息**，不替你转。");
            o.Add("");
            o.Add("Java 的转换规则很简单，一句话：");
            o.Add("**不会丢信息的转换它自动做，可能丢信息的必须你亲口说要做。**");
            o.Add("");
            o.Add("  小 → 大（int → double、int → long）      自动，不用管");
            o.Add("  大 → 小（double → int、long → int）      要写 (int) 明确表示你知道");
            o.Add("");
            o.Add("怎么改：加一个显式转换（cast）。");
            o.Add($"    {到} n = ({到}) d;        ← 括号里写目标类型");
            o.Add("");
            o.Add("⚠ 两个必须知道的事：");
            o.Add("  1. (int) 是**截断**，不是四舍五入：");
            o.Add("         (int) 3.7  →  3        (不是 4)");
            o.Add("         (int) -3.7 →  -3       (不是 -4)");
            o.Add("     要四舍五入得用 Math.round(d)（它返回 long，必要时再转一次）");
            o.Add("  2. 这和**整数除法**是两回事，别搞混：");
            o.Add("         double d = 7 / 2;      → 3.0  （先算整数除法得 3，再变成 3.0）");
            o.Add("         double d = 7 / 2.0;    → 3.5  （有一个是小数，结果就是小数）");
            o.Add("");
            o.Add("顺便记住：Java **不做隐式窄化**这件事，是它比 C++ 安全的地方之一。");
            o.Add("C++ 里 int n = 3.7; 会安安静静地变成 3，什么都不说。");
            return o;
        }

        // ── ⑥ cannot find symbol —— Java 报错里的头号常客 ──
        // 实测原文： Main.java:5: error: cannot find symbol
        //               System.out.println(cont);
        //                                  ^
        //             symbol:   variable cont
        //             location: class Main
        //
        // 这条信息量很大：symbol 会告诉你它是变量 / 类 / 方法，
        // 按种类分开讲比拼一份通用清单有用得多。
        if (Has(msg, "cannot find symbol", "找不到符号"))
        {
            string 种类 = "", 名字 = "", 位置 = "";
            var sm = Regex.Match(raw, @"symbol:\s+(\w+)\s+([^\s\n]+)");
            if (!sm.Success) sm = Regex.Match(raw, @"符号:\s+(\w+)\s+([^\s\n]+)");
            if (sm.Success) { 种类 = sm.Groups[1].Value; 名字 = sm.Groups[2].Value; }
            var lm = Regex.Match(raw, @"location:\s+([^\n]+)");
            if (!lm.Success) lm = Regex.Match(raw, @"位置:\s*([^\n]+)");
            if (lm.Success) 位置 = lm.Groups[1].Value.Trim();

            // 中英文的"种类"都要认 —— 实测中文是
            //   「符号: 变量 cont」/「符号: 类 Scanner」/「符号: 方法 Println(String)」
            // 英文是 variable / class / method。少认一个，中文报错就会掉进兜底分支。
            bool 是变量 = 种类 == "variable" || 种类 == "变量";
            bool 是类 = 种类 == "class" || 种类 == "类";
            bool 是方法 = 种类 == "method" || 种类 == "方法";

            o.Add(名字.Length > 0
                ? $"编译器不认识「{名字}」这个东西。"
                : "编译器在你写的位置上找不到这个名字，不知道它是什么。");
            o.Add("");
            o.Add("这是 Java 里最常见的一条报错，但它的原因就那么几种。");
            o.Add("**先看编译器告诉你的种类**，再对号入座：");
            o.Add("");

            if (是变量)
            {
                o.Add($"  symbol 说的是 variable（变量），也就是「{名字}」被当成了变量用。查三件事：");
                o.Add("");
                o.Add($"  1. **你到底声明过它没有？** Java 要求先写类型再写名字：");
                o.Add($"         int {名字} = 0;        ← 有这一行才算存在");
                o.Add($"         {名字} = 0;            ← 只有这一行 = 没声明（从 Python 过来最容易犯）");
                o.Add($"  2. **拼写和大小写一样吗？** Java **区分大小写**：");
                o.Add($"         {名字} / {首字母大写(名字)} / {全大写(名字)} 是三个不同的名字");
                o.Add("     少写一个字母、大小写反了，都是这个报错。");
                o.Add($"  3. **它是不是在花括号外面？** 在 {{ }} 里声明的变量出了这个花括号就没了：");
                o.Add("         在 if / for / while 里声明的变量，出了它们的作用域就不可见");
                o.Add("");
                o.Add("**排查思路（这条最有用）**：");
                o.Add($"     别盯着报错那一行看，反过来问自己：「{名字} 的声明写在哪一行？」");
                o.Add("     找不到那一行 —— 那就是没声明。这就是全部答案。");
                o.Add("     （在 C# 里对应的是先想清楚 type(x)，在 Java 里就是先找它的类型写在哪。）");
            }
            else if (是类)
            {
                o.Add($"  symbol 说的是 class（类），也就是「{名字}」被当成了一个类型名。两件事最可疑：");
                o.Add("");
                o.Add("  1. **忘了 import。** Java 的标准库类大多要显式导入：");
                o.Add("         import java.util.Scanner;");
                o.Add("         import java.util.ArrayList;");
                o.Add("         import java.util.List;");
                o.Add("     放在文件**最上面**（package 之后、class 之前）。");
                o.Add("     例外：java.lang 里的东西不用导入 —— String、System、Math、Integer 都在里面。");
                o.Add("");
                o.Add("  2. **大小写不对。** Java 的类名习惯首字母大写，而且大小写是名字的一部分：");
                o.Add("         String   ← 对      string   ← 没有这个类型");
                o.Add("         System   ← 对      system   ← 会被当成包名，报 package system does not exist");
                o.Add("         Scanner  ← 对      scanner  ← 没有这个类");
                o.Add("");
                o.Add("  3. 自己写的类名拼错了，或者这个类没跟你在同一个目录里。");
            }
            else if (是方法)
            {
                o.Add($"  symbol 说的是 method（方法），也就是你调用了一个不存在的「{名字}」。查三件事：");
                o.Add("");
                o.Add("  1. **方法名拼错或大小写不对。** 最常见的就是这个：");
                o.Add("         System.out.Println(\"hi\");   ← 错，println 是小写开头");
                o.Add("         System.out.println(\"hi\");   ← 对");
                o.Add("     Java 的命名习惯：变量和方法首字母小写，类首字母大写。");
                o.Add("  2. **参数对不上。** 方法名对，但参数个数或类型不一样，也算「找不到方法」：");
                o.Add("         Math.pow(2)         ← 少了一个参数（要 pow(2, 3)）");
                o.Add("  3. **这个方法是你自己写的吗？** 自己写的方法要在同一个类里（或者用对象调用），");
                o.Add("     而且要注意 static —— 忘了 static 会报另一条专门的错。");
            }
            else
            {
                o.Add("  按这个顺序查：");
                o.Add("");
                o.Add("  1. **这个名字声明过吗？** Java 里每个变量都要先写类型：int x = 0;");
                o.Add("  2. **拼写、大小写一样吗？** Java 区分大小写，Name 和 name 是两个东西");
                o.Add("  3. **是标准库的东西吗？** 是的话检查 import；");
                o.Add("     （java.lang 里的 String / System / Math 不用 import）");
                o.Add("  4. **作用域对吗？** 在花括号里声明的变量，出了花括号就没了");
            }

            if (位置.Length > 0)
            {
                o.Add("");
                // 只写值不写 "location:" 前缀 —— 中文报错里那一行本来就叫「位置: 类 Main」，
                // 混着英文标签反而更乱。值本身就是"在哪个范围里找的"。
                o.Add($"  编译器还告诉了你它是在哪儿找的：{位置}");
                o.Add("  这一行说的是「我在这个范围内没找到它」——");
                o.Add("  如果那里出现了 PrintStream / String 这种类型名，");
                o.Add("  那说明名字你写了个大概，错的只是**大小写**。");
            }

            o.Add("");
            o.Add("顺便记住：**cannot find symbol 是「名字」的问题，不是「类型」的问题。**");
            o.Add("编译器想说的是「我不认识这个名字」，而不是「你类型用错了」。");
            o.Add("所以第一反应永远是：这个名字，我在哪儿声明过它？");
            return o;
        }

        // ── ⑦ 类型不兼容（不含窄化，那条上面已经拦掉了）──
        // 实测原文（英文）： incompatible types: String cannot be converted to int
        //                  incompatible types: void cannot be converted to int
        // 实测原文（中文）： 不兼容的类型: String无法转换为int
        //                 不兼容的类型: int无法转换为boolean
        if (Has(msg, "incompatible types", "不兼容的类型"))
        {
            var m = Regex.Match(msg, @"(\w+)\s+cannot be converted to\s+(\w+)");
            if (!m.Success) m = Regex.Match(msg, @"从\s*(\w+)\s*转换到\s*(\w+)");
            // ⚠ 中文有两种句式：「从double转换到int可能会有损失」和「int无法转换为boolean」。
            //   少认后面这种，`if (x = 5)` 那条最常见的报错就会掉进兜底分支、说不出所以然。
            if (!m.Success) m = Regex.Match(msg, @"(\w+)\s*无法转换为\s*(\w+)");
            string 从 = m.Success ? m.Groups[1].Value : "";
            string 到 = m.Success ? m.Groups[2].Value : "";

            if (从.Length > 0 && 到.Length > 0)
                o.Add($"类型对不上：{从} 不能直接变成 {到}。");
            else
                o.Add("类型对不上：右边的东西不是左边要的类型。");
            o.Add("");
            o.Add("Java 是强类型语言，而且它**只替你做不会丢信息的转换**：");
            o.Add("  int → double、int → long     自动");
            o.Add("  别的方向、以及文字↔数字之间   必须你自己写代码转");
            o.Add("");
            o.Add("怎么改 —— 转换速查（这几个背下来很有用）：");
            o.Add("    文字 → 整数      int n = Integer.parseInt(s);");
            o.Add("    文字 → 小数      double d = Double.parseDouble(s);");
            o.Add("    整数 → 文字      String s = String.valueOf(n);");
            o.Add("                     或者  String s = \"\" + n;        ← 最省事的写法");
            o.Add("    小数 → 整数      int n = (int) d;                 ← 会截断，不是四舍五入");
            o.Add("");
            if (从 == "void")
            {
                o.Add("  这一次是 **void**：你调用了一个没有返回值的方法，却想把它的结果装进变量。");
                o.Add("  void 的意思是「什么都不返回」，所以没有任何东西可以赋给左边。");
                o.Add("  要么把方法改成有返回值的（把 void 换成 int），要么别接它的返回值。");
            }
            else if (到 == "boolean")
            {
                // 实测： if (x = 5) → 不兼容的类型: int无法转换为boolean
                o.Add("  这一次是 **boolean**：if / while 的条件只接受 true 或 false。");
                o.Add("");
                o.Add("  最常见的写法错误是**把 == 写成了 =**：");
                o.Add("         if (x = 5)       ← 错：这是\"把 5 赋给 x\"，赋值的结果是 int，不是 boolean");
                o.Add("         if (x == 5)      ← 对：这才是比较");
                o.Add("");
                o.Add("  顺带说：Java 在这里比 C++ 严。C++ 里 if (x = 5) 只是警告"
                     + "（甚至不警告），");
                o.Add("  在 Java 里它直接不让你编译 —— 这条规则替很多人挡掉了一个真实的 bug。");
            }
            else if (从 == "String" || 从 == "string")
            {
                o.Add("  这一次左边是数字、右边是文字。数字和文字在 Java 里是**完全不同**的东西，");
                o.Add("  不会自动互相变。");
                o.Add("");
                o.Add("  ⚠ 要转文字要注意：`Integer.parseInt(\"abc\")` **能编译通过**，");
                o.Add("    但一运行就抛 NumberFormatException —— 编译器不知道字符串里装的是什么，");
                o.Add("    只有运行时才知道。所以转换之前最好确认内容真的是数字。");
            }
            o.Add("");
            o.Add("顺便记住：Java 里 `\"\" + x` 是一个万能招 ——");
            o.Add("`+` 只要有一边是字符串，另一边就自动被转成字符串：");
            o.Add("    System.out.println(\"总数=\" + 42);     // 输出 总数=42");
            return o;
        }

        // ── ⑧ 全角（中文）标点 ──
        // 实测： int x = 5；  →  Main.java:3: error: illegal character: '\uff1b'
        // 这条是中文学生特有的坑，而且 javac 只给一个 Unicode 码点，
        // 不查表根本看不出是哪个字符 —— 这里就替他把表查了。
        if (Has(msg, "illegal character", "非法字符"))
        {
            o.Add("代码里出现了一个编译器不认识的字符。");
            o.Add("");
            string 具体 = IllegalCharDetail(raw);
            if (具体.Length > 0) { o.Add("  " + 具体); o.Add(""); }
            o.Add("**这是中文输入法最容易造成的坑**：中文标点和英文标点长得几乎一样，");
            o.Add("但编译器只认英文的那一套。");
            o.Add("");
            o.Add("    ；　，　。　（　）　\"　\"　：　？　！　、　　← 全角，全都不认");
            o.Add("    ;   ,   .   (   )   \"   \"   :   ?   !   ,     ← 半角，只认这些");
            o.Add("");
            o.Add("怎么改：把输入法切回**英文**（通常是 Shift 或 Ctrl+空格），");
            o.Add("把这一行连同上一行的标点重新打一遍。光看是看不出来的，重打最省事。");
            o.Add("");
            o.Add("顺便记住两件事，免得矫枉过正：");
            o.Add("  · **字符串里面的中文完全没问题**：System.out.println(\"你好\"); 是合法代码");
            o.Add("  · **变量名用中文也是允许的**（实测 int 总数 = 42; 能编译能运行）——");
            o.Add("    Java 的标识符支持 Unicode。出问题的只有**标点**，不是汉字。");
            o.Add("    不过课堂上还是建议用英文名，因为报错信息和别人的代码都是英文的。");
            return o;
        }

        // ── ⑨ 漏分号 ──
        // 实测原文： Main.java:3: error: ';' expected
        if (Has(msg, "';' expected", "需要';'", "expected ';'"))
        {
            o.Add("这里少了一个分号。");
            o.Add("");
            o.Add("Java 和 C# 一样：**每条语句的末尾都要有分号**（这一点和 Python 不同）。");
            o.Add("");
            o.Add("两种可能，都要看一眼：");
            o.Add("  · 就是**这一行**末尾漏了 —— 看上面「出在这里」那一行");
            o.Add("  · 或者**上一行**漏了，javac 把两行连起来读，报到了下一行");
            o.Add("    （这一点和 C++ 一样：报错行有时候要往上多看一行）");
            o.Add("");
            o.Add("怎么改：把这一行和上一行的末尾都检查一遍，缺的补上。");
            o.Add("");
            o.Add("顺便记住：有一个「分号位置对、意思全错」的坑 ——");
            o.Add("    if (x > 0);");
            o.Add("        System.out.println(\"正数\");");
            o.Add("那个分号是**合法**的（它是一条空语句，意思是\"什么都不做\"），");
            o.Add("于是 if 管住的只有那个空语句，下一行无论如何都会执行。");
            o.Add("这种错误 Java 一声不吭。**if / for / while 的括号后面不要写分号。**");
            return o;
        }

        // ── ⑩ 括号没配平 ──
        // 实测原文： Main.java:4: error: reached end of file while parsing
        if (Has(msg, "reached end of file while parsing", "到达文件结尾", "分析时已到达文件结尾"))
        {
            o.Add("javac 一路读到了文件末尾，还没把这个类读完 —— 也就是**少了一个 }**。");
            o.Add("");
            o.Add("Java 用花括号把代码分层，每开一个 { 就必须有一个 } 收尾：");
            o.Add("    public class Main {              ← 第 1 层开");
            o.Add("        public static void main(...) {   ← 第 2 层开");
            o.Add("        }                            ← 第 2 层关");
            o.Add("    }                                ← 第 1 层关");
            o.Add("");
            o.Add("怎么找：从**最后一行往上**数，看哪个 } 少了；");
            o.Add("或者把整段代码重新缩进对齐一遍（IDE 里是 Ctrl+Shift+F）—— ");
            o.Add("缩进一乱，缺的那个括号就自己冒出来了。");
            o.Add("");
            o.Add("顺便记住：javac 说「读到文件结尾还没读完」，意思是括号**少了**，不是多了。");
            return o;
        }

        // ── ⑪ 字符串引号没闭合 ──
        // 实测原文： Main.java:3: error: unclosed string literal
        if (Has(msg, "unclosed string literal", "未结束的字符串", "未闭合的字符串"))
        {
            o.Add("一个字符串的引号没有闭合。");
            o.Add("");
            o.Add("Java 的字符串必须**成对**的英文双引号：");
            o.Add("    System.out.println(\"你好\");      ← 对");
            o.Add("    System.out.println(\"你好);       ← 错，少了结尾的引号");
            o.Add("");
            o.Add("字符串里的内容要换行，得写 \\n（不能直接敲回车）：");
            o.Add("    System.out.println(\"第一行\\n第二行\");");
            o.Add("");
            o.Add("顺便记住：中文引号 “ ” 不是 Java 的引号。");
            o.Add("用中文引号有时候报 illegal character，有时候报这一条 —— 都是同一个原因。");
            return o;
        }

        // ── ⑫ 少了 return ──
        // 实测原文： Main.java:8: error: missing return statement
        if (Has(msg, "missing return statement", "缺少返回语句"))
        {
            o.Add("这个方法声明了「要返回一个值」，但有的分支上没有 return。");
            o.Add("");
            o.Add("比如：");
            o.Add("    static int 判断(int x) {");
            o.Add("        if (x > 0) return 1;");
            o.Add("        // ← x <= 0 的时候，这里没有 return");
            o.Add("    }");
            o.Add("");
            o.Add("Java 比 C++ 严格：它要求**所有可能的路径**都有返回值。");
            o.Add("哪怕你觉得「这个分支不可能走到」，它也不接受 —— 这是好事，");
            o.Add("因为它把「忘了处理某种情况」提前变成了编译错误。");
            o.Add("");
            o.Add("怎么改（挑一个）：");
            o.Add("  · 末尾补一个兜底的 return：   return 0;");
            o.Add("  · 或者把 if 写成 if / else，让两条路都有返回值");
            o.Add("");
            o.Add("顺便记住：void 方法没有返回值，不写 return 也行");
            o.Add("（想提前结束可以写 return; —— 光一个 return，不带值）。");
            return o;
        }

        // ── ⑬ 包名 / 类名大小写 ──
        // 实测原文： Main.java:3: error: package system does not exist
        if (Has(msg, "package", "程序包") && Has(msg, "does not exist", "不存在"))
        {
            var m = Regex.Match(msg, @"package\s+([\w.]+)\s+does not exist");
            if (!m.Success) m = Regex.Match(msg, @"程序包\s*([\w.]+)\s*不存在");
            string 包 = m.Success ? m.Groups[1].Value : "";

            if (包 == "system" || 包 == "math" || 包 == "string")
            {
                o.Add($"你写的是 {包}，但 Java 的标准类是 **{char.ToUpperInvariant(包[0]) + 包.Substring(1)}** —— 首字母要大写。");
                o.Add("");
                o.Add("这是纯粹的**大小写**问题：");
                o.Add("    system.out.println(...)   ← 错，Java 以为 system 是个包名");
                o.Add("    System.out.println(...)   ← 对");
                o.Add("");
                o.Add("顺便记住：Java 的命名习惯是");
                o.Add("  · 类名    首字母大写    System、String、Scanner、Math");
                o.Add("  · 变量/方法 首字母小写  out、println、length");
                o.Add("  · 包名    全小写        java.util");
                o.Add("看到报错里出现「包」这个字，先想想是不是把类名写成小写的了。");
            }
            else
            {
                o.Add(包.Length > 0 ? $"编译器找不到名为 {包} 的包。" : "编译器找不到你引用的那个包。");
                o.Add("");
                o.Add("如果你写了 import，检查拼写：");
                o.Add("    import java.util.Scanner;      ← 对");
                o.Add("    import java.util.scanner;      ← 错，类名要大写");
                o.Add("    import util.Scanner;           ← 错，少了 java.");
                o.Add("");
                o.Add("顺便记住：只有 java.lang 里的类（String、System、Math、Integer）不用 import，");
                o.Add("别的都要写清楚它在哪个包里。");
            }
            return o;
        }

        // ── ⑭ 语句本身不成立 ──
        // 实测： int = 5;  →  error: not a statement  +  error: ';' expected
        //       if (x = 5) →  incompatible types: int cannot be converted to boolean
        if (Has(msg, "not a statement", "不是语句", "<identifier> expected", "需要<标识符>",
                "unexpected type", "意外的类型"))
        {
            o.Add("这一行本身不成立 —— Java 读懂了每个字，但拼不出一句合法的话。");
            o.Add("");
            o.Add("常见的三种：");
            o.Add("");
            o.Add("  1. **声明变量时忘了写变量名**");
            o.Add("         int = 5;         ← 错：int 是类型，不是名字");
            o.Add("         int x = 5;       ← 对");
            o.Add("");
            o.Add("  2. **把赋值 = 写成了比较 ==**（或者反过来）");
            o.Add("         if (x = 5)       ← 错：Java 的 if 只接受 boolean");
            o.Add("         if (x == 5)      ← 对");
            o.Add("     Java 在这里比 C++ 严：C++ 里 if (x = 5) 只是警告，");
            o.Add("     在 Java 里它直接不让你编译 —— 因为 int 不是 boolean。");
            o.Add("");
            o.Add("  3. **上一行漏了分号**，两行粘在一起变成一句读不通的话");
            o.Add("");
            o.Add("顺便记住：Java 的 if / while 条件**必须是 boolean**。");
            o.Add("C++ 里 if (1) 是合法的（非 0 即真），Java 里会报类型不兼容。");
            return o;
        }

        // ══════════════════ 运行时异常 ══════════════════
        // 下面这些都要求 !compileFailed —— 编译错误的文字里偶尔也会出现
        // 这些单词（比如学生把 \"NullPointerException\" 写在字符串里），
        // 用 compileFailed 把它们和真正的运行时异常区分开。

        // ── ⑮ 数组下标越界 ──
        if (!compileFailed && raw.Contains("ArrayIndexOutOfBoundsException", StringComparison.Ordinal))
        {
            var m = Regex.Match(raw, @"Index\s+(\d+)\s+out of bounds for length\s+(\d+)");
            o.Add("数组下标越界了 —— 你访问了数组里不存在的位置。");
            o.Add("");
            if (m.Success)
            {
                o.Add($"  报错里的两个数是白送的线索：");
                o.Add($"      Index {m.Groups[1].Value}   ← 你访问的下标");
                o.Add($"      length {m.Groups[2].Value}  ← 数组真实的长度");
                o.Add($"  合法下标只能是 0 到 {int.Parse(m.Groups[2].Value) - 1}（长度减一）。");
                o.Add("");
                if (m.Groups[2].Value == "0" && source.Contains("args["))
                {
                    o.Add("  length 是 0，而且你的代码里有 args[...] —— 这是**命令行参数**的问题：");
                    o.Add("  这个程序在等外面传参数进来，但运行时没有参数可传，所以 args 是空的。");
                    o.Add("  练习里用不到命令行参数，直接把值写在代码里更省事：");
                    o.Add("      int n = 5;            ← 代替 args[0]");
                    o.Add("  如果确实要练 args，就得在运行时写成 `java Main 5`。");
                    o.Add("");
                }
            }
            o.Add("**Java 会检查下标，这是好事**：它当场拦住你并说清楚越了多少，");
            o.Add("而不是像 C++ 那样悄悄去读别的内存、最后给你一个莫名其妙的值。");
            o.Add("（C++ 里 int a[3]; 读 a[10] 不报错也不崩，只是结果错了 —— 那才难查。）");
            o.Add("");
            o.Add("怎么改：检查所有数组访问，下标必须满足 0 <= i && i < a.length。");
            o.Add("循环写成 for (int i = 0; i < a.length; i++)，注意是 **< 不是 <=**。");
            o.Add("（`<= a.length` 是最经典的一个 off-by-one：多跑一次，正好越界。）");
            o.Add("");
            o.Add("顺便记住：数组的长度是 `a.length`（**属性，不加括号**）；");
            o.Add("字符串的长度是 `s.length()`（**方法，要加括号**）。");
            o.Add("这两个写混了会报 cannot find symbol，那是编译错误。");
            return o;
        }

        // ── ⑯ NullPointerException ──
        if (!compileFailed && raw.Contains("NullPointerException", StringComparison.Ordinal))
        {
            o.Add("你对着一个 **null**（空引用）做了操作 —— 调方法、取字段、取下标。");
            o.Add("");
            o.Add("null 的意思不是\"空字符串\"也不是\"0\"，而是**\"这个变量没有指向任何对象\"**。");
            o.Add("对它做任何\"对象才有的操作\"，都会立刻抛这个异常。");
            o.Add("");
            o.Add("这就是 Java 的头号错误 —— 和 C# 的 NullReferenceException 是同一个东西，");
            o.Add("只是名字不同。见到它，任务只有一个：**找出哪个变量是 null**。");
            o.Add("");

            // 我们编译时加了 -g，JVM 才能把变量名写进提示里（实测差别：
            // 不加 -g 显示 "<local1>"，加了显示 "s"）—— 这条线索值得单独点出来
            var m = Regex.Match(raw, "because\\s+\"([^\"]+)\"\\s+is null");
            if (m.Success && !m.Groups[1].Value.StartsWith("<", StringComparison.Ordinal))
            {
                o.Add($"  **报错里已经告诉你了：就是变量 {m.Groups[1].Value}。**");
                o.Add($"  （这是新版 JDK 的帮助信息，能直接点出变量名 —— 好好利用它。）");
            }
            else
            {
                o.Add("  报错里如果出现 \"<local1>\" 这种写法，说明它想告诉你变量名但拿不到；");
                o.Add("  那就自己找：这一步用到了哪个对象？它有没有被赋过值？");
            }
            o.Add("");
            o.Add("三种最常见的原因：");
            o.Add("  1. **声明了但没赋值**：");
            o.Add("         String s;              // s 现在是 null");
            o.Add("         s.length();            // ← 这里抛");
            o.Add("     注意：**类里的成员变量不写初值就是 null**（不像局部变量会编译报错），");
            o.Add("     所以这个坑经常出现在成员变量上。");
            o.Add("  2. **方法返回了 null，你没检查就用了**");
            o.Add("         String s = 找名字(id);   // 找不到时返回 null");
            o.Add("         s.length();             // ← 抛");
            o.Add("  3. **对象数组的元素还没创建**");
            o.Add("         String[] a = new String[3];   // 创建了数组，但元素都是 null");
            o.Add("         a[0].length();                // ← 抛：a[0] 还是 null");
            o.Add("         a[0] = \"hi\";                  // 得先给它一个对象");
            o.Add("");
            o.Add("怎么改：用之前先判一下，或者从源头保证它被赋过值。");
            o.Add("    if (s != null) { System.out.println(s.length()); }");
            o.Add("");
            o.Add("顺便记住：Java 里**编译期**管不了 null（局部变量漏赋值它管，");
            o.Add("但\"这个方法会不会返回 null\"它不知道），所以 NPE 只能在运行时才暴露。");
            o.Add("这也是为什么写 Java 要养成习惯：拿到一个对象，先想它有没有可能是 null。");
            return o;
        }

        // ── ⑰ NumberFormatException ──
        if (!compileFailed && raw.Contains("NumberFormatException", StringComparison.Ordinal))
        {
            var m = Regex.Match(raw, "For input string:\\s*\"([^\"]*)\"");
            o.Add("你在把**文字转成数字**，但那段文字不是合法的数字。");
            o.Add("");
            if (m.Success) o.Add($"  问题就出在这段内容上：「{m.Groups[1].Value}」");
            o.Add("");
            o.Add("    Integer.parseInt(\"12\")    → 12      可以");
            o.Add("    Integer.parseInt(\"abc\")   → 抛这个异常");
            o.Add("    Integer.parseInt(\"3.5\")   → 也抛（要小数用 Double.parseDouble）");
            o.Add("    Integer.parseInt(\" 12 \")  → 也抛（前后有空格也不行）");
            o.Add("");
            o.Add("⚠ 为什么它**编译能过**：编译器不知道那个字符串里装的是什么，");
            o.Add("  只有运行时才知道。所以这是运行时异常，不是编译错误。");
            o.Add("");
            o.Add("怎么改（挑一个）：");
            o.Add("  1. **先确认来源**：如果这个字符串是从输入读来的，");
            o.Add("     那就要假设用户会输入任何东西 —— 包括你没想到的。");
            o.Add("  2. **用 try / catch 兜住**（这是 Java 处理这类问题的标准做法）：");
            o.Add("         try {");
            o.Add("             int n = Integer.parseInt(s);");
            o.Add("             System.out.println(n);");
            o.Add("         } catch (NumberFormatException e) {");
            o.Add("             System.out.println(\"这不是数字：\" + s);");
            o.Add("         }");
            o.Add("  3. **或者先校验再转**：");
            o.Add("         if (s.matches(\"-?\\\\d+\")) { int n = Integer.parseInt(s); }");
            o.Add("");
            o.Add("顺便记住：`catch` 不是\"掩盖错误\"，是\"我来处理这种意外\"。");
            o.Add("真正该避免的是 catch 块里什么都不做 —— 那才是把问题藏起来。");
            return o;
        }

        // ── ⑱ 字符串下标越界 ──
        if (!compileFailed && raw.Contains("StringIndexOutOfBoundsException", StringComparison.Ordinal))
        {
            var m = Regex.Match(raw, @"Index\s+(\d+)\s+out of bounds for length\s+(\d+)");
            o.Add("你取的字符串位置超出了字符串的长度。");
            o.Add("");
            if (m.Success)
            {
                o.Add($"      Index {m.Groups[1].Value}   ← 你取的位置");
                o.Add($"      length {m.Groups[2].Value}  ← 字符串真实长度");
                o.Add($"  合法位置是 0 到 {int.Parse(m.Groups[2].Value) - 1}。");
                o.Add("");
            }
            o.Add("**这里最容易错的是差一个**：长度是 2 的字符串，最后一位是 1 不是 2 ——");
            o.Add("因为下标从 0 开始数：");
            o.Add("    String s = \"hi\";");
            o.Add("    s.charAt(0) → 'h'     s.charAt(1) → 'i'");
            o.Add("    s.charAt(2) → 抛异常   s.length() → 2");
            o.Add("");
            o.Add("取最后一个字符要写：");
            o.Add("    s.charAt(s.length() - 1)");
            o.Add("");
            o.Add("顺便记住：字符串和数组一样，下标从 0 开始。");
            o.Add("循环遍历字符串写成 for (int i = 0; i < s.length(); i++)，同样是 **< 不是 <=**。");
            return o;
        }

        // ── ⑲ 除以 0 ──
        if (!compileFailed && raw.Contains("ArithmeticException", StringComparison.Ordinal)
            && Has(raw, "/ by zero", "除以零"))
        {
            o.Add("整数除以 0 了。");
            o.Add("");
            o.Add("怎么改：除之前判断一下除数是不是 0。");
            o.Add("    if (b != 0) {");
            o.Add("        System.out.println(a / b);");
            o.Add("    } else {");
            o.Add("        System.out.println(\"除数不能是 0\");");
            o.Add("    }");
            o.Add("");
            o.Add("找找看：除数是不是某个变量，而它在这条路径上刚好是 0？");
            o.Add("（取模 % 也一样，`a % 0` 同样抛这个异常。）");
            o.Add("");
            o.Add("顺便记住三件容易混的事：");
            o.Add("  · Java 里整数除以 0 **抛异常**，有明确提示（C++ 是直接崩，什么都不说）");
            o.Add("  · 但**浮点数**除以 0 不抛，会得到 Infinity：");
            o.Add("        System.out.println(1.0 / 0);   → Infinity");
            o.Add("        System.out.println(1 / 0);     → ArithmeticException");
            o.Add("  · 所以 1/0 和 1.0/0 在这里是两个完全不同的结果");
            return o;
        }

        // ── ⑳ String.format 的参数对不上 ──
        if (!compileFailed && raw.Contains("MissingFormatArgumentException", StringComparison.Ordinal))
        {
            var m = Regex.Match(raw, "Format specifier '([^']*)'");
            o.Add("`String.format`（或 printf）里，格式串要的参数比实际给的多。");
            o.Add("");
            if (m.Success) o.Add($"  报错说的是这个占位符没东西可填：{m.Groups[1].Value}");
            o.Add("");
            o.Add("    String.format(\"%d %d\", 1)      ← 两个 %d，只给了一个值 → 抛");
            o.Add("    String.format(\"%d %d\", 1, 2)   ← 对");
            o.Add("");
            o.Add("怎么改：数一数格式串里有几个 %，后面就给几个参数，**顺序和类型都要对上**。");
            o.Add("");
            o.Add("顺便记住：**拼接字符串根本不需要格式串。**");
            o.Add("Java 里最省事、最不容易出错的写法是 `+`：");
            o.Add("    System.out.println(\"总数=\" + 总数);");
            o.Add("`+` 只要有一边是字符串，另一边就自动转成字符串 ——");
            o.Add("没有占位符，也就没有\"个数对不上\"这个问题。");
            o.Add("（要把任意值变成字符串，也可以写 String.valueOf(x)。）");
            return o;
        }

        // ── ㉑ 想读输入，但没有输入 ──
        if (!compileFailed && raw.Contains("NoSuchElementException", StringComparison.Ordinal))
        {
            o.Add("程序想读取输入，但**这里没有输入可以读**。");
            o.Add("");
            o.Add("这个练习环境不会有人在旁边敲键盘 —— 输入流一开始就是结束状态，");
            o.Add("所以 Scanner 一读就抛出这个异常（不是卡住不动）。");
            o.Add("");
            o.Add("怎么改：");
            o.Add("  · 练习阶段**不要用 Scanner**，直接把值写在代码里：");
            o.Add("        int n = 5;              // 代替 sc.nextInt()");
            o.Add("    逻辑先写对、跑通，再去考虑输入的事。");
            o.Add("  · 或者给它一个默认值：");
            o.Add("        int n = sc.hasNextInt() ? sc.nextInt() : 0;");
            o.Add("");
            o.Add("顺便记住：Scanner 要 `import java.util.Scanner;`。");
            o.Add("忘了 import 报的是 cannot find symbol（编译错误），和这条不是一回事 ——");
            o.Add("一个是\"没有这个类\"，一个是\"读不到东西\"。");
            return o;
        }

        // ── ㉒ 递归没有出口 ──
        if (!compileFailed && raw.Contains("StackOverflowError", StringComparison.Ordinal))
        {
            o.Add("递归没有尽头，调用栈满了。");
            o.Add("");
            o.Add("每调用一次自己，就多占一层栈；调用太多次（默认几千层），栈就用完了。");
            o.Add("");
            o.Add("两种常见原因：");
            o.Add("  1. **忘了写终止条件** —— 递归函数必须有\"什么时候不再调用自己\"的分支");
            o.Add("  2. **终止条件写错了**，永远到不了。比如：");
            o.Add("         static int 阶乘(int n) {");
            o.Add("             return n * 阶乘(n - 1);      ← 没有 n <= 1 的出口");
            o.Add("         }");
            o.Add("     正确的开头应该是： if (n <= 1) return 1;");
            o.Add("");
            o.Add("怎么查：确认每一次递归调用都**在靠近**出口 ——");
            o.Add("n 在变小？下标在靠近终点？没有原地打转？");
            o.Add("");
            o.Add("顺便记住：StackOverflow 里的 Stack 指的是\"调用栈\"（记录函数调用关系的地方），");
            o.Add("和数据结构里的那个\"栈\"只是名字像。");
            return o;
        }

        // ── ㉓ 编译用的 JDK 比运行用的新 ──
        // 实测： JDK 25 的 javac 编译 + JDK 21 的 java 运行 →
        //   UnsupportedClassVersionError: ... (class file version 69.0),
        //   this version of the Java Runtime only recognizes class file versions up to 65.0
        if (!compileFailed && Has(raw, "UnsupportedClassVersionError", "class file version"))
        {
            o.Add("编译用的 JDK 比运行用的 JDK **新**，运行时认不出这个 class 文件。");
            o.Add("");
            o.Add("每个 class 文件里都写着一个版本号，运行的 JVM 只认不高于自己的版本。");
            o.Add("所以：用新版 JDK 编译、用旧版 JDK 运行，**编译这一步完全成功**，");
            o.Add("失败发生在运行 —— 而且报错里完全看不出跟\"两个 JDK\"有关。");
            o.Add("");
            o.Add("怎么改：让 javac 和 java 来自**同一套 JDK**。");
            o.Add("  · 检查 `javac -version` 和 `java -version` 是不是同一个版本号");
            o.Add("  · 不一样的话，把 PATH 里两个都指向同一个 JDK 的 bin 目录");
            o.Add("  · 或者临时给 javac 加 --release 参数，让它按旧版本生成 class");
            o.Add("");
            o.Add("顺便记住：**这不是你代码的问题，是环境的问题。**");
            o.Add("遇到\"编译能过、运行报环境错\"，第一个想到的就是两套工具链版本不一致。");
            return o;
        }

        // ── ㉔ 其它运行时异常：认不出来就承认，但给出能用的排查法 ──
        if (!compileFailed && raw.Contains("Exception in thread", StringComparison.Ordinal))
        {
            var m = Regex.Match(raw, "Exception in thread \"[^\"]*\"\\s+([\\w.$]+)");
            string 类型 = m.Success ? m.Groups[1].Value : "";
            o.Add(类型.Length > 0
                ? $"这是一个运行时异常（{类型}），我没有内置它的专门解释。"
                : "这是一个运行时异常，我没有内置它的专门解释。");
            o.Add("");
            o.Add("但堆栈本身给的信息足够你自己查：");
            o.Add("  1. **上面已经指出了行号** —— 那是堆栈里第一处属于 Main.java 的位置，");
            o.Add("     也就是你的代码里出事的地方（堆栈前面几行大多是 JDK 内部的代码，不是你写的）");
            o.Add("  2. **异常的名字就说明了类型** —— 以 Exception 或 Error 结尾的那个词");
            o.Add("     通常直接对应原因：NullPointer / NumberFormat / IndexOutOfBounds …");
            o.Add("  3. **先看第一条异常** —— 后面跟着的 Caused by 往往是更根本的原因");
            o.Add("");
            o.Add("把异常的名字复制去搜索，比搜一整段堆栈有效得多。");
            return o;
        }

        // ── ㉕ 程序跑完了、没报错，但结果可能不对 ──
        //
        // 这一段是 Java 版**最该有**的东西：下面这几处 Java 一声不吭，
        // 程序正常退出、退出码 0，只是答案是错的。
        // 会报错的坑学生自己会查；不会报错的坑，只能靠这里点出来。
        // 措辞上只说"按可能性排"，不做断言 —— 调用方是因为"结果和预期不符"
        // 才走到这里的，我并不知道它期望什么。
        if (!compileFailed)
        {
            o.Add("程序从头跑到尾了，没有报错 —— 所以问题多半出在**不会报错的那些地方**。");
            o.Add("");
            o.Add("Java 有几处「不报错但会算错」的坑，按常见程度排：");
            o.Add("");
            o.Add("  1. **整数除法**（最常见）");
            o.Add("     两个 int 相除，结果还是 int，**小数部分直接丢掉**（不是四舍五入）：");
            o.Add("         7 / 2        → 3        （不是 3.5）");
            o.Add("         1 / 3        → 0        （不是 0.33，直接是 0）");
            o.Add("     要小数就得让其中一个变成小数：");
            o.Add("         7 / 2.0      → 3.5");
            o.Add("         (double) a / b → 3.5");
            o.Add("     ⚠ 注意结果类型由**运算的两个数**决定，不是由左边的变量决定：");
            o.Add("         double d = 7 / 2;      → 3.0（先算整数除法得 3，再变成 3.0）");
            o.Add("");
            o.Add("  2. **用 == 比较字符串**");
            o.Add("     `==` 比的是\"是不是同一个对象\"，不是\"内容一不一样\"。");
            o.Add("     要内容相等，用 `a.equals(b)`。");
            o.Add("     ⚠ 它**有时候是对的**，这才是最坑的地方：");
            o.Add("         实测 \"hi\" == \"h\" + \"i\" 恰好是 true（编译器把它折成了同一个常量）；");
            o.Add("         同样两个内容相同的字符串，只要有一个来自输入或拼接，就会变成 false。");
            o.Add("         别赌运气 —— 比内容一律用 equals。");
            o.Add("");
            o.Add("  3. **`(int)` 是截断，不是四舍五入**");
            o.Add("         (int) 3.7  → 3        (int) -3.7 → -3");
            o.Add("     要四舍五入用 Math.round(x)。");
            o.Add("");
            o.Add("  4. **循环多跑一次或少跑一次**");
            o.Add("         下标从 0 开始，所以是 i < n，不是 i <= n");
            o.Add("         求和时累加变量忘了从 0 开始，或者把第一项漏了/多算了");
            o.Add("");
            o.Add("  5. **`i++` 和 `++i` 混在表达式里**");
            o.Add("     单独一行写 `i++;` 永远是对的；写进别的表达式里就容易差一。");
            o.Add("");
            o.Add("  6. **除法取模对负数的处理**");
            o.Add("         -7 / 2 → -3（向 0 取整，不是 -4）      -7 % 2 → -1");
            o.Add("");
            o.Add("怎么查：把中间值打印出来，看**第一次和预期不一样**的那一步。");
            o.Add("    System.out.println(\"调试：a=\" + a + \" b=\" + b);");
            o.Add("这比盯着代码看快得多 —— 变量的值和你以为的不一样，问题就在那里。");
            return o;
        }

        return o;
    }

    // ══════════════════ 小工具 ══════════════════

    /// <summary>
    /// 从 "illegal character: '\uff1b'" 里把那个字符还原出来，并说出它应该是什么。
    ///
    /// 为什么值得专门做：javac 只给一个 Unicode 码点（\uff1b），
    /// 人脑没法把它翻译成"这是个中文分号"，学生只会一头雾水。
    /// 这里替他把表查了 —— 这是中文学生特有的坑，也是最容易解决的一个。
    /// </summary>
    private static string IllegalCharDetail(string raw)
    {
        var m = Regex.Match(raw, @"illegal character:\s*'(\\u[0-9a-fA-F]{4}|[^'])'");
        if (!m.Success) m = Regex.Match(raw, @"非法字符:\s*'(\\u[0-9a-fA-F]{4}|[^'])'");
        if (!m.Success) return "";

        string tok = m.Groups[1].Value;
        if (tok.StartsWith("\\u", StringComparison.Ordinal) && tok.Length == 6
            && int.TryParse(tok.Substring(2), System.Globalization.NumberStyles.HexNumber,
                            System.Globalization.CultureInfo.InvariantCulture, out int code))
        {
            char ch = (char)code;
            return $"javac 说的那个字符是「{ch}」（U+{code:X4}）—— {全角名称(ch)}";
        }
        return $"javac 说的那个字符是「{tok}」";
    }

    /// <summary>把一个全角标点翻译成"它应该是哪个英文符号"。</summary>
    private static string 全角名称(char c) => c switch
    {
        '；' => "中文分号，应该用英文的 ;",
        '，' => "中文逗号，应该用英文的 ,",
        '。' => "中文句号，应该用英文的 .",
        '（' => "中文左括号，应该用英文的 (",
        '）' => "中文右括号，应该用英文的 )",
        '“' or '”' => "中文双引号，应该用英文的 \"",
        '‘' or '’' => "中文单引号，应该用英文的 '",
        '：' => "中文冒号，应该用英文的 :",
        '？' => "中文问号，应该用英文的 ?",
        '！' => "中文感叹号，应该用英文的 !",
        '、' => "中文顿号，应该用英文的 ,",
        '　' => "全角空格（U+3000），删掉换成普通空格",
        '｛' => "全角左花括号，应该用英文的 {",
        '｝' => "全角右花括号，应该用英文的 }",
        _ => "这是一个全角（中文）标点，代码里要用对应的英文半角符号",
    };

    /// <summary>
    /// 看看 main 具体是怎么写错的，给出针对这一种写法的提示。
    /// 泛泛列一遍四种写法不如直接说"你这次缺的是 static"。
    /// </summary>
    private static string 诊断main(string source)
    {
        if (Regex.IsMatch(source, @"\bvoid\s+Main\s*\("))
            return "  你这次的问题：方法名写成了 **Main**（大写 M）。Java 区分大小写，入口必须是小写的 main。";
        if (Regex.IsMatch(source, @"\bmain\s*\(\s*String\s+\w+\s*\)"))
            return "  你这次的问题：参数写成了 `String 名字`，少了方括号。\n"
                 + "  必须是**数组**形式：`String[] args`（String 后面要有 []）。";
        if (Regex.IsMatch(source, @"\bmain\s*\(\s*\)"))
            return "  你这次的问题：main 没有参数。即使不用命令行参数，也必须写成 `main(String[] args)`。";
        if (Regex.IsMatch(source, @"\b(int|long|boolean|String|double)\s+main\s*\("))
            return "  你这次的问题：main 的返回类型不对。入口必须是 **void**（返回值没人接收）。";
        if (Regex.IsMatch(source, @"\bmain\s*\([^)]*\)"))
        {
            if (!Regex.IsMatch(source, @"(static[^)]{0,80}?\bmain\s*\()", RegexOptions.Singleline))
                return "  你这次的问题：main 前面**少了 static**。";
            if (!Regex.IsMatch(source, @"public[^)]{0,80}?\bmain\s*\(", RegexOptions.Singleline))
                return "  你这次的问题：main 前面**少了 public**。";
        }
        return "";
    }

    private static string 首字母大写(string s) =>
        s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
    private static string 全大写(string s) => s.ToUpperInvariant();
}
