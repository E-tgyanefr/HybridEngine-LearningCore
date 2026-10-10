using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("01", "第 01 课：输出 —— 让程序开口说话",
    "1. 用 printf 打印一行文字：你好，C\n" +
    "2. 定义两个 int 变量：甲 = 6、乙 = 7\n" +
    "3. 打印它们的和。要求真的用变量相加，不要把 13 硬写死。")]
public static class 第01课
{
    public static string 模板 => """
// 第 01 课：输出 —— 让程序开口说话
//
// 要做：
//   1. 打印一行文字：你好，C
//   2. 定义 int 变量 甲 = 6，int 变量 乙 = 7
//   3. 打印它们的和（用变量相加，不要写死 13）
//
// 记住四件事：
//   · 每条语句末尾要有分号 ;
//   · 打印用 printf("格式", 值);
//   · 换行在格式串里用 \n
//   · **int 的格式是 %d**（用 %s 打整数是经典的错）
//
// 写完后运行：dotnet run --project csharp/c-lessons 检查 01

// ── 在下面写你的代码 ──
// 提示：本课是完整程序，要自己写 #include 和 main：

//   #include <stdio.h>
//
//   int main(void) {
//       // 在这里写
//       return 0;
//   }

""";

    public static string 参考答案 => """
#include <stdio.h>

int main(void) {
    printf("你好，C\n");

    int 甲 = 6;
    int 乙 = 7;
    printf("%d\n", 甲 + 乙);

    return 0;
}
""";

    [检查说明("程序能不能编译、能不能跑起来")]
    public static CheckResult 检查_程序能运行(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        if (run.Raw.Error != null)
            return CheckResult.不过("程序没能正常运行：\n      " + run.Raw.Error);
        if (run.行.Count == 0)
            return CheckResult.不过("程序一行都没打印。\n      printf 写了吗？格式串后面要跟换行 \\n。");
        return CheckResult.过();
    }

    [检查说明("打印了「你好，C」这一行")]
    public static CheckResult 检查_第一行文字(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        var 行 = run.行;
        if (行.Count < 1)
            return CheckResult.不过("一行都没打印出来。");
        if (!行[0].Contains("你好，C"))
            return CheckResult.不过($"第一行应该是「你好，C」，实际打印的是「{行[0]}」。\n      注意逗号是**中文逗号**，C 是大写。");
        return CheckResult.过($"第一行 = {行[0]}");
    }

    [检查说明("第二行是 13（= 6 + 7）")]
    public static CheckResult 检查_和(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        var 行 = run.行;
        if (行.Count < 2)
            return CheckResult.不过($"应该有 2 行输出，实际 {行.Count} 行。\n      是不是忘了打印和？");
        if (行[1].Trim() != "13")
            return CheckResult.不过($"第二行应该是 13，实际是「{行[1].Trim()}」。\n      检查：变量是 6 和 7 吗？printf 用的是 %d 吗？");
        return CheckResult.过();
    }

    [检查说明("真的用了变量相加（不是把 13 写死）")]
    public static CheckResult 检查_不是硬写(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains("13"))
            return CheckResult.不过("代码里出现了字面量 13 —— 那是把答案写死了。\n      要求用两个变量相加：printf(\"%d\\n\", 甲 + 乙);");
        if (!去注释.Contains("+"))
            return CheckResult.不过("没看到加法运算。要用 甲 + 乙，不是直接打印一个数。");
        return CheckResult.过();
    }

    [检查说明("没有忘记 return 0")]
    public static CheckResult 检查_返回值(string 代码)
    {
        if (!代码.Contains("return"))
            return CheckResult.不过("main 结尾要有 return 0;（告诉系统程序正常结束）");
        return CheckResult.过();
    }

    internal static string 去掉注释(string s)
    {
        var o = new System.Text.StringBuilder();
        bool 块 = false;
        for (int i = 0; i < s.Length; i++)
        {
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n') i++;
                o.Append('\n'); continue;
            }
            if (!块 && i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*') { 块 = true; i++; continue; }
            if (块 && i + 1 < s.Length && s[i] == '*' && s[i + 1] == '/') { 块 = false; i++; continue; }
            if (!块) o.Append(s[i]);
        }
        return o.ToString();
    }
}
