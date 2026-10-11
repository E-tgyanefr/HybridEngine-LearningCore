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
// ══ 先介绍：数组不够用的时候 ══
//
//   第 04 课用的是数组：长度定死，一旦建立就改不了。
//   可现实里数据是长出来的 —— 学生一个个录进来、日志一行行写进来。
//   你需要"能加、能删、能查"的容器。C# 里最常用的两个：
//
//       List<T>                       有序的一串，能增能删能按位置取
//       Dictionary<TKey, TValue>      键 → 值 的查表，按键查是**瞬间**的
//
//   `<T>` 里的 `T` 是**类型参数** —— 一个占位符，用来之前替换成真类型。
//   第 04 课你已经用过 `List<int>` 了，这一课把它和泛型本身讲透。
//
//
// ══ 一、为什么需要泛型（而不是啥都装 object 的 ArrayList）══
//
//   老 C# 只有 `ArrayList`，里面装的是 `object`，什么都塞得进去：
//
//       var 旧表 = new ArrayList();
//       旧表.Add(1);
//       旧表.Add("字符串");                 // 也让它进 —— 编译通过
//       int x = (int)旧表[1];              // 💥 运行时 InvalidCastException
//
//   两个问题：
//     ① 装进去 / 拿出来要**装箱拆箱**，慢
//     ② 类型错误要等到**运行时**才炸
//
//   泛型把这两件事都解决了：
//
//       var 新表 = new List<int>();
//       新表.Add(1);
//       新表.Add("字符串");                 // ❌ 编译不过，当场拦住
//       int x = 新表[1];                    // 不用强转，类型已知
//
//   ⇒ **泛型的价值：把类型错误从运行时提前到编译时。**
//     写 `List<int>` 之后，编译器全程知道里面只有 int，
//     所以它既能帮你拦错误，又不用装箱。
//
//
// ══ 二、List<T> 的常用操作 ══
//
//       var 名单 = new List<string>();        // 空表
//       var 数们 = new List<int> { 3, 1, 4 };  // 集合初始化器
//
//       名单.Add("甲");                        // 追加到末尾
//       名单.AddRange(new[] { "乙", "丙" });    // 一次加一串
//       名单.Insert(0, "顶");                  // 插到下标 0
//
//       名单.Count                             // ⚠ 是 Count，不是 Length
//       名单[0]                                // 按位置取（下标从 0）
//       名单[^1]                               // 最后一个（^ 是"从末尾数"）
//       名单.Contains("甲")                    // 有没有
//       名单.IndexOf("乙")                     // 第几个；没有是 -1
//
//       名单.Remove("甲");                     // 按**值**删（删第一个匹配的）
//       名单.RemoveAt(0);                      // 按**下标**删
//       名单.RemoveAll(x => x.Length > 1);      // 删掉所有满足条件的
//       名单.Clear();                          // 清空
//       名单.Sort();                           // 就地排（会改自己）
//
//   ⚠ `List<T>` 的 `Count` 是**属性**，数组的 `Length` 也是属性 ——
//     但一个叫 Count 一个叫 Length。列表用 Count，数组用 Length。
//     （List 上写 `.Length` 编译不过：
//       `error CS1061: "List<int>"未包含"Length"的定义`。）
//
//
// ══ 三、Dictionary<TKey, TValue>：按键查表 ══
//
//       var 分数表 = new Dictionary<string, int>();
//       分数表["甲"] = 90;                    // 写：键不存在就新增
//       分数表["甲"] = 95;                    // 写：键已存在就覆盖
//
//       int s = 分数表["甲"];                  // 读
//
//       Console.WriteLine(分数表.Count);        // 2
//       Console.WriteLine(分数表.ContainsKey("甲"));   // True
//
//       foreach (var 对 in 分数表)
//           Console.WriteLine(对.Key + "=" + 对.Value);
//
//   ⚠⚠ **两种取值方式，行为完全不同 —— 这是这一课最重要的坑：**
//
//       int a = 分数表["不存在的键"];              // 💥 KeyNotFoundException
//       if (分数表.ContainsKey("不存在的键"))       // ✅ 先问再取
//           int b = 分数表["不存在的键"];
//       if (分数表.TryGetValue("不存在的键", out int c))   // ✅✅ 一步到位
//           Console.WriteLine(c);
//
//     · 索引器 `表[键]`：键不存在 ⇒ **抛异常**（不是返回 null / 0）
//     · `TryGetValue`：返回 bool，找到就把值写进 out 参数
//
//   ⇒ 只要键**可能不存在**，就用 `TryGetValue`。
//     它是"查表"这个动作的标准写法，比 ContainsKey + 索引器少查一遍。
//
//   ⚠ 字典**没有顺序**。`foreach` 出来的顺序不保证是你插入的顺序，
//     也不保证每次都一样。要固定顺序 ⇒ 遍历前用 OrderBy 排一下。
//
//   ⚠ 键不能是 null，也必须是"可做哈希"的：
//     用自定义的 class 当键时，它默认按**引用**比较 ⇒ 内容一样的两个对象
//     是两个不同的键。要用内容当键 ⇒ 用 `record`（第 09 课），
//     它自带逐字段的相等与哈希。
//
//
// ══ 四、泛型方法：自己写一个 `<T>` ══
//
//   方法名后面加上 `<T>`，`T` 就成了这个方法里的一个"类型占位符"：
//
//       static int 数满足<T>(List<T> 们, Func<T, bool> 条件)
//       {
//           int 计 = 0;
//           foreach (var 项 in 们)
//               if (条件(项)) 计++;          // 条件 是个"收 T、返回 bool"的函数
//           return 计;
//       }
//
//   用的时候编译器**自己推断** T：
//
//       var 数们 = new List<int> { 1, 2, 3, 4 };
//       Console.WriteLine(数满足(数们, n => n % 2 == 0));      // 2
//
//       var 名单 = new List<string> { "甲", "乙丙" };
//       Console.WriteLine(数满足(名单, s => s.Length == 1));   // 1
//
//   同一份代码，`T` 分别变成了 `int` 和 `string` —— 这就是泛型。
//
//   `Func<T, bool>` 是"**收一个 T，返回一个 bool** 的函数"的类型
//   （`Func<...>` 的最后一个是返回类型；不收参数用 `Action`）。
//   第 08 课你写的接口是"一个对象的契约"，`Func` 是"一个函数的契约"。
//
//   ⚠ 泛型约束 `where`：想在泛型里**用** T 的某个能力，就得先声明它有那个能力。
//
//       static T 取大的<T>(T a, T b) where T : IComparable<T>
//       {
//           return a.CompareTo(b) >= 0 ? a : b;      // ← 有约束才敢调 CompareTo
//       }
//
//     常见的几个：
//       where T : class            必须是引用类型
//       where T : struct           必须是值类型
//       where T : new()            必须有无参构造器（才能写 new T()）
//       where T : 基类名 / 接口名    必须继承自它
//
//     不加约束时，编译器只知道 T 是"某个类型" —— 你连 `a > b` 都写不了，
//     报错是 `error CS0019: 运算符">"无法应用于"T"和"T"类型的操作数`。
//     这不是刁难：泛型代码要为**所有** T 成立，而 T 可能是没有 `>` 的类型。
//
//   ⚠ 约束**不能**写成"随便哪个类型"之外的运行时检查 ——
//     `where T : int` 这种是写不了的（int 是密封值类型，没意义）。
//
//
// ══ 五、常见坑 ══
//
//   坑 1：用 `表[键]` 去查一个可能不存在的键
//
//       var 表 = new Dictionary<string, int> { ["甲"] = 90 };
//       Console.WriteLine(表["乙"]);          // 💥
//
//     症状：`System.Collections.Generic.KeyNotFoundException:
//           给定关键字不在字典中`，程序直接终止。
//     为什么：索引器的语义是"**给我这个键的值**"，键不在就只能报错 ——
//           它没法凭空返回一个值（int 的"没有"是什么？0 吗？那和真的 0 混了）。
//     改法：`表.TryGetValue("乙", out int v)`，先看返回的 bool。
//
//   坑 2：在 `foreach` 里增删这个集合
//
//       foreach (var x in 数们)
//           if (x < 0) 数们.Remove(x);        // 💥
//
//     症状：`System.InvalidOperationException: 集合已修改；可能无法执行枚举操作。`
//     为什么：foreach 内部拿的是一个"版本号"，它要求遍历期间集合的版本不变。
//           你 Remove 一次，版本号就变了 ⇒ 下一轮 move next 直接抛。
//     改法：
//       · 最省事： 数们.RemoveAll(x => x < 0);
//       · 或者倒着用 for：
//             for (int i = 数们.Count - 1; i >= 0; i--)
//                 if (数们[i] < 0) 数们.RemoveAt(i);
//
//   坑 3：以为 `表[键] = 值` 只做"修改"
//
//       表["新键"] = 1;                        // 键不存在 ⇒ **新增**，不报错
//
//     症状：循环里反复写同一个键，结果 Count 一直涨，最后发现多了一堆
//           你以为是重复的键。
//     为什么：索引器的 set 是"有则改、无则加"（upsert）。
//     改法：想"只在键已存在时才写"就 `if (表.ContainsKey(k)) 表[k] = v;`。
//           —— 这也正是"统计词频"里 `表[词] = 表.GetValueOrDefault(词) + 1;`
//           能一行搞定的原因。
//
//   坑 4：`List<T>` 的浅拷贝陷阱 —— `List<int> b = a;` 不是复制
//
//       var a = new List<int> { 1, 2 };
//       var b = a;                              // ← 只是抄了地址
//       b.Add(3);
//       Console.WriteLine(a.Count);            // 3 ❗
//
//     症状：改 b 影响了 a，两个变量像是被绑在一起。
//     为什么：`List<T>` 是**引用类型**（第 09 课那张表）—— 变量里装的是地址。
//     改法：要真的复制一份内容：`var b = new List<int>(a);`
//
//   坑 5：泛型里对 T 用了它不一定有的运算
//
//       static T 加起来<T>(T a, T b) => a + b;      // ❌
//
//     症状：`error CS0019: 运算符"+"无法应用于"T"和"T"类型的操作数`。
//     为什么：不加约束时，T 可能是任何类型，包括没有 `+` 的类型。
//           C# 里没有"数值类型"这个约束可加。
//     改法：别写通用加法 —— 老老实实写具体的重载
//           （`加起来(int, int)` / `加起来(double, double)`），
//           或者用 `dynamic`（那是拿掉类型检查，得不偿失）。
//
//
// ══ 轮到你了 ══
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
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 12
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
    printf("%d\n", 数满足());
    printf("%d\n", 数满足());
    printf("%d\n", 数满足());
    printf("%d\n", 数满足());

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "数满足(new List<int>{1,2,3,4}, n => n % 2 == 0)",
            "\"2\"",
            "数满足(new List<string>{\"甲\",\"乙丙\"}, s => s.Length == 1)",
            "\"1\""
        };
        string[] 输入 = {
            "第 1 次调用",
            "第 2 次调用",
            "第 3 次调用",
            "第 4 次调用"
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
                    "      ⚠ 这一条要求**同一个程序里连着调**的结果 —— 如果你用普通局部变量，它每次都会回到初值。");
            }
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("查到了：键在 / 键不在两条路都对")]
    public static CheckResult 检查_4(string 代码)
    {
        var 调用点 = """
    printf("%d\n", 查到了());
    printf("%d\n", 查到了());
    printf("%d\n", 查到了());
    printf("%d\n", 查到了());

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "查到了(new Dictionary<string,int>{[\"甲\"]=90}, \"甲\", out int v1) + \":\" + v1",
            "\"True:90\"",
            "查到了(new Dictionary<string,int>{[\"甲\"]=90}, \"乙\", out int v2)",
            "\"False\""
        };
        string[] 输入 = {
            "第 1 次调用",
            "第 2 次调用",
            "第 3 次调用",
            "第 4 次调用"
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
                    "      ⚠ 这一条要求**同一个程序里连着调**的结果 —— 如果你用普通局部变量，它每次都会回到初值。");
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
