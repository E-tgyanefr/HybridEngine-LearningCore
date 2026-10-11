using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("15", "第 15 课：LINQ 入门 —— `Where` / `Select` / `OrderBy` 与延迟执行",
    """
1. 写 `static int[] 挑偶数(int[] 数们)` —— 用 `Where` 挑出偶数，返回**数组**（用 `ToArray()`）
2. 写 `static List<成绩记录> 加一分(List<成绩记录> 们)` —— 用 `Select` 造出"分数 +1"的**新**记录（原列表一个字都不能变），返回 `List<成绩记录>`
3. 写 `static void 演示延迟()` —— 建一个 `List<int> 数们 = new List<int> { 1, 2, 3 }`，先定义查询 `var 查 = 数们.Where(数 => 数 > 1);`，然后 `数们.Add(99);`，**最后**才 `foreach` 打印 —— 每行一个数，没有别的话
4. `record 成绩记录(string 名字, int 分数);` 由你自己补上（第 14 课写过）
""")]
public static class 第15课
{
    public static string 模板 => """
// 第 15 课：LINQ 入门 —— Where / Select / OrderBy 与延迟执行
//
// 这一课讲一件事：**用一句话筛选、变换、排序一串数据**。
//
// ─────────────────────────────────────────────────────
//  先介绍：不用 LINQ 要写什么
// ─────────────────────────────────────────────────────
//
// 「挑出所有偶数」不用 LINQ 要写：
//
//     var 结果 = new List<int>();
//     foreach (var 数 in 数们) { if (数 % 2 == 0) { 结果.Add(数); } }
//
// 用 LINQ 是一行：
//
//     var 结果 = 数们.Where(数 => 数 % 2 == 0).ToArray();
//
// ─────────────────────────────────────────────────────
//  二、三个最常用的
// ─────────────────────────────────────────────────────
//
//     Where(条件)        挑出满足条件的    ← 筛选
//     Select(变换)       把每个变成别的东西 ← 变换
//     OrderBy / OrderByDescending(键)      ← 排序
//
//     int[] 数们 = { 1, 2, 3, 4, 5 };
//     数们.Where(数 => 数 % 2 == 0)              ⇒ 2, 4
//     数们.Select(数 => 数 * 10)                 ⇒ 10, 20, …
//     数们.OrderByDescending(数 => 数)            ⇒ 5, 4, 3, …
//
// ⚠ `数 => 数 % 2 == 0` 是 **lambda**：读作「给我一个 数，我告诉你它是不是偶数」。
//
// ⚠ 这些方法返回的是**查询**（IEnumerable），不是数组/列表。
//   要变成数组加 `.ToArray()`，要变成列表加 `.ToList()`。
//
// ─────────────────────────────────────────────────────
//  三、延迟执行：查询定义的时候不算
// ─────────────────────────────────────────────────────
//
//     var 数们 = new List<int> { 1, 2, 3 };
//     var 查 = 数们.Where(数 => 数 > 1);     ← 只是**记下**这个条件，没算
//     数们.Add(99);                          ← 改数据
//     foreach (var 数 in 查) { ... }         ← **现在**才算 ⇒ 2, 3, 99
//
// ⚠ 99 也出来了！因为查询是**枚举的那一刻**才去读数据的。
//   这就是「延迟执行」。
//
//   好处：省事、可以拼很多步再一起算。
//   代价：你以为拿到的是「当时那一刻的快照」，其实不是。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：以为查询是快照
// ─────────────────────────────────────────────────────
//
//     var 查 = 数们.Where(数 => 数 > 1);
//     var 定下来 = 查.ToList();              ← 加了这句才是快照
//     数们.Add(99);
//
//     foreach (var 数 in 查) { }              ⇒ 2, 3, 99    ← 跟着变了
//     foreach (var 数 in 定下来) { }          ⇒ 2, 3        ← 没变
//
// 你会看到：**什么都不报**，只是两个结果不一样。
//
// 为什么：`查` 只是一段「怎么算」的说明；`定下来` 是**真的算完存好**的列表。
//
// ⇒ 想要快照，就 `.ToList()` / `.ToArray()` 一下。
//   这一课的 演示延迟 **就是要你亲眼看到 99 出现** ——
//   它出现才证明你真的把查询定义在 Add 之前了。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   GroupBy / Join / 聚合 → 这门课不涉及。
//
// ─────────────────────────────────────────────────────
//  ══ 轮到你了 ══
// ─────────────────────────────────────────────────────
//
//     static int[] 挑偶数(int[] 数们)              // Where + ToArray
//
//     static List<成绩记录> 加一分(List<成绩记录> 们)   // Select 造**新**记录
//         —— 原列表里的记录一个都不能被改
//
//     static void 演示延迟()                        // 见下面的步骤
//         var 数们 = new List<int> { 1, 2, 3 };
//         var 查 = 数们.Where(数 => 数 > 1);         // 先定义
//         数们.Add(99);                              // 再改数据
//         foreach (var 数 in 查) Console.WriteLine(数);   // 最后才枚举
//
//     record 成绩记录(string 名字, int 分数);
//
//   ⚠ `演示延迟` 的输出应该是三行：2 / 3 / 99 —— 99 必须出现，
//     它出现才证明你真的把查询定义在 Add 之前了。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """

record 成绩记录(string 名字, int 分数);

static int[] 挑偶数(int[] 数们)
{
    return 数们.Where(数 => 数 % 2 == 0).ToArray();
}

static List<成绩记录> 加一分(List<成绩记录> 们)
{
    // ⚠ 位置式 record 的属性是 init-only，改不了 ⇒ 只能用 with 造新对象
    //    （所以"原列表不变"是天然成立的，但你不能去改 项.分数）
    return 们.Select(项 => 项 with { 分数 = 项.分数 + 1 }).ToList();
}

static void 演示延迟()
{
    var 数们 = new List<int> { 1, 2, 3 };

    // ⚠⚠ 这一行只是"配方"：一个元素都还没看
    var 查 = 数们.Where(数 => 数 > 1);

    // 查询定义**之后**才加的数据
    数们.Add(99);

    // 直到这里才真的开跑 —— 所以 99 也在结果里
    foreach (var 数 in 查)
    {
        Console.WriteLine(数);
    }
}
""";


    [检查说明("用了 Where 和 Select")]
    public static CheckResult 检查_1(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains(".Where("))
            return CheckResult.不过("没找到「.Where(」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("Select 用的是 with 造新记录（不是改原对象）")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("with {"))
            return CheckResult.不过("没找到「with {」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("挑偶数：多组都对")]
    public static CheckResult 检查_3(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(string.Join(",", 挑偶数(new int[]{1,2,3,4,5})));
    Console.WriteLine(string.Join(",", 挑偶数(new int[]{0,2})));
    Console.WriteLine(string.Join(",", 挑偶数(new int[]{2,4,6})));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "2,4",
            "0,2",
            "2,4,6"
        };
        string[] 输入 = {
            "new int[]{1,2,3,4,5}",
            "new int[]{0,2}",
            "new int[]{2,4,6}"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count
                ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
            {
                string 这是 = i < 输入.Length ? 输入[i] : "";
                return CheckResult.不过(
                    $"输入 [{这是}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 多组输入都对才算对 —— 只对一组可能是碰巧。");
            }
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("加一分：分数加一，而且是新对象")]
    public static CheckResult 检查_4(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(加一分(new List<成绩记录>{ new 学生代码.成绩记录("甲", 59) })[0].分数);
    Console.WriteLine(加一分(new List<成绩记录>{ new 学生代码.成绩记录("甲", 59) })[0].名字);

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "60",
            "甲"
        };
        string[] 输入 = {
            "加一分(new List<成绩记录>{ new 成绩记录(\"甲\", 59) })[0].分数",
            "加一分(new List<成绩记录>{ new 成绩记录(\"甲\", 59) })[0].名字"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count
                ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
            {
                string 这是 = i < 输入.Length ? 输入[i] : "";
                return CheckResult.不过(
                    $"输入 [{这是}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 要把每一步都跑对 —— 中间任何一步错了，后面的结果就对不上。");
            }
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("延迟执行：查询定义在 Add 之前 ⇒ 99 必须出现")]
    public static CheckResult 检查_5(string 代码)
    {
        var 调用点 = """
    演示延迟();
    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine();

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "2",
            "3",
            "99"
        };
        string[] 输入 = {
            "演示延迟(); ⇒ ",
            "",
            ""
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count
                ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
            {
                string 这是 = i < 输入.Length ? 输入[i] : "";
                return CheckResult.不过(
                    $"输入 [{这是}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 要把每一步都跑对 —— 中间任何一步错了，后面的结果就对不上。");
            }
        }
        return CheckResult.过("每一组都对");
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
