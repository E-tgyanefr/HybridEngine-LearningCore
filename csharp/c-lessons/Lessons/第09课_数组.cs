using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("09", "第 09 课：数组 —— 一排格子",
    """
写函数 int 求和(int 数们[], int 个数)：返回所有元素之和
""")]
public static class 第09课
{
    public static string 模板 => """
// 第 09 课：数组 —— 一排格子
//
// 先介绍：一排连续的格子
//
//     int 成绩[5];                 ← 声明 5 个格子
//     int 成绩[5] = {90, 75, 60, 45, 88};
//
//     成绩[0]   ⇒ 90        ⚠ 下标**从 0 开始**
//     成绩[4]   ⇒ 88        最后一个
//
// ⚠⚠⚠ **这一课唯一要记住的坑：C 的数组不检查越界。**
//
//     成绩[5]   ← 越界了！但 C **不报错**，它会读/写到别的内存去。
//                 写越界可能让你后面完全不相干的变量莫名其妙变值，
//                 或者程序直接崩（Segmentation fault）。
//
//   为什么：C 追求速度，不做运行时检查。数组名本质上只是一个地址，
//   `成绩[5]` 被算成"地址 + 5 个格子"，它不问你是不是超出去了。
//   ⇒ **下标的正确性是你自己的责任。**
//
// ⚠ 数组**没有长度信息**：用 `sizeof(数组)/sizeof(数组[0])` 只能算
//   **在同一个函数内**声明的数组。传进函数的数组会"退化"成指针，
//   所以**必须另外传一个"个数"参数** —— 这就是这一课的函数有两个参数的原因。
//
//     int 求和(int 数们[], int 个数)     ← 个数必须自己传
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   写函数：  int 求和(int 数们[], int 个数)
//   返回所有元素之和；个数是 0 时返回 0
//
// 提示：不用写 main。

// ── 在下面写你的函数 ──

""";

    public static string 参考答案 => """
int 求和(int 数们[], int 个数) {
    /* ⚠ 起点 0 —— C 未初始化的局部变量是随机值 */
    int 和 = 0;
    /* ⚠ i < 个数 不是 i <= 个数（那会越界读一个） */
    for (int i = 0; i < 个数; i++) {
        和 += 数们[i];
    }
    return 和;
}
""";


    [检查说明("写了 求和 函数（带个数参数）")]
    public static CheckResult 检查_1(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 求和(0,0));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过)
        {
            if (run.编译信息.Contains("求和"))
                return CheckResult.不过("没找到「求和」这个函数 —— 或者签名和题目要求不一致。");
            return CppCheck.编译失败说明(run);
        }
        return CheckResult.过("「求和」可以正常调用");
    }

    [检查说明("数组求和多组都对（含负数）")]
    public static CheckResult 检查_2(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    int __a0[] = {5, 3, 1, 2, 3};
    printf("%d\n", 求和(__a0, 5));
    int __a1[] = {5, 1, 100};
    printf("%d\n", 求和(__a1, 3));
    int __a2[] = {5, 3, -1, -2, -3};
    printf("%d\n", 求和(__a2, 5));
    int __a3[] = {3, 0, 7, 8, 9};
    printf("%d\n", 求和(__a3, 5));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "14",
            "106",
            "2",
            "27"
        };
        string[] 输入 = {
            "5,3,1,2,3",
            "5,1,100",
            "5,3,-1,-2,-3",
            "3,0,7,8,9"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
                return CheckResult.不过(
                    $"输入 [{(i < 输入.Length ? 输入[i] : "")}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 函数拿到的是地址 ⇒ 改的就是原数组（或返回值要对）。");
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
