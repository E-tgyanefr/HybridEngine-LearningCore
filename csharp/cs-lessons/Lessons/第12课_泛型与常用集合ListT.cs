using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("12", "第 12 课：泛型与常用集合 —— `List<T>` 与 `Dictionary<TKey,TValue>`",
    """
1. 写 `static int 数满足<T>(List<T> 们, Func<T, bool> 条件)` —— 返回满足条件的元素个数
2. 写 `static bool 查到了(Dictionary<string, int> 表, string 键, out int 值)` —— 用 `TryGetValue`
3. 写 `static string 名次表(Dictionary<string, int> 表)` —— 按分数从高到低排，每行 `"名字 分数"`，用换行 `\n` 连接，末尾不带换行；空字典返回 `""`
""")]
public static class 第12课
{
    public static string 模板 => """
// 第 12 课：泛型与常用集合 —— List<T> 与 Dictionary<TKey,TValue>
//
// 这一课讲两件事：**泛型 <T>**（一份代码适用于多种类型）、
// **Dictionary**（按名字找东西，比列表快得多）。
//
// ─────────────────────────────────────────────────────
//  先介绍：为什么要泛型
// ─────────────────────────────────────────────────────
//
// 想写一个「数一数列表里有几个符合条件的」，列表里装 int 还是 string？
// 不用泛型就得给每种类型写一遍。
//
//     static int 数满足<T>(List<T> 们, Func<T, bool> 条件)
//                      ↑ T 是占位符：调用时自动换成实际类型
//
//     Func<T, bool>      ← 「收一个 T，返回一个 bool」的函数
//
// 用起来：
//
//     数满足(new List<int> { 1, 2, 3 }, 数 => 数 > 1)          ⇒ 2
//     数满足(new List<string> { "a", "" }, 字 => 字 != "")     ⇒ 1
//
// ⚠ 同一份代码，两种类型 —— 这就是泛型的价值。
//
// ─────────────────────────────────────────────────────
//  二、Dictionary：按名字找，不用一个个翻
// ─────────────────────────────────────────────────────
//
//     var 表 = new Dictionary<string, int>();
//     表["小明"] = 92;              存
//     表["小明"]                    取（键不存在会**抛异常**）
//     表.ContainsKey("小明")        true / false
//
// ⚠ 安全的取法（键可能不在时**必须**用它）：
//
//     表.TryGetValue("小明", out int 值)
//     ⇒ 键在：返回 true，值 被赋上；键不在：返回 false，值 = 0
//
//   ⚠ 直接用 表["不存在的键"] 会抛 KeyNotFoundException ——
//     而 TryGetValue 不会。真实代码里几乎都用后者。
//
// ─────────────────────────────────────────────────────
//  三、排序：OrderBy / OrderByDescending
// ─────────────────────────────────────────────────────
//
//     var 排好的 = 表.OrderByDescending(项 => 项.Value);
//                                              ↑ 按值（分数）排
//     项.Key      名字
//     项.Value    分数
//
// ⚠ 排序结果是一条**查询**，不是列表 —— 要遍历它或 ToList() 才会真的算。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：out 参数漏赋值
// ─────────────────────────────────────────────────────
//
//     static bool 查到了(Dictionary<string, int> 表, string 键, out int 值)
//     {
//         if (表.ContainsKey(键)) { 值 = 表[键]; return true; }
//         return false;            ← 这条路 值 没赋值
//     }
//
// 你会看到（csc 原文）：
//
//     错误 CS0177: 在控件离开当前方法之前，必须分配输出参数
//
// 为什么：`out` 是给调用方的承诺 ——「我保证这个方法一定会给这个参数一个值」。
//   少一条路径赋值，承诺就破了。编译器逐条路径检查它。
//
// ⇒ 用 `TryGetValue` 正好天然满足（它两种情况下都会给 值 赋值）：
//
//     return 表.TryGetValue(键, out 值);
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   异常处理 → 第 13 课。LINQ 的 Where / Select → 第 15 课。
//
// ─────────────────────────────────────────────────────
//  ══ 轮到你了 ══
// ─────────────────────────────────────────────────────
//
//   写三个方法：
//
//     static int 数满足<T>(List<T> 们, Func<T, bool> 条件)
//
//     static bool 查到了(Dictionary<string, int> 表, string 键, out int 值)
//         —— 键在 ⇒ true 且 值 = 表[键]；键不在 ⇒ false（用 TryGetValue）
//
//     static string 名次表(Dictionary<string, int> 表)
//         —— 按分数**从高到低**排，每行 "名字 分数"，用 '\n' 连接
//            空字典返回 ""（没有任何多余换行）
//
//   ⚠ `out` 参数在方法里**每条路径都必须赋值**
//     （第 05 课讲过）—— 用 `TryGetValue` 正好天然满足。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """

static int 数满足<T>(List<T> 们, Func<T, bool> 条件)
{
    int 计 = 0;
    foreach (var 项 in 们)
    {
        if (条件(项)) 计++;
    }
    return 计;
}

static bool 查到了(Dictionary<string, int> 表, string 键, out int 值)
{
    // ⚠ 不能写 值 = 表[键]; —— 键不在会抛 KeyNotFoundException
    return 表.TryGetValue(键, out 值);
}

static string 名次表(Dictionary<string, int> 表)
{
    if (表.Count == 0) return "";

    var 行 = 表.OrderByDescending(对 => 对.Value)
               .ThenBy(对 => 对.Key, StringComparer.Ordinal)   // 同分时按名字排，结果稳定
               .Select(对 => 对.Key + " " + 对.Value);

    return string.Join("\n", 行);      // ⚠ 末尾不会多一个换行
}
""";


    [检查说明("数满足 用了泛型参数 T")]
    public static CheckResult 检查_1(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("<T>"))
            return CheckResult.不过("没找到「<T>」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("查到了 用的是 TryGetValue")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("TryGetValue"))
            return CheckResult.不过("没找到「TryGetValue」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("数满足：int 与 string 两种 T 都对")]
    public static CheckResult 检查_3(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(数满足(new List<int>{1,2,3,4}, n => n % 2 == 0));
    Console.WriteLine(数满足(new List<string>{"甲","乙丙"}, s => s.Length == 1));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "2",
            "1"
        };
        string[] 输入 = {
            "数满足(new List<int>{1,2,3,4}, n => n % 2 == 0)",
            "数满足(new List<string>{\"甲\",\"乙丙\"}, s => s.Length == 1)"
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

    [检查说明("查到了：键在 / 键不在两条路都对")]
    public static CheckResult 检查_4(string 代码)
    {
        var 调用点 = """
    int v1;
    Console.WriteLine(查到了(new Dictionary<string,int>{["甲"]=90}, "甲", out v1) + ":" + v1);
    int v2;
    Console.WriteLine(查到了(new Dictionary<string,int>{["甲"]=90}, "乙", out v2));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "True:90",
            "False"
        };
        string[] 输入 = {
            "int v1; ⇒ 查到了(new Dictionary<string,int>{[\"甲\"]=90}, \"甲\", out v1) + \":\" + v1",
            "int v2; ⇒ 查到了(new Dictionary<string,int>{[\"甲\"]=90}, \"乙\", out v2)"
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

    [检查说明("名次表：按分数从高到低")]
    public static CheckResult 检查_5(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(名次表(new Dictionary<string,int>{["甲"]=70,["乙"]=90,["丙"]=80}).Replace("\n", " / "));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "乙 90 / 丙 80 / 甲 70"
        };
        string[] 输入 = {
            "new Dictionary<string,int>{[\"甲\"]=70,[\"乙\"]=90,[\"丙\"]=80}"
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
