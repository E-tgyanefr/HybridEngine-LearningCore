using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CppLessons;

[课程("00", "第 00 课：跑通第一个程序 —— 把工具链装好",
    """
1. 确认 g++ 和 dotnet 都装好（本课会真的去调它们）
2. 搞清楚 C++ 和 Python 的流程差别：编译 → 链接 → 运行
3. 让编译器报一次错，并且学会从第一条 error 读起
""")]
public static class 第00课
{
    public static string 模板 => """
// 第 00 课：跑通第一个程序 —— 把工具链装好
//
// ⚠⚠ 这一课**不教语法**，只干一件事：让你这台机器真的能编译、能运行。
//
//    为什么把它排在第 00 课：
//      C++ 要先把源码编译成可执行文件，而这需要一个**单独安装的编译器**。
//      调研过的权威教程里，"卡在装工具"是放弃率最高的一步。
//      所以它值得单独占一课，而不是塞在第一课的注释里。
//
// ─────────────────────────────────────────────────────
//  一、需要什么（两样，都是免费的）
// ─────────────────────────────────────────────────────
//
//  ① .NET SDK  —— "课程运行器"。它替你敲 g++ 命令，并跑起来看结果。
//
//        winget install Microsoft.DotNet.SDK.8
//
//     ⚠ 要的是 **SDK**，不是 Runtime —— Runtime 里没有编译器。
//
//  ② g++       —— 真正的 C++ 编译器。
//
//        winget install BrechtSanders.WinLibs.POSIX.UCRT
//
//     装完**重开一个窗口**（让 PATH 生效），然后敲：
//
//        g++ --version
//
//     能看到版本号就成了。
//
//  ⚠ 装完必须重开窗口：PATH 是"进程启动时"读进去的，
//    已经开着的窗口里永远看不到新装的东西 —— 这是最常见的"我明明装了"。
//
// ─────────────────────────────────────────────────────
//  二、C++ 比 C 还多一步：编译、**链接**、运行
// ─────────────────────────────────────────────────────
//
//  C 和 C++ 都是"先编译、再运行"两步。C++ 内部其实还有第三步：
//
//      main.cpp
//         ↓  编译（compile）     每个 .cpp 单独变成"目标文件" .o
//      main.o
//         ↓  链接（link）        把 .o 和标准库拼成一个可执行文件
//      main.exe
//         ↓  运行
//      结果
//
//  为什么值得知道"链接"这一步：
//    **链接错误和编译错误长得不一样，说的是不同的事。**
//
//      编译错误  →  "你这行代码语法不对"        例如漏了分号
//      链接错误  →  "你用了某个东西，但我找不到它"  例如函数只有声明没定义
//
//    `undefined reference to ...` 就是典型的**链接**错误。
//    看到它别去改语法 —— 去查"那个函数/变量到底定义了没有"。
//    （多文件编译时这是最常见的坑，第 16 课会再遇到。）
//
// ─────────────────────────────────────────────────────
//  三、这一课唯一要练的手艺：读第一条 error
// ─────────────────────────────────────────────────────
//
//  报错长这样（这是**故意写错**的代码）：
//
//      main.cpp:4:28: error: expected ';' before 'return'
//          4 |     std::cout << "你好" << std::endl
//            |                                ^
//
//  拆开看四样东西：
//
//      main.cpp  :  哪个文件
//      4         :  **第几行**        ← 先看这个
//      28        :  第几列（次要）
//      error     :  严重程度
//
//  ⚠⚠ **两条铁律**：
//
//   ① **只看第一条 error。** 后面那几十条多数是它引起的连锁反应。
//      C++ 的模板报错尤其能滚出一屏 —— 全都不是你要修的。
//
//   ② **报错行号常常要往上多看一行。** 编译器是读到"它不认识的东西"
//      才报的错，而那个东西往往是**上一行**漏了什么造成的。
//
//  error 和 warning：
//
//      error    —— 编译**停了**，不会产生可执行文件（链接也不会发生）
//      warning  —— 编译**继续**，但它在提醒你"这里很可能是 bug"
//
//  ⚠⚠ **不要忽略 warning。** "能编译"不等于"能跑对"。
//     这门课后面会专门讲几个"只给 warning、但结果一定是错的"的坑。
//
// ─────────────────────────────────────────────────────
//  轮到你了
// ─────────────────────────────────────────────────────
//
//  这一课没有算法要写，只有三件事：
//
//    1. 确认 g++ 和 dotnet 都装好了
//       （界面首页的「环境自检」会显示；命令行敲 g++ --version 也行）
//
//    2. 让编译器**报一次错**，并且读懂它
//       下面留了一个 main。先故意漏掉一个分号 → 检查 → 看报错 →
//       把分号补上 → 再检查。
//
//       为什么要你故意写错一次：**报错是编译器在跟你说话**。
//       第一次被它骂过的人，第二次才不慌。这一课就是让你先被骂一次，
//       而且是在"没人看、不扣分"的时候被骂。
//
//    3. 让它打印出下面这三行，就算跑通了
//
//          工具链就绪
//          我准备好学 C++ 了
//          编译 → 链接 → 运行
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//  这一课在界面上点「检查」就行。
//  命令行（如果你在源码目录里）：
//
//      dotnet run --project csharp/cpp-lessons -- 检查 00
//
//  卡住了？把报错连同你的代码发出去问，比空想快得多。

// ── 在下面写你的代码 ──

#include <iostream>

int main() {
    std::cout << "工具链就绪" << std::endl;
    /* ⚠ 先故意把下面这一行的分号删掉，检查一次，看看编译器怎么说。
       读完报错再把分号补回来。 */
    std::cout << "我准备好学 C++ 了" << std::endl;
    std::cout << "编译 → 链接 → 运行" << std::endl;
    return 0;
}
""";

    public static string 参考答案 => """
#include <iostream>

int main() {
    /* ① 先漏掉分号编译一次，读懂第一条 error，再补回来 ——
       这一课练的就是"读报错"这件事。

       报错大概长这样：
           main.cpp:5:43: error: expected ';' before 'std'
       意思是"第 5 行这里，我以为该有个分号"。

       ② 顺便认识一下 **链接错误**长什么样：
           undefined reference to `某个函数'
       那不是语法问题，是"你用了它，但找不到它的定义"。 */
    std::cout << "工具链就绪" << std::endl;
    std::cout << "我准备好学 C++ 了" << std::endl;
    std::cout << "编译 → 链接 → 运行" << std::endl;
    return 0;
}
""";


    [检查说明("工具链就绪（gcc 与 dotnet 都在）")]
    public static CheckResult 检查_1a(string 代码)
    {
        return CppLesson.工具链就绪(代码);
    }

    [检查说明("程序能编译、能跑起来（说明 g++ 和 dotnet 都就绪）")]
    public static CheckResult 检查_1b(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "工具链就绪",
            "我准备好学 C++ 了",
            "编译 → 链接 → 运行",
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

    [检查说明("写的是 C++ 的头文件（#include <iostream> 而不是 stdio.h）")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("#include <iostream>"))
            return CheckResult.不过("没找到「#include <iostream>」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("用了 std::cout 输出")]
    public static CheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("std::cout"))
            return CheckResult.不过("没找到「std::cout」—— 这一课要求用到它。");
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
