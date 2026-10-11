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
// 先介绍：数组那个"长度必须写死"的毛病
//
// 第 06 课你已经知道数组的硬伤：
//
//     int 数们[5];               ← 长度写死
//     数们[5] = 6;                ← ✗ 越界，而且**不报错**（这是最坏的）
//
// 现实里的"一串数"很少能预先知道有几个：读文件、用户输入、
// 筛选结果……于是实际代码里到处是"猜一个大一点的数"：
//
//     int 数们[1000];            ← 猜
//     int 个数 = 0;              ← 还得自己记用了几个
//
// C++ 的答案叫 **vector**：一个**会自己变长**的数组。
//
//     #include <vector>
//     std::vector<int> 数们;      ← 空的，长度 0
//     数们.push_back(5);          ← 加一个，长度变成 1
//     数们.push_back(2);          ← 长度 2 … 想加多少加多少
//
// ⚠ 它和数组**用法几乎一样**（都用 `[]` 取元素），但三件事变好了：
//     ① 长度自己长 —— 不用猜、不用自己记个数
//     ② `数们.size()` 随时问得出长度
//     ③ 出了 `<algorithm>` 那一堆现成算法可以用（排序、查找、求和…）
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
// ① 建、加、取、问长度
//
//     #include <vector>
//
//     std::vector<int> 数们 = {5, 2, 9, 1};   ← 直接花括号初始化
//     数们.push_back(7);                       ← 加到末尾
//     数们.size()                              ⇒ 5     ⚠ 是**方法**（有括号）
//     数们[0]                                  ⇒ 5     ⚠ 下标从 0 开始
//     数们.at(0)                               ⇒ 5     ← 会检查越界（[] 不检查）
//
//   ⚠ `size()` 返回的类型是 `size_t`（无符号整数）。
//     拿它和 `int i` 比较时编译器会警告"有符号/无符号比较"。
//     这一课用 `for (int i = 0; i < (int)数们.size(); i++)` 强制转一下就好。
//
// ② 两种遍历
//
//     旧的（按下标）：
//         for (int i = 0; i < (int)数们.size(); i++)
//             printf("%d\n", 数们[i]);
//
//     新的（范围 for，C++11 起）—— 更短，而且**不会写错边界**：
//         for (int 数 : 数们)
//             printf("%d\n", 数);
//
//     ⚠ 想**改**元素要加 `&`：
//         for (int& 数 : 数们) 数 *= 2;      ← ✓ 真的改了
//         for (int  数 : 数们) 数 *= 2;      ← ✗ 改的是副本，白改
//
// ③ 常用算法（都在 <algorithm> / <numeric> 里）
//
//     #include <algorithm>
//     #include <numeric>          ← accumulate 在这里，不在 algorithm！
//
//     std::sort(数们.begin(), 数们.end());         ← 从小到大
//     std::reverse(数们.begin(), 数们.end());      ← 反转
//     std::accumulate(数们.begin(), 数们.end(), 0) ⇒ 总和（第三个参数是初值）
//     std::max_element(数们.begin(), 数们.end())   ⇒ **指向**最大元素的迭代器
//
//   ⚠ 每个算法都要给一对"从哪到哪"：`begin()` 和 `end()`。
//     `end()` **不是**最后一个元素，而是"最后一个元素的**下一个**位置"
//     —— 这是 C++ 的标准约定（叫"半开区间"），记住"end 是越界的那个"。
//
// ⚠ `accumulate` 在 `<numeric>` 里，不在 `<algorithm>` 里 ——
//   这是很多人第一次用的时候的"找不到符号"。用到它就两个头文件都 include。
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：对空的 vector 排序 / 取元素
//
//     std::vector<int> 空;
//     std::sort(空.begin(), 空.end());      // ✓ 其实没问题（空区间）
//     printf("%d\n", 空[0]);                // ✗ 未定义行为
//
// `空[0]` **不会**报错，它会读一块不属于你的内存 —— 可能打印个垃圾数，
// 可能崩，也可能"看起来正常"。这是 C++ 里最危险的一类错。
// 用 `空.at(0)` 会抛异常（至少你知道出事了），或者先判 `空.empty()`。
//
// ⚠ 坑 2：一边遍历一边 push_back（长度变了）
//
//     for (int 数 : 数们) {
//         if (数 > 3) 数们.push_back(数 * 2);     // ✗ 未定义行为
//     }
//
// 你会看到：可能死循环、可能崩、可能"结果少几个"。
// 为什么：范围 for 背后记着"现在走到第几个"，你在循环里把长度改了，
// 它记的位置就失效了（push_back 还可能让整块内存**搬家**，见下面）。
// ⇒ 想加就先记在另一个 vector 里，循环结束后再合并。
//
// ⚠ 坑 3：以为 push_back 之后原来的"指针/引用"还有效
//
//     std::vector<int> 数们 = {1, 2, 3};
//     int* p = &数们[0];          ← 拿到第一个元素的地址
//     数们.push_back(4);           ← 可能让**整块内存搬家**
//     printf("%d\n", *p);         ← ✗ p 已经指向一块废弃的内存
//
// 你会看到：打印出垃圾值，或者崩溃 —— 而且**加得少的时候看起来正常**，
// 换个大一点的数据才崩（因为它要扩容了）。这种"数据量小的时候对"的 bug
// 最难查。
// ⇒ 记住：**vector 扩容会让所有指向它元素的指针/引用失效。**
//   要用地址就在扩容都做完之后再取。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   main 里按顺序做：
//
//     ① std::vector<int> 数们 = {5, 2, 9, 1};
//        printf("%d\n", (int)数们.size());        → 4
//
//     ② std::sort(数们.begin(), 数们.end());
//        for (int 数 : 数们) printf("%d\n", 数);   → 1 / 2 / 5 / 9
//
//     ③ printf("%d\n", std::accumulate(数们.begin(), 数们.end(), 0));
//                                                  → 17  （1+2+5+9）
//
//     ④ std::reverse(数们.begin(), 数们.end());
//        printf("%d\n", 数们[0]);                  → 9   （反转后第一个是最大的）
//
// 期望输出：4 / 1 / 2 / 5 / 9 / 17 / 9（共 7 行）
//
// ⚠ 要 include 三个头：<vector> <algorithm> <numeric>
//   （accumulate 在 <numeric> 里 —— 漏了会说"找不到 accumulate"）
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
