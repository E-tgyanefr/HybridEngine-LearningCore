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
// ─────────────────────────────────────────────────────
//  先介绍：给一段计算起个名字
// ─────────────────────────────────────────────────────
//
// 前三课的代码都是一条道写到底。可有些计算你会用很多次：
//
//     int a = 6 * 2;
//     int b = 7 * 2;
//     int c = n * 2;
//
// 抄三遍还行，抄三十遍就有两个问题：**打错一个字就错一处**，而且想改规则
// （比如改成乘 3）得挨个改。
//
// 方法（Java 里"函数"就叫**方法**）解决的就是这件事：
// **把一段计算打包、起个名字，以后叫名字就行。**
//
//     static int 加倍(int n) {
//         return n * 2;              ← 交出去的东西
//     }
//
//     System.out.println(加倍(6));   ⇒ 12
//     System.out.println(加倍(100)); ⇒ 200
//
// 关键在那个参数 n：**同一个方法，喂不同的数据，给不同的结果**。这才叫
// "打包"，否则只是把一行代码挪个地方。
//
// ⚠ 但 Java 的方法比 C++ / Python 多一道门槛：**static**。原因是 Java 里所有
//   方法都住在类里面，而"调用一个方法"有两种方式：
//
//     · 写了 static 的方法属于**类**   —— 直接写名字就能调用
//     · 不写 static 的方法属于**对象** —— 必须先 new 一个对象出来
//
//   而 main 是 static 的：程序刚启动的那一刻一个对象都还没有，所以从 main 里
//   直接写名字调用的方法，**也必须是 static 的**。漏写 static，你会看到：
//       错误: 无法从静态上下文中引用非静态 方法 加倍(int)
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
// ① 一个方法有四个部分，缺一不可
//
//     static  返回类型  方法名 (参数类型 参数名) {
//         方法体
//         return 结果;
//     }
//
//     static int 加倍(int n) {
//         return n * 2;
//     }
//
//     · static   —— 属于类，能直接从 main 里叫（这一课的重点）
//     · 返回类型  —— 交出整数写 int，交出文字写 String，什么都不交写 void
//     · 方法名    —— 要和题目一模一样（Java **区分大小写**）
//     · 参数      —— 要几个写几个，每个都要写**类型**：int n，不是只写 n
//
//   ⚠ 方法名后面的括号和花括号都不能省，而**末尾不要写分号**
//     （这一点和变量声明相反：int n = 1; 有分号，方法没有）。
//
// ② return：把结果**交出去**；void：只做事，不交值（所以不写 return）
//
//     static int 加倍(int n) { return n * 2; }        ⇒ 加倍(4) 给调用者 8
//     static void 打印星星(int n) {
//         for (int i = 0; i < n; i++) {
//             System.out.print("*");     ← 打到屏幕上，不换行（println 才会换行）
//         }
//     }
//
// ③ 「交出结果」和「打印出来」是两件事（最容易混的一处）
//
//     static int 加倍(int n) { System.out.println(n * 2); }   ✗ 打印了，没交出值
//     static int 加倍(int n) { return n * 2; }                ✓ 这才叫交给调用者
//
//   为什么前者不行：调用 加倍(4) 的地方**拿不到任何东西**，编译器也会拦你
//   —— 你会看到 错误: 缺少返回语句。
//   一句话：**print 是给别人看的，return 是给程序用的。**
//
// ④ 想用几次用几次，而且换个输入就换个结果：加倍(3) → 6，加倍(0) → 0，
//   加倍(-4) → -8。
//
//   ⚠ 如果一个方法不管给什么都返回同一个数，那它多半是把答案写死了：
//         static int 加倍(int n) { return 6; }     ✗ 6 和 n 没有关系
//     判断一个方法写对没有，靠的就是**换几组输入多试几次**。
//
// ⑤ 为什么这一课不用你写 main：课程会替你补一个标准的 public static void
//   main(String[] args)，从里面调用你的四个方法。好处有两个 —— 你专心写方法
//   本身；而且漏写 static 时那条错误**一定会出现**（见坑 1 的说明）。
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：方法忘了写 static（本课第一大坑）
//
//     int 加倍(int n) { return n * 2; }        ← 少了 static
//
// 你会看到：错误: 无法从静态上下文中引用非静态 方法 加倍(int)
// 为什么：检查时你的方法会被放进 class Main 里、**从 static 的 main 调用**：
//       public class Main {
//           int 加倍(int n) { return n * 2; }        ← 你的方法
//           public static void main(String[] args) {
//               System.out.println(加倍(4));         ← 从这里调用 → 编译不过
//           }
//       }
//   不写 static 的方法属于"对象"，得先 new 一个出来才能用：
//       Main m = new Main();   System.out.println(m.加倍(4));
// ⇒ 纯计算的方法就该写 static，这一课全用这种。
//   ⚠ 顺便记住（**新版 JDK 25 起**）：如果你**自己**写 main，而且写成不带
//     static 的 void main(String[] args)，那么它里面的非 static 方法调用
//     **不会报错**，程序照样跑。但换到 JDK 21 或更早、或者把代码交到别的
//     环境，就会撞墙。所以"在这台机器上能跑"不等于写对了。这也正是本课不让
//     你写 main 的原因：把这条错误稳定地暴露出来。
//
// ⚠ 坑 2：声明了返回类型，方法里却没有 return
//
//     static int 加倍(int n) {
//         System.out.println(n * 2);        ← 打印了，但什么都没交出去
//     }
//
// 你会看到：错误: 缺少返回语句
// 为什么：你答应了"这个方法交出一个 int"，可它走到末尾也没交出任何东西。
//   ⚠ 注意这里的因果：**打印不等于返回**。System.out.println 只是把字打到
//     屏幕上，方法本身什么都没给调用者。
// ⇒ 有返回类型的方法，**每条可能的路径最后都要 return**。如果确实是"只做事、
//   不交值"，那就把返回类型改成 void（比如 打印星星）。
//
// ⚠ 坑 3：把 void 方法的调用当成了值
//
//     static void 星星(int n) { ... }
//     System.out.println("A[" + 星星(3) + "]");      ← 拿 void 去拼接
//
// 你会看到：错误: 此处不允许使用 '空' 类型      （"空"就是 void 的中文说法）
// 为什么：void 方法**没有值**，你没法把它塞进字符串拼接或者 println 的参数里。
//   void 方法只能当**一条语句**调用：星星(3);
// ⇒ 分清这一课的两个方法：星星(int n) 要**返回**一个字符串（String），调用处
//   能拿去用；打印星星(int n) 只**打印**，不交值（void），只能单独一行调用。
//
// ⚠ 坑 4：参数类型对不上，或者方法名写错
//
//     System.out.println(加倍("x"));            ← 传了字符串
//     System.out.println(加倍2(4));             ← 名字打错了
//
// 你会看到（第一行）：错误: 不兼容的类型: String无法转换为int
//   （第二行）：错误: 找不到符号   符号: 方法 加倍2(int)   位置: 类 Main
// 为什么：Java 的参数类型是**硬约束**，不会替你自动转换；方法名也**区分大小写**，
//   中文标识符一个字符都不能差 —— 多一个字、少一个字，编译器就认为你没写。
// ⇒ 传之前先看方法要什么（加倍 要一个整数、最大值 要两个）；方法名照题目抄。
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
//   看这一课要干什么：dotnet run --project csharp/java-lessons 05
//   看答案（先自己写一遍）：dotnet run --project csharp/java-lessons 答案 05
//
//   ⚠ 有一条检查会拿**四组不同的输入**去调每一个方法。如果你的方法不管给
//     什么都返回同一个答案，就是在那儿露馅的。
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
