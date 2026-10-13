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
    // ⚠⚠ 这一课原来只有 18 行、只有「要做」（旧风格）。改成**教程式**：
    //   先介绍 → 用法与例子 → 常见坑 → 轮到你了 → 怎么自己测。
    //   作者最早的批评就是「你这是教程吗，只有习题啊」——
    //   而 C# 的 01~08 是唯一还没改过来的八课。
    public static string 模板 => """
// 第 01 课：输出与变量 —— 让程序开口说话
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    打印和变量（Console.WriteLine）
//
//  需要先会
//    第 00 课：能编译、能跑
//
// ─────────────────────────────────────────────────────
//  先介绍：一个程序要能"说话"，也要能"记住东西"
// ─────────────────────────────────────────────────────
//
// 程序干的事说到底就两件：
//
//     ① 把结果显示出来（不然你不知道它算了个啥）
//     ② 把中间结果暂时存起来（不然每算一步都要重新输一遍）
//
// 这一课就学这两件事的最小版本：**打印** 和 **变量**。
//
//     打印  →  Console.WriteLine("你好，C#")
//     变量  →  int 甲 = 6;
//
// ⚠ 为什么先学这两个：后面每一课你都要用它们**看结果**和**存中间值**。
//   一个是"能看见"，一个是"能记住"，缺一个后面的课都做不下去。
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
// ① 打印一行字
//
//     Console.WriteLine("你好，C#");
//
//   读作"往控制台写一整行"。⚠ 最后那个分号 `;` **必须有** ——
//   C# 里每条语句都要用分号结尾，这是它和 Python 最大的长相差别。
//
//     Console.WriteLine("第一行");
//     Console.WriteLine("第二行");
//     ⇒ 第一行
//       第二行
//
// ② 变量：给一个值起个名字
//
//     int 甲 = 6;        ← 声明一个"整数"变量，叫 甲，里面放 6
//     int 乙 = 7;
//     int 和 = 甲 + 乙;   ← 用名字做计算
//
//   拆开看这五个部分：
//
//         int    甲   =   6   ;
//         ↑      ↑   ↑   ↑   ↑
//       类型    名字 赋值 值  分号（必须有）
//
//   ⚠ **类型必须写**。只写 `甲 = 6;` 是编译错误 —— 编译器不知道 甲 是什么。
//     （这一点和 Python 很不一样：Python 里 `甲 = 6` 就够了。
//       C# 要你在**写代码的时候**就告诉它类型。）
//
// ③ 把变量打出来
//
//     Console.WriteLine(甲);             ⇒ 6
//     Console.WriteLine(甲 + 乙);        ⇒ 13
//     Console.WriteLine("和是" + 和);     ⇒ 和是13
//
//   ⚠ 第三种最常用：**字符串 + 数字**会自动拼成一段文字。
//     让输出能看懂，而不是光秃秃一个数 —— 以后调试全靠它。
//
// ④ 字符串 vs 字符（现在分清，后面天天用）
//
//     "你好"     双引号 = **字符串**（0 个或多个字符）
//     'A'        单引号 = **字符**（正好一个）
//
//    写错了编译器会说类型不对：
//     Console.WriteLine('你好');    // ✗ 单引号里放不下两个字
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：忘了分号
//
//     Console.WriteLine("你好，C#")
//                                ↑ 少了 ;
//
// 你会看到：error CS1002: 需要分号（英文 `; expected`）
// 为什么：C# 用分号判断"这句话说完了"。没有分号，它会把下一行也当成同一句。
// ⚠ **报错行号经常指向下一行** —— 编译器是读到这里才发现少了东西。
//   看到 CS1002 时，先看**上一行末尾**。
//
// ⚠ 坑 2：中文标点
//
//     Console.WriteLine("你好，C#")；      ← 末尾是全角分号 ；
//
// 你会看到：error CS1002: 需要分号（**明明看着有分号**）
// 为什么：`;` 和 `；` 是两个不同的字符。同理还有 `(` `（`、`"` `”`。
// ⇒ 写代码时**把输入法切回英文** —— 这是初学者最高频的错，没有之一。
//
// ⚠ 坑 3：WriteLine / Console 的大小写
//
//     console.WriteLine("x");     // ✗ 小写 c
//     Console.writeline("x");     // ✗ 小写 l
//
// 你会看到：error CS0103: 当前上下文中不存在名称"console"
// 为什么：**C# 区分大小写**。`Console` 和 `console` 是两个不同的名字。
// ⇒ 记法：`Console` 是**类名**（大写开头），`WriteLine` 是它的方法。
//
// ⚠ 坑 4：定义了两个变量，却没用它们
//
//     int 甲 = 6;
//     int 乙 = 7;
//     Console.WriteLine(13);      ← 写死了
//
// 你会看到：**编译通过，输出也对** —— 但这一课的检查过不了。
// 为什么：题目要的是"用变量相加"，考的是**你会不会用变量**，
// 不是"你能不能算出 13"。写死数字换个题就不对了 ——
// 这类"结果碰对了"要自己警惕。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//   写一个方法 `static void 打招呼()`，里面按顺序做三件事：
//
//     1. 定义两个 int 变量：甲 = 6、乙 = 7
//     2. 打印一行：       你好，C#
//     3. 打印它们的和（用变量相加，**不要写死 13**）
//
//   期望输出两行：
//
//        你好，C#
//        13
//
//    两件事要注意：
//     · `打招呼` 的**签名已经有了**，你只写**方法体**（花括号里面）
//     · ⚠ **不用写 Main** —— 本课只考你写方法，入口由课程提供
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//
//   点界面上的「检查」。
//   命令行（在源码目录里）：
//
//       dotnet run --project csharp/cs-lessons 检查 01
//
//   想先看这一课要干什么：
//
//       dotnet run --project csharp/cs-lessons 01
//
//   卡住了就看答案，但**先自己写一遍**：
//
//       dotnet run --project csharp/cs-lessons 答案 01
//
//   ⚠ C# 和 C++ / Java 不一样的地方：**.NET SDK 自带编译器**，
//     不用另外装 g++ / JDK。装了 SDK 就什么都能编译。

// ── 在下面写你的方法 ──
static void 打招呼()
{
    // 在这里写

}
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
                     代码, @"(?m)^\s*(?:public\s+|internal\s+|private\s+)?(?:static\s+)?(?:sealed\s+)?(?:abstract\s+)?(?:class|interface|enum|struct|record)\s+(\w+)"))
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
