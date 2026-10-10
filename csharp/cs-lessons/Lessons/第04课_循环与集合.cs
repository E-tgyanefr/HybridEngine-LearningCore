using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("04", "第 04 课：循环与集合 —— List 与 foreach",
    "写一个方法 `static int 求和(List<int> 数)`：\n" +
    "  用 foreach 把列表里的数加起来返回\n" +
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
                    $"求和({名[i]}) 应当是 {期望[i]}，实际是「{行[i].Trim()}」。\n" +
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
                    $"第 {i + 1} 组错了：应当是「{期望[i]}」，实际是「{行[i].Trim()}」。\n" +
                    "      ⚠ 三件事：偶数判据是 `x % 2 == 0`（不是 `x % 2 == 1`）；\n" +
                    "        没有偶数时要返回**空列表**（不是 null）；\n" +
                    "        **0 是偶数** —— `0 % 2 == 0`。");
        return CheckResult.过("含「没有偶数」与 0 的情况都对");
    }

    [检查说明("用了 List 而不是定长数组")]
    public static CheckResult 检查_用List(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("List<"))
            return CheckResult.不过(
                "没看到 `List<...>` —— 这一课要练的就是 List。\n" +
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
            { while (i < s.Length && s[i] != '\n') i++; o.Append('\n'); continue; }
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') { 块 = true; i++; continue; }
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') { 块 = false; i++; continue; }
            if (!块) o.Append(s[i]);
        }
        return o.ToString();
    }
}
