// 考级题库：C# 基础与编程能力
//
// ══ 关于题目来源 ══
// 与 Python 考级题库同一原则：**按公开考纲的知识点自编练习题，不是真题**。
// 目的是让你具备该等级的能力，而不是记住某年的答案。
//
// ══ 这个文件为什么长得不像普通题库 ══
// C# 是编译语言，题库要在**运行时被编译**才能读进来。所以这里的 Dump()
// 负责把题目按行协议打印出来，主程序解析。协议见 LearnCode/BankLoader.cs。
//
// 对出题人：**只改下面的 Q 列表和 CODE_* 两张表，Dump() 不用动。**

using System;
using System.Collections.Generic;

public static class CSharpBank
{
    // ── 普通题：类型, 主题, 难度, 题干, 解释, 答案 ──
    // choice 的答案是选项序号（1 起）；output 的答案是期望输出。
    // 选择题的选项写在题干里（用 A/B/C/D 列出来）—— 行协议保持简单，不再加字段。
    static readonly List<string[]> 普通题 = new List<string[]>
    {
        // ═══════ 基本语法 ═══════
        new[]{"choice", "基本语法", "1",
         "C# 里每条语句末尾必须有什么？\\n\\n  A. 分号 ;\\n  B. 冒号 :\\n  C. 什么都不用\\n  D. 句号 .",
         "C# 用分号表示语句结束 —— 这是和 Python 最明显的区别之一。Python 靠换行。",
         "1"},

        new[]{"output", "基本语法", "2",
         "下面代码会打印什么？\\n\\n    int a = 7;\\n    int b = 2;\\n    Console.WriteLine(a / b);",
         "**整数除以整数，结果还是整数** —— 小数部分被直接丢掉，不是四舍五入。7 / 2 = 3。\\n想要小数得让至少一边是浮点：7 / 2.0 得 3.5。这是 C# 新手最容易算错的地方。",
         "3"},

        new[]{"output", "基本语法", "2",
         "下面代码会打印什么？\\n\\n    Console.WriteLine(7 % 3);",
         "% 是取余数（7 除以 3 余 1）。整数取余在 C# 和 Python 里结果一致，\\n但**负数取余两者不同**：-3 % 2 在 C# 里是 -1，在 Python 里是 1。跨语言时要留意。",
         "1"},

        new[]{"choice", "基本语法", "2",
         "下面哪个**不是**合法的 C# 变量名？\\n\\n  A. _name\\n  B. name2\\n  C. 2name\\n  D. name_2",
         "变量名不能以数字开头。name2 可以（数字在后面），2name 不行。\\n另外不能是关键字（if、for 等）。想用关键字当名字可以加 @ 前缀（如 @class），但那属于特殊场合。",
         "3"},

        new[]{"output", "基本语法", "2",
         "下面代码会打印什么？\\n\\n    string s = \"Hello\";\\n    Console.WriteLine(s.Length);",
         "字符串的 Length 是**属性不是方法** —— 写 s.Length，加上括号反而编译不过。\\n这是 C#（属性）与 Java（方法 length()）的经典差别。",
         "5"},

        new[]{"output", "基本语法", "2",
         "下面代码会打印什么？\\n\\n    Console.WriteLine(\"a\" + 1 + 2);",
         "从左到右算：\"a\" + 1 先变成文字 \"a1\"，再 + 2 得 \"a12\"。\\n若写成 1 + 2 + \"a\"，先算 1+2=3 再拼，得 \"3a\"。**顺序改变结果**，常考。",
         "a12"},

        new[]{"output", "基本语法", "3",
         "下面代码会打印什么？\\n\\n    int x = 5;\\n    Console.WriteLine($\"x 是 {x}, 两倍是 {x * 2}\");",
         "$ 开头的字符串叫**字符串插值**：大括号里不只能是变量，也能写表达式，会先算再放进去。\\n这是 C# 里最常用的拼接方式，比用 + 拼更清楚。",
         "x 是 5, 两倍是 10"},

        // ═══════ 类型与转换 ═══════
        new[]{"choice", "类型", "2",
         "C# 里 int 和 double 相加，结果是什么类型？\\n\\n  A. int\\n  B. double\\n  C. 编译错误\\n  D. 取决于值的大小",
         "C# 会自动把 int **提升**成 double（不丢精度），所以结果是 double。\\n反过来把 double 赋给 int 必须显式转换 (int)，因为会丢小数 —— 编译器不让你悄悄丢。",
         "2"},

        new[]{"output", "类型", "3",
         "下面代码会打印什么？\\n\\n    double d = 3.9;\\n    int i = (int)d;\\n    Console.WriteLine(i);",
         "显式转换 (int) 是**截断**不是四舍五入 —— 3.9 变 3，不是 4。\\n想四舍五入要用 Math.Round(3.9) 得 4。这个区别在算钱的时候很要命。",
         "3"},

        new[]{"choice", "类型", "2",
         "把用户输入的文字转成整数，最安全的写法是？\\n\\n  A. int.Parse(输入)\\n  B. int.TryParse(输入, out int n)\\n  C. (int)输入\\n  D. Convert.ToIn32(输入)",
         "**处理用户输入永远用 TryParse** —— 因为用户什么都可能输。\\nParse 遇到非数字会抛 FormatException（程序崩），TryParse 只返回 false，你可以提示重新输入。\n选项 D 里 ToIn32 还是拼错的（应为 ToInt32）—— 真实考试里也常这么设干扰项。",
         "2"},

        // ═══════ 流程控制 ═══════
        new[]{"output", "流程控制", "2",
         "下面代码会打印几行？\\n\\n    for (int i = 0; i < 3; i++)\\n        Console.WriteLine(i);",
         "for 的三段是「初始化; 条件; 每轮之后做什么」。i 取 0、1、2，条件 i<3 不成立就停。\\n注意条件是 **< 不是 <=** —— 写 <= 会多跑一次，最经典的差一错误。",
         "3"},

        new[]{"output", "流程控制", "3",
         "下面代码会打印什么？\\n\\n    for (int i = 0; i < 5; i++)\\n    {\\n        if (i == 2) continue;\\n        if (i == 4) break;\\n        Console.Write(i);\\n    }",
         "continue 跳过本次（i=2 不打印）；break 结束整个循环（i=4 时直接跳出）。\\n所以打印 0、1、3。二者区别是必考点。",
         "013"},

        new[]{"choice", "流程控制", "2",
         "switch 语句里每个 case 末尾通常要写什么？\\n\\n  A. break;\\n  B. continue;\\n  C. 什么都不用\\n  D. return;",
         "C# **不允许 case 之间「掉落」** —— 每个分支必须以 break（或 return/goto）结束，否则编译不过。\\n这一点和 Java 不同：Java 忘了 break 会继续跑下一个 case（很隐蔽的 bug），C# 直接不让你编译。",
         "1"},

        // ═══════ 数组与集合 ═══════
        new[]{"output", "数组", "2",
         "下面代码会打印什么？\\n\\n    int[] a = { 10, 20, 30 };\\n    Console.WriteLine(a.Length);",
         "数组用 Length（属性，没有括号）。\\n注意列表 List 用的是 **Count** 而不是 Length —— 这个不一致常让人写错，考试也爱考。",
         "3"},

        new[]{"output", "数组", "3",
         "下面代码会打印什么？\\n\\n    int[] a = { 10, 20, 30 };\\n    Console.WriteLine(a[a.Length - 1]);",
         "下标从 0 开始，所以最后一个元素是 a[Length-1] = a[2] = 30。\\nC# 8 之后也能写 a[^1]（从末尾数），更不容易算错。",
         "30"},

        new[]{"choice", "集合", "2",
         "List<int> 和 int[] 的主要区别是？\\n\\n  A. List 可以动态增删元素，数组长度固定\\n  B. List 更快\\n  C. 数组能存对象，List 只能存数字\\n  D. 没有区别",
         "数组**长度固定**（创建后不能加减元素），List 可以动态增删。\\n需要边遍历边加元素时必须用 List —— 用数组会在运行时抛异常。",
         "1"},

        // ═══════ 方法与参数 ═══════
        new[]{"output", "方法", "3",
         "下面代码会打印什么？\\n\\n    static int Add(int a, int b = 10) { return a + b; }\\n    Console.WriteLine(Add(5));",
         "b = 10 是**默认参数**：不传就用 10，所以 5 + 10 = 15。\\n默认参数必须放在**没有默认值的参数后面**，否则编译不过。",
         "15"},

        new[]{"choice", "方法", "3",
         "方法签名里的 out 关键字表示什么？\\n\\n  A. 这个参数用于输出结果，方法内部必须给它赋值\\n  B. 这个参数可以不传\\n  C. 这个参数是只读的\\n  D. 这个参数会被复制一份",
         "out 表示这个参数用来**带出结果**（方法内部必须给它赋值，否则编译不过）。\\nTryParse 就是这么用的：返回 bool 表示成功与否，数字通过 out 带出来。",
         "1"},
    };

    // ── 写代码题：题号 → (题干, 解释, 入口方法名) ──
    // 学生的方法**返回字符串**，判分时比对输出。
    // 为什么不用反射拿真正的值：学生代码在**另一个进程**里编译运行，
    // 跨进程只能传字符串。详见 LearnCode/CSharpRunner.cs 顶部说明。
    static readonly List<string[]> 代码题 = new List<string[]>
    {
        new[]{"是偶数", "写一个方法 是偶数(int n)：n 是偶数返回 \"是\"，否则返回 \"不是\"。",
         "用 n % 2 == 0 判断余数。注意 C# 里负数取余得负数（-3 % 2 是 -1），\\n但 -1 != 0 所以判断结果仍然正确 —— 和 Python 的取余规则不同，跨语言时要留意。",
         "是偶数"},

        new[]{"求和", "写一个方法 求和(int[] a)：返回所有元素之和（用字符串返回）；空数组返回 \"0\"。",
         "思路：遍历累加。初始值必须是 0 而不是第一个元素 —— 否则空数组会下标越界。\\n也可以用 LINQ 的 a.Sum()，但考试通常要求手写循环。",
         "求和"},
    };

    // ── 写代码题的测试用例：入口名 → 若干 (说明, 调用表达式, 期望输出) ──
    static readonly Dictionary<string, (string Desc, string Call, string Expect)[]> TESTS =
        new Dictionary<string, (string, string, string)[]>
    {
        {
            "是偶数", new (string, string, string)[]
            {
                ("是偶数(4) 应得 是", "是偶数(4)", "是"),
                ("是偶数(7) 应得 不是", "是偶数(7)", "不是"),
                ("是偶数(0) 应得 是", "是偶数(0)", "是"),
                ("是偶数(-3) 应得 不是", "是偶数(-3)", "不是"),
            }
        },
        {
            "求和", new (string, string, string)[]
            {
                ("求和({1,2,3}) 应得 6", "求和(new int[]{1,2,3})", "6"),
                ("求和(空数组) 应得 0", "求和(new int[0])", "0"),
                ("求和({5}) 应得 5", "求和(new int[]{5})", "5"),
                ("求和({-1,1,10}) 应得 10", "求和(new int[]{-1,1,10})", "10"),
            }
        },
    };

    /// <summary>把题目按行协议打印出来。改动题目不需要动这里。</summary>
    public static void Dump()
    {
        // E  名称  简称  级别  依据  时长
        Console.WriteLine("E\tC# 编程能力练习（二级水平）\tC#\t二级\t按公开考纲自编练习题（非真题）\t120");

        for (int i = 0; i < 普通题.Count; i++)
        {
            var q = 普通题[i];
            Console.WriteLine("Q\tC#-" + (i + 1).ToString("000") + "\t" + q[0] + "\t" + q[1]
                              + "\t" + q[2] + "\t" + Esc(q[3]) + "\t" + Esc(q[4]) + "\t" + Esc(q[5]));
        }

        foreach (var c in 代码题)
        {
            string entry = c[3];
            Console.WriteLine("K\t" + entry + "\t" + Esc(c[1]) + "\t" + Esc(c[2]));
            foreach (var t in TESTS[entry])
                Console.WriteLine("T\t" + entry + "\t" + Esc(t.Desc) + "\t" + Esc(t.Call) + "\t" + Esc(t.Expect));
        }
    }

    /// <summary>换行和制表符要转义 —— 否则会破坏按行解析的协议。</summary>
    static string Esc(string s) =>
        s.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "").Replace("\t", "\\t");
}
