using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("00", "第 00 课：跑通第一个程序 —— 把工具链装好",
    """
1. 确认 gcc 和 dotnet 都装好（本课会真的去调它们）
2. 搞清楚为什么 C 要"先编译、再运行"两步
3. 让 gcc 报一次错，并且学会从第一条 error 读起
""")]
public static class 第00课
{
    public static string 模板 => """
// 第 00 课：跑通第一个程序 —— 把工具链装好
//
// ⚠⚠ 这一课**不教语法**，只干一件事：让你这台机器真的能编译、能运行。
//
//    为什么把它排在第 00 课：
//      C / C++ / Java / C# 四门课都要**先把代码编译成可执行文件**，
//      而这需要外部工具链。调研过的主流教程里，"卡在装工具"是
//      **放弃率最高的一步** —— 比指针、比虚函数都更容易让人走掉。
//      所以它值得单独占一课，而不是塞在第一课的注释里。
//
// ─────────────────────────────────────────────────────
//  一、需要什么（两样，都是免费的）
// ─────────────────────────────────────────────────────
//
//  ① .NET SDK  —— 它是"课程运行器"。这门课用它来**编译并运行你的 C 代码**，
//                 所以你不需要自己敲 gcc 命令，它会替你敲。
//
//        winget install Microsoft.DotNet.SDK.8
//
//     或者去 https://dotnet.microsoft.com/download 下。
//     ⚠ 要的是 **SDK**，不是 Runtime —— Runtime 里没有编译器。
//
//  ② gcc       —— 真正的 C 编译器。
//
//        winget install BrechtSanders.WinLibs.POSIX.UCRT
//
//     装完**重开一个窗口**（让 PATH 生效），然后敲：
//
//        gcc --version
//
//     能看到版本号就成了。
//
//  ⚠ 装完必须重开窗口：PATH 是"进程启动时"读进去的，
//    已经开着的窗口里永远看不到新装的东西 —— 这是最常见的"我明明装了"。
//
// ─────────────────────────────────────────────────────
//  二、C 为什么要"两步"：先编译、再运行
// ─────────────────────────────────────────────────────
//
//  Python 里你写 `python 脚本.py`，它**一边读一边执行**。
//  C 不是这样。C 的流程是：
//
//      你写的 hello.c
//          ↓  gcc hello.c -o hello.exe     ← 第一步：编译
//      hello.exe（机器码，人看不懂）
//          ↓  双击 / 敲 hello.exe           ← 第二步：运行
//      屏幕上出现结果
//
//  中间那个 hello.exe 是**真正的程序**：它不再需要 gcc，
//  也不需要你那份 .c 文件，拷给别人就能跑。
//
//  代价是：**你写的代码要先被人（编译器）检查一遍**。
//  语法有错，编译器当场拒绝，连失败的程序都不给你。
//  所以 C 的报错比 Python 多，但**很多错在运行之前就被拦住了** ——
//  这是编译型语言换来"跑得快"所付的钱。
//
//  ⚠ 这门课的流程是：
//
//        写代码  →  检查  →  自动编译 + 自动运行 + 看结果
//
//    "编译"和"运行"两步是**替你做的**，但你必须知道它们在发生 ——
//    不然你读不懂报错，也解释不了"为什么我的程序跑不起来"。
//
// ─────────────────────────────────────────────────────
//  三、这一课唯一要练的手艺：读第一条 error
// ─────────────────────────────────────────────────────
//
//  编译器的报错长这样（这是**故意写错**的代码）：
//
//      hello.c:3:1: error: expected ';' before '}' token
//         3 | }
//           | ^
//
//  拆开看就四样东西：
//
//      hello.c  :  哪个文件
//      3        :  **第几行**        ← 先看这个
//      1        :  第几列（次要）
//      error    :  严重程度
//
//  ⚠⚠ **两条铁律**：
//
//   ① **只看第一条 error。** 后面那几十条多数是它引起的连锁反应 ——
//      一个没写对的括号能让编译器抱怨一整屏。改完第一条再重新编译。
//
//   ② **报错行号常常要往上多看一行。** 编译器是在读到"它不认识的
//      东西"时才报错的，而那个东西往往是**上一行漏了分号**造成的。
//      上面那个例子说"第 3 行 } 前面少了分号"，真正该补分号的是第 2 行。
//
//  error 和 warning 是两回事：
//
//      error    —— 编译**停了**，没有可执行文件产生
//      warning  —— 编译**继续**，但它在提醒你"这里很可能是 bug"
//
//  ⚠⚠ **不要忽略 warning。** "能编译"不等于"能跑对"。
//     这门课后面会专门讲几个"只给 warning、但结果一定是错的"的坑
//     （比如 scanf 忘了写 &）。
//
// ─────────────────────────────────────────────────────
//  轮到你了
// ─────────────────────────────────────────────────────
//
//  这一课没有算法要写，只有三件事要做：
//
//    1. 确认 gcc 和 dotnet 都装好了
//       （界面首页的「环境自检」会显示；命令行敲 gcc --version 也行）
//
//    2. 让 gcc **报一次错**，并且读懂它
//       下面留了一个空的 main。先故意漏掉那个分号 → 检查 → 看报错 →
//       把分号补上 → 再检查。
//
//       为什么要你故意写错一次：**报错是编译器在跟你说话**。
//       第一次被它骂过的人，第二次才不慌。这一课就是让你先被骂一次，
//       而且是在"没人看、不扣分"的时候被骂。
//
//    3. 让它打印出下面这三行，就算跑通了
//
//          工具链就绪
//          我准备好学 C 了
//          第一步：编译，第二步：运行
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//  这一课在界面上点「检查」就行。
//  命令行（如果你在源码目录里）：
//
//      dotnet run --project csharp/c-lessons -- 检查 00
//
//  卡住了？把报错连同你的代码发出去问，比空想快得多。

// ── 在下面写你的代码 ──

#include <stdio.h>

int main(void) {
    printf("工具链就绪\n");
    /* ⚠ 先故意把下面这一行的分号删掉，检查一次，看看 gcc 怎么说。
       读完报错再把分号补回来。 */
    printf("我准备好学 C 了\n");
    printf("第一步：编译，第二步：运行\n");
    return 0;
}
""";

    public static string 参考答案 => """
#include <stdio.h>

int main(void) {
    /* ① 先漏掉分号编译一次，读懂第一条 error，再补回来 ——
       这一课练的就是"读报错"这件事。

       报错大概长这样：
           main.c:6:1: error: expected ';' before 'printf'
       意思是"第 6 行这里，我以为该有个分号"。 */
    printf("工具链就绪\n");
    printf("我准备好学 C 了\n");
    printf("第一步：编译，第二步：运行\n");
    return 0;
}
""";


    [检查说明("工具链就绪（gcc 与 dotnet 都在）")]
    public static CheckResult 检查_1a(string 代码)
    {
        return CppLesson.工具链就绪(代码);
    }

    [检查说明("程序能编译、能跑起来（说明 gcc 和 dotnet 都就绪）")]
    public static CheckResult 检查_1b(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "工具链就绪",
            "我准备好学 C 了",
            "第一步：编译，第二步：运行",
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

    [检查说明("写的是 C 的标准骨架（#include + int main）")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("int main"))
            return CheckResult.不过("没找到「int main」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("分号补回来了（每个 printf 后面都有 ;）")]
    public static CheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains(";"))
            return CheckResult.不过("没找到「;」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("没把 return 0 忘了（main 要告诉系统\"正常结束\"）")]
    public static CheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("return 0"))
            return CheckResult.不过("没找到「return 0」—— 这一课要求用到它。");
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
