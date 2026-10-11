using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("11", "第 11 课：指针 —— 改参数，外面会不会变",
    """
写函数 void 加倍(int *p)：把 p 指向的那个数乘以 2
""")]
public static class 第11课
{
    public static string 模板 => """
// 第 11 课：指针 —— 改参数，外面会不会变
//
// 先介绍：为什么需要指针
//
// 先看一个"不生效"的函数：
//
//     void 加倍(int x) {
//         x = x * 2;            ← 改的是**拷贝**
//     }
//
//     int 数 = 5;
//     加倍(数);
//     printf("%d\n", 数);       ⇒ 还是 5！
//
// 为什么：C 的函数参数默认是**按值传递** —— 传进去的是一份**拷贝**。
// 函数里怎么改，都动不到外面那个变量。
//
// 想改到外面，就要传**地址**：
//
//     void 加倍(int *p) {       ← p 是"指向一个 int 的指针"
//         *p = *p * 2;          ← *p 是"p 指向的那个数"
//     }
//
//     int 数 = 5;
//     加倍(&数);                ← 传地址（那个 & 又出现了）
//     printf("%d\n", 数);       ⇒ 10  ✅
//
// ⚠⚠ 两个符号别搞混 —— 这是这一课唯一要记住的东西：
//
//     &数     取地址 —— "数在内存的哪里"
//     *p     取值   —— "p 指的那个地方**里面**是什么"
//
//   在**声明**里 `int *p` 的 `*` 是"这是个指针"；
//   在**表达式**里 `*p` 的 `*` 是"取出它指的东西"。同一个符号两种意思。
//
// ⚠ 第 05 课的 `scanf("%d", &数)` 就是这个道理 ——
//   它要改你那个变量，所以必须知道地址。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   写函数：  void 加倍(int *p)
//   把 p 指的那个数乘以 2（要让**外面**的变量真的变）
//
// 提示：不用写 main。

// ── 在下面写你的函数 ──

""";

    public static string 参考答案 => """
void 加倍(int *p) {
    /* ⚠ *p 才是"p 指的那个数"；写 p = p * 2 是在改指针本身，没用 */
    *p = *p * 2;
}
""";


    [检查说明("写了 加倍 函数（参数是指针）")]
    public static CheckResult 检查_1(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    int __v = 3;
    加倍(&__v);
    return 0;
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        return CheckResult.过();
    }

    [检查说明("真的改到了外面的变量")]
    public static CheckResult 检查_2(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    int __v0 = 5;
    加倍(&__v0);
    printf("%d\n", __v0);
    int __v1 = 0;
    加倍(&__v1);
    printf("%d\n", __v1);
    int __v2 = -3;
    加倍(&__v2);
    printf("%d\n", __v2);
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "10",
            "0",
            "-6",
        };
        string[] 输入 = {
            "5",
            "0",
            "-3",
        };
        for (int i = 0; i < 期望.Length; i++)
            if (i >= run.行.Count || run.行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"传入 {输入[i]} 之后应当是 {期望[i]}，" +
                    $"实际是「{(i < run.行.Count ? run.行[i].Trim() : "（没输出）")}」。\n" +
                    "      ⚠ 要在函数里写 *p = ...，才会改到外面的变量；只写 p = ... 改的是指针自己。");
        return CheckResult.过("每一组都真的改到了外面的变量");
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
