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
//
// 先介绍：一个变量，能装好几种东西
//
// 上一课的结尾埋了一个细节：`介绍()` 里调的是 `叫声()`。
// 这次把那件事讲清楚 —— 它叫**多态**，是继承真正的回报。
//
// 先说一个更要紧的问题：父类那个兜底版本是不是很尴尬？
//
//     class 动物 {
//         public String 叫声() { return "……"; }      ← 这个"……"到底是什么意思
//     }
//
// 现实里"一个动物"叫起来是什么声音？**答不出来。**
// 只有具体的狗、猫、牛才有叫声。"一般的动物"这个概念是抽象的，
// 它只是我们用来把狗和猫归成一类的一个说法。
//
// 硬给它编一个"……"是自欺欺人 —— 更糟的是，它会**掩盖错误**：
// 你新写了一个 牛 类忘了覆写 叫声，程序照跑不误，
// 只是牛叫起来是"……"，你要过很久才发现。
//
// 所以 Java 让你把这层意思直接写出来：
//
//     abstract class 形状 {
//         public abstract double 面积();        ← 只写签名，没有方法体
//     }
//
// 这句话的意思是："**凡是形状，都必须能算面积；但怎么算，我不知道，你们自己说。**"
// 于是：
//     · 形状 自己**不能 new**（它不完整，new 出来面积算什么呢）
//     · 每个子类**必须**把 面积() 实现出来，不写就编译不过
//     · 忘了写？编译器当场拦住你，而不是等到"牛叫起来是……"
//
// 那多态在哪？在这里：
//
//     形状 甲 = new 圆(2);          ← 左边是 形状，右边是 圆
//
// 一个"形状"变量装了一个"圆"对象，这叫**向上转型**。
// 合法，因为圆**是**形状（继承关系保证了这一点）。接下来才是魔法：
//
//     甲.说明()   →  里面调 面积()  →  执行的是 **圆** 那一版的 面积()
//
// 明明变量类型是 形状，跑的却是 圆 的代码。
// 这就是**动态绑定**：调用哪个方法，不看变量声明成什么类型，
// **看运行时这个变量实际指向的是哪个对象**。
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
// ① 抽象类长什么样
//
//     abstract class 形状 {
//         public abstract double 面积();          ← 只有签名，末尾分号
//         public String 说明() {                   ← 具体方法：有方法体，照旧
//             return "面积=" + Math.round(面积());
//         }
//     }
//
//   两条规矩：
//     · 有抽象方法的类**必须**标 abstract
//     · 抽象类**不能 new**：`new 形状()` 会报
//         错误: 形状是抽象的; 无法实例化
//   它只能当**类型**用 —— 当变量类型、当参数类型、当数组元素类型。
//
// ② 子类必须交作业
//
//     class 圆 extends 形状 {
//         private double 半径;
//         public 圆(double 半径) { this.半径 = 半径; }
//         @Override
//         public double 面积() { return Math.PI * 半径 * 半径; }
//     }
//
//   ⚠ 如果你忘了写 面积()：
//        错误: 圆不是抽象的, 并且未覆盖形状中的抽象方法面积()
//     这条报错很直白，而且**在编译期**就拦住你 —— 这就是抽象类最实在的好处。
//
// ③ 动态绑定：同一行代码，两种行为
//
//     形状 甲 = new 圆(2);
//     形状 乙 = new 长方形(4, 5);
//     System.out.println(甲.说明());      ⇒ 面积=13      （圆：3.14159*2*2 ≈ 12.57）
//     System.out.println(乙.说明());      ⇒ 面积=20      （长方形：4*5）
//
//   两行**调的是同一个方法**（都写在 形状 里，只有一份），结果却不一样。
//   差别来自"甲指向谁、乙指向谁"。
//
// ④ 真正省事的地方：数组 + 循环
//
//     形状[] 图形们 = new 形状[2];        ← 元素类型是父类
//     图形们[0] = new 长方形(4, 5);
//     图形们[1] = new 圆(2);
//     for (形状 图 : 图形们) {
//         System.out.println(图.说明());    ← 不用判断"这是圆还是长方形"
//     }
//
//   这一段是这一课的重点。注意循环体里**没有 if，没有类型判断** ——
//   因为它不需要：图.说明() 自己会找对正确的 面积()。
//   以后再加一个 三角形 类，这个循环一个字都不用改。
//
// ⚠ 向上转型之后，能调用什么？
//
//     形状 甲 = new 圆(2);
//     甲.面积();        ✓ 形状 里有这个方法
//     甲.说明();        ✓ 形状 里有这个方法
//     甲.半径          ✗ 编译错误：找不到符号
//
// 一句话记法：**编译器只看左边（变量声明成什么类型），
//             运行时才看右边（实际是哪个对象）。**
// 真想拿子类特有的东西，就得**向下转型**，并且先用 instanceof 确认：
//
//     if (甲 instanceof 圆) {
//         圆 圆甲 = (圆) 甲;             ← 强转，把类型"说回去"
//         System.out.println(圆甲.半径);
//     }
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：忘了实现抽象方法 —— 症状是"这个类也得是抽象的"
//
// 你会看到：错误: 圆不是抽象的, 并且未覆盖形状中的抽象方法面积()
// 为什么：抽象方法就是一张**必须签收的合同**。父类说"每个形状都得能算面积"，
// 你继承了它就得交作业。交不出，那你也是抽象的。
// ⚠ 报错里说的"未覆盖"，就是"没写 @Override 那个方法"。
//
// ⚠ 坑 2：想 new 抽象类 / 想 extends 两个类
//
//     new 形状();                    // ✗ 错误: 形状是抽象的; 无法实例化
//     class 圆 extends 形状, 可画 {}  // ✗ 错误: 需要'{'  ← 语法就不对
//
// 症状一：想拿抽象类当"临时对象"用，编译器不让 ——
//   说明你需要的是**另一个具体的子类**，或者那个方法本来就该是 static。
// 症状二：语法错误，因为 **Java 只能单继承**：一个类只能 extends 一个父类。
//   想"同时是两样东西"怎么办？这是下一课接口的内容。
//
// ⚠ 坑 3：跳过 instanceof 就向下转型
//
//     形状 甲 = new 长方形(4, 5);
//     圆 圆甲 = (圆) 甲;                 ← 编译**通过**了！
//     圆甲.面积();                       ← 跑起来才崩
//
// 你会看到：编译通过 —— 但运行时抛
//     Exception in thread "main" java.lang.ClassCastException:
//         class 长方形 cannot be cast to class 圆
// 为什么：编译器只看类型（形状 转 圆 是可能的，它放行），
// 是不是真的圆要**跑起来才知道**。长方形不是圆，所以崩。
// ⚠ 规矩是：**向下转型之前先问一句 instanceof。**
//
// ⚠ 坑 4：抽象方法写了方法体，或者忘了 abstract
//
//     public abstract double 面积() { return 0; }      // ✗
//     你会看到：错误: 抽象方法不能有主体
//     public double 面积();                            // ✗ 少了 abstract
//     你会看到：错误: 缺少方法主体, 或声明抽象
//   抽象方法就两条：**签名 + 分号**，没有花括号，并且必须带 abstract。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   main 里四步，按顺序：
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
// 期望五行输出：
//     面积=20
//     面积=13
//     面积=13
//     面积=20
//     面积=13
//
// ⚠ 说明() 里要写 Math.round(面积())，不要自己写死数字；
//   圆 的 面积() 要写 Math.PI，不要用 3.14（那是错的算法，检查会看 Math.PI）。

// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/java-lessons 检查 13
//
//   看这一课要干什么：dotnet run --project csharp/java-lessons 13
//   看答案（先自己写一遍）：dotnet run --project csharp/java-lessons 答案 13
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
