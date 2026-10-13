using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("14", "第 14 课：接口 —— 契约，不是血缘",
    """
1. 写 interface 形状：double 面积(); 和 default String 说明() 返回 "面积=" + Math.round(面积())
2. 写 interface 可命名：String 名字();
3. 写 圆 implements 形状, 可命名：半径、面积() 用 Math.PI、名字() 返回 "半径为" + 半径 + "的圆"
4. main 里：用 圆 自己的类型调面积和说明；用 形状 类型的变量调说明；用 可命名 类型的变量调名字；打印两个视角的变量是否 ==
""")]
public static class 第14课
{
    public static string 模板 => """
// 第 14 课：接口 —— 契约，不是血缘
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**不靠继承，也能约定「你必须会这几件事」**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    接口：契约，不是血缘
//
//  需要先会
//    第 13 课：抽象类
//
// ─────────────────────────────────────────────────────
//  先介绍：接口和抽象类的区别
// ─────────────────────────────────────────────────────
//
//     抽象类   是**血缘**关系：「圆 **是一种** 形状」
//     接口     是**契约**关系：「圆 **会** 被命名」
//
// 关键差别：**一个类只能继承一个父类，但可以实现很多接口**。
// 圆是一种形状，同时它也会被命名、也会被比较 —— 血缘只有一条，契约可以很多条。
//
// ─────────────────────────────────────────────────────
//  一、interface 怎么写、怎么用
// ─────────────────────────────────────────────────────
//
//     interface 可命名 {
//         String 名字();                ← 只写签名，没有函数体
//     }
//
//     interface 形状 {
//         double 面积();
//         String 说明();
//     }
//
//     class 圆 implements 形状, 可命名 {      ← 可以同时实现好几个（用逗号）
//         private final double 半径;
//
//         public 圆(double 半径) { this.半径 = 半径; }
//
//         @Override public double 面积() { return Math.PI * 半径 * 半径; }
//         @Override public String 说明() { return "面积=" + Math.round(面积()); }
//         @Override public String 名字() { return "半径为" + 半径 + "的圆"; }
//     }
//
// ─────────────────────────────────────────────────────
//  二、换个视角看同一个对象
// ─────────────────────────────────────────────────────
//
//     圆 盘 = new 圆(2);
//     形状 当形状看 = 盘;        ← 只看得到 面积() / 说明()
//     可命名 当名字看 = 盘;      ← 只看得到 名字()
//
// ⚠ 换了视角 ≠ 换了对象。两个变量里装的是**同一个地址**：
//
//     当形状看 == 当名字看        ⇒ true
//
//   这一点第 08 课讲过：Java 的类类型变量装的是引用。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：implements 了却没实现全部方法
// ─────────────────────────────────────────────────────
//
//     class 圆 implements 形状, 可命名 {
//         @Override public double 面积() { ... }
//         // 忘了 说明() 和 名字()
//     }
//
// 你会看到（javac 原文）：
//
//     错误: 圆不是抽象的, 并且未覆盖形状中的抽象方法说明()
//
// 为什么：接口就是契约 —— 签了就得全做到，缺一条这个类就是「不完整的」，
//   那不完整的类只能标 abstract（那就不能 new 了）。
//
// ⇒ 两条出路：把缺的方法补上（正常做法），或者把类标成 abstract。
//   ⚠ 报错里那句「不是抽象的」是在说：**你既没实现，又没标 abstract**。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   接口的默认方法（default 方法）→ 这门课不涉及。
//   泛型里的接口（Comparable 等）→ 第 16 课沾一点。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  main 里五步，按顺序：
//
//     圆 盘 = new 圆(2);
//
//     // ① 用 圆 自己的类型调
//     System.out.println(盘.面积());              → 12.566370614359172
//     System.out.println(盘.说明());              → 面积=13
//
//     // ② 换个视角：把同一个对象看成「形状」
//     形状 当形状看 = 盘;
//     System.out.println(当形状看.说明());         → 面积=13
//
//     // ③ 再换个视角：看成「可命名」
//     可命名 当名字看 = 盘;
//     System.out.println(当名字看.名字());         → 半径为2.0的圆
//
//     // ④ 同一个对象，两个不同视角的变量指着它
//     System.out.println(当形状看 == 当名字看);    → true
//
//  ⚠ 第一行那个长小数不用自己算 —— 直接 println(盘.面积()) 打出来的就是它。
//    （Java 打印 double 只打有效位，不会像 C 的 %f 那样补一堆 0。）
//  ⚠ ④ 那行打印的是 true —— 想清楚为什么。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """
// 第 14 课参考答案：接口
//
// ⚠ public class Main 必须是文件里第一个"非注释、非空行"的语句。
//    形状 / 可命名 / 圆 都放在 Main 后面。
//    接口也不写 public（一个文件只能有一个 public 类型，位置留给 Main）。

public class Main {
    public static void main(String[] args) {
        圆 盘 = new 圆(2);

        // ① 用 圆 自己的类型看：接口里的东西和实现的方法都在
        System.out.println(盘.面积());               // 12.566370614359172
        System.out.println(盘.说明());               // 面积=13（default 里的 Math.round）

        // ② 换个视角，只把它看成"形状"这个契约
        形状 当形状看 = 盘;                          // 向上转型，和上一课是一回事
        System.out.println(当形状看.说明());          // 面积=13
        // ⚠ 这里不能写 当形状看.名字() —— 形状 这个接口里没有 名字

        // ③ 再换一个视角：只把它看成"可命名"
        可命名 当名字看 = 盘;
        System.out.println(当名字看.名字());          // 半径为2.0的圆

        // ④ 换个视角 ≠ 换个对象：两个变量里装的是同一个地址
        System.out.println(当形状看 == 当名字看);      // true
    }
}

// ── 下面是我写的接口和类 ──

// 接口 = 契约：凡实现 形状 的类，都保证能算面积
interface 形状 {
    // 抽象方法。接口里的方法默认就是 public 的，所以 public abstract 可以省
    double 面积();

    // default 方法：接口里唯一允许"有方法体"的方法。
    // 用它，是为了让拼字符串这段活儿只写一次 —— 所有实现类都白拿。
    default String 说明() {
        return "面积=" + Math.round(面积());      // ⚠ 调 面积()，动态绑定生效
    }
}

// 第二个契约：能给自己的名字
interface 可命名 {
    String 名字();
}

// 一个类可以 implements **多个**接口（extends 只能一个类，这是关键区别）
class 圆 implements 形状, 可命名 {
    private double 半径;      // 数据放在实现类里 —— 接口不能有实例字段

    public 圆(double 半径) {
        this.半径 = 半径;
    }

    // ⚠ 实现接口的方法一律写 public（接口里的方法本来就是 public）
    @Override
    public double 面积() {
        return Math.PI * 半径 * 半径;
    }

    @Override
    public String 名字() {
        return "半径为" + 半径 + "的圆";
    }
}
""";


    [Java检查说明("两个视角都能用：面积、说明、名字、同一对象")]
    public static JavaCheckResult 检查_1(string 代码)
    {
        var run = JavaCheck.直接运行(代码);
        if (!run.编译通过) return JavaCheck.编译失败说明(run);
        string[] 期望 = {
            "12.566370614359172",
            "面积=13",
            "面积=13",
            "半径为2.0的圆",
            "true",
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

    [Java检查说明("形状 写成了 interface（不是 abstract class）")]
    public static JavaCheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("interface 形状"))
            return JavaCheckResult.不过("没找到「interface 形状」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("圆 同时实现了两个接口")]
    public static JavaCheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("implements 形状, 可命名"))
            return JavaCheckResult.不过("没找到「implements 形状, 可命名」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("default 方法写在接口里（说明只有一份）")]
    public static JavaCheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("default String 说明()"))
            return JavaCheckResult.不过("没找到「default String 说明()」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("实现的方法都是 public")]
    public static JavaCheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("public double 面积()"))
            return JavaCheckResult.不过("没找到「public double 面积()」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("圆的面积用了 Math.PI")]
    public static JavaCheckResult 检查_6(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("Math.PI"))
            return JavaCheckResult.不过("没找到「Math.PI」—— 这一课要求用到它。");
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
