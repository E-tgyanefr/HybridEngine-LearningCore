using System;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("05", "第 05 课：函数 —— 把计算打包起来",
    "写一个函数 `int 平方(int x)`，返回 x 的平方\n" +
    "再写一个 `int 两数之和(int a, int b)`")]
public static class 第05课
{
    public static string 模板 => """
// 第 05 课：函数 —— 把计算打包起来
//
// 要做两个函数：
//
//     int 平方(int x);              // 返回 x * x
//     int 两数之和(int a, int b);    // 返回 a + b
//
// 函数的形状（**四样东西，一个都不能少**）：
//
//     返回类型  函数名(参数类型 参数名) { ... return 结果; }
//
// ⚠ 最容易漏的两处：
//   1. **参数也要写类型**：`int 平方(int x)` 不是 `int 平方(x)`
//   2. **必须 return**：不 return 的话返回值是随机的（C 不像 Python 会自动给 None）
//
// 写完后运行：dotnet run --project csharp/c-lessons 检查 05

// ── 在下面写你的两个函数 ──
// 这一课**不用**写 main（main 由课程提供）。

""";

    public static string 参考答案 => """
int 平方(int x) {
    return x * x;
}

int 两数之和(int a, int b) {
    return a + b;
}
""";

    [检查说明("两个函数都写了，签名对")]
    public static CheckResult 检查_签名(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("平方"))
            return CheckResult.不过("没找到名为「平方」的函数。");
        if (!去注释.Contains("两数之和"))
            return CheckResult.不过("没找到名为「两数之和」的函数。");
        return CheckResult.过();
    }

    [检查说明("平方() 对不同输入都对（7 个用例）")]
    public static CheckResult 检查_平方(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 平方(1));
    printf("%d\n", 平方(2));
    printf("%d\n", 平方(3));
    printf("%d\n", 平方(5));
    printf("%d\n", 平方(10));
    printf("%d\n", 平方(0));
    printf("%d\n", 平方(-3));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "1", "4", "9", "25", "100", "0", "9" };
        string[] 输入 = { "1", "2", "3", "5", "10", "0", "-3" };
        if (run.行.Count < 期望.Length)
            return CheckResult.不过($"应该打印 {期望.Length} 行，实际 {run.行.Count} 行。");
        for (int i = 0; i < 期望.Length; i++)
            if (run.行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"平方({输入[i]}) 应该是 {期望[i]}，实际是「{run.行[i].Trim()}」。\n" +
                    "      ⚠ 多组输入都对才算对 —— 只对一组可能是碰巧。\n" +
                    "        特别注意 `平方(0)` 和 `平方(-3)`：负负得正，(-3)×(-3) = 9。");
        return CheckResult.过("7 组输入全对（含 0 与负数）");
    }

    [检查说明("两数之和() 对不同输入都对")]
    public static CheckResult 检查_两数之和(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 两数之和(1, 2));
    printf("%d\n", 两数之和(100, 200));
    printf("%d\n", 两数之和(-5, 5));
    printf("%d\n", 两数之和(0, 0));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "3", "300", "0", "0" };
        if (run.行.Count < 期望.Length)
            return CheckResult.不过($"应该打印 {期望.Length} 行，实际 {run.行.Count} 行。");
        for (int i = 0; i < 期望.Length; i++)
            if (run.行[i].Trim() != 期望[i])
                return CheckResult.不过($"第 {i + 1} 组错了：应该是 {期望[i]}，实际「{run.行[i].Trim()}」。");
        return CheckResult.过();
    }

    [检查说明("参数写了类型 + 有 return")]
    public static CheckResult 检查_参数类型(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains("平方(x)") || 去注释.Contains("平方( x )"))
            return CheckResult.不过(
                "参数 **漏了类型**：C 里每个参数都要写类型。\n" +
                "      写成 `int 平方(int x)`，不是 `int 平方(x)`。");
        if (!去注释.Contains("return"))
            return CheckResult.不过(
                "没看到 return。\n" +
                "      ⚠ C 不像 Python：不写 return 的话函数返回的是**内存里的残留值**，\n" +
                "        不是 None，也不报错 —— 这种错最难查。");
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

