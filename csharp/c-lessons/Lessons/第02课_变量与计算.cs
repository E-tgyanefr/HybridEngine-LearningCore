using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("02", "第 02 课：变量与计算 —— 让程序算起来",
    "1. 定义 int 变量 半径 = 5\n" +
    "2. 用 3.14159 作为圆周率，算面积 = 圆周率 × 半径 × 半径\n" +
    "3. 用 %.2f 打印面积（保留两位小数）")]
public static class 第02课
{
    public static string 模板 => """
// 第 02 课：变量与计算
//
// 要做：
//   1. 定义 int 变量 半径 = 5
//   2. 用 3.14159 作为圆周率，算 面积 = 圆周率 × 半径 × 半径
//   3. 用 %.2f 打印面积（保留两位小数）
//
// 本课的重点：**整数与小数是两种类型**
//   · int 存整数          半径 = 5
//   · double 存小数       面积 = 78.54...
//   · 打印 double 要用 %f，不是 %d
//   · %.2f 的意思是"保留两位小数"
//
// ⚠ 常见坑：`double 面积 = 3.14159 * 半径 * 半径;` 里如果 半径 是 int，
//   乘法会先按 int 算 —— 但这里因为 3.14159 是 double，
//   整体会提升成 double，所以没问题。真正会错的是 `int 面积 = ...`（截断）。
//
// 写完后运行：dotnet run --project csharp/c-lessons 检查 02

// ── 在下面写你的代码 ──
// 提示：本课是完整程序，要自己写 #include 和 main。

""";

    public static string 参考答案 => """
#include <stdio.h>

int main(void) {
    int 半径 = 5;
    double 圆周率 = 3.14159;
    double 面积 = 圆周率 * 半径 * 半径;
    printf("%.2f\n", 面积);
    return 0;
}
""";

    [检查说明("程序能编译运行")]
    public static CheckResult 检查_能运行(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        if (run.Raw.Error != null) return CheckResult.不过("程序没能正常运行：\n      " + run.Raw.Error);
        if (run.行.Count == 0) return CheckResult.不过("一行都没打印。");
        return CheckResult.过();
    }

    [检查说明("打印的面积是 78.54")]
    public static CheckResult 检查_面积(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        var 行 = run.行;
        if (行.Count == 0) return CheckResult.不过("没有输出。");
        string 首 = 行[0].Trim();
        if (首 != "78.54")
            return CheckResult.不过(
                $"应该打印 78.54（3.14159 × 5 × 5 = 78.53975，保留两位 = 78.54），实际是「{首}」。\n" +
                "      检查三件事：半径是 5 吗？用的是 %.2f 吗？乘法写了 半径 × 半径 吗？");
        return CheckResult.过();
    }

    [检查说明("用了 double（不是 int 存小数）")]
    public static CheckResult 检查_用了double(string 代码)
    {
        if (!代码.Contains("double"))
            return CheckResult.不过("没看到 double。\n      面积是小数，必须用 double（用 int 会被截断成整数 78）。");
        return CheckResult.过();
    }

    [检查说明("打印格式用的是 %f 而不是 %d")]
    public static CheckResult 检查_格式对(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        int i = 去注释.IndexOf("printf");
        if (i < 0) return CheckResult.不过("没找到 printf。");
        string 段 = 去注释.Substring(i, Math.Min(80, 去注释.Length - i));
        if (段.Contains("%d"))
            return CheckResult.不过("printf 里用了 %d 打小数 —— %d 只能打整数，\n      打小数要用 %f（保留两位是 %.2f）。");
        if (!段.Contains("%.2f") && !段.Contains("%f"))
            return CheckResult.不过("printf 里没看到 %f。打 double 要用 %f 或 %.2f。");
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
