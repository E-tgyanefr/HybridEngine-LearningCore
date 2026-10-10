using System;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("04", "第 04 课：循环 —— 让程序重复做事",
    "用 for 循环打印 1 到 5，每行一个数字\n" +
    "然后另起一行打印它们的和（应该是 15）")]
public static class 第04课
{
    public static string 模板 => """
// 第 04 课：循环 —— 让程序重复做事
//
// 要做：
//   1. 用 for 循环打印 1 到 5，每行一个
//   2. 另起一行打印它们的和（1+2+3+4+5 = 15）
//
// for 循环的三个部分（用分号隔开，**每部分都不能少**）：
//
//     for (初始化; 条件; 每轮之后做什么) { ... }
//
// ⚠ 经典坑：`for (int i = 1; i < 5; i++)` 会漏掉 5
//    想跑到 5 就得写 `i <= 5`
// ⚠ 求和变量必须在循环**外面**定义并初始化（int 和 = 0;）：
//    定义在循环里的话每轮都重置，最后永远是最后一个数
//
// 写完后运行：dotnet run --project csharp/c-lessons 检查 04

// ── 在下面写你的代码 ──
// 提示：本课是完整程序，要自己写 #include 和 main。

""";

    public static string 参考答案 => """
#include <stdio.h>

int main(void) {
    int 和 = 0;
    for (int i = 1; i <= 5; i++) {
        printf("%d\n", i);
        和 += i;
    }
    printf("%d\n", 和);
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

    [检查说明("前 5 行是 1 2 3 4 5")]
    public static CheckResult 检查_循环输出(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        var 行 = run.行;
        if (行.Count < 5) return CheckResult.不过($"应该有 6 行输出（1-5 加和），实际 {行.Count} 行。");
        for (int i = 0; i < 5; i++)
            if (行[i].Trim() != (i + 1).ToString())
                return CheckResult.不过(
                    $"第 {i + 1} 行应该是 {i + 1}，实际是「{行[i].Trim()}」。\n" +
                    "      循环条件是不是写成 `i < 5` 了？（那会漏掉 5，而且开头可能从 0 开始）\n" +
                    "      要打印 1..5 得写 `for (int i = 1; i <= 5; i++)`。");
        return CheckResult.过("1 2 3 4 5");
    }

    [检查说明("第 6 行是 15（和）")]
    public static CheckResult 检查_和(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        var 行 = run.行;
        if (行.Count < 6) return CheckResult.不过("只打印了 5 行 —— 和的输出没写？");
        if (行[5].Trim() != "15")
            return CheckResult.不过(
                $"第 6 行应该是 15，实际是「{行[5].Trim()}」。\n" +
                "      最常见的原因：求和变量 `和 = 0` 写在了**循环里面** ⇒ 每轮都清零，最后只剩 5。\n" +
                "      它必须定义在循环**外面**。");
        return CheckResult.过();
    }

    [检查说明("真的用了 for 循环")]
    public static CheckResult 检查_用了for(string 代码)
    {
        if (!去掉注释(代码).Contains("for"))
            return CheckResult.不过("没看到 for。要求用循环，不是把 5 行 printf 抄一遍。");
        return CheckResult.过();
    }

    [检查说明("没有未初始化变量（编译警告）")]
    public static CheckResult 检查_无警告(string 代码)
    {
        // ⚠ C 最经典的坑：`int 和;` 不初始化，累加时"碰巧"读到 0 ⇒ 程序跑对了，
        //   而下次可能是别的值。判它通过等于**奖励一个真实的 bug**。
        var run = CppCheck.直接运行(代码);
        if (run.Raw.HasCorrectnessWarning)
            return CheckResult.不过(
                "编译器报了「可能未初始化的变量」——这类问题在 C 里是**未定义行为**：\n" +
                "      今天跑对了，换台机器/换个编译器版本就可能给出别的结果。\n" +
                "      累加变量一定要写成 `int 和 = 0;`（就地给它一个初值）。");
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

