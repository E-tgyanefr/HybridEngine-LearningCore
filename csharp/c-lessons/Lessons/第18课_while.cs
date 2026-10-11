using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("18", "第 18 课：while 与 do-while —— 不知道要做几次的时候",
    """
1. 用 while 写 数位和(数)：返回一个正整数的各位数字之和
2. 用 do-while 写 至少做一次(数)：同一个计算，但用后判断的循环
""")]
public static class 第18课
{
    public static string 模板 => """
// 第 18 课：while 与 do-while —— 不知道要做几次的时候
//
// 先介绍：for 和 while 什么时候用哪个
//
//     第 07 课的 for   —— **你知道要做几次**
//                          "从 1 加到 100"、"遍历这个数组"
//
//     这一课的 while  —— **你不知道要做几次**，
//                          只知道"什么时候该停"
//                          "一直取数字，直到取完为止"
//
// 三条循环长得像，各有各的用处：
//
//     while (条件) { ... }        先判断，再做。**可能一次都不做**
//     do { ... } while (条件);    先做一次，再判断。**至少做一次**
//     for (...) { ... }           三段式。次数明确时最清楚
//
// ─────────────────────────────────────────────────────
//  一、while 的形状
// ─────────────────────────────────────────────────────
//
//     初始化;
//     while (条件) {
//         做事;
//         **改变量**;        ← ⚠⚠ 这一行忘了就是死循环
//     }
//
// ⚠⚠ **while 最常见的错：忘了在循环体里改变量。**
//
//     int i = 0;
//     while (i < 5) {
//         printf("%d\n", i);
//         /* 忘了 i++ ⇒ i 永远是 0 ⇒ 条件永远成立 ⇒ 死循环 */
//     }
//
//   这条和 for 的区别很值得体会：
//     for 把"改变量"写在**括号里**，想忘都忘不了；
//     while 把"改变量"丢在**循环体里**，忘了也没人拦你，程序就那么卡住。
//   ⇒ 写 while 的时候，先问自己一句：**"哪个变量在变？它在哪里变？"**
//
// ─────────────────────────────────────────────────────
//  二、do-while 的形状（末尾那个分号不能少）
// ─────────────────────────────────────────────────────
//
//     do {
//         做事;
//     } while (条件);        ← ⚠ 末尾**必须**有分号
//
//  它和 while 唯一的差别：**先做一次，再判断**。
//  所以"条件一开始就不成立"的时候，while 一次都不做，do-while 会做一次。
//
//      int i = 100;
//      while (i < 5) { printf("while 跑了\n"); }      // 一次都不跑
//      do { printf("do-while 跑了\n"); } while (i < 5); // 跑一次
//
//  什么时候真的需要它？**"至少得做一次"的场合**：
//  菜单（至少显示一次）、读输入（至少读一次才知道对不对）、
//  重试（至少试一次）。这些用 while 写都要在循环外面先抄一遍，很别扭。
//
// ─────────────────────────────────────────────────────
//  三、这一课的例子：数位和
// ─────────────────────────────────────────────────────
//
//  算 1234 的各位之和（1+2+3+4 = 10）。你为什么**不知道要做几次**？
//  因为 5 是一位数、12345678 是八位数 —— 循环次数取决于输入。
//
//  两个工具（都是整数运算，正好复习第 03 课）：
//
//     数 % 10    取最后一位      1234 % 10  = 4
//     数 / 10    把最后一位去掉   1234 / 10  = 123   （整数除法，小数被丢掉）
//
//  于是循环体是：
//
//     和 += 数 % 10;        拿走最后一位，加到和里
//     数 /= 10;             把最后一位扔掉
//                            ↑ 这就是"让循环往前走"的那一步
//
//  什么时候停？**数变成 0 的时候**（没位数可取了）。
//
//  ⚠⚠ 这里有个边界要想清楚：**输入是 0 怎么办？**
//
//     while (数 > 0) {...}      ⇒ 0 一次都不进循环 ⇒ 返回 0     ✅ 对
//     do { ... } while (数 > 0); ⇒ 会先做一次，返回 0 也是 0    ✅ 也对
//   这一课要你**两种都写一遍**，然后自己体会：这个题目里它们结果一样，
//   但换个题目（比如"至少问一次用户"）就不一样了。
//
// ⚠ 还有个负数问题：`-123 % 10` 在 C 里是 **-3**（不是 3）。
//   检查只喂正数，所以不用管；但你要知道这件事存在 ——
//   真实项目里输入永远可能带负号。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   写两个函数：
//
//     1. int 数位和(int 数)         用 **while**
//     2. int 至少做一次(int 数)     用 **do-while**，算法完全一样
//
//   两个都应该：1234 → 10、7 → 7、0 → 0
//
// 提示：不用写 main。

// ── 在下面写你的两个函数 ──

""";

    public static string 参考答案 => """
int 数位和(int 数) {
    int 和 = 0;
    /* ⚠ 条件写 数 > 0：数变成 0 就不进循环了（输入是 0 时直接返回 0，正确） */
    while (数 > 0) {
        和 += 数 % 10;      /* 拿走最后一位 */
        数 /= 10;            /* ⚠⚠ 这一行是"让循环往前走"的关键 —— 删了它必死循环 */
    }
    return 和;
}

int 至少做一次(int 数) {
    int 和 = 0;
    /* do-while：先做一次再判断。
       这个题目里它和 while 结果一样（因为 0 加不加都是 0），
       但换个题目就不一样了 —— 比如"至少问用户一次"。 */
    do {
        和 += 数 % 10;
        数 /= 10;
    } while (数 > 0);       /* ⚠ 末尾这个分号不能少 */
    return 和;
}
""";


    [检查说明("写了 数位和 函数")]
    public static CheckResult 检查_1(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 数位和(1234));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过)
        {
            if (run.编译信息.Contains("数位和"))
                return CheckResult.不过("没找到「数位和」这个函数 —— 或者签名和题目要求不一致。");
            return CppCheck.编译失败说明(run);
        }
        return CheckResult.过("「数位和」可以正常调用");
    }

    [检查说明("数位和 用 while 算对了")]
    public static CheckResult 检查_2(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 数位和(1234));
    printf("%d\n", 数位和(7));
    printf("%d\n", 数位和(0));
    printf("%d\n", 数位和(999));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "10",
            "7",
            "0",
            "27"
        };
        string[] 输入 = {
            "1234",
            "7",
            "0",
            "999"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
                return CheckResult.不过(
                    $"输入 [{(i < 输入.Length ? 输入[i] : "")}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 多组输入都对才算对 —— 只对一组可能是碰巧。");
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("写了 至少做一次 函数")]
    public static CheckResult 检查_3(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 至少做一次(1234));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过)
        {
            if (run.编译信息.Contains("至少做一次"))
                return CheckResult.不过("没找到「至少做一次」这个函数 —— 或者签名和题目要求不一致。");
            return CppCheck.编译失败说明(run);
        }
        return CheckResult.过("「至少做一次」可以正常调用");
    }

    [检查说明("至少做一次 也算对了（do-while 写法）")]
    public static CheckResult 检查_4(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 至少做一次(1234));
    printf("%d\n", 至少做一次(7));
    printf("%d\n", 至少做一次(0));
    printf("%d\n", 至少做一次(100));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "10",
            "7",
            "0",
            "1"
        };
        string[] 输入 = {
            "1234",
            "7",
            "0",
            "100"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
                return CheckResult.不过(
                    $"输入 [{(i < 输入.Length ? 输入[i] : "")}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 多组输入都对才算对 —— 只对一组可能是碰巧。");
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("真的用了 while")]
    public static CheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("while"))
            return CheckResult.不过("没找到「while」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("真的用了 do-while")]
    public static CheckResult 检查_6(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("do {"))
            return CheckResult.不过("没找到「do {」—— 这一课要求用到它。");
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
