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
// ══ 先介绍：这一课要拆掉一个几乎所有 C# 新手都有的错误直觉 ══
//
//   你在第 07 课写过 class。从那一课起，脑子里大概有了个印象：
//
//       把东西传给方法，方法改了它，外面就跟着变。
//
//   这个印象**只对一半的类型成立**。C# 的类型分两族，赋值与传参的行为完全不同：
//
//      值类型（value type）：  int / double / bool / struct / record struct
//      引用类型（reference type）：class / record / string / 数组 / List<T> / 接口
//
//   区别的根源就一句话：
//
//      **变量里装的是"值本身"，还是"东西在哪里的地址"。**
//
//   值类型变量装的就是数据本身 —— 所以复制变量 = 复制整份数据。
//   引用类型变量装的是地址 —— 所以复制变量 = 复制地址，两个名字指向**同一个对象**。
//
//   这一课你会发现：`struct` 和 `record` 都能改一个东西的两个名字是否互相影响，
//   而选错会写出"方法里改了、外面没变"这种**不报错、只出错**的 bug。
//
//
// ══ 一、值语义 vs 引用语义：一次看明白 ══
//
//   ┌─ 值类型：赋值 = 复印一份 ────────────────────────────┐
//
//       struct 分数 { public int 得分; }
//
//       分数 a = new 分数();
//       a.得分 = 80;
//       分数 b = a;            // ← 复印：b 是**另一份**数据
//       b.得分 = 50;
//       Console.WriteLine(a.得分);   // 80   ← a 没被影响
//       Console.WriteLine(b.得分);   // 50
//
//   └──────────────────────────────────────────────────────┘
//
//   ┌─ 引用类型：赋值 = 又抄了一个地址 ────────────────────┐
//
//       class 成绩单 { public int 得分; }
//
//       成绩单 a = new 成绩单();
//       a.得分 = 80;
//       成绩单 b = a;          // ← 变量里那份**地址**被复制
//       b.得分 = 50;
//       Console.WriteLine(a.得分);   // 50   ← a 也变了！同一个对象
//
//   └──────────────────────────────────────────────────────┘
//
//   把这张对照表记牢，这一课就过了一半：
//
//       int / double / bool / char / 枚举 / struct / record struct
//                                        ⇒ 赋值、传参都**复制**
//
//       string / 数组 / List<T> / class / record / 接口 / 委托
//                                        ⇒ 赋值、传参都只**复制地址**
//
//   ⚠ string 是引用类型，但它有个额外特性：**不可变**。
//     你以为在"改"字符串，其实每次都造了一个新串。所以它表现得像值类型。
//
//   ⚠ record **默认是引用类型**（`record 名字(...)` 等价于 `record class`）。
//     想要值语义得写 `record struct 名字(...)`。
//     这是最容易记反的一处：record ≠ 值类型，record **只是相等语义是值语义**。
//
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────

// ══ 二、那"值语义"到底在哪里咬人？ ══
//
//   不在赋值上 —— 赋值写错了编译器不管，但你自己看得见。
//   咬人的地方是**传参**：方法拿到的是一个副本，改了不影响调用者。
//
//       struct 分数 { public int 得分; }
//
//       static void 加分(分数 s)          // s 是**副本**
//       {
//           s.得分 += 10;                 // ✅ 编译通过
//       }
//
//       var 甲 = new 分数();
//       甲.得分 = 80;
//       加分(甲);
//       Console.WriteLine(甲.得分);       // 还是 80 ❗
//
//   ⚠⚠ 注意这个 bug 的**症状**：**不报错、不警告，方法里那句 `+= 10` 是合法的，
//      编译器只是默默改了一个马上要被扔掉的副本。**
//      你盯着代码看不出问题，只有打印出来才发现数字没变。
//
//   想真的改到外面，只能把"改好的整份"**返回**出去（这一课要写的 `数值改不掉`
//   就是这个演示 —— 返回值里有 90，但外面那个变量纹丝不动）。
//
//
// ══ 三、struct / record / class：三个都写"数据"，怎么选？ ══
//
//   ① `class`  —— 有身份的东西
//        人、订单、连接、窗口…… 两个"内容一样"的订单仍然是**两个**订单。
//        默认比较是"是不是同一个对象"（引用相等）。
//
//   ② `record`（引用类型）—— 一张**内容说了算**的纸
//        一条成绩记录、一个坐标、一条日志行、一个 API 返回值。
//        内容一样就是相等：
//
//            var a = new 成绩记录("甲", 90);
//            var b = new 成绩记录("甲", 90);
//            Console.WriteLine(a == b);      // True  ← 逐字段比内容
//            Console.WriteLine(a.Equals(b)); // True
//
//        如果是 `class`，同样的两行会打印 **False**（比的是地址）。
//
//        ⚠ 还有两个白送的好处：
//           · `ToString()` 自动是 `成绩记录 { 名字 = 甲, 分数 = 90 }` 这种能读的样子
//             （class 默认是 "类型名"，打印出来毫无信息）
//           · `with` 表达式能"改一个字段、其余照抄"：
//                 var 乙 = a with { 分数 = 60 };   // 新对象，a 不变
//
//   ③ `struct`（值类型）—— 一小把数字，**并且你明确要值语义**
//        · 字段少（官方建议 ≤ 16 字节左右，别塞 List 和 string 进去）
//        · 生命周期短、数量多（坐标、颜色、范围）
//        · 你**希望**复制是独立的
//
//   ⚠ 选错的代价不对称：
//      · 该用 record 却用了 class ⇒ 相等判断悄悄变成"比地址"，去重/查找全失效
//      · 该用 class 却用了 struct ⇒ 传参开始复制，改一处不生效（上面那个 bug）
//      两样都不报错。⇒ **拿不准就先用 class 或 record**，struct 留给"我很确定"的场合。
//
//
// ══ 四、元组：不想为"就这两个值"专门造一个类型 ══
//
//   方法只能 return 一个值。以前你要么写个 out 参数，要么专门定义一个类。
//   元组让"一次交回两个结果"变成一行：
//
//       static (int 最低, int 最高) 取最高最低(int[] 数们)
//       {
//           int 低 = 数们[0], 高 = 数们[0];
//           for (int i = 1; i < 数们.Length; i++)
//           {
//               if (数们[i] < 低) 低 = 数们[i];
//               if (数们[i] > 高) 高 = 数们[i];
//           }
//           return (低, 高);
//       }
//
//   定义处写 `(int 最低, int 最高)`，返回处写 `(低, 高)` —— 位置一一对应。
//
//   接的时候两种写法：
//
//       var 一对 = 取最高最低(数们);
//       Console.WriteLine(一对.最低);        // ← 有名字，.名字 取
//
//       var (低, 高) = 取最高最低(数们);      // ← 解构，一次拆成两个变量
//
//   ⚠ 元组是**值类型**（`ValueTuple`），所以它也是复制语义。
//   ⚠ 元组适合"两三个值、就地用完就扔"。超过三四个，或者它要在
//     好几个方法之间传来传去 ⇒ **给它起个名字，用 record**。
//     满屏的 `(int, string, double, bool)` 三周后没人看得懂。
//
//
// ══ 五、常见坑 ══
//
//   坑 1：以为改了 struct 参数，外面就变了
//
//       static void 加分(分数 s) { s.得分 += 10; }
//       加分(甲);
//       Console.WriteLine(甲.得分);    // 80，不是 90
//
//     症状：方法里改了，打印出来没变，也不报任何错。
//     为什么：struct 是按值传参，方法里的 `s` 是一个**副本**。
//     改法：返回值带出来（`return s;`），或者参数写 `ref 分数 s`
//           （`ref` 是第 05 课学的，它绕过复制）。
//
//   坑 2：以为 `record` 是值类型，也以为它传参不会共享
//
//       record 成绩记录(string 名字, int 分数);
//
//       static void 改(成绩记录 r) { r.分数 = 100; }   // ❌ 编译不过
//
//     症状：`error CS0200: 属性或索引器"成绩记录.分数"不能分配给 —— 它是只读的`。
//     为什么：位置式 record 自动生成的是 `{ get; init; }` 属性，
//           **只有 init，没有 set** ⇒ 构造完了就不能再赋值。
//     要改只能造一个新的：`var 新 = r with { 分数 = 100 };`
//
//     还有一层：record 本身是**引用类型**，`r` 参数确实是共享的 ——
//     但它的属性不允许你写，所以这个"共享"反而害不到你。
//     别把"相等是值语义"和"传参是值语义"混成一句话记。
//
//   坑 3：相等判断选错了类型，`Contains` / `==` / `Distinct` 悄悄失灵
//
//       class 成绩记录 { public string 名字; public int 分数; }   // 本该用 record
//       var 表 = new List<成绩记录> { new 成绩记录{名字="甲",分数=90} };
//       Console.WriteLine(表.Contains(new 成绩记录{名字="甲",分数=90}));  // False
//
//     症状：明明"一样"的两条，去重去不掉、查查找不到。
//     为什么：class 的 == 和 Equals 默认比**地址**；两条是不同对象 ⇒ False。
//     改法：类型改成 `record`（或 `record struct`），相等自动逐字段比。
//
//   坑 4：把元组的名字当成了类型的一部分
//
//       static (int 最低, int 最高) 取最高最低(int[] 数们) { ... }
//       var 一对 = 取最高最低(数们);
//       Console.WriteLine(取最高最低(数们).最小);   // ❌ 编译不过
//
//     症状：`error CS1061: "ValueTuple<int, int>" 未包含"最小"的定义`。
//     为什么：`(int 最低, int 最高)` 和 `(int 甲, int 乙)` 是**同一个类型**，
//           名字只是给编译器和你自己看的标签，跟着**定义那一处**走。
//
//   坑 5：数组为空时去读 `数们[0]`
//
//       int 低 = 数们[0];        // 空数组 ⇒ IndexOutOfRangeException
//
//     症状：传进空数组就崩，报 `System.IndexOutOfRangeException`。
//     为什么：`数们[0]` 在长度为 0 的数组上根本没有那个格子。
//     改法：开头先 `if (数们.Length == 0) return (0, 0);`
//          —— 初值也从 `数们[0]` 起（不要写 `int 低 = 0;`，
//          那在全负数时会返回 0，跟第 16 课 C 里那个坑一模一样）。
//
//
// ══ 轮到你了 ══
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
// ⚠ record 那一行**末尾有分号**（是"位置式 record"的简写），
//   写成带 `{ }` 的完整形式也行。
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 09

// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/cs-lessons 检查 09
//
//   看这一课要干什么：dotnet run --project csharp/cs-lessons 09
//   看答案（先自己写一遍）：dotnet run --project csharp/cs-lessons 答案 09
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
