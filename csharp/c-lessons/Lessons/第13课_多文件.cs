using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("13", "第 13 课：函数声明 —— 写在后面也能用",
    """
1. 写函数 int 三倍(int x)（**定义写在 main 后面**）
2. 在 main 前面写它的**声明**
""")]
public static class 第13课
{
    public static string 模板 => """
// 第 13 课：函数声明 —— 写在后面也能用
//
// 先介绍：为什么需要"声明"
//
// C 编译器是**从上往下**读的。所以这样写会报错：
//
//     int main(void) {
//         printf("%d\n", 三倍(5));     ← 报 implicit declaration of '三倍'
//         return 0;
//     }
//     int 三倍(int x) { return x * 3; }   ← 定义在下面，编译器还没读到
//
// 解决：**在使用之前先"声明"一下**（也叫函数原型）：
//
//     int 三倍(int x);              ← 声明：只有类型，没有函数体，末尾分号
//
//     int main(void) {
//         printf("%d\n", 三倍(5));  ← 现在编译器知道有这么个函数了
//         return 0;
//     }
//
//     int 三倍(int x) {             ← 定义：真正的实现
//         return x * 3;
//     }
//
// ⚠⚠ 这一课要记住的两件事：
//
//   ① **声明 vs 定义**
//        声明：`int 三倍(int x);`     末尾**有分号**，没有 { }
//        定义：`int 三倍(int x) { ... }`  有函数体
//      放分号的地方不同，作用完全不同。
//
//   ② **为什么报错信息里说 implicit declaration**
//      那是编译器在说"我没见过这个函数，就当你返回 int 吧"——
//      这是个**警告级**的问题，但现代 C 已经当错误处理了。
//      看到它，就是"函数定义在使用之后，而且没声明"。
//
// ⚠ 这也解释了 #include 在干嘛：`#include <stdio.h>` 里就是
//   printf 的**声明**。所以你在 main 里能用 printf。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   1. 在 main **前面**写 `int 三倍(int x);`（声明）
//   2. 在 main **后面**写 `int 三倍(int x) { return x * 3; }`（定义）
//   3. main 里调用它并打印

// ── 在下面写你的代码 ──

""";

    public static string 参考答案 => """
#include <stdio.h>

int 三倍(int x);            /* ⚠ 声明：末尾有分号，没有 {} */

int main(void) {
    printf("%d\n", 三倍(5));
    return 0;
}

int 三倍(int x) {           /* ⚠ 定义在下面 */
    return x * 3;
}
""";


    [检查说明("程序能编译运行（说明声明写对了）")]
    public static CheckResult 检查_1(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "15"
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

    [检查说明("打印了 15")]
    public static CheckResult 检查_2(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "15"
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

    [检查说明("声明在定义前面")]
    public static CheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        int 主 = 去注释.IndexOf("int main");
        if (主 < 0)
            return CheckResult.不过("这一课要求写完整的 main —— 找不到 int main。");
        string 前 = 去注释.Substring(0, 主);
        string 后 = 去注释.Substring(主);
        if (!前.Contains("int 三倍"))
            return CheckResult.不过("声明要写在 main **前面**（写法：int 三倍(int x); 末尾有分号），否则编译器读不到它。");
        if (!后.Contains("int 三倍"))
            return CheckResult.不过("函数定义要写在 main **后面** —— 这一课练的就是这个顺序。");
        return CheckResult.过("声明在 main 前、定义在 main 后 —— 顺序对了");
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
