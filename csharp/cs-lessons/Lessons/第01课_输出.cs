using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("01", "第 01 课：输出与变量 —— 让程序开口说话",
    "写一个方法 `static void 打招呼()`：\n" +
    "  1. 打印一行：你好，C#\n" +
    "  2. 定义 int 变量 甲 = 6、乙 = 7\n" +
    "  3. 打印它们的和（用变量相加，不要写死 13）")]
public static class 第01课
{
    public static string 模板 => """
// 第 01 课：输出与变量
//
// 要做：
//   1. 打印一行：你好，C#
//   2. 定义 int 变量 甲 = 6、int 变量 乙 = 7
//   3. 打印它们的和（用变量相加，不要写死 13）
//
// 记住三件事：
//   · 每条语句末尾要有分号 ;
//   · 打印用 Console.WriteLine(...)
//   · 字符串用双引号 "..."，单个字符才用单引号 '...'
//
// ⚠ C# 是**编译型**语言 —— 但 .NET SDK 自带编译器，
//   所以不用像 C++ / Java 那样另外装 g++ / JDK。
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 01

// ── 在下面写你的方法 ──
// 这一课**不用**写 Main（本课只考你写方法，Main 由课程提供）。

""";

    public static string 参考答案 => """
static void 打招呼()
{
    Console.WriteLine("你好，C#");

    int 甲 = 6;
    int 乙 = 7;
    Console.WriteLine(甲 + 乙);
}
""";

    [检查说明("编译通过，且方法签名对")]
    public static CheckResult 检查_能编译(string 代码)
    {
        var r = CppCheck.写了函数(代码, "打招呼", "打招呼()");
        if (!r.Ok) return r;
        return CheckResult.过();
    }

    [检查说明("打印了「你好，C#」这一行")]
    public static CheckResult 检查_第一行文字(string 代码)
    {
        // ⚠ 统一入口：**调用点**（可以是任意 C# 语句）。
        //   骨架会把它放进 `入口.Main` —— 不用自己写骨架，
        //   也就不会和库追加的 `入口` 类撞车。
        var run = 调用(代码, "打招呼();");
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        var 行 = run.行;
        if (行.Count < 1) return CheckResult.不过("一行都没打印。");
        if (!行[0].Contains("你好，C#"))
            return CheckResult.不过(
                $"第一行应该是「你好，C#」，实际是「{行[0]}」。\n" +
                "      注意：逗号是中文逗号，C# 是大写 S（C# 里 # 就是普通字符）。");
        return CheckResult.过($"第一行 = {行[0]}");
    }

    [检查说明("第二行是 13（= 6 + 7）")]
    public static CheckResult 检查_和(string 代码)
    {
        // ⚠ 统一入口：**调用点**（可以是任意 C# 语句）。
        //   骨架会把它放进 `入口.Main` —— 不用自己写骨架，
        //   也就不会和库追加的 `入口` 类撞车。
        var run = 调用(代码, "打招呼();");
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
        var 行 = run.行;
        if (行.Count < 2)
            return CheckResult.不过($"应该有 2 行输出，实际 {行.Count} 行 —— 是不是忘了打印和？");
        if (行[1].Trim() != "13")
            return CheckResult.不过(
                $"第二行应该是 13，实际是「{行[1].Trim()}」。\n" +
                "      检查：变量是 6 和 7 吗？真的用 + 相加了吗？");
        return CheckResult.过();
    }

    [检查说明("真的用了变量相加（不是写死 13）")]
    public static CheckResult 检查_不是硬写(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains("13"))
            return CheckResult.不过(
                "代码里出现了字面量 13 —— 那是把答案写死了。\n" +
                "      要求用两个变量相加：Console.WriteLine(甲 + 乙);");
        if (!去注释.Contains("+"))
            return CheckResult.不过("没看到加法运算。要用 甲 + 乙，不是直接打印一个数。");
        return CheckResult.过();
    }

    [检查说明("用了 Console.WriteLine（不是 Write）")]
    public static CheckResult 检查_用对了打印(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("Console"))
            return CheckResult.不过("没看到 Console —— C# 的打印是 Console.WriteLine(...)");
        // ⚠ Write 不换行 ⇒ 两行输出会挤成一行。新手常犯。
        if (去注释.Contains("Console.Write(") && !去注释.Contains("Console.WriteLine("))
            return CheckResult.不过(
                "你用的是 `Console.Write`（不换行）—— 两行输出会挤成一行。\n" +
                "      要换行请用 `Console.WriteLine`。");
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


    /// <summary>
    /// 调用学员的某个方法并返回它打印的所有行。
    ///
    /// ⚠ 为什么需要它：本课的参考答案是**一个方法**（不是完整程序），
    ///   所以不能用 `直接运行`（那是给"学员写了完整程序"用的）——
    ///   它需要一个 `Main`，而方法答案里没有 ⇒ 什么都不打印。
    ///   ⇒ 用**骨架 + 调用点**：由课程提供入口，只考方法本身。
    /// </summary>
    private static CppRun 调用(string 代码, string 调用点)
    {
        return CppCheck.编译并运行(代码, null, 限定(代码, 调用点));
    }

    /// <summary>
    /// 把调用点里的**裸方法名**限定成 `学生代码.xxx`。
    ///
    /// ⚠ 为什么必须这么做（C# 课最绕的一处，改了很多遍才通）：
    ///
    ///   · 库把学员代码放进 `学生代码` 类，而它自己生成的 `入口` 是
    ///     **平级类** ⇒ `入口.Main` 里直接写 `打招呼()` 编译不过：
    ///         error CS0103: 当前上下文中不存在名称 '打招呼'
    ///
    ///   · 而调用点常常是**语句序列**（好几条、甚至 foreach），
    ///     不能给整段机械加前缀 —— 会生成
    ///         () => { 学生代码.foreach (var n in ...) ... }
    ///     ⇒ error CS1001: Identifier expected
    ///
    ///   · 反射也帮不上：临时工程**不引用我们的库**，
    ///     生成出来的代码里根本没有 `CppCheck` 可用。
    ///
    ///   ⇒ 就在**课程侧**做：把调用点里出现的、**学员自己定义的方法名**
    ///     逐个替换成 `学生代码.<名字>`。
    ///     语义等价（静态方法加类名限定不改变行为），
    ///     而且对"语句序列"和"单个调用"一视同仁 —— 同一套正则。
    ///
    ///   方法名是从**学员代码**里扫出来的，所以不会误伤
    ///   `Console.WriteLine` 这类库方法（它们的名字不在学员的定义里）。
    /// </summary>
    private static string 限定(string 代码, string 调用点)
    {
        // ⚠⚠ **类**也要公开（第 07/08 课踩到）：
        //   学员写的 `class 计数器` 默认是 `internal`，而库生成的 `入口`
        //   与它平级却在**同一个程序集**里 —— 严格说能访问，但我们的
        //   调用点在 `入口` 类内，写 `学生代码.计数器` 时要求
        //   **嵌套可见性**成立，而 `学生代码` 是 public 静态类、
        //   里面的 `计数器` 若是 internal 就不可见 ⇒
        //       error CS0122: '计数器' 不可访问，因为它具有一定的保护级别
        //   ⇒ 和"方法补 public"同理，把**顶层类**也补上 public。
        //     方法补 public 在 `补公开` 里做，类在这里补。
        var 类名们 = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(
                     代码, @"(?m)^\s*(?:public\s+|internal\s+|private\s+)?(?:static\s+)?(?:sealed\s+)?(?:abstract\s+)?(?:class|interface|enum|struct)\s+(\w+)"))
        {
            string 全 = m.Value;
            if (全.Contains("public") || 全.Contains("internal")
                || 全.Contains("private") || 全.Contains("protected")) continue;
            类名们.Add(m.Groups[1].Value);
        }
        // ⚠ 类名/接口名的限定要小心**两种位置**：
        //   · 类型引用位置（`new 圆(...)`、`List<图形>`）⇒ 要加 `学生代码.`
        //   · **基类型位置**（`class 圆 : 图形`）⇒ **不能**加
        //     （`class 圆 : 学生代码.图形` 非法）
        //   而我们替换的是**调用点**（不是学员的类声明），里面不会出现
        //   `class 圆 : 图形` —— 所以可以放心整体加前缀。
        //   唯一要防的是**重复加**（已经写全名的地方）。
        foreach (var 名 in 类名们)
        {
            string 前缀 = "学生代码." + 名;
            // 先临时把已有的全名换成占位符，避免二次加前缀
            调用点 = 调用点.Replace(前缀, "\u0001" + 名);
            调用点 = System.Text.RegularExpressions.Regex.Replace(
                调用点, @"(?<![\w.])" + System.Text.RegularExpressions.Regex.Escape(名) + @"\b",
                前缀);
            调用点 = 调用点.Replace("\u0001" + 名, 前缀);
        }

        var 名字们 = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(
                     代码, @"\bstatic\s+[\w<>\[\]\?,\s]+?\s+(\w+)\s*\("))
            名字们.Add(m.Groups[1].Value);
        string 出 = 调用点;
        foreach (var 名 in 名字们)
            出 = System.Text.RegularExpressions.Regex.Replace(
                出, @"(?<![\w.])" + System.Text.RegularExpressions.Regex.Escape(名) + @"\s*\(",
                "学生代码." + 名 + "(");
        return 出;
    }
}
