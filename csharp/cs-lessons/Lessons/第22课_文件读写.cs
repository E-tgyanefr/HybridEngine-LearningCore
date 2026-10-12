// 第 22 课：文件读写 —— File / StreamReader，using 为什么必要
//
// ══ 这一课要教什么 ══
// 程序的数据不该只活在内存里。这一课教两条路：
//   · `File.ReadAllLines` / `WriteAllText` / `AppendAllText` —— 一行搞定，最常用
//   · `StreamReader` —— 自己开、自己关，用 `using` 保证「一定会关」
// 以及那个**只有在 Windows 上才会暴露**的坑：忘了 `using`，文件就被占住，
// 后面谁都写不进去、删不掉 —— 而且**什么错都不报**，直到别处抛 IOException。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点：**读完文件之后，文件必须能被删掉**。
//   调用点先建 `锁甲.txt`，调学生的 `首行`，然后 `File.Delete`：
//   用了 `using`（或 `File.ReadAllLines`）⇒ 打印「成功」；
//   忘了关的 `StreamReader` ⇒ 抛 `IOException`，打印「失败:IOException」。
//   这条判据是**行为**，不是看源码里有没有 using 三个字母 —— 学生抄个
//   `using` 却把 StreamReader 建在括号外面（`using (读) {...}` 但 读 是外面 new 的）
//   一样会被抓住。
//   第二条（文本）才是「有没有用 StreamReader」：题目要求手写一遍，
//   不然 `File.ReadAllLines(路径)[0]` 就绕过去了。
// ⚠ 文件都在**临时工程的当前目录**里建（相对名字），互不干扰、跑完就扔。

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("22", "第 22 课：文件读写 —— File / StreamReader，using 为什么必要",
    """
1. 写 `static string[] 读所有行(string 路径)` —— 用 `File.ReadAllLines`
2. 写 `static int 求和(string 路径)` —— 文件里每行一个整数，返回它们的和；空文件 → 0
3. 写 `static string 首行(string 路径)` —— 用 `StreamReader` 读第一行，**必须 `using`**
4. 写 `static void 加一行(string 路径, string 行)` —— 用 `File.AppendAllText` 追加一行（行尾换行）
""")]
public static class 第22课
{
    public static string 模板 => """
// 第 22 课：文件读写 —— File / StreamReader，using 为什么必要
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**数据怎么存到硬盘上、又怎么拿回来** ——
// 以及 `using` 到底在保证什么。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    File.ReadAllLines / WriteAllText / AppendAllText；StreamReader + using
//
//  需要先会
//    第 06 课：字符串；第 13 课：using 与 IDisposable（这一课把它用到文件上）
//
// ─────────────────────────────────────────────────────
//  先介绍：文件就是「一整个字符串」
// ─────────────────────────────────────────────────────
//
// 内存里的东西一关程序就没了。要留下来就得写进文件：
//
//     写   File.WriteAllText("a.txt", 「你好」);           整个文件替换成这一句
//          File.AppendAllText("a.txt", 「再见\n」);         **追加**到末尾
//          File.WriteAllLines("a.txt", new[] { "1", "2" });  一行一条
//
//     读   File.ReadAllText("a.txt")          整个读成一个字符串
//          File.ReadAllLines("a.txt")         读成 string[]（一行一个元素）
//
// ⚠ 路径是**相对当前工作目录**的。这一课的检查在临时目录里跑，
//   所以题目里用 「读甲.txt」 这种相对名字就行。
//
// ─────────────────────────────────────────────────────
//  一、把一串整数读进来求和
// ─────────────────────────────────────────────────────
//
//     string[] 行们 = File.ReadAllLines(路径);
//     int 和 = 0;
//     foreach (string 行 in 行们) {
//         if (行.Trim().Length == 0) continue;        ← 空行跳过
//         和 += int.Parse(行.Trim());
//     }
//
// ⚠ `int.Parse` 遇到空串或非数字会抛 `FormatException` —— 真实代码里
//   要用 `int.TryParse`（第 05 课）先问一句「这行是不是数字」。
//
// ─────────────────────────────────────────────────────
//  二、StreamReader：自己开、自己关
// ─────────────────────────────────────────────────────
//
//     using (var 读 = new StreamReader(路径)) {      ← 括号版
//         return 读.ReadLine();
//     }
//
//     using var 读 = new StreamReader(路径);          ← C# 8 起更常见的写法
//     return 读.ReadLine();                          ← 出了作用域自动 Dispose
//
// 两个都行：`using` 保证**不管怎么离开这段代码**（正常返回、抛异常），
//   `Dispose()` 都会被调用，文件句柄都会被还回去。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：StreamReader 忘了关，文件就被占住
// ─────────────────────────────────────────────────────
//
//     static string 首行(string 路径) {
//         var 读 = new StreamReader(路径);      ← 没有 using
//         return 读.ReadLine();
//     }
//
// 你会看到：**这个方法本身完全正常**，读出来的内容一个字都不差。
//   可是接着 `File.Delete(路径)` 会抛：
//
//         IOException: The process cannot access the file
//         because it is being used by another process.
//
//   检查里就是这样问的：读完文件之后**试着把文件删掉**，删除失败就判不过。
//
// 为什么：`StreamReader` 打开文件时向系统**要了一个句柄**，它在 `Dispose()` 里
//   才还回去。忘了关 = 句柄一直占着（直到垃圾回收碰巧收了它，什么时候不确定）。
//   ⇒ 规矩：**凡是 new 出来的、实现了 IDisposable 的东西，一律 using。**
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   异常的抓捕与自定义异常 → 第 13 课（已学）。
//   目录、二进制、JSON → 本课程不涉及。
//
// ─────────────────────────────────────────────────────
//  ══ 轮到你了 ══
// ─────────────────────────────────────────────────────
//
//   在下面写四个方法（骨架已经替你写好了 using System.IO）：
//
//    1. static string[] 读所有行(string 路径)
//         用 `File.ReadAllLines`；文件是空的就是**长度 0 的数组**
//
//    2. static int 求和(string 路径)
//         文件里每行一个整数（可能带负号），返回它们的和；空文件 → 0
//
//    3. static string 首行(string 路径)
//         用 `StreamReader` 读第一行；**必须用 using**（检查会试着删文件）
//
//    4. static void 加一行(string 路径, string 行)
//         用 `File.AppendAllText` 把 行 追加到文件末尾，**行尾要有换行**
//         ⚠ 是追加，不是覆盖：文件里原来的内容要还在
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """
// 参考答案 —— 文件读写
//
// ⚠ 一句话记住这一课：new 出来的、实现了 IDisposable 的东西，一律 using。

// 1. 一次读进所有行：最常用，也最省心（它自己会关文件）
static string[] 读所有行(string 路径)
{
    return File.ReadAllLines(路径);
}

// 2. 每行一个整数，求和
static int 求和(string 路径)
{
    string[] 行们 = File.ReadAllLines(路径);
    int 和 = 0;
    foreach (string 行 in 行们)
    {
        string 干净 = 行.Trim();
        if (干净.Length == 0)
        {
            continue;                       // 空行跳过
        }
        和 += int.Parse(干净);
    }
    return 和;
}

// 3. StreamReader：**using** 才是「文件什么时候被关掉」的答案
static string 首行(string 路径)
{
    // using var：出了这个方法就 Dispose —— 正常 return、抛异常都一样
    using var 读 = new StreamReader(路径);
    return 读.ReadLine();
}

// 4. 追加一行：AppendAllText 不会动原来的内容
static void 加一行(string 路径, string 行)
{
    File.AppendAllText(路径, 行 + "\n");     // ⚠ 换行要自己加
}
""";

    // ══════════════════ 检查 ══════════════════

    [检查说明("四个方法都在，签名也对")]
    public static CheckResult 检查_函数都在(string 代码)
    {
        const string 探针 = """
string 探针路 = "探针甲.txt";
File.WriteAllText(探针路, "1\n");
Console.WriteLine(读所有行(探针路).Length + 求和(探针路) + 首行(探针路).Length);
加一行(探针路, "2");
""";
        var run = 课工具.探针(代码, 探针);
        if (run.编译通过) return CheckResult.过("四个方法都能调用，签名也对");
        return 课工具.函数缺失(run,
            "          static string[] 读所有行(string 路径)\n" +
            "          static int      求和(string 路径)\n" +
            "          static string   首行(string 路径)\n" +
            "          static void     加一行(string 路径, string 行)");
    }

    [检查说明("读所有行：三行 / 空文件 / 只有一行都对")]
    public static CheckResult 检查_读所有行(string 代码)
    {
        var run = 课工具.跑一次(代码, 调用点());
        return 课工具.比一比(run,
            "      `File.ReadAllLines(路径)` 一行搞定：一行一个元素。\n" +
            "          空文件 ⇒ 长度 0 的数组（不是 null、也不是一个空串）",
            ("读甲", "10,20,30", "读所有行(读甲.txt)　文件里是三行 10 / 20 / 30"),
            ("读甲长", "3", "读所有行(读甲.txt).Length"),
            ("读空", "0", "读所有行(读空.txt).Length　空文件"),
            ("读单", "只有一行", "读所有行(读单.txt)"));
    }

    [检查说明("求和：正数 / 空文件 / 负数都对")]
    public static CheckResult 检查_求和(string 代码)
    {
        var run = 课工具.跑一次(代码, 调用点());
        return 课工具.比一比(run,
            "      每行一个整数（可能要处理负号），空文件是 0。\n" +
            "      ⚠ 文件最后一行后面可能有换行 —— ReadAllLines 会处理好，" +
            "不要自己按 \\n 切字符串。",
            ("和甲", "60", "求和(读甲.txt)　10+20+30"),
            ("和空", "0", "求和(读空.txt)　空文件"),
            ("和负", "-2", "求和(读负.txt)　-5 + 3"),
            ("和单", "7", "求和(读单数.txt)"));
    }

    [检查说明("★ 读过之后文件必须能被删掉 —— 忘了 using 的 StreamReader 会占住它")]
    public static CheckResult 检查_没锁住文件(string 代码)
    {
        var run = 课工具.跑一次(代码, 调用点());
        return 课工具.比一比(run,
            "      这一条是**行为检查**：调用点先建好 锁甲.txt，调你的 首行，\n" +
            "      然后 `File.Delete(锁甲.txt)`。\n" +
            "          成功       ⇒ 文件句柄已经还回去了\n" +
            "          失败:IOException ⇒ 有个 StreamReader 还开着它（没 using）\n" +
            "      ⚠ 光写 using 还不够：`var 读 = new StreamReader(路径);` 写在外面、" +
            "using 写在别处，文件照样是开着的。",
            ("首行锁", "第一行", "首行(锁甲.txt)"),
            ("删锁", "第一行/成功", "读完 锁甲.txt 之后立刻删掉它：必须成功"));
    }

    [检查说明("加一行：是**追加**不是覆盖，新文件也能建出来")]
    public static CheckResult 检查_加一行(string 代码)
    {
        var run = 课工具.跑一次(代码, 调用点());
        return 课工具.比一比(run,
            "      `File.AppendAllText(路径, 行 + \"\\n\")`：\n" +
            "          · 原来的内容必须还在（不是覆盖）\n" +
            "          · 新的一行要在**末尾**\n" +
            "          · 文件本来不存在时，AppendAllText 会自己建出来",
            ("追加", "原来的一行|新的一行", "对已有文件 加一行(追乙.txt, 新的一行)"),
            ("加新", "甲|乙", "对**不存在**的文件连着 加一行 两次"),
            ("加后读", "原来的一行|新的一行|再一次", "再加一行之后"));
    }

    [检查说明("首行用的是 StreamReader（不是 File.ReadAllLines 绕过去）")]
    public static CheckResult 检查_用了StreamReader(string 代码)
    {
        string 净 = 课工具.去掉注释(代码);
        string 体 = 课工具.函数体(净, "首行");
        if (体 == null)
            return CheckResult.不过("没找到「首行」的方法体 —— 花括号写全了吗？");
        if (!体.Contains("StreamReader"))
            return CheckResult.不过(
                "「首行」里没看到 `StreamReader`。\n" +
                "      `File.ReadAllLines(路径)[0]` 结果也对（而且不会锁住文件），\n" +
                "      但这一课要你亲手开一次、关一次，才知道 `using` 在管什么。\n" +
                "      写法：\n" +
                "          using var 读 = new StreamReader(路径);\n" +
                "          return 读.ReadLine();");
        if (!课工具.含(体, "using"))
            return CheckResult.不过(
                "「首行」里有 StreamReader，但**没有 using** —— 文件会被占住。\n" +
                "          using var 读 = new StreamReader(路径);\n" +
                "          return 读.ReadLine();");
        return CheckResult.过("用了 StreamReader，而且确实用 using 管着它");
    }

    // ══════════════════ 调用点 ══════════════════

    private static string 调用点() => 课工具.造语句调用点("""
string 路甲 = "读甲.txt";
string 路空 = "读空.txt";
string 路单 = "读单.txt";
string 路负 = "读负.txt";
string 路单数 = "读单数.txt";
string 路锁 = "锁甲.txt";
string 路追 = "追乙.txt";
string 路新 = "追丙.txt";
File.WriteAllLines(路甲, new string[] { "10", "20", "30" });
File.WriteAllText(路空, "");
File.WriteAllText(路单, "只有一行");
File.WriteAllLines(路负, new string[] { "-5", "3" });
File.WriteAllText(路单数, "7");
File.WriteAllLines(路锁, new string[] { "第一行", "第二行" });
File.WriteAllLines(路追, new string[] { "原来的一行" });
File.Delete(路新);
// ⚠ 跨组共用的变量必须声明在**这一层**：每组各自的 try { } 里声明的，出了那组就没了
//   （我第一版把 首甲 声明在「首行锁」那一组里，下一组用它 ⇒ CS0103 首甲）
string 首甲 = "";
string 删锁结果 = "";
""",
        ("读甲", "", "string.Join(\",\", 读所有行(路甲))"),
        ("读甲长", "", "读所有行(路甲).Length"),
        ("读空", "", "读所有行(路空).Length"),
        ("读单", "", "string.Join(\",\", 读所有行(路单))"),
        ("和甲", "", "求和(路甲)"),
        ("和空", "", "求和(路空)"),
        ("和负", "", "求和(路负)"),
        ("和单", "", "求和(路单数)"),
        ("首行锁", "首甲 = 首行(路锁);", "首甲"),
        ("删锁", "try { File.Delete(路锁); 删锁结果 = \"成功\"; } catch (Exception __e2) { 删锁结果 = \"失败:\" + __e2.GetType().Name; }",
                 "首甲 + \"/\" + 删锁结果"),
        ("追加", "加一行(路追, \"新的一行\");", "string.Join(\"|\", File.ReadAllLines(路追))"),
        ("加新", "加一行(路新, \"甲\"); 加一行(路新, \"乙\");", "string.Join(\"|\", File.ReadAllLines(路新))"),
        ("加后读", "加一行(路追, \"再一次\");", "string.Join(\"|\", File.ReadAllLines(路追))"));
}
