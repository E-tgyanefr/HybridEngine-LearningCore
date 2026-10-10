using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("05", "第 05 课：方法与参数 —— ref / out / 默认值",
    "写三个方法：\n" +
    "  static int 平方(int x)                       // 返回 x 的平方\n" +
    "  static void 加倍(ref int x)                  // 把传入的变量翻倍（真的改到外面）\n" +
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
                    $"平方({名[i]}) 应当是 {期望[i]}，实际是「{行[i].Trim()}」。\n" +
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
                    $"第 {i + 1} 个用例应当是 {期望[i]}，实际是「{行[i].Trim()}」。\n" +
                    "      ⚠ 要让方法改到外面，参数必须写 `ref`：\n" +
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
                    $"第 {i + 1} 个用例应当是「{期望[i]}」，实际是「{行[i].Trim()}」。\n" +
                    "      ⚠ 转不了的时候也要给 `值` 赋一个值（`out` 要求方法里**必须**赋值）——\n" +
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
            { while (i < s.Length && s[i] != '\n') i++; o.Append('\n'); continue; }
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') { 块 = true; i++; continue; }
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') { 块 = false; i++; continue; }
            if (!块) o.Append(s[i]);
        }
        return o.ToString();
    }
}
