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
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**怎么在程序跑起来之后才决定要多少内存**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    动态内存：什么时候该 new、什么时候不该
//
//  需要先会
//    第 08 课：指针
//
// ─────────────────────────────────────────────────────
//  先介绍：两种内存
// ─────────────────────────────────────────────────────
//
//     栈      局部变量住这儿，函数一返回就自动回收
//             优点：不用管；缺点：**小**（通常几 MB），而且大小编译时就知道
//
//     堆      用 new 从这儿要，**要自己 delete**
//             优点：大、可以运行时决定大小；缺点：忘了还就漏了
//
// 什么时候真的需要 new：
//
//     · 大小要到运行时才知道，而且很大（几百万元素）
//     · 这个东西要活得比创建它的函数更久
//     · 要共享给好几个地方用
//
// ⚠⚠ **其余情况都别用 new。** 真实项目里 90% 的 new 都是没必要的 ——
//   用 vector 或普通局部变量就够了，而且不用操心回收。
//
// ─────────────────────────────────────────────────────
//  一、new / delete 的配对
// ─────────────────────────────────────────────────────
//
//     int* p = new int(5);       要一个 int，初值 5
//     std::cout << *p;           ⇒ 5
//     delete p;                  **必须还**
//
//     int* 数组 = new int[n];     要 n 个 int
//     数组[0] = 1;
//     delete[] 数组;              ⚠ 数组要用 delete[]（带方括号）
//
// ⚠ 配对规则：**new 配 delete，new[] 配 delete[]。**
//   用错了是未定义行为 —— 可能没事，可能崩，都不算「对」。
//
// ─────────────────────────────────────────────────────
//  二、更省事的办法：用 vector
// ─────────────────────────────────────────────────────
//
//     std::vector<int> 数(n);     ← 一行搞定，函数返回时自动回收
//
// 对照一下：
//
//     int* p = new int[n];           要写 new，还要记得 delete[]
//     std::vector<int> 数(n);         什么都不用记
//
// ⇒ **能用 vector 就用 vector。** 这一课教你 new，是让你看得懂别人的代码，
//   不是让你以后到处 new。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：忘了 delete（内存泄漏）
// ─────────────────────────────────────────────────────
//
//     int* p = new int(5);
//     // …用完就忘了
//     return;                       ← p 没了，那块内存还在
//
// 你会看到：**什么都不报，程序正常跑完、结果也对。**
//   泄漏的症状是**跑久了越来越吃内存**，而不是当场出错 ——
//   所以它在小程序里根本看不出来，到大程序里才炸。
//
// 为什么危险：那块内存已经被程序「忘了」，但系统还认为你在用，
//   谁都不能再用它，直到进程结束。
//
// ⇒ 三条规矩：
//     ① 能用 vector / 局部变量就别 new；
//     ② 真 new 了，**在同一个函数里** delete（别跨函数传所有权）；
//     ③ new[] 一定要配 delete[]。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   智能指针（unique_ptr / shared_ptr）—— 那才是现代 C++ 的答案，
//   但它是另一套语法，这门课到这儿为止。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  按题目要求写几个用 new / delete 的函数。
//   检查会看你有没有**配对**地 delete。
//   不用写 main。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
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
