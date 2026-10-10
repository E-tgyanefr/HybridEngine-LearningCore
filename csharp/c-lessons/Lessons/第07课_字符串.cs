using System;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("07", "第 07 课：字符串 —— 'A' 和 \"A\" 不是一回事",
    "写一个函数 `int 长度(const char* s)`，返回字符串长度（不能用 strlen）\n" +
    "写完了你就会明白字符串为什么用 \\0 结尾")]
public static class 第07课
{
    public static string 模板 => """
// 第 07 课：字符串
//
// 要做：自己写一个求长度的函数（**不许用 strlen**）
//
//     int 长度(const char* s);
//
// ⚠ C 的字符串与别的语言**完全不同**，这是本课的重点：
//
//   1. **'A' 和 "A" 不是一回事**
//        'A'  → 一个字符（char），值是 65
//        "A"  → 一个字符串，占 **2** 个字节：'A' 和结尾的 '\0'
//   2. 字符串靠 **结尾的 '\0'** 表示"到此为止"——
//      没有它，程序会一直往后读，直到撞上垃圾或崩溃
//   3. 所以求长度就是"从开头数到 '\0' 之前"
//
// 写完后运行：dotnet run --project csharp/c-lessons 检查 07

// ── 在下面写你的函数 ──
// 骨架已 include <string.h>，但**要求不许用 strlen** —— 自己数。

""";

    public static string 参考答案 => """
int 长度(const char* s) {
    int n = 0;
    while (s[n] != '\0') n++;
    return n;
}
""";

    [检查说明("编译通过，签名对")]
    public static CheckResult 检查_能编译(string 代码)
    {
        // ⚠ 与 03 课同一个坑：这里要传**表达式**，不是带分号的完整语句。
        //   传语句的话机制会把它当"要打印的值"再包一层 ⇒ 两处 printf 叠在一起。
        //   `长度` 返回 int ⇒ 表达式正好合适（机制用 %d 打印）。
        var run = CppCheck.写了函数(代码, "长度", "长度(\"abc\")");
        if (!run.Ok) return run;
        return CheckResult.过();
    }

    [检查说明("对不同字符串都对（含空串与中文）")]
    public static CheckResult 检查_正确性(string 代码)
    {
        var 骨架 = """
#include <stdio.h>
#include <string.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 长度(""));
    printf("%d\n", 长度("a"));
    printf("%d\n", 长度("abc"));
    printf("%d\n", 长度("hello world"));
    printf("%d\n", 长度("你好，C"));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        // "你好，C" 是 3 个中文字 + 1 个中文逗号 + 1 个 C = UTF-8 下 3*3+3+1 = 13 字节
        // ⚠ 期望值我第一版算错了（写 13）。
        //   UTF-8 下："你好，C" = 你(3) + 好(3) + ，(3，中文逗号) + C(1) = **10 字节**。
        //   13 是我把逗号按英文半角(1 字节)算错了 —— 而这个"错"会被判据抓住：
        //   用参考答案跑检查时它报「应该是 13，实际 10」，于是我回来改期望值。
        //   ⇒ 这正是"用参考答案跑一遍"的价值：**它抓的是课程自己的错**。
        string[] 期望 = { "0", "1", "3", "11", "10" };
        string[] 输入 = { "\"\"", "\"a\"", "\"abc\"", "\"hello world\"", "\"你好，C\"" };
        if (run.行.Count < 期望.Length)
            return CheckResult.不过($"应该打印 {期望.Length} 行，实际 {run.行.Count} 行。");
        for (int i = 0; i < 期望.Length; i++)
            if (run.行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"长度({输入[i]}) 应该是 {期望[i]}，实际「{run.行[i].Trim()}」。\n" +
                    "      ⚠ 空串要返回 0（`while (s[0] != '\\0')` 时不进循环）。\n" +
                    "      ⚠ 中文在 UTF-8 下**一个汉字 3 个字节** —— 所以「你好，C」是 13 不是 3。\n" +
                    "        这不是你写错了，是 C 的字符串按**字节**数，不按「字数」。");
        return CheckResult.过("空串 / 1 字符 / 英文 / 含空格 / 中文（字节数）全对");
    }

    [检查说明("没有偷偷用 strlen")]
    public static CheckResult 检查_没用strlen(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains("strlen"))
            return CheckResult.不过(
                "用了 strlen —— 这一课的要求是**自己数**。\n" +
                "      目的就是让你体会「字符串靠 \\0 结尾」这件事。\n" +
                "      提示：`while (s[n] != '\\0') n++;`");
        return CheckResult.过();
    }

    [检查说明("用的是 '\\0'（单引号）比较")]
    public static CheckResult 检查_用单引号(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        // ⚠ '"\0"'（双引号）是**字符串**，拿 char 跟它比是错的 —— 而且 C 通常只给警告
        if (去注释.Contains("\"\\0\"") || 去注释.Contains("\"\\0\""))
            return CheckResult.不过(
                "用了**双引号**的 \"\\0\" —— 那是字符串（指向内存的指针），\n" +
                "      不能跟 `s[n]`（一个 char）比较。要用**单引号**：`s[n] != '\\0'`。\n" +
                "      这正是本课标题说的：'A' 和 \"A\" 不是一回事。");
        if (!去注释.Contains("'\\0'"))
            return CheckResult.不过("没看到用 '\\0' 判断结尾。字符串的结束标记就是它。");
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

