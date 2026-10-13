// 第 37 课：数学库 —— Math 里的那些东西，以及浮点误差
//
// ══ 这一课要教什么 ══
// Java 的 `Math` 类（在 `java.lang` 里，**不用 import**）把常用的数学函数都备好了：
//   sqrt（开方）、pow（幂）、abs（绝对值）、floor / ceil / round（取整的三种）、
//   max / min、PI / E、以及三角函数和 log。
//
// 这一课的另一半更重要：**`double` 不是精确的**。
// `0.1 + 0.2` 在 Java 里不等于 `0.3` —— 这不是 bug，是二进制小数的必然结果。
// 所以「两个 double 相等吗」这个问题**只能用"差不多相等"来回答**：
//
//     Math.abs(a - b) < 1e-9
//
// 不这么写的话，你的判断会在某个犄角旮旯的输入上悄悄给出错误的答案 ——
// 而且**编译和运行都不会报任何错**。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点：**`几乎相等` 必须用误差范围，不能写 `==`**。
//   调用点里打印了两件事：
//     · 用学生的 几乎相等 判断 0.1+0.2 和 0.3      → 必须是 true
//     · 检查 `0.1 + 0.2 == 0.3` 直接比            → 必须是 **false**（这就是问题本身）
//   第二条是课程自己写死的（`(0.1 + 0.2 == 0.3)`），学生改不了它 ——
//   它把「浮点数确实不精确」这件事**当场展示**出来，让第一条的 true 有说服力。
//   学生把 几乎相等 写成 `a == b` 的话，第一件事会打出 false（被抓住）。
// 第二条：**取整的三种方式必须分清**：
//   `(int)` 是**截断**（3.9 → 3、-3.9 → -3）、`Math.floor` 往下、`Math.ceil` 往上、
//   `Math.round` 四舍五入（返回 long）。检查里喂正数和负数各一组。
// 第三条：`开方` 要能挡住负数（`Math.sqrt(-1)` 给 NaN，**不抛异常**）——
//   这是"Java 不报错但结果没意义"的又一个例子。
//
// ⚠ 学生只写方法；骨架预置 import java.util.*。

using System.Collections.Generic;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("37", "第 37 课：数学库 —— Math 里的那些东西，以及浮点误差",
    "1. 写 static double 两点距离(double 甲行, double 甲列, double 乙行, double 乙列)：用 Math.sqrt\n" +
    "2. 写 static double 开方(double x)：⚠ 负数要返回 -1（Math.sqrt 对负数给的是 NaN，不抛异常）\n" +
    "3. 写 static int 取整往下(double x) / static int 取整往上(double x) / static long 四舍五入(double x)：分别用 floor / ceil / round\n" +
    "4. 写 static boolean 几乎相等(double a, double b)：⚠ 必须用误差范围（Math.abs(a-b) < 1e-9），不能写 a == b\n" +
    "5. 写 static long 组合数(int n, int k)：C(n,k) = n! / (k! (n-k)!)")]
public static class 第37课
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
// 第 37 课：数学库 —— Math 里的那些东西，以及浮点误差
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲两件事：**Math 里有什么**，以及
// **`double` 不精确，所以别用 == 比它**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    Math 类；floor / ceil / round 的区别；浮点误差
//
//  需要先会
//    第 02 课：类型（int 和 double 的差别）
//    第 05 课：方法
//
// ─────────────────────────────────────────────────────
//  先介绍：数学函数都在 Math 里
// ─────────────────────────────────────────────────────
//
// `Math` 在 **java.lang** 里 —— 和 `String`、`System` 一样，
// **不用 import** 就能用。（这一点和第 16 课的 ArrayList 不同。）
//
// 常用的：
//
//     Math.sqrt(x)      开平方            sqrt(2) ≈ 1.4142135623730951
//     Math.pow(a, b)    a 的 b 次方        pow(2, 10) = 1024.0
//     Math.abs(x)       绝对值             abs(-7) = 7
//     Math.max(a, b)    大的那个
//     Math.min(a, b)    小的那个
//     Math.floor(x)     **往下**取整（往负无穷）   floor(3.9) = 3.0   floor(-3.1) = -4.0
//     Math.ceil(x)      **往上**取整（往正无穷）   ceil(3.1) = 4.0    ceil(-3.9) = -3.0
//     Math.round(x)     四舍五入，返回 **long**    round(3.5) = 4     round(-3.5) = -3
//     Math.PI           3.141592653589793
//     Math.E            2.718281828459045
//
// ⚠ 四处容易踩的：
//   · `sqrt` / `pow` **返回 double**。想得到 int 得自己转：
//     `(int) Math.sqrt(16)` ⇒ 4
//   · `Math.round` 返回的是 **long**，不是 int（大数才装得下）。
//   · `Math.abs` 对 `Integer.MIN_VALUE` **无效**（它没有对应的正数），
//     会返回一个负数。这个坑很偏，但知道一下没坏处。
//   · 三角函数（sin / cos / tan）**吃的是弧度**，不是角度。
//     角度要先转：`Math.sin(Math.toRadians(30))`。
//
// ─────────────────────────────────────────────────────
//  一、三种"取整"，一定要分清
//   ─────────────────────────────────────────────────────
//
// 以 3.7 和 -3.7 为例：
//
//     (int) 3.7      =  3      截断（把小数部分直接扔掉，往零的方向）
//     (int) -3.7     = -3
//
//     Math.floor(3.7)  =  3.0  往下（往负无穷）
//     Math.floor(-3.7) = -4.0
//
//     Math.ceil(3.7)   =  4.0  往上（往正无穷）
//     Math.ceil(-3.7)  = -3.0
//
//     Math.round(3.7)  =  4    四舍五入（返回 long）
//     Math.round(-3.5) = -3     ⚠ 负数这里是"往上进位"，不是你想的 -4
//
// ⚠ **正数上 `(int)` 和 `Math.floor` 结果一样**，所以很多人以为它们是一回事。
//   负数上立刻分道扬镳：`(int) -3.7` 是 -3，`Math.floor(-3.7)` 是 -4。
//   「截断」不是「向下取整」—— 这两个词是不同的意思。
//
// ─────────────────────────────────────────────────────
//  二、`double` 不精确：0.1 + 0.2 != 0.3
// ─────────────────────────────────────────────────────
//
// 在 Java 里（`jshell` 或任何程序里）打一句：
//
//     System.out.println(0.1 + 0.2);          → 0.30000000000000004
//     System.out.println(0.1 + 0.2 == 0.3);   → **false**
//
// 为什么：`double` 用二进制存小数，而 **0.1 在二进制里是无限循环的**
//   （就像 1/3 在十进制里是 0.3333… 永远写不完）。
//   它只能存一个"最接近 0.1 的二进制小数"，于是加起来的和也就"最接近 0.3 但不是 0.3"。
//
// ⚠ 这不是 Java 的毛病 —— C、C++、Python、JavaScript 全都一样
//   （Python 里打 `0.1 + 0.2` 也是 `0.30000000000000004`）。
//   也**不是 bug**，是"用有限位表示无限小数"的必然结果。
//
// ⇒ 所以 **永远不要用 `==` 比较两个 double**。判断"相等"只有一种写法：
//
//     Math.abs(a - b) < 一个很小的数        通常取 1e-9
//
// ⚠ 那个"很小的数"叫**误差容限**（epsilon），取多大要看你的问题：
//   · 一般计算题 `1e-9` 够用
//   · 如果中间做了几万次累加，误差会累积，可能要用 `1e-6`
//   · 竞赛题一般会明确写"误差不超过 1e-6"
//
// 另一个常见做法是**输出时**才限定小数位（而不是比较时）：
//
//     System.out.printf("%.6f%n", Math.sqrt(2));      → 1.414214
//
// ⚠ `%n` 是"换行"，比在格式串里写 `\n` 更规范（它会按系统给正确的换行符）。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：用 == 比两个 double
// ─────────────────────────────────────────────────────
//
//     static boolean 几乎相等(double a, double b) {
//         return a == b;                     ← 看起来天经地义
//     }
//
// 你会看到：**编译通过、大部分时候也对**（比如 3.0 == 3.0 就是 true）。
//   可只要其中一个数是"算出来的"（尤其是除法、开方、0.1 这种），
//   它就会在你意想不到的时候返回 **false** ——
//   而且**不报错、不警告**，你只会看到"结果不对"。
//
// 为什么它危险：这类错误**不会稳定复现**。
//   同一份代码，换个输入、换个编译顺序、换个 JDK，结果就可能变 ——
//   那是最难查的一类 bug（和第 20 课的整数溢出是同一个性质：
//   **静默地算错**）。
//
// ⇒ 判等一律写 `Math.abs(a - b) < 1e-9`。
//
//   附：`Math.sqrt(-1)` **不抛异常**，它返回 `NaN`（Not a Number）。
//   而 NaN 有个怪脾气：**它和任何数（包括它自己）比较都是 false** ——
//   所以 `x == Double.NaN` 永远是 false，要判它得用 `Double.isNaN(x)`。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
//   大数运算（BigInteger / BigDecimal）→ 不涉及。
//   数论算法（gcd、质数）→ 第 23、24 课。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 你**只写方法**：不用写 class，不用写 main；`java.util.*` 已经 import 好了
//    （Math 在 java.lang 里，本来就不用 import）。每个方法都要带 **static**。
//
//   1. static double 两点距离(double 甲行, double 甲列, double 乙行, double 乙列)
//        平面两点距离：sqrt((乙行-甲行)² + (乙列-甲列)²)。
//        用 Math.sqrt，平方可以用 `d * d` 或者 Math.pow。
//
//   2. static double 开方(double x)
//        x < 0 时返回 **-1**（题目约定）；否则返回 Math.sqrt(x)。
//        ⚠ 别直接返回 Math.sqrt(-1) —— 那是 NaN，不是错误也不是异常。
//
//   3. static int 取整往下(double x)     用 Math.floor，转成 int
//       static int 取整往上(double x)     用 Math.ceil，转成 int
//       static long 四舍五入(double x)    用 Math.round（它本来就返回 long）
//
//   4. static boolean 几乎相等(double a, double b)
//        |a - b| < 1e-9 就算相等。
//        ⚠ **不许写 `a == b`**（检查里会拿 0.1+0.2 和 0.3 来试）。
//
//   5. static long 组合数(int n, int k)
//        C(n,k) = n! / (k! × (n-k)!)。k > n 或 k < 0 时返回 0。
//        ⚠ 用 long，而且每一步都先乘后除（别用 Math.pow 那种浮点算法，
//          它在大数上会算不准 —— 第 23 课讲过整数运算才是精确的）。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
    static double 两点距离(double 甲行, double 甲列, double 乙行, double 乙列) {
        return 0;
    }

    static double 开方(double x) {
        return -1;
    }

    static int 取整往下(double x) {
        return 0;
    }

    static int 取整往上(double x) {
        return 0;
    }

    static long 四舍五入(double x) {
        return 0;
    }

    static boolean 几乎相等(double a, double b) {
        return false;
    }

    static long 组合数(int n, int k) {
        return 0;
    }
""";

    public static string 参考答案 => """
// 参考答案 —— Math 与浮点误差（只写方法，不用写 class / main / import）
//
// ⚠ Math 在 java.lang 里，不用 import。它的函数大多**返回 double**，
//   要 int 得自己转 —— 注意 `(int)` 是**截断**，不是四舍五入也不是向下取整。

// 1. 两点距离：sqrt((Δ行)² + (Δ列)²)
static double 两点距离(double 甲行, double 甲列, double 乙行, double 乙列) {
    double 行差 = 乙行 - 甲行;
    double 列差 = 乙列 - 甲列;
    return Math.sqrt(行差 * 行差 + 列差 * 列差);      /* 平方用 d*d 最快也最准 */
}

// 2. 开方：⚠ Math.sqrt(负数) 返回的是 NaN，**不抛异常** —— 所以要自己挡
static double 开方(double x) {
    if (x < 0) {
        return -1;                        /* 题目约定的"出错"返回值 */
    }
    return Math.sqrt(x);
}

// 3. 三种取整，务必分清（拿 -3.7 试一下就现形）
static int 取整往下(double x) {
    return (int) Math.floor(x);           /* 往负无穷：-3.7 → -4 */
}

static int 取整往上(double x) {
    return (int) Math.ceil(x);            /* 往正无穷：-3.7 → -3 */
}

static long 四舍五入(double x) {
    return Math.round(x);                 /* 它本来就返回 long，不用转 */
}

// 4. 判两个 double 相等：**只能用误差范围**
//    ⚠ 写 `a == b` 的话，0.1 + 0.2 == 0.3 会返回 false（它们确实不相等）
static boolean 几乎相等(double a, double b) {
    return Math.abs(a - b) < 1e-9;
}

// 5. 组合数：C(n,k) = n! / (k! (n-k)!)
//    每一步先乘后除，保证中间结果一直是整数（整数运算才是精确的）
static long 组合数(int n, int k) {
    if (k < 0 || k > n || n < 0) {
        return 0;
    }
    if (k > n - k) {
        k = n - k;                        /* 利用对称性少算几步，也少一点溢出风险 */
    }
    long 结果 = 1;
    for (int i = 1; i <= k; i++) {
        结果 = 结果 * (n - k + i) / i;      /* ⚠ 先乘后除：结果 * (...) 一定能被 i 整除 */
    }
    return 结果;
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("七个方法都在，签名也对")]
    public static JavaCheckResult 检查_函数都在(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        if (!Regex.IsMatch(净, @"static\s+double\s+两点距离\s*\(\s*double\s+\w+\s*,\s*double\s+\w+\s*,\s*double\s+\w+\s*,\s*double\s+\w+\s*\)"))
            缺.Add("      两点距离　（static double 两点距离(double 甲行, double 甲列, double 乙行, double 乙列)）");
        if (!Regex.IsMatch(净, @"static\s+double\s+开方\s*\(\s*double\s+\w+\s*\)"))
            缺.Add("      开方　　　（static double 开方(double x)）");
        if (!Regex.IsMatch(净, @"static\s+int\s+取整往下\s*\(\s*double\s+\w+\s*\)"))
            缺.Add("      取整往下　（static int 取整往下(double x)）");
        if (!Regex.IsMatch(净, @"static\s+int\s+取整往上\s*\(\s*double\s+\w+\s*\)"))
            缺.Add("      取整往上　（static int 取整往上(double x)）");
        if (!Regex.IsMatch(净, @"static\s+long\s+四舍五入\s*\(\s*double\s+\w+\s*\)"))
            缺.Add("      四舍五入　（static long 四舍五入(double x)）");
        if (!Regex.IsMatch(净, @"static\s+boolean\s+几乎相等\s*\(\s*double\s+\w+\s*,\s*double\s+\w+\s*\)"))
            缺.Add("      几乎相等　（static boolean 几乎相等(double a, double b)）");
        if (!Regex.IsMatch(净, @"static\s+long\s+组合数\s*\(\s*int\s+\w+\s*,\s*int\s+\w+\s*\)"))
            缺.Add("      组合数　　（static long 组合数(int n, int k)）");

        if (缺.Count == 0) return JavaCheckResult.过("七个方法都在，签名也对");
        return JavaCheckResult.不过(
            "有方法没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      ⚠ 四舍五入 返回 **long**（Math.round 本来就是 long）；\n" +
            "        取整往下 / 取整往上 返回 **int**（要自己从 double 转过来）。");
    }

    [Java检查说明("★ 几乎相等 必须用误差范围（0.1+0.2 和 0.3 要判成相等）")]
    public static JavaCheckResult 检查_浮点判等(string 代码)
    {
        // ⚠ 核心检查点。
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);

        if (看看(run, "直接比|") != "false")
            return JavaCheckResult.不过(
                "检查里 `0.1 + 0.2 == 0.3` 居然不是 false —— 这不正常，先确认代码没被改动。\n" +
                "      （课程自己写死了这一行，用来说明「double 确实不精确」。）");

        if (看看(run, "差不多|") != "true")
            return JavaCheckResult.不过(
                "几乎相等(0.1 + 0.2, 0.3) 返回了 **false** —— 说明你在用 `==` 比较。\n" +
                "      事实是：\n" +
                "          0.1 + 0.2          → 0.30000000000000004\n" +
                "          0.1 + 0.2 == 0.3   → **false**（就是上面那行 `直接比|`）\n" +
                "      为什么：double 用二进制存小数，而 0.1 在二进制里是**无限循环**的\n" +
                "      （像 1/3 在十进制里永远写不完），只能存一个最接近它的值 ——\n" +
                "      加起来的和自然也就「最接近 0.3 但不是 0.3」。\n" +
                "      ⚠ 这不是 Java 的毛病，C / C++ / Python / JavaScript 全都一样。\n" +
                "      ⇒ 判等只有一种写法：\n" +
                "          Math.abs(a - b) < 1e-9\n" +
                "        那个 1e-9 叫**误差容限**。做很多次累加的话，容限可能要给到 1e-6。");

        return JavaCheckResult.过("几乎相等 用的是 Math.abs(a-b) < 1e-9 —— 正确的浮点判等方式");
    }

    [Java检查说明("★ 三种取整分得清（负数上才现形）")]
    public static JavaCheckResult 检查_取整(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("下正|", "3", "取整往下(3.7)　floor ⇒ 3"),
            ("下负|", "-4", "⚠ 取整往下(-3.7)　floor 是**往负无穷** ⇒ -4"),
            ("上正|", "4", "取整往上(3.1)　ceil ⇒ 4"),
            ("上负|", "-3", "⚠ 取整往上(-3.7)　ceil 是**往正无穷** ⇒ -3"),
            ("上整|", "5", "取整往上(5.0)　已经是整数 ⇒ 还是 5"),
            ("四正|", "4", "四舍五入(3.5)　Math.round ⇒ 4"),
            ("四负|", "-3", "⚠ 四舍五入(-3.5)　Math.round 给的是 **-3**（往正方向进位），不是 -4"),
            ("四小|", "3", "四舍五入(3.4) ⇒ 3"));
    }

    [Java检查说明("开方和两点距离：负数、恰好整数、勾股数")]
    public static JavaCheckResult 检查_开方和距离(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("开正|", "true", "开方(4) ≈ 2.0"),
            ("开九|", "true", "开方(9) ≈ 3.0"),
            ("开零|", "true", "开方(0) ≈ 0.0"),
            ("开负|", "-1.0", "⚠ 开方(-1)　题目约定返回 -1（不是 NaN）—— Math.sqrt(-1) 给的是 NaN，不抛异常"),
            ("距勾股|", "true", "两点距离((0,0) → (3,4)) ≈ 5.0（勾股数，浮点算出来是 5.0 或 5.000000000000001）"),
            ("距同点|", "true", "两点距离((2,3) → (2,3)) ≈ 0.0"),
            ("距斜|", "true", "两点距离((0,0) → (1,1)) ≈ 1.4142135623730951"),
            ("距负|", "true", "两点距离((-1,-1) → (2,3)) ≈ 5.0（Δ = 3 和 4）"));
    }

    [Java检查说明("组合数：C(5,2)=10、C(20,10)=184756（整数算，不能用浮点）")]
    public static JavaCheckResult 检查_组合数(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("组5_2|", "10", "组合数(5, 2)　C(5,2) = 10"),
            ("组5_0|", "1", "组合数(5, 0)　C(n,0) = 1"),
            ("组5_5|", "1", "组合数(5, 5)　全选也是一种选法"),
            ("组越界|", "0", "组合数(5, 6)　k > n ⇒ 0"),
            ("组负|", "0", "组合数(5, -1)　k < 0 ⇒ 0"),
            ("组10_3|", "120", "组合数(10, 3)　= 120"),
            ("组20_10|", "184756", "组合数(20, 10)　= 184756"),
            ("组40_20|", "137846528820", "组合数(40, 20)　= 137846528820 ⚠ 已经超过 int 上限 —— 所以要用 long"));
    }

    // ══════════════════ 工具 ══════════════════

    private const string 调用点 = """
        System.out.println("直接比|" + (0.1 + 0.2 == 0.3));
        System.out.println("差不多|" + 几乎相等(0.1 + 0.2, 0.3));
        System.out.println("差得多|" + 几乎相等(0.1, 0.3));

        System.out.println("下正|" + 取整往下(3.7));
        System.out.println("下负|" + 取整往下(-3.7));
        System.out.println("上正|" + 取整往上(3.1));
        System.out.println("上负|" + 取整往上(-3.7));
        System.out.println("上整|" + 取整往上(5.0));
        System.out.println("四正|" + 四舍五入(3.5));
        System.out.println("四负|" + 四舍五入(-3.5));
        System.out.println("四小|" + 四舍五入(3.4));

        System.out.println("开正|" + 几乎相等(开方(4), 2.0));
        System.out.println("开九|" + 几乎相等(开方(9), 3.0));
        System.out.println("开零|" + 几乎相等(开方(0), 0.0));
        System.out.println("开负|" + 开方(-1));

        System.out.println("距勾股|" + 几乎相等(两点距离(0, 0, 3, 4), 5.0));
        System.out.println("距同点|" + 几乎相等(两点距离(2, 3, 2, 3), 0.0));
        System.out.println("距斜|" + 几乎相等(两点距离(0, 0, 1, 1), 1.4142135623730951));
        System.out.println("距负|" + 几乎相等(两点距离(-1, -1, 2, 3), 5.0));

        System.out.println("组5_2|" + 组合数(5, 2));
        System.out.println("组5_0|" + 组合数(5, 0));
        System.out.println("组5_5|" + 组合数(5, 5));
        System.out.println("组越界|" + 组合数(5, 6));
        System.out.println("组负|" + 组合数(5, -1));
        System.out.println("组10_3|" + 组合数(10, 3));
        System.out.println("组20_10|" + 组合数(20, 10));
        System.out.println("组40_20|" + 组合数(40, 20));
""";

    private static string 上次的代码 = "";
    private static JavaRun 上次的运行;

    private static JavaRun 跑一次(string 代码)
    {
        if (上次的运行 != null && 上次的代码 == 代码) return 上次的运行;
        上次的代码 = 代码;
        var 包 = JavaCheck.编译并运行(代码, 骨架, 调用点);
        // ⚠ 课程自己也用 几乎相等 —— 如果学生没写这个方法，整个调用点都编译不过。
        //   所以这里先看一眼原代码里有没有它，没有就直接给一句人话。
        上次的运行 = 包.编译通过 ? 包 : null;
        return 包;
    }

    private static string 先别急着比(JavaRun run)
    {
        if (run.Raw.CompileFailed)
        {
            string 信息 = run.编译信息;
            if (信息.Contains("几乎相等"))
                return "编译器不认识 `几乎相等(...)` —— 这个方法还没写（或者名字/参数和题目不一样）。\n" +
                       "      要写成：static boolean 几乎相等(double a, double b)\n" +
                       "      ⚠ 课程自己的检查代码也要用它，所以它没写好的话后面全跑不了。";
            return JavaCheck.编译失败说明(run).Detail;
        }
        if (run.Raw.Error != null)
            return "程序没跑完就退出了：\n      " + run.Raw.Error;
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
            "      这一课两处最容易错：\n" +
            "        · **取整**：`(int)` 是截断、floor 往下、ceil 往上、round 四舍五入（返回 long）。\n" +
            "          正数上截断和 floor 一样，**负数上就分开了**（(int)-3.7 = -3，floor = -4）。\n" +
            "        · **浮点判等**：只能用 `Math.abs(a-b) < 1e-9`；写 `==` 会在 0.1+0.2 那类地方翻车。");
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
