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
// 先介绍：为什么需要"把一堆东西捆成一个"
//
// 前面几课你已经在忍受这件事了。想表示"一个点"（有 x 和 y），
// 你得用两个变量：
//
//     int x1 = 3, y1 = 4;
//     int x2 = 0, y2 = 0;
//
// 想写"算两个点的距离"，函数签名就得是这样：
//
//     int 距离平方(int x1, int y1, int x2, int y2);   ← 四个参数
//
// 四个还忍得住。可"一个学生"有名字、学号、成绩、班级、入学年份……
// 那就是七八个参数，而且**顺序一错就悄悄算错**（x1 和 x2 传反了不报错）。
// 更麻烦的是：函数**只能返回一个值** —— 想返回一个"点"怎么办？
//
// struct 就是干这个的：**把几个变量捆成一个新的类型**。
//
//     struct 点 {
//         int x;
//         int y;
//     };                    ← ⚠ 这个分号别忘，最容易漏
//
// 从此 `点` 就和 `int`、`double` 一样是一个类型了：
//
//     点 a;              ← 一个点
//     a.x = 3;           ← 用 `.` 取出里面的字段
//     a.y = 4;
//
// 于是那个四参数函数变成：
//
//     int 距离平方(点 a, 点 b);      ← 两个参数，而且**传反了也看得出来**
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
// ① 定义、创建、访问
//
//     struct 点 {
//         int x;
//         int y;
//     };
//
//     int main() {
//         点 a;                   ← 创建（字段是**未初始化**的，值是随机的）
//         a.x = 3;
//         a.y = 4;
//
//         点 b = {0, 0};          ← 花括号初始化：按**声明顺序**填
//         点 c = {5};             ← 只给一个 ⇒ 剩下的补 0（c.x=5, c.y=0）
//
//         printf("%d %d\n", a.x, b.y);
//         return 0;
//     }
//
//   ⚠ `点 a;` 之后 **a.x 和 a.y 里是垃圾值**（栈上没清过的内存）。
//     要用就先赋值。这是新手最常见的 bug 来源之一。
//
// ② 当函数参数、当返回值 —— 这才是 struct 真正省事的地方
//
//     点 造点(int x, int y) {
//         点 p;
//         p.x = x;
//         p.y = y;
//         return p;               ← 可以直接返回一个 struct
//     }
//
//     int 距离平方(点 a, 点 b) {
//         int dx = a.x - b.x;
//         int dy = a.y - b.y;
//         return dx * dx + dy * dy;
//     }
//
// ③ struct 和 class 的区别（一句话）
//
//     struct  →  成员默认是 **public**（谁都能访问）
//     class   →  成员默认是 **private**（只有自己能用）
//
//   C++ 里两者**别的都一样**（都能有函数、都能继承）。
//   习惯用法：**纯数据用 struct，有行为要封装的用 class**。
//   这一课先只碰数据，所以用 struct。
//
// ④ struct 之间可以直接赋值
//
//     点 a = {3, 4};
//     点 b = a;               ← ✓ 整个复制一份（**逐个字段拷贝**）
//     b.x = 99;
//     printf("%d\n", a.x);    ⇒ 3    ← a 没被影响
//
//   ⚠ 这一点和数组**不一样**：数组不能整个赋值（`int b[3] = a;` 是错的），
//     而 struct 可以。⚠ 但这也埋着一个坑 —— 见下面坑 2。
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：定义后面忘分号
//
//     struct 点 {
//         int x;
//         int y;
//     }                      ← 少了 ;
//
// 你会看到一串**完全看不懂**的报错（编译器把后面的代码当成变量名了）：
//     error: expected ';' after struct definition
//     error: '点' does not name a type
// **记住这条**：`struct` / `class` / `enum` 的花括号后面**要分号**，
// 而 `if` / `for` / 函数的花括号后面**不要**。这是 C++ 里少有的"不一致"。
//
// ⚠ 坑 2：以为 struct 里有数组时 `b = a` 还是安全的
//
//     struct 名单 {
//         int 数[3];
//     };
//     名单 a = {{1, 2, 3}};
//     名单 b = a;            ← ✓ 这个其实**是安全的**（数组被整个复制）
//
//   为什么安全：C++ 的 struct 赋值是"**逐成员复制**"，
//   数组作为成员会**整块拷走**。危险的是**指针**成员：
//
//     struct 坏 {
//         int* p;
//     };
//     坏 a;  a.p = new int(5);
//     坏 b = a;              ← ✗ 只复制了**指针**，两块 b.p 和 a.p 指向同一处
//     *b.p = 99;
//     printf("%d\n", *a.p);  ⇒ 99   ← 改 b 动了 a！
//   ⚠ 这叫"浅拷贝"。以后写了带指针的 struct，要么自己写拷贝逻辑，
//     要么（更好）用 vector / string 这类会自己管好内存的类型。
//
// ⚠ 坑 3：直接比较两个 struct
//
//     点 a = {3, 4}, b = {3, 4};
//     if (a == b) { ... }        // ✗ 编译错误
//
// 你会看到：error: no match for 'operator==' (operand types are '点' and '点')
// 为什么：C++ **不会自动替你生成"逐字段比较"** —— 它不知道该比哪些字段
// （万一有个字段是缓存、不该参与比较呢）。想比就自己写：
//     bool 相等(点 a, 点 b) { return a.x == b.x && a.y == b.y; }
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   ① 定义 struct 点（两个 int：x、y）
//
//   ② 写 `点 造点(int x, int y)` —— 造一个点返回
//
//   ③ 写 `int 距离平方(点 a, 点 b)` —— 返回 (a.x-b.x)^2 + (a.y-b.y)^2
//        提示：先算 dx 和 dy，再 return dx*dx + dy*dy;
//
//   ④ main 里：
//        点 a = 造点(3, 4);
//        点 b = 造点(0, 0);
//        printf("%d\n", 距离平方(a, b));      → 25   （3²+4²=25）
//        printf("%d\n", a.x);                 → 3
//        printf("%d\n", b.x);                 → 0
//
// 期望输出三行：25 / 3 / 0

// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/cpp-lessons 检查 12
//
//   看这一课要干什么：dotnet run --project csharp/cpp-lessons 12
//   看答案（先自己写一遍）：dotnet run --project csharp/cpp-lessons 答案 12
//
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
