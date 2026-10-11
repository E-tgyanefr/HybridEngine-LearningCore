using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CppLessons;

[课程("12", "第 12 课：结构体与类 —— 把一堆东西捆成一个",
    """
1. 写一个 struct 点，两个 int 字段 x 和 y，以及一个函数 点 造点(int x, int y)
2. 写一个函数 int 距离平方(点 a, 点 b)：返回 (a.x-b.x)^2 + (a.y-b.y)^2
3. main 里造两个点并打印距离平方，再打印 a.x 和 b.x
""")]
public static class 第12课
{
    public static string 模板 => """
// 第 12 课：结构体与类 —— 把一堆东西捆成一个
//
// 这一课讲一件事：**自己造一种类型**，把几个值绑在一起。
//
// ─────────────────────────────────────────────────────
//  先介绍：一个学生不只是「一个名字」
// ─────────────────────────────────────────────────────
//
// 一个学生有名字、有分数、有班级。用三个变量分开装当然也行，
// 但它们**其实是同一条数据的几半** —— 而你没法把它们一起传来传去。
//
//     struct 学生 {
//         std::string 名字;
//         int 分数;
//     };
//
// 从此 `学生` 就和 `int` 一样是个**类型**了。
//
// ─────────────────────────────────────────────────────
//  一、struct 的基本用法
// ─────────────────────────────────────────────────────
//
//     struct 学生 {
//         std::string 名字;         ← 成员
//         int 分数;
//     };                            ⚠ 结尾这个分号不能少
//
//     student 甲;                    ← 造一个（这时还是空的）
//     甲.名字 = "小明";              用 . 访问成员
//     甲.分数 = 92;
//
//     std::cout << 甲.名字 << std::endl;      ⇒ 小明
//
// 一次性造好也行（**聚合初始化**，按声明顺序给值）：
//
//     学生 甲 = {"小明", 92};
//
// ─────────────────────────────────────────────────────
//  二、struct 和 class 的区别
// ─────────────────────────────────────────────────────
//
//     差不多是同一件东西，只差一个默认值：
//
//         struct  成员默认是 **public**（外面能直接访问）
//         class   成员默认是 **private**（外面不能访问）
//
// 约定俗成：**只装数据就用 struct，要藏东西/带行为就用 class。**
//
// class 里可以写函数（叫「方法」），struct 也可以 —— 语法一样。
//
// ─────────────────────────────────────────────────────
//  三、整体赋值会复制一份
// ─────────────────────────────────────────────────────
//
//     学生 甲 = {"小明", 92};
//     学生 乙 = 甲;              ← **复制**：乙 是另一份
//     乙.分数 = 100;
//     std::cout << 甲.分数;      ⇒ 92    ← 甲 没被影响
//
// ⚠ 这一点和 Java / C# **相反** —— 那两边这样做只是给同一个对象起了个新名字。
//   C++ 的 struct 是**值语义**：赋值 = 复制。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：忘了结尾的分号
// ─────────────────────────────────────────────────────
//
//     struct 学生 {
//         std::string 名字;
//         int 分数;
//     }                             ← 少了这个分号
//
//     int main() { ... }
//
// 你会看到（g++ 原文）：
//
//     error: expected ';' after struct definition
//
// 为什么容易漏：函数的 `}` 后面**不写**分号，struct 的 `}` 后面**要写** ——
//   两个都是花括号结尾，习惯很容易带错。
//
// ⇒ 记法：**struct / class 的定义，花括号后面跟一个分号。**
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   继承、多态 —— 这门课不涉及（那是 Java / C# 线的第 12、13 课）。
//   构造函数 —— 这门课用「聚合初始化」就够。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  按题目要求定义一个 struct，并写几个函数处理它。
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
// 第 12 课参考答案：结构体与类
#include <stdio.h>

// ① 把 x 和 y 捆成一个新类型
//    ⚠ 花括号后面这个分号**必须写** —— 这是 struct/class/enum 独有的规矩
struct 点 {
    int x;
    int y;
};

// ② 造一个点返回 —— struct 可以当返回值，这就是它比"传四个参数"省事的地方
点 造点(int x, int y) {
    点 p;              // p.x / p.y 现在是垃圾值，下面两行把它们填上
    p.x = x;
    p.y = y;
    return p;
}

// ③ 两个点当参数 —— 顺序传反了也看得出来，不会像四个 int 那样悄悄算错
int 距离平方(点 a, 点 b) {
    int dx = a.x - b.x;
    int dy = a.y - b.y;
    return dx * dx + dy * dy;
}

int main(void) {
    点 a = 造点(3, 4);
    点 b = 造点(0, 0);

    printf("%d\n", 距离平方(a, b));   // 3² + 4² = 25
    printf("%d\n", a.x);             // 3
    printf("%d\n", b.x);             // 0
    return 0;
}
""";


    [检查说明("结构体定义、造点、距离平方都对")]
    public static CheckResult 检查_1(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "25",
            "3",
            "0",
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

    [检查说明("定义了 struct 点（两个 int 字段）")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("struct 点"))
            return CheckResult.不过("没找到「struct 点」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("定义了 x 字段")]
    public static CheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("int x;"))
            return CheckResult.不过("没找到「int x;」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("定义了 y 字段")]
    public static CheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("int y;"))
            return CheckResult.不过("没找到「int y;」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("结构体定义后面有分号")]
    public static CheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("};"))
            return CheckResult.不过("没找到「};」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("距离平方用的是字段而不是写死数字")]
    public static CheckResult 检查_6(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("a.x"))
            return CheckResult.不过("没找到「a.x」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("main 里用 造点 造了两个点")]
    public static CheckResult 检查_7(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("造点("))
            return CheckResult.不过("没找到「造点(」—— 这一课要求用到它。");
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
