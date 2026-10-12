using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("13", "第 13 课：多态与抽象类 —— 一个变量，多种形状",
    """
1. 写抽象类 形状：public abstract double 面积();、public String 说明() 返回 "面积=" + Math.round(面积())
2. 写 长方形 extends 形状（宽、高，面积 = 宽*高）；写 圆 extends 形状（半径，面积 = Math.PI * 半径 * 半径）
3. main 里：直接打印两个对象的说明()；再用 形状 类型的变量装一个 圆 打印说明()；最后放进 形状[] 数组用增强 for 逐个打印
""")]
public static class 第13课
{
    public static string 模板 => """
// 第 13 课：多态与抽象类 —— 一个变量，多种形状
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**父类类型的变量，装子类对象，调用时走子类的方法**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    多态与抽象类：一个变量，多种形状
//
//  需要先会
//    第 12 课：继承与覆写
//
// ─────────────────────────────────────────────────────
//  先介绍：为什么要这样
// ─────────────────────────────────────────────────────
//
// 一个数组里想同时装长方形和圆，怎么写？两个类型不一样。
// 让它们都是「形状」的子类，数组就写成 `形状[]`。
// 遍历的时候**循环体里一句 if 都不用写** —— 每个对象自己知道该算哪个面积。
//
// ─────────────────────────────────────────────────────
//  一、抽象类：只声明，不实现
// ─────────────────────────────────────────────────────
//
//     abstract class 形状 {
//         public abstract double 面积();          ← 没有函数体，只有分号
//
//         public String 说明() {                 ← 有实现的方法，子类直接白拿
//             return "面积=" + Math.round(面积());
//         }
//     }
//
// ⚠ 两个后果：
//
//     abstract 类**不能 new** —— `new 形状()` 编译不过（它还不完整）
//     子类**必须**实现所有 abstract 方法，否则子类也得标 abstract
//
// 这一课所有类都放在同一个文件里时，**只有 Main 能是 public**（第 00 课的规矩）。
//
// ─────────────────────────────────────────────────────
//  二、多态：一个变量，多种形状
// ─────────────────────────────────────────────────────
//
//     形状 一个形状 = new 圆(2);        ← 左边是父类类型，右边是子类对象
//     一个形状.说明();                 ← 走的是**圆**的 面积()
//
// 这叫**向上转型**。判据：**调用哪个方法，看 new 的是谁**（运行时决定）。
//
// 为什么数组能用它：
//
//     形状[] 图形们 = { new 长方形(4, 5), new 圆(2) };
//     for (形状 图 : 图形们) { System.out.println(图.说明()); }
//
//   循环体里**没有 if** —— 加一种新形状不用改这段循环。这就是它的价值。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：抽象类不能 new
// ─────────────────────────────────────────────────────
//
//     形状 图 = new 形状();
//
// 你会看到（javac 原文）：
//
//     错误: 形状是抽象的; 无法实例化
//
// 为什么：抽象方法**没有函数体**，这个对象真被造出来，调用 面积() 时无处可去。
//   ⇒ 编译器干脆不让你造。
//
// ⚠ 反过来也常见：**子类忘了实现抽象方法**，那时的报错是
//
//     错误: 长方形不是抽象的, 并且未覆盖形状中的抽象方法面积()
//
//   ⇒ 两条报错说的是同一件事的两面，看到哪条都知道该去补什么。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   接口（比抽象类更彻底的「只声明」）→ 第 14 课。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  main 里四步，按顺序：
//
//     长方形 桌 = new 长方形(4, 5);
//     圆 盘 = new 圆(2);
//
//     // ① 直接调用：走的是各自的 面积()
//     System.out.println(桌.说明());          → 面积=20
//     System.out.println(盘.说明());          → 面积=13
//
//     // ② 向上转型
//     形状 一个形状 = new 圆(2);
//     System.out.println(一个形状.说明());     → 面积=13
//
//     // ③ 数组 + 增强 for：循环体里不要写 if
//     形状[] 图形们 = new 形状[2];
//     图形们[0] = new 长方形(4, 5);
//     图形们[1] = new 圆(2);
//     for (形状 图 : 图形们) {
//         System.out.println(图.说明());
//     }
//
//  期望五行输出：
//     面积=20
//     面积=13
//     面积=13
//     面积=20
//     面积=13
//
//  ⚠ 说明() 里要写 Math.round(面积())，不要自己写死数字；
//    圆 的 面积() 要写 Math.PI，不要用 3.14（那是错的算法，检查会看 Math.PI）。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """
// 第 13 课参考答案：多态与抽象类
//
// ⚠ public class Main 必须是文件里第一个"非注释、非空行"的语句。
//    形状 / 长方形 / 圆 全都放在 Main 后面，都不写 public。

public class Main {
    public static void main(String[] args) {
        长方形 桌 = new 长方形(4, 5);
        圆 盘 = new 圆(2);

        // ① 直接调用：各自走各自的 面积()
        System.out.println(桌.说明());            // 面积=20
        System.out.println(盘.说明());            // 面积=13（3.14159*4 ≈ 12.57）

        // ② 向上转型：变量类型是父类，对象是子类 —— 合法，因为"圆是形状"
        形状 一个形状 = new 圆(2);
        System.out.println(一个形状.说明());       // 面积=13，跑的是 圆 的 面积()
        // ⚠ 这里不能写 一个形状.半径 —— 编译器只看左边（形状 里没有 半径）

        // ③ 数组 + 增强 for：循环体里不需要判断"这是圆还是长方形"
        形状[] 图形们 = new 形状[2];              // 元素类型是父类
        图形们[0] = new 长方形(4, 5);
        图形们[1] = new 圆(2);
        for (形状 图 : 图形们) {
            System.out.println(图.说明());         // 动态绑定：各自找对自己的 面积()
        }
    }
}

// ── 下面是我写的类 ──

// abstract：这个类不完整，不能 new，只能当类型用
abstract class 形状 {
    // 抽象方法：只有签名和分号，**没有花括号**
    // 意思是"凡是形状都必须能算面积，但怎么算由子类说"
    public abstract double 面积();

    // 具体方法：有方法体。里面调 面积()，谁调 说明 就用谁的 面积
    public String 说明() {
        // ⚠ 一定要写 面积()，不能写某个具体数字
        return "面积=" + Math.round(面积());
    }
}

class 长方形 extends 形状 {
    private double 宽;
    private double 高;

    public 长方形(double 宽, double 高) {
        this.宽 = 宽;
        this.高 = 高;
    }

    @Override
    public double 面积() {
        return 宽 * 高;
    }
}

class 圆 extends 形状 {
    private double 半径;

    public 圆(double 半径) {
        this.半径 = 半径;
    }

    @Override
    public double 面积() {
        return Math.PI * 半径 * 半径;      // ⚠ 用 Math.PI，不要用 3.14
    }
}
""";


    [Java检查说明("五行的面积都对（含数组里循环出来的两行）")]
    public static JavaCheckResult 检查_1(string 代码)
    {
        var run = JavaCheck.直接运行(代码);
        if (!run.编译通过) return JavaCheck.编译失败说明(run);
        string[] 期望 = {
            "面积=20",
            "面积=13",
            "面积=13",
            "面积=20",
            "面积=13",
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

    [Java检查说明("形状声明成了抽象类")]
    public static JavaCheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("abstract class 形状"))
            return JavaCheckResult.不过("没找到「abstract class 形状」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("面积是抽象方法（签名 + 分号，没有方法体）")]
    public static JavaCheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("public abstract double 面积();"))
            return JavaCheckResult.不过("没找到「public abstract double 面积();」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("圆用 Math.PI 算面积")]
    public static JavaCheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("Math.PI"))
            return JavaCheckResult.不过("没找到「Math.PI」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("两个子类都写了 @Override")]
    public static JavaCheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("@Override"))
            return JavaCheckResult.不过("没找到「@Override」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("说明() 里调用的是 面积()，不是写死的数字")]
    public static JavaCheckResult 检查_6(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("Math.round(面积())"))
            return JavaCheckResult.不过("没找到「Math.round(面积())」—— 这一课要求用到它。");
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
