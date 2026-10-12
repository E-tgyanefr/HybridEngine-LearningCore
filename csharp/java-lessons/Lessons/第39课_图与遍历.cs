// 第 39 课：图与遍历 —— 邻接表、DFS/BFS、连通块
//
// ══ 这一课要教什么 ══
// 从链表（一条线）到树（分叉），这一课到最一般的结构：**图**。
// 图里任意两个点之间都能连边，还能有环 —— 所以"怎么把它存下来"本身
// 就成了第一个要解决的问题。
//
// 三种存法，这一课用第二种：
//   ① 邻接矩阵  int[][] 边 — 查"a 和 b 连不连"是 O(1)，但 n 大时开不下（n² 格）
//   ② **邻接表** List<List<Integer>> — 每个点只存"它连出去的那些点"，省空间
//   ③ 边表      把所有边列成 {u, v} 的数组 — 适合"遍历所有边"的算法
//
// `List<List<Integer>>` 这个类型看着吓人，其实只是"一张表，每格里又是一张表"：
// 外层下标是点，内层是它连出去的点。
//
// 这一课还要把第 35 课的 DFS/BFS 搬到**真正的一般图**上 ——
// 网格只是图的一个特例（每个格子连着上下左右四个格子），
// 而一般图里每个点能连的邻居个数是**不固定**的，所以循环要改成
// `for (int 邻居 : 邻接表.get(点))`。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点一：**邻接表要双向加边**（无向图）。
//   只写 `邻接表.get(u).add(v)` 的话，从 u 能走到 v、从 v 走不回 u ——
//   连通块会数成错的（一组用例能抓出来），而且**编译和运行都不报错**。
//   检查里喂无向图的连通块，只加单向边会得到明显偏大的块数。
// 核心检查点二：**别用递归 DFS**。
//   检查里喂一条 **10000 个点的长链**（0—1—2—…—9999）：
//   递归 DFS 深度 10000 ⇒ StackOverflowError；BFS/显式栈没问题。
//   实测：递归写法在 10000 的链上必崩（JDK 默认栈大约几千层）。
//   所以 连通块数 / 能否到达 都要求循环 + 队列（或显式栈）。
// 第三条：`建图` 要能处理"点数 0"和"边里有重复边"（多重边不该把答案搞错）。
//
// ⚠ 学生只写方法；骨架预置 import java.util.*。

using System.Collections.Generic;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("39", "第 39 课：图与遍历 —— 邻接表、DFS/BFS、连通块",
    "1. 写 static List<List<Integer>> 建图(int 点数, int[][] 边)：无向图，**每条边两个方向都要加**\n" +
    "2. 写 static boolean 能否到达(List<List<Integer>> 图, int 起, int 终)：用队列（BFS）或显式栈，**别用递归**\n" +
    "3. 写 static int 连通块数(List<List<Integer>> 图)：数一数有几片互相走不到的区域\n" +
    "4. ⚠ 检查里有一条 10000 个点的长链 —— 递归 DFS 在那里会爆栈")]
public static class 第39课
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
// 第 39 课：图与遍历 —— 邻接表、DFS/BFS、连通块
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**图怎么存**（邻接表），以及怎么在它上面走。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    图的邻接表表示；一般图上的 DFS / BFS；连通块
//
//  需要先会
//    第 35 课：网格上的 DFS / BFS（图的一个特例）
//    第 31 课：队列
//    第 16 课：ArrayList 和泛型（List<List<Integer>> 就是两层表）
//
// ─────────────────────────────────────────────────────
//  先介绍：图 = 点 + 边
// ─────────────────────────────────────────────────────
//
// 前面几课的结构其实都是图的特例：
//
//     链表   每个点只连着一个"下一个"
//     树     每个点连着一个父结点和若干孩子，**没有环**
//     图     **随便连**，可以有环，一个点可以连很多个点
//
// 图在生活里到处都是：地图（路口 + 道路）、社交网络（人 + 好友）、
// 网页链接、课程先修关系……
//
// 存一张图，先要解决"**每个点连了谁**"这件事。
//
// ─────────────────────────────────────────────────────
//  一、三种存法
// ─────────────────────────────────────────────────────
//
//     ① 邻接矩阵   `boolean[][] 连` 或者 `int[][] 权`
//                  查"a、b 连不连"是 O(1)，但**n 个点要开 n×n 格** ——
//                  n = 10 万时就是 100 亿格，开不下。
//
//     ② 邻接表     `List<List<Integer>> 图`
//                  只存**真的存在的边**，省空间。
//                  `图.get(3)` 就是"3 号点连出去的所有点"。
//
//     ③ 边表       `int[][] 边`，每行 {u, v}
//                  只关心"遍历所有边"的算法用它（比如最小生成树）。
//
// 这一课用 **②邻接表** —— 竞赛和工程里最常用的就是它。
//
// ─────────────────────────────────────────────────────
//  二、邻接表长什么样
// ─────────────────────────────────────────────────────
//
// `List<List<Integer>>` 读作"一个 List，里面的每一格也是一个 List<Integer>"。
// 外层下标是**点**，内层是它**连出去的点**：
//
//     点 0 连着 1、2         图.get(0) = [1, 2]
//     点 1 连着 0、2         图.get(1) = [0, 2]
//     点 2 连着 0、1         图.get(2) = [0, 1]
//     点 3 谁也不连          图.get(3) = []
//
// 建图分两步 —— ⚠ **第一步千万别忘**：
//
//     List<List<Integer>> 图 = new ArrayList<>();
//     for (int i = 0; i < 点数; i++) {
//         图.add(new ArrayList<>());            ← ① 先给每个点建一个空表
//     }
//     for (int[] 一条边 : 边) {
//         图.get(一条边[0]).add(一条边[1]);       ← ② 再加边
//         图.get(一条边[1]).add(一条边[0]);       ← ⚠ 无向图：**反方向也要加**
//     }
//
// ⚠ 忘了第一步的话，`图.get(0)` 会抛 `IndexOutOfBoundsException`
//   —— 空表里当然没有第 0 格。
//
// ⚠ 忘了反方向那一次 add，是这一课最阴的错：
//   **编译通过、运行正常、从 u 能走到 v** —— 只有"从 v 走不回 u"这一个症状。
//   连通块会数错，而且是那种"看起来差不多"的错。
//
// ─────────────────────────────────────────────────────
//  三、在图上走：和第 35 课一模一样，只是邻居要遍历
// ─────────────────────────────────────────────────────
//
// 网格上每个格子的邻居是固定的四个（上下左右），所以写四行；
// 一般图里每个点的邻居个数**不固定**，所以改成遍历邻接表：
//
//     Deque<Integer> 队 = new ArrayDeque<>();
//     boolean[] 来过 = new boolean[点数];
//     队.offer(起);
//     来过[起] = true;
//     while (!队.isEmpty()) {
//         int 当前 = 队.poll();
//         for (int 邻居 : 图.get(当前)) {          ← ⚠ 这一行就是"一般图"的写法
//             if (!来过[邻居]) {
//                 来过[邻居] = true;
//                 队.offer(邻居);
//             }
//         }
//     }
//
// ⚠ `来过`（visited）在图上比在网格上**更要紧**：
//   网格是平面结构，绕圈的代价有限；一般图可以有任意多的环，
//   没有 visited 就会反复走同一条回路，永远出不来。
//
// 「连通块」= 从任意一个没来过的点出发走一遍，能碰到的所有点算一块。
// 数一共起了几次搜索，就是几块：
//
//     int 块数 = 0;
//     for (int i = 0; i < 点数; i++) {
//         if (来过[i]) continue;
//         块数 = 块数 + 1;                       ← 每起一次搜索，就发现一块新的
//         …从 i 出发 BFS，把这一块全标上…
//     }
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：递归 DFS 在长链上爆栈
// ─────────────────────────────────────────────────────
//
//     static void 深搜(List<List<Integer>> 图, boolean[] 来过, int 点) {
//         来过[点] = true;
//         for (int 邻居 : 图.get(点)) {
//             if (!来过[邻居]) 深搜(图, 来过, 邻居);      ← 递归
//         }
//     }
//
// 这份代码**完全正确**，在几十个点的图上跑得好好的。但检查里有一条链：
//
//     0 — 1 — 2 — 3 — … — 9999
//
// 那条链上递归深度是 **10000 层**，于是：
//
//     Exception in thread "main" java.lang.StackOverflowError
//
// 为什么：递归的每一层都占一格**调用栈**，而调用栈很小（默认大约几千层）。
//   网格（第 35 课）上最多几万层也会崩，图上的链更是随随便便就上万层。
//
// ⇒ 图的遍历**默认用循环 + 队列**（BFS），或者用循环 + **显式栈**（把待处理的点
//   压进一个 `Deque`，而不是压在调用栈上）。
//   要写递归的话，只适合"深度肯定很小"的场合。
//
//   ⚠ 这一条和"算法对不对"无关 —— 它是**实现方式**的坑，而且只在数据大时出现。
//     所以本地小数据测不出来，必须专门拿长链试一次。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
//   带权图的最短路（Dijkstra）→ 第 40 课。
//   有向图、拓扑排序 → 不涉及。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 你**只写方法**：不用写 class，不用写 main；`java.util.*` 已经 import 好了。
//    每个方法都要带 **static**。
//
//   1. static List<List<Integer>> 建图(int 点数, int[][] 边)
//        **无向图**：每条边 {u, v} 要**两个方向都加**。
//        ⚠ 每个点先建好一个空表，再加边。
//
//   2. static boolean 能否到达(List<List<Integer>> 图, int 起, int 终)
//        用队列（BFS）或者显式栈。起点就是终点返回 true。
//
//   3. static int 连通块数(List<List<Integer>> 图)
//        有几片互相走不到的区域。**孤立点自己算一块。**
//
//  ⚠ 检查里有一条 **10000 个点的长链** —— 递归 DFS 在那里会爆栈。
//    所以两个遍历都请写成**循环 + 队列**。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
    static List<List<Integer>> 建图(int 点数, int[][] 边) {
        return null;
    }

    static boolean 能否到达(List<List<Integer>> 图, int 起, int 终) {
        return false;
    }

    static int 连通块数(List<List<Integer>> 图) {
        return 0;
    }
""";

    public static string 参考答案 => """
// 参考答案 —— 图的邻接表与遍历（只写方法，不用写 class / main / import）
//
// 两个遍历都用**队列的循环**，不用递归 ——
// 检查里有一条 10000 个点的链，递归 DFS 在那里会爆栈。

// 1. 建图：⚠ 第一步先给每个点建一个空表，第二步无向图要**两个方向都加**
static List<List<Integer>> 建图(int 点数, int[][] 边) {
    List<List<Integer>> 图 = new ArrayList<>();
    for (int i = 0; i < 点数; i++) {
        图.add(new ArrayList<>());         /* ⚠ 忘了这一句，后面 图.get(i) 就 IndexOutOfBounds */
    }
    for (int i = 0; i < 边.length; i++) {
        int 甲 = 边[i][0];
        int 乙 = 边[i][1];
        if (甲 < 0 || 甲 >= 点数 || 乙 < 0 || 乙 >= 点数) continue;   /* 越界的边跳过 */
        图.get(甲).add(乙);
        图.get(乙).add(甲);                 /* ⚠ 无向图：反方向也要加，否则 v 走不回 u */
    }
    return 图;
}

// 2. 能不能到达：BFS。一般图里邻居靠遍历邻接表拿到（不是固定的四个方向）
static boolean 能否到达(List<List<Integer>> 图, int 起, int 终) {
    if (图 == null || 图.isEmpty()) return false;
    int 点数 = 图.size();
    if (起 < 0 || 起 >= 点数 || 终 < 0 || 终 >= 点数) return false;
    if (起 == 终) return true;

    boolean[] 来过 = new boolean[点数];
    Deque<Integer> 队 = new ArrayDeque<>();
    队.offer(起);
    来过[起] = true;
    while (!队.isEmpty()) {
        int 当前 = 队.poll();
        for (int 邻居 : 图.get(当前)) {       /* ⚠ 「一般图」就体现在这一行 */
            if (来过[邻居]) continue;
            if (邻居 == 终) return true;
            来过[邻居] = true;
            队.offer(邻居);
        }
    }
    return false;
}

// 3. 连通块：从每个没来过的点起一次 BFS，数一共起了几次
static int 连通块数(List<List<Integer>> 图) {
    if (图 == null || 图.isEmpty()) return 0;
    int 点数 = 图.size();
    boolean[] 来过 = new boolean[点数];
    int 块数 = 0;
    for (int i = 0; i < 点数; i++) {
        if (来过[i]) continue;
        块数 = 块数 + 1;                    /* 每起一次搜索 = 发现一块新的 */
        Deque<Integer> 队 = new ArrayDeque<>();
        队.offer(i);
        来过[i] = true;
        while (!队.isEmpty()) {
            int 当前 = 队.poll();
            for (int 邻居 : 图.get(当前)) {
                if (来过[邻居]) continue;
                来过[邻居] = true;
                队.offer(邻居);
            }
        }
    }
    return 块数;
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("三个方法都在，签名也对")]
    public static JavaCheckResult 检查_函数都在(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        if (!Regex.IsMatch(净, @"static\s+List\s*<\s*List\s*<\s*Integer\s*>\s*>\s+建图\s*\(\s*int\s+\w+\s*,\s*int\s*\[\s*\]\s*\[\s*\]\s*\w+\s*\)"))
            缺.Add("      建图　　（static List<List<Integer>> 建图(int 点数, int[][] 边)）");
        if (!Regex.IsMatch(净, @"static\s+boolean\s+能否到达\s*\(\s*List\s*<\s*List\s*<\s*Integer\s*>\s*>\s+\w+\s*,\s*int\s+\w+\s*,\s*int\s+\w+\s*\)"))
            缺.Add("      能否到达（static boolean 能否到达(List<List<Integer>> 图, int 起, int 终)）");
        if (!Regex.IsMatch(净, @"static\s+int\s+连通块数\s*\(\s*List\s*<\s*List\s*<\s*Integer\s*>\s*>\s+\w+\s*\)"))
            缺.Add("      连通块数（static int 连通块数(List<List<Integer>> 图)）");

        if (缺.Count == 0) return JavaCheckResult.过("三个方法都在，签名也对");
        return JavaCheckResult.不过(
            "有方法没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      ⚠ 邻接表的类型要写全：**List<List<Integer>>**（尖括号里还是尖括号）。\n" +
            "        它是「一张表，每格里又是一张表」—— 外层下标是点，内层是它连出去的点。");
    }

    [Java检查说明("★ 建图：无向图两个方向都要加（只加一边连通块就错）")]
    public static JavaCheckResult 检查_建图(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);

        var 错 = new List<string>();
        for (int i = 1; i <= 4; i++)
        {
            string 正 = 看看(run, "正" + i + "|");
            string 反 = 看看(run, "反" + i + "|");
            if (正.Length == 0 || 反.Length == 0) continue;
            if (正 != 反) 错.Add($"      第 {i} 条边：0 能到 1 吗 = {正}，1 能到 0 吗 = {反}  ← 应当一样");
        }
        if (错.Count == 0)
            return JavaCheckResult.过("无向图的两个方向都是通的 —— 反方向那次 add 没漏");

        return JavaCheckResult.不过(
            "这张图**不是无向的**（或者有一边漏了）：\n" + string.Join("\n", 错) + "\n" +
            "      最可能的原因：只写了 `图.get(u).add(v);`，忘了反方向那一句。\n" +
            "      ⚠ 这个错很阴：**编译通过、运行正常、从 u 也能走到 v** ——\n" +
            "        只有「从 v 走不回 u」这一个症状，而连通块会因此数错。\n" +
            "      ⇒ 无向图每条边都要加两次：\n" +
            "          图.get(甲).add(乙);\n" +
            "          图.get(乙).add(甲);      // ← 这一句\n" +
            "      （有向图才只加一次 —— 那要看题目说的是哪种。）");
    }

    [Java检查说明("★ 长链不爆栈：10000 个点连成一条线也要跑得完")]
    public static JavaCheckResult 检查_长链(string 代码)
    {
        // ⚠ 核心检查点：递归 DFS 在 10000 层的链上必崩。
        var run = 跑一次(代码);
        if (run.Raw.Error != null && run.Raw.Error.Contains("StackOverflow"))
            return JavaCheckResult.不过(
                "**栈溢出**了（StackOverflowError）—— 链太长了。\n" +
                "      检查里有一条 **10000 个点** 连成的链：0 — 1 — 2 — … — 9999。\n" +
                "      递归 DFS 在这条链上要走 10000 层，每一层占一格**调用栈** ⇒ 装不下。\n" +
                "      ⇒ 图的遍历默认用**循环 + 队列**：\n" +
                "          Deque<Integer> 队 = new ArrayDeque<>();\n" +
                "          队.offer(起);  来过[起] = true;\n" +
                "          while (!队.isEmpty()) {\n" +
                "              int 当前 = 队.poll();\n" +
                "              for (int 邻居 : 图.get(当前)) { if (!来过[邻居]) { … } }\n" +
                "          }\n" +
                "      ⚠ 这和「算法对不对」无关，是**实现方式**的坑：\n" +
                "        小数据上一点问题都没有，数据一大就崩 —— 所以必须专门拿长链试一次。");
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("链首尾|", "true", "10000 个点的长链：0 能走到 9999"),
            ("链中段|", "true", "从 5000 能走到 8000"),
            ("链块|", "1", "整条链是一块（连通块数 = 1）"));
    }

    [Java检查说明("邻接表：每个点的邻居列表和预期一致")]
    public static JavaCheckResult 检查_邻接表(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("点0|", "1 2", "建图(4, {{0,1},{0,2},{1,2}})　点 0 连着 1、2"),
            ("点1|", "0 2", "点 1 连着 0、2"),
            ("点2|", "1 0", "点 2 连着 1、0　⚠ 顺序按边的加入顺序来：边 {1,2} 先加了一次 2→1，边 {2,0} 再加了一次 2→0"),
            ("点3|", "(空)", "⚠ 点 3 谁也不连 —— 它是**孤立点**，但仍然是「一块」（连通块要算上它）"),
            ("点数|", "7", "图里要有 7 个点（哪怕有的点是孤立的）"),
            ("空图点|", "0", "建图(0, {})　0 个点"),
            ("自环|", "自环也在", "建图(2, {{0,0},{0,1}})　自环边 {0,0} 不该把建图搞崩"));
    }

    [Java检查说明("能否到达 / 连通块数（含孤立点、多个块、多重边）")]
    public static JavaCheckResult 检查_遍历(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("可达同|", "true", "能否到达(甲, 3, 3)　起点就是终点 ⇒ true"),
            ("可达跨|", "false", "能否到达(甲, 0, 4)　0 和 4 在不同的连通块里"),
            ("可达内|", "true", "能否到达(甲, 1, 2)　1 和 2 在同一个三角形里"),
            ("可达孤|", "false", "能否到达(甲, 3, 0)　3 是孤立点"),
            ("越界起|", "false", "能否到达(甲, -1, 0)　起点越界 ⇒ false"),
            ("越界终|", "false", "能否到达(甲, 0, 99)　终点越界 ⇒ false"),
            ("块甲|", "4", "连通块数(甲)　⚠ 边的集合是 {{0,1},{1,2},{2,0},{4,5}} ⇒ {0,1,2}、{4,5}、{3}、{6} 一共 **4** 块"),
            ("块空|", "0", "连通块数(空图)　0 个点 ⇒ 0"),
            ("块单|", "1", "连通块数(1 个点、没有边)　孤立点自己算一块"),
            ("块全孤|", "5", "连通块数(5 个点、没有边)　5 块"),
            ("块重边|", "2", "连通块数(3 个点，边是 0-1、1-0、0-1 三条重复的)　⇒ {0,1} 一块、{2} 一块 = 2"));
    }

    // ══════════════════ 工具 ══════════════════

    private const string 调用点 = """
        int[][] 甲边 = {{0, 1}, {1, 2}, {2, 0}, {4, 5}};
        List<List<Integer>> 甲 = 建图(7, 甲边);
        System.out.println("点0|" + 表(甲.get(0)));
        System.out.println("点1|" + 表(甲.get(1)));
        System.out.println("点2|" + 表(甲.get(2)));
        System.out.println("点3|" + 表(甲.get(3)));
        System.out.println("点数|" + 甲.size());
        System.out.println("空图点|" + 建图(0, new int[][]{}).size());
        List<List<Integer>> 自环图 = 建图(2, new int[][]{{0, 0}, {0, 1}});
        System.out.println("自环|" + (自环图.size() == 2 ? "自环也在" : "崩了"));

        System.out.println("正1|" + 能否到达(甲, 0, 1));
        System.out.println("反1|" + 能否到达(甲, 1, 0));
        System.out.println("正2|" + 能否到达(甲, 4, 5));
        System.out.println("反2|" + 能否到达(甲, 5, 4));
        System.out.println("正3|" + 能否到达(甲, 0, 2));
        System.out.println("反3|" + 能否到达(甲, 2, 0));
        System.out.println("正4|" + 能否到达(甲, 2, 1));
        System.out.println("反4|" + 能否到达(甲, 1, 2));

        System.out.println("可达同|" + 能否到达(甲, 3, 3));
        System.out.println("可达跨|" + 能否到达(甲, 0, 4));
        System.out.println("可达内|" + 能否到达(甲, 1, 2));
        System.out.println("可达孤|" + 能否到达(甲, 3, 0));
        System.out.println("越界起|" + 能否到达(甲, -1, 0));
        System.out.println("越界终|" + 能否到达(甲, 0, 99));
        System.out.println("块甲|" + 连通块数(甲));
        System.out.println("块空|" + 连通块数(建图(0, new int[][]{})));
        System.out.println("块单|" + 连通块数(建图(1, new int[][]{})));
        System.out.println("块全孤|" + 连通块数(建图(5, new int[][]{})));
        System.out.println("块重边|" + 连通块数(建图(3, new int[][]{{0, 1}, {1, 0}, {0, 1}})));

        int 链长 = 10000;
        int[][] 链边 = new int[链长 - 1][2];
        for (int i = 0; i < 链长 - 1; i++) {
            链边[i][0] = i;
            链边[i][1] = i + 1;
        }
        List<List<Integer>> 链 = 建图(链长, 链边);
        System.out.println("链首尾|" + 能否到达(链, 0, 链长 - 1));
        System.out.println("链中段|" + 能否到达(链, 5000, 8000));
        System.out.println("链块|" + 连通块数(链));
""";

    /// <summary>调用点里的小工具：把邻接表的一行拼成字符串（空表显示「(空)」）。</summary>
    private const string 表方法 = """
    static String 表(List<Integer> 邻居) {
        if (邻居 == null || 邻居.isEmpty()) return "(空)";
        StringBuilder 串 = new StringBuilder();
        for (int i = 0; i < 邻居.size(); i++) {
            if (i > 0) 串.append(" ");
            串.append(邻居.get(i));
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
        var 包 = JavaCheck.编译并运行(代码 + "\n" + 表方法, 骨架, 调用点);
        if (包.Raw.CompileFailed && 包.编译信息.Contains("已在类"))
            包 = JavaCheck.编译并运行(代码, 骨架, 调用点);   /* 学生自己写了 表 的话退回 */
        上次的运行 = 包.编译通过 ? 包 : null;
        return 包;
    }

    private static string 先别急着比(JavaRun run)
    {
        if (run.Raw.CompileFailed)
        {
            string 信息 = run.编译信息;
            if (信息.Contains("IndexOutOfBounds") || 信息.Contains("索引"))
                return "编译没过的信息里出现了下标相关 —— 不过更可能是**运行时**才越界的。";
            return JavaCheck.编译失败说明(run).Detail;
        }
        if (run.Raw.Error != null)
        {
            if (run.Raw.Error.Contains("IndexOutOfBounds"))
                return "下标越界了（IndexOutOfBoundsException）。\n" +
                       "      这一课最经典的原因是**建图时忘了先给每个点建空表**：\n" +
                       "          List<List<Integer>> 图 = new ArrayList<>();\n" +
                       "          for (int i = 0; i < 点数; i++) 图.add(new ArrayList<>());   // ← 漏了这句\n" +
                       "          …\n" +
                       "          图.get(0).add(1);      // 空表里没有第 0 格 ⇒ 越界\n" +
                       "      另一处：点的编号可能 >= 点数（越界的边要跳过）。";
            if (run.Raw.Error.Contains("NullPointer"))
                return "空指针了（NullPointerException）——\n" +
                       "      多半是 建图 返回了 null，后面 图.get(...) 就崩了。\n" +
                       "      ⚠ 任何情况下都要返回一个**真的 List**（哪怕是空的）。";
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
            else if (!一样(得到, 期望))
                错.Add("      " + 说明 + "：得到「" + 得到 + "」，应该是「" + 期望 + "」");
        }
        if (错.Count == 0) return JavaCheckResult.过($"这几组都对（{用例们.Length} 组）");

        return JavaCheckResult.不过(
            "结果不对：\n" + string.Join("\n", 错) + "\n" +
            "      图的三个要点：\n" +
            "        建图   ① 先给每个点 `图.add(new ArrayList<>());`\n" +
            "               ② 无向图每条边**两个方向都要 add**\n" +
            "        遍历   队列 + visited；邻居靠 `for (int 邻居 : 图.get(点))` 拿\n" +
            "        连通块 每个没来过的点起一次搜索，数一共起了几次");
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
}
