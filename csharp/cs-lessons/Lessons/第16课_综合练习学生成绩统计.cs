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
// ══ 这一课不是新语法 ══
//
//   是把 09~15 拼起来做一个小东西。先把账对一下：
//
//     第 09 课  record + 元组         ← 成绩记录、统计的返回类型
//     第 10 课  属性与访问控制          ← record 的 init 属性
//     第 11 课  override / base       ← 想扩展时可以派生一个"带班号的记录"
//     第 12 课  List<T>               ← 数据放在哪
//     第 13 课  异常                  ← 空列表这个边界情况
//     第 14 课  模式匹配               ← 报警()
//     第 15 课  LINQ                  ← 名字们()
//
//   任务是：**统计一群学生的成绩**。
//
//
// ══ 一、先把数据形状定下来 ══
//
//   第 09 课讲过：一条"内容说了算"的数据 ⇒ **record**，不是 class。
//
//       record 成绩记录(string 名字, int 分数);
//
//   为什么是 record 而不是 class？回头看一眼第 09 课那张表：
//     · 两条内容一样的记录，`==` 相等、`Equals` 相等、`Contains` 找得到
//     · `ToString()` 是可读的
//     · `with` 能"改一个字段、其余照抄"
//   这三样在统计代码里全都要用（下面 `报警` 的测试就靠 `==` 比内容）。
//
//   ⚠ 位置式 record 的属性是 `{ get; init; }` —— **构造完就不能再赋值**。
//     想"分数加一"只能造新的：`项 with { 分数 = 项.分数 + 1 }`。
//     这一条是第 09 课的坑 2，这里会真的撞上。
//
//
// ══ 二、`统计` 的返回类型：为什么用元组 ══
//
//   要一次交回四样东西。四种做法：
//
//     ① 四个 out 参数 → 签名长得没法看，还要求调用方预先声明四个变量
//     ② 专门定义一个 `class 统计结果` → 为一次返回造一个类型，重了
//     ③ 返回 `List<object>` → 类型全丢了，取的时候要强转
//     ④ **元组** `(int 人数, int 及格, double 平均, string 头名)`  ✅
//
//   四个值、就地用完、有名字 ⇒ 元组正合适（第 09 课第四节）。
//   如果这东西要在十几个方法之间传，那就该给它起个名字用 record 了。
//
//   实现要点：
//
//       static (int 人数, int 及格, double 平均, string 头名) 统计(
//           List<成绩记录> 们, int 及格线)
//       {
//           if (们.Count == 0)
//               return (0, 0, 0, "");          // ⚠ 边界先处理掉
//
//           int 人数 = 们.Count;
//           int 及格数 = 们.Count(项 => 项.分数 >= 及格线);      // ← LINQ 的 Count
//           double 平均 = Math.Round(们.Average(项 => 项.分数), 2);
//           string 头名 = 们.OrderByDescending(项 => 项.分数)
//                          .ThenBy(项 => 项.名字, StringComparer.Ordinal)
//                          .Select(项 => 项.名字)
//                          .First();
//           return (人数, 及格数, 平均, 头名);
//       }
//
//   ⚠⚠ **空列表必须特判**，三个原因，一个比一个硬：
//
//     · `Average()` 在空序列上会抛
//       `System.InvalidOperationException: 序列不包含任何元素`
//     · `First()` 在空序列上抛同一个异常
//     · 就算你躲开了这两个，"平均分"在**零个人**时该是多少？没有定义。
//       ⇒ 返回 (0, 0, 0, "") 是明确说"没有数据"，比返回 0 清楚。
//
//     这和 C 课第 16 课那个 `if (个数 <= 0) return;` 是同一件事：
//     **统计函数的第一个 if 永远是"空的时候怎么办"。**
//
//   ⚠ `Average(项 => 项.分数)` 返回的是 `double`（分数虽然是 int，
//     但它会先转成 double 再算，**不会有整数除法的坑**）。
//     这跟第 13 课你自己手写 `(double)和 / 个数` 是一回事，LINQ 替你做掉了。
//
//   ⚠ 同分怎么办？`OrderByDescending` 只按分数排，两个人同为最高分时
//     `First()` 取到哪个**不保证**。要结果稳定就再加一级：
//         .ThenBy(项 => 项.名字, StringComparer.Ordinal)
//     （`StringComparer.Ordinal` 是按字符编码比，和区域设置无关 ⇒ 跨机器可复现。
//      第 12 课的 `名次表` 用的也是它。）
//
//
// ══ 三、`报警`：模式匹配的用武之地 ══
//
//   这一段有四种情况，而且**顺序有讲究** —— "名字空"最优先：
//
//       static string 报警(成绩记录 r) => r switch
//       {
//           { 名字: "" }              => "名字是空的",
//           { 分数: < 0 or > 100 }    => "数据异常",
//           { 分数: < 60 }            => "不及格",
//           _                         => "正常",
//       };
//
//   为什么"名字空"要排第一：一个名字为空 **且** 分数 200 的记录，
//   两条规则都命中 —— 谁在前面谁说了算。
//   数据本身就不合法时，"数据有问题"比"分数异常"更该先说。
//
//   ⚠ 属性模式的写法是 `{ 属性名: 模式 }`，冒号。
//     写成 `{ 名字 = "" }` 会报 `CS1003: 语法错误`（第 14 课的坑 7）。
//
//   ⚠ `{ 分数: < 0 or > 100 }` 里的 `or` 是**模式**的连接词，
//     不是 `||`。它是"分数这个位置上的值，匹配 `< 0` **或** `> 100`"。
//
//   ⚠ switch 表达式必须穷尽 ⇒ 保留 `_ => "正常"`。
//     漏了它只有一条警告，运行时遇到没覆盖的值直接抛。
//
//
// ══ 四、`名字们`：LINQ 三步，顺序别乱 ══
//
//       static List<string> 名字们(List<成绩记录> 们)
//       {
//           return 们.Where(项 => 项.名字 != "")            // ① 筛掉空名字
//                    .OrderByDescending(项 => 项.分数)      // ② 按分数降序
//                    .Select(项 => 项.名字)                 // ③ 只要名字
//                    .ToList();                             // ④ 收口
//       }
//
//   ⚠ **`Select` 必须在排序之后。** 顺序反了会怎样？
//
//         们.Select(项 => 项.名字).OrderByDescending(名 => 名)
//         //                               ↑ 现在 名 是 string
//         //                                  ⇒ 变成按**名字的字典序**排
//
//     症状：**不报错**，能编译（string 也能排序），但结果是按姓氏排的，
//           不是按分数排的。这是"能跑但错"的一类。
//     为什么：`Select` 之后元素就变成 string 了，后面所有操作都作用在 string 上。
//     ⇒ 记法：**先筛（Where）、再排（OrderBy）、后投影（Select）、最后收口。**
//        `Select` 把它左边的"丰富信息"压扁了，压扁之后就拿不回来了。
//
//   ⚠ 第 15 课的延迟执行在这里也踩得到：
//     这个方法**最后有 `.ToList()`**，所以返回的列表是**当下**数据的快照。
//     把 `.ToList()` 去掉（返回类型改成 `IEnumerable<string>`）也合法，
//     但那之后调用方每次遍历都会重新读一遍原列表 ——
//     **一边遍历一边往原列表加人**就会撞上 `集合已修改` 异常。
//
//   ⚠ `.Where(项 => 项.名字 != "")` 里比较的是**内容**（不是引用），
//     所以是对的。若换成 `!= null`，空串会漏过去 —— 两种"空"不一样。
//
//
// ══ 五、常见坑（这一课会真的撞上的）══
//
//   坑 1：空列表直接算 ⇒ 抛异常
//
//       double 平均 = 们.Average(项 => 项.分数);      // 们 是空列表
//
//     症状：`System.InvalidOperationException: 序列不包含任何元素`
//           —— 报错在 `Average` 那一行，但你会怀疑是数据源的问题。
//     为什么：平均值是"和 ÷ 个数"，个数是 0 —— 结果没有定义，所以抛。
//     改法：方法**第一行**就 `if (们.Count == 0) return (0, 0, 0, "");`
//
//   坑 2：想改 record 的字段
//
//       项.分数 += 1;                              // ❌
//
//     症状：`error CS0200: 属性或索引器"成绩记录.分数"不能分配给 —— 它是只读的`。
//     为什么：位置式 record 生成的是 `{ get; init; }`，只有 init 没有 set。
//     改法：`项 with { 分数 = 项.分数 + 1 }` 造一个新的（第 09 课坑 2 + 第 15 课）。
//
//   坑 3：`Select` 写在了 `OrderBy` 前面
//
//     见上一节。症状是**结果顺序不对但编译通过**。
//
//   坑 4：平均分算成了整数（自己手写循环时）
//
//       return (double)(和 / 个数);                 // ❌ 括号包错了
//
//     症状：平均分永远是整数，比如 87.5 变成 87。
//     为什么：`和 / 个数` 两边都是 int ⇒ **先**做了整数除法（向零取整），
//           取整完了才转 double —— 小数早就没了。
//     改法：`(double)和 / 个数` —— **强转要写在除法之前**，让其中一边先变成 double。
//           或者干脆用 `们.Average(...)`。
//
//   坑 5：switch 表达式漏了兜底
//
//       static string 报警(成绩记录 r) => r switch
//       {
//           { 分数: < 60 } => "不及格",
//       };
//
//     症状：`warning CS8509: 表达式不涵盖其输入类型的所有可能值`；
//           分数是 90 时运行期抛非穷举异常。
//     为什么：switch 表达式要"整体有一个值"，没匹配上就没值可给。
//     改法：`_ => "正常"`。对 record + 开放取值的属性，兜底是**必需项**。
//
//   坑 6：`First()` 用在可能空的序列上
//
//       string 头名 = 们.Where(项 => 项.分数 >= 100).Select(项 => 项.名字).First();
//
//     症状：`System.InvalidOperationException: 序列不包含任何元素`。
//     为什么：`First()` 承诺一定给一个。全都不到 100 分时它给不出。
//     改法：`FirstOrDefault()`（引用类型给 null），或者先 `Any()` 问一句。
//           ⚠ 但注意：`FirstOrDefault` 之后拿到的是 **null**，
//             再 `.Length` 就 NullReferenceException —— 换了个地方崩而已。
//             要真的稳妥，就 `?? ""` 兜一下。
//
//   坑 7：为了"快"用 Dictionary 统计，然后用 `表[键] += 1`
//
//       var 计数 = new Dictionary<string, int>();
//       foreach (var 项 in 们) 计数[项.名字] += 1;     // 💥 第一次就炸
//
//     症状：`System.Collections.Generic.KeyNotFoundException`。
//     为什么：`计数[名字] += 1` 展开是 `计数[名字] = 计数[名字] + 1` ——
//           先去**读** `计数[名字]`，而那个键第一次出现时还不存在 ⇒ 读就炸了。
//     改法：
//       · `计数[名字] = 计数.TryGetValue(名字, out int c) ? c + 1 : 1;`
//       · 或者 `计数[名字] = 计数.GetValueOrDefault(名字) + 1;`
//       · 或者干脆交给 LINQ：
//           `们.GroupBy(项 => 项.名字).ToDictionary(g => g.Key, g => g.Count())`
//     这是第 12 课坑 1 和坑 3 的组合拳，统计类代码里极常见。
//
//
// ══ 轮到你了 ══
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
//   ⚠ 元组返回的**平均分是 double 且已经四舍五入到两位**，
//     所以你 `Console.WriteLine(统计(...).平均)` 会看到 86.67 这样的数。
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 16
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
    return r is { 分数: < 及格线 };
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

    [检查说明("统计：人数 / 及格 / 平均（两位小数）/ 头名 都对")]
    public static CheckResult 检查_4(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(统计(new List<成绩记录>{ new 学生代码.成绩记录("甲", 90), new 学生代码.成绩记录("乙", 80), new 学生代码.成绩记录("丙", 90) }, 85).平均);
    Console.WriteLine(统计(new List<成绩记录>{ new 学生代码.成绩记录("甲", 90), new 学生代码.成绩记录("乙", 80), new 学生代码.成绩记录("丙", 90) }, 85).头名);

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "86.67",
            "甲"
        };
        string[] 输入 = {
            "统计(new List<成绩记录>{ new 成绩记录(\"甲\", 90), new 成绩记录(\"乙\", 80), new 成绩记录(\"丙\", 90) }, 85).平均",
            "统计(new List<成绩记录>{ new 成绩记录(\"甲\", 90), new 成绩记录(\"乙\", 80), new 成绩记录(\"丙\", 90) }, 85).头名"
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

    [检查说明("统计：空列表不抛异常")]
    public static CheckResult 检查_5(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(统计(new List<成绩记录>(), 60).人数);
    Console.WriteLine(统计(new List<成绩记录>(), 60).头名);

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "0",
            ""
        };
        string[] 输入 = {
            "统计(new List<成绩记录>(), 60).人数",
            "统计(new List<成绩记录>(), 60).头名"
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
