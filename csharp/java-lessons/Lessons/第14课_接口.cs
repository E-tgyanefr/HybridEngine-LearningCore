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
//
// 先介绍：只继承一个，可我就是"两样都是"
//
// 上一课结尾留了个问题：**Java 只能 extends 一个类**。
// 可现实里一个东西常常同时是好几样东西：
//
//     一个圆    是形状、也是可命名的
//     一部手机  能打电话、也能当相机
//     一个学生  是人、也是可比较的（能跟别人比大小）
//
// 硬要你用继承表达"手机 既 extends 电话 又 extends 相机" —— 做不到。
// 而且更根本的问题是：**"能拍照"这件事不该靠血缘来表达。**
// 相机能拍照，是因为它是相机；手机能拍照，跟它爹是谁没关系。
// "能拍照"是一种**能力/约定**，不是一种**出身**。
//
// 接口就是给"能力/约定"用的：
//
//     interface 形状 {
//         double 面积();                 ← 只有签名，末尾分号
//     }
//
// 念法是"**凡是实现 形状 的类，都保证有 面积()**"。
// 它是一个**契约**：我不管你是什么、你爹是谁，
// 你只要签了这个约，我就知道你一定能算面积 —— 于是我就敢调。
//
// 写"签合同"的关键字不是 extends，而是 **implements**：
//
//     class 圆 implements 形状, 可命名 { ... }
//                                  ↑ 逗号隔开，想实现几个就几个
//
// 一个类 **extends 只能一个，implements 可以很多个**。
// 这是接口最重要的本事，也是它存在的第一理由。
//
// ─────────────────────────────────────────────────────
//  用法与例子：接口长什么样
// ─────────────────────────────────────────────────────
//
// ① 接口声明
//
//     interface 形状 {
//         double 面积();                          ← 抽象方法（可省 public abstract）
//         default String 说明() {                  ← default 方法：**有方法体**
//             return "面积=" + Math.round(面积());
//         }
//     }
//
//   几个容易记混的点：
//     · 接口**不是类**：写 interface，不写 class。它不能被 new。
//     · 方法默认就是 public 的，而且**必须**是 public。
//       所以 `double 面积();` 就等于 `public abstract double 面积();`。
//     · default 方法是个例外：**它带方法体**。
//       为什么允许 —— 因为要"加一个新方法又不弄坏所有已经实现的类"。
//       这一课用它，是为了让 说明() 这套拼字符串的活儿只写一次。
//     · 接口可以**多继承接口**：`interface A extends B, C` —— 这是允许的，
//       因为接口没有字段、没有实现细节，不会撞车。
//
// ② 实现
//
//     class 圆 implements 形状, 可命名 {
//         private double 半径;
//         public 圆(double 半径) { this.半径 = 半径; }
//         @Override public double 面积() { return Math.PI * 半径 * 半径; }
//         @Override public String 名字() { return "半径为" + 半径 + "的圆"; }
//     }
//
//   ⚠ 实现接口的方法**必须是 public** —— 接口里的方法本来就是 public，
//     实现时权限不能变小（和上一课覆写的规矩是同一条）。
//     忘了写 public 会报：正在尝试分配更低的访问权限; 以前为public
//
// ③ 用接口当类型
//
//     形状 甲 = new 圆(2);         ← 和上一课的向上转型一模一样
//     可命名 乙 = new 圆(2);
//
//     System.out.println(甲.面积());        ⇒ 12.566370614359172
//     System.out.println(乙.名字());        ⇒ 半径为2.0的圆
//
//   注意 甲 和 乙 是**同一个类的两个视角**：
//   通过 甲 你只看得见 面积/说明（形状 这个契约里有的），
//   通过 乙 你只看得见 名字。
//   ⚠ 所以 `甲.名字()` 编译不过 —— 形状 这个接口里没有 名字。
//
// ④ 接口和抽象类怎么选
//
//     问题                          用哪个
//     ────────────────────────────────────────────────
//     子类共用的**字段**和构造方法    抽象类（接口没有实例字段）
//     表达"**是一种**"（狗是动物）    抽象类
//     表达"**能做**/遵守某个约定"     接口
//     一个类要同时具备好几种能力      接口（可以 implements 很多个）
//
//   一句话：**is-a 用抽象类，can-do 用接口。**
//   拿不准的时候用接口 —— 它更松，不会把你的继承名额（只有 1 个）用掉。
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：在接口里写实例字段
//
//     interface 形状 {
//         private double 半径;             ← ✗ 编译错误
//     }
//
// 你会看到：错误: 接口不能有私有字段 / 修饰符 private 不合法
// 为什么：接口是"约定"，约定不能被某个实现改来改去。
// 接口里**只能有常量**（`int 最大 = 100;` 会被自动当成
// `public static final int 最大 = 100;`，因为接口里没有"实例"可言）。
// 想存数据就得在**实现类**里存 —— 这就是 圆 里那个 半径 的意义。
// ⚠ 顺带一个后果：接口里的字段全是 static final，所以它**全类共用一份**，
//   千万别想着在接口里放"每个对象不一样"的值。
//
// ⚠ 坑 2：忘了实现接口里的某个方法
//
// 你会看到：错误: 圆不是抽象的, 并且未覆盖可命名中的抽象方法名字()
// 为什么：签了合同就得交作业 —— 和上一课抽象方法一模一样的道理。
// ⚠ 注意报错会**指名道姓说是哪个接口的哪个方法**（这里写的是 可命名 中的 名字），
//   所以这条报错非常好用：它直接告诉你缺哪一份作业。
//
// ⚠ 坑 3：以为"接口能 new"
//
//     形状 甲 = new 形状();              // ✗
// 你会看到：错误: 形状是抽象的; 无法实例化
// 为什么：接口连方法体都没有（default 除外），new 出来一个"面积()"没法算的东西？
// 接口只能当**类型**：变量类型、参数类型、数组元素类型、返回值类型。
//
// ⚠ 坑 4：实现方法时忘了 public
//
//     class 圆 implements 形状 {
//         double 面积() { return 1; }       ← 不写 public
//     }
//
// 你会看到：
//     错误: 圆中的面积()无法实现形状中的面积()
//     正在尝试分配更低的访问权限; 以前为public
// **实现接口的方法一律写 public。** 这条和上一课覆写的规矩是同一条。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   main 里五步，按顺序：
//
//     圆 盘 = new 圆(2);
//
//     // ① 用 圆 自己的类型调
//     System.out.println(盘.面积());              → 12.566370614359172
//     System.out.println(盘.说明());              → 面积=13
//
//     // ② 换个视角：把同一个对象看成"形状"
//     形状 当形状看 = 盘;
//     System.out.println(当形状看.说明());         → 面积=13
//
//     // ③ 再换个视角：看成"可命名"
//     可命名 当名字看 = 盘;
//     System.out.println(当名字看.名字());         → 半径为2.0的圆
//
//     // ④ 同一个对象，两个不同视角的变量指着它
//     System.out.println(当形状看 == 当名字看);    → true
//
// ⚠ 第一行那个长小数不用自己算 —— 直接 println(盘.面积()) 打出来的就是它。
//   （Java 打印 double 只打有效位，不会像 C 的 %f 那样补一堆 0。）
// ⚠ ④ 那行打印的是 true —— 想清楚为什么：
//   Java 的类类型变量里装的是**引用**（第 08 课讲过），
//   两个变量里装的是同一个地址，所以 == 为 true。
//   换个视角不等于换了对象。
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
