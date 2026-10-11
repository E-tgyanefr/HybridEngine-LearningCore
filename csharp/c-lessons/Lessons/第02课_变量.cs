using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("02", "第 02 课：变量 —— 给一个值起个名字",
    """
1. 定义 int 变量 年龄 = 18
2. 定义 int 变量 学号 = 7
3. 打印这两个变量（分两行）
""")]
public static class 第02课
{
    public static string 模板 => """
// 第 02 课：变量 —— 给一个值起个名字
//
// 先介绍：变量 = 给一个值起个名字
//
//     int 年龄 = 18;
//     ↑   ↑     ↑
//    类型  名字  值
//
// ⚠⚠ **C 和 Python 最大的区别就在这里：C 必须先说类型。**
//
//     Python：  年龄 = 18              不用写类型
//     C：       int 年龄 = 18;         必须写 int
//
// 为什么：C 是**编译型**语言，编译时要为每个变量分配固定的内存大小，
// 所以它必须知道你打算装什么。这也让 C 跑得很快。
//
// 这一课只用 int（整数）。小数（float/double）在第 04 课。
//
// ⚠ 常见的错：
//     年龄 = 18;              ❌ 没写类型 ⇒ 报 undeclared identifier
//     int 年龄 = 18           ❌ 漏了分号 ⇒ 报 expected ';'
//     int 年龄 = 18;          ✅
//
// 打印变量和打印常量一样，都是 printf：
//     printf("%d\n", 年龄);   ← %d 是"这里放一个整数"的占位符
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   1. int 变量 年龄 = 18
//   2. int 变量 学号 = 7
//   3. 分两行打印它们

// ── 在下面写你的代码 ──

""";

    public static string 参考答案 => """
#include <stdio.h>

int main(void) {
    int 年龄 = 18;
    int 学号 = 7;
    printf("%d\n", 年龄);
    printf("%d\n", 学号);
    return 0;
}
""";


    [检查说明("打印了 2 行：18 和 7")]
    public static CheckResult 检查_1(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "18",
            "7"
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

    [检查说明("真的定义了 int 变量（不是直接 printf 常量）")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("int 年龄"))
            return CheckResult.不过("没找到「int 年龄」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("「学号」也是 int")]
    public static CheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("int 学号"))
            return CheckResult.不过("没找到「int 学号」—— 这一课要求用到它。");
        return CheckResult.过();
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
