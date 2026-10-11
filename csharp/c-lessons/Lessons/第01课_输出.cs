using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("01", "第 01 课：printf —— 让程序开口说话",
    """
1. 打印一行文字：你好，C
2. 打印一个整数：42
3. 最后一行打印：完成
""")]
public static class 第01课
{
    public static string 模板 => """
// 第 01 课：printf —— 让程序开口说话
//
// 先介绍：C 程序的骨架
//   C 程序**一定**长这个样子，少一样都编译不过：
//
//       #include <stdio.h>          ← 头文件：printf 需要它（stdio = standard input/output）
//
//       int main(void) {            ← 主函数：程序从这里开始跑
//           ...                     ← 你的代码
//           return 0;               ← 告诉系统"正常结束"
//       }
//
// 三件必须记住的事：
//   ① 每条语句末尾要有**分号** ;    （漏了会报 expected ';'）
//   ② 代码块用**花括号** { }        （C 不看缩进，看括号）
//   ③ 打印用 printf("...")          （f = formatted，格式化输出）
//
// 换行要在引号里面写 \n —— 它不是"两个字符"，是一个换行符。
//   想打「你好」再换行：   printf("你好\n");
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   1. 打印一行文字：你好，C
//   2. 打印一个整数：42
//   3. 最后一行打印：完成
//
// 提示：完整程序，要自己写 #include 和 main。

// ── 在下面写你的代码 ──

""";

    public static string 参考答案 => """
#include <stdio.h>

int main(void) {
    printf("你好，C\n");
    printf("%d\n", 42);
    printf("完成\n");
    return 0;
}
""";


    [检查说明("程序能编译、能跑起来")]
    public static CheckResult 检查_1(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "你好，C",
            "42",
            "完成",
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

    [检查说明("打印了 3 行，内容分别是「你好，C」「42」「完成」")]
    public static CheckResult 检查_2(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "你好，C",
            "42",
            "完成",
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

    [检查说明("换行写对了（用的是 \\n，不是字面的反斜杠 n）")]
    public static CheckResult 检查_3(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string 全部 = string.Join("\n", run.行);
        if (全部.Contains("\\\\n"))
            return CheckResult.不过("输出里出现了「\\\\n」—— 换行要用 \\n 转义。");
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
