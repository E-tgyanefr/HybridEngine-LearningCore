// 第 01 课：输出与变量 —— 让程序开口说话
//
// ══ 这一课要教什么 ══
// 让 C++ 程序把东西打到屏幕上。这看起来简单，但它是**所有后续课程的反馈通道** ——
// 没有输出，学生就不知道自己写的对不对，也就没法自己排查问题。
//
// 所以第一课不讲语法细节，只讲一件事：**怎么让程序说话**。
//
// ══ 检查设计说明（给维护者）══
// 这一课的"核心检查点"是第 3 条：**变量必须真的在参与计算**。
// 学生完全可以把答案硬写死（std::cout << 42;）也能输出对 ——
// 那样就学不到"变量"是什么。所以有一条检查专门验证这一点。

using System.Linq;
using LearnCode;
using LearnCode.CppLessons;

[课程("01", "第 01 课：输出 —— 让程序开口说话",
    "1. 用 std::cout 打印一行文字：你好，C++\n" +
    "2. 定义一个 int 变量 甲 = 6，一个 int 变量 乙 = 7\n" +
    "3. 打印它们的和。要求真的用变量相加，不要把 13 硬写死。")]
public static class 第01课
{
    public static string 模板 => """
// 第 01 课：输出 —— 让程序开口说话
//
// 要做：
//   1. 打印一行文字：你好，C++
//   2. 定义 int 变量 甲 = 6，int 变量 乙 = 7
//   3. 打印它们的和（用变量相加，不要写死 13）
//
// 记住三件事：
//   · 每条语句末尾要有分号 ;
//   · 打印用 std::cout << 内容;
//   · 换行用 std::endl 或 "\n"
//
// 写完后运行：dotnet run --project csharp/cpp-lessons 检查 01

// ── 在下面写你的代码 ──
// 提示：本课的作业是一个完整的程序，需要自己写 main：
//
//   #include <iostream>
//
//   int main() {
//       // 在这里写
//       return 0;
//   }

""";

    public static string 参考答案 => """
#include <iostream>

int main() {
    std::cout << "你好，C++" << std::endl;

    int 甲 = 6;
    int 乙 = 7;
    std::cout << 甲 + 乙 << std::endl;

    return 0;
}
""";

    // ══════════════════ 检查 ══════════════════

    [检查说明("程序能不能编译、能不能跑起来")]
    public static CheckResult 检查_程序能运行(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        if (run.Raw.Error != null)
            return CheckResult.不过("程序没能正常运行：\n      " + run.Raw.Error);
        if (run.行.Count == 0)
            return CheckResult.不过(
                "程序跑起来了，但**什么也没打印**。\n" +
                "      要做到这一点需要两样东西：\n" +
                "        ① #include <iostream>       ← 让程序认识 std::cout\n" +
                "        ② std::cout << \"内容\";      ← 真的打印点什么\n" +
                "      另外确认你的代码里有 int main() { ... } —— 那是程序的入口。");
        return CheckResult.过($"程序正常运行，打印了 {run.行.Count} 行");
    }

    [检查说明("有没有按题目要求定义变量（而不是直接把答案写出来）")]
    public static CheckResult 检查_定义了变量(string 代码)
    {
        // 这条检查看的是**声明语法**：源码里有没有 int 甲 = 数字;
        //
        // 为什么不用探针去"触摸"变量：学生的 甲/乙 是 main 里的局部变量，
        // 函数外的探针看不见它们，必然编译失败 —— 我第一版就是这么错的，
        // 而且报出来的提示是「你没写 int」，把学生引到错误方向上。
        //
        // 之所以敢只看语法：**下一条检查（变量真的在用）已经严格验证过了** ——
        // 它把初值改掉重新编译，看输出会不会跟着变。有那条兜底，
        // 这条的职责就只是「提示你还没把变量写出来」。
        var 声明甲 = System.Text.RegularExpressions.Regex.IsMatch(
            代码, @"int\s+甲\s*=\s*-?\d+\s*;");
        var 声明乙 = System.Text.RegularExpressions.Regex.IsMatch(
            代码, @"int\s+乙\s*=\s*-?\d+\s*;");

        if (声明甲 && 声明乙)
            return CheckResult.过("甲 和 乙 都按 int 类型声明了");

        if (!声明甲 && !声明乙)
            return CheckResult.不过(
                "还没有声明「甲」和「乙」这两个变量。\n" +
                "      C++ 的变量必须先写**类型**才能用：\n" +
                "          int 甲 = 6;\n" +
                "          int 乙 = 7;\n" +
                "      ⚠ 这一点和 Python 不同 —— Python 写 甲 = 6 就行，\n" +
                "        而 C++ 必须写 int 甲 = 6;（告诉编译器它装的是整数）。\n" +
                "      另外注意分号 ; 和**英文**等号，中文标点编译器不认。");
        if (!声明甲)
            return CheckResult.不过("没有找到「甲」的声明。写法：int 甲 = 6;");
        return CheckResult.不过("没有找到「乙」的声明。写法：int 乙 = 7;");
    }

    [检查说明("变量是不是真的参与了计算（不是把答案写死）")]
    public static CheckResult 检查_变量真的在用(string 代码)
    {
        // ⚠ 这一条是本课的核心检查点。
        //
        // 学生完全可以写 std::cout << 13; 也能输出对 —— 但那学不到"变量"是什么。
        // 怎么分辨：**把声明里的初值改掉，重新编译，看输出会不会跟着变。**
        //   真用变量 → 输出跟着变
        //   写死答案 → 输出不变
        // 这个办法只看输出，不需要访问变量本身，所以变量在 main 里还是在全局都成立。
        string 改过 = 改写初值(代码, "甲", 100);
        改过 = 改写初值(改过, "乙", 200);

        if (改过 == 代码)
            return CheckResult.不过(
                "没有找到可以改写的变量声明。\n" +
                "      期望看到这样的写法：\n" +
                "          int 甲 = 6;\n" +
                "      确认变量名是「甲」和「乙」，而且初值写的是数字。");

        var run = CppCheck.直接运行(改过);
        if (!run.编译通过) return CppCheck.编译失败说明(run);

        string 输出 = run.全部输出;
        if (输出.Contains("300"))
            return CheckResult.过("甲 和 乙 确实参与了计算 —— 改成 100 和 200 之后，和变成了 300");

        return CheckResult.不过(
            "甲 和 乙 好像没有真的参与计算。\n" +
            "      我把它们的初值改成 甲 = 100、乙 = 200 再跑，\n" +
            "      期望看到 300（100 + 200），但得到的是：\n" +
            $"          {(输出.Trim().Length == 0 ? "（空）" : 输出.Trim().Replace("\n", "\n          "))}\n" +
            "      这通常意味着和是**写死的**，比如直接写了：\n" +
            "          std::cout << 13 << std::endl;\n" +
            "      改法：让变量真的参与进来 ——\n" +
            "          std::cout << 甲 + 乙 << std::endl;");
    }

    /// <summary>
    /// 把「int 变量名 = 数字」里的数字换成新值。
    ///
    /// 为什么要做这种文本改写：C++ 里没法从外部读到一个函数内部的局部变量，
    /// 而"变量有没有真的参与计算"恰恰是本课要考的东西。
    /// 改掉初值再重编译，用**输出的变化**来间接验证 —— 这是绕开作用域限制的办法。
    /// </summary>
    private static string 改写初值(string 代码, string 变量名, int 新值)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            代码,
            @"(int\s+" + System.Text.RegularExpressions.Regex.Escape(变量名) + @"\s*=\s*)-?\d+",
            "${1}" + 新值);
    }

    [检查说明("有没有打印那行文字")]
    public static CheckResult 检查_打印了文字(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        if (run.全部输出.Contains("你好，C++"))
            return CheckResult.过("打印了「你好，C++」");
        return CheckResult.不过(
            "没有看到「你好，C++」这行文字。\n" +
            $"      实际打印的是：\n          {run.全部输出.Replace("\n", "\n          ")}\n" +
            "      检查两点：\n" +
            "        · 文字要放在**双引号**里：\"你好，C++\"\n" +
            "        · 逗号要是**英文逗号** ，不是中文的 ，（这是新手最隐蔽的坑）");
    }

    [检查说明("和算得对不对")]
    public static CheckResult 检查_和正确(string 代码)
    {
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);

        // 找出输出里的数字行（那行文字不算）
        var 数字行 = run.行.Where(l => l.Trim().Length > 0
                                       && l.Trim().All(c => char.IsDigit(c) || c == '-')
                                       && !l.Contains("你好")).ToList();
        if (数字行.Count == 0)
            return CheckResult.不过(
                "没有看到打印出来的和。\n" +
                "      6 + 7 应该是 13，用 std::cout << 甲 + 乙 << std::endl; 打印出来。");
        if (数字行.Any(l => l.Trim() == "13"))
            return CheckResult.过("6 + 7 = 13，正确");
        return CheckResult.不过(
            $"和不对：期望 13，实际打印了 {string.Join("、", 数字行.Select(l => l.Trim()))}。\n" +
            "      检查一下变量的值（甲 = 6，乙 = 7）和相加的写法。");
    }
}
