// 第 19 课：算法复杂度 —— 为什么 n=100000 时不能两两比一遍
//
// ══ 这一课要教什么 ══
// 同一道题（把 1..n 加起来），这一课写三个函数：一个循环一遍、两层循环、
// 直接套公式。它们**答案完全一样**，但在 n=100000 时差距是：
//
//     O(1)   直接套公式        一瞬间（和 n 没关系）
//     O(n)   一个循环          约 0.2 毫秒
//     O(n²)  两层循环          约 7 秒        ← 慢几万倍
//
// 这是本课唯一要真正记住的事：**程序能不能跑完，取决于它的增长速度**。
// 竞赛题里 n 给到 100000，就是在暗示你「O(n²) 不行」。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点一：**`平方和` 里必须真的有两层循环，而且不许去调用 公式 / 一重和。**
//   偷懒解法：`return 公式(n);`（答案对、但这一课就白上了）。
//   防法：去掉注释后切开它的函数体，数 for/while 的个数，并查有没有调用别的函数。
// 核心检查点二：**`平方和(20000)` 必须在时限内跑完。**
//   真写两层循环是 4 亿次比较 —— 本机约 0.4 秒，能过；
//   把 n 再放大到 100000 就是 100 亿次，会撞上运行器的 60 秒超时（见模板里的表格）。
//
// ⚠ 给维护者：标记一律用**前后各出现一次**的写法（`"和0|" … "和0| "`），
//   取值只读「开头标记之后、下一个竖线之前」那一小段（照第 17 课修好的写法）。
//   千万不要用「带数字的标记名」去当 IndexOf 的判据（`和1|` 会命中 `和10|`）。

using System.Collections.Generic;
using HybridEngineLight;
using HybridEngineLight.CppLessons;

[课程("19", "第 19 课：算法复杂度 —— 为什么 n=100000 时不能两两比一遍",
    "1. 写 long long 一重和(long long n)，用一个循环把 1+2+…+n 加起来（O(n)）\n" +
    "2. 写 long long 公式(long long n)，直接用 n*(n+1)/2 算出来（O(1)）\n" +
    "3. 写 long long 平方和(long long n)，用**两层循环**把 1+2+…+n 加出来（O(n²)）—— 故意写慢，好亲眼看见差距")]
public static class 第19课
{
    public static string 模板 => """
// 第 19 课：算法复杂度 —— 为什么 n=100000 时不能两两比一遍
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**同一个答案，不同的算法，快的快死、慢的慢死**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    O(1) / O(n) / O(n²) —— 怎么数循环的次数，以及它为什么决定成败
//
//  需要先会
//    第 04 课：循环（这一课数的就是循环）
//
// ─────────────────────────────────────────────────────
//  先介绍：三份答案一样的代码
// ─────────────────────────────────────────────────────
//
// 求 1+2+…+n，有三种写法：
//
//     一个循环            加 n 次                      O(n)
//     两层循环            加 n×n 次（故意写慢）        O(n²)
//     套公式 n(n+1)/2     算一次                      O(1)
//
// 结果一模一样。n 小的时候也都是一瞬间 —— 所以**必须拿大 n 去试**，
// 才能看出谁快谁慢。
//
// ─────────────────────────────────────────────────────
//  一、怎么数「复杂度」：数循环跑了几次
// ─────────────────────────────────────────────────────
//
//     for (int i = 0; i < n; i++) { … }              跑 n 次        ⇒ O(n)
//     for (i…) for (j…) { … }                       跑 n×n 次      ⇒ O(n²)
//     for (i = 1; i < n; i *= 2) { … }              跑 log₂n 次    ⇒ O(log n)
//     一句 n*(n+1)/2                                 跑 1 次        ⇒ O(1)
//
// **只数最内层那句话被执行的次数**，再把它写成 n 的式子，去掉系数和低次项：
//     3n² + 5n + 7  ⇒  O(n²)       （n 大了以后 n² 说了算）
//
// ─────────────────────────────────────────────────────
//  二、为什么 O(n²) 在 n=100000 时不能用
// ─────────────────────────────────────────────────────
//
// 一句话：**n 翻 10 倍，O(n) 慢 10 倍，O(n²) 慢 100 倍。**
//
//     复杂度      n=1000      n=10000      n=100000
//     O(1)        一瞬间      一瞬间        一瞬间
//     O(n)        约 1 微秒    约 10 微秒     约 0.1 毫秒
//     O(n²)       约 1 毫秒    约 0.1 秒      约 10 秒
//
// （这张表是量级估算：假设机器一秒能做约 10 亿次简单操作。
//   本机实测：两层循环加到 100000 要 6~10 秒 —— 和估算对得上。）
//
// ⚠ 竞赛里给的时间限制通常是 1 秒。看到 n = 100000，
//   就该立刻反应过来：「O(n²) 是 10^10 次，肯定超时，得换个算法」。
//
// ─────────────────────────────────────────────────────
//  三、看到 n 的范围，倒推该用什么算法
// ─────────────────────────────────────────────────────
//
//     n ≤ 10          O(n!) 都行（最多 300 万次）
//     n ≤ 500         O(n³) 勉强（1.25 亿次）
//     n ≤ 5000        O(n²) 可以（2500 万次）
//     n ≤ 10^6        O(n log n) 或 O(n)
//     n ≤ 10^8        O(n) 还得常数小
//
// 竞赛题里 **n 的范围就是提示**：它写着 n ≤ 100000，就是在说
// 「O(n²) 不行，去想想 O(n log n) 或 O(n)」。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：n 大起来时 int 会溢出
// ─────────────────────────────────────────────────────
//
//     int 和 = 0;
//     for (int i = 1; i <= 100000; i++) { 和 += i; }      ✗ 和 会变成负数
//
// 你会看到：**编译通过、没有警告**，程序也正常结束，只是结果是**负数**。
//
// 为什么：1+2+…+100000 = 5000050000，而 int 最大只能到 2147483647。
//   超出的部分**悄悄绕回负数**（这叫溢出）—— C++ 不会拦你。
//   ⇒ 大数一律用 `long long`（能装到约 9.2×10^18）。
//
// 附：用 int 硬算再乘的写法，编译器有时会警告
//
//     warning: integer overflow in expression of type 'int' results in …
//
// 但**很多时候它一个字都不说** —— 别指望编译器替你发现。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   空间复杂度、递归的复杂度 → 这一课只数循环。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  写三个函数（不用写 main、不用写 #include）。都返回 long long：
//
//    1. long long 一重和(long long n)      一个循环，O(n)
//    2. long long 公式(long long n)        直接 n*(n+1)/2，O(1)
//    3. long long 平方和(long long n)      **两层循环**加出同一个答案，O(n²)
//
//  ⚠ 三个都要返回 1+2+…+n（参数和返回值都用 long long，别用 int —— 见上面那个坑）。
//  ⚠ `平方和` 故意写得慢，**就是**要让你在 n 大时看见差距；
//    但答案必须对，而且**不许**偷懒去调用 公式 或 一重和。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
long long 一重和(long long n)
{
}

long long 公式(long long n)
{
}

long long 平方和(long long n)
{
}
""";

    public static string 参考答案 => """
// 参考答案 —— 三种复杂度

// 1. O(n)：加 n 次。n=100000 时约零点几毫秒
long long 一重和(long long n) {
    long long 累加 = 0;
    for (long long i = 1; i <= n; i++) {
        累加 += i;
    }
    return 累加;
}

// 2. O(1)：和 n 一点关系都没有，几次算术就出答案
//    ⚠ n*(n+1) 在 n=100000 时是 10^10，远超 int 的 21 亿 ⇒ 必须用 long long
long long 公式(long long n) {
    return n * (n + 1) / 2;
}

// 3. O(n²)：**故意**用两层循环把同一件事做 n 倍
//    内层那一趟其实只加了一个数，外面白跑了 n 遍 —— 这就是"慢算法"的样子
long long 平方和(long long n) {
    long long 累加 = 0;
    for (long long i = 1; i <= n; i++) {
        for (long long j = 1; j <= n; j++) {
            if (j == i) {          /* 只有 j 等于 i 的那一次才真的加 */
                累加 += i;
            }
        }
    }
    return 累加;
}
""";

    // ══════════════════ 检查 ══════════════════

    [检查说明("三个函数都在，签名也对（都要 long long）")]
    public static CheckResult 检查_函数都在(string 代码)
    {
        var 缺 = new List<string>();
        foreach (var 名 in new[] { "一重和", "公式", "平方和" })
        {
            var run = 编译并运行(代码, null, "volatile long long 丢弃 = " + 名 + "(1);\n");
            if (!run.编译通过)
            {
                if (run.编译信息.Contains(名) &&
                    (run.编译信息.Contains("was not declared") || run.编译信息.Contains("not declared")))
                    缺.Add("      " + 名);
                else
                    return CppCheck.编译失败说明(run);
            }
        }
        if (缺.Count == 0) return CheckResult.过("三个函数都在，签名也对");

        return CheckResult.不过(
            "有函数没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      这一课要求的三个签名是（注意返回类型是 **long long**，不是 int）：\n" +
            "          long long 一重和(long long n)\n" +
            "          long long 公式(long long n)\n" +
            "          long long 平方和(long long n)\n" +
            "      为什么必须 long long：1+2+…+100000 = 5000050000，\n" +
            "      int 最大只到 2147483647 —— 用 int 会悄悄变成负数。");
    }

    [检查说明("三份答案都是 1+2+…+n（小 n 时三份必须一样）")]
    public static CheckResult 检查_答案一致(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停 != null) return CheckResult.不过(停);

        return 比一比(run,
            ("和0|", "0", "一重和(0)　一个数都不加，应当是 0"),
            ("和1|", "1", "一重和(1)"),
            ("和10|", "55", "一重和(10)"),
            ("式10|", "55", "公式(10)　和 一重和(10) 应当一样"),
            ("方10|", "55", "平方和(10)　三个函数算的是同一件事"),
            ("和万|", "50005000", "一重和(10000)　大一点也要对"));
    }

    [检查说明("平方和(20000) 必须算对而且要跑得完")]
    public static CheckResult 检查_大n(string 代码)
    {
        var run = 编译并运行(代码, null,
            "volatile long long 甲 = 一重和(100000);\n" +
            "volatile long long 乙 = 平方和(20000);\n" +
            "std::cout << \"大方|\" << 乙 << \"|大方| \";\n");
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        if (run.Raw.Error != null)
            return CheckResult.不过(
                "程序没跑完：" + run.Raw.Error + "\n" +
                "      ⚠ 如果上面写的是「超时」，那正是这一课要你亲眼看见的事：\n" +
                "        `平方和` 是 **O(n²)** —— n 变成 20000 时要跑 4 亿次，\n" +
                "        n 再大 5 倍就是 100 亿次，一台机器要跑好几分钟。\n" +
                "      这一课的意义就在这里：**答案一样，快慢差几万倍**。\n" +
                "      竞赛题给 n ≤ 100000，就是在说「O(n²) 不行」。\n" +
                "      （想让程序跑完，就把两层循环换成一层 —— 那才是 O(n)。）");

        string 得到 = 取一段(run, "大方|");
        if (得到 == "200010000")
            return CheckResult.过("一重和(100000) 与 平方和(20000) 都在时限内跑完了");
        return CheckResult.不过(
            "平方和(20000) 应当是 200010000（1+2+…+20000），实际得到 " + 显示(得到) + "。\n" +
            "      内层循环的条件和加法要再对一遍 —— 两层循环也必须算对。");
    }

    [检查说明("★ 平方和里要真的有两层循环，而且不许去调用 公式/一重和")]
    public static CheckResult 检查_平方和是真两层(string 代码)
    {
        // ⚠ 核心检查点（防偷懒）：这一课要的就是"慢算法"本身，
        //   `return 公式(n);` 虽然答案对，但这一课就白上了。
        string 净 = 去掉注释(代码);
        string 体 = 函数体(净, "平方和");
        if (体 == null)
            return CheckResult.不过("找不到「平方和」的函数体 —— 花括号配对了吗？");

        var 问题 = new List<string>();
        int 循环数 = 数循环(体);
        if (循环数 < 2)
            问题.Add("      平方和：只数到 " + 循环数 + " 个循环，这一课要的是**两层**（O(n²)）");
        if (体.Contains("公式(") || 体.Contains("一重和("))
            问题.Add("      平方和：里面调用了 公式 / 一重和 —— 那就不是 O(n²) 了，这一课要你亲手写出那个慢");

        if (问题.Count == 0)
            return CheckResult.过("平方和里确实有两层循环，也没偷用别的函数");
        return CheckResult.不过(
            "检查了去掉注释后的源码：\n" + string.Join("\n", 问题) + "\n" +
            "      这一课要你**故意写一个慢算法**，这样三份代码的差距才看得见：\n" +
            "          一重和   O(n)    加了 n 次\n" +
            "          平方和   O(n²)   外面白跑 n 遍（两层循环套着）\n" +
            "          公式     O(1)    一次算术\n" +
            "      写法就是老老实实套两层 for，内层里做一次加法 —— 简单，但慢得真实。");
    }

    // ══════════════════ 跑一次、比一比 ══════════════════

    private const string 调用点 = """
std::cout << "和0|" << 一重和(0) << "|和0| \n";
std::cout << "和1|" << 一重和(1) << "|和1| \n";
std::cout << "和10|" << 一重和(10) << "|和10| \n";
std::cout << "式10|" << 公式(10) << "|式10| \n";
std::cout << "方10|" << 平方和(10) << "|方10| \n";
std::cout << "和万|" << 一重和(10000) << "|和万| \n";
""";

    private static string 上次的代码;
    private static CppRun 上次的运行;

    private static CppRun 跑一次(string 代码)
    {
        if (上次的运行 != null && 上次的代码 == 代码) return 上次的运行;
        上次的代码 = 代码;
        上次的运行 = 编译并运行(代码, null, 调用点);
        return 上次的运行;
    }

    /// <summary>
    /// 编译并运行，**环境抽风时重试一次**。
    ///
    /// ⚠ 为什么需要它（实测踩到的）：运行器每次在一个全新的临时目录里编译，
    ///   而 Windows 上刚建出来的目录偶尔会被扫毒/索引短暂占住，g++ 会报
    ///       ld.exe: cannot open output file …prog.exe: No such file or directory
    ///       cc1plus.exe: fatal error: …main.cpp: No such file or directory
    ///   这两种都是**环境**问题（而且往往只发生在一次检查的第一次编译上），
    ///   不是学生代码的问题。同一份代码重跑一次就好了 ——
    ///   可要是不重试，学生看到的是"编译没能通过"，会一头雾水地去改本来没错的代码。
    /// </summary>
    private static CppRun 编译并运行(string 代码, string 骨架, string 调用点)
    {
        var run = CppCheck.编译并运行(代码, 骨架, 调用点);
        if (run.编译通过) return run;
        if (!环境抽风(run)) return run;                 // 真的是学生的错，别重试
        System.Threading.Thread.Sleep(300);
        var 再来 = CppCheck.编译并运行(代码, 骨架, 调用点);
        return 再来.编译通过 ? 再来 : run;
    }

    private static bool 环境抽风(CppRun run) =>
        run.编译信息.Contains("No such file or directory") ||
        run.编译信息.Contains("cannot open output file");

    private static string 先别急着比(CppRun run)
    {
        if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run).Detail;
        if (run.Raw.Error != null)
            return
                "程序没跑完就退出了：" + run.Raw.Error + "\n" +
                "      这一课最容易出问题的地方是**溢出**：\n" +
                "      1+2+…+10000 = 50005000 已经超出 short，100000 的和更是超出 int ——\n" +
                "      三个函数都用 `long long`，别用 int。";
        return null;
    }

    private static CheckResult 比一比(CppRun run, params (string 标记, string 期望, string 说明)[] 用例们)
    {
        var 错 = new List<string>();
        foreach (var (标记, 期望, 说明) in 用例们)
        {
            string 得到 = 取一段(run, 标记);
            if (得到 == null) 错.Add("      " + 说明 + "：程序没有给出这一组的输出");
            else if (得到 != 期望) 错.Add("      " + 说明 + "：得到 " + 显示(得到) + "，应该是 " + 显示(期望));
        }
        if (错.Count == 0) return CheckResult.过("这几组都对");

        return CheckResult.不过(
            "结果不对：\n" + string.Join("\n", 错) + "\n" +
            "      三份代码算的是同一件事（1+2+…+n），所以答案必须一样：\n" +
            "          一重和  for (i = 1; i <= n; i++) 和 += i;\n" +
            "          公式    return n * (n + 1) / 2;\n" +
            "          平方和  两层循环，内层里 和 += i;\n" +
            "      ⚠ n=0 时应当是 0（循环一次都不进）。");
    }

    /// <summary>
    /// 取「标记|」后面那一格内容，直到**下一根竖线**为止。
    ///
    /// ⚠ 这一课的标记名里带数字（和1|、和10|），所以**只能**用带竖线的完整标记去找：
    ///   只写名字的话，找 `和1|` 会先命中 `和10|` 的前半截，取值就多带一根竖线。
    ///   这一处和第 17 课是同一类坑：判据必须挑「只属于这个标记」的东西。
    /// </summary>
    private static string 取一段(CppRun run, string 标记)
    {
        foreach (var 行 in run.行)
        {
            int 起 = 行.IndexOf(标记, System.StringComparison.Ordinal);
            while (起 >= 0)
            {
                if (起 == 0 || 行[起 - 1] == ' ' || 行[起 - 1] == '\t')
                {
                    int 头 = 起 + 标记.Length;
                    int 尾 = 行.IndexOf('|', 头);
                    if (尾 < 0) 尾 = 行.Length;
                    return 行.Substring(头, 尾 - 头).Trim();
                }
                起 = 行.IndexOf(标记, 起 + 1, System.StringComparison.Ordinal);
            }
        }
        return null;
    }

    private static string 显示(string s) => s == null ? "（没拿到）" : (s.Length == 0 ? "（空）" : s);

    private static string 去掉注释(string s)
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

    private static string 函数体(string 净源码, string 函数名)
    {
        int 名 = 净源码.IndexOf(函数名 + "(", System.StringComparison.Ordinal);
        if (名 < 0) return null;
        int 起 = 净源码.IndexOf('{', 名);
        if (起 < 0) return null;
        int 深 = 0;
        for (int i = 起; i < 净源码.Length; i++)
        {
            if (净源码[i] == '{') 深++;
            else if (净源码[i] == '}')
            {
                深--;
                if (深 == 0) return 净源码.Substring(起 + 1, i - 起 - 1);
            }
        }
        return null;
    }

    /// <summary>数一个函数体里出现了几个 for / while。</summary>
    private static int 数循环(string 体)
    {
        int n = 0;
        foreach (var 关键词 in new[] { "for", "while" })
        {
            for (int i = 体.IndexOf(关键词, System.StringComparison.Ordinal);
                 i >= 0;
                 i = 体.IndexOf(关键词, i + 1, System.StringComparison.Ordinal))
            {
                if (i == 0 || !char.IsLetterOrDigit(体[i - 1])) n++;
            }
        }
        return n;
    }
}
