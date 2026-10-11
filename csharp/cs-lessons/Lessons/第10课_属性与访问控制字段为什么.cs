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
// 这一课讲一件事：**别让人随便改你的字段，留一个能检查的入口**。
//
// ─────────────────────────────────────────────────────
//  先介绍：public 字段的问题
// ─────────────────────────────────────────────────────
//
//     class 学生 { public int 成绩; }
//
//     甲.成绩 = -999;          ← 编译器一声不吭，而这是没意义的
//
// 第 07 课的 class 里字段就是 public 的。这一课把它关上，
// 只留一个能检查的入口。
//
// ─────────────────────────────────────────────────────
//  一、属性：长得像字段，其实是方法
// ─────────────────────────────────────────────────────
//
//     public string 名字 { get; }                 ← 只读：只有 getter
//     public int 成绩 { get; private set; }       ← 外面只读，**本类**能写
//
// ⚠ 三档对照：
//
//     public int 成绩;                    ✗ 谁都能改，没有检查
//     public int 成绩 { get; }            ✓ 只读 —— 连本类都不能改
//     public int 成绩 { get; private set; } ✓ 外面只读，本类能改
//     public int 成绩 { get; set; }       ← 等于 public 字段，没意义
//
// ⚠ 只读属性（只有 get）**必须在构造器里赋上值**，否则编译不过：
//
//     错误 CS8618: 不可为 null 的属性在退出构造函数时必须包含非 null 值
//
// ─────────────────────────────────────────────────────
//  二、readonly 字段：只能赋一次
// ─────────────────────────────────────────────────────
//
//     private readonly string 学号;
//
//     public 学生(string 学号) { this.学号 = 学号; }    ← 在构造器里赋上
//
// ⚠ readonly 和「只有 get 的属性」差不多，区别是：
//
//     readonly 字段   只能在**声明时或构造器里**赋值
//     get-only 属性   一样，但它对外暴露成一个「属性」
//
//   想看一个值但不想让别人改，两种都行。
//
// ─────────────────────────────────────────────────────
//  三、setter 里检查，或者干脆不给 setter
// ─────────────────────────────────────────────────────
//
//     public void 改成绩(int 分)
//     {
//         if (分 < 0 || 分 > 100) { return; }     ← 不合理就直接不改
//         成绩 = 分;                              ← 本类能写 private set
//     }
//
// ⚠ 判断用 **||** 不是 &&：两个条件**有一个**不合理就拒绝。
//   写成 && 等于完全没有检查（要求同时小于 0 又大于 100）。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：本类自己也改不了只读属性
// ─────────────────────────────────────────────────────
//
//     public string 名字 { get; }        ← 只有 get
//
//     public 学生(string 名字)
//     {
//         名字 = 名字;                    ← 想给它赋值
//     }
//
// 你会看到（csc 原文）：
//
//     错误 CS0200: 属性或索引器"学生.名字"不能分配给 -- 它是只读的
//
// 为什么：`{ get; }` 是「谁也改不了」，**包括这个类自己的方法**。
//   它只能在构造器里通过 `this.名字 = 名字;` 那种形式赋上初值。
//
// ⚠ 分清楚两条报错（长得像，说的是两件事）：
//
//     类自己写        错误 CS0200: … 不能分配给 -- 它是只读的
//     外面写          错误 CS0272: … 不可用，因为它是只读的
//
// ⇒ 要在本类里改，就得写 `{ get; private set; }`。
//   要彻底不能改，就用 readonly 字段 + 构造器赋值。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   virtual / override → 第 11 课。
//
// ─────────────────────────────────────────────────────
//  ══ 轮到你了 ══
// ─────────────────────────────────────────────────────
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
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
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
    var __s = new 学生代码.学生("甲", 70);
    Console.WriteLine(__s.报告());
    __s.改成绩(999);
    Console.WriteLine(__s.报告());

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "甲 70",
            "甲 70"
        };
        string[] 输入 = {
            "var __s = new 学生(\"甲\", 70); ⇒ __s.报告()",
            "__s.改成绩(999); ⇒ __s.报告()"
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
    var __c = new 学生代码.计数器(1);
    Console.WriteLine(__c.值);
    __c.加(3);
    Console.WriteLine(__c.值);
    __c.设为(10);
    Console.WriteLine(__c.值);
    __c.重置();
    Console.WriteLine(__c.值);

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "0",
            "3",
            "10",
            "0"
        };
        string[] 输入 = {
            "var __c = new 计数器(1); ⇒ __c.值",
            "__c.加(3); ⇒ __c.值",
            "__c.设为(10); ⇒ __c.值",
            "__c.重置(); ⇒ __c.值"
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
