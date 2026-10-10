// HybridEngineLight.Explain —— 把 C# 报错翻译成人话
//
// 与 Python 版（hybridengine_light/explain.py）共用同一套设计：
//
//     发生了什么     一句话，不含术语
//     出在哪一行     直接指出
//     怎么改         给能直接复制去用的写法
//     顺便记住       一条可迁移的知识
//
// 最后一条是它区别于搜索引擎的地方：查一次报错应该学到一点东西。
//
// ══ C# 与 Python 的报错差别（这决定了规则表要重写，不能照搬）══
//   · C# 有**编译错误**：代码根本没能运行。Python 只有语法错误是这个性质。
//     编译错误带 CSxxxx 编号 —— 那个编号是最可靠的识别依据，比消息文本稳定得多。
//   · C# 的 **NullReferenceException** 是新手第一大杀手，Python 里没有对应物
//     （Python 是 AttributeError on NoneType，相对少见）。
//   · C# 是**静态类型**：类型不匹配在编译期就报，而不是运行到才报。
//
// ══ 不做什么 ══
//   · 不猜测意图去自动改代码 —— 新手学不到东西，而且会改错。
//   · 不吞掉原始报错 —— 原文始终保留在末尾（老师/搜索要用）。

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HybridEngineLight;

public static class Explain
{
    /// <summary>把一个异常变成一段给人看的解释。</summary>
    public static string For(Exception ex) => For(ex, null, null);

    /// <param name="file">用户代码文件名（用于挑出"用户写的"那一帧）</param>
    /// <param name="line">用户代码行号（有就给）</param>
    public static string For(Exception ex, string? file, int? line)
    {
        var sb = new StringBuilder();
        sb.Append('\n').Append('─', 56).Append('\n');
        sb.Append("出了点问题：").Append(ex.GetType().Name).Append('\n');
        sb.Append('─', 56).Append('\n');

        if (line is int ln)
            sb.Append("出在这里：\n  第 ").Append(ln).Append(" 行\n\n");

        var body = Rules(ex);
        if (body.Count > 0)
        {
            foreach (var l in body) sb.Append(l).Append('\n');
        }
        else
        {
            // 认不出来就**承认**，而不是编一段像模像样的解释
            sb.Append("这个错误我没有内置解释（我的规则表里没有它）。\n\n");
            sb.Append("通用的排查办法：\n");
            sb.Append("  1. 看报错第一行 —— 那是类型和原因（下面的调用栈只是路径）\n");
            sb.Append("  2. 在出错那行前面 Console.WriteLine() 一下相关变量，看它们到底是什么\n");
            sb.Append("  3. 把报错原文复制去搜索（下面就是原文，方便复制）\n");
        }

        sb.Append('\n').Append("原始报错（老师或搜索需要用到的原文）：\n");
        sb.Append('─', 56).Append('\n');
        sb.Append(ex.ToString().TrimEnd()).Append('\n');
        return sb.ToString();
    }

    // ── 按顺序匹配：具体的放前面 ──
    private static List<string> Rules(Exception ex)
    {
        var outp = new List<string>();
        string msg = ex.Message ?? "";

        switch (ex)
        {
            case NullReferenceException:
                outp.AddRange(new[]
                {
                    "你用了一个**还是空（null）**的东西。",
                    "",
                    "这是 C# 新手最常遇到的错误，没有之一。意思是：",
                    "  你写了「某个东西.某个功能()」，但那个「某个东西」此刻并不存在。",
                    "",
                    "三种常见来源，对照一下：",
                    "  1. 变量声明了但没赋值 —— 例如 string 名字;  之后直接用 名字.Length",
                    "  2. 查找没找到 —— 例如 First() 在没有匹配项时返回 null，Find() 找不到也返回 null",
                    "  3. 数组/列表里的那一格是 null —— 例如一个装对象的数组，还没往里放东西",
                    "",
                    "怎么改（挑一个）：",
                    "  · 先赋值再用： string 名字 = \"小明\";",
                    "  · 不确定就先问一句： if (东西 != null) { 东西.做点什么(); }",
                    "  · 查找类的方法改用不返回 null 的版本：",
                    "      FirstOrDefault() 仍可能返回 null，但 Where(...).ToList() 不会",
                    "  · 想快速定位是哪一行崩的 → 在那一行前面 Console.WriteLine(东西);",
                    "    输出空白说明它确实是 null",
                    "",
                    "顺便记住：**null 的意思是「这里什么都没有」**。",
                    "对它做任何操作都会崩，所以用之前先确认它不是 null。",
                });
                break;

            case IndexOutOfRangeException:
                outp.AddRange(new[]
                {
                    "你要的位置超出了数组范围。",
                    "",
                    "关键在于：**C# 的位置从 0 开始数**。",
                    "  一个长度 3 的数组，合法下标是 0、1、2 —— 没有 3。",
                    "",
                    "怎么改：",
                    "  · 用 数组.Length 确认边界：合法范围是 0 .. Length-1",
                    "  · 循环用 for (int i = 0; i < 数组.Length; i++) —— **用 < 不是 <=",
                    "  · 取最后一个元素：数组[数组.Length - 1]",
                    "",
                    "顺便记住：报错说「IndexOutOfRange」就是「下标越界」；",
                    "C# 里还有一种 ArgumentOutOfRangeException 是给列表/方法的，含义类似。",
                });
                break;

            case KeyNotFoundException:
                outp.AddRange(new[]
                {
                    "字典里没有这个键。",
                    "",
                    "怎么改：",
                    "  · 先看看有哪些键： foreach (var k in 字典.Keys) Console.WriteLine(k);",
                    "  · 不确定有没有 → 用 TryGetValue，没有也不会崩：",
                    "      if (字典.TryGetValue(\"键\", out var 值)) { 用 值 }",
                    "  · 只想拿个默认值 → 字典.GetValueOrDefault(\"键\")",
                    "",
                    "顺便记住：字典[键] 取不到会抛异常，TryGetValue 取不到返回 false —— 后者更安全。",
                });
                break;

            case DivideByZeroException:
                outp.AddRange(new[]
                {
                    "整数除法的除数是 0。",
                    "",
                    "怎么改：除之前先判断：",
                    "  if (除数 != 0) { 结果 = 被除数 / 除数; }",
                    "",
                    "顺便记住两点：",
                    "  · 报错说「除数」，不是「被除数」。a / b 里 b 是除数。",
                    "  · **浮点数除以 0 不会崩**，会得到 Infinity（无穷大）—— 只有整数除法才抛这个。",
                });
                break;

            case FormatException when msg.Contains("Input string was not in a correct format")
                                   || msg.Contains("was not in a correct format"):
                outp.AddRange(new[]
                {
                    "你想把一段文字转成数字，但那段文字不是数字。",
                    "",
                    "最常见的来源：用户输入。输入了字母、或者空着直接回车。",
                    "",
                    "怎么改：",
                    "  · 用不会崩的版本，然后判断有没有成功：",
                    "      if (int.TryParse(输入, out int 数字)) { 用 数字 } else { 提示用户重新输入 }",
                    "  · 要允许小数 → 用 double.TryParse 代替 int.TryParse",
                    "",
                    "顺便记住：int.Parse 失败会抛异常，int.TryParse 失败只返回 false。",
                    "**处理用户输入时永远用 TryParse** —— 因为用户什么都可能输。",
                });
                break;

            case FileNotFoundException:
            case DirectoryNotFoundException:
                outp.AddRange(new[]
                {
                    "找不到这个文件或文件夹。",
                    "",
                    "怎么改：注意两件事 ——",
                    "  · 文件名和路径是否拼对了（含扩展名 .txt / .csv）",
                    "  · **程序的工作目录**是哪里。相对路径是相对「你运行程序时所在的目录」，",
                    "    不是相对这个 .cs 文件。",
                    "",
                    "顺便记住：不确定就先打印 Console.WriteLine(Directory.GetCurrentDirectory());",
                });
                break;

            case InvalidOperationException when msg.Contains("Sequence contains no elements"):
                outp.AddRange(new[]
                {
                    "你在一个**空的**序列上用了 First() / Max() / Single() 这类方法，",
                    "而它们要求序列里至少要有一个元素。",
                    "",
                    "怎么改（挑一个）：",
                    "  · 换成不要求非空的版本：FirstOrDefault() / Max() 前先 Any() 判断",
                    "  · 先确认有元素： if (列表.Count > 0) { ... }",
                    "",
                    "顺便记住：First() = 「必须有第一个」，FirstOrDefault() = 「有就给，没有给默认值」。",
                });
                break;

            case ArgumentNullException:
                outp.AddRange(new[]
                {
                    "你把 null 传给了一个不接受 null 的地方。",
                    "",
                    "怎么改：传之前确认那个值不是 null；或者想清楚为什么它会是 null。",
                    "",
                    "顺便记住：即使你没写 null，很多方法在「找不到」时也会返回 null ——",
                    "例如 FirstOrDefault()、Find()、GetValueOrDefault()。",
                });
                break;

            case ArgumentOutOfRangeException:
                outp.AddRange(new[]
                {
                    "你给的数值超出了这个方法允许的范围。",
                    "",
                    "怎么改：看报错的详细信息 —— 它会写出参数名和允许的范围。",
                    "常见情况：索引、数量、长度传了负数。",
                    "",
                    "顺便记住：这条报错里会带参数名，比只知道「越界」有用得多。"
                });
                break;

            case StackOverflowException:
                outp.AddRange(new[]
                {
                    "函数调用自己太多次，把调用栈撑爆了。",
                    "",
                    "几乎一定是**递归没有终止条件**，或者终止条件永远不成立。",
                    "",
                    "怎么改：检查递归函数里那个 if 判断，确认它在某一步会变成 false。",
                    "  static int 阶乘(int n) {",
                    "      if (n <= 1) return 1;        // ← 这一行必须有，而且真的会到达",
                    "      return n * 阶乘(n - 1);",
                    "  }",
                    "",
                    "顺便记住：每个递归都必须有一个「不用再递归就能直接算出来」的结束情况。",
                });
                break;
        }

        // 编译错误（CSxxxx）：那一串编号是最可靠的识别依据，比消息文本稳定
        var cs = Regex.Match(msg, @"\b(CS\d{4})\b");
        if (cs.Success) outp.AddRange(CompilerErrors(cs.Groups[1].Value, msg));

        return outp;
    }

    /// <summary>常见编译错误。编号来自 C# 编译器，比较稳定。</summary>
    private static List<string> CompilerErrors(string code, string msg) => code switch
    {
        "CS0103" => new List<string>
        {
            "你用了一个编译器不认识的**名字**。",
            "",
            "三种常见原因：",
            "  1. 拼错了 —— C# **区分大小写**，Name 和 name 是两个不同的东西",
            "  2. 用在了声明之前 —— 变量要先声明再用",
            "  3. 忘了引号 —— 想写文字要加双引号：\"你好\"，不加会被当成变量名去找",
            "",
            "怎么改：想清楚「这是个变量，还是一段文字」。",
            "  是文字 → 写成 \"...\"（带引号）",
            "  是变量 → 往上找它在哪里声明的；没声明过就先声明。",
            "",
            "顺便记住：C# 里**名字必须先用类型声明过**才存在，例如 string 名字 = \"小明\";",
        },
        "CS1002" => new List<string>
        {
            "少了分号（;）。",
            "",
            "C# 要求**每条语句末尾都有分号** —— 这是和 Python 最明显的区别之一。",
            "",
            "怎么改：看报错指出的那一行，以及**它的上一行**（漏分号常报到下一行）。",
            "",
            "顺便记住：不需要分号的地方 —— if / for / while / class / 方法签名那一行的末尾。",
        },
        "CS1513" => new List<string>
        {
            "少了右大括号 }。",
            "",
            "C# 用大括号 { } 划分代码块，而且**必须成对**。",
            "",
            "怎么改：从报错位置往上找最近的一个没闭合的 {。",
            "技巧：把光标放在某个 { 上，编辑器通常会高亮它对应的 }。",
            "",
            "顺便记住：C# 靠大括号判断层级，**和缩进无关** ——",
            "这一点和 Python 正好相反（Python 靠缩进）。",
        },
        "CS0165" => new List<string>
        {
            "你用了一个可能**还没被赋值**的变量。",
            "",
            "怎么改：",
            "  · 声明时就给个初值： int 总数 = 0;",
            "  · 或者确保所有分支都赋了值（if 和 else 里都要赋）",
            "",
            "顺便记住：C# 会检查「所有可能的路径是否都赋过值」，这是静态类型语言的优势 ——",
            "它在你运行之前就发现问题。",
        },
        "CS0029" => new List<string>
        {
            "类型对不上：你想把一种类型放进另一种类型的变量里。",
            "",
            "怎么改：",
            "  · 想转成文字 → 用 .ToString() 或者字符串插值 $\"...{变量}...\"",
            "  · 想把文字转成数字 → int.Parse / int.TryParse",
            "  · 数值类型不同 → 显式转换 (double)整数",
            "",
            "顺便记住：C# 是**强类型**语言，每种变量只能装对应类型的东西。",
            "想把数字和文字拼起来，最省事的写法是字符串插值： $\"你考了{分数}分\"",
        },
        "CS0019" => new List<string>
        {
            "这个运算符不能用在**这两种类型之间**。",
            "",
            "最常见的是拿数字和文字相加 —— C# 里 + 对这两者不通用。",
            "",
            "怎么改：优先用字符串插值，比手动转好写也不容易错：",
            "  ❌ \"结果是 \" + 数字",
            "  ✅ $\"结果是 {数字}\"",
            "",
            "顺便记住：$\"...{变量}...\" 是 C# 里最常用的拼接方式，会自动把变量变成文字。",
        },
        "CS0136" => new List<string>
        {
            "同名变量重复声明了 —— 内层作用域的名字和外层撞了。",
            "",
            "C# 不允许在嵌套的块里用**外层已经用过的名字**，哪怕类型不同。",
            "",
            "怎么改：把内层那个改个名字，或者复用外层的（不要再声明一次）。",
            "",
            "顺便记住：这叫「变量遮蔽」，C# 直接禁止，是为了避免看代码时分不清用的是哪一个。",
        },
        "CS0246" => new List<string>
        {
            "编译器找不到这个**类型**。",
            "",
            "三种常见原因：",
            "  1. 忘了 using —— 在文件顶部加一行 using System.Collections.Generic; 之类",
            "  2. 拼错了类型名（含大小写）",
            "  3. 用了需要额外安装的库，但没装",
            "",
            "顺便记住：报错里这个消息往往就告诉了你缺哪个类型；",
            "把它复制去搜索，第一条通常就是该加哪个 using。",
        },
        _ => new List<string>(),
    };
}
