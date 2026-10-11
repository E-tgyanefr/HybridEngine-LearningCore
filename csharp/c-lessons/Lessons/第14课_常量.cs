using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("14", "第 14 课：#define 与 const —— 给常量起个名字",
    """
1. 用 #define 定义 圆周率 3.14159
2. 打印半径 2 的圆面积（保留两位小数）
""")]
public static class 第14课
{
    public static string 模板 => """
// 第 14 课：#define 与 const —— 给常量起个名字
//
// 先介绍：不希望被改的值，怎么给它起名字
//
// 程序里常有"固定不变"的值：圆周率、最大人数、税率……
// 直接写 `3.14159` 散落在各处，改的时候要找遍全文 —— 而且容易漏。
//
// 两种起名字的办法：
//
//   ① **#define**（预处理宏）—— 老办法，用得最广
//
//        #define 圆周率 3.14159
//        ↑ 没有等号！也没有分号！
//
//   ② **const**（真正的常量变量）
//
//        const double 圆周率 = 3.14159;
//
// ⚠⚠ 这一课要记住的坑：**#define 那行没有等号也没有分号。**
//
//     #define 圆周率 = 3.14159      ❌ 那个等号会被当成值的一部分
//     #define 圆周率 3.14159;       ❌ 分号也会被当成值的一部分
//     #define 圆周率 3.14159        ✅
//
//   为什么：`#define` 是**纯粹的文本替换**，不是变量声明。
//   编译器在编译前，把所有 `圆周率` 三个字**原样换成** `3.14159`。
//   所以那个分号会一起被换进去，变成 `printf("%f", 3.14159;);` ⇒ 语法错。
//
//   ⚠ 也正因为是文本替换，`#define` 后面**不写类型** —— 它没有类型。
//
// ⚠ 用哪个：#define 老、快、处处能用；const 有类型、能调试。
//   本课用 #define（读别人的 C 代码时见得最多）。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   1. 用 #define 定义 圆周率 3.14159（⚠ 没有等号、没有分号）
//   2. 打印半径 2 的圆面积，保留两位小数

// ── 在下面写你的代码 ──

""";

    public static string 参考答案 => """
#include <stdio.h>

#define 圆周率 3.14159        /* ⚠ 没有等号，没有分号 */

int main(void) {
    printf("%.2f\n", 圆周率 * 2 * 2);
    return 0;
}
""";


    [检查说明("用了 #define 而且写法正确（没有等号分号）")]
    public static CheckResult 检查_1(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains("#define 圆周率 ="))
            return CheckResult.不过("出现了不该有的写法：#define 圆周率 =");
        return CheckResult.过();
    }

    [检查说明("程序能编译运行")]
    public static CheckResult 检查_2(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "12.57",
        };
        var 实际 = run.行;
        if (实际.Count != 期望.Length)
            return CheckResult.不过($"应该有 {期望.Length} 行输出，实际 {实际.Count} 行。" +
                "\n      打印出来的：\n        " + string.Join("\n        ", 实际));
        for (int i = 0; i < 期望.Length; i++)
            if (实际[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 行应当是「{期望[i]}」，实际是「{实际[i].Trim()}」。");
        return CheckResult.过($"{期望.Length} 行输出全对");
    }

    [检查说明("打印了 12.57")]
    public static CheckResult 检查_3(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "12.57",
        };
        var 实际 = run.行;
        if (实际.Count != 期望.Length)
            return CheckResult.不过($"应该有 {期望.Length} 行输出，实际 {实际.Count} 行。" +
                "\n      打印出来的：\n        " + string.Join("\n        ", 实际));
        for (int i = 0; i < 期望.Length; i++)
            if (实际[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 行应当是「{期望[i]}」，实际是「{实际[i].Trim()}」。");
        return CheckResult.过($"{期望.Length} 行输出全对");
    }

    private static string 去掉注释(string s)
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
