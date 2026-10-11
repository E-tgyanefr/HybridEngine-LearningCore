using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("08", "第 08 课：函数 —— 把计算打包起来",
    """
1. 写函数 int 平方(int x)
2. 写函数 int 较大(int a, int b)：返回较大的那个
""")]
public static class 第08课
{
    public static string 模板 => """
// 第 08 课：函数 —— 把计算打包起来
//
// 先介绍：函数 = 起个名字的一段计算
//
//     int 平方(int x) {
//     ↑        ↑
//     返回类型  函数名(参数类型 参数名)
//         ...
//         return 结果;
//     }
//
// ⚠⚠ **C 的函数必须写返回类型**（Python 可以不写）：
//     平方(x) { }              ❌ 老式写法，现代 C 不允许
//     int 平方(int x) { }      ✅
//
// ⚠ 参数也要写类型，而且**每个参数都要写**：
//     int 加(int a, b)         ❌ b 没类型
//     int 加(int a, int b)     ✅
//
// ⚠ `return` 之后函数立刻结束；不是 void 的函数**每条路径都要有 return**：
//     int 判(int x) {
//         if (x > 0) return 1;
//         /* 如果 x <= 0 就走到这里，没有 return ⇒ 返回随机值！ */
//     }
//     编译器会给警告（control reaches end of non-void function），
//     但**不一定报错** ⇒ 你会拿到随机结果。
//
// ⚠ 函数写在 main **前面**；写在后面要先声明（那是第 13 课）。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   1. int 平方(int x)              返回 x * x
//   2. int 较大(int a, int b)       返回较大的那个
//
// 提示：不用写 main。

// ── 在下面写你的两个函数 ──

""";

    public static string 参考答案 => """
int 平方(int x) {
    return x * x;
}

int 较大(int a, int b) {
    if (a > b) {
        return a;
    }
    return b;          /* ⚠ 每条路径都要有 return */
}
""";


    [检查说明("两个函数都写了")]
    public static CheckResult 检查_1(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 平方(3));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过)
        {
            if (run.编译信息.Contains("平方"))
                return CheckResult.不过("没找到「平方」这个函数 —— 或者签名和题目要求不一致。");
            return CppCheck.编译失败说明(run);
        }
        return CheckResult.过("「平方」可以正常调用");
    }

    [检查说明("平方 多组都对（含 0 与负数）")]
    public static CheckResult 检查_2(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 平方(3));
    printf("%d\n", 平方(0));
    printf("%d\n", 平方(-4));
    printf("%d\n", 平方(10));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "9",
            "0",
            "16",
            "100"
        };
        string[] 输入 = {
            "3",
            "0",
            "-4",
            "10"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count
                ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
            {
                string 这是 = i < 输入.Length ? 输入[i] : "";
                return CheckResult.不过(
                    $"输入 [{这是}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 多组输入都对才算对 —— 只对一组可能是碰巧。");
            }
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("较大 多组都对（含相等）")]
    public static CheckResult 检查_3(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 较大(1,2));
    printf("%d\n", 较大(5,5));
    printf("%d\n", 较大(-3,-7));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "2",
            "5",
            "-3"
        };
        string[] 输入 = {
            "1,2",
            "5,5",
            "-3,-7"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count
                ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
            {
                string 这是 = i < 输入.Length ? 输入[i] : "";
                return CheckResult.不过(
                    $"输入 [{这是}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 多组输入都对才算对 —— 只对一组可能是碰巧。");
            }
        }
        return CheckResult.过("每一组都对");
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
