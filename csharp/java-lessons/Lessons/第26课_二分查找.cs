// 第 26 课：二分查找 —— 每次砍掉一半
//
// ══ 这一课要教什么 ══
// 在一排**已经排好序**的数里找一个目标。笨办法是从头看到尾：O(n)。
// 二分反过来：看正中间那一个，比目标大就往左半边找，小就往右半边 ——
// 每次砍掉一半，O(log n)。一百万个元素只要 20 次比较。
//
// 但二分是「**边界最容易写错**」的算法：循环条件写 `low < high` 还是 `low <= high`、
// `high = mid` 还是 `high = mid - 1` —— 差一个等号，结果就从「找不到」变成「死循环」，
// 而且**编译一个字都不报**。这一课的价值几乎全在这个边界感上。
//
// 另外要讲清楚 `Arrays.binarySearch` 的一个真实差异：
// **它找不到时返回的不是 -1，而是 `-(插入点) - 1`**。
// 不知道这件事的人会把「找不到」当成「找到了一个负数下标」。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点一：**不许用 Arrays.binarySearch 冒充手写**。
//   去掉注释后切出 `二分查找` 的方法体，里面出现 `Arrays.binarySearch` 就判不过。
// 核心检查点二：**10 万元素上不能死循环**。
//   学生最常写错的两处：
//     · `while (low < high)`（漏了等号）⇒ 某些目标**找不到**（结果错）
//     · `low = mid`（少写 +1）⇒ **死循环**，撞 60 秒超时
//   调用点里对 10 万元素做十几次查找，任一处死循环就整体超时。
// 核心检查点三：**库的返回值要转成和手写一致的语义**。
//   `库二分` 要求找不到时也返回 -1（把 `-(插入点)-1` 转过来）——
//   直接 `return Arrays.binarySearch(...)` 的话，找不到时返回的是负数但不是 -1，
//   检查里专门喂「不存在的目标」把它抓出来。
//
// ⚠ 学生只写方法；骨架预置 import java.util.Arrays。

using System.Collections.Generic;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("26", "第 26 课：二分查找 —— 每次砍掉一半",
    "1. 写 static int 二分查找(int[] 数, int 目标)：找到返回**下标**，找不到返回 **-1**（手写，不许调库）\n" +
    "2. 写 static int 库二分(int[] 数, int 目标)：用 Arrays.binarySearch，但找不到时也要返回 -1\n" +
    "   （⚠ 库找不到时返回的是 -(插入点)-1，要自己转过来）\n" +
    "3. 两个方法对同一个输入必须给出一致的结果（含空数组、单元素、目标比所有数都大/都小）")]
public static class 第26课
{
    private const string 骨架 = """
import java.util.Arrays;

public class Main {
/*学生代码*/

    public static void main(String[] args) {
/*调用点*/
    }
}
""";

    public static string 模板 => """
// 第 26 课：二分查找 —— 每次砍掉一半
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**二分本身不难，难的是那几个边界**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    二分查找（手写 + Arrays.binarySearch 的差别）
//
//  需要先会
//    第 06 课：数组
//    第 04 课：while 循环
//
// ─────────────────────────────────────────────────────
//  先介绍：猜数字游戏
// ─────────────────────────────────────────────────────
//
// 我心里想一个 1~100 的数，你来猜，我只回答「大了 / 小了 / 对了」。
// 最聪明的猜法不是 1、2、3… 那样一个一个问，而是**每次都猜中间那个**：
//
//     50 → 大了，那答案在 1~49
//     25 → 小了，那答案在 26~49
//     37 → …
//
// 每问一次，可能的范围就**少一半**。100 个数最多 7 次就猜中。
// 这就是二分查找。
//
// ⚠ 前提只有一个，但它不能破：**这排数必须是排好序的**。
//   连"中间那个"是偏大还是偏小都不知道，二分就无从谈起。
//
// ─────────────────────────────────────────────────────
//  一、手写二分：三个变量，四行关键代码
// ─────────────────────────────────────────────────────
//
//     int low = 0;
//     int high = 数.length - 1;               ← ⚠ 是 length - 1（下标从 0 开始）
//     while (low <= high) {                    ← ⚠ 是 <=，不是 <
//         int mid = low + (high - low) / 2;    ← 中点
//         if (数[mid] == 目标) return mid;      ← 找到了
//         if (数[mid] < 目标) {
//             low = mid + 1;                   ← ⚠ 是 mid + 1，不是 mid
//         } else {
//             high = mid - 1;                  ← ⚠ 是 mid - 1，不是 mid
//         }
//     }
//     return -1;                               ← 循环走完还没找到
//
// 这个写法里 `low` 和 `high` 表示的是「**还有可能藏着目标的区间**」，
// 两端都算在内（闭区间）。所以：
//
//     循环条件   low <= high      区间里还有数就继续（low == high 时还剩一个）
//     缩小左半   low = mid + 1    数[mid] 已经比过了，把它排除掉
//     缩小右半   high = mid - 1   同上
//
// ⚠ 中间那句为什么写 `low + (high - low) / 2` 而不是 `(low + high) / 2`：
//   两个写法在这里结果一样，但 **low + high 在数组极大时可能溢出 int**（第 20 课那个病）。
//   `low + (high - low) / 2` 永远安全。习惯它。
//
// ─────────────────────────────────────────────────────
//  二、三个等号，三种错法
// ─────────────────────────────────────────────────────
//
//     ✗ while (low < high)      区间里只剩一个数时退出循环 —— 那个数永远查不到
//     ✗ low = mid               数[mid] 没被排除 ⇒ 区间不再变小 ⇒ **死循环**
//     ✗ high = 数.length        用长度当上界 ⇒ 第一次就可能去读 数[length]（越界）
//
// 三种都**编译通过**。前两种只是结果不对（或者永远跑不完），
// 第三种会抛：
//
//     Exception in thread "main" java.lang.ArrayIndexOutOfBoundsException:
//         Index 10 out of bounds for length 10
//
// ─────────────────────────────────────────────────────
//  三、Arrays.binarySearch 的一个陷阱
// ─────────────────────────────────────────────────────
//
// Java 自带二分：
//
//     int 位置 = Arrays.binarySearch(数, 目标);
//
// 找到时返回下标 —— 这点和手写一样。**找不到时它返回的不是 -1**：
//
//     返回   -(插入点) - 1
//
// 「插入点」= 如果要把目标插进去保持有序，它该在的下标。
//
// 看个例子（数 = {10, 20, 30, 40, 50}）：
//
//     Arrays.binarySearch(数, 35)   →   -4
//          因为 35 该插在下标 3 的位置 ⇒ -(3) - 1 = -4
//     Arrays.binarySearch(数, 5)    →   -1        ⇒ -(0) - 1
//     Arrays.binarySearch(数, 99)   →   -6        ⇒ -(5) - 1
//
// ⚠ 为什么这么设计：负数表示「没找到」，而这个负数还**顺便告诉了你插在哪儿** ——
//   这对「插入到有序数组里」的用法很有用。但它和「-1 表示找不到」的习惯不一样，
//   所以 `if (位置 >= 0)` 是对的，`if (位置 == -1)` 是错的。
//
// ⇒ 这一课要求你把库的结果**转成和手写一致的语义**（找不到就是 -1）：
//
//     int 位置 = Arrays.binarySearch(数, 目标);
//     return 位置 >= 0 ? 位置 : -1;
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：low = mid（少写一个 +1）
// ─────────────────────────────────────────────────────
//
//     while (low <= high) {
//         int mid = (low + high) / 2;
//         if (数[mid] < 目标) {
//             low = mid;                      ← ⚠ 少了 +1
//         }
//         …
//     }
//
// 你会看到：**编译通过、开始几轮看着正常**，然后程序**再也不结束**。
//
// 为什么：当区间只剩两个数（low = 5、high = 6）时，mid 算出来是 5。
//   如果 数[5] < 目标，你写 `low = mid` 就等于 `low = 5` —— **一点没动**，
//   下一轮还是这三个数、还是这个 mid。区间再也不缩小，永远出不来。
//
// ⇒ 二分里凡是要「排除 mid 这一个」，**必须写成 `mid ± 1`**。
//   记住那句话：**数[mid] 已经比过了，不能再放进下一轮的区间里。**
//
// （检查里会拿 10 万个数做十几次查找 —— 任何一处死循环都会直接超时。）
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
//   排序（二分的前提）→ 第 18、29、30 课。
//   二分答案（对答案做二分）→ 不涉及。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 你**只写方法**：不用写 class，不用写 main；
//    `java.util.Arrays` 已经 import 好了。每个方法都要带 **static**。
//
//   1. static int 二分查找(int[] 数, int 目标)
//        **手写**二分。找到返回下标，找不到返回 -1。
//        ⚠ 不许调 Arrays.binarySearch（检查会切开方法体看）。
//
//   2. static int 库二分(int[] 数, int 目标)
//        用 Arrays.binarySearch，但**找不到时也返回 -1**（把它那个
//        `-(插入点)-1` 转过来）。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
//   ⚠ 检查里会拿 10 万个数做十几次查找。如果你看到一个「超时」，
//     那八成就是上面说的 `low = mid` 死循环。
//
// ── 在下面写 ──
    static int 二分查找(int[] 数, int 目标) {
        return -1;
    }

    static int 库二分(int[] 数, int 目标) {
        return -1;
    }
""";

    public static string 参考答案 => """
// 参考答案 —— 二分查找（只写方法，不用写 class / main / import）

// 1. 手写二分：low / high 表示「还有可能藏着目标的闭区间」
//    看清楚三个 mid ± 1 —— 数[mid] 比过了就不能再留在区间里
static int 二分查找(int[] 数, int 目标) {
    int low = 0;
    int high = 数.length - 1;                 /* ⚠ length - 1：下标从 0 开始 */
    while (low <= high) {                     /* ⚠ <= ：区间里还剩一个数时也要进来比一比 */
        int mid = low + (high - low) / 2;     /* 这样写不会溢出（low + high 在大数组上会） */
        if (数[mid] == 目标) {
            return mid;
        }
        if (数[mid] < 目标) {
            low = mid + 1;                    /* ⚠ mid + 1：把 mid 排除掉，否则死循环 */
        } else {
            high = mid - 1;                   /* ⚠ mid - 1 */
        }
    }
    return -1;                                /* 区间空了 ⇒ 就是没有 */
}

// 2. 库二分：⚠ 它的「找不到」不是 -1，而是 -(插入点) - 1
//    所以只判断 < 0 是不够的（那样返回值会是 -4、-1、-6 …），
//    这一课要求统一成 -1
static int 库二分(int[] 数, int 目标) {
    int 位置 = Arrays.binarySearch(数, 目标);
    if (位置 >= 0) {
        return 位置;
    }
    return -1;
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("两个方法都在，签名也对")]
    public static JavaCheckResult 检查_函数都在(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        foreach (var 名 in new[] { "二分查找", "库二分" })
            if (!Regex.IsMatch(净, @"static\s+int\s+" + 名 + @"\s*\(\s*int\s*\[\s*\]\s*\w+\s*,\s*int\s+\w+\s*\)"))
                缺.Add("      " + 名 + "　（static int " + 名 + "(int[] 数, int 目标)）");

        if (缺.Count == 0) return JavaCheckResult.过("两个方法都在，签名也对");
        return JavaCheckResult.不过(
            "有方法没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      ⚠ 参数是 (int[] 数, int 目标)，返回 int（下标，找不到是 -1）。");
    }

    [Java检查说明("★ 手写的那个不许调 Arrays.binarySearch（那样这项练习就没了）")]
    public static JavaCheckResult 检查_不许调库(string 代码)
    {
        // ⚠ 核心检查点之一。
        string 净 = 去掉注释(代码);
        string 体 = 函数体(净, "二分查找");
        if (体 == null)
            return JavaCheckResult.不过("没找到 二分查找 的函数体 —— 先确认这个方法写全了（花括号配对）。");

        if (体.Contains("Arrays.binarySearch"))
            return JavaCheckResult.不过(
                "二分查找 里调了 Arrays.binarySearch —— 那是第 2 个方法（库二分）该干的事。\n" +
                "      这一课要练的是**你自己把边界写对**：\n" +
                "          int low = 0, high = 数.length - 1;\n" +
                "          while (low <= high) {\n" +
                "              int mid = low + (high - low) / 2;\n" +
                "              if (数[mid] == 目标) return mid;\n" +
                "              if (数[mid] < 目标) low = mid + 1; else high = mid - 1;\n" +
                "          }\n" +
                "          return -1;\n" +
                "      三个 mid ± 1 和那个 <= 就是这一课的全部价值 —— 调一次库就全绕过去了。\n" +
                "      （真实项目里当然该用库；这里是练习。）");

        if (!体.Contains("while") && !体.Contains("for"))
            return JavaCheckResult.不过(
                "二分查找 里没有循环 —— 二分是「一轮砍掉一半」，必须有个循环（或者递归）。\n" +
                "      如果只是想用别的方式找，那这一课就没练到了：\n" +
                "      从头扫到尾是 O(n)，二分是 O(log n)，一百万个元素差 5 万倍。");

        return JavaCheckResult.过("手写的二分：没用库，也有循环");
    }

    [Java检查说明("★ 十万个数：结果对，而且不能死循环（low = mid 就会卡住）")]
    public static JavaCheckResult 检查_十万个数(string 代码)
    {
        // ⚠ 核心检查点之二：死循环会在这里撞超时。
        var run = 跑一次(代码);
        if (run.Raw.Error != null && run.Raw.Error.Contains("超时"))
            return JavaCheckResult.不过(
                "查找十万个数的时候**卡住了**（超时）—— 这是二分最经典的一个错：死循环。\n" +
                "      最可能的原因：`low = mid`（少了 +1），或者 `high = mid`（少了 -1）。\n" +
                "      为什么它会死循环：区间只剩两个数时（low=5、high=6），mid 算出来是 5，\n" +
                "      如果 数[5] < 目标，你写 `low = mid` 就等于**什么都没改**，\n" +
                "      下一轮还是这三个数、还是这个 mid —— 永远出不来。\n" +
                "      ⇒ 记住：**数[mid] 已经比过了，不能再放进下一轮的区间**，\n" +
                "        所以必须是 low = mid + 1 / high = mid - 1。");
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("大首|", "0", "10 万个偶数里找 0（第一个）⇒ 下标 0"),
            ("大中|", "50000", "找 100000 ⇒ 下标 50000"),
            ("大尾|", "99999", "找 199998（最后一个）⇒ 下标 99999"),
            ("大有一|", "1234", "找 2468 ⇒ 下标 1234"),
            ("大无一|", "-1", "找 3（不存在，且比所有数都小）⇒ -1"),
            ("大无二|", "-1", "找 200001（不存在，且比所有数都大）⇒ -1"),
            ("大无三|", "-1", "找 99999（是奇数，数组里全是偶数）⇒ -1"));
    }

    [Java检查说明("★ 库二分：找不到时必须返回 -1（不是 -(插入点)-1）")]
    public static JavaCheckResult 检查_库的语义(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("库找有|", "1", "库二分({10,20,30,40,50}, 20) ⇒ 下标 1"),
            ("库无小|", "-1", "库二分({10,20,30,40,50}, 5)　⚠ Arrays.binarySearch 给的是 **-1**（-(0)-1），刚好也是 -1，容易蒙对"),
            ("库无中|", "-1", "库二分({10,20,30,40,50}, 35)　⚠ 库给的是 **-4**（-(插入点 3)-1）—— 直接 return 它就会得到 -4，\n" +
                "            这一课要求统一成 -1，所以要写 `位置 >= 0 ? 位置 : -1`"),
            ("库无大|", "-1", "库二分({10,20,30,40,50}, 99)　⚠ 库给的是 **-6**"));
    }

    [Java检查说明("边界：空数组、单元素、两端（手写和库必须一致）")]
    public static JavaCheckResult 检查_边界(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("空手|", "-1", "二分查找({}, 5)　空数组 ⇒ 直接 -1"),
            ("空库|", "-1", "库二分({}, 5)　⚠ 空数组时库也返回 -1，但别指望它总是这样"),
            ("单手|", "0", "二分查找({7}, 7)　只有一个元素且命中"),
            ("单库|", "0", "库二分({7}, 7)"),
            ("单无|", "-1", "二分查找({7}, 5)　只有一个元素且不命中 ⇒ ⚠ 循环条件写成 low < high 的话这里会错"),
            ("左端|", "0", "二分查找({1,3,5,7,9}, 1)　最左边那个"),
            ("右端|", "4", "二分查找({1,3,5,7,9}, 9)　最右边那个"),
            ("中间|", "2", "二分查找({1,3,5,7,9}, 5)"),
            ("库中间|", "2", "库二分({1,3,5,7,9}, 5)"));
    }

    // ══════════════════ 工具 ══════════════════

    private const string 调用点 = """
        int[] 甲 = {10, 20, 30, 40, 50};
        System.out.println("库找有|" + 库二分(甲, 20));
        System.out.println("库无小|" + 库二分(甲, 5));
        System.out.println("库无中|" + 库二分(甲, 35));
        System.out.println("库无大|" + 库二分(甲, 99));

        int[] 空 = {};
        System.out.println("空手|" + 二分查找(空, 5));
        System.out.println("空库|" + 库二分(空, 5));
        int[] 单 = {7};
        System.out.println("单手|" + 二分查找(单, 7));
        System.out.println("单库|" + 库二分(单, 7));
        System.out.println("单无|" + 二分查找(单, 5));

        int[] 乙 = {1, 3, 5, 7, 9};
        System.out.println("左端|" + 二分查找(乙, 1));
        System.out.println("右端|" + 二分查找(乙, 9));
        System.out.println("中间|" + 二分查找(乙, 5));
        System.out.println("库中间|" + 库二分(乙, 5));

        int[] 大 = new int[100000];
        for (int i = 0; i < 大.length; i++) {
            大[i] = i * 2;
        }
        System.out.println("大首|" + 二分查找(大, 0));
        System.out.println("大中|" + 二分查找(大, 100000));
        System.out.println("大尾|" + 二分查找(大, 199998));
        System.out.println("大有一|" + 二分查找(大, 2468));
        System.out.println("大无一|" + 二分查找(大, 3));
        System.out.println("大无二|" + 二分查找(大, 200001));
        System.out.println("大无三|" + 二分查找(大, 99999));
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
        {
            if (run.Raw.Error.Contains("ArrayIndexOutOfBounds"))
                return "下标越界了：\n      " + run.Raw.Error + "\n" +
                       "      二分的上界是 **数.length - 1**（最后一个元素的下标），\n" +
                       "      写成 数.length 就会去读 数[length] —— 越界。";
            return "程序没跑完就退出了：\n      " + run.Raw.Error;
        }
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
            "      二分的三个边界，一个一个对：\n" +
            "          int high = 数.length - 1;          ← 不是 数.length\n" +
            "          while (low <= high)               ← 不是 low < high\n" +
            "          low = mid + 1 / high = mid - 1    ← 必须有 ±1\n" +
            "      记住「区间」的意思：low 和 high 之间**还有可能藏着目标**。\n" +
            "      数[mid] 已经比过了 ⇒ 它必须被排除在新区间之外。");
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
}
