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
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    让程序打印东西（printf）
//
//  需要先会
//    第 00 课：能编译、能跑
//
// ─────────────────────────────────────────────────────
//  先介绍：C 程序的骨架
// ─────────────────────────────────────────────────────
//
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
//  用法与例子
// ─────────────────────────────────────────────────────
//
//  ① 打印一段固定文字：把内容放在双引号里。
//
//      printf("你好，C\n");
//
//  ② 打印一个值：先用**格式符**占个位，再把值写在逗号后面。
//
//      printf("%d\n", 42);
//
//      %d 就是"这里放一个整数"。逗号后面的 42 会填进去。
//
//  ③ 一行里放多个值：占位符和值的**顺序一一对应**。
//
//      printf("%d 加 %d 等于 %d\n", 1, 2, 3);      ⇒ 1 加 2 等于 3
//
//  ④ 常用的四个格式符（这一课先记 %d 和 %s，后面几课都会用到）：
//
//      %d    整数          int
//      %f    小数          double
//      %s    字符串        char*
//      %c    单个字符      char
//
//  一个完整的、能跑的例子：
//
//      #include <stdio.h>
//
//      int main(void) {
//          printf("你好，C\n");
//          printf("%d\n", 42);
//          printf("完成\n");
//          return 0;
//      }
//
//  输出（三行）：
//
//      你好，C
//      42
//      完成
//
//  ⚠ 注意输出里**看不到** `%d`，也看不到 `\n`：
//      %d 被 42 替换掉了，\n 变成了一个真正的换行。
//
//  ⚠ 如果不写 \n，两次 printf 的输出会挤在同一行：
//      printf("你好，C");    printf("完成");
//      ⇒ 你好，C完成
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
//  ⚠ 坑 1：漏了 #include <stdio.h>
//
//      错误写法：
//          int main(void) {
//              printf("你好\n");
//              return 0;
//          }
//
//      你会看到什么（gcc 原文，下面的横线是编译器画的，原文里是竖线）：
//
//          main.c:2:5: error: implicit declaration of function 'printf' [-Wimplicit-function-declaration]
//              2 │     printf("你好\n");
//                │     ^~~~~~
//          main.c:1:1: note: include '<stdio.h>' or provide a declaration of 'printf'
//
//      为什么：printf 不是语言自带的关键字，它只是 stdio.h 里**声明**的一个函数。
//      编译器从上往下读，读到 printf 时没见过它，只好猜"它大概返回 int" ——
//      这个"猜"就是 implicit declaration（隐式声明）。
//      现代 C（C99 以后）已经把这个"猜"当成错误了。
//      ⇒ 看到这条，第一反应永远是：**头文件写了吗？**
//
//  ⚠ 坑 2：格式符和值的类型对不上
//
//      错误写法（用 %d 去打印字符串）：
//
//          printf("%d\n", "你好");
//
//      你会看到什么（gcc 原文）：
//
//          main.c:3:14: warning: format '%d' expects argument of type 'int', but argument 2 has type 'char *' [-Wformat=]
//              3 │     printf("%d\n", "你好");
//                │             ~^     ~~~~~~
//                │              │     │
//                │              │     │
//                │              │     char *
//                │              int
//                │             %s
//
//      运行时你看到的不是报错，而是**一个莫名其妙的整数**（内存地址）：
//
//          267931728
//
//      为什么：%d 告诉 printf"把这段内存当成一个 int 来解释"，
//      而 "你好" 是 char*（字符串第一个字符的地址）。
//      它照做了 —— 于是你得到一个地址，而不是你想要的文字。
//      ⚠ 好消息：gcc 会**警告**你，而且最后那行 `%s` 就是它给你的改法。
//      ⇒ **warning 要读，它经常直接告诉你该换成哪个格式符。**
//
//  ⚠ 坑 3：把 \n 写成了 /n，或者漏了分号
//
//      错误写法：
//          printf("你好/n");          /* 斜杠方向反了 */
//
//      你会看到什么：编译**通过**，但屏幕上真的印出「你好/n」四个字。
//
//      为什么：反斜杠 `\` 才叫"转义符"。`/` 只是个普通的斜杠字符，
//      C 不认识 `\n` 以外的写法，就把它原样打出来。
//
//      另一种更吵的错误 —— 漏分号：
//
//          printf("你好，C\n")
//          return 0;
//
//          main.c:3:17: error: expected ';' before 'return'
//              3 │     printf("你好，C\n")
//                │                 ^
//                │                 ;
//                │     return 0;
//                │     ~~~~~~
//
//      为什么：编译器读到 `return` 时才发现上一句没说"结束"。
//      ⚠ 报的是**第 3 行**，但该补分号的是**第 2 行** —— 往上多看一行。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   1. 打印一行文字：你好，C
//   2. 打印一个整数：42
//   3. 最后一行打印：完成
//
// 提示：完整程序，要自己写 #include 和 main。
//
// ⚠ 期望输出（三行，一字不差）：
//
//      你好，C
//      42
//      完成
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。
//   命令行：dotnet run --project csharp/c-lessons 检查 01
//   看这一课要干什么：dotnet run --project csharp/c-lessons 01
//   看答案：dotnet run --project csharp/c-lessons 答案 01
//
// ── 在下面写 ──
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
