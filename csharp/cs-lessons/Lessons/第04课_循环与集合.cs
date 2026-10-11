using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("04", "第 04 课：循环与集合 —— List 与 foreach",
    "写一个方法 `static int 求和(List<int> 数)`：\n" +
    "  用 foreach 把列表里的数加起来返回\n" +
    "  再写一个 `static List<int> 只要偶数(List<int> 数)`，返回其中的偶数")]
public static class 第04课
{
    public static string 模板 => """
// 第 04 课：循环与集合 —— List 与 foreach
//
// 要做两个方法：
//
//     static int 求和(List<int> 数)          // 用 foreach 加起来
//     static List<int> 只要偶数(List<int> 数) // 挑出偶数，返回新列表
//
// ══ 这一课的重点：C# 的数组是**定长**的 ══
//
//   ① `int[] a = {1,2,3};` —— 长度固定，**不能加元素**
//      `a[3] = 4;` 直接抛 IndexOutOfRangeException
//
//   ② 要能增删就用 `List<int>`：
//        var 数 = new List<int> { 1, 2, 3 };
//        数.Add(4);              // 可以加
//        Console.WriteLine(数.Count);   // 注意是 Count，不是 Length
//
//   ③ 遍历：
//        foreach (var x in 数) { ... }        // 只读，最常用
//        for (int i = 0; i < 数.Count; i++)   // 需要下标时
//
// ⚠ 两个最容易记混的地方：
//     · 数组用 `.Length`，List 用 `.Count`
//     · 在 `foreach` 里**不能**增删那个集合（会抛异常）——
//       要改就先收集到另一个 List，循环结束后再处理
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 04

// ── 在下面写你的两个方法 ──
// 顶部已经 using System.Collections.Generic;（List 要用它）

""";

    public static string 参考答案 => """
static int 求和(List<int> 数)
{
    int 和 = 0;
    foreach (var x in 数) 和 += x;
    return 和;
}

static List<int> 只要偶数(List<int> 数)
{
    var 出 = new List<int>();
    foreach (var x in 数)
        if (x % 2 == 0) 出.Add(x);
    return 出;
}
""";

    [检查说明("两个方法都写了")]
    public static CheckResult 检查_签名(string 代码)
    {
        foreach (var n in new[] { "求和", "只要偶数" })
            if (!去掉注释(代码).Contains(n))
                return CheckResult.不过($"没找到「{n}」这个方法。");
        return CheckResult.过();
    }

    [检查说明("求和 对不同列表都对（含空列表与负数）")]
    public static CheckResult 检查_求和(string 代码)
    {
        var run = 调用(代码, """
            Console.WriteLine(学生代码.求和(new List<int> { 1, 2, 3, 4, 5 }));
            Console.WriteLine(学生代码.求和(new List<int> { }));
            Console.WriteLine(学生代码.求和(new List<int> { -5, 5 }));
            Console.WriteLine(学生代码.求和(new List<int> { 100 }));
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "15", "0", "0", "100" };
        string[] 名 = { "[1..5]", "空列表", "[-5,5]", "[100]" };
        var 行 = run.行;
        if (行.Count < 4) return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"求和({名[i]}) 应当是 {期望[i]}，实际是“{行[i].Trim()}”。\n" +
                    "      ⚠ 空列表要返回 0 —— 和变量必须**先初始化为 0**。");
        return CheckResult.过("4 组输入全对（含空列表与负数）");
    }

    [检查说明("只要偶数 结果正确")]
    public static CheckResult 检查_只要偶数(string 代码)
    {
        var run = 调用(代码, """
            Console.WriteLine(string.Join(",", 学生代码.只要偶数(new List<int> { 1, 2, 3, 4, 5, 6 })));
            Console.WriteLine("[" + string.Join(",", 学生代码.只要偶数(new List<int> { 1, 3, 5 })) + "]");
            Console.WriteLine(string.Join(",", 学生代码.只要偶数(new List<int> { -2, 0, 2 })));
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "2,4,6", "[]", "-2,0,2" };
        var 行 = run.行;
        if (行.Count < 3) return CheckResult.不过($"应该打印 3 行，实际 {行.Count} 行。");
        for (int i = 0; i < 3; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 组错了：应当是“{期望[i]}”，实际是“{行[i].Trim()}”。\n" +
                    "      ⚠ 三件事：偶数判据是 `x % 2 == 0`（不是 `x % 2 == 1`）；\n" +
                    "        没有偶数时要返回**空列表**（不是 null）；\n" +
                    "        **0 是偶数** —— `0 % 2 == 0`。");
        return CheckResult.过("含“没有偶数”与 0 的情况都对");
    }

    [检查说明("用了 List 而不是定长数组")]
    public static CheckResult 检查_用List(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("List<"))
            return CheckResult.不过(
                "没看到 `List<...>` —— 这一课要练的就是 List。\n" +
                "      定长数组（int[]）不能 Add，长度也固定。");
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
