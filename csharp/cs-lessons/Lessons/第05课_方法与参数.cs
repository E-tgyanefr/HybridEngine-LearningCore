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
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    方法、参数，以及 ref / out / 默认值
//
//  需要先会
//    第 01 课：变量
//
// ─────────────────────────────────────────────────────
//  先介绍：方法拿到的是"复印件"
// ─────────────────────────────────────────────────────
//
// 一个方法像个黑盒：给它几个值，它算完还你一个值。
//
//     static int 平方(int x) { return x * x; }
//
// 但有一件事新手几乎一定会踩：
//
//     **方法里的参数，是调用者那个变量的"复印件"。**
//
//     static void 加一(int x) { x = x + 1; }     // 改的是复印件
//     int 我的数 = 5;
//     加一(我的数);
//     Console.WriteLine(我的数);                 ⇒ 还是 5
//
// 这个设计不是坑，是**保护**：方法不会顺手改掉你外面的一堆变量。
// 但有时候你**就是**想让它改到外面（把一个数翻倍、把一个字符串转成数字）。
//
// C# 给了两个关键字专门干这件事：
//
//     ref   我允许你改这个变量本身
//     out   这个变量我进去时没有值，你出来时**必须**给我一个
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
// ① 普通参数：复印件
//
//     static int 平方(int x) => x * x;
//
//   方法里怎么改 `x`，外面都看不见。**返回值才是"给外面的东西"**。
//
// ② ref：让方法改到外面
//
//     static void 加倍(ref int x) { x = x * 2; }
//
//     int a = 5;
//     加倍(ref a);            // ⚠ 调用时也必须写 ref
//     Console.WriteLine(a);   ⇒ 10
//
//   ⚠ `ref` **两边都要写**：声明处写，调用处也写。
//   ⚠ 传进去的必须是个**变量**，而且**已经赋过值**（方法里会先读它）。
//
// ③ out：方法"顺带"给你一个结果
//
//     static bool 试着转成整数(string s, out int 值)
//     {
//         return int.TryParse(s, out 值);
//     }
//
//     int v;
//     Console.WriteLine(试着转成整数("42", out v) + ":" + v);   ⇒ True:42
//     Console.WriteLine(试着转成整数("abc", out v));            ⇒ False
//
//   ref 与 out 的差别就两条：
//
//     · `ref`   进去之前**必须先赋过值**
//     · `out`   进去之前**可以没值**，但方法里**必须给它赋值**
//
//   为什么要有 out：一个方法只能 return 一个值。
//   "成功了吗"和"结果是几"是两样东西 ⇒ 一样 return，一样用 out 带出来。
//
// ④ 默认参数值
//
//     static int 加(int a, int b = 1) => a + b;
//
//     加(5)       ⇒ 6      （b 用默认值 1）
//
//   ⚠ 有默认值的参数必须排在**后面**；调用时也能按名字给（`加(5, b: 9)` ⇒ 14）。
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：调用时忘了写 ref
//
//     static void 加倍(ref int x) { x = x * 2; }
//     int a = 5;
//     加倍(a);                  // ← 少了一个 ref
//
// 你会看到：error CS1620: 参数 1 必须与关键字“ref”一起传递
// 为什么：`ref` 是**方法签名的一部分**，编译器要你写出来，
//         表示"我知道这个方法会改我的变量"。⇒ 声明和调用**两处都要写**。
//
// ⚠ 坑 2：传给 ref 的不是"变量"，或者它还没赋过值
//
//     加倍(ref 5);
//     int a;  加倍(ref a);
//
// 你会看到：error CS1510: ref 或 out 值必须是可以赋值的变量
//           （换成没赋值的变量则报）error CS0165: 使用了未赋值的局部变量“a”
// 为什么：`ref` 要的是"某个变量的位置"，常量和表达式没有位置可给；
//         而方法里会先**读**那个变量再乘 2，所以进去之前它必须已经有值。
//
// ⚠ 坑 3：out 参数忘了赋值
//
//     static bool 试着转成整数(string s, out int 值) { return false; }
//     //                                               ↑ 从没给 值 赋过值
//
// 你会看到：error CS0177: 控制离开当前方法之前必须对 out 参数“值”赋值
// 为什么：`out` 是对调用者的**承诺** ——"出来时它一定有值"。
//   ⚠ 孪生的一条：方法里**先读** out 参数也不行 ——
//     error CS0269: 使用了未赋值的 out 参数“值”
//   ⇒ 最省事的写法：`return int.TryParse(s, out 值);`
//
// ⚠ 坑 4：给 ref / out 参数写默认值
//
//     static void 加倍(ref int x = 5) { ... }
//
// 你会看到：error CS1741: ref 或 out 参数不能有默认值
// 为什么：ref / out 的值由**调用者**决定，再给个默认值自相矛盾。
// ⇒ 另外，**默认值必须是编译期常量**：
//       static int 加(int x = 计数)     ⇒ error CS1736: “x”的默认参数值必须是编译时常量
//     默认值是在编译时写进调用点的，只能写字面量（`= 0`、`= 1`）。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//   写三个方法：
//
//     1. static int 平方(int x)                         返回 x * x
//     2. static void 加倍(ref int x)                    让**外面的变量**真的翻倍
//     3. static bool 试着转成整数(string s, out int 值)  转得了 → true 并给 值 赋值；
//                                                       转不了 → false（值 也要有值）
//
//   期望结果：
//
//     平方:         1→1   0→0   -3→9   10→100
//     加倍:         5→10  0→0   -3→-6
//     试着转成整数: "42"→True 且 值=42    "-7"→True 且 值=-7
//                   "abc"→False           ""→False
//
//   ⚠ 三件事：
//     · 三个签名**都给你了**，你只写方法体
//     · ⚠ **负负得正**：(-3) × (-3) = 9，不用特判负数
//     · 不用写 Main、不用写 using，入口由课程提供
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//
//   点界面上的「检查」。
//
//       dotnet run --project csharp/cs-lessons 检查 05
//       dotnet run --project csharp/cs-lessons 05
//       dotnet run --project csharp/cs-lessons 答案 05
//
//   ⚠ 常见症状对照：
//     · 加倍 之后外面没变 → 参数上少了 `ref`（声明处或调用处）
//     · 报 CS1620 → 调用时忘了写 ref
//     · 报 CS0177 → out 参数在某条路径上没赋值
//     · "abc" 那一组把程序搞崩 → 失败路径也得给 值 赋个值

// ── 在下面写你的三个方法 ──
static int 平方(int x)
{
    // 在这里写

}

static void 加倍(ref int x)
{
    // 在这里写

}

static bool 试着转成整数(string s, out int 值)
{
    // 在这里写

}
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
