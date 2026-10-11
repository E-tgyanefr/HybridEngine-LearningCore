using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CppLessons;

[课程("14", "第 14 课：动态内存 —— 什么时候该 new，什么时候不该",
    """
1. 写一个函数 造数组(int n)，用 new int[n] 造一个长度为 n 的数组，把每个元素填成 i*i，返回它
2. main 里用 造数组(4) 得到数组，打印 4 个元素（0 1 4 9），然后 delete[]
3. 写一个函数 求和(int* 数们, int n)，返回前 n 个元素的和；用 1 2 3 4 调用并打印
""")]
public static class 第14课
{
    public static string 模板 => """
// 第 14 课：动态内存 —— 什么时候该 new，什么时候不该
//
// 先介绍：这一课先讲**不该用**的时候
//
// 大多数教材把 new / delete 讲得很靠前。我把它排在 14 课，是有意的：
//
//   ⚠⚠ **现代 C++ 里，绝大多数场合你不该手写 new / delete。**
//
//   需要"长度运行时才知道的数组"？ → `std::vector`（上一课）
//   需要"字符串"？                → `std::string`（第 07 课）
//   需要"一个能指向任何东西的东西"？ → 智能指针（以后会碰到）
//
//   这三个东西**自己管好内存** —— 你不需要记住"在哪一行释放"。
//   而手写 new / delete 意味着你**必须**记住 —— 忘一次就是内存泄漏，
//   释错一次就是崩溃，而且这两种错**都不会当场告诉你**。
//
// 那为什么还要学？因为你要**看懂别人的代码**：
//   · 老代码、C 风格的库、操作系统接口里到处是 new/delete
//   · 报错信息里会出现 "heap-use-after-free"、"memory leak" 这些词
//   · 你自己写容器/工具的时候（少数场合）真的需要它
//
// 所以这一课的目标是：**知道它是什么、知道它危险在哪、知道什么时候不用它。**
//
// ─────────────────────────────────────────────────────
//  用法与例子：栈 vs 堆
// ─────────────────────────────────────────────────────
//
// 先分清内存里的两个地方：
//
//     栈（stack）      函数里 `int a;` 这种，**函数一返回自动消失**
//                      快、自动、但**大小必须在编译时知道**
//
//     堆（heap）       `new` 出来的，**一直活着直到你 delete**
//                      可以运行时决定大小，但**必须自己记得释放**
//
//     int  a[10];              ← 栈上，长度必须写死
//     int* p = new int[n];     ← 堆上，n 可以是运行时算出来的
//
//   ⚠ `int* p = new int[n]` 读作"我要 n 个 int 的地方，把首地址给我"。
//     p 本身是个**指针**（在栈上，8 字节），它指向的那块内存在**堆**上。
//
// 用完要还：
//
//     delete[] p;               ← ⚠ 数组要用 **delete[]**，不是 delete
//     p = nullptr;              ← 好习惯：还完之后把指针清掉
//
//   ⚠ `delete` 和 `delete[]` 写错是**未定义行为** —— 可能没事，
//     可能崩，可能悄悄破坏内存。编译器**不会**提醒你。
//
// ⚠ 指针怎么传给函数：数组参数会"退化"成指针
//
//     int 求和(int* 数们, int n)      ← 注意：**必须另外告诉它有几个**
//
//   为什么：`int* 数们` 只是一个地址，**它自己不知道那块地方有多长**。
//     所以数组当参数时，长度得单独传。
//     （这也是 vector 的好处：`数们.size()` 随时问得到。）
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：new 了没 delete —— **内存泄漏**
//
//     void 干活() {
//         int* p = new int[1000];
//         // ... 用了 p ...
//         return;              // ✗ 忘了 delete[] p
//     }
//
// 你会看到：**什么都没发生** —— 程序照跑，第一次调用也没事。
// 但每调一次就漏掉 4KB，一个循环里调一百万次就是 4GB。
// 症状是"程序跑一阵子越来越慢，最后被系统杀掉"。
// ⇒ 这是**最难查的一类 bug**：错误和症状之间隔得很远。
//   现代做法就是**别用裸 new** —— 用 vector，它会在析构时自己释放。
//
// ⚠ 坑 2：delete 之后继续用（悬垂指针）
//
//     int* p = new int[10];
//     delete[] p;
//     p[0] = 5;                // ✗ 那块内存已经还回去了
//
// 你会看到：多半**看起来是好的**（那块内存还没被别的东西拿走），
// 但它是"别人的地方"了 —— 一旦程序里别处申请了内存，就会撞在一起，
// 表现为"改 A 的值，B 变了"。
// ⇒ 还完之后立刻 `p = nullptr;` —— 这样再用就是明确的崩溃（更好查）。
//
// ⚠ 坑 3：同一块内存 delete 两次
//
//     int* p = new int[10];
//     int* q = p;
//     delete[] p;
//     delete[] q;              // ✗ 同一块，删了两次
//
// 你会看到：程序直接崩（堆管理器发现异常）——
// 这个错**反而是好事**：它当场就崩，不像前两个那样悄悄进行。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   ① `int* 造数组(int n)`
//        用 new int[n] 造一个数组，把第 i 个填成 i * i，返回它
//        ⚠ 一定要 new（栈上的数组在函数返回时就没了，返回它的地址是错的）
//
//   ② `int 求和(int* 数们, int n)` —— 返回前 n 个元素的和
//
//   ③ main 里：
//        int* a = 造数组(4);
//        for (int i = 0; i < 4; i++) printf("%d\n", a[i]);   → 0 / 1 / 4 / 9
//        delete[] a;           ← ⚠ 数组要用 delete[]，别忘了
//        a = nullptr;
//
//        int b[4] = {1, 2, 3, 4};
//        printf("%d\n", 求和(b, 4));                          → 10
//
// 期望输出五行：0 / 1 / 4 / 9 / 10

// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/cpp-lessons 检查 14
//
//   看这一课要干什么：dotnet run --project csharp/cpp-lessons 14
//   看答案（先自己写一遍）：dotnet run --project csharp/cpp-lessons 答案 14
//
""";

    public static string 参考答案 => """
// 第 14 课参考答案：动态内存
#include <stdio.h>

// ⚠ 为什么这里**必须** new：栈上的数组在函数返回时就销毁了，
//   把它的地址返回出去 ⇒ 用的时候那块内存已经是别人的了。
//   （这类错叫"返回局部变量的地址"，编译器**通常会警告**但不阻止你。）
int* 造数组(int n) {
    int* p = new int[n];        // 堆上要 n 个 int
    for (int i = 0; i < n; i++) {
        p[i] = i * i;           // p[i] 就是 *(p + i)，两种写法等价
    }
    return p;
}

// ⚠ 数组当参数会"退化"成一个地址 ⇒ 长度必须**另外传**
int 求和(int* 数们, int n) {
    int 和 = 0;
    for (int i = 0; i < n; i++) {
        和 += 数们[i];
    }
    return 和;
}

int main(void) {
    int* a = 造数组(4);
    for (int i = 0; i < 4; i++) {
        printf("%d\n", a[i]);   // 0 1 4 9
    }
    delete[] a;                 // ⚠ 数组用 delete[]，不是 delete
    a = nullptr;                // 好习惯：还完就清掉，之后误用会明确崩

    int b[4] = {1, 2, 3, 4};    // 这个在栈上，长度编译时就知道 ⇒ 不用 new
    printf("%d\n", 求和(b, 4)); // 10
    return 0;
}
""";


    [检查说明("造数组（new + 填 i*i）、求和、释放都对")]
    public static CheckResult 检查_1(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "0",
            "1",
            "4",
            "9",
            "10",
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

    [检查说明("用 new 造了数组（不是栈上的）")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("new int[n]"))
            return CheckResult.不过("没找到「new int[n]」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("数组用的是 delete[]")]
    public static CheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("delete[]"))
            return CheckResult.不过("没找到「delete[]」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("函数返回的是指针")]
    public static CheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("int* 造数组(int n)"))
            return CheckResult.不过("没找到「int* 造数组(int n)」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("求和函数接收指针和长度")]
    public static CheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("int 求和(int* 数们, int n)"))
            return CheckResult.不过("没找到「int 求和(int* 数们, int n)」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("没有用单个 delete（数组必须 delete[]）")]
    public static CheckResult 检查_6(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains("delete a;"))
            return CheckResult.不过("出现了不该有的写法：delete a;");
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
