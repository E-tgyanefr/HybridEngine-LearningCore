using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("06", "第 06 课：字符串 —— 不可变与插值",
    "写两个方法：\n" +
    "  static string 反转(string s)              // 把字符串反转\n" +
    "  static int 数元音(string s)               // 数出其中的 aeiou（不分大小写）")]
public static class 第06课
{
    public static string 模板 => """
// 第 06 课：字符串 —— 不可变与插值
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    字符串：不可变是什么意思、插值怎么写
//
//  需要先会
//    第 04 课：List（遍历的兄弟写法）
//
// ─────────────────────────────────────────────────────
//  先介绍：字符串是"改不动"的
// ─────────────────────────────────────────────────────
//
// 数字能改：
//
//     int 分 = 60;
//     分 = 分 + 10;        // 分 变成 70
//
// 字符串长得很像，但**行为完全不同**：
//
//     string s = "abc";
//     s.ToUpper();              // ← 这行什么也没发生
//     Console.WriteLine(s);     ⇒ 还是 abc
//
// 因为 C# 的字符串是**不可变（immutable）**的：
// **一旦造出来，这个字符串本身永远不会变。**
//
// 所有"看起来在改字符串"的方法（ToUpper、Replace、Substring、Trim…），
// 真实做的事都是：**照着原串造一个新的，然后把新的还给你**。
// 你不管那个返回值，它就静静地被扔掉了。
//
//     s = s.ToUpper();          // ← 把新串接回来，这才是"改"
//
// ⚠ 为什么要设计得这么"别扭"：字符串到处被共享（变量、参数、字典的键…），
//   如果能就地修改，任何一处改动都会**悄悄影响所有引用它的人**。
//   不可变换来的是"拿到一个字符串就可以放心用"；代价就是**忘了接收返回值**。
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
// ① 三个"返回新串"的常用方法
//
//     string s = "  Hello  ";
//     s.Trim()             ⇒ "Hello"        （去掉两头空白）
//     s.ToUpper()          ⇒ "  HELLO  "
//     s.Replace("l", "L")  ⇒ "  HeLLo  "
//
//   ⚠ 三行里的 `s` 本身**都没变**。要用结果就得接住：`s = s.Trim();`
//
// ② 长度和单个字符
//
//     string s = "abc";
//     Console.WriteLine(s.Length);         ⇒ 3     ⚠ 属性，不加括号
//     Console.WriteLine(s[0]);             ⇒ a     （下标从 0 开始，类型是 char）
//     Console.WriteLine(s.IndexOf('z'));   ⇒ -1    （找不到返回 -1，不是 0）
//
//   ⚠ `s.Length()` 是编译错误 —— `Length` 是**属性**（一个值），不是方法（一段行为）。
//     Java 那边恰好相反，是 `s.length()`。两门语言在这里不一样。
//
// ③ 拼串 vs 插值
//
//     string 名字 = "小明";  int 年龄 = 9;
//
//     Console.WriteLine("你好，" + 名字 + "，今年 " + 年龄 + " 岁");
//     Console.WriteLine($"你好，{名字}，今年 {年龄} 岁");        // ← 推荐
//
//   结果一样，插值读起来清楚得多，也少犯括号和空格的错。
//   ⚠ 插值里还能带格式：`{值:F2}` 表示保留两位小数。
//         Console.WriteLine($"{3.14159:F2}");      ⇒ 3.14
//   ⚠ `+` 是**从左往右**算的，所以下面两个不一样：
//         Console.WriteLine("a" + 1 + 2);     ⇒ a12
//         Console.WriteLine(1 + 2 + "a");     ⇒ 3a
//
// ④ 字符串比"内容"，不比"地址"
//
//     string a = "abc";
//     string b = new string(new char[] { 'a', 'b', 'c' });   // 另一个对象
//     Console.WriteLine(a == b);      ⇒ True    ← 比的是**内容**
//
//   ⚠ 这一点和 Java 不同：Java 的 `==` 比引用，要 `.Equals` 才比内容。
//     C# 的 `string` 把 `==` 重载成了**逐字符比内容**，可以放心用。
//
// ⑤ 本课要写的两个方法：反转、数元音
//
//   反转最省事的写法（要 using System.Linq，本课顶部已经带了）：
//
//     new string(s.Reverse().ToArray())
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：把 Length 当方法
//
//     Console.WriteLine(s.Length());
//
// 你会看到：error CS1955: 不可调用的成员“string.Length”不能像方法一样使用。
// 为什么：属性是"值"，方法是"行为"，调用语法不同。
//         Java 是 `s.length()`，从 Java 过来的人几乎必踩。⇒ 写成 `s.Length`。
//
// ⚠ 坑 2："改了"字符串，其实改的是被扔掉的新串
//
//     string s = "abc";
//     s.ToUpper();
//     Console.WriteLine(s);         ⇒ abc
//
// 你会看到：**不报错、不警告**，只是输出和你以为的不一样。
// 为什么：ToUpper 返回的是**新串**，没接住就被扔了。
// ⇒ 记住这条规律：字符串方法**全都有返回值**，要接住。
//
// ⚠ 坑 3：想按 Java / C 的习惯改一个字符
//
//     string s = "abc";
//     s[0] = 'A';
//
// 你会看到：error CS0200: 无法为属性或索引器“string.this[int]”赋值 - 它是只读的
// 为什么：不可变 ⇒ 索引器只提供 get，没有 set。
// ⇒ 要改只能造新串，例如 `s = "A" + s.Substring(1);`
//
// ⚠ 坑 4：数元音忘了处理大小写
//
//     if ("aeiou".IndexOf(c) >= 0) n++;      // "AEIOU" 会被数成 0
//
// 你会看到：**编译通过**，小写串对、大写串错。
// 为什么：`'A'` 和 `'a'` 是两个不同的字符。
// ⇒ 先 `s.ToLower()` 再数。
//   （冷知识：`ToLower()` 按**当前区域设置**转换，土耳其语环境下
//     `"I".ToLower()` 得到的是 `ı` 而不是 i。本课不涉及，知道有这回事就行。）
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//   写两个方法：
//
//     1. static string 反转(string s)
//          "abc" → "cba"      "" → ""（空串，⚠ 不要返回 null）
//          提示：`new string(s.Reverse().ToArray())`
//
//     2. static int 数元音(string s)
//          数出 a e i o u 的个数，**不分大小写**
//          提示：先 `s.ToLower()`，再用 `"aeiou".IndexOf(c) >= 0` 判断
//
//   期望结果：
//
//     反转:    "" → （空）   "a" → a   "abc" → cba   "你好世界" → 界世好你
//     数元音:  "" → 0   "xyz" → 0   "Hello" → 2   "AEIOU" → 5   "Programming" → 3
//
//   ⚠ 三件事：
//     · 两个签名**都给你了**，你只写方法体
//     · 空串要返回**空串**（`new string(空数组)` 就是空串，不会报错）
//     · 不用写 Main、不用写 using，入口由课程提供
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//
//   点界面上的「检查」。检查会喂空串、单字符、中文、大小写混排。
//
//       dotnet run --project csharp/cs-lessons 检查 06
//       dotnet run --project csharp/cs-lessons 06
//       dotnet run --project csharp/cs-lessons 答案 06
//
//   ⚠ 常见症状对照：
//     · 反转后还是原串 → 忘了接住返回值
//     · "AEIOU" 数出 0 → 忘了 `ToLower()`
//     · 报 CS1955 提到 Length → 写成了 `s.Length()`
//     · 报 CS0200 提到"它是只读的" → 想给 `s[0]` 赋值，字符串是不可变的

// ── 在下面写你的两个方法 ──
static string 反转(string s)
{
    // 在这里写

}

static int 数元音(string s)
{
    // 在这里写

}
""";

    public static string 参考答案 => """
static string 反转(string s)
{
    return new string(s.Reverse().ToArray());
}

static int 数元音(string s)
{
    int n = 0;
    foreach (var c in s.ToLower())
        if ("aeiou".IndexOf(c) >= 0) n++;
    return n;
}
""";

    [检查说明("两个方法都写了")]
    public static CheckResult 检查_签名(string 代码)
    {
        foreach (var n in new[] { "反转", "数元音" })
            if (!去掉注释(代码).Contains(n))
                return CheckResult.不过($"没找到「{n}」这个方法。");
        return CheckResult.过();
    }

    [检查说明("反转 对不同字符串都对（含空串与中文）")]
    public static CheckResult 检查_反转(string 代码)
    {
        var run = 调用(代码, """
            Console.WriteLine("[" + 学生代码.反转("") + "]");
            Console.WriteLine(学生代码.反转("a"));
            Console.WriteLine(学生代码.反转("abc"));
            Console.WriteLine(学生代码.反转("你好世界"));
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "[]", "a", "cba", "界世好你" };
        var 行 = run.行;
        if (行.Count < 4) return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是“{期望[i]}”，实际是“{行[i].Trim()}”。\n" +
                    "      ⚠ 空字符串要返回空字符串（别返回 null）。\n" +
                    "      最省事的写法：`new string(s.Reverse().ToArray())`");
        return CheckResult.过("空串 / 单字符 / 英文 / 中文 都对");
    }

    [检查说明("数元音 不分大小写")]
    public static CheckResult 检查_数元音(string 代码)
    {
        var run = 调用(代码, """
            foreach (var __s in new[] { "", "xyz", "Hello", "AEIOU", "Programming" })
                Console.WriteLine(学生代码.数元音(__s));
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "0", "0", "2", "5", "3" };
        var 行 = run.行;
        if (行.Count < 5) return CheckResult.不过($"应该打印 5 行，实际 {行.Count} 行。");
        for (int i = 0; i < 5; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是 {期望[i]}，实际是“{行[i].Trim()}”。\n" +
                    "      ⚠ 重点：**不分大小写** —— 先 `s.ToLower()` 再数。\n" +
                    "        （忘了这步的话“AEIOU”会数成 0。）");
        return CheckResult.过("空串 / 无元音 / 小写 / 大写 / 混合 都对");
    }

    [检查说明("没把 Length 当方法调用")]
    public static CheckResult 检查_Length用法(string 代码)
    {
        // ⚠ 这条守的是课程里讲的那个坑：`s.Length()` 编译不过
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains(".Length()"))
            return CheckResult.不过(
                "写了 `s.Length()` —— C# 里 Length 是**属性不是方法**，加括号编译不过。\n" +
                "      正确写法：`s.Length`\n" +
                "      （Java 那边才是 `s.length()` —— 两门语言在这里不一样。）");
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
