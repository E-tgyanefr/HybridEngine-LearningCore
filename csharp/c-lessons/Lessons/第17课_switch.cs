using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("17", "第 17 课：switch —— 一个值对上好几个可能",
    """
写函数 季节(月)（用 switch 加贯穿实现）：3-5 月返回 "春"、6-8 月返回 "夏"、9-11 月返回 "秋"、12/1/2 月返回 "冬"、其它返回 "不认识"
""")]
public static class 第17课
{
    public static string 模板 => """
// 第 17 课：switch —— 一个值对上好几个可能
//
// 先介绍：和 if 是两种不同的东西
//
//     if      —— **判断条件**（可以是很复杂的表达式）
//     switch  —— **把同一个值和一堆候选值比相等**
//
//         switch (月) {
//             case 3:
//                 ...
//                 break;
//             case 4:
//             case 5:
//                 ...
//                 break;
//             default:
//                 ...
//         }
//
// 用哪个？一条实用判据：
//
//     「等于…等于…等于…」        → switch 更清楚
//     「大于、小于、并且、或者」   → 只能用 if
//
// ⚠ switch 里**只能比整数和字符**，不能比字符串、不能比小数。
//   （`case "你好":` 编译不过 —— 字符串比较要用 strcmp，第 10 课讲的那件事。）
//
// ─────────────────────────────────────────────────────
//  ⚠⚠ 这一课唯一要记住的坑：break
// ─────────────────────────────────────────────────────
//
// **switch 不会自己停下来。** 一个 case 做完了，它会**继续往下做下一个 case**。
// 这叫「贯穿」（fallthrough），是 C 里最坑人的设计之一。
//
//     switch (2) {
//         case 1: printf("一\n");        // 不执行
//         case 2: printf("二\n");        // 执行 —— 从这里进去
//         case 3: printf("三\n");        // **也执行！** 因为上面没 break
//     }
//
//     实际输出：二、三          ← 不是你以为的"只有二"
//
// ⇒ **每个 case 末尾几乎都要写 break;**，忘了它就等于让程序"漏"下去。
//
// ⚠ 但贯穿**有时候是故意的** —— 而且这是这一课要你用的写法。
//   想表达"3、4、5 都算春天"，可以叠着写：
//
//     case 3:
//     case 4:
//     case 5:
//         return "春";          ← 三个标签共用同一段代码
//
//   这比写 `if (月 == 3 || 月 == 4 || 月 == 5)` 短，也不容易漏掉一个。
//   ⇒ 所以贯穿既是坑、也是工具 —— **关键是你要知道自己有没有用它。**
//
// ⚠ 别忘 default：所有 case 都不匹配时走它。
//   没有 default 又不匹配 —— 什么都不做，而你要自己保证"什么都不做"是对的。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   写函数：  char* 季节(int 月)
//
//     3 4 5   → "春"
//     6 7 8   → "夏"
//     9 10 11 → "秋"
//     12 1 2  → "冬"
//     其它    → "不认识"
//
//   ⚠ 用 switch，而且**故意用贯穿**把 3/4/5 叠在一起 —— 这一课练的就是这个。
//   ⚠ 返回字符串要用双引号：return "春";（不是 '春'，单引号是单个字符）
//
// 提示：不用写 main。

// ── 在下面写你的函数 ──

""";

    public static string 参考答案 => """
char* 季节(int 月) {
    switch (月) {
        /* ⚠ 这三个 case 故意叠在一起 —— 贯穿用在这里是对的：
              它们共用同一段代码，比写 if (月==3 || 月==4 || 月==5) 更清楚。
              ⚠ 注意最后那个 case 5 后面的代码，末尾一定要 break。 */
        case 3:
        case 4:
        case 5:
            return "春";
        case 6:
        case 7:
        case 8:
            return "夏";
        case 9:
        case 10:
        case 11:
            return "秋";
        case 12:
        case 1:
        case 2:
            return "冬";
        default:
            /* 其它月份（比如 13）走到这里。
               ⚠ 别省掉 default：没有它、又不匹配时，函数会掉到末尾，
                 而"非 void 函数掉到末尾"返回的是**随机值**。 */
            return "不认识";
    }
}
""";


    [检查说明("写了 季节 函数")]
    public static CheckResult 检查_1(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 季节(3));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过)
        {
            if (run.编译信息.Contains("季节"))
                return CheckResult.不过("没找到「季节」这个函数 —— 或者签名和题目要求不一致。");
            return CppCheck.编译失败说明(run);
        }
        return CheckResult.过("「季节」可以正常调用");
    }

    [检查说明("四季都判对了")]
    public static CheckResult 检查_2(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%s\n", 季节(3));
    printf("%s\n", 季节(6));
    printf("%s\n", 季节(9));
    printf("%s\n", 季节(12));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "春",
            "夏",
            "秋",
            "冬"
        };
        string[] 输入 = {
            "3",
            "6",
            "9",
            "12"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
                return CheckResult.不过(
                    $"输入 [{(i < 输入.Length ? 输入[i] : "")}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 返回的是 C 字符串（双引号），不是字符（单引号）。");
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("边界都对（5/8/11/2 是各季的最后一月）")]
    public static CheckResult 检查_3(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%s\n", 季节(5));
    printf("%s\n", 季节(8));
    printf("%s\n", 季节(11));
    printf("%s\n", 季节(2));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "春",
            "夏",
            "秋",
            "冬"
        };
        string[] 输入 = {
            "5",
            "8",
            "11",
            "2"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
                return CheckResult.不过(
                    $"输入 [{(i < 输入.Length ? 输入[i] : "")}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 返回的是 C 字符串（双引号），不是字符（单引号）。");
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("不认识的月份返回「不认识」（说明 default 没漏）")]
    public static CheckResult 检查_4(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%s\n", 季节(13));
    printf("%s\n", 季节(0));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "不认识",
            "不认识"
        };
        string[] 输入 = {
            "13",
            "0"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
                return CheckResult.不过(
                    $"输入 [{(i < 输入.Length ? 输入[i] : "")}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 返回的是 C 字符串（双引号），不是字符（单引号）。");
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("用了 switch（这一课就是练它）")]
    public static CheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("switch"))
            return CheckResult.不过("没找到「switch」—— 这一课要求用到它。");
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
