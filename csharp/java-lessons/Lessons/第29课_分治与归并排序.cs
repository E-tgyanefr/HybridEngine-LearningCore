// 第 29 课：分治与归并排序 —— 把大问题劈成两半
//
// ══ 这一课要教什么 ══
// 第 18 课那三种排序都是 O(n²)：n = 10 万时要一百亿次比较。
// 归并排序换了个思路 —— **分治**：
//   ① 分：把数组劈成两半
//   ② 治：两半各自排好（递归）
//   ③ 合：把两个**已经有序**的段并成一个有序段
//
// 「合」这一步是全部的关键，也是最容易小看的一步：
// 合并两个有序段只要 O(n) —— 每次只要比较两段的**开头**，谁小就取谁。
// 于是总时间 T(n) = 2T(n/2) + O(n) = **O(n log n)**，10 万个数几百毫秒的事。
//
// 这也是 `Arrays.sort` 内部在做的事情之一（对象数组用的是归并的变体 TimSort）。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点一：**不许用现成排序**（Arrays.sort / Collections.sort）。
//   去掉注释后看源码文本就够了 —— 这一课的难点不在"写不出循环"，而在"想通分治"，
//   所以禁库函数比第 18 课更硬：出现了直接判不过。
// 核心检查点二：**必须真的递归**（`归并排序` 的方法体里出现 `归并排序(`）。
//   学生很容易把 `归并排序` 写成"调用一下 归并 然后返回"，
//   或者干脆在里面写个冒泡 —— 那样输出全对，但分治一点没学到。
// 核心检查点三：**10 万个随机数要排得完**。
//   冒泡冒充是 O(n²) = 100 亿次比较 ⇒ 撞 60 秒超时。
//   ⚠ 随机数用固定种子（`new Random(12345)`），保证每次运行数据一样、可复现。
// 第四条：`归并` 也要真的在合并（方法体里有循环）。
//
// ⚠ 学生只写方法；骨架预置 import java.util.Arrays / java.util.Random。

using System.Collections.Generic;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("29", "第 29 课：分治与归并排序 —— 把大问题劈成两半",
    "1. 写 static void 归并(int[] 数, int 左, int 中, int 右)：把 左..中 和 中+1..右 两段**已排好**的并成一段\n" +
    "2. 写 static void 归并排序(int[] 数, int 左, int 右)：分治 —— 劈成两半、各自排好、再合并\n" +
    "3. 写 static int 分治最大(int[] 数, int 左, int 右)：用分治求这一段里最大的数\n" +
    "4. ⚠ 不许用 Arrays.sort —— 而且 归并排序 必须真的自己调用自己")]
public static class 第29课
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
// 第 29 课：分治与归并排序 —— 把大问题劈成两半
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**把一个大问题劈成两个同样形状的小问题**（分治）。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    分治；归并排序（O(n log n)）
//
//  需要先会
//    第 18 课：三种 O(n²) 的排序（对比着看）
//    第 28 课：递归（归并排序就是一个递归）
//
// ─────────────────────────────────────────────────────
//  先介绍：O(n²) 不够用了
// ─────────────────────────────────────────────────────
//
// 第 18 课那三种排序都是两层循环 ⇒ O(n²)。数一下代价：
//
//     n = 1000      100 万次比较        还行
//     n = 10 万     100 亿次比较        几十秒
//     n = 1 万      1 亿次              几秒
//
// 而 `Arrays.sort` 排 10 万个数只要**几毫秒**。它凭什么快这么多？
// 因为它不是"反复比较交换"，而是**分治**。
//
// ─────────────────────────────────────────────────────
//  一、分治：分 → 治 → 合
// ─────────────────────────────────────────────────────
//
// 归并排序的三步：
//
//     ① 分   把数组从中间劈成两半
//     ② 治   两半**各自**排好（这就是递归 —— 同一个问题、更小的规模）
//     ③ 合   把两个已经有序的段**并**成一个有序段
//
// 基准情形：段里只有 0 个或 1 个数 —— 那它本来就是有序的，什么都不用做。
//
//     static void 归并排序(int[] 数, int 左, int 右) {
//         if (左 >= 右) return;                   ← 基准情形：一个数（或没有）已经有序
//         int 中 = 左 + (右 - 左) / 2;
//         归并排序(数, 左, 中);                     ← 排好左半
//         归并排序(数, 中 + 1, 右);                 ← 排好右半
//         归并(数, 左, 中, 右);                     ← 把两半并起来
//     }
//
// 注意 ③ 的前提：**两半必须已经有序**。所以 归并 只能在两个递归调用之后。
//
// ─────────────────────────────────────────────────────
//  二、「合」这一步：为什么它只要 O(n)
// ─────────────────────────────────────────────────────
//
// 把 {1, 4, 7} 和 {2, 3, 9} 并成一段。关键在于**两个段都已经有序**，
// 所以每次只要看两段的**开头**：
//
//     1 vs 2   → 取 1     左段剩下 {4, 7}
//     4 vs 2   → 取 2     右段剩下 {3, 9}
//     4 vs 3   → 取 3     右段剩下 {9}
//     4 vs 9   → 取 4
//     7 vs 9   → 取 7
//     左段空了 → 把右段剩下的 9 直接搬过来
//
// 每一步都确定地取走一个数 ⇒ 总共 n 步 ⇒ **O(n)**。
//
// ⚠ 实现上最容易错的一处：**不能原地并**（直接把结果写回 数 里），
//   因为写回去会覆盖掉还没比较的元素。标准做法是**先搬到临时数组**，再写回来：
//
//     int[] 临时 = new int[右 - 左 + 1];
//     int i = 左, j = 中 + 1, k = 0;
//     while (i <= 中 && j <= 右) {
//         临时[k++] = (数[i] <= 数[j]) ? 数[i++] : 数[j++];     ← ⚠ <= 才能保稳定
//     }
//     while (i <= 中) 临时[k++] = 数[i++];                     ← 左边剩下的一股脑搬
//     while (j <= 右) 临时[k++] = 数[j++];                     ← 右边剩下的
//     for (int t = 0; t < 临时.length; t++) 数[左 + t] = 临时[t];   ← 写回原数组
//
// ⚠ 那个 `<=` 不是随便写的：**相等时先取左边的**，归并排序才是**稳定**的
//   （第 19 课讲过稳定性 —— 归并是"稳定的 O(n log n)"，这也是它的一个卖点）。
//
// ─────────────────────────────────────────────────────
//  三、为什么是 O(n log n)
// ─────────────────────────────────────────────────────
//
// 每一层递归都要处理 n 个数（合并的总量），而"每次劈一半"能劈 log n 层：
//
//     n = 8：劈成 4+4 → 2+2+2+2 → 1×8          一共 3 层 = log₂8
//
// 每层 O(n)，一共 O(log n) 层 ⇒ **O(n log n)**。
//
// 对比一下（n = 10 万）：
//
//     O(n²)        100 亿次       几十秒
//     O(n log n)   170 万次       几毫秒
//
// ⚠ 代价是**额外的内存**：那个临时数组。归并排序不是"原地"的。
//   快排（第 30 课）在原地做，所以工程里常常更快 —— 两者各有取舍。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：以为 Arrays.sort 和手写没关系
// ─────────────────────────────────────────────────────
//
//     static void 归并排序(int[] 数, int 左, int 右) {
//         Arrays.sort(数);                       ← 输出完全正确，一行搞定
//     }
//
// 你会看到：**每一组数据都排对了**，而且比手写还快。
//
// 为什么这一课不许用它：`Arrays.sort` 用的**正是这一课要讲的东西** ——
//   对象数组走归并的变体（TimSort），基本类型走双轴快排。
//   把它当黑盒调一下，"分治为什么能到 O(n log n)"你就永远只能背结论。
//
// ⇒ 手写。而且 `归并排序` 里**必须真的自己调用自己** —— 检查会切开方法体看。
//
//   （真实项目里当然用 Arrays.sort。这一课是把它拆开看里面。）
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
//   快排（另一种 O(n log n)，但原地、不稳定）→ 第 30 课。
//   二分（也是"每次砍一半"，但它砍的是查找范围）→ 第 26 课。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 你**只写方法**：不用写 class，不用写 main；
//    `java.util.Arrays` 和 `java.util.Random` 已经 import 好了。
//    每个方法都要带 **static**。
//
//   1. static void 归并(int[] 数, int 左, int 中, int 右)
//        把 数[左..中] 和 数[中+1..右] 两个**已排好的段**并成一个有序段
//        （结果留在 数 里）。⚠ 要借一个临时数组，不能直接原地并。
//
//   2. static void 归并排序(int[] 数, int 左, int 右)
//        分治：左 >= 右 就是基准情形（一个数已经有序）；
//        否则劈成两半、各自递归排好、再 归并 起来。
//        ⚠ 必须真的自己调用自己。
//
//   3. static int 分治最大(int[] 数, int 左, int 右)
//        用分治求 数[左..右] 里最大的那个数（空区间返回 Integer.MIN_VALUE）。
//
//  ⚠ 三个都**不许**用 Arrays.sort。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
//   ⚠ 检查里会排 10 万个随机数。手写归并是几百毫秒的事；
//     如果超过一分钟，那多半是递归没写对（或者里面藏着 O(n²) 的写法）。
//
// ── 在下面写 ──
    static void 归并(int[] 数, int 左, int 中, int 右) {
    }

    static void 归并排序(int[] 数, int 左, int 右) {
    }

    static int 分治最大(int[] 数, int 左, int 右) {
        return 0;
    }
""";

    public static string 参考答案 => """
// 参考答案 —— 分治与归并排序（只写方法，不用写 class / main / import）

// 1. 合并两个已经有序的段：每次只看两个段的开头，谁小取谁
//    ⚠ 不能原地并 —— 结果要先进临时数组，最后再写回原数组
static void 归并(int[] 数, int 左, int 中, int 右) {
    int[] 临时 = new int[右 - 左 + 1];
    int i = 左;
    int j = 中 + 1;
    int k = 0;
    while (i <= 中 && j <= 右) {
        /* ⚠ 相等时取左边的（<=），归并排序才是稳定的 */
        if (数[i] <= 数[j]) {
            临时[k] = 数[i];
            i = i + 1;
        } else {
            临时[k] = 数[j];
            j = j + 1;
        }
        k = k + 1;
    }
    while (i <= 中) {                     /* 左段还剩的（它们本来就都比右边剩的大） */
        临时[k] = 数[i];
        i = i + 1;
        k = k + 1;
    }
    while (j <= 右) {                     /* 右段还剩的 */
        临时[k] = 数[j];
        j = j + 1;
        k = k + 1;
    }
    for (int t = 0; t < 临时.length; t++) {   /* 写回原数组的对应位置 */
        数[左 + t] = 临时[t];
    }
}

// 2. 分治：分 → 治 → 合
static void 归并排序(int[] 数, int 左, int 右) {
    if (左 >= 右) {
        return;                           /* 基准情形：0 个或 1 个数，已经有序 */
    }
    int 中 = 左 + (右 - 左) / 2;            /* 这样写不会溢出（第 26 课讲过） */
    归并排序(数, 左, 中);                    /* 治左半 */
    归并排序(数, 中 + 1, 右);                /* 治右半 */
    归并(数, 左, 中, 右);                    /* ⚠ 合必须在两个递归**之后** —— 两半得先有序 */
}

// 3. 分治求最大：也是「分两半、各求一个、取大的」
static int 分治最大(int[] 数, int 左, int 右) {
    if (左 > 右) {
        return Integer.MIN_VALUE;         /* 空区间：返回最小的 int，当"没有"用 */
    }
    if (左 == 右) {
        return 数[左];                     /* 基准情形：一个数，它就是最大的 */
    }
    int 中 = 左 + (右 - 左) / 2;
    int 左大 = 分治最大(数, 左, 中);
    int 右大 = 分治最大(数, 中 + 1, 右);
    return 左大 > 右大 ? 左大 : 右大;
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("三个方法都在，签名也对")]
    public static JavaCheckResult 检查_函数都在(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        if (!Regex.IsMatch(净, @"static\s+void\s+归并\s*\(\s*int\s*\[\s*\]\s*\w+\s*,\s*int\s+\w+\s*,\s*int\s+\w+\s*,\s*int\s+\w+\s*\)"))
            缺.Add("      归并　　（static void 归并(int[] 数, int 左, int 中, int 右)）");
        if (!Regex.IsMatch(净, @"static\s+void\s+归并排序\s*\(\s*int\s*\[\s*\]\s*\w+\s*,\s*int\s+\w+\s*,\s*int\s+\w+\s*\)"))
            缺.Add("      归并排序（static void 归并排序(int[] 数, int 左, int 右)）");
        if (!Regex.IsMatch(净, @"static\s+int\s+分治最大\s*\(\s*int\s*\[\s*\]\s*\w+\s*,\s*int\s+\w+\s*,\s*int\s+\w+\s*\)"))
            缺.Add("      分治最大（static int 分治最大(int[] 数, int 左, int 右)）");

        if (缺.Count == 0) return JavaCheckResult.过("三个方法都在，签名也对");
        return JavaCheckResult.不过(
            "有方法没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      ⚠ 都是「数组 + 左 + 右」这种形状（归并 多一个 中）。");
    }

    [Java检查说明("★ 不许用 Arrays.sort（这一课要拆开看它内部）")]
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
            "      Arrays.sort 排得又快又好 —— 但它用的**正是这一课要讲的东西**：\n" +
            "        对象数组 → 归并的变体（TimSort）\n" +
            "        基本类型 → 双轴快排\n" +
            "      把它当黑盒调一下，「分治为什么能到 O(n log n)」就永远只能背结论。\n" +
            "      ⇒ 手写「分 → 治 → 合」三步：\n" +
            "          分   int 中 = 左 + (右 - 左) / 2;\n" +
            "          治   归并排序(数, 左, 中);  归并排序(数, 中 + 1, 右);\n" +
            "          合   归并(数, 左, 中, 右);\n" +
            "      （真实项目里当然用 Arrays.sort。这一课是把它拆开看。）");
    }

    [Java检查说明("★ 归并排序真的递归（方法体里自己调用自己）")]
    public static JavaCheckResult 检查_真的递归(string 代码)
    {
        string 净 = 去掉注释(代码);
        string 体 = 函数体(净, "归并排序");
        if (体 == null)
            return JavaCheckResult.不过("没找到 归并排序 的函数体 —— 先确认这个方法写全了（花括号配对）。");

        if (!体.Contains("归并排序("))
            return JavaCheckResult.不过(
                "归并排序 的函数体里**没有调用自己** —— 它不是分治，是别的东西。\n" +
                "      分治的三步都要写出来：\n" +
                "          if (左 >= 右) return;              // 基准情形\n" +
                "          int 中 = 左 + (右 - 左) / 2;        // ① 分\n" +
                "          归并排序(数, 左, 中);                // ② 治左半\n" +
                "          归并排序(数, 中 + 1, 右);            // ② 治右半\n" +
                "          归并(数, 左, 中, 右);                // ③ 合\n" +
                "      ⚠ 如果你写成了「调用一次 归并 就返回」，或者在里面写了冒泡，\n" +
                "        输出可能对，但分治一点都没学到。");

        if (!体.Contains("if"))
            return JavaCheckResult.不过(
                "归并排序 里没有基准情形。\n" +
                "      递归必须有出口，这里的出口是「段里只剩 0 个或 1 个数」：\n" +
                "          if (左 >= 右) { return; }\n" +
                "      少了它，中 会一直算下去，最后报 java.lang.StackOverflowError。");

        return JavaCheckResult.过("归并排序 是分治递归：有基准情形、也调用了自己");
    }

    [Java检查说明("★ 十万个随机数排得完（冒泡冒充会超时）")]
    public static JavaCheckResult 检查_十万个数(string 代码)
    {
        var run = 跑一次(代码);
        if (run.Raw.Error != null && run.Raw.Error.Contains("超时"))
            return JavaCheckResult.不过(
                "排十万个数的时候**卡住了**（超时）。\n" +
                "      如果里面藏着 O(n²) 的写法（冒泡、选择、插入），十万个数是 100 亿次比较 ——\n" +
                "      几十秒到几分钟，必然撞超时。\n" +
                "      归并排序是 **O(n log n)**：大约 170 万次，几百毫秒。\n" +
                "      ⇒ 检查一下 `归并排序` 是不是真的「分两半 → 各自递归 → 合并」：\n" +
                "          分   中 = 左 + (右 - 左) / 2\n" +
                "          治   归并排序(数, 左, 中);  归并排序(数, 中 + 1, 右);\n" +
                "          合   归并(数, 左, 中, 右);");
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("十万有序|", "true", "十万个固定种子的随机数：排完之后必须是严格不下降的（有序）"),
            ("十万元素|", "100000", "一个都不能少（排完长度不变）"),
            ("十万最大|", "true", "排完之后最后一个 ≥ 第 0 个（首尾这一对也要有序）"),
            ("十万对照|", "true", "把排好的结果和「用 Arrays.sort 排的副本」逐个比 —— 应当完全一样"));
    }

    [Java检查说明("各种数据都排对（空、单个、重复、已排好、倒序、负数）")]
    public static JavaCheckResult 检查_各种数据(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("空|", "[]", "归并排序({}, 0, -1)　0 个元素：一个都不排"),
            ("空一|", "[]", "归并排序({}, 0, 0)　⚠ 左 = 0、右 = -1 时也是基准情形（左 >= 右）"),
            ("单|", "[42]", "归并排序({42}, 0, 0)　基准情形"),
            ("普通|", "[1, 2, 4, 5]", "归并排序({5,1,4,2})"),
            ("重复|", "[1, 3, 3, 7, 7]", "含重复元素"),
            ("已排|", "[1, 2, 3, 4, 5]", "已经排好的"),
            ("倒序|", "[-3, -1, 2, 6, 9]", "含负数"),
            ("负全|", "[-40, -9, -8, -7, -6, -5, -2, -1]", "全是负数"));
    }

    [Java检查说明("分治最大：分两半各求一个再取大的（含边界）")]
    public static JavaCheckResult 检查_分治最大(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("最大单|", "42", "分治最大({42}, 0, 0)"),
            ("最大普通|", "5", "分治最大({5,1,4,2}, 0, 3)"),
            ("最大负|", "-1", "⚠ 分治最大({-9,-8,-7,-1}, 0, 3)　全是负数时最大的是 -1 —— 初值写 0 的话这里会错成 0"),
            ("最大段|", "4", "分治最大({5,1,4,2}, 1, 3)　只看下标 1..3 这一段 ⇒ 4"),
            ("最大空|", "-2147483648", "分治最大({1,2}, 0, -1)　空区间 ⇒ Integer.MIN_VALUE"));
    }

    // ══════════════════ 工具 ══════════════════

    private const string 调用点 = """
        int[] 甲 = {5, 1, 4, 2};
        归并排序(甲, 0, 甲.length - 1);
        System.out.println("普通|" + Arrays.toString(甲));

        int[] 乙 = {7, 3, 7, 1, 3};
        归并排序(乙, 0, 乙.length - 1);
        System.out.println("重复|" + Arrays.toString(乙));

        int[] 丙 = {1, 2, 3, 4, 5};
        归并排序(丙, 0, 丙.length - 1);
        System.out.println("已排|" + Arrays.toString(丙));

        int[] 丁 = {9, 6, 2, -1, -3};
        归并排序(丁, 0, 丁.length - 1);
        System.out.println("倒序|" + Arrays.toString(丁));

        int[] 戊 = {-5, -40, -1, -9, -8, -7, -6, -2};
        归并排序(戊, 0, 戊.length - 1);
        System.out.println("负全|" + Arrays.toString(戊));

        int[] 空 = {};
        归并排序(空, 0, -1);
        System.out.println("空|" + Arrays.toString(空));
        归并排序(空, 0, 0);
        System.out.println("空一|" + Arrays.toString(空));

        int[] 单 = {42};
        归并排序(单, 0, 0);
        System.out.println("单|" + Arrays.toString(单));

        System.out.println("最大单|" + 分治最大(单, 0, 0));
        System.out.println("最大普通|" + 分治最大(new int[]{5, 1, 4, 2}, 0, 3));
        System.out.println("最大负|" + 分治最大(new int[]{-9, -8, -7, -1}, 0, 3));
        System.out.println("最大段|" + 分治最大(new int[]{5, 1, 4, 2}, 1, 3));
        System.out.println("最大空|" + 分治最大(new int[]{1, 2}, 0, -1));

        Random 随机 = new Random(12345);
        int[] 大 = new int[100000];
        for (int i = 0; i < 大.length; i++) {
            大[i] = 随机.nextInt(100000);
        }
        归并排序(大, 0, 大.length - 1);
        boolean 有序 = true;
        for (int i = 1; i < 大.length; i++) {
            if (大[i - 1] > 大[i]) 有序 = false;
        }
        System.out.println("十万有序|" + 有序);
        System.out.println("十万元素|" + 大.length);
        System.out.println("十万最大|" + (大.length == 0 || 大[大.length - 1] >= 大[0]));
        Random 随机2 = new Random(12345);
        int[] 对照 = new int[100000];
        for (int i = 0; i < 对照.length; i++) {
            对照[i] = 随机2.nextInt(100000);
        }
        Arrays.sort(对照);                    /* 调用点自己用库排一份，拿它当标准答案 */
        boolean 一致 = true;
        for (int i = 0; i < 对照.length; i++) {
            if (大[i] != 对照[i]) 一致 = false;
        }
        System.out.println("十万对照|" + 一致);
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
                return "**栈溢出**了（StackOverflowError）—— 递归没有出口。\n" +
                       "      归并排序的基准情形是「段里只剩 0 个或 1 个数」：\n" +
                       "          if (左 >= 右) { return; }\n" +
                       "      少了它，中 会一直算下去，永远到不了底。";
            if (run.Raw.Error.Contains("ArrayIndexOutOfBounds"))
                return "下标越界了：\n      " + run.Raw.Error + "\n" +
                       "      这一课常见的越界：\n" +
                       "        · 中 算错（中 应该是 左 + (右 - 左) / 2，不是 数.length / 2）\n" +
                       "        · 临时数组开小了（要 右 - 左 + 1 格）\n" +
                       "        · 写回时用 临时[t] 对 数[t]（应该对 数[左 + t]）";
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
            "      合并那一步（最容易错的地方）：\n" +
            "          int[] 临时 = new int[右 - 左 + 1];     ← 格数要够\n" +
            "          int i = 左, j = 中 + 1, k = 0;\n" +
            "          while (i <= 中 && j <= 右) 临时[k++] = (数[i] <= 数[j]) ? 数[i++] : 数[j++];\n" +
            "          while (i <= 中) 临时[k++] = 数[i++];\n" +
            "          while (j <= 右) 临时[k++] = 数[j++];\n" +
            "          for (int t = 0; t < 临时.length; t++) 数[左 + t] = 临时[t];   ← ⚠ 是 左+t\n" +
            "      ⚠ 还有一处：归并 必须在两个递归**之后**（两半得先有序才能并）。");
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
