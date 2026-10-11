using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("07", "第 07 课：for —— 让程序重复做事",
    """
写函数 int 求和到(int n)：返回 1 + 2 + ... + n
""")]
public static class 第07课
{
    public static string 模板 => """
// 第 07 课：for —— 让程序重复做事
//
// 先介绍：重复做一件事
//
//     for (int i = 1; i <= 5; i++) {
//         printf("%d\n", i);
//     }
//
// for 的圆括号里有**三段**，用分号隔开：
//
//     for ( 初始化 ; 继续条件 ; 每轮之后做 )
//           int i=1   i <= 5      i++
//
//   · 初始化：循环开始前做一次
//   · 继续条件：每轮开始前检查，**为假就停**
//   · i++ 是 `i = i + 1` 的简写
//
// ⚠ 三段**都用分号隔开**，不是逗号：
//     for (int i = 1, i <= 5, i++)     ❌
//     for (int i = 1; i <= 5; i++)     ✅
//
// ⚠⚠ 这一课要记住的坑：**差一错误（off-by-one）**
//     i <= 5   跑 5 次（1,2,3,4,5）      ✅
//     i < 5    跑 4 次（1,2,3,4）        ← 少一次
//     i <= 5 但 i 从 0 开始 ⇒ 跑 6 次     ← 多一次
//   写循环时**一定要在脑子里数一遍**：第一次进循环时 i 是几？最后一次是几？
//
// 累加的套路（和 Python 一样，但**必须先初始化**）：
//     int 和 = 0;              ← ⚠ 忘了这行，和会是随机的垃圾值
//     for (...) 和 += x;
//
// ⚠ C 里未初始化的局部变量**不会自动是 0** —— 它是内存里的随机值。
//   这是 C 与 Python 的又一个大区别。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   写函数：  int 求和到(int n)
//   返回 1 + 2 + ... + n
//
//   求和到(5)  ⇒ 15
//   求和到(1)  ⇒ 1
//   求和到(0)  ⇒ 0
//
// 提示：不用写 main。

// ── 在下面写你的函数 ──

""";

    public static string 参考答案 => """
int 求和到(int n) {
    /* ⚠ 起点必须是 0 —— C 里未初始化的局部变量是随机值 */
    int 和 = 0;
    /* ⚠ i <= n 不是 i < n（少一次） */
    for (int i = 1; i <= n; i++) {
        和 += i;
    }
    return 和;
}
""";


    [检查说明("写了 求和到 函数")]
    public static CheckResult 检查_1(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 求和到(5));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过)
        {
            if (run.编译信息.Contains("求和到"))
                return CheckResult.不过("没找到「求和到」这个函数 —— 或者签名和题目要求不一致。");
            return CppCheck.编译失败说明(run);
        }
        return CheckResult.过("「求和到」可以正常调用");
    }

    [检查说明("四组都对（含 0 和 1 两个边界）")]
    public static CheckResult 检查_2(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 求和到(5));
    printf("%d\n", 求和到(1));
    printf("%d\n", 求和到(0));
    printf("%d\n", 求和到(100));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "15",
            "1",
            "0",
            "5050"
        };
        string[] 输入 = {
            "5",
            "1",
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
