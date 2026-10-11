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
// 先介绍：两个类有八成内容是一样的，怎么办
//
// 假设你写了一个 狗 类、一个 猫 类，写完会发现它们是这样的：
//
//     class 狗 {
//         private String 名字;
//         public String 取名字() { return 名字; }
//         public String 介绍() { return 名字 + "说" + 叫声(); }
//         public String 叫声() { return "汪"; }
//     }
//     class 猫 {
//         private String 名字;                        ← 一模一样
//         public String 取名字() { return 名字; }      ← 一模一样
//         public String 介绍() { ... }                ← 一模一样
//         public String 叫声() { return "喵"; }        ← 只有这里不一样
//     }
//
// 四样东西里三样完全相同，只有 叫声 不同。
// 这不是"差不多"，这是**同一段代码抄了两遍**。抄两遍的代价是：
//   ① 以后 介绍 的格式要改（比如"说"改成"叫道"），你得改两个地方，
//      有了十种动物就改十处 —— 漏一个就是一个不一致的 bug。
//   ② 名字 这个字段在两个类里是**两套**，你没法写一个方法同时收狗和猫。
//
// 继承就是为这件事准备的：**把共同的部分写一次，放在父类里，子类去拿。**
//
//     class 动物 { ...名字、取名字、介绍、叫声... }        ← 共同的部分，写一次
//     class 狗 extends 动物 { ...只要写 叫声... }          ← 拿过来用
//     class 猫 extends 动物 { ...只要写 叫声... }
//
// `extends` 读作"继承自"，`狗 extends 动物` 的意思是
// **"狗 是一种 动物，动物 有的 狗 全都有，另外 狗 还能有自己的东西"**。
//
// ─────────────────────────────────────────────────────
//  用法与例子：三个关键字
// ─────────────────────────────────────────────────────
//
// ① extends —— 建立父子关系
//
//     class 狗 extends 动物 {
//         // 这里什么都不写，狗 就已经有 名字、取名字()、介绍()、叫声() 了
//     }
//
//   拿过来的东西**不用再声明一遍**。子类自动拥有父类的字段和方法
//   （private 字段也在，只是子类不能直接碰，得走父类的 getter —— 见坑 2）。
//
// ② super —— 指"父类那一份"
//
//   父类的构造方法不会被继承（构造方法是"造这个类"的步骤，不能照搬），
//   所以子类的构造方法**必须自己把父类需要的值交上去**：
//
//     public 狗(String 名字) {
//         super(名字);            ← 调用父类的构造方法：动物(String 名字)
//     }
//
//   ⚠ super(...) 必须是子类构造方法的**第一条语句** —— 和上一课的 this(...) 一样。
//     道理也一样：**父类的部分要先构造好**，子类的部分才有地方放。
//
//   `super` 还有第二种用法，不带括号：
//     super.叫声()        ← 明确地说"我要调父类那一版的 叫声"
//
// ③ @Override —— 覆写
//
//   子类里写一个**和父类同名、同参数、同返回类型**的方法，就把父类那版盖掉了：
//
//     class 狗 extends 动物 {
//         @Override
//         public String 叫声() { return "汪"; }
//     }
//
//   @Override 这个注解**不影响运行**，它是写给编译器看的：
//   它说"我打算覆写父类的某个方法，你帮我核对一下"。
//   **对不上它就报错**，这正是它的价值（见坑 1）。
//
//   覆写的三条硬要求：
//     · 方法名和参数列表必须**完全一样**（有一个字不同就变成另一个方法了）
//     · 返回类型必须一样（或者更"窄"）
//     · 访问权限**不能变小**：父类是 public，子类也得是 public
//
// ⚠ 一个值得先记住的写法：介绍() 里调用的是 `叫声()`，**不是**写死的"……"。
//   父类的方法里调另一个方法时，实际执行的是**子类覆写的那一版**。
//   于是 介绍() 只写了一次，狗和猫各自说出来的是"汪"和"喵"。
//   （这是下一课"多态"的全部秘密，今天先记住这个写法。）
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：覆写时参数写错 —— 症状是"它怎么还是父类的行为"
//
//     class 狗 extends 动物 {
//         public String 叫声(String 后缀) { return "汪" + 后缀; }     ← 多了个参数！
//     }
//     System.out.println(狗.叫声());        ⇒ ……        ← 你以为会打"汪"
//
// 你会看到：明明写了 叫声，调出来还是父类那个"……"，而且**不报错**。
// 为什么：参数不一样就是**另一个方法**了（叫重载，第 10 课学过），
// 它跟父类的 叫声() 并存，谁也没盖住谁。调 叫声() 时找到的还是父类那版。
//
// 这就是 @Override 存在的意义：加上它，编译器会替你核对 ——
// 会直接报：错误: 方法不会覆盖或实现超类型的方法
// **一句话：只要你打算覆写，就先打 @Override。**
//
// ⚠ 坑 2：想在子类里直接碰父类的 private 字段
//
//     class 狗 extends 动物 {
//         public String 打招呼() { return "我是" + 名字; }      ← ✗
//     }
//
// 你会看到：错误: 名字 在 动物 中是 private 访问控制
// 为什么：**private 是"只有本类"，子类也算"外面"。**
// 继承拿过来的是"存在"，不是"看得见"。
// 想读就走父类给的 getter：`return "我是" + 取名字();`
// 什么时候用 protected？—— 当你确实希望子类直接碰这个字段时。
// 但**大多数时候更好的答案是给个 getter**，字段仍然 private。
//
// ⚠ 坑 3：覆写时把访问权限改小了
//
//     class 狗 extends 动物 {
//         String 叫声() { return "汪"; }        ← 不写 public
//     }
//
// 你会看到：
//     错误: 狗中的叫声()无法覆盖动物中的叫声()
//     正在尝试分配更低的访问权限; 以前为public
// 为什么：如果允许缩小权限，就会出现荒谬的局面 ——
// 一个 动物 类型的变量（外面能调 叫声）实际指向狗，一调就"没权限"了。
// 记法：**覆写时权限只能不变或者变大。** 照抄父类的 public 就行。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   main 里四行，按这个顺序：
//
//     狗 旺财 = new 狗("旺财");
//     猫 咪咪 = new 猫("咪咪");
//
//     System.out.println(旺财.介绍());
//     System.out.println(旺财.叫声());
//     System.out.println(咪咪.叫声());
//     System.out.println(咪咪.介绍());
//
// 期望：
//     旺财说汪
//     汪
//     喵
//     咪咪说喵
//
// ⚠ 第一行和第四行证明的是同一件事：**介绍() 只写在父类里一次**，
//   但它拼出来的"汪/喵"是各自子类的。这就说明覆写真的生效了。
//   如果两行都打"……"，回去看坑 1。

// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/java-lessons 检查 12
//
//   看这一课要干什么：dotnet run --project csharp/java-lessons 12
//   看答案（先自己写一遍）：dotnet run --project csharp/java-lessons 答案 12
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
