using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("07", "第 07 课：类与对象 —— 把数据和行为放在一起",
    "写一个 `class 计数器`：\n" +
    "  · 有一个只读属性 `int 值`\n" +
    "  · 构造时可以从某个数开始（默认 0）\n" +
    "  · 方法 `加(int n)` 累加，`重置()` 归零\n" +
    "  再写 `class 银行账户`：`存(int n)` / `取(int n)`，取多了要拒绝")]
public static class 第07课
{
    public static string 模板 => """
// 第 07 课：类与对象 —— 把数据和行为放在一起
//
// ─────────────────────────────────────────────────────
//  先介绍：为什么要把"数据"和"改数据的规则"关在一起
// ─────────────────────────────────────────────────────
//
// 假设要写一个银行账户。没有类的时候，你会这么写：
//
//     int 余额 = 0;
//     void 存(int n) { 余额 += n; }
//     void 取(int n) { 余额 -= n; }        // ⚠ 忘了检查够不够
//
// 这段代码本身没错，但有个隐患：**余额是个谁都能改的变量**。
//
//     余额 = -500;  取(999999);         // 编译器一声不吭，余额直接变负数
//
// 问题不在"有人写错了"，而在**规则没有唯一的出口**：
// "余额不能变负"这条规则，散落在每一个用到余额的地方。
//
// 类要做的事只有一件：**把数据和改数据的规则放进同一个盒子**。
//
//     class 银行账户
//     {
//         public int 余额 { get; private set; }     // 外面只能读
//         public void 存(int n) { ... }
//         public bool 取(int n) { ... }             // 唯一的出口，里面有检查
//     }
//
// 现在想把余额改成负数？做不到 —— 外面只能**读** `余额`，
// 唯一的入口是 `存` / `取`，而 `取` 会检查。
//
//   ⇒ 这叫**封装**。它不是"把东西藏起来"，而是**让规则只有一个出口**。
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
// ① 一个最小的类
//
//     class 计数器
//     {
//         public int 值 { get; private set; }     // 属性：外面读得到、改不了
//
//         public 计数器(int 初值 = 0)             // 构造函数：与类同名、没有返回类型
//         {
//             值 = 初值;
//         }
//
//         public void 加(int n) => 值 += n;       // 方法
//         public void 重置() => 值 = 0;
//     }
//
// ② new 出一个对象，再调用它的方法
//
//     var 甲 = new 计数器();        // 用默认值 0
//     var 乙 = new 计数器(100);     // 从 100 开始
//
//     甲.加(3);  甲.加(4);
//     Console.WriteLine(甲.值);     ⇒ 7
//     甲.重置();
//     Console.WriteLine(甲.值);     ⇒ 0
//     Console.WriteLine(乙.值);     ⇒ 100     （甲乙互不影响）
//
//   ⚠ `甲` 和 `乙` 是**两个不同的对象**，各自有一份自己的 `值`。
//
// ③ 字段 vs 属性
//
//     public int 值;                        // 字段：外面能直接读写，没法保护
//     public int 值 { get; private set; }    // 属性：外面读得到，只有类自己能写
//     public int 值 { get; }                 // ⚠ 只有 get：**连类自己都改不了**
//
//   ⚠ 只有 get 是"构造完就冻住"。本课的 `加` / `重置` 要能改它，所以必须写 `private set`。
//     三种位置：外面读 甲.值 ✅ ｜ 外面写 甲.值 = 5; ❌ ｜ 类里写 值 += n; ✅
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：写成 `{ get; }`，然后类自己的方法去改它
//
//     public int 值 { get; }
//     public void 加(int n) { 值 += n; }        // ← 编译不过
//
// 你会看到：error CS0200: 无法为属性或索引器“学生代码.计数器.值”赋值 - 它是只读的
// 为什么：`{ get; }` 只生成了一个 getter，**根本没有 setter** —— 类自己也一样改不了。
// ⇒ 要"外面不能写、自己能写"，写成 `{ get; private set; }`。
//
// ⚠ 坑 2：以为加了 private set 外面就还能写
//
//     甲.值 = 5;
//
// 你会看到：error CS0272: 属性或索引器“学生代码.计数器.值”不能用在此上下文中，因为 set 访问器不可访问
// 为什么：`private set` 只对**类内部**开放。这正是封装想要的效果 ——
//         外面想动余额，只能走 `存` / `取`。
//
// ⚠ 坑 3：把属性当方法调
//
//     Console.WriteLine(甲.值());
//
// 你会看到：error CS1955: 不可调用的成员“学生代码.计数器.值”不能像方法一样使用。
// 为什么：属性是**值**不是方法 —— 方法带括号，属性不带。
//
// ⚠ 坑 4：构造函数前面多写了返回类型
//
//     public void 计数器(int 初值 = 0) { }      // ← 多了 void
//
// 你会看到：error CS0542: “计数器”: 成员名不能与它们的封闭类型相同
// 为什么：构造函数**没有返回类型**。写上 `void` 它就变成普通方法，
//         而 C# 不允许方法名和类名相同。
//
// ⚠ 坑 5：构造函数没有默认值，却写 `new 计数器()`
//        （而它是 `public 计数器(int 初值)` —— 少了 `= 0`）
// 你会看到：error CS7036: 未提供与“学生代码.计数器.计数器(int)”的所需参数“初值”对应的参数
// ⇒ 本课要求默认为 0，所以要写 `= 0`。
//
// ⚠ 坑 6：取钱的顺序写反了
//
//     public bool 取(int n) { 余额 -= n; return 余额 >= 0; }   // ← 先扣再检查
//
// 你会看到：**编译通过**，取 999 之后余额变成负数 —— 钱已经被扣掉了。
// 为什么：检查必须在**扣之前**。
// ⇒ 正确顺序：**先检查（n 合法 且 n <= 余额），通过了才扣**；取不出来就 `return false;`。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//   把骨架里两个类填完整（签名都给你了）：
//
//     1. class 计数器
//          public int 值 { get; private set; }     只读属性（外面读得到、改不了）
//          public 计数器(int 初值 = 0)             默认从 0 开始
//          public void 加(int n)                   累加
//          public void 重置()                      归零
//
//     2. class 银行账户
//          public int 余额 { get; private set; }
//          public 银行账户(int 初值 = 0)
//          public void 存(int n)
//          public bool 取(int n)                   钱不够 → false，且**余额一点不变**
//
//   期望结果：
//
//     计数器:   new 计数器() → 0 ; 加3加4 → 7 ; 重置 → 0 ; new 计数器(100) → 100 ; 加1 → 101
//     银行账户: 100 ; 存50 → 150 ; 取30 → True 且 120 ; 取999 → False 且 **120** ; 取120 → True 且 0
//
//   ⚠ 三件事：
//     · 属性必须是**只读入口**：外面只能读，要改只能通过类自己的方法
//     · ⚠ 取不出来的时候余额**必须一点都不变** —— 先检查、后扣钱
//     · 不用写 Main、不用写 using，入口由课程提供
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//
//   点界面上的「检查」。
//
//       dotnet run --project csharp/cs-lessons 检查 07
//       dotnet run --project csharp/cs-lessons 答案 07
//
//   ⚠ 常见症状对照：
//     · 报 CS0200 说"它是只读的" → 属性写成了 `{ get; }`，改成 `{ get; private set; }`
//     · 取 999 之后余额变负 → 先扣了再检查
//     · new 计数器(100) 拿不到 100 → 构造函数漏了往属性里赋值，或参数少了 `= 0`
//     · 报 CS0542 → 构造函数前面多写了返回类型

// ── 在下面写你的两个类 ──
class 计数器
{
    // 在这里写

}

class 银行账户
{
    // 在这里写

}
""";

    public static string 参考答案 => """
class 计数器
{
    public int 值 { get; private set; }

    public 计数器(int 初值 = 0)
    {
        值 = 初值;
    }

    public void 加(int n) => 值 += n;

    public void 重置() => 值 = 0;
}

class 银行账户
{
    public int 余额 { get; private set; }

    public 银行账户(int 初值 = 0)
    {
        余额 = 初值;
    }

    public void 存(int n)
    {
        if (n <= 0) return;          // 不合法就什么也不做
        余额 += n;
    }

    public bool 取(int n)
    {
        if (n <= 0 || n > 余额) return false;   // ⚠ 钱不够必须**拒绝并保持不变**
        余额 -= n;
        return true;
    }
}
""";

    [检查说明("两个类都写了")]
    public static CheckResult 检查_签名(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        foreach (var n in new[] { "class 计数器", "class 银行账户" })
            if (!去注释.Contains(n))
                return CheckResult.不过($"没找到「{n}」。");
        return CheckResult.过();
    }

    [检查说明("计数器：构造 / 加 / 重置 都对")]
    public static CheckResult 检查_计数器(string 代码)
    {
        var run = 调用(代码, """
            var __a = new 计数器();
            Console.WriteLine(__a.值);
            __a.加(3); __a.加(4);
            Console.WriteLine(__a.值);
            __a.重置();
            Console.WriteLine(__a.值);

            var __b = new 计数器(100);
            Console.WriteLine(__b.值);
            __b.加(1);
            Console.WriteLine(__b.值);
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "0", "7", "0", "100", "101" };
        var 行 = run.行;
        if (行.Count < 5) return CheckResult.不过($"应该打印 5 行，实际 {行.Count} 行。");
        for (int i = 0; i < 5; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是 {期望[i]}，实际是“{行[i].Trim()}”。\n" +
                    "      ⚠ 注意第 4 个：`new 计数器(100)` 要能从 100 开始\n" +
                    "        ⇒ 构造函数要有 `int 初值 = 0` 这个参数。");
        return CheckResult.过("默认 0 起 / 累加 / 重置 / 指定初值 都对");
    }

    [检查说明("银行账户：钱不够时**拒绝**且余额不变")]
    public static CheckResult 检查_账户(string 代码)
    {
        var run = 调用(代码, """
            var __a = new 银行账户(100);
            Console.WriteLine(__a.余额);
            __a.存(50);
            Console.WriteLine(__a.余额);
            Console.WriteLine(__a.取(30) + ":" + __a.余额);
            Console.WriteLine(__a.取(999) + ":" + __a.余额);
            Console.WriteLine(__a.取(120) + ":" + __a.余额);
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "100", "150", "True:120", "False:120", "True:0" };
        var 行 = run.行;
        if (行.Count < 5) return CheckResult.不过($"应该打印 5 行，实际 {行.Count} 行。");
        for (int i = 0; i < 5; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是“{期望[i]}”，实际是“{行[i].Trim()}”。\n" +
                    "      ⚠ 最要紧的是第 4 个：**取不出来的时候余额必须一点都不变**。\n" +
                    "        常见错法：先 `余额 -= n;` 再检查 —— 那时已经扣了。\n" +
                    "        正确顺序：**先检查，通过才扣**。");
        return CheckResult.过("存取 / 拒绝 / 正好取光 都对");
    }

    [检查说明("余额是只读属性（外面不能直接改）")]
    public static CheckResult 检查_只读(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("{ get;"))
            return CheckResult.不过(
                "没看到只读属性写法 `{ get; ... }`。\n" +
                "      这一课要练的就是封装：余额/值只能由类自己的方法改。");
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
