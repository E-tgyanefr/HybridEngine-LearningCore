// 第 02 课：输入与计算 —— 让程序接收数据，看清 int 与 double 的分界
//
// ══ 这一课要教什么 ══
// 第 01 课的程序只会自己说话，不会听。这一课让程序**接收输入** ——
// 从此同一个程序能处理不同的数据，不用每次改代码。
//
// 但"输入 + 计算"在 Java 里带来两个**不报错的坑**，这一课的重点全在这里：
//   ① 两个 int 相除，结果还是 int：7 / 2 得 3，不是 3.5。
//      编译不报错、运行不警告 —— 它就这么安安静静地把小数丢掉了。
//   ② nextInt() 读到不是整数的东西会抛 InputMismatchException，程序当场结束。
//      ⚠ 这一点和 C++ **相反**：C++ 的 cin 是"静默失败"（变量被写成 0、
//      后面的读入全部失效，程序照跑）；Java 是"当场抛异常"，把问题立刻摆出来。
//      两种设计各有代价，学生必须知道自己这门是哪种 —— 所以这一课两条都考。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点是第 5 条（平均数不能是整数除法）和第 6 条（非法输入必须被拦住）。
//
// 怎么让"需要输入"的题自动检查：**文本替换**，把学生写的
//     new Scanner(System.in)
// 换成
//     new java.util.Scanner("小明\n7 2")
// Scanner 本来就接受一个字符串当输入源。本机实测（openjdk 25.0.3）：
//     new Scanner("7 2") 上 nextInt() 依次得到 7、2，hasNextInt() 读完变 false，
//     读不到东西时抛 NoSuchElementException，读到 abc 抛 InputMismatchException
// —— 和读键盘时的语义**完全一致**。所以这次替换只换"数据从哪来"，不换任何语义。
// （C++ 组用 istringstream 替换 std::cin 是同一个道理。）
//
// ⚠ 为什么**必须**替换、不能真喂输入：JavaRunner 起子进程时把 stdin
//   重定向后**立刻关掉**（见 JavaRunner 文件头第 ⑥ 条）——
//   学生真去读键盘只会立刻读到 EOF，抛 NoSuchElementException。
//   那是"这里没有输入"，不是"你写错了"，反馈会指错方向。
//
// ⚠ 替换成**全限定名** new java.util.Scanner(...)：学生要是写的是
//   new java.util.Scanner(System.in)（没有 import），换成 new Scanner(...) 反而编译不过。
//
// 这个办法的局限（写在这里免得后面的人以为它万能）：
//   · 只认准 `new Scanner(System.in)` 这一种写法。学生要是先把 System.in 存进
//     另一个变量再 new Scanner(那个变量)，替换不中 —— 所以有一条
//     读入方式不对() 先把这种情况挡下来，明说"请用标准写法"，而不是放着它去 EOF。
//   · 输入是**固定字符串**，所以这一课只能考"给定输入算得对不对"，
//     考不了"和用户来回交互"（那种题本来也不适合自动判分）。
//
// 学生能怎么偷懒，被哪条挡住：
//   · 把 商=3、平均数=4.5 写死          → 第 3 条换一组输入（10 4 → 2 和 7）再跑
//   · 用 double 存结果、算法却是整数除法 → 第 5 条（4.0 不等于 4.5）
//   · 不管输入是不是数字                → 第 6 条（InputMismatchException，程序当场结束）

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("02", "第 02 课：输入与计算",
    "1. 用 Scanner 读输入：先读一行姓名（nextLine），再读两个整数（nextInt）到 int 甲、int 乙\n" +
    "2. 第一行打印：你好，<姓名>\n" +
    "3. 打印一行「商=...」：甲 除以 乙 的整数商（输入 7 2 → 商=3）\n" +
    "4. 打印一行「平均数=...」：必须带小数（输入 7 2 → 平均数=4.5）\n" +
    "5. 两个数里只要有一个不是整数，就打印「输入无效」，并且不要再往下算")]
public static class 第02课
{
    public static string 模板 => """
// 第 02 课：输入与计算 —— 让程序接收数据
//
// ─────────────────────────────────────────────────────
//  先介绍：程序得能"听"
// ─────────────────────────────────────────────────────
//
// 第 01 课的程序只会自己说话，不会听 —— 想换一组数据，只能改代码再跑。
// 这一课让程序**接收输入**：从此同一个程序能处理不同的数据。
//
// 但"输入 + 计算"在 Java 里带来两个**不报错的坑**，这一课的重点全在这里：
//
//   ① **两个 int 相除，结果还是 int**：7 / 2 得 3，不是 3.5。
//      编译不报错、运行不警告 —— 它就这么安安静静地把小数丢掉了。
//
//   ② **nextInt() 读到不是整数的东西会抛异常**：InputMismatchException，
//      不处理的话程序**当场结束**。
//      ⚠ 这一点和 C++ **相反**：C++ 的 cin 是"静默失败"（不抛异常、把变量
//      写成 0、后面的读入全部失效，程序照跑）；Java 是"当场抛异常"，
//      把问题立刻摆出来。两种设计各有代价，你必须知道自己这门是哪种。
//
// 为什么这两件事值得单占一课：因为它们都是"**看起来成功了，其实错了**"的
//   类型 —— 初学者最怕这种。报错反而好办，不报错才难查。
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
// ① Scanner：Java 读输入的标准工具
//
//     import java.util.Scanner;                        ← 写在文件**最上面**
//
//     public class Main {
//         public static void main(String[] args) {
//             Scanner 输入 = new Scanner(System.in);    ← 建一个读数器
//             String 姓名 = 输入.nextLine();            ← 读一整行
//             int 甲 = 输入.nextInt();                  ← 读一个整数
//         }
//     }
//
//   ⚠ import 必须写在 **class 外面**（文件最上面）。写在 class 的 { } 里面报
//     「非法的类型开始」；忘了 import 报「找不到符号：类 Scanner」。
//
// ② 三个最常用的读法
//
//     输入.nextLine()       读**一整行**（读到回车为止），结果是 String
//     输入.nextInt()        读一个**整数**，结果是 int
//     输入.hasNextInt()     问一句："下一个东西是整数吗？" 结果是 boolean
//
//   nextInt 读的是"一个词"，nextLine 读的是"一整行" —— 分工不同。
//
// ③ 整数除法：小数会被丢掉（不是四舍五入）
//
//     甲 / 乙              ⇒ 3      ← 丢掉小数，**不是**四舍五入
//     (甲 + 乙) / 2        ⇒ 4      ← 先按整数算出 4，再存进哪里都还是 4
//     (甲 + 乙) / 2.0      ⇒ 4.5    ← 除号两边**至少有一个是小数**才有小数
//
//   ⚠ 特别注意这一种写法**也是错的**：double 平均 = (甲 + 乙) / 2;
//     右边先按整数算出 4，**然后**才变成 4.0 存进去。类型写成 double 只能
//     决定"结果装在哪"，决定不了"怎么算"。
//
// ④ 读之前先问一句，能躲开异常；不 return 的话，程序会拿着没读到东西的变量
//   继续算，然后在后面抛一个**更难懂**的异常。
//
//     if (!输入.hasNextInt()) {
//         System.out.println("输入无效");
//         return;                           ← 这个 return 不能少：不往下算
//     }
//     int 甲 = 输入.nextInt();
//
// ⑤ 顺序很重要：**先 nextLine 读姓名，再 nextInt 读数字** —— 本课要求的
//   顺序正好绕开了那个经典坑（见坑 4），别调换。
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：整数除法把小数丢了（本课第一个大坑）
//
//     double 平均 = (甲 + 乙) / 2;         // 甲 = 7、乙 = 2 时
//     System.out.println("平均数=" + 平均);        ⇒ 平均数=4.0    ✗ 应该 4.5
//
// 你会看到：**什么都不报**。编译通过、运行正常，只是值是错的 —— 连类型都是
//   double，看上去"我用了小数"，其实没有。
// 为什么：两个 int 相除，Java 先按**整数**算出 4，再把 4 变成 4.0 存进 double。
//   算的时候就已经丢了小数，存的时候救不回来。
// ⇒ 除号两边至少有一个写成小数：(甲 + 乙) / 2.0
//   ⚠ 对照着记：7 / 2 得 3（整数商），7 / 2.0 得 3.5（小数商）。
//
// ⚠ 坑 2：import 忘了写，或者写错了位置
//
//     public class Main {
//         import java.util.Scanner;          ← ✗ 写在 class 里面了
//     }
//
// 你会看到：错误: 非法的类型开始      （跟着还有一条：错误: 需要<标识符>）
// 为什么：**import 只能出现在文件最上面、class 外面**。class 的花括号里只允许
//   放"成员"（字段、方法），import 不是成员。
// ⇒ 把它挪到第 1 行：import java.util.Scanner;  然后才是 public class Main。
//   ⚠ 另一种写法是**根本没写 import**：Scanner 输入 = new Scanner(System.in);
//     报的是 错误: 找不到符号   符号: 类 Scanner   位置: 类 Main。
//
// ⚠ 坑 3：nextInt() 读到不是整数的东西 —— 程序当场结束
//
//     int 甲 = 输入.nextInt();        ← 输入是 abc 的时候
//
// 你会看到：
//     Exception in thread "main" java.util.InputMismatchException
//         at java.base/java.util.Scanner.nextInt(Scanner.java:2297)
//         at Main.main(Main.java:8)
//   程序在这里就停了，后面的活一行都干不了。
// 为什么：Scanner 的 nextInt 读不到整数就**抛异常**。
//   ⚠ 这和 C++ **正好相反**：C++ 的 cin 读失败是**一声不吭**的 —— 不抛异常、
//     把变量写成 0、后面的读入全部失效，程序继续跑。Java 的选择是当场抛异常
//     （这是好意，错误不会被藏起来），代价是你不处理就全停。
// ⇒ 读之前先问一句：if (!输入.hasNextInt()) { ...; return; }
//
// ⚠ 坑 4：先 nextInt() 再 nextLine()，那个 nextLine 读到的是"剩下的东西"
//
//     int 甲 = 输入.nextInt();          ← 输入这一行是 "7 2"
//     String 姓名 = 输入.nextLine();    ← 读到的**不是下一行**
//
// 你会看到：姓名 里装的是 " 2"（这一行**剩下**的部分）；如果数字后面就是
//   回车，那它读到的是**空串**。程序不报错，只是名字是空的。
// 为什么：nextInt() 只吃掉**数字本身**，把这一行剩下的东西（空格、以及结尾
//   那个回车）**留在原地**。紧跟其后的 nextLine() 读到的是"这一行剩下的
//   部分"，而不是下一行。这是分工不同，不是 bug。⇒ 先读姓名、再读数字。
//
// ⚠ 坑 5：整数除以 0 —— 直接抛异常（不是 Infinity）
//
//     int 甲 = 0;   System.out.println(10 / 甲);
//
// 你会看到：Exception in thread "main" java.lang.ArithmeticException: / by zero
// 为什么：**整数除法除以 0 会抛异常**（小数不一样：10.0 / 0 得到 Infinity，
//   不抛异常，别记混）。⇒ 分母是用户给的数时，先判断它是不是 0。
//   ⚠ 顺带一条编译期的坑：写成 int 甲;（不给初值）再在 try 里赋值，后面用到
//     甲 会报 错误: 可能尚未初始化变量甲。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
// ⚠ 写一个**完整的程序**（import + public class Main + main 方法）。
//
//   1. 用 Scanner 读入：先读**一行**姓名（输入.nextLine()），
//      再读两个整数（输入.nextInt() 两次），存进 int 甲、int 乙
//   2. 打印一行    你好，<姓名>
//   3. 打印一行    商=...        （甲 除以 乙 的整数商；输入 7 2 → 商=3）
//   4. 打印一行    平均数=...    （必须带小数；输入 7 2 → 平均数=4.5）
//   5. 两个数里只要有一个不是整数，就打印「输入无效」，并且**不要再往下算**
//
// 检查时程序会自动拿到一组固定输入（就像有人替你敲了键盘）：
//     第一组  小明 / 7 / 2      期望  你好，小明 / 商=3 / 平均数=4.5
//     第二组  小明 / 10 / 4     期望  你好，小明 / 商=2 / 平均数=7.0
//     第三组  小明 / abc        期望  输入无效
//
// ⚠ 两组**不同**的输入是有意的：把答案写死的人，第二组就会露馅。
// ⚠ 「输入无效」打印完一定要 return（或者把计算放进 else 里）—— 光打印不
//   停下来，程序会接着拿 0 去算，然后抛一个不相干的除零异常。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/java-lessons 检查 02
//
//   看这一课要干什么：dotnet run --project csharp/java-lessons 02
//   看答案（先自己写一遍）：dotnet run --project csharp/java-lessons 答案 02
//
//   ⚠ 检查会把你的 new Scanner(System.in) **悄悄换成**一个装好数据的
//     Scanner（等价于有人替你敲了上面那几组输入），所以这里**必须**写成
//     `new Scanner(System.in)` 这一种形式，不要绕：
//        Scanner 输入 = new Scanner(System.in);      ✓
//        先把 System.in 存进别的变量再传给 Scanner    ✗ 换不中，读不到东西
//
// ── 在下面写 ──
import java.util.Scanner;

public class Main {
    public static void main(String[] args) {
        // 在这里写

    }
}
""";

    public static string 参考答案 => """
import java.util.Scanner;

public class Main {
    public static void main(String[] args) {
        Scanner 输入 = new Scanner(System.in);

        // 1. 先读一行姓名。nextLine() 读的是**整行**（读到回车为止）
        String 姓名 = 输入.nextLine();
        System.out.println("你好，" + 姓名);

        // 2. 读两个整数。
        //    读之前先问 hasNextInt()：直接 nextInt() 的话，
        //    遇到 abc 会抛 InputMismatchException，程序当场结束。
        if (!输入.hasNextInt()) {
            System.out.println("输入无效");
            return;                       // ← 不往下算
        }
        int 甲 = 输入.nextInt();

        if (!输入.hasNextInt()) {
            System.out.println("输入无效");
            return;
        }
        int 乙 = 输入.nextInt();

        // 3. 两个 int 相除，结果还是 int：7 / 2 得 3（小数被丢掉）
        System.out.println("商=" + (甲 / 乙));

        // 4. 除号右边写成 2.0，除法才按小数算
        //    ⚠ 写成 double 平均 = (甲 + 乙) / 2; 是错的：右边先算出整数 4，再变成 4.0
        double 平均 = (甲 + 乙) / 2.0;
        System.out.println("平均数=" + 平均);
    }
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("程序能不能把输入读进来、能不能正常跑完")]
    public static JavaCheckResult 检查_程序能运行(string 代码)
    {
        var 读入 = 读入方式不对(代码);
        if (读入 != null) return 读入.Value;

        var run = 跑(代码, "小明\n7 2");
        if (run.Raw.CompileFailed) return 编译失败说明(代码, run);
        if (run.Raw.Error != null)
            return JavaCheckResult.不过(
                "程序没能正常跑完：\n      " + 缩进(run.Raw.Error) + "\n" +
                读入错误的提示(run));

        if (run.行.Count == 0)
            return JavaCheckResult.不过(
                "程序跑起来了，但**什么也没打印**。\n" +
                "      输入是「小明 / 7 / 2」时至少要打印三行，形如：\n" +
                "          你好，小明\n" +
                "          商=3\n" +
                "          平均数=4.5\n" +
                "      确认三件事：\n" +
                "        · import java.util.Scanner; 写在文件最上面\n" +
                "        · 用 System.out.println(...) 打印\n" +
                "        · main 里真的读了三样东西（一行姓名 + 两个整数）");

        return JavaCheckResult.过($"输入 小明/7/2 时打印了 {run.行.Count} 行");
    }

    [Java检查说明("姓名是用 nextLine() 读的整行吗（顺序搞反会读到空行）")]
    public static JavaCheckResult 检查_姓名读对了(string 代码)
    {
        // 这一条专门盯 nextLine()：本课要求**先读姓名、再读数字**。
        // 顺序反了（先 nextInt 再 nextLine）是 Java 新手最常撞的一堵墙 ——
        // 那个 nextLine 会读到上一行残留的回车，拿到一个空字符串。
        var 读入 = 读入方式不对(代码);
        if (读入 != null) return 读入.Value;

        var run = 跑(代码, "小红\n7 2");
        if (run.Raw.CompileFailed) return 编译失败说明(代码, run);
        if (run.Raw.Error != null)
            return JavaCheckResult.不过(
                "程序没能正常跑完：\n      " + 缩进(run.Raw.Error) + "\n" +
                读入错误的提示(run));

        string 输出 = 看得见的输出(run);

        if (输出.Contains("小红"))
            return JavaCheckResult.过("姓名读对了：输入 小红 时打印了「你好，小红」");

        if (输出.Contains("你好，"))
            return JavaCheckResult.不过(
                "名字读出来是**空的**（打印成了「你好，」后面什么都没有）。\n" +
                "      原因几乎可以肯定是**顺序**：先 nextInt() 再 nextLine()。\n" +
                "          为什么：nextInt() 只吃掉数字本身，把这一行剩下的东西\n" +
                "          （包括结尾那个回车）**留在原地**。紧跟其后的 nextLine()\n" +
                "          读到的是「这一行剩下的部分」—— 也就是一个空字符串。\n" +
                "      本课要求的顺序正好绕开它，别调换：\n" +
                "          ① 先  String 姓名 = 输入.nextLine();      ← 读整行\n" +
                "          ② 再  int 甲 = 输入.nextInt();            ← 读数字\n" +
                "      顺便记住：这不是 bug，是 nextInt / nextLine 的分工不同 ——\n" +
                "      nextInt 读「一个词」，nextLine 读「一整行」。");

        return JavaCheckResult.不过(
            "没有看到姓名。输入的第一行是「小红」，程序应该打印「你好，小红」。\n" +
            $"      实际打印的是：\n{缩进(空则(输出))}\n" +
            "      写法：\n" +
            "          String 姓名 = 输入.nextLine();   // nextLine 读一整行\n" +
            "          System.out.println(\"你好，\" + 姓名);\n" +
            "      确认读姓名用的是 nextLine()（不是 nextInt / next），\n" +
            "      并且它是**第一个**读入动作。");
    }

    [Java检查说明("数字是从输入读进来的，还是写死在代码里的")]
    public static JavaCheckResult 检查_数字来自输入(string 代码)
    {
        // 换一组输入再跑：真读了输入 → 结果跟着变；写死答案 → 结果不变。
        var 读入 = 读入方式不对(代码);
        if (读入 != null) return 读入.Value;

        var run = 跑(代码, "小明\n10 4");
        if (run.Raw.CompileFailed) return 编译失败说明(代码, run);
        if (run.Raw.Error != null)
            return JavaCheckResult.不过("输入 10 4 时程序没能正常跑完：\n      "
                                        + 缩进(run.Raw.Error) + "\n" + 读入错误的提示(run));

        string 输出 = 看得见的输出(run);
        string 商 = 取数值(输出, "商");
        string 平均 = 取数值(输出, "平均数");

        if (商.Length == 0 || 平均.Length == 0)
            return JavaCheckResult.不过(
                "没有按要求的格式打印。\n" +
                $"      输入 小明/10/4 时你的程序打印的是：\n{缩进(空则(输出))}\n" +
                "      期望是带标签的两行：\n" +
                "          商=2\n" +
                "          平均数=7.0\n" +
                "      也就是 System.out.println(\"商=\" + (甲 / 乙)); 这样的写法。");

        if (商.Trim() == "2" && 数值等于(平均, 7.0))
            return JavaCheckResult.过("输入 10 4 得到 商=2、平均数=7.0 —— 数字确实来自输入");

        // 输入 10 4 却给出 7 和 2 的结果 → 典型的"答案写死"
        if (商.Trim() == "3" || 数值等于(平均, 4.5))
            return JavaCheckResult.不过(
                $"输入的是 小明/10/4，你的程序却给出 商={商.Trim()}、平均数={平均.Trim()} —— 那是 7 和 2 的结果。\n" +
                "      也就是说数字**没从输入读进来**，是写死在代码里的。\n" +
                "      改法：让变量的值来自输入，再拿变量去算：\n" +
                "          int 甲 = 输入.nextInt();\n" +
                "          int 乙 = 输入.nextInt();\n" +
                "          System.out.println(\"商=\" + (甲 / 乙));");

        return JavaCheckResult.不过(
            $"输入 小明/10/4 时应该是 商=2、平均数=7.0，你的程序给的是 商={商.Trim()}、平均数={平均.Trim()}。\n" +
            "      检查两件事：\n" +
            "        · 两个数是不是都读进来了（输入.nextInt() 调了两次，存进 甲 和 乙）\n" +
            "        · 算的时候用的是这两条输入，还是别的数字");
    }

    [Java检查说明("整数除法算对了没有（7 ÷ 2 在 Java 里是 3）")]
    public static JavaCheckResult 检查_整数除法(string 代码)
    {
        var 读入 = 读入方式不对(代码);
        if (读入 != null) return 读入.Value;

        var run = 跑(代码, "小明\n7 2");
        if (run.Raw.CompileFailed) return 编译失败说明(代码, run);
        if (run.Raw.Error != null)
            return JavaCheckResult.不过("程序没能正常跑完：\n      "
                                        + 缩进(run.Raw.Error) + "\n" + 读入错误的提示(run));

        string 输出 = 看得见的输出(run);
        string 商 = 取数值(输出, "商");

        if (商.Length == 0)
            return JavaCheckResult.不过(
                "没有找到「商=...」这一行。\n" +
                $"      输入 小明/7/2 时你的程序打印的是：\n{缩进(空则(输出))}\n" +
                "      要打印成 System.out.println(\"商=\" + (甲 / 乙)); 的样子。");

        if (商.Trim() == "3")
            return JavaCheckResult.过("7 / 2 = 3，整数除法算对了");

        if (数值等于(商, 3.5))
            return JavaCheckResult.不过(
                "你的商是 3.5 —— 那是**平均数**，不是整数商。\n" +
                "      两个 int 相除，Java 会自动把小数部分**丢掉**：7 / 2 就是 3。\n" +
                "      所以「商」这一行直接写 System.out.println(\"商=\" + (甲 / 乙)); 就行，\n" +
                "      不要在前面加 double —— 加了就变成 3.5 了。\n" +
                "      （这一课就是要把这两个结果摆在一起看：同一个 7 / 2，\n" +
                "        int 版是 3，小数版是 3.5。）");

        return JavaCheckResult.不过(
            $"输入 7 2 时，商应该是 3，你的程序给的是 {商.Trim()}。\n" +
            "      两个 int 相除的规则：**小数部分直接丢掉**（不是四舍五入）。\n" +
            "          7 / 2  →  3\n" +
            "          9 / 2  →  4\n" +
            "      顺便记住：只有除号两边**至少有一个是小数**时，才会得到小数结果。");
    }

    [Java检查说明("平均数是不是真的带小数（整数除法的陷阱就藏在这里）")]
    public static JavaCheckResult 检查_平均数是小数(string 代码)
    {
        // ⚠ 这一条是本课的核心检查点。
        //
        // 为什么它是"核心"：7 / 2 得 3 这件事，**编译不报错、运行不警告**。
        // 学生写 double 平均 = (甲 + 乙) / 2; 会得到 4.0 —— 类型是 double，
        // 看上去"用了小数"，值却是错的。编译器从头到尾一声不吭。
        //
        // 为什么用两组输入：只测一组的话，把 4.5 写死也能过。
        var 读入 = 读入方式不对(代码);
        if (读入 != null) return 读入.Value;

        var 通过 = new List<string>();

        foreach (var (输入, 期望) in new[] { ("小明\n7 2", 4.5), ("小明\n1 2", 1.5) })
        {
            var run = 跑(代码, 输入);
            if (run.Raw.CompileFailed) return 编译失败说明(代码, run);
            if (run.Raw.Error != null)
                return JavaCheckResult.不过($"输入 {输入简述(输入)} 时程序没能正常跑完：\n      "
                                            + 缩进(run.Raw.Error) + "\n" + 读入错误的提示(run));

            string 输出 = 看得见的输出(run);
            string 平均 = 取数值(输出, "平均数");
            if (平均.Length == 0)
                return JavaCheckResult.不过(
                    "没有找到「平均数=...」这一行。\n" +
                    $"      输入 {输入简述(输入)} 时你的程序打印的是：\n{缩进(空则(输出))}\n" +
                    "      要打印成 System.out.println(\"平均数=\" + 平均); 的样子。");

            if (!数值等于(平均, 期望))
                return JavaCheckResult.不过(
                    $"输入 {输入简述(输入)} 时，平均数应该是 {期望}，你的程序给的是 {平均.Trim()}。\n" +
                    "      ⚠ 十有八九踩了**整数除法**这个坑 —— 它是 Java 里最安静的错：\n" +
                    "      编译不报错、运行不警告，只是把小数据掉了。\n" +
                    "          (甲 + 乙) / 2          // 两个 int 相除 → 小数被丢掉：9/2 得 4\n" +
                    "          (甲 + 乙) / 2.0        // ← 右边写成小数，才有小数结果\n" +
                    "          (double)(甲 + 乙) / 2  // ← 或者把其中一个转成 double\n" +
                    "      ⚠ 特别注意这一种写法**也是错的**：\n" +
                    "          double 平均 = (甲 + 乙) / 2;\n" +
                    "      右边先按整数算出 4，再存进 double —— 存进去的仍然是 4.0。\n" +
                    "      类型写成 double 只能决定「结果装在哪」，决定不了「怎么算」。\n" +
                    "      记住一句话：**除号两边至少要有一个是小数，除法才按小数算。**");

            通过.Add($"{输入简述(输入)} → {平均.Trim()}");
        }

        return JavaCheckResult.过($"两组输入都算出了小数（{string.Join("、", 通过)}）");
    }

    [Java检查说明("输入不是整数时，程序有没有拦住它（Java 会抛异常，不是静默失败）")]
    public static JavaCheckResult 检查_非法输入被拦住(string 代码)
    {
        // ⚠ 这一条是本课的核心检查点（第二条）。
        //
        // Java 和 C++ 在这里的差别，是这一课最该讲清楚的一件事：
        //   C++   std::cin >> 甲 读到 abc：不抛异常、不打印、程序照跑，
        //         甲 被写成 0，而且后面所有 >> 全部失效 —— 全靠学生自己检查。
        //   Java  scanner.nextInt() 读到 abc：**当场抛 InputMismatchException**，
        //         不处理的话程序立刻结束（还打一串堆栈）。
        // 学生以为"输入了就是对的"，两边的后果却完全不一样 —— 所以要专门考。
        var 读入 = 读入方式不对(代码);
        if (读入 != null) return 读入.Value;

        foreach (var 输入 in new[] { "小明\nabc", "小明\n7 abc" })
        {
            var run = 跑(代码, 输入);
            if (run.Raw.CompileFailed) return 编译失败说明(代码, run);

            string 可见 = 看得见的输出(run);
            bool 崩了 = run.Raw.Error != null;

            if (崩了 && 是输入异常(run))
                return JavaCheckResult.不过(
                    $"输入「{输入简述(输入)}」时程序**抛异常**、直接结束了。\n" +
                    "      这正是这一课要讲的第二个坑：nextInt() 读到不是整数的东西会抛异常。\n" +
                    "      ⚠ 这一点和 C++ **正好相反** ——\n" +
                    "          C++ 的 cin 读失败是**一声不吭**的：不抛异常、程序继续跑，\n" +
                    "          变量被写成 0，而且后面的读入全部失效；\n" +
                    "          Java 的选择是**当场抛异常**，把问题立刻摆到你面前。\n" +
                    "      Java 这么设计是好意（错误不会被藏起来），代价是：\n" +
                    "      你不处理，程序就直接结束 —— 后面的活一行都干不了。\n" +
                    "      怎么处理（挑一个）：\n" +
                    "        ① 读之前先问一句（推荐，最干净）：\n" +
                    "              if (!输入.hasNextInt()) {\n" +
                    "                  System.out.println(\"输入无效\");\n" +
                    "                  return;\n" +
                    "              }\n" +
                    "              int 甲 = 输入.nextInt();\n" +
                    "        ② 或者用 try / catch 把异常接住：\n" +
                    "              try {\n" +
                    "                  甲 = 输入.nextInt();\n" +
                    "              } catch (java.util.InputMismatchException e) {\n" +
                    "                  System.out.println(\"输入无效\");\n" +
                    "                  return;              // ← 这个 return 不能少\n" +
                    "              }\n" +
                    "      ⚠ 用 try / catch 时注意：要是写成 int 甲;（不给初值）然后在 try 里赋值，\n" +
                    "        后面用 甲 的地方会**编译不过**（可能尚未初始化变量甲）——\n" +
                    "        Java 要求「每一条能走到使用点的路径上都赋过值」。所以干脆用 ① 的写法。");

            if (崩了)
                return JavaCheckResult.不过(
                    $"输入「{输入简述(输入)}」时程序没能正常跑完：\n      "
                    + 缩进(run.Raw.Error ?? "")
                    + (可见.Contains("输入无效") ? 没有停下来的提示(可见) : ""));

            if (可见.Contains("输入无效") && 没算结果(可见))
                continue;                          // 这一组输入过关，测下一组

            if (可见.Contains("输入无效"))
            {
                string 算出来的商 = 取数值(可见, "商");
                return JavaCheckResult.不过(
                    $"输入「{输入简述(输入)}」时你打印了「输入无效」，但**还是把结果算出来了**" +
                    $"（商={算出来的商}）。\n" +
                    "      无效输入就该到此为止：打印之后写一句 return;，不要再往下算。\n" +
                    "      （如果用的是 if / else，就把计算放进 else 里。）");
            }

            return JavaCheckResult.不过(
                $"输入「{输入简述(输入)}」时，程序应该发现这不是整数、打印「输入无效」，\n" +
                $"      但你的程序打印的是：\n{缩进(空则(可见))}\n" +
                "      这就是 nextInt() 的脾气：读不到整数就**抛 InputMismatchException**。\n" +
                "      ⚠ Java 和 C++ 在这里不一样：C++ 的 cin 读失败是静默的（变量写 0、\n" +
                "        程序继续跑），Java 是当场抛异常 —— 不处理就直接结束。\n" +
                "      改法：读完立刻检查（或者读之前先问一句）：\n" +
                "          if (!输入.hasNextInt()) {\n" +
                "              System.out.println(\"输入无效\");\n" +
                "              return;\n" +
                "          }\n" +
                "          int 甲 = 输入.nextInt();");
        }

        return JavaCheckResult.过("两组非法输入都被拦住了：打印「输入无效」之后没有再算");
    }

    // ══════════════════ 工具 ══════════════════

    /// <summary>
    /// 把学生代码里的 `new Scanner(System.in)` 换成**预置好内容的 Scanner**，然后整程序编译运行。
    ///
    /// 为什么要替换，而不是真的从终端喂输入：
    ///   ① 检查必须全自动 —— 不能要求学生在跑检查时手动敲 小明 / 7 / 2；
    ///   ② JavaRunner 起子进程时把 stdin 重定向后**立刻关掉**（见它文件头第 ⑥ 条），
    ///      学生真去读键盘只会立刻 EOF 抛 NoSuchElementException ——
    ///      那说明的是"这里没有输入"，不是"你写错了"。
    ///
    /// 替换之后，`new Scanner(System.in)` 变成 `new java.util.Scanner("小明\n7 2")`。
    /// Scanner 本来就接受字符串当输入源，实测语义和读键盘**一致**：
    /// nextInt / hasNextInt / nextLine / 抛异常的行为全都一样。
    /// 所以这一课要考的两件事（整数除法的结果、非法输入抛异常）照样考得出来。
    ///
    /// ⚠ 用**全限定名** java.util.Scanner：学生要是写的是 `new java.util.Scanner(...)`
    ///   （没有 import），换成短名字会编译不过 —— 那是我们造成的假错误。
    /// </summary>
    private static JavaRun 跑(string 代码, string 输入)
    {
        return JavaCheck.直接运行(换输入(代码, 输入));
    }

    /// <summary>把 `new Scanner(System.in)` 换成 `new java.util.Scanner("…")`。</summary>
    private static string 换输入(string 代码, string 输入)
    {
        return Regex.Replace(
            代码,
            @"new\s+(?:java\s*\.\s*util\s*\.\s*)?Scanner\s*\(\s*System\s*\.\s*in\s*\)",
            "new java.util.Scanner(" + Java字符串(输入) + ")");
    }

    private static string Java字符串(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";

    /// <summary>
    /// 读入方式不对时给学生的话（null = 没问题）。
    ///
    /// 为什么要有这一条，而不是直接去跑：文本替换**只认** `new Scanner(System.in)`
    /// 这一种写法。学生换成别的读法时，替换不中，程序就会去读那个已经被关掉的
    /// stdin，然后报 NoSuchElementException —— 那是我们造成的假错，必须在这里拦住。
    /// </summary>
    private static JavaCheckResult? 读入方式不对(string 代码)
    {
        string 净 = 去掉注释(代码);

        if (!Regex.IsMatch(净, @"\bScanner\b"))
        {
            if (Regex.IsMatch(净, @"\b(BufferedReader|InputStreamReader|System\s*\.\s*in\s*\.\s*read)\b"))
                return JavaCheckResult.不过(
                    "这一课要求用 **Scanner** 读输入，你用的是别的读法。\n" +
                    "      改法（把 BufferedReader 那一套换成）：\n" +
                    "          import java.util.Scanner;\n" +
                    "          Scanner 输入 = new Scanner(System.in);\n" +
                    "          String 姓名 = 输入.nextLine();\n" +
                    "          int 甲 = 输入.nextInt();\n" +
                    "      为什么非要用 Scanner：这一课要考的两件事都在它身上 ——\n" +
                    "      nextInt() 读到非整数会抛 InputMismatchException，\n" +
                    "      以及 nextInt / nextLine 对「一个词」和「一整行」的不同处理。");

            if (Regex.IsMatch(净, @"\bargs\s*\["))
                return JavaCheckResult.不过(
                    "你用的是命令行参数 args[...]，不是**读输入**。\n" +
                    "      这一课要练的是程序运行起来之后从输入读数据：\n" +
                    "          import java.util.Scanner;\n" +
                    "          Scanner 输入 = new Scanner(System.in);\n" +
                    "          int 甲 = 输入.nextInt();");

            return JavaCheckResult.不过(
                "代码里没有找到 Scanner —— 这一课要求数字**由程序读进来**，不能写在代码里。\n" +
                "      写法：\n" +
                "          import java.util.Scanner;              // ← 写在文件最上面\n" +
                "          Scanner 输入 = new Scanner(System.in); // ← 建一个读数器\n" +
                "          int 甲 = 输入.nextInt();               // ← 读一个整数\n" +
                "      确认 import 写在了 class **外面**（文件最上面）——\n" +
                "      写在 class 里面 Java 不认，会报「非法的类型开始」。");
        }

        // 用了 Scanner，但读的地方不是标准写法 —— 替换不中，跑起来只会 EOF。
        string 替换后 = 换输入(代码, "0");
        if (替换后 == 代码 || Regex.IsMatch(去掉注释(替换后), @"System\s*\.\s*in"))
            return JavaCheckResult.不过(
                "你用了 Scanner，但**读的那一行不是标准写法**。\n" +
                "      这一课要求写成：\n" +
                "          Scanner 输入 = new Scanner(System.in);\n" +
                "      也就是说 new Scanner(...) 的括号里要**直接写 System.in**。\n" +
                "      为什么这一条这么死：检查的时候，程序会把你写的 System.in\n" +
                "      换成一组预置好的输入（'小明'、7、2），这样检查才能全自动。\n" +
                "      要是把 System.in 先存进别的变量、或者从别的地方拿输入流，\n" +
                "      这套替换就失效了 —— 程序只会读到「没有输入」，报的错也帮不上你。\n" +
                "      改法：直接写 new Scanner(System.in)，不要绕。");

        return null;
    }

    /// <summary>程序抛的是不是"读输入读坏了"那两个异常。</summary>
    private static bool 是输入异常(JavaRun run)
    {
        string e = run.Raw.RunOutput + " " + (run.Raw.Error ?? "");
        return e.Contains("InputMismatchException") || e.Contains("NoSuchElementException");
    }

    /// <summary>程序崩了的时候，按异常类型给一句能对上号的话。</summary>
    private static string 读入错误的提示(JavaRun run)
    {
        if (是输入异常(run))
            return "      这个异常是**读输入**的时候抛出来的：\n" +
                   "        · InputMismatchException → nextInt() 读到的不是整数（比如 abc）\n" +
                   "        · NoSuchElementException → 已经没有东西可读了（读多了，或者输入提前结束）\n" +
                   "      Java 不像 C++ 的 cin 那样静默失败 —— 它当场抛异常，你不接住程序就结束。";
        return "";
    }

    /// <summary>
    /// 「打印了输入无效，却还往下算」的说明。
    ///
    /// 这是本课最常见的一种半对解法：学生确实检查了输入，但检查完只是打印一句，
    /// 没有 return —— 于是程序拿着 0（或者没读成功的变量）继续算。
    /// 实测（反面测试 C）：接着除以 0，抛 java.lang.ArithmeticException: / by zero。
    /// 光看"程序没能正常跑完"学生会以为是别的问题，所以这里必须点破。
    /// </summary>
    private static string 没有停下来的提示(string 可见) =>
        "\n      从输出看，你**已经发现**输入不对了（打印了「输入无效」），" +
        "但没有停下来：\n" +
        "          " + 可见.Trim().Replace("\n", "\n          ") + "\n" +
        "      程序接着拿 0 去算 —— 除法那一行于是抛异常（除以 0）。\n" +
        "      改法：打印之后加一句 return;（或者把后面的计算放进 else 里）：\n" +
        "          if (!输入.hasNextInt()) {\n" +
        "              System.out.println(\"输入无效\");\n" +
        "              return;                  // ← 这一句不能少\n" +
        "          }";

    /// <summary>输出里有没有算出结果（用来看"打印了输入无效之后有没有继续算"）。</summary>
    private static bool 没算结果(string 输出) =>
        取数值(输出, "商").Length == 0 && 取数值(输出, "平均数").Length == 0;

    /// <summary>
    /// 找出「标签=数字」里的数字（= 也可以写成 : 、： 或空格）。
    ///
    /// 为什么不做整行严格比对：那样学生把 商=3 写成 商 = 3 就会失败，
    /// 而这一课要考的是**算得对不对**，不是空格打得对不对。
    /// </summary>
    private static string 取数值(string 输出, string 标签)
    {
        var m = Regex.Match(输出, Regex.Escape(标签) + @"\s*[=:：]?\s*(-?\d+(?:\.\d+)?)");
        return m.Success ? m.Groups[1].Value : "";
    }

    private static bool 数值等于(string 文本, double 期望) =>
        double.TryParse(文本.Trim().Replace(',', '.'), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out double v)
        && Math.Abs(v - 期望) < 1e-9;

    /// <summary>把「小明\n7 2」写成给人看的「小明 / 7 / 2」。</summary>
    private static string 输入简述(string 输入) =>
        string.Join(" / ", 输入.Split('\n').Where(s => s.Trim().Length > 0));

    /// <summary>程序崩溃时 run.行 是空的，真正的输出要去 Raw.RunOutput 里找。</summary>
    private static string 看得见的输出(JavaRun run) =>
        run.行.Count > 0 ? run.全部输出 : run.Raw.RunOutput;

    /// <summary>编译失败时的说明 —— 先认几条 Java 特有的、这一课特别容易撞上的错。</summary>
    private static JavaCheckResult 编译失败说明(string 代码, JavaRun run)
    {
        string 信息 = run.编译信息;

        // import 写在了 class 里面（本课要求写完整程序，最容易犯）
        if (信息.Contains("非法的类型开始") || 信息.Contains("illegal start of type"))
            return JavaCheckResult.不过(
                "编译没过：import 的位置不对。\n" +
                "      **import 必须写在 class 外面**，也就是文件的最上面：\n" +
                "          import java.util.Scanner;      ← 第 1 行，class 之前\n\n" +
                "          public class Main {            ← 然后才是类\n" +
                "          }\n" +
                "      写在 class 的 { } 里面，javac 会报「非法的类型开始」。");

        // 可能尚未初始化：本课用 try/catch 接异常时几乎必然撞上
        if (信息.Contains("尚未初始化") || 信息.Contains("might not have been initialized"))
            return JavaCheckResult.不过(
                "编译没过：有个变量**可能还没赋过值**就被用了。\n" +
                "      在本课里，这几乎总是这一种情况：\n" +
                "          int 甲;                                  // ← 声明了，没给值\n" +
                "          try {\n" +
                "              甲 = 输入.nextInt();\n" +
                "          } catch (java.util.InputMismatchException e) {\n" +
                "              System.out.println(\"输入无效\");      // ← 这条路上 甲 没被赋值\n" +
                "          }\n" +
                "          System.out.println(\"商=\" + (甲 / 乙));   // ← 编译器在这里拦住你\n" +
                "      Java 要求：**每一条能走到使用点的路径上都必须赋过值**。\n" +
                "      catch 那条路没赋值 → 编译错误。这是 Java 在替你挡 bug（C++ 不会管）。\n" +
                "      怎么改（挑一个）：\n" +
                "        ① 换成先问一句的写法（推荐，最干净）：\n" +
                "              if (!输入.hasNextInt()) {\n" +
                "                  System.out.println(\"输入无效\");\n" +
                "                  return;\n" +
                "              }\n" +
                "              int 甲 = 输入.nextInt();\n" +
                "        ② 或者在 catch 里加一句 return;（不要再往下算）\n" +
                "        ③ 或者声明时给个初值 int 甲 = 0; —— 但那样非法输入会被算成 0，\n" +
                "           属于「把错误藏起来」，不推荐。\n" +
                "      编译器原文：\n" + 缩进(前几行(信息)));

        return JavaCheck.编译失败说明(run);
    }

    /// <summary>去掉注释 —— 免得注释里的 Scanner / System.in 把源码检查骗过去。</summary>
    private static string 去掉注释(string 代码)
    {
        string s = Regex.Replace(代码, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(s, @"//[^\n]*", " ");
    }

    private static string 空则(string s) => s.Trim().Length == 0 ? "（没有任何输出）" : s.Trim();

    private static string 前几行(string s)
    {
        var 行 = s.Replace("\r\n", "\n").Split('\n')
                   .Where(l => l.Trim().Length > 0).Take(6).ToList();
        return 行.Count > 0 ? string.Join("\n", 行) : "（编译器没有输出）";
    }

    private static string 缩进(string s) => s.Trim().Replace("\n", "\n      ");
}
