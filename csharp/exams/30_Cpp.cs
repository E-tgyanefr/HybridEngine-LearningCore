// C++ 题库：基础与编程能力
//
// ══ 关于题目来源 ══
// 与 Python、C# 题库同一原则：**按公开考纲的知识点自编练习题，不是真题**。
//
// ══ 出题重点说明 ══
// C++ 的考点和 Python/C# 有本质差别，所以这里的侧重点不一样：
//   · **编译和执行是分开的** —— 很多错误编译期就报，另一些要到运行时才崩
//   · **指针与内存** —— C++ 独有，也是它最强也最危险的地方
//   · **值语义与引用** —— 参数怎么传，决定了函数能不能改到外面的东西
//   · **整数除法 / 类型转换** —— 和 C# 类似但更严格
//
// 行协议见 HybridEngine-light/BankLoader.cs。
// 对出题人：**只改下面的题表，Dump() 不用动。**

using System;
using System.Collections.Generic;

public static class CppBank
{
    // 类型, 主题, 难度, 题干, 解释, 答案
    // choice 的答案是选项序号（1 起）；output 的答案是期望输出
    static readonly List<string[]> 题目 = new List<string[]>
    {
        // ═══════ 基本语法 ═══════
        new[]{"choice", "基本语法", "1",
         "C++ 程序的入口函数是哪个？\\n\\n  A. main\\n  B. start\\n  C. begin\\n  D. program",
         "C++ 程序的入口固定是 main。\\n注意它必须返回 int：写 int main() 而不是 void main()（后者在很多编译器上能过，但不符合标准）。",
         "1"},

        new[]{"output", "基本语法", "2",
         "下面代码会输出什么？\\n\\n    int a = 7;\\n    int b = 2;\\n    std::cout << a / b;",
         "**整数除以整数，结果还是整数** —— 小数部分被直接丢掉。7 / 2 = 3。\\n想要 3.5 得让至少一边是浮点：a / 2.0 或 (double)a / b。\\n这是 C++ 新手最常算错的地方。",
         "3"},

        new[]{"output", "基本语法", "2",
         "下面代码会输出什么？\\n\\n    std::cout << 7 % 3;",
         "% 是取余数。7 除以 3 余 1。\\n注意：**C++ 的 % 只能用于整数**，写 7.0 % 3 会编译不过（Python 可以，这是差别）。",
         "1"},

        new[]{"choice", "基本语法", "2",
         "C++ 里每条语句末尾必须有什么？\\n\\n  A. 分号 ;\\n  B. 冒号 :\\n  C. 什么都不用\\n  D. 句号 .",
         "C++ 用分号表示语句结束。\\n**漏分号的报错会出现在下一行**，而且编译器说的是 \\\"expected ';' before ...\\\" —— \\n新手常盯着报错那一行改半天，其实要看上一行。",
         "1"},

        new[]{"output", "基本语法", "3",
         "下面代码会输出什么？\\n\\n    int x = 5;\\n    if (x = 3) { std::cout << \\\"真\\\"; }\\n    else { std::cout << \\\"假\\\"; }",
         "**这是 C++ 最阴险的坑之一。** x = 3 是**赋值**不是比较（应该是 x == 3）。\\nx 被改成 3，而 3 是非零值 → 条件为真 → 输出「真」。\\n更糟的是编译器可能只给个警告，不报错 —— 程序能跑，但逻辑完全不对。",
         "真"},

        // ═══════ 编译 vs 运行 ═══════
        new[]{"choice", "编译与运行", "2",
         "下面哪个错误**编译时**就会被发现？\\n\\n  A. 变量名拼错\\n  B. 数组下标越界\\n  C. 整数除以 0\\n  D. 空指针解引用",
         "**拼错变量名编译期就报**（undeclared identifier）。\\n后面三个都是**运行时**才出事 —— 而 C++ 的运行时不帮你检查，\\n结果就是读到垃圾值或者直接崩（段错误）。这是 C++ 和 Python 最大的区别。",
         "1"},

        new[]{"choice", "编译与运行", "3",
         "int a[3]; 然后访问 a[5]，会发生什么？\\n\\n  A. 未定义行为 —— 可能读到垃圾值，也可能崩溃\\n  B. 抛出一个越界异常\\n  C. 自动扩容数组\\n  D. 编译报错",
         "**C++ 不检查数组下标。** 越界是「未定义行为」—— 什么都可能发生：\\n读到别的变量的值、崩溃、或者看起来正常但结果错了。\\nPython 会抛 IndexError，C# 会抛 IndexOutOfRangeException，**C++ 什么都不做**。\\n这就是 C++ 快但危险的原因：检查的活儿交给了你。",
         "1"},

        // ═══════ 指针与内存（C++ 独有）═══════
        new[]{"output", "指针", "3",
         "下面代码会输出什么？\\n\\n    int x = 5;\\n    int* p = &x;\\n    *p = 9;\\n    std::cout << x;",
         "p 存的是 x 的**地址**（&x 取地址），*p 是「p 指向的那个东西」。\\n所以 *p = 9 就是改 x，输出 9。\\n两个符号要分清：**& 取地址，* 解引用（取那个地址上的值）**。",
         "9"},

        new[]{"choice", "指针", "3",
         "int* p; 然后直接 *p = 5; 会怎样？\\n\\n  A. 未定义行为 —— p 没初始化，指向随机地址\\n  B. 正常赋值 5\\n  C. 编译报错\\n  D. p 会自动指向一个新变量",
         "**指针没初始化就是野指针** —— 它指向一个随机地址。\\n往里写数据 = 破坏别人的内存，结果不可预测（可能立刻崩，也可能过一会儿才崩）。\\n正确写法：int x; int* p = &x; 或者 int* p = nullptr;（暂时不指向任何东西）。",
         "1"},

        new[]{"choice", "指针", "2",
         "值传递和引用传递的区别是什么？\\n\\n  A. 引用传递能改到外面的变量，值传递不能\\n  B. 值传递更快\\n  C. 没有区别\\n  D. 引用传递会复制一份",
         "**值传递是复制一份**，函数里改的是副本，外面的变量不变。\\n**引用传递（int& x）是直接操作原来那个变量**，改了外面就变。\\n想改外面的值就必须用引用（或指针）—— 这是 C++ 里一个必须搞清的概念。",
         "1"},

        // ═══════ 字符串 ═══════
        new[]{"output", "字符串", "2",
         "下面代码会输出什么？\\n\\n    std::string s = \\\"Hello\\\";\\n    std::cout << s.length();",
         "std::string 的 length() **是个函数，要加括号**。\\n这一点和 C#（s.Length 是属性，不加括号）正好相反，跨语言时容易混。\\n另外 s[0] 是 'H'（下标从 0 开始）。",
         "5"},

        new[]{"choice", "字符串", "2",
         "char 和 std::string 的区别是？\\n\\n  A. char 存一个字符，string 存一串\\n  B. 完全一样\\n  C. char 更长\\n  D. string 只能存字母",
         "char 是**单个字符**，用单引号：char c = 'A';\\nstd::string 是**一串字符**，用双引号：std::string s = \\\"ABC\\\";\\n⚠ 单引号 vs 双引号在 C++ 里区别很严格：'A' 是 char，\\\"A\\\" 是字符串（末尾还有个隐藏的结束符）。",
         "1"},

        // ═══════ 函数 ═══════
        new[]{"output", "函数", "3",
         "下面代码会输出什么？\\n\\n    void 加一(int& n) { n = n + 1; }\\n    int main() {\\n        int a = 5;\\n        加一(a);\\n        std::cout << a;\\n    }",
         "参数写成 int& n（**引用**）时，函数里改 n 就是改外面的 a。\\n所以输出 6。\\n如果把 & 去掉（void 加一(int n)），函数改的是副本，a 还是 5 —— 这是最容易搞错的地方。",
         "6"},

        new[]{"choice", "函数", "2",
         "函数声明了要返回值，但某个分支忘了 return，会怎样？\\n\\n  A. 编译器给警告，运行结果不确定\\n  B. 自动返回 0\\n  C. 编译报错\\n  D. 自动返回上一次的值",
         "多数编译器给**警告**（control reaches end of non-void function），不是错误。\\n但那个分支的返回值是**不确定的** —— 可能碰巧对，也可能每次不一样。\\n所以：警告也要看，别只盯着 error。",
         "1"},
    };

    // 写代码题：入口名 → (题干, 解释)
    static readonly List<string[]> 代码题 = new List<string[]>
    {
        new[]{"平方和", "写一个函数 平方和(int n)：返回 1² + 2² + ... + n²（n 为 0 时返回 0）。",
         "用 for 循环累加。注意累加变量必须初始化为 0 —— 不初始化的话它是随机值（C++ 局部变量不会自动清零，这点和 Python 不同）。\\n循环写法通常是 for (int i = 1; i <= n; i++)。",
         "平方和"},
        new[]{"最大值", "写一个函数 最大值(int a, int b)：返回两个数里较大的那个。",
         "可以用 if / else，也可以用三元运算符 (a > b) ? a : b。\\n⚠ 注意别写成 if (a = b)（赋值）—— 那会把 b 的值赋给 a，然后条件永远为真。",
         "最大值"},
    };

    // 代码题测试：入口名 → (说明, 调用表达式, 期望输出)
    static readonly Dictionary<string, (string Desc, string Call, string Expect)[]> 测试 =
        new Dictionary<string, (string, string, string)[]>
    {
        { "平方和", new (string, string, string)[] {
            ("平方和(3) 应得 14", "平方和(3)", "14"),
            ("平方和(1) 应得 1", "平方和(1)", "1"),
            ("平方和(0) 应得 0", "平方和(0)", "0"),
            ("平方和(10) 应得 385", "平方和(10)", "385"),
        }},
        { "最大值", new (string, string, string)[] {
            ("最大值(3,5) 应得 5", "最大值(3, 5)", "5"),
            ("最大值(9,2) 应得 9", "最大值(9, 2)", "9"),
            ("最大值(4,4) 应得 4", "最大值(4, 4)", "4"),
            ("最大值(-1,-7) 应得 -1", "最大值(-1, -7)", "-1"),
        }},
    };

    // 代码题的**程序骨架**：学生的函数会被替换到 /*学生函数*/ 的位置。
    //
    // 为什么要有这个：学生写的是一**个函数**，而 C++ 必须有 main() 才能编译。
    // 骨架子由题库提供 —— 这样学生不用管 #include、不用管 main，
    // 可以专心写那一件事（这也是"少点干扰"的做法）。
    //
    // 注意每个骨架都自带一个 main：它按顺序打印每条测试的结果，
    // 每行一个值。判分时父进程按行比对。
    static string 骨架(string 入口, (string Desc, string Call, string Expect)[] 用例)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("#include <iostream>\n");
        sb.Append("#include <string>\n");
        sb.Append("#include <vector>\n");
        sb.Append("#include <cmath>\n\n");
        sb.Append("// ── 学生写的函数 ──\n");
        sb.Append("/*学生函数*/\n\n");
        sb.Append("int main() {\n");
        foreach (var t in 用例)
            sb.Append("    std::cout << ").Append(t.Call).Append(" << \"\\n\";\n");
        sb.Append("    return 0;\n");
        sb.Append("}\n");
        return sb.ToString();
    }

    /// <summary>把题目按行协议打印出来。改题目不需要动这里。</summary>
    public static void Dump()
    {
        Console.WriteLine("E\tC++ 编程能力练习（基础水平）\tC++\t入门\t按公开知识点自编练习题（非真题）\t120");

        for (int i = 0; i < 题目.Count; i++)
        {
            var q = 题目[i];
            Console.WriteLine("Q\tCPP-" + (i + 1).ToString("000") + "\t" + q[0] + "\t" + q[1]
                              + "\t" + q[2] + "\t" + 转义(q[3]) + "\t" + 转义(q[4]) + "\t" + 转义(q[5]));
        }

        foreach (var c in 代码题)
        {
            string 入口 = c[3];
            Console.WriteLine("K\t" + 入口 + "\t" + 转义(c[1]) + "\t" + 转义(c[2]));
            // W 行：程序骨架。学生在里面写函数，我们负责把 #include 和 main 备好。
            //
            // ⚠ 这里用 **Base64** 而不是普通转义。
            //   骨架里要写 C++ 的换行符 "\n"，而协议层也用反斜杠转义 ——
            //   两种含义的 \n 混在一起，往返一次就错位，生成的源码里字符串
            //   被真实换行劈成两半。Base64 的字符集里没有反斜杠、换行、制表符，
            //   所以内容是什么都不可能破坏协议。
            //   代价只是字段不可读 —— 但骨架本来也不是给人读的（题目在 K 行的题干里）。
            Console.WriteLine("W\t" + 入口 + "\t" + Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(骨架(入口, 测试[入口]))));
            foreach (var t in 测试[入口])
                Console.WriteLine("T\t" + 入口 + "\t" + 转义(t.Desc) + "\t" + 转义(t.Call) + "\t" + 转义(t.Expect));
        }
    }

    static string 转义(string s) =>
        s.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "").Replace("\t", "\\t");
}
