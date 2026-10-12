using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("11", "第 11 课：static 与 final —— 属于类的东西",
    """
1. 写一个 计数器 类：private static int 个数 = 0; / public static final int 起始值 = 0; / private String 名字;
2. 写四个静态方法：报数()、加一()、重置()、平方(int x)
3. main 里依次打印：起始值、平方(7)、加一两次后、一个加、另一个加、两个看到、重置后
""")]
public static class 第11课
{
    public static string 模板 => """
// 第 11 课：static 与 final —— 属于类的东西
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**有些东西属于「类」，不属于某个对象**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    static 和 final：属于类的东西
//
//  需要先会
//    第 09 课：private 字段
//
// ─────────────────────────────────────────────────────
//  先介绍：一个计数器的困境
// ─────────────────────────────────────────────────────
//
// 想统计「一共 new 了几个对象」，用普通字段做不到 ——
// 每 new 一个就多一份自己的字段，谁也不认识谁。
//
// 需要一个**大家共用**的东西。那就是 static。
//
// ─────────────────────────────────────────────────────
//  一、static：属于类，只有一份
// ─────────────────────────────────────────────────────
//
//     class 计数器 {
//         static int 个数 = 0;              ← 整个类共用一份
//
//         static void 加一() { 个数++; }     ← 静态方法：不用 new 就能调
//         static int 报数() { return 个数; }
//     }
//
//     计数器.加一();        用**类名**调用，不用 new
//     计数器.报数();        ⇒ 1
//
// ⚠ 判断一个东西该不该 static，问一句：
//
//     「**每一只**都要有自己的一份吗？」
//         要   → 不写 static（字段）
//         不要 → 写 static（大家共用）
//
// 对照第 08 课的坑：给 `猫.年龄` 加了 static，结果改一只、**所有猫一起变**。
//
// ─────────────────────────────────────────────────────
//  二、final：赋值一次就不能再改
// ─────────────────────────────────────────────────────
//
//     static final int 起始值 = 0;
//     起始值 = 5;             ✗ 错误: 无法为最终变量 起始值 分配值
//
//     `static final` 一起用，就是别的语言里的「常量」。
//     命名习惯上通常写成全大写（START），这门课沿用中文名。
//
// ─────────────────────────────────────────────────────
//  三、main 为什么是 static
// ─────────────────────────────────────────────────────
//
//     程序刚启动的那一刻，**一个对象都还没有**。
//
// 所以入口只能是静态的 —— 它不需要任何对象就能被调用。
// 而 static 方法里**不能直接调用非静态方法**（那些方法得有对象才能用）：
//
//     错误: 无法从静态上下文中引用非静态 方法 加倍(int)
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：静态的东西被所有对象共享
// ─────────────────────────────────────────────────────
//
//     计数器 甲 = new 计数器();
//     计数器 乙 = new 计数器();
//     甲.加一();
//     System.out.println(乙.报数());      ⇒ 1     ← 乙 明明是新的
//
// 你会看到：**编译通过**，只是「乙 的个数」不是 0，而是 1。
//
// 为什么：`个数` 是 static 的，整个类只有**一份** ——
//   甲.加一() 改的就是那一份，乙 看到的自然也是 1。
//
// ⚠ 写 `甲.加一()` 语法上合法（编译器不拦），但它**有误导性** ——
//   看起来像在改「甲自己的」东西。规范写法一律用**类名**：`计数器.加一()`。
//
// ⇒ 记住那条判据：**「每一只都要有自己的一份吗？」**
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   继承 → 第 12 课。接口 → 第 14 课。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  main 里按这个顺序打印（检查就是按这个顺序比对的）：
//
//     System.out.println("起始值=" + 计数器.起始值);        ← 用**类名**读静态常量
//     System.out.println("平方(7)=" + 计数器.平方(7));      ← 用**类名**调静态方法
//
//     计数器.加一();      // 第一次：个数 0 → 1
//     计数器.加一();      // 第二次：个数 1 → 2
//     System.out.println("加一两次后=" + 计数器.报数());
//
//     计数器 甲 = new 计数器();
//     计数器 乙 = new 计数器();
//     甲.加一();
//     System.out.println("一个加=" + 甲.报数());
//     乙.加一();
//     System.out.println("另一个加=" + 乙.报数());
//     System.out.println("两个看到=" + 甲.报数());
//
//     计数器.重置();
//     System.out.println("重置后=" + 计数器.报数());
//
//  ⚠ 要写 `static final int 起始值 = 0;`、`static int 平方(int n)`、
//    `static void 加一()`、`static int 报数()`、`static void 重置()`。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """
// 第 11 课参考答案：static 与 final
//
// ⚠ public class Main 必须是文件里第一个"非注释、非空行"的语句。
//    计数器 类放后面，且不写 public。

public class Main {
    public static void main(String[] args) {
        // ① 静态常量与静态方法：不用 new，直接用**类名**拿
        System.out.println("起始值=" + 计数器.起始值);
        System.out.println("平方(7)=" + 计数器.平方(7));

        // ② 静态字段：属于类，不属于任何一个对象
        计数器.加一();
        计数器.加一();
        System.out.println("加一两次后=" + 计数器.报数());   // 2

        // ③ 造两个对象也只是"两个入口"，它们摸到的是同一份 个数
        计数器 甲 = new 计数器();
        计数器 乙 = new 计数器();
        甲.加一();
        System.out.println("一个加=" + 甲.报数());          // 3（不是 1！）
        乙.加一();
        System.out.println("另一个加=" + 乙.报数());        // 4
        System.out.println("两个看到=" + 甲.报数());        // 4 —— 甲看到的也变了

        // ④ 重置：类的静态字段归位
        计数器.重置();
        System.out.println("重置后=" + 计数器.报数());      // 0
    }
}

// ── 下面是我写的类 ──
class 计数器 {
    // 静态字段：属于"计数器"这个类，全类只有一份，所有对象共用
    // ⚠ 没写 static 的话这里会变成"每个对象各一份"，第 ③ 步就全错了
    private static int 个数 = 0;

    // 静态常量：public static final 三个词连用才是 Java 里常量的标准写法
    public static final int 起始值 = 0;

    // 实例字段：每个对象各一份。它和上面的 个数 放在一起，
    // 是为了让你一眼看出两种成员的区别（这一课不直接用它）。
    private String 名字;

    // 静态方法：不碰任何实例字段，所以可以（也应该）是 static
    public static int 报数() {
        return 个数;
    }

    public static void 加一() {
        个数 = 个数 + 1;        // 改的是类的那一份，所有对象都看得见
    }

    public static void 重置() {
        个数 = 起始值;          // 用常量而不是写死 0，哪天改起始值只改一处
    }

    // 纯计算，和"哪一个对象"无关 → static
    public static int 平方(int x) {
        return x * x;
    }
}
""";


    [Java检查说明("三种静态成员都对，两个对象看到同一个数")]
    public static JavaCheckResult 检查_1(string 代码)
    {
        var run = JavaCheck.直接运行(代码);
        if (!run.编译通过) return JavaCheck.编译失败说明(run);
        string[] 期望 = {
            "起始值=0",
            "平方(7)=49",
            "加一两次后=2",
            "一个加=3",
            "另一个加=4",
            "两个看到=4",
            "重置后=0",
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

    [Java检查说明("计数用的字段声明成了 static")]
    public static JavaCheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("private static int 个数"))
            return JavaCheckResult.不过("没找到「private static int 个数」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("常量写成了 static final")]
    public static JavaCheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("static final int 起始值"))
            return JavaCheckResult.不过("没找到「static final int 起始值」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("平方是静态方法")]
    public static JavaCheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("public static int 平方(int x)"))
            return JavaCheckResult.不过("没找到「public static int 平方(int x)」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("两个对象看到的是同一个数（没有变成\"各一份\"）")]
    public static JavaCheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("甲.报数()"))
            return JavaCheckResult.不过("没找到「甲.报数()」—— 这一课要求用到它。");
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
