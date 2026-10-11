using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CppLessons;

[课程("15", "第 15 课：综合练习 —— 一个小统计程序",
    """
1. 写 struct 成绩（string 名字 / int 分数）
2. 写函数 平均分(vector<成绩>& 们) 返回 double
3. 写函数 最高分名字(vector<成绩>& 们) 返回 string
4. main 里造三个成绩，打印 个数、平均分、最高分名字
""")]
public static class 第15课
{
    public static string 模板 => """
// 第 15 课：综合练习 —— 一个小统计程序
//
// 这一课**不教新东西**。它把前面 14 课的东西拼起来，做一个真能用的小程序：
// 读一组"名字 + 分数"，算出人数、平均分、最高分是谁。
//
// 为什么要有这么一课：
//   前 14 课每一课只考一个点（循环、struct、vector、字符串…），
//   而**真实程序里这些东西永远是一起用的**。
//   你会在这课里体会到"单独都会，合起来就卡" —— 那是正常现象，
//   也正是这一课要练的：**把大问题拆成几个小函数**。
//
// ─────────────────────────────────────────────────────
//  拆解：一个大问题 → 三个小函数
// ─────────────────────────────────────────────────────
//
// 题目是"做统计"。直接写 main 会变成一大坨。先拆：
//
//     struct 成绩 {              ← ① 数据长什么样
//         std::string 名字;
//         int 分数;
//     };
//
//     int 人数(清单)              ← ② 每个问题一个小函数
//     double 平均分(清单)
//     std::string 最高分名字(清单)
//
//   ⚠ 「清单」的类型：`std::vector<成绩>`
//     读作"一堆成绩"。vector 里可以装你自己定义的类型 —— 这很常用。
//
// ─────────────────────────────────────────────────────
//  三个要留意的点
// ─────────────────────────────────────────────────────
//
// ① **整数除法**（这个坑你在第 02 课见过，这里会再撞一次）
//
//     double 平均 = 总和 / 个数;          // ✗ 两个 int 相除 ⇒ 整数除法，小数被丢掉
//     double 平均 = (double)总和 / 个数;   // ✓ 至少一边是 double
//
//   症状：三个人的分数是 80 / 85 / 90，你算出 85 而不是 85.0 ——
//   更糟的是 80 / 80 / 81 会算出 80（正确是 80.333…），
//   看着"差不多"，但它是错的。**这类错不会报错，只会算错。**
//
// ② 空清单要单独处理
//
//     清单[0]                  ← 清单为空时是**未定义行为**
//     总和 / (int)清单.size()   ← 除以 0 ⇒ 崩溃
//
//   真实程序里"没有数据"是常态（文件是空的、筛选后一个都不剩）。
//   所以每个函数开头都该先想一句：**空的时候返回什么？**
//     人数为空 → 0    平均分为空 → 0    最高分名字为空 → 空字符串
//
// ③ 字符串要用 `std::string`，不是 `char[]`
//
//     #include <string>
//     std::string 名字 = "甲";     ← 可以赋值、可以比较（==）、可以 .size()
//
//   ⚠ `char 名字[20]` 那种老写法不能赋值（`名字 = "甲"` 是错的），
//     比较也不能用 ==（比的是地址）。**现代 C++ 一律用 std::string。**
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   ① `struct 成绩`：std::string 名字; 和 int 分数;
//
//   ② `double 平均分(std::vector<成绩>& 们)`
//        · 空的时候返回 0
//        · 用循环求和，⚠ 除的时候至少一边转 double
//
//   ③ `std::string 最高分名字(std::vector<成绩>& 们)`
//        · 空的时候返回 ""
//        · 先假设第 0 个最高，然后从第 1 个开始比
//
//   ④ main 里：
//        std::vector<成绩> 表 = { {"甲", 80}, {"乙", 90}, {"丙", 85} };
//        printf("%d\n", (int)表.size());                     → 3
//        printf("%.2f\n", 平均分(表));                        → 85.00
//        printf("%s\n", 最高分名字(表).c_str());              → 乙
//
// 期望输出三行：3 / 85.00 / 乙
//
// ⚠ 打印 std::string 要写 `.c_str()`（printf 的 %s 只认 C 风格的字符串）。
//   （cout 可以直接打，但这一课一直用的是 printf。）
""";

    public static string 参考答案 => """
// 第 15 课参考答案：综合练习 —— 一个小统计程序
#include <stdio.h>
#include <string>
#include <vector>

// ① 数据长什么样
struct 成绩 {
    std::string 名字;      // ⚠ 用 std::string，不是 char[]
    int 分数;
};

// ② 平均分
double 平均分(const std::vector<成绩>& 们) {
    if (们.empty()) return 0.0;          // 空的时候先返回，别等到除零
    int 总和 = 0;
    for (const 成绩& 一 : 们) {           // ⚠ 加 & 就不复制；加 const 表示只读
        总和 += 一.分数;
    }
    // ⚠⚠ 至少一边转成 double，否则这里是**整数除法**（小数被丢掉）
    return (double)总和 / (int)们.size();
}

// ③ 最高分是谁
std::string 最高分名字(const std::vector<成绩>& 们) {
    if (们.empty()) return "";            // 空清单不能取 [0]
    int 最好 = 0;                         // 先假设第 0 个最高
    for (int i = 1; i < (int)们.size(); i++) {   // ⚠ 从 1 开始比
        if (们[i].分数 > 们[最好].分数) 最好 = i;
    }
    return 们[最好].名字;
}

int main(void) {
    std::vector<成绩> 表 = { {"甲", 80}, {"乙", 90}, {"丙", 85} };

    printf("%d\n", (int)表.size());                 // 3
    printf("%.2f\n", 平均分(表));                    // 85.00
    printf("%s\n", 最高分名字(表).c_str());          // 乙
    return 0;
}
""";


    [检查说明("人数、平均分、最高分名字都对")]
    public static CheckResult 检查_1(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "3",
            "85.00",
            "乙",
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

    [检查说明("定义了 struct 成绩")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("struct 成绩"))
            return CheckResult.不过("没找到「struct 成绩」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("用了 std::string（不是 char 数组）")]
    public static CheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("std::string 名字"))
            return CheckResult.不过("没找到「std::string 名字」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("平均分里做了 double 转换（避开整数除法）")]
    public static CheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("(double)总和"))
            return CheckResult.不过("没找到「(double)总和」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("空清单被处理了")]
    public static CheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("empty()"))
            return CheckResult.不过("没找到「empty()」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("最高分是先假设第 0 个、再从 1 开始比")]
    public static CheckResult 检查_6(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("int 最好 = 0"))
            return CheckResult.不过("没找到「int 最好 = 0」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("用了 vector<成绩>")]
    public static CheckResult 检查_7(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("std::vector<成绩>"))
            return CheckResult.不过("没找到「std::vector<成绩>」—— 这一课要求用到它。");
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
