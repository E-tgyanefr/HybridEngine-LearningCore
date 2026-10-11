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
// 第 14 课：模式匹配 —— 让"判断+取值"变成一件事
//
// ══ 先介绍：老写法里那些重复的啰嗦 ══
//
//   假设你有个 `object 东西`，想知道它是不是 int、是的话值多少：
//
//       // 老写法
//       if (东西 is int)
//       {
//           int n = (int)东西;          // ← 刚刚才判断过，现在又要强转一次
//           Console.WriteLine(n + 1);
//       }
//
//   `is` 判断了一次类型，`(int)` 又转了一次 —— 两次说的是同一件事。
//
//   模式匹配把这两步合成一步：
//
//       if (东西 is int n)              // ← 判断成功的同时，n 就已经是 int 了
//       {
//           Console.WriteLine(n + 1);   // 直接用，不用强转
//       }
//
//   ⇒ 这就是模式匹配的全部动机：**把"检查形状"和"取出里面的东西"合并**，
//     顺带让编译器能证明"你取的时候它一定是对的"。
//
//
// ══ 一、`is` 的五种模式 ══
//
//   ① 类型模式：`东西 is int n`
//
//        if (东西 is string s && s.Length > 0)     // ⚠ && 后面 s 就能用了
//            Console.WriteLine(s.ToUpper());
//
//      ⚠ 在同一个条件里 `is` 之后可以放心用 s ——
//        因为 `&&` 短路：s 有值才可能走到后半句。
//
//   ② 常量 / 关系模式：`is 0`、`is >= 90`、`is < 0`
//
//        if (分 is >= 0 and <= 100)               // 等价于 分 >= 0 && 分 <= 100
//
//   ③ 逻辑模式：`and` / `or` / `not`
//
//        if (分 is < 0 or > 100)   Console.WriteLine("无效");
//        if (分 is not 0)          Console.WriteLine("不是零");
//
//      ⚠ 是关键字 `and` / `or` / `not`，**不是** `&&` / `||` / `!`。
//        两者别混：`分 is >= 0 && 分 <= 100` 里 `&&` 两边必须是完整表达式；
//        `分 is >= 0 and <= 100` 里 `and` 两边是**模式**，右边那个 `<= 100`
//        自己不带左操作数（左操作数就是被匹配的那个值）。
//
//   ④ 属性模式：`东西 is 成绩记录 { 分数: >= 90 }`
//
//        if (r is { 分数: >= 90, 名字: not "" })
//            Console.WriteLine(r.名字 + " 优秀");
//
//      `{ 属性名: 模式 }` —— 意思是"这个对象是成绩记录，而且它的 `分数` 属性
//      本身匹配 `>= 90` 这个模式"。可以一次写好几个属性，逗号隔开（相当于 and）。
//
//   ⑤ 位置模式（record 专用，因为 record 有自动解构）：
//
//        if (r is (var 名, >= 90))
//            Console.WriteLine(名 + " 优秀");
//
//      `(var 名, >= 90)` 按**声明顺序**对应 `成绩记录(名字, 分数)`。
//
//
// ══ 二、switch 表达式：一条链的判断，写成一张表 ══
//
//       static string 描述(int 分) => 分 switch
//       {
//           < 0          => "无效",
//           >= 90        => "优",
//           >= 80        => "良",
//           >= 60        => "及格",
//           _            => "不及格",
//       };
//
//   和老 `switch` 语句的差别，一眼就能看出来：
//
//       老 switch 语句                      switch 表达式
//       ─────────────────────────────       ────────────────────────────
//       switch (分) { ... }                分 switch { ... }
//       case 90: ...                        >= 90 => "优",
//       **每条都要 break;**                  用逗号，不用 break
//       是**语句**，不返回值                 是**表达式**，整体有个值
//       一个 case 只能写一个常量             `>= 90`、`< 0`、`int n` 都行
//
//   ⚠ 四个语法点，写错一个就编译不过：
//     ① `switch` 在**后面**：`分 switch` —— 不是 `switch (分)`
//     ② 每个分支 `模式 => 值`，**末尾是逗号**
//     ③ 整体是一个表达式 ⇒ 可以直接 `return ... ;`，或者用 `=>` 方法体
//     ④ 必须**穷尽**：所有可能的值都要被某个分支（或 `_`）接住
//
//   ⚠ 漏了兜底会怎样？
//
//       static string 描述(int 分) => 分 switch { >= 60 => "及格" };
//       // warning CS8509: 表达式不涵盖其输入类型的所有可能值
//       //                 （它不处理 < 60）
//
//     症状：**是一条警告，不是错误** —— 编译能过，但你输入 30 的时候，
//           程序在运行时抛"非穷举的 switch 表达式"（SwitchExpressionException）。
//     为什么：switch 表达式要"整体有个值"，没匹配上就没有值可给。
//     改法：加一条 `_ => ...`。**int 这种类型永远需要兜底**，
//           因为编译器不可能枚举完 40 亿个 int。
//     什么时候不用兜底：`bool` 两个都写了，或者枚举每个成员都写了。
//
//   ⚠ 分支是**从上往下**匹配的，第一个命中的赢：
//
//          >= 60 => "及格",
//          >= 90 => "优",          // ← 到不了这里！95 在第一行就被 60 接住了
//
//     ⇒ **从最严格写到最宽松**（和第 03 课的 if 链是同一个坑）。
//
//   ⚠ `when` 子句：模式之外还想再加个条件时用它：
//
//       分 switch
//       {
//           int n when n > 1000 => "爆表",
//           _ => "正常",
//       }
//
//
// ══ 三、模式匹配的两种用法：值 vs 类型 ══
//
//   · 匹配**值**（数字、字符串、枚举）⇒ 关系模式 + switch 表达式最好用
//   · 匹配**类型**（一个 object / 基类引用，可能是好几种东西）
//     ⇒ `is` 类型模式 + switch 表达式的类型模式
//
//       static string 描述对象(object 东西) => 东西 switch
//       {
//           int n when n > 0      => "正数 " + n,
//           int n                 => "非正数 " + n,
//           string s when s == "" => "空字符串",
//           string s              => "字符串: " + s,
//           null                  => "什么都没有",
//           _                     => "不认识的类型",
//       };
//
//     顺序在这里是**必须**的：`string s when s == ""` 一定得写在 `string s` 前面。
//
//
// ══ 四、常见坑 ══
//
//   坑 1：`is` 里声明了变量却在外面用
//
//       if (东西 is int n) { }
//       Console.WriteLine(n);          // ❌ 编译不过
//
//     症状：`error CS0103: 当前上下文中不存在名称"n"`。
//     为什么：模式变量的作用域就是"这个 `is` 为真的那个分支"。编译器
//           不能保证它在那之外有值 ⇒ 干脆不让它在外面存在。
//     改法：把要用的代码挪进 if；或者换个名字并预先声明一个变量接住。
//
//   坑 2：`is` 里声明的变量类型不是你以为的那个
//
//       if (东西 is int s) { s.ToUpper(); }   // ❌
//
//     症状：`error CS1061: "int"未包含"ToUpper"的定义`。
//     为什么：模式变量的类型**由模式决定** —— `is int s` 里 s 就是 `int`。
//     改法：想当字符串用就 `is string s`。
//           （另一种写法是 `is var x`，那 x 就是**原来的类型**，别搞混。）
//
//   坑 3：用 `as` 代替 `is`，然后忘了判 null
//
//       string s = 东西 as string;
//       Console.WriteLine(s.Length);      // 💥 东西不是 string 时 s 是 null
//
//     症状：`NullReferenceException`，而且崩在**下一行**，看起来跟 as 无关。
//     为什么：`as` 转换失败时**返回 null，不抛异常**；null 上取 `.Length` 才炸。
//     改法：用 `is` 模式，一步到位：
//             if (东西 is string s) Console.WriteLine(s.Length);
//           非要用 as 就得紧跟着判 null。
//
//   坑 4：switch 表达式漏兜底（见上面）—— 编译只警告，运行时才炸
//
//       static string 分类(int x) => x switch { > 0 => "正" };
//       Console.WriteLine(分类(-1));      // 💥 非穷举的 switch 表达式
//
//     症状：`警告 CS8509` 你可能根本没看，运行到 -1 才崩。
//     为什么：表达式必须在所有情况下都有一个值。
//     改法：`_ => "非正"`。对 int / string 这类开放类型，**兜底是必需项**。
//
//   坑 5：`is` 里面用 `&&`，把模式当表达式写
//
//       if (分 is >= 0 && <= 100)         // ❌
//
//     症状：`error CS1525: 表达式项"<="无效`。
//     为什么：`&&` 是**逻辑运算符**，它两边要的是完整表达式，
//           而 `<= 100` 缺左操作数，不是表达式。`and` 才是**模式**的连接词。
//     改法：`分 is >= 0 and <= 100`。
//
//   坑 6：`and` / `or` 当成 `&&` / `||` 用在整个表达式上
//
//       if (分 >= 0 and 分 <= 100)        // ❌
//
//     症状：也是 `CS1525` / `CS1003` 一类"语法无效"的报错。
//     为什么：`and` 只能出现在 `is` 模式的**里面**。
//           左边那个 `分 >= 0` 是普通比较表达式，接不上 `and`。
//     改法：要么全程普通写法 `分 >= 0 && 分 <= 100`，
//           要么全程模式 `分 is >= 0 and <= 100`。**别混。**
//
//   坑 7：属性模式里把 `:` 写成 `=` 或 `.`
//
//       r is 成绩记录 { 分数 = >= 90 }      // ❌
//       r is 成绩记录 { 分数.>= 90 }        // ❌
//       r is 成绩记录 { 分数: >= 90 }       // ✅
//
//     症状：`error CS1003: 语法错误，应输入","`（因为 `=` 被当成别的东西了）。
//     为什么：属性模式固定是 `{ 属性名: 模式 }`，`:` 是分隔符。
//
//   坑 8：一个模式变量被 `or` 的两个分支各声明一次，然后拿去用
//
//       if (东西 is int n or string n)        // ❌ 编译不过
//           Console.WriteLine(n);             //    这里 n 没有被确定赋值
//
//     症状：`error CS0128: 已在当前范围中定义名为"n"的局部变量`
//           （同名重复声明），或者说变量"未赋值"。
//     为什么：`or` 两个分支是**互相排斥**的可能路径 —— 只走其中一条，
//           编译器没法保证 n 一定被赋了值。
//     改法：两个分支用不同的名字（`int a or string b`），
//           或者在分支各自内部把值取出来用。
//
//   坑 9：`object` 上的 `==` 和模式匹配混着用
//
//       if (东西 == 1) Console.WriteLine("是一");         // 能编译
//       if (东西 is 1)  Console.WriteLine("是一");        // 也是它
//
//     症状：`东西 = 1`（装箱的 int）时两种都对，但 `==` 对 object 走的是
//           **引用比较**，换个装箱方式结果就会不一样。
//     为什么：`object == object` 比的是引用（string 有自己的 == 重载，是例外）。
//     改法：判类型/判值一律用模式 —— `东西 is int n && n == 1`，意图清楚。
//
//
// ══ 轮到你了 ══
//
//     static string 描述(int 分)              // switch 表达式，含负数 => "无效"
//     record 成绩记录(string 名字, int 分数);
//     static bool 至少一门不及格(成绩记录 r)     // 属性模式 + or
//     static string 描述对象(object 东西)       // is 类型模式 + when
//
//   ⚠ 描述里的分支顺序：**最严格的先写**（`< 0` 在最前面，不能掉进 `_`）。
//   ⚠ 描述对象的顺序：`string` 的空串判断要在"一般字符串"之前。
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 14
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
