using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("02", "第 02 课：类型与计算 —— int 和 double 不是一回事",
    "写一个方法 `static double 算面积(int 半径)`：\n" +
    "  用 3.14159 作为圆周率，返回 圆的面积（圆周率 × 半径 × 半径）")]
public static class 第02课
{
    public static string 模板 => """
// 第 02 课：类型与计算 —— int 和 double 不是一回事
//
// 要做：写一个方法
//
//     static double 算面积(int 半径)
//
//   返回 3.14159 × 半径 × 半径
//
// ⚠ C# 的类型比 Python 严格得多，这一课的重点就是这件事：
//
//   ① **整数除法会丢小数**
//        int a = 7, b = 2;
//        a / b        →  3      （不是 3.5！）
//        (double)a / b → 3.5    （把其中一个转成 double 才行）
//
//   ② **返回类型要匹配**
//        方法声明是 double，就必须真的返回一个小数（或能隐式转成小数的东西）
//
//   ③ `3.14159` 这种带小数点的字面量是 **double**，
//      所以 `3.14159 * 半径 * 半径` 整个式子会提升成 double —— 这是对的做法。
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 02

// ── 在下面写你的方法 ──

""";

    public static string 参考答案 => """
static double 算面积(int 半径)
{
    return 3.14159 * 半径 * 半径;
}
""";

    [检查说明("方法存在，签名对")]
    public static CheckResult 检查_签名(string 代码)
    {
        var r = CppCheck.写了函数(代码, "算面积", "算面积(5)");
        if (!r.Ok) return r;
        return CheckResult.过();
    }

    [检查说明("半径 5 时面积约 78.54（对不同半径都对）")]
    public static CheckResult 检查_正确性(string 代码)
    {
        // ⚠ **统一入口**：一条调用点搞定所有用例。
        //
        //   原来这里写了一大段自定义骨架（自己拼 using、类、Main），
        //   结果和库追加的 `入口` 类撞车。现在改成：
        //   骨架由课程库提供，调用点里用**内插字符串**把每个用例打一行 ——
        //   不用自己造骨架，也就不会撞车。
        //
        //   ⚠ 用 `InvariantCulture`：否则某些区域设置下小数点是逗号，
        //     输出会变成 "78,53975"，判据就认不出来了。
        var run = 调用(代码, """
            foreach (var __r in new[] { 5, 1, 10 })
                Console.WriteLine(学生代码.算面积(__r).ToString("0.#####",
                    System.Globalization.CultureInfo.InvariantCulture));
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        var 行 = run.行;
        if (行.Count < 3)
            return CheckResult.不过($"应该打印 3 行，实际 {行.Count} 行。");
        string[] 期望 = { "78.53975", "3.14159", "314.159" };
        string[] 半径 = { "5", "1", "10" };
        for (int i = 0; i < 3; i++)
        {
            if (!double.TryParse(行[i].Trim(), System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out double 实))
                return CheckResult.不过($"第 {i + 1} 个结果不是数字：「{行[i].Trim()}」");
            double 期 = double.Parse(期望[i], System.Globalization.CultureInfo.InvariantCulture);
            if (Math.Abs(实 - 期) > 0.01)
                return CheckResult.不过(
                    $"半径 {半径[i]}：应当是 {期望[i]}，实际是 {实}。\n" +
                    "      ⚠ 检查一件事：**是不是整数除法把小数丢了**？\n" +
                    "         `3.14159 * 半径 * 半径` 里因为有小数，整个式子才是 double；\n" +
                    "         如果写成 `22 / 7 * 半径 * 半径`，前半段会按 int 算成 3。");
        }
        return CheckResult.过("半径 5 / 1 / 10 都对");
    }

    [检查说明("返回类型是 double（不是 int）")]
    public static CheckResult 检查_返回类型(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains("int 算面积"))
            return CheckResult.不过(
                "返回类型写成了 `int` —— 面积是小数，用 int 会被**截断**（78.53 变成 78）。\n" +
                "      要写成 `static double 算面积(int 半径)`。");
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
