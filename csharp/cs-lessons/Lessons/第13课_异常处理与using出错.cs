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
// ══ 先介绍：什么该用异常 ══
//
//   先分清两类"不对"：
//
//     ① **可以预料的、调用方应该处理的**：用户输入了 "abc" 要转成数字
//        ⇒ 用返回值表达：`int.TryParse(s, out int v)` 返回 bool（第 05 课）
//
//     ② **不正常的、继续往下跑没意义的**：数组下标越界、除数为 0、
//        文件被别人删了、网络断了
//        ⇒ 用**异常**：throw 出去，谁处理谁 catch
//
//   ⚠ 判断标准不是"严重不严重"，而是：
//     **"调用方有没有可能合理地处理它？"**
//     转不了的数字 ⇒ 有（提示用户重输）⇒ 用返回值
//     下标越界 ⇒ 没有（代码写错了）⇒ 抛
//
//   异常还有一个隐藏的好处：**它让"正常路径"保持干净。**
//
//       // 不用异常：每一步都要检查，主逻辑被淹没
//       int a = 读(...);   if (a < 0) return -1;
//       int b = 读(...);   if (b < 0) return -1;
//       int c = 算(a, b);  if (c < 0) return -1;
//
//       // 用异常：正常路径一目了然，"出错"只在发生的地方说一次
//       int a = 读(...);
//       int b = 读(...);
//       return 算(a, b);
//
//
// ══ 一、try / catch / finally 三段各管什么 ══
//
//       try
//       {
//           // 可能出事的代码
//       }
//       catch (特定的异常类型 ex)
//       {
//           // 处理它。⚠ 只有类型**匹配**才进来
//       }
//       catch (Exception ex)
//       {
//           // 兜底。⚠ 必须放在**具体类型之后**
//       }
//       finally
//       {
//           // 无论如何都跑：正常结束、return、异常，都跑
//       }
//
//   ⚠⚠ catch 是**按顺序匹配**的，编译器还额外要求你把具体的写在前面：
//
//       catch (Exception ex) { }             // 放前面
//       catch (ArgumentException ex) { }     // ⇒ error CS0160:
//       // "上一个 catch 子句已经捕获此类型或超类型的所有异常"
//
//     为什么：`Exception` 是所有异常的祖先，第一个 catch 就把一切都吃掉了，
//           后面那个永远轮不到。编译器直接判它多余 —— 这是好事，
//     它省得你在运行时才发现"我的 catch 从来不执行"。
//
//   不写 catch 的 try/finally 是合法的，而且很有用：
//
//       try { ... }
//       finally { 清理(); }        // 没有 catch ⇒ 异常继续往上传，但清理一定会做
//
//
// ══ 二、`throw`、`throw ex`、`throw new`：三者的堆栈差很多 ══
//
//   堆栈跟踪（stack trace）就是"错误是怎么一层层传上来的"那张清单。
//   排查线上问题时，它比错误消息本身更重要。
//
//   ┌─ ① `throw;`   —— 原样再抛，**堆栈完整保留** ────────────┐
//
//       catch (Exception ex)
//       {
//           Console.WriteLine("记一笔日志：" + ex.Message);
//           throw;                 // ✅ 起始点是**最初出事的那一行**
//       }
//
//   └────────────────────────────────────────────────────────┘
//
//   ┌─ ② `throw ex;` —— 从**这一行**重新开始，原始堆栈被砍掉 ──┐
//
//       catch (Exception ex)
//       {
//           throw ex;              // ❌ 堆栈从这里开始
//       }
//
//     症状：日志里堆栈最上面是**你自己的 catch 那一行**，
//           再往上什么都没有 —— 你只知道"出过错"，不知道**在哪里出的**。
//     为什么：`throw ex` 是一次新的抛出，CLR 认为"起点就是这里"。
//           它和 `throw new Exception(...)` 在这一点上没有区别。
//     改法：**只写 `throw;`**，不要带那个 `ex`。
//
//   └────────────────────────────────────────────────────────┘
//
//   ③ `throw new XxxException("...", ex);` —— 想**换成自己的异常类型**时用。
//      把原异常当 `InnerException` 传进去，堆栈虽然重开，
//      但原始的错还在 `ex.InnerException` 里，不算丢。
//
//       catch (IOException ex)
//       {
//           throw new 我的异常("保存失败", ex);     // ✅ 换了类型，但没丢原因
//       }
//
//   ⚠ 三句口诀：
//       要原样上抛    ⇒ `throw;`
//       要换类型上抛  ⇒ `throw new 类型(msg, ex);`
//       `throw ex;`    ⇒ **永远不写**
//
//
// ══ 三、什么时候**不**该 catch ══
//
//   官方的话是："Don't catch an exception unless you can handle it."
//   翻成大白话：**你 catch 了，就得做点什么。**
//
//   什么算"做了点什么"？
//     · 恢复了（用默认值重试、换个路径）
//     · 转成用户能懂的话（"文件不存在，请先保存"）
//     · 加一条有用的日志，然后 `throw;` 继续上抛
//     · 转成别的异常类型上抛，带上 InnerException
//
//   什么不算？
//     · `catch (Exception) { }`                ← 空 catch，"吞异常"
//     · `catch (Exception ex) { }`             ← 变量声明了却不用，编译器还会警告
//     · `catch (Exception ex) { return null; }` ← 把错误伪装成"没有数据"
//
//   ⚠⚠ 空 catch 是**最坏的一种 bug 制造机**：
//     症状是"程序不崩，但结果不对/什么都没发生"，而且**日志里一个字都没有**。
//     你会花一整天去找一个本来一行日志就能指出的问题。
//     如果实在要忽略（极少见），至少写清楚为什么：
//         catch (Exception) { /* 这里确实可以忽略：删临时文件失败无所谓 */ }
//
//   ⚠ 还有一条：**别用异常做正常流程控制**。
//        try { return 表[键]; } catch (KeyNotFoundException) { return 0; }
//     能跑，但异常构造很贵，而且把"正常"和"异常"混成一件事了 ——
//     该用 `TryGetValue`（第 12 课）。
//
//
// ══ 四、`IDisposable` 与 `using` ══
//
//   有些东西**用完必须还**：文件句柄、网络连接、数据库连接、图形资源。
//   C# 用接口 `IDisposable` 表达这件事，它只有一个方法：
//
//       public interface IDisposable { void Dispose(); }
//
//   谁实现了它，谁就承诺"调用 Dispose() 会把我占的资源放掉"。
//
//   老写法（啰嗦，而且容易漏）：
//
//       var w = new StringWriter();
//       try { w.WriteLine("..."); }
//       finally { w.Dispose(); }        // 必须自己记得
//
//   新写法：`using` **声明**
//
//       using var w = new StringWriter();
//       w.WriteLine("...");
//       // 方法/作用域结束的时候，自动 Dispose() —— 出了异常也照样 Dispose
//
//   或者 `using` **语句**，能限定更小的范围：
//
//       using (var w = new StringWriter())
//       {
//           w.WriteLine("...");
//       }   // ← 出了这个花括号就 Dispose
//
//   ⚠ 两者的区别只是**作用域**：`using var` 管到所在块的末尾，
//     `using (...)` 管到那个花括号。功能上等价 —— 编译器都会把它
//     展开成 try/finally + Dispose。
//
//   ⚠⚠ `using` 和 `return` 能共存，而且顺序是确定的：
//
//       static int 带资源算(int 数)
//       {
//           using var w = new StringWriter();
//           w.WriteLine("干活");
//           return 数 * 2;          // ← 先算好返回值，**再 Dispose**，最后才真的返回
//       }
//
//     不用担心"return 会不会跳过 Dispose" —— **不会**。
//     这正是 finally 的语义。这条你要是自己想明白了，这一课就通了。
//
//   ⚠ `using` 只对**实现了 IDisposable** 的类型有效。
//     对一个没实现的类型写 `using var x = new 我的类();` 报
//     `error CS1674: "我的类": using 语句中使用的类型必须可隐式转换为
//      "System.IDisposable"`。
//
//
// ══ 五、常见坑 ══
//
//   坑 1：`catch (Exception) { }` 吞掉一切
//
//       try { 危险操作(); } catch (Exception) { }
//       正常继续();
//
//     症状：程序**不崩**，但后面拿到的数据是错的 / 该保存的没保存；
//           更糟的是**没有任何日志**，你不知道刚才发生过什么。
//     为什么：异常的全部信息（类型 + 消息 + 堆栈）都被扔了。
//     改法：要么真的处理，要么 `catch (Exception ex) { 记日志(ex); throw; }`。
//
//   坑 2：`throw ex;` 而不是 `throw;`
//
//       catch (Exception ex) { throw ex; }
//
//     症状：日志里的堆栈最上面是你的 catch 那一行，源头不见了。
//     为什么：`throw ex;` 是一次**新的抛出**，起点就是这一行。
//     改法：`throw;`。就少打四个字符，但堆栈是天壤之别。
//
//   坑 3：catch 顺序写反
//
//       catch (Exception ex) { ... }
//       catch (ArgumentException ex) { ... }     // ❌
//
//     症状：`error CS0160: 上一个 catch 子句已经捕获此类型或超类型的所有异常`。
//     为什么：Exception 是 ArgumentException 的祖先，第一个就吃完了。
//     改法：**具体的写前面，笼统的写后面**。
//
//   坑 4：以为 C# 有 Java 那样的"受检异常"
//
//       // Java 里必须写 throws IOException，C# 里**不需要**
//       public static int 读文件(string 路径) { ... }
//
//     症状：看到别人的签名里没有异常声明，以为"这个方法不会抛异常"。
//     为什么：C# **所有异常都是非受检的**，签名里不体现，
//           编译器也不强制你 catch。能不能抛，只能看文档 / 看实现。
//     改法：把这当成"要自己操心的事" —— 调别人的方法时，
//           想一想"它可能抛什么"。
//
//   坑 5：用异常做正常的流程判断
//
//       static int 取(Dictionary<string,int> 表, string 键)
//       {
//           try { return 表[键]; }
//           catch (KeyNotFoundException) { return 0; }        // ❌
//       }
//
//     症状：能跑，但"键不存在"和"值真的是 0"混成同一个结果，无法区分；
//           而且这一行的开销比正常查表大得多。
//     为什么：异常构造要抓堆栈，是重活；而且语义上"没查到"根本不是异常。
//     改法：`表.TryGetValue(键, out int v)` + 返回 bool（第 12 课的写法）。
//
//   坑 6：自己造的异常没有可读的消息
//
//       throw new Exception("错了");
//
//     症状：线上日志里一堆"错了"，谁也判断不出发生了什么。
//     为什么：消息是**给人看的唯一线索**，得写"哪个值、什么范围、期望什么"。
//     改法：`throw new ArgumentException($"分数 {分} 超出 0~100 的范围");`
//           而且尽量用**标准异常类型**（ArgumentException /
//           ArgumentOutOfRangeException / InvalidOperationException /
//           NotSupportedException），只有这样调用方才能可靠地 catch。
//           自定义异常留给"调用方需要单独处理"的业务错误。
//
//
// ══ 轮到你了 ══
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
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 13
""";

    public static string 参考答案 => """
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

    [检查说明("算平均 用了浮点除法（不是整数除法）")]
    public static CheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("(double)"))
            return CheckResult.不过("没找到「(double)」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("带资源算 用了 using")]
    public static CheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("using"))
            return CheckResult.不过("没找到「using」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("查成绩：越界真的抛异常，合法下标取得到")]
    public static CheckResult 检查_5(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 查成绩(new int[]{10,20,30}, 1));
    printf("%d\n", 查成绩(new int[]{10,20,30}, 2));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
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
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 带资源算(21));
    printf("%d\n", 带资源算(0));
    printf("%d\n", 带资源算(-5));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
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
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 算平均(new int[]{1,2}));
    printf("%d\n", 算平均(new int[]{90,80,70}));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "1.5",
            "80"
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
