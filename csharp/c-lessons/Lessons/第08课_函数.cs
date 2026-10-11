using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("08", "第 08 课：函数 —— 把计算打包起来",
    """
1. 写函数 int 平方(int x)
2. 写函数 int 较大(int a, int b)：返回较大的那个
""")]
public static class 第08课
{
    public static string 模板 => """
// 第 08 课：函数 —— 把计算打包起来
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    把一段计算打包成函数，想用几次用几次
//
//  需要先会
//    第 02 课：变量
//
// ─────────────────────────────────────────────────────
//  先介绍：函数 = 起个名字的一段计算
// ─────────────────────────────────────────────────────
//
//     int 平方(int x) {
//     ↑        ↑
//     返回类型  函数名(参数类型 参数名)
//         ...
//         return 结果;
//     }
//
// ⚠⚠ **C 的函数必须写返回类型**（Python 可以不写）：
//     平方(x) { }              ❌ 老式写法，现代 C 不允许
//     int 平方(int x) { }      ✅
//
// ⚠ 参数也要写类型，而且**每个参数都要写**：
//     int 加(int a, b)         ❌ b 没类型
//     int 加(int a, int b)     ✅
//
// ⚠ `return` 之后函数立刻结束；不是 void 的函数**每条路径都要有 return**：
//     int 判(int x) {
//         if (x > 0) return 1;
//         /* 如果 x <= 0 就走到这里，没有 return ⇒ 返回随机值！ */
//     }
//     编译器会给警告（control reaches end of non-void function），
//     但**不一定报错** ⇒ 你会拿到随机结果。
//
// ⚠ 函数写在 main **前面**；写在后面要先声明（那是第 13 课）。
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
//  ① 不接收参数、不返回值的函数：返回类型写 void，参数写 void。
//
//      void 打个招呼(void) {
//          printf("你好\n");
//      }
//
//  ② 有参数、有返回值（最常见的形状）：
//
//      int 平方(int x) {
//          return x * x;
//      }
//
//  ③ 调用它：写函数名，括号里放实参。返回值可以直接用、可以打印、可以存起来。
//
//      printf("%d\n", 平方(3));        ⇒ 9        直接打印
//      int r = 平方(4);                           存进变量（r 是 16）
//
//  ④ 参数可以有多个，用逗号隔开：
//
//      int 较大(int a, int b) {
//          if (a > b) {
//              return a;
//          }
//          return b;               /* ⚠ 这条路径也要 return */
//      }
//
//  ⑤ void 函数里 `return;` 表示"提前结束"，不带值。
//
//      void 检查(int x) {
//          if (x < 0) {
//              return;             /* 负数就不往下做了 */
//          }
//          printf("正数\n");
//      }
//
//  一个完整的、能跑的例子：
//
//      #include <stdio.h>
//
//      int 平方(int x) {
//          return x * x;
//      }
//
//      int 较大(int a, int b) {
//          if (a > b) {
//              return a;
//          }
//          return b;
//      }
//
//      int main(void) {
//          printf("%d\n", 平方(3));        ⇒ 9
//          printf("%d\n", 平方(0));        ⇒ 0
//          printf("%d\n", 较大(1, 2));     ⇒ 2
//          printf("%d\n", 较大(5, 5));     ⇒ 5
//          return 0;
//      }
//
//  ⚠ 为什么"函数要写在 main 前面"：编译器从上往下读，
//    读到 `平方(3)` 那一行时，它得**先知道**平方长什么样。
//    写在前面它就知道了；写在后面就要先"声明"（第 13 课）。
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
//  ⚠ 坑 1：不是每条路径都有 return
//
//      错误写法：
//          int 判(int x) {
//              if (x > 0) return 1;
//              /* x <= 0 时掉到这里，没有 return */
//          }
//
//      你会看到什么（gcc 原文）：
//
//          main.c:4:1: warning: control reaches end of non-void function [-Wreturn-type]
//              4 │ }
//                │ ^
//
//      ⚠ 只是 **warning** —— 编译照过，.exe 照生成。
//      运行时 `判(0)` 返回一个随机整数，每次跑都可能不一样。
//
//      为什么：函数说好返回 int，可 CPU 到达 `}` 时没有一个明确的返回值，
//      它就返回寄存在返回位置上的**残留值**（上一步运算留下的东西）。
//      ⇒ 规矩：**不是 void 的函数，最后一行也要有个 return**，
//        哪怕你觉得自己已经覆盖了所有情况。
//      上面那个例子最稳的写法是：
//          int 判(int x) {
//              if (x > 0) return 1;
//              return 0;               /* 兜底，明明白白 */
//          }
//
//  ⚠ 坑 2：参数忘了写类型
//
//      错误写法：
//          int 加(int a, b) {
//              return a + b;
//          }
//
//      你会看到什么（gcc 原文）：
//
//          main.c:2:15: error: unknown type name 'b'
//              2 │ int 加(int a, b) { return a + b; }
//                │               ^
//
//      为什么：C 的语法里，每个参数都是"类型 + 名字"的一个小声明。
//      编译器看到 `b` 就直接把它当成类型名去找 —— 找不到这个类型，报错。
//      ⚠ 有意思的是报错说的是 "unknown type name"，它**不会**提示"你漏了 int"，
//        所以第一次见容易懵。看到"某个变量的名字被当成类型名"，
//        先想：**是不是前一个类型忘了写**。
//      对了，`int a, b;` 在**变量声明**里是合法的（两个都是 int），
//      但在**参数列表**里不合法 —— 这点不一样，别混。
//
//  ⚠ 坑 3：把函数写在 main 后面，又没声明
//
//      错误写法：
//          #include <stdio.h>
//
//          int main(void) {
//              printf("%d\n", 平方(3));     /* ⚠ 平方还没出现过 */
//              return 0;
//          }
//
//          int 平方(int x) { return x * x; }
//
//      你会看到什么（gcc 原文）：
//
//          main.c:4:20: error: implicit declaration of function '平方' [-Wimplicit-function-declaration]
//              4 │     printf("%d\n", 平方(3));
//                │                    ^~~~
//
//      为什么：编译器是**从上往下、一遍读完**的，它不做"先扫一遍全文"。
//      读到 `平方(3)` 的时候它压根没见过这个名字，只能猜"大概是个返回 int 的函数"。
//      这个"猜"在 C99 之后就是错误。
//      ⇒ 两种改法：把定义挪到 main 前面（本课推荐），
//        或者在 main 前面加一行声明 `int 平方(int x);`（第 13 课）。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   1. int 平方(int x)              返回 x * x
//   2. int 较大(int a, int b)       返回较大的那个
//
// 提示：不用写 main。
//
// ⚠ 期望结果：
//
//      平方(3)  ⇒ 9        平方(0)  ⇒ 0        平方(-4) ⇒ 16
//      较大(1,2) ⇒ 2       较大(5,5) ⇒ 5       较大(-3,-7) ⇒ -3
//
// ⚠ 两个地方最容易丢分：
//   · 平方(-4) 要是 16 —— 负数乘负数得正数，别写成 -16。
//   · 较大(5,5) 要是 5 —— 用 `>` 而不是 `>=` 也能过，但两条路径都要有 return。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。
//   命令行：dotnet run --project csharp/c-lessons 检查 08
//   看这一课要干什么：dotnet run --project csharp/c-lessons 08
//   看答案：dotnet run --project csharp/c-lessons 答案 08
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """
int 平方(int x) {
    return x * x;
}

int 较大(int a, int b) {
    if (a > b) {
        return a;
    }
    return b;          /* ⚠ 每条路径都要有 return */
}
""";


    [检查说明("两个函数都写了")]
    public static CheckResult 检查_1(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 平方(3));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过)
        {
            if (run.编译信息.Contains("平方"))
                return CheckResult.不过("没找到「平方」这个函数 —— 或者签名和题目要求不一致。");
            return CppCheck.编译失败说明(run);
        }
        return CheckResult.过("「平方」可以正常调用");
    }

    [检查说明("平方 多组都对（含 0 与负数）")]
    public static CheckResult 检查_2(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 平方(3));
    printf("%d\n", 平方(0));
    printf("%d\n", 平方(-4));
    printf("%d\n", 平方(10));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "9",
            "0",
            "16",
            "100"
        };
        string[] 输入 = {
            "3",
            "0",
            "-4",
            "10"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
                return CheckResult.不过(
                    $"输入 [{(i < 输入.Length ? 输入[i] : "")}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 多组输入都对才算对 —— 只对一组可能是碰巧。");
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("较大 多组都对（含相等）")]
    public static CheckResult 检查_3(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 较大(1,2));
    printf("%d\n", 较大(5,5));
    printf("%d\n", 较大(-3,-7));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "2",
            "5",
            "-3"
        };
        string[] 输入 = {
            "1,2",
            "5,5",
            "-3,-7"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
                return CheckResult.不过(
                    $"输入 [{(i < 输入.Length ? 输入[i] : "")}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 多组输入都对才算对 —— 只对一组可能是碰巧。");
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
