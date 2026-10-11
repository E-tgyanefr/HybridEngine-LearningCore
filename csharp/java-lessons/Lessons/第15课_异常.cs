using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("15", "第 15 课：异常处理 —— 出错了怎么办",
    """
1. 写两个静态方法：取元素(int[] 数们, int 下标)（下标为负就 throw new IllegalArgumentException）；除(int 甲, int 乙)
2. main 里用 try/catch/finally 包住五件事：打印 取元素(数们,0)、取元素(数们,1)、"第三个也拿到了"、取元素(数们,9)、除(10,0)
3. catch 里打印 "出事:" + e.getMessage()；finally 里打印 "这行总会执行"
4. main 里要有 int[] 数们 = {10, 20, 30};
""")]
public static class 第15课
{
    public static string 模板 => """
// 第 15 课：异常处理 —— 出错了怎么办
//
// 这一课讲一件事：**出错了不要让它把程序打死，你要能接住它**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    异常处理：try / catch / finally，以及自己抛异常
//
//  需要先会
//    第 10 课：构造方法（自定义异常要用它）
//
// ─────────────────────────────────────────────────────
//  先介绍：不接住会怎样
// ─────────────────────────────────────────────────────
//
// 访问一个越界的下标，Java 会**抛异常**。没人接住的话：
//
//     程序当场停下，后面的代码一行都不执行。
//
// 而「用户输入了个奇怪的数」这种事，在真实程序里天天发生 ——
// 你希望的是「告诉他这不对，然后继续」，不是整个程序崩掉。
//
// ─────────────────────────────────────────────────────
//  一、try / catch / finally 的形状
// ─────────────────────────────────────────────────────
//
//     try {
//         // 可能出事的代码
//     } catch (Exception e) {
//         // 出事之后来这里
//     } finally {
//         // **不管出没出事都会执行**
//     }
//
// ⚠ 三条要记牢的规则：
//
//     ① try 里出事那一行之后的语句**全部跳过**，直接跳到 catch
//     ② catch 收的是**类型匹配**的异常（catch (Exception e) 能收住几乎所有）
//     ③ finally 一定会执行 —— 哪怕 catch 里又 return 了
//
// ─────────────────────────────────────────────────────
//  二、e.getMessage()：异常自己带一句说明
// ─────────────────────────────────────────────────────
//
//     catch (Exception e) {
//         System.out.println("出事:" + e.getMessage());
//     }
//
// 越界异常的 getMessage() 是 `Index 9 out of bounds for length 3` ——
// 它把「哪里错、错在哪」都说清楚了。
//
// ─────────────────────────────────────────────────────
//  三、自己抛异常：throw
// ─────────────────────────────────────────────────────
//
//     public static int 取元素(int[] 数们, int 下标) {
//         if (下标 < 0) {
//             throw new IllegalArgumentException("下标不能是负数");
//         }
//         return 数们[下标];
//     }
//
// ⚠ 这是「主动拒绝不合理输入」的写法 —— 不是等 Java 报错，是你先拦。
//   Java 自己会查**上界**（越界抛 ArrayIndexOutOfBoundsException），
//   但**负数下标也要拦** —— 所以这一句必须写。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：把 try 的边界划错了
// ─────────────────────────────────────────────────────
//
//     System.out.println(取元素(数们, 0));
//     try {
//         System.out.println(取元素(数们, 9));
//     } catch (Exception e) { }
//     System.out.println(取元素(数们, 1));
//
// 你是想「接住出错的那一句，然后继续」。
//
// 但常见的错法是**把整段都包进 try**：
//
//     try {
//         System.out.println(取元素(数们, 0));
//         System.out.println(取元素(数们, 9));   ← 出事
//         System.out.println(取元素(数们, 1));   ← **被跳过，永远不执行**
//     } catch (Exception e) { }
//
// 你会看到：没有报错，只是**少打印了几行**。
//
// 为什么：try 里出事那一行之后的语句全部跳过 —— 这是规则，不是 bug。
//   ⇒ 所以这一课的期望输出里，`除(10, 0)` **从来没被执行过**。
//     写的时候要想清楚：**我到底想让哪几句被跳过？**
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   自定义异常类、throws 声明 → 这门课不涉及。
//   资源关闭（try-with-resources）→ 也在这里，但那是另一个语法。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  main 里这么写（顺序就是检查比对的顺序）：
//
//     int[] 数们 = {10, 20, 30};
//
//     try {
//         System.out.println(取元素(数们, 0));      → 10
//         System.out.println(取元素(数们, 1));      → 20
//         System.out.println("第三个也拿到了");      → 第三个也拿到了
//         System.out.println(取元素(数们, 9));      → 这里出事！9 越界
//         System.out.println(除(10, 0));           → 这一行永远到不了
//     } catch (Exception e) {
//         System.out.println("出事:" + e.getMessage());
//     } finally {
//         System.out.println("这行总会执行");
//     }
//
//  期望输出（五行）：
//     10
//     20
//     第三个也拿到了
//     出事:Index 9 out of bounds for length 3
//     这行总会执行
//
//  ⚠ 三件要想清楚的事：
//   ① 「第三个也拿到了」打印了，说明前三行都正常跑完了；
//   ② 出事那行之后、catch 之前，try 里**剩下的语句全被跳过** ——
//      所以 除(10, 0) 从来没被执行；
//   ③ finally 那行在 catch 之后照样打印。
//
//  取元素 里那句 throw 单独说：负数下标的检查在练习里不会被触发
//  （main 没传负数），但**它必须写** —— 检查会看代码里有没有这句话。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """
// 第 15 课参考答案：异常处理
//
// ⚠ public class Main 必须是文件里第一个"非注释、非空行"的语句。
//    这一课所有方法都写在 Main 里面，没有别的类 —— 最省事，也最不容易出错。

public class Main {
    public static void main(String[] args) {
        int[] 数们 = {10, 20, 30};

        try {
            // 前三行都正常
            System.out.println(取元素(数们, 0));       // 10
            System.out.println(取元素(数们, 1));       // 20
            System.out.println("第三个也拿到了");

            // ⚠ 这一行会抛 ArrayIndexOutOfBoundsException：
            //   try 块里**剩下的语句全部跳过**，直接跳到 catch
            System.out.println(取元素(数们, 9));
            System.out.println(除(10, 0));             // 永远执行不到
        } catch (Exception e) {
            // e.getMessage() 是 JDK 给的说明文字：Index 9 out of bounds for length 3
            System.out.println("出事:" + e.getMessage());
        } finally {
            // 不管上面出没出事，这里都会执行（收尾的地方）
            System.out.println("这行总会执行");
        }
    }

    // 自己抛异常：负数下标我认为不合法，当场把问题扔出去
    public static int 取元素(int[] 数们, int 下标) {
        if (下标 < 0) {
            // throw = "扔一个异常出去"，让调用者去处理
            // 注意：throw（动作）和 throws（声明）是两件事
            throw new IllegalArgumentException("下标不能是负数");
        }
        return 数们[下标];              // 下标太大 → JDK 自己抛越界异常
    }

    // 整数除零会抛 ArithmeticException（小数除零不会，那是 Infinity）
    public static int 除(int 甲, int 乙) {
        return 甲 / 乙;
    }
}
""";


    [Java检查说明("五行的顺序和内容都对（越界后 try 里剩下的被跳过）")]
    public static JavaCheckResult 检查_1(string 代码)
    {
        var run = JavaCheck.直接运行(代码);
        if (!run.编译通过) return JavaCheck.编译失败说明(run);
        string[] 期望 = {
            "10",
            "20",
            "第三个也拿到了",
            "出事:Index 9 out of bounds for length 3",
            "这行总会执行",
        };
        var 实际 = run.行;
        if (实际.Count != 期望.Length)
            return JavaCheckResult.不过($"应该有 {期望.Length} 行输出，实际 {实际.Count} 行。" +
                "\n      打印出来的：\n        " + string.Join("\n        ", 实际));
        for (int i = 0; i < 期望.Length; i++)
            if (实际[i].Trim() != 期望[i])
                return JavaCheckResult.不过(
                    $"第 {i + 1} 行应当是「{期望[i]}」，实际是「{实际[i].Trim()}」。");
        return JavaCheckResult.过($"{期望.Length} 行输出全对");
    }

    [Java检查说明("用了 try / catch / finally 三个块")]
    public static JavaCheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("finally"))
            return JavaCheckResult.不过("没找到「finally」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("catch 里打印了异常的说明文字（不是空的 catch）")]
    public static JavaCheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("e.getMessage()"))
            return JavaCheckResult.不过("没找到「e.getMessage()」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("自己抛了异常（负数下标的检查）")]
    public static JavaCheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("throw new IllegalArgumentException"))
            return JavaCheckResult.不过("没找到「throw new IllegalArgumentException」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("两个方法都声明成了 public static")]
    public static JavaCheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("public static int 取元素(int[] 数们, int 下标)"))
            return JavaCheckResult.不过("没找到「public static int 取元素(int[] 数们, int 下标)」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("数组用的是 length（不是 length() 方法）")]
    public static JavaCheckResult 检查_6(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains(".length()"))
            return JavaCheckResult.不过("出现了不该有的写法：.length()");
        return JavaCheckResult.过();
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
