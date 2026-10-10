using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("03", "第 03 课：条件判断 —— switch 与 if 的选择",
    "写一个方法 `static string 判等级(int 分)`：\n" +
    "  ≥90 返回 \"优秀\"、≥60 返回 \"及格\"、否则 \"不及格\"\n" +
    "  再写一个 `static string 星期几(int n)`，用 **switch 表达式**返回 \"周一\"…\"周日\"")]
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
                    $"第 {i + 1} 个用例错了：应当是「{期望[i]}」，实际是「{行[i].Trim()}」。\n" +
                    "      四个用例是 95 / 90 / 60 / 59 —— 重点看 **90 和 60 这两个边界**。\n" +
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
                    $"星期几({i + 1}) 应当是「{期望[i]}」，实际是「{行[i].Trim()}」。\n" +
                    "      ⚠ 注意输入 8 应当返回「不是星期」—— 别忘了 `_ =>` 那一支。");
        return CheckResult.过("1..7 都对，8 走默认分支");
    }

    [检查说明("星期几 真的用了 switch")]
    public static CheckResult 检查_用了switch(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("switch"))
            return CheckResult.不过(
                "「星期几」没有用 switch。\n" +
                "      这一课就是要你见识 C# 的 switch 表达式：\n" +
                "          return n switch { 1 => \"周一\", ..., _ => \"不是星期\" };");
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
