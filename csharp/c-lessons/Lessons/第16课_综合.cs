using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("16", "第 16 课：综合练习 —— 一个小统计程序",
    """
写函数 void 统计(int 数们[], int 个数, int *最大, int *最小)：把最大/最小值写回调用者那里
""")]
public static class 第16课
{
    public static string 模板 => """
// 第 16 课：综合练习 —— 一个小统计程序
//
// 这一课不是新语法 —— 是把前面学的**拼起来**：
//
//     第 06 课  if            比大小
//     第 07 课  for           遍历
//     第 09 课  数组 + 个数参数
//     第 11 课  指针改参数     ← 这一课的重点
//
// 任务：写一个函数，一次算出**最大**和**最小**两个结果。
//
// ⚠ 难点在于：一个函数只能 `return` **一个**值。
//   要交回两个结果，就得用**指针参数** —— 把结果写进调用者给的地址里：
//
//     void 统计(int 数们[], int 个数, int *最大, int *最小) {
//         *最大 = ...;      ← 写进调用者的变量
//         *最小 = ...;
//     }
//
//   调用时：
//     int 大, 小;
//     统计(数据, 5, &大, &小);      ← 传地址
//     printf("%d %d\n", 大, 小);
//
// ⚠⚠ 三个必须注意的点：
//
//   ① **初值要从第一个元素起**，不能写 0：
//        int 大 = 0;                     ❌ 全负数时会返回 0
//        int 大 = 数们[0];               ✅
//
//   ② **个数为 0 要特判** —— 否则 `数们[0]` 就越界读了：
//        if (个数 <= 0) return;
//      这是 C 里最常见的崩溃来源之一。
//
//   ③ 写结果要写 **`*最大`**，不是 `最大`：
//        *最大 = 数们[i];                ✅
//        最大 = &数们[i];                ❌ 那是在改指针本身，外面看不到
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   写函数：  void 统计(int 数们[], int 个数, int *最大, int *最小)
//   把最大值写进 *最大，最小值写进 *最小
//   个数为 0 时直接返回（什么都别写）
//
// 提示：不用写 main。

// ── 在下面写你的函数 ──

""";

    public static string 参考答案 => """
void 统计(int 数们[], int 个数, int *最大, int *最小) {
    /* ⚠ 空数组要特判，否则 数们[0] 就越界读了 */
    if (个数 <= 0) {
        return;
    }
    /* ⚠ 初值从第一个元素起 —— 写 0 的话全负数就错了 */
    int 大 = 数们[0];
    int 小 = 数们[0];
    for (int i = 1; i < 个数; i++) {
        if (数们[i] > 大) 大 = 数们[i];
        if (数们[i] < 小) 小 = 数们[i];
    }
    *最大 = 大;              /* ⚠ *最大 才是"写进调用者的变量" */
    *最小 = 小;
}
""";


    [检查说明("写了 统计 函数")]
    public static CheckResult 检查_1(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("统计"))
            return CheckResult.不过("没找到「统计」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("能编译（指针参数用法正确）")]
    public static CheckResult 检查_2(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        return CheckResult.过();
    }

    [检查说明("最大最小值都对（含全负数与单元素）")]
    public static CheckResult 检查_3(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    int __a0[] = {3, 1, 4, 1, 5};
    int __d0 = -999, __x0 = 999;
    统计(__a0, 5, &__d0, &__x0);
    printf("%d %d\n", __d0, __x0);
    int __a1[] = {-5, -1, -9};
    int __d1 = -999, __x1 = 999;
    统计(__a1, 3, &__d1, &__x1);
    printf("%d %d\n", __d1, __x1);
    int __a2[] = {7};
    int __d2 = -999, __x2 = 999;
    统计(__a2, 1, &__d2, &__x2);
    printf("%d %d\n", __d2, __x2);
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "5 1",
            "-1 -9",
            "7 7"
        };
        string[] 输入 = {
            "3,1,4,1,5",
            "-5,-1,-9",
            "7"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
                return CheckResult.不过(
                    $"输入 [{(i < 输入.Length ? 输入[i] : "")}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 两个坑：① 初值要从第一个元素起 ② 空数组要特判");
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
