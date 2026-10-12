# 反面测试：每课喂一个「偷懒但输出对」的答案，看检查抓不抓得住。
#
# 为什么必须做：「参考答案全过」只证明检查**不冤枉对的**；
#   如果检查永远返回"通过"，那它也照样"参考答案全过" —— 但那对学员毫无价值。
#
# 每个答案都**能编译**（签名齐全），只是用了不该用的东西：
#   · 把答案写死 / 少写边界        · 调现成库绕过要考的知识点
#   · 漏掉某一层的判断（不判空、不标记、不倒着遍历……）
#
# 用法：python _cs17_40\反面测试.py
import io
import os
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
根 = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
答案 = os.path.join(根, "cs-answers")
env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"
env["TEMP"] = r"C:\lc_cs_tmp"
env["TMP"] = r"C:\lc_cs_tmp"

# (课号, 这个答案偷的懒, 期望被哪条检查抓住的关键词, 代码)
错例 = [
    ("17", "转锯齿拿 表.Length（总格数）当行数", "转锯齿", """
static int 行和(int[,] 表, int 行) { return 0; }
static int[] 列和(int[,] 表) { return new int[0]; }
static int[][] 转锯齿(int[,] 表)
{
    int[][] 出 = new int[表.Length][];                 // ★ 总格数当行数
    for (int i = 0; i < 表.Length; i++)
    {
        出[i] = new int[表.GetLength(1)];
        for (int j = 0; j < 表.GetLength(1); j++) 出[i][j] = 表[i % 表.GetLength(0), j];
    }
    return 出;
}
static int 锯齿最大(int[][] 锯齿) { int m = 锯齿[0][0]; foreach (var r in 锯齿) foreach (var x in r) if (x > m) m = x; return m; }
"""),
    ("18", "冒泡排序里直接调 Array.Sort（输出完全正确）", "现成排序", """
static void 冒泡排序(int[] 数) { Array.Sort(数); }
static void 选择排序(int[] 数) { Array.Sort(数); }
static void 插入排序(int[] 数) { Array.Sort(数); }
static int[] 用库排(int[] 数) { int[] c = (int[])数.Clone(); Array.Sort(c); return c; }
"""),
    ("19", "比较器里顺手把名字也比了（同分返回非 0）", "比较器只比分数", """
record 成绩(string 名字, int 分数);
class 按分数降序 : IComparer<成绩>
{
    public int Compare(成绩 甲, 成绩 乙)
    {
        if (甲.分数 != 乙.分数) return 乙.分数.CompareTo(甲.分数);
        return string.CompareOrdinal(甲.名字, 乙.名字);   // ★ 多管了
    }
}
class 按数值降序 : IComparer<int> { public int Compare(int 甲, int 乙) => 乙.CompareTo(甲); }
static List<string> 降序名字(List<成绩> 们) => 们.OrderBy(项 => 项, new 按分数降序()).Select(项 => 项.名字).ToList();
static void 原地降序(int[] 数) { Array.Sort(数, new 按数值降序()); }
"""),
    ("20", "冒泡排序并计数：Array.Sort + 背公式", "现成排序", """
static int 线性查找(int[] 数, int 目标, out long 比较次数)
{
    比较次数 = 0;
    for (int i = 0; i < 数.Length; i++) { 比较次数++; if (数[i] == 目标) return i; }
    return -1;
}
static long 冒泡排序并计数(int[] 数)
{
    Array.Sort(数);                                    // ★ 排序交给库
    return (long)数.Length * (数.Length - 1) / 2;      // ★ 次数是背出来的
}
static long 折半次数(long n) { long c = 0; while (n > 1) { n = (n + 1) / 2; c++; } return c; }
"""),
    ("21", "斐波那契写成递归（这一课要的是从前往后填）", "不许递归", """
static long 爬楼梯(int n) { if (n <= 1) return 1; return 爬楼梯(n - 1) + 爬楼梯(n - 2); }
static long 斐波那契(int n) { if (n <= 1) return n; return 斐波那契(n - 1) + 斐波那契(n - 2); }
static int[] 斐波那契表(int n)
{
    int[] 表 = new int[n];
    for (int i = 0; i < n; i++) 表[i] = (int)斐波那契(i);
    return 表;
}
"""),
    ("22", "首行忘了 using（文件被占住，读出来的内容却完全正确）", "删掉", """
static string[] 读所有行(string 路径) => File.ReadAllLines(路径);
static int 求和(string 路径) { int s = 0; foreach (var l in File.ReadAllLines(路径)) if (l.Trim().Length > 0) s += int.Parse(l.Trim()); return s; }
static string 首行(string 路径)
{
    var 读 = new StreamReader(路径);      // ★ 没有 using
    return 读.ReadLine();
}
static void 加一行(string 路径, string 行) { File.AppendAllText(路径, 行 + "\\n"); }
"""),
    ("23", "是质数只试除到 1000（小数字全对）", "是质数", """
static List<int> 因数们(int n) { var 出 = new List<int>(); for (int d = 1; d <= n; d++) if (n % d == 0) 出.Add(d); return 出; }
static bool 是质数(long n)
{
    if (n < 2) return false;
    for (long d = 2; d < 1000; d++) if (n % d == 0) return false;   // ★ 上限写死
    return true;
}
static List<long> 质因数(long n)
{
    var 出 = new List<long>(); long 剩 = n;
    for (long d = 2; d * d <= 剩; d++) if (剩 % d == 0) { 出.Add(d); 剩 /= d; }
    if (剩 > 1) 出.Add(剩);
    return 出;
}
"""),
    ("24", "最小公倍数先乘后除（大数溢出成负数）", "最小公倍数", """
static long 最大公约数(long a, long b) { a = Math.Abs(a); b = Math.Abs(b); while (b != 0) { long 余 = a % b; a = b; b = 余; } return a; }
static long 最小公倍数(long a, long b)
{
    if (a == 0 || b == 0) return 0;
    return a * b / 最大公约数(a, b);                    // ★ 先乘就溢出
}
static string 约分(long 分子, long 分母) { long g = 最大公约数(分子, 分母); if (g == 0) g = 1; return 分子 / g + "/" + 分母 / g; }
"""),
    ("25", "素数个数用逐个数试除（答案全对，但慢几十倍）", "3 秒", """
static bool[] 筛(int n)
{
    bool[] 是 = new bool[n + 1];
    for (int i = 2; i <= n; i++) 是[i] = true;
    for (long i = 2; i * i <= n; i++) if (是[i]) for (long j = i * i; j <= n; j += i) 是[j] = false;
    return 是;
}
static int[] 素数表(int n) { var 出 = new List<int>(); var 是 = 筛(n); for (int i = 2; i <= n; i++) if (是[i]) 出.Add(i); return 出.ToArray(); }
static int 素数个数(int n)
{
    int 个 = 0;                                         // ★ 一个一个试除
    for (int i = 2; i <= n; i++)
    {
        bool 质 = true;
        for (long d = 2; d * d <= i; d++) if (i % d == 0) { 质 = false; break; }
        if (质) 个++;
    }
    return 个;
}
"""),
    ("26", "左边界直接用 Array.BinarySearch（重复元素拿不到第一个）", "左边界", """
static int 手写二分(int[] 数, int 目标) => Array.BinarySearch(数, 目标) < 0 ? -1 : Array.BinarySearch(数, 目标);
static int 左边界(int[] 数, int 目标)
{
    int p = Array.BinarySearch(数, 目标);               // ★ 返回哪一个不确定
    if (p >= 0) return p;
    return ~p;
}
static long 整数平方根(long n) { long 左 = 0, 右 = n; while (左 < 右) { long 中 = 左 + (右 - 左 + 1) / 2; if (中 <= n / 中) 左 = 中; else 右 = 中 - 1; } return 左; }
"""),
    ("27", "区间调度按开始时间排（贪错了键）", "最多活动", """
static int[] 找零(int 金额, int[] 面额)
{
    int[] 张 = new int[面额.Length];
    for (int i = 0; i < 面额.Length; i++) { 张[i] = 金额 / 面额[i]; 金额 %= 面额[i]; }
    return 张;
}
static int 最多活动(int[][] 区间)
{
    var 排 = 区间.OrderBy(段 => 段[0]).ToList();        // ★ 应该按 段[1]
    int 个 = 0, 上次结束 = int.MinValue;
    foreach (var 段 in 排) if (段[0] >= 上次结束) { 个++; 上次结束 = 段[1]; }
    return 个;
}
"""),
    ("28", "汉诺塔只打出 n 条 A->C（步数不对、步骤也不合法）", "汉诺塔", """
static long 阶乘(int n) => n <= 0 ? 1 : n * 阶乘(n - 1);
static List<string> 汉诺塔(int n)
{
    var 出 = new List<string>();
    for (int i = 0; i < n; i++) 出.Add("A->C");          // ★ 假步骤
    return 出;
}
static int 数字和(long n) { n = Math.Abs(n); return n < 10 ? (int)n : (int)(n % 10) + 数字和(n / 10); }
"""),
    ("29", "逆序对用双重循环（输出对，但不是分治）", "分治", """
static int[] 合并(int[] 甲, int[] 乙)
{
    int[] 出 = new int[甲.Length + 乙.Length]; int i = 0, j = 0, k = 0;
    while (i < 甲.Length && j < 乙.Length) 出[k++] = 甲[i] <= 乙[j] ? 甲[i++] : 乙[j++];
    while (i < 甲.Length) 出[k++] = 甲[i++];
    while (j < 乙.Length) 出[k++] = 乙[j++];
    return 出;
}
static void 归并排序(int[] 数) { Array.Sort(数); }
static long 逆序对(int[] 数)
{
    long 共 = 0;                                        // ★ 双重循环 O(n²)
    for (int i = 0; i < 数.Length; i++)
        for (int j = i + 1; j < 数.Length; j++)
            if (数[i] > 数[j]) 共++;
    return 共;
}
"""),
    ("30", "分区直接返回 右（不搬元素）", "分区语义", """
static int 分区(int[] 数, int 左, int 右) => 右;        // ★ 什么都没搬
static void 快速排序(int[] 数) { Array.Sort(数); }
"""),
    ("31", "处理括号不判空栈（\")(\" 上直接 Pop）", "处理括号", """
static bool 配对(string s)
{
    var 栈 = new Stack<char>();
    foreach (char c in s)
    {
        if (c == '(' || c == '[' || c == '{') 栈.Push(c);
        else if (c == ')' || c == ']' || c == '}')
        {
            if (栈.Count == 0) return false;
            char 顶 = 栈.Pop();
            if (c == ')' && 顶 != '(') return false;
            if (c == ']' && 顶 != '[') return false;
            if (c == '}' && 顶 != '{') return false;
        }
    }
    return 栈.Count == 0;
}
static int[] 处理括号(string s)
{
    var 栈 = new Stack<char>(); int 配不上 = 0, 最大深度 = 0;
    foreach (char c in s)
    {
        if (c == '(') { 栈.Push(c); if (栈.Count > 最大深度) 最大深度 = 栈.Count; }
        else if (c == ')') { 栈.Pop(); }                 // ★ 空栈上 Pop ⇒ 抛异常
    }
    if (栈.Count > 0) 配不上 = 1;
    return new int[] { 配不上, 最大深度 };
}
static int 约瑟夫(int n, int k) { var 队 = new Queue<int>(); for (int i = 1; i <= n; i++) 队.Enqueue(i); while (队.Count > 1) { for (int i = 1; i < k; i++) 队.Enqueue(队.Dequeue()); 队.Dequeue(); } return 队.Dequeue(); }
"""),
    ("32", "反转先把值抄进 List 再重建（输出完全正确）", "不许借助", """
class 结点 { public int 值; public 结点 下; public 结点(int 值) { this.值 = 值; } }
static 结点 建表(int[] 数)
{
    if (数.Length == 0) return null; 结点 头 = new 结点(数[0]); 结点 尾 = 头;
    for (int i = 1; i < 数.Length; i++) { 尾.下 = new 结点(数[i]); 尾 = 尾.下; }
    return 头;
}
static int 长度(结点 头) { int n = 0; for (结点 c = 头; c != null; c = c.下) n++; return n; }
static 结点 反转(结点 头)
{
    var 值们 = new List<int>();                          // ★ 借助 List
    for (结点 c = 头; c != null; c = c.下) 值们.Add(c.值);
    结点 新头 = null;
    foreach (int x in 值们) { var n = new 结点(x); n.下 = 新头; 新头 = n; }
    return 新头;
}
static string 打出(结点 头) { if (头 == null) return "空"; var 出 = new List<string>(); for (结点 c = 头; c != null; c = c.下) 出.Add(c.值.ToString()); return string.Join("->", 出); }
static List<int> 用库反转(List<int> 数) { var 链 = new LinkedList<int>(); foreach (int x in 数) 链.AddFirst(x); return new List<int>(链); }
"""),
    ("33", "先序 / 中序 / 后序 都写成中序（单看一份看不出问题）", "互不相同", """
class 树结点 { public int 值; public 树结点 左; public 树结点 右; public 树结点(int 值) { this.值 = 值; } }
static void 收(树结点 根, List<int> 出) { if (根 == null) return; 收(根.左, 出); 出.Add(根.值); 收(根.右, 出); }
static List<int> 先序(树结点 根) { var 出 = new List<int>(); 收(根, 出); return 出; }
static List<int> 中序(树结点 根) { var 出 = new List<int>(); 收(根, 出); return 出; }
static List<int> 后序(树结点 根) { var 出 = new List<int>(); 收(根, 出); return 出; }
static int 深度(树结点 根) { if (根 == null) return 0; int 左 = 深度(根.左), 右 = 深度(根.右); return (左 > 右 ? 左 : 右) + 1; }
"""),
    ("34", "查找把整棵树收进 List 再 Contains（O(n)，但答案全对）", "查找用", """
class 树结点 { public int 值; public 树结点 左; public 树结点 右; public 树结点(int 值) { this.值 = 值; } }
static 树结点 插入(树结点 根, int 值)
{
    if (根 == null) return new 树结点(值);
    if (值 == 根.值) return 根;
    if (值 < 根.值) 根.左 = 插入(根.左, 值); else 根.右 = 插入(根.右, 值);
    return 根;
}
static bool 查找(树结点 根, int 值)
{
    var 全部 = 中序(根);                                  // ★ 收集再找
    return 全部.Contains(值);
}
static List<int> 中序(树结点 根) { var 出 = new List<int>(); 收(根, 出); return 出; }
static void 收(树结点 根, List<int> 出) { if (根 == null) return; 收(根.左, 出); 出.Add(根.值); 收(根.右, 出); }
static int 高度(树结点 根) { if (根 == null) return 0; int 左 = 高度(根.左), 右 = 高度(根.右); return (左 > 右 ? 左 : 右) + 1; }
"""),
    ("35", "最短步数：到不了返回 0（不是 -1）", "最短步数", """
static int 最短步数(int[,] 迷宫, int 起行, int 起列, int 止行, int 止列)
{
    int 行数 = 迷宫.GetLength(0), 列数 = 迷宫.GetLength(1);
    if (迷宫[起行, 起列] == 1 || 迷宫[止行, 止列] == 1) return 0;   // ★ 该 -1
    int[,] 步 = new int[行数, 列数];
    for (int i = 0; i < 行数; i++) for (int j = 0; j < 列数; j++) 步[i, j] = -1;
    步[起行, 起列] = 0;
    var 队 = new Queue<(int, int)>(); 队.Enqueue((起行, 起列));
    int[] 行移 = { -1, 1, 0, 0 }, 列移 = { 0, 0, -1, 1 };
    while (队.Count > 0)
    {
        var (行, 列) = 队.Dequeue();
        for (int d = 0; d < 4; d++)
        {
            int 新行 = 行 + 行移[d], 新列 = 列 + 列移[d];
            if (新行 < 0 || 新行 >= 行数 || 新列 < 0 || 新列 >= 列数) continue;
            if (迷宫[新行, 新列] == 1 || 步[新行, 新列] != -1) continue;
            步[新行, 新列] = 步[行, 列] + 1; 队.Enqueue((新行, 新列));
        }
    }
    return 步[止行, 止列] == -1 ? 0 : 步[止行, 止列];                 // ★ 0 冒充"到不了"
}
static int 可达格子数(int[,] 迷宫, int 起行, int 起列)
{
    int 行数 = 迷宫.GetLength(0), 列数 = 迷宫.GetLength(1);
    if (迷宫[起行, 起列] == 1) return 0;
    bool[,] 到过 = new bool[行数, 列数]; 到过[起行, 起列] = true; int 个 = 1;
    var 队 = new Queue<(int, int)>(); 队.Enqueue((起行, 起列));
    int[] 行移 = { -1, 1, 0, 0 }, 列移 = { 0, 0, -1, 1 };
    while (队.Count > 0)
    {
        var (行, 列) = 队.Dequeue();
        for (int d = 0; d < 4; d++)
        {
            int 新行 = 行 + 行移[d], 新列 = 列 + 列移[d];
            if (新行 < 0 || 新行 >= 行数 || 新列 < 0 || 新列 >= 列数) continue;
            if (迷宫[新行, 新列] == 1 || 到过[新行, 新列]) continue;
            到过[新行, 新列] = true; 个++; 队.Enqueue((新行, 新列));
        }
    }
    return 个;
}
"""),
    ("36", "01 背包容量正着遍历（变成完全背包）", "背包", """
static long 爬楼梯(int n) { if (n <= 1) return 1; long a = 1, b = 1; for (int i = 2; i <= n; i++) { long c = a + b; a = b; b = c; } return b; }
static long 打家劫舍(int[] 房)
{
    long[] dp = new long[房.Length + 1];
    for (int i = 1; i <= 房.Length; i++) dp[i] = Math.Max(dp[i - 1], (i >= 2 ? dp[i - 2] : 0) + 房[i - 1]);
    return dp[房.Length];
}
static long 背包(int[] 重, int[] 值, int 容量)
{
    long[] dp = new long[容量 + 1];
    for (int i = 0; i < 重.Length; i++)
        for (int j = 重[i]; j <= 容量; j++)              // ★ 正着 ⇒ 完全背包
            dp[j] = Math.Max(dp[j], dp[j - 重[i]] + 值[i]);
    return dp[容量];
}
"""),
    ("37", "几乎相等拿 == 比（浮点误差让 0.1+0.2 ≠ 0.3）", "几乎相等", """
static double 两点距离(double x1, double y1, double x2, double y2) { double a = x1 - x2, b = y1 - y2; return Math.Sqrt(a * a + b * b); }
static double 圆面积(double 半径) => Math.PI * 半径 * 半径;
static bool 几乎相等(double 甲, double 乙) => 甲 == 乙;                 // ★ 直接 ==
static double 四舍五入(double x) => Math.Floor(x + 0.5);
static long 整数平方根(long n) { if (n <= 0) return 0; long r = (long)Math.Sqrt(n); while ((r + 1) <= n / (r + 1)) r++; while (r > 0 && r > n / r) r--; return r; }
"""),
    ("38", "路径数有障碍：不检查起点/终点（堵住了也返回 1）", "障碍", """
static long 路径数(int 行, int 列)
{
    long[,] dp = new long[行, 列]; dp[0, 0] = 1;
    for (int i = 0; i < 行; i++) for (int j = 0; j < 列; j++)
    {
        if (i == 0 && j == 0) continue;
        dp[i, j] = (i > 0 ? dp[i - 1, j] : 0) + (j > 0 ? dp[i, j - 1] : 0);
    }
    return dp[行 - 1, 列 - 1];
}
static long 路径数有障碍(int[,] 格)
{
    int 行 = 格.GetLength(0), 列 = 格.GetLength(1);
    long[,] dp = new long[行, 列]; dp[0, 0] = 1;          // ★ 没判起点/终点是不是障碍
    for (int i = 0; i < 行; i++) for (int j = 0; j < 列; j++)
    {
        if (i == 0 && j == 0) continue;
        if (格[i, j] == 1) { dp[i, j] = 0; continue; }
        dp[i, j] = (i > 0 ? dp[i - 1, j] : 0) + (j > 0 ? dp[i, j - 1] : 0);
    }
    return dp[行 - 1, 列 - 1];
}
static int 最长公共子序列(string 甲, string 乙)
{
    int n = 甲.Length, m = 乙.Length; int[,] dp = new int[n + 1, m + 1];
    for (int i = 1; i <= n; i++) for (int j = 1; j <= m; j++)
        dp[i, j] = 甲[i - 1] == 乙[j - 1] ? dp[i - 1, j - 1] + 1 : Math.Max(dp[i - 1, j], dp[i, j - 1]);
    return dp[n, m];
}
"""),
    ("39", "连通块不算孤立点（只看有边的点）", "连通块", """
static List<int> 深度优先(List<List<int>> 图, int 起)
{
    var 出 = new List<int>(); if (图.Count == 0) return 出;
    bool[] 到过 = new bool[图.Count]; 走(图, 起, 到过, 出); return 出;
}
static void 走(List<List<int>> 图, int 点, bool[] 到过, List<int> 出)
{
    到过[点] = true; 出.Add(点);
    foreach (int 邻 in 图[点]) if (!到过[邻]) 走(图, 邻, 到过, 出);
}
static List<int> 广度优先(List<List<int>> 图, int 起)
{
    var 出 = new List<int>(); if (图.Count == 0) return 出;
    bool[] 到过 = new bool[图.Count]; var 队 = new Queue<int>(); 到过[起] = true; 队.Enqueue(起);
    while (队.Count > 0) { int 点 = 队.Dequeue(); 出.Add(点); foreach (int 邻 in 图[点]) if (!到过[邻]) { 到过[邻] = true; 队.Enqueue(邻); } }
    return 出;
}
static int 连通块数(List<List<int>> 图)
{
    bool[] 到过 = new bool[图.Count]; int 块 = 0;
    for (int i = 0; i < 图.Count; i++)
    {
        if (图[i].Count == 0) continue;                  // ★ 跳过孤立点
        if (!到过[i]) { 块++; var 废 = new List<int>(); 走(图, i, 到过, 废); }
    }
    return 块;
}
static int 最远距离(List<List<int>> 图, int 起)
{
    if (图.Count == 0) return -1;
    int[] 层 = new int[图.Count]; for (int i = 0; i < 层.Length; i++) 层[i] = -1;
    层[起] = 0; var 队 = new Queue<int>(); 队.Enqueue(起); int 最大 = 0;
    while (队.Count > 0)
    {
        int 点 = 队.Dequeue();
        foreach (int 邻 in 图[点]) if (层[邻] == -1) { 层[邻] = 层[点] + 1; if (层[邻] > 最大) 最大 = 层[邻]; 队.Enqueue(邻); }
    }
    return 最大;
}
"""),
    ("40", "无向图只加单向边（从起点看着对，换起点全 -1）", "两个方向都要加", """
static long[] 最短路(int 点数, int[][] 边, int 起)
{
    var 图 = new List<List<(int 到, int 权)>>();
    for (int i = 0; i < 点数; i++) 图.Add(new List<(int, int)>());
    foreach (int[] 条 in 边) 图[条[0]].Add((条[1], 条[2]));        // ★ 只加一个方向
    long[] 距离 = new long[点数];
    for (int i = 0; i < 点数; i++) 距离[i] = -1;
    距离[起] = 0;
    var 队 = new PriorityQueue<int, int>(); 队.Enqueue(起, 0);
    while (队.Count > 0)
    {
        队.TryDequeue(out int 点, out int 当前);
        if (当前 > 距离[点]) continue;
        foreach (var (到, 权) in 图[点])
        {
            long 新 = 距离[点] + 权;
            if (距离[到] == -1 || 新 < 距离[到]) { 距离[到] = 新; 队.Enqueue(到, (int)新); }
        }
    }
    return 距离;
}
"""),
]


def 跑(编号):
    r = subprocess.run(["dotnet", "run", "--project", "csharp/cs-lessons", "检查", 编号],
                       cwd=根, env=env, capture_output=True, timeout=900)
    return (r.stdout + r.stderr).decode("utf-8", "replace")


好 = 0
目标 = sys.argv[1:]          # 可选：只跑这几课（省时间）
for 编号, 偷懒, 期望着, 代码 in 错例:
    if 目标 and 编号 not in 目标:
        continue
    os.makedirs(答案, exist_ok=True)
    现 = sorted(f for f in os.listdir(答案) if f.startswith(编号))
    if not 现:
        跑(编号)                                   # 让它把答题文件建出来
        现 = sorted(f for f in os.listdir(答案) if f.startswith(编号))
    if not 现:
        print("  ?? %s 建不出答题文件" % 编号)
        continue
    io.open(os.path.join(答案, 现[0]), "w", encoding="utf-8", newline="").write(代码)

    out = 跑(编号)
    全过 = "全部通过" in out
    失败 = [l.strip() for l in out.splitlines() if l.strip().startswith("✗")]
    抓到 = any(期望着 in l for l in 失败)
    好 += 1 if (not 全过 and 抓到) else 0
    print("  %s %s  %s" % ("OK " if (not 全过 and 抓到) else "XX ", 编号, 偷懒))
    if 全过:
        print("        **偷懒答案被判成通过了** —— 这条检查是摆设")
    elif not 抓到:
        print("        报错了，但没有 %s 这一条：" % 期望着)
        for l in 失败[:4]:
            print("          " + l[:100])

print()
print("  == %d/%d 个偷懒答案被正确拒绝 ==" % (好, len(错例)))
