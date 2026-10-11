using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("11", "第 11 课：`virtual` / `override` / `new` —— 覆写与隐藏不是一回事",
    """
1. 写 `class 动物`：构造器 `public 动物(string 名字)` 把名字存进 `public string 名字 { get; }`，`public virtual string 叫声()` 返回 `"..."`，`public string 介绍()` 返回 `"名字 说 叫声"`
2. 写 `class 狗 : 动物`：构造器 `public 狗(string 名字) : base(名字)`，用 `sealed override` 写 `叫声()` 返回 `"汪汪"`；再写 `class 玩具狗 : 狗`，用 `new` 隐藏叫声（返回 `"玩具叫"`）
3. 写 `class 汽车`：`public string 品牌 { get; }`、`public int 最高速 { get; }`，构造器 `public 汽车(string 品牌, int 最高速)`；再写 `class 跑车 : 汽车`，构造器 `public 跑车(string 品牌, int 最高速, int 零百加速) : base(品牌, 最高速)`，属性 `public int 零百加速 { get; }`，方法 `public string 简介()` 返回 `"品牌 最高速 零百加速"`
""")]
public static class 第11课
{
    public static string 模板 => """
// 第 11 课：virtual / override / new —— 覆写与隐藏不是一回事
//
// ══ 先介绍：多态是怎么生效的 ══
//
//   第 08 课你让三个图形共用一套调用。那时候你可能没细想：
//   `g.面积()` 里的 `g` 是接口类型，为什么能调到"圆"的实现？
//
//   答案在**方法分派**上。C# 里方法有两种绑定时机：
//
//     编译时绑定：看**变量的类型**，定死调哪个方法
//     运行时绑定：看**对象的实际类型**，到运行时才决定调哪个
//
//   C# 的默认是**编译时绑定**。`virtual` 是唯一把某个方法切换到
//   运行时绑定的开关 —— 而 `override` 是"我要接这个开关"。
//
//   ══ 三个词各管什么 ══
//
//     `virtual`   写在**基类**：这个方法允许被改写
//     `override`  写在**派生类**：我要改写它，并且保持"看实际类型"
//     `new`       写在**派生类**：我要**换一个同名方法**，跟基类那个没关系
//
//   关键在于 `new` 和 `override` 的差别 —— 这一个字的差别，
//   决定了"通过基类类型调用时，到底跑哪一个"。
//
//
// ══ 一、最小对照实验：这一课的全部内容都在这 12 行里 ══
//
//       class 动物
//       {
//           public virtual string 叫声() => "...";
//       }
//
//       class 狗 : 动物
//       {
//           public override string 叫声() => "汪汪";     // 覆写
//       }
//
//       class 玩具狗 : 狗
//       {
//           public new string 叫声() => "玩具叫";         // 隐藏
//       }
//
//   现在把两种东西都当成"动物"来用，调同一个 `叫声()`：
//
//       动物 a1 = new 狗("旺财");
//       动物 a2 = new 玩具狗("小玩具");
//
//       Console.WriteLine(a1.叫声());   // 汪汪     ← override 生效
//       Console.WriteLine(a2.叫声());   // ...      ← ❗ new 不生效！
//
//   ⚠⚠ `a2` 那里是 `...`（狗从动物继承来的实现），不是 `玩具叫`。
//
//   为什么：`new` **没有**接上运行时绑定。编译器看到 `a2` 的**声明类型**
//   是 `动物`，就定死调 `动物.叫声()` —— 运行时根本不去问对象到底是什么。
//
//   想真的调到"玩具叫"，得把变量声明成 `玩具狗`：
//
//       玩具狗 t = new 玩具狗("小玩具");
//       Console.WriteLine(t.叫声());   // 玩具叫    ← 编译时看到的是玩具狗
//
//   ⇒ 一句话记法：
//
//       **override 是"换掉实现"，new 是"另起一个同名的方法"。**
//
//   再来一个更刺眼的对照 —— 同一个对象，两种声明方式，两种结果：
//
//       玩具狗 t = new 玩具狗("小玩具");
//       Console.WriteLine(t.叫声());             // 玩具叫
//       Console.WriteLine(((动物)t).叫声());     // ...     ← 把地址"看成"动物，就变了
//
//   注意：**对象从头到尾只有一个**。变的只是"编译器允许你看见哪个方法"。
//
//
// ══ 二、什么时候该用哪个？ ══
//
//   · 派生类想**改变行为**、而且希望"外面用基类类型调用时也生效"
//       ⇒ `override`（前提：基类那个方法标了 `virtual` / `abstract`）
//
//   · 基类的方法**没有**标 virtual，你就是想提供一个同名的新方法
//       ⇒ 编译器会逼你写 `new`（不写只是警告 `CS0108`，但一定要写）
//       ⚠ 这种"隐藏"通常是**设计事故的信号**：先问一句
//         "是不是该回基类加 virtual？"
//
//   · 想彻底禁止别人继续覆写
//       ⇒ 在 override 上加 `sealed`：
//            public sealed override string 叫声() => "汪汪";
//          这样 `玩具狗 : 狗` 再写 `override 叫声()` 会报
//            `error CS0239: "玩具狗.叫声()": 无法重写继承成员"狗.叫声()"，
//             因为它已密封`
//          —— 本课的玩具狗因此只能用 `new`。
//
//   ⚠ `sealed` 也可以整个加在类上（`sealed class 玩具狗`），
//     意思是"这个类不许被继承"。那和 `sealed override` 是两回事。
//
//
// ══ 三、构造器链 `: base(...)` ══
//
//   派生类**从来不继承构造器**。你必须自己写一个，并且负责把基类需要的东西递上去。
//
//       class 汽车
//       {
//           public string 品牌 { get; }
//           public int 最高速 { get; }
//           public 汽车(string 品牌, int 最高速)
//           {
//               this.品牌 = 品牌;
//               this.最高速 = 最高速;
//           }
//       }
//
//       class 跑车 : 汽车
//       {
//           public int 零百加速 { get; }
//
//           public 跑车(string 品牌, int 最高速, int 零百加速)
//               : base(品牌, 最高速)            // ← 这一行把前两个参数交上去
//           {
//               this.零百加速 = 零百加速;        // ← 然后才轮到自己的
//           }
//       }
//
//   ⚠⚠ **`: base(...)` 是这个顺序里的第一步，永远在派生类构造器体之前跑。**
//     所以派生类构造器里能安全地读基类的属性（因为那时候基类已经初始化好了）。
//
//   ⚠⚠ 忘了写 `: base(...)` 会怎样？**不是运行时出错，是编译不过**：
//
//       跑车(string 品牌, int 最高速, int 零百加速) { ... }
//       // error CS7036: 未提供与"汽车.汽车(string, int)"的所需参数
//       //               对应的实参
//
//     为什么：编译器默认帮你插一句 `: base()`（**无参**的），
//           而 `汽车` 只有带参数的构造器 ⇒ 找不到无参的那个 ⇒ 编译失败。
//     什么时候能省：基类**有**无参构造器时（比如你自己写了一个，
//           或者基类一个构造器都没写、编译器送了它一个无参的）。
//
//   ⚠ 每一步顺序（背下来，报错时对得上号）：
//       ① 派生类的**字段初始化器**（`public int x = 5;`）
//       ② 基类构造器（`: base(...)`，它会递归地继续往上）
//       ③ 派生类构造器的**函数体**
//
//
// ══ 四、常见坑 ══
//
//   坑 1：该写 override 却写了 new ⇒ 变成隐藏
//
//       class 狗 : 动物 { public new string 叫声() => "汪汪"; }
//       动物 a = new 狗("旺财");
//       Console.WriteLine(a.叫声());        // ...
//
//     症状：派生类自己的测试全过，但**别人拿着基类类型一调用就变回基类行为**；
//           而且**不报错**（最多一条 `CS0108` 警告让你加 new）。
//     为什么：new 不会接上运行时绑定，编译时按声明类型定死。
//     改法：把 `new` 改成 `override`。若编译器说"没有可重写的成员"，
//           说明基类那个方法没标 `virtual` —— 回基类补上。
//
//   坑 2：基类漏写 `virtual`
//
//       class 动物 { public string 叫声() => "..."; }        // 忘了 virtual
//       class 狗 : 动物 { public override string 叫声() => "汪汪"; }
//
//     症状：`error CS0506: "狗.叫声()": 无法重写继承成员"动物.叫声()"，
//           因为它未标记为 virtual、abstract 或 override`。
//     为什么：`override` 需要一个"允许被改写"的基类成员，`virtual` 就是那个许可。
//     改法：基类 `public virtual string 叫声()`。
//           —— 这条报错信息本身已经把答案说出来了，照着改就行。
//
//   坑 3：以为派生类会继承构造器
//
//       class 动物 { public 动物(string 名字) { ... } }
//       class 狗 : 动物 { }                    // 什么都没写
//       var d = new 狗("旺财");                 // ❌
//
//     症状：`error CS7036: 未提供与"动物.动物(string)"的所需参数对应的实参`，
//           而且报错**指向 `class 狗` 那一行**，你会以为是狗写错了。
//     为什么：狗需要一个能接收 名字 的构造器，而它一个都没有；
//           编译器送的那个是 `狗() : base()`，而 `动物` 没有无参构造器。
//     改法：给狗写 `public 狗(string 名字) : base(名字) { }`。
//
//   坑 4：在构造器里调用虚方法
//
//       class 基
//       {
//           public 基() { 初始化(); }
//           protected virtual void 初始化() { }
//       }
//       class 派 : 基
//       {
//           private readonly int[] 数据 = new int[10];
//           protected override void 初始化() { 数据[0] = 1; }   // 💥
//       }
//
//     症状：`NullReferenceException`，而且堆栈指向基类构造器那一行 ——
//           看起来完全不像自己写的代码出错。
//     为什么：基类构造器跑的时候，**派生类的字段还没初始化**（顺序见上），
//           `数据` 还是 null，而虚方法已经被派发到派生类的实现上了。
//     改法：构造器里别调虚方法；真要调，就在派生类自己构造完之后再调。
//
//   坑 5：`sealed override` 之后继续 override
//
//       class 狗 : 动物 { public sealed override string 叫声() => "汪汪"; }
//       class 玩具狗 : 狗 { public override string 叫声() => "玩具叫"; }
//
//     症状：`error CS0239: "玩具狗.叫声()": 无法重写继承成员"狗.叫声()"，
//           因为它已密封`。
//     为什么：`sealed` 的语义就是"到此为止"。
//     改法：这一课**故意**让你撞这个错，然后用 `new` 绕过去 ——
//           绕过去了也就能亲眼看到隐藏和覆写的差别了。
//
//
// ══ 轮到你了 ══
//
//     class 动物                          // 名字属性 + virtual 叫声 + 介绍
//     class 狗     : 动物                 // sealed override 叫声 → "汪汪"
//     class 玩具狗 : 狗                   // new 隐藏叫声 → "玩具叫"
//
//     class 汽车                          // 品牌 / 最高速
//     class 跑车   : 汽车                 // : base(...) + 零百加速 + 简介()
//
//   ⚠ 玩具狗必须用 `new`（因为狗的叫声是 sealed override）。
//     这正是本课要看的东西：`动物 a = new 玩具狗(...); a.叫声()` 返回的是
//     狗的实现，不是玩具狗的。
//
//   ⚠ `介绍()` 要写成 `名字 + " 说 " + 叫声()` —— 这样它调的是**虚**方法，
//     运行时按实际类型走。
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 11
""";

    public static string 参考答案 => """
class 动物
{
    public string 名字 { get; }

    public 动物(string 名字)
    {
        this.名字 = 名字;            // ⚠ this. 不能省
    }

    public virtual string 叫声() => "...";

    public string 介绍() => 名字 + " 说 " + 叫声();
}

class 狗 : 动物
{
    public 狗(string 名字) : base(名字) { }      // ⚠ 派生类不继承构造器

    public sealed override string 叫声() => "汪汪";   // sealed ⇒ 下面不许再 override
}

class 玩具狗 : 狗
{
    public 玩具狗(string 名字) : base(名字) { }

    // ⚠ 狗的叫声是 sealed ⇒ 只能 new（隐藏）。写 override 会 CS0239
    public new string 叫声() => "玩具叫";
}

class 汽车
{
    public string 品牌 { get; }
    public int 最高速 { get; }

    public 汽车(string 品牌, int 最高速)
    {
        this.品牌 = 品牌;
        this.最高速 = 最高速;
    }
}

class 跑车 : 汽车
{
    public int 零百加速 { get; }

    // ⚠ : base(...) 先跑，然后才是下面这行
    public 跑车(string 品牌, int 最高速, int 零百加速)
        : base(品牌, 最高速)
    {
        this.零百加速 = 零百加速;
    }

    public string 简介() => 品牌 + " " + 最高速 + " " + 零百加速;
}
""";


    [检查说明("狗真的覆写了叫声（不是 new）")]
    public static CheckResult 检查_1(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("override string 叫声()"))
            return CheckResult.不过("没找到「override string 叫声()」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("隐藏：玩具狗按声明类型走")]
    public static CheckResult 检查_2(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(用声明类型取叫声(new 玩具狗("小玩具")));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "玩具叫"
        };
        string[] 输入 = {
            "new 玩具狗(\"小玩具\")"
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

    [检查说明("跑车把参数交给了基类")]
    public static CheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains(": base("))
            return CheckResult.不过("没找到「: base(」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("多态：通过基类类型调用也走派生类实现")]
    public static CheckResult 检查_4(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(用多态取叫声(new 狗("旺财")));
    Console.WriteLine(用多态取叫声(new 动物("甲")));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "汪汪",
            "……"
        };
        string[] 输入 = {
            "new 狗(\"旺财\")",
            "new 动物(\"甲\")"
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

    [检查说明("隐藏：玩具狗按声明类型走")]
    public static CheckResult 检查_5(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(用声明类型取叫声(new 玩具狗("小玩具")));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "玩具叫"
        };
        string[] 输入 = {
            "new 玩具狗(\"小玩具\")"
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

    [检查说明("构造器链：base 先跑，自己的属性也赋值了")]
    public static CheckResult 检查_6(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(new 学生代码.跑车("甲", 300, 4).简介());
    Console.WriteLine(new 学生代码.跑车("乙", 200, 8).简介());

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "\"甲 300 4\"",
            "\"乙 200 8\""
        };
        string[] 输入 = {
            "new 跑车(\"甲\", 300, 4).简介()",
            "new 跑车(\"乙\", 200, 8).简介()"
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
