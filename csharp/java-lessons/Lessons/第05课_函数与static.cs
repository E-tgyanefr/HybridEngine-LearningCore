// 第 05 课：函数与 static —— 把一段计算打包起来，反复使用
//
// ══ 这一课要教什么 ══
// 方法（Java 里"函数"就叫**方法**）是「给一段计算起个名字」。
// 有了它，同一个计算不用抄第二遍，而且一段代码能**对不同输入给出不同结果**。
//
// 但 Java 的方法比 C++ / Python 多一道门槛：**static**。
// 原因是 Java 里所有方法都住在类里面，而「调用一个方法」有两种完全不同的方式：
//   · 写了 static 的方法属于**类** —— 直接写名字就能调用
//   · 不写 static 的方法属于**对象** —— 必须先 new 一个对象出来
// 而 main 是 static 的：程序刚启动的那一刻一个对象都还没有，
// 所以从 main 里直接写名字调用的方法，**也必须是 static 的**。
// 漏写 static，就会看到：无法从静态上下文中引用非静态 方法 加倍(int)
//
// ══ 检查设计说明（给维护者）══
// 两个检查点，一个防"没学到"，一个防"写错了却看不出来"：
//
//   ① 核心：方法必须真的写了 static —— 检查_方法是static的 用两个角度一起看：
//      · 语法角度：正则找「static … 方法名(」
//      · 编译角度：把学生的方法放进**课程提供的 static main** 里调用，
//        编译不过 + 报「静态上下文」→ 就是漏了 static。
//
//   ② 方法要真的用参数算 —— 四组不同的输入必须得到四个不同的答案。
//      写死 `return 6;` 能蒙对一组用例，蒙不过四组。
//
// ⚠ 给维护者（这一条是本课设计的关键，别改成"让学生自己写 main"）：
//
//   同事实测 + 我在本机（openjdk 25.0.3）复核过：**JDK 25 放宽了 main 的写法**。
//   下面这段**能编译、能跑、还能打印出 8**：
//
//       public class Main {
//           int 加倍(int n) { return n * 2; }      // ← 没有 static
//           void main(String[] args) {             // ← main 也没有 static
//               System.out.println(加倍(4));
//           }
//       }
//
//   也就是说：只要学生自己写 main，"漏写 static" 在他机器上**根本不会报错**。
//   本课的做法是把 main 换成课程里写死的 public static void main(String[] args)，
//   再从那个 static 上下文里调用学生的方法 —— 这条错误就又稳定地暴露出来了
//   （本机实测报：错误: 无法从静态上下文中引用非静态 方法 加倍(int)）。
//   代价是学生这一课不写 main（和 C++ 课程第 05 课同一个形状：只写方法）。
//
//   另一个坑：本机 locale 是中文，javac 的报错**是中文的**。
//   所以下面匹配报错时中英两套都认（"静态上下文" / "static context"），
//   换一台英文 locale 的机器也不会失效。

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("05", "第 05 课：函数与 static —— 把计算打包，想用几次用几次",
    "1. 写 static int 加倍(int n)：返回 n 的两倍\n" +
    "2. 写 static int 最大值(int a, int b)：返回较大的那个\n" +
    "3. 写 static String 星星(int n)：返回 n 个星号组成的字符串；n 是 0 时返回空串\n" +
    "4. 写 static void 打印星星(int n)：打印 n 个星号，不换行；n 是 0 时什么都不打印\n" +
    "⚠ 四个方法都必须写 static（本课重点）。不用写 class、不用写 main —— 课程替你补上")]
public static class 第05课
{
    public static string 模板 => """
// 第 05 课：函数与 static —— 把计算打包，想用几次用几次
//
// 这一课讲一件事：**给一段计算起个名字**，以后叫名字就行。
//
// ─────────────────────────────────────────────────────
//  先介绍：为什么要给计算起名字
// ─────────────────────────────────────────────────────
//
// 同一个式子你会写很多遍：
//
//     int a = 6 * 2;
//     int b = 7 * 2;
//     int c = n * 2;
//
// 抄三遍还行，抄三十遍就有两个问题：打错一个字就错一处；
// 想改成乘 3，得挨个改。
//
// ─────────────────────────────────────────────────────
//  一、方法长这样，四样都要写
// ─────────────────────────────────────────────────────
//
//     static int 加倍(int n) {
//         return n * 2;
//     }
//
//     ↑      ↑    ↑    ↑
//     静态   返回 名字 参数
//
//     static      属于类，不用 new 就能调（这一课的重点）
//     int         交出去的是整数（什么都不交就写 void）
//     加倍        名字（自己起）
//     (int n)     要一个整数，在里面叫 n
//     return      把结果交出去
//
// 用起来：
//
//     System.out.println(加倍(4));       ⇒ 8
//
// ⚠ 方法名后面**不要写分号**（这一点和变量声明相反）。
//
// ─────────────────────────────────────────────────────
//  二、static：Java 比别的语言多的一道门槛
// ─────────────────────────────────────────────────────
//
// Java 里所有方法都住在类里面，而「调用一个方法」有两种方式：
//
//     写了 static   属于**类**   —— 直接写名字就能调用
//     不写 static   属于**对象** —— 必须先 new 一个对象出来
//
// 而 main 是 static 的：程序刚启动时一个对象都还没有，
// 所以从 main 里直接写名字调用的方法，**也必须是 static 的**。
//
// 漏写 static，你会看到：
//
//     错误: 无法从静态上下文中引用非静态 方法 加倍(int)
//
// ─────────────────────────────────────────────────────
//  三、void：只做事，不交值
// ─────────────────────────────────────────────────────
//
//     static void 打印星星(int n) {
//         for (int i = 0; i < n; i++) {
//             System.out.print("*");        ← print 打完**不换行**
//         }
//     }
//
// ⚠ 分清楚这两件事：
//
//     System.out.println(n * 2);     打到屏幕上 —— 方法本身什么都没交出去
//     return n * 2;                  交给调用它的人 —— 能被拿去继续算
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：声明了返回类型，却没有 return
// ─────────────────────────────────────────────────────
//
//     static int 加倍(int n) {
//         System.out.println(n * 2);        ← 打印了，但什么都没交出去
//     }
//
// 你会看到（javac 原文）：
//
//     错误: 缺少返回语句
//
// 为什么：你答应了「这个方法交出一个 int」，可它走到末尾也没交出任何东西。
//   ⚠ 注意这里的因果：**打印不等于返回**。
//
// ⇒ 有返回类型的方法，**每条可能的路径最后都要 return**。
//   确实只做事不交值，就把返回类型改成 void。
//
//   ⚠ 顺便记住一件容易吃亏的事（**新版 JDK 25 起**）：
//     如果你**自己**写 main，而且写成不带 static 的 void main(String[] args)，
//     那么它里面的非 static 方法调用**不会报错**，程序照样跑。
//     但换到 JDK 21 或更早、或者把代码交到别的环境，就会撞墙。
//     ⇒ 「在这台机器上能跑」不等于写对了。该写 static 就写 static。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   数组、集合（一次装很多东西）→ 第 06 课。
//   类与对象（不写 static 的那种方法）→ 第 08 课。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
// ⚠ 只写**方法**：不用写 class、不用写 main、不用写 import —— 课程会替你补上。
// ⚠ 四个方法**都必须写 static**（本课重点）。
//
//   1. static int 加倍(int n)              返回 n 的两倍
//   2. static int 最大值(int a, int b)      返回较大的那个
//   3. static String 星星(int n)           返回 n 个星号组成的字符串；n 是 0 时返回空串 ""
//   4. static void 打印星星(int n)         打印 n 个星号，**不换行**；n 是 0 时什么都不打印
//
// 对照着分辨这两件事：
//     星星      是**交出**结果（有返回类型 String，要 return）
//     打印星星  是**做事**（返回类型 void，用 System.out.print，不写 return）
//
// 自检几组：
//     加倍(3) 6 / 加倍(10) 20 / 加倍(0) 0 / 加倍(-4) -8
//     最大值(3,7) 7 / 最大值(7,3) 7 / 最大值(5,5) 5
//     星星(3) "***" / 星星(0) ""（空串，不是 "null"）；打印星星(3) 打出 *** 且**不换行**
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/java-lessons 检查 05
//
//   ⚠ 有一条检查会拿**四组不同的输入**去调每一个方法。
//     如果你的方法不管给什么都返回同一个答案，就是在那儿露馅的。
//
// ── 在下面写 ──
static int 加倍(int n)
{
}

static int 最大值(int a, int b)
{
}

static String 星星(int n)
{
}

static void 打印星星(int n)
{
}
""";

    public static string 参考答案 => """
// 1. 加倍：一个参数、一个返回值
static int 加倍(int n) {
    return n * 2;              // return 把结果交给调用它的人
}

// 2. 最大值：两个参数，用 if 比一下
static int 最大值(int a, int b) {
    if (a > b) {
        return a;              // a 大就交 a
    }
    return b;                  // 走到这里说明 a 没比 b 大
}

// 3. 星星：**返回**一个字符串（不是打印出来）
static String 星星(int n) {
    String 结果 = "";
    for (int i = 0; i < n; i++) {
        结果 = 结果 + "*";      // n 是 0 时循环一次都不进，结果就是空串
    }
    return 结果;
}

// 4. 打印星星：void —— 只做事，不交值
//    所以它没有 return，也不用写 return;
//    注意用 print 而不是 println：题目要求**不换行**
static void 打印星星(int n) {
    for (int i = 0; i < n; i++) {
        System.out.print("*");
    }
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("加倍：四组不同的输入都要算对（不是永远返回同一个答案）")]
    public static JavaCheckResult 检查_加倍(string 代码)
    {
        var run = 编译并调用(代码, "加倍(3)", "加倍(10)", "加倍(0)", "加倍(-4)");
        if (!run.编译通过) return 编译不通过(run);

        var 实际 = 行们(run);
        if (实际.Count == 4 && 实际.Distinct().Count() == 1)
            return JavaCheckResult.不过(
                "输入 3、10、0、-4，得到的却是**同一个答案**：" + 实际[0] + "\n" +
                "      这正是「方法没有真的用参数算」的典型特征 —— 它多半返回了一个固定值：\n" +
                "          static int 加倍(int n) { return 6; }        ✗ 6 是写死的，跟 n 没关系\n" +
                "      参数 n 就是「这一次的输入」，要拿它去算：\n" +
                "          static int 加倍(int n) { return n * 2; }    ✓\n" +
                "      顺便记住：判断一个方法写对没有，就是**换几组输入多试几次** ——\n" +
                "                一组用例对了，不代表方法对了。");

        var 结果 = 比对(run, ("加倍(3)", "6"), ("加倍(10)", "20"), ("加倍(0)", "0"), ("加倍(-4)", "-8"));
        if (结果.Ok) return 结果;
        return JavaCheckResult.不过(结果.Detail + "\n" +
            "      检查一下算式：加倍 要做的是「n 的两倍」，写成 n * 2。\n" +
            "      （负数也要对：-4 的两倍是 -8。）");
    }

    [Java检查说明("最大值：两个数的大小关系反过来也要对")]
    public static JavaCheckResult 检查_最大值(string 代码)
    {
        var run = 编译并调用(代码, "最大值(3, 7)", "最大值(7, 3)", "最大值(5, 5)", "最大值(-2, -9)");
        if (!run.编译通过) return 编译不通过(run);

        var 结果 = 比对(run, ("最大值(3, 7)", "7"), ("最大值(7, 3)", "7"),
                             ("最大值(5, 5)", "5"), ("最大值(-2, -9)", "-2"));
        if (结果.Ok) return 结果;
        return JavaCheckResult.不过(结果.Detail + "\n" +
            "      注意两个数**反过来**的情况也要对：\n" +
            "          if (a > b) return a;    ← 少了这一句，最大值(3, 7) 就会得到 3\n" +
            "          return b;               ← 剩下的情况交 b\n" +
            "      两个数相等时（5 和 5）返回哪个都对，所以那种情况不算错。");
    }

    [Java检查说明("星星：n 个星号；n 是 0 时交出空字符串")]
    public static JavaCheckResult 检查_星星(string 代码)
    {
        // 为什么要给结果套上方括号：**空字符串打印出来就是一行空白**，
        // 而 JavaRunner 会把空行丢掉（它只收非空的行），
        // 光看输出分不清「返回了空串」和「什么都没打印」。套上 "A[" … "]" 就看得见了。
        var run = 编译并调用(代码, @"""A["" + 星星(3) + ""]""",
                                  @"""B["" + 星星(1) + ""]""",
                                  @"""C["" + 星星(0) + ""]""");
        if (!run.编译通过)
        {
            // 「方法声明成 void 了」在这里有个非常典型的报错：把 void 塞进字符串拼接
            //   实测原文：错误: 此处不允许使用 '空' 类型
            // 学生看到「'空' 类型」是一头雾水的，必须翻译。
            if (含(run.编译信息, "not allowed here", "不允许使用"))
                return JavaCheckResult.不过(
                    "星星 现在**交不出值** —— 编译器说这里不允许使用「空（void）」类型。\n" +
                    "      多半是它的返回类型写成了 void：\n" +
                    "          static void 星星(int n) { ... }        ✗ 只做事，不交值\n" +
                    "          static String 星星(int n) { ... }      ✓ 交出一个字符串\n" +
                    "      ⚠ 分清这两件事：\n" +
                    "          在方法里 System.out.print(\"*\")  → 打到屏幕上，方法本身什么都没交出\n" +
                    "          return 结果;                    → 把结果交给调用它的人\n" +
                    "      这一题要的是**返回**（第 4 题的 打印星星 才是打印）。");
            return 编译不通过(run);
        }

        var 结果 = 比对(run, ("星星(3)", "A[***]"), ("星星(1)", "B[*]"), ("星星(0)", "C[]"));
        if (结果.Ok) return 结果;
        return JavaCheckResult.不过(结果.Detail + "\n" +
            "      检查两件事：\n" +
            "        · 循环的边界：for (int i = 0; i < n; i++)  ✓ 转 n 次\n" +
            "                       for (int i = 0; i <= n; i++) ✗ 会多一个星号\n" +
            "        · n 是 0 的时候应该**返回空串**（循环一次都不进，结果就是 \"\"）");
    }

    [Java检查说明("void 方法：只打印，不交出值")]
    public static JavaCheckResult 检查_打印星星(string 代码)
    {
        // void 方法不能塞进 System.out.println(...)，只能当**语句**调用，
        // 所以这里用「标记 + 换行」的办法看它到底打印了什么。
        var run = 编译并调用语句(代码,
            @"System.out.print(""A|"");",
            @"打印星星(3);",
            @"System.out.println();",
            @"System.out.print(""B|"");",
            @"打印星星(0);",
            @"System.out.println();");
        if (!run.编译通过) return 编译不通过(run);

        var 实际 = 行们(run);
        if (实际.Count != 2)
            return JavaCheckResult.不过(
                "打印星星 打出来的内容和预期不一样。\n" +
                "      期望是两行（每行以 A| / B| 开头），实际是：\n" +
                "          " + (实际.Count == 0 ? "（没有任何输出）" : string.Join(" ⏎ ", 实际)) + "\n" +
                "      最常见的原因：用了 println —— 它每打一个星号就换一次行。\n" +
                "          System.out.print(\"*\");     ✓ 不换行（题目要的是这个）\n" +
                "          System.out.println(\"*\");   ✗ 会换行\n" +
                "      如果一行都没有，检查循环有没有真的执行：for (int i = 0; i < n; i++)");

        if (实际[0] == "A|***" && 实际[1] == "B|")
            return JavaCheckResult.过("打印星星(3) 打出 ***；打印星星(0) 什么都没打 —— void 方法用对了");

        return JavaCheckResult.不过(
            "打印星星 的结果不对：\n" +
            "      打印星星(3) 之后那一行是：" + 显示(实际[0]) + "，应该是「A|***」\n" +
            "      打印星星(0) 之后那一行是：" + 显示(实际[1]) + "，应该是「B|」（什么都不打印）\n" +
            "      检查循环边界：\n" +
            "          for (int i = 0; i < n; i++)      ✓ n 个星号\n" +
            "          for (int i = 0; i <= n; i++)     ✗ n+1 个\n" +
            "      n 是 0 时循环一次都不该进 —— 这也正是 i < n 的含义。");
    }

    [Java检查说明("每个方法前面都要写 static（本课重点）")]
    public static JavaCheckResult 检查_方法是static的(string 代码)
    {
        // ⚠ 本课的核心检查点。用**两个角度**一起看，因为单靠任何一个都有缝：
        //
        //   角度一（语法）：正则找「static … 方法名(」。
        //     好处是不起编译器、立刻能报；坏处是它只认字面，认不出语义。
        //   角度二（编译）：把学生的方法放进**课程提供的 static main** 里调用一次。
        //     漏 static 时 javac 会报「无法从静态上下文中引用非静态 方法 …」。
        //     这是真正的证据 —— 而且它不依赖学生自己的 main 怎么写，
        //     所以 JDK 25 那条"放宽 main 写法"的口子在这里堵上了（见文件头说明）。
        var 缺 = new List<string>();
        string 净 = 去注释(代码);        // ⚠ 先去掉注释：注释里出现 "static" 会骗过正则（实测踩过）
        foreach (var 名字 in 方法名们)
            if (!Regex.IsMatch(净, @"\bstatic\b[^;{}()]*?\b" + Regex.Escape(名字) + @"\s*\("))
                缺.Add(名字);

        if (缺.Count > 0)
            return JavaCheckResult.不过(
                "这些方法没有写 static：" + string.Join("、", 缺) + "\n" +
                "      " + 漏static说明);

        // 四个方法都从**课程的 static main** 里调一次。
        // 注意 打印星星 是 void，不能塞进 println —— 只能当语句调用。
        var run = 编译并调用语句(代码,
            @"System.out.println(加倍(1));",
            @"System.out.println(最大值(1, 2));",
            @"System.out.println(星星(1));",
            @"打印星星(1);");
        if (!run.编译通过)
        {
            // 语法上写了 static，编译却还报静态上下文 —— 只可能是别的写法（例如内部类）。
            // 这种情况交给通用翻译，别硬说成"你漏了 static"。
            if (含(run.编译信息, "static context", "静态上下文"))
                return JavaCheckResult.不过("编译没能通过：\n      " + 漏static说明);
            return 编译不通过(run);
        }

        return JavaCheckResult.过("加倍、最大值、星星、打印星星 都写了 static —— 从 static 的 main 里调得到它们");
    }

    [Java检查说明("四个方法是不是都写了（名字、返回类型、参数都要对）")]
    public static JavaCheckResult 检查_方法都写了吗(string 代码)
    {
        // 这一条只管「有没有写、签名对不对」，报告一次列全，
        // 免得学生一个方法一个字地试错。static 由上面那条专门负责。
        var 签名们 = new (string 名字, string 正则, string 样子)[]
        {
            ("加倍",     @"int\s+加倍\s*\(\s*int\s+\w+\s*\)",            "static int 加倍(int n)"),
            ("最大值",   @"int\s+最大值\s*\(\s*int\s+\w+\s*,\s*int\s+\w+\s*\)", "static int 最大值(int a, int b)"),
            ("星星",     @"String\s+星星\s*\(\s*int\s+\w+\s*\)",          "static String 星星(int n)"),
            ("打印星星", @"void\s+打印星星\s*\(\s*int\s+\w+\s*\)",        "static void 打印星星(int n)"),
        };

        var 缺 = new List<string>();
        string 净 = 去注释(代码);
        foreach (var (名字, 正则, 样子) in 签名们)
            if (!Regex.IsMatch(净, 正则)) 缺.Add($"      {名字}  —— 要写成：{样子}");

        if (缺.Count == 0)
            return JavaCheckResult.过("四个方法的返回类型和参数都写对了");

        return JavaCheckResult.不过(
            "这几个方法没有找到（或者签名和题目不一样）：\n" +
            string.Join("\n", 缺) + "\n" +
            "      一个方法的四个部分缺一不可：\n" +
            "          返回类型  方法名 (参数类型 参数名) { … }\n" +
            "        · 返回类型：交出整数写 int，交出文字写 String，什么都不交写 void\n" +
            "        · 参数：要几个就写几个，每个都要写**类型**（int n，不是只写 n）\n" +
            "        · 方法名要和题目一模一样（Java **区分大小写**）\n" +
            "      顺便记住：方法名后面的括号和花括号都不能省，而且末尾不需要分号。");
    }

    // ══════════════════ 小工具 ══════════════════

    private static readonly string[] 方法名们 = { "加倍", "最大值", "星星", "打印星星" };

    /// <summary>漏写 static 时给学生看的话（本课的核心提示）。</summary>
    private const string 漏static说明 =
        "你的方法没有写 static，所以从 main 里调不到它。\n" +
        "      main 是 static 的，程序刚启动时一个对象都还没有 ——\n" +
        "      它只能直接调用**同样属于类**的方法，也就是 static 方法。\n" +
        "      不写 static 的方法属于**对象**，必须先 new 一个出来才能用：\n" +
        "          Main m = new Main();\n" +
        "          m.加倍(4)                     ← 这样才调得到非 static 的方法\n" +
        "\n" +
        "      怎么改（挑一个）：\n" +
        "        ① 给方法加上 static —— 纯计算的方法就该这样，本课用这种：\n" +
        "               static int 加倍(int n) { return n * 2; }\n" +
        "        ② 或者先创建对象，再用对象去调（第 08 课会讲）：\n" +
        "               Main m = new Main();\n" +
        "               System.out.println(m.加倍(4));\n" +
        "\n" +
        "      ⚠ 顺便记住一件容易吃亏的事：**新版 JDK（25 起）放宽了 main 的写法**。\n" +
        "        如果你自己写 main，而且写成不带 static 的 void main(String[] args)，\n" +
        "        那么它里面的非 static 方法调用**不会报错**，程序照样跑。\n" +
        "        但换到 JDK 21 或更早、或者把代码交到别的环境，就会撞墙。\n" +
        "        所以「在这台机器上能跑」不等于写对了 —— 该写 static 就写 static。";

    /// <summary>把一组「表达式」拼成 main 里的一段代码（每条打印一行）。</summary>
    private static JavaRun 编译并调用(string 代码, params string[] 表达式们)
    {
        var sb = new StringBuilder();
        foreach (var e in 表达式们) sb.Append("        System.out.println(").Append(e).Append(");\n");
        return JavaCheck.编译并运行(代码, null, sb.ToString());
    }

    /// <summary>把一串语句原样放进 main（void 方法只能这么调）。</summary>
    private static JavaRun 编译并调用语句(string 代码, params string[] 语句们)
    {
        var sb = new StringBuilder();
        foreach (var s in 语句们) sb.Append("        ").Append(s).Append('\n');
        return JavaCheck.编译并运行(代码, null, sb.ToString());
    }

    /// <summary>编译没过时：先给针对性说明，没有就交给通用翻译。</summary>
    private static JavaCheckResult 编译不通过(JavaRun run)
    {
        string 信息 = run.编译信息;

        // ① 漏写 static —— 本课的核心错误
        if (含(信息, "static context", "静态上下文"))
            return JavaCheckResult.不过(漏static说明);

        // ② 声明了返回类型，方法体里却没有 return
        //    实测原文：错误: 缺少返回语句
        if (含(信息, "missing return statement", "缺少返回语句"))
            return JavaCheckResult.不过(
                "方法声明了要交出结果，但方法体里**没有 return**。\n" +
                "      编译器把这句提醒藏在编译输出里：「缺少返回语句」。\n" +
                "      多半是你把结果打印出来了，而不是交出去：\n" +
                "          static int 加倍(int n) { System.out.println(n * 2); }   ✗ 打印了，没交出\n" +
                "          static int 加倍(int n) { return n * 2; }                ✓\n" +
                "      这两句做的是完全不同的事：\n" +
                "          System.out.println(n * 2);   ← 打到屏幕上，方法本身什么都没交出\n" +
                "          return n * 2;                ← 把结果交给调用它的人\n" +
                "      有返回类型的方法，**每条可能的路径最后都要 return**。");

        // ③ 方法名 / 参数对不上（javac 的 cannot find symbol 也走这里）
        if (含(信息, "cannot find symbol", "找不到符号"))
            return JavaCheckResult.不过(
                "编译器不认识这个名字 —— 通常是有个方法没写、或者名字/参数和题目不一样。\n" +
                "      这一课要求的四个方法（一个字符都不能差，Java 区分大小写）：\n" +
                "          static int 加倍(int n)\n" +
                "          static int 最大值(int a, int b)\n" +
                "          static String 星星(int n)\n" +
                "          static void 打印星星(int n)\n" +
                "      两种常见写法错误：\n" +
                "        · 忘了写参数类型：加倍(n)              ✗ 要写 加倍(int n)\n" +
                "        · 参数个数不对：加倍(3, 4)             ✗ 加倍 只要一个参数\n" +
                "      ⚠ 也别自己写 class / main —— 课程已经替你写好了，\n" +
                "        把方法直接写在答题文件里就行（写了反而会把名字挡在里面）。");

        // ④ 参数个数/类型对不上（javac 单独一条报错）
        if (含(信息, "cannot be applied to given types", "应用到给定类型"))
            return JavaCheckResult.不过(
                "调用时给的参数和方法声明的对不上（个数或类型不一样）。\n" +
                "      这一课要求的参数：\n" +
                "          加倍(int n)              → 一个整数\n" +
                "          最大值(int a, int b)      → 两个整数\n" +
                "          星星(int n) / 打印星星(int n) → 一个整数");

        // ⑤ 把 void 方法的调用当成了值（塞进 println / 字符串拼接）
        //    实测原文：错误: 此处不允许使用 '空' 类型
        if (含(信息, "not allowed here", "不允许使用"))
            return JavaCheckResult.不过(
                "有个方法**交不出值**，但检查程序把它当值用了（编译器说这里不允许使用「空（void）」类型）。\n" +
                "      也就是说它的返回类型是 void：\n" +
                "          static void 星星(int n) { ... }        ✗ 只做事，不交值\n" +
                "          static String 星星(int n) { ... }      ✓ 交出一个字符串\n" +
                "      ⚠ 分清这两件事：\n" +
                "          在方法里用 System.out.print(...)  → 打到屏幕上，方法本身什么都没交出\n" +
                "          return 结果;                     → 把结果交给调用它的人\n" +
                "      这一课只有 打印星星 应该是 void；加倍、最大值、星星 都要交出值。");

        return JavaCheck.编译失败说明(run);
    }

    /// <summary>把实际打印的行和期望逐条比对。</summary>
    private static JavaCheckResult 比对(JavaRun run, params (string 说明, string 期望)[] 用例们)
    {
        var 实际 = 行们(run);
        if (实际.Count != 用例们.Length)
            return JavaCheckResult.不过(
                $"应该打印 {用例们.Length} 行（每个用例一行），实际打印了 {实际.Count} 行：\n" +
                "          " + (实际.Count == 0 ? "（没有任何输出）" : string.Join(" ⏎ ", 实际)) + "\n" +
                "      常见原因：方法里忘了 return（那就没有值可打印），" +
                "或者方法自己在打印东西（多出来的行会混进结果里）。");

        var 错 = new List<string>();
        for (int i = 0; i < 用例们.Length; i++)
            if (实际[i] != 用例们[i].期望)
                错.Add($"      {用例们[i].说明}：得到 {显示(实际[i])}，应该是 {显示(用例们[i].期望)}");

        if (错.Count == 0) return JavaCheckResult.过($"全部 {用例们.Length} 个用例都正确");
        return JavaCheckResult.不过($"通过 {用例们.Length - 错.Count}/{用例们.Length}：\n" + string.Join("\n", 错));
    }

    private static List<string> 行们(JavaRun run) => run.行.Select(l => l.Trim()).ToList();

    /// <summary>
    /// 去掉注释，再做语法（正则）检查。
    ///
    /// ⚠ 这一步是实测踩出来的：反面测试时我在注释里写了「四个方法都没写 static」，
    ///   正则 `\bstatic\b[^;{}()]*?加倍\s*\(` 就从那句注释开始一路匹配到了下面的
    ///   `int 加倍(int n)`，于是"漏了 static"没被抓出来。
    ///   学生也完全可能写「// 这里要写 static 吗？」这样的注释。
    ///
    /// 局限：字符串里的 // 也会被当成注释切掉。这对本课的检查没影响
    /// （我们只看方法声明），所以不为它加词法分析。
    /// </summary>
    private static string 去注释(string 代码)
    {
        string 无块注释 = Regex.Replace(代码, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(无块注释, @"//[^\n]*", " ");
    }

    /// <summary>报错文本里认不认得出这些片段（中英都要认，见文件头）。</summary>
    private static bool 含(string 文本, params string[] 片段)
    {
        foreach (var f in 片段)
            if (文本.Contains(f, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static string 显示(string s) => s.Length == 0 ? "（空）" : s;
}
