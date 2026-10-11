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
// 第 15 课：LINQ 入门 —— Where / Select / OrderBy，以及"它到底什么时候跑"
//
// ══ 先介绍：把"遍历 + 判断 + 收集"压成一行 ══
//
//   没有 LINQ 的时候，想挑出所有偶数，你得这么写：
//
//       var 结果 = new List<int>();
//       foreach (var 数 in 数们)
//           if (数 % 2 == 0)
//               结果.Add(数);
//
//   三行，而且**意图被埋起来了** —— 你要先读完整段才知道它在干"筛选"。
//
//   LINQ 把这件事变成一行：
//
//       var 结果 = 数们.Where(数 => 数 % 2 == 0).ToList();
//
//   `Where` 收一个"每个元素 → 要不要它"的函数，返回筛选后的序列。
//
//   ⚠ 但这一课真正的主角不是这几个方法名 —— **是「延迟执行」。**
//     LINQ 最出名的坑就在这里，而且它的症状非常反直觉。
//
//
// ══ 一、五个最常用的操作 ══
//
//       var 分数 = new List<int> { 88, 42, 95, 60, 73 };
//
//   ┌ Where —— 筛选：留下满足条件的 ────────────────────────┐
//       var 及格 = 分数.Where(分 => 分 >= 60);
//       // 88, 95, 60, 73
//   └──────────────────────────────────────────────────────┘
//
//   ┌ Select —— 投影：把每个元素**变成**另一个东西 ──────────┐
//       var 标签 = 名单.Select(名 => 名 + " 同学");
//       var 编号 = 名单.Select((名, i) => i + 1);       // 带下标的重载
//   └──────────────────────────────────────────────────────┘
//
//   ┌ OrderBy / OrderByDescending —— 排序 ──────────────────┐
//       var 升 = 分数.OrderBy(分 => 分);
//       var 降 = 分数.OrderByDescending(分 => 分);
//       var 多级 = 们.OrderBy(项 => 项.班).ThenByDescending(项 => 项.分);
//       //   ↑ 第二级必须用 ThenBy，不能写第二个 OrderBy（那会覆盖第一级）
//   └──────────────────────────────────────────────────────┘
//
//   ┌ First / FirstOrDefault / Count / Sum / Average / Max ─┐
//       var 头名 = 分数.Max();
//       var 平均 = 分数.Average();                 // 返回 double
//       var 几个 = 分数.Count(分 => 分 >= 60);
//       var 第一个及格 = 分数.FirstOrDefault(分 => 分 >= 60);
//       //        ⚠ First 找不到 ⇒ 抛异常；FirstOrDefault ⇒ 给默认值(0)
//   └──────────────────────────────────────────────────────┘
//
//   ⚠ 这些方法的返回值是 `IEnumerable<T>`，**不是 List**。
//     想变成具体容器要显式收口：
//         .ToList()      变成 List<T>
//         .ToArray()     变成 T[]
//         .ToDictionary(x => x.键, x => x.值)
//     不收口也行 —— 直接 foreach 它、或者继续接下一个 LINQ 方法。
//
//
// ══ 二、延迟执行：这一课唯一必须搞懂的东西 ══
//
//   先看一个能让你愣住的例子：
//
//       var 数们 = new List<int> { 1, 2, 3 };
//
//       var 查 = 数们.Where(数 => 数 > 1);      // ← 这一行**什么都没做**
//
//       Console.WriteLine(查.Count());          // 2
//       Console.WriteLine(查.Count());          // 2     （只是又算了一遍）
//
//   再看：
//
//       var 数们 = new List<int> { 1, 2, 3 };
//       var 查 = 数们.Where(数 => 数 > 1);      // 定义查询
//
//       数们.Add(99);                            // ← 定义**之后**改数据
//
//       foreach (var 数 in 查)
//           Console.WriteLine(数);               // 2  3  99
//                                                  //        ↑ 99 也在里面！！
//
//   ⚠⚠ **99 是在查询定义之后才加进去的，但它出现在结果里了。**
//
//   为什么：`Where(...)` 这一行**只是描述了一个查询**，它一个元素都没看过。
//   真正的遍历发生在**你开始枚举它的时候** —— `foreach`、`Count()`、
//   `ToList()`、`ToArray()`、`Sum()`…… 每一次枚举都**重新跑一遍**查询，
//   而且读的是**当下**的数据。
//
//   ⇒ 一句话：**LINQ 查询是"配方"，不是"菜"。**
//     写 `Where` 那一行是在写菜谱；`foreach` 才是真的下锅。
//
//   这个设计有它的道理：
//     · 省事 —— 你写五个 Where 也只是一次遍历
//     · 能用在无限序列、数据库、网络流上（`IEnumerable<T>` 可以一个一个来）
//     · 组合查询不用产生中间集合
//
//   代价就是下面这些坑。
//
//   ⚠ 一个更隐蔽的后果：**同一个查询被枚举两次，就是算了两遍。**
//
//       var 查 = 数们.Where(数 => { Console.WriteLine("看了 " + 数); return 数 > 1; });
//       Console.WriteLine(查.Count());      // 打印了 3 行"看了 x"
//       Console.WriteLine(查.Count());      // **又**打印了 3 行
//
//     如果那个 lambda 里有开销大的操作（读文件、算哈希），
//     你就在不知情的情况下做了两次。⇒ 确定要复用结果时，
//     **立刻收口**：`var 查 = 数们.Where(...).ToList();`
//
//
// ══ 三、查询语法：同一件事的另一种写法 ══
//
//   C# 还有一种长得像 SQL 的写法，叫**查询语法**：
//
//       var 及格的名字 =
//           from 项 in 名单
//           where 项.分数 >= 60
//           orderby 项.分数 descending
//           select 项.名字;
//
//   等价的**方法语法**：
//
//       var 及格的名字 =
//           名单.Where(项 => 项.分数 >= 60)
//               .OrderByDescending(项 => 项.分数)
//               .Select(项 => 项.名字);
//
//   ⚠ 两者**完全等价** —— 查询语法在编译时就被翻译成方法调用。
//     选哪个纯看可读性：
//       · 只有 Where/Select/OrderBy ⇒ 方法语法更短
//       · 要 `join`、或者要 `let` 定义中间变量 ⇒ 查询语法清楚得多
//     本课**按方法语法考**（更常用，也更容易和 lambda 对上）。
//
//   ⚠ 查询语法有个硬性规定：**必须以 `select` 或 `group` 结尾**。
//     只写 `from x in 表 where ...` 是不完整的，编译不过。
//
//
// ══ 四、一个最容易踩的意外：延迟执行 + 可变数据 = 结果漂移 ══
//
//       var 们 = new List<成绩记录> { new("甲", 90), new("乙", 50) };
//       var 不及格 = 们.Where(项 => 项.分数 < 60);        // 现在有 1 个
//
//       们.Add(new 成绩记录("丙", 30));                    // 又来了一个
//
//       Console.WriteLine(不及格.Count());                 // 2 ！
//
//   ⚠ 如果这个查询是在"数据已经稳定"之后定义、但**在别处**被枚举，
//     你拿到的结果取决于**枚举那一刻**的数据 —— 排查这类问题时
//     你会觉得"同一个变量每次打印出来都不一样"。
//
//   ⇒ 经验法则：
//     · 结果要**存起来、传出去、用第二次** ⇒ 立刻 `.ToList()` / `.ToArray()`
//     · 只是当场 foreach 一遍 ⇒ 不用收口，省一次拷贝
//
//
// ══ 五、常见坑 ══
//
//   坑 1：以为 `Where` 已经执行了（本课的头号坑）
//
//       var 查 = 数们.Where(数 => 数 > 1);
//       Console.WriteLine("我筛完了");         // 你以为这里已经筛过了
//       // 其实一个元素都还没看
//
//     症状：断点打在 lambda 里，发现它在 `Where` 那一行**根本不停**，
//           而是停在你 `foreach` / `Count()` 的时候；
//           或者"我已经筛过了，后来又加的数据不该出现"—— 但它出现了。
//     为什么：Where 只是构造了一个查询对象，枚举时才逐个元素调你的 lambda。
//     改法：需要"现在就定下来"就收口 `.ToList()`。
//
//   坑 2：把同一个查询用两次，以为它缓存了
//
//       var 查 = 数们.Where(慢判断);
//       if (查.Count() > 0 && 查.First().分数 > 60)   // 慢判断跑了不止一遍
//
//     症状：日志里同一个元素被判断了好几次；性能莫名其妙地差。
//     为什么：每次枚举（`Count()` 一次、`First()` 又一次）都从头跑。
//     改法：`var 快 = 查.ToList();` 然后用 `快`。
//
//   坑 3：在 LINQ 的 lambda 里改原集合
//
//       foreach (var 项 in 们.Where(项 => 项.分数 < 60)) 们.Remove(项);   // 💥
//
//     症状：`System.InvalidOperationException: 集合已修改；可能无法执行枚举操作。`
//           —— 和第 12 课 `foreach` 里增删是同一个异常。
//     为什么：`Where` 是延迟的、且**共享**原集合的枚举器；
//           你在遍历途中改集合，版本号变了 ⇒ 抛。
//     改法：
//       · 先收口再改：`foreach (var 项 in 们.Where(条件).ToList()) 们.Remove(项);`
//       · 或者 `们.RemoveAll(项 => 条件);`（最省事，一步到位）
//
//   坑 4：用两个 `OrderBy` 想实现多级排序
//
//       var 排 = 们.OrderBy(项 => 项.班).OrderBy(项 => 项.分数);   // ❌
//
//     症状：班那一级**完全没起作用**，结果只按分数排。
//     为什么：第二个 `OrderBy` 是**重新排序**，它不记得前一次的键。
//     改法：第二级开始用 `ThenBy` / `ThenByDescending`：
//           `们.OrderBy(项 => 项.班).ThenBy(项 => 项.分数)`
//
//   坑 5：`First()` / `Single()` 在空序列上抛异常
//
//       var 头名 = 们.Where(项 => 项.分数 >= 100).First();     // 💥
//
//     症状：`System.InvalidOperationException: 序列不包含任何元素`。
//     为什么：`First()` 承诺"一定给你一个"，给不出就报错。
//     改法：可能为空就用 `FirstOrDefault()`（值类型给默认值、引用类型给 null），
//           或者先 `Any()` 问一句。
//     顺带一提 `Single()`：它要求**正好一个** —— 0 个抛、2 个也抛。
//
//   坑 6：在 lambda 里改了外部变量，然后奇怪它为什么变了
//
//       int 上限 = 60;
//       var 查 = 们.Where(项 => 项.分数 >= 上限);
//       上限 = 90;                                  // ← 改了
//       var 结果 = 查.ToList();                      // 用的是 **90**！
//
//     症状：结果和"写查询时的意图"不一致。
//     为什么：lambda 捕获的是**变量本身**（不是当时的值），
//           而且延迟执行让求值发生在**改之后**。
//           两个坑叠在一起，结果非常反直觉。
//     改法：把要用的值先复制到不会变的局部变量里，
//           或者定义完立刻收口 `.ToList()`。
//
//   坑 7：忘了 `using System.Linq;`
//
//     症状：一个方法都点不出来，报
//           `error CS1061: "List<int>"未包含"Where"的定义`
//           或者 `error CS0246: 未能找到类型或命名空间名"Enumerable"`。
//     为什么：LINQ 的扩展方法是 `System.Linq` 命名空间里的静态类
//           `Enumerable` 提供的，不 using 就看不见。
//     改法：文件顶部 `using System.Linq;`。
//           ⚠ 课程骨架的顶部**已经**有三个 using，`System.Linq` 在内。
//             但你自己新建 .cs 文件时要记得加 —— 这是新手最常撞的一个。
//
//
// ══ 轮到你了 ══
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
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 15
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

    [检查说明("挑偶数：多组都对，含空数组")]
    public static CheckResult 检查_3(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(string.Join(",", 挑偶数(new int[]{1,2,3,4,5})));
    Console.WriteLine(string.Join(",", 挑偶数(new int[]{1,3,5})));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "2,4",
            "new int[]{2,4,6}"
        };
        string[] 输入 = {
            "new int[]{1,2,3,4,5}",
            "new int[]{1,3,5}"
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
        var run = CppCheck.直接运行(代码);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "2",
            "3",
            "99",
        };
        var 实际 = run.行;
        if (实际.Count != 期望.Length)
            return CheckResult.不过($"应该有 {期望.Length} 行输出，实际 {实际.Count} 行。" +
                "\n      打印出来的：\n        " + string.Join("\n        ", 实际));
        for (int i = 0; i < 期望.Length; i++)
            if (实际[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 行应当是「{期望[i]}」，实际是「{实际[i].Trim()}」。");
        return CheckResult.过($"{期望.Length} 行输出全对");
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
