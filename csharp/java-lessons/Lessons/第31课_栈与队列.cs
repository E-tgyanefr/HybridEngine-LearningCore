// 第 31 课：栈与队列 —— 后进先出、先进先出
//
// ══ 这一课要教什么 ══
// 两种"只能从一头进出"的容器：
//   栈（stack）  后进先出（LIFO）—— 像一摞盘子，只能从顶上拿
//   队列（queue）先进先出（FIFO）—— 像排队，从队尾进、队头出
//
// 它们在算法里出现的频率极高，而且各有"一眼就该想到它"的场景：
//   看到「最近的、还没配对的」     → 栈（括号匹配、表达式求值、撤销）
//   看到「按顺序一层层往外扩」     → 队列（第 35 课的 BFS、任务排队）
//
// Java 里这两个都用 **`ArrayDeque`** 实现（一个类，两套方法名）：
//   当栈用：push / pop / peek
//   当队列用：offer / poll / peek
// ⚠ 不要用 `LinkedList`：它能用，但每个元素一个结点对象，又慢又费内存。
//   也不要再用过时的 `Stack` 类（它是同步的，而且 API 设计有历史包袱）。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点一：**括号匹配必须用栈**，而且 `(` 和 `)` 的**交叉错配**要判错。
//   `([)]` 这种"数量对得上、顺序对不上"的情况，用**计数器**或者
//   "从左往右随便配"的写法都会判成 true。只有栈能正确处理 ——
//   检查里专门喂 `([)]`、`(]`、`{)` 这几组。
//   另外要求源码里 括号匹配 的方法体出现 `push(` 和 `pop(`（真的用了栈）。
// 核心检查点二：**用 ArrayDeque，不是 LinkedList**。
//   去掉注释后出现 `new LinkedList` / `LinkedList<` 就判不过（会给提示而不是死判）。
// 第三条：轮转的边界（k 比数组长、k = 0、空数组）。
//
// ⚠ 学生只写方法；骨架预置 import java.util.*。

using System.Collections.Generic;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("31", "第 31 课：栈与队列 —— 后进先出、先进先出",
    "1. 写 static boolean 括号匹配(String s)：三种括号 () [] {} 要配对，而且顺序要对（([)] 是错的）\n" +
    "2. 写 static String 用栈倒序(int[] 数)：用 Deque 的 push/pop 把数组倒过来，返回空格分隔的字符串\n" +
    "3. 写 static String 用队列轮转(int[] 数, int k)：用 ArrayDeque 当队列，把数组**左移 k 位**，返回空格分隔的字符串\n" +
    "4. ⚠ 栈和队列都用 ArrayDeque，不要用 LinkedList")]
public static class 第31课
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
// 第 31 课：栈与队列 —— 后进先出、先进先出
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲两件事：**栈和队列各自的规矩**，以及
// 「看到什么特征就该想到用哪一个」。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    栈（LIFO）与队列（FIFO）：Deque / ArrayDeque、括号匹配
//
//  需要先会
//    第 16 课：集合（ArrayList / 泛型）
//    第 07 课：字符串（charAt / 字符比较）
//
// ─────────────────────────────────────────────────────
//  先介绍：两种"只能从一头进出"的容器
// ─────────────────────────────────────────────────────
//
// 数组和 ArrayList 是**哪儿都能进、哪儿都能出**的。
// 但有两类问题，限制住进出的一端反而更省事：
//
//     栈    一摞盘子：只能往顶上放、从顶上拿。**后进先出**（LIFO）
//     队列  排队买票：从队尾进、从队头出。    **先进先出**（FIFO）
//
// 它们不是"功能更少的表"，而是**规矩本身就在帮你**：
//   一摞盘子里，"最近放上去的那个"永远是第一个被拿到的 ——
//   这正是处理「还没配对的左括号」「上一步操作」时需要的东西。
//
// ─────────────────────────────────────────────────────
//  一、Java 里怎么写：ArrayDeque 一个类，两套方法名
// ─────────────────────────────────────────────────────
//
//     Deque<Integer> 栈 = new ArrayDeque<>();
//     栈.push(1);          压入（放顶上）
//     栈.pop();            弹出并返回（拿顶上那个）
//     栈.peek();           看一眼顶上，**不拿走**
//     栈.isEmpty();        空了吗
//
//     Deque<Integer> 队 = new ArrayDeque<>();
//     队.offer(1);         入队（放队尾）
//     队.poll();           出队并返回（拿队头那个）
//     队.peek();           看一眼队头
//     队.isEmpty();
//
// ⚠ 三件必须记住的事：
//   · **一个 ArrayDeque 既能当栈也能当队列**，区别只是用哪套方法名。
//     混着用（offer 进去、pop 出来）也能跑，但那是自己给自己挖坑 ——
//     想清楚这个容器是栈还是队列，然后只用那一套方法。
//   · 对**空**的栈/队列调 pop / poll，返回的是 **null**（不是抛异常）。
//     而 null 一拆箱成 int 就抛 NullPointerException ——
//     所以循环条件里永远先问 `!栈.isEmpty()`。
//   · `Deque` 是接口，`ArrayDeque` 是实现（第 14 课那个套路）。
//     声明写 `Deque<Integer>`，new 的时候写 `ArrayDeque<>`。
//
// ⚠ 不要用 `LinkedList`，也不要用老的 `Stack` 类：
//   · `LinkedList` 每个元素都要 new 一个结点对象，又慢又费内存
//   · `Stack` 是 JDK 1 留下的老类，方法都是同步的（单线程下白付出代价）
//   Java 官方文档现在明确推荐：**用栈或队列，就用 ArrayDeque**。
//
// ─────────────────────────────────────────────────────
//  二、括号匹配：栈的教科书例子
// ─────────────────────────────────────────────────────
//
// 判断一串括号是否配对：`([]{})` 对，`([)]` 错。
//
// 从左往右扫，遇到**左**括号就压进栈，遇到**右**括号就检查栈顶：
//
//     ① 栈是空的                ⇒ 错（这个右括号没有对应的左括号）
//     ② 栈顶和它不是一对        ⇒ 错（顺序错了）
//     ③ 是一对                  ⇒ 弹出栈顶，继续
//
// 扫完之后，**栈必须是空的**（否则还有左括号没配上）。
//
//     Deque<Character> 栈 = new ArrayDeque<>();
//     for (int i = 0; i < s.length(); i++) {
//         char c = s.charAt(i);
//         if (c == '(' || c == '[' || c == '{') {
//             栈.push(c);
//         } else {
//             if (栈.isEmpty()) return false;
//             char 顶 = 栈.pop();
//             if (顶 == '(' && c != ')') return false;
//             if (顶 == '[' && c != ']') return false;
//             if (顶 == '{' && c != '}') return false;
//         }
//     }
//     return 栈.isEmpty();
//
// ⚠ 为什么 `([)]` 必须判错：它的括号**数量**是配对的（两个左、两个右），
//   但 `(` 遇到的是 `]` —— 顺序不对。**光数个数是抓不住这一类的。**
//   这也是"为什么非得用栈"的答案：栈记的是**最近的那个还没配对的左括号**。
//
// ─────────────────────────────────────────────────────
//  三、什么时候该想到栈/队列
// ─────────────────────────────────────────────────────
//
//     看到「最近的那个」「还没配对的」「上一步」   → 栈
//         括号匹配、表达式求值、撤销（Ctrl+Z）、浏览器的后退
//     看到「按顺序一层层往外扩」「先来的先处理」   → 队列
//         第 35 课的 BFS 找最短步数、打印任务排队、消息队列
//
// 记这两条比记 API 有用得多 —— API 忘了可以查，认不出该用什么才是真的卡住。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：拿计数器代替栈
// ─────────────────────────────────────────────────────
//
//     static boolean 括号匹配(String s) {
//         int 左 = 0, 右 = 0;
//         for (int i = 0; i < s.length(); i++) {
//             if (s.charAt(i) == '(') 左++;
//             if (s.charAt(i) == ')') 右++;
//         }
//         return 左 == 右;                    ← `([)]` 会在这里被放过去
//     }
//
// 你会看到：**`([]{})` 判对、`()(` 判错**（数量不等），看起来还挺像回事。
// 但 `([)]` 会返回 **true** —— 而正确答案是 false。
//
// 为什么：数量相等只说明"括号一样多"，说明不了"顺序对"。
//   `([)]` 里的 `(` 从一开始就被 `]` 挡住了，它俩根本不该配对。
//
// ⇒ 必须用栈：栈记住的是**最近一个还没配对的左括号**，
//   右括号一来就跟它比 —— 顺序对不对，一比就知道。
//
//   附一条这一课真会撞上的运行期错：
//   对空的 Deque 调 pop，拿到的是 null；直接拆箱成 int 就会：
//
//       Exception in thread "main" java.lang.NullPointerException:
//           Cannot invoke "java.lang.Integer.intValue()" because ...
//
//   ⇒ 循环条件里先问 `!栈.isEmpty()`。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
//   用队列做 BFS 找最短路 → 第 35 课。
//   自己用数组实现栈/队列 → 不涉及。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 你**只写方法**：不用写 class，不用写 main；`java.util.*` 已经 import 好了。
//    每个方法都要带 **static**。
//
//   1. static boolean 括号匹配(String s)
//        三种括号 () [] {} 都要配对，而且**顺序要对**：
//        `([)]` 是错的、`([]{})` 是对的。空串算对。
//        ⚠ 必须用 Deque 的 push/pop（检查会看方法体里有没有它俩）。
//
//   2. static String 用栈倒序(int[] 数)
//        用 `Deque<Integer>` 的 push / pop 把数组倒过来，
//        返回**空格分隔**的字符串（`{1,2,3}` ⇒ `3 2 1`，空数组 ⇒ 空串）。
//
//   3. static String 用队列轮转(int[] 数, int k)
//        用 `ArrayDeque` 当队列，把数组**左移 k 位**再返回（空格分隔）。
//        `{1,2,3,4,5}` 左移 2 位 ⇒ `3 4 5 1 2`。
//        ⚠ k 可能比数组长（先把 k 化成 k % 长度）、k 可能是 0、数组可能是空的。
//
//  ⚠ 两个容器都用 **ArrayDeque**，不要用 LinkedList。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
    static boolean 括号匹配(String s) {
        return false;
    }

    static String 用栈倒序(int[] 数) {
        return "";
    }

    static String 用队列轮转(int[] 数, int k) {
        return "";
    }
""";

    public static string 参考答案 => """
// 参考答案 —— 栈与队列（只写方法，不用写 class / main / import）

// 1. 括号匹配：栈记住「最近一个还没配对的左括号」
//    ⚠ 数量相等不等于顺序对 —— ([)] 就是数量对、顺序错
static boolean 括号匹配(String s) {
    Deque<Character> 栈 = new ArrayDeque<>();
    for (int i = 0; i < s.length(); i++) {
        char c = s.charAt(i);
        if (c == '(' || c == '[' || c == '{') {
            栈.push(c);
        } else if (c == ')' || c == ']' || c == '}') {
            if (栈.isEmpty()) {
                return false;                     /* 右括号来了，可没有左括号等它 */
            }
            char 顶 = 栈.pop();                    /* 最近的那个左括号 */
            if (顶 == '(' && c != ')') return false;
            if (顶 == '[' && c != ']') return false;
            if (顶 == '{' && c != '}') return false;
        }
    }
    return 栈.isEmpty();                          /* 还有左括号没配对上 ⇒ 错 */
}

// 2. 用栈倒序：先全部压进去，再一个个弹出来 —— 出来的顺序就是倒的
static String 用栈倒序(int[] 数) {
    Deque<Integer> 栈 = new ArrayDeque<>();
    for (int i = 0; i < 数.length; i++) {
        栈.push(数[i]);                        /* 后进 */
    }
    StringBuilder 结果 = new StringBuilder();
    while (!栈.isEmpty()) {                      /* ⚠ 先问空不空：空栈 pop 得到 null */
        if (结果.length() > 0) {
            结果.append(" ");
        }
        结果.append(栈.pop());                  /* 先出 */
    }
    return 结果.toString();
}

// 3. 用队列轮转：把前 k 个依次从队头拿到队尾
static String 用队列轮转(int[] 数, int k) {
    Deque<Integer> 队 = new ArrayDeque<>();
    for (int i = 0; i < 数.length; i++) {
        队.offer(数[i]);                       /* 入队（队尾） */
    }
    if (数.length > 0) {
        int 移 = k % 数.length;                  /* ⚠ k 可能比长度大，也保证不是负数 */
        for (int i = 0; i < 移; i++) {
            队.offer(队.poll());                /* 队头出来、立刻进队尾 —— 这就是"轮转" */
        }
    }
    StringBuilder 结果 = new StringBuilder();
    while (!队.isEmpty()) {
        if (结果.length() > 0) {
            结果.append(" ");
        }
        结果.append(队.poll());
    }
    return 结果.toString();
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("三个方法都在，签名也对")]
    public static JavaCheckResult 检查_函数都在(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        if (!Regex.IsMatch(净, @"static\s+boolean\s+括号匹配\s*\(\s*String\s+\w+\s*\)"))
            缺.Add("      括号匹配　（static boolean 括号匹配(String s)）");
        if (!Regex.IsMatch(净, @"static\s+String\s+用栈倒序\s*\(\s*int\s*\[\s*\]\s*\w+\s*\)"))
            缺.Add("      用栈倒序　（static String 用栈倒序(int[] 数)）");
        if (!Regex.IsMatch(净, @"static\s+String\s+用队列轮转\s*\(\s*int\s*\[\s*\]\s*\w+\s*,\s*int\s+\w+\s*\)"))
            缺.Add("      用队列轮转（static String 用队列轮转(int[] 数, int k)）");

        if (缺.Count == 0) return JavaCheckResult.过("三个方法都在，签名也对");
        return JavaCheckResult.不过(
            "有方法没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      ⚠ 后两个返回的都是 **String**（空格分隔的数列），不是数组。");
    }

    [Java检查说明("★ 括号匹配：交叉错配的 ([)] 必须判错（计数器写法会放过它）")]
    public static JavaCheckResult 检查_交叉错配(string 代码)
    {
        // ⚠ 核心检查点：数量相等但顺序错的括号，只有栈能判对。
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);

        if (看看(run, "交叉|") == "true")
            return JavaCheckResult.不过(
                "括号匹配(([)]) 返回了 **true** —— 正确答案是 false。\n" +
                "      `([)]` 的括号**数量**是配对的（两个左、两个右），但顺序不对：\n" +
                "      第一个 `(` 从一开头就被 `]` 挡住了，它俩根本不该配成一对。\n" +
                "      这说明你数的是个数（或者从左往右随便配）—— 那种写法抓不住这一类。\n" +
                "      ⇒ 用栈：栈记住的是**最近一个还没配对的左括号**：\n" +
                "          遇到左括号 → push 进去\n" +
                "          遇到右括号 → 栈空就 false；否则 pop 出来看看是不是**这一种**的左括号\n" +
                "          扫完 → 栈必须也是空的\n" +
                "        `([)]` 走到 `]` 时，栈顶是 `(`（不是 `[`）⇒ 立刻判错。");

        return 比(run,
            ("空串|", "true", "括号匹配(空串)　没有括号要配 ⇒ 对"),
            ("纯文字|", "true", "括号匹配(abc)　一个括号都没有 ⇒ 对"),
            ("简单对|", "true", "括号匹配(())"),
            ("三种|", "true", "括号匹配({[]})"),
            ("嵌套|", "true", "括号匹配(([]{})[])"),
            ("多左|", "false", "括号匹配(()　左括号多了，扫完栈不空"),
            ("多右|", "false", "括号匹配())　右括号多了，栈空时来了右括号"),
            ("先后错|", "false", "括号匹配())(　数量一样多，但顺序错"),
            ("交叉|", "false", "⚠ 括号匹配(([)])　数量对、顺序错 —— 计数器写法会在这里露馅"),
            ("错配|", "false", "括号匹配((])"),
            ("错配二|", "false", "括号匹配({)}"),
            ("长串|", "true", "括号匹配({[()]}{[()]})"));
    }

    [Java检查说明("★ 括号匹配真的用了栈（push 和 pop 都要出现）")]
    public static JavaCheckResult 检查_用了栈(string 代码)
    {
        string 净 = 去掉注释(代码);
        string 体 = 函数体(净, "括号匹配");
        if (体 == null)
            return JavaCheckResult.不过("没找到 括号匹配 的函数体 —— 先确认这个方法写全了（花括号配对）。");

        var 缺 = new List<string>();
        if (!体.Contains("push(")) 缺.Add("      · 没有 push(（左括号要压进栈）");
        if (!体.Contains("pop(")) 缺.Add("      · 没有 pop(（右括号要跟栈顶比一比）");
        if (缺.Count == 0) return JavaCheckResult.过("括号匹配 里 push / pop 都在 —— 用的是栈");

        return JavaCheckResult.不过(
            "括号匹配 里没看到栈的操作：\n" + string.Join("\n", 缺) + "\n" +
            "      这一课的正题就是**用栈解决配对问题**：\n" +
            "          Deque<Character> 栈 = new ArrayDeque<>();\n" +
            "          遇到 ( [ {  → 栈.push(c);\n" +
            "          遇到 ) ] }  → if (栈.isEmpty()) return false;\n" +
            "                        char 顶 = 栈.pop();  然后检查 顶 和 c 是不是一对\n" +
            "          扫完        → return 栈.isEmpty();\n" +
            "      ⚠ 数个数（或者用 String.replace 反复消括号）都能过一部分用例，\n" +
            "        但 `([)]` 那种顺序错的一定会露馅。");
    }

    [Java检查说明("栈倒序：先压后弹，出来就是倒的")]
    public static JavaCheckResult 检查_倒序(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("倒空|", "", "用栈倒序({})　空数组 ⇒ 空串"),
            ("倒单|", "7", "用栈倒序({7})"),
            ("倒三|", "3 2 1", "用栈倒序({1,2,3})　后进的先出 ⇒ 3 2 1"),
            ("倒五|", "9 6 2 -1 -3", "用栈倒序({-3,-1,2,6,9})　含负数"),
            ("倒同|", "5 5 5", "用栈倒序({5,5,5})"));
    }

    [Java检查说明("队列轮转：左移 k 位（k 比长度大、k=0、空数组都要对）")]
    public static JavaCheckResult 检查_轮转(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("轮0|", "1 2 3 4 5", "用队列轮转({1,2,3,4,5}, 0)　不移"),
            ("轮2|", "3 4 5 1 2", "用队列轮转({1,2,3,4,5}, 2)　左移 2 位"),
            ("轮5|", "1 2 3 4 5", "用队列轮转({1,2,3,4,5}, 5)　移一个整圈 ⇒ 回到原样"),
            ("轮7|", "3 4 5 1 2", "⚠ 用队列轮转({1,2,3,4,5}, 7)　7 % 5 = 2 ⇒ 和移 2 位一样"),
            ("轮空|", "", "用队列轮转({}, 3)　空数组 ⇒ 空串（别在这里除零：长度是 0 时不能算 k % 长度）"),
            ("轮单|", "9", "用队列轮转({9}, 3)　一个元素，怎么转都是它"));
    }

    [Java检查说明("用的是 ArrayDeque（不是 LinkedList）")]
    public static JavaCheckResult 检查_用ArrayDeque(string 代码)
    {
        string 净 = 去掉注释(代码);
        if (净.Contains("new LinkedList") || 净.Contains("LinkedList<") || 净.Contains("LinkedList "))
            return JavaCheckResult.不过(
                "用了 LinkedList。它能当栈和队列，但**这一课要求用 ArrayDeque**：\n" +
                "        · ArrayDeque 内部是一段数组，快、省内存\n" +
                "        · LinkedList 每个元素都要 new 一个结点对象 —— 又慢又占地方\n" +
                "        · Java 官方文档现在明确推荐：栈和队列都用 ArrayDeque\n" +
                "      ⇒ 把 `new LinkedList<>()` 换成 `new ArrayDeque<>()`，\n" +
                "        声明那头写 `Deque<Integer>`（第 14 课那个「接口 + 实现」的套路）。");

        if (!净.Contains("ArrayDeque"))
            return JavaCheckResult.不过(
                "没看到 ArrayDeque —— 这一课要求用它来实现栈和队列。\n" +
                "          Deque<Integer> 栈 = new ArrayDeque<>();\n" +
                "          栈.push(1);  栈.pop();  栈.peek();  栈.isEmpty();\n" +
                "          Deque<Integer> 队 = new ArrayDeque<>();\n" +
                "          队.offer(1);  队.poll();  队.peek();  队.isEmpty();\n" +
                "      ⚠ Deque 是接口、ArrayDeque 是实现；声明写接口那头。");

        return JavaCheckResult.过("用的是 ArrayDeque —— Java 里当栈/队列的标准选择");
    }

    // ══════════════════ 工具 ══════════════════

    private const string 调用点 = """
        System.out.println("空串|" + 括号匹配(""));
        System.out.println("纯文字|" + 括号匹配("abc"));
        System.out.println("简单对|" + 括号匹配("()"));
        System.out.println("三种|" + 括号匹配("({[]})"));
        System.out.println("嵌套|" + 括号匹配("(([]{})[])"));
        System.out.println("多左|" + 括号匹配("(()"));
        System.out.println("多右|" + 括号匹配("())"));
        System.out.println("先后错|" + 括号匹配("())("));
        System.out.println("交叉|" + 括号匹配("([)]"));
        System.out.println("错配|" + 括号匹配("(]"));
        System.out.println("错配二|" + 括号匹配("{)"));
        System.out.println("长串|" + 括号匹配("({[()]}{[()]})"));

        System.out.println("倒空|" + 用栈倒序(new int[]{}));
        System.out.println("倒单|" + 用栈倒序(new int[]{7}));
        System.out.println("倒三|" + 用栈倒序(new int[]{1, 2, 3}));
        System.out.println("倒五|" + 用栈倒序(new int[]{-3, -1, 2, 6, 9}));
        System.out.println("倒同|" + 用栈倒序(new int[]{5, 5, 5}));

        System.out.println("轮0|" + 用队列轮转(new int[]{1, 2, 3, 4, 5}, 0));
        System.out.println("轮2|" + 用队列轮转(new int[]{1, 2, 3, 4, 5}, 2));
        System.out.println("轮5|" + 用队列轮转(new int[]{1, 2, 3, 4, 5}, 5));
        System.out.println("轮7|" + 用队列轮转(new int[]{1, 2, 3, 4, 5}, 7));
        System.out.println("轮空|" + 用队列轮转(new int[]{}, 3));
        System.out.println("轮单|" + 用队列轮转(new int[]{9}, 3));
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
                return "空指针了（NullPointerException）。\n" +
                       "      这一课最常见的原因：**对空的栈/队列调 pop / poll** ——\n" +
                       "      pop 和 poll 在容器为空时返回的是 **null**，拆箱成 int 就崩。\n" +
                       "      ⇒ 出栈/出队之前先问一句：\n" +
                       "          while (!栈.isEmpty()) { int 值 = 栈.pop(); … }\n" +
                       "      （peek 也一样：空的时候返回 null。）";
            if (run.Raw.Error.Contains("/ by zero"))
                return "除零了（ArithmeticException）。\n" +
                       "      多半是轮转里算了 `k % 数.length` 而数组是空的 ⇒ 0 做除数。\n" +
                       "      ⇒ 先判长度：if (数.length > 0) { int 移 = k % 数.length; … }";
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
            bool 空期望 = 期望.Length == 0;
            if (得到.Length == 0 && !空期望) 错.Add("      " + 说明 + "：没有找到这个标记的输出");
            else if (!一样(得到, 期望))
                错.Add("      " + 说明 + "：得到「" + 得到 + "」，应该是「" + 期望 + "」");
        }
        if (错.Count == 0) return JavaCheckResult.过($"这几组都对（{用例们.Length} 组）");

        return JavaCheckResult.不过(
            "结果不对：\n" + string.Join("\n", 错) + "\n" +
            "      这一课三个方法的要点：\n" +
            "        括号匹配   用栈记住「最近一个还没配对的左括号」；扫完栈要空\n" +
            "        用栈倒序   push 全部、再 pop 出来就是倒的\n" +
            "        用队列轮转 队头 poll 出来立刻 offer 回队尾 —— 转 k 次就是左移 k 位\n" +
            "      ⚠ 轮转要先 `k % 长度`（k 可能比长度大），而且长度 0 时不能取模。");
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
