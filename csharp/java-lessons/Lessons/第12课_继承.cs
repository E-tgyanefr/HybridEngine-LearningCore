using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("12", "第 12 课：继承 —— extends、super、方法覆写",
    """
1. 写 动物 类：private String 名字;、构造方法 动物(String)、取名字()、叫声()（返回"……"）、介绍()（返回 名字 + "说" + 叫声()）
2. 写 狗 extends 动物：构造方法 狗(String 名字) 必须 super(名字)；@Override public String 叫声() 返回 "汪"
3. 写 猫 extends 动物：同构，叫声() 返回 "喵"
4. main 里造一只狗一只猫，依次打印 狗.介绍() / 狗.叫声() / 猫.叫声() / 猫.介绍()
""")]
public static class 第12课
{
    public static string 模板 => """
// 第 12 课：继承 —— extends、super、方法覆写
//
// 这一课讲一件事：**子类可以白拿父类的代码，还能改掉其中一部分**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    继承：extends、super、方法覆写
//
//  需要先会
//    第 10 课：构造方法；第 11 课：static
//
// ─────────────────────────────────────────────────────
//  先介绍：狗和猫都会「介绍自己」
// ─────────────────────────────────────────────────────
//
// 如果各写一遍「我叫旺财」，两个类里就有两份一模一样的代码。
// 共同的部分提到父类里写一次，各自不同的部分（叫声）在子类里覆写。
//
// ─────────────────────────────────────────────────────
//  一、extends 和 super
// ─────────────────────────────────────────────────────
//
//     class 动物 {
//         protected String 名字;                 ← protected：子类能用
//
//         public 动物(String 名字) { this.名字 = 名字; }
//
//         public String 叫声() { return "……"; }   ← 父类的默认版本
//         public String 介绍() { return 名字 + "说" + 叫声(); }
//     }
//
//     class 狗 extends 动物 {                     ← 白拿 名字 / 介绍()
//         public 狗(String 名字) { super(名字); } ← 调用父类构造方法
//
//         @Override
//         public String 叫声() { return "汪"; }    ← 覆写：换掉这一个方法
//     }
//
// ⚠ 两个必须写的东西：
//
//     super(名字)     子类构造方法里**第一句**就得调父类构造方法
//     @Override       注解，不是语法要求，但**强烈建议写** ——
//                     名字打错时编译器会当场告诉你（不写就悄悄变成新方法）
//
// ─────────────────────────────────────────────────────
//  二、覆写 vs 重载
// ─────────────────────────────────────────────────────
//
//     覆写（override）  子类重新实现父类的**同一个**方法（名字、参数都一样）
//     重载（overload）  同一个类里**同名不同参数**的几个方法（第 10 课见过）
//
// ─────────────────────────────────────────────────────
//  三、protected 是什么
// ─────────────────────────────────────────────────────
//
//     private     只有自己这个类能用
//     protected   自己 + **子类**能用
//     public      谁都能用
//
// ⚠ 第 09 课学的 private 在这里会让子类用不了 名字 ⇒ 所以父类的字段用 protected。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：覆写时名字打错（悄悄变成新方法）
// ─────────────────────────────────────────────────────
//
//     class 狗 extends 动物 {
//         public String 叫生() { return "汪"; }     ← 打错一个字
//     }
//
// 你会看到：**编译通过、一声不吭**。只是 旺财.介绍() 打出来是「旺财说……」——
//   因为 介绍() 调的还是父类那个 叫声()，你写的 叫生() 根本没人调。
//
// 为什么：名字不一样就是**另一个方法**，不是覆写。编译器没有理由拦你。
//
// ⇒ 防身：**写方法前先打 @Override。**
//
//     @Override
//     public String 叫生() { ... }
//     错误: 方法不会覆盖或实现超类型的方法
//
//   编译器当场就把打错的字揪出来了。这是 Java 给你的免费保险。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   多态和抽象类 → 第 13 课。接口 → 第 14 课。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  main 里四行，按这个顺序：
//
//     狗 旺财 = new 狗("旺财");
//     猫 咪咪 = new 猫("咪咪");
//
//     System.out.println(旺财.介绍());
//     System.out.println(旺财.叫声());
//     System.out.println(咪咪.叫声());
//     System.out.println(咪咪.介绍());
//
//  期望：
//     旺财说汪
//     汪
//     喵
//     咪咪说喵
//
//  ⚠ 第一行和第四行证明的是同一件事：**介绍() 只写在父类里一次**，
//    但它拼出来的「汪/喵」是各自子类的。这就说明覆写真的生效了。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """
// 第 12 课参考答案：继承 —— extends、super、@Override
//
// ⚠ public class Main 必须是文件里第一个"非注释、非空行"的语句。
//    动物 / 狗 / 猫 都放在 Main 后面，而且都不写 public。

public class Main {
    public static void main(String[] args) {
        狗 旺财 = new 狗("旺财");        // 狗 的构造方法里会 super("旺财")
        猫 咪咪 = new 猫("咪咪");

        // 介绍() 只在父类 动物 里写了一次，但拼出来的是各自子类的叫声
        System.out.println(旺财.介绍());   // 旺财说汪
        System.out.println(旺财.叫声());   // 汪
        System.out.println(咪咪.叫声());   // 喵
        System.out.println(咪咪.介绍());   // 咪咪说喵
    }
}

// ── 下面是我写的类 ──

// 父类：把"所有动物都有的东西"集中写在这里，只写一次
class 动物 {
    private String 名字;          // private：子类也不能直接碰，要走 取名字()

    public 动物(String 名字) {
        this.名字 = 名字;
    }

    public String 取名字() {
        return 名字;
    }

    // 兜底版本：一般动物就叫"……"
    public String 叫声() {
        return "……";
    }

    // ⚠ 关键的一行：这里调用的是 叫声()，不是写死 "……"。
    //    谁调用 介绍()，就用谁的 叫声() —— 这是下一课多态的全部秘密。
    public String 介绍() {
        return 名字 + "说" + 叫声();
    }
}

// 子类：extends 之后，名字、取名字()、介绍() 全都自动有了，一个字都不用重写
class 狗 extends 动物 {
    public 狗(String 名字) {
        super(名字);        // ⚠ 必须是第一条语句：先把父类那部分构造好
    }

    // @Override 让编译器替我核对：名字/参数/返回类型必须和父类那版对得上。
    // ⚠ 万一我参数写错了（变成重载），它会直接报错，而不是静悄悄地不生效。
    @Override
    public String 叫声() {
        return "汪";
    }
}

class 猫 extends 动物 {
    public 猫(String 名字) {
        super(名字);
    }

    @Override
    public String 叫声() {
        return "喵";
    }
}
""";


    [Java检查说明("覆写生效了：介绍里的叫声是各自的")]
    public static JavaCheckResult 检查_1(string 代码)
    {
        var run = JavaCheck.直接运行(代码);
        if (!run.编译通过) return JavaCheck.编译失败说明(run);
        string[] 期望 = {
            "旺财说汪",
            "汪",
            "喵",
            "咪咪说喵",
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

    [Java检查说明("狗 继承了 动物")]
    public static JavaCheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("class 狗 extends 动物"))
            return JavaCheckResult.不过("没找到「class 狗 extends 动物」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("子类构造方法把名字交给了父类")]
    public static JavaCheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("super(名字);"))
            return JavaCheckResult.不过("没找到「super(名字);」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("覆写带了 @Override")]
    public static JavaCheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("@Override"))
            return JavaCheckResult.不过("没找到「@Override」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("子类的 叫声 是 public（没有降低权限）")]
    public static JavaCheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("public String 叫声()"))
            return JavaCheckResult.不过("没找到「public String 叫声()」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("没有自己重复写\"介绍\"（介绍只在父类写一次）")]
    public static JavaCheckResult 检查_6(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains("旺财说\""))
            return JavaCheckResult.不过("出现了不该有的写法：旺财说\"");
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
