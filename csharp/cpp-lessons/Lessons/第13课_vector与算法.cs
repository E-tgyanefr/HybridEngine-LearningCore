using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CppLessons;

[课程("13", "第 13 课：vector 与常用算法 —— 会自己变长的数组",
    """
1. 用 vector<int> 存 {5, 2, 9, 1}，打印它的 size()
2. 用 sort 从小到大排序，然后逐行打印每个元素
3. 用 accumulate 求总和并打印；用 reverse 反转后再打印第一个元素
""")]
public static class 第13课
{
    public static string 模板 => """
// 第 13 课：vector 与常用算法 —— 会自己变长的数组
//
// 这一课讲一件事：**用 std::vector 装一串东西**，以及几个现成的算法。
//
// ─────────────────────────────────────────────────────
//  先介绍：为什么不直接用数组
// ─────────────────────────────────────────────────────
//
// 数组的格数**声明时就定死了**，而且它**自己不知道有多长** ——
// 传进函数还得另外给一个长度参数（第 06 课那个坑）。
//
// vector 两样都解决：能加能减，而且自己知道长度。
//
// ─────────────────────────────────────────────────────
//  一、vector 的常用操作
// ─────────────────────────────────────────────────────
//
//     std::vector<int> 数 = {1, 2, 3};
//
//     数.size()             3          有几个     ← 方法，有括号
//     数.push_back(4)       末尾加一个  ⇒ {1,2,3,4}
//     数.pop_back()         删掉最后一个
//     数[0]                 第 0 个（**不检查越界**）
//     数.at(0)              第 0 个（**越界会抛异常**）
//     数.empty()            空不空（true / false）
//
// 遍历：
//
//     for (int x : 数) { ... }                       ← 只读，最省事
//     for (int i = 0; i < (int)数.size(); i++) { }   ← 需要下标时
//
// ⚠ `(int)` 那个转换不能省：size() 给的是**无符号**类型，
//   直接和 int 比会警告 `comparison of integer expressions of different signedness`。
//
// ─────────────────────────────────────────────────────
//  二、几个现成的算法（都在 <algorithm> 里）
// ─────────────────────────────────────────────────────
//
//     #include <algorithm>
//
//     std::sort(数.begin(), 数.end());          从小到大排序
//     std::reverse(数.begin(), 数.end());       倒过来
//     std::max_element(数.begin(), 数.end())    最大的那个在哪（**返回的是位置**）
//     std::accumulate(...)                      求和（在 <numeric> 里）
//
// ⚠ max_element / min_element 返回的是**迭代器（位置）**，不是值：
//
//     int 最大 = *std::max_element(数.begin(), 数.end());      ← 要加 *
//     ⚠ 空 vector 上调用它是**未定义行为** —— 先判 empty()。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：循环里边遍历边删
// ─────────────────────────────────────────────────────
//
//     for (int i = 0; i < (int)数.size(); i++) {
//         if (数[i] % 2 == 0) { 数.erase(数.begin() + i); }   ← 删完下标就乱了
//     }
//
// 你会看到：**编译通过、没有警告**，只是结果不对 —— 有些该删的没删，
//   有些不该删的被跳过了。
//
// 为什么：erase 之后，后面的元素**整体往前挪了一格**，
//   而 i 照样加 1 ⇒ 你跳过了紧跟在被删元素后面的那一个。
//
// ⇒ 边遍历边删的时候，**删完不要 i++**：
//
//     if (该删) { 数.erase(数.begin() + i); } else { i++; }
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   迭代器的细节 → 用到再查。这一课只要会 begin()/end() 这两个写法。
//   map / set 等其它容器 → 这门课不涉及。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  按题目要求写几个操作 vector 的函数。
//  ⚠ 不用写 main。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """
// 第 13 课参考答案：vector 与常用算法
#include <stdio.h>
#include <vector>
#include <algorithm>
#include <numeric>        // ⚠ accumulate 在这里，**不在** <algorithm>

int main(void) {
    // ① 建一个会自己变长的数组
    std::vector<int> 数们 = {5, 2, 9, 1};
    printf("%d\n", (int)数们.size());          // 4

    // ② 排序 —— 一对 begin/end 表示"整个区间"
    std::sort(数们.begin(), 数们.end());
    for (int 数 : 数们) {                       // 范围 for：不会写错边界
        printf("%d\n", 数);
    }

    // ③ 求和 —— 第三个参数 0 是"从 0 开始累加"
    printf("%d\n", std::accumulate(数们.begin(), 数们.end(), 0));   // 17

    // ④ 反转之后再取第一个（现在最大）
    std::reverse(数们.begin(), 数们.end());
    printf("%d\n", 数们[0]);                   // 9
    return 0;
}
""";


    [检查说明("vector 的建、排序、求和、反转都对")]
    public static CheckResult 检查_1(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "4",
            "1",
            "2",
            "5",
            "9",
            "17",
            "9",
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

    [检查说明("用了 vector（不是写死的数组）")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("std::vector<int>"))
            return CheckResult.不过("没找到「std::vector<int>」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("用了 sort")]
    public static CheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("std::sort"))
            return CheckResult.不过("没找到「std::sort」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("用了 accumulate 求和")]
    public static CheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("std::accumulate"))
            return CheckResult.不过("没找到「std::accumulate」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("include 了 vector")]
    public static CheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("#include <vector>"))
            return CheckResult.不过("没找到「#include <vector>」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("include 了 algorithm")]
    public static CheckResult 检查_6(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("#include <algorithm>"))
            return CheckResult.不过("没找到「#include <algorithm>」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("include 了 numeric（accumulate 在这里）")]
    public static CheckResult 检查_7(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("#include <numeric>"))
            return CheckResult.不过("没找到「#include <numeric>」—— 这一课要求用到它。");
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
