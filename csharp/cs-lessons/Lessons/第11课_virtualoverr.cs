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
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**子类改掉父类的方法，有两种改法，效果完全不同**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    virtual / override / new：覆写和隐藏不是一回事
//
//  需要先会
//    第 08 课：继承与接口
//
// ─────────────────────────────────────────────────────
//  先介绍：为什么会有两种
// ─────────────────────────────────────────────────────
//
// 第 08 课已经会用 `override` 了。那 `new` 是什么？
//
//     override   真的**换掉**那个方法 —— 通过父类变量调用也走子类版本
//     new        只是**盖住**它 —— 通过父类变量调用还走父类版本
//
// 差别在「用父类类型看这个对象」时才暴露出来，而那正是多态的场景。
//
// ─────────────────────────────────────────────────────
//  一、virtual / override
// ─────────────────────────────────────────────────────
//
//     class 动物
//     {
//         public string 名字 { get; }
//         public 动物(string 名字) { this.名字 = 名字; }
//
//         public virtual string 叫声() { return "……"; }
//         public string 介绍() { return 名字 + " 说 " + 叫声(); }
//     }
//
//     class 狗 : 动物
//     {
//         public 狗(string 名字) : base(名字) { }
//         public override string 叫声() { return "汪汪"; }
//     }
//
// ⚠ 两条规矩：
//
//     父类方法要写 `virtual`，子类才能 override（不写就改不了）
//     子类重写要写 `override`（不写就变成 new，悄悄藏起来）
//
// ─────────────────────────────────────────────────────
//  二、sealed override：到此为止
// ─────────────────────────────────────────────────────
//
//     public sealed override string 叫声() { return "汪汪"; }
//
// ⚠ `sealed` 的意思是「**我改完了，我的子类不许再改**」。
//   狗的叫声 sealed 之后，玩具狗就不能 override 了 —— 只能 new（隐藏）。
//
// ─────────────────────────────────────────────────────
//  三、new：隐藏，不是覆写
// ─────────────────────────────────────────────────────
//
//     class 玩具狗 : 狗
//     {
//         public new string 叫声() { return "玩具叫"; }
//     }
//
// ⚠ 关键实验：**用父类类型看它**：
//
//     玩具狗 玩 = new 玩具狗("球球");
//     玩.叫声();                     ⇒ 玩具叫      ← 看的是玩具狗
//
//     狗 当狗看 = new 玩具狗("球球");
//     当狗看.叫声();                 ⇒ 汪汪        ← **还是狗的！**
//
//   这就是 new 和 override 的分水岭：换个视角看，结果就变了。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：该写 override 的地方写了 new
// ─────────────────────────────────────────────────────
//
//     class 狗 : 动物
//     {
//         public new string 叫声() { return "汪汪"; }    ← 用了 new
//     }
//
//     动物 a = new 狗("旺财");
//     Console.WriteLine(a.叫声());        ⇒ ……      ← 你以为会打「汪汪」
//
// 你会看到：**编译通过**，只是多态失效了 —— 通过父类变量调用走的还是父类的版本。
//
// 为什么：`new` 只是「这个名字在本类里重新起一个」，两个方法**同时存在**；
//   调哪个由**变量的类型**决定，不是由对象的实际类型决定。
//
// ⚠ 编译器给的是 **warning CS0108**（「…隐藏了继承的成员；如果打算重写，请使用 override」）
//   —— 是 warning 不是 error，所以你不看警告就会带着这个 bug 继续往下写。
//
// ⇒ 一条判据：**想改掉父类的行为，就用 override；**
//   `new` 只在「我确实想留两个版本」时才用，那很少见。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   泛型与集合 → 第 12 课。异常 → 第 13 课。
//
// ─────────────────────────────────────────────────────
//  ══ 轮到你了 ══
// ─────────────────────────────────────────────────────
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
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
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

    [检查说明("隐藏：玩具狗按声明类型走（new 出来的按声明类型走）")]
    public static CheckResult 检查_2(string 代码)
    {
        var 调用点 = """
    var __t = new 学生代码.玩具狗("小玩具");
    Console.WriteLine(__t.叫声());

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "玩具叫"
        };
        string[] 输入 = {
            "var __t = new 玩具狗(\"小玩具\"); ⇒ __t.叫声()"
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
    var __d = new 学生代码.狗("旺财");
    Console.WriteLine(__d.叫声());
    动物 __a = new 学生代码.狗("旺财");
    Console.WriteLine(__a.叫声());
    动物 __b = new 学生代码.动物("甲");
    Console.WriteLine(__b.叫声());

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "汪汪",
            "汪汪",
            "..."
        };
        string[] 输入 = {
            "var __d = new 狗(\"旺财\"); ⇒ __d.叫声()",
            "动物 __a = new 狗(\"旺财\"); ⇒ __a.叫声()",
            "动物 __b = new 动物(\"甲\"); ⇒ __b.叫声()"
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

    [检查说明("隐藏：玩具狗按声明类型走（new 出来的按声明类型走）")]
    public static CheckResult 检查_5(string 代码)
    {
        var 调用点 = """
    var __t = new 学生代码.玩具狗("小玩具");
    Console.WriteLine(__t.叫声());

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "玩具叫"
        };
        string[] 输入 = {
            "var __t = new 玩具狗(\"小玩具\"); ⇒ __t.叫声()"
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
            "甲 300 4",
            "乙 200 8"
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
