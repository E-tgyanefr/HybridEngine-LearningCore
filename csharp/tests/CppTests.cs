// C++ 支持的测试：编译器、运行、报错翻译、判分。
//
// ══ 为什么要单独测 ════
//
// C++ 这条路比其他语言多两个**外部依赖**：编译器、临时目录。
// 于是多出几类只有真跑才会暴露的问题：
//
//   · 编译失败被误判成成功（因为上一轮的可执行文件还在）—— 我踩过
//   · 同一路径反复编译复用了旧产物，跑的是上一次的代码 —— 我踩过
//   · 警告检测发生在编译后、解释发生在运行后，读到的是被覆盖过的内容 —— 我踩过
//   · 传输代码时的多层转义把源码弄坏 —— 我踩过
//
// 这四条都是"看起来在跑、结果却是错的"，比崩溃危险得多。所以逐条钉住。

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LearnCode;

internal static class CppTests
{
    public static void Run(Action<bool, string> check, Action<object, object, string> eq)
    {
        Console.WriteLine("\n=== C++ 支持 ===");

        // ── 编译器能不能找到 ──
        bool 有编译器 = CppRunner.Compiler != null;
        check(有编译器, "找得到 C++ 编译器（找不到就跳过其余测试）");
        if (!有编译器)
        {
            Console.WriteLine("  （没找到编译器，跳过 C++ 的其余测试）");
            Console.WriteLine("  " + CppRunner.InstallHint.Split('\n')[0]);
            return;
        }
        Console.WriteLine($"  编译器 = {CppRunner.Compiler}");

        // ── 能编译并运行 ──
        var r1 = CppRunner.BuildAndRun(
            "#include <iostream>\nint main() {\n    std::cout << 6 * 7 << \"\\n\";\n    return 0;\n}\n");
        eq(r1.Error, null, "正确的程序能编译并运行");
        eq(string.Join(",", r1.Lines), "42", "程序输出正确");

        // ── 编译失败必须被认出来（不能因为旧文件而误判成功）──
        var r2 = CppRunner.BuildAndRun("#include <iostream>\nint main() {\n    int x = 5\n    return 0;\n}\n");
        check(r2.CompileFailed, "漏分号被判定为「编译失败」");
        check(r2.Error != null, "编译失败时给出了错误信息");

        // 关键：编译失败之后，再跑一个正确程序仍要正常 —— 确认没有残留状态互相污染
        var r3 = CppRunner.BuildAndRun(
            "#include <iostream>\nint main() {\n    std::cout << 1 << \"\\n\";\n    return 0;\n}\n");
        eq(string.Join(",", r3.Lines), "1", "编译失败之后仍能正常跑新程序（无状态残留）");

        // ── 报错翻译：必须指出「真凶在上一行」──
        string 坏 = "#include <iostream>\nint main() {\n    int x = 5\n    std::cout << x;\n    return 0;\n}\n";
        var r4 = CppRunner.BuildAndRun(坏);
        string 解释 = ExplainCpp.For(r4.CompileOutput, 坏, true);
        check(解释.Contains("上一行"), "翻译指出了「真凶在上一行」（C++ 报错最坑的地方）");
        check(解释.Contains("分号"), "翻译提到了分号");
        check(解释.Contains("原始报错"), "保留了原始报错");

        // ── 运行时崩溃必须被认出来 ──
        // 实测退出码：除零 = -1073741676 (0xC0000094)
        var r5 = CppRunner.BuildAndRun(
            "#include <iostream>\nint main() {\n    int a = 5, b = 0;\n    std::cout << a / b;\n    return 0;\n}\n");
        check(r5.Error != null, "整数除以 0 被认出来（不是静默通过）");
        if (r5.Error != null)
            check(ExplainCpp.For(r5.RunOutput, "", false).Contains("除以 0"),
                  "除零被翻译成「除以 0」");

        // ── 未初始化变量：输出对，但必须判不通过 ──
        var r6 = CppRunner.BuildAndRun(
            "#include <iostream>\nint main() {\n    int s;\n    for (int i = 1; i <= 3; i++) s += i * i;\n    std::cout << s << \"\\n\";\n    return 0;\n}\n");
        check(r6.HasCorrectnessWarning, "未初始化变量触发了「正确性警告」");
        // 关键：编译输出里必须还有警告 —— 不能被运行输出覆盖掉
        check(r6.CompileOutput.Contains("uninitialized"),
              "编译输出保留了 uninitialized 警告（没被运行输出覆盖）");
        check(r6.Raw.Contains("uninitialized") || r6.CompileOutput.Contains("uninitialized"),
              "Raw 仍能取到编译信息");
        check(ExplainCpp.ExplainWarning(r6.CompileOutput).Contains("随机值"),
              "警告解释讲到了「随机值」这个关键概念");
    }
}
