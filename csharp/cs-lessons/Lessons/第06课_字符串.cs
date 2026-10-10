using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("06", "第 06 课：字符串 —— 不可变与插值",
    "写两个方法：\n" +
    "  static string 反转(string s)              // 把字符串反转\n" +
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
                    $"第 {i + 1} 个用例应当是「{期望[i]}」，实际是「{行[i].Trim()}」。\n" +
                    "      ⚠ 空字符串要返回空字符串（别返回 null）。\n" +
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
                    $"第 {i + 1} 个用例应当是 {期望[i]}，实际是「{行[i].Trim()}」。\n" +
                    "      ⚠ 重点：**不分大小写** —— 先 `s.ToLower()` 再数。\n" +
                    "        （忘了这步的话「AEIOU」会数成 0。）");
        return CheckResult.过("空串 / 无元音 / 小写 / 大写 / 混合 都对");
    }

    [检查说明("没把 Length 当方法调用")]
    public static CheckResult 检查_Length用法(string 代码)
    {
        // ⚠ 这条守的是课程里讲的那个坑：`s.Length()` 编译不过
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains(".Length()"))
            return CheckResult.不过(
                "写了 `s.Length()` —— C# 里 Length 是**属性不是方法**，加括号编译不过。\n" +
                "      正确写法：`s.Length`\n" +
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
            { while (i < s.Length && s[i] != '\n') i++; o.Append('\n'); continue; }
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') { 块 = true; i++; continue; }
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') { 块 = false; i++; continue; }
            if (!块) o.Append(s[i]);
        }
        return o.ToString();
    }
}
