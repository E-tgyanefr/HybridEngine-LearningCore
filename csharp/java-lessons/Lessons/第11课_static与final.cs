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
//
// 先介绍：static 解决的是"这份数据不属于任何一个对象"
//
// 前面几课，每个字段都是**每个对象各有一份**：
//
//     计数器 甲 = new 计数器();
//     计数器 乙 = new 计数器();
//     甲 有自己的字段，乙 也有自己的 —— 改甲不动乙。
//
// 这个模型很好用，但有一类数据它装不下：**"一共造了几个对象"**。
//
// 这个数字属于谁？属于 甲 吗？可 乙 也是造出来的，凭什么算在 甲 头上？
// 它不属于任何一个对象，它属于**整个类**。
//
// static 就是给这种成员准备的：
//
//     不加 static  →  属于**对象**，每个对象各一份
//     加了 static  →  属于**类**，只有一份，所有对象共用
//
//     class 计数器 {
//         private static int 个数 = 0;    ← 类成员：全类只有一份
//         private String 名字;            ← 实例成员：每个对象一份
//     }
//
// 直觉检验很简单：**这个值需要"每个对象一个"吗？**
//   人的名字、猫的年龄 → 需要 → 不用 static
//   造了几个对象、全局的常量、纯计算的工具方法 → 不需要 → 用 static
//
// 顺带一个现实证据：main 方法自己就是 static 的。
//   `public static void main(String[] args)` —— 程序刚启动的时候
//   **一个对象都还没有**，哪来的实例方法给你调用？
//   所以入口必须是 static：它不依赖任何对象就能跑。
//
// ─────────────────────────────────────────────────────
//  用法与例子：三种 static 成员
// ─────────────────────────────────────────────────────
//
// ① 静态字段 —— 全类一份
//
//     public static void 加一() { 个数 = 个数 + 1; }
//
//     计数器 甲 = new 计数器();
//     计数器 乙 = new 计数器();
//     甲.加一();
//     乙.加一();
//     System.out.println(甲.报数());    ⇒ 2      ← 不是 1！
//     System.out.println(乙.报数());    ⇒ 2
//     连"报数"都该这么调（更像"类的事"）：
//     System.out.println(计数器.报数());  ⇒ 2
//
//   ⚠ 上面 甲.加一() 和 计数器.加一() **都能编译**，做的事完全一样。
//     Java 允许你"用对象名去调静态方法"，但这是在骗自己 ——
//     它容易让人误以为 个数 是 甲 的。**能写 类名.方法() 就用类名。**
//
// ② 静态常量 —— static + final
//
//     public static final int 起始值 = 0;
//                 ↑      ↑
//              属于类   只能赋一次值
//
//     这是 Java 里"常量"的标准写法：`public static final` 三个词连用。
//     读法是"公开的、全类共用的、不可改的值"。
//     以后你会在 JDK 自己身上看到一模一样的形状：
//         Math.PI      （Math 类里的 public static final double）
//         Integer.MAX_VALUE
//
// ③ 静态方法 —— 不碰实例字段的方法
//
//     public static int 平方(int x) { return x * x; }
//
//     为什么它可以（而且应该）是 static：
//     **它算出来的东西只跟参数有关，跟"哪一个对象"毫无关系。**
//     这类方法用 static 有个好处：不需要先 new 一个对象就能用。
//
//         int r = 计数器.平方(7);      ← 直接调，没有 new
//
//     Java 自己的 Math 类就是这么干的：Math.sqrt(2)、Math.abs(-3)，
//     谁也不觉得需要先 new 一个 Math。
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：在静态方法里用实例字段 —— 症状是"无法从静态上下文中引用非静态变量"
//
//     public static void 打招呼() {
//         System.out.println(名字);        ← ✗
//     }
//
// 你会看到：错误: 无法从静态上下文中引用非静态 变量 名字
// 为什么：这是编译器的实话 —— 静态方法属于**类**，它被调用的时候
// **可能一个对象都还没造出来**（甚至永远不会造）。那 `名字` 该是谁的名字？
// ⚠ 反过来是允许的：实例方法里用静态字段完全没问题
//   （实例方法被调用时，类肯定已经加载了，静态字段一定存在）。
//   记成一句话：**静态的看不见实例的，实例的看得见静态的。**
//
// ⚠ 坑 2：以为"final 修饰一个引用，对象内容也改不了了"
//
//     public static final int[] 表 = {1, 2, 3};
//     表[0] = 99;                    // ✓ 编译通过！跑起来真的改了
//     表 = new int[]{4, 5, 6};       // ✗ 编译错误：无法为最终变量 表 指定值
//
// 你会看到：明明标了 final，数组里的值还是被改了 —— 于是你以为 final 没生效。
// 为什么：final 锁住的是**变量这一格**，也就是"表 这个名字只能指向那一个数组"。
// 而数组是**对象**，`表` 里装的是它的地址；改 表[0] 是"顺着地址进去改内容"，
// 没有给 表 重新赋值，所以 final 管不着。
// 一句话：**final 管"指向谁"，不管"指向的东西里面是什么"。**
//
// ⚠ 坑 3：直接用一个"其实每个对象不一样"的值，却给它加了 static
//
//     class 计数器 {
//         public static String 名字;         ← 手滑加了 static
//     }
//     计数器 甲 = new 计数器();  计数器 乙 = new 计数器();
//     System.out.println(甲.名字);          ⇒ 后造的那个
//
// 你会看到：造了两个对象，但它们"同名"，都是后造的那个。
// 为什么：名字加了 static 就只剩一份了，第二个对象把它盖掉了。
// **同一个 bug 在第 08 课也出现过**（两只猫的年龄一起变），
// 所以看到"多个对象莫名同步变化"，第一个念头就是：这个字段是不是 static？
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   main 里按这个顺序打印（检查就是按这个顺序比对的）：
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
// 期望输出：
//     起始值=0
//     平方(7)=49
//     加一两次后=2
//     一个加=3
//     另一个加=4
//     两个看到=4
//     重置后=0
//
// ⚠ 注意"一个加=3"：前面已经加过两次了，个数**不会**因为造了新对象就归零。
//   这正是 static 的意思 —— 它属于类，跟造不造对象没关系。

// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/java-lessons 检查 11
//
//   看这一课要干什么：dotnet run --project csharp/java-lessons 11
//   看答案（先自己写一遍）：dotnet run --project csharp/java-lessons 答案 11
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/java-lessons 检查 
11
//
//   看这一课要干什么：dotnet run --project csharp/java-lessons 
11
//   看答案（先自己写一遍）：dotnet run --project csharp/java-lessons 答案 
11
//
//   
⚠ 检查会造两个对象、分别给那个 static 计数器加一，再回头看它们看到的是不是同一个数 —— static 到底是「属于类」还是「属于对象」，这里藏不住。
//
//   检查会把你的代码**真的编译、真的跑一遍**再逐条比对 —— 所以它红了就一定有原因，
//   照着那段说明改就行：它一般会直接点出你写错在哪、以及正确的写法长什么样。
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
