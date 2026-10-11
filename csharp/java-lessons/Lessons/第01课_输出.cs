// 第 01 课：输出与变量 —— 让程序开口说话
//
// ══ 这一课要教什么 ══
// 让 Java 程序把东西打到屏幕上，并且记住一件事：**类**。
//
// Java 和前面几门语言最大的不同：**不能写游离的代码**。
// Python 可以在文件顶层写 print("hi")；C++ 可以在 main 里写；
// 而 Java 里**每一样东西都必须装在一个类里面**。
// 所以第一课不讲语法细节，先把"类 → main → 输出"这条链路走通。
//
// ══ 检查设计说明（给维护者）══
// 这一课的核心检查点是**变量真的参与了计算**（不是把答案写死）。
// 学生完全可以写 System.out.println(13); 也能输出对 —— 那样学不到「变量」。
// 抓法：用**文本替换**改掉声明里的初值，重新编译，看输出会不会跟着变。
//   真用变量 → 变成 300；写死 → 还是 13。
//
// 为什么用文本替换而不是探针：Java 的局部变量在 main 里面，
// 外部方法看不见 —— 我第一版就是想用探针访问，行不通（和 C++ 那边踩的是同一个坑）。

using System.Linq;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("01", "第 01 课：输出与变量 —— 让程序开口说话",
    "1. 写一个完整的 Java 程序（要有 public class Main 和 main 方法）\n" +
    "2. 打印一行文字：你好，Java\n" +
    "3. 定义 int 变量 甲 = 6、int 变量 乙 = 7，打印它们的和\n" +
    "   要求真的用变量相加，不要把 13 写死")]
public static class 第01课
{
    public static string 模板 => """
// 第 01 课：输出与变量 —— 让程序开口说话
//
// ─────────────────────────────────────────────────────
//  先介绍：程序得先能"说话"
// ─────────────────────────────────────────────────────
//
// 一个程序如果什么都不输出，你就**看不见它在干什么** —— 算得对不对、
// 走到哪一步了、是不是根本没跑，你全不知道。所以每一门语言的第一课
// 都是同一件事：**把东西打到屏幕上**。
//
// 第二件事是**记住数据**：
//
//     System.out.println(6 + 7);        ← 只能算这一次
//     int 甲 = 6;                        ← 把 6 存进一个叫「甲」的格子
//     int 乙 = 7;
//     System.out.println(甲 + 乙);       ← 以后想算几次算几次
//
// 变量的价值不在"少打几个字"，而在**给数据起了名字**：以后要改，只改
// 声明那一行，用到它的地方全都跟着变。把答案 13 写死，这个程序就永远
// 只会算这一道题。
//
// ⚠ Java 和前面几门语言最不一样的一点：**什么都得写在类里面**。
//   Python 可以在文件顶层写 print("hi")；C 可以在 main 里直接写；而 Java
//   里**每一样东西都必须装在一个类里面** —— 没有游离的代码。所以第一课
//   先把这条链路走通：**类 → main 方法 → 语句**，三层缺一不可。
//   忘了这条会看到：错误: 需要类、接口、批注类型、枚举、记录、方法或字段
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
// ① 一个能跑的最小程序
//
//     public class Main {
//         public static void main(String[] args) {
//             System.out.println("你好，Java");
//         }
//     }
//
//   public class Main 是**外壳**（名字必须和文件名一致），main 是**入口**
//   （程序从这一行开始跑），里面那条才真正干活。
//
// ② println 和 print 差一个换行；文字要用双引号包起来
//
//     System.out.println("你好，Java");     打了之后**换行**（ln = line）
//     System.out.print("你好，Java");       打了之后**不换行**
//     System.out.println(你好，Java);       ✗ 编译器会去找一个叫「你好」的东西
//
//   引号里的内容**原样打出来**，包括空格和标点 —— 一个字符都不会变。
//
// ③ 变量：先写类型，再写名字，再给初值
//
//     int 甲 = 6;                            ← int 是"装整数的格子"
//     int 乙 = 7;
//     System.out.println(甲 + 乙);           ⇒ 13
//
//   ⚠ 类型**必须写**。Python 写 甲 = 6 就行，Java 必须写 int 甲 = 6;
//   ⚠ 等号是**英文**的 =，分号是**英文**的 ;，中文标点编译器不认。
//
// ④ + 有两种意思，看两边是什么
//
//     System.out.println(甲 + 乙);           ⇒ 13      ← 两边都是数字：加法
//     System.out.println("和=" + 甲);        ⇒ 和=6    ← 有一边是文字：拼接
//     System.out.println("和=" + 甲 + 乙);   ⇒ 和=67   ✗
//     System.out.println("和=" + (甲 + 乙)); ⇒ 和=13   ✓ 想先算加法就加括号
//
// ⑤ 变量可以改：甲 = 甲 + 1; 之后 甲 就是 7（右边先算，再存回左边的格子）。
// ⑥ 注释：// 后面这一行不算代码，编译器直接跳过。
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：类名和文件名对不上
//
//     public class main {            ← 文件叫 Main.java，这里却写了小写 main
//
// 你会看到：错误: 类 main 是公共的, 应在名为 main.java 的文件中声明
// 为什么：**一个 .java 文件里的 public 类，名字必须和文件名一模一样**，
//   连大小写都要一样。Java 的规矩，别的语言没有这一条。
// ⇒ 你的答题文件叫 Main.java，所以类只能叫 `public class Main`。
//
// ⚠ 坑 2：分号打成了中文的 ；
//
//     System.out.println("你好，Java")；        ← 最后这个是全角的 ；
//
// 你会看到：错误: 非法字符: '\uff1b'      （\uff1b 就是全角分号的编号）
// 为什么：javac 只认**半角符号**。中文输入法下打标点很容易出这个错，而且
//   它长得和 ; 几乎一样，肉眼很难发现。同类还有中文逗号、括号、引号。
// ⇒ 写 Java 代码时把输入法切到英文，或者至少切到"英文标点"。
//
// ⚠ 坑 3：引号用了中文的 “ ”，或者文字里的标点跟题目不一样
//
//     System.out.println(“你好，Java”);
//
// 你会看到：错误: 进行语法分析时已到达文件结尾
// 为什么：编译器不认全角的 “ ”，它以为这个字符串到文件末尾都没结束。
//   ⚠ 这条报错的**行号会指到最后一行**，看着像"别的地方出错了"——
//     其实问题就在你写引号的那一行。
// ⇒ 它还有个**更隐蔽**的版本：字符串里的逗号用了中文的
//     System.out.println("你好，Java");     ← 这样**不报错**
//   编译完全通过，只是打出来的是「你好，Java」而不是「你好,Java」——
//   检查一比对文字就红了。**字符串里的标点也要和题目一模一样。**
//
// ⚠ 坑 4：把「拼接」当成了「加法」
//
//     System.out.println("和=" + 甲 + 乙);       ⇒ 和=67      ✗
//
// 你会看到：**什么都不报**，程序跑得好好的，只是结果不对。
// 为什么：+ 从左往右算。先算 "和=" + 甲 —— 左边是文字，于是这一步变成
//   **拼接**，得到 "和=6"；再 + 乙 还是拼接 → "和=67"。一旦 + 的某一边是
//   String，它就变成拼接，而且**改不回来**了。
// ⇒ 想先算加法就把算式用括号圈起来："和=" + (甲 + 乙)
//   ⚠ 它的错法是**静默的**：不报错、不警告，只有结果悄悄不对。
//
// ⚠ 坑 5：语句写在了类外面
//
//     System.out.println("你好，Java");        ← 文件里就这么孤零零一句
//
// 你会看到：错误: 需要类、接口、批注类型、枚举、记录、方法或字段
// 为什么：Java **没有游离的代码**，每条语句都必须待在「类 → 方法 → 语句」
//   这三层里面。忘了写 main、或者花括号配对搞错，也会得到同一句话。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
// ⚠ 要写一个**完整程序**：public class Main + main 方法，两样都要有。
//
//   1. 打印一行：你好，Java
//   2. 定义两个 int 变量：甲 = 6、乙 = 7
//   3. 打印它们的和（应该是 13）
//      ⚠ 必须**真的用变量相加**，不要把 13 写死：
//            System.out.println(13);              ✗ 检查会看穿（见下）
//            System.out.println(甲 + 乙);         ✓
//        为什么较这个真：把答案写死，这个程序就只能算这一道题。
//
// ⚠ 记住三件事（这一课的门槛全在这儿）：
//   · 入口方法必须写成 public static void main(String[] args)
//   · 打印用 System.out.println(...)
//   · 每条语句末尾要有分号 ;（半角）
//
// ⚠ 一个 .java 文件里 public 类只能有一个，而且必须叫 Main。
//
// 期望输出两行：你好，Java / 13
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/java-lessons 检查 01
//
//   看这一课要干什么：dotnet run --project csharp/java-lessons 01
//   看答案（先自己写一遍）：dotnet run --project csharp/java-lessons 答案 01
//
//   ⚠ 检查里有一条会**偷偷把你的 甲 = 6 改成 甲 = 100、乙 改成 200 再跑一遍**，
//     看和会不会变成 300。写死 13 的写法就是在那里露馅的。
//
// ── 在下面写 ──
public class Main {
    public static void main(String[] args) {
        // 在这里写

    }
}
""";

    public static string 参考答案 => """
public class Main {
    public static void main(String[] args) {
        System.out.println("你好，Java");

        int 甲 = 6;
        int 乙 = 7;
        System.out.println(甲 + 乙);
    }
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("程序能不能编译、能不能跑起来")]
    public static JavaCheckResult 检查_程序能运行(string 代码)
    {
        var run = JavaCheck.直接运行(代码);

        if (run.Raw.CompileFailed)
        {
            // 类名不符是 Java 特有的第一道门槛，单独讲清楚
            string 信息 = run.编译信息;
            if (信息.Contains("应在名为") || 信息.Contains("should be declared in a file"))
                return JavaCheckResult.不过(
                    "类名和文件名对不上。\n" +
                    "      **Java 要求：public 的类名必须和文件名完全一致。**\n" +
                    "      你的答题文件叫 Main.java，所以类名必须是 Main：\n" +
                    "          public class Main {   ← 和文件名一致（区分大小写）\n" +
                    "      这一条是 Java 特有的，Python 和 C++ 都没有这个要求。");
            return JavaCheck.编译失败说明(run);
        }

        if (run.Raw.Error != null)
            return JavaCheckResult.不过("程序没能正常运行：\n      " + run.Raw.Error);

        if (run.行.Count == 0)
            return JavaCheckResult.不过(
                "程序跑起来了，但**什么也没打印**。\n" +
                "      要做到这一点需要：\n" +
                "        ① 一个 public class Main\n" +
                "        ② 里面一个 public static void main(String[] args)\n" +
                "        ③ 用 System.out.println(...) 真的打印点什么\n" +
                "      ⚠ Java 不会自动执行任何东西 —— main 方法就是程序的入口。");

        return JavaCheckResult.过($"程序正常运行，打印了 {run.行.Count} 行");
    }

    [Java检查说明("有没有按题目要求定义变量（而不是直接把答案写出来）")]
    public static JavaCheckResult 检查_定义了变量(string 代码)
    {
        // 这条看的是**声明语法**。
        // 之所以敢只看语法：下一条检查（变量真的在用）已经用文本替换严格验证过了 ——
        // 它改掉初值重新编译，看输出会不会跟着变。有那条兜底，
        // 这条的职责就只是「提示你还没把变量写出来」。
        //
        // ⚠ 不用探针去"触摸"变量：学生的 甲/乙 在 main 里面，
        //   类里的别的成员方法看不见它们，探针必然编译失败，
        //   而且报出来的话是错的（说"你没写 int"，其实写了）。
        var 声明甲 = System.Text.RegularExpressions.Regex.IsMatch(
            代码, @"int\s+甲\s*=\s*-?\d+\s*;");
        var 声明乙 = System.Text.RegularExpressions.Regex.IsMatch(
            代码, @"int\s+乙\s*=\s*-?\d+\s*;");

        if (声明甲 && 声明乙)
            return JavaCheckResult.过("甲 和 乙 都按 int 类型声明了");

        if (!声明甲 && !声明乙)
            return JavaCheckResult.不过(
                "还没有声明「甲」和「乙」这两个变量。\n" +
                "      Java 的变量必须先写**类型**才能用：\n" +
                "          int 甲 = 6;\n" +
                "          int 乙 = 7;\n" +
                "      ⚠ 这一点和 Python 不同 —— Python 写 甲 = 6 就行，\n" +
                "        而 Java 必须写 int 甲 = 6;（告诉编译器它装的是整数）。\n" +
                "      另外注意分号 ; 和**英文**等号，中文标点编译器不认。");
        if (!声明甲)
            return JavaCheckResult.不过("没有找到「甲」的声明。写法：int 甲 = 6;");
        return JavaCheckResult.不过("没有找到「乙」的声明。写法：int 乙 = 7;");
    }

    [Java检查说明("变量是不是真的参与了计算（不是把答案写死）")]
    public static JavaCheckResult 检查_变量真的在用(string 代码)
    {
        // ⚠ 这一条是本课的核心检查点。
        //
        // 学生完全可以写 System.out.println(13); 也能输出对 —— 但那学不到"变量"。
        // 怎么分辨：**把声明里的初值改掉，重新编译，看输出会不会跟着变。**
        //   真用变量 → 输出跟着变（300）
        //   写死答案 → 输出不变（13）
        // 这个办法只看输出，不需要访问变量本身，所以变量在 main 里也成立。
        string 改过 = 改写初值(代码, "甲", 100);
        改过 = 改写初值(改过, "乙", 200);

        if (改过 == 代码)
            return JavaCheckResult.不过(
                "没有找到可以改写的变量声明。\n" +
                "      期望看到这样的写法：\n" +
                "          int 甲 = 6;\n" +
                "      确认变量名是「甲」和「乙」，而且初值写的是数字。");

        var run = JavaCheck.直接运行(改过);
        if (!run.编译通过) return JavaCheck.编译失败说明(run);

        string 输出 = run.全部输出;
        if (输出.Contains("300"))
            return JavaCheckResult.过("甲 和 乙 确实参与了计算 —— 改成 100 和 200 之后，和变成了 300");

        return JavaCheckResult.不过(
            "甲 和 乙 好像没有真的参与计算。\n" +
            "      我把它们的初值改成 甲 = 100、乙 = 200 再跑，\n" +
            "      期望看到 300（100 + 200），但得到的是：\n" +
            $"          {(输出.Trim().Length == 0 ? "（空）" : 输出.Trim().Replace("\n", "\n          "))}\n" +
            "      这通常意味着和是**写死的**，比如直接写了：\n" +
            "          System.out.println(13);\n" +
            "      改法：让变量真的参与进来 ——\n" +
            "          System.out.println(甲 + 乙);");
    }

    [Java检查说明("有没有打印那行文字")]
    public static JavaCheckResult 检查_打印了文字(string 代码)
    {
        var run = JavaCheck.直接运行(代码);
        if (!run.编译通过) return JavaCheck.编译失败说明(run);
        if (run.全部输出.Contains("你好，Java"))
            return JavaCheckResult.过("打印了「你好，Java」");
        return JavaCheckResult.不过(
            "没有看到「你好，Java」这行文字。\n" +
            $"      实际打印的是：\n          {run.全部输出.Replace("\n", "\n          ")}\n" +
            "      检查两点：\n" +
            "        · 文字要放在**双引号**里：\"你好，Java\"\n" +
            "        · 逗号要是**英文逗号** ，不是中文的 ，（这是最隐蔽的坑）");
    }

    [Java检查说明("和算得对不对")]
    public static JavaCheckResult 检查_和正确(string 代码)
    {
        var run = JavaCheck.直接运行(代码);
        if (!run.编译通过) return JavaCheck.编译失败说明(run);

        var 数字行 = run.行.Where(l => l.Trim().Length > 0
                                       && l.Trim().All(c => char.IsDigit(c) || c == '-')
                                       && !l.Contains("你好")).ToList();
        if (数字行.Count == 0)
            return JavaCheckResult.不过(
                "没有看到打印出来的和。\n" +
                "      6 + 7 应该是 13，用 System.out.println(甲 + 乙); 打印出来。");
        if (数字行.Any(l => l.Trim() == "13"))
            return JavaCheckResult.过("6 + 7 = 13，正确");
        return JavaCheckResult.不过(
            $"和不对：期望 13，实际打印了 {string.Join("、", 数字行.Select(l => l.Trim()))}。\n" +
            "      检查一下变量的值（甲 = 6，乙 = 7）和相加的写法。");
    }

    [Java检查说明("main 的写法是不是标准的（换台机器也能跑）")]
    public static JavaCheckResult 检查_main写法(string 代码)
    {
        // ⚠ 这条检查的存在理由（同事实测发现的）：
        //
        // JDK 25 **放宽了 main 的写法** —— 漏 static、漏 public、
        // 甚至没有参数，实测都能编译能跑！
        //
        //     public void main(String[] a)     ← 漏 static，JDK 25 上能跑
        //     static void main(String[] a)     ← 漏 public，能跑
        //     public static void main()        ← 没参数，能跑
        //
        // 后果：学生在这台机器上怎么写都能过，**换到 JDK 21 或别的环境就撞墙**。
        // 不提醒的话，我们等于放过了一个真实的错误写法 ——
        // 这和「未初始化变量碰巧算对」是同一类问题：通过了，但学到的是错的。
        //
        // 注意：**不判错**（它确实能跑），只提醒。
        string 提醒 = ExplainJava.MainSignatureNote(代码);
        if (提醒.Length == 0)
            return JavaCheckResult.过("main 写法标准：public static void main(String[] args)");
        return JavaCheckResult.过("（提醒，不算错）" + 提醒);
    }

    /// <summary>
    /// 把「int 变量名 = 数字」里的数字换成新值。
    ///
    /// 为什么要做文本改写：Java 里没法从外部读到一个方法内部的局部变量，
    /// 而"变量有没有真的参与计算"恰恰是本课要考的东西。
    /// 改掉初值再重编译，用**输出的变化**来间接验证 —— 绕开作用域限制的办法。
    /// </summary>
    private static string 改写初值(string 代码, string 变量名, int 新值)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            代码,
            @"(int\s+" + System.Text.RegularExpressions.Regex.Escape(变量名) + @"\s*=\s*)-?\d+",
            "${1}" + 新值);
    }
}
