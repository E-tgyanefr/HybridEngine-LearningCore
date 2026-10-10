import io
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
CS = os.path.join(LC, "csharp", "cs-lessons")
S = os.path.join(CS, "Lessons")
A = os.path.join(LC, "cs-solutions")
os.makedirs(S, exist_ok=True)
os.makedirs(A, exist_ok=True)

头 = '''using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

'''

课程 = {}
参考 = {}

课程["第01课_输出"] = ('''[课程("01", "第 01 课：输出与变量 —— 让程序开口说话",
    "写一个方法 `static void 打招呼()`：\\n" +
    "  1. 打印一行：你好，C#\\n" +
    "  2. 定义 int 变量 甲 = 6、乙 = 7\\n" +
    "  3. 打印它们的和（用变量相加，不要写死 13）")]
public static class 第01课
{
    public static string 模板 => """
// 第 01 课：输出与变量
//
// 要做：
//   1. 打印一行：你好，C#
//   2. 定义 int 变量 甲 = 6、int 变量 乙 = 7
//   3. 打印它们的和（用变量相加，不要写死 13）
//
// 记住三件事：
//   · 每条语句末尾要有分号 ;
//   · 打印用 Console.WriteLine(...)
//   · 字符串用双引号 "..."，单个字符才用单引号 '...'
//
// ⚠ C# 是**编译型**语言 —— 但 .NET SDK 自带编译器，
//   所以不用像 C++ / Java 那样另外装 g++ / JDK。
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 01

// ── 在下面写你的方法 ──
// 这一课**不用**写 Main（本课只考你写方法，Main 由课程提供）。

""";

    public static string 参考答案 => """
static void 打招呼()
{
    Console.WriteLine("你好，C#");

    int 甲 = 6;
    int 乙 = 7;
    Console.WriteLine(甲 + 乙);
}
""";

    [检查说明("编译通过，且方法签名对")]
    public static CheckResult 检查_能编译(string 代码)
    {
        var r = CppCheck.写了函数(代码, "打招呼", "打招呼()");
        if (!r.Ok) return r;
        return CheckResult.过();
    }

    [检查说明("打印了「你好，C#」这一行")]
    public static CheckResult 检查_第一行文字(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        var 行 = run.行;
        if (行.Count < 1) return CheckResult.不过("一行都没打印。");
        if (!行[0].Contains("你好，C#"))
            return CheckResult.不过(
                $"第一行应该是「你好，C#」，实际是「{行[0]}」。\\n" +
                "      注意：逗号是中文逗号，C# 是大写 S（C# 里 # 就是普通字符）。");
        return CheckResult.过($"第一行 = {行[0]}");
    }

    [检查说明("第二行是 13（= 6 + 7）")]
    public static CheckResult 检查_和(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        var 行 = run.行;
        if (行.Count < 2)
            return CheckResult.不过($"应该有 2 行输出，实际 {行.Count} 行 —— 是不是忘了打印和？");
        if (行[1].Trim() != "13")
            return CheckResult.不过(
                $"第二行应该是 13，实际是「{行[1].Trim()}」。\\n" +
                "      检查：变量是 6 和 7 吗？真的用 + 相加了吗？");
        return CheckResult.过();
    }

    [检查说明("真的用了变量相加（不是写死 13）")]
    public static CheckResult 检查_不是硬写(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains("13"))
            return CheckResult.不过(
                "代码里出现了字面量 13 —— 那是把答案写死了。\\n" +
                "      要求用两个变量相加：Console.WriteLine(甲 + 乙);");
        if (!去注释.Contains("+"))
            return CheckResult.不过("没看到加法运算。要用 甲 + 乙，不是直接打印一个数。");
        return CheckResult.过();
    }

    [检查说明("用了 Console.WriteLine（不是 Write）")]
    public static CheckResult 检查_用对了打印(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("Console"))
            return CheckResult.不过("没看到 Console —— C# 的打印是 Console.WriteLine(...)");
        // ⚠ Write 不换行 ⇒ 两行输出会挤成一行。新手常犯。
        if (去注释.Contains("Console.Write(") && !去注释.Contains("Console.WriteLine("))
            return CheckResult.不过(
                "你用的是 `Console.Write`（不换行）—— 两行输出会挤成一行。\\n" +
                "      要换行请用 `Console.WriteLine`。");
        return CheckResult.过();
    }

    internal static string 去掉注释(string s)
    {
        var o = new System.Text.StringBuilder();
        bool 块 = false;
        for (int i = 0; i < s.Length; i++)
        {
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '/')
            { while (i < s.Length && s[i] != '\\n') i++; o.Append('\\n'); continue; }
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') { 块 = true; i++; continue; }
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') { 块 = false; i++; continue; }
            if (!块) o.Append(s[i]);
        }
        return o.ToString();
    }
}
''')

参考["第01课_输出"] = '''// 第 01 课参考答案
static void 打招呼()
{
    Console.WriteLine("你好，C#");

    int 甲 = 6;
    int 乙 = 7;
    Console.WriteLine(甲 + 乙);
}
'''

课程["第02课_类型与计算"] = ('''[课程("02", "第 02 课：类型与计算 —— int 和 double 不是一回事",
    "写一个方法 `static double 算面积(int 半径)`：\\n" +
    "  用 3.14159 作为圆周率，返回 圆的面积（圆周率 × 半径 × 半径）")]
public static class 第02课
{
    public static string 模板 => """
// 第 02 课：类型与计算 —— int 和 double 不是一回事
//
// 要做：写一个方法
//
//     static double 算面积(int 半径)
//
//   返回 3.14159 × 半径 × 半径
//
// ⚠ C# 的类型比 Python 严格得多，这一课的重点就是这件事：
//
//   ① **整数除法会丢小数**
//        int a = 7, b = 2;
//        a / b        →  3      （不是 3.5！）
//        (double)a / b → 3.5    （把其中一个转成 double 才行）
//
//   ② **返回类型要匹配**
//        方法声明是 double，就必须真的返回一个小数（或能隐式转成小数的东西）
//
//   ③ `3.14159` 这种带小数点的字面量是 **double**，
//      所以 `3.14159 * 半径 * 半径` 整个式子会提升成 double —— 这是对的做法。
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 02

// ── 在下面写你的方法 ──

""";

    public static string 参考答案 => """
static double 算面积(int 半径)
{
    return 3.14159 * 半径 * 半径;
}
""";

    [检查说明("方法存在，签名对")]
    public static CheckResult 检查_签名(string 代码)
    {
        var r = CppCheck.写了函数(代码, "算面积", "算面积(5)");
        if (!r.Ok) return r;
        return CheckResult.过();
    }

    [检查说明("半径 5 时面积约 78.54（对不同半径都对）")]
    public static CheckResult 检查_正确性(string 代码)
    {
        var 用例 = new[]
        {
            new CppCheck.用例("半径5",  "算面积(5)",  "78.53975"),
            new CppCheck.用例("半径1",  "算面积(1)",  "3.14159"),
            new CppCheck.用例("半径10", "算面积(10)", "314.159"),
        };
        var run = CppCheck.编译并运行(代码, null, "");
        // 用自定义骨架一次跑完所有用例（double 要按数值比，不能逐字符比）
        var 骨架 = """
using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

public static class 学生代码
{
/*学生代码*/
}

public static class 入口
{
    public static void Main()
    {
        Console.WriteLine(算面积(5).ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture));
        Console.WriteLine(算面积(1).ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture));
        Console.WriteLine(算面积(10).ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture));
    }
}
""";
        var rr = CppCheck.编译并运行(代码, 骨架);
        if (!rr.编译通过) return CppCheck.编译失败说明(rr);
        var 行 = rr.行;
        if (行.Count < 3)
            return CheckResult.不过($"应该打印 3 行，实际 {行.Count} 行。");
        for (int i = 0; i < 3; i++)
        {
            if (!double.TryParse(行[i].Trim(), System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out double 实))
                return CheckResult.不过($"第 {i + 1} 个结果不是数字：「{行[i].Trim()}」");
            double 期 = double.Parse(用例[i].期望, System.Globalization.CultureInfo.InvariantCulture);
            if (Math.Abs(实 - 期) > 0.01)
                return CheckResult.不过(
                    $"半径 {用例[i].说明}：应当是 {用例[i].期望}，实际是 {实}。\\n" +
                    "      ⚠ 检查一件事：**是不是整数除法把小数丢了**？\\n" +
                    "         `3.14159 * 半径 * 半径` 里因为有小数，整个式子才是 double；\\n" +
                    "         如果写成 `22 / 7 * 半径 * 半径`，前半段会按 int 算成 3。");
        }
        return CheckResult.过("半径 5 / 1 / 10 都对");
    }

    [检查说明("返回类型是 double（不是 int）")]
    public static CheckResult 检查_返回类型(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains("int 算面积"))
            return CheckResult.不过(
                "返回类型写成了 `int` —— 面积是小数，用 int 会被**截断**（78.53 变成 78）。\\n" +
                "      要写成 `static double 算面积(int 半径)`。");
        return CheckResult.过();
    }

    internal static string 去掉注释(string s)
    {
        var o = new System.Text.StringBuilder();
        bool 块 = false;
        for (int i = 0; i < s.Length; i++)
        {
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '/')
            { while (i < s.Length && s[i] != '\\n') i++; o.Append('\\n'); continue; }
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') { 块 = true; i++; continue; }
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') { 块 = false; i++; continue; }
            if (!块) o.Append(s[i]);
        }
        return o.ToString();
    }
}
''')

参考["第02课_类型与计算"] = '''// 第 02 课参考答案
static double 算面积(int 半径)
{
    // ⚠ 3.14159 带小数点 ⇒ 是 double ⇒ 整个式子提升成 double（不会丢小数）
    return 3.14159 * 半径 * 半径;
}
'''

课程["第03课_条件"] = ('''[课程("03", "第 03 课：条件判断 —— switch 与 if 的选择",
    "写一个方法 `static string 判等级(int 分)`：\\n" +
    "  ≥90 返回 \\"优秀\\"、≥60 返回 \\"及格\\"、否则 \\"不及格\\"\\n" +
    "  再写一个 `static string 星期几(int n)`，用 **switch 表达式**返回 \\"周一\\"…\\"周日\\"")]
public static class 第03课
{
    public static string 模板 => """
// 第 03 课：条件判断 —— switch 与 if 的选择
//
// 要做两个方法：
//
//     static string 判等级(int 分)      // ≥90 优秀 · ≥60 及格 · 否则不及格
//     static string 星期几(int n)       // 1→周一 … 7→周日，其他 → "不是星期"
//
// ══ 这一课的重点：C# 的 switch 与别的语言不一样 ══
//
//  ① **C# 不允许 case 之间"掉落"**
//       每个分支必须以 break / return 结束，否则**编译不过**。
//       （这跟 C / C++ / Java 的旧式 switch 不同 —— 那边忘了 break 会"串烧"。）
//
//  ② C# 有 **switch 表达式**，写起来短得多：
//
//        return n switch
//        {
//            1 => "周一",
//            2 => "周二",
//            _ => "不是星期",     // _ 是"其他所有情况"
//        };
//
//     三行就写完了，而且不会忘 break。
//
//  ⚠ 判等级那题注意顺序：要先判 ≥90 再判 ≥60。
//    反过来的话 95 分也会被判成"及格"。
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 03

// ── 在下面写你的两个方法 ──

""";

    public static string 参考答案 => """
static string 判等级(int 分)
{
    if (分 >= 90) return "优秀";
    else if (分 >= 60) return "及格";
    else return "不及格";
}

static string 星期几(int n)
{
    return n switch
    {
        1 => "周一",
        2 => "周二",
        3 => "周三",
        4 => "周四",
        5 => "周五",
        6 => "周六",
        7 => "周日",
        _ => "不是星期",
    };
}
""";

    [检查说明("两个方法都写了")]
    public static CheckResult 检查_签名(string 代码)
    {
        foreach (var n in new[] { "判等级", "星期几" })
            if (!去掉注释(代码).Contains(n))
                return CheckResult.不过($"没找到「{n}」这个方法。");
        return CheckResult.过();
    }

    [检查说明("判等级 的边界值（95 / 90 / 60 / 59）都对")]
    public static CheckResult 检查_判等级(string 代码)
    {
        var 骨架 = """
using System;

public static class 学生代码
{
/*学生代码*/
}

public static class 入口
{
    public static void Main()
    {
        Console.WriteLine(判等级(95));
        Console.WriteLine(判等级(90));
        Console.WriteLine(判等级(60));
        Console.WriteLine(判等级(59));
    }
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "优秀", "优秀", "及格", "不及格" };
        var 行 = run.行;
        if (行.Count < 4) return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例错了：应当是「{期望[i]}」，实际是「{行[i].Trim()}」。\\n" +
                    "      四个用例是 95 / 90 / 60 / 59 —— 重点看 **90 和 60 这两个边界**。\\n" +
                    "      （`> 90` 和 `>= 90` 的差别就在这里。）");
        return CheckResult.过("95→优秀 · 90→优秀 · 60→及格 · 59→不及格");
    }

    [检查说明("星期几 用 switch（而且没有忘 break 的问题）")]
    public static CheckResult 检查_星期几(string 代码)
    {
        var 骨架 = """
using System;

public static class 学生代码
{
/*学生代码*/
}

public static class 入口
{
    public static void Main()
    {
        for (int i = 1; i <= 8; i++) Console.WriteLine(星期几(i));
    }
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "周一", "周二", "周三", "周四", "周五", "周六", "周日", "不是星期" };
        var 行 = run.行;
        if (行.Count < 8) return CheckResult.不过($"应该打印 8 行，实际 {行.Count} 行。");
        for (int i = 0; i < 8; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"星期几({i + 1}) 应当是「{期望[i]}」，实际是「{行[i].Trim()}」。\\n" +
                    "      ⚠ 注意输入 8 应当返回「不是星期」—— 别忘了 `_ =>` 那一支。");
        return CheckResult.过("1..7 都对，8 走默认分支");
    }

    [检查说明("星期几 真的用了 switch")]
    public static CheckResult 检查_用了switch(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("switch"))
            return CheckResult.不过(
                "「星期几」没有用 switch。\\n" +
                "      这一课就是要你见识 C# 的 switch 表达式：\\n" +
                "          return n switch { 1 => \\"周一\\", ..., _ => \\"不是星期\\" };");
        return CheckResult.过();
    }

    internal static string 去掉注释(string s)
    {
        var o = new System.Text.StringBuilder();
        bool 块 = false;
        for (int i = 0; i < s.Length; i++)
        {
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '/')
            { while (i < s.Length && s[i] != '\\n') i++; o.Append('\\n'); continue; }
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') { 块 = true; i++; continue; }
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') { 块 = false; i++; continue; }
            if (!块) o.Append(s[i]);
        }
        return o.ToString();
    }
}
''')

参考["第03课_条件"] = '''// 第 03 课参考答案
static string 判等级(int 分)
{
    if (分 >= 90) return "优秀";
    else if (分 >= 60) return "及格";
    else return "不及格";
}

static string 星期几(int n)
{
    // C# 的 switch 表达式：不用 break，也漏不掉
    return n switch
    {
        1 => "周一",
        2 => "周二",
        3 => "周三",
        4 => "周四",
        5 => "周五",
        6 => "周六",
        7 => "周日",
        _ => "不是星期",
    };
}
'''

课程["第04课_循环与集合"] = ('''[课程("04", "第 04 课：循环与集合 —— List 与 foreach",
    "写一个方法 `static int 求和(List<int> 数)`：\\n" +
    "  用 foreach 把列表里的数加起来返回\\n" +
    "  再写一个 `static List<int> 只要偶数(List<int> 数)`，返回其中的偶数")]
public static class 第04课
{
    public static string 模板 => """
// 第 04 课：循环与集合 —— List 与 foreach
//
// 要做两个方法：
//
//     static int 求和(List<int> 数)          // 用 foreach 加起来
//     static List<int> 只要偶数(List<int> 数) // 挑出偶数，返回新列表
//
// ══ 这一课的重点：C# 的数组是**定长**的 ══
//
//   ① `int[] a = {1,2,3};` —— 长度固定，**不能加元素**
//      `a[3] = 4;` 直接抛 IndexOutOfRangeException
//
//   ② 要能增删就用 `List<int>`：
//        var 数 = new List<int> { 1, 2, 3 };
//        数.Add(4);              // 可以加
//        Console.WriteLine(数.Count);   // 注意是 Count，不是 Length
//
//   ③ 遍历：
//        foreach (var x in 数) { ... }        // 只读，最常用
//        for (int i = 0; i < 数.Count; i++)   // 需要下标时
//
// ⚠ 两个最容易记混的地方：
//     · 数组用 `.Length`，List 用 `.Count`
//     · 在 `foreach` 里**不能**增删那个集合（会抛异常）——
//       要改就先收集到另一个 List，循环结束后再处理
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 04

// ── 在下面写你的两个方法 ──
// 顶部已经 using System.Collections.Generic;（List 要用它）

""";

    public static string 参考答案 => """
static int 求和(List<int> 数)
{
    int 和 = 0;
    foreach (var x in 数) 和 += x;
    return 和;
}

static List<int> 只要偶数(List<int> 数)
{
    var 出 = new List<int>();
    foreach (var x in 数)
        if (x % 2 == 0) 出.Add(x);
    return 出;
}
""";

    [检查说明("两个方法都写了")]
    public static CheckResult 检查_签名(string 代码)
    {
        foreach (var n in new[] { "求和", "只要偶数" })
            if (!去掉注释(代码).Contains(n))
                return CheckResult.不过($"没找到「{n}」这个方法。");
        return CheckResult.过();
    }

    [检查说明("求和 对不同列表都对（含空列表与负数）")]
    public static CheckResult 检查_求和(string 代码)
    {
        var 骨架 = """
using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

public static class 学生代码
{
/*学生代码*/
}

public static class 入口
{
    public static void Main()
    {
        Console.WriteLine(求和(new List<int> { 1, 2, 3, 4, 5 }));
        Console.WriteLine(求和(new List<int> { }));
        Console.WriteLine(求和(new List<int> { -5, 5 }));
        Console.WriteLine(求和(new List<int> { 100 }));
    }
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "15", "0", "0", "100" };
        string[] 名 = { "[1..5]", "空列表", "[-5,5]", "[100]" };
        var 行 = run.行;
        if (行.Count < 4) return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"求和({名[i]}) 应当是 {期望[i]}，实际是「{行[i].Trim()}」。\\n" +
                    "      ⚠ 空列表要返回 0 —— 和变量必须**先初始化为 0**。");
        return CheckResult.过("4 组输入全对（含空列表与负数）");
    }

    [检查说明("只要偶数 结果正确")]
    public static CheckResult 检查_只要偶数(string 代码)
    {
        var 骨架 = """
using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

public static class 学生代码
{
/*学生代码*/
}

public static class 入口
{
    public static void Main()
    {
        Console.WriteLine(string.Join(",", 只要偶数(new List<int> { 1, 2, 3, 4, 5, 6 })));
        Console.WriteLine("[" + string.Join(",", 只要偶数(new List<int> { 1, 3, 5 })) + "]");
        Console.WriteLine(string.Join(",", 只要偶数(new List<int> { -2, 0, 2 })));
    }
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "2,4,6", "[]", "-2,0,2" };
        var 行 = run.行;
        if (行.Count < 3) return CheckResult.不过($"应该打印 3 行，实际 {行.Count} 行。");
        for (int i = 0; i < 3; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 组错了：应当是「{期望[i]}」，实际是「{行[i].Trim()}」。\\n" +
                    "      ⚠ 三件事：偶数判据是 `x % 2 == 0`（不是 `x % 2 == 1`）；\\n" +
                    "        没有偶数时要返回**空列表**（不是 null）；\\n" +
                    "        **0 是偶数** —— `0 % 2 == 0`。");
        return CheckResult.过("含「没有偶数」与 0 的情况都对");
    }

    [检查说明("用了 List 而不是定长数组")]
    public static CheckResult 检查_用List(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("List<"))
            return CheckResult.不过(
                "没看到 `List<...>` —— 这一课要练的就是 List。\\n" +
                "      定长数组（int[]）不能 Add，长度也固定。");
        return CheckResult.过();
    }

    internal static string 去掉注释(string s)
    {
        var o = new System.Text.StringBuilder();
        bool 块 = false;
        for (int i = 0; i < s.Length; i++)
        {
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '/')
            { while (i < s.Length && s[i] != '\\n') i++; o.Append('\\n'); continue; }
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') { 块 = true; i++; continue; }
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') { 块 = false; i++; continue; }
            if (!块) o.Append(s[i]);
        }
        return o.ToString();
    }
}
''')

参考["第04课_循环与集合"] = '''// 第 04 课参考答案
static int 求和(List<int> 数)
{
    int 和 = 0;                 // ⚠ 必须初始化，否则空列表会出问题
    foreach (var x in 数) 和 += x;
    return 和;
}

static List<int> 只要偶数(List<int> 数)
{
    var 出 = new List<int>();   // 空列表，不是 null
    foreach (var x in 数)
        if (x % 2 == 0) 出.Add(x);
    return 出;
}
'''

课程["第05课_方法与参数"] = ('''[课程("05", "第 05 课：方法与参数 —— ref / out / 默认值",
    "写三个方法：\\n" +
    "  static int 平方(int x)                       // 返回 x 的平方\\n" +
    "  static void 加倍(ref int x)                  // 把传入的变量翻倍（真的改到外面）\\n" +
    "  static bool 试着转成整数(string s, out int 值) // 能转就 true 并赋值")]
public static class 第05课
{
    public static string 模板 => """
// 第 05 课：方法与参数 —— ref / out / 默认值
//
// 要做三个方法：
//
//     static int 平方(int x)                        // 返回 x * x
//     static void 加倍(ref int x)                   // 让外面的变量真的翻倍
//     static bool 试着转成整数(string s, out int 值)  // 能转就 true 并给 值 赋值
//
// ══ 这一课的重点：C# 的参数默认是「拷贝」 ══
//
//   调用 `加倍(我的数)` 时，传进去的是**一份拷贝** ——
//   方法里怎么改都影响不到外面。
//
//   想改到外面，必须加 `ref`（或 `out`）：
//
//       static void 加倍(ref int x) { x = x * 2; }
//       int a = 5;
//       加倍(ref a);          // ⚠ 调用时也要写 ref
//       Console.WriteLine(a); // 10
//
//   `out` 与 `ref` 的区别：
//     · `ref`   传进去之前**必须先赋过值**
//     · `out`   传进去之前**可以没值**，但方法里**必须给它赋值**
//     · `out` 的典型用途：`TryParse` 那种"成功了吗 + 顺带给你结果"
//
//   例如：
//       static bool 试着转成整数(string s, out int 值)
//       {
//           return int.TryParse(s, out 值);
//       }
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 05

// ── 在下面写你的三个方法 ──

""";

    public static string 参考答案 => """
static int 平方(int x)
{
    return x * x;
}

static void 加倍(ref int x)
{
    x = x * 2;
}

static bool 试着转成整数(string s, out int 值)
{
    return int.TryParse(s, out 值);
}
""";

    [检查说明("三个方法都写了")]
    public static CheckResult 检查_签名(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        foreach (var n in new[] { "平方", "加倍", "试着转成整数" })
            if (!去注释.Contains(n))
                return CheckResult.不过($"没找到「{n}」这个方法。");
        return CheckResult.过();
    }

    [检查说明("平方 对不同输入都对（含 0 与负数）")]
    public static CheckResult 检查_平方(string 代码)
    {
        var 骨架 = """
using System;

public static class 学生代码
{
/*学生代码*/
}

public static class 入口
{
    public static void Main()
    {
        Console.WriteLine(平方(1));
        Console.WriteLine(平方(0));
        Console.WriteLine(平方(-3));
        Console.WriteLine(平方(10));
    }
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "1", "0", "9", "100" };
        string[] 名 = { "1", "0", "-3", "10" };
        var 行 = run.行;
        if (行.Count < 4) return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"平方({名[i]}) 应当是 {期望[i]}，实际是「{行[i].Trim()}」。\\n" +
                    "      ⚠ 负负得正：(-3) × (-3) = 9。");
        return CheckResult.过("4 组全对（含 0 与负数）");
    }

    [检查说明("加倍(ref) 真的改到了外面的变量")]
    public static CheckResult 检查_加倍(string 代码)
    {
        var 骨架 = """
using System;

public static class 学生代码
{
/*学生代码*/
}

public static class 入口
{
    public static void Main()
    {
        int a = 5;
        加倍(ref a);
        Console.WriteLine(a);
        int b = 0;
        加倍(ref b);
        Console.WriteLine(b);
        int c = -3;
        加倍(ref c);
        Console.WriteLine(c);
    }
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "10", "0", "-6" };
        var 行 = run.行;
        if (行.Count < 3) return CheckResult.不过($"应该打印 3 行，实际 {行.Count} 行。");
        for (int i = 0; i < 3; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是 {期望[i]}，实际是「{行[i].Trim()}」。\\n" +
                    "      ⚠ 要让方法改到外面，参数必须写 `ref`：\\n" +
                    "         `static void 加倍(ref int x)` 且方法体里 `x = x * 2;`");
        return CheckResult.过("5→10 · 0→0 · -3→-6（真的改到了外面）");
    }

    [检查说明("试着转成整数(out) 成功/失败两条路都对")]
    public static CheckResult 检查_试着转(string 代码)
    {
        var 骨架 = """
using System;

public static class 学生代码
{
/*学生代码*/
}

public static class 入口
{
    public static void Main()
    {
        int v;
        Console.WriteLine(试着转成整数("42", out v) + ":" + v);
        Console.WriteLine(试着转成整数("-7", out v) + ":" + v);
        Console.WriteLine(试着转成整数("abc", out v));
        Console.WriteLine(试着转成整数("", out v));
    }
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "True:42", "True:-7", "False", "False" };
        var 行 = run.行;
        if (行.Count < 4) return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是「{期望[i]}」，实际是「{行[i].Trim()}」。\\n" +
                    "      ⚠ 转不了的时候也要给 `值` 赋一个值（`out` 要求方法里**必须**赋值）——\\n" +
                    "         最省事的做法就是直接用 `int.TryParse(s, out 值);` 的返回值。");
        return CheckResult.过("转得了 / 转不了 两条路都对");
    }

    internal static string 去掉注释(string s)
    {
        var o = new System.Text.StringBuilder();
        bool 块 = false;
        for (int i = 0; i < s.Length; i++)
        {
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '/')
            { while (i < s.Length && s[i] != '\\n') i++; o.Append('\\n'); continue; }
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') { 块 = true; i++; continue; }
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') { 块 = false; i++; continue; }
            if (!块) o.Append(s[i]);
        }
        return o.ToString();
    }
}
''')

参考["第05课_方法与参数"] = '''// 第 05 课参考答案
static int 平方(int x)
{
    return x * x;
}

static void 加倍(ref int x)
{
    // ref 让方法能改到**外面那个变量**，而不是一份拷贝
    x = x * 2;
}

static bool 试着转成整数(string s, out int 值)
{
    // out 参数要求方法里必须赋值 —— TryParse 正好两个要求都满足
    return int.TryParse(s, out 值);
}
'''

课程["第06课_字符串"] = ('''[课程("06", "第 06 课：字符串 —— 不可变与插值",
    "写两个方法：\\n" +
    "  static string 反转(string s)              // 把字符串反转\\n" +
    "  static int 数元音(string s)               // 数出其中的 aeiou（不分大小写）")]
public static class 第06课
{
    public static string 模板 => """
// 第 06 课：字符串 —— 不可变与插值
//
// 要做两个方法：
//
//     static string 反转(string s)     // "abc" → "cba"
//     static int 数元音(string s)      // "Hello" → 2（e 和 o）
//
// ══ 这一课的重点：C# 的字符串是**不可变**的 ══
//
//   所有"看起来在改字符串"的方法，其实都是**返回一个新字符串**：
//
//       string s = "abc";
//       s.ToUpper();              // ⚠ 这行什么也没做！返回值被丢掉了
//       Console.WriteLine(s);     // 还是 "abc"
//
//       s = s.ToUpper();          // 这样才对
//
//   ⇒ 常见的坑：忘了接收返回值。
//
// ══ 三个好用的东西 ══
//
//   ① 插值字符串（比 + 拼串清楚得多）：
//        Console.WriteLine($"你好，{名字}！今年 {年龄} 岁。");
//
//   ② `s.Length` 是**属性不是方法** —— 写 `s.Length`，
//      加括号 `s.Length()` 反而**编译不过**。（Java 那边是 `s.length()`）
//
//   ③ 反转最省事的写法：
//        new string(s.Reverse().ToArray())     // 需要 using System.Linq;
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 06

// ── 在下面写你的两个方法 ──
// 顶部已经 using System.Linq;

""";

    public static string 参考答案 => """
static string 反转(string s)
{
    return new string(s.Reverse().ToArray());
}

static int 数元音(string s)
{
    int n = 0;
    foreach (var c in s.ToLower())
        if ("aeiou".IndexOf(c) >= 0) n++;
    return n;
}
""";

    [检查说明("两个方法都写了")]
    public static CheckResult 检查_签名(string 代码)
    {
        foreach (var n in new[] { "反转", "数元音" })
            if (!去掉注释(代码).Contains(n))
                return CheckResult.不过($"没找到「{n}」这个方法。");
        return CheckResult.过();
    }

    [检查说明("反转 对不同字符串都对（含空串与中文）")]
    public static CheckResult 检查_反转(string 代码)
    {
        var 骨架 = """
using System;
using System.Linq;

public static class 学生代码
{
/*学生代码*/
}

public static class 入口
{
    public static void Main()
    {
        Console.WriteLine("[" + 反转("") + "]");
        Console.WriteLine(反转("a"));
        Console.WriteLine(反转("abc"));
        Console.WriteLine(反转("你好世界"));
    }
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "[]", "a", "cba", "界世好你" };
        var 行 = run.行;
        if (行.Count < 4) return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是「{期望[i]}」，实际是「{行[i].Trim()}」。\\n" +
                    "      ⚠ 空字符串要返回空字符串（别返回 null）。\\n" +
                    "      最省事的写法：`new string(s.Reverse().ToArray())`");
        return CheckResult.过("空串 / 单字符 / 英文 / 中文 都对");
    }

    [检查说明("数元音 不分大小写")]
    public static CheckResult 检查_数元音(string 代码)
    {
        var 骨架 = """
using System;
using System.Linq;

public static class 学生代码
{
/*学生代码*/
}

public static class 入口
{
    public static void Main()
    {
        Console.WriteLine(数元音(""));
        Console.WriteLine(数元音("xyz"));
        Console.WriteLine(数元音("Hello"));
        Console.WriteLine(数元音("AEIOU"));
        Console.WriteLine(数元音("Programming"));
    }
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "0", "0", "2", "5", "3" };
        var 行 = run.行;
        if (行.Count < 5) return CheckResult.不过($"应该打印 5 行，实际 {行.Count} 行。");
        for (int i = 0; i < 5; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是 {期望[i]}，实际是「{行[i].Trim()}」。\\n" +
                    "      ⚠ 重点：**不分大小写** —— 先 `s.ToLower()` 再数。\\n" +
                    "        （忘了这步的话 \"AEIOU\" 会数成 0。）");
        return CheckResult.过("空串 / 无元音 / 小写 / 大写 / 混合 都对");
    }

    [检查说明("没把 Length 当方法调用")]
    public static CheckResult 检查_Length用法(string 代码)
    {
        // ⚠ 这条守的是课程里讲的那个坑：`s.Length()` 编译不过
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains(".Length()"))
            return CheckResult.不过(
                "写了 `s.Length()` —— C# 里 Length 是**属性不是方法**，加括号编译不过。\\n" +
                "      正确写法：`s.Length`\\n" +
                "      （Java 那边才是 `s.length()` —— 两门语言在这里不一样。）");
        return CheckResult.过();
    }

    internal static string 去掉注释(string s)
    {
        var o = new System.Text.StringBuilder();
        bool 块 = false;
        for (int i = 0; i < s.Length; i++)
        {
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '/')
            { while (i < s.Length && s[i] != '\\n') i++; o.Append('\\n'); continue; }
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') { 块 = true; i++; continue; }
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') { 块 = false; i++; continue; }
            if (!块) o.Append(s[i]);
        }
        return o.ToString();
    }
}
''')

参考["第06课_字符串"] = '''// 第 06 课参考答案
static string 反转(string s)
{
    // 字符串是不可变的 ⇒ 这里其实是**造了一个新的**字符串
    return new string(s.Reverse().ToArray());
}

static int 数元音(string s)
{
    int n = 0;
    foreach (var c in s.ToLower())          // ⚠ 先转小写，才能不分大小写
        if ("aeiou".IndexOf(c) >= 0) n++;
    return n;
}
'''

课程["第07课_类与对象"] = ('''[课程("07", "第 07 课：类与对象 —— 把数据和行为放在一起",
    "写一个 `class 计数器`：\\n" +
    "  · 有一个只读属性 `int 值`\\n" +
    "  · 构造时可以从某个数开始（默认 0）\\n" +
    "  · 方法 `加(int n)` 累加，`重置()` 归零\\n" +
    "  再写 `class 银行账户`：`存(int n)` / `取(int n)`，取多了要拒绝")]
public static class 第07课
{
    public static string 模板 => """
// 第 07 课：类与对象 —— 把数据和行为放在一起
//
// 要做两个类：
//
//     class 计数器
//     {
//         public int 值 { get; }        // 只读：外面不能直接改
//         public 计数器(int 初值 = 0)    // 构造时可以给起点
//         public void 加(int n)
//         public void 重置()
//     }
//
//     class 银行账户
//     {
//         public int 余额 { get; }
//         public 银行账户(int 初值 = 0)
//         public void 存(int n)
//         public bool 取(int n)         // 钱不够返回 false，且**不能**扣
//     }
//
// ══ 这一课的重点：**把数据和行为关在一起** ══
//
//   没有类的时候，你会写：
//       int 余额 = 0;
//       void 存(int n) { 余额 += n; }        // 余额是个"谁都能改"的变量
//       void 取(int n) { 余额 -= n; }        // 忘了检查够不够 → 余额变负数
//
//   有类之后，"余额"和"改余额的规则"在**同一个地方**：
//   想把余额改成负数？做不到 —— 唯一的入口是 存/取，而 取 会检查。
//
//   ⇒ 这叫**封装**：不是为了"藏起来"，而是为了让**规则只有一个出口**。
//
// ⚠ 三个语法点：
//   ① `{ get; }` 是**只读属性** —— 只有 get，外面赋不了值
//      想改只能通过类自己的方法
//   ② 构造函数名与类名相同、没有返回类型：`public 计数器(int 初值 = 0)`
//   ③ `= 0` 是**默认参数** —— 写 `new 计数器()` 和 `new 计数器(5)` 都能用
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 07

// ── 在下面写你的两个类 ──

""";

    public static string 参考答案 => """
class 计数器
{
    public int 值 { get; private set; }

    public 计数器(int 初值 = 0)
    {
        值 = 初值;
    }

    public void 加(int n) => 值 += n;

    public void 重置() => 值 = 0;
}

class 银行账户
{
    public int 余额 { get; private set; }

    public 银行账户(int 初值 = 0)
    {
        余额 = 初值;
    }

    public void 存(int n)
    {
        if (n <= 0) return;          // 不合法就什么也不做
        余额 += n;
    }

    public bool 取(int n)
    {
        if (n <= 0 || n > 余额) return false;   // ⚠ 钱不够必须**拒绝并保持不变**
        余额 -= n;
        return true;
    }
}
""";

    [检查说明("两个类都写了")]
    public static CheckResult 检查_签名(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        foreach (var n in new[] { "class 计数器", "class 银行账户" })
            if (!去注释.Contains(n))
                return CheckResult.不过($"没找到「{n}」。");
        return CheckResult.过();
    }

    [检查说明("计数器：构造 / 加 / 重置 都对")]
    public static CheckResult 检查_计数器(string 代码)
    {
        var 骨架 = """
using System;

public static class 入口
{
/*学生代码*/

    public static void Main()
    {
        var a = new 计数器();
        Console.WriteLine(a.值);
        a.加(3); a.加(4);
        Console.WriteLine(a.值);
        a.重置();
        Console.WriteLine(a.值);

        var b = new 计数器(100);
        Console.WriteLine(b.值);
        b.加(1);
        Console.WriteLine(b.值);
    }
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "0", "7", "0", "100", "101" };
        var 行 = run.行;
        if (行.Count < 5) return CheckResult.不过($"应该打印 5 行，实际 {行.Count} 行。");
        for (int i = 0; i < 5; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是 {期望[i]}，实际是「{行[i].Trim()}」。\\n" +
                    "      ⚠ 注意第 4 个：`new 计数器(100)` 要能从 100 开始\\n" +
                    "        ⇒ 构造函数要有 `int 初值 = 0` 这个参数。");
        return CheckResult.过("默认 0 起 / 累加 / 重置 / 指定初值 都对");
    }

    [检查说明("银行账户：钱不够时**拒绝**且余额不变")]
    public static CheckResult 检查_账户(string 代码)
    {
        var 骨架 = """
using System;

public static class 入口
{
/*学生代码*/

    public static void Main()
    {
        var a = new 银行账户(100);
        Console.WriteLine(a.余额);
        a.存(50);
        Console.WriteLine(a.余额);
        Console.WriteLine(a.取(30) + ":" + a.余额);
        Console.WriteLine(a.取(999) + ":" + a.余额);   // 取不出来，余额不能变
        Console.WriteLine(a.取(120) + ":" + a.余额);   // 正好取光
    }
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "100", "150", "True:120", "False:120", "True:0" };
        var 行 = run.行;
        if (行.Count < 5) return CheckResult.不过($"应该打印 5 行，实际 {行.Count} 行。");
        for (int i = 0; i < 5; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是「{期望[i]}」，实际是「{行[i].Trim()}」。\\n" +
                    "      ⚠ 最要紧的是第 4 个：**取不出来的时候余额必须一点都不变**。\\n" +
                    "        常见错法：先 `余额 -= n;` 再检查 —— 那时已经扣了。\\n" +
                    "        正确顺序：**先检查，通过才扣**。");
        return CheckResult.过("存取 / 拒绝 / 正好取光 都对");
    }

    [检查说明("余额是只读属性（外面不能直接改）")]
    public static CheckResult 检查_只读(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("{ get;"))
            return CheckResult.不过(
                "没看到只读属性写法 `{ get; ... }`。\\n" +
                "      这一课要练的就是封装：余额/值只能由类自己的方法改。");
        return CheckResult.过();
    }

    internal static string 去掉注释(string s)
    {
        var o = new System.Text.StringBuilder();
        bool 块 = false;
        for (int i = 0; i < s.Length; i++)
        {
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '/')
            { while (i < s.Length && s[i] != '\\n') i++; o.Append('\\n'); continue; }
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') { 块 = true; i++; continue; }
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') { 块 = false; i++; continue; }
            if (!块) o.Append(s[i]);
        }
        return o.ToString();
    }
}
''')

参考["第07课_类与对象"] = '''// 第 07 课参考答案
class 计数器
{
    // 只读属性：外面**改不了**，唯一的入口是下面的方法
    public int 值 { get; private set; }

    public 计数器(int 初值 = 0)   // 默认参数 ⇒ new 计数器() 也行
    {
        值 = 初值;
    }

    public void 加(int n) => 值 += n;
    public void 重置() => 值 = 0;
}

class 银行账户
{
    public int 余额 { get; private set; }

    public 银行账户(int 初值 = 0)
    {
        余额 = 初值;
    }

    public void 存(int n)
    {
        if (n <= 0) return;
        余额 += n;
    }

    public bool 取(int n)
    {
        // ⚠ 顺序很重要：**先检查，通过才扣**。
        //   反过来的话，一次失败的取款已经把余额扣掉了。
        if (n <= 0 || n > 余额) return false;
        余额 -= n;
        return true;
    }
}
'''

课程["第08课_继承与接口"] = ('''[课程("08", "第 08 课：继承与接口 —— 各做各的，同一套调用",
    "写一个接口 `interface 图形 { double 面积(); string 名字(); }`\\n" +
    "  再写三个实现：圆 / 矩形 / 正方形\\n" +
    "  最后写 `static string 报告(List<图形> 们)` 把它们逐个描述出来")]
public static class 第08课
{
    public static string 模板 => """
// 第 08 课：继承与接口 —— 各做各的，同一套调用
//
// 要做：
//
//     interface 图形
//     {
//         double 面积();
//         string 名字();
//     }
//
//     class 圆     : 图形     // 构造时给半径
//     class 矩形   : 图形     // 构造时给宽高
//     class 正方形 : 矩形     // 复用矩形（正方形是特殊的矩形）
//
//     static string 报告(List<图形> 们)   // 每个打印一行："名字 面积=xx.xx"
//
// ══ 这一课的重点：**同一套调用，各做各的** ══
//
//   注意 `报告` 这个方法：它**不知道**自己拿到的是圆还是矩形。
//   它只知道"这是个图形，能问它面积和名字"。
//
//       foreach (var g in 们)
//           Console.WriteLine($"{g.名字()} 面积={g.面积():F2}");
//
//   ⇒ 明天加一个"三角形"，`报告` **一个字都不用改**。
//     这就是接口的价值：**调用方与实现方解耦**。
//
// ⚠ 三个语法点：
//   ① 接口里的方法**不写实现**，也不写 public（默认就是公开的）
//   ② 实现接口用 `:`（C# 只有冒号，没有 Java 的 implements 关键字）
//   ③ 实现的方法必须是 `public` —— 少了会被编译器拦住
//      （这是好事：C# 在这点上比 C++ 宽容，比 Java 严格）
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 08

// ── 在下面写你的接口和类 ──
// 顶部已经 using System.Collections.Generic;

""";

    public static string 参考答案 => """
interface 图形
{
    double 面积();
    string 名字();
}

class 圆 : 图形
{
    private readonly double 半径;
    public 圆(double 半径) { this.半径 = 半径; }
    public double 面积() => 3.14159 * 半径 * 半径;
    public string 名字() => "圆";
}

class 矩形 : 图形
{
    protected readonly double 宽, 高;
    public 矩形(double 宽, double 高) { this.宽 = 宽; this.高 = 高; }
    public virtual double 面积() => 宽 * 高;
    public virtual string 名字() => "矩形";
}

class 正方形 : 矩形
{
    public 正方形(double 边长) : base(边长, 边长) { }
    public override string 名字() => "正方形";
}

static string 报告(List<图形> 们)
{
    var sb = new System.Text.StringBuilder();
    foreach (var g in 们)
        sb.AppendLine($"{g.名字()} 面积={g.面积():F2}");
    return sb.ToString().TrimEnd();
}
""";

    [检查说明("接口与三个类都写了")]
    public static CheckResult 检查_签名(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        foreach (var n in new[] { "interface 图形", "class 圆", "class 矩形", "class 正方形" })
            if (!去注释.Contains(n))
                return CheckResult.不过($"没找到「{n}」。");
        if (!去注释.Contains("报告"))
            return CheckResult.不过("没找到「报告」这个方法。");
        return CheckResult.过();
    }

    [检查说明("三种图形都能算面积（正方形复用了矩形）")]
    public static CheckResult 检查_面积(string 代码)
    {
        var 骨架 = """
using System;
using System.Collections.Generic;

public static class 入口
{
/*学生代码*/

    public static void Main()
    {
        var 们 = new List<图形> { new 圆(1), new 矩形(3, 4), new 正方形(5) };
        foreach (var g in 们)
            Console.WriteLine($"{g.名字()}|{g.面积():F2}");
    }
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "圆|3.14", "矩形|12.00", "正方形|25.00" };
        var 行 = run.行;
        if (行.Count < 3) return CheckResult.不过($"应该打印 3 行，实际 {行.Count} 行。");
        for (int i = 0; i < 3; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个应当是「{期望[i]}」，实际是「{行[i].Trim()}」。\\n" +
                    "      ⚠ 中间那个别搞混：矩形(3,4) 面积是 12.00。\\n" +
                    "        正方形(5) 应当复用矩形 —— 5×5 = 25.00。");
        return CheckResult.过("圆 3.14 · 矩形 12.00 · 正方形 25.00");
    }

    [检查说明("报告() 对同一批图形都能描述（它不认识具体类型）")]
    public static CheckResult 检查_报告(string 代码)
    {
        var 骨架 = """
using System;
using System.Collections.Generic;

public static class 入口
{
/*学生代码*/

    public static void Main()
    {
        var 们 = new List<图形> { new 正方形(2), new 圆(2) };
        Console.WriteLine(报告(们));
    }
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        var 文 = string.Join("\\n", run.行);
        // 只要包含两种图形与面积就行（格式给学员留了自由）
        if (!文.Contains("正方形") || !文.Contains("圆"))
            return CheckResult.不过(
                $"报告里应当同时出现「正方形」和「圆」，实际是：\\n{文}\\n" +
                "      ⚠ 报告() 里要用 `g.名字()` 和 `g.面积()`，\\n" +
                "        而不是去判断"它是不是圆"。");
        if (!文.Contains("4.00") && !文.Contains("12.57"))
            return CheckResult.不过(
                $"报告里没看到面积数字，实际是：\\n{文}\\n" +
                "      写法：$\"{g.名字()} 面积={g.面积():F2}\"");
        return CheckResult.过("两种图形都被描述了");
    }

    [检查说明("正方形真的继承自矩形（不是各写一遍）")]
    public static CheckResult 检查_继承(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!System.Text.RegularExpressions.Regex.IsMatch(
                去注释, @"class\s+正方形\s*:\s*矩形"))
            return CheckResult.不过(
                "「正方形」应当写成 `class 正方形 : 矩形` —— 复用矩形的面积计算。\\n" +
                "      这一课要练的就是继承：正方形是特殊的矩形，\\n" +
                "      把自己的边长同时当宽和高传上去就行（`base(边长, 边长)`）。");
        return CheckResult.过();
    }

    internal static string 去掉注释(string s)
    {
        var o = new System.Text.StringBuilder();
        bool 块 = false;
        for (int i = 0; i < s.Length; i++)
        {
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '/')
            { while (i < s.Length && s[i] != '\\n') i++; o.Append('\\n'); continue; }
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') { 块 = true; i++; continue; }
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') { 块 = false; i++; continue; }
            if (!块) o.Append(s[i]);
        }
        return o.ToString();
    }
}
''')

参考["第08课_继承与接口"] = '''// 第 08 课参考答案
interface 图形
{
    double 面积();
    string 名字();
}

class 圆 : 图形
{
    private readonly double 半径;
    public 圆(double 半径) { this.半径 = 半径; }
    public double 面积() => 3.14159 * 半径 * 半径;
    public string 名字() => "圆";
}

class 矩形 : 图形
{
    protected readonly double 宽, 高;
    public 矩形(double 宽, double 高) { this.宽 = 宽; this.高 = 高; }
    public virtual double 面积() => 宽 * 高;
    public virtual string 名字() => "矩形";
}

class 正方形 : 矩形
{
    // 正方形**就是**一个宽高相等的矩形 ⇒ 复用它的面积计算
    public 正方形(double 边长) : base(边长, 边长) { }
    public override string 名字() => "正方形";
}

// ⚠ 注意这个方法：它**不知道**拿到的是圆还是矩形
static string 报告(List<图形> 们)
{
    var sb = new System.Text.StringBuilder();
    foreach (var g in 们)
        sb.AppendLine($"{g.名字()} 面积={g.面积():F2}");
    return sb.ToString().TrimEnd();
}
'''

for 名, 体 in 课程.items():
    io.open(os.path.join(S, 名 + ".cs"), "w", encoding="utf-8", newline="\r\n").write(头 + 体)
    print("  课程 cs-lessons/Lessons/%s.cs" % 名)
for 名, 体 in 参考.items():
    io.open(os.path.join(A, 名 + ".cs"), "w", encoding="utf-8", newline="\r\n").write(体)
    print("  参考 cs-solutions/%s.cs" % 名)
print("  共 %d 课" % len(课程))
