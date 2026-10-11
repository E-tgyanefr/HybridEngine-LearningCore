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
//
// 要做两个方法：
//
//     static string 判等级(int 分)      // ≥90 优秀 · ≥60 及格 · 否则不及格
//     static string 星期几(int n)       // 1→周一 … 7→周日，其他 → "不是星期"
//
// ══ 这一课的重点：C# 的 switch 与别的语言不一样 ══
//
//  ① **C# 不允许 case 之间"掉落"**
//       每个分支必须以 break / return 结束，否则**编译不过**。
//       （这跟 C / C++ / Java 的旧式 switch 不同 —— 那边忘了 break 会"串烧"。）
//
//  ② C# 有 **switch 表达式**，写起来短得多：
//
//        return n switch
//        {
//            1 => "周一",
//            2 => "周二",
//            _ => "不是星期",     // _ 是"其他所有情况"
//        };
//
//     三行就写完了，而且不会忘 break。
//
//  ⚠ 判等级那题注意顺序：要先判 ≥90 再判 ≥60。
//    反过来的话 95 分也会被判成"及格"。
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 03

// ── 在下面写你的两个方法 ──

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
