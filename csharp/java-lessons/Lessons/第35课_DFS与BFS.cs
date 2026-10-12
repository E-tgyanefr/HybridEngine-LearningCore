// 第 35 课：DFS 与 BFS —— 在迷宫上找路
//
// ══ 这一课要教什么 ══
// 前面几课处理的是"一条线"（数组）、"分叉"（树）、"互相牵着"（图，下一课）。
// 这一课先在一个**网格**上练两种最基础的搜索：
//
//     DFS（深度优先）  一条路走到黑，走不通就退回来换一条  —— 用**栈**（或者递归）
//     BFS（广度优先）  一层一层往外扩，先看近的再看远的    —— 用**队列**
//
// ⚠ 两者最重要的差别不是"写法"，而是**能回答的问题不一样**：
//   · 「能不能到达」  DFS / BFS 都行
//   · 「**最少**几步到」**只有 BFS 行** —— 因为 BFS 是按距离从近到远扩展的，
//     第一次碰到目标时走的就是最短路。DFS 找到的那条路可能绕了很远。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点一：**最短步数必须用 BFS**（队列），不能用 DFS 回溯"找一条然后比长短"。
//   DFS 也能算对（把每条路的长度都试一遍），但慢得多 —— 而且在空旷的大迷宫里
//   是指数级的。抓法有两层：
//     ① 文本：`最短步数` 的方法体里必须出现 `Queue` / `ArrayDeque` / `LinkedList`
//     ② **数据**：喂一座 **200×200 全空旷**的迷宫（起点左上、终点右下）。
//        DFS 不剪枝在这种"到处都能走"的图上会爆栈（StackOverflowError）或者超时，
//        BFS 几十毫秒就出答案 **398 步**（200+200-2）。
// 核心检查点二：**递归 DFS 在 300×300 上会爆栈**。
//   `连通块数` 那一题喂一座 300×300 的空旷迷宫：递归 DFS 要 90000 层，必崩；
//   用 BFS 队列（或者显式栈）就不会。实测：递归写法在 300×300 上报
//   StackOverflowError，迭代写法正常返回 1（整片连通）。
// 第三条：起点就是终点要返回 0、走不到要返回 -1、越界的坐标要挡住。
//
// ⚠ 学生只写方法；骨架预置 import java.util.*。

using System.Collections.Generic;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("35", "第 35 课：DFS 与 BFS —— 在迷宫上找路",
    "1. 写 static boolean 能到达(int[][] 迷宫, int 起行, int 起列, int 终行, int 终列)：0 能走、1 是墙\n" +
    "2. 写 static int 最短步数(int[][] 迷宫, int 起行, int 起列, int 终行, int 终列)：**必须用 BFS（队列）**\n" +
    "   走不到返回 -1；起点就是终点返回 0\n" +
    "3. 写 static int 连通块数(int[][] 迷宫)：数一数有几片互不相连的可走区域（上下左右四方向）\n" +
    "4. ⚠ 大迷宫上不能爆栈：200×200、300×300 都要跑得完 —— 用队列（或显式栈），别用递归")]
public static class 第35课
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
// 第 35 课：DFS 与 BFS —— 在迷宫上找路
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**DFS 和 BFS 能回答的问题不一样** ——
// 「最短路」只有 BFS 能直接答。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    DFS、BFS；用网格表示地图；visited 数组
//
//  需要先会
//    第 31 课：栈与队列（BFS 用的就是队列）
//    第 17 课：二维数组（迷宫就是一张表）
//
// ─────────────────────────────────────────────────────
//  先介绍：迷宫就是一张表格
// ─────────────────────────────────────────────────────
//
// 用 int[][] 表示地图：**0 能走、1 是墙**。比如：
//
//     {0, 0, 0, 0}
//     {1, 1, 0, 1}
//     {0, 0, 0, 0}      从 (0,0) 走到 (2,3) 要走几步？
//
// 每一步只能上下左右走一格（不能斜着走），不能穿墙。
//
// 这类问题一律是同一个套路：
//
//     ① 一个**方向数组**：上、下、左、右
//     ② 一个 **visited 表**：这一步我来过没有（防绕圈）
//     ③ 一个**待处理的容器**：栈（深搜）或者队列（广搜）
//
//          int[] 行方向 = {-1, 1, 0, 0};
//          int[] 列方向 = {0, 0, -1, 1};
//          for (int d = 0; d < 4; d++) {
//              int 新行 = 行 + 行方向[d];
//              int 新列 = 列 + 列方向[d];
//              // 然后检查 新行/新列 有没有越界、是不是墙、来没来过
//          }
//
// ⚠ 方向数组的好处是**四个方向一次写完**。手写四段 if 当然也行，
//   但漏掉一个方向、或者把行列写反，那种错很难看出来。
//
// ─────────────────────────────────────────────────────
//  一、visited：为什么必须有它
// ─────────────────────────────────────────────────────
//
// 没有 visited 的话，你从 (0,0) 走到 (0,1)，下一步又会走回 (0,0) ——
// 来回横跳，永远出不来。
//
//     boolean[][] 来过 = new boolean[行数][列数];
//
// ⚠ 「标记来过」的时机很讲究：
//   · **一进队列就标记**（BFS 里推荐这样）—— 同一个格子不会被重复入队
//   · 出队时才标记 —— 一个格子可能被入队好几次，队列白白变长（运气差会超内存）
//
// ─────────────────────────────────────────────────────
//  二、BFS：一层一层往外扩（用队列）
// ─────────────────────────────────────────────────────
//
//     Deque<int[]> 队 = new ArrayDeque<>();
//     队.offer(new int[]{起行, 起列, 0});            ← 第三个是"走到这儿的步数"
//     来过[起行][起列] = true;
//     while (!队.isEmpty()) {
//         int[] 当前 = 队.poll();
//         if (当前[0] == 终行 && 当前[1] == 终列) return 当前[2];   ← 第一次碰到就是最短
//         for (四个方向) {
//             if (能走 && !来过[新行][新列]) {
//                 来过[新行][新列] = true;
//                 队.offer(new int[]{新行, 新列, 当前[2] + 1});
//             }
//         }
//     }
//     return -1;                                     ← 队列空了 ⇒ 到不了
//
// ⚠ 「步数」是**跟着队列一起传**的（存在数组的第三个格子里）。
//   也可以另开一张"距离表" `int[][] 距离`，两种都行 —— 但别忘了一件事：
//   **BFS 里每个格子只会被处理一次**，所以第一次写进去的距离就是最短距离。
//
// ─────────────────────────────────────────────────────
//  三、DFS：一条路走到黑（用栈或递归）
// ─────────────────────────────────────────────────────
//
// 递归写法最短：
//
//     static boolean 深搜(int[][] 迷宫, boolean[][] 来过, int 行, int 列,
//                         int 终行, int 终列) {
//         if (行 < 0 || 行 >= 迷宫.length || 列 < 0 || 列 >= 迷宫[0].length) return false;
//         if (迷宫[行][列] == 1 || 来过[行][列]) return false;
//         if (行 == 终行 && 列 == 终列) return true;
//         来过[行][列] = true;
//         return 深搜(…上…) || 深搜(…下…) || 深搜(…左…) || 深搜(…右…);
//     }
//
// ⚠ 递归 DFS 有一个**真实的、和输入规模有关的风险**：
//   每一层递归占一格调用栈。一座 200×200 的空旷迷宫最长路径可能有几万步 ——
//   递归深度跟着涨，涨到栈装不下就：
//
//       Exception in thread "main" java.lang.StackOverflowError
//
//   ⇒ 地图大的时候，DFS 也要写成**用显式栈的循环**（把"待处理的位置"压进
//     `Deque`，而不是压在调用栈上）。BFS 天生就是循环，没有这个问题。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：用 DFS 求"最少几步"
// ─────────────────────────────────────────────────────
//
//     static int 最短步数(int[][] 迷宫, …) {
//         // 用 DFS 找到一条路，数一数几步              ← ✗ 找到的不一定是最短的
//     }
//
// 你会看到：**小迷宫里经常能碰对**（因为只有一条路可走），
//   于是你很容易以为它是对的。换一座"好几条路都能走"的迷宫就错了。
//
// 为什么 BFS 才是对的：BFS 用**队列**，先处理"走 1 步能到的"、
//   再处理"走 2 步能到的"… 所以它是**按距离从近到远**铺开的。
//   第一次碰到终点时，你走的就是最短路 —— 这就是"最短路只有 BFS 能直接答"的意思。
//
// ⇒ 记这条对应关系：
//
//     问「能不能到」            DFS / BFS 都行
//     问「**最少**几步」        只能 BFS
//     问「所有走法」             DFS（回溯）
//     问「有几个连通块」        都行（每个没来过的格子起一次搜索）
//
//   （检查里会喂一座 200×200 的空旷迷宫：正确答案是 398 步，
//     而 DFS 那种走法在那里会绕到爆栈或者超时。）
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
//   一般图上的 DFS/BFS（邻接表）→ 第 39 课。
//   带权图的最短路（Dijkstra）→ 第 40 课。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 你**只写方法**：不用写 class，不用写 main；`java.util.*` 已经 import 好了。
//    每个方法都要带 **static**。
//
//   1. static boolean 能到达(int[][] 迷宫, int 起行, int 起列, int 终行, int 终列)
//        0 能走、1 是墙，只能上下左右走。起点或终点是墙、或者越界 ⇒ false。
//
//   2. static int 最短步数(int[][] 迷宫, int 起行, int 起列, int 终行, int 终列)
//        **必须用 BFS（队列）**。走不到返回 -1；起点就是终点返回 **0**。
//        ⚠ 检查会切开方法体，看里面有没有 Queue / ArrayDeque。
//
//   3. static int 连通块数(int[][] 迷宫)
//        数一数有几片互不相连的可走区域（上下左右四方向）。
//
//  ⚠ 检查里最大的迷宫是 **300×300** —— 递归 DFS 在那里会爆栈。
//    所以 `连通块数` 和 `最短步数` 都请写成**用队列（或显式栈）的循环**。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
    static boolean 能到达(int[][] 迷宫, int 起行, int 起列, int 终行, int 终列) {
        return false;
    }

    static int 最短步数(int[][] 迷宫, int 起行, int 起列, int 终行, int 终列) {
        return -1;
    }

    static int 连通块数(int[][] 迷宫) {
        return 0;
    }
""";

    public static string 参考答案 => """
// 参考答案 —— DFS 与 BFS（只写方法，不用写 class / main / import）
//
// 两个方法都用**队列**（BFS 思路），因为 BFS 天生是循环、不会爆栈，
// 而且能直接回答"最少几步"。方向数组四个方向一次写完。

static final int[] 行方向 = {-1, 1, 0, 0};
static final int[] 列方向 = {0, 0, -1, 1};

// 1. 能不能到达：BFS 铺开，能碰到终点就是 true
static boolean 能到达(int[][] 迷宫, int 起行, int 起列, int 终行, int 终列) {
    if (迷宫 == null || 迷宫.length == 0) return false;
    int 行数 = 迷宫.length;
    int 列数 = 迷宫[0].length;
    if (起行 < 0 || 起行 >= 行数 || 起列 < 0 || 起列 >= 列数) return false;
    if (终行 < 0 || 终行 >= 行数 || 终列 < 0 || 终列 >= 列数) return false;
    if (迷宫[起行][起列] == 1 || 迷宫[终行][终列] == 1) return false;

    boolean[][] 来过 = new boolean[行数][列数];
    Deque<int[]> 队 = new ArrayDeque<>();
    队.offer(new int[]{起行, 起列});
    来过[起行][起列] = true;                  /* ⚠ 一进队列就标记，别等出队 */
    while (!队.isEmpty()) {
        int[] 当前 = 队.poll();
        if (当前[0] == 终行 && 当前[1] == 终列) {
            return true;
        }
        for (int d = 0; d < 4; d++) {
            int 新行 = 当前[0] + 行方向[d];
            int 新列 = 当前[1] + 列方向[d];
            if (新行 < 0 || 新行 >= 行数 || 新列 < 0 || 新列 >= 列数) continue;
            if (迷宫[新行][新列] == 1 || 来过[新行][新列]) continue;
            来过[新行][新列] = true;
            队.offer(new int[]{新行, 新列});
        }
    }
    return false;                             /* 队列空了还碰不到 ⇒ 到不了 */
}

// 2. 最少几步：⚠ 只有 BFS 能直接答对
//    队列是按"距离从近到远"铺开的，所以第一次碰到终点就是最短路
static int 最短步数(int[][] 迷宫, int 起行, int 起列, int 终行, int 终列) {
    if (!能到达(迷宫, 起行, 起列, 终行, 终列)) return -1;
    if (起行 == 终行 && 起列 == 终列) return 0;

    int 行数 = 迷宫.length;
    int 列数 = 迷宫[0].length;
    int[][] 距离 = new int[行数][列数];        /* 0 表示还没去过（起点也是 0，所以起点先特判掉了） */
    boolean[][] 来过 = new boolean[行数][列数];
    Deque<int[]> 队 = new ArrayDeque<>();
    队.offer(new int[]{起行, 起列});
    来过[起行][起列] = true;
    while (!队.isEmpty()) {
        int[] 当前 = 队.poll();
        for (int d = 0; d < 4; d++) {
            int 新行 = 当前[0] + 行方向[d];
            int 新列 = 当前[1] + 列方向[d];
            if (新行 < 0 || 新行 >= 行数 || 新列 < 0 || 新列 >= 列数) continue;
            if (迷宫[新行][新列] == 1 || 来过[新行][新列]) continue;
            来过[新行][新列] = true;
            距离[新行][新列] = 距离[当前[0]][当前[1]] + 1;
            if (新行 == 终行 && 新列 == 终列) {
                return 距离[新行][新列];           /* 第一次碰到就是最短 */
            }
            队.offer(new int[]{新行, 新列});
        }
    }
    return -1;
}

// 3. 连通块：每个"还没来过的能走格子"起一次 BFS，数一共起了几次
//    ⚠ 用队列而不是递归 —— 300×300 的空旷迷宫里，递归 DFS 要 9 万层，必然爆栈
static int 连通块数(int[][] 迷宫) {
    if (迷宫 == null || 迷宫.length == 0) return 0;
    int 行数 = 迷宫.length;
    int 列数 = 迷宫[0].length;
    boolean[][] 来过 = new boolean[行数][列数];
    int 块数 = 0;
    for (int i = 0; i < 行数; i++) {
        for (int j = 0; j < 列数; j++) {
            if (迷宫[i][j] == 1 || 来过[i][j]) continue;
            块数 = 块数 + 1;
            Deque<int[]> 队 = new ArrayDeque<>();
            队.offer(new int[]{i, j});
            来过[i][j] = true;
            while (!队.isEmpty()) {
                int[] 当前 = 队.poll();
                for (int d = 0; d < 4; d++) {
                    int 新行 = 当前[0] + 行方向[d];
                    int 新列 = 当前[1] + 列方向[d];
                    if (新行 < 0 || 新行 >= 行数 || 新列 < 0 || 新列 >= 列数) continue;
                    if (迷宫[新行][新列] == 1 || 来过[新行][新列]) continue;
                    来过[新行][新列] = true;
                    队.offer(new int[]{新行, 新列});
                }
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
        if (!Regex.IsMatch(净, @"static\s+boolean\s+能到达\s*\(\s*int\s*\[\s*\]\s*\[\s*\]\s*\w+\s*,\s*int\s+\w+\s*,\s*int\s+\w+\s*,\s*int\s+\w+\s*,\s*int\s+\w+\s*\)"))
            缺.Add("      能到达　（static boolean 能到达(int[][] 迷宫, int 起行, int 起列, int 终行, int 终列)）");
        if (!Regex.IsMatch(净, @"static\s+int\s+最短步数\s*\(\s*int\s*\[\s*\]\s*\[\s*\]\s*\w+\s*,\s*int\s+\w+\s*,\s*int\s+\w+\s*,\s*int\s+\w+\s*,\s*int\s+\w+\s*\)"))
            缺.Add("      最短步数（static int 最短步数(int[][] 迷宫, int 起行, int 起列, int 终行, int 终列)）");
        if (!Regex.IsMatch(净, @"static\s+int\s+连通块数\s*\(\s*int\s*\[\s*\]\s*\[\s*\]\s*\w+\s*\)"))
            缺.Add("      连通块数（static int 连通块数(int[][] 迷宫)）");

        if (缺.Count == 0) return JavaCheckResult.过("三个方法都在，签名也对");
        return JavaCheckResult.不过(
            "有方法没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      ⚠ 迷宫参数是**二维数组** int[][]；坐标是 行、列 两个 int。");
    }

    [Java检查说明("★ 最短步数用的是 BFS（方法体里有 Queue / ArrayDeque）")]
    public static JavaCheckResult 检查_用的是队列(string 代码)
    {
        // ⚠ 核心检查点之一：DFS 也能算对，但它走的不一定是最短路。
        string 净 = 去掉注释(代码);
        string 体 = 函数体(净, "最短步数");
        if (体 == null)
            return JavaCheckResult.不过("没找到 最短步数 的函数体 —— 先确认这个方法写全了（花括号配对）。");

        if (!体.Contains("Queue") && !体.Contains("ArrayDeque") && !体.Contains("LinkedList"))
            return JavaCheckResult.不过(
                "最短步数 里没有队列（Queue / ArrayDeque）—— 它看起来不是 BFS。\n" +
                "      ⚠ 「最少几步」只有 BFS 能直接答对：\n" +
                "        BFS 用**队列**，先处理走 1 步能到的、再处理走 2 步能到的…\n" +
                "        ——它是**按距离从近到远**铺开的，所以第一次碰到终点就是最短路。\n" +
                "        DFS 是「一条路走到黑」，找到的那条路可能绕了很远；\n" +
                "        小迷宫里（只有一条路）它经常碰巧对，路一多就错。\n" +
                "      ⇒ 用 BFS：\n" +
                "          Deque<int[]> 队 = new ArrayDeque<>();\n" +
                "          队.offer(new int[]{起行, 起列, 0});     // 第三个格子记步数\n" +
                "          while (!队.isEmpty()) { int[] 当前 = 队.poll(); … 队.offer(new int[]{新行, 新列, 当前[2] + 1}); }");

        return JavaCheckResult.过("最短步数 用的是队列 —— BFS");
    }

    [Java检查说明("★ 200×200 的空旷迷宫：398 步（DFS 在这里会爆栈或超时）")]
    public static JavaCheckResult 检查_大迷宫(string 代码)
    {
        var run = 跑一次(代码);
        if (run.Raw.Error != null && run.Raw.Error.Contains("StackOverflow"))
            return JavaCheckResult.不过(
                "**栈溢出**了（StackOverflowError）—— 大迷宫里递归 DFS 的典型症状。\n" +
                "      检查里喂了一座 **200×200 全空旷**的迷宫（起点左上、终点右下）。\n" +
                "      递归 DFS 在这种「到处都能走」的图上会走出几万步长的路径，\n" +
                "      每一层递归占一格调用栈 ⇒ 栈装不下。\n" +
                "      ⇒ 用**队列的循环**（BFS），它不占调用栈：\n" +
                "          Deque<int[]> 队 = new ArrayDeque<>();\n" +
                "          队.offer(new int[]{起行, 起列, 0});\n" +
                "          while (!队.isEmpty()) { … }\n" +
                "      （要写 DFS 也行，但得把「待处理的位置」压进一个**显式的栈**，\n" +
                "        不能压在调用栈上。）");
        if (run.Raw.Error != null && run.Raw.Error.Contains("超时"))
            return JavaCheckResult.不过(
                "跑大迷宫的时候**超时**了。\n" +
                "      200×200 的空旷迷宫用 BFS 是几十毫秒的事（答案 398 步 —— 200+200-2）。\n" +
                "      超时通常说明在做「把每条可能的路径都试一遍」那种搜索 ——\n" +
                "      空旷网格上路径数是指数级的。\n" +
                "      ⇒ 换成 BFS：每个格子只处理一次，总共 4 万次操作。");
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("大到达|", "true", "200×200 空旷迷宫：起点左上、终点右下，当然到得了"),
            ("大最短|", "398", "⚠ 空旷迷宫的最短步数 = (200-1) + (200-1) = 398（一路向右再一路向下）"));
    }

    [Java检查说明("★ 300×300 的连通块：用队列才跑得完（递归会爆栈）")]
    public static JavaCheckResult 检查_大连通块(string 代码)
    {
        var run = 跑一次(代码);
        if (run.Raw.Error != null && run.Raw.Error.Contains("StackOverflow"))
            return JavaCheckResult.不过(
                "**栈溢出**了（StackOverflowError）—— 连通块 里的递归 DFS 崩了。\n" +
                "      检查里喂的是 **300×300 全空旷**的迷宫：整片连通，递归深度 90000 层。\n" +
                "      ⇒ 改成用**队列**的循环（和 BFS 一模一样的骨架，只是不需要记步数）：\n" +
                "          Deque<int[]> 队 = new ArrayDeque<>();\n" +
                "          队.offer(new int[]{i, j});  来过[i][j] = true;\n" +
                "          while (!队.isEmpty()) { int[] 当前 = 队.poll(); …四个方向… }\n" +
                "      ⚠ 记住这条：**递归 DFS 的深度跟地图大小成正比** ——\n" +
                "        小地图上没事，地图一大就崩。");
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("连通空旷|", "1", "300×300 全空旷 ⇒ 整片连通，1 块"),
            ("连通墙面|", "0", "全是墙 ⇒ 没有能走的格子，0 块"),
            ("连通两片|", "2", "被一堵竖墙分成左右两片 ⇒ 2 块"));
    }

    [Java检查说明("小迷宫：能到达 / 最短步数 / 连通块（含边界）")]
    public static JavaCheckResult 检查_小迷宫(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("甲最短|", "3", "最短步数(甲, 0,0 → 1,2)　甲 是 {{0,0,0},{1,1,0}}，(0,0)→(0,1)→(0,2)→(1,2) 三步"),
            ("乙到达|", "true", "能到达(乙, 0,0 → 2,3)"),
            ("乙最短|", "5", "最短步数(乙, 0,0 → 2,3)　乙 是 {{0,0,0,0},{1,1,0,1},{0,0,0,0}}"),
            ("丙到达|", "false", "能到达(丙, 0,0 → 0,3)　⚠ 丙 是 {{0,1,1,0},{0,1,1,0},{0,1,1,0}}：中间两列全是墙，左右两片走不通"),
            ("丙最短|", "-1", "最短步数(丙, …) 走不到 ⇒ -1"),
            ("同点|", "0", "最短步数(甲, 0,0 → 0,0)　起点就是终点 ⇒ 0"),
            ("起墙|", "false", "能到达(甲, 1,0 → 0,0)　**起点是墙** ⇒ false"),
            ("终墙|", "false", "能到达(甲, 0,0 → 1,0)　**终点是墙** ⇒ false"),
            ("越界|", "false", "能到达(甲, 0,0 → 5,5)　终点越界 ⇒ false"),
            ("越界二|", "false", "能到达(甲, -1,0 → 0,0)　起点越界 ⇒ false"),
            ("丙块|", "2", "连通块数(丙)　中间两列全是墙 ⇒ 左、右两片，各 3 个格子"),
            ("丁块|", "4", "连通块数(丁)　四个孤立的点"),
            ("甲块|", "1", "连通块数(甲)　一片"),
            ("空块|", "0", "连通块数({})　空地图"));
    }

    // ══════════════════ 工具 ══════════════════

    private const string 调用点 = """
        int[][] 甲 = {{0, 0, 0}, {1, 1, 0}};
        int[][] 乙 = {{0, 0, 0, 0}, {1, 1, 0, 1}, {0, 0, 0, 0}};
        int[][] 丙 = {{0, 1, 1, 0}, {0, 1, 1, 0}, {0, 1, 1, 0}};
        int[][] 丁 = {{0, 1, 0}, {1, 1, 1}, {0, 1, 0}};

        System.out.println("甲最短|" + 最短步数(甲, 0, 0, 1, 2));
        System.out.println("乙到达|" + 能到达(乙, 0, 0, 2, 3));
        System.out.println("乙最短|" + 最短步数(乙, 0, 0, 2, 3));
        System.out.println("丙到达|" + 能到达(丙, 0, 0, 0, 3));
        System.out.println("丙最短|" + 最短步数(丙, 0, 0, 0, 3));
        System.out.println("同点|" + 最短步数(甲, 0, 0, 0, 0));
        System.out.println("起墙|" + 能到达(甲, 1, 0, 0, 0));
        System.out.println("终墙|" + 能到达(甲, 0, 0, 1, 0));
        System.out.println("越界|" + 能到达(甲, 0, 0, 5, 5));
        System.out.println("越界二|" + 能到达(甲, -1, 0, 0, 0));
        System.out.println("丙块|" + 连通块数(丙));
        System.out.println("丁块|" + 连通块数(丁));
        System.out.println("甲块|" + 连通块数(甲));
        System.out.println("空块|" + 连通块数(new int[][]{}));

        int 大 = 200;
        int[][] 空旷 = new int[大][大];
        System.out.println("大到达|" + 能到达(空旷, 0, 0, 大 - 1, 大 - 1));
        System.out.println("大最短|" + 最短步数(空旷, 0, 0, 大 - 1, 大 - 1));

        int 更大 = 300;
        int[][] 大空旷 = new int[更大][更大];
        System.out.println("连通空旷|" + 连通块数(大空旷));

        int[][] 全墙 = new int[10][10];
        for (int i = 0; i < 10; i++) {
            for (int j = 0; j < 10; j++) {
                全墙[i][j] = 1;
            }
        }
        System.out.println("连通墙面|" + 连通块数(全墙));

        int[][] 隔开 = new int[5][7];
        for (int i = 0; i < 5; i++) {
            隔开[i][3] = 1;
        }
        System.out.println("连通两片|" + 连通块数(隔开));
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
                       "      这一课常见的原因：`迷宫.length == 0` 时去取 `迷宫[0].length` ——\n" +
                       "      空地图没有第 0 行。⇒ 先判 `if (迷宫 == null || 迷宫.length == 0) return …;`";
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
            "      BFS 的骨架：\n" +
            "          Deque<int[]> 队 = new ArrayDeque<>();\n" +
            "          队.offer(new int[]{起行, 起列, 0});  来过[起行][起列] = true;\n" +
            "          while (!队.isEmpty()) {\n" +
            "              int[] 当前 = 队.poll();\n" +
            "              for (四个方向) { 不越界、不是墙、没来过 ⇒ 标记 + 入队（步数 +1）}\n" +
            "          }\n" +
            "      ⚠ 四个容易漏的边界：起点/终点越界、起点/终点是墙、起点就是终点（0 步）、走不到（-1）。");
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
