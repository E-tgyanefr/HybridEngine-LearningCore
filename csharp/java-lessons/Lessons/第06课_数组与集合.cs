// 第 06 课：数组与集合 —— 一次装很多东西
//
// ══ 这一课要教什么 ══
// 前面一课的方法一次只处理一个值。要装一串值，Java 有两种容器：
//
//   · 数组 int[] —— 长度**固定**，用 a[i] 取，长度写在 a.length
//     ⚠ length 是**属性**（字段），**不加括号**
//   · ArrayList —— 长度会**长大**，用 表.get(i) 取，长度写在 表.size()
//     ⚠ size 是**方法**，**必须加括号**
//
// 这两条正好是这一课的核心检查点：`数组.length`（不带括号）
// 和 `字符串.length()`（带括号）写法不一样，初学者混着写是家常便饭。
// 好消息是 Java 的编译器**会当场报错**，不像整数除法那样静默出错 ——
// 所以检查的任务不是"抓出来"，而是"把报错翻译成人话，告诉他括号该不该加"。
//
// 第二个要讲清的点：`int[] a = new int[3];` 之后 a 里**自动全是 0**。
// 这一点和 C++ 相反（C++ 的局部数组是未初始化的随机值）——
// 学生带着 C++ 的直觉读这段代码，会以为打印出来的是垃圾值。
//
// 第三个点：**Java 会检查下标**。a[3] 越界不是"读到隔壁的内存"，
// 而是当场抛 ArrayIndexOutOfBoundsException。比 C++ 安全，但程序会停。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点：`数组.length` 不带括号、`ArrayList.size()` 要括号。
//   抓法不是正则，而是**编译**：学生写了 a.length() 或 表.size，
//   检查程序调用他的方法时 javac 直接报
//      错误: 找不到符号  符号: 方法 length()  位置: 类型为int[]的变量 a
//   我们把这句翻译成「数组的 length 是属性，把括号去掉」。
//   这样"能算出正确答案但写法是错的"也跑不掉（因为根本编译不过）。
//
// 偷懒解法防法：每个方法都用**多个不同输入**验（不同长度的数组、
//   越界的两个方向、加元素前后各看一次大小）。写死一个返回值蒙不过去。
//
// ⚠ 给维护者：这一课的骨架是**自定义**的（不是 JavaCheck.默认骨架），
//   因为学生要用 ArrayList，而 import 必须写在文件最上面、class 外面。
//   骨架里已经写好了 `import java.util.ArrayList;`，
//   学生若自己再写一遍 import，javac 会报「非法的类型开始」——
//   那条错误我们在 编译不通过 里专门翻译了。

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HybridEngine-light;
using HybridEngineLight.JavaLessons;

[Java课程("06", "第 06 课：数组与集合 —— 一次装很多东西",
    "1. static int 数组长度(int[] a)：返回数组的长度（a.length，**不加括号**）\n" +
    "2. static int 取元素(int[] a, int i)：返回第 i 个元素（i 保证合法）\n" +
    "3. static boolean 越界了吗(int[] a, int i)：下标 i 超出范围时返回 true\n" +
    "4. static int[] 新建数组(int n)：返回一个长度为 n 的新数组\n" +
    "5. static int 列表大小(ArrayList<Integer> 表)：返回表里元素的个数（表.size()，**要括号**）\n" +
    "6. static int 列表取(ArrayList<Integer> 表, int i)：返回第 i 个元素（表.get(i)）\n" +
    "7. static void 列表加(ArrayList<Integer> 表, int 值)：把值加进表里（表.add(值)）")]
public static class 第06课
{
    /// <summary>
    /// 这一课的自定义骨架。
    ///
    /// 和 JavaCheck.默认骨架 的区别只有一条：顶上多了一行 import。
    /// 学生的方法要收 ArrayList&lt;Integer&gt;，而 import 必须写在 class 外面、
    /// 文件最上面 —— 默认骨架是把学生代码塞进 class 里的，塞不下 import。
    /// </summary>
    private const string 骨架 = """
import java.util.ArrayList;

public class Main {
/*学生代码*/

    public static void main(String[] args) {
/*调用点*/
    }
}
""";

    public static string 模板 => """
// 第 06 课：数组与集合
//
// 要做（只写**方法**，不用写 class、不用写 main，**也不用写 import** ——
//       课程已经替你写好了 import java.util.ArrayList;）：
//   1. static int 数组长度(int[] a)                  返回数组的长度
//   2. static int 取元素(int[] a, int i)              返回第 i 个元素（i 保证合法）
//   3. static boolean 越界了吗(int[] a, int i)        下标 i 超出范围就返回 true
//   4. static int[] 新建数组(int n)                   返回一个长度为 n 的新数组
//   5. static int 列表大小(ArrayList<Integer> 表)     返回表里元素的个数
//   6. static int 列表取(ArrayList<Integer> 表, int i) 返回第 i 个元素
//   7. static void 列表加(ArrayList<Integer> 表, int 值) 把值加进表里
//
// ⚠ 这一课只有一条规矩，但两个容器**写法正好相反**，务必记牢：
//
//     数组：        int[] a = new int[3];
//                   a.length        ← 属性，**不加括号**
//                   a[0]            ← 用方括号取元素
//
//     ArrayList：   ArrayList<Integer> 表 = new ArrayList<>();
//                   表.size()       ← 方法，**要加括号**
//                   表.get(0)       ← 用 get 取元素
//                   表.add(9)       ← 用 add 往里加
//
//   写错了编译器会当场报错，比如把数组写成 a.length() 会报：
//       错误: 找不到符号   符号: 方法 length()
//   反过来把字符串写成 s.length 也会报错。记住一句话：
//       **数组的 length 是"东西"，字符串和集合的 length()/size() 是"动作"。**
//
// ⚠ 还有两件和 C++ 不一样的事：
//     · new int[3] 之后，三个元素**自动都是 0**（Java 会初始化）。
//       C++ 里同样是 new int[3]，读出来却是随机值 —— 别把那个直觉带过来。
//     · Java **会检查下标**：a[3] 越界不是读到别的内存，
//       而是当场抛 ArrayIndexOutOfBoundsException，程序立刻停下。
//       这是好事（错误暴露得早），但要用 a.length 先把范围判断好。
//
// 写完后运行：dotnet run --project csharp/java-lessons 检查 06

// ── 在下面写你的方法 ──

""";

    public static string 参考答案 => """
// 1. 数组长度：length 是**属性**，没有括号
static int 数组长度(int[] a) {
    return a.length;
}

// 2. 取元素：方括号里写下标，从 0 开始数
static int 取元素(int[] a, int i) {
    return a[i];
}

// 3. 越界了吗：合法的下标是 0 … a.length - 1
static boolean 越界了吗(int[] a, int i) {
    return i < 0 || i >= a.length;
}

// 4. 新建数组：元素自动全是 0（不是随机值）
static int[] 新建数组(int n) {
    return new int[n];
}

// 5. 列表大小：size() 是**方法**，要括号（ArrayList 没有 length）
static int 列表大小(ArrayList<Integer> 表) {
    return 表.size();
}

// 6. 列表取：用 get(i)，不是 表[i]
static int 列表取(ArrayList<Integer> 表, int i) {
    return 表.get(i);
}

// 7. 列表加：add 会**改变这张表本身**（和字符串不一样，字符串改不了）
static void 列表加(ArrayList<Integer> 表, int 值) {
    表.add(值);
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("数组长度：a.length 是属性，**不能加括号**")]
    public static JavaCheckResult 检查_数组长度(string 代码)
    {
        var run = 编译并调用(代码,
            "数组长度(new int[]{1, 2, 3})",
            "数组长度(new int[0])",
            "数组长度(new int[7])");
        if (!run.编译通过) return 编译不通过(run);

        var 结果 = 比对(run, ("数组长度(new int[]{1,2,3})", "3"),
                             ("数组长度(new int[0])", "0"),
                             ("数组长度(new int[7])", "7"));
        if (结果.Ok) return 结果;
        return JavaCheckResult.不过(结果.Detail + "\n" +
            "      数组的长度用 a.length 取（属性，**不带括号**）。\n" +
            "      空数组（new int[0]）的长度是 0 —— 这也是合法数组，不用特殊处理。");
    }

    [Java检查说明("取元素：用方括号 a[i]，下标从 0 开始")]
    public static JavaCheckResult 检查_取元素(string 代码)
    {
        var run = 编译并调用(代码,
            "取元素(new int[]{10, 20, 30}, 0)",
            "取元素(new int[]{10, 20, 30}, 2)",
            "取元素(new int[]{5}, 0)");
        if (!run.编译通过) return 编译不通过(run);

        var 结果 = 比对(run, ("取元素({10,20,30}, 0)", "10"),
                             ("取元素({10,20,30}, 2)", "30"),
                             ("取元素({5}, 0)", "5"));
        if (结果.Ok) return 结果;
        return JavaCheckResult.不过(结果.Detail + "\n" +
            "      取元素就直接用方括号：\n" +
            "          return a[i];\n" +
            "      ⚠ 下标是**从 0 开始**数的：{10, 20, 30} 里\n" +
            "          a[0] 是 10、a[1] 是 20、a[2] 是 30 —— 没有 a[3]。");
    }

    [Java检查说明("越界了吗：合法的下标是 0 到 a.length - 1，两头都要判断")]
    public static JavaCheckResult 检查_越界了吗(string 代码)
    {
        var run = 编译并调用(代码,
            "越界了吗(new int[]{1, 2, 3}, 0)",
            "越界了吗(new int[]{1, 2, 3}, 2)",
            "越界了吗(new int[]{1, 2, 3}, 3)",
            "越界了吗(new int[]{1, 2, 3}, -1)",
            "越界了吗(new int[0], 0)");
        if (!run.编译通过) return 编译不通过(run);

        var 结果 = 比对(run, ("下标 0（合法）", "false"),
                             ("下标 2（最后一个，合法）", "false"),
                             ("下标 3（越界）", "true"),
                             ("下标 -1（越界）", "true"),
                             ("空数组的下标 0（越界）", "true"));
        if (结果.Ok) return 结果;
        return JavaCheckResult.不过(结果.Detail + "\n" +
            "      判断越界要**两头都看**，因为 Java 的下标范围是 0 … a.length - 1：\n" +
            "          return i < 0 || i >= a.length;      ✓ 两头都挡\n" +
            "          return i > a.length;                ✗ 漏了负数，而且 i == a.length 也是越界\n" +
            "          return i >= a.length;               ✗ 漏了负数那头\n" +
            "      ⚠ 为什么必须自己判断：Java **会检查下标**，越界时直接抛\n" +
            "        ArrayIndexOutOfBoundsException 把程序打断 —— 不会像 C++ 那样\n" +
            "        「悄悄」读到隔壁的内存。先用 a.length 判好范围，程序才稳。");
    }

    [Java检查说明("新建数组：长度对，而且元素自动都是 0（Java 会初始化）")]
    public static JavaCheckResult 检查_新建数组(string 代码)
    {
        // ⚠ 这一条想纠正一个从 C++ 带过来的直觉：
        //   C++ 里 new int[3] 读出来是随机值；Java 里**自动全是 0**。
        //   所以这里把元素一个个打出来 —— 学生看到满屏 0，印象比读文字深。
        var run = 编译并调用(代码,
            "新建数组(3).length",
            "新建数组(4)[0]",
            "新建数组(4)[1]",
            "新建数组(4)[3]",
            "新建数组(0).length");
        if (!run.编译通过) return 编译不通过(run);

        var 结果 = 比对(run, ("新建数组(3).length", "3"),
                             ("新建数组(4)[0]", "0"),
                             ("新建数组(4)[1]", "0"),
                             ("新建数组(4)[3]", "0"),
                             ("新建数组(0).length", "0"));
        if (结果.Ok)
            return JavaCheckResult.过(
                "新建数组(4) 的四个元素都是 0 —— 记住这一点：\n" +
                "      Java 里 new int[4] 之后**元素自动全是 0**，不用自己填。\n" +
                "      （C++ 里 new int[4] 读出来是随机值 —— 那边的直觉别带过来。）");
        return JavaCheckResult.不过(结果.Detail + "\n" +
            "      新建数组 要返回一个**新**数组：return new int[n];\n" +
            "      注意 n 是长度，用 new int[n] 而不是 new int[n - 1]；\n" +
            "      而且 new int[n] 的元素自动都是 0，不需要自己一个个赋值。");
    }

    [Java检查说明("ArrayList：add 加、get 取、size() 带括号，而且 add 真的改变了那张表")]
    public static JavaCheckResult 检查_列表操作(string 代码)
    {
        // 这一条要连着调好几个方法，还得先建一张表 —— 所以调用点写成一段语句。
        var run = 编译并调用语句(代码,
            @"ArrayList<Integer> 表 = new ArrayList<>();",
            @"列表加(表, 7);",
            @"列表加(表, 8);",
            @"列表加(表, 9);",
            @"System.out.println(""大小:"" + 列表大小(表));",
            @"System.out.println(""第一个:"" + 列表取(表, 0));",
            @"System.out.println(""最后一个:"" + 列表取(表, 2));",
            @"列表加(表, 10);",
            @"System.out.println(""再加一个:"" + 列表大小(表));");
        if (!run.编译通过) return 编译不通过(run);

        var 结果 = 比对(run, ("加了 3 个之后的大小", "大小:3"),
                             ("第一个元素", "第一个:7"),
                             ("最后一个元素", "最后一个:9"),
                             ("再加一个之后的大小", "再加一个:4"));
        if (结果.Ok) return 结果;
        return JavaCheckResult.不过(结果.Detail + "\n" +
            "      ArrayList 的三个常用操作：\n" +
            "          表.add(值);        ← 加到最后（注意：它会**改变 表 本身**）\n" +
            "          表.get(i);         ← 取第 i 个（从 0 开始数）\n" +
            "          表.size()          ← 有几个元素（**是方法，要括号**）\n" +
            "      ⚠ 两个常见的写法错误：\n" +
            "          ArrayList 上没有 length —— 那是数组的（而且不带括号）\n" +
            "          表[0] 这种方括号只对数组有效，ArrayList 要用 get(0)");
    }

    [Java检查说明("演示：Java 会检查下标 —— 越界时抛 ArrayIndexOutOfBoundsException")]
    public static JavaCheckResult 检查_越界会抛异常(string 代码)
    {
        // 这一条是**演示**，不是考学生写代码。
        // 但它是真的跑学生写的 新建数组(3)，所以也不是空跑：
        //   预期结果是「程序打印两行，然后**崩掉**」——
        //   崩掉这件事本身就是证据：Java 检查下标，不会像 C++ 那样读到别的内存。
        var run = 编译并调用语句(代码,
            @"int[] a = 新建数组(3);",
            @"System.out.println(""长度:"" + a.length);",
            @"System.out.println(""下面故意访问 a[3]（越界）——"");",
            @"System.out.println(a[3]);",
            @"System.out.println(""这一行不会被执行到"");");
        if (run.Raw.CompileFailed) return 编译不通过(run);

        if (含(run.Raw.RunOutput, "ArrayIndexOutOfBoundsException"))
            return JavaCheckResult.过(
                "程序在 a[3] 那里抛出了 ArrayIndexOutOfBoundsException，当场停下 —— \n" +
                "      这正是 Java 和 C++ 的一个重要区别：\n" +
                "        · Java **检查下标**：越界立刻抛异常，错误暴露得早\n" +
                "        · C++ 不检查：a[3] 会去读数组旁边的内存，读到什么都可能，\n" +
                "          程序还可能「看起来正常」地跑完 —— 那才是最难查的 bug\n" +
                "      所以写数组时先用 a.length 把下标判好（就是 越界了吗 那个方法）。");

        if (run.Raw.Error != null)
            return JavaCheckResult.不过(
                "程序没有按预期在越界那一步抛出 ArrayIndexOutOfBoundsException。\n" +
                "      实际发生的是：\n          " + run.Raw.Error.Replace("\n", "\n          ") + "\n" +
                "      先确认 新建数组(3) 能编译通过（这一条检查要用到它）。");

        return JavaCheckResult.不过(
            "程序居然正常跑完了，a[3] 没有报错 —— 这不正常，说明 新建数组 返回的数组比 3 还长。\n" +
            "      新建数组(3) 应该正好返回 3 个元素的数组。");
    }

    [Java检查说明("七个方法是不是都写了（名字、返回类型、参数都要对，而且都要有 static）")]
    public static JavaCheckResult 检查_方法都写了吗(string 代码)
    {
        var 签名们 = new (string 名字, string 正则, string 样子)[]
        {
            ("数组长度", @"static\s+int\s+数组长度\s*\(\s*int\s*\[\s*\]\s*\w+\s*\)",
                "static int 数组长度(int[] a)"),
            ("取元素", @"static\s+int\s+取元素\s*\(\s*int\s*\[\s*\]\s*\w+\s*,\s*int\s+\w+\s*\)",
                "static int 取元素(int[] a, int i)"),
            ("越界了吗", @"static\s+boolean\s+越界了吗\s*\(\s*int\s*\[\s*\]\s*\w+\s*,\s*int\s+\w+\s*\)",
                "static boolean 越界了吗(int[] a, int i)"),
            ("新建数组", @"static\s+int\s*\[\s*\]\s+新建数组\s*\(\s*int\s+\w+\s*\)",
                "static int[] 新建数组(int n)"),
            ("列表大小", @"static\s+int\s+列表大小\s*\(\s*ArrayList\s*<\s*Integer\s*>\s*\w+\s*\)",
                "static int 列表大小(ArrayList<Integer> 表)"),
            ("列表取", @"static\s+int\s+列表取\s*\(\s*ArrayList\s*<\s*Integer\s*>\s*\w+\s*,\s*int\s+\w+\s*\)",
                "static int 列表取(ArrayList<Integer> 表, int i)"),
            ("列表加", @"static\s+void\s+列表加\s*\(\s*ArrayList\s*<\s*Integer\s*>\s*\w+\s*,\s*int\s+\w+\s*\)",
                "static void 列表加(ArrayList<Integer> 表, int 值)"),
        };

        var 缺 = new List<string>();
        string 净 = 去注释(代码);
        foreach (var (名字, 正则, 样子) in 签名们)
            if (!Regex.IsMatch(净, 正则)) 缺.Add($"      {名字}  —— 要写成：{样子}");

        if (缺.Count == 0)
            return JavaCheckResult.过("七个方法的返回类型、参数和 static 都写对了");

        return JavaCheckResult.不过(
            "这几个方法没有找到（或者签名和题目不一样）：\n" +
            string.Join("\n", 缺) + "\n" +
            "      一个方法的四个部分缺一不可：\n" +
            "          static  返回类型  方法名 (参数类型 参数名) { … }\n" +
            "        · **static 不能漏**（第 05 课讲过：main 是 static 的，只调得到 static 方法）\n" +
            "        · 数组类型的写法是把方括号跟在类型后面：int[] a（不是 int a[]，也不是 int a）\n" +
            "        · ArrayList 要写全：ArrayList<Integer> 表（尖括号里的 Integer 表示\n" +
            "          「这张表装整数」—— 不能只写 ArrayList，也不能装 int）\n" +
            "        · 方法名要和题目一模一样（Java 区分大小写）\n" +
            "      ⚠ 不要自己写 import —— 课程已经在文件最上面写好 import java.util.ArrayList; 了。");
    }

    // ══════════════════ 小工具 ══════════════════

    private static JavaRun 编译并调用(string 代码, params string[] 表达式们)
    {
        var sb = new StringBuilder();
        foreach (var e in 表达式们) sb.Append("        System.out.println(").Append(e).Append(");\n");
        return JavaCheck.编译并运行(代码, 骨架, sb.ToString());
    }

    private static JavaRun 编译并调用语句(string 代码, params string[] 语句们)
    {
        var sb = new StringBuilder();
        foreach (var s in 语句们) sb.Append("        ").Append(s).Append('\n');
        return JavaCheck.编译并运行(代码, 骨架, sb.ToString());
    }

    /// <summary>编译没过时：先给针对性说明，没有就交给通用翻译。</summary>
    private static JavaCheckResult 编译不通过(JavaRun run)
    {
        string 信息 = run.编译信息;

        // ① 学生在 class 里面又写了一遍 import（最常见的写法错误）
        //    实测原文：错误: 非法的类型开始 / 错误: 需要<标识符>
        if (含(信息, "illegal start of type", "非法的类型开始"))
            return JavaCheckResult.不过(
                "文件里多了一句 import（或者 package）**位置不对**。\n" +
                "      Java 的 import 只能写在文件**最上面、class 外面**：\n" +
                "          import java.util.ArrayList;      ← 必须在 class 之前\n" +
                "          public class Main { … }\n" +
                "      而这一课的 import **课程已经替你写好了** ——\n" +
                "      你只要写方法就行，把多余的 import / package 那句删掉。");

        // ② 数组的 length 写成了方法（本课核心坑之一）
        //    实测原文：错误: 找不到符号  符号: 方法 length()  位置: 类型为int[]的变量 a
        if (含(信息, "length()") && 含(信息, "int[]"))
            return JavaCheckResult.不过(
                "数组的 length 写成了 length() —— **多了两个括号**。\n" +
                "      编译器报的是「找不到符号：方法 length()」：数组上根本没有这个方法。\n" +
                "          a.length        ✓ 数组的长度是**属性**（一个「东西」，不是动作）\n" +
                "          a.length()      ✗ 数组没有 length() 这个方法\n" +
                "      ⚠ 这正是本课要记牢的那条规矩 —— 两个容器写法**正好相反**：\n" +
                "          int[] a;            a.length        ← 数组：属性，**不加括号**\n" +
                "          String s;           s.length()      ← 字符串：方法，**要括号**\n" +
                "          ArrayList 表;       表.size()        ← 集合：方法，**要括号**\n" +
                "      一句话：数组的 length 是「东西」，字符串和集合的 length()/size() 是「动作」。");

        // ③ ArrayList 上写 .length / .size 不带括号
        //    实测原文（.size 少了括号）：错误: size 在 ArrayList 中是 private 访问控制
        //    实测原文（写成 .length）  ：错误: 找不到符号  符号: 变量 length
        if (含(信息, "ArrayList"))
        {
            if (含(信息, "private", "私有") && 含(信息, "size"))
                return JavaCheckResult.不过(
                    "ArrayList 的 size 少了两个括号。\n" +
                    "      编译器那句「size 在 ArrayList 中是 private 访问控制」看着吓人，\n" +
                    "      其实说的是：你碰到了一个不该直接碰的东西 —— 因为它是个**方法**，不是属性。\n" +
                    "          表.size()       ✓ 方法，要括号\n" +
                    "          表.size         ✗ 编译器会去找名叫 size 的字段，然后撞上私有的那个\n" +
                    "      （对照着记：数组是 a.length 不带括号，集合是 表.size() 带括号。）");
            if (含(信息, "variable length", "变量 length"))
                return JavaCheckResult.不过(
                    "ArrayList 上没有 length —— 那是**数组**才有的属性。\n" +
                    "      集合求个数用的是 size()：\n" +
                    "          ArrayList<Integer> 表 = new ArrayList<>();\n" +
                    "          表.size()       ✓ 有几个元素（方法，要括号）\n" +
                    "          表.length       ✗ 编译不过\n" +
                    "      ⚠ 顺便分清取元素的方式：数组用 a[i]，ArrayList 用 表.get(i)。");
        }

        // ④ 字符串的 length 少了括号（第 07 课的正题，这里顺手提醒）
        if (含(信息, "variable length", "变量 length") && 含(信息, "String"))
            return JavaCheckResult.不过(
                "字符串的 length 少了两个括号。\n" +
                "          s.length()      ✓ 字符串的长度是**方法**，要括号\n" +
                "          s.length        ✗ 编译器报「找不到符号：变量 length」\n" +
                "      ⚠ 和数组正好相反：数组是 a.length（不带括号）。两个都记一遍。");

        // ⑤ 忘了 static（第 05 课的正题）
        if (含(信息, "static context", "静态上下文"))
            return JavaCheckResult.不过(
                "有的方法没有写 static，从 main 里调不到它。\n" +
                "      main 是 static 的，只有 static 方法能直接按名字调用：\n" +
                "          static int 数组长度(int[] a) { return a.length; }\n" +
                "      每个方法前面都要有 static（第 05 课专门讲过这一条）。");

        // ⑥ 声明了返回类型却没有 return
        if (含(信息, "missing return statement", "缺少返回语句"))
            return JavaCheckResult.不过(
                "方法声明了要交出结果，但方法体里**没有 return**。\n" +
                "      比如 数组长度 要写成：\n" +
                "          static int 数组长度(int[] a) { return a.length; }\n" +
                "      注意：方法里打印（System.out.println）**不等于**交出结果 ——\n" +
                "      交出结果只有一条路：return。");

        // ⑦ 方法名/类型对不上
        if (含(信息, "cannot find symbol", "找不到符号"))
            return JavaCheckResult.不过(
                "编译器不认识这个名字 —— 通常是有个方法没写、或者名字/类型和题目不一样。\n" +
                "      这一课要求的七个方法：\n" +
                "          static int 数组长度(int[] a)\n" +
                "          static int 取元素(int[] a, int i)\n" +
                "          static boolean 越界了吗(int[] a, int i)\n" +
                "          static int[] 新建数组(int n)\n" +
                "          static int 列表大小(ArrayList<Integer> 表)\n" +
                "          static int 列表取(ArrayList<Integer> 表, int i)\n" +
                "          static void 列表加(ArrayList<Integer> 表, int 值)\n" +
                "      ⚠ 如果报的是「找不到符号：类 List」之类 —— 那是你写了 List。\n" +
                "        课程只导入了 ArrayList，用 ArrayList<Integer> 这个写法。");

        if (含(信息, "cannot be applied to given types", "应用到给定类型"))
            return JavaCheckResult.不过(
                "调用时给的参数和方法声明的对不上（个数或类型不一样）。\n" +
                "      注意数组参数要写 int[] a（类型带方括号），集合参数要写 ArrayList<Integer> 表。");

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

    /// <summary>报错文本里认不认得出这些片段（中英都要认，见第 05 课文件头的说明）。</summary>
    private static bool 含(string 文本, params string[] 片段)
    {
        foreach (var f in 片段)
            if (文本.Contains(f, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>去掉注释再做语法（正则）检查 —— 注释里的字会骗过正则（第 05 课实测踩过）。</summary>
    private static string 去注释(string 代码)
    {
        string 无块注释 = Regex.Replace(代码, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(无块注释, @"//[^\n]*", " ");
    }

    private static string 显示(string s) => s.Length == 0 ? "（空）" : s;
}
