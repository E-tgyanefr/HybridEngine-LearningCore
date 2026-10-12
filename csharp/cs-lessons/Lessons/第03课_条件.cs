using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("03", "第 03 课：条件判断 —— switch 与 if 的选择",
    "写一个方法 `static string 判等级(int 分)`：\n" +
    "  ≥90 返回 \"优秀\"、≥60 返回 \"及格\"、否则 \"不及格\"\n" +
    "  再写一个 `static string 星期几(int n)`，用 **switch 表达式**返回 \"周一\"…\"周日\"")]
public static class 第03课
{
    public static string 模板 => """
// 第 03 课：条件判断 —— switch 与 if 的选择
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    条件判断：switch 和 if 各什么时候用
//
//  需要先会
//    第 01 课：比较那套写法
//
// ─────────────────────────────────────────────────────
//  先介绍：程序不能只会"照着算"
// ─────────────────────────────────────────────────────
//
// 前两课的程序都是**一条路走到底**：打印、算面积。
// 真实程序几乎每一步都在分岔：
//
//     够 90 分吗？→ 优秀；否则够 60 吗？→ 及格；否则不及格
//
// 这就是"条件"：**同一段代码，按情况走不同的路**。
//
// C# 的条件写法有两种，这一课两种都要用上：
//
//     if / else if / else   按**范围**判断（分数 ≥ 90）
//     switch                按**具体值**挑（1 → 周一，2 → 周二）
//
// ⚠ 但 C# 的 switch 和 C / C++ / Java 的旧式 switch 有一处**根本不同**：
//   它**不允许 case 之间"掉落"**。忘写 break / return 不是"串烧"，
//   而是**直接编译不过**。这是 C# 故意的 —— 忘 break 是 C 里最经典的一类 bug。
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
// ① if / else if / else
//
//     if (分 >= 90)      Console.WriteLine("优秀");
//     else if (分 >= 60) Console.WriteLine("及格");
//     else               Console.WriteLine("不及格");
//
//   条件必须是一个 **bool**（true / false）。
//   比较用 `>=` `>` `==` `!=` `<` `<=`；组合用 `&&`（且） `||`（或） `!`（非）。
//
//   ⚠ 相等是 `==`（两个等号）。一个 `=` 是**赋值**，写法上错了编译器会拦你。
//
// ② 老式 switch 语句：每个分支必须"走掉"
//
//     switch (n)
//     {
//         case 1: return "周一";
//         case 2: return "周二";
//         default: return "不是星期";
//     }
//
//   每个 case 必须以 `break;` 或 `return` 结束。
//
//   ⚠ 唯一允许的例外是**空的 case 并列**，它没有"掉落"：
//
//         case 1:
//         case 2: return "周一周二都算";
//
// ③ switch 表达式（推荐，短得多，而且不会忘 break）
//
//     return n switch
//     {
//         1 => "周一",
//         2 => "周二",
//         3 => "周三",
//         4 => "周四",
//         5 => "周五",
//         6 => "周六",
//         7 => "周日",
//         _ => "不是星期",      // `_` 是"其余所有情况"，放最后
//     };
//
//   · 用 `=>` 而不是 `case ... :`
//   · 每支末尾是**逗号**（最后那支的逗号可有可无）
//   · 它是个**表达式**：可以直接 return，也可以放在赋值右边
//
// ④ 本课要写的两个方法
//
//     static string 判等级(int 分)   95/90→优秀 · 60→及格 · 59→不及格
//     static string 星期几(int n)    1→周一 … 7→周日 · 8→不是星期
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：case 之间"掉落"
//
//     switch (n)
//     {
//         case 1: return "周一";
//         case 2:
//             Console.WriteLine("周二");     // ← 这里既没 break 也没 return
//         case 3: return "周三";
//     }
//
// 你会看到：error CS0163: 控制不能从一个 case 标签(“case 2:”)贯穿到另一个 case 标签
// 为什么：C# 从语言层面禁掉了 C 那种"串烧" —— 要出去就写 break 或 return。
//
// ⚠ 坑 2：switch 表达式漏了 `_`
//
//     return n switch { 1 => "周一", 2 => "周二" };
//
// 你会看到（⚠ 编译只是**警告**，不是错误）：
//     warning CS8509: switch 表达式不处理其输入类型的所有可能的值(它不是穷举)。例如，模式“0”未包含在内。
// 于是程序**编译过了**，但一喂 8 就崩：
//     未处理异常 System.Runtime.CompilerServices.SwitchExpressionException:
//     Non-exhaustive switch expression failed to match its input.
//     Unmatched value was 9.
//
// 为什么：switch 表达式必须**穷举**所有可能的值；int 有四十多亿种，你列不全
//         ⇒ 只能靠 `_` 兜底。
// ⇒ ⚠ 这条最阴：不报错，只在**某个输入**上崩。写完一定要喂一个越界值试试。
//
// ⚠ 坑 3：if 里少写一个等号
//
//     if (分 = 90) return "优秀";      // ← 一个 = 是赋值
//
// 你会看到：error CS0029: 无法将类型“int”隐式转换为“bool”
// 为什么：赋值表达式的类型是 int，而 if 要 bool。
//         C# 故意不让它通过（C 里能过，于是 `if (x = 1)` 成了经典 bug）。
//
// ⚠ 坑 4：判等级的顺序写反了
//
//     if (分 >= 60) return "及格";
//     else if (分 >= 90) return "优秀";     // ← 永远轮不到
//
// 你会看到：**编译通过**，跑出来 95 分被判成"及格"。
// 为什么：第一个条件先命中就 return 了。
// ⇒ **范围窄的条件写在前面**：先判 ≥ 90，再判 ≥ 60。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//   写两个方法：
//
//     1. static string 判等级(int 分)
//          ≥ 90 → "优秀"     ≥ 60 → "及格"     否则 → "不及格"
//        用 if / else if / else。 注意顺序：先判 90，再判 60。
//
//     2. static string 星期几(int n)
//          1 → "周一" … 7 → "周日"，其他（比如 8）→ "不是星期"
//         这一课**要求用 switch 表达式**：
//            n switch { 1 => "周一", …, _ => "不是星期" }
//
//   期望结果：
//
//     判等级:  95 → 优秀   90 → 优秀   60 → 及格   59 → 不及格
//     星期几:  1 → 周一 … 7 → 周日    8 → 不是星期
//
//    两件事：
//     · 两个签名**都给你了**，你只写方法体
//     · **不用写 Main、不用写 using**，入口由课程提供
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//
//   点界面上的「检查」。检查会试 95 / 90 / 60 / 59 和 1…8 这些边界值。
//
//       dotnet run --project csharp/cs-lessons 检查 03
//       dotnet run --project csharp/cs-lessons 03
//       dotnet run --project csharp/cs-lessons 答案 03
//
//    常见症状对照：
//     · 90 被判成"及格"   → `>=` 写成了 `>`
//     · 60 被判成"不及格" → 边界上用了 `>` 而不是 `>=`
//     · 8 让程序崩（SwitchExpressionException）→ 漏了 `_ =>` 那一支
//     · 报 CS0163 → 老式 switch 里有个 case 忘了 break / return

// ── 在下面写你的两个方法 ──
static string 判等级(int 分)
{
    // 在这里写

}

static string 星期几(int n)
{
    // 在这里写（要求用 switch 表达式）

}
""";

    public static string 参考答案 => """
static string 判等级(int 分)
{
    if (分 >= 90) return "优秀";
    else if (分 >= 60) return "及格";
    else return "不及格";
}

static string 星期几(int n)
{
    return n switch
    {
        1 => "周一",
        2 => "周二",
        3 => "周三",
        4 => "周四",
        5 => "周五",
        6 => "周六",
        7 => "周日",
        _ => "不是星期",
    };
}
""";

    [检查说明("两个方法都写了")]
    public static CheckResult 检查_签名(string 代码)
    {
        foreach (var n in new[] { "判等级", "星期几" })
            if (!去掉注释(代码).Contains(n))
                return CheckResult.不过($"没找到「{n}」这个方法。");
        return CheckResult.过();
    }

    [检查说明("判等级 的边界值（95 / 90 / 60 / 59）都对")]
    public static CheckResult 检查_判等级(string 代码)
    {
        // ⚠ 统一入口：一条调用点跑完所有用例（原来这里写了一大段自定义骨架，
        //   会和库追加的 `入口` 类撞车 —— 详见 第01课 里的说明）。
        var run = 调用(代码,
            "foreach (var __n in new[] { 95, 90, 60, 59 }) Console.WriteLine(学生代码.判等级(__n));");
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "优秀", "优秀", "及格", "不及格" };
        var 行 = run.行;
        if (行.Count < 4) return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例错了：应当是“{期望[i]}”，实际是“{行[i].Trim()}”。\n" +
                    "      四个用例是 95 / 90 / 60 / 59 —— 重点看 **90 和 60 这两个边界**。\n" +
                    "      （`> 90` 和 `>= 90` 的差别就在这里。）");
        return CheckResult.过("95→优秀 · 90→优秀 · 60→及格 · 59→不及格");
    }

    [检查说明("星期几 用 switch（而且没有忘 break 的问题）")]
    public static CheckResult 检查_星期几(string 代码)
    {
        var run = 调用(代码,
            "for (int __i = 1; __i <= 8; __i++) Console.WriteLine(学生代码.星期几(__i));");
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "周一", "周二", "周三", "周四", "周五", "周六", "周日", "不是星期" };
        var 行 = run.行;
        if (行.Count < 8) return CheckResult.不过($"应该打印 8 行，实际 {行.Count} 行。");
        for (int i = 0; i < 8; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"星期几({i + 1}) 应当是“{期望[i]}”，实际是“{行[i].Trim()}”。\n" +
                    "      ⚠ 注意输入 8 应当返回“不是星期” —— 别忘了 `_ =>` 那一支。");
        return CheckResult.过("1..7 都对，8 走默认分支");
    }

    [检查说明("星期几 真的用了 switch")]
    public static CheckResult 检查_用了switch(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("switch"))
            return CheckResult.不过(
                "「星期几」没有用 switch。\n" +
                "      这一课就是要你见识 C# 的 switch 表达式：\n" +
                "          return n switch { 1 => \"周一\", ..., _ => \"不是星期\" };");
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
