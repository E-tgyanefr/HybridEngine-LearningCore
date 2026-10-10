using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("08", "第 08 课：继承与接口 —— 各做各的，同一套调用",
    "写一个接口 `interface 图形 { double 面积(); string 名字(); }`\n" +
    "  再写三个实现：圆 / 矩形 / 正方形\n" +
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
                    $"第 {i + 1} 个应当是「{期望[i]}」，实际是「{行[i].Trim()}」。\n" +
                    "      ⚠ 中间那个别搞混：矩形(3,4) 面积是 12.00。\n" +
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
        var 文 = string.Join("\n", run.行);
        // 只要包含两种图形与面积就行（格式给学员留了自由）
        if (!文.Contains("正方形") || !文.Contains("圆"))
            return CheckResult.不过(
                $"报告里应当同时出现「正方形」和「圆」，实际是：\n{文}\n" +
                "      ⚠ 报告() 里要用 `g.名字()` 和 `g.面积()`，\n" +
                "        而不是去判断「它是不是圆」。");
        if (!文.Contains("4.00") && !文.Contains("12.57"))
            return CheckResult.不过(
                $"报告里没看到面积数字，实际是：\n{文}\n" +
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
                "「正方形」应当写成 `class 正方形 : 矩形` —— 复用矩形的面积计算。\n" +
                "      这一课要练的就是继承：正方形是特殊的矩形，\n" +
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
            { while (i < s.Length && s[i] != '\n') i++; o.Append('\n'); continue; }
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') { 块 = true; i++; continue; }
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') { 块 = false; i++; continue; }
            if (!块) o.Append(s[i]);
        }
        return o.ToString();
    }
}
