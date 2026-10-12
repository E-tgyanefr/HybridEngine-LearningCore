// 第 30 课：快速排序 —— 原地、平均最快，但最坏会退化
//
// ══ 这一课要教什么 ══
// 快排和归并都是 O(n log n)，但思路完全不同：
//   归并是「先分到底、再合并」，合并时要额外空间；
//   快排是「**先挑一个基准值，把小的甩到左边、大的甩到右边**」，
//   这一趟走完，基准值就**到了它最终该在的位置**，然后左右两段各自再来一遍。
//
// 快排平均比归并快（原地、访存连续），但它有一个著名的软肋：
// **基准值选得不好就退化成 O(n²)** —— 最典型的是「数组本来就有序，
// 而你每次都取最后一个当基准」。这一课要把这个退化**亲手跑出来**。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点一：**不许用 Arrays.sort**（同第 29 课）。
// 核心检查点二：**有序数据上不能栈溢出**。
//   这是本课真正要教的坑，而且它**一定会发生**：
//   Lomuto 划分 + 「取最后一个当基准」，在**已经有序**的输入上每次只切掉一个元素，
//   递归深度 = n。调用点里喂 50000 个已经有序的数 —— 递归实现会报
//     java.lang.StackOverflowError
//   而随机基准（或者三数取中）把它压到 O(log n) 深，顺利跑完。
//   ⚠ 这个 50000 是实测过的：Lomuto + 末位基准在 20000 上必崩（栈深 20000），
//     而在 5000 上恰好不崩 —— 所以这里用 20000，别调小。
// 核心检查点三：**划分 要真的把基准放到位**（方法体里有循环，而且返回值在下标范围内）——
//   第 3 条检查直接调 划分 然后验证「基准左边的都不大于它、右边的都不小于它」。
//   ⚠ 注意：划分 会改动数组，所以每次调用前都要重新准备一份数据。
//
// ⚠ 学生只写方法；骨架预置 import java.util.Arrays / java.util.Random。

using System.Collections.Generic;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("30", "第 30 课：快速排序 —— 原地、平均最快，但最坏会退化",
    "1. 写 static int 划分(int[] 数, int 左, int 右)：挑一个基准，把小的甩左边、大的甩右边，返回基准最终的下标\n" +
    "2. 写 static void 快速排序(int[] 数, int 左, int 右)：递归地把左右两段各排一遍\n" +
    "3. ⚠ 基准值要**随机挑**（或者三数取中）—— 每次都取最后一个的话，有序数据上会栈溢出\n" +
    "4. 不许用 Arrays.sort")]
public static class 第30课
{
    private const string 骨架 = """
import java.util.Arrays;
import java.util.Random;

public class Main {
/*学生代码*/

    public static void main(String[] args) {
/*调用点*/
    }
}
""";

    public static string 模板 => """
// 第 30 课：快速排序 —— 原地、平均最快，但最坏会退化
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**同样叫 O(n log n)，选错了基准就会退化成 O(n²)**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    快速排序：划分（partition）、基准值的选择、最坏情况
//
//  需要先会
//    第 29 课：分治与归并排序（同一个量级，另一种思路）
//    第 18 课：交换两个数的三行写法
//
// ─────────────────────────────────────────────────────
//  先介绍：快排的核心是「一趟划分」
// ─────────────────────────────────────────────────────
//
// 归并排序是「先分到底、再合并」。快排反过来 —— **先干一件大事，再分**：
//
//     ① 挑一个数当**基准**（pivot）
//     ② 走一趟：把比基准小的都甩到左边、比它大的都甩到右边
//        ⇒ 这一趟走完，**基准就站在它最终该在的位置上了**
//     ③ 基准左边那一段、右边那一段，各自再来一遍
//
// 关键差别：归并的"分"只是切开，真正干活的是"合"；
//   快排的"分"就**已经把基准安置好了**，所以它不需要"合"。
//
// ─────────────────────────────────────────────────────
//  一、Lomuto 划分：一个指针扫一遍
// ─────────────────────────────────────────────────────
//
// 取最后一个元素当基准，用一个 `小` 记录"比基准小的那一段的末尾"：
//
//     int 基准 = 数[右];
//     int 小 = 左 - 1;                       ← 还没有任何元素确定比基准小
//     for (int i = 左; i < 右; i++) {
//         if (数[i] <= 基准) {
//             小 = 小 + 1;
//             交换 数[小] 和 数[i];            ← 把它丢进"小"那一段
//         }
//     }
//     交换 数[小 + 1] 和 数[右];              ← 基准放到分界点上
//     return 小 + 1;                          ← 这就是基准最终的下标
//
// 走完这一趟，`数[左..小]` 全 ≤ 基准，`数[小+2..右]` 全 > 基准，
// 而 数[小+1] 就是基准 —— **它的位置已经是对的了**，不用再动。
//
// ─────────────────────────────────────────────────────
//  二、递归：左右两段各自再来
// ─────────────────────────────────────────────────────
//
//     static void 快速排序(int[] 数, int 左, int 右) {
//         if (左 >= 右) return;              ← 基准情形：0 个或 1 个数
//         int 位 = 划分(数, 左, 右);
//         快速排序(数, 左, 位 - 1);            ← 左边那段
//         快速排序(数, 位 + 1, 右);            ← 右边那段
//     }
//
// ⚠ 注意两个递归区间**都把基准排除在外**（位-1 / 位+1）——
//   基准已经在最终位置上了，再排它一次是白干，而且可能死循环。
//
// ─────────────────────────────────────────────────────
//  三、最坏情况：基准选得不好，快排会变成 O(n²)
// ─────────────────────────────────────────────────────
//
// 快排快不快，**全看基准选得好不好**：
//
//     基准每次都落在正中间   ⇒ 每次劈一半 ⇒ log n 层 ⇒ O(n log n)   好
//     基准每次都落在最边上   ⇒ 每次只切掉一个 ⇒ n 层   ⇒ O(n²)      坏
//
// 最要命的例子：**数组本来就有序，而你每次都取最后一个当基准**。
//
//     数 = {1, 2, 3, 4, 5, …, n}        取最后一个（n）当基准
//     一趟走完：前 n-1 个全都 ≤ n ⇒ 它们全在基准左边，基准还是落在最右
//     ⇒ 下一趟要处理 n-1 个，再下一趟 n-2 个… ⇒ **递归深度 n**
//
// 后果有两层，而且第二层比第一层更早要命：
//   · 时间：O(n²)
//   · **空间**：每一层递归都占一格调用栈 ⇒ n 层 ⇒ n = 20000 时就
//
//         Exception in thread "main" java.lang.StackOverflowError
//
// ⇒ 解决办法很简单：**随机挑基准**。随机化之后，"每次都挑到最边上"
//   变成一个小概率事件，期望深度回到 O(log n)。
//
//     int 随 = 左 + 随机.nextInt(右 - 左 + 1);
//     交换 数[随] 和 数[右];              ← 先把它换到末尾，后面的代码一行都不用改
//     …（照 Lomuto 走）
//
// ⚠ 为什么要"换到末尾再照旧走"：这样 **划分 的其余逻辑完全不用动**，
//   只是把"从哪儿挑基准"这一件事分离出来。改代码时把变化点收在一处，是个好习惯。
//
//   另一种常见做法是**三数取中**（取 左、中、右 三个数的中位数当基准）——
//   效果接近，而且不需要 Random。用来考试手写也很稳。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：有序数据 + 固定基准 = 栈溢出
// ─────────────────────────────────────────────────────
//
//     static int 划分(int[] 数, int 左, int 右) {
//         int 基准 = 数[右];                  ← 固定取最后一个
//         …
//     }
//
// 你会看到（喂一份**已经排好序**的数据时）：
//
//     Exception in thread "main" java.lang.StackOverflowError
//
// 为什么：有序数组里"最后一个"正好是最大的那个，一趟划分只把它自己安置好，
//   剩下 n-1 个全在左边 ⇒ 递归深度 n。
//   20000 个数就是 20000 层 —— 栈装不下。
//
// ⚠ 这个坑最阴的地方：**随机数据上完全看不出来**。
//   随机打乱的数组里"最后一个"平均落在中间，深度是 O(log n)。
//   所以你必须**专门拿有序数据试一次**，才会撞上它。
//
// ⇒ 加两行随机化。附带的好处：它同时挡住了"对手专门构造一组数据来卡你"。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
//   双轴快排（JDK 对**基本类型**用的那个）→ 不涉及。
//   归并排序 → 上一课。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 你**只写方法**：不用写 class，不用写 main；
//    `java.util.Arrays` 和 `java.util.Random` 已经 import 好了。
//    每个方法都要带 **static**。
//
//   1. static int 划分(int[] 数, int 左, int 右)
//        挑一个基准、把小的甩左边大的甩右边，返回**基准最终的下标**。
//        用 Lomuto 或 Hoare 都行，但要在注释里说清你用的是哪种。
//        ⚠ 基准要**随机挑**（或者三数取中）。
//
//   2. static void 快速排序(int[] 数, int 左, int 右)
//        if (左 >= 右) return; 然后 划分、递归左右两段（**不含基准**）。
//
//  ⚠ 不许用 Arrays.sort。检查会喂一份**已经有序的 20000 个数** ——
//    固定取末尾当基准的写法在那里会栈溢出。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
    static int 划分(int[] 数, int 左, int 右) {
        return 左;
    }

    static void 快速排序(int[] 数, int 左, int 右) {
    }
""";

    public static string 参考答案 => """
// 参考答案 —— 快速排序（只写方法，不用写 class / main / import）
//
// 用的是 **Lomuto 划分**：取基准、一个指针扫一遍、最后把基准换到分界点。
// 基准**随机挑** —— 这两行就是这一课的正题（不加它，有序数据上会栈溢出）。

// 随机数发生器：写成一个静态字段，别每次调用都 new Random()
// （每次 new 出来的是同一个种子，短时间内会给出一样的数）
static final Random 随机 = new Random(20240601);

// 划分：返回基准最终的下标
// 走完之后：数[左..位-1] 全 ≤ 基准，数[位] 是基准（位置已定），数[位+1..右] 全 > 基准
static int 划分(int[] 数, int 左, int 右) {
    /* ① 随机挑一个当基准，先换到末尾 —— 这样下面的 Lomuto 逻辑不用动 */
    int 随 = 左 + 随机.nextInt(右 - 左 + 1);
    int 临时 = 数[随];
    数[随] = 数[右];
    数[右] = 临时;

    /* ② Lomuto：小 记录"比基准小的那一段"的末尾 */
    int 基准 = 数[右];
    int 小 = 左 - 1;
    for (int i = 左; i < 右; i++) {
        if (数[i] <= 基准) {
            小 = 小 + 1;
            int 换 = 数[小];
            数[小] = 数[i];
            数[i] = 换;
        }
    }

    /* ③ 把基准放到分界点上 —— 这一步之后它的位置就是最终的了 */
    int 换2 = 数[小 + 1];
    数[小 + 1] = 数[右];
    数[右] = 换2;
    return 小 + 1;
}

// 递归：基准已经在最终位置，所以左右两段都要**把它排除掉**（位-1 / 位+1）
static void 快速排序(int[] 数, int 左, int 右) {
    if (左 >= 右) {
        return;                           /* 基准情形：0 个或 1 个数已经有序 */
    }
    int 位 = 划分(数, 左, 右);
    快速排序(数, 左, 位 - 1);
    快速排序(数, 位 + 1, 右);
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("两个方法都在，签名也对")]
    public static JavaCheckResult 检查_函数都在(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        if (!Regex.IsMatch(净, @"static\s+int\s+划分\s*\(\s*int\s*\[\s*\]\s*\w+\s*,\s*int\s+\w+\s*,\s*int\s+\w+\s*\)"))
            缺.Add("      划分　　（static int 划分(int[] 数, int 左, int 右)）");
        if (!Regex.IsMatch(净, @"static\s+void\s+快速排序\s*\(\s*int\s*\[\s*\]\s*\w+\s*,\s*int\s+\w+\s*,\s*int\s+\w+\s*\)"))
            缺.Add("      快速排序（static void 快速排序(int[] 数, int 左, int 右)）");

        if (缺.Count == 0) return JavaCheckResult.过("两个方法都在，签名也对");
        return JavaCheckResult.不过(
            "有方法没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      ⚠ 划分 返回的是 **int**（基准最终的下标），不是 void。");
    }

    [Java检查说明("★ 不许用 Arrays.sort")]
    public static JavaCheckResult 检查_没调现成排序(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 用了 = new List<string>();
        foreach (var 名 in new[] { "Arrays.sort", "Collections.sort" })
            if (净.Contains(名)) 用了.Add(名);
        if (用了.Count == 0)
            return JavaCheckResult.过("没有看到现成的排序调用 —— 是手写的");

        return JavaCheckResult.不过(
            "代码里出现了现成的排序：" + string.Join("、", 用了) + "\n" +
            "      这一课要你亲手实现「划分」——它是快排的全部：\n" +
            "          挑基准 → 小的甩左边、大的甩右边 → 基准就位 → 左右各自递归\n" +
            "      Arrays.sort 对**基本类型**用的正是双轴快排，把它当黑盒调一下就什么都没练到。\n" +
            "      ⇒ 换成 Lomuto（或者 Hoare）划分：\n" +
            "          int 基准 = 数[右];  int 小 = 左 - 1;\n" +
            "          for (int i = 左; i < 右; i++)\n" +
            "              if (数[i] <= 基准) { 小++; 交换 数[小] 和 数[i]; }\n" +
            "          交换 数[小 + 1] 和 数[右];  return 小 + 1;");
    }

    [Java检查说明("★ 有序的 50000 个数：固定取末尾当基准会栈溢出（随机基准就没事）")]
    public static JavaCheckResult 检查_有序不崩(string 代码)
    {
        // ⚠ 核心检查点：Lomuto + 末尾基准在有序数据上递归深度 = n。
        //   实测 20000 个数会报 StackOverflowError，而随机基准顺利跑完。
        var run = 跑一次(代码);
        if (run.Raw.Error != null && run.Raw.Error.Contains("StackOverflow"))
            return JavaCheckResult.不过(
                "**栈溢出**了（java.lang.StackOverflowError）—— 这正是这一课要你亲眼看见的坑。\n" +
                "      检查里喂了一份**已经排好序**的 50000 个数。\n" +
                "      如果你每次都取最后一个当基准：最后一个正好是最大的那个，\n" +
                "      一趟划分只把它自己安置好，剩下 n-1 个全在左边 ⇒ **递归深度 n**：\n" +
                "          第 1 层处理 50000 个，第 2 层 49999 个，第 3 层 49998 个…\n" +
                "      每一层都占一格调用栈，5 万层 ⇒ 栈装不下。\n" +
                "      ⇒ 基准**随机挑**（进 划分 的第一步就是它）：\n" +
                "          int 随 = 左 + 随机.nextInt(右 - 左 + 1);\n" +
                "          交换 数[随] 和 数[右];        // 先换到末尾，后面的 Lomuto 一行都不用改\n" +
                "      ⚠ 这个坑在**随机数据上看不出来** —— 随机数组里「最后一个」平均落在中间。\n" +
                "        所以必须专门拿有序数据试一次，才会撞上它。");
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("有序五万|", "true", "把已经排好序的 50000 个数交给你的快排 —— 不能崩，而且排完仍然有序"),
            ("有序首尾|", "true", "排完之后首尾也要有序（第 0 个 ≤ 最后一个）"));
    }

    [Java检查说明("★ 划分 是对的：返回的位置左边都不大于它、右边都不小于它")]
    public static JavaCheckResult 检查_划分正确(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);

        string 净 = 去掉注释(代码);
        string 体 = 函数体(净, "划分");
        if (体 == null)
            return JavaCheckResult.不过("没找到 划分 的函数体 —— 先确认这个方法写全了（花括号配对）。");
        if (!Regex.IsMatch(体, @"\b(for|while)\b"))
            return JavaCheckResult.不过(
                "划分 的函数体里没有循环 —— 它必须**扫一遍这一段**，把比基准小的挪到前面去。\n" +
                "      Lomuto 的样子：\n" +
                "          int 基准 = 数[右];  int 小 = 左 - 1;\n" +
                "          for (int i = 左; i < 右; i++)\n" +
                "              if (数[i] <= 基准) { 小++; 交换 数[小] 和 数[i]; }\n" +
                "          交换 数[小 + 1] 和 数[右];  return 小 + 1;\n" +
                "      ⚠ 返回值不是随便给的：它是**基准最终的位置**，递归就靠它切两半。");

        return 比(run,
            ("划分位|", "true", "划分({3,1,4,1,5,9,2,6}, 0, 7) 返回的下标要在 0..7 之内"),
            ("划分左|", "true", "返回的位置左边所有数都 ≤ 基准"),
            ("划分右|", "true", "返回的位置右边所有数都 ≥ 基准"),
            ("划分值|", "true", "返回位置上那个数，就是这一趟选中的基准（它已经在最终位置了）"));
    }

    [Java检查说明("各种数据都排对（空、单个、重复、已排好、倒序、大量随机）")]
    public static JavaCheckResult 检查_各种数据(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("空|", "[]", "快速排序({}, 0, -1)　0 个元素"),
            ("单|", "[42]", "快速排序({42}, 0, 0)　基准情形"),
            ("两个|", "[1, 2]", "快速排序({2,1}, 0, 1)　⚠ 最小的递归规模，基准排除写错就会死循环"),
            ("普通|", "[1, 2, 4, 5]", "快速排序({5,1,4,2})"),
            ("重复|", "[1, 3, 3, 7, 7]", "含重复元素"),
            ("全同|", "[6, 6, 6, 6, 6]", "⚠ 全部相同 —— Lomuto 里 `<=` 写对就不会退化"),
            ("已排|", "[1, 2, 3, 4, 5]", "已经排好的小数组"),
            ("倒序|", "[-3, -1, 2, 6, 9]", "含负数"),
            ("随机|", "true", "50000 个固定种子的随机数：排完之后必须有序"));
    }

    // ══════════════════ 工具 ══════════════════

    private const string 调用点 = """
        int[] 空 = {};
        快速排序(空, 0, -1);
        System.out.println("空|" + Arrays.toString(空));

        int[] 单 = {42};
        快速排序(单, 0, 0);
        System.out.println("单|" + Arrays.toString(单));

        int[] 两 = {2, 1};
        快速排序(两, 0, 1);
        System.out.println("两个|" + Arrays.toString(两));

        int[] 甲 = {5, 1, 4, 2};
        快速排序(甲, 0, 甲.length - 1);
        System.out.println("普通|" + Arrays.toString(甲));

        int[] 乙 = {7, 3, 7, 1, 3};
        快速排序(乙, 0, 乙.length - 1);
        System.out.println("重复|" + Arrays.toString(乙));

        int[] 丙 = {6, 6, 6, 6, 6};
        快速排序(丙, 0, 丙.length - 1);
        System.out.println("全同|" + Arrays.toString(丙));

        int[] 丁 = {1, 2, 3, 4, 5};
        快速排序(丁, 0, 丁.length - 1);
        System.out.println("已排|" + Arrays.toString(丁));

        int[] 戊 = {9, 6, 2, -1, -3};
        快速排序(戊, 0, 戊.length - 1);
        System.out.println("倒序|" + Arrays.toString(戊));

        /* 划分 单测：调一次，再验「基准左边的都不大于它、右边都不小于它」 */
        int[] 己 = {3, 1, 4, 1, 5, 9, 2, 6};
        int 位 = 划分(己, 0, 7);
        boolean 位对 = 位 >= 0 && 位 <= 7;
        boolean 左对 = true;
        for (int i = 0; i < 位; i++) {
            if (己[i] > 己[位]) 左对 = false;
        }
        boolean 右对 = true;
        for (int i = 位 + 1; i < 己.length; i++) {
            if (己[i] < 己[位]) 右对 = false;
        }
        System.out.println("划分位|" + 位对);
        System.out.println("划分左|" + 左对);
        System.out.println("划分右|" + 右对);
        System.out.println("划分值|" + (位对 && 左对 && 右对));

        Random 随机 = new Random(999);
        int[] 大 = new int[50000];
        for (int i = 0; i < 大.length; i++) {
            大[i] = 随机.nextInt(100000);
        }
        快速排序(大, 0, 大.length - 1);
        boolean 大有序 = true;
        for (int i = 1; i < 大.length; i++) {
            if (大[i - 1] > 大[i]) 大有序 = false;
        }
        System.out.println("随机|" + 大有序);

        /* ⚠ 这一组是这一课的正题：**已经有序**的 50000 个数。
           固定取末尾当基准 ⇒ 递归深度 20000 ⇒ StackOverflowError。 */
        int[] 有序 = new int[50000];
        for (int i = 0; i < 有序.length; i++) {
            有序[i] = i;
        }
        快速排序(有序, 0, 有序.length - 1);
        boolean 有序对 = true;
        for (int i = 1; i < 有序.length; i++) {
            if (有序[i - 1] > 有序[i]) 有序对 = false;
        }
        System.out.println("有序五万|" + 有序对);
        System.out.println("有序首尾|" + (有序[0] <= 有序[有序.length - 1]));
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
            if (run.Raw.Error.Contains("StackOverflow"))
                return "**栈溢出**了（StackOverflowError）—— 递归没有出口，或者基准选得不好。\n" +
                       "      两个常见原因：\n" +
                       "        · 递归区间忘了把基准排除（应该是 位-1 / 位+1）⇒ 区间不缩小 ⇒ 无限递归\n" +
                       "        · 基准固定取末尾，而数据已经有序 ⇒ 递归深度 = n（这一课的正题）";
            if (run.Raw.Error.Contains("ArrayIndexOutOfBounds"))
                return "下标越界了：\n      " + run.Raw.Error + "\n" +
                       "      ⚠ 检查一下 划分 里的循环范围：Lomuto 的内层是 `i < 右`（基准本身不参与扫描），\n" +
                       "        而且递归时基准要排除掉（位-1 / 位+1）。";
            if (run.Raw.Error.Contains("超时"))
                return "跑不完（超时）。最可能：递归区间没把基准排除，导致区间不缩小、无限递归。";
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
            "      快排的三处要点：\n" +
            "        · 划分 走完后基准**就在最终位置**上，所以递归区间要把它排除（位-1 / 位+1）\n" +
            "        · 基准要随机挑（不然有序数据上深度 = n，栈溢出）\n" +
            "        · 交换永远是三行：临时 = a; a = b; b = 临时;");
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

    /// <summary>取某个方法的「{ … }」内容（配对花括号）。找不到返回 null。</summary>
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
