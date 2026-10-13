// 第 24 课：辗转相除 —— 两个数的最大公约数
//
// ══ 这一课要教什么 ══
// 求最大公约数（gcd）最笨的办法是从小的那个数往下试。
// 欧几里得在两千三百年前发现了一个快得多的办法，靠的是一条等式：
//
//     gcd(a, b) = gcd(b, a % b)          一直除到余数为 0
//
// 为什么它对：a 和 b 的**公因数**，恰好就是 b 和 a%b 的公因数
// （因为 a = (a/b)*b + a%b，任何能整除 a 和 b 的数一定能整除 a%b，反之亦然）。
// 既然两边的公因数集合一模一样，最大的那个自然也一模一样。
//
// 这一课要带走的是：**把「求一个东西」变成「求它的一个更小版本」**，
// 然后让那个「更小」一路缩到能直接回答为止。这正是第 28 课递归的核心手法。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点一：**lcm 必须先除后乘**。
//   最小公倍数 = a * b / gcd。学生十有八九写成 `a * b / 最大公约数(a, b)` ——
//   小数字上完全正确，但 a、b 接近 long 上限时 `a * b` 先溢出，
//   结果变成一个莫名其妙的小数（甚至负数）。
//   防法：喂一对**故意让 a*b 溢出**的大数（123456789012345 和 987654321098765），
//   正确写法 (a/gcd)*b 给出 24386526227404214271909850785，
//   先乘后除的写法给出的完全是另一个数。
// 核心检查点二：**和 BigInteger.gcd 对答案**。
//   Java 自带 `BigInteger.valueOf(a).gcd(...)` —— 那是 JDK 的实现，
//   拿它当标准答案比我自己算的可信得多。检查里让两者逐个用例对照，
//   而且**大数、0、负数**都对照一遍。
// 第三条：gcd(0, b) = b（这一条是从「辗转相除到余数 0」推出来的），
//   负数要先取绝对值 —— 这两条也是真实实现里最容易漏的。
//
// ⚠ 学生只写方法；骨架预置 import java.math.BigInteger。

using System.Collections.Generic;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("24", "第 24 课：辗转相除 —— 两个数的最大公约数",
    "1. 写 static long 最大公约数(long a, long b)：用辗转相除（a % b），负数要先取绝对值\n" +
    "2. 写 static long 最小公倍数(long a, long b)：⚠ 必须先除后乘（a / gcd * b），否则会溢出\n" +
    "3. 写 static long 内置公约数(long a, long b)：用 BigInteger.valueOf(a).gcd(...) 当参照\n" +
    "4. 三个方法的结果必须一致（检查会拿内置的那个当标准答案）")]
public static class 第24课
{
    private const string 骨架 = """
import java.math.BigInteger;

public class Main {
/*学生代码*/

    public static void main(String[] args) {
/*调用点*/
    }
}
""";

    public static string 模板 => """
// 第 24 课：辗转相除 —— 两个数的最大公约数
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**一条等式就能把问题缩小**（gcd(a,b) = gcd(b, a%b)）。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    欧几里得算法（辗转相除）求最大公约数 / 最小公倍数
//
//  需要先会
//    第 23 课：整除、因数
//    第 05 课：方法（这一课要写递归形式的方法）
//
// ─────────────────────────────────────────────────────
//  先介绍：最大公约数
// ─────────────────────────────────────────────────────
//
// 12 和 18 的公约数：1、2、3、6 —— 最大的是 **6**。
//
// 笨办法：从小的那个数（12）往下试，第一个能同时整除两个数的就是答案。
// 12 次试除。数字大起来（比如几百亿）就慢得没法用了。
//
// 欧几里得的办法只需要**几步**。它靠的是这条等式：
//
//     gcd(a, b) = gcd(b, a % b)
//
// 一直算到余数变成 0 为止。
//
// ─────────────────────────────────────────────────────
//  一、为什么 gcd(a, b) = gcd(b, a % b)
// ─────────────────────────────────────────────────────
//
// 把 a 写成「商 × b + 余数」：
//
//     a = (a / b) * b + (a % b)
//
// 现在问一句：**能整除 a 和 b 的数，是不是也能整除 a % b？**
//
//   如果一个数 d 能整除 a、也能整除 b，那它就能整除 (a/b)*b，
//   于是它也能整除 a - (a/b)*b —— 那正是 a % b。
//
// 反过来也一样（a = 商*b + 余数，能整除 b 和余数的数当然能整除 a）。
//
// ⇒ **两边的公因数集合一模一样**，所以最大的那个当然也一样。
//
// 手算一遍 gcd(18, 12)：
//
//     gcd(18, 12)  = gcd(12, 18 % 12 = 6)
//     gcd(12, 6)   = gcd(6, 12 % 6 = 0)
//     gcd(6, 0)    = 6                  ← 余数是 0 了，另一个数就是答案
//
// 三步就出来了。换成 gcd(1000000007, 998244353) 也只要几十步。
//
// ─────────────────────────────────────────────────────
//  二、写成方法：递归和循环都行
// ─────────────────────────────────────────────────────
//
// **递归**（最贴近上面那条等式，一眼就对）：
//
//     static long 最大公约数(long a, long b) {
//         if (b == 0) return a;                ← 基准情形：余数是 0 了
//         return 最大公约数(b, a % b);          ← 换成「更小的一版」再问
//     }
//
// **循环**（省掉调用栈，工程里更常见）：
//
//     while (b != 0) { long 临时 = a % b; a = b; b = 临时; }
//     return a;
//
// ⚠ 两个都要处理的两件事：
//   · **负数**：gcd 只谈「约数」，所以先取绝对值 `Math.abs`。
//     （-12 和 18 的最大公约数也是 6。）
//   · **0**：gcd(0, b) = b（b 的约数就是 0 和 b 的公共约数）。
//     递归写法里 `b == 0 return a` 天生长这样；循环写法也不用特判。
//
// ─────────────────────────────────────────────────────
//  三、最小公倍数：⚠ 必须先除后乘
// ─────────────────────────────────────────────────────
//
//     lcm(a, b) = a * b / gcd(a, b)
//
// 这个式子在数学上没错，**但直接照着写会溢出**：
//
//     int  a = 100000, b = 99999;      a * b = 9999900000 → 已经超过 int 上限
//     long a = 1e12,   b = 1e12;       a * b = 1e24       → 超过 long 上限（约 9.2e18）
//
// 而 a * b 里有一半的因子是 gcd，那部分是白乘的。**先除掉再乘**就没有这个问题：
//
//     lcm(a, b) = (a / gcd(a, b)) * b        ← 先除后乘
//
// 为什么这样就够：a / gcd 是一个整数（gcd 本来就是 a 的约数），
//   而且 a/gcd 和 b **互质**，乘起来正好是 lcm，中间结果不会超出 lcm 本身。
//
// ⇒ 记这一条：**能先约分就先约分**，别把两个大数先乘起来。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：先乘后除
// ─────────────────────────────────────────────────────
//
//     static long 最小公倍数(long a, long b) {
//         return a * b / 最大公约数(a, b);         ← 小数字上完全正确
//     }
//
// 你会看到：**gcd(12,18) 那种小例子全对**，只有大数上炸：
//
//     System.out.println(最小公倍数(123456789012345L, 987654321098765L));
//     → 会打印出一个莫名其妙的小数（可能是负数）
//
// 为什么：`a * b` 是 1.2e29，早就冲出了 long 的上限（9.22e18），
//   高位被丢掉，剩下的位再去除以 gcd —— 得到的当然不是 lcm。
//
// ⚠ 这件事最坑的地方：**Java 的整数溢出完全不报错**。
//   编译通过、运行正常、不抛异常，只是答案不对。
//   第 20 课那个「int 累加 1..100000 变成负数」是同一个病。
//
// ⇒ 改成 `(a / 最大公约数(a, b)) * b`。先把公约数除掉，乘出来的就是最终答案那么大。
//
//   附：Java 自带 `BigInteger` 能装任意大的整数，它也有 gcd：
//
//       BigInteger.valueOf(a).gcd(BigInteger.valueOf(b)).longValue()
//
//   这一课让你用它**当参照**：自己写的和一个 JDK 认证过的实现对一对，
//   比"我觉得对"可靠得多。（真实项目里也是这个习惯：拿一个可信的实现做基准。）
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   一次求很多个数的 gcd → 循环调用自己写的那个就行，不涉及。
//   BigInteger 的完整用法 → 不涉及（这里只用它的 gcd 做参照）。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 你**只写方法**：不用写 class，不用写 main；
//    `java.math.BigInteger` 已经 import 好了。每个方法都要带 **static**。
//
//   1. static long 最大公约数(long a, long b)
//        辗转相除。⚠ 负数先取绝对值；gcd(0, b) = b。
//
//   2. static long 最小公倍数(long a, long b)
//        ⚠ 必须写成 (a / gcd) * b —— 先乘的话大数上会溢出（而且不报错）。
//
//   3. static long 内置公约数(long a, long b)
//        用 BigInteger 当参照：
//          return BigInteger.valueOf(a).gcd(BigInteger.valueOf(b)).longValue();
//        ⚠ 它不接受负数，所以先 Math.abs 一下。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
    static long 最大公约数(long a, long b) {
        return 0;
    }

    static long 最小公倍数(long a, long b) {
        return 0;
    }

    static long 内置公约数(long a, long b) {
        return 0;
    }
""";

    public static string 参考答案 => """
// 参考答案 —— 辗转相除（只写方法，不用写 class / main / import）

// 1. 最大公约数：递归写法，和那条等式长得一模一样
//    gcd(a, b) = gcd(b, a % b)，余数是 0 的时候，另一个数就是答案
static long 最大公约数(long a, long b) {
    a = Math.abs(a);                       /* gcd 只谈约数，负数先取绝对值 */
    b = Math.abs(b);
    if (b == 0) {
        return a;                          /* 余数是 0 了 —— 基准情形，也顺手管住了 gcd(0, b) = b */
    }
    return 最大公约数(b, a % b);             /* 换成「更小的一版」再问 */
}

// 2. 最小公倍数：⚠ 先除后乘
//    a * b / gcd 在小数字上对，大数字上 a*b 先溢出（而且不报错）——
//    (a / gcd) * b 就不会，因为 a/gcd 和 b 互质，中间结果最大就是 lcm 本身。
static long 最小公倍数(long a, long b) {
    a = Math.abs(a);
    b = Math.abs(b);
    if (a == 0 || b == 0) {
        return 0;                          /* 0 和任何数的「公倍数」这一课规定是 0 */
    }
    return (a / 最大公约数(a, b)) * b;        /* ← 先除，再乘 */
}

// 3. 用 JDK 自带的当参照
//    BigInteger 能装任意大的整数，它的 gcd 是 JDK 实现 ——
//    拿它对照自己写的，比「我觉得对」可靠得多。
static long 内置公约数(long a, long b) {
    return BigInteger.valueOf(Math.abs(a)).gcd(BigInteger.valueOf(Math.abs(b))).longValue();
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("三个方法都在，签名也对（都是 long 进 long 出）")]
    public static JavaCheckResult 检查_函数都在(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        foreach (var 名 in new[] { "最大公约数", "最小公倍数", "内置公约数" })
            if (!Regex.IsMatch(净, @"static\s+long\s+" + 名 + @"\s*\(\s*long\s+\w+\s*,\s*long\s+\w+\s*\)"))
                缺.Add("      " + 名 + "　（static long " + 名 + "(long a, long b)）");

        if (缺.Count == 0) return JavaCheckResult.过("三个方法都在，签名也对");
        return JavaCheckResult.不过(
            "有方法没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      ⚠ 参数和返回**都必须是 long** —— 这一课要用到超过 int 上限的数\n" +
            "        （123456789012345 这种，int 根本装不下）。");
    }

    [Java检查说明("★ 最大公约数：和自己的参照（BigInteger.gcd）逐个对得上")]
    public static JavaCheckResult 检查_公约数对得上(string 代码)
    {
        // ⚠ 这一条是核心检查点：拿 JDK 的实现当标准答案，比手算可信。
        //   调用点里每个用例都同时打印「自己写的」和「内置的」，这里比它们一不一致。
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);

        var 错 = new List<string>();
        for (int i = 1; i <= 8; i++)
        {
            string 自己 = 看看(run, "gcd" + i + "自己|");
            string 内置 = 看看(run, "gcd" + i + "内置|");
            if (自己.Length == 0 || 内置.Length == 0) continue;
            if (自己 != 内置) 错.Add($"      第 {i} 组：你算的是 {自己}，BigInteger.gcd 给的是 {内置}");
        }
        if (错.Count == 0) return JavaCheckResult.过("八组 gcd 和 BigInteger.gcd 的结果全都一致");

        return JavaCheckResult.不过(
            "你写的 最大公约数 和 Java 自带的 BigInteger.gcd 对不上：\n" + string.Join("\n", 错) + "\n" +
            "      最可能的两处：\n" +
            "        · **负数**没取绝对值（gcd 只谈约数，-12 和 18 的最大公约数也是 6）\n" +
            "        · **0** 没处理好。gcd(0, b) = b —— 想想「0 和 b 的公共约数是谁的约数」\n" +
            "      递推式本身还是那句：gcd(a, b) = gcd(b, a % b)，余数是 0 时另一个数就是答案。");
    }

    [Java检查说明("★ 最小公倍数：大数上不能溢出（必须先除后乘）")]
    public static JavaCheckResult 检查_公倍数不溢出(string 代码)
    {
        // ⚠ 第二条核心检查点：先乘后除在小数字上全对，只有大数才露馅。
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("lcm小|", "36", "最小公倍数(12, 18)　12 和 18 的最小公倍数是 36"),
            ("lcm互质|", "35", "最小公倍数(5, 7)　互质 ⇒ 直接相乘"),
            ("lcm倍|", "24", "最小公倍数(24, 8)　一个数是另一个的倍数 ⇒ 大的那个"),
            ("lcm零|", "0", "最小公倍数(0, 7)　0 参与的这样定：返回 0"),
            ("lcm负|", "36", "最小公倍数(-12, 18)　负数取绝对值之后和正数一样"),
            ("lcm大|", "8000000000000000000", "⚠ 最小公倍数(8000000000000000000, 4000000000000000000)　这两个数一乘是 3.2e37，早就冲出 long 的上限（9.22e18）——\n" +
                "            写成 a * b / gcd 的话，中间结果先溢出，答案会变成一个莫名其妙的小数。\n" +
                "            正确写法：(a / gcd) * b ⇒ 公约数是 4000000000000000000，\n" +
                "            a / gcd = 2，再乘 b，答案 8000000000000000000 —— 正好还在 long 里。"),
            ("lcm大二|", "9223372036854775806", "⚠ 最小公倍数(9223372036854775806, 2)　a*b 又是溢出的，但真正的 lcm 只有 9.22e18，装得下 —— 只要先除后乘"));
    }

    [Java检查说明("边界：0 和负数（gcd(0, b) = b；负数取绝对值）")]
    public static JavaCheckResult 检查_边界(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("gcd零|", "7", "最大公约数(0, 7)　0 和 b 的公共约数就是 b 的约数 ⇒ 答案是 7"),
            ("gcd都零|", "0", "最大公约数(0, 0)　这一课规定返回 0"),
            ("gcd负|", "6", "最大公约数(-12, 18)　负数先取绝对值"),
            ("gcd双负|", "6", "最大公约数(-12, -18)"),
            ("gcd相等|", "13", "最大公约数(13, 13)　两个一样的数，答案就是它自己"),
            ("gcd一|", "1", "最大公约数(1, 1)　1 和任何数互质"));
    }

    // ══════════════════ 工具 ══════════════════

    private const string 调用点 = """
        System.out.println("gcd1自己|" + 最大公约数(12, 18));
        System.out.println("gcd1内置|" + 内置公约数(12, 18));
        System.out.println("gcd2自己|" + 最大公约数(100, 75));
        System.out.println("gcd2内置|" + 内置公约数(100, 75));
        System.out.println("gcd3自己|" + 最大公约数(0, 7));
        System.out.println("gcd3内置|" + 内置公约数(0, 7));
        System.out.println("gcd4自己|" + 最大公约数(-12, 18));
        System.out.println("gcd4内置|" + 内置公约数(-12, 18));
        System.out.println("gcd5自己|" + 最大公约数(96, 36));
        System.out.println("gcd5内置|" + 内置公约数(96, 36));
        System.out.println("gcd6自己|" + 最大公约数(123456789012345L, 987654321098765L));
        System.out.println("gcd6内置|" + 内置公约数(123456789012345L, 987654321098765L));
        System.out.println("gcd7自己|" + 最大公约数(1000000007L, 998244353L));
        System.out.println("gcd7内置|" + 内置公约数(1000000007L, 998244353L));
        System.out.println("gcd8自己|" + 最大公约数(1, 1));
        System.out.println("gcd8内置|" + 内置公约数(1, 1));

        System.out.println("gcd零|" + 最大公约数(0, 7));
        System.out.println("gcd都零|" + 最大公约数(0, 0));
        System.out.println("gcd负|" + 最大公约数(-12, 18));
        System.out.println("gcd双负|" + 最大公约数(-12, -18));
        System.out.println("gcd相等|" + 最大公约数(13, 13));
        System.out.println("gcd一|" + 最大公约数(1, 1));

        System.out.println("lcm小|" + 最小公倍数(12, 18));
        System.out.println("lcm互质|" + 最小公倍数(5, 7));
        System.out.println("lcm倍|" + 最小公倍数(24, 8));
        System.out.println("lcm零|" + 最小公倍数(0, 7));
        System.out.println("lcm负|" + 最小公倍数(-12, 18));
        System.out.println("lcm大|" + 最小公倍数(8000000000000000000L, 4000000000000000000L));
        System.out.println("lcm大二|" + 最小公倍数(9223372036854775806L, 2L));
""";

    private static string 上次的代码 = "";
    private static JavaRun 上次的运行;

    private static JavaRun 跑一次(string 代码)
    {
        if (上次的运行 != null && 上次的代码 == 代码) return 上次的运行;
        上次的代码 = 代码;
        var 包 = JavaCheck.编译并运行(代码, 骨架, 调用点);
        上次的运行 = 包.编译通过 ? 包 : null;
        return 包;
    }

    private static string 先别急着比(JavaRun run)
    {
        if (run.Raw.CompileFailed) return JavaCheck.编译失败说明(run).Detail;
        if (run.Raw.Error != null)
            return "程序没跑完就退出了：\n      " + run.Raw.Error + "\n" +
                   "      这一课最常见的是 **StackOverflowError**：递归写法忘了基准情形\n" +
                   "      （`if (b == 0) return a;`），会一直递归下去直到栈满。\n" +
                   "      另一处：BigInteger 的 gcd **不接受负数**，要先用 Math.abs。";
        return "";
    }

    private static JavaCheckResult 比(JavaRun run, params (string 标记, string 期望, string 说明)[] 用例们)
    {
        var 错 = new List<string>();
        foreach (var (标记, 期望, 说明) in 用例们)
        {
            string 得到 = 看看(run, 标记);
            if (得到.Length == 0) 错.Add("      " + 说明 + "：没有找到这个标记的输出");
            else if (得到.Trim() != 期望.Trim())
                错.Add("      " + 说明 + "：得到「" + 得到 + "」，应该是「" + 期望 + "」");
        }
        if (错.Count == 0) return JavaCheckResult.过($"这几组都对（{用例们.Length} 组）");

        return JavaCheckResult.不过(
            "结果不对：\n" + string.Join("\n", 错) + "\n" +
            "      这一课两个要点：\n" +
            "        · gcd(a, b) = gcd(b, a % b)，余数 0 时另一个数就是答案；\n" +
            "          负数先 Math.abs，gcd(0, b) = b。\n" +
            "        · lcm 要 **(a / gcd) * b** —— 先乘后除在大数上会溢出，\n" +
            "          而 Java 的溢出**不报错**，只是答案悄悄变错。");
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
