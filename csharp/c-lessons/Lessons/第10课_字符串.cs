using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("10", "第 10 课：字符串 —— 一串字符",
    """
写函数 int 长度(char 文字[])：返回字符串长度（不能用 strlen）
""")]
public static class 第10课
{
    public static string 模板 => """
// 第 10 课：字符串 —— 一串字符
//
// 先介绍：C 的字符串其实是"字符数组 + 一个结尾标记"
//
//     char 名字[] = "小明";
//
// 它在内存里长这样：
//     '小' '明' '\0'
//                ↑
//                结束标记（值是 0，写作 '\0'）
//
// ⚠⚠⚠ **C 的字符串靠 `\0` 判断"到哪结束"**，它**没有长度字段**。
//   所以你遍历字符串时，判据是"这个字符是不是 '\0'"，不是长度：
//
//     int i = 0;
//     while (文字[i] != '\0') {     ← 到结束标记就停
//         i++;
//     }
//
// ⇒ 这就是为什么这一课请你**自己数**（不用 strlen）：
//   数一遍你才会真正明白"字符串是怎么结束的"。
//
// ⚠ `'A'`（单引号）是一个**字符**；`"A"`（双引号）是一个**字符串**（含 '\0'）。
//   它们是不同类型，不能混：
//     char c = 'A';         ✅
//     char c = "A";         ❌ 类型不匹配（编译器会警告）
//
// ⚠ 中文一个字通常占 3 个字节（UTF-8），所以 `strlen("小明")` 是 6 不是 2。
//   检查里只用**英文**测长度，避免这个干扰。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   写函数：  int 长度(char 文字[])
//   返回字符串的长度（**不要用 strlen**，自己数）
//
// 提示：不用写 main。

// ── 在下面写你的函数 ──

""";

    public static string 参考答案 => """
int 长度(char 文字[]) {
    int i = 0;
    /* ⚠ 判据是"是不是结束标记"，不是某个固定长度 */
    while (文字[i] != '\0') {
        i++;
    }
    return i;
}
""";


    [检查说明("写了 长度 函数")]
    public static CheckResult 检查_1(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 长度("abc"));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过)
        {
            if (run.编译信息.Contains("长度"))
                return CheckResult.不过("没找到「长度」这个函数 —— 或者签名和题目要求不一致。");
            return CppCheck.编译失败说明(run);
        }
        return CheckResult.过("「长度」可以正常调用");
    }

    [检查说明("多组都对（含空串）")]
    public static CheckResult 检查_2(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 长度(""));
    printf("%d\n", 长度("a"));
    printf("%d\n", 长度("abc"));
    printf("%d\n", 长度("hello world"));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "0",
            "1",
            "3",
            "11"
        };
        string[] 输入 = {
            "\"\"",
            "\"a\"",
            "\"abc\"",
            "\"hello world\""
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
