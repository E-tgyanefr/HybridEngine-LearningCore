// Java 题库：基础与编程能力
//
// ══ 关于题目来源 ══
// 与 Python、C#、C++ 题库同一原则：**按公开知识点自编练习题，不是真题**。
// 目的是让你具备该等级的能力，而不是记住某年的答案。
//
// ══ Java 的考点为什么和别的语言不一样 ══
// Java 的语法长得像 C#，但**让新手卡住的地方完全不同**：
//   · 一切都要写在类里 —— 没有「游离的函数」这回事
//   · main 是 static，所以不能直接调用非 static 的方法（新手第一大困惑）
//   · 公共类名必须和文件名一致（编译器直接拒绝）
//   · 字符串比较**必须用 .equals()**，用 == 比的是「是不是同一个对象」
//   · 整数除法照样截断，而且**不报错**（静默陷阱）
//   · 包装类缓存导致 == 的行为随数值变化（127 和 128 结果不同）
//
// 最后两条是 Java 特有、别的语言没有的坑，所以这里的重点放在这些上。
//
// ══ ⚠ 写题库时的转义规则（我在这里摔过三次）══
//
// 这个文件是 **C# 源码**，但里面的字符串要描述 **Java 代码**。两套转义会撞车：
//
//     想要 C# 字符串里出现    就写
//     ─────────────────────  ──────
//     一个真实换行（协议用）    \n        ← 一个反斜杠
//     一个双引号              \"        ← 一个反斜杠
//     一个字面反斜杠           \\
//
// 我犯的错是写了 \\"（两个反斜杠+引号）—— C# 把它解析成「一个字面反斜杠」
// 然后**字符串就结束了**，后面全乱。**不是 \" 就是 \\，没有 \\" 这种组合。**
//
// 另外：题库文本里不要直接写英文双引号，用中文「」——
// 视觉上更清楚，也不会和 C# 的字符串定界符混淆。

using System;
using System.Collections.Generic;

public static class JavaBank
{
    // 类型, 主题, 难度, 题干, 解释, 答案
    // choice 的答案是选项序号（1 起）；output 的答案是期望输出
    static readonly List<string[]> 题目 = new List<string[]>
    {
        // ═══════ 基本结构 ═══════
        new[]{"choice", "基本结构", "1",
            "Java 程序的入口方法是哪个？\n\n  A. public static void main(String[] args)\n  B. public void main()\n  C. static main()\n  D. public static main()",
            "四个部分缺一不可：public（公开）、static（不用建对象就能调用）、void（没有返回值）、String[] args（命令行参数）。\n漏掉 static 的话，JVM 没法在不创建对象的情况下调用它，程序根本起不来。",
            "1"},

        new[]{"choice", "基本结构", "2",
            "Java 里所有代码必须写在哪里？\n\n  A. 类里面\n  B. 文件顶层就行\n  C. 函数里面\n  D. 随便哪里",
            "**Java 没有「游离的函数」** —— 每个方法都必须在某个类里。\n这一点和 C++（可以写全局函数）、Python（可以写顶层函数）都不同。\n所以 Java 的第一个概念不是「变量」而是「类」，因为不写类什么都写不了。",
            "1"},

        new[]{"output", "基本结构", "2",
            "下面代码会输出什么？\n\n    int a = 7;\n    int b = 2;\n    System.out.println(a / b);",
            "**整数除以整数，结果还是整数** —— 小数部分被直接丢掉。7 / 2 = 3。\n想要 3.5 得让至少一边是浮点：a / 2.0 或 (double)a / b。\n⚠ Java 这里**不报错也不警告**，是静默的语义陷阱。",
            "3"},

        new[]{"output", "基本结构", "2",
            "下面代码会输出什么？\n\n    System.out.println(1 + 2 + \"a\");",
            "从左到右算：1 + 2 先算成 3，再和 \"a\" 拼接得 \"3a\"。\n如果写 \"a\" + 1 + 2，先得 \"a1\" 再得 \"a12\"。\n**顺序会改变结果**，这是常考的坑。",
            "3a"},

        // ═══════ static（Java 特有的头号困惑）═══════
        new[]{"choice", "static", "3",
            "在 main 方法里直接调用一个**没写 static** 的方法，会怎样？\n\n  A. 编译错误：无法从静态上下文中引用非静态方法\n  B. 正常运行\n  C. 运行时抛异常\n  D. 自动帮你创建对象",
            "**这是 Java 新手最常见的困惑，没有之一。**\nmain 是 static 的，它属于类而不属于某个对象。\n而没写 static 的方法属于**对象** —— 你得先 new 一个对象才能调用它。\n\n两种改法：给那个方法加 static；或者先创建对象：new 类名().方法()。",
            "1"},

        new[]{"output", "static", "3",
            "下面代码能编译吗？会输出什么？\n\n    public class Main {\n        static int 加倍(int n) { return n * 2; }\n        public static void main(String[] a) {\n            System.out.println(加倍(3));\n        }\n    }",
            "能编译，输出 6。\n因为 加倍 也加了 static —— 静态方法之间可以互相直接调用，不需要对象。\n**记住这个对照**：static 调 static 没问题；static 调非 static 必须借对象。",
            "6"},

        // ═══════ 字符串（Java 特有的大坑）═══════
        new[]{"choice", "字符串", "3",
            "判断两个字符串内容是否相同，应该怎么写？\n\n  A. s1.equals(s2)\n  B. s1 == s2\n  C. s1.compare(s2)\n  D. s1 = s2",
            "**== 比的是「是不是同一个对象」，不是「内容一样不一样」。**\n\n    String a = \"hi\";\n    String b = new String(\"hi\");\n    a == b        // false！内容一样但不是一个对象\n    a.equals(b)   // true\n\n这是 Java 最著名的坑，面试必问、新手必踩。\n（顺便：C# 里 == 对字符串做了特殊处理，行为**和 Java 不同**，跨语言时要留意。）",
            "1"},

        new[]{"output", "字符串", "2",
            "下面代码会输出什么？\n\n    String s = \"Hello\";\n    System.out.println(s.length());",
            "length() 是**方法，要加括号**。\n注意和数组对比：数组用 a.length（**属性，不加括号**）。\n这个不一致是 Java 的历史遗留，考试常考。",
            "5"},

        new[]{"output", "字符串", "2",
            "下面代码会输出什么？\n\n    System.out.println(\"Hello\".charAt(1));",
            "charAt(1) 取下标 1 的字符，也就是第二个字符 e（下标从 0 开始）。\n注意返回的是 char 类型，不能直接当字符串用。",
            "e"},

        new[]{"choice", "字符串", "2",
            "下面哪个是**合法**的字符串写法？\n\n  A. String s = \"abc\";\n  B. String s = 'abc';\n  C. string s = \"abc\";\n  D. String s = abc;",
            "Java 的字符串类型是**大写开头的 String**（它是一个类，不是基本类型）。\n单引号 'a' 是 char（单个字符），双引号 \"abc\" 才是字符串。\n不加引号的 abc 会被当成变量名去找，报 cannot find symbol。",
            "1"},

        // ═══════ 数组 ═══════
        new[]{"output", "数组", "2",
            "下面代码会输出什么？\n\n    int[] a = {10, 20, 30};\n    System.out.println(a.length);",
            "数组的长度是 **a.length（属性，不加括号）**。\n而 String 的长度是 s.length()（方法，要加括号）。\n这个不一致很容易写错，考试也爱考。",
            "3"},

        new[]{"output", "数组", "3",
            "下面代码会输出什么？\n\n    int[] a = {10, 20, 30};\n    System.out.println(a[a.length - 1]);",
            "下标从 0 开始，所以最后一个元素是 a[length-1] = a[2] = 30。\n**Java 会检查下标**：写 a[3] 会抛 ArrayIndexOutOfBoundsException，\n不会像 C++ 那样悄悄读到垃圾值。这一点 Java 比 C++ 安全。",
            "30"},

        new[]{"choice", "数组", "3",
            "int[] a = new int[3]; 之后，a[0] 的值是什么？\n\n  A. 0\n  B. 随机值\n  C. null\n  D. 编译错误",
            "**Java 会把数组元素自动初始化为该类型的默认值**：int 是 0，boolean 是 false，对象是 null。\n这一点和 C++ 相反 —— C++ 里 int a[3]; 的元素是**随机值**（那是最容易踩的坑之一）。\n所以 Java 更安全，但也更啰嗦（必须写 new）。",
            "1"},

        // ═══════ 类型与转换 ═══════
        new[]{"choice", "类型", "3",
            "Java 里把 double 赋给 int 会发生什么？\n\n  A. 编译错误：可能有精度损失\n  B. 自动截断小数\n  C. 自动四舍五入\n  D. 运行时抛异常",
            "**Java 不允许隐式窄化转换** —— double 装得下的东西 int 装不下，编译器直接拒绝。\n必须显式写 (int)d，而且那是**截断**不是四舍五入：\n    (int)3.9  →  3（不是 4）\n想要四舍五入用 Math.round(3.9)。",
            "1"},

        new[]{"output", "类型", "3",
            "下面代码会输出什么？\n\n    double d = 3.9;\n    int i = (int)d;\n    System.out.println(i);",
            "(int) 是**截断**不是四舍五入 —— 3.9 变 3，不是 4。\n想四舍五入要用 Math.round(3.9) 得 4。\n这个区别在算钱的时候很要命。",
            "3"},

        new[]{"choice", "类型", "3",
            "Integer a = 127; Integer b = 127; 那么 a == b 是什么？\n\n  A. true（-128~127 有缓存）\n  B. false\n  C. 编译错误\n  D. 取决于运气",
            "**自动装箱 + 缓存**：Java 把 -128~127 的 Integer 缓存复用，所以这个范围内 a == b 是 true。\n但把 127 换成 128，a == b 就变成 **false** 了 —— 超出缓存范围会新建对象。\n\n所以：**包装类之间永远用 .equals() 比，不要用 ==**。\n这是 Java 面试的经典陷阱，知道的人不多但确实会咬人。",
            "1"},

        // ═══════ 面向对象（Java 的核心）═══════
        new[]{"choice", "面向对象", "2",
            "创建对象用的关键字是哪个？\n\n  A. new\n  B. create\n  C. make\n  D. object",
            "new 后面跟**构造方法**：new 类名()。\nJava 里对象一定要 new 出来（字符串字面量这种特例除外）。\n这和 C++ 不同 —— C++ 可以在栈上直接声明对象：类名 变量;",
            "1"},

        new[]{"choice", "面向对象", "3",
            "同一个类里，构造方法的名字应该是什么？\n\n  A. 和类名完全相同\n  B. 叫 constructor\n  C. 叫 init\n  D. 随便起",
            "构造方法的名字**必须和类名完全一样**，而且**没有返回类型**（连 void 都不写）。\n写成 void 类名() 就变成普通方法了，不会被当构造方法用 —— 这个错误很隐蔽。",
            "1"},

        // ═══════ 异常 ═══════
        new[]{"choice", "异常", "3",
            "Integer.parseInt(\"abc\") 会怎样？\n\n  A. 抛 NumberFormatException\n  B. 返回 0\n  C. 返回 null\n  D. 编译错误",
            "**抛 NumberFormatException**（运行时异常）。\n这是处理用户输入时最常见的问题 —— 用户什么都可能输。\n怎么改：用 try/catch 接住，或者先校验格式。\n\n对比：C# 的 int.Parse 也抛异常，但 C# 提供了 TryParse（不抛）；\nJava 没有等价的，得自己 try/catch。",
            "1"},
    };

    // ── 写代码题：入口名 → (题干, 解释) ──
    static readonly List<string[]> 代码题 = new List<string[]>
    {
        new[]{"平方和", "写一个 static 方法 平方和(int n)：返回 1² + 2² + ... + n²（n 为 0 时返回 0）。",
            "用 for 循环累加。累加变量要初始化为 0。\n方法要加 **static**，否则 main 里调用不了它 —— 这是本课最容易卡住的地方。",
            "平方和"},

        new[]{"反转", "写一个 static 方法 反转(String s)：返回把字符串倒过来的结果。例如 反转(\"abc\") 得到 \"cba\"。",
            "两种写法：\n  · 用 StringBuilder：new StringBuilder(s).reverse().toString()\n  · 用循环从后往前取字符拼接\n空字符串和单字符是边界情况，两种写法都能天然处理。",
            "反转"},
    };

    // ── 代码题测试：入口名 → (说明, 调用表达式, 期望输出) ──
    static readonly Dictionary<string, (string Desc, string Call, string Expect)[]> 测试 =
        new Dictionary<string, (string, string, string)[]>
    {
        {
            "平方和", new (string, string, string)[]
            {
                ("平方和(3) 应得 14", "平方和(3)", "14"),
                ("平方和(1) 应得 1", "平方和(1)", "1"),
                ("平方和(0) 应得 0", "平方和(0)", "0"),
                ("平方和(10) 应得 385", "平方和(10)", "385"),
            }
        },
        {
            "反转", new (string, string, string)[]
            {
                ("反转 abc 应得 cba", "反转(\"abc\")", "cba"),
                ("反转空串 应得空串", "反转(\"\")", ""),
                ("反转单字 应得原字", "反转(\"中\")", "中"),
                ("反转 你好ab 应得 ba好你", "反转(\"你好ab\")", "ba好你"),
            }
        },
    };

    /// <summary>
    /// 代码题的程序骨架：学生的 static 方法会被放到 /*学生代码*/ 的位置。
    ///
    /// ⚠ 类名必须是 Main —— Java 的公共类名必须和文件名一致，
    ///   而 JavaRunner 固定用 Main.java。这一条和 C++ 不同（C++ 无所谓名字）。
    /// </summary>
    static string 骨架(string 入口, (string Desc, string Call, string Expect)[] 用例)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("public class Main {\n");
        sb.Append("    /*学生代码*/\n\n");
        sb.Append("    public static void main(String[] args) {\n");
        foreach (var t in 用例)
            sb.Append("        System.out.println(").Append(t.Call).Append(");\n");
        sb.Append("    }\n");
        sb.Append("}\n");
        return sb.ToString();
    }

    /// <summary>把题目按行协议打印出来。改题目不需要动这里。</summary>
    public static void Dump()
    {
        Console.WriteLine("E\tJava 编程能力练习（基础水平）\tJava\t入门\t按公开知识点自编练习题（非真题）\t120");

        for (int i = 0; i < 题目.Count; i++)
        {
            var q = 题目[i];
            Console.WriteLine("Q\tJAVA-" + (i + 1).ToString("000") + "\t" + q[0] + "\t" + q[1]
                              + "\t" + q[2] + "\t" + 转义(q[3]) + "\t" + 转义(q[4]) + "\t" + 转义(q[5]));
        }

        foreach (var c in 代码题)
        {
            string 入口 = c[3];
            Console.WriteLine("K\t" + 入口 + "\t" + 转义(c[1]) + "\t" + 转义(c[2]));
            // W 行用 Base64 —— 骨架里有 Java 的 \" 和换行，普通转义会和协议撞车
            Console.WriteLine("W\t" + 入口 + "\t" + Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(骨架(入口, 测试[入口]))));
            foreach (var t in 测试[入口])
                Console.WriteLine("T\t" + 入口 + "\t" + 转义(t.Desc) + "\t" + 转义(t.Call) + "\t" + 转义(t.Expect));
        }
    }

    static string 转义(string s) =>
        s.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "").Replace("\t", "\\t");
}
