// 第 17 课：二维数组 —— int[,]（矩形）与 int[][]（锯齿）
//
// ══ 这一课要教什么 ══
// 前面几课的「一排格子」只能装一行数据。班级成绩、棋盘、地图、矩阵 —— 这些
// 都是**一张表格**。C# 给了两种装法：矩形数组 int[,]（一整块内存、每行一样长）
// 和锯齿数组 int[][]（数组的数组、每行可以不一样长）。
//
// 这一课真正要建立的是**「长度从哪儿问」的方位感**：
//     int[,]   行数 = GetLength(0)，列数 = GetLength(1)，**Length 是总格数**
//     int[][]  行数 = Length，第 i 行的列数 = 锯[i].Length
// 以及那个**编译通过、结果全错**的地方：把 表.Length 当成行数用。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点一：**转锯齿**用 5×2 的表（Length=10，行数=5）。
//   把 表.Length 当行数的人会 new 出 10 行，输出里立刻多出一半 —— 抓得死死的。
//   再配一张 1×4 的表（Length=4，行数=1），两个方向都错不了。
// 核心检查点二：**锯齿最大**里有一张**全是负数**的锯齿（-9,-8 / -1）。
//   初值随手写 0 的人在这里返回 0；而只按 锯[0].Length 走的人会漏掉第 2 行的 30。
// 另外喂了三种形状的表（2×3 / 3×2 / 5×2 / 1×4）：把答案写死的、或者只对
// 某一份数据凑出来的，换一份立刻露馅。
//
// ⚠ 给维护者：所有输出都用「标记|值」的格式，每组调用各自 try/catch ——
//   学生代码抛异常时只影响那一组（不会把整次检查变成「编译失败」）。

using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("17", "第 17 课：二维数组 —— int[,]（矩形）与 int[][]（锯齿）",
    """
1. 写 `static int 行和(int[,] 表, int 行)` —— 第 行 行所有列的和（列数用 表.GetLength(1)）
2. 写 `static int[] 列和(int[,] 表)` —— 返回长度 = 列数 的数组，第 j 项是第 j 列的和
3. 写 `static int[][] 转锯齿(int[,] 表)` —— 搬成锯齿：结果有 GetLength(0) 行，每行长 GetLength(1)
4. 写 `static int 锯齿最大(int[][] 锯齿)` —— 锯齿里所有数的最大值（⚠ 每行长度不一样）
""")]
public static class 第17课
{
    public static string 模板 => """
// 第 17 课：二维数组 —— int[,]（矩形）与 int[][]（锯齿）
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**一张表格怎么装**。C# 给了两种装法，
// 而它们「长度从哪儿问」的写法**不一样** —— 这是本课的全部难点。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    矩形数组 int[,] 与锯齿数组 int[][]
//
//  需要先会
//    第 04 课：循环与集合（两层循环就是从这里来的）
//
// ─────────────────────────────────────────────────────
//  先介绍：一排格子不够用了
// ─────────────────────────────────────────────────────
//
// 一个班的成绩：3 个学生、每人 2 门课 —— 一共 6 个数。
// 用一维数组装得下，但你看不出「谁的第几门课」。装成表格就清楚了：
//
//            第0列  第1列
//   第0行      90     80         ← 第 0 个学生的两门课
//   第1行      70     75
//   第2行      60     95
//
// ─────────────────────────────────────────────────────
//  一、矩形数组：int[,]
// ─────────────────────────────────────────────────────
//
//     int[,] 表 = new int[3, 2] {
//         { 90, 80 },              3 行 2 列（一共 6 格）
//         { 70, 75 },
//         { 60, 95 }
//     };
//
//     表[1, 0]          第 1 行第 0 列 —— **一个方括号，两个下标，中间一个逗号**
//     表.GetLength(0)   行数 = 3
//     表.GetLength(1)   列数 = 2
//     表.Length         总格数 = 6        ← ⚠ 不是行数！
//
// ─────────────────────────────────────────────────────
//  二、锯齿数组：int[][]
// ─────────────────────────────────────────────────────
//
//     int[][] 锯 = new int[3][];          先只定行数，每一行还是空的
//     锯[0] = new int[] { 90, 80 };       每一行**自己 new**
//     锯[1] = new int[] { 70 };
//     锯[2] = new int[] { 60, 95, 100 };
//
//     锯.Length          行数 = 3
//     锯[1].Length       第 1 行的长度 = 1     ← 每行可以不一样
//     锯[1][0]           第 1 行第 0 个
//
// 什么时候用哪个：**每行一样长**（成绩表、矩阵、棋盘）用矩形；
// **每行不一样长**（每个人的所有分数、一行一句话）用锯齿。
//
// ─────────────────────────────────────────────────────
//  三、走遍一张表：两层循环
// ─────────────────────────────────────────────────────
//
// 矩形数组 —— **下标写在同一个方括号里，逗号隔开**：
//
//     for (int i = 0; i < 表.GetLength(0); i++) {
//         for (int j = 0; j < 表.GetLength(1); j++) {
//             表[i, j]
//         }
//     }
//
// 锯齿数组 —— **两层方括号**，内层长度看那一行：
//
//     for (int i = 0; i < 锯.Length; i++) {
//         for (int j = 0; j < 锯[i].Length; j++) {
//             锯[i][j]
//         }
//     }
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：表.Length 不是行数
// ─────────────────────────────────────────────────────
//
//     int[][] 出 = new int[表.Length][];        ← 3×2 的表，Length 是 6，不是 3
//     for (int i = 0; i < 表.Length; i++) {     ← 于是多跑了一半的趟
//         ...
//     }
//
// 你会看到：**编译通过、程序不崩**，只是行数多算了一倍 —— 结果全错。
//   为什么它不报错：new 一个长一点的数组是合法的；多出来的那几行是 null，
//   你要是再去读 出[i][j] 才会抛 NullReferenceException，否则它就**安安静静地错**。
//
// 为什么：矩形数组是一整块内存，Length 数的是**所有格子**（行 × 列）。
//   行数只有 GetLength(0) 知道，列数是 GetLength(1)。
//
// 附：另外两个真会撞上的报错（csc 与运行时的原话）：
//
//     想拿 表[0] 取一整行：
//         error CS0022: [] 内的索引数错误，应为 2
//
//     锯齿忘了 new 内层，就去读 锯[0][0]：
//         NullReferenceException: Object reference not set to an instance of an object.
//     （第二句是**运行时**报错 —— 编译期看不出来，因为 锯[0] 只是个 null。）
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   排序、查找 → 第 18、26 课。
//   用表格一格一格填数（动态规划）→ 第 36、38 课。
//
// ─────────────────────────────────────────────────────
//  ══ 轮到你了 ══
// ─────────────────────────────────────────────────────
//
//   在下面写四个方法（骨架已经替你写好了 using System / Collections.Generic / Linq / IO）：
//
//    1. static int 行和(int[,] 表, int 行)
//         把第 行 行的每一列加起来（列数 = 表.GetLength(1)）
//
//    2. static int[] 列和(int[,] 表)
//         返回一个数组，**长度 = 列数**，第 j 个装第 j 列的和
//
//    3. static int[][] 转锯齿(int[,] 表)
//         把矩形搬成锯齿：结果有 GetLength(0) 行，每行长 GetLength(1)
//         （⚠ 锯齿的每一行都要自己 new）
//
//    4. static int 锯齿最大(int[][] 锯齿)
//         所有格子里的最大值。⚠ 每行长度不一样，而且**整张表可能全是负数** ——
//         初值不能随手写 0。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """
// 参考答案 —— 二维数组
// 骨架顶上有 using System; / Collections.Generic; / Linq; / IO;，直接用就行。

// 1. 行和：第 行 那一行，把每一列加起来
//    ⚠ 列数是 表.GetLength(1) —— 不是 表.Length（那是总格数）
static int 行和(int[,] 表, int 行)
{
    int 和 = 0;
    for (int j = 0; j < 表.GetLength(1); j++)
    {
        和 += 表[行, j];
    }
    return 和;
}

// 2. 列和：结果的长度是**列数**，第 j 个装第 j 列的和
static int[] 列和(int[,] 表)
{
    int 行数 = 表.GetLength(0);
    int 列数 = 表.GetLength(1);
    int[] 出 = new int[列数];
    for (int j = 0; j < 列数; j++)
    {
        int 和 = 0;
        for (int i = 0; i < 行数; i++)
        {
            和 += 表[i, j];          // ⚠ 先 i 后 j，中间一个逗号
        }
        出[j] = 和;
    }
    return 出;
}

// 3. 转锯齿：行数 = GetLength(0)，每行的长度 = GetLength(1)
static int[][] 转锯齿(int[,] 表)
{
    int 行数 = 表.GetLength(0);
    int 列数 = 表.GetLength(1);
    int[][] 出 = new int[行数][];          // ⚠ 不是 new int[表.Length][]
    for (int i = 0; i < 行数; i++)
    {
        出[i] = new int[列数];             // 锯齿的每一行都要自己 new
        for (int j = 0; j < 列数; j++)
        {
            出[i][j] = 表[i, j];
        }
    }
    return 出;
}

// 4. 锯齿最大：每一行的长度**不一样**，所以内层要用 锯齿[i].Length
static int 锯齿最大(int[][] 锯齿)
{
    int 最大 = 锯齿[0][0];                 // ⚠ 初值取一个真实存在的数（不能写 0）
    for (int i = 0; i < 锯齿.Length; i++)
    {
        for (int j = 0; j < 锯齿[i].Length; j++)
        {
            if (锯齿[i][j] > 最大)
            {
                最大 = 锯齿[i][j];
            }
        }
    }
    return 最大;
}
""";

    // ══════════════════ 检查 ══════════════════

    [检查说明("四个方法都在，而且签名和题目一样")]
    public static CheckResult 检查_函数都在(string 代码)
    {
        // 探针：一次编译同时试四个方法。少任何一个，编译器都会说不认识这个名字。
        const string 探针 = """
int[,] 探针表 = new int[2, 2] { { 1, 2 }, { 3, 4 } };
int[][] 探针锯 = new int[1][] { new int[] { 5 } };
Console.WriteLine(行和(探针表, 0) + string.Join(",", 列和(探针表)) + 转锯齿(探针表).Length + 锯齿最大(探针锯));
""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 探针));
        if (run.编译通过) return CheckResult.过("四个方法都能调用，签名也对");

        string 信息 = run.编译信息;
        if (信息.Contains("CS0103") || 信息.Contains("不存在名称"))
            return CheckResult.不过(
                "有方法没找到 —— 编译器说「当前上下文中不存在名称」。\n" +
                "      这一课要求的四个签名是：\n" +
                "          static int    行和(int[,] 表, int 行)\n" +
                "          static int[]  列和(int[,] 表)\n" +
                "          static int[][] 转锯齿(int[,] 表)\n" +
                "          static int    锯齿最大(int[][] 锯齿)\n" +
                "      ⚠ 名字、参数类型、返回类型都要一致：\n" +
                "         int[] 和 int[][] 是两个不同的类型，少一对 [] 编译器就认不出来。");
        return CppCheck.编译失败说明(run);
    }

    [检查说明("行和：三种形状的表都对（2×3 / 5×2 / 1×4）")]
    public static CheckResult 检查_行和(string 代码)
    {
        var run = 跑一次(代码);
        return 比一比(run,
            ("行和甲0", "6", "行和(表甲, 0)　表甲是 2×3：1+2+3"),
            ("行和甲1", "15", "行和(表甲, 1)　4+5+6"),
            ("行和丙4", "10", "行和(表丙, 4)　表丙是 5×2：4+4 —— ⚠ 表丙的 Length 是 10，别把行数当 Length"),
            ("行和丁0", "34", "行和(表丁, 0)　表丁只有 1 行 4 列：7+8+9+10"));
    }

    [检查说明("列和：返回数组的长度必须是列数（不是行数、也不是 Length）")]
    public static CheckResult 检查_列和(string 代码)
    {
        var run = 跑一次(代码);
        return 比一比(run,
            ("列和甲", "5,7,9", "列和(表甲)　2×3 的表 → 3 列：1+4 / 2+5 / 3+6"),
            ("列和乙", "9,12", "列和(表乙)　3×2 的表 → **2** 列：1+3+5 / 2+4+6（⚠ 行数是 3，别拿行数当长度）"),
            ("列和丁", "7,8,9,10", "列和(表丁)　1 行 4 列 → 4 列，每列只有一个数"));
    }

    [检查说明("★ 转锯齿：行数用 GetLength(0)，不是 表.Length（5×2 的表最能把这条抓出来）")]
    public static CheckResult 检查_转锯齿(string 代码)
    {
        var run = 跑一次(代码);
        return 比一比(run,
            ("锯行数甲", "2", "转锯齿(表甲).Length　2×3 的表 → 2 行（⚠ 表甲的 Length 是 6）"),
            ("锯行数丙", "5", "转锯齿(表丙).Length　5×2 的表 → 5 行（⚠ 表丙的 Length 是 10 —— 拿它当行数这里会是 10）"),
            ("锯列数丙", "2", "转锯齿(表丙)[0].Length　每一行的长度是**列数** 2"),
            ("锯内容甲", "1,2,3;4,5,6", "转锯齿(表甲) 的内容"),
            ("锯内容乙", "1,2;3,4;5,6", "转锯齿(表乙) 的内容　3×2 的表 → 3 行、每行 2 个"));
    }

    [检查说明("★ 锯齿最大：每行长度不一样，全负数时初值写 0 会错")]
    public static CheckResult 检查_锯齿最大(string 代码)
    {
        var run = 跑一次(代码);
        return 比一比(run,
            ("锯最大一", "30", "锯齿最大(锯一)　锯一 = {1,9} / {30} / {4,5,6} —— 最大值在**第 2 行**，只按 锯[0].Length 走会漏掉它"),
            ("锯最大二", "-1", "锯齿最大(锯二)　整张表全是负数：{-9,-8} / {-1} —— 初值写 0 的话这里会返回 0"),
            ("锯最大三", "7", "锯齿最大(锯三)　只有一行一个数"));
    }

    [检查说明("源码里真的用了 GetLength / Length 问长度（不是把行数写死）")]
    public static CheckResult 检查_问了长度(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        string 转体 = 函数体(净, "转锯齿");
        if (转体 == null)
            缺.Add("      转锯齿：没找到这个方法体（花括号写全了吗）");
        else if (!转体.Contains("GetLength"))
            缺.Add("      转锯齿：没看到 GetLength —— 行数、列数都要**问**出来，不能写死");
        string 锯体 = 函数体(净, "锯齿最大");
        if (锯体 == null)
            缺.Add("      锯齿最大：没找到这个方法体");
        // ⚠ 判据要**宽**：`锯齿[i].Length` 和 `foreach (var 行 in 锯齿)` 都是对的写法，
        //   只卡 .Length 会把正确的 foreach 解法判错（我第一版就是这么写的）。
        else if (!锯体.Contains(".Length") && !锯体.Contains("foreach"))
            缺.Add("      锯齿最大：每一行的长度要**问**出来 —— 用 锯齿[i].Length，" +
                   "或者用 foreach 直接走每一行");

        if (缺.Count == 0) return CheckResult.过("长度都是从数据里问出来的");
        return CheckResult.不过(
            "检查了去掉注释后的源码，发现问题：\n" + string.Join("\n", 缺) + "\n" +
            "      这一课要练的正是「长度从哪儿问」：\n" +
            "          矩形  int[,]    行数 GetLength(0)、列数 GetLength(1)、Length 是总格数\n" +
            "          锯齿  int[][]   行数 Length、第 i 行的列数 锯[i].Length\n" +
            "      写死成 3 行 2 列，换一张表（检查里就换了）立刻就错。");
    }

    // ══════════════════ 跑一次、比一比 ══════════════════

    /// <summary>这一课所有用例的调用点：准备数据 → 每次调用打一行「标记|值」。</summary>
    private static string 调用点() => 造调用点("""
int[,] 表甲 = new int[2, 3] { { 1, 2, 3 }, { 4, 5, 6 } };
int[,] 表乙 = new int[3, 2] { { 1, 2 }, { 3, 4 }, { 5, 6 } };
int[,] 表丙 = new int[5, 2] { { 1, 1 }, { 2, 2 }, { 3, 3 }, { 4, 4 }, { 5, 5 } };
int[,] 表丁 = new int[1, 4] { { 7, 8, 9, 10 } };
int[][] 锯一 = new int[][] { new int[] { 1, 9 }, new int[] { 30 }, new int[] { 4, 5, 6 } };
int[][] 锯二 = new int[][] { new int[] { -9, -8 }, new int[] { -1 } };
int[][] 锯三 = new int[][] { new int[] { 7 } };
""",
            ("行和甲0", "行和(表甲, 0)"),
            ("行和甲1", "行和(表甲, 1)"),
            ("行和丙4", "行和(表丙, 4)"),
            ("行和丁0", "行和(表丁, 0)"),
            ("列和甲", "string.Join(\",\", 列和(表甲))"),
            ("列和乙", "string.Join(\",\", 列和(表乙))"),
            ("列和丁", "string.Join(\",\", 列和(表丁))"),
            ("锯行数甲", "转锯齿(表甲).Length"),
            ("锯行数丙", "转锯齿(表丙).Length"),
            ("锯列数丙", "转锯齿(表丙)[0].Length"),
            ("锯内容甲", "string.Join(\";\", 转锯齿(表甲).Select(行 => string.Join(\",\", 行)))"),
            ("锯内容乙", "string.Join(\";\", 转锯齿(表乙).Select(行 => string.Join(\",\", 行)))"),
            ("锯最大一", "锯齿最大(锯一)"),
            ("锯最大二", "锯齿最大(锯二)"),
            ("锯最大三", "锯齿最大(锯三)"));

    // 一次检查会给每一课的所有检查方法各跑一次，而它们的输入是同一份代码 ——
    // 编译一次就够了（每次编译要一两秒）。
    private static string 上次代码;
    private static CppRun 上次运行;

    private static CppRun 跑一次(string 代码)
    {
        if (上次运行 != null && 上次代码 == 代码) return 上次运行;
        上次代码 = 代码;
        上次运行 = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点()));
        return 上次运行;
    }

    /// <summary>
    /// 拼调用点：每组各自 try/catch。
    /// ⚠ 学生代码抛异常时**只影响那一组** —— 不然会变成「程序崩了」，
    ///   所有检查一起红，学生看不出是哪一组出的问题。
    /// </summary>
    private static string 造调用点(string 准备, params (string 标记, string 表达式)[] 用例们)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(准备).Append('\n');
        foreach (var (标记, 表达式) in 用例们)
        {
            sb.Append("try { Console.WriteLine(\"").Append(标记).Append("|\" + (")
              .Append(表达式)
              .Append(")); } catch (Exception __e) { Console.WriteLine(\"")
              .Append(标记).Append("|崩溃:\" + __e.GetType().Name); }\n");
        }
        return sb.ToString();
    }

    /// <summary>比一批「标记|值」—— 学生自己多打几行输出也不影响对答案。</summary>
    private static CheckResult 比一比(CppRun run,
        params (string 标记, string 期望, string 说明)[] 用例们)
    {
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        var 错 = new List<string>();
        foreach (var (标记, 期望, 说明) in 用例们)
        {
            string 得到 = 取标记(run, 标记);
            if (得到 == null) 错.Add("      " + 说明 + "：程序没有给出这一组的输出");
            else if (得到.StartsWith("崩溃", StringComparison.Ordinal))
                错.Add("      " + 说明 + "：这一组**抛异常**了（" + 得到 + "）");
            else if (得到 != 期望)
                错.Add("      " + 说明 + "：得到 " + 显示(得到) + "，应该是 " + 显示(期望));
        }
        if (错.Count == 0) return CheckResult.过("这几组都对");

        return CheckResult.不过(
            "结果不对：\n" + string.Join("\n", 错) + "\n" +
            "      两种表的走法：\n" +
            "          矩形  for (i < 表.GetLength(0)) for (j < 表.GetLength(1)) 取 表[i, j]\n" +
            "          锯齿  for (i < 锯.Length)      for (j < 锯[i].Length)   取 锯[i][j]\n" +
            "      ⚠ 最容易错的是拿 表.Length（总格数）当行数 —— 编译通过、结果全错。");
    }

    /// <summary>取出「标记|」这一行后面的值。没有这一行就返回 null。</summary>
    private static string 取标记(CppRun run, string 标记)
    {
        string 头 = 标记 + "|";
        foreach (var l in run.行)
        {
            string s = l.Trim();
            if (s.StartsWith(头, StringComparison.Ordinal)) return s.Substring(头.Length).Trim();
        }
        return null;
    }

    private static string 显示(string s) => s.Length == 0 ? "（空）" : s;

    /// <summary>去掉注释，免得注释里的字样骗过文本检查。</summary>
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

    /// <summary>取某个方法名的「{ … }」内容（配对花括号）。找不到返回 null。</summary>
    private static string 函数体(string 净源码, string 方法名)
    {
        int 名 = 净源码.IndexOf(方法名 + "(", StringComparison.Ordinal);
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
