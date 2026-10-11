using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CppLessons;

[课程("09", "第 09 课：const 与 constexpr —— 承诺不改，和编译时就算好",
    """
1. 用 const 写一个函数 int 圆周长的百倍(int 半径)，里面用 const int 圆周率的百倍
2. 用 constexpr 定义一个编译期常量 格子数，并用它给一个数组定长度
""")]
public static class 第09课
{
    public static string 模板 => """
// 第 09 课：const 与 constexpr —— 承诺不改，和编译时就算好
//
// 先介绍：`const` 和 `constexpr` 长得很像，但不能互换
//
//     const       **我承诺不改它**（运行时的常量）
//     constexpr   **编译的时候它就已经是一个确定的数了**（编译期常量）
//
//  每个 constexpr 都是 const，但**反过来不成立**：
//
//     int 读一个数();                        // 运行时才知道
//     const int a = 读一个数();               // ✅ 合法：承诺不改
//     constexpr int b = 读一个数();           // ❌ 编译不过：编译时算不出来
//
//  判据很简单：**这个值在编译的时候能不能算出来？**
//
// ─────────────────────────────────────────────────────
//  一、`const`：把"不该改"写进代码里
// ─────────────────────────────────────────────────────
//
//     const double 圆周率 = 3.14159;
//     圆周率 = 3.14;              // ❌ 编译报错：assignment of read-only variable
//
//  它挡住的是一类具体的事故：**手滑改了一个本该固定的值**。
//  比如税率、圆周率、最大人数 —— 改错了程序照样跑，只是结果全错。
//  让编译器替你记住"这个不许改"，比靠人记住可靠得多。
//
//  ⚠⚠ **`const` 到底锁住了什么（这一课最容易搞错的一处）：**
//
//        const int* p;          // p **指向的 int** 不能改；p 自己能改（换指向）
//        int* const p = &x;     // p **自己**不能改（不能换指向）；指向的能改
//        const int* const p = &x;   // 两个都不能改
//
//     读法：**从右往左读** —— `const int* p` 读作
//     "p 是一个指针，指向 const int"。
//     这不是"指向的东西和指针本身都不能改"，很多人第一反应会读错。
//
//  ⚠ 和 `#define`（C 那边的做法）比：`const` **有类型**，
//    所以编译器能帮你检查类型、调试器里也能看见它。
//    C++ 里**优先用 const / constexpr，不要用 #define 定义常量**。
//
// ─────────────────────────────────────────────────────
//  二、`constexpr`：让编译器替你算
// ─────────────────────────────────────────────────────
//
//     constexpr int 边长 = 4;
//     constexpr int 面积 = 边长 * 边长;      // 编译器**当场算出 16**
//
//  好处有两个：
//
//     ① 这个计算**不用等程序跑起来**，编译时就做完了（快一点）
//     ② 它能用在**必须编译时确定**的地方 —— 比如数组长度：
//
//            int 格子[边长 * 边长];          // ✅ constexpr 可以
//            const int n = 读一个数();
//            int 格子[n];                    // ❌ 标准 C++ 里不行（那是运行时才知道的）
//
//  ⚠⚠ **这就是 const 和 constexpr 最实用的一条分界线。**
//     需要"用它来定数组长度"的时候，必须 constexpr。
//
//  ⚠ 顺便记住：`const` 变量**也可能**是编译期常量（如果初值就是字面量），
//    但语言**不保证**这一点 ⇒ 要"保证"，就得写 constexpr。
//    这条区别在 C++ 面试里也常问。
//
// ─────────────────────────────────────────────────────
//  三、什么时候用哪个（一条实用判据）
// ─────────────────────────────────────────────────────
//
//     初值是字面量、而且我要拿它定数组长度或做别的编译期计算  → constexpr
//     初值要运行时才知道（读文件、读传感器、算出来）           → const
//     只是函数参数，保证函数里不改它                          → const（放在参数上）
//
//  ⚠ 一个常见误解："用了 const 程序会变快。"
//    不会 —— const 是**给编译器和给人看**的约束，不是性能开关。
//    真正省时间的是 constexpr（把计算挪到编译期）。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   ① int 圆周长的百倍(int 半径)
//      函数**里面**定义 `const int 圆周率的百倍 = 314;`
//      返回 2 × 圆周率的百倍 × 半径 ÷ 100（整数运算，正好整除）
//      半径 1 ⇒ 2×314×1/100 = 6；半径 2 ⇒ 12
//
//      ⚠ 为什么用「百倍」这种写法：这一课的重点是 const/constexpr，
//        不是浮点数 —— 用整数能把注意力留在这一课要学的东西上。
//
//   ② 在文件里（函数外面）定义 `constexpr int 格子数 = 12;`
//      再写一个函数 int 格子数是多少()，直接 return 格子数;
//
//   ⚠ 检查会看你有没有真的写 const 和 constexpr —— 这一课就是练这两个词。
//
// 提示：不用写 main。

// ── 在下面写 ──

""";

    public static string 参考答案 => """
/* ⚠ 编译期常量：写在函数外面，编译器当场就知道它是 12。
   为什么用 constexpr 而不是 const —— 因为它要能用来定数组长度。
   `int 格子[格子数];` 这种写法**只有 constexpr 才行**。 */
constexpr int 格子数 = 12;

int 圆周长的百倍(int 半径) {
    /* ⚠ const：承诺这个值在函数里不会被改。
       手滑写 `圆周率的百倍 = 314;` 会**编译报错**，而不是悄悄算错。 */
    const int 圆周率的百倍 = 314;
    return 2 * 圆周率的百倍 * 半径 / 100;
}

int 格子数是多少() {
    /* constexpr 也能当普通常量用 */
    return 格子数;
}
""";


    [检查说明("写了 圆周长的百倍 函数")]
    public static CheckResult 检查_1(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 圆周长的百倍(1));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过)
        {
            if (run.编译信息.Contains("圆周长的百倍"))
                return CheckResult.不过("没找到「圆周长的百倍」这个函数 —— 或者签名和题目要求不一致。");
            return CppCheck.编译失败说明(run);
        }
        return CheckResult.过("「圆周长的百倍」可以正常调用");
    }

    [检查说明("圆周长的百倍 算对了（半径 1 / 2 / 10）")]
    public static CheckResult 检查_2(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 圆周长的百倍(1));
    printf("%d\n", 圆周长的百倍(2));
    printf("%d\n", 圆周长的百倍(10));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "6",
            "12",
            "62"
        };
        string[] 输入 = {
            "1",
            "2",
            "10"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count
                ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
            {
                string 这是 = i < 输入.Length ? 输入[i] : "";
                return CheckResult.不过(
                    $"输入 [{这是}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 多组输入都对才算对 —— 只对一组可能是碰巧。");
            }
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("用了 const（这一课练它）")]
    public static CheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("const"))
            return CheckResult.不过("没找到「const」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("用了 constexpr（编译期常量）")]
    public static CheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("constexpr"))
            return CheckResult.不过("没找到「constexpr」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("写了 格子数是多少 函数")]
    public static CheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("格子数是多少"))
            return CheckResult.不过("没找到「格子数是多少」—— 这一课要求用到它。");
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
