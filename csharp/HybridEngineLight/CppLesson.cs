// HybridEngineLight.CppLesson —— 写 C++ 课程时的检查辅助。
//
// ══ 这个文件是「课程作者」的接口 ══
//
// 每一课是一个 .cs 文件，里面写若干检查。检查要验证的是**学生写的 C++ 代码**，
// 所以每一条检查本质上都是：
//
//     把学生的 C++ 代码编译运行 → 看输出对不对 → 不对就说清楚哪不对
//
// 这件事每次重写一遍既啰嗦又容易错，所以收在这里。课程作者只需要用这几个方法：
//
//     CppCheck.编译并运行(学生代码)            → CppRun
//     CppCheck.输出等于(学生代码, "42")        → CheckResult
//     CppCheck.输出逐行(学生代码, "1", "2")    → CheckResult
//     CppCheck.能编译(学生代码)                → CheckResult
//     CppCheck.写了函数(学生代码, "平方")       → CheckResult
//     CppCheck.运行后(学生代码, 检查函数)       → CheckResult
//
// ══ 为什么课程作者不该自己拼「检查框架」 ══
//
// 因为**检查写错的代价不对称**：
//   · 检查太松 → 学生抄一遍就过，那一课白上
//   · 检查太严 → 正确解法被判错，学生莫名卡住然后放弃
// 两种错都不会自己暴露。集中在这里，才有可能被统一测试覆盖。

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HybridEngineLight;

/// <summary>一次学生代码的运行结果（编译 + 执行）。</summary>
public sealed class CppRun
{
    public CppResult Raw = null!;
    /// <summary>学生代码本身有没有编译通过。</summary>
    public bool 编译通过 => !Raw.CompileFailed && Raw.Error == null;
    /// <summary>程序打印的所有行。</summary>
    public IReadOnlyList<string> 行 => Raw.Lines;
    /// <summary>合并的输出（用于查找关键字）。</summary>
    public string 全部输出 => string.Join("\n", Raw.Lines);
    /// <summary>编译输出（错误 + 警告）。</summary>
    public string 编译信息 => Raw.CompileOutput;
    /// <summary>编译失败或有正确性警告 —— 这两种情况都不该判通过。</summary>
    public bool 有问题 => Raw.CompileFailed || Raw.Error != null || Raw.HasCorrectnessWarning;
}

/// <summary>一条检查的结果：过没过 + 给学生的说明。</summary>
public readonly struct CheckResult
{
    public readonly bool Ok;
    public readonly string Detail;
    public CheckResult(bool ok, string detail) { Ok = ok; Detail = detail; }
    public static CheckResult 过(string detail = "") => new(true, detail);
    public static CheckResult 不过(string detail) => new(false, detail);
}

public static class CppCheck
{
    /// <summary>
    /// 学生代码的**默认程序骨架**。
    /// 里面有一个 /*学生代码*/ 占位符，会被替换成学生写的内容。
    ///
    /// 为什么需要：学生写的通常只是**一个函数**，而 C++ 必须有 main() 才能编译。
    /// 骨架由课程提供，学生就不用管 #include、不用管 main —— 少点干扰。
    /// 需要自定义输出时（比如打印多个结果），传自己的骨架。
    /// </summary>
    public const string 默认骨架 = """
#include <iostream>
#include <string>
#include <vector>
#include <cmath>
#include <algorithm>

/*学生代码*/

int main() {
    /*调用点*/
    return 0;
}
""";

    /// <summary>
    /// **C 语言**的骨架（`CppRunner.用C == true` 时用它）。
    ///
    /// ⚠ 必须与 C++ 那份**分开**，不能共用：
    ///   · C 没有 `<iostream>` / `std::cout` / `<vector>` / `<algorithm>`
    ///   · C 没有重载 —— `printf` 要自己写格式串
    ///   共用的话学生看到的是「我明明写的是 C，为什么报 `iostream: No such file`」。
    /// </summary>
    public const string 默认骨架C = """
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>

/*学生代码*/

int main(void) {
    /*调用点*/
    return 0;
}
""";

    /// <summary>把学生代码塞进骨架并编译运行。</summary>
    public static CppRun 编译并运行(string 学生代码, string? 骨架 = null, string 调用点 = "")
    {
        string 用骨架 = 骨架 ?? (CppRunner.用CSharp ? 默认骨架CSharp
                                : (CppRunner.用C ? 默认骨架C : 默认骨架));
        // ⚠ 占位符**按语言取不同的写法**：
        //   C/C++ 用 `/*学生代码*/`（块注释合法）
        //   C#    用 `// __学生代码__`（C# **不支持** /* */ 块注释！）
        //   我第一版对 C# 也用了 /* */ ⇒ 占位符没被替换，
        //   而 `/*` 被编译器当成预处理器指令：
        //       error CS1024: Preprocessor directive expected
        //   —— 课程自己的错，学员会以为是自己写错了。
        string 程序;
        if (CppRunner.用CSharp)
            程序 = 用骨架.Replace("// __学生代码__", 学生代码)
                        .Replace("// __调用点__", 调用点);
        else
            程序 = 用骨架.Replace("/*学生代码*/", 学生代码)
                        .Replace("/*调用点*/", 调用点);
        // ⚠ C# 的骨架**自带入口类**（`入口.Main`）⇒ 整段程序直接编译运行。
        //   C/C++ 则走 gcc（骨架里也有 main）。
        // 两种 C# 形态必须分开，否则会撞出「入口 重复定义」：
        //   · 骨架路径（本方法）：骨架自带 `入口.Main`（内容就是调用点）
        //     ⇒ 整段程序直接跑，**不能让 DumpOutput 再加一个 入口**
        //   · 完整程序路径（`直接运行`）：学员自己写了 Main ⇒ 也直接跑
        return 跑(程序, 调用点: CppRunner.用CSharp ? 调用点 : "");
    }

    /// <summary>
    /// **C#** 的骨架。
    ///
    /// ⚠ C# 与 C/C++ 的骨架完全不同：C# 没有"全局函数"，
    ///   所有方法必须在类里 ⇒ 学生的代码要塞进一个 `学生代码` 类。
    ///   这也是"同一个 CppCheck 能同时服务 C# 课"的关键：
    ///   **只是把学生代码放进不同的壳里**，检查逻辑一行都不用改。
    /// </summary>
    public const string 默认骨架CSharp = """
using System;
using System.Collections.Generic;
using System.Linq;

public static class 学生代码
{
// __学生代码__
}

public static class 入口
{
    public static void Main()
    {
        // __调用点__
    }
}
""";

    /// <summary>
    /// 把**一整段程序**跑起来（不经骨架）。
    /// 按语言选实现：C# 走 `CSharpRunner`（dotnet 自己就是编译器，
    /// 不需要另外装东西），C/C++ 走 gcc。
    /// </summary>
    private static CppRun 跑(string 程序, string 调用点 = "")
    {
        if (!CppRunner.用CSharp)
            return new CppRun { Raw = CppRunner.BuildAndRun(程序) };

        // ⚠⚠ C# 侧有两个不同的入口，**必须选对**：
        //
        //   · `DumpOutput(body, entry)` —— body 是**类一级的成员**
        //     （方法/类定义），它自己再套一个 `入口.Main` 去调 `entry()`。
        //     ⇒ 我们从骨架里取出 `学生代码` 类的**成员**，入口名就是 `Main`
        //       （骨架的 `入口.Main` 里写着我们的调用点）。
        //
        //   · `Run(body, cases)` —— body 也放进 `学生代码` 类里，
        //     但输出靠"测试用例"，**不跑 Main** ⇒ 收不到程序自己打印的东西。
        //
        //   我第一版对"独立程序"用了 `Run` —— 于是学员的程序**根本没执行**，
        //   检查全报"一行都没打印"。**能编译 ≠ 会执行。**
        var raw = new CppResult();
        CSharpResult c;      // ⚠ 它在 HybridEngineLight 命名空间下，不是 CSharpRunner 的嵌套类型
        // ⚠⚠ 这里改了五遍才对，每一遍都是"库的语义没看清"。全记下来：
        //
        //   `DumpOutput(body, entry)` 的实际行为：
        //       usings… + 我们的 body + **它自己的 `入口` 类**
        //       而那个 `入口.Main` 里写着 `try { <entry>(); }`
        //
        //   ① body 传"整段程序" ⇒ 和它的 `入口` 撞车
        //        → CS0101: 命名空间已包含「入口」的定义
        //   ② `entry` 传空串 ⇒ 生成 `();`
        //        → CS1525: Invalid expression term ')'
        //   ③ "从 `class 入口` 删到结尾" ⇒ 带走上一个类的收尾 `}`
        //        → CS1004: Duplicate modifier
        //   ④ 保留原来的 usings ⇒ `System` 重复(CS0105) 且
        //        `using HybridEngineLight;` 在临时工程里不存在(CS0246)
        //   ⑤ `entry` 传"空 lambda" ⇒ **程序什么都不做**（行数 0）
        //        → 检查全报"一行都没打印" —— 最隐蔽的一个：
        //          **能编译、能运行，就是没干正事。**
        //
        //   ⇒ 正确：body = `学生代码` 类的成员（不含 usings）
        //            entry = **调用点本身**（C# 写 `() => { <调用点> }`）
        // ⚠ 调用点必须**限定类名**（`学生代码.xxx()`）：
        //   库生成的 `入口` 类与 `学生代码` 是**两个平级类**，
        //   不像 C++ 那样在同一个作用域里。
        //   直接写 `打招呼()` ⇒ CS0103: 当前上下文中不存在名称。
        // ⚠⚠ `entry` 会被库**当成一个方法名**用（它自己拼 `<entry>()`）。
        //   而我们的 `调用点` 形态是 `打招呼()` 或 `打招呼(95)`
        //   —— **末尾那个 `()` 要留**（那是库会补的），但**参数要带上**：
        //       entry = "学生代码.打招呼"        ⇒ 库拼成 学生代码.打招呼()   ✓
        //       entry = "学生代码.打招呼(95)"    ⇒ 库拼成 学生代码.打招呼(95)() ✗
        //   我第一版把整个 `调用点` 贴上 ⇒ 生成 `学生代码.打招呼();();`
        //       error CS0149: Method name expected
        //   ⇒ 正确做法：取到"方法名 + 参数表"，**去掉库即将补的那个空 `()`**。
        //
        //   做法：若调用点形如 `xxx()`（空参数），去掉尾部 `()`留给库补；
        //        若带参数，则用 lambda（库补的 `()` 会作用在 lambda 上，
        //        立即执行 —— 这正是我们要的）。
        string entry;
        if (调用点.Length == 0)
            entry = "() => { }";
        else if (调用点.EndsWith("()", StringComparison.Ordinal))
            entry = "学生代码." + 调用点.Substring(0, 调用点.Length - 2);
        else
            entry = "() => { 学生代码." + 调用点 + "; }";
        c = CSharpRunner.DumpOutput(骨架转成可跑程序(程序), entry);
        if (c.Error != null)
        {
            raw.Error = c.Error;
            raw.CompileFailed = true;
            raw.CompileOutput = c.Raw;
        }
        foreach (var l in c.RawLines) raw.Lines.Add(l);
        raw.RunOutput = c.Raw;
        return new CppRun { Raw = raw };
    }

    /// <summary>
    /// 把**骨架程序**改写成 `DumpOutput` 能接受的形式。
    ///
    /// ⚠⚠ 这一处我改了四遍才对，每次都是"没看清库的语义"。记在这里：
    ///
    ///   `DumpOutput(body, entry)` 做的事是：
    ///       程序 = usings… + 我们的 body + **自己的 `入口` 类**
    ///       而那个 `入口.Main` 里写着 `try { <entry>(); }`
    ///
    ///   ⇒ 三个坑：
    ///     ① 我们的程序里**已经有 `入口` 类** ⇒ 两个同名类 ⇒
    ///          error CS0101: 命名空间已包含「入口」的定义
    ///     ② `entry` 传空串 ⇒ 生成 `();` ⇒
    ///          error CS1525: Invalid expression term ')'
    ///     ③ 直接"从 `class 入口` 删到结尾" ⇒ 把**上一个类的收尾 `}`** 也带走了 ⇒
    ///          error CS1004: Duplicate modifier
    ///
    ///   ⇒ 正确做法：**把 `学生代码` 类的花括号内容取出来**当 body
    ///     （正好就是"学员的方法们"），加上 usings，再交给 DumpOutput；
    ///     入口用一个合法但什么都不做的表达式（立即执行的空 lambda）。
    /// </summary>
    private static string 骨架转成可跑程序(string 程序)
    {
        int 类 = 程序.IndexOf("class 学生代码", StringComparison.Ordinal);
        if (类 < 0)
        {
            // 学员写的是**完整程序**（没有 `学生代码` 类）。
            // ⚠ 不能原样返回：库会再追加一个 `入口` 类，与学员的撞车。
            //   ⇒ 把学员的 **Main 方法体**搬进 `学生代码` 类，
            //     其余（他的 `入口` 类）丢掉 —— 入口由库提供。
            return "public static class 学生代码\n{\n" + 搬Main(程序) + "\n}\n";
        }
        int 起 = 程序.IndexOf('{', 类);
        if (起 < 0) return 程序;
        int 深 = 0, 止 = -1;
        for (int i = 起; i < 程序.Length; i++)
        {
            if (程序[i] == '{') 深++;
            else if (程序[i] == '}')
            {
                深--;
                if (深 == 0) { 止 = i; break; }
            }
        }
        if (止 < 0) return 程序;
        string 成员 = 补公开(程序.Substring(起 + 1, 止 - 起 - 1));
        // ⚠ **不要保留原来的 usings**：
        //   ① 库（DumpOutput）自己会加 System / Collections.Generic / Linq / Text
        //      ⇒ 再留一份会 warning CS0105（重复 using）
        //   ② 骨架里的 `using HybridEngineLight;` 在**临时工程**里根本不存在
        //      （那个工程不引用我们的库）⇒ error CS0246
        //   ⇒ 只交"类里的成员"，usings 让库去管。
        return "public static class 学生代码\n{\n" + 成员 + "\n}\n";
    }

    /// <summary>
    /// 把**完整程序**里学员的 `Main` 方法体搬出来，改成 `学生代码` 类的一个方法。
    ///
    /// ⚠ 为什么不能原样跑：库（DumpOutput）**总会追加自己的 `入口` 类**，
    ///   而学员的完整程序里也有 `入口` ⇒ 两个同名类 ⇒ CS0101。
    ///   ⇒ 只留他的 `Main` 体，入口统一由库提供。
    /// </summary>
    private static string 搬Main(string 程序)
    {
        int m = 程序.IndexOf("static void Main", StringComparison.Ordinal);
        if (m < 0) m = 程序.IndexOf("static int Main", StringComparison.Ordinal);
        if (m < 0) return "public static void Main() { }";
        int 起 = 程序.IndexOf('{', m);
        if (起 < 0) return "public static void Main() { }";
        int 深 = 0, 止 = -1;
        for (int i = 起; i < 程序.Length; i++)
        {
            if (程序[i] == '{') 深++;
            else if (程序[i] == '}')
            {
                深--;
                if (深 == 0) { 止 = i; break; }
            }
        }
        if (止 < 0) return "public static void Main() { }";
        // 学员的 Main 可能是 `static int Main()` ⇒ 返回类型统一换成 void，
        // 免得他的 `return 0;` 在我们生成的 void 方法里编译不过。
        string 体 = 程序.Substring(起 + 1, 止 - 起 - 1);
        体 = System.Text.RegularExpressions.Regex.Replace(体, @"\breturn\s+\d+\s*;", "return;");
        return "public static void Main()\n{" + 体 + "\n}";
    }

    /// <summary>
    /// 给学员写的方法补上 `public`。
    ///
    /// ⚠ 为什么必须补：库生成的 `入口` 类与 `学生代码` 是**两个平级类**，
    ///   而 C# 的默认访问级别是 `private` ⇒ 入口类调不到学员的方法：
    ///       error CS0122: '学生代码.打招呼()' is inaccessible
    ///       due to its protection level
    ///
    ///   与其让每个学员都记住"这里要写 public"（那是**框架的限制**，
    ///   不是 C# 语言的重点），不如由课程替他补上。
    ///   学员想自己写 public 也不会重复（已经有的不再加）。
    /// </summary>
    private static string 补公开(string 成员)
    {
        var 出 = new System.Text.StringBuilder();
        foreach (var 原行 in 成员.Split('\n'))
        {
            string 行 = 原行;
            string 去空格 = 行.TrimStart();
            // 只处理"看起来是方法声明"的行：含括号、有 static、还没写访问修饰符
            if (去空格.StartsWith("static ", StringComparison.Ordinal) &&
                去空格.Contains("(") &&
                !去空格.StartsWith("public ", StringComparison.Ordinal) &&
                !去空格.StartsWith("private ", StringComparison.Ordinal) &&
                !去空格.StartsWith("internal ", StringComparison.Ordinal) &&
                !去空格.StartsWith("protected ", StringComparison.Ordinal))
            {
                int 缩进 = 行.Length - 去空格.Length;
                行 = 行.Substring(0, 缩进) + "public " + 去空格;
            }
            出.AppendLine(行);
        }
        return 出.ToString();
    }

    /// <summary>
    /// 从**完整程序**里取出 `学生代码` 类的成员
    /// <summary>
    /// 从**完整程序**里取出 `学生代码` 类的成员（不含 usings 与类声明）。
    ///
    /// ⚠ 定位必须从 `class 学生代码` 之后的第一个 '{' 起、再做强配对的 '}'：
    ///   用"第一个 '{'" 会取到**那个类**的开括号（因为 usings 后紧跟它），
    ///   而用"最后一个 '}'"会取到 `入口` 类的收尾 ⇒ 取出的片段里
    ///   带着 `入口` 的定义 ⇒ `CS0101: 已包含 '入口' 的定义`。
    /// </summary>
    private static string 取出成员(string 程序)
    {
        int 类 = 程序.IndexOf("class 学生代码", StringComparison.Ordinal);
        if (类 < 0) return 程序;
        int 起 = 程序.IndexOf('{', 类);
        if (起 < 0) return 程序;
        int 深 = 0;
        for (int i = 起; i < 程序.Length; i++)
        {
            if (程序[i] == '{') 深++;
            else if (程序[i] == '}')
            {
                深--;
                if (深 == 0) return 程序.Substring(起 + 1, i - 起 - 1);
            }
        }
        return 程序.Substring(起 + 1);
    }

    // ══════════════════ 常用检查 ══════════════════

    /// <summary>
    /// 直接编译运行学生代码，**不套任何骨架**。
    ///
    /// 什么时候用它：学生写的就是**完整程序**（自己带 main）的那种课 ——
    /// 比如第一课"让程序开口说话"，学生必须自己写 #include 和 main 才算学会。
    ///
    /// ⚠ 用错会出很迷惑的错误：学生写了 main，而骨架里也有 main 时，
    ///   GCC 报的是 "redefinition of 'int main()'" —— 学生会以为是自己写错了。
    ///   所以「这一课学生写完整程序、还是只写一个函数」必须由课程作者明确选。
    /// </summary>
    public static CppRun 直接运行(string 学生代码)
    {
        // 学员给的是**完整程序**（含自己的 Main）⇒ 不需要调用点，
        // 由我们生成的入口直接跑他的 Main。
        return 跑(学生代码, 调用点: "Main()");
    }

    /// <summary>
    /// 一组测试用例：一段调用代码 + 它应该打印出的结果。
    ///
    /// 为什么需要：只拼一个调用点的检查测不了**带参数的函数** ——
    /// 而"同一个函数对不同输入给出不同结果"恰恰是函数这一课要考的东西。
    /// 一个用例写死输出、蒙对就过；一组用例蒙不过去。
    /// </summary>
    public readonly record struct 用例(string 说明, string 调用, string 期望);

    /// <summary>一次编译，跑完所有用例，逐条比对。</summary>
    public static CheckResult 逐个用例(string 学生代码, params 用例[] 用例们)
    {
        if (用例们.Length == 0) return CheckResult.不过("这道题没有测试用例（课程的问题）");

        // 一次编译，把所有用例的调用都塞进 main —— 编译一遍能省一两秒
        var 调用点 = new System.Text.StringBuilder();
        for (int i = 0; i < 用例们.Length; i++)
            // ⚠ 打印语句**随语言变**：C 没有 `std::cout`，只有 `printf`。
            //   硬编码 `std::cout` 的话，C 课会报"`cout` 未声明"，
            //   而那是**课程自己的错**，学生却会以为是自己写错了。
            if (CppRunner.用C)
                调用点.Append("    printf(\"%d\\n\", ").Append(用例们[i].调用).Append(");\n");
            else
                调用点.Append("    std::cout << ").Append(用例们[i].调用).Append(" << \"\\n\";\n");

        var run = 编译并运行(学生代码, null, 调用点.ToString());
        if (!run.编译通过) return 编译失败说明(run);

        var 实际 = run.行;
        if (实际.Count != 用例们.Length)
            return CheckResult.不过(
                $"程序应该打印 {用例们.Length} 行（每个用例一行），但打印了 {实际.Count} 行。\n" +
                "      检查一下你的函数是不是对每个输入都打印了一次结果。");

        var 错的 = new List<string>();
        for (int i = 0; i < 用例们.Length; i++)
        {
            string 得到 = 实际[i].Trim();
            string 期望 = 用例们[i].期望.Trim();
            if (得到 != 期望)
                错的.Add($"      {用例们[i].说明}：得到 {显示(得到)}，应该是 {显示(期望)}");
        }
        if (错的.Count == 0)
            return CheckResult.过($"全部 {用例们.Length} 个用例都正确");

        return CheckResult.不过(
            $"通过 {用例们.Length - 错的.Count}/{用例们.Length}：\n" +
            string.Join("\n", 错的) + "\n" +
            "      对每个用例都想一遍：你的函数算出来的是什么？");
    }

    /// <summary>要求输出正好是这些行（逐行比较，忽略首尾空白）。</summary>
    public static CheckResult 输出逐行(string 学生代码, params string[] 期望行)
    {
        var run = 编译并运行(学生代码);
        if (!run.编译通过) return 编译失败说明(run);
        var 实际 = run.行;
        if (实际.Count == 期望行.Length)
        {
            bool 全同 = true;
            for (int i = 0; i < 实际.Count; i++)
                if (实际[i].Trim() != 期望行[i].Trim()) { 全同 = false; break; }
            if (全同) return CheckResult.过($"输出正确：{string.Join(" / ", 实际)}");
        }
        return CheckResult.不过(输出不符说明(期望行, 实际));
    }

    /// <summary>要求输出里包含某个片段（不关心别的输出）。</summary>
    public static CheckResult 输出包含(string 学生代码, string 片段)
    {
        var run = 编译并运行(学生代码);
        if (!run.编译通过) return 编译失败说明(run);
        if (run.全部输出.Contains(片段))
            return CheckResult.过($"输出里有「{片段}」");
        return CheckResult.不过(
            $"输出里应该有「{片段}」，但实际输出是：\n" +
            $"      {显示(run.全部输出)}\n" +
            $"      检查一下打印的内容和格式（空格、标点都算）。");
    }

    /// <summary>只要求能编译通过（不检查运行结果）。</summary>
    public static CheckResult 能编译(string 学生代码)
    {
        var run = 编译并运行(学生代码);
        if (run.Raw.CompileFailed) return 编译失败说明(run);
        if (run.Raw.HasCorrectnessWarning)
            return CheckResult.不过(
                "能编译，但编译器发现了隐患：\n" +
                ExplainCpp.ExplainWarning(run.编译信息));
        return CheckResult.过("编译通过，没有警告");
    }

    /// <summary>
    /// 要求源码里定义了指定的函数。
    ///
    /// 怎么验：写一个小程序去**调用**它。调用不了（编译不过）就说明没定义。
    /// 比用正则匹配源码可靠得多 —— 正则会被注释和字符串骗过去。
    /// </summary>
    public static CheckResult 写了函数(string 学生代码, string 函数名, string 调用表达式 = "")
    {
        string 调用 = 调用表达式.Length > 0 ? 调用表达式 : 函数名 + "()";
        // ⚠⚠ 骨架与打印语句**都要随语言变**。
        //   我第一版只改了 `逐个用例`，漏了这一处 —— 于是 C 课的签名检查生成出：
        //       std::cout << printf("%s\n", 判等级(95)); << "\n";
        //   而报错信息里出现的是「CC1PLUS」与 `<<` ——
        //   **课程自己的 bug，学生却会以为是自己写错了**（这正是最坏的一类）。
        //   教训：凡是"拼代码给学生跑"的地方，都必须问一遍"这行在 C 里成立吗"。
        // ⚠⚠ 骨架与调用点**都要按语言选**（三种语言三套）。
        //   我改这里时只处理了 C 与 C++，**漏了 C#** ⇒ C# 课选了 C++ 骨架，
        //   而 `编译并运行` 在 C# 模式下按 `// __调用点__` 替换
        //   ⇒ C++ 骨架里的 `/*调用点*/` 没被替换 ⇒ 残留的 `/*`
        //   ⇒ error CS1024: Preprocessor directive expected
        //   —— 又是"课程自己的错，学员以为是自己写错"。
        //
        //   教训（第三次了）：**凡是"按语言分派"的地方，加新语言时必须逐处过一遍**。
        //   与其靠记性，不如让每处都显式写出三种情况 —— 漏了编译器会提醒。
        string 骨架;
        if (CppRunner.用CSharp)
            骨架 = 默认骨架CSharp.Replace("// __调用点__", 调用 + ";");
        else if (CppRunner.用C)
            骨架 = 默认骨架C.Replace("/*调用点*/", $"printf(\"%d\\n\", (int)({调用}));");
        else
            骨架 = 默认骨架.Replace("/*调用点*/", $"std::cout << {调用} << \"\\n\";");
        var run = 编译并运行(学生代码, 骨架);
        if (!run.编译通过)
        {
            // 区分"没写这个函数"和"函数写了但里面有别的编译错误"
            if (run.编译信息.Contains(函数名) &&
                (run.编译信息.Contains("was not declared") || run.编译信息.Contains("not declared")))
                return CheckResult.不过(
                    $"没有找到「{函数名}」这个函数。\n" +
                    $"      写法：\n" +
                    $"          int {函数名}(...) {{\n" +
                    $"              ...\n" +
                    $"          }}\n" +
                    $"      注意：函数的**返回类型必须写**，不能像 Python 那样只写 def。");
            return 编译失败说明(run);
        }
        return CheckResult.过($"「{函数名}」可以正常调用，输出 {显示(run.全部输出)}");
    }

    /// <summary>给需要看「运行结果对象」的复杂检查用。</summary>
    public static CheckResult 运行后(string 学生代码, Func<CppRun, CheckResult> 检查,
                                     string? 骨架 = null, string 调用点 = "")
    {
        var run = 编译并运行(学生代码, 骨架, 调用点);
        if (!run.编译通过) return 编译失败说明(run);
        return 检查(run);
    }

    // ══════════════════ 说明文字 ══════════════════

    /// <summary>编译失败时给学生的说明 —— 直接把翻译结果贴上去。</summary>
    public static CheckResult 编译失败说明(CppRun run)
    {
        if (run.Raw.CompileFailed)
        {
            // ⚠ 报错翻译**必须按语言选**：
            //   C++ 用 ExplainCpp，C# 用 Explain。
            //   我第一版写死了 ExplainCpp ⇒ C# 课报错里出现
            //   "C++ 区分大小写""C++ 的报错行号要往上多看一行" ——
            //   **课程自己的错，学员却会以为是自己写错了。**
            var 说明 = CppRunner.用CSharp
                ? Explain.For(new Exception(run.Raw.CompileOutput))
                : ExplainCpp.For(run.Raw.CompileOutput, run.Raw.CompileOutput, true);
            return CheckResult.不过(说明.TrimEnd());
        }
        return CheckResult.不过("程序没能正常运行：\n      " + (run.Raw.Error ?? "未知原因"));
    }

    public static string 输出不符说明(IReadOnlyList<string> 期望, IReadOnlyList<string> 实际)
    {
        var sb = new StringBuilder();
        sb.Append("输出不对。\n");
        sb.Append("      期望（").Append(期望.Count).Append(" 行）：\n");
        foreach (var l in 期望) sb.Append("          ").Append(l).Append('\n');
        sb.Append("      实际（").Append(实际.Count).Append(" 行）：\n");
        if (实际.Count == 0) sb.Append("          （没有任何输出）\n");
        foreach (var l in 实际) sb.Append("          ").Append(l).Append('\n');
        sb.Append("      逐行对照一下，注意**数量**和**顺序**也要一致。");
        return sb.ToString();
    }

    private static string 显示(string s) => s.Length == 0 ? "（空）" : s.Replace("\n", " ⏎ ");
}

/// <summary>
/// 课程文件里的一条检查：名字 + 检查函数。
/// 课程作者用 CppLesson.收集(this) 把本文件里的检查交给运行器。
/// </summary>
/// <summary>
/// 给一条检查写一句**给人看的**说明。
///
/// 为什么不直接用函数名：函数名是为代码服务的（「检查_变量真的在用」），
/// 而列给学生看的应该是完整的话（「变量是不是真的参与了计算」）。
/// 两者要求不同，所以分开写。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class 检查说明Attribute : Attribute
{
    public string 文字;
    public 检查说明Attribute(string 文字) { this.文字 = 文字; }
}

public sealed record CppCheckItem(string 名字, Func<string, CheckResult> 检查);

public static class CppLesson
{
    /// <summary>
    /// 从一个「课程定义对象」里收集检查。
    ///
    /// 约定：课程文件里定义一堆 **public static CheckResult 检查_xxx(string 学生代码)**，
    /// 这个方法会自动把它们装上（按定义顺序）。
    /// 前缀用中文「检查_」而不是 CHECK_ —— 学生一眼能看出那是检查，而不是要写的代码。
    /// </summary>
    public static List<CppCheckItem> 收集(Type 课程类型)
    {
        var list = new List<CppCheckItem>();
        foreach (var m in 课程类型.GetMethods(System.Reflection.BindingFlags.Public
                                              | System.Reflection.BindingFlags.Static
                                              | System.Reflection.BindingFlags.DeclaredOnly))
        {
            if (!m.Name.StartsWith("检查_", StringComparison.Ordinal)) continue;
            if (m.ReturnType != typeof(CheckResult)) continue;
            var ps = m.GetParameters();
            if (ps.Length != 1 || ps[0].ParameterType != typeof(string)) continue;

            // 优先用 [检查说明] 里那句给人看的话；没写就退回函数名
            var attr = (检查说明Attribute?)Attribute.GetCustomAttribute(m, typeof(检查说明Attribute));
            string 名字 = attr?.文字 ?? m.Name.Substring(3);
            var fn = (Func<string, CheckResult>)(代码 => (CheckResult)m.Invoke(null, new object[] { 代码 })!);
            list.Add(new CppCheckItem(名字, fn));
        }
        return list;
    }
}
