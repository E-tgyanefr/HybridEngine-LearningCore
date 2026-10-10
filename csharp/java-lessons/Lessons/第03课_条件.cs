// 第 03 课：条件判断 —— 让程序会做选择
//
// ══ 这一课要教什么 ══
// 到上一课为止，程序只会一条道走到黑。这一课让它**分岔**：同一个方法，
// 不同的输入走不同的分支 —— 这是"程序开始像在思考"的第一步。
//
// 而 Java 在 `=` 和 `==` 这件事上的表现**和 C/C++ 不一样**，必须讲清楚，
// 因为它是"一半被拦住、一半没拦住"的：
//   ① if (分数 = 3)        编译不过 —— 因为 int 不是 boolean
//                          （C/C++ 只是发个警告，Java 直接拒绝编译 —— 这是好事）
//   ② if (年 % 4 = 0)      编译不过 —— 报「意外的类型 / 需要: 变量」
//                          （= 的优先级最低，左边 x%4 不是变量）
//   ③ boolean 标志 = …;
//      if (标志 = true)    **编译通过、0 条警告，而且条件永远为真** ← 真正的坑
// 第 ③ 种是 Java 唯一拦不住的形式，所以这一课的核心检查点专门盯它。
// （三条都是本机 javac 25.0.3 实测的，不是推测。）
//
// ══ 检查设计说明（给维护者）══
// 核心检查点是第 5 条（boolean 标志不能把 == 写成 =），用**行为**抓：
//   if (标志 = true) 让"是闰年"这个分支恒为真，于是 2023 也会被判成闰年 ——
//   一个年份不可能既是平年又是闰年，一测就现形。
// 源码扫描只是拿来把提示说得更准（"我看到了 if (… = …)"），**不作为判据**。
//
// 另外几条：
//   · 第 2/3/4/6 条用 逐个用例 多组输入一起测 —— 一组蒙对不算过。
//     第 3 条的边界值（90/89/60/59/±越界）专门抓 >= 写成 > 这类边界错。
//   · 第 7 条要求源码里真的出现 boolean / && / || / switch：闰年用嵌套 if、
//     星期名用 if-else 链也能写对，但那就绕过了这一课要练的东西。
//     这是**有意**的一条"练到了没有"的检查（题目里已经写明了要求）。
//
// ⚠ 这一课的方法必须写 static：检查是把学生的代码放进 class Main 里、
//   从 main 里调用它们（JavaCheck.默认骨架）。非 static 的方法在静态上下文里
//   调不到，会编译不过 —— 第 05 课会专门讲 static，这里先用着。
//   （"忘了 static" 的报错由 ExplainJava 负责翻译，本课只加一句指路。）

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LearnCode;
using LearnCode.JavaLessons;

[Java课程("03", "第 03 课：条件判断",
    "1. static String 猜数字(int 猜)：等于 42 → \"对了\" / 小于 42 → \"小了\" / 大于 42 → \"大了\"\n" +
    "2. static String 成绩等级(int 分数)：越界 → \"分数不合法\" / >=90 → \"优秀\" / >=60 → \"及格\" / 其他 → \"不及格\"\n" +
    "3. static String 闰年(int 年)：先把结果算进一个 boolean 变量，再用它判断。用 && 和 || 把条件连起来写\n" +
    "4. static String 星期名(int 星期几)：用 switch；1→\"星期一\" … 7→\"星期日\"；其他 → \"没有这个星期\"")]
public static class 第03课
{
    public static string 模板 => """
// 第 03 课：条件判断
//
// 要做：写四个**方法**（不用写 main，检查时会自动调用它们）
// ⚠ 四个都要写 static，而且只要方法，不要写 class（检查时会替你套上）
//
//   1. static String 猜数字(int 猜)
//        猜 == 42  → "对了"
//        猜 <  42  → "小了"
//        猜 >  42  → "大了"
//
//   2. static String 成绩等级(int 分数)
//        分数 < 0 或者 分数 > 100  → "分数不合法"    ← 这个条件用 || 写
//        分数 >= 90                → "优秀"
//        分数 >= 60                → "及格"
//        其他                      → "不及格"
//      ⚠ 想清楚条件的**先后顺序**：分数 = 105 应该走哪一条？
//
//   3. static String 闰年(int 年)
//        「能被 4 整除 且 不能被 100 整除」 或者 「能被 400 整除」 → "闰年"
//        否则 → "平年"
//      ⚠ 两件事都要做到：
//        ① 用 && 和 || 把条件连成**一个**条件，不要用嵌套 if —— 这一课就是要练它俩
//        ② 先把结果算进一个 boolean 变量，再用这个变量去 if：
//              boolean 是闰年 = (年 % 4 == 0 && 年 % 100 != 0) || 年 % 400 == 0;
//              if (是闰年) { ... }
//      （整除用 %：年 % 4 == 0 就是"能被 4 整除"）
//
//   4. static String 星期名(int 星期几)
//        用 **switch** 写：1 → "星期一" … 7 → "星期日"
//        其他数字 → "没有这个星期"
//      ⚠ 这一课要求必须用 switch（用一串 if-else 也能写对，但那就没练到它）
//
// 三个必须知道的点：
//
//   · 判断相等是**两个**等号： if (猜 == 42)
//     Java 在这一点上比 C/C++ 严，但**只严一半**：
//         if (分数 = 3)          ← 编译不过（int 不是 boolean，Java 当场拦住你）
//         if (年 % 4 = 0)        ← 编译不过（报「意外的类型」，同上）
//         if (标志 = true)       ← **能编译！** boolean 赋给 boolean 是合法的，
//                                  而赋值的结果就是刚存进去的 true → 条件**永远为真**
//     ⚠ 最后这一种 Java 拦不住：程序照跑，结果全错，一声不吭。
//       所以：判断就用 ==，一个 = 是赋值。
//
//   · 每个分支都要有 return。
//     Java 要求「所有能走到的路径都有返回值」，漏了会报「缺少返回语句」。
//     最省事的写法是 if / else if / else 兜住全部情况。
//
//   · 方法要写 static（检查是从 main 里调用它们的）：
//         static String 猜数字(int 猜) { ... }
//     漏了 static 会报「无法从静态上下文中引用非静态 方法」。
//
// 写完后运行：dotnet run --project csharp/java-lessons 检查 03

// ── 在下面写你的代码 ──
// 提示：只需要写方法。形如：
//
//   static String 猜数字(int 猜) {
//       if (猜 == 42) {
//           return "对了";
//       }
//       ...
//   }

""";

    public static string 参考答案 => """
static String 猜数字(int 猜) {
    if (猜 == 42) {
        return "对了";
    } else if (猜 < 42) {
        return "小了";
    } else {
        return "大了";
    }
}

static String 成绩等级(int 分数) {
    // 先排除不合法的情况 —— 顺序反了的话 105 会被判成"优秀"
    if (分数 < 0 || 分数 > 100) {
        return "分数不合法";
    } else if (分数 >= 90) {
        return "优秀";
    } else if (分数 >= 60) {
        return "及格";
    } else {
        return "不及格";
    }
}

static String 闰年(int 年) {
    // 先把"是不是闰年"算进一个 boolean 变量，再用它判断（这一课要练的写法）
    boolean 是闰年 = (年 % 4 == 0 && 年 % 100 != 0) || 年 % 400 == 0;
    if (是闰年) {
        return "闰年";
    } else {
        return "平年";
    }
}

static String 星期名(int 星期几) {
    switch (星期几) {
        case 1:  return "星期一";
        case 2:  return "星期二";
        case 3:  return "星期三";
        case 4:  return "星期四";
        case 5:  return "星期五";
        case 6:  return "星期六";
        case 7:  return "星期日";
        default: return "没有这个星期";
    }
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("四个方法都写好了吗（名字、参数类型、返回类型、static 要对上）")]
    public static JavaCheckResult 检查_四个方法都在(string 代码)
    {
        string 调用点 =
            "        System.out.println(猜数字(42));\n" +
            "        System.out.println(成绩等级(95));\n" +
            "        System.out.println(闰年(2024));\n" +
            "        System.out.println(星期名(1));\n";

        var run = JavaCheck.编译并运行(代码, null, 调用点);
        if (run.Raw.CompileFailed) return 骨架编译失败说明(代码, run);

        if (run.Raw.Error != null)
            return JavaCheckResult.不过("方法调用了，但程序没能跑完：\n      " + 缩进(run.Raw.Error));

        return JavaCheckResult.过("四个方法都能正常调用");
    }

    [Java检查说明("猜数字：三个分支判断得对不对")]
    public static JavaCheckResult 检查_猜数字(string 代码)
    {
        return JavaCheck.逐个用例(代码,
            new JavaCheck.用例("猜 10（比 42 小）", "猜数字(10)", "小了"),
            new JavaCheck.用例("猜 0（比 42 小）", "猜数字(0)", "小了"),
            new JavaCheck.用例("猜 41（差一点）", "猜数字(41)", "小了"),
            new JavaCheck.用例("猜 42（正好）", "猜数字(42)", "对了"),
            new JavaCheck.用例("猜 43（多了一点）", "猜数字(43)", "大了"),
            new JavaCheck.用例("猜 100（比 42 大）", "猜数字(100)", "大了"));
    }

    [Java检查说明("成绩等级：边界分数（90 / 60）和越界分数判对了没有")]
    public static JavaCheckResult 检查_成绩等级(string 代码)
    {
        // 90 / 89 和 60 / 59 这几对是照妖镜：
        // 把 >= 写成 > 的人，90 分和 60 分这两条会立刻失败。
        return JavaCheck.逐个用例(代码,
            new JavaCheck.用例("95 分", "成绩等级(95)", "优秀"),
            new JavaCheck.用例("90 分（边界，算优秀）", "成绩等级(90)", "优秀"),
            new JavaCheck.用例("89 分（差一点）", "成绩等级(89)", "及格"),
            new JavaCheck.用例("60 分（边界，算及格）", "成绩等级(60)", "及格"),
            new JavaCheck.用例("59 分（差一点）", "成绩等级(59)", "不及格"),
            new JavaCheck.用例("0 分", "成绩等级(0)", "不及格"),
            new JavaCheck.用例("-1 分（不合法）", "成绩等级(-1)", "分数不合法"),
            new JavaCheck.用例("101 分（不合法）", "成绩等级(101)", "分数不合法"));
    }

    [Java检查说明("闰年：&& 和 || 拼出来的条件对不对（1900 和 2000 是分水岭）")]
    public static JavaCheckResult 检查_闰年(string 代码)
    {
        // 1900 和 2000 是这一题的照妖镜：
        //   1900 能被 4 整除、也能被 100 整除 → 平年（只写 年 % 4 == 0 的会判错）
        //   2000 能被 400 整除              → 闰年（漏掉 || 后面那半句的会判错）
        return JavaCheck.逐个用例(代码,
            new JavaCheck.用例("2024（普通闰年）", "闰年(2024)", "闰年"),
            new JavaCheck.用例("2023（普通平年）", "闰年(2023)", "平年"),
            new JavaCheck.用例("1900（整百但不被 400 整除）", "闰年(1900)", "平年"),
            new JavaCheck.用例("2000（被 400 整除）", "闰年(2000)", "闰年"),
            new JavaCheck.用例("2100（整百但不被 400 整除）", "闰年(2100)", "平年"));
    }

    [Java检查说明("boolean 标志有没有把 == 写成 =（写成 = 程序照跑，而且永远为真）")]
    public static JavaCheckResult 检查_boolean标志没写错(string 代码)
    {
        // ⚠ 这一条是本课的核心检查点。
        //
        // 为什么单挑它：`=` 和 `==` 在 Java 里有三种下场（本机 javac 25.0.3 实测）：
        //   ① if (分数 = 3)      编译不过：不兼容的类型: int无法转换为boolean
        //   ② if (年 % 4 = 0)    编译不过：意外的类型 / 需要: 变量 / 找到: 值
        //   ③ if (标志 = true)   **编译通过、零警告**，而且条件**永远为真**
        // 前两种编译器替学生挡了，第三种挡不住 —— 只考它。
        //
        // 怎么用行为抓，而不是去猜源码：
        //   `if (标志 = true)` 让"闰年"这个分支恒定成立 → 2023 也会被判成闰年。
        //   2023 是平年。一个年份不可能两个都不是/都是 —— 一测就现形。
        var run = JavaCheck.编译并运行(代码, null,
            "        System.out.println(闰年(2023));\n" +
            "        System.out.println(闰年(2024));\n");

        if (run.Raw.CompileFailed) return 骨架编译失败说明(代码, run);
        if (run.行.Count < 2)
            return JavaCheckResult.不过(
                "闰年() 没有用 return 返回字符串，所以拿不到结果。\n" +
                "      这一课的方法要**返回**字符串：\n" +
                "          static String 闰年(int 年) {\n" +
                "              boolean 是闰年 = (年 % 4 == 0 && 年 % 100 != 0) || 年 % 400 == 0;\n" +
                "              if (是闰年) return \"闰年\";\n" +
                "              return \"平年\";\n" +
                "          }\n" +
                "      而不是在方法里 System.out.println(\"闰年\");");

        string 平年 = run.行[0].Trim();     // 闰年(2023) 应该是"平年"
        string 闰年结果 = run.行[1].Trim(); // 闰年(2024) 应该是"闰年"

        if (平年 == "平年" && 闰年结果 == "闰年")
            return JavaCheckResult.过("判断用的是 ==：2023 判「平年」、2024 判「闰年」，两个分支分得开");

        bool 源码里把赋值当条件 = 条件里有赋值(代码);

        if (平年 == "闰年" && 闰年结果 == "闰年")
            return JavaCheckResult.不过(
                (源码里把赋值当条件
                    ? "我在你的代码里看到了形如 if (… = …) 的写法 —— 问题就在这里。\n"
                    : "") +
                "闰年(2023) 给的是「" + 平年 + "」，闰年(2024) 给的也是「" + 闰年结果 + "」。\n" +
                "      2023 不是闰年、2024 是，两个结果**不可能一样**——\n" +
                "      出现这种「所有输入都走同一个分支」的现象，几乎就是把 == 写成了 =：\n" +
                "          if (是闰年 == true)   // 判断：是闰年 是不是 true ？\n" +
                "          if (是闰年 = true)    // 赋值：把 true 存进 是闰年，再拿它当条件\n" +
                "      赋值表达式的值就是刚存进去的 true —— 所以条件**永远为真**。\n" +
                "      ⚠ 最坑的是：这**不是编译错误**。javac 一声不吭，程序照跑、照样出结果。\n" +
                "        （Java 只挡住了 int 那种：if (分数 = 3) 会报「int 无法转换为 boolean」。\n" +
                "          boolean 赋给 boolean 是合法的，所以这一种它拦不住。）\n" +
                "      改法：判断相等用 ==；而 boolean 变量最省事的写法是**直接写它**：\n" +
                "          boolean 是闰年 = (年 % 4 == 0 && 年 % 100 != 0) || 年 % 400 == 0;\n" +
                "          if (是闰年) {          // ← 不用写 == true，多写反而多一次手滑机会\n" +
                "              return \"闰年\";\n" +
                "          }");

        if (平年 == "平年" && 闰年结果 == "平年")
            return JavaCheckResult.不过(
                "闰年(2023) 和 闰年(2024) 都给的是「平年」—— 2024 是闰年，应该是「闰年」。\n" +
                "      这种「所有输入都走同一个分支」的现象，通常是把 == 写成了 =：\n" +
                "          if (是闰年 = false)   // 把 false 存进变量，条件永远为假\n" +
                "      赋值表达式的值就是刚存进去的那个值，所以条件跟变量原来的值无关了。\n" +
                "      ⚠ 这**不是编译错误**，javac 不会提醒你 —— boolean 赋给 boolean 是合法的。\n" +
                "      改法：判断相等用 ==，或者 boolean 变量直接写 if (是闰年)。\n" +
                "      顺便检查闰年条件本身写全了没有：\n" +
                "          (年 % 4 == 0 && 年 % 100 != 0) || 年 % 400 == 0");

        return JavaCheckResult.不过(
            $"闰年(2023) 应该是「平年」，你的方法给的是「{平年}」；\n" +
            $"      闰年(2024) 应该是「闰年」，给的是「{闰年结果}」。\n" +
            "      想清楚这三件事：\n" +
            "        · 判断相等用 ==（两个等号），一个 = 是赋值\n" +
            "        · 闰年的条件要**两半**：能被 4 整除且不被 100 整除，**或者**能被 400 整除\n" +
            "              boolean 是闰年 = (年 % 4 == 0 && 年 % 100 != 0) || 年 % 400 == 0;\n" +
            "        · 最后一定要有一个 else（或者无条件的 return \"平年\";），\n" +
            "          否则 Java 会报「缺少返回语句」");
    }

    [Java检查说明("星期名：switch 的数对不对（1~7 和范围外都要照顾到）")]
    public static JavaCheckResult 检查_星期名(string 代码)
    {
        return JavaCheck.逐个用例(代码,
            new JavaCheck.用例("星期名(1)", "星期名(1)", "星期一"),
            new JavaCheck.用例("星期名(2)", "星期名(2)", "星期二"),
            new JavaCheck.用例("星期名(3)", "星期名(3)", "星期三"),
            new JavaCheck.用例("星期名(4)", "星期名(4)", "星期四"),
            new JavaCheck.用例("星期名(5)", "星期名(5)", "星期五"),
            new JavaCheck.用例("星期名(6)", "星期名(6)", "星期六"),
            new JavaCheck.用例("星期名(7)", "星期名(7)", "星期日"),
            new JavaCheck.用例("星期名(0)（范围外）", "星期名(0)", "没有这个星期"),
            new JavaCheck.用例("星期名(8)（范围外）", "星期名(8)", "没有这个星期"),
            new JavaCheck.用例("星期名(-1)（范围外）", "星期名(-1)", "没有这个星期"));
    }

    [Java检查说明("boolean / && / || / switch 这一课要求都亲手写一遍")]
    public static JavaCheckResult 检查_该练的语法练到了(string 代码)
    {
        // 为什么用"看源码"而不是"看行为"：
        // 闰年用嵌套 if 也能写对、星期名用 if-else 链也能写对（行为一样），
        // 但那就绕过了这一课要练的东西。这是**有意**的一条"练到了没有"的检查 ——
        // 题目里已经写明了要求。
        // 为了不被注释骗过去，先把注释去掉再找。
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        if (!Regex.IsMatch(净, @"\bboolean\b"))
            缺.Add("boolean 变量（闰年那题：先把结果算进一个 boolean，再用它判断）");
        if (!净.Contains("&&")) 缺.Add("&&（并且 —— 闰年条件的前半句要用）");
        if (!净.Contains("||")) 缺.Add("||（或者 —— 越界判断和闰年后半句要用）");
        if (!Regex.IsMatch(净, @"\bswitch\b")) 缺.Add("switch（星期名那题必须用它）");

        if (缺.Count == 0)
            return JavaCheckResult.过("boolean、&&、||、switch 都用上了");

        return JavaCheckResult.不过(
            "这一课要求这四样都亲手写一遍，你的代码里还缺：\n" +
            string.Join("", 缺.Select(x => "        · " + x + "\n")) +
            "      题目里的分工是这样：\n" +
            "          成绩等级  用 || 写越界判断：if (分数 < 0 || 分数 > 100)\n" +
            "          闰年      用 boolean 变量装结果，条件用 && 和 || 拼成一个\n" +
            "          星期名    用 switch\n" +
            "      如果你用了别的写法（嵌套 if、if-else 链），结果可能是对的 ——\n" +
            "      但这一课就是要练这几个东西，改过来再交。\n" +
            "      顺便记住：\n" +
            "          &&  两边都为真，结果才为真     （并且）\n" +
            "          ||  只要有一个为真，结果就为真 （或者）\n" +
            "          !   取反：!是闰年 就是「不是闰年」\n" +
            "      优先级：&& 比 || 先算，所以上面那层的括号其实可以省，\n" +
            "      但**写上更好读** —— 半年后回来看，你会感谢自己。");
    }

    // ══════════════════ 工具 ══════════════════

    /// <summary>
    /// 编译失败时的说明 —— 先认几条这一课特别容易撞上的 Java 错。
    ///
    /// 这几条都是本机 javac 25.0.3 实测过的原文：
    ///     无法从静态上下文中引用非静态 方法 猜数字(int)     ← 忘了 static
    ///     意外的类型 / 需要: 变量 / 找到: 值                ← 复合条件里把 == 写成 =
    ///     非法的类型开始                                   ← import 写进了 class 里面
    ///     （缺 return、找不到符号这些交给 ExplainJava 统一讲）
    /// </summary>
    private static JavaCheckResult 骨架编译失败说明(string 代码, JavaRun run)
    {
        string 信息 = run.编译信息;

        if (信息.Contains("无法从静态上下文") || 信息.Contains("cannot be referenced from a static context"))
            return JavaCheckResult.不过(
                "编译没过：**方法忘了写 static**。\n" +
                "      检查的时候，你的方法是被放进 class Main 里、从 main 里调用的：\n" +
                "          public class Main {\n" +
                "              static String 猜数字(int 猜) { ... }   // ← 你的方法\n" +
                "              public static void main(String[] args) {\n" +
                "                  System.out.println(猜数字(10));    // ← 从这里调用\n" +
                "              }\n" +
                "          }\n" +
                "      main 是 static 的，它**只能直接调用 static 的方法** ——\n" +
                "      不写 static 的方法属于「对象」，得先 new 一个对象出来才能用。\n" +
                "      改法（这一课先用第一种，第 05 课专门讲 static）：\n" +
                "          static String 猜数字(int 猜) { ... }   // ← 加上 static\n" +
                "      编译器原文：\n" + 缩进(前几行(信息)));

        if (信息.Contains("意外的类型") || 信息.Contains("unexpected type")
            || 信息.Contains("需要: 变量") || 信息.Contains("required: variable"))
            return JavaCheckResult.不过(
                "编译没过：**条件里大概把 == 写成了 =**。\n" +
                "      比如这样的写法：\n" +
                "          if (年 % 4 = 0)                     ← 错\n" +
                "          if (分数 >= 90 && 全勤 = true)      ← 错（复合条件里也一样）\n" +
                "      为什么编译不过：= 的优先级最低，上面第一行会被理解成\n" +
                "      「把 0 存进 年 % 4」—— 可 年 % 4 不是一个变量，存不进去，\n" +
                "      于是 javac 报「意外的类型 / 需要: 变量 / 找到: 值」。\n" +
                "      ✅ 好消息：**Java 在这里比 C/C++ 严** —— C/C++ 对 if (x = 3) 只是\n" +
                "         给个警告（甚至不警告），Java 直接不让你编译。\n" +
                "      ⚠ 但要记住它只挡住了**一半**：\n" +
                "         if (分数 = 3)       编译不过（int 不是 boolean）\n" +
                "         if (标志 = true)    **能编译**，而且条件永远为真 —— 这个 Java 拦不住。\n" +
                "      改法：判断相等一律用 ==。\n" +
                "      防身技巧：把常量写在左边 —— if (42 == 猜)。手滑写成 = 会直接编译不过。\n" +
                "      编译器原文：\n" + 缩进(前几行(信息)));

        if (信息.Contains("非法的类型开始") || 信息.Contains("illegal start of type"))
            return JavaCheckResult.不过(
                "编译没过：代码里有 import（或者 class）写在了**方法外面**。\n" +
                "      这一课只需要写四个**方法**，不要写 import、也不写 class ——\n" +
                "      检查的时候会把它套进 class Main 里：\n" +
                "          public class Main {\n" +
                "              // ↓ 你的代码放在这里，所以 import 放到这儿是不合法的\n" +
                "              static String 猜数字(int 猜) { ... }\n" +
                "          }\n" +
                "      import 只能出现在文件最上面（class 外面）。这一课用不到 import，\n" +
                "      删掉它就好。");

        return JavaCheck.编译失败说明(run);
    }

    /// <summary>
    /// 源码里有没有「把赋值当条件」的写法，比如 if (标志 = true)。
    ///
    /// ⚠ 它只用来把提示说得更准，**不作为判据** ——
    /// 正则会被各种写法绕过去（跨行、括号嵌套、写在 for 里），
    /// 真正可靠的是上面那条按行为抓的检查。
    ///
    /// 注意检查的范围是 if / while 的**整个条件**，包括复合条件：
    /// 只要括号里出现了 `= 值` 这种形式（排除 ==、!=、&lt;=、&gt;=），就算命中。
    /// </summary>
    private static bool 条件里有赋值(string 代码)
    {
        string 净 = 去掉注释(代码);
        foreach (Match m in Regex.Matches(净, @"\b(if|while)\s*\(([^()]*)\)"))
        {
            string 条件 = m.Groups[2].Value;
            // 先去掉 ==、!=、<=、>= 这些**合法**的等号
            string 剩下的 = 条件.Replace("==", " ").Replace("!=", " ")
                                 .Replace("<=", " ").Replace(">=", " ");
            if (Regex.IsMatch(剩下的, @"(?<![=!<>+\-*/%])=(?!=)")) return true;
        }
        return false;
    }

    /// <summary>去掉注释 —— 免得注释里的 && / switch / if (x = 1) 被当成代码。</summary>
    private static string 去掉注释(string 代码)
    {
        string s = Regex.Replace(代码, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(s, @"//[^\n]*", " ");
    }

    private static string 前几行(string s)
    {
        var 行 = s.Replace("\r\n", "\n").Split('\n')
                   .Where(l => l.Trim().Length > 0).Take(6).ToList();
        return 行.Count > 0 ? string.Join("\n", 行) : "（编译器没有输出）";
    }

    private static string 缩进(string s) => s.Trim().Replace("\n", "\n      ");
}
