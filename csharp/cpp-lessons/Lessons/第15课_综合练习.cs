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
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课**不教新东西**。前面 14 课学的，这一课全部用一遍。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    把前面学的拼起来做一个小统计程序（综合练习）
//
//  需要先会
//    第 02 课 cin + 第 13 课 vector
//
// ─────────────────────────────────────────────────────
//  先介绍：为什么要有综合练习
// ─────────────────────────────────────────────────────
//
// 单独会用一个语法点，和**把它们拼起来做成一件事**，是两种能力。
// 这一课就是把零散的点连起来：读输入 → 存起来 → 算 → 输出。
//
// ─────────────────────────────────────────────────────
//  用法与例子：这一课会用到什么
// ─────────────────────────────────────────────────────
//
//     第 02 课   std::cin 读输入、整数除法
//     第 04 课   for 循环、累加变量给初值
//     第 05 课   函数、return
//     第 06 课   vector 装一串数
//     第 13 课   sort、max_element
//
// 一个完整的骨架（照这个往上加）：
//
//     #include <iostream>
//     #include <vector>
//     #include <algorithm>
//
//     int main() {
//         int n = 0;
//         if (!(std::cin >> n)) { std::cout << "输入无效"; return 0; }
//
//         std::vector<int> 数;
//         for (int i = 0; i < n; i++) {
//             int x = 0;
//             std::cin >> x;
//             数.push_back(x);
//         }
//         ...算...
//     }
//
// ─────────────────────────────────────────────────────
//  三个要留意的点
// ─────────────────────────────────────────────────────
//
//  ① **先判 n，再读**：n <= 0 的时候不要进循环（差一错误最容易在这儿）。
//  ② **求平均要防丢小数**：`和 / n` 两边都是 int ⇒ 写成 `(double)和 / n`。
//  ③ **空 vector 上别调 max_element**：先 `if (数.empty())` 拦一下。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   没有新语法。做不出来就回头翻对应的那一课。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  按题目要求写一个完整的统计程序。
//  ⚠ 这一课要自己写 #include 和 main。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
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
