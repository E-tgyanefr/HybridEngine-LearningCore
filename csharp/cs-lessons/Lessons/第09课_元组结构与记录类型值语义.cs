using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("09", "第 09 课：元组、结构与记录类型 —— 值语义 vs 引用语义",
    """
1. 定义 `struct 分数 { public int 得分; }`，写 `static 分数 数值改不掉(分数 s)` —— 里面写 `s.得分 = 90;` 然后 `return s;`
2. 定义记录类型 `record 成绩记录(string 名字, int 分数);`，写 `static (string 名字, bool 及格) 取名字(成绩记录 r)` —— 及格是 `分数 >= 60`
3. 定义 `static (int 最低, int 最高) 取最高最低(int[] 数们)` —— 数组为空时返回 `(0, 0)`
""")]
public static class 第09课
{
    public static string 模板 => """
// 第 09 课：元组、结构与记录类型 —— 值语义 vs 引用语义
//
// 这一课讲三样新东西：**元组**（一次返回好几个值）、**struct**、**record**。
//
// ─────────────────────────────────────────────────────
//  先介绍：为什么需要它们
// ─────────────────────────────────────────────────────
//
// 想一次返回「最低分和最高分」两个数，方法只能返回一个值 —— 怎么办？
// 想表达「一条成绩记录」，用 class 又太重 —— 怎么办？
// 这一课的三样东西各回答一个。
//
// ─────────────────────────────────────────────────────
//  一、元组：一次返回好几个值
// ─────────────────────────────────────────────────────
//
//     static (int 最低, int 最高) 取最高最低(int[] 数们)
//     {
//         return (1, 9);            ← 用括号一次交出去两个
//     }
//
//     var r = 取最高最低(数们);
//     Console.WriteLine(r.最低);    ← 按名字取，不用记住第几个
//
// ⚠ 返回类型那里写了名字（`int 最低`），所以调用方能用 `.最低` ——
//   这是元组最好用的地方。不写名字就只能 `.Item1`、`.Item2`。
//
// ─────────────────────────────────────────────────────
//  二、struct：值语义（赋值 = 复制一份）
// ─────────────────────────────────────────────────────
//
//     struct 分数 { public int 得分; }
//
//     分数 a = new 分数();
//     a.得分 = 10;
//     分数 b = a;              ← **复制**一份，b 和 a 无关
//     b.得分 = 99;
//     Console.WriteLine(a.得分);      ⇒ 10    ← a 没被影响
//
// ⚠ 对比 class（第 07 课学过）：
//
//     struct   值语义   赋值 = 复制一份
//     class    引用语义 赋值 = 两个名字指向同一个对象
//
// ⚠ 还有一件**容易上当**的事：struct 当参数传进去，函数里改的是**副本**：
//
//     static 分数 数值改不掉(分数 s) { s.得分 = 90; return s; }
//
//   这个函数的**返回值**是 90，但**调用者的变量一点没变**。
//   这和第 08 课 Java 那个「值传递」是同一个现象。
//
// ─────────────────────────────────────────────────────
//  三、record：一句话定义一条不可变记录
// ─────────────────────────────────────────────────────
//
//     record 成绩记录(string 名字, int 分数);      ← ⚠ 末尾有分号！
//
// 这一行就够，编译器替你生成好了：
//
//     · 构造方法  new 成绩记录("小明", 92)
//     · 两个只读属性  .名字  .分数
//     · **按内容**比较的 == （不像 class 比地址）
//
// ⚠ 值语义 + 内容相等 —— 这就是 record 存在的理由：
//   「一条数据」用它，比 class 省事得多。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：以为 struct 传进函数里改了就会变
// ─────────────────────────────────────────────────────
//
//     struct 分数 { public int 得分; }
//
//     static void 改它(分数 s) { s.得分 = 90; }
//
//     分数 a = new 分数();
//     改它(a);
//     Console.WriteLine(a.得分);      ⇒ 0     ← 你以为变了
//
// 你会看到：**什么都不报**，只是 a 一点没变。
//
// 为什么：struct 是**值语义** —— 传参数时复制了一份，函数里改的是副本。
//   副本在函数结束时就被扔了。
//
// ⇒ 两条出路：
//     ① 让函数**返回**改好的那个（本课要求的就是这种）；
//     ② 改用 class（引用语义，函数里改的就是原对象）。
//
//   ⚠ 这也解释了为什么 .NET 里的大多数类型是 class ——
//     值语义在「传进去想改」的时候特别容易踩。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   属性与访问控制 → 第 10 课。record 的 with 表达式 → 这门课不涉及。
//
// ─────────────────────────────────────────────────────
//  ══ 轮到你了 ══
// ─────────────────────────────────────────────────────
//
//   写三样东西（都在下面，不用写 Main、不用写 class Program）：
//
//     1. struct 分数 { public int 得分; }
//        再加  static 分数 数值改不掉(分数 s)
//            里面 `s.得分 = 90;` 然后 `return s;`
//        —— 这个函数的**返回值**是 90，而调用者的变量应该一点没变。
//
//     2. record 成绩记录(string 名字, int 分数);
//        再加  static (string 名字, bool 及格) 取名字(成绩记录 r)
//            及格 = `r.分数 >= 60`
//
//     3. static (int 最低, int 最高) 取最高最低(int[] 数们)
//            数组为空返回 (0, 0)；否则返回最小和最大
//
// ⚠ record 那一行**末尾有分号**（是「位置式 record」的简写），
//   写成带 `{ }` 的完整形式也行。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """
// ── 1. struct：值语义 ──
struct 分数
{
    public int 得分;
}

static 分数 数值改不掉(分数 s)
{
    // ⚠ 这里改的是**副本** —— 编译器不报错，但调用者的变量不会变
    s.得分 = 90;
    return s;
}

// ── 2. record：内容说了算的相等 ──
record 成绩记录(string 名字, int 分数);

static (string 名字, bool 及格) 取名字(成绩记录 r)
{
    // ⚠ 位置式 record 的属性是 { get; init; }，不能写 r.分数 = ...
    return (r.名字, r.分数 >= 60);
}

// ── 3. 元组：一次交回两个结果 ──
static (int 最低, int 最高) 取最高最低(int[] 数们)
{
    // ⚠ 空数组要特判，否则 数们[0] 直接 IndexOutOfRangeException
    if (数们.Length == 0)
    {
        return (0, 0);
    }

    // ⚠ 初值从第一个元素起 —— 写 0 的话全负数就错了
    int 低 = 数们[0];
    int 高 = 数们[0];
    for (int i = 1; i < 数们.Length; i++)
    {
        if (数们[i] < 低) 低 = 数们[i];
        if (数们[i] > 高) 高 = 数们[i];
    }
    return (低, 高);
}
""";


    [检查说明("record 类型真的写了")]
    public static CheckResult 检查_1(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("record 成绩记录"))
            return CheckResult.不过("没找到「record 成绩记录」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("struct 类型真的写了")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("struct 分数"))
            return CheckResult.不过("没找到「struct 分数」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("数值改不掉：返回值是 90，且那个字段是 int")]
    public static CheckResult 检查_3(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(数值改不掉(new 分数 { 得分 = 80 }).得分);

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "90"
        };
        string[] 输入 = {
            "new 分数 { 得分 = 80 }"
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

    [检查说明("取名字：及格线 60 两边都要对")]
    public static CheckResult 检查_4(string 代码)
    {
        var 调用点 = """
    Console.WriteLine($"({取名字(new 成绩记录("甲", 60)).Item1}, {取名字(new 成绩记录("甲", 60)).Item2})");
    Console.WriteLine($"({取名字(new 成绩记录("乙", 59)).Item1}, {取名字(new 成绩记录("乙", 59)).Item2})");

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "(甲, True)",
            "(乙, False)"
        };
        string[] 输入 = {
            "new 成绩记录(\"甲\", 60)",
            "new 成绩记录(\"乙\", 59)"
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

    [检查说明("取最高最低：多组都对，含全负数与单元素")]
    public static CheckResult 检查_5(string 代码)
    {
        var 调用点 = """
    Console.WriteLine($"({取最高最低(new int[]{3,1,4,1,5}).Item1}, {取最高最低(new int[]{3,1,4,1,5}).Item2})");
    Console.WriteLine($"({取最高最低(new int[]{-5,-1,-9}).Item1}, {取最高最低(new int[]{-5,-1,-9}).Item2})");
    Console.WriteLine($"({取最高最低(new int[]{7}).Item1}, {取最高最低(new int[]{7}).Item2})");
    Console.WriteLine($"({取最高最低(new int[]{0,0}).Item1}, {取最高最低(new int[]{0,0}).Item2})");

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "(1, 5)",
            "(-9, -1)",
            "(7, 7)",
            "(0, 0)"
        };
        string[] 输入 = {
            "new int[]{3,1,4,1,5}",
            "new int[]{-5,-1,-9}",
            "new int[]{7}",
            "new int[]{0,0}"
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
