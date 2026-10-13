using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("13", "第 13 课：函数声明 —— 写在后面也能用",
    """
1. 写函数 int 三倍(int x)（**定义写在 main 后面**）
2. 在 main 前面写它的**声明**
""")]
public static class 第13课
{
    public static string 模板 => """
// 第 13 课：函数声明 —— 写在后面也能用
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    函数声明：函数写在后面也能用；多文件怎么编
//
//  需要先会
//    第 08 课：函数
//
// ─────────────────────────────────────────────────────
//  先介绍：为什么需要"声明"
// ─────────────────────────────────────────────────────
//
// C 编译器是**从上往下**读的。所以这样写会报错：
//
//     int main(void) {
//         printf("%d\n", 三倍(5));     ← 报 implicit declaration of '三倍'
//         return 0;
//     }
//     int 三倍(int x) { return x * 3; }   ← 定义在下面，编译器还没读到
//
// 解决：**在使用之前先"声明"一下**（也叫函数原型）：
//
//     int 三倍(int x);              ← 声明：只有类型，没有函数体，末尾分号
//
//     int main(void) {
//         printf("%d\n", 三倍(5));  ← 现在编译器知道有这么个函数了
//         return 0;
//     }
//
//     int 三倍(int x) {             ← 定义：真正的实现
//         return x * 3;
//     }
//
// ⚠⚠ 这一课要记住的两件事：
//
//   ① **声明 vs 定义**
//        声明：`int 三倍(int x);`     末尾**有分号**，没有 { }
//        定义：`int 三倍(int x) { ... }`  有函数体
//      放分号的地方不同，作用完全不同。
//
//   ② **为什么报错信息里说 implicit declaration**
//      那是编译器在说"我没见过这个函数，就当你返回 int 吧"——
//      这是个**警告级**的问题，但现代 C 已经当错误处理了。
//      看到它，就是"函数定义在使用之后，而且没声明"。
//
// ⚠ 这也解释了 #include 在干嘛：`#include <stdio.h>` 里就是
//   printf 的**声明**。所以你在 main 里能用 printf。
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
//  ① 声明就是"把函数的第一行抄下来，末尾加分号"：
//
//      定义：  int 三倍(int x) { return x * 3; }
//      声明：  int 三倍(int x);
//
//      ⚠ 参数名可以省：`int 三倍(int);` 也对。
//        但**类型必须和定义完全一致**，否则就是另一个函数（坑 3）。
//
//  ② 标准布局：声明在上面，main 在中间，定义在下面。
//
//      #include <stdio.h>
//
//      int 三倍(int x);            /* 声明 */
//
//      int main(void) {
//          printf("%d\n", 三倍(5));    ⇒ 15
//          return 0;
//      }
//
//      int 三倍(int x) {           /* 定义 */
//          return x * 3;
//      }
//
//  ③ 为什么真实项目都这么写：函数多了以后，
//     "谁在谁前面"会变成一团乱麻。有了声明，**顺序就自由了**。
//
//  ④ 最大的那个例子就是头文件：
//     `#include <stdio.h>` 干的事，就是把你用到的那些函数（printf、scanf…）
//     的**声明**一次性抄进你的文件。它里面一个函数体都没有。
//
//      ⚠ 所以"找不到 printf 的声明"和"找不到你自己函数的声明"
//        是**同一件事**，报错措辞也几乎一样。
//
//  ⚠ 声明写在文件开头，定义写在文件末尾 —— 这是 C 项目最经典的排版，
//    以后你读别人的代码会一直见到它。
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
//  ⚠ 坑 1：函数定义在使用之后，且没有声明
//
//      错误写法：
//          #include <stdio.h>
//
//          int main(void) {
//              printf("%d\n", 三倍(5));     /*  三倍 还没出现过 */
//              return 0;
//          }
//
//          int 三倍(int x) { return x * 3; }
//
//      你会看到什么（gcc 原文，中文函数名会被转义）：
//
//          main.c:4:20: error: implicit declaration of function '\U00004e09\U0000500d' [-Wimplicit-function-declaration]
//              4 │     printf("%d\n", 三倍(5));
//                │                    ^~~~
//
//      为什么：编译器**只往下读，不回头**。读到 `三倍(5)` 时它没见过这个名字，
//      只好按"返回 int 的无参函数"去猜。
//      ⚠ 那句 `[-Wimplicit-function-declaration]` 是开关名字 ——
//        它说明这原本是个"警告"，现在被当成错误。看到它，
//        只需要问一句：**这个函数的声明在哪？**
//
//  ⚠ 坑 2：声明末尾忘了分号（写成了定义的样子）
//
//      错误写法：
//          int 三倍(int x)             /*  想让它是声明，但少了分号 */
//
//          int main(void) { ... }
//
//      你会看到什么（gcc 原文）：
//
//          main.c:1:16: error: expected ';', identifier or '(' before 'int'
//              1 │ int 三倍(int x)
//                │                ^
//                │                ;
//          main.c:3:1: error: expected ';', identifier or '(' before 'int'
//
//      为什么：没有分号，编译器就以为你在**写定义**，于是接着找 `{`，
//      结果找到了 `int main(void)` ⇒ 崩。
//      ⚠ 它甚至好心地给你显示了该补的东西：`;`。
//      ⇒ 声明和定义的区别就在**一个分号**上：
//          有分号、没函数体  ⇒  声明
//          没分号、有函数体  ⇒  定义
//
//  ⚠ 坑 3：声明和定义的类型对不上
//
//      错误写法（声明说返回 double，定义却返回 int）：
//          double 三倍(int x);             /* 声明 */
//
//          int main(void) {
//              printf("%f\n", 三倍(5));
//              return 0;
//          }
//
//          int 三倍(int x) {               /*  定义是 int */
//              return x * 3;
//          }
//
//      你会看到什么：
//
//          main.c:7:5: error: conflicting types for '三倍'; have 'int(int)'
//              7 │ int 三倍(int x) {
//                │     ^~~~
//          main.c:1:8: note: previous declaration of '三倍' with type 'double(int)'
//              1 │ double 三倍(int x);
//                │        ^~~~
//
//      为什么：C 靠"名字 + 参数类型"认函数。声明和定义必须是同一份契约。
//      编译器把两处都列出来了（一处 error、一处 note），
//      意思是"你在第 1 行说是 double，在第 7 行又说是 int"。
//      ⇒ 改的时候**两处一起改**。这也是为什么真实项目把头文件
//        当"契约"来管：改错了编译器立刻拦住你。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   1. 在 main **前面**写 `int 三倍(int x);`（声明）
//   2. 在 main **后面**写 `int 三倍(int x) { return x * 3; }`（定义）
//   3. main 里调用它并打印
//
//  期望输出（一行）：
//
//      15
//
// ⚠ 这一课的检查会看**顺序**：声明必须排在定义前面。
//   如果两处调换了，或者少了一处，都算没过。
//   也别忘了自己写 `#include <stdio.h>` —— printf 也要声明。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。
//   命令行：dotnet run --project csharp/c-lessons 检查 13
//   看这一课要干什么：dotnet run --project csharp/c-lessons 13
//   看答案：dotnet run --project csharp/c-lessons 答案 13
//
// ── 在下面写 ──
//   printf 的**声明**。所以你在 main 里能用 printf。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   1. 在 main **前面**写 `int 三倍(int x);`（声明）
//   2. 在 main **后面**写 `int 三倍(int x) { return x * 3; }`（定义）
//   3. main 里调用它并打印

// ── 在下面写你的代码 ──
//   printf 的**声明**。所以你在 main 里能用 printf。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   1. 在 main **前面**写 `int 三倍(int x);`（声明）
//   2. 在 main **后面**写 `int 三倍(int x) { return x * 3; }`（定义）
//   3. main 里调用它并打印

// ── 在下面写你的代码 ──

""";

    public static string 参考答案 => """
#include <stdio.h>

int 三倍(int x);            /* ⚠ 声明：末尾有分号，没有 {} */

int main(void) {
    printf("%d\n", 三倍(5));
    return 0;
}

int 三倍(int x) {           /* ⚠ 定义在下面 */
    return x * 3;
}
""";


    [检查说明("程序能编译运行（说明声明写对了）")]
    public static CheckResult 检查_1(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "15",
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

    [检查说明("打印了 15")]
    public static CheckResult 检查_2(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "15",
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

    [检查说明("声明在定义前面")]
    public static CheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        int 主 = 去注释.IndexOf("int main");
        if (主 < 0)
            return CheckResult.不过("这一课要写完整的 main —— 找不到 int main。");
        string 前 = 去注释.Substring(0, 主);
        string 后 = 去注释.Substring(主);
        if (!前.Contains("int 三倍"))
            return CheckResult.不过("声明要写在 main **前面**，否则编译器读不到它。");
        if (!后.Contains("int 三倍"))
            return CheckResult.不过("函数定义要写在 main **后面** —— 这一课练的就是这个顺序。");
        return CheckResult.过("声明在 main 前、定义在 main 后 —— 顺序对了");
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
