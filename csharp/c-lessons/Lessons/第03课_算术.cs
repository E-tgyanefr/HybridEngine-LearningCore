using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("03", "第 03 课：算术 —— 让程序算起来",
    """
1. 定义 int 甲 = 17、int 乙 = 5
2. 打印 甲 / 乙（整数除法，看结果是多少）
3. 打印 甲 % 乙（取余）
""")]
public static class 第03课
{
    public static string 模板 => """
// 第 03 课：算术 —— 让程序算起来
//
// 先介绍：C 的五个运算符
//
//     +   加      3 + 2   ⇒ 5
//     -   减      3 - 2   ⇒ 1
//     *   乘      3 * 2   ⇒ 6
//     /   除      7 / 2   ⇒ 3        ← ⚠ 两个整数相除，结果还是整数！
//     %   取余    7 % 2   ⇒ 1        ← 余数
//
// ⚠⚠⚠ **这一课唯一要记住的坑：整数除法会丢掉小数。**
//
//     17 / 5   ⇒  3        （不是 3.4！）
//     1 / 2    ⇒  0        （不是 0.5！）
//
// 为什么：两边都是 int，C 就用整数运算，小数部分**直接扔掉**（不是四舍五入）。
// 而且它**不报错、不警告** —— 你只会拿到一个错的结果。这是最难查的一类 bug。
//
// 想要小数，必须让**至少一边是小数**：
//     17.0 / 5     ⇒  3.4
//     17 / 5.0     ⇒  3.4
//     (double)17 / 5  ⇒  3.4        ← 强制转换，第 04 课细讲
//
// `%` 取余很有用：判断能不能整除（`x % 2 == 0` 就是偶数）。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   1. int 甲 = 17;  int 乙 = 5;
//   2. 打印 甲 / 乙   （想想结果会是几）
//   3. 打印 甲 % 乙

// ── 在下面写你的代码 ──

""";

    public static string 参考答案 => """
#include <stdio.h>

int main(void) {
    int 甲 = 17;
    int 乙 = 5;
    printf("%d\n", 甲 / 乙);      /* 3 —— 小数被丢掉了 */
    printf("%d\n", 甲 % 乙);      /* 2 —— 余数 */
    return 0;
}
""";


    [检查说明("打印了 2 行：3（整除）和 2（余数）")]
    public static CheckResult 检查_1(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "3",
            "2"
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

    [检查说明("用了变量相除（不是写死 3）")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("甲 / 乙"))
            return CheckResult.不过("没找到「甲 / 乙」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("用了取余运算符 %")]
    public static CheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("%"))
            return CheckResult.不过("没找到「%」—— 这一课要求用到它。");
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
