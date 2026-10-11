using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("19", "第 19 课：递归 —— 函数调用自己",
    """
写两个递归函数：int 阶乘(int n) 和 int 斐波那契(int n)
""")]
public static class 第19课
{
    public static string 模板 => """
// 第 19 课：递归 —— 函数调用自己
//
// 先介绍：一个函数可以调用它自己
//
//     阶乘 n! = 1 × 2 × 3 × ... × n
//
//     用循环写（你已经会了）：
//         int 结果 = 1;
//         for (int i = 2; i <= n; i++) 结果 *= i;
//
//     用递归写：
//         n! = n × (n-1)!        ← 把问题**变小一号**，再问自己
//
//         int 阶乘(int n) {
//             if (n <= 1) return 1;      ← ① 什么时候停
//             return n * 阶乘(n - 1);    ← ② 用"小一号的答案"拼出答案
//         }
//
// ─────────────────────────────────────────────────────
//  ⚠⚠ 递归必须有的两样东西
// ─────────────────────────────────────────────────────
//
//   ① **基准情形**（base case）—— 什么时候**不再**调用自己，直接给答案。
//        上面那行 `if (n <= 1) return 1;` 就是它。
//
//   ② **每次都在变小** —— 参数必须朝着基准情形走。
//        `阶乘(n - 1)` 比 `阶乘(n)` 小一号 ⇒ 早晚会到 1。
//
//   ⚠⚠⚠ **缺了任何一样，程序会崩 —— 而且崩法很难看。**
//
//      int 阶乘(int n) {
//          return n * 阶乘(n - 1);      /* 没有基准情形 */
//      }
//
//   它会一直调用下去，每调一次就在内存里压一层（叫"调用栈"），
//   直到把栈用完 ⇒ 崩溃。这个错叫**栈溢出**（stack overflow）。
//
//   ⚠ 在 Windows 上你看到的多半是程序直接消失、退出码不是 0，
//     而不是一句好看的报错。**这就是递归最需要小心的地方。**
//
// ─────────────────────────────────────────────────────
//  为什么值得学：有些问题用递归写短得多
// ─────────────────────────────────────────────────────
//
//  阶乘用循环写就挺好，递归没占到便宜。但下面这类问题：
//
//     · 文件夹里还有文件夹，一层套一层 —— 怎么数所有文件？
//     · 一棵树，每个节点还有子树 —— 怎么遍历？
//     · 汉诺塔、走迷宫、表达式求值
//
//  **它们本身就是递归定义的**，用循环写要自己造一个"栈"来记住位置，
//  用递归写则几乎是直译。这一课先在小问题上把套路练熟。
//
// ─────────────────────────────────────────────────────
//  ⚠ 一个反例：斐波那契**不该**用这种递归写
// ─────────────────────────────────────────────────────
//
//      fib(n) = fib(n-1) + fib(n-2)
//
//  按这个式子直接写成递归：
//
//      int 斐波那契(int n) {
//          if (n <= 1) return n;
//          return 斐波那契(n - 1) + 斐波那契(n - 2);   ← 能算对，但极慢
//      }
//
//  ⚠⚠ 它**能算出正确答案**，但算 fib(40) 要调用自己上**亿**次。
//     为什么：算 fib(5) 要 fib(4) 和 fib(3)，而 fib(4) 又要 fib(3) ——
//     **同一个 fib(3) 被重复算了无数遍**。这叫"重叠子问题"。
//
//  这一课仍然让你这么写，因为**它最能训练"递归思维"**，
//  而且检查只喂到 n ≤ 20（够快）。但你要记住：
//
//     ⚠ **能写出递归，不等于该用递归。**
//       判断依据是"有没有大量重复计算"，不是"写起来短不短"。
//  （正式的做法叫"动态规划"或"记忆化"：把算过的答案存起来，
//    下次直接查表。那是后面的内容。）
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   写两个**递归**函数（不许用循环）：
//
//     1. int 阶乘(int n)          0! 和 1! 都是 1
//     2. int 斐波那契(int n)       fib(0)=0, fib(1)=1, fib(2)=1, fib(3)=2 …
//
//   ⚠ 两个都必须有**基准情形**，而且参数要**每次变小** ——
//     少了任何一样，检查会看到程序崩溃。
//
// 提示：不用写 main。

// ── 在下面写你的两个函数 ──

""";

    public static string 参考答案 => """
int 阶乘(int n) {
    /* ⚠ 基准情形：这里停止递归，直接给答案 */
    if (n <= 1) {
        return 1;
    }
    /* ⚠ 参数比原来小一号 ⇒ 早晚会碰到基准情形 */
    return n * 阶乘(n - 1);
}

int 斐波那契(int n) {
    /* ⚠ 基准情形有两个（n=0 和 n=1），少一个就会掉到负数去 */
    if (n <= 0) {
        return 0;
    }
    if (n == 1) {
        return 1;
    }
    /* ⚠ 这样写能算对，但**极慢** ——
       斐波那契(3) 在算 斐波那契(6) 的过程中会被重复计算好几遍。
       这一课先练"递归思维"，效率的事以后再说。 */
    return 斐波那契(n - 1) + 斐波那契(n - 2);
}
""";


    [检查说明("写了 阶乘 函数")]
    public static CheckResult 检查_1(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 阶乘(5));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过)
        {
            if (run.编译信息.Contains("阶乘"))
                return CheckResult.不过("没找到「阶乘」这个函数 —— 或者签名和题目要求不一致。");
            return CppCheck.编译失败说明(run);
        }
        return CheckResult.过("「阶乘」可以正常调用");
    }

    [检查说明("阶乘 多组都对（含 0 和 1 两个基准情形）")]
    public static CheckResult 检查_2(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 阶乘(0));
    printf("%d\n", 阶乘(1));
    printf("%d\n", 阶乘(5));
    printf("%d\n", 阶乘(10));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "1",
            "1",
            "120",
            "3628800"
        };
        string[] 输入 = {
            "0",
            "1",
            "5",
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

    [检查说明("写了 斐波那契 函数")]
    public static CheckResult 检查_3(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 斐波那契(10));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过)
        {
            if (run.编译信息.Contains("斐波那契"))
                return CheckResult.不过("没找到「斐波那契」这个函数 —— 或者签名和题目要求不一致。");
            return CppCheck.编译失败说明(run);
        }
        return CheckResult.过("「斐波那契」可以正常调用");
    }

    [检查说明("斐波那契 多组都对（含两个基准情形）")]
    public static CheckResult 检查_4(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 斐波那契(0));
    printf("%d\n", 斐波那契(1));
    printf("%d\n", 斐波那契(2));
    printf("%d\n", 斐波那契(10));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "0",
            "1",
            "1",
            "55"
        };
        string[] 输入 = {
            "0",
            "1",
            "2",
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

    [检查说明("真的用了递归（函数里调用了自己）")]
    public static CheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("阶乘("))
            return CheckResult.不过("没找到「阶乘(」—— 这一课要求用到它。");
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
