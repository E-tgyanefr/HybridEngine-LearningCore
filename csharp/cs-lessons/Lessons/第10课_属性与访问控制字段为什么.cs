using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("10", "第 10 课：属性与访问控制 —— 字段为什么不该是 public",
    """
1. 写 `class 学生`：构造器 `public 学生(string 名字, int 成绩)`，属性 `public string 名字 { get; }`、`public int 成绩 { get; private set; }`，方法 `public void 改成绩(int 分)` —— 分不在 0~100 之间就**什么都不做**；`public string 报告()` 返回 `"名字 成绩"` 一行
2. 写 `class 计数器`：属性 `public int 值 { get; private set; }`，方法 `public void 加(int n)`、`public void 设为(int v)`（v 为负数时不做任何事）、`public void 重置()`
3. 两个类里都要有一条 `readonly` 字段
""")]
public static class 第10课
{
    public static string 模板 => """
// 第 10 课：属性与访问控制 —— 字段为什么不该是 public
//
// ══ 先介绍：从"谁都能改"到"只有我能改" ══
//
//   第 07 课你已经把数据和行为关在一起了。但那时候你多半是这么写的：
//
//       class 学生
//       {
//           public string 名字;
//           public int 成绩;          // ← public 字段：外面随便写
//       }
//
//       var 甲 = new 学生();
//       甲.成绩 = -999;               // ✅ 编译通过 —— 但这是垃圾数据
//       甲.成绩 = 99999;              // ✅ 也通过
//
//   问题不在"能不能写"，而在**没有任何地方能拦住它**。
//   等到某天要统计平均分，你拿到一堆 -999，回头去查是谁写的 —— 无处可查，
//   因为每一个 `甲.成绩 = ...` 都是一个合法的写入点。
//
//   属性（property）解决的正是这件事：
//
//       class 学生
//       {
//           public int 成绩 { get; private set; }   // 外面只能读
//
//           public void 改成绩(int 分)
//           {
//               if (分 < 0 || 分 > 100) return;     // ← 唯一入口，唯一的规则
//               成绩 = 分;
//           }
//       }
//
//   ⇒ 这就是**封装**：不是"藏起来让你看不见"，而是让
//     **规则只有一个出口**。想把成绩改成 -999？做不到。
//
//
// ══ 一、属性 vs 字段：写起来像字段，其实是方法 ══
//
//   最简形式（自动实现属性，最常用）：
//
//       public string 名字 { get; set; }              // 可读可写
//       public string 名字 { get; }                    // 只读：只有构造器能赋
//       public int 成绩 { get; private set; }          // 外人能读，本类能写
//       public int 值 { get; init; }                   // 只能在**创建时**写
//
//   编译器在背后给你造了一个隐藏字段 + 两个方法（get_名字 / set_名字）。
//   所以 `甲.成绩` 用起来像字段，其实是**调用了一个方法** —— 这正是它能带逻辑的原因。
//
//   想亲手写逻辑时，用完整形式：
//
//       private int 成绩_存;                            // 私有字段，外面看不见
//       public int 成绩
//       {
//           get { return 成绩_存; }
//           set
//           {
//               if (value < 0 || value > 100)             // value = 即将写入的值
//                   throw new ArgumentOutOfRangeException(nameof(value));
//               成绩_存 = value;
//           }
//       }
//
//   `value` 是 setter 里的**关键字**（不是你自己起的名字），表示"外面想写进来的那个值"。
//
//   ⚠ 什么时候用哪种：
//     · 不需要校验、不需要计算 ⇒ **自动属性** `{ get; set; }`，别自己造字段
//     · 需要校验、需要惰性计算、需要触发别的动作 ⇒ 完整形式
//
//
// ══ 二、访问修饰符：默认值是什么？ ══
//
//       public      谁都能访问
//       private     只有**本类内部**能访问          ← 类成员的默认值！
//       protected   本类 + 派生类能访问
//       internal    同一个程序集（同一个项目）能访问  ← **顶层类型**的默认值
//       protected internal / private protected      组合，用到再说
//
//   ⚠⚠ 两个默认值必须记牢，它们相反：
//        **类里面的成员（字段/方法/属性）默认是 `private`**
//        **顶层的 class / struct / record / interface 默认是 `internal`**
//
//     症状 1：只写 `int 成绩;` 不写 public，从外面 `甲.成绩` 会报
//             `error CS0122: "学生.成绩"不可访问，因为它具有一定的保护级别`。
//     症状 2：只写 `class 学生`，别的项目引用你这个库时找不到它 ——
//             因为它是 internal，出了这个程序集就没了。
//     ⇒ 想给外面用，**必须显式写 public**。
//
//   ⚠ 别给什么都加 public。判断标准很简单：
//     "这个成员是不是这个类的**对外承诺**？" 不是 ⇒ 私有。
//
//
// ══ 三、`readonly`：给"不该变的字段"上锁 ══
//
//   `readonly` 只能用在**字段**上（不能用在属性上，也不能用在局部变量上）：
//
//       class 银行账户
//       {
//           private readonly string 账号;        // 开户时定了就不许再改
//           public 银行账户(string 账号) { this.账号 = 账号; }
//       }
//
//   规则：**只能在声明处或构造器里赋值**，之后任何地方再写 `账号 = ...`
//        都是 `error CS0191: 只读字段不能作为赋值目标（除非在构造函数中）`。
//
//   ⚠ 三个容易混的：
//     · `readonly` 字段   —— 运行期不能改，但**值可以是运行时才知道的**
//                            （比如构造器参数）
//     · `const`           —— 编译期就定死，必须写死一个字面量，
//                            而且**自动是 static**，只能配基本类型和 string
//     · `readonly` 修饰引用类型的字段 ⇒ 锁的是"指向哪个对象"，
//                            **不是对象内容**！`readonly List<int> a` 里
//                            照样可以 `a.Add(1)`。
//
//   ⚠ 和 `{ get; }` 属性的区别：
//     · `{ get; }` 外面能读，不能写           ← 给外人看的只读
//     · `readonly` 字段 外面连读都读不到（除非你自己加个属性暴露出去）
//     ⇒ 通常成对用：私有 readonly 字段 + 公开的 `{ get; }` 属性。
//
//
// ══ 四、`required` 与 `init`（现代 C# 的"必须填"）══
//
//   老写法靠构造器强制："想 new 出对象？必须把参数交齐。"
//   新写法可以让属性本身就是必填的：
//
//       class 配置
//       {
//           public required string 名字 { get; init; }
//           public int 超时秒 { get; init; } = 30;      // 有默认值，可不填
//       }
//
//       var c = new 配置 { 名字 = "主库" };              // ✅
//       var d = new 配置();                              // ❌ 编译错误
//       // error CS9035: 必须设置必需成员"配置.名字"
//
//   ⚠⚠ `required` 有一个**极其常见的连锁反应**：
//
//     一旦某个属性写成 `required`，所有 `new 配置()` 的地方都编译不过了 ——
//     包括你自己以前写的、包括测试代码。报错是
//     `error CS9035: 必须设置必需成员`。
//     所以 `required` 适合**新写的类型**；给已有类型补 `required` 之前，
//     先确认所有创建点都改得过来。
//
//   ⚠ 本课的两个类**不要求**用 required（构造器已经把必填项管住了）。
//     记住它是什么、报错长什么样就够了。
//
//
// ══ 五、常见坑 ══
//
//   坑 1：用 public 字段代替属性，规则就散掉了
//
//       class 学生 { public int 成绩; }
//       甲.成绩 = -999;
//
//     症状：程序能跑、能编译，但统计结果离谱；而且**查不出是哪儿写坏的**。
//     为什么：字段没有"入口"，任何一行赋值都直接落到数据上。
//     改法：字段改私有（或删掉换自动属性），公开一个 `{ get; private set; }`
//           + 一个带校验的方法。
//
//   坑 2：以为"自动生成的无参构造器"一直都在
//
//       class 学生 { public 学生(string 名字) { ... } }
//       var 甲 = new 学生();          // ❌ 编译不过
//
//     症状：`error CS7036: 未提供与"学生.学生(string)"的所需参数对应的实参`。
//     为什么：**只要你写了任何一个构造器，编译器就不再送你无参的那个了。**
//           这是官方专门讲过的点，很多人被它绊一次。
//     改法：要么给参数加默认值 `public 学生(string 名字 = "")`，
//          要么显式再写一个 `public 学生() { }`。
//
//   坑 3：`{ get; private set; }` 里 set 的权限记反了
//
//       public int 成绩 { get; private set; }
//       甲.成绩 = 90;                   // ❌ 编译不过
//
//     症状：`error CS0272: 属性或索引器"学生.成绩"不能用在此上下文中 --
//           set 访问器不可访问`。
//     为什么：`private set` 的意思是"**只有这个类自己**能写"，
//           **外面（包括 Main、包括检查器）都不能写** ⇒ 编译期就拦住了。
//           这正是你要的效果，只是第一次撞见会以为写错了。
//     改法：谁都别想在类外面写 —— 想要那种自由就写 `set;`，
//          但那就等于把规则又扔掉了。
//
//   坑 4：把 `readonly` 当成"字段指向的对象不能变"
//
//       private readonly List<int> 缓冲 = new List<int>();
//       缓冲.Add(1);                     // ✅ 编译通过！
//
//     症状：以为加了 readonly 就"内容也冻住了"，结果照样被改。
//     为什么：引用类型字段里装的是**地址**。readonly 锁的是这个地址
//           （不许 `缓冲 = new List<int>()`），**不是**堆上那个列表。
//     改法：要冻内容得靠 `IReadOnlyList<int>` 之类的**类型**去限制，
//           或者干脆不要暴露可变集合。
//
//   坑 5：构造器里参数名和属性名撞了，直接写 `名字 = 名字;`
//
//       public 学生(string 名字) { 名字 = 名字; }      // ❌ 什么都没干
//
//     症状：构造完了一打印，属性是 null / 0。
//     为什么：两边都写 `名字` 时，靠得近的那个（参数）胜出 ⇒ 变成
//           "把参数赋给参数"，属性压根没被碰过。
//     改法：写 `this.名字 = 名字;`，把 `this.` 明确指向字段/属性。
//
//
// ══ 轮到你了 ══
//
//   写两个类（不用写 Main）：
//
//     class 学生
//     {
//         public string 名字 { get; }               // 只读
//         public int 成绩 { get; private set; }     // 外面只读，本类能写
//         public 学生(string 名字, int 成绩)
//         public void 改成绩(int 分)   // 不在 0~100 ⇒ 什么都不做
//         public string 报告()          // "名字 成绩"
//     }
//
//     class 计数器
//     {
//         public int 值 { get; private set; }
//         public void 加(int n)
//         public void 设为(int v)       // v < 0 ⇒ 什么都不做
//         public void 重置()
//     }
//
//   ⚠ 两个类里**都要有一条 readonly 字段**（比如 `private readonly string 学号;`）。
//     只读字段必须在构造器里赋上值。
//
//   ⚠ 构造器里别写 `名字 = 名字;` —— 要写 `this.名字 = 名字;`。
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 10
""";

    public static string 参考答案 => """
class 学生
{
    // readonly 字段：只能在声明处或构造器里赋值
    private readonly string 学号;

    public string 名字 { get; }              // 只读：外面改不了
    public int 成绩 { get; private set; }    // 外面只读，本类能写

    public 学生(string 名字, int 成绩)
    {
        this.名字 = 名字;                    // ⚠ this. 不能省 —— 否则是"参数 = 参数"
        学号 = "S-" + 名字;                  // readonly 字段在这里赋值，合法
        this.成绩 = 成绩 < 0 ? 0 : (成绩 > 100 ? 100 : 成绩);
    }

    public void 改成绩(int 分)
    {
        if (分 < 0 || 分 > 100) return;      // ⚠ 非法就什么都不做，不能先写再检查
        成绩 = 分;
    }

    public string 报告() => 名字 + " " + 成绩;
}

class 计数器
{
    private readonly int 步长;               // readonly 字段

    public int 值 { get; private set; }

    public 计数器(int 步长 = 1)
    {
        this.步长 = 步长;
        值 = 0;
    }

    public void 加(int n) => 值 += n * 步长;

    public void 设为(int v)
    {
        if (v < 0) return;                   // ⚠ 负数直接拒绝，值保持不变
        值 = v;
    }

    public void 重置() => 值 = 0;
}
""";


    [检查说明("两个公开属性都写成属性而不是字段")]
    public static CheckResult 检查_1(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("{ get;"))
            return CheckResult.不过("没找到「{ get;」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("成绩的 set 是私有的")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("private set"))
            return CheckResult.不过("没找到「private set」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("用了 readonly 字段")]
    public static CheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("readonly"))
            return CheckResult.不过("没找到「readonly」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("学生：非法分数被拒绝，合法分数生效")]
    public static CheckResult 检查_4(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(new 学生("甲", 70).报告());
    Console.WriteLine(new 学生("甲", 70).改成绩(999).报告());

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "\"甲 70\"",
            "\"甲 70\""
        };
        string[] 输入 = {
            "new 学生(\"甲\", 70).报告()",
            "new 学生(\"甲\", 70).改成绩(999).报告()"
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

    [检查说明("计数器：加 / 设为 / 重置 串起来都对")]
    public static CheckResult 检查_5(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(new 计数器(1).值);
    Console.WriteLine(new 计数器(1).加(3).值);

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "\"0\"",
            "\"3\""
        };
        string[] 输入 = {
            "new 计数器(1).值",
            "new 计数器(1).加(3).值"
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
