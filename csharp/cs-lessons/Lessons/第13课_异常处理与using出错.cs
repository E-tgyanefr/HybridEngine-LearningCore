using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("13", "第 13 课：异常处理与 `using` —— 出错的时候怎么办",
    """
1. 写 `class 分数越界异常 : Exception`，构造器 `public 分数越界异常(string 消息) : base(消息)`；再写 `static int 查成绩(int[] 表, int 下标)` —— 下标越界时 `throw new 分数越界异常(...)`
2. 写 `static double 算平均(int[] 数们)` —— 空数组时 `throw new ArgumentException("数组不能为空")`，否则返回平均值
3. 写 `static int 带资源算(int 数)` —— 用 `using` 开一个 `StringWriter`，往里写一行字，最后 `return 数 * 2`（证明 `using` 和 `return` 能共存）
""")]
public static class 第13课
{
    public static string 模板 => """
// 第 13 课：异常处理与 using —— 出错的时候怎么办
//
// 这一课讲两件事：**接住错误**（try / catch）、**自己抛错误**（throw）。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    异常处理与 using
//
//  需要先会
//    第 08 课：类（自定义异常要继承 Exception）
//
// ─────────────────────────────────────────────────────
//  先介绍：不接住会怎样
// ─────────────────────────────────────────────────────
//
// 访问一个越界的下标，C# 会抛异常。没人接住的话：
//
//     程序当场停下（界面上会显示一大段红色的堆栈）。
//
// 而「用户给了个奇怪的数」在真实程序里天天发生 ——
// 你希望的是「告诉他这不对，然后继续」，不是整个程序崩掉。
//
// ─────────────────────────────────────────────────────
//  一、try / catch / finally
// ─────────────────────────────────────────────────────
//
//     try
//     {
//         // 可能出事的代码
//     }
//     catch (Exception e)
//     {
//         // 出事之后来这里
//     }
//     finally
//     {
//         // **不管出没出事都会执行**
//     }
//
// ⚠ 三条要记牢的：
//
//     ① try 里出事那一行之后的语句**全部跳过**，直接跳到 catch
//     ② catch 收的是**类型匹配**的异常
//     ③ finally 一定会执行 —— 哪怕 catch 里又 return 了
//
// ─────────────────────────────────────────────────────
//  二、自己抛异常：throw
// ─────────────────────────────────────────────────────
//
//     if (下标 < 0 || 下标 >= 表.Length)
//     {
//         throw new 分数越界异常("下标越界：" + 下标);
//     }
//
// 这是「主动拒绝不合理输入」的写法 —— 不是等运行时报错，是你先拦。
//
// ⚠ 自定义异常类长这样（继承 Exception）：
//
//     class 分数越界异常 : Exception
//     {
//         public 分数越界异常(string 消息) : base(消息) { }
//     }
//
//   那个 `: base(消息)` 是把消息交给父类存起来 —— 之后 e.Message 才取得到。
//   第 11 课学的 `: base(...)` 在这里又用上了。
//
// ─────────────────────────────────────────────────────
//  三、using：用完自动收拾
// ─────────────────────────────────────────────────────
//
//     using (var 写手 = new System.IO.StringWriter())
//     {
//         写手.WriteLine("记一笔");
//     }                        ← 出了这个花括号，写手 自动被关掉
//
// ⚠ `using` 在这里有两个意思，别混：
//
//     using System;                    文件**最上面**：引用命名空间
//     using (var x = new ...) { }      代码里：用完自动释放资源
//
//   本课的 StringWriter 在 System.IO 里 —— 要么写全名
//   `System.IO.StringWriter`，要么自己在文件顶上加一行 `using System.IO;`。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：忘了 : base(消息)
// ─────────────────────────────────────────────────────
//
//     class 分数越界异常 : Exception
//     {
//         public 分数越界异常(string 消息) { }      ← 没交给 base
//     }
//
// 你会看到：**能编译能跑**，只是 `e.Message` 是空的或者一句默认话，
//   你精心写的那句「下标越界：9」**丢了**。
//
// 为什么：消息存在父类 Exception 里。你不通过 `: base(消息)` 交上去，
//   子类的构造器拿到参数之后就扔掉了 —— 编译器不管这件事。
//
// ⇒ 写自定义异常时记住那一句：`public 名字(string 消息) : base(消息) { }`
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   模式匹配 → 第 14 课。LINQ → 第 15 课。
//
// ─────────────────────────────────────────────────────
//  ══ 轮到你了 ══
// ─────────────────────────────────────────────────────
//
//     class 分数越界异常 : Exception
//     {
//         public 分数越界异常(string 消息) : base(消息) { }
//     }
//
//     static int 查成绩(int[] 表, int 下标)
//         —— 下标 < 0 或 >= 表.Length ⇒ throw new 分数越界异常(...)
//            否则返回 表[下标]
//
//     static double 算平均(int[] 数们)
//         —— 空数组 ⇒ throw new ArgumentException("数组不能为空")
//            否则返回平均值
//
//     static int 带资源算(int 数)
//         —— using 一个 StringWriter，往里写一行，返回 数 * 2
//
//   ⚠ 顶部已经 using System; / System.Collections.Generic; / System.Linq;
//     `StringWriter` 在 `System.IO` 里 —— 写全名 `System.IO.StringWriter`，
//     或者自己在文件顶上加一行 `using System.IO;`。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """

class 分数越界异常 : Exception
{
    public 分数越界异常(string 消息) : base(消息) { }
}

static int 查成绩(int[] 表, int 下标)
{
    if (下标 < 0 || 下标 >= 表.Length)
    {
        // ⚠ 消息要写清"哪个值、什么范围" —— 这是将来唯一的线索
        throw new 分数越界异常($"下标 {下标} 超出 0~{表.Length - 1} 的范围");
    }
    return 表[下标];
}

static double 算平均(int[] 数们)
{
    if (数们.Length == 0)
    {
        throw new ArgumentException("数组不能为空");
    }

    int 和 = 0;
    for (int i = 0; i < 数们.Length; i++)
    {
        和 += 数们[i];
    }
    // ⚠ 至少一边要是 double，否则这里是整数除法（向零取整）
    return (double)和 / 数们.Length;
}

static int 带资源算(int 数)
{
    // using var：作用域结束时自动 Dispose —— 出了异常也照样 Dispose
    // ⚠ 这里原来写的是 `写手.WriteLine(...)` —— 而 `写手` 是**变量名**，
    //   它自己的 `using var 写手 = new StringWriter();` 被删掉了，
    //   于是这一行引用了一个不存在的名字：
    //       error CS0103: 当前上下文中不存在名称"写手"
    //   ⇒ 补回声明。要演示的重点是「using 和 return 能共存」。
    using var 写手 = new StringWriter();
    写手.WriteLine("干活");

    // ⚠ return 不会跳过 Dispose：先算好返回值，再 Dispose，最后才返回
    return 数 * 2;
}
""";


    [检查说明("自定义异常写了，而且继承了 Exception")]
    public static CheckResult 检查_1(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains(": Exception"))
            return CheckResult.不过("没找到「: Exception」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("真的用了 throw")]
    public static CheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("throw new"))
            return CheckResult.不过("没找到「throw new」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("算平均：平均值是小数（不是向零取整的整数）")]
    public static CheckResult 检查_3(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(算平均(new int[]{1,2}).ToString("F6"));
    Console.WriteLine(算平均(new int[]{90,80,70}).ToString("F6"));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "1.500000",
            "80.000000"
        };
        string[] 输入 = {
            "new int[]{1,2}",
            "new int[]{90,80,70}"
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

    [检查说明("带资源算：using 与 return 共存，值还是对的")]
    public static CheckResult 检查_4(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(带资源算(21));
    Console.WriteLine(带资源算(0));
    Console.WriteLine(带资源算(-5));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "42",
            "0",
            "-10"
        };
        string[] 输入 = {
            "21",
            "0",
            "-5"
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

    [检查说明("查成绩：合法下标取得到")]
    public static CheckResult 检查_5(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(查成绩(new int[]{10,20,30}, 1));
    Console.WriteLine(查成绩(new int[]{10,20,30}, 2));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "20",
            "30"
        };
        string[] 输入 = {
            "new int[]{10,20,30}, 1",
            "new int[]{10,20,30}, 2"
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

    [检查说明("带资源算：using 与 return 共存，值还是对的")]
    public static CheckResult 检查_6(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(带资源算(21));
    Console.WriteLine(带资源算(0));
    Console.WriteLine(带资源算(-5));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "42",
            "0",
            "-10"
        };
        string[] 输入 = {
            "21",
            "0",
            "-5"
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

    [检查说明("算平均：平均值是小数（不是向零取整的整数）")]
    public static CheckResult 检查_7(string 代码)
    {
        var 调用点 = """
    Console.WriteLine(算平均(new int[]{1,2}).ToString("F6"));
    Console.WriteLine(算平均(new int[]{90,80,70}).ToString("F6"));

""";
        var run = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "1.500000",
            "80.000000"
        };
        string[] 输入 = {
            "new int[]{1,2}",
            "new int[]{90,80,70}"
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
