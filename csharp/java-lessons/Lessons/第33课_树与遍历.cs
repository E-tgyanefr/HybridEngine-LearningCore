// 第 33 课：树与遍历 —— 分叉的结构，三种走法
//
// ══ 这一课要教什么 ══
// 链表每个结点最多牵一个"下一个"；**树**每个结点可以牵两个（甚至更多）。
// 「每个结点最多两个孩子」的那种叫**二叉树**，它是数据结构里出场率最高的一棵树：
// 表达式、文件目录、排序树（下一课）、堆、决策树……都是它的变体。
//
// 这一课要建立两件事：
//   ① 二叉树的**递归定义** —— 「一棵树 = 一个根 + 一棵左子树 + 一棵右子树」。
//      这个定义直接翻译成代码，就是这一课所有方法的写法。
//   ② **三种遍历**（前序/中序/后序）的差别只在"什么时候处理根"：
//         前序  根 → 左 → 右        （先办事，再往下走）
//         中序  左 → 根 → 右        （走完左边回来才办事）
//         后序  左 → 右 → 根        （两边都办完才轮到自己）
//      三份代码的**顺序完全一样**，只是那行 println 的位置挪了挪。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点一：**三种遍历都必须自己调用自己**。
//   这是本课唯一的技能点。学生完全可以写 `System.out.println("1 2 4 5 3")`
//   （甚至用循环把一棵树扫成一行），所以去掉注释后切开三个方法体，
//   各自必须出现自己的名字。
// 核心检查点二：**换一棵形状不同的树，结果必须跟着变**。
//   调用点里喂**五棵**形状完全不同的树：满树、只有右孩子的链、
//   只有左孩子的链、单个结点、**空树（null）**。
//   写死输出的写法在这五棵上必然崩或者全错。
//   空树那一棵还顺带抓「忘了判 根 == null」的写法（会 NPE）。
// 第三条：`结点个数` 和 `树高` 也用递归（它们是"树形递归"最直白的两个例子，
//   而且 `树高` 里那个「空树高度是 0」的边界很容易写成 -1）。
//
// ⚠ 学生只写 `static class 结点` + 五个方法。

using System.Collections.Generic;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("33", "第 33 课：树与遍历 —— 分叉的结构，三种走法",
    "1. 写 static class 结点：字段 int 值; 结点 左; 结点 右;（构造方法 结点(int 值)）\n" +
    "2. 写 static void 前序(结点 根)：根 → 左 → 右，每个数后面跟一个空格\n" +
    "3. 写 static void 中序(结点 根)：左 → 根 → 右\n" +
    "4. 写 static void 后序(结点 根)：左 → 右 → 根\n" +
    "5. 写 static int 结点个数(结点 根) 和 static int 树高(结点 根)（空树是 0）\n" +
    "6. 三种遍历都要**自己调用自己**，而且要能扛住空树（根 == null）")]
public static class 第33课
{
    private const string 骨架 = """
public class Main {
/*学生代码*/

    public static void main(String[] args) {
/*调用点*/
    }
}
""";

    public static string 模板 => """
// 第 33 课：树与遍历 —— 分叉的结构，三种走法
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**树的定义本身就是递归的**，所以树的代码也全是递归。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    二叉树；前序 / 中序 / 后序遍历
//
//  需要先会
//    第 32 课：链表（结点里放"下一个"的引用 —— 树只是放了两个）
//    第 28 课：递归
//
// ─────────────────────────────────────────────────────
//  先介绍：从"一条线"到"分叉"
// ─────────────────────────────────────────────────────
//
// 链表是一条线：每个结点牵一个"下一个"。
// 树则是**分叉**的：每个结点可以牵两个（左、右），而那两个又各自分叉。
//
//            1            ← 根
//          /   \
//         2     3         ← 1 的左孩子、右孩子
//        / \
//       4   5             ← 2 的左孩子、右孩子
//
// 规矩只有一条：**每个结点最多两个孩子**（左、右），
// 而且孩子下面还能再有孩子。没孩子的地方就是 **null**（和链表的结尾一样）。
//
// ⚠ 二叉树最重要的性质是它的**定义本身**：
//
//     一棵树 = 一个根 + 一棵左子树 + 一棵右子树
//
// 注意右边那两项 —— **它们也是树**。这句话一念出来，
// 这一课的所有代码就都写完了：因为"处理一棵树"就是
// "处理根 + 处理左子树 + 处理右子树"，而后面两项又是同一个问题。
//
// ─────────────────────────────────────────────────────
//  一、结点类：比链表多一个字段
// ─────────────────────────────────────────────────────
//
//     static class 结点 {
//         int 值;
//         结点 左;                    ← 和第 32 课的 `Node 下;` 一模一样的套路
//         结点 右;
//         结点(int 值) { this.值 = 值; }
//     }
//
// 手工搭出上面那棵树：
//
//     结点 根 = new 结点(1);
//     根.左 = new 结点(2);
//     根.右 = new 结点(3);
//     根.左.左 = new 结点(4);
//     根.左.右 = new 结点(5);
//
// ─────────────────────────────────────────────────────
//  二、三种遍历：差别只在"什么时候处理根"
// ─────────────────────────────────────────────────────
//
//     **前序**（根在最前）      根 → 左 → 右
//     **中序**（根在中间）      左 → 根 → 右
//     **后序**（根在最后）      左 → 右 → 根
//
// 三份代码的**行数一样、顺序一样**，只是"处理根"那一行的位置不同：
//
//     static void 前序(结点 根) {
//         if (根 == null) return;              ← ⚠ 基准情形：空树什么都不做
//         System.out.print(根.值 + " ");        ← 根在这里
//         前序(根.左);
//         前序(根.右);
//     }
//
//     static void 中序(结点 根) {
//         if (根 == null) return;
//         中序(根.左);
//         System.out.print(根.值 + " ");        ← 根挪到了中间
//         中序(根.右);
//     }
//
//     static void 后序(结点 根) {
//         if (根 == null) return;
//         后序(根.左);
//         后序(根.右);
//         System.out.print(根.值 + " ");        ← 根挪到了最后
//     }
//
// ⚠ 注意 `if (根 == null) return;` 这一行在三份里都有，而且**必须是第一行**。
//   树里到处是 null（叶子结点的孩子），少这一行，第一步就 NPE。
//
// 拿上面那棵树走一遍（自己纸上画一下，比看十遍有用）：
//
//     前序  1 2 4 5 3
//     中序  4 2 5 1 3
//     后序  4 5 2 3 1
//
// ─────────────────────────────────────────────────────
//  三、另外两个"树形递归"：数结点、算高度
// ─────────────────────────────────────────────────────
//
//     static int 结点个数(结点 根) {
//         if (根 == null) return 0;                       ← 空树 0 个
//         return 1 + 结点个数(根.左) + 结点个数(根.右);      ← 自己 + 左子树 + 右子树
//     }
//
//     static int 树高(结点 根) {
//         if (根 == null) return 0;                       ← ⚠ 空树高 0（不是 -1）
//         int 左高 = 树高(根.左);
//         int 右高 = 树高(根.右);
//         return 1 + (左高 > 右高 ? 左高 : 右高);           ← 1 + 两边里更高的那个
//     }
//
// ⚠ 树高那一句「空树高 0」要记牢：单个结点的树高是 **1**。
//   写成 `return -1` 的话，单个结点的树高会算成 0 —— 差一位，后面全错。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：三种遍历写成一样的
// ─────────────────────────────────────────────────────
//
//     static void 前序(结点 根) { if (根 == null) return; System.out.print(根.值 + " "); 前序(根.左); 前序(根.右); }
//     static void 中序(结点 根) { if (根 == null) return; System.out.print(根.值 + " "); 中序(根.左); 中序(根.右); }   ← 抄漏了
//     static void 后序(结点 根) { if (根 == null) return; System.out.print(根.值 + " "); 后序(根.左); 后序(根.右); }   ← 也抄漏了
//
// 你会看到：**三行输出一模一样**（都是前序），而且**编译器一句话都不说** ——
//   三个方法的代码各自都是合法的，只是"根"的位置没挪。
//
// 为什么危险：中序是三种里最有用的一种 ——
//   **二叉排序树的中序遍历正好是"从小到大排好序"**（下一课会用这一点）。
//   把中序写成了前序，排序树就白建了，而且不会报任何错。
//
// ⇒ 写完一个，**别复制粘贴**：把"处理根"那一行在三个方法里挪到不同位置，
//   然后拿一棵三层的树手工验一遍（前序第一个是根、后序最后一个是根、中序根在中间）。
//
//   附一条树代码最常见的运行期错（忘了判 null）：
//
//       Exception in thread "main" java.lang.NullPointerException:
//           Cannot read field "值" because "根" is null
//
//   ⇒ `if (根 == null) return;` 永远是递归的第一行。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
//   二叉排序树（插入/查找）→ 第 34 课。
//   非递归遍历（用栈）→ 不涉及。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 你**只写一个类 + 五个方法**：不用写 class Main、不用写 main、不用 import。
//
//   1. static class 结点
//        字段：`int 值;`、`结点 左;`、`结点 右;`
//        构造方法：`结点(int 值) { this.值 = 值; }`
//
//   2. static void 前序(结点 根)     根 → 左 → 右
//   3. static void 中序(结点 根)     左 → 根 → 右
//   4. static void 后序(结点 根)     左 → 右 → 根
//        ⚠ 三个都**打印**，每个数后面跟**一个空格**（用 System.out.print，不是 println）。
//        ⚠ 三个都必须在方法体里**自己调用自己**（检查会切开看）。
//
//   5. static int 结点个数(结点 根)  空树是 0
//   6. static int 树高(结点 根)      空树是 0，单个结点的树高是 1
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
//   ⚠ 检查会拿**五棵形状完全不同的树**（含一棵空树）分别走一遍，
//     所以把答案写死是没用的。
//
// ── 在下面写 ──
    static class 结点 {
        int 值;
        结点 左;
        结点 右;
    }

    static void 前序(结点 根) {
    }

    static void 中序(结点 根) {
    }

    static void 后序(结点 根) {
    }

    static int 结点个数(结点 根) {
        return 0;
    }

    static int 树高(结点 根) {
        return 0;
    }
""";

    public static string 参考答案 => """
// 参考答案 —— 二叉树与遍历（只写一个类 + 五个方法）
//
// 三份遍历的代码**行数一样、顺序一样**，唯一不同的是
// 「处理根」那一行（System.out.print）站在第几行。

static class 结点 {
    int 值;
    结点 左;
    结点 右;

    结点(int 值) {
        this.值 = 值;
    }
}

// 前序：根 → 左 → 右
static void 前序(结点 根) {
    if (根 == null) {
        return;                        /* ⚠ 基准情形必须是第一行 —— 树里到处是 null */
    }
    System.out.print(根.值 + " ");      /* 根在最前面 */
    前序(根.左);
    前序(根.右);
}

// 中序：左 → 根 → 右
// ⚠ 这一种最有用：二叉排序树的中序遍历 = 从小到大的顺序（下一课用）
static void 中序(结点 根) {
    if (根 == null) {
        return;
    }
    中序(根.左);
    System.out.print(根.值 + " ");      /* 根在中间 */
    中序(根.右);
}

// 后序：左 → 右 → 根
static void 后序(结点 根) {
    if (根 == null) {
        return;
    }
    后序(根.左);
    后序(根.右);
    System.out.print(根.值 + " ");      /* 根在最后 */
}

// 数结点：自己 + 左子树的 + 右子树的
static int 结点个数(结点 根) {
    if (根 == null) {
        return 0;
    }
    return 1 + 结点个数(根.左) + 结点个数(根.右);
}

// 树高：1 + 两边里更高的那个
static int 树高(结点 根) {
    if (根 == null) {
        return 0;                      /* ⚠ 空树高 0 —— 不是 -1 */
    }
    int 左高 = 树高(根.左);
    int 右高 = 树高(根.右);
    return 1 + (左高 > 右高 ? 左高 : 右高);
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("结点类是自己写的（有 左 / 右 两个字段）")]
    public static JavaCheckResult 检查_结点类(string 代码)
    {
        string 净 = 去掉注释(代码);
        if (!Regex.IsMatch(净, @"class\s+结点"))
            return JavaCheckResult.不过(
                "没找到 `class 结点` —— 这一课要自己写结点类。\n" +
                "          static class 结点 {\n" +
                "              int 值;\n" +
                "              结点 左;                  // 和第 32 课的 Node 下; 是同一个套路\n" +
                "              结点 右;\n" +
                "              结点(int 值) { this.值 = 值; }\n" +
                "          }");
        if (!净.Contains("结点 左") || !净.Contains("结点 右"))
            return JavaCheckResult.不过(
                "结点 里没看到 `结点 左;` 和 `结点 右;` 两个字段。\n" +
                "      「二叉树」的意思就是**每个结点最多两个孩子**：左孩子和右孩子。\n" +
                "      两个字段都要有 —— 只有一个就变成链表了（上一课）。");
        return JavaCheckResult.过("结点是自己写的，左 / 右 都在");
    }

    [Java检查说明("五个方法都在，签名也对")]
    public static JavaCheckResult 检查_函数都在(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        foreach (var 名 in new[] { "前序", "中序", "后序" })
            if (!Regex.IsMatch(净, @"static\s+void\s+" + 名 + @"\s*\(\s*结点\s+\w+\s*\)"))
                缺.Add("      " + 名 + "　（static void " + 名 + "(结点 根)）");
        if (!Regex.IsMatch(净, @"static\s+int\s+结点个数\s*\(\s*结点\s+\w+\s*\)"))
            缺.Add("      结点个数（static int 结点个数(结点 根)）");
        if (!Regex.IsMatch(净, @"static\s+int\s+树高\s*\(\s*结点\s+\w+\s*\)"))
            缺.Add("      树高　　（static int 树高(结点 根)）");

        if (缺.Count == 0) return JavaCheckResult.过("五个方法都在，签名也对");
        return JavaCheckResult.不过(
            "有方法没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      ⚠ 三个遍历的返回类型是 **void**（它们只负责打印），参数是 结点。");
    }

    [Java检查说明("★ 三种遍历各自都自己调用自己（不是写死一串输出）")]
    public static JavaCheckResult 检查_真的递归(string 代码)
    {
        // ⚠ 核心检查点之一。
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        foreach (var 名 in new[] { "前序", "中序", "后序" })
        {
            string 体 = 函数体(净, 名);
            if (体 == null) { 缺.Add("      " + 名 + "：没找到函数体（花括号配对了吗？）"); continue; }
            if (!体.Contains(名 + "(")) 缺.Add("      " + 名 + "：方法体里没有调用自己");
            if (!体.Contains("null")) 缺.Add("      " + 名 + "：没有判空（`if (根 == null) return;`）");
        }
        if (缺.Count == 0)
            return JavaCheckResult.过("三个遍历都是递归，而且都判了 null");

        return JavaCheckResult.不过(
            "检查了去掉注释后的源码，发现问题：\n" + string.Join("\n", 缺) + "\n" +
            "      三种遍历的骨架**完全一样**，只有「处理根」那一行的位置不同：\n" +
            "          前序   if (根 == null) return;  print(根);  前序(根.左);  前序(根.右);\n" +
            "          中序   if (根 == null) return;  中序(根.左);  print(根);  中序(根.右);\n" +
            "          后序   if (根 == null) return;  后序(根.左);  后序(根.右);  print(根);\n" +
            "      ⚠ `if (根 == null) return;` 必须是每个方法的第一行 ——\n" +
            "        树里到处是 null（叶子结点的孩子），少这一行第一步就 NPE。");
    }

    [Java检查说明("★ 五种形状的树都走对（含空树、只有左孩子的链、只有右孩子的链）")]
    public static JavaCheckResult 检查_五种形状(string 代码)
    {
        // ⚠ 核心检查点之二：五棵形状完全不同的树，写死输出的必然全错。
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);

        var 错 = new List<string>();
        var 期望 = new (string 名, string 值)[]
        {
            ("甲前|", "1 2 4 5 3 "),
            ("甲中|", "4 2 5 1 3 "),
            ("甲后|", "4 5 2 3 1 "),
            ("右前|", "1 2 3 4 "),
            ("右中|", "1 2 3 4 "),
            ("右后|", "4 3 2 1 "),
            ("左前|", "1 2 3 4 "),
            ("左中|", "4 3 2 1 "),
            ("左后|", "4 3 2 1 "),
            ("单前|", "7 "),
            ("单中|", "7 "),
            ("单后|", "7 "),
            ("空前|", ""),
            ("空中|", ""),
            ("空后|", ""),
        };
        foreach (var (名, 值) in 期望)
        {
            string 得到 = 看看Raw(run, 名);
            if (!一样(得到, 值)) 错.Add("      " + 名 + " 得到「" + 得到.Trim() + "」，应该是「" + 值.Trim() + "」");
        }
        if (错.Count == 0) return JavaCheckResult.过($"{期望.Length} 组遍历输出全对（五棵树）");

        return JavaCheckResult.不过(
            "遍历的输出不对：\n" + string.Join("\n", 错) + "\n" +
            "      对着第一棵树（根 1，左孩子 2，右孩子 3，2 又有左 4 右 5）自己走一遍：\n" +
            "          前序  根→左→右    1 2 4 5 3\n" +
            "          中序  左→根→右    4 2 5 1 3\n" +
            "          后序  左→右→根    4 5 2 3 1\n" +
            "      ⚠ 三种里最容易写混的是中序和后序 —— 它们的第一个数都是 4，\n" +
            "        差别在后面。拿纸画一遍树，标出走的顺序。\n" +
            "      ⚠ 只有左孩子（或只有右孩子）的那种树（退化成一条链）也算出来了：\n" +
            "        前序和后序在链上正好是**反的**。");
    }

    [Java检查说明("结点个数和树高（空树 0、单结点树高 1）")]
    public static JavaCheckResult 检查_个数和高度(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("甲个数|", "5", "结点个数(甲)　甲 有 1、2、3、4、5 五个结点"),
            ("甲高|", "3", "树高(甲)　1 → 2 → 4 是三层"),
            ("右个数|", "4", "结点个数(右链)"),
            ("右高|", "4", "树高(右链)　⚠ 退化成一条链，高度就是结点个数"),
            ("左高|", "4", "树高(左链)"),
            ("单个|", "1", "结点个数(单个结点)"),
            ("单高|", "1", "⚠ 树高(单个结点) 是 **1**，不是 0 —— 空树才是 0"),
            ("空个数|", "0", "结点个数(null)　空树 0 个"),
            ("空高|", "0", "树高(null)　空树高 0"));
    }

    // ══════════════════ 工具 ══════════════════

    private const string 调用点 = """
        结点 甲 = new 结点(1);
        甲.左 = new 结点(2);
        甲.右 = new 结点(3);
        甲.左.左 = new 结点(4);
        甲.左.右 = new 结点(5);

        结点 右链 = new 结点(1);
        右链.右 = new 结点(2);
        右链.右.右 = new 结点(3);
        右链.右.右.右 = new 结点(4);

        结点 左链 = new 结点(1);
        左链.左 = new 结点(2);
        左链.左.左 = new 结点(3);
        左链.左.左.左 = new 结点(4);

        结点 单个 = new 结点(7);

        System.out.print("甲前|"); 前序(甲); System.out.println();
        System.out.print("甲中|"); 中序(甲); System.out.println();
        System.out.print("甲后|"); 后序(甲); System.out.println();
        System.out.print("右前|"); 前序(右链); System.out.println();
        System.out.print("右中|"); 中序(右链); System.out.println();
        System.out.print("右后|"); 后序(右链); System.out.println();
        System.out.print("左前|"); 前序(左链); System.out.println();
        System.out.print("左中|"); 中序(左链); System.out.println();
        System.out.print("左后|"); 后序(左链); System.out.println();
        System.out.print("单前|"); 前序(单个); System.out.println();
        System.out.print("单中|"); 中序(单个); System.out.println();
        System.out.print("单后|"); 后序(单个); System.out.println();
        System.out.print("空前|"); 前序(null); System.out.println();
        System.out.print("空中|"); 中序(null); System.out.println();
        System.out.print("空后|"); 后序(null); System.out.println();

        System.out.println("甲个数|" + 结点个数(甲));
        System.out.println("甲高|" + 树高(甲));
        System.out.println("右个数|" + 结点个数(右链));
        System.out.println("右高|" + 树高(右链));
        System.out.println("左高|" + 树高(左链));
        System.out.println("单个|" + 结点个数(单个));
        System.out.println("单高|" + 树高(单个));
        System.out.println("空个数|" + 结点个数(null));
        System.out.println("空高|" + 树高(null));
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
            if (run.Raw.Error.Contains("NullPointer"))
                return "空指针了（NullPointerException）—— 树代码的第一号坑。\n" +
                       "      树里到处是 null（叶子结点的孩子都是 null），\n" +
                       "      **每个递归方法的第一行**都必须是：\n" +
                       "          if (根 == null) { return; }        // void 的\n" +
                       "          if (根 == null) { return 0; }      // 返回 int 的\n" +
                       "      少了它，走到叶子结点以后再往下走一步就崩。";
            if (run.Raw.Error.Contains("StackOverflow"))
                return "栈溢出（StackOverflowError）—— 递归没有出口。\n" +
                       "      检查一下 `if (根 == null) return;` 是不是每个遍历方法的第一行。";
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
            "      两条容易错的：\n" +
            "        · **树高**：空树是 0、单个结点是 1。写成 `return -1` 的话单结点会算成 0。\n" +
            "        · **结点个数**：`1 + 结点个数(左) + 结点个数(右)` —— 那个 1 是根自己。");
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

    /// <summary>和 看看 一样，但不 Trim —— 遍历输出要保留末尾那个空格的区别。</summary>
    private static string 看看Raw(JavaRun run, string 标记)
    {
        foreach (var 行 in run.行)
        {
            int 位 = 行.IndexOf(标记, System.StringComparison.Ordinal);
            if (位 >= 0) return 行.Substring(位 + 标记.Length);
        }
        return "";
    }

    /// <summary>把连续空白折成一个空格再比 —— 空格多一个少一个不该判错。</summary>
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
