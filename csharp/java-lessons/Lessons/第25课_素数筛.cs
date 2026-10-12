// 第 25 课：素数筛 —— 一次求出所有质数
//
// ══ 这一课要教什么 ══
// 第 23 课判断「一个数」是不是质数，用的是试除：O(√n)。
// 要一次求出 1 到 n 里**所有**质数，就得做 n 次试除 —— 总共 O(n√n)。
// n = 100 万时那是十亿次运算。
//
// 埃拉托斯特尼筛法换了个方向：**不判断，而是划掉**。
// 开一张「还是不是候选」的标记表，从 2 开始，遇到没被划掉的质数，
// 就把它的所有倍数划掉。每个合数**只会被它的最小质因数划掉一次**，
// 所以总共只要 O(n log log n) —— 100 万几乎是瞬间。
//
// 这一课要建立的是这个思路转换：**「一个一个判断」变成「一批一批排除」**。
// 后面第 26 课的二分、第 35 课的 visited 数组，都是同一类「用一张表记住已知信息」。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点：**n = 2000000（两百万）必须在几十秒内跑完**。
//   试除法写法在 n=200万 上要跑 O(n√n) ≈ 2.8e9 次运算（几十秒到几分钟），
//   而埃氏筛 < 1 秒。所以调用点里**直接跑 n = 2000000**：
//   学生写试除法会撞 60 秒默认超时（或者被我自己的 30 秒预算闸门拦下）。
//   ⚠ 为了让"超时"这件事有个好一点的提示，调用点里自己带一个时间闸门：
//     跑之前记 nanoTime，跑完算秒数，打出来；检查里看到秒数 > 30 就明确说
//     「这是试除法的量级，不是筛法」。
// 第二条：**筛的数组必须真的存在** —— 去掉注释后 `筛`（或者它调用的私有方法）
//   里必须出现 `boolean[` 或 `boolean [`（标记表），不能是硬编码的质数表。
// 第三条：**答案要和小 n 上的试除法逐个对得上**（0..1 每个数都比一遍）——
//   筛法的边界最容易错在「n 本身算不算」（i*i 上界、表开到 n+1）。
//
// ⚠ 学生只写方法；骨架预置 import。

using System.Collections.Generic;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("25", "第 25 课：素数筛 —— 一次求出所有质数",
    "1. 写 static boolean[] 标记表(int n)：返回长度 n+1 的表，表[i] 表示 i 是不是质数\n" +
    "2. 写 static int[] 筛(int n)：返回 0..n 里所有质数（从小到大）\n" +
    "3. 写 static int 数质数(int n)：0..n 里有几个质数\n" +
    "4. 写 static boolean 试除法(int n)：第 23 课那种一个一个试的写法（这一课用它做速度对照）\n" +
    "5. ⚠ 调用点会跑 n = 2000000 —— 用试除法会超时，必须真的用筛法")]
public static class 第25课
{
    private const string 骨架 = """
import java.util.*;

public class Main {
/*学生代码*/

    public static void main(String[] args) {
/*调用点*/
    }
}
""";

    public static string 模板 => """
// 第 25 课：素数筛 —— 一次求出所有质数
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**不一个一个判断，而是一批一批划掉**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    埃拉托斯特尼筛法（埃氏筛）
//
//  需要先会
//    第 23 课：质数判断（试除法）
//    第 06 课：数组（boolean[] 标记表）
//    第 20 课：复杂度（为什么 n 大了就要换算法）
//
// ─────────────────────────────────────────────────────
//  先介绍：求「所有」质数，试除法不够用
// ─────────────────────────────────────────────────────
//
// 第 23 课判断一个数是不是质数，要试到 √n。那求 1 到 n 里**所有**质数呢？
// 对每个数都试一遍 ⇒ 大约 n × √n 次运算：
//
//     n = 1000          约 3 万次         一眨眼
//     n = 100 万        约 10 亿次        几秒
//     n = 200 万        约 28 亿次        几十秒到几分钟  ← 太慢了
//
// 埃拉托斯特尼（公元前 3 世纪的希腊人）想了个完全不同的办法。
//
// ─────────────────────────────────────────────────────
//  一、筛法：把合数「划掉」
// ─────────────────────────────────────────────────────
//
// 拿一张纸写下 2 到 30：
//
//     2  3  4  5  6  7  8  9  10 11 12 13 14 15 16 17 18 19 20 …
//
// 从 2 开始：
//
//     2 是质数（它没被划掉）⇒ **把 2 的所有倍数划掉**：4、6、8、10、…
//     3 是质数            ⇒ 把 3 的所有倍数划掉：6、9、12、15、…
//     4 已经被划掉了       ⇒ 跳过（它不是质数，用不着拿它去划）
//     5 是质数            ⇒ 把 5 的倍数划掉…
//     …
//
// 走完之后，**没被划掉的就是质数**。
//
// 为什么快：每个合数都只会被**它的最小质因数**划掉一次，
//   总共大约 n log log n 次操作 ⇒ n = 200 万时不到 1 秒。
//
// 代码里的「纸」就是一张 boolean 表：
//
//     boolean[] 是质数 = new boolean[n + 1];        ← ⚠ n+1：下标 0..n 都要装得下
//     Arrays.fill(是质数, true);                     ← 先全当成候选
//     是质数[0] = false;
//     是质数[1] = false;                             ← ⚠ 0 和 1 不是质数（第 23 课讲过）
//
//     for (int i = 2; (long) i * i <= n; i++) {
//         if (是质数[i]) {                           ← 只拿质数去划
//             for (int j = i * i; j <= n; j = j + i) {   ← ⚠ 从 i*i 开始（见下）
//                 是质数[j] = false;
//             }
//         }
//     }
//
// ─────────────────────────────────────────────────────
//  二、两个「为什么这么写」的细节
// ─────────────────────────────────────────────────────
//
// **① 外层为什么只到 √n？**
//   如果一个数 x ≤ n 是合数，它一定有一个 ≤ √n 的质因数。
//   所以「用 ≤ √n 的质数去划」已经能把 n 以内所有合数都划掉。
//   再往后走是白费 —— 那也正是第 23 课 `i*i <= n` 的同一个道理。
//
// **② 内层为什么从 i*i 开始，不是从 2*i？**
//   2*i、3*i、…、(i-1)*i 这些小倍数，**在更小的质数那一轮就已经被划掉了**
//   （它们都有比 i 更小的质因数）。从 i*i 开始能省掉一大半重复劳动。
//
//   ⚠ `i * i` 要用 `(long) i * i` 或者把 j 声明成 long 才不会溢出 ——
//     第 23 课踩过这个坑。这里写 `j = i * i` 时 i ≤ √n ≤ 46341（n 在 int 内），
//     i*i 还在 int 里，所以直接写也行；但养成习惯写 (long) 更稳。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：筛法的「上界」最容易开错
// ─────────────────────────────────────────────────────
//
//     boolean[] 是质数 = new boolean[n];            ← 少了 1
//     …
//         是质数[n] = false;                         ← 于是这里越界
//
// 你会看到：
//
//     Exception in thread "main" java.lang.ArrayIndexOutOfBoundsException:
//         Index 30 out of bounds for length 30
//
// 为什么：`new boolean[30]` 的合法下标是 **0 ~ 29**。你要能装下下标 n，
//   长度就得是 **n + 1**。这是数组的通用规矩（第 06 课），
//   但在筛法里特别容易犯 —— 因为平时写 `new int[n]` 写顺了手。
//
// 另外两处同样是边界：
//   · 忘了把 0 和 1 置成 false ⇒ 结果里会多出两个「质数」
//   · 内层写 `j <= n` 而不是 `j < n` ⇒ 最后一格漏掉（n 本身是质数时就错了）
//
// ⇒ 记住一句：**「0 到 n」一共有 n+1 个数**。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
//   更快的筛（线性筛 / 欧拉筛，O(n)）→ 不涉及。
//   大数质因数分解 → 第 23 课。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 你**只写方法**：不用写 class，不用写 main；`java.util.Arrays` 已经 import 好了
//    （Arrays.fill 要用）。每个方法都要带 **static**。
//
//   1. static boolean[] 标记表(int n)
//        返回长度 **n+1** 的表，表[i] 为 true 表示 i 是质数。
//        ⚠ n < 2 时返回的表里全是 false（0 和 1 不是质数）。
//
//   2. static int[] 筛(int n)
//        返回 0..n 里所有质数，从小到大。
//
//   3. static int 数质数(int n)
//        返回 0..n 里有几个质数。
//
//   4. static boolean 试除法(int n)
//        第 23 课那种「试到 √n」的写法 —— 这一课用它做**速度对照**，
//        所以你不用改它，照第 23 课的写法写就行。
//
//  ⚠ **调用点会跑 n = 2000000（两百万）**。
//    筛法不到 1 秒；如果 `筛`/`标记表` 里写的是试除法，会撞上超时。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
//   ⚠ 这一课的检查会跑两百万 —— 第一次可能要几秒，属正常。
//     如果**超过一分钟**，那基本就是筛法没写对（写成试除法了）。
//
// ── 在下面写 ──
    static boolean[] 标记表(int n) {
        return new boolean[0];
    }

    static int[] 筛(int n) {
        return new int[0];
    }

    static int 数质数(int n) {
        return 0;
    }

    static boolean 试除法(int n) {
        return false;
    }
""";

    public static string 参考答案 => """
// 参考答案 —— 埃氏筛（只写方法，不用写 class / main / import）

// 1. 标记表：表[i] == true 表示 i 是质数
//    先把所有数都当成候选，再把合数一个个划掉
static boolean[] 标记表(int n) {
    boolean[] 是质数 = new boolean[n + 1];      /* ⚠ n+1：下标 0..n 都要装得下 */
    if (n < 2) {
        return 是质数;                          /* 0 和 1 都不是质数，表里全是 false 就对 */
    }
    Arrays.fill(是质数, true);
    是质数[0] = false;                          /* ⚠ 这两行不能忘 */
    是质数[1] = false;

    for (int i = 2; (long) i * i <= n; i++) {    /* 只需用 ≤ √n 的质数去划 */
        if (是质数[i]) {                        /* 被划掉过的（合数）不用管 */
            for (int j = i * i; j <= n; j = j + i) {
                是质数[j] = false;              /* 从 i*i 开始：更小的倍数前几轮已经划过了 */
            }
        }
    }
    return 是质数;
}

// 2. 把所有「还站着的」收集起来
static int[] 筛(int n) {
    boolean[] 是质数 = 标记表(n);
    int 个数 = 0;
    for (int i = 0; i <= n; i++) {
        if (是质数[i]) {
            个数 = 个数 + 1;
        }
    }
    int[] 结果 = new int[个数];
    int 位置 = 0;
    for (int i = 0; i <= n; i++) {
        if (是质数[i]) {
            结果[位置] = i;
            位置 = 位置 + 1;
        }
    }
    return 结果;
}

// 3. 数一数就行（不用真的建数组）
static int 数质数(int n) {
    boolean[] 是质数 = 标记表(n);
    int 个数 = 0;
    for (int i = 0; i <= n; i++) {
        if (是质数[i]) {
            个数 = 个数 + 1;
        }
    }
    return 个数;
}

// 4. 试除法（第 23 课的写法）—— 这一课拿它做速度对照
static boolean 试除法(int n) {
    if (n < 2) {
        return false;
    }
    for (int i = 2; (long) i * i <= n; i++) {
        if (n % i == 0) {
            return false;
        }
    }
    return true;
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("四个方法都在，签名也对")]
    public static JavaCheckResult 检查_函数都在(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        if (!Regex.IsMatch(净, @"static\s+boolean\s*\[\s*\]\s+标记表\s*\(\s*int\s+\w+\s*\)"))
            缺.Add("      标记表　（static boolean[] 标记表(int n)）");
        if (!Regex.IsMatch(净, @"static\s+int\s*\[\s*\]\s+筛\s*\(\s*int\s+\w+\s*\)"))
            缺.Add("      筛　　　（static int[] 筛(int n)）");
        if (!Regex.IsMatch(净, @"static\s+int\s+数质数\s*\(\s*int\s+\w+\s*\)"))
            缺.Add("      数质数　（static int 数质数(int n)）");
        if (!Regex.IsMatch(净, @"static\s+boolean\s+试除法\s*\(\s*int\s+\w+\s*\)"))
            缺.Add("      试除法　（static boolean 试除法(int n)）");

        if (缺.Count == 0) return JavaCheckResult.过("四个方法都在，签名也对");
        return JavaCheckResult.不过(
            "有方法没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      ⚠ 两个数组返回类型是 boolean[] 和 int[]（方括号跟类型）。");
    }

    [Java检查说明("★ 小范围：筛出来的质数和试除法逐个对得上（含 0 和 1 的边界）")]
    public static JavaCheckResult 检查_小范围对得上(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("表长|", "31", "标记表(30) 的长度应当是 31 —— 「0 到 n」一共 n+1 个数"),
            ("表0|", "false", "标记表(30)[0]　0 不是质数"),
            ("表1|", "false", "标记表(30)[1]　⚠ 1 不是质数（这一格最容易忘）"),
            ("表2|", "true", "标记表(30)[2]　2 是质数"),
            ("表30|", "false", "标记表(30)[30]　30 是合数"),
            ("表-1|", "false", "标记表(1)[1]　n < 2 时表里全是 false"),
            ("筛30|", "2 3 5 7 11 13 17 19 23 29", "筛(30)　10 个质数"),
            ("数10|", "4", "数质数(10)　2、3、5、7"),
            ("数100|", "25", "数质数(100)　25 个质数"),
            ("数1000|", "168", "数质数(1000)　168 个质数"),
            ("数1|", "0", "数质数(1)　0 和 1 都不是质数 ⇒ 0 个"),
            ("数2|", "1", "数质数(2)　只有 2 一个"));
    }

    [Java检查说明("★ 两百万：必须真的是筛法（试除法在这里会超时）")]
    public static JavaCheckResult 检查_两百万(string 代码)
    {
        // ⚠ 这一条是本课的核心检查点。
        //   试除法在 n=200万 上要 28 亿次运算 —— 撞 60 秒超时。
        var run = 跑一次(代码);
        if (run.Raw.Error != null && run.Raw.Error.Contains("超时"))
            return JavaCheckResult.不过(
                "跑两百万的时候**超时了** —— 这基本可以确定：你写的不是筛法，是试除法。\n" +
                "      量级对比（n = 200 万）：\n" +
                "          试除法   每个数都要试到 √n ⇒ 约 28 亿次运算 ⇒ 几十秒到几分钟\n" +
                "          埃氏筛   每个合数只被划掉一次 ⇒ 不到 1 秒\n" +
                "      ⇒ 换成「开一张标记表，把合数一批一批划掉」：\n" +
                "          boolean[] 是质数 = new boolean[n + 1];   // ⚠ n+1\n" +
                "          Arrays.fill(是质数, true);\n" +
                "          是质数[0] = false; 是质数[1] = false;\n" +
                "          for (int i = 2; (long) i * i <= n; i++)\n" +
                "              if (是质数[i])\n" +
                "                  for (int j = i * i; j <= n; j += i) 是质数[j] = false;\n" +
                "      思路的转变在这里：**不再一个一个判断，而是一批一批排除**。");

        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);

        long 秒 = 长整数(run, "两百万秒|");
        if (秒 > 30)
            return JavaCheckResult.不过(
                $"跑两百万花了 {秒} 秒 —— 这个量级说明 `筛` 里在逐个试除，不是筛法。\n" +
                "      筛法应该是**一秒以内**（每个合数只被划掉一次）。\n" +
                "      ⇒ 检查一下 筛 / 标记表 里是不是真的开了 boolean[] 标记表，\n" +
                "        而不是对每个数调 试除法。");

        return 比(run,
            ("两百万个数|", "148933", "数质数(2000000)　两百万以内一共有 148933 个质数"),
            ("两百万首|", "2 3 5 7 11 13", "筛(2000000) 的前 6 个"),
            ("两百万尾|", "1999993", "筛(2000000) 的最后一个 —— 它应当 ≤ 2000000（大了就是上界写错了）"));
    }

    [Java检查说明("筛法真的用了标记表（不是把质数表写死在代码里）")]
    public static JavaCheckResult 检查_用了标记表(string 代码)
    {
        string 净 = 去掉注释(代码);
        if (净.Contains("boolean[") || 净.Contains("boolean ["))
            return JavaCheckResult.过("看到 boolean[] 标记表 —— 是筛法该有的样子");

        return JavaCheckResult.不过(
            "没有看到 `boolean[]` 标记表。\n" +
            "      筛法的核心就是这张表：**先假设所有数都是质数，再把合数划掉**。\n" +
            "          boolean[] 是质数 = new boolean[n + 1];\n" +
            "          Arrays.fill(是质数, true);\n" +
            "      ⚠ 把质数一个个写死在数组里是不行的：\n" +
            "        调用点跑的是 n = 2000000，得写 148933 个数 —— 而且题目要的是算法。");
    }

    // ══════════════════ 工具 ══════════════════

    private const string 调用点 = """
        boolean[] 表30 = 标记表(30);
        System.out.println("表长|" + 表30.length);
        System.out.println("表0|" + 表30[0]);
        System.out.println("表1|" + 表30[1]);
        System.out.println("表2|" + 表30[2]);
        System.out.println("表30|" + 表30[30]);
        boolean[] 表1 = 标记表(1);
        System.out.println("表-1|" + 表1[1]);

        System.out.println("筛30|" + 拼(筛(30)));
        System.out.println("数1|" + 数质数(1));
        System.out.println("数2|" + 数质数(2));
        System.out.println("数10|" + 数质数(10));
        System.out.println("数100|" + 数质数(100));
        System.out.println("数1000|" + 数质数(1000));

        long 起 = System.nanoTime();
        int 两百万个数 = 数质数(2000000);
        int[] 两百万表 = 筛(2000000);
        long 两百万纳秒 = System.nanoTime() - 起;
        System.out.println("两百万秒|" + (两百万纳秒 / 1000000000));
        System.out.println("两百万个数|" + 两百万个数);
        StringBuilder 首 = new StringBuilder();
        for (int i = 0; i < 6 && i < 两百万表.length; i++) {
            if (i > 0) 首.append(" ");
            首.append(两百万表[i]);
        }
        System.out.println("两百万首|" + 首);
        System.out.println("两百万尾|" + 两百万表[两百万表.length - 1]);

        int 一致的个数 = 0;
        boolean[] 小表 = 标记表(2000);   /* 只算一次，别在循环里反复算 */
        for (int i = 0; i <= 2000; i++) {
            if (小表[i] == 试除法(i)) 一致的个数++;
        }
        System.out.println("小范围一致|" + 一致的个数);
""";

    /// <summary>调用点里用的小工具：把 int[] 拼成一行（课程自己写的）。</summary>
    private const string 拼接方法 = """
    static String 拼(int[] 数) {
        StringBuilder 串 = new StringBuilder();
        for (int i = 0; i < 数.length; i++) {
            if (i > 0) 串.append(" ");
            串.append(数[i]);
        }
        return 串.toString();
    }

""";

    private static string 上次的代码 = "";
    private static JavaRun 上次的运行;

    private static JavaRun 跑一次(string 代码)
    {
        if (上次的运行 != null && 上次的代码 == 代码) return 上次的运行;
        上次的代码 = 代码;
        var 包 = JavaCheck.编译并运行(代码 + "\n" + 拼接方法, 骨架, 调用点);
        上次的运行 = 包.编译通过 ? 包 : null;
        return 包;
    }

    private static string 先别急着比(JavaRun run)
    {
        if (run.Raw.CompileFailed) return JavaCheck.编译失败说明(run).Detail;
        if (run.Raw.Error != null)
            return "程序没跑完就退出了：\n      " + run.Raw.Error + "\n" +
                   "      这一课最容易在下标上翻车：`new boolean[n]` 只能装下标 0..n-1，\n" +
                   "      要装到 n 就得写 **new boolean[n + 1]**。";
        return "";
    }

    private static JavaCheckResult 比(JavaRun run, params (string 标记, string 期望, string 说明)[] 用例们)
    {
        var 错 = new List<string>();
        foreach (var (标记, 期望, 说明) in 用例们)
        {
            string 得到 = 看看(run, 标记);
            if (得到.Length == 0) 错.Add("      " + 说明 + "：没有找到这个标记的输出");
            else if (!一样(得到, 期望))
                错.Add("      " + 说明 + "：得到「" + 得到 + "」，应该是「" + 期望 + "」");
        }
        if (错.Count == 0) return JavaCheckResult.过($"这几组都对（{用例们.Length} 组）");

        return JavaCheckResult.不过(
            "结果不对：\n" + string.Join("\n", 错) + "\n" +
            "      筛法的三个边界：\n" +
            "        · 表要开 **n + 1** 格（下标 0..n）\n" +
            "        · **0 和 1 要手动置成 false**（Arrays.fill 会把它们也设成 true）\n" +
            "        · 外层只到 √n，内层从 **i*i** 开始、每次 +i");
    }

    private static string 看看(JavaRun run, string 标记)
    {
        foreach (var 行 in run.行)
        {
            int 位 = 行.IndexOf(标记, System.StringComparison.Ordinal);
            if (位 >= 0) return 行.Substring(位 + 标记.Length).Trim();
        }
        return "";
    }

    private static long 长整数(JavaRun run, string 标记)
    {
        string s = 看看(run, 标记);
        return long.TryParse(s, out long v) ? v : -1;
    }

    private static bool 一样(string 甲, string 乙) =>
        Regex.Replace(甲.Trim(), @"\s+", " ") == Regex.Replace(乙.Trim(), @"\s+", " ");

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
}
