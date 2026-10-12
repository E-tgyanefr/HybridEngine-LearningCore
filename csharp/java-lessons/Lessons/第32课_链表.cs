// 第 32 课：链表 —— 每个结点牵着下一个
//
// ══ 这一课要教什么 ══
// 数组（和 ArrayList）在内存里是**连续一整块**，好处是"按下标取"是 O(1)，
// 代价是"往中间插一个"要挪动后面所有人。
//
// 链表反过来：每个元素是一个**独立的结点**，结点里存着自己的值和一个
// 「下一个结点」的引用。插入只要改两根线，代价是"想找第 5 个"必须从头一个个走。
//
// 这一课要亲手写出这个结构，并且体会**改指针**这件事：
//   反转链表 —— 三十年来最经典的面试题之一，因为它是「你必须真的在脑子里画图」
//   的那一类：三个指针、四行代码、顺序错一步就断链。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点一：**必须自己写 Node 类**（`static class Node`），
//   而且不许用 `java.util.LinkedList` 冒充。去掉注释后看这两样。
// 核心检查点二：**反转必须真的改指针，不能只打印**。
//   抓法是**自动的**，不用额外检查代码：调用点里先量长度，再反转，
//   再**量一次长度、打印一次、再反转回来**。如果 `反转` 只是打印了一串倒序
//   （或者返回了一条新链表而没改原来的），后面几步立刻对不上。
//   实测：只打印不改指针的实现在「反转后长度」那一步就错。
// 第三条：`打印` 要能处理空链表（返回 `空`）和单结点 —— 遍历时最容易在这里 NPE。
//
// ⚠ 学生只写 `static class Node` + 三个方法；骨架已经 import java.util.*，
//   但**不允许**用 LinkedList。

using System.Collections.Generic;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("32", "第 32 课：链表 —— 每个结点牵着下一个",
    "1. 写 static class Node：字段 int 值; Node 下;（构造方法 Node(int 值)）\n" +
    "2. 写 static int 求长度(Node 头)：从头走到尾，数一数有几个结点\n" +
    "3. 写 static Node 反转(Node 头)：**就地**把指针反过来，返回新的头结点\n" +
    "4. 写 static String 打印(Node 头)：形如 1->2->3；空链表返回 空\n" +
    "5. ⚠ 不许用 java.util.LinkedList —— 这一课要自己写出结点和指针")]
public static class 第32课
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
// 第 32 课：链表 —— 每个结点牵着下一个
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**数据不一定要排成一整块**（链表），以及
// 「改指针」这件事为什么值得单独练一遍。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    单链表：结点、指针、反转
//
//  需要先会
//    第 08 课：类与对象（Node 就是一个类）
//    第 10 课：构造方法
//
// ─────────────────────────────────────────────────────
//  先介绍：数组的代价
// ─────────────────────────────────────────────────────
//
// 数组（和 ArrayList）在内存里是**连续一整块**：
//
//     [ 10 ][ 20 ][ 30 ][ 40 ][ 50 ]      ← 挨着放
//       0      1      2      3      4
//
// 好处：按下标取是 **O(1)**（`数[3]` 一算地址就到了）。
// 代价：**往中间插一个**，后面所有人都要往后挪一格 —— O(n)。
//
// 链表换个办法：每个元素是**独立的一个结点**，结点里存两样东西 ——
// 自己的值 + **下一个结点在哪里**：
//
//     头 → [10|•] → [20|•] → [30|•] → [40|•] → null
//
// 插入只要改两根线（O(1)，前提是你已经站在那个位置）；
// 代价是**想找第 5 个必须从头一个个走** —— O(n)。
//
// ⚠ 一句话记住这个取舍：**链表用"查找慢"换"插入删除快"。**
//
// ─────────────────────────────────────────────────────
//  一、结点类：一个类，两个字段
// ─────────────────────────────────────────────────────
//
//     static class Node {
//         int 值;
//         Node 下;                    ← ⚠ 类型是 Node 自己 —— 这叫"自引用"
//         Node(int 值) { this.值 = 值; }
//     }
//
// ⚠ 三处容易卡住：
//   · `Node 下;` 里那个 **Node 就是正在定义的这个类**。类可以引用自己
//     （就像方法可以调用自己 —— 第 28 课）。
//   · 字段没赋值时默认是 **null**，表示"后面没有了"。
//     链表的**结尾**就是某个结点的 下 == null。
//   · `this.值 = 值;` —— 字段和参数重名时必须写 this（第 10 课讲过）。
//
// 手工串一条 1 → 2 → 3：
//
//     Node 头 = new Node(1);
//     头.下 = new Node(2);
//     头.下.下 = new Node(3);
//
// 访问第 3 个要写 `头.下.下` —— 一眼就看出链表的"慢"在哪儿了：
// 除了从头一个个走，没有别的办法。
//
// ─────────────────────────────────────────────────────
//  二、遍历：走到 null 为止
// ─────────────────────────────────────────────────────
//
//     Node 当前 = 头;
//     while (当前 != null) {
//         处理 当前.值;
//         当前 = 当前.下;             ← ⚠ 别忘了往前走，否则死循环
//     }
//
// ⚠ 循环条件里**必须先问 null 再取字段**：
//   写成 `while (当前.下 != null)` 的话，空链表（头 == null）第一下就 NPE 了。
//
// 想拼接成 `1->2->3` 的话，每个结点前面的箭头要单独处理（第一个前面没有箭头）：
//
//     StringBuilder 串 = new StringBuilder();
//     while (当前 != null) {
//         if (串.length() > 0) 串.append("->");
//         串.append(当前.值);
//         当前 = 当前.下;
//     }
//
// ─────────────────────────────────────────────────────
//  三、反转：三个指针，四行代码
// ─────────────────────────────────────────────────────
//
// 要把 1→2→3→null 变成 3→2→1→null。**不新建结点，只改指针。**
//
// 难点在于：当你把 `2.下` 改成指向 1 的时候，**2 原来指向 3 的那根线就断了** ——
// 如果之前没把 3 记下来，后面那半条链就永远找不回来了。
//
// ⇒ 所以需要三个指针：
//
//     上一个   已经反好的那一段的头
//     当前     正在处理的结点
//     下一个   当前的后一个（**在改指针之前先存下来**）
//
//     static Node 反转(Node 头) {
//         Node 上一个 = null;
//         Node 当前 = 头;
//         while (当前 != null) {
//             Node 下一个 = 当前.下;      ← ⚠ 第一件事：先记住后面
//             当前.下 = 上一个;           ← 然后才断开、反向
//             上一个 = 当前;              ← 往前走：上一个 挪到 当前
//             当前 = 下一个;              ←          当前 挪到 下一个
//         }
//         return 上一个;                  ← ⚠ 返回 上一个，不是 当前（当前已经是 null 了）
//     }
//
// ⚠ 两个最容易错的地方：
//   · **顺序**：`Node 下一个 = 当前.下;` 必须是循环体第一句。
//     先改 `当前.下` 再存，就已经晚了。
//   · **返回值**：循环结束时 `当前` 是 null，`上一个` 才是新的头。
//
//   画一遍 1→2→3（自己拿张纸跟着走一遍，比看十遍代码有用）：
//
//     开局         上一个=null  当前=1
//     第 1 轮      下一个=2   1.下=null  上一个=1  当前=2
//     第 2 轮      下一个=3   2.下=1     上一个=2  当前=3
//     第 3 轮      下一个=null 3.下=2    上一个=3  当前=null
//     结束         返回 3  ⇒  3→2→1→null     ✓
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：只打印、没改指针
// ─────────────────────────────────────────────────────
//
//     static Node 反转(Node 头) {
//         if (头 == null) return null;
//         System.out.println(3);        ← 把值倒着打印出来，看着"像反转了"
//         return 头;                     ← 链表本身一动没动
//     }
//
// 你会看到：**输出的那几行数字完全正确** —— 如果检查只比对打印结果，它就骗过去了。
//
// 为什么这一定是错的：这一课要练的是**改指针**。
//   真正的反转要让「头」这个引用换成新的头，而且原来的结点之间的关系整个翻过来 ——
//   所以检查会做这件事：**反转之后再量一次长度、再打印一次、再反转回来**。
//   没改指针的实现在第二步就露馅了。
//
//   附一条这一课真会撞上的运行期错（`打印` 最容易犯）：
//
//       Exception in thread "main" java.lang.NullPointerException:
//           Cannot read field "值" because "当前" is null
//
//   ⇒ 循环条件写成 `while (当前 != null)`，别在进循环前就去取 `当前.值`。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
//   双向链表、循环链表 → 不涉及。
//   用链表做 LRU 缓存那种工程套路 → 不涉及。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 你**只写一个类 + 三个方法**：不用写 class Main、不用写 main。
//    `java.util.*` 已经 import 好了（但**不许**用 LinkedList）。
//
//   1. static class Node
//        字段：`int 值;`、`Node 下;`
//        构造方法：`Node(int 值) { this.值 = 值; }`
//
//   2. static int 求长度(Node 头)
//        从头走到尾，数一数有几个结点。空链表是 0。
//
//   3. static Node 反转(Node 头)
//        **就地**把指针反过来，返回**新的头**。空链表返回 null。
//
//   4. static String 打印(Node 头)
//        形如 `1->2->3`（没有空格）。空链表返回 `空`。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
//   ⚠ 检查会「反转 → 再量长度 → 再打印 → 再反转回来」，
//     所以只打印不改指针的写法会被抓住。
//
// ── 在下面写 ──
    static class Node {
        int 值;
        Node 下;
    }

    static int 求长度(Node 头) {
        return 0;
    }

    static Node 反转(Node 头) {
        return null;
    }

    static String 打印(Node 头) {
        return "";
    }
""";

    public static string 参考答案 => """
// 参考答案 —— 单链表（只写一个类 + 三个方法，不用写 class Main / main）

// 结点：自己的值 + 下一个结点在哪
// ⚠ `Node 下;` 里的 Node 就是这个类自己 —— 类可以引用自己
static class Node {
    int 值;
    Node 下;

    Node(int 值) {
        this.值 = 值;                      /* ⚠ 字段和参数重名，this 不能省 */
    }
}

// 求长度：走到 null 为止，走一步数一个
static int 求长度(Node 头) {
    int 个数 = 0;
    Node 当前 = 头;
    while (当前 != null) {
        个数 = 个数 + 1;
        当前 = 当前.下;                    /* ⚠ 别忘了往前走 */
    }
    return 个数;
}

// 反转：三个指针，循环体四句话，顺序不能换
static Node 反转(Node 头) {
    Node 上一个 = null;                    /* 已经反好的那一段的头 */
    Node 当前 = 头;
    while (当前 != null) {
        Node 下一个 = 当前.下;              /* ⚠ 第一句：先把后面记住，否则一会儿就断了 */
        当前.下 = 上一个;                   /* 断开、反向 */
        上一个 = 当前;                      /* 往前走两步 */
        当前 = 下一个;
    }
    return 上一个;                          /* ⚠ 返回 上一个 —— 循环结束时 当前 已经是 null */
}

// 打印：拼接成 1->2->3；箭头只在"不是第一个"的时候加
static String 打印(Node 头) {
    if (头 == null) {
        return "空";
    }
    StringBuilder 串 = new StringBuilder();
    Node 当前 = 头;
    while (当前 != null) {
        if (串.length() > 0) {
            串.append("->");
        }
        串.append(当前.值);
        当前 = 当前.下;
    }
    return 串.toString();
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("★ Node 类是自己写的，而且没用 java.util.LinkedList 冒充")]
    public static JavaCheckResult 检查_自己写的结点(string 代码)
    {
        // ⚠ 核心检查点之一。
        string 净 = 去掉注释(代码);
        if (净.Contains("new LinkedList") || 净.Contains("LinkedList<") || 净.Contains("LinkedList "))
            return JavaCheckResult.不过(
                "用了 java.util.LinkedList。\n" +
                "      它内部当然也是链表，但这一课要你**自己把结点和指针写出来** ——\n" +
                "      「改指针」这件事必须手写过一遍，才谈得上会用链表。\n" +
                "      ⇒ 自己写一个结点类：\n" +
                "          static class Node {\n" +
                "              int 值;\n" +
                "              Node 下;                   // 类型就是这个类自己\n" +
                "              Node(int 值) { this.值 = 值; }\n" +
                "          }\n" +
                "      然后所有操作都靠 `当前 = 当前.下;` 一步步走过去。");

        if (!Regex.IsMatch(净, @"class\s+Node"))
            return JavaCheckResult.不过(
                "没找到 `class Node` —— 这一课要求自己写结点类。\n" +
                "          static class Node {\n" +
                "              int 值;\n" +
                "              Node 下;\n" +
                "              Node(int 值) { this.值 = 值; }\n" +
                "          }\n" +
                "      ⚠ 里面的 `Node 下;` 是「自己指向自己」的类型 —— 这正是链表能串起来的原因。");
        if (!净.Contains("Node 下"))
            return JavaCheckResult.不过(
                "Node 里没看到 `Node 下;` 这个字段。\n" +
                "      链表靠的就是它：每个结点记着**下一个结点在哪里**。\n" +
                "      没有它，结点之间就没有联系了。");

        return JavaCheckResult.过("Node 是自己写的，也没拿 LinkedList 冒充");
    }

    [Java检查说明("三个方法都在，签名也对")]
    public static JavaCheckResult 检查_函数都在(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        if (!Regex.IsMatch(净, @"static\s+int\s+求长度\s*\(\s*Node\s+\w+\s*\)"))
            缺.Add("      求长度　（static int 求长度(Node 头)）");
        if (!Regex.IsMatch(净, @"static\s+Node\s+反转\s*\(\s*Node\s+\w+\s*\)"))
            缺.Add("      反转　　（static Node 反转(Node 头)）");
        if (!Regex.IsMatch(净, @"static\s+String\s+打印\s*\(\s*Node\s+\w+\s*\)"))
            缺.Add("      打印　　（static String 打印(Node 头)）");

        if (缺.Count == 0) return JavaCheckResult.过("三个方法都在，签名也对");
        return JavaCheckResult.不过(
            "有方法没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      ⚠ 反转 返回的是 **Node**（新的头结点），不是 void —— 头变了，调用方得知道。");
    }

    [Java检查说明("★ 反转真的改了指针（反转后长度、两次反转都验一遍）")]
    public static JavaCheckResult 检查_真的反转(string 代码)
    {
        // ⚠ 核心检查点之二：只打印不改指针的写法会在这里露馅 ——
        //   调用点里反转之后又量了长度、又打印、还反转回来比了一次。
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("反转后|", "3->2->1", "反转(1->2->3) 之后打印，应当是 3->2->1"),
            ("反转后长度|", "3", "反转**不改结点个数** —— 还是 3（只打印不返转的话这里就对不上）"),
            ("再反转|", "1->2->3", "把刚才的结果再反转一次，应当回到 1->2->3（真的改了指针才回得来）"),
            ("长反转|", "5->6->7->8->9", "反转(9->8->7->6->5)"),
            ("长长度|", "5", "反转前后长度都是 5"),
            ("空反转|", "空", "反转(null) ⇒ 还是空链表（别在这里 NPE）"),
            ("单反转|", "7", "反转(单结点) ⇒ 还是它自己"));
    }

    [Java检查说明("求长度：空、单结点、多个（走到 null 为止）")]
    public static JavaCheckResult 检查_长度(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("空长|", "0", "求长度(null)　空链表是 0"),
            ("单长|", "1", "求长度(单结点)"),
            ("三长|", "3", "求长度(1->2->3)"),
            ("五长|", "5", "求长度(9->8->7->6->5)"));
    }

    [Java检查说明("打印：格式 1->2->3，空链表是 空")]
    public static JavaCheckResult 检查_打印(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("空打印|", "空", "打印(null)　空链表打印 空"),
            ("单打印|", "7", "打印(单结点)　⚠ 只有一个结点时前面**不该有箭头**"),
            ("三打印|", "1->2->3", "打印(1->2->3)　箭头在数字之间，没有空格"),
            ("五打印|", "9->8->7->6->5", "打印(9->8->7->6->5)"));
    }

    // ══════════════════ 工具 ══════════════════

    private const string 调用点 = """
        Node 三头 = new Node(1);
        三头.下 = new Node(2);
        三头.下.下 = new Node(3);
        System.out.println("空长|" + 求长度(null));
        System.out.println("单长|" + 求长度(new Node(7)));
        System.out.println("三长|" + 求长度(三头));
        System.out.println("空打印|" + 打印(null));
        System.out.println("单打印|" + 打印(new Node(7)));
        System.out.println("三打印|" + 打印(三头));

        Node 反后 = 反转(三头);
        System.out.println("反转后|" + 打印(反后));
        System.out.println("反转后长度|" + 求长度(反后));
        Node 又回 = 反转(反后);
        System.out.println("再反转|" + 打印(又回));

        Node 五头 = new Node(9);
        五头.下 = new Node(8);
        五头.下.下 = new Node(7);
        五头.下.下.下 = new Node(6);
        五头.下.下.下.下 = new Node(5);
        System.out.println("五长|" + 求长度(五头));
        System.out.println("五打印|" + 打印(五头));
        Node 五反 = 反转(五头);
        System.out.println("长反转|" + 打印(五反));
        System.out.println("长长度|" + 求长度(五反));
        System.out.println("空反转|" + 打印(反转(null)));
        System.out.println("单反转|" + 打印(反转(new Node(7))));
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
                return "空指针了（NullPointerException）—— 链表课最常见的一个。\n" +
                       "      两个高发点：\n" +
                       "        · 循环条件写成了 `while (当前.下 != null)`：空链表（头 == null）\n" +
                       "          第一下就 NPE。要先问 **当前** 是不是 null。\n" +
                       "        · 反转里忘了先存 `Node 下一个 = 当前.下;`，改完指针再往前走就踩空了。\n" +
                       "      ⇒ 遍历的骨架永远是：\n" +
                       "          Node 当前 = 头;\n" +
                       "          while (当前 != null) { …; 当前 = 当前.下; }";
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
            "      链表的三个要点：\n" +
            "        遍历    Node 当前 = 头;  while (当前 != null) { … 当前 = 当前.下; }\n" +
            "        反转    先存 下一个、再改指针、最后往前走两步；返回 **上一个**\n" +
            "        打印    箭头只在「不是第一个」时加（用 串.length() > 0 判断）");
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
