using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("06", "第 06 课：if —— 让程序做选择",
    """
写函数 int 判等级(int 分)：90 及以上返回 3、60 及以上返回 2、否则返回 1
""")]
public static class 第06课
{
    public static string 模板 => """
// 第 06 课：if —— 让程序做选择
//
// 先介绍：if 让程序分岔
//
//     if (条件) {
//         条件成立时做的事
//     } else {
//         不成立时做的事
//     }
//
// ⚠ C 的 if 有**两处括号**，都要写：
//     if (分数 >= 90) {        ← 条件外面那对圆括号不能少
//         ...
//     }                        ← 花括号也别忘
//
// 比较运算符：
//     ==  等于      ⚠⚠ **两个等号**！一个等号是赋值
//     !=  不等于
//     >   大于      >=  大于等于
//     <   小于      <=  小于等于
//
// ⚠⚠ 最常见的错：
//     if (分数 = 90)           ❌ 这是**赋值**！把 90 塞进分数，然后判断 90 是不是真
//     if (分数 == 90)          ✅
//     而且 `if (分数 = 90)` **不一定报错** —— 它会永远成立（90 非 0 = 真）。
//     这就是最难查的一类错。看到 if 里只有一个等号，立刻改掉。
//
// ⚠ 判断的顺序很重要（和 Python 一样）：
//     if (分数 >= 60) return 2;
//     else if (分数 >= 90) return 3;   ← 到不了这里！
//   95 分在第一行就满足了。⇒ **从最严格的往最宽松的写**。
//
// ⚠ C 里没有"真/假"类型（C99 之前），约定：**0 是假，非 0 是真**。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   写一个函数（写在 main **外面**）：
//
//       int 判等级(int 分) {
//           ...
//       }
//
//   90 分及以上 → 返回 3
//   60 分及以上 → 返回 2
//   其他        → 返回 1
//
// 提示：本课只考这个函数，**不用写 main**（课程会提供）。

// ── 在下面写你的函数 ──

""";

    public static string 参考答案 => """
int 判等级(int 分) {
    /* ⚠ 顺序：从最严格到最宽松；边界用 >= 不是 > */
    if (分 >= 90) {
        return 3;
    } else if (分 >= 60) {
        return 2;
    } else {
        return 1;
    }
}
""";


    [检查说明("写了 判等级 函数")]
    public static CheckResult 检查_1(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 判等级(95));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过)
        {
            string 信 = run.编译信息;
            if (信.Contains("判等级"))
                return CheckResult.不过("没找到「判等级」这个函数 —— 或者它的签名和题目要求不一致。");
            return CppCheck.编译失败说明(run);
        }
        return CheckResult.过("「判等级」可以正常调用");
    }

    [检查说明("四个边界都对（95 / 90 / 60 / 59）")]
    public static CheckResult 检查_2(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
        printf("%d\n", 判等级(95));
        printf("%d\n", 判等级(90));
        printf("%d\n", 判等级(60));
        printf("%d\n", 判等级(59));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "3",
            "3",
            "2",
            "1",
        };
        string[] 输入 = {
            "95",
            "90",
            "60",
            "59",
        };
        if (run.行.Count < 期望.Length)
            return CheckResult.不过($"应该打印 {期望.Length} 行，实际 {run.行.Count} 行。");
        for (int i = 0; i < 期望.Length; i++)
            if (run.行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"判等级({输入[i]}) 应该是 {期望[i]}，实际是「{run.行[i].Trim()}」。\n" +
                    "      ⚠ 多组输入都对才算对 —— 只对一组可能是碰巧。");
        return CheckResult.过($"{期望.Length} 组输入全对");
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
