using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("05", "第 05 课：scanf —— 让程序会听",
    """
1. 用 scanf 读一个整数
2. 打印这个整数加 1 的结果
""")]
public static class 第05课
{
    public static string 模板 => """
// 第 05 课：scanf —— 让程序会听
//
// 先介绍：怎么从键盘读东西
//
//     int 数;
//     scanf("%d", &数);         ← 读一个整数放进「数」
//                  ↑
//                  这个 & 是**取地址**，不能少！
//
// ⚠⚠⚠ **这一课唯一要记住的坑：scanf 必须写 `&`。**
//
//     为什么：printf 是"把值给出去"，只要值就行；
//     而 scanf 是"把读到的值**存进去**"，它必须知道**存到哪块内存**。
//     `&数` 就是"变量「数」所在的内存地址"。
//
//     写了 &  ⇒  正确
//     忘了 &  ⇒  程序**可能在运行时崩溃**（Segmentation fault），
//                也可能"看起来跑完了"但变量没被赋值 ——
//                编译器只给个警告，不报错。所以特别危险。
//
// ⚠ 格式符要和类型对上：
//     读 int      scanf("%d", &x);
//     读 double   scanf("%lf", &x);     ← ⚠ 是 %lf（有个 l），不是 %f
//     读字符      scanf("%c", &c);
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   1. 定义一个 int 变量
//   2. 用 scanf("%d", &变量) 读一个整数
//   3. 打印它 + 1 的结果
//
// ⚠ 检查会假装从键盘输入一个数，看你的程序有没有读对。

// ── 在下面写你的代码 ──

""";

    public static string 参考答案 => """
#include <stdio.h>

int main(void) {
    int 数;
    scanf("%d", &数);          /* ⚠ & 不能少 */
    printf("%d\n", 数 + 1);
    return 0;
}
""";


    [检查说明("写了 scanf 而且带了 & 取地址")]
    public static CheckResult 检查_1(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("&"))
            return CheckResult.不过("没找到「&」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("需要输入才能跑（说明真的在读）")]
    public static CheckResult 检查_2(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        return CheckResult.过("能编译运行（这一课的程序要读输入）");
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
