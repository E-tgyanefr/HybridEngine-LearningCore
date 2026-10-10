// HybridEngineLight.ExplainCpp —— 把 C++ 编译器报错翻译成人话。
//
// ══ 为什么 C++ 比 Python / C# 更需要这个 ══
//
// Python 的报错至少说的是人话（"name 'x' is not defined"）。
// C++ 的报错是出了名的难懂，而且**经常指错地方**：
//
//     第 3 行： int x = 5        ← 这里漏了分号
//     第 4 行： error: expected ',' or ';' before 'std'   ← 却报在第 4 行
//
// 新手会盯着第 4 行改半天，而那行完全没问题。
// 真实原因是**上一行漏了分号** —— 这个信息编译器不会说。
//
// 所以这个翻译器有一条 Python/C# 版没有的规则：
// **「expected ... before ...」八成是上一行的问题，要明确指出来。**
//
// ══ 结构（三个语言版本统一）══
//     发生了什么 → 出在哪一行 → 怎么改 → 顺便记住
//
// ══ 不做什么 ══
//   · 不自动改代码 —— 新手学不到东西，而且会改错
//   · 不吞掉原始报错 —— 原文始终保留在末尾

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HybridEngineLight;

public static class ExplainCpp
{
    /// <summary>把一次编译/运行失败变成一段给人看的解释。</summary>
    /// <param name="raw">编译器或程序的原始输出</param>
    /// <param name="source">学生写的源码（用来判断上下文，例如上一行有没有分号）</param>
    /// <param name="compileFailed">true=编译失败；false=运行时出错</param>
    public static string For(string raw, string source, bool compileFailed)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append('\n').Append('─', 56).Append('\n');
        sb.Append(compileFailed ? "编译没能通过\n" : "程序运行时出错了\n");
        sb.Append('─', 56).Append('\n');

        var (行号, 消息) = PickError(raw);
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
            sb.Append("  1. 先看**第一条** error —— 后面的往往是它引起的连锁反应\n");
            sb.Append("  2. 分号、括号、引号有没有配对上（尤其看报错那一行的**上一行**）\n");
            sb.Append("  3. 把报错原文复制去搜索（下面就是原文，方便复制）\n");
        }

        sb.Append('\n').Append("原始报错（老师或搜索需要用到的原文）：\n");
        sb.Append('─', 56).Append('\n');
        // 原始输出可能很长（模板展开），截断一下但保留第一条 error
        var lines = raw.Split('\n');
        int shown = 0;
        foreach (var l in lines)
        {
            sb.Append(l.TrimEnd()).Append('\n');
            if (++shown >= 24) { sb.Append("（…后面的输出省略）\n"); break; }
        }
        return sb.ToString();
    }

    /// <summary>从编译器输出里取 (行号, 消息)。</summary>
    private static (int, string) PickError(string raw)
    {
        foreach (var line in raw.Split('\n'))
        {
            string s = line.Trim();
            int i = s.IndexOf("error:", StringComparison.Ordinal);
            if (i < 0) continue;
            string 消息 = s.Substring(i + 6).Trim();
            // 行号形如  main.cpp:4:5: error: ...
            var m = Regex.Match(s, @":(\d+):\d+:\s*error:");
            int 行 = m.Success ? int.Parse(m.Groups[1].Value) : 0;
            return (行, 消息);
        }
        return (0, "");
    }

    private static string SourceLine(string source, int lineNo)
    {
        var ls = source.Replace("\r\n", "\n").Split('\n');
        if (lineNo >= 1 && lineNo <= ls.Length) return ls[lineNo - 1].TrimEnd();
        return "";
    }

    private static string PrevLine(string source, int lineNo)
    {
        var ls = source.Replace("\r\n", "\n").Split('\n');
        int i = lineNo - 2;                       // 行号是 1 起的
        return (i >= 0 && i < ls.Length) ? ls[i].TrimEnd() : "";
    }

    /// <summary>
    /// 源码里有没有数组（或类似的下标访问）—— 用来决定要不要提「越界不崩」那条。
    /// 这是个保守判断：宁可多提一句，也不要漏掉一个真实的越界。
    /// </summary>
    private static bool 疑似越界(string source)
    {
        if (!source.Contains("[")) return false;
        foreach (var t in new[] { "int ", "double ", "char ", "float ", "long " })
            if (source.Contains(t)) return true;
        return false;
    }

    /// <summary>
    /// 解释一条**警告**（不是错误）。
    /// 警告容易被新手忽略，但未初始化变量这类警告说的是真实存在的 bug ——
    /// 只是这次碰巧没发作。
    /// </summary>
    public static string ExplainWarning(string raw)
    {
        if (raw.Contains("uninitialized"))
        {
            return
                "  · 你声明了一个变量但**没有给它初值**，然后就拿它去累加/运算。\n"
                + "  · 在 C++ 里，未初始化的局部变量里装的是**随机值** ——\n"
                + "    这次它碰巧是 0，所以结果看起来是对的。但它没有任何保证。\n"
                + "  · 改法：声明时就给初值。\n"
                + "        int s = 0;          ← 这样就对了\n"
                + "        int s;              ← 这样是隐患\n"
                + "  · 顺便记住：C++ 不会帮你把局部变量清零（这一点和 Python、C# 都不同）。";
        }
        if (raw.Contains("array-bounds"))
            return "  · 编译器发现你访问了数组范围之外的下标。即使没崩，读到的也是别的内存。\n"
                 + "  · 改法：下标必须满足 0 <= i && i < 数组长度。";
        if (raw.Contains("return-local-addr"))
            return "  · 你返回了一个**局部变量**的地址 —— 那个变量在函数结束时就被销毁了，\n"
                 + "    拿到的地址指向无效内存。改法：返回值本身，而不是它的地址。";
        return "  · 编译器给出了警告，建议对照上面的原始输出检查一下。";
    }

    // ══════════════════ 规则 ══════════════════
    private static List<string> Rules(string msg, string raw, string source, int lineNo, bool compileFailed)
    {
        var o = new List<string>();

        // ── ① 最经典的一条：expected ... before ... ──
        // 这一条放在最前面，因为它覆盖了初学者最大一部分报错，
        // 而且**报错行几乎总是错的**（真凶在上一行）。
        if (msg.StartsWith("expected", StringComparison.Ordinal))
        {
            var prev = PrevLine(source, lineNo);
            bool prevLooksUnfinished = prev.Length > 0
                && !prev.TrimEnd().EndsWith(";", StringComparison.Ordinal)
                && !prev.TrimEnd().EndsWith("{", StringComparison.Ordinal)
                && !prev.TrimEnd().EndsWith("}", StringComparison.Ordinal)
                && !prev.TrimEnd().StartsWith("#", StringComparison.Ordinal)
                && !prev.TrimEnd().EndsWith(":", StringComparison.Ordinal);

            o.Add("编译器在这一行读不下去了，但它说的位置**经常不是真正的问题所在**。");
            o.Add("");
            if (prevLooksUnfinished)
            {
                o.Add($"⚠ 最可能的原因：**上一行（第 {lineNo - 1} 行）漏了分号**");
                o.Add($"      第 {lineNo - 1} 行： {prev}");
                o.Add("");
                o.Add("C++ 用分号表示语句结束。漏了之后编译器会一路读到下一行的内容，");
                o.Add("然后在**它读不懂的地方**报错 —— 所以报错行是下一行。");
            }
            else
            {
                o.Add("检查这几处：");
                o.Add("  · 这一行和**上一行**的末尾有没有分号 ;");
                o.Add("  · 括号有没有配对：( ) { } [ ]");
                o.Add("  · 字符串的引号有没有闭合");
            }
            o.Add("");
            o.Add("怎么改：先看**上一行**，再回来看这一行。");
            o.Add("");
            o.Add("顺便记住：C++ 的报错行号要**往上多看一行**。这条经验能省掉你很多时间。");
            return o;
        }

        // ── ② 用了没声明的名字 ──
        if (msg.Contains("was not declared in this scope") || msg.Contains("undeclared"))
        {
            var m = Regex.Match(msg, @"'([^']+)'");
            string 名字 = m.Success ? m.Groups[1].Value : "某个名字";
            o.Add($"你用了「{名字}」，但编译器不知道它是什么。");
            o.Add("");
            o.Add("C++ 有一条 Python 没有的规则：**变量和函数都必须先声明类型才能用**。");
            o.Add("");
            o.Add("三种常见原因：");
            o.Add($"  1. 拼错了 —— C++ **区分大小写**，Name 和 name 是两个东西");
            o.Add($"  2. 没声明类型 —— 要写成 int {名字} = ...;  而不是直接 {名字} = ...;");
            o.Add($"  3. 用在了声明之前 —— 变量要先声明再用");
            o.Add("");
            o.Add($"怎么改：先想清楚「{名字}是变量还是函数」。");
            o.Add($"  是变量 → 在用它之前写 int {名字} = 初值;");
            o.Add("  是标准库的东西 → 检查 #include 和命名空间（例如 std::cout 需要 #include <iostream>）");
            o.Add("");
            o.Add("顺便记住：C++ 里每个变量都必须有**类型**，这是它和 Python 最大的区别。");
            return o;
        }

        // ── ③ 忘了 std:: ──
        if (msg.Contains("'cout' was not declared") || msg.Contains("'endl' was not declared")
            || msg.Contains("'cin' was not declared") || msg.Contains("'string' was not declared"))
        {
            o.Add("标准库的东西都在 **std** 这个命名空间里，用的时候要带上 std::。");
            o.Add("");
            o.Add("怎么改（挑一个）：");
            o.Add("  · 每次带上前缀： std::cout << \"你好\";");
            o.Add("  · 或者文件开头加一行： using namespace std;");
            o.Add("      然后就能直接写 cout << \"你好\";");
            o.Add("");
            o.Add("另外还要确认 include 了对应的头文件：");
            o.Add("  #include <iostream>   ← cout / cin / endl");
            o.Add("  #include <string>     ← std::string");
            o.Add("");
            o.Add("顺便记住：`using namespace std;` 在练习里很方便，但大型项目里一般不写 ——");
            o.Add("          因为 std 里的名字太多，容易和你的名字撞上。");
            return o;
        }

        // ── ④ 少了头文件 ──
        if (msg.Contains("No such file or directory") && raw.Contains("#include"))
        {
            var m = Regex.Match(raw, @"#include\s*<([^>]+)>");
            string 头 = m.Success ? m.Groups[1].Value : "那个头文件";
            o.Add($"找不到头文件 <{头}>。");
            o.Add("");
            o.Add("检查一下拼写 —— 常见的几个是：");
            o.Add("  <iostream>  输入输出（cout / cin）");
            o.Add("  <string>    字符串");
            o.Add("  <cmath>     数学函数（sqrt / pow）");
            o.Add("  <vector>    动态数组");
            o.Add("");
            o.Add("注意是**尖括号** < >，不是引号 \" \" ——");
            o.Add("  尖括号 = 标准库的头文件");
            o.Add("  引号   = 你自己项目里的头文件");
            return o;
        }

        // ── ⑤ 类型不匹配 ──
        if (msg.Contains("cannot convert") || msg.Contains("invalid conversion")
            || msg.Contains("no matching function"))
        {
            o.Add("类型对不上。");
            o.Add("");
            o.Add("C++ 是**强类型**语言：每种变量只能装对应类型的东西，而且不会自动帮你转。");
            o.Add("");
            o.Add("怎么改：");
            o.Add("  · 想把整数当小数用 → 显式转换： double d = (double)n / 2;");
            o.Add("  · 数字转文字 → 用 std::to_string(n)");
            o.Add("  · 文字转数字 → 用 std::stoi(s)");
            o.Add("");
            o.Add("⚠ 一个特别常见的坑：**整数除以整数，结果还是整数**。");
            o.Add("      int a = 7, b = 2;");
            o.Add("      cout << a / b;        // 输出 3，不是 3.5");
            o.Add("      cout << (double)a / b; // 这样才是 3.5");
            return o;
        }

        // ── ⑥ 忘了 return ──
        if (msg.Contains("control reaches end of non-void function"))
        {
            o.Add("这个函数声明了要返回一个值，但有的路径上没有 return。");
            o.Add("");
            o.Add("比如：");
            o.Add("    int 判断(int x) {");
            o.Add("        if (x > 0) return 1;");
            o.Add("        // ← x <= 0 的时候没有 return");
            o.Add("    }");
            o.Add("");
            o.Add("怎么改：确保**所有分支**都有返回值。最简单的办法是在末尾补一个兜底的：");
            o.Add("    return 0;");
            o.Add("");
            o.Add("顺便记住：int main() 可以不写 return 0（C++ 会自动补），但其它函数不行。");
            return o;
        }

        // ── ⑦ 分号、括号之类的通用语法 ──
        if (msg.Contains("expected initializer") || msg.Contains("expected primary-expression"))
        {
            o.Add("这一行的语法不完整。");
            o.Add("");
            o.Add("最常见的是漏了分号，或者用了中文标点。");
            o.Add("");
            o.Add("⚠ **中文标点是新手最隐蔽的坑**：");
            o.Add("      ；　（）　，　\"　\"");
            o.Add("    这些看起来和英文的差不多，但编译器一个都不认。");
            o.Add("    输入法切到中文时很容易打出来。");
            o.Add("");
            o.Add("怎么改：把这一行的标点重新用**英文输入法**打一遍。");
            return o;
        }

        // ── ⑧ 运行时：段错误（最常见的崩溃）──
        if (!compileFailed && (raw.Contains("Segmentation fault") || raw.Contains("segmentation fault")
                               || raw.Contains("0xC0000005") || raw.Contains("access violation")))
        {
            o.Add("程序崩了 —— 访问了不该访问的内存。");
            o.Add("");
            o.Add("**这是 C++ 最典型的错误**，Python 和 C# 里不会有（它们会帮你检查）。");
            o.Add("");
            o.Add("三种常见原因：");
            o.Add("  1. 数组下标越界 —— int a[3]; 却访问 a[5]");
            o.Add("     注意：C++ **不会检查下标**，越界也不报错，直接崩或读到垃圾值");
            o.Add("  2. 用了没初始化的指针 —— int* p; *p = 5;");
            o.Add("  3. 访问已经释放的内存");
            o.Add("");
            o.Add("怎么改：");
            o.Add("  · 检查所有数组访问，下标必须 < 数组长度");
            o.Add("  · 指针必须指向有效内存再用（int x = 5; int* p = &x;）");
            o.Add("  · 拿不准就先用 cout 打印，看程序跑到哪里崩的");
            o.Add("");
            o.Add("顺便记住：**这就是 C++ 比 Python 快、但也更容易崩的原因** ——");
            o.Add("          它把内存检查的活儿交给了你自己。");
            return o;
        }

        // ── ⑨ 除零（整数）──
        // ── 运行时崩溃：按**退出码**判断 ──
        //
        // 这几个码是实测出来的，不是猜的（Windows / MinGW）：
        //     空指针解引用    -1073741819  (0xC0000005)  访问违例
        //     整数除以 0      -1073741676  (0xC0000094)  整数除零
        //     正常退出        0
        // 注意也接受 0xC0000005 这种未签名写法（不同平台/工具显示不一样）。
        bool 访问违例 = raw.Contains("-1073741819") || raw.Contains("0xC0000005")
                        || raw.Contains("3221225477") || raw.Contains("Segmentation fault");
        bool 整数除零 = raw.Contains("-1073741676") || raw.Contains("0xC0000094")
                        || raw.Contains("3221225620") || raw.Contains("Floating point exception");

        // ⚠ 除零这条规则**必须按退出码判断**（不能无条件返回）。
        //
        //   这里踩过两次坑，值得记下来：
        //
        //   ① 原来有两个**无条件返回「整数除以 0」的裸块** ——
        //      后果是任何不认识的编译错误都被误诊成除零。
        //      同事在验收时撞到 3 次：学生照着「检查除数是不是 0」去查，
        //      而他的代码里根本没有除法，查不到只会怀疑自己。
        //      **错误的诊断比没有诊断更糟。**
        //
        //   ② 我删那两块的时候，连**正确规则的函数体**一起删掉了，
        //      留下一个悬空的 `if (整数除零)` 直接接下一个 if ——
        //      编译居然能过，但这道规则静默失效了。是测试抓出来的：
        //      `FAIL: 除零被翻译成「除以 0」`。
        //
        //   所以下面把「判断条件」和「函数体」写在一起，不再分开 —— 免得再被误删。
        if (!compileFailed && 整数除零)
        {
            o.Add("整数除以 0 了。");
            o.Add("");
            o.Add("怎么改：除之前判断一下除数是不是 0：");
            o.Add("    if (b != 0) std::cout << a / b;");
            o.Add("");
            o.Add("顺便记住：**浮点数除以 0 不会崩**（会得到 inf），只有整数除法才崩。");
            o.Add("          而且三个语言的表现完全不同：");
            o.Add("            Python  抛 ZeroDivisionError —— 有明确提示");
            o.Add("            C#      抛 DivideByZeroException —— 有明确提示");
            o.Add("            C++     **直接崩，什么都不说**");
            o.Add("          这就是为什么 C++ 调试更依赖「自己打印中间值」。");
            return o;
        }

        if (!compileFailed && 访问违例)
        {
            o.Add("程序崩了 —— 访问了不该访问的内存。");
            o.Add("");
            o.Add("**这是 C++ 最典型的错误**，Python 和 C# 里不会有（它们会帮你检查）。");
            o.Add("");
            o.Add("三种常见原因：");
            o.Add("  1. 用了没初始化的指针或空指针 —— int* p; *p = 5;");
            o.Add("  2. 数组下标越界（越界很多时才会崩，见下面那条提醒）");
            o.Add("  3. 访问已经释放的内存");
            o.Add("");
            o.Add("怎么改：");
            o.Add("  · 指针必须指向有效内存再用： int x = 5; int* p = &x;");
            o.Add("  · 检查所有数组访问，下标必须 < 数组长度");
            o.Add("  · 拿不准就在每一步前面 cout 一下，看程序跑到哪一步崩的");
            o.Add("");
            o.Add("顺便记住：**这就是 C++ 比 Python 快、但也更容易崩的原因** ——");
            o.Add("          它把内存检查的活儿交给了你自己。");
            return o;
        }

        // ⚠ 实测发现的情况：数组越界**不一定崩**。
        //   我试过 int a[3]={1,2,3}; 然后读 a[10]，程序正常退出并输出 32758 ——
        //   那是别的变量的内存被当成数组元素读了。
        //   编译器不警告、程序不崩，**结果悄无声息地错了**。
        //   这是 C++ 里最难查的一类 bug，值得单独讲一条。
        if (!compileFailed && raw.Contains("exit code 0") && 疑似越界(source))
        {
            o.Add("程序正常结束了，但有个 C++ 特有的危险要提醒你：**数组越界不一定会崩**。");
            o.Add("");
            o.Add("越界一点点的时候，程序读到的往往是**别的变量的内存** ——");
            o.Add("不报错、不崩溃，只是结果莫名其妙地不对。");
            o.Add("");
            o.Add("    int a[3] = {1, 2, 3};");
            o.Add("    std::cout << a[10];     // 不崩，打印一个垃圾值");
            o.Add("");
            o.Add("怎么改：检查所有数组访问的下标，必须满足 0 <= i && i < 数组长度。");
            o.Add("      循环写成 for (int i = 0; i < n; i++)，注意是 **< 不是 <=**。");
            o.Add("");
            o.Add("顺便记住：C++ **完全不检查下标**。");
            o.Add("          Python 抛 IndexError、C# 抛异常，C++ 什么都不做 ——");
            o.Add("          它假设你知道自己在干什么。");
            return o;
        }

        if (!compileFailed && raw.Contains("exit code"))
        {
            o.Add("程序异常退出了（不是我认识的崩溃类型）。");
            o.Add("");
            o.Add("怎么查：在关键步骤前面加 cout 打印，确认程序走到哪一步就不走了。");
            o.Add("    std::cout << \"到这里了\\n\";");
            o.Add("");
            o.Add("这是 C++ 里最实用的排查手段 —— 因为它不会像 Python 那样告诉你出错在哪一行。");
            return o;
        }

        // ⚠ 这里原来还有个**无条件返回「整数除以 0」的裸块**（第二处）。
        //   两个裸块叠在一起，后果是：**任何不认识的编译错误都被误诊成除零**。
        //   同事在验收时撞到 3 次 —— 学生会照着「检查除数是不是 0」去查，
        //   而他的代码里根本没有除法，查不到只会怀疑自己。
        //
        //   **错误的诊断比没有诊断更糟**：它主动把人引到错路上。
        //   所以这里什么都不返回，让 For() 走通用兜底（「这个错误我没有内置解释」）。
        //   要判断除零，靠上面按**实测退出码**的那两条规则（那是有证据的）。

        return o;
    }
}
