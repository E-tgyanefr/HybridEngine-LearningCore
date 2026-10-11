using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("04", "第 04 课：小数 —— float 与 double",
    """
1. 定义 double 圆周率 = 3.14159
2. 打印 圆周率 * 2 * 2（半径 2 的圆面积）
3. 只保留两位小数（用 %.2f）
""")]
public static class 第04课
{
    public static string 模板 => """
// 第 04 课：小数 —— float 与 double
//
// 先介绍：C 里表示小数的两种类型
//
//     float    单精度    大约 7 位有效数字
//     double   双精度    大约 15 位      ← **默认选它**
//
// ⚠ 日常写代码**一律用 double**：
//   float 省的那点内存现在毫无意义，而精度不够会在累加很多次之后露出来。
//
// 打印小数的格式符是 **%f**（不是 %d！）：
//
//     double x = 3.14;
//     printf("%f\n", x);        ⇒ 3.140000      （默认 6 位小数）
//     printf("%.2f\n", x);      ⇒ 3.14          （保留 2 位）
//
// ⚠⚠ 用 %d 打小数是**经典的致命错**：
//     printf("%d\n", 3.14);     ⇒ 打印出一个完全莫名其妙的整数
//     （因为 %d 按整数去解释那段内存，而小数的内存布局不是整数。）
//     而且**编译器不一定报错** —— 你只会看到一个乱码般的数字。
//
// ⚠ 字面量 `3.14159` 天生就是 double；想写 float 要加后缀 `3.14f`。
//
// ⚠ 第 03 课那个坑在这里有解：
//     17 / 5        ⇒ 3        （整数除法）
//     17.0 / 5      ⇒ 3.4      （有小数参与 ⇒ 按小数算）
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   1. double 圆周率 = 3.14159;
//   2. 打印 圆周率 * 2 * 2   —— 半径 2 的圆面积
//   3. 用 %.2f 只保留两位小数

// ── 在下面写你的代码 ──

""";

    public static string 参考答案 => """
#include <stdio.h>

int main(void) {
    double 圆周率 = 3.14159;
    printf("%.2f\n", 圆周率 * 2 * 2);
    return 0;
}
""";


    [检查说明("打印了 1 行，约等于 12.57（保留两位）")]
    public static CheckResult 检查_1(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "12.57"
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

    [检查说明("用了 double 而不是 float")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("double"))
            return CheckResult.不过("没找到「double」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("用了 %.2f（保留两位）")]
    public static CheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("%.2f"))
            return CheckResult.不过("没找到「%.2f」—— 这一课要求用到它。");
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
