// 第 04 课：循环 —— 让程序重复做事，并且重复得**正好**
//
// ══ 这一课要教什么 ══
// 循环是"程序能干活"的开始：求和、计数、查找都要靠它。
// 但循环的错法和前面的都不一样 —— 它**不会报错，也不会崩**，
// 只是把结果算错一点点，而那一点点很难看出来：
//
//   差一错误（off-by-one）：for (int i = 1; i < n; i++) 少跑一次。
//     算 1..10 的和，它给你 45，看上去像个正常答案。
//   累加变量没给初值：int 和; 里面是内存里的残留值。
//     这次碰巧是 0，结果"对了" —— 但它随时可能变成别的数。
//
// 这两条是这一课的核心检查点（第 2/3/4 条覆盖差一，第 5 条专门盯未初始化）。
//
// ══ 检查设计说明（给维护者）══
// 为什么差一错误要用**多组输入**测：
//   · i < n 的写法在 n = 0 时和正确写法给出一样的结果（都不跑），
//     所以只测一个大数（比如 n = 10）看不出病根，只能看出"差了 10"。
//   · 用例里放了 n = 0 和 n = 1 这两个极端 ——
//     n = 1 时正确答案是 1，i < n 会给出 0，一眼就能看出是"少跑了一次"。
//
// 为什么未初始化要专门查编译器警告，而不是看输出：
//   实测 g++ -O1 -Wall 对 `int 和; 和 += i;` 会给出 -Wmaybe-uninitialized，
//   而**程序碰巧算出正确答案**（我第一次跑就打印了 15）。
//   只看输出的话，这个真实的 bug 会被判通过 —— 那是最坏的教学反馈。
//   CppRunner 已经把这类警告单独拎出来（HasCorrectnessWarning），这里直接用。
//
// 第 6 条是**有意**的"练到了没有"检查：break/continue 用别的写法也能达到同样结果，
// 但这一课的题目就是要练它们四个关键字（for / while / break / continue）。

using System.Linq;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.CppLessons;

[课程("04", "第 04 课：循环",
    "1. int 求和(int n)：1+2+…+n，n<=0 返回 0。用 for\n" +
    "2. int 数偶数(int n)：1..n 里偶数的个数，n<=0 返回 0。用 while，用 continue 跳过奇数\n" +
    "3. int 第一个因子(int n)：n 的最小因子（>=2）；n<2 返回 -1；找不到就返回 n。用 for，找到后用 break 跳出")]
public static class 第04课
{
    public static string 模板 => """
// 第 04 课：循环
//
// 要做：写三个函数（不用写 main，检查时会自动调用它们）
//
//   1. int 求和(int n)                    ← 用 **for**
//        1 + 2 + ... + n
//        n <= 0 时返回 0
//
//   2. int 数偶数(int n)                  ← 用 **while**，用 **continue** 跳过奇数
//        1..n 里一共有多少个偶数
//        n <= 0 时返回 0
//
//   3. int 第一个因子(int n)               ← 用 **for**，找到后用 **break** 跳出
//        n 的最小因子（能整除 n、并且 >= 2 的最小的数）
//          9 → 3      10 → 2      4 → 2
//        n < 2 时返回 -1
//        找不到因子（说明 n 是质数）就返回 n 本身：  7 → 7
//
// 三个必须知道的点：
//
//   · 累加变量必须给初值：  int 和 = 0;
//     C++ 里 int 和; 装的是**内存里的残留值**，不是 0。
//     这一点和 Python、C# 都不同（那边一定会给你一个 0）。
//     没给初值的累加可能"这次碰巧对了"，但没有任何保证 —— 编译器 -Wall
//     会给一条未初始化警告，看到它就必须改。
//
//   · 差一错误（off-by-one）：for (int i = 1; i <= n; i++) 里的 <=
//     写成 < 就会少跑一次。写循环时先问自己两句：
//         n = 1 时，循环该跑几次？   n = 0 时，该不该跑？
//
//   · 三种循环的分工：
//         for        次数已知（从 1 走到 n）—— 最常用
//         while      以条件为主（不知道要跑几次）
//         do-while   **至少执行一次**（先做一遍再看条件）
//     break    立刻跳出整个循环
//     continue 跳过这一轮的剩余部分，直接进入下一轮
//     ⚠ 用 continue 时，别忘了让循环变量往前走，否则会**死循环**。
//
// 写完后运行：dotnet run --project csharp/cpp-lessons 检查 04

// ── 在下面写你的代码 ──
// 提示：只需要写函数。形如：
//
//   int 求和(int n) {
//       int 和 = 0;                        // ← 初值不能少
//       for (int i = 1; i <= n; i++) {
//           和 += i;
//       }
//       return 和;
//   }

""";

    public static string 参考答案 => """
#include <iostream>

int 求和(int n) {
    int 和 = 0;                      // ← 初值不能少，C++ 不会帮你清零
    for (int i = 1; i <= n; i++) {   // ← 是 <= n，不是 < n
        和 += i;
    }
    return 和;
}

int 数偶数(int n) {
    int 个数 = 0;
    int i = 0;
    while (i < n) {
        i++;                         // ← 先让 i 前进，continue 才不会死循环
        if (i % 2 != 0) {
            continue;                // 奇数跳过，不计数
        }
        个数++;
    }
    return 个数;
}

int 第一个因子(int n) {
    if (n < 2) {
        return -1;
    }

    int 结果 = n;                    // n 是质数的话，答案就是 n 自己
    for (int i = 2; i < n; i++) {
        if (n % i == 0) {
            结果 = i;
            break;                   // 找到最小的就跳出，不用再找了
        }
    }
    return 结果;
}
""";

    // ══════════════════ 检查 ══════════════════

    [检查说明("三个函数都写好了吗（名字、参数类型、返回类型要对上）")]
    public static CheckResult 检查_三个函数都在(string 代码)
    {
        string 调用点 =
            "    std::cout << 求和(5) << \"\\n\";\n" +
            "    std::cout << 数偶数(10) << \"\\n\";\n" +
            "    std::cout << 第一个因子(9) << \"\\n\";\n";

        var run = CppCheck.编译并运行(代码, null, 调用点);
        if (run.Raw.CompileFailed)
        {
            string 信息 = run.编译信息;
            if (信息.Contains("求和") || 信息.Contains("数偶数") || 信息.Contains("第一个因子"))
                return CheckResult.不过(
                    "检查用的调用代码编译不过 —— 多半是**函数的名字或类型**和题目不一样。\n" +
                    "      题目要的三个函数，签名一字不差是这样：\n" +
                    "          int 求和(int n)\n" +
                    "          int 数偶数(int n)\n" +
                    "          int 第一个因子(int n)\n" +
                    "      三个地方容易写错：\n" +
                    "        · 返回类型是 int（结果是数，不是打印）\n" +
                    "        · 用 return 把结果交出去，不要在函数里 cout\n" +
                    "        · { } 要配对，少一个会连带报出一串看不懂的错\n" +
                    "      编译器原文（前面几行）：\n" + 缩进(前几行(信息)));

            return CppCheck.编译失败说明(run);
        }

        if (run.Raw.Error != null)
            return CheckResult.不过("函数调用了，但程序没能跑完：\n      " + 缩进(run.Raw.Error));

        return CheckResult.过("三个函数都能正常调用");
    }

    [检查说明("求和：1 到 n 的和对不对（差一错误在这里最藏不住）")]
    public static CheckResult 检查_求和(string 代码)
    {
        // n = 0 和 n = 1 是故意放的：
        //   n = 0 时，i < n 和 i <= n 都不跑，看不出问题；
        //   n = 1 时，正确答案是 1，i < n 会给出 0 —— 少跑一次立刻现形。
        return CppCheck.逐个用例(代码,
            new CppCheck.用例("求和(0)（一个都不加）", "求和(0)", "0"),
            new CppCheck.用例("求和(1)（只加 1）", "求和(1)", "1"),
            new CppCheck.用例("求和(2)", "求和(2)", "3"),
            new CppCheck.用例("求和(3)", "求和(3)", "6"),
            new CppCheck.用例("求和(5)", "求和(5)", "15"),
            new CppCheck.用例("求和(10)", "求和(10)", "55"),
            new CppCheck.用例("求和(-3)（负数，直接返回 0）", "求和(-3)", "0"));
    }

    [检查说明("数偶数：while 循环数出来的个数对不对")]
    public static CheckResult 检查_数偶数(string 代码)
    {
        return CppCheck.逐个用例(代码,
            new CppCheck.用例("数偶数(0)", "数偶数(0)", "0"),
            new CppCheck.用例("数偶数(1)", "数偶数(1)", "0"),
            new CppCheck.用例("数偶数(2)（只有 2）", "数偶数(2)", "1"),
            new CppCheck.用例("数偶数(3)（只有 2）", "数偶数(3)", "1"),
            new CppCheck.用例("数偶数(4)（2、4）", "数偶数(4)", "2"),
            new CppCheck.用例("数偶数(10)", "数偶数(10)", "5"),
            new CppCheck.用例("数偶数(11)（11 是奇数，不增）", "数偶数(11)", "5"));
    }

    [检查说明("第一个因子：break 用得对不对（质数和 n<2 都要照顾到）")]
    public static CheckResult 检查_第一个因子(string 代码)
    {
        return CppCheck.逐个用例(代码,
            new CppCheck.用例("第一个因子(0)（小于 2）", "第一个因子(0)", "-1"),
            new CppCheck.用例("第一个因子(1)（小于 2）", "第一个因子(1)", "-1"),
            new CppCheck.用例("第一个因子(-5)（小于 2）", "第一个因子(-5)", "-1"),
            new CppCheck.用例("第一个因子(4)", "第一个因子(4)", "2"),
            new CppCheck.用例("第一个因子(10)", "第一个因子(10)", "2"),
            new CppCheck.用例("第一个因子(9)", "第一个因子(9)", "3"),
            new CppCheck.用例("第一个因子(49)", "第一个因子(49)", "7"),
            new CppCheck.用例("第一个因子(7)（质数，返回自己）", "第一个因子(7)", "7"));
    }

    [检查说明("累加变量给了初值吗（C++ 的局部变量里是随机值）")]
    public static CheckResult 检查_累加变量给了初值(string 代码)
    {
        // ⚠ 这一条是本课的核心检查点之一。
        //
        // 为什么不看输出：`int 和;` 然后累加，**这次很可能碰巧算出正确答案**
        //   （我自己试的时候 g++ -O1 就打印了 15）。
        // 只看结果会把一个真实的 bug 判成通过 —— 所以这里看**编译器警告**。
        // g++ -O1 -Wall 对这种情况会给 -Wmaybe-uninitialized，
        // CppRunner 已经把这类影响正确性的警告单独标出来了。
        var run = CppCheck.编译并运行(代码, null,
            "    std::cout << 求和(5) << \"\\n\";\n" +
            "    std::cout << 数偶数(10) << \"\\n\";\n" +
            "    std::cout << 第一个因子(9) << \"\\n\";\n");

        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);

        if (run.Raw.HasCorrectnessWarning && !run.编译信息.Contains("uninitialized"))
            // 别的几类正确性警告（越界、返回局部变量地址）不归这一课管，
            // 交给统一的翻译器说 —— 免得把"没给初值"的帽子扣错。
            return CheckResult.不过(
                "编译器发现了隐患：\n" + ExplainCpp.ExplainWarning(run.编译信息) +
                "\n      编译器原文：\n" + 缩进(警告行(run.编译信息)));

        if (run.Raw.HasCorrectnessWarning)
            return CheckResult.不过(
                "编译器发现了一个**真实隐患**：你有变量**声明了却没给初值**，就拿去累加了。\n" +
                "      比如这样：\n" +
                "          int 和;                              // ← 里面是随机值\n" +
                "          for (int i = 1; i <= n; i++) 和 += i;\n" +
                "      C++ 里局部变量**不会自动清零** —— int 和; 装的是内存里剩下的东西。\n" +
                "      这次它可能碰巧是 0（结果看起来还对），下次就不是了。\n" +
                "      这一点和 Python、C# 都不同：那两边的变量一定有个确定的值，\n" +
                "      C++ 没有这个保证 —— 用它就是在赌运气。\n" +
                "      改法：声明的时候就给初值。\n" +
                "          int 和 = 0;                  // ← 累加（+=）用 0\n" +
                "          int 积 = 1;                  // ← 累乘（*=）用 1\n" +
                "          int 最大 = 第一个数;          // ← 找最大值用第一个元素\n" +
                "      编译器原文（行号是套上检查骨架之后的位置，不必去数你自己的文件 ——\n" +
                "      看变量名就够了）：\n" + 缩进(警告行(run.编译信息)));

        return CheckResult.过("累加变量都给了初值，编译器没有意见");
    }

    [检查说明("for / while / break / continue 这一课要求都亲手写一遍")]
    public static CheckResult 检查_循环关键字都练到了(string 代码)
    {
        // 为什么用"看源码"：break / continue 换成别的写法（比如 return 直接返回、
        // if 包住计数）也能得到同样的结果，但这一课的题目就是要练这四个。
        // 这是**有意**的一条"练到了没有"的检查 —— 题目里已经写明了分工。
        // 先把注释去掉，免得注释里的 while 把检查骗过去。
        string 净 = 去掉注释(代码);
        var 缺 = new System.Collections.Generic.List<string>();
        if (!Regex.IsMatch(净, @"\bfor\b")) 缺.Add("for（求和、第一个因子要用）");
        if (!Regex.IsMatch(净, @"\bwhile\b")) 缺.Add("while（数偶数要用）");
        if (!Regex.IsMatch(净, @"\bbreak\b")) 缺.Add("break（第一个因子找到后要跳出循环）");
        if (!Regex.IsMatch(净, @"\bcontinue\b")) 缺.Add("continue（数偶数里跳过奇数）");

        if (缺.Count == 0)
            return CheckResult.过("for、while、break、continue 都用上了");

        return CheckResult.不过(
            "这一课要求四个关键字都亲手写一遍，你的代码里还缺：\n" +
            string.Join("", 缺.Select(x => "        · " + x + "\n")) +
            "      题目里的分工是这样：\n" +
            "          求和       用 for            （次数已知：1 走到 n）\n" +
            "          数偶数     用 while          （用 continue 跳过奇数）\n" +
            "          第一个因子  找到因子用 break   （跳出整个循环，不再往下找）\n" +
            "      如果你用了别的写法（比如直接 return、或者用 if 包住计数），\n" +
            "      结果可能是对的 —— 但这一课就是要练这四个开关，改过来再交。\n" +
            "      顺便记住它俩的区别：\n" +
            "          break    → 整个循环到此为止\n" +
            "          continue → 只跳过这一轮剩下的部分，下一轮照跑\n" +
            "      ⚠ 用 continue 时最容易踩的坑：忘了让循环变量往前走 → 死循环。");
    }

    // ══════════════════ 工具 ══════════════════

    /// <summary>
    /// 只挑出编译器输出里带 warning 的那几行（未初始化的位置就在里面），并做两处清理：
    ///   · g++ 对**中文标识符**会输出 '\U0000548c' 这种转义，学生根本看不懂 —— 换回真正的字
    ///   · 临时目录的长路径没有意义 —— 只留 main.cpp:行号
    /// </summary>
    private static string 警告行(string 编译信息)
    {
        var 行 = 编译信息.Replace("\r\n", "\n").Split('\n')
                         .Where(l => l.Contains("warning:") || l.Contains("note:"))
                         .Take(4).Select(清理编译器行).ToList();
        return 行.Count > 0 ? string.Join("\n", 行) : 编译信息;
    }

    private static string 清理编译器行(string 行)
    {
        行 = Regex.Replace(行, @"\\U([0-9a-fA-F]{8})", m =>
            char.ConvertFromUtf32(System.Convert.ToInt32(m.Groups[1].Value, 16)));
        行 = Regex.Replace(行, @"\\u([0-9a-fA-F]{4})", m =>
            ((char)System.Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
        return Regex.Replace(行, @"[A-Za-z]:\\[^\s:]*\\main\.cpp", "main.cpp");
    }

    /// <summary>去掉注释 —— 免得注释里的 while / break 被当成代码。</summary>
    private static string 去掉注释(string 代码)
    {
        string s = Regex.Replace(代码, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(s, @"//[^\n]*", " ");
    }

    private static string 前几行(string s)
    {
        var 行 = s.Replace("\r\n", "\n").Split('\n')
                   .Where(l => l.Trim().Length > 0).Take(6).ToList();
        return 行.Count > 0 ? string.Join("\n", 行) : "（编译器没有输出）";
    }

    private static string 缩进(string s) => s.Trim().Replace("\n", "\n      ");
}
