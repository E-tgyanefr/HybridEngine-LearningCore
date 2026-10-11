using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("08", "第 08 课：继承与接口 —— 各做各的，同一套调用",
    "写一个接口 `interface 图形 { double 面积(); string 名字(); }`\n" +
    "  再写三个实现：圆 / 矩形 / 正方形\n" +
    "  最后写 `static string 报告(List<图形> 们)` 把它们逐个描述出来")]
public static class 第08课
{
    public static string 模板 => """
// 第 08 课：继承与接口 —— 各做各的，同一套调用
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    继承与接口：各做各的，同一套调用
//
//  需要先会
//    第 07 课：类与对象
//
// ─────────────────────────────────────────────────────
//  先介绍：让"调用方"不必认识"实现方"
// ─────────────────────────────────────────────────────
//
// 要写一段代码，把一批图形逐个描述出来。不知道图形都有哪些时，你只能一个个问：
//
//     foreach (var g in 们)
//         if (g is 圆) { ... }
//         else if (g is 矩形) { ... }
//         else if (g is 三角形) { ... }      // ← 每加一种就要回来改这里
//
// 它跟"到底有哪些图形"死死绑在一起：**明天加个三角形，它就得改。**
//
// 接口把这件事翻过来：
//
//     interface 图形 { double 面积(); string 名字(); }
//
//     foreach (var g in 们)
//         Console.WriteLine($"{g.名字()} 面积={g.面积():F2}");
//
// 这段代码**不知道**自己拿到的是圆还是矩形，它只问两件事：
// "你叫什么""你多大面积"，每个类自己回答。
//
//   ⇒ 明天加一个"三角形"，这段代码**一个字都不用改**。
//     这就是接口的价值：**调用方与实现方解耦**。
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
// ① 接口：一份"必须会做什么"的清单
//
//     interface 图形 { double 面积(); string 名字(); }
//
//   ⚠ 接口里**只写声明**（签名后面直接一个分号），不写实现，也不写 public ——
//     接口成员默认就是公开的。
//
// ② 实现接口：用 `:`（C# 没有 Java 的 implements 关键字）
//
//     class 圆 : 图形
//     {
//         private readonly double 半径;
//         public 圆(double 半径) { this.半径 = 半径; }
//         public double 面积() => 3.14159 * 半径 * 半径;
//         public string 名字() => "圆";
//     }
//
//   ⚠ 实现出来的两个方法**必须写 public**，少了编译器直接拦住。
//   ⚠ `this.半径 = 半径;` 用来区分"字段"和"同名参数"。
//
// ③ 继承：复用已经写好的类
//
//     class 矩形 : 图形
//     {
//         protected readonly double 宽, 高;
//         public 矩形(double 宽, double 高) { this.宽 = 宽; this.高 = 高; }
//         public virtual double 面积() => 宽 * 高;      // virtual = 允许子类改写
//         public virtual string 名字() => "矩形";
//     }
//
//     class 正方形 : 矩形                                // 正方形**是**一种矩形
//     {
//         public 正方形(double 边长) : base(边长, 边长) { }   // 边长同时当宽和高
//         public override string 名字() => "正方形";          // 只改写名字
//     }
//
//   `virtual` / `override` 是一对：基类说"这个方法可以被改写"，
//   子类说"我改写它"。基类少了 `virtual`，子类的 `override` 就编译不过。
//
// ④ 多态：同一行调用，各做各的
//
//     var 们 = new List<图形> { new 圆(1), new 矩形(3, 4), new 正方形(5) };
//     foreach (var g in 们) Console.WriteLine($"{g.名字()} / {g.面积():F2}");
//
//     ⇒ 圆 / 3.14      矩形 / 12.00      正方形 / 25.00
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：实现接口时忘了写 public
//
//     class 圆 : 图形 { double 面积() => 1;  public string 名字() => "圆"; }
//     //                 ↑ 少了 public
//
// 你会看到：error CS0737: “学生代码.圆”不实现接口成员“学生代码.图形.面积()”。
//          “学生代码.圆.面积()”无法实现接口成员，因为它不是公共的。
// 为什么：接口成员默认公开，实现也必须公开；C# 里不写修饰符 = 最窄的可见性。
// ⇒ 实现接口的方法**全都加 public**。
//
// ⚠ 坑 2：漏实现一个成员
//
//     class 圆 : 图形 { public double 面积() => 1; }      // 名字() 没写
//
// 你会看到：error CS0535: “学生代码.圆”不实现接口成员“学生代码.图形.名字()”
// 为什么：接口是**一份完整清单**，签了就得全做到。
//
// ⚠ 坑 3：类声明忘了写 `: 图形`
//
//     class 圆 { public double 面积() => 1; public string 名字() => "圆"; }
//     // 而用到它的地方写的是： new List<图形> { new 圆(1) }
//
// 你会看到：error CS0266: 无法将类型“学生代码.圆”隐式转换为“学生代码.图形”。存在一个显式转换(是否缺少强制转换?)
// 为什么：方法名字一样**不等于**实现了接口 —— 继承关系必须写明。
//
// ⚠ 坑 4：改写一个没标 virtual 的方法
//
//     class 矩形 { public string 名字() => "矩形"; }          // 没有 virtual
//     class 正方形 : 矩形 { public override string 名字() => "正方形"; }
//
// 你会看到：error CS0506: “学生代码.正方形.名字()”: 继承成员“学生代码.矩形.名字()”未标记为 virtual、abstract 或 override，无法进行重写
// 为什么：基类没允许被改写。⇒ 要让子类能改写，基类那个方法必须写 `virtual`。
//
// ⚠ 坑 5：忘了调基类构造
//
//     class 正方形 : 矩形 { public 正方形(double 边长) { } }   // ← 没告诉矩形宽和高
//
// 你会看到：error CS7036: 未提供与“学生代码.矩形.矩形(double, double)”的所需参数“宽”对应的参数
// 为什么：子类构造时会先构造基类那部分，而矩形的构造函数要求两个参数。
// ⇒ 写 `: base(边长, 边长)`。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//   把骨架填完整（接口、三个类、一个方法都给你了）：
//
//     1. interface 图形          double 面积();  string 名字();
//     2. class 圆     : 图形      构造拿半径；面积 = 3.14159 × 半径 × 半径
//     3. class 矩形   : 图形      构造拿宽高；面积 = 宽 × 高；两个方法加 virtual
//     4. class 正方形 : 矩形      构造 `: base(边长, 边长)`；只改写 `名字()`
//     5. static string 报告(List<图形> 们)   每个一行：`名字 面积=xx.xx`（两位小数）
//
//   期望结果：
//
//     圆(1) → 圆 / 3.14     矩形(3,4) → 矩形 / 12.00     正方形(5) → 正方形 / 25.00
//     报告([正方形(2), 圆(2)]) 里要同时出现"正方形"和"圆"，面积是 4.00 和 12.57
//
//   ⚠ 三件事：
//     · 实现接口的方法**必须 public**；`正方形` 要**真的继承** 矩形
//     · `报告` 里只准用 `g.名字()` / `g.面积()`，**不许** `is 圆` 这种判断
//     · 不用写 Main、不用写 using，入口由课程提供
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//
//   点界面上的「检查」。检查会测面积、报告，还会确认"正方形真的继承自矩形"。
//
//       dotnet run --project csharp/cs-lessons 检查 08
//       dotnet run --project csharp/cs-lessons 答案 08
//
//   ⚠ 刚生成的答题文件是个骨架（方法体都是空的），直接检查会报错 —— 填完就好。
//   ⚠ 常见症状对照：
//     · 报 CS0737 → 实现接口的方法忘了 public
//     · 报 CS0535 → 接口里有成员没实现
//     · 报 CS0506 → 基类那个方法没写 `virtual`
//     · 报 CS7036 → `正方形` 的构造函数少了 `: base(边长, 边长)`
//     · 报 CS0266 → 某个类忘了写 `: 图形`（写成 Java 的 implements 则会报 CS1514）

// ── 在下面写你的接口和类 ──
interface 图形
{
    // 在这里写两个方法声明（不写实现）

}

class 圆 : 图形 { }

class 矩形 : 图形 { }

class 正方形 : 矩形 { }

static string 报告(List<图形> 们)
{
    // 在这里写

}
""";

    public static string 参考答案 => """
interface 图形
{
    double 面积();
    string 名字();
}

class 圆 : 图形
{
    private readonly double 半径;
    public 圆(double 半径) { this.半径 = 半径; }
    public double 面积() => 3.14159 * 半径 * 半径;
    public string 名字() => "圆";
}

class 矩形 : 图形
{
    protected readonly double 宽, 高;
    public 矩形(double 宽, double 高) { this.宽 = 宽; this.高 = 高; }
    public virtual double 面积() => 宽 * 高;
    public virtual string 名字() => "矩形";
}

class 正方形 : 矩形
{
    public 正方形(double 边长) : base(边长, 边长) { }
    public override string 名字() => "正方形";
}

static string 报告(List<图形> 们)
{
    var sb = new System.Text.StringBuilder();
    foreach (var g in 们)
        sb.AppendLine($"{g.名字()} 面积={g.面积():F2}");
    return sb.ToString().TrimEnd();
}
""";

    [检查说明("接口与三个类都写了")]
    public static CheckResult 检查_签名(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        foreach (var n in new[] { "interface 图形", "class 圆", "class 矩形", "class 正方形" })
            if (!去注释.Contains(n))
                return CheckResult.不过($"没找到「{n}」。");
        if (!去注释.Contains("报告"))
            return CheckResult.不过("没找到「报告」这个方法。");
        return CheckResult.过();
    }

    [检查说明("三种图形都能算面积（正方形复用了矩形）")]
    public static CheckResult 检查_面积(string 代码)
    {
        var run = 调用(代码, """
            var __们 = new List<图形> { new 圆(1), new 矩形(3, 4), new 正方形(5) };
            foreach (var __g in __们)
                Console.WriteLine($"{__g.名字()}|{__g.面积():F2}");
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "圆|3.14", "矩形|12.00", "正方形|25.00" };
        var 行 = run.行;
        if (行.Count < 3) return CheckResult.不过($"应该打印 3 行，实际 {行.Count} 行。");
        for (int i = 0; i < 3; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个应当是“{期望[i]}”，实际是“{行[i].Trim()}”。\n" +
                    "      ⚠ 中间那个别搞混：矩形(3,4) 面积是 12.00。\n" +
                    "        正方形(5) 应当复用矩形 —— 5×5 = 25.00。");
        return CheckResult.过("圆 3.14 · 矩形 12.00 · 正方形 25.00");
    }

    [检查说明("报告() 对同一批图形都能描述（它不认识具体类型）")]
    public static CheckResult 检查_报告(string 代码)
    {
        var run = 调用(代码, """
            var __们 = new List<图形> { new 正方形(2), new 圆(2) };
            Console.WriteLine(学生代码.报告(__们));
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        var 文 = string.Join("\n", run.行);
        if (!文.Contains("正方形") || !文.Contains("圆"))
            return CheckResult.不过(
                $"报告里应当同时出现“正方形”和“圆”，实际是：\n{文}\n" +
                "      ⚠ 报告() 里要用 `g.名字()` 和 `g.面积()`，\n" +
                "        而不是去判断“它是不是圆”。");
        if (!文.Contains("4.00") && !文.Contains("12.57"))
            return CheckResult.不过(
                $"报告里没看到面积数字，实际是：\n{文}\n" +
                "      写法：$\"{g.名字()} 面积={g.面积():F2}\"");
        return CheckResult.过("两种图形都被描述了");
    }

    [检查说明("正方形真的继承自矩形（不是各写一遍）")]
    public static CheckResult 检查_继承(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!System.Text.RegularExpressions.Regex.IsMatch(
                去注释, @"class\s+正方形\s*:\s*矩形"))
            return CheckResult.不过(
                "「正方形」应当写成 `class 正方形 : 矩形` —— 复用矩形的面积计算。\n" +
                "      这一课要练的就是继承：正方形是特殊的矩形，\n" +
                "      把自己的边长同时当宽和高传上去就行（`base(边长, 边长)`）。");
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
