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
// 先介绍：程序出错时，"谁来收拾"
//
// 先看这段：
//
//     int[] 数们 = {10, 20, 30};
//     System.out.println(数们[9]);
//
// 跑起来是这样：
//
//     Exception in thread "main" java.lang.ArrayIndexOutOfBoundsException:
//         Index 9 out of bounds for length 3
//             at Main.main(Main.java:7)
//     （程序到此为止，后面一行都不执行了）
//
// 注意最后那句：**后面的代码一行都没跑**。程序**整个停在那**。
//
// 这在学习的时候没什么 —— 你看到报错，改一改再跑。
// 但在真正给人用的程序里，这是灾难：
//   一个输入框里少打了一个逗号 → 整个程序白屏退出，用户刚才填的半小时全没了。
//   一个文件不存在 → 批处理跑到一半死掉，前面处理过的数据状态不明。
//
// 而且很多时候**出错是正常的、可以预料的**：
//   用户输入的东西本来就可能是错的（"请输入年龄"→ 他打的是"abc"）
//   要读的文件本来就可能不在（U 盘拔了）
//   除法的分母本来就可能是 0（用户填的）
// 这些不是"你的程序有 bug"，是**世界的常态**。
//
// 异常机制就是让程序能说这句话：
// **"这一步可能会出事，出事了我知道该怎么办，别整个死掉。"**
//
//     try {
//         可能会出事的代码
//     } catch (Exception e) {
//         出事了就在这里收拾
//     } finally {
//         不管出没出事都要做的事
//     }
//
// 三个块的分工，用一句话记住：
//     try     —— 我试试
//     catch   —— 出事了，我来处理（**程序从这里继续往下跑**）
//     finally —— 不管怎样都执行（收尾：关文件、断连接）
//
// ─────────────────────────────────────────────────────
//  用法与例子：三个块各就各位
// ─────────────────────────────────────────────────────
//
//     int[] 数们 = {10, 20, 30};
//
//     try {
//         System.out.println(数们[0]);        ← 正常
//         System.out.println(数们[1]);        ← 正常
//         System.out.println(数们[9]);        ← 出事了！从这里跳出去
//         System.out.println("这行不会执行");  ← 被跳过了
//     } catch (Exception e) {
//         System.out.println("出事:" + e.getMessage());
//     } finally {
//         System.out.println("这行总会执行");
//     }
//     System.out.println("程序继续往下跑");    ← 这行**会**执行
//
// 关键的一条（很多人第一次都会想错）：
// **try 块里出错之后，剩下的语句全部跳过，直接跳到 catch。**
// 它不是"跳过这一行、继续下一行"，是**整套跳出去**。
//
// ① 异常对象 e 里有什么
//
//     e.getMessage()      → 出事的说明文字，比如 "Index 9 out of bounds for length 3"
//     e.printStackTrace() → 把那一大串"at Main.main(Main.java:7)"打到屏幕上
//
// 平时给人看的界面里用 getMessage()（短、能懂），
// 自己调试的时候用 printStackTrace()（信息全）。
//
// ② 常见的内置异常，认名字就知道是什么事
//
//     ArrayIndexOutOfBoundsException   数组下标越界     数们[9]
//     NullPointerException             对 null 调方法    甲.取名字() 而 甲 是 null
//     ArithmeticException              算术错误         10 / 0
//     NumberFormatException            字符串转数字失败  Integer.parseInt("abc")
//     IllegalArgumentException         参数不合法       你自己抛的
//     ClassCastException               转型转错了       (圆) 一个长方形
//
//   注意 ArithmeticException 那条：**整数除法除以 0** 会抛异常。
//   （小数不一样：10.0 / 0 得到的是 Infinity，不抛异常。别记混。）
//
// ③ 自己抛异常：throw new ...
//
//   有时候"出错"不是 Java 报的，而是**你**觉得不合理：
//
//     public static int 取元素(int[] 数们, int 下标) {
//         if (下标 < 0) {
//             throw new IllegalArgumentException("下标不能是负数");
//         }
//         return 数们[下标];
//     }
//
//   念法是："下标是负数这件事，我认为它不合法 —— 我不打算硬着头皮往下算，
//   我要**当场把问题扔出去**，让调用我的人去处理。"
//
//   ⚠ throw 和 throws 长得像，意思不一样，别混：
//       throw  new IllegalArgumentException(...);   ← 动作："扔一个"
//       throws IllegalArgumentException              ← 声明："我这个方法可能会扔"
//
// ④ 受查异常：编译器盯着你必须处理的那种
//
//   Java 的异常分两大家族：
//
//     运行时异常（RuntimeException 及其子类）   编译器**不管**
//         ArrayIndexOutOfBounds / NullPointer / Arithmetic / IllegalArgument ...
//
//     受查异常（其他 Exception）                编译器**强制**你处理
//         最典型的就是文件操作：new FileReader("a.txt") 会扔 IOException
//         你不处理就**编译不过**，报：
//             错误: 未报告的异常错误IOException; 必须对其进行捕获或声明以便抛出
//
//   两种"处理"方式，二选一：
//         try { ... } catch (IOException e) { ... }      ← 当场接住
//         public void 读文件() throws IOException { ... } ← 往上甩给调用者
//
//   这一课不碰文件，所以练习里都是运行时异常（编译器不逼你）。
//   但**看到"未报告的异常错误"这句话，你就知道碰上受查异常了**。
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：catch 里什么都不做（"吞掉"异常）—— 最坏的一种写法
//
//     try {
//         某件重要的事();
//     } catch (Exception e) {
//     }                          ← 空的花括号
//
// 你会看到：程序"跑得好好的"，一点报错都没有 —— 然后你发现某件重要的事**根本没做成**。
// 为什么：异常的全部价值就是"告诉你出事了"。空 catch 等于把报警器拆了。
// 这是最难查的一类 bug，因为你连"哪里出事了"都不知道。
// 至少也要打印一句：
//     catch (Exception e) { System.out.println("出事了:" + e.getMessage()); }
// ⚠ 而且 catch (Exception e) 本身就已经很宽了 —— 它会把
//   NullPointer、ClassCast 这些**代码 bug** 一起吞掉。
//   写具体一点（catch (ArrayIndexOutOfBoundsException e)）能少吞很多东西。
//
// ⚠ 坑 2：把父类异常写在子类前面
//
//     } catch (Exception e) {                        ← 太宽，什么都接住了
//         System.out.println("一般错误");
//     } catch (ArrayIndexOutOfBoundsException e) {   ← 永远轮不到这里
//         System.out.println("下标越界");
//     }
//
// 你会看到：明明越界了，打印出来的却是"一般错误"。
// 为什么：catch 是**从上往下**试的，第一个能接住的就接走了。
// Exception 是所有异常的父类，它能接住一切 —— 后面那些具体的就没机会了。
// ⚠ 有的情况下编译器直接报错：错误: 已捕获到异常错误...（在更通用的之后被捕获）
//   看到那条别慌，就是顺序问题：**从最具体的往最笼统的写。**
//
// ⚠ 坑 3：在 finally 里 return
//
//     public static int 算() {
//         try {
//             return 1;
//         } finally {
//             return 2;          ← 把上面那个 1 覆盖掉了
//         }
//     }
//     System.out.println(算());       ⇒ 2      ← 不是 1
//
// 你会看到：try 里明明 return 1，方法却返回了 2，而且**编译器不报错**。
// 为什么：finally 的语义是"无论如何都要执行"，它连 return 前面那一瞬间都能插进去，
// 于是它里面的 return 直接把外层的返回值换掉了。
// **规矩：finally 里不要写 return**（也别写 break/continue）——
// 它只应该用来收尾：关文件、关连接、删临时文件。
//
// ⚠ 坑 4：以为"出了异常的循环会整个停下"
//
//     for (int i = 0; i < 数们.length; i++) {
//         try {
//             System.out.println(数们[i]);
//         } catch (Exception e) {
//             System.out.println("这个位置没有");
//         }
//     }
//
// 你会看到：循环**继续**跑完了（挨个打印"这个位置没有"）。
// 为什么：try/catch 在**循环体里面**。每次出事都被当场接住，
// 接住以后继续下一轮 —— 循环控制完全没有被打断。
// 反过来说：如果 try 在循环**外面**，第一次出事就会跳出整个循环。
// **try 放在哪一层，决定了"出事以后从哪儿继续"。** 这个位置是你写的，要自己想清楚。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   main 里这么写（顺序就是检查比对的顺序）：
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
// 期望输出（五行）：
//     10
//     20
//     第三个也拿到了
//     出事:Index 9 out of bounds for length 3
//     这行总会执行
//
// ⚠ 三件要想清楚的事：
//   ① "第三个也拿到了" 打印了，说明前三行都正常跑完了；
//   ② 出事那行之后、catch 之前，try 里**剩下的语句全被跳过** ——
//      所以 除(10, 0) 从来没被执行；
//   ③ finally 那行在 catch 之后照样打印。
//
// 取元素 里那句 throw 单独说：负数下标的检查在练习里不会被触发
// （main 没传负数），但**它必须写** —— 检查会看代码里有没有这句话。
// 这就是"自己抛异常"的样子：不是等 Java 报错，而是你主动拒绝不合理的输入。

// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/java-lessons 检查 15
//
//   看这一课要干什么：dotnet run --project csharp/java-lessons 15
//   看答案（先自己写一遍）：dotnet run --project csharp/java-lessons 答案 15
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/java-lessons 检查 
15
//
//   看这一课要干什么：dotnet run --project csharp/java-lessons 
15
//   看答案（先自己写一遍）：dotnet run --project csharp/java-lessons 答案 
15
//
//   
⚠ 检查会逐行比对那五行的**顺序和内容** —— 尤其「出事:Index 9 out of bounds for length 3」那一行：它证明 try 里出错之后，剩下的语句全被跳过了。
//
//   检查会把你的代码**真的编译、真的跑一遍**再逐条比对 —— 所以它红了就一定有原因，
//   照着那段说明改就行：它一般会直接点出你写错在哪、以及正确的写法长什么样。
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
