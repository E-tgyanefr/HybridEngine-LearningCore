using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("02", "第 02 课：类型与计算 —— int 和 double 不是一回事",
    "写一个方法 `static double 算面积(int 半径)`：\n" +
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
                    $"半径 {用例[i].说明}：应当是 {用例[i].期望}，实际是 {实}。\n" +
                    "      ⚠ 检查一件事：**是不是整数除法把小数丢了**？\n" +
                    "         `3.14159 * 半径 * 半径` 里因为有小数，整个式子才是 double；\n" +
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
                "返回类型写成了 `int` —— 面积是小数，用 int 会被**截断**（78.53 变成 78）。\n" +
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
            { while (i < s.Length && s[i] != '\n') i++; o.Append('\n'); continue; }
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') { 块 = true; i++; continue; }
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') { 块 = false; i++; continue; }
            if (!块) o.Append(s[i]);
        }
        return o.ToString();
    }
}
