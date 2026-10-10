using System;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("06", "第 06 课：数组 —— 一排格子",
    "定义一个装 5 个整数的数组 {3, 1, 4, 1, 5}\n" +
    "用循环求总和与最大值，各打印一行")]
public static class 第06课
{
    public static string 模板 => """
// 第 06 课：数组 —— 一排格子
//
// 要做：
//   1. 定义 int 数组 {3, 1, 4, 1, 5}
//   2. 用循环求它们的**总和**，打印一行
//   3. 用循环求**最大值**，打印一行
//
// 数组的写法：
//
//     int 数[5] = {3, 1, 4, 1, 5};      // 注意是**花括号**，不是方括号
//     数[0] 是第一个（C 的下标从 **0** 开始）
//     最后一个的下标是 4，不是 5
//
// ⚠ 三个经典坑：
//   1. 下标从 0 开始 ⇒ 循环要写 `for (int i = 0; i < 5; i++)`，写 `<= 5` 就越界
//   2. 求最大值的初值：设成 0 的话，如果数组全是负数就错了
//      （本课的数据是正的，所以 0 也能过；但更稳妥的是设成 数[0]）
//   3. `int 数[5] = {…}` 才是初始化；`int 数[5]; 数 = {…};` 是错的
//
// 写完后运行：dotnet run --project csharp/c-lessons 检查 06

// ── 在下面写你的代码 ──
// 提示：本课是完整程序，要自己写 #include 和 main。

""";

    public static string 参考答案 => """
#include <stdio.h>

int main(void) {
    int 数[5] = {3, 1, 4, 1, 5};

    int 和 = 0;
    for (int i = 0; i < 5; i++) 和 += 数[i];
    printf("%d\n", 和);

    int 最大 = 数[0];
    for (int i = 1; i < 5; i++)
        if (数[i] > 最大) 最大 = 数[i];
    printf("%d\n", 最大);

    return 0;
}
""";

    [检查说明("程序能编译运行")]
    public static CheckResult 检查_能运行(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        if (run.Raw.Error != null) return CheckResult.不过("程序没能正常运行：\n      " + run.Raw.Error);
        if (run.行.Count == 0) return CheckResult.不过("一行都没打印。");
        return CheckResult.过();
    }

    [检查说明("总和 = 14")]
    public static CheckResult 检查_总和(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        var 行 = run.行;
        if (行.Count < 1) return CheckResult.不过("没有输出。");
        if (行[0].Trim() != "14")
            return CheckResult.不过(
                $"总和应该是 14（3+1+4+1+5），实际「{行[0].Trim()}」。\n" +
                "      检查：循环是 `i < 5` 吗？数组是 {3,1,4,1,5} 吗？\n" +
                "      ⚠ 写 `i <= 5` 会读到第 6 个格子（越界），总和会偏大。");
        return CheckResult.过();
    }

    [检查说明("最大值 = 5")]
    public static CheckResult 检查_最大值(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        var 行 = run.行;
        if (行.Count < 2) return CheckResult.不过("只打印了 1 行 —— 最大值那行没写？");
        if (行[1].Trim() != "5")
            return CheckResult.不过(
                $"最大值应该是 5，实际「{行[1].Trim()}」。\n" +
                "      检查：是不是把 `>` 写成 `<` 了？（那求出来的是最小值）");
        return CheckResult.过();
    }

    [检查说明("真的用了数组 + 循环")]
    public static CheckResult 检查_结构(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("["))
            return CheckResult.不过("没看到数组下标，要求用数组。");
        if (!去注释.Contains("for"))
            return CheckResult.不过("没看到 for。要求用循环遍历，不是手写 几个加法。");
        return CheckResult.过();
    }

    [检查说明("没有越界、没有未初始化（编译警告）")]
    public static CheckResult 检查_无警告(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.HasCorrectnessWarning)
            return CheckResult.不过(
                "编译器报了「数组越界」或「可能未初始化」——\n" +
                "      这类问题在 C 里**不一定崩**：它可能安静地读到隔壁内存，算出个看着像样的错数。\n" +
                "      数组下标 0..4，循环条件要写 `i < 5`。");
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

