using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("14", "第 14 课：模式匹配 —— `is`、switch 表达式、`and`/`or`/`not`",
    """
1. 写 `static string 描述(int 分)` —— 用 **switch 表达式**：`< 0` 返回 `"无效"`、`>= 90` 返回 `"优"`、`>= 80` 返回 `"良"`、`>= 60` 返回 `"及格"`、其余返回 `"不及格"`
2. 写 `record 成绩记录(string 名字, int 分数);` 和 `static bool 至少一门不及格(成绩记录 r)` —— 用**属性模式 + `or`**：名字为空 **或** 分数不在 0~100 之间 ⇒ 返回 `true`
3. 写 `static string 描述对象(object 东西)` —— 用 `is` 类型模式 + `when`：`int n` 且 `n > 0` ⇒ `"正数 n"`；`string s` 且非空 ⇒ `"字符串: s"`；`string` 但是空串 ⇒ `"空字符串"`；其他 ⇒ `"不认识的类型"`
""")]
public static class 第14课
{
    public static string 模板 => """
// 第 14 课：模式匹配 —— is、switch 表达式、and/or/not
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**把「一串 if-else」写成更像人话的样子**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    模式匹配：is / switch 表达式 / and or not
//
//  需要先会
//    第 03 课：switch 语句
//
// ─────────────────────────────────────────────────────
//  先介绍：if-else 的两种啰嗦
// ─────────────────────────────────────────────────────
//
//     ① 取一个值、按范围分档 —— 要写一长串 if / else if，还得靠 return
//     ② 判断「这是个什么类型」—— 要先判断类型，再转类型，再判断内容
//
// 模式匹配把这两件事都变短。
//
// ─────────────────────────────────────────────────────
//  一、switch 表达式：分支直接给值
// ─────────────────────────────────────────────────────
//
//     static string 描述(int 分) => 分 switch
//     {
//         < 0   => "无效",
//         < 60  => "不及格",
//         < 90  => "及格",
//         _     => "优秀",       ← `_` 是兜底（以上都不是）
//     };
//
// ⚠ 和老的 switch **语句**比：这里每个分支是**一个值**，不是一串语句；
//   而且不需要 break（不会贯穿）。
//
// ⚠ 顺序：**从上往下，第一个匹配的胜出** ⇒ 范围要从窄到宽写。
//   `< 0` 必须写在最前面，否则负数会掉进后面的分支。
//
// ─────────────────────────────────────────────────────
//  二、is：判断类型 + 顺手转好 + 顺手取属性
// ─────────────────────────────────────────────────────
//
//     static string 描述对象(object 东西) => 东西 switch
//     {
//         int 数          => "整数：" + 数,        ← 类型模式：匹配上就转好
//         string 字 when 字.Length == 0 => "空字符串",
//         string 字       => "字符串：" + 字,
//         null            => "什么都没有",
//         _               => "别的",
//     };
//
// ⚠ `when` 是附加条件（相当于「而且」）。
// ⚠ **空串那条必须写在一般字符串之前** —— 否则空串会被上面那条接走。
//
// ─────────────────────────────────────────────────────
//  三、属性模式与 and / or / not
// ─────────────────────────────────────────────────────
//
//     record 成绩记录(string 名字, int 分数);
//
//     static bool 至少一门不及格(成绩记录 r) =>
//         r is { 分数: > 0 and < 60 };        ← 属性模式：直接看某个属性
//
//     r is { 分数: > 0 and < 60 }    分数在 0 和 60 之间
//     r is { 分数: <= 0 or >= 60 }   不在那个区间里
//     r is not null                  不是 null
//
// ⚠ 关系模式里的边界必须是**编译期常量**。
//   写 `r is { 分数: < 及格线 }`（及格线是变量）会报：
//
//     错误 CS9135: 类型"int"的常量值应为…
//
//   ⇒ 这种情况用 when：`r is { } x when x.分数 < 及格线`
//     或者干脆写 `r.分数 < 及格线`。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：分支顺序写反了（漏了最严的那条）
// ─────────────────────────────────────────────────────
//
//     static string 描述(int 分) => 分 switch
//     {
//         < 60  => "不及格",
//         < 0   => "无效",        ← 永远轮不到：-5 也是 < 60
//         _     => "及格",
//     };
//
// 你会看到：**编译通过**，只是 描述(-5) 返回 "不及格"，而不是 "无效"。
//
// 为什么：switch 表达式是**从上往下**试的，第一个匹配的胜出。
//   `< 60` 把负数也接走了，后面那条成了死代码。
//
// ⚠ 编译器对这种情况**只给一个 warning（CS8510 无法访问的代码）**，
//   不给 error —— 所以你必须自己盯顺序。
//
//   ⚠ 另一件相关的事：漏写 `_` 兜底只是**warning CS8509**（不穷尽），
//     能编译；但喂到没覆盖的输入时会**抛异常**。所以兜底那条别省。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   LINQ（Where / Select）→ 第 15 课。
//
// ─────────────────────────────────────────────────────
//  ══ 轮到你了 ══
// ─────────────────────────────────────────────────────
//
//     static string 描述(int 分)              // switch 表达式，含负数 => "无效"
//     record 成绩记录(string 名字, int 分数);
//     static bool 至少一门不及格(成绩记录 r)     // 属性模式 + or
//     static string 描述对象(object 东西)       // is 类型模式 + when
//
//   ⚠ 描述里的分支顺序：**最严格的先写**（`< 0` 在最前面，不能掉进 `_`）。
//   ⚠ 描述对象的顺序：`string` 的空串判断要在「一般字符串」之前。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """

static string 描述(int 分) => 分 switch
{
    < 0      => "无效",        // ⚠ 负分要单独接住，不能掉进 _ 里
    >= 90    => "优",
    >= 80    => "良",
    >= 60    => "及格",
    _        => "不及格",      // ⚠ 兜底：int 永远需要
};

record 成绩记录(string 名字, int 分数);

static bool 至少一门不及格(成绩记录 r)
{
    // 属性模式 + or：名字空 或 分数越界 ⇒ 数据有问题，也算"不及格"
    return r is { 名字: "" } or { 分数: < 0 or > 100 };
}

static string 描述对象(object 东西) => 东西 switch
{
    int n when n > 0           => "正数 " + n,
    string s when s.Length > 0 => "字符串: " + s,
    string                     => "空字符串",       // ⚠ 必须排在"一般字符串"之前，
                                                    //   其实上面那条 when 已经把它排掉了，
                                                    //   这一条只兜剩下的 string
    null                       => "什么都没有",
    _                          => "不认识的类型",
};
""";


    [检查说明("用了 switch 表达式（不是老 switch 语句）")]
    public static CheckResult 检查_1(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("switch"))
            return CheckResult.不过("没找到「switch」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("用了 or / and / not 里的至少一个")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("or"))
            return CheckResult.不过("没找到「or」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("描述：负数必须走\"无效\"，其余四档都对")]
    public static CheckResult 检查_3(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(描述(-1));
    Console.WriteLine(描述(0));
    Console.WriteLine(描述(59));
    Console.WriteLine(描述(60));
    Console.WriteLine(描述(89));
    Console.WriteLine(描述(90));
    Console.WriteLine(描述(100));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "无效",
            "不及格",
            "不及格",
            "及格",
            "良",
            "优",
            "优"
        };
        string[] 输入 = {
            "-1",
            "0",
            "59",
            "60",
            "89",
            "90",
            "100"
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

    [检查说明("至少一门不及格：正常数据是 False，空名字或越界是 True")]
    public static CheckResult 检查_4(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(至少一门不及格(new 成绩记录("甲", 90)));
    Console.WriteLine(至少一门不及格(new 成绩记录("", 90)));
    Console.WriteLine(至少一门不及格(new 成绩记录("乙", -1)));
    Console.WriteLine(至少一门不及格(new 成绩记录("丙", 101)));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "False",
            "True",
            "True",
            "True"
        };
        string[] 输入 = {
            "new 成绩记录(\"甲\", 90)",
            "new 成绩记录(\"\", 90)",
            "new 成绩记录(\"乙\", -1)",
            "new 成绩记录(\"丙\", 101)"
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

    [检查说明("描述对象：四种类型分支都对")]
    public static CheckResult 检查_5(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(描述对象((object)7));
    Console.WriteLine(描述对象((object)(-7)));
    Console.WriteLine(描述对象((object)"甲"));
    Console.WriteLine(描述对象((object)""));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "正数 7",
            "不认识的类型",
            "字符串: 甲",
            "空字符串"
        };
        string[] 输入 = {
            "(object)7",
            "(object)(-7)",
            "(object)\"甲\"",
            "(object)\"\""
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
