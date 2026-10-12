// 第 40 课：最短路 —— Dijkstra 与优先队列
//
// ══ 这一课要教什么 ══
// 第 35 课在网格上求"最少步数"，用的是 BFS —— 因为**每一步的代价都一样**（都算 1 步）。
// 一旦边上有了**权重**（路程、时间、花费），BFS 就不管用了：
// 三步的近路可能比一步的远路还便宜。
//
// Dijkstra 算法解决的就是这个：**边权非负**的图上求单源最短路。
// 它的想法出奇地简单：
//
//     ① 起点距离 0，别的点距离"无穷大"
//     ② 每次从**还没确定的点**里挑出**当前距离最小**的那个，宣布它的距离已定
//     ③ 用它去**松弛**它的邻居：如果"绕它走"更近，就更新邻居的距离
//     ④ 重复到所有点都确定（或者队列空）
//
// 朴素实现第 ② 步要扫一遍所有点 ⇒ O(V²)。
// 用**优先队列**（小顶堆）来挑最小的那个 ⇒ O((V+E) log V)。
// 这一课的重点就是这一处替换 —— 而它正是"数据结构让算法变快"的最好例子。
//
// ⚠ Dijkstra 有一条硬前提：**边权不能是负数**。
//   这一课的所有边权都是正数（题目这么规定）。负权图要用 Bellman-Ford 之类，
//   不在这门课的范围内 —— 但你要知道 Dijkstra 在这里会给出错的答案。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点一：**必须用优先队列**。
//   朴素 Dijkstra（每次扫描所有点找最小）在小图上结果一模一样，
//   只有规模上去才看得出来。抓法有两层：
//     ① 文本：`最短距离` 的方法体里必须出现 `PriorityQueue`
//     ② **数据**：喂一张 **50000 个点、约 150000 条边**的稀疏图。
//        优先队列版 < 2 秒；O(V²) 版是 2.5e9 次操作 ⇒ 撞 60 秒超时。
// 核心检查点二：**松弛的条件必须是"更近才更新"**（`新距离 < 距离[邻居]`）。
//   写成 `<=` 会把已确定的点反复入队（不是错，但队列会膨胀）；
//   忘了比较、无条件覆盖则会把距离改大 —— 检查里的两组找零式用例能抓出来。
// 第三条：**`路径` 要从终点回溯**（记录每个点是从谁走过来的），
//   而且**返回值要包含起点和终点**；走不到返回空串。
//   检查里比的是完整路径串（如 `0 1 2 3`），顺序错了立刻现形。
//
// ⚠ 学生只写方法；骨架预置 import java.util.*。

using System.Collections.Generic;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("40", "第 40 课：最短路 —— Dijkstra 与优先队列",
    "1. 写 static List<List<int[]>> 建带权图(int 点数, int[][] 边)：边是 {u, v, 权重}，无向图**两个方向都要加**\n" +
    "2. 写 static long 最短距离(List<List<int[]>> 图, int 起, int 终)：⚠ 必须用 PriorityQueue；走不到返回 -1\n" +
    "3. 写 static String 路径(List<List<int[]>> 图, int 起, int 终)：形如 0 1 2 3；走不到返回 空\n" +
    "4. ⚠ 检查里有一张 5 万点、15 万条边的大图 —— 每次都扫一遍所有点的写法会超时")]
public static class 第40课
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
// 第 40 课：最短路 —— Dijkstra 与优先队列
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**边上有了代价之后，"最近"要靠优先队列来挑**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    Dijkstra 最短路；优先队列（PriorityQueue）；松弛
//
//  需要先会
//    第 39 课：图的邻接表、BFS
//    第 35 课：网格上的 BFS 最短路（边权全是 1 的特例）
//
// ─────────────────────────────────────────────────────
//  先介绍：边上有了"代价"，BFS 就不够用了
// ─────────────────────────────────────────────────────
//
// 第 35 课在网格上求最少步数，用 BFS —— 因为每一步的代价都是 1，
// "先碰到的就是最近的"。
//
// 可真实的地图不是这样：每条路的长度不一样。
//
//        0 ---10--- 1
//        |          |
//        1          1
//        |          |
//        2 ---1---- 3
//
// 从 0 到 3：走 0→2→3 要 1+1 = **2**；走 0→1→3 要 10+1 = 11。
// BFS 会先碰到 0→1（只隔一条边），于是给出错误答案。
//
// ⇒ 边上带权重时，要用 **Dijkstra**。
//
// ─────────────────────────────────────────────────────
//  一、Dijkstra 的四步
// ─────────────────────────────────────────────────────
//
//     ① 起点距离 = 0，其他所有点距离 = "无穷大"
//     ② 每次从**还没确定的**点里挑出**当前距离最小的**那个，它的距离就此确定
//     ③ **松弛**它的每个邻居：从它绕过去会不会更近？
//            新距离 = 距离[它] + 这条边的权重
//            if (新距离 < 距离[邻居]) 距离[邻居] = 新距离;
//     ④ 重复 ② ③，直到所有点都确定
//
// ⚠ 为什么"挑出距离最小的那个，它就定了"？
//   因为**边权都是非负的** —— 想再绕一圈回来只会更远，不可能更近。
//   这一条正是 Dijkstra 的硬前提：**有负权边时它会给出错的答案**。
//
// ─────────────────────────────────────────────────────
//  二、朴素版为什么慢，优先队列怎么救
// ─────────────────────────────────────────────────────
//
// 第 ② 步"挑出距离最小的点"，最直白的写法是**扫一遍所有点**：
//
//     for (int i = 0; i < 点数; i++) if (!确定[i] && 距离[i] < 最小) { 最小 = 距离[i]; 选中 = i; }
//
// 每次 O(V)，一共挑 V 次 ⇒ **O(V²)**。
//   V = 1000 时是 100 万次，还行；V = 5 万时是 25 亿次 —— 跑不完。
//
// 换成**优先队列**（小顶堆）：把"候选（距离, 点）"丢进去，
// 每次 `poll()` 拿最小的，只要 O(log V)。
// 总代价 **O((V + E) log V)** —— 5 万点、15 万条边也就几百万次操作。
//
//     PriorityQueue<long[]> 堆 = new PriorityQueue<>((a, b) -> Long.compare(a[0], b[0]));
//     堆.offer(new long[]{0, 起});                       ← {距离, 点}
//     while (!堆.isEmpty()) {
//         long[] 当前 = 堆.poll();
//         long 距离 = 当前[0];
//         int 点 = (int) 当前[1];
//         if (距离 > 最短[点]) continue;                  ← ⚠ 过期的条目，跳过
//         // 松弛邻居…
//     }
//
// ⚠ 三处要点：
//   · 优先队列里存的是 `long[]{距离, 点}`，**别的格子是 long**，
//     取出来时下标要 `(int) 当前[1]` 转一下。
//   · 比较器要写 `Long.compare(a[0], b[0])` —— **不要写 `a[0] - b[0]`**：
//     那在 long 上会溢出（第 20 课那个病），距离一大排序就乱了。
//   · **`if (距离 > 最短[点]) continue;` 这一句不能省**：
//     同一个点可能被入队好几次（每次距离变近就入一次），
//     堆里会留着**过期的旧距离**。跳过它们是这个算法的常规做法
//     （这叫"惰性删除"，比在堆里真删掉简单得多）。
//
// ─────────────────────────────────────────────────────
//  三、怎么把"路径"也求出来
// ─────────────────────────────────────────────────────
//
// 距离算出来了，可"怎么走"还得单独记：**每个点是"从谁那儿过来的"**。
//
//     int[] 前驱 = new int[点数];
//     Arrays.fill(前驱, -1);
//     …松弛成功时： 前驱[邻居] = 点;
//
// 最后从终点一路回溯到起点（再翻过来）：
//
//     List<String> 倒着 = new ArrayList<>();
//     for (int 走 = 终; 走 != -1; 走 = 前驱[走]) {
//         倒着.add("" + 走);
//         if (走 == 起) break;                 ← ⚠ 别忘了起点也要放进去、也要停
//     }
//     // 再反过来拼成 "0 1 2 3"
//
// ⚠ 路径题最容易错的地方：**返回值要不要含起点和终点**。
//   这一课规定**都要含**（从起点出发、到终点结束），空图/走不到返回 `空`。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：忘记用优先队列（每次都扫一遍所有点）
// ─────────────────────────────────────────────────────
//
//     static long 最短距离(List<List<int[]>> 图, int 起, int 终) {
//         …
//         for (int 轮 = 0; 轮 < 点数; 轮++) {
//             int 选中 = -1;
//             for (int i = 0; i < 点数; i++) {                 ← 每次扫一遍所有点
//                 if (!确定[i] && (选中 == -1 || 距离[i] < 距离[选中])) 选中 = i;
//             }
//             …
//         }
//     }
//
// 你会看到：**小图上完全正确**（我本地拿 6 个点的图试过，答案一字不差）——
//   所以你怎么测都测不出问题。
//
// 但它是 **O(V²)**：检查里那张图有 **5 万个点**，
//   5 万 × 5 万 = **25 亿次**操作 ⇒ 撞超时。
//
// ⇒ 把"挑最小的"那一步交给优先队列。这也是这一课真正的收获：
//   **同一个算法，换一个数据结构，量级就变了。**
//
//   （这一课的正题其实是"优先队列"而不是 Dijkstra 本身 ——
//     Dijkstra 的思路你多半已经猜到了，难的是"怎么高效地挑最小"。）
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
//   负权边（Bellman-Ford / SPFA）→ 不涉及。
//   所有点对之间的最短路（Floyd）→ 不涉及。
//   A* 启发式搜索 → 不涉及。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 你**只写方法**：不用写 class，不用写 main；`java.util.*` 已经 import 好了。
//    每个方法都要带 **static**。
//
//   1. static List<List<int[]>> 建带权图(int 点数, int[][] 边)
//        每条边是 `{u, v, 权重}`。**无向图：两个方向都要加**。
//        邻接表每格里存 `int[]{邻居, 权重}`。
//
//   2. static long 最短距离(List<List<int[]>> 图, int 起, int 终)
//        ⚠ **必须用 PriorityQueue**（检查会切开方法体看）。
//        走不到返回 -1；起点就是终点返回 0。
//
//   3. static String 路径(List<List<int[]>> 图, int 起, int 终)
//        形如 `0 1 2 3`（**含起点和终点**，空格分隔）。走不到返回 `空`。
//
//  ⚠ 检查里有一张 **5 万点、约 15 万条边**的大图 ——
//    "每次都扫一遍所有点"的写法在那里会超时。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
    static List<List<int[]>> 建带权图(int 点数, int[][] 边) {
        return null;
    }

    static long 最短距离(List<List<int[]>> 图, int 起, int 终) {
        return -1;
    }

    static String 路径(List<List<int[]>> 图, int 起, int 终) {
        return "";
    }
""";

    public static string 参考答案 => """
// 参考答案 —— Dijkstra + 优先队列（只写方法，不用写 class / main / import）

// 1. 建带权图：邻接表每格存 {邻居, 权重}。无向图两个方向都要加
static List<List<int[]>> 建带权图(int 点数, int[][] 边) {
    List<List<int[]>> 图 = new ArrayList<>();
    for (int i = 0; i < 点数; i++) {
        图.add(new ArrayList<>());         /* ⚠ 先给每个点建空表 */
    }
    for (int i = 0; i < 边.length; i++) {
        int 甲 = 边[i][0];
        int 乙 = 边[i][1];
        int 权 = 边[i][2];
        if (甲 < 0 || 甲 >= 点数 || 乙 < 0 || 乙 >= 点数) continue;
        图.get(甲).add(new int[]{乙, 权});
        图.get(乙).add(new int[]{甲, 权});   /* ⚠ 无向图：反方向也要 */
    }
    return 图;
}

// 2. Dijkstra 求距离：优先队列挑"当前距离最小的点"
static long 最短距离(List<List<int[]>> 图, int 起, int 终) {
    if (图 == null || 图.isEmpty()) return -1;
    int 点数 = 图.size();
    if (起 < 0 || 起 >= 点数 || 终 < 0 || 终 >= 点数) return -1;
    if (起 == 终) return 0;

    long 无穷 = Long.MAX_VALUE / 4;         /* 别用 MAX_VALUE：加权重时会溢出 */
    long[] 最短 = new long[点数];
    Arrays.fill(最短, 无穷);
    最短[起] = 0;

    /* ⚠ 比较器写 Long.compare，不要写 a[0] - b[0]（long 相减会溢出） */
    PriorityQueue<long[]> 堆 = new PriorityQueue<>((a, b) -> Long.compare(a[0], b[0]));
    堆.offer(new long[]{0, 起});
    while (!堆.isEmpty()) {
        long[] 当前 = 堆.poll();
        long 距离 = 当前[0];
        int 点 = (int) 当前[1];
        if (距离 > 最短[点]) continue;       /* ⚠ 堆里的过期条目，跳过 */
        if (点 == 终) return 距离;            /* 第一个被取出来的终点就是最短的 */

        for (int[] 一条 : 图.get(点)) {
            int 邻居 = 一条[0];
            long 新距离 = 距离 + 一条[1];
            if (新距离 < 最短[邻居]) {        /* ⚠ 只有更近才更新 —— 这就是"松弛" */
                最短[邻居] = 新距离;
                堆.offer(new long[]{新距离, 邻居});
            }
        }
    }
    return 最短[终] == 无穷 ? -1 : 最短[终];
}

// 3. 路径：松弛成功时顺手记下"从谁过来的"，最后从终点回溯
static String 路径(List<List<int[]>> 图, int 起, int 终) {
    if (图 == null || 图.isEmpty()) return "空";
    int 点数 = 图.size();
    if (起 < 0 || 起 >= 点数 || 终 < 0 || 终 >= 点数) return "空";
    if (起 == 终) return "" + 起;

    long 无穷 = Long.MAX_VALUE / 4;
    long[] 最短 = new long[点数];
    int[] 前驱 = new int[点数];
    Arrays.fill(最短, 无穷);
    Arrays.fill(前驱, -1);
    最短[起] = 0;

    PriorityQueue<long[]> 堆 = new PriorityQueue<>((a, b) -> Long.compare(a[0], b[0]));
    堆.offer(new long[]{0, 起});
    while (!堆.isEmpty()) {
        long[] 当前 = 堆.poll();
        long 距离 = 当前[0];
        int 点 = (int) 当前[1];
        if (距离 > 最短[点]) continue;
        for (int[] 一条 : 图.get(点)) {
            int 邻居 = 一条[0];
            long 新距离 = 距离 + 一条[1];
            if (新距离 < 最短[邻居]) {
                最短[邻居] = 新距离;
                前驱[邻居] = 点;              /* ⚠ 记下是从谁过来的 */
                堆.offer(new long[]{新距离, 邻居});
            }
        }
    }
    if (最短[终] == 无穷) return "空";

    /* 从终点一路往起点回溯，再翻过来 */
    List<String> 倒着 = new ArrayList<>();
    int 走 = 终;
    while (走 != -1) {
        倒着.add("" + 走);
        if (走 == 起) break;                 /* ⚠ 到起点就停（起点也要放进路径里） */
        走 = 前驱[走];
    }
    StringBuilder 串 = new StringBuilder();
    for (int i = 倒着.size() - 1; i >= 0; i--) {
        if (串.length() > 0) 串.append(" ");
        串.append(倒着.get(i));
    }
    return 串.toString();
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("三个方法都在，签名也对")]
    public static JavaCheckResult 检查_函数都在(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        if (!Regex.IsMatch(净, @"static\s+List\s*<\s*List\s*<\s*int\s*\[\s*\]\s*>\s*>\s+建带权图\s*\(\s*int\s+\w+\s*,\s*int\s*\[\s*\]\s*\[\s*\]\s*\w+\s*\)"))
            缺.Add("      建带权图（static List<List<int[]>> 建带权图(int 点数, int[][] 边)）");
        if (!Regex.IsMatch(净, @"static\s+long\s+最短距离\s*\(\s*List\s*<\s*List\s*<\s*int\s*\[\s*\]\s*>\s*>\s+\w+\s*,\s*int\s+\w+\s*,\s*int\s+\w+\s*\)"))
            缺.Add("      最短距离（static long 最短距离(List<List<int[]>> 图, int 起, int 终)）");
        if (!Regex.IsMatch(净, @"static\s+String\s+路径\s*\(\s*List\s*<\s*List\s*<\s*int\s*\[\s*\]\s*>\s*>\s+\w+\s*,\s*int\s+\w+\s*,\s*int\s+\w+\s*\)"))
            缺.Add("      路径　　（static String 路径(List<List<int[]>> 图, int 起, int 终)）");

        if (缺.Count == 0) return JavaCheckResult.过("三个方法都在，签名也对");
        return JavaCheckResult.不过(
            "有方法没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      ⚠ 邻接表的类型是 **List<List<int[]>>** —— 每个邻居存成 {邻居, 权重} 两个数。\n" +
            "      ⚠ 最短距离 返回 **long**（路径长度可能很大）。");
    }

    [Java检查说明("★ 最短距离用了 PriorityQueue")]
    public static JavaCheckResult 检查_用了优先队列(string 代码)
    {
        // ⚠ 核心检查点之一。
        string 净 = 去掉注释(代码);
        string 体 = 函数体(净, "最短距离");
        if (体 == null)
            return JavaCheckResult.不过("没找到 最短距离 的函数体 —— 先确认这个方法写全了（花括号配对）。");

        if (!体.Contains("PriorityQueue"))
            return JavaCheckResult.不过(
                "最短距离 里没有 **PriorityQueue** —— 这一课的正题就是它。\n" +
                "      Dijkstra 的每一步都要「从还没确定的点里挑出**当前距离最小**的那个」。\n" +
                "      最直白的写法是扫一遍所有点，但那是 **O(V²)**：\n" +
                "        检查里那张大图有 5 万个点 ⇒ 5万 × 5万 = 25 亿次 ⇒ 跑不完。\n" +
                "      ⇒ 把「挑最小的」交给优先队列（小顶堆）：\n" +
                "          PriorityQueue<long[]> 堆 = new PriorityQueue<>((a, b) -> Long.compare(a[0], b[0]));\n" +
                "          堆.offer(new long[]{0, 起});                  // {距离, 点}\n" +
                "          while (!堆.isEmpty()) {\n" +
                "              long[] 当前 = 堆.poll();\n" +
                "              if (当前[0] > 最短[点]) continue;          // ⚠ 过期的条目\n" +
                "              …松弛邻居，变近了就把新的 (距离, 点) 入堆…\n" +
                "          }\n" +
                "      ⚠ 比较器别写 `a[0] - b[0]` —— long 相减会溢出，排序就乱了。");

        return JavaCheckResult.过("最短距离 用的是 PriorityQueue");
    }

    [Java检查说明("★ 小图：最短路结果全对（边权不同，BFS 会答错的那种）")]
    public static JavaCheckResult 检查_小图(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("同点|", "0", "最短距离(甲, 0, 0)　起点就是终点 ⇒ 0"),
            ("经典|", "2", "⚠ 最短距离(甲, 0, 3)　甲 里 0→2→3 只要 1+1 = 2，而 0→1→3 要 10+1 = 11 ——\n" +
                "            BFS 会先碰到 0→1（只隔一条边）然后给出错误答案，这就是要用 Dijkstra 的原因"),
            ("绕远|", "4", "最短距离(甲, 0, 5)　0→2→3→4→5 代价 1+1+1+1 = 4"),
            ("单向难|", "5", "最短距离(甲, 0, 7)　0→2→3→1→6→7 代价 1+1+1+1+1 = 5"),
            ("不可达|", "-1", "最短距离(甲, 0, 100)　编号越界 ⇒ -1"),
            ("孤立|", "-1", "最短距离(乙, 0, 4)　乙 里 4 是孤立点 ⇒ -1"),
            ("乙同|", "0", "最短距离(乙, 4, 4)　孤立点到它自己 ⇒ 0"),
            ("乙路|", "4", "最短距离(乙, 0, 3)　乙 是链 0-1-2-3-4，边权 1,2,1 ⇒ 0→3 是 1+2+1 = 4"),
            ("丙权|", "4", "⚠ 最短距离(丙, 0, 2)　直连的边权是 7，但绕一下 0→1→2 只要 3+1 = **4** ——\n" +
                "            这就是「最短」不等于「直连」最直白的一个例子"),
            ("丙小|", "3", "最短距离(丙, 0, 1)"));
    }

    [Java检查说明("★ 路径：含起点和终点、顺序对、走不到返回 空")]
    public static JavaCheckResult 检查_路径(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("路同|", "0", "路径(甲, 0, 0)　起点就是终点 ⇒ 只有起点"),
            ("路经典|", "0 2 3", "⚠ 路径(甲, 0, 3)　最短路的走法是 0→2→3（代价 2）；**含起点和终点**、用空格分隔"),
            ("路5|", "0 2 3 4 5", "路径(甲, 0, 5)"),
            ("路7|", "0 2 3 1 6 7", "路径(甲, 0, 7)　⚠ 含起点 0 和终点 7，一共 6 个点"),
            ("路不可|", "空", "路径(甲, 0, 100)　编号越界 ⇒ 空"),
            ("路孤|", "空", "路径(乙, 0, 4)　走不到 ⇒ 空"));
    }

    [Java检查说明("★ 大图：5 万点、15 万条边（每次都扫所有点的写法会超时）")]
    public static JavaCheckResult 检查_大图(string 代码)
    {
        var run = 跑一次(代码);
        if (run.Raw.Error != null && run.Raw.Error.Contains("超时"))
            return JavaCheckResult.不过(
                "跑大图的时候**超时**了。\n" +
                "      检查里那张图有 **5 万个点、约 15 万条边**。\n" +
                "      如果每次挑最小点都扫一遍所有点，那是 5万 × 5万 = **25 亿次**操作。\n" +
                "      ⇒ 用 **PriorityQueue** 挑最小的那个（O(log V)）：\n" +
                "          PriorityQueue<long[]> 堆 = new PriorityQueue<>((a, b) -> Long.compare(a[0], b[0]));\n" +
                "          堆.offer(new long[]{0, 起});\n" +
                "          while (!堆.isEmpty()) {\n" +
                "              long[] 当前 = 堆.poll();\n" +
                "              int 点 = (int) 当前[1];\n" +
                "              if (当前[0] > 最短[点]) continue;\n" +
                "              for (int[] 一条 : 图.get(点)) { …松弛… }\n" +
                "          }\n" +
                "      ⚠ 同一个算法（Dijkstra），换个数据结构就从「跑不完」变成「一秒内」 ——\n" +
                "        这正是这一课最想让你看见的东西。");
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);

        long 差距 = 长整数(run, "大差距|");
        if (差距 != 0)
            return JavaCheckResult.不过(
                $"大图上的最短距离算错了：你给的和正确值差 {差距}（得到 {看看(run, "大距离|")}，应该是 {看看(run, "大参照|")}）。\n" +
                "      5 万个点、15 万条边的图，优先队列版应当一秒内出答案。\n" +
                "      ⚠ 距离要用 **long**：路径长度可能超过 int。");

        return 比(run,
            ("大距离|", "12", "⚠ 5 万点、10 万条边的大图，从 0 到 49999 的最短距离"),
            ("大差距|", "0", "和课程自己用另一种方式算出来的值比较 —— 必须一致"),
            ("大路径首|", "0", "大图上的路径也要从起点 0 开始"),
            ("大路径长|", "true", "路径串不为空（大图上是可达的）"));
    }

    // ══════════════════ 工具 ══════════════════

    /// <summary>课程自带的一份参考实现（只给检查用，学生不用写）—— 拿它当标准答案。</summary>
    private const string 参照方法 = """
    static long 参照最短路(int[][] 边, int 点数, int 起, int 终) {
        List<List<int[]>> 图 = new ArrayList<>();
        for (int i = 0; i < 点数; i++) 图.add(new ArrayList<>());
        for (int i = 0; i < 边.length; i++) {
            图.get(边[i][0]).add(new int[]{边[i][1], 边[i][2]});
            图.get(边[i][1]).add(new int[]{边[i][0], 边[i][2]});
        }
        long 无穷 = Long.MAX_VALUE / 4;
        long[] 最短 = new long[点数];
        Arrays.fill(最短, 无穷);
        最短[起] = 0;
        PriorityQueue<long[]> 堆 = new PriorityQueue<>((a, b) -> Long.compare(a[0], b[0]));
        堆.offer(new long[]{0, 起});
        while (!堆.isEmpty()) {
            long[] 当前 = 堆.poll();
            long 距离 = 当前[0];
            int 点 = (int) 当前[1];
            if (距离 > 最短[点]) continue;
            for (int[] 一条 : 图.get(点)) {
                long 新 = 距离 + 一条[1];
                if (新 < 最短[一条[0]]) {
                    最短[一条[0]] = 新;
                    堆.offer(new long[]{新, 一条[0]});
                }
            }
        }
        return 最短[终] == 无穷 ? -1 : 最短[终];
    }

""";

    private const string 调用点 = """
        int[][] 甲边 = {{0, 1, 10}, {0, 2, 1}, {1, 6, 1}, {2, 3, 1}, {3, 4, 1}, {3, 1, 1}, {4, 5, 1}, {6, 7, 1}};
        List<List<int[]>> 甲 = 建带权图(10, 甲边);
        System.out.println("同点|" + 最短距离(甲, 0, 0));
        System.out.println("经典|" + 最短距离(甲, 0, 3));
        System.out.println("绕远|" + 最短距离(甲, 0, 5));
        System.out.println("单向难|" + 最短距离(甲, 0, 7));
        System.out.println("不可达|" + 最短距离(甲, 0, 100));
        System.out.println("路同|" + 路径(甲, 0, 0));
        System.out.println("路经典|" + 路径(甲, 0, 3));
        System.out.println("路5|" + 路径(甲, 0, 5));
        System.out.println("路7|" + 路径(甲, 0, 7));
        System.out.println("路不可|" + 路径(甲, 0, 100));

        int[][] 乙边 = {{0, 1, 1}, {1, 2, 2}, {2, 3, 1}};
        List<List<int[]>> 乙 = 建带权图(5, 乙边);
        System.out.println("孤立|" + 最短距离(乙, 0, 4));
        System.out.println("乙同|" + 最短距离(乙, 4, 4));
        System.out.println("乙路|" + 最短距离(乙, 0, 3));
        System.out.println("路孤|" + 路径(乙, 0, 4));

        int[][] 丙边 = {{0, 1, 3}, {0, 2, 7}, {1, 2, 1}};
        List<List<int[]>> 丙 = 建带权图(3, 丙边);
        System.out.println("丙权|" + 最短距离(丙, 0, 2));
        System.out.println("丙小|" + 最短距离(丙, 0, 1));

        int 大点数 = 50000;
        int[][] 大边 = new int[150000][3];
        int 写 = 0;
        for (int i = 0; i + 1 < 大点数 && 写 < 大边.length; i++) {
            大边[写][0] = i;
            大边[写][1] = i + 1;
            大边[写][2] = 1;
            写++;
        }
        for (int i = 0; i < 大点数 && 写 < 大边.length; i++) {
            int 甲点 = i;
            int 乙点 = (i * 7919 + 13) % 大点数;
            if (甲点 == 乙点) continue;
            大边[写][0] = 甲点;
            大边[写][1] = 乙点;
            大边[写][2] = (i % 5) + 1;
            写++;
        }
        int[][] 大边实 = Arrays.copyOf(大边, 写);
        List<List<int[]>> 大图 = 建带权图(大点数, 大边实);
        long 大距离 = 最短距离(大图, 0, 大点数 - 1);
        long 大参照 = 参照最短路(大边实, 大点数, 0, 大点数 - 1);   /* 课程自己算一份当标准答案 */
        System.out.println("大距离|" + 大距离);
        System.out.println("大参照|" + 大参照);
        System.out.println("大差距|" + (大距离 - 大参照));
        String 大路 = 路径(大图, 0, 大点数 - 1);
        String[] 大路段 = 大路.split(" ");
        System.out.println("大路径首|" + (大路段.length > 0 ? 大路段[0] : ""));
        System.out.println("大路径长|" + (大路.length() > 0));
""";

    private static string 上次的代码 = "";
    private static JavaRun 上次的运行;

    private static JavaRun 跑一次(string 代码)
    {
        if (上次的运行 != null && 上次的代码 == 代码) return 上次的运行;
        上次的代码 = 代码;
        var 包 = JavaCheck.编译并运行(代码 + "\n" + 参照方法, 骨架, 调用点);
        if (包.Raw.CompileFailed && 包.编译信息.Contains("已在类"))
            包 = JavaCheck.编译并运行(代码, 骨架, 调用点);
        上次的运行 = 包.编译通过 ? 包 : null;
        return 包;
    }

    private static string 先别急着比(JavaRun run)
    {
        if (run.Raw.CompileFailed) return JavaCheck.编译失败说明(run).Detail;
        if (run.Raw.Error != null)
        {
            if (run.Raw.Error.Contains("IndexOutOfBounds"))
                return "下标越界了：\n      " + run.Raw.Error + "\n" +
                       "      这一课常见的两处：\n" +
                       "        · 建带权图 忘了先 `图.add(new ArrayList<>());`\n" +
                       "        · 前驱回溯时没判 -1（`走 = 前驱[走]` 走成了 -1 还去取下标）";
            if (run.Raw.Error.Contains("NullPointer"))
                return "空指针了（NullPointerException）——\n" +
                       "      多半是 建带权图 返回了 null，或者前驱表忘了初始化。\n" +
                       "      ⚠ 前驱表要 `Arrays.fill(前驱, -1);`，否则默认 0 会让回溯走进死循环。";
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
            "      Dijkstra 的骨架：\n" +
            "          最短[起] = 0，其余 无穷（用 Long.MAX_VALUE / 4，别用 MAX_VALUE —— 加权重会溢出）\n" +
            "          PriorityQueue<long[]> 堆，offer({0, 起})\n" +
            "          while (!堆.isEmpty()) {\n" +
            "              poll 出 (距离, 点)；距离 > 最短[点] 就 continue（过期条目）\n" +
            "              for (每条边) { 新距离 = 距离 + 权重; if (新距离 < 最短[邻居]) 更新 + 入堆 + 记前驱 }\n" +
            "          }\n" +
            "      ⚠ 路径要**含起点和终点**；走不到返回 空。");
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
        return long.TryParse(s, out long v) ? v : 0;
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
