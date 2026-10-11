using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("05", "第 05 课：方法与参数 —— ref / out / 默认值",
    "写三个方法：\n" +
    "  static int 平方(int x)                       // 返回 x 的平方\n" +
    "  static void 加倍(ref int x)                  // 把传入的变量翻倍（真的改到外面）\n" +
    "  static bool 试着转成整数(string s, out int 值) // 能转就 true 并赋值")]
public static class 第05课
{
    public static string 模板 => """
// 第 05 课：方法与参数 —— ref / out / 默认值
//
// 要做三个方法：
//
//     static int 平方(int x)                        // 返回 x * x
//     static void 加倍(ref int x)                   // 让外面的变量真的翻倍
//     static bool 试着转成整数(string s, out int 值)  // 能转就 true 并给 值 赋值
//
// ══ 这一课的重点：C# 的参数默认是「拷贝」 ══
//
//   调用 `加倍(我的数)` 时，传进去的是**一份拷贝** ——
//   方法里怎么改都影响不到外面。
//
//   想改到外面，必须加 `ref`（或 `out`）：
//
//       static void 加倍(ref int x) { x = x * 2; }
//       int a = 5;
//       加倍(ref a);          // ⚠ 调用时也要写 ref
//       Console.WriteLine(a); // 10
//
//   `out` 与 `ref` 的区别：
//     · `ref`   传进去之前**必须先赋过值**
//     · `out`   传进去之前**可以没值**，但方法里**必须给它赋值**
//     · `out` 的典型用途：`TryParse` 那种"成功了吗 + 顺带给你结果"
//
//   例如：
//       static bool 试着转成整数(string s, out int 值)
//       {
//           return int.TryParse(s, out 值);
//       }
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 05

// ── 在下面写你的三个方法 ──

""";

    public static string 参考答案 => """
static int 平方(int x)
{
    return x * x;
}

static void 加倍(ref int x)
{
    x = x * 2;
}

static bool 试着转成整数(string s, out int 值)
{
    return int.TryParse(s, out 值);
}
""";

    [检查说明("三个方法都写了")]
    public static CheckResult 检查_签名(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        foreach (var n in new[] { "平方", "加倍", "试着转成整数" })
            if (!去注释.Contains(n))
                return CheckResult.不过($"没找到「{n}」这个方法。");
        return CheckResult.过();
    }

    [检查说明("平方 对不同输入都对（含 0 与负数）")]
    public static CheckResult 检查_平方(string 代码)
    {
        var run = 调用(代码,
            "foreach (var __n in new[] { 1, 0, -3, 10 }) Console.WriteLine(学生代码.平方(__n));");
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "1", "0", "9", "100" };
        string[] 名 = { "1", "0", "-3", "10" };
        var 行 = run.行;
        if (行.Count < 4) return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"平方({名[i]}) 应当是 {期望[i]}，实际是“{行[i].Trim()}”。\n" +
                    "      ⚠ 负负得正：(-3) × (-3) = 9。");
        return CheckResult.过("4 组全对（含 0 与负数）");
    }

    [检查说明("加倍(ref) 真的改到了外面的变量")]
    public static CheckResult 检查_加倍(string 代码)
    {
        // ⚠ ref 参数在调用点里也要写 `ref` —— 这正是这一课要考的。
        var run = 调用(代码, """
            int __a = 5;  学生代码.加倍(ref __a);  Console.WriteLine(__a);
            int __b = 0;  学生代码.加倍(ref __b);  Console.WriteLine(__b);
            int __c = -3; 学生代码.加倍(ref __c);  Console.WriteLine(__c);
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "10", "0", "-6" };
        var 行 = run.行;
        if (行.Count < 3) return CheckResult.不过($"应该打印 3 行，实际 {行.Count} 行。");
        for (int i = 0; i < 3; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是 {期望[i]}，实际是“{行[i].Trim()}”。\n" +
                    "      ⚠ 要让方法改到外面，参数必须写 `ref`：\n" +
                    "         `static void 加倍(ref int x)` 且方法体里 `x = x * 2;`");
        return CheckResult.过("5→10 · 0→0 · -3→-6（真的改到了外面）");
    }

    [检查说明("试着转成整数(out) 成功/失败两条路都对")]
    public static CheckResult 检查_试着转(string 代码)
    {
        var run = 调用(代码, """
            int __v;
            Console.WriteLine(学生代码.试着转成整数("42", out __v) + ":" + __v);
            Console.WriteLine(学生代码.试着转成整数("-7", out __v) + ":" + __v);
            Console.WriteLine(学生代码.试着转成整数("abc", out __v));
            Console.WriteLine(学生代码.试着转成整数("", out __v));
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "True:42", "True:-7", "False", "False" };
        var 行 = run.行;
        if (行.Count < 4) return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是“{期望[i]}”，实际是“{行[i].Trim()}”。\n" +
                    "      ⚠ 转不了的时候也要给 `值` 赋一个值（`out` 要求方法里**必须**赋值）——\n" +
                    "         最省事的做法就是直接用 `int.TryParse(s, out 值);` 的返回值。");
        return CheckResult.过("转得了 / 转不了 两条路都对");
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
