using System;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("08", "第 08 课：指针 —— 改了参数，外面会不会变",
    "写一个 `void 加倍(int* p)`，把指针指向的数乘 2\n" +
    "对比：写一个 `void 加倍_传值(int x)` —— 它改不动外面的变量")]
public static class 第08课
{
    public static string 模板 => """
// 第 08 课：指针 —— 改了参数，外面会不会变
//
// 要做两个函数，**故意形成对比**：
//
//     void 加倍(int* p)      { … }   // 通过指针改外面
//     void 加倍_传值(int x)  { … }   // 只改自己那份拷贝
//
// 这一课要让你亲眼看到一件事：
//   **C 的函数参数永远是"拷贝"**。想让函数改外面的变量，必须传地址。
//
// 三个符号（背下来）：
//
//     int* p    声明一个"指向 int 的指针"
//     &变量      取地址（"这个变量住在哪"）
//     *p        解引用（"去那个地址看看里面的值"）
//
//        int a = 5;
//        加倍(&a);        // 传的是地址
//        printf("%d", a); // 变成 10
//
//        int b = 5;
//        加倍_传值(b);     // 传的是值（一份拷贝）
//        printf("%d", b); // 还是 5 —— 函数改的是它自己那份拷贝
//
// 写完后运行：dotnet run --project csharp/c-lessons 检查 08

// ── 在下面写你的两个函数 ──
// 这一课**不用**写 main。

""";

    public static string 参考答案 => """
void 加倍(int* p) {
    *p = *p * 2;
}

void 加倍_传值(int x) {
    x = x * 2;
}
""";

    [检查说明("两个函数都写了，签名对")]
    public static CheckResult 检查_签名(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("加倍"))
            return CheckResult.不过("没找到「加倍」。");
        if (!去注释.Contains("int*") && !去注释.Contains("int *"))
            return CheckResult.不过(
                "没看到指针参数。`加倍` 要收一个**地址**：`void 加倍(int* p)`。");
        if (!去注释.Contains("加倍_传值"))
            return CheckResult.不过("没找到「加倍_传值」（这一课要两个函数做对比）。");
        return CheckResult.过();
    }

    [检查说明("加倍(&a) 真的改到了外面的 a")]
    public static CheckResult 检查_指针生效(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    int a = 5;
    加倍(&a);
    printf("%d\n", a);
    int b = 100;
    加倍(&b);
    printf("%d\n", b);
    int c = 0;
    加倍(&c);
    printf("%d\n", c);
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "10", "200", "0" };
        if (run.行.Count < 3)
            return CheckResult.不过($"应该打印 3 行，实际 {run.行.Count} 行。");
        for (int i = 0; i < 3; i++)
            if (run.行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例：应该是 {期望[i]}，实际「{run.行[i].Trim()}」。\n" +
                    "      要在函数里改到外面，必须 `*p = *p * 2;`（**带星号**）。\n" +
                    "      写成 `p = p * 2;` 改的是指针自己的地址 —— 外面看不到变化。");
        return CheckResult.过("5→10 · 100→200 · 0→0 全对");
    }

    [检查说明("加倍_传值() 确实改不动外面（这正是要看到的）")]
    public static CheckResult 检查_传值无效(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    int b = 5;
    加倍_传值(b);
    printf("%d\n", b);
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        if (run.行.Count < 1) return CheckResult.不过("没有输出。");
        if (run.行[0].Trim() != "5")
            return CheckResult.不过(
                $"`加倍_传值(b)` 之后 b 应该是 **5**（没变），实际「{run.行[0].Trim()}」。\n" +
                "      ⚠ 这一项就是本课的重点：**C 的参数是拷贝**，函数改的是它自己那份。\n" +
                "        如果你把它改成用指针了，那它就变成「加倍」了，不再是「传值」版。\n" +
                "        这一课要的就是这两个函数的**差别**。");
        return CheckResult.过("b 仍是 5 —— 传值改不动外面，符合预期");
    }

    [检查说明("用了 & 取地址 + * 解引用")]
    public static CheckResult 检查_符号(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("*"))
            return CheckResult.不过("没看到解引用 `*p`。要通过指针改值，必须写 `*p = …`。");
        return CheckResult.过();
    }

    internal static string 去掉注释(string s)
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

