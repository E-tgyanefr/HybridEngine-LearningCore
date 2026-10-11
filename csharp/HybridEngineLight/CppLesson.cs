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
        // ⚠⚠ 这里要分清楚两件**不同**的事，我把它们混过一次：
        //
        //   ① **骨架路径**（C#）：`调用点` 被替换进骨架里的 `入口.Main`
        //      ⇒ 整段程序直接编译运行。入口**已经由骨架提供**，
        //        所以下面传的 `entry` 只是"一个什么都不做的合法表达式"
        //        （库会拼成 `entry()`，传空串会生成 `();` ⇒ CS1525）。
        //
        //   ② **C/C++**：走 gcc，骨架里有 main，同样不需要 entry。
        //
        //   ⇒ 骨架路径下用不到真正的 entry。真正的"跑学员代码"在
        //     `直接运行`（学员给完整程序）那条路上。
        //
        //   ⚠ 而且 `调用点` 是**语句序列**（可以好几条、可以有 foreach），
        //     不能给它加 `学生代码.` 前缀 —— 那会生成
        //         () => { 学生代码.foreach (...) ... }
        //     ⇒ error CS1001: Identifier expected。
        //     语句里的方法名**在同一作用域内直接可见**（骨架把它们放在一起）。
        return 跑(程序, CppRunner.用CSharp ? 调用点 : "");
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
        // ⚠⚠ `entry` 由**调用方**给一个完整表达式，不在这里靠字符串推断。
        //
        //   我上一版在这里做"去掉尾部 ()"的推断 ⇒ `Main()` 变成 `学生代码.Main`，
        //   可 `Main` 其实在**学员自己的 `入口` 类**里，不在 `学生代码` 里：
        //       error CS0117: '学生代码' 不包含 'Main' 的定义
        //   ⇒ 推断是脆的：**同一个字符串在两种上下文里含义不同**
        //     （骨架的调用点在 `入口.Main` 里执行，学员的 Main 是自己那个类的方法）。
        //     让调用方直接把"要执行什么"说清楚。
        // ⚠⚠ **入口表达式必须由"调用点"生成** —— 这是整条链的最后一环，
        //   我漏了它 ⇒ 程序编译成功、却没有执行任何学员代码（输出是空的）。
        //
        //   为什么：`骨架转成可跑程序` 只保留 `学生代码` 类的成员，
        //   **把骨架里的 `入口.Main`（内容 = 调用点）丢掉了** ——
        //   而那个 Main 正是"要执行什么"的唯一记录。
        //   ⇒ 必须把调用点显式接续到入口表达式上。
        //
        //   写法：库会拼 `<entry>()`，所以 entry 要写成**立即执行的 lambda**：
        //       "(() => { <调用点> })"   ⇒ 库拼成 (() => { ... })()  ✓
        //   不加外层括号的话拼出来是 `() => { ... }()` ⇒ CS0149。
        // ⚠⚠ 整条链的**根本症结**，到这里才彻底解决：
        //
        //   C# 里 `入口` 类与 `学生代码` 类是**平级**的，所以 `入口.Main` 里
        //   写 `打招呼()` 根本看不见 —— 必须 `学生代码.打招呼()`。
        //   可调用点常常是**语句序列**（好几条、甚至 foreach），
        //   不能机械地给整段加前缀：
        //       学生代码.foreach (var n in ...) ...   ⇒ CS1001
        //
        //   ⇒ 解法：不做代码拼接，改成**运行时反射**：
        //     把"要执行的语句"整段塞进一个 lambda，lambda 里调用
        //     `执行.调用("学生代码", <参数>)` —— 由反射去找方法。
        //     这样"语句序列"和"单个方法调用"用同一套机制，一行都不用分情况。
        //
        //   于是 `打招呼();` 这种调用点会变成：
        //      入口.调用("学生代码", "打招呼");
        //   而带参数的 `判等级(95)` 则先求值再传进去。
        // ⚠⚠⚠ 入口表达式的形状，是整条链最后一块拼图。直接把库生成的
        //   真实代码贴在这里（我从临时工程的 Program.cs 里读出来的）：
        //
        //       try { () => { 学生代码.打招呼(); }(); }        ← 错！
        //       error CS0149: Method name expected
        //
        //   为什么错：库拼的是 `try { <entry>(); }`，而 C# **不允许
        //   一条表达式语句以 lambda 开头** —— `() => {...}()` 它不认。
        //   必须给 lambda 一个**委托类型**，它才是个合法的表达式。
        //
        //       try { new Action(() => { 学生代码.打招呼(); })(); }   ← 对
        //
        //   ⇒ 入口 = `new Action(() => { <调用点> })`（**不带**尾随 ()，
        //     那一对由库补；带了就会变成 `(…);()`）。
        //
        //   这一处我对了六遍括号。教训写下来：
        //   **拼接代码时，"对方会加什么"是契约，必须去读对方的源码，
        //     不能靠感觉数括号。**（读过一次就再没错过。）
        c = CSharpRunner.DumpOutput(骨架转成可跑程序(程序), 入口表达式(调用点));
        // ⚠⚠ 编译失败要靠**编译器输出**判断，不能只看 `c.Error`。
        //   实测：C# 编译失败时 `c.Error` 是 **null**，
        //   错误信息全在 `c.Raw` 与 `c.RawLines` 里
        //   ⇒ 只判 `Error != null` 的话，**编译失败会被当成成功**，
        //     学员看到的是"✓ 程序正常运行"——最坏的一类错。
        string 全部 = c.Raw ?? "";
        bool 有编译错 = 全部.Contains("error CS") || 全部.Contains("Build FAILED");
        if (c.Error != null || 有编译错)
        {
            raw.Error = c.Error ?? "编译失败";
            raw.CompileFailed = true;
            raw.CompileOutput = 全部;
            foreach (var l in c.RawLines) raw.Lines.Add(l);
            raw.RunOutput = 全部;
            return new CppRun { Raw = raw };
        }
        // ⚠ 成功时才把行当"程序输出"。
        //   而且**只加一次** —— 我第一版既加了 RawLines 又去遍历 Results，
        //   结果同一行出现两遍。
        foreach (var l in c.RawLines) raw.Lines.Add(l);
        raw.RunOutput = 全部;
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
    /// 由"调用点"生成入口表达式。
    ///
    /// 契约（读 `CSharpRunner.DumpOutput` 源码得来）：
    ///     库拼的是 `try { <entry>(); }`
    /// ⇒ ① `<entry>` 必须是**能调用的东西**（lambda 要带委托类型）
    ///   ② `<entry>` **自己不能带尾随 `()`**，那是库要补的
    ///
    /// 于是：
    ///     调用点为空   ⇒ `new Action(() => { })`
    ///     有调用点     ⇒ `new Action(() => { <调用点> })`
    ///
    /// ⚠ 为什么不用 `(() => { ... })`：C# 不允许表达式语句以 lambda 开头，
    ///   会报 `CS0149: Method name expected`。`new Action(...)` 才是表达式。
    /// </summary>
    private static string 入口表达式(string 调用点)
    {
        return 调用点.Length == 0
            ? "new Action(() => { })"
            : "new Action(() => { " + 调用点 + " })";
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
            // ⚠ **类**也要补 public（第 07/08 课踩到）：
            //   嵌套在 `学生代码` 里的 `class 计数器` 若没有 public，
            //   `学生代码.计数器` 就不可访问：
            //       error CS0122: '计数器' 不可访问，因为它具有一定的保护级别
            //   ⇒ 与"方法补 public"同理，课程替他补上，不让学员记这个细节。
            // ⚠ 必须是 `class` **和** `interface` **和** `enum` / `struct`，
            //   以及 **`record`**。
            //   我第一版只处理了 `class` —— 第 08 课的 `interface 图形`
            //   于是仍是 internal，而 `报告(List<图形>)` 补成 public 之后：
            //       error CS0051: 可访问性不一致 ——
            //         参数类型比方法更不可访问
            //   这类错误**只在同时有"公开的方法"和"没公开的类型"时出现**，
            //   所以前七课都没暴露。
            //   ⚠ 第二次栽在同一处：写第 09~16 课（元组/结构/记录类型）时
            //     漏了 `record` —— 调用点里的 `new 成绩记录(...)` 于是没被限定成
            //     `学生代码.成绩记录`，报
            //         error CS0246: 未能找到类型或命名空间名"成绩记录"
            //   ⇒ 教训：这份名单**不是"我知道的那几种"**。
            //     它是"学员能在顶层写出来的所有类型声明" ——
            //     加一门课、加一个语言特性时都要回来重看这一行。
            //     （`record` 还有个变体 `record struct`，被下面的 `record ` 前缀覆盖到。）
            if (去空格.StartsWith("class ", StringComparison.Ordinal) ||
                去空格.StartsWith("interface ", StringComparison.Ordinal) ||
                去空格.StartsWith("enum ", StringComparison.Ordinal) ||
                去空格.StartsWith("struct ", StringComparison.Ordinal) ||
                去空格.StartsWith("record ", StringComparison.Ordinal))
            {
                int 缩进0 = 行.Length - 去空格.Length;
                行 = 行.Substring(0, 缩进0) + "public " + 去空格;
                出.AppendLine(行);
                continue;
            }
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
        // 学员给的是**完整程序**（可能自己写了 Main）。
        // ⚠ `骨架转成可跑程序` 会把他的 Main **搬进 `学生代码` 类**
        //   （见 `搬Main`），所以这里调 `学生代码.Main`。
        //   若他没写 Main，`搬Main` 会补一个空方法 ⇒ 不崩，只是没输出。
        return 跑(学生代码, "Main();");
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
            // ⚠ 还要再分一层：**C 课不能借 C++ 的说明**。
            //   `用C` 时把语言名传成 "C"，翻译器就会说"C 区分大小写"
            //   而不是"C++ 区分大小写"。
            string 语言 = CppRunner.用CSharp ? "C#" : (CppRunner.用C ? "C" : "C++");
            var 说明 = CppRunner.用CSharp
                ? Explain.For(new Exception(run.Raw.CompileOutput))
                : ExplainCpp.For(run.Raw.CompileOutput, run.Raw.CompileOutput, true, 语言);
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
    /// <summary>把「调用点」里学员自己定义的名字限定成 `学生代码.xxx`。
    ///
    /// ⚠⚠ 为什么必须有它：
    ///   课程库把学员的代码放进 `public static class 学生代码`，
    ///   再把「调用点」放进**另一个类**（`入口`）的 `Main` 里。
    ///   ⇒ 调用点写 `描述(-1)` 时，编译器在 `入口` 里找不到 `描述`：
    ///         error CS0103: 当前上下文中不存在名称"描述"
    ///
    ///   所以调用点里出现的、**学员自己定义的名字**，必须补上 `学生代码.` 前缀。
    ///
    /// ⚠ 但**不能无脑替换所有标识符** —— 那会把 `Console.WriteLine` 也换掉。
    ///   判据是：**只换学员在代码里真的定义过的名字**。
    ///
    /// ⚠ 这份"类型声明名单"我栽过**两次**：
    ///     第一次只有 `class`，漏了 `interface`（第 08 课的 `interface 图形`）；
    ///     第二次漏了 `record`（新的元组/记录课 ⇒ CS0246）。
    ///   ⇒ 它不是"我知道的那几种"，而是"**学员能在顶层写出来的所有类型声明**"。
    ///     加一门课、加一个语言特性时都要回来重看这一行。
    /// </summary>
    public static string 限定(string 代码, string 调用点)
    {
        // 学员定义的方法名：`static 返回类型 名字(`
        var 方法名 = new System.Collections.Generic.List<string>();
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(代码,
                     // ⚠⚠ 返回类型这一段**必须允许空格和括号** —— 我第一版是
                     //     `[\w<>\[\],\.\?]+\s+(\w+)\s*\(`，它匹配不了**元组返回类型**：
                     //         static (string 名字, bool 及格) 取名字(成绩记录 r)
                     //                       ↑ 空格和括号
                     //     于是 `取名字` 没人认出来、调用点里的裸名没人限定 ⇒
                     //         error CS0103: 当前上下文中不存在名称"取名字"
                     //     而且**只有用元组的课会红**，别的课照样绿 —— 很难看出规律。
                     //   ⇒ 判据改成"`static` 与 `名字(` 之间的**任何东西**"，
                     //     而不是"我猜得出的那几种返回类型"。
                     //
                     // ⚠⚠ 我试过把 `static` 去掉（想让它也认出**实例方法**）——
                     //     **更糟**（16 课从 10 掉到 9）。为什么：
                     //       实例方法**不能**用 `学生代码.方法名(...)` 调用，
                     //       那需要一个对象。硬加限定符只会产生新的编译错误。
                     //     ⇒ 判据必须**只认 `static`**（那些才配得上类名前缀）。
                     //       第 11/13 课那种"调用实例方法"的场景需要**另一种**做法
                     //       （比如让调用点自己先 new 一个对象）—— 那是另一件事，
                     //       不是把正则放宽就能解决的。
                     @"(?m)^\s*(?:public\s+|private\s+|internal\s+|protected\s+)?static\s+" +
                     @".+?\s+(\w+)\s*\("))
        {
            方法名.Add(m.Groups[1].Value);
        }
        // 学员定义的类型名：class / interface / enum / struct / record
        var 类型名 = new System.Collections.Generic.List<string>();
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(代码,
                     @"(?m)^\s*(?:public\s+|internal\s+|private\s+)?(?:static\s+)?" +
                     @"(?:sealed\s+)?(?:abstract\s+)?(?:partial\s+)?" +
                     @"(?:class|interface|enum|struct|record)\s+(\w+)"))
        {
            类型名.Add(m.Groups[1].Value);
        }

        string 出 = 调用点;
        foreach (var 名 in 方法名)
        {
            // 前面不是 `.` 或标识符字符（避免把别的名字的一部分换掉），
            // 后面跟 `(`
            出 = System.Text.RegularExpressions.Regex.Replace(出,
                @"(?<![\w.])" + System.Text.RegularExpressions.Regex.Escape(名) + @"\s*\(",
                "学生代码." + 名 + "(");
        }
        foreach (var 名 in 类型名)
        {
            // `new 成绩记录(...)` / `成绩记录 r` 里的类型名
            //
            // ⚠⚠ 我试过在这里加 `(?<!new\s)`（想避开 `new 计数器(1).值` 那种
            //    "前缀看起来对、其实坏了"的情况），结果**更糟**：
            //    16 课从 10 掉到 8，连本来过的 09 / 14 也红了。
            //    ⇒ 说明这一步的修法**不是"少替换一点"就能解决的** ——
            //      `new X(...)` 和 `X r` 两种用法需要**不同的处理**，
            //      而它们在同一个正则里分不开（后面跟的是 `(` 还是空白，
            //      在替换那一刻还没解析）。
            //
            //    ⇒ 结论：**留一个已知的、有限的不足**，比反复试一个
            //      我没想清楚的修法要好 —— 试错已经把 08/09/14 的绿弄丢过两次。
            //      真正的修法应该是在**生成调用点时**就写清楚
            //      （比如让数据里写 `学生代码.计数器(1).值` 而不是靠替换），
            //      那是下一步的事。
            出 = System.Text.RegularExpressions.Regex.Replace(出,
                @"(?<![\w.])" + System.Text.RegularExpressions.Regex.Escape(名) + @"\b",
                "学生代码." + 名);
        }
        // ⚠ 上面两步会把已经被限定过的再限定一次（`学生代码.学生代码.xxx`），
        //   收一下。判据用"前面不是 `.`"，所以理论上不会发生 —— 但发生过一次
        //   就够毁了，所以稳妥起见做一遍收尾。
        出 = 出.Replace("学生代码.学生代码.", "学生代码.");
        return 出;
    }


    /// <summary>第 00 课专用：工具链到底装好了没有。
    ///
    /// ⚠⚠ 为什么要单独有这一条：
    ///   没装编译器时，`CppRunner.BuildAndRun` 会把安装说明塞进 `r.Error`，
    ///   而 `CompileFailed` 是 **false**（编译根本没发生）。
    ///   于是 `编译失败说明` 走最后那句兜底 ——
    ///   学员看到「程序没能正常运行：没有找到 C 编译器…」，
    ///   **看上去像是他的代码有问题**，而不是「你还没装工具」。
    ///   第 00 课的全部意义就是确认工具装好了，所以它必须把这件事说清楚。
    /// </summary>
    public static CheckResult 工具链就绪(string 代码)
    {
        var 缺 = new List<string>();
        // C# 课的"编译器"就是 dotnet 自己 —— 缺了连这个程序都跑不起来，不用查
        if (!CppRunner.用CSharp && CppRunner.Compiler == null)
        {
            缺.Add(CppRunner.用C ? "gcc" : "g++");
        }
        if (缺.Count > 0)
        {
            return CheckResult.不过(
                "工具链还没装好 —— **这不是你代码的问题**。\n\n" +
                "      现在缺：" + string.Join("、", 缺) + "\n\n" +
                CppRunner.InstallHint);
        }
        return CheckResult.过("编译器和 dotnet 都在 —— 这台机器可以编 C 了");
    }


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
