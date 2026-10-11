using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("16", "第 16 课：综合练习 —— 学生成绩统计",
    """
1. 写 `static (int 人数, int 及格, double 平均, string 头名) 统计(List<成绩记录> 们, int 及格线)` —— 空列表返回 `(0, 0, 0, "")`；否则人数、及格人数、平均分（`Math.Round` 保留两位小数）、分数最高者的名字（同分按名字 `StringComparer.Ordinal` 排，保证稳定）
2. 写 `static bool 不及格(成绩记录 r, int 及格线)` —— 一行，用模式匹配
3. 写 `static string 报警(成绩记录 r)` —— 用 switch 表达式：空名字 ⇒ `"名字是空的"`、分数 `< 0` 或 `> 100` ⇒ `"数据异常"`、`< 60` ⇒ `"不及格"`、其余 ⇒ `"正常"`
4. 写 `static List<string> 名字们(List<成绩记录> 们)` —— LINQ：筛掉空名字 → 按分数从高到低 → 取名字 → `ToList`
5. `record 成绩记录(string 名字, int 分数);` 自己补上
""")]
public static class 第16课
{
    public static string 模板 => """
// 第 16 课：综合练习 —— 学生成绩统计
//
// 这一课**不教新东西**。前面 15 课学的，这一课全部用一遍。
//
// ─────────────────────────────────────────────────────
//  先介绍：为什么要有综合练习
// ─────────────────────────────────────────────────────
//
// 单独会用一个语法点，和**把它们拼起来做成一件事**，是两种能力。
// 这一课就是把零散的点连起来：一条数据 → 一堆数据 → 算出来 → 报出来。
//
// ─────────────────────────────────────────────────────
//  一、这一课会用到什么
// ─────────────────────────────────────────────────────
//
//     第 09 课   record（一条成绩记录）
//     第 12 课   List<T>（一堆记录）
//     第 14 课   模式匹配（写「不及格」这种判断）
//     第 15 课   LINQ（筛、排、取名字）
//
// ─────────────────────────────────────────────────────
//  二、元组返回：一次交出四个数
// ─────────────────────────────────────────────────────
//
//     static (int 人数, int 及格, double 平均, string 头名) 统计(
//         List<成绩记录> 们, int 及格线)
//
// ⚠ 空列表要返回 `(0, 0, 0, "")` —— 别去算平均（会除以 0）。
// ⚠ 平均分**保留两位小数**：`Math.Round(x, 2)`。
//
// ─────────────────────────────────────────────────────
//  三、「头名」为什么要排序而不是找最大
// ─────────────────────────────────────────────────────
//
//     同分的时候，谁算头名？
//
// 不规定的话结果会随数据顺序变化 —— 检查这次过、下次不过。
// 本课的规定是：**同分按名字 Ordinal 排**，这样结果稳定。
//
//     var 排好的 = 们.OrderByDescending(r => r.分数)
//                    .ThenBy(r => r.名字, StringComparer.Ordinal);
//     头名 = 排好的.First().名字;
//
// ⚠ `ThenBy` 是第二排序键 —— 第一个键相同时才看它。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：空列表没先拦掉
// ─────────────────────────────────────────────────────
//
//     static (int 人数, int 及格, double 平均, string 头名) 统计(
//         List<成绩记录> 们, int 及格线)
//     {
//         var 平均 = 们.Average(r => r.分数);        ← 列表空的时候
//         var 头名 = 们.OrderByDescending(...).First().名字;   ← 也是
//     }
//
// 你会看到：遍历空列表时 `Average()` 抛
//   `InvalidOperationException: Sequence contains no elements`，
//   或者 `First()` 抛同一句话 —— 界面上一片红。
//
// 为什么：平均值要除以个数；一个都没有的时候它没法给答案。
//   `First()` 更是直接说「我要第一个」，可一个都没有。
//
// ⇒ **函数的第一句就拦掉空列表**：
//
//     if (们.Count == 0) { return (0, 0, 0, ""); }
//
//   ⚠ 这条不只是这一课 —— `Average()`、`Max()`、`First()` 在空集合上
//     全都会抛。检查里专门有一组空列表的用例等在这里。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   没有新语法。做不出来就回头翻对应的那一课。
//
// ─────────────────────────────────────────────────────
//  ══ 轮到你了 ══
// ─────────────────────────────────────────────────────
//
//     record 成绩记录(string 名字, int 分数);
//
//     static (int 人数, int 及格, double 平均, string 头名) 统计(
//         List<成绩记录> 们, int 及格线)
//         —— 空列表 ⇒ (0, 0, 0, "")
//            平均分**保留两位小数**（用 Math.Round(x, 2)）
//            头名 = 分数最高者的名字（同分按名字 Ordinal 排，保证稳定）
//
//     static bool 不及格(成绩记录 r, int 及格线)
//         —— 一行，用模式匹配
//
//     static string 报警(成绩记录 r)
//         —— switch 表达式，四种结果
//
//     static List<string> 名字们(List<成绩记录> 们)
//         —— 筛空名字 → 按分数降序 → 取名字 → ToList
//
//   ⚠ 元组返回的**平均分是 double 且已经四舍五入到两位**。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」—— 这一课综合用到了前面各课，红了先看是哪一条。
//     卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """

record 成绩记录(string 名字, int 分数);

static (int 人数, int 及格, double 平均, string 头名) 统计(
    List<成绩记录> 们, int 及格线)
{
    // ⚠ 统计函数的第一件事永远是"空的时候怎么办"
    //   Average / First 在空序列上都会抛 InvalidOperationException
    if (们.Count == 0)
    {
        return (0, 0, 0, "");
    }

    int 人数 = 们.Count;
    int 及格数 = 们.Count(项 => 项.分数 >= 及格线);
    double 平均 = Math.Round(们.Average(项 => 项.分数), 2);   // Average 自己就是 double

    // ⚠ 同分时加一级名字排序，结果才稳定（不然 First 取到谁不保证）
    string 头名 = 们.OrderByDescending(项 => 项.分数)
                    .ThenBy(项 => 项.名字, StringComparer.Ordinal)
                    .Select(项 => 项.名字)
                    .First();

    return (人数, 及格数, 平均, 头名);
}

static bool 不及格(成绩记录 r, int 及格线)
{
    // 模式匹配一行搞定 —— 等价于 r.分数 < 及格线
    // ⚠ 这里**不能**写 `r is { 分数: < 及格线 }` —— 模式里的关系运算符
    //   要求右边是**编译期常量**，而 及格线 是参数：
    //       error CS9135: 类型"int"的常量值应为…
    //   ⇒ 变量比较就用普通的 < 。
    return r.分数 < 及格线;
}

static string 报警(成绩记录 r) => r switch
{
    // ⚠ 顺序：数据本身不合法的最优先说
    { 名字: "" }           => "名字是空的",
    { 分数: < 0 or > 100 } => "数据异常",
    { 分数: < 60 }         => "不及格",
    _                      => "正常",
};

static List<string> 名字们(List<成绩记录> 们)
{
    // ⚠ 顺序不能乱：先筛、再排、后投影、最后收口
    //   把 Select 提到 OrderBy 前面 ⇒ 变成按"名字"的字典序排，而且不报错
    return 们.Where(项 => 项.名字 != "")
             .OrderByDescending(项 => 项.分数)
             .Select(项 => 项.名字)
             .ToList();
}
""";


    [检查说明("用了 record 定义成绩记录")]
    public static CheckResult 检查_1(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("record 成绩记录"))
            return CheckResult.不过("没找到「record 成绩记录」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("统计的返回值是元组（不是 out 参数）")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("(int 人数"))
            return CheckResult.不过("没找到「(int 人数」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("报警：名字空的优先级最高，四种结果都对")]
    public static CheckResult 检查_3(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(报警(new 学生代码.成绩记录("", 200)));
    Console.WriteLine(报警(new 学生代码.成绩记录("甲", 200)));
    Console.WriteLine(报警(new 学生代码.成绩记录("甲", 59)));
    Console.WriteLine(报警(new 学生代码.成绩记录("甲", 60)));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "名字是空的",
            "数据异常",
            "不及格",
            "正常"
        };
        string[] 输入 = {
            "new 成绩记录(\"\", 200)",
            "new 成绩记录(\"甲\", 200)",
            "new 成绩记录(\"甲\", 59)",
            "new 成绩记录(\"甲\", 60)"
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

    [检查说明("统计：人数 / 及格 / 平均 / 头名 都对")]
    public static CheckResult 检查_4(string 代码)
    {
        var 调用点 = """
    Console.WriteLine($"({统计(new List<成绩记录>{ new 成绩记录("甲", 90), new 成绩记录("乙", 80), new 成绩记录("丙", 90) }, 85).Item1}, {统计(new List<成绩记录>{ new 成绩记录("甲", 90), new 成绩记录("乙", 80), new 成绩记录("丙", 90) }, 85).Item2}, {统计(new List<成绩记录>{ new 成绩记录("甲", 90), new 成绩记录("乙", 80), new 成绩记录("丙", 90) }, 85).Item3}, {统计(new List<成绩记录>{ new 成绩记录("甲", 90), new 成绩记录("乙", 80), new 成绩记录("丙", 90) }, 85).Item4})");

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "(3, 2, 86.67, 丙)"
        };
        string[] 输入 = {
            "new List<成绩记录>{ new 成绩记录(\"甲\", 90), new 成绩记录(\"乙\", 80), new 成绩记录(\"丙\", 90) }, 85"
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
                    "      ⚠ 多组输入都对才算对。");
            }
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("统计：空列表不抛异常")]
    public static CheckResult 检查_5(string 代码)
    {
        var 调用点 = """
    Console.WriteLine($"({统计(new List<成绩记录>(), 60).Item1}, {统计(new List<成绩记录>(), 60).Item2}, {统计(new List<成绩记录>(), 60).Item3}, {统计(new List<成绩记录>(), 60).Item4})");

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "(0, 0, 0, )"
        };
        string[] 输入 = {
            "new List<成绩记录>(), 60"
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
                    "      ⚠ 多组输入都对才算对。");
            }
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("报警：名字空的优先级最高，四种结果都对")]
    public static CheckResult 检查_6(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(报警(new 学生代码.成绩记录("", 200)));
    Console.WriteLine(报警(new 学生代码.成绩记录("甲", 200)));
    Console.WriteLine(报警(new 学生代码.成绩记录("甲", 59)));
    Console.WriteLine(报警(new 学生代码.成绩记录("甲", 60)));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "名字是空的",
            "数据异常",
            "不及格",
            "正常"
        };
        string[] 输入 = {
            "new 成绩记录(\"\", 200)",
            "new 成绩记录(\"甲\", 200)",
            "new 成绩记录(\"甲\", 59)",
            "new 成绩记录(\"甲\", 60)"
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

    [检查说明("名字们：空名字被筛掉，顺序是按分数降序")]
    public static CheckResult 检查_7(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(string.Join(",", 名字们(new List<成绩记录>{ new 学生代码.成绩记录("甲", 70), new 学生代码.成绩记录("", 99), new 学生代码.成绩记录("乙", 88) })));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "乙,甲"
        };
        string[] 输入 = {
            "string.Join(\",\", 名字们(new List<成绩记录>{ new 成绩记录(\"甲\", 70), new 成绩记录(\"\", 99), new 成绩记录(\"乙\", 88) }))"
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
