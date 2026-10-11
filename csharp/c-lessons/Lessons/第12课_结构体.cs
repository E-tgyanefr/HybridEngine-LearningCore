using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("12", "第 12 课：结构体 —— 把几个东西打成一包",
    """
1. 定义 struct 学生，含 学号(int) 和 分数(int)
2. 写函数 int 是优等生(struct 学生 s)：分数 >= 90 返回 1，否则 0
""")]
public static class 第12课
{
    public static string 模板 => """
// 第 12 课：结构体 —— 把几个东西打成一包
//
// 先介绍：把相关的数据放在一起
//
// 一个学生有学号、有分数。用两个变量存很别扭：
//
//     int 学号1 = 7;  int 分数1 = 92;
//     int 学号2 = 8;  int 分数2 = 55;      ← 学生一多就疯了
//
// 结构体把它们打成**一个**类型：
//
//     struct 学生 {
//         int 学号;
//         int 分数;
//     };
//     ↑ 这个分号不能少！结构体定义末尾**一定要有分号**
//
// 用起来：
//
//     struct 学生 a;
//     a.学号 = 7;            ← 用 `.` 取里面的字段
//     a.分数 = 92;
//
//     struct 学生 b = {8, 55};      ← 也可以一次给全
//
// ⚠⚠ 这一课要记住的两个坑：
//
//   ① **结构体定义末尾的分号**：`};`
//      漏了会报一堆看不懂的错（因为它会以为你在声明变量）。
//
//   ② 类型名要写 `struct 学生`，**不能只写 `学生`**：
//        struct 学生 a;        ✅
//        学生 a;               ❌ unknown type name '学生'
//      （用 `typedef` 可以省掉 struct，那是以后的事。）
//
// ⚠ 结构体可以**整体传给函数**（按值传递，会拷贝一份）。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   1. 定义 struct 学生 { int 学号; int 分数; };
//   2. 写函数 int 是优等生(struct 学生 s)
//      分数 >= 90 返回 1，否则返回 0
//
// 提示：不用写 main。

// ── 在下面写你的结构体和函数 ──

""";

    public static string 参考答案 => """
struct 学生 {
    int 学号;
    int 分数;
};                      /* ⚠ 这个分号不能少 */

int 是优等生(struct 学生 s) {
    if (s.分数 >= 90) {     /* ⚠ 用 . 取字段；类型名要写 struct 学生 */
        return 1;
    }
    return 0;
}
""";


    [检查说明("定义了 struct 学生")]
    public static CheckResult 检查_1(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("struct 学生"))
            return CheckResult.不过("没找到「struct 学生」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("写了 是优等生 函数")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("是优等生"))
            return CheckResult.不过("没找到「是优等生」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("函数能编译（结构体用法正确）")]
    public static CheckResult 检查_3(string 代码)
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
