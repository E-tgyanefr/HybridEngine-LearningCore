using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("09", "第 09 课：封装与访问控制 —— 把字段关进盒子里",
    """
1. 写一个 人 类，字段必须 private：private String 名字; / private int 年龄;
2. 写四个方法：取名字() / 取年龄() / 改名字(String) / 改年龄(int)
3. 改年龄 要校验：小于 0 或大于 150 就不修改；改名字 要校验：null 或去掉空格后为空就不修改
4. main 里依次打印三组：正常工作、非法值被拒绝、两个名字一个对象
""")]
public static class 第09课
{
    public static string 模板 => """
// 第 09 课：封装与访问控制 —— 把字段关进盒子里
//
// 先介绍：为什么要"封装"
//
// 现在你的字段是长这样的：
//
//     class 人 {
//         String 名字;
//         int 年龄;
//     }
//
// 用起来：
//
//     人 甲 = new 人();
//     甲.年龄 = -5;          ← 编译通过！跑起来也不报错！
//     甲.年龄 = 99999;       ← 也通过
//
// 问题就在这：**你写 人 这个类的时候，脑子里有一条规矩** ——
// "年龄必须是 0 到 150 之间的整数"。可这条规矩没地方写，
// 它只活在你脑子里。任何人拿到 人 这个类，都能把 年龄 改成 -5，
// 你的程序就此带着一个不可能的数据一路跑下去，等你发现的时候
// 已经是"某个人的年龄是 -5，所以他免票"这种莫名其妙的 bug。
//
// 那能不能在"每次用到年龄"的地方都写一遍检查？
// 不能 —— 你不知道以后有多少个地方会用到它。而且总有人忘。
//
// 封装的思路是：**把规矩焊死在数据旁边**。
//   ① 字段不许别人直接碰  → private
//   ② 想读，走 取年龄()   → getter
//   ③ 想改，走 改年龄(v)  → setter，**检查写在里面**
//
// 这样"年龄永远是合法的"就成了这个类**自己保证**的事，
// 而不是"每个用它的人都记得要检查"。
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
// private 的意思很直白：**只有这个类自己看得见**。
//
//     class 人 {
//         private int 年龄;          ← 外面看不见了
//
//         public int 取年龄() {      ← 给外面开一扇窗：只能看
//             return 年龄;
//         }
//
//         public void 改年龄(int 值) {   ← 给外面开一扇门：检查了才放进来
//             if (值 < 0 || 值 > 150) {
//                 return;            ← 非法值？当没听见
//             }
//             this.年龄 = 值;
//         }
//     }
//
// 外面的世界随之改变：
//
//     甲.年龄 = -5;         // ✗ 编译错误：年龄 在 人 中是 private 访问控制
//     甲.改年龄(-5);        // ✓ 编译通过 —— 但值被拒绝了，年龄没变
//     甲.改年龄(30);        // ✓ 改成功
//     int 岁 = 甲.取年龄();  // ✓ 读得到
//
// ⚠ 注意第二行和第三行的区别，这是封装真正的价值：
//   **它不是"不让你改"，而是"只能按规矩改"。**
//   非法的调用不再是一个错误，而是一个**被忽略的请求**。
//
// 套路的形状是固定的：名字 → 取名字()/改名字(String)，年龄 → 取年龄()/改年龄(int)。
// 方法名怎么起随你（很多书用 getName / setAge），关键是**成对出现**：一个读、一个写。
//
// ─────────────────────────────────────────────────────
//  Java 的四种访问修饰符（按可见范围从小到大）
// ─────────────────────────────────────────────────────
//
//     private      只有**本类**里面能碰
//     （不写）      同一个**包**里能碰        ← 什么都不写叫"包访问权限"
//     protected    同包 + **子类**能碰（下一课讲继承时会用到）
//     public       谁都能碰
//
// 两条要立刻记住的：
//
//   ① **不写修饰符 = 包访问权限**，不是 private！
//      你以前写的 `String 名字;` 就是这个级别，所以同一个文件里别的类也能改它。
//
//   ② 字段一律 private，方法一律 public，这条"默认搭配"能覆盖九成情况。
//
// ⚠ 一个文件里能放好几个类，但 public 的**只能有一个**，而且必须叫 Main。
//   所以下面写的 人 不能加 public —— 加上会报：
//       错误: 类 人 是公共的, 应在名为 人.java 的文件中声明
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：setter 里忘了 this —— 症状是"字段永远是 0 / null"
//
//     public void 改年龄(int 年龄) {      ← 参数也叫 年龄
//         年龄 = 年龄;                   ← 把参数赋给参数自己
//     }
//
// 你会看到：调用 甲.改年龄(30) 之后 甲.取年龄() 打印出来是 **0**，
// 一次都没改成功，也不报任何错。
// 为什么：参数名和字段名一样时，**光写名字指的是参数**（离得近的赢）。
// 要真进到字段里必须写 this.年龄 = 年龄;
// 更省事的办法是给参数换个名字（参考答案用的就是这个）：
//     public void 改年龄(int 值) { ... 年龄 = 值; }   ← 一眼知道谁是谁
//
// ⚠ 坑 2：以为 getter 返回的是"复印件"
//
//     int 岁 = 甲.取年龄();     ← int 是基本类型，确实是拷贝，改 岁 不影响 甲
//     String 名 = 甲.取名字();  ← String 不可变，也安全
//
// 但换成对象类型就危险了（以后会撞上）：如果字段是 int[] 或 ArrayList，
// getter 直接把那个**引用**交出去，外面 列表.add(...) 就等于绕过 setter 改了字段。
// 症状：明明没人调 setter，字段却变了。
// 记住一句话：**getter 交出引用 = 封装破了个洞。**
//
// ⚠ 坑 3：只给 setter 加了校验，构造方法没加
//
//     new 人("张三", -5);      ← 绕过了所有 setter，直接造出一个非法对象
//
// 你会看到：用 setter 时校验好好的，可对象刚 new 出来就已经是坏的。
// 为什么：构造方法也是"往字段里写值"的入口，封装要封的是**所有入口**。
// 这一课我们没写构造方法，但下一课一学就要记得把同样的检查搬进去。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   main 里按这个顺序写（检查就按这个顺序比对）：
//
//     System.out.println("== 正常工作 ==");
//     // new 一个人，用 setter 改成：名字 "李四"，年龄 30
//     // 打印："名字=" + 取名字()
//     // 打印："年龄=" + 取年龄()
//
//     System.out.println("== 非法值被拒绝 ==");
//     // 试 改年龄(200)  → 超过上界，应该不变
//     // 试 改年龄(-1)   → 小于下界，应该不变
//     // 试 改名字(null) → 应该不变
//     // 打印："年龄还是=" + 取年龄()
//     // 打印："名字还是=" + 取名字()
//
//     System.out.println("== 两个名字，一个对象 ==");
//     // 再声明一个同类型的变量，把它**赋成**第一个人（不要 new）
//     // 通过第二个名字 改年龄(40)
//     // 打印："第一个名字看到=" + 第一个.取年龄()
//
// 判年龄：值 < 0 || 值 > 150 → return
// 判名字：值 == null || 值.trim().isEmpty() → return
// ⚠ 注意是 || 不是 &&：两个条件中一个就拒绝。
//   写成 && 会变成"两个都满足才拒绝"，等于几乎没检查。

// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/java-lessons 检查 09
//
//   看这一课要干什么：dotnet run --project csharp/java-lessons 09
//   看答案（先自己写一遍）：dotnet run --project csharp/java-lessons 答案 09
""";

    public static string 参考答案 => """
// 第 09 课参考答案：封装与访问控制
//
// 一个要点：一个 .java 文件里可以有多个类，但 public 的只能有一个，
// 而且必须是 Main（文件名是 Main.java）。所以 人 不写 public。
//
// ⚠ public class Main 必须是文件里第一个"非注释、非空行"的语句。
//    所以把人 放在后面，不要放在 Main 上面。

public class Main {
    public static void main(String[] args) {
        // ① 正常工作：setter 把值放进去了
        System.out.println("== 正常工作 ==");
        人 甲 = new 人();
        甲.改名字("张三");
        甲.改年龄(20);
        甲.改名字("李四");              // 覆盖一次，证明"改"真的生效
        甲.改年龄(30);
        System.out.println("名字=" + 甲.取名字());
        System.out.println("年龄=" + 甲.取年龄());

        // ② 非法值被拒绝：三次都该"什么都没发生"
        System.out.println("== 非法值被拒绝 ==");
        甲.改年龄(200);                 // 超过 150 → setter 直接 return
        甲.改年龄(-1);                  // 小于 0   → 直接 return
        甲.改名字(null);                // null     → 直接 return
        System.out.println("年龄还是=" + 甲.取年龄());
        System.out.println("名字还是=" + 甲.取名字());

        // ③ 两个名字，一个对象：赋值不复制对象，只多起了一个名字
        System.out.println("== 两个名字，一个对象 ==");
        人 乙 = 甲;                     // ⚠ 这不是 new —— 两个名字指同一块地方
        乙.改年龄(40);                  // 通过 乙 改
        System.out.println("第一个名字看到=" + 甲.取年龄());   // 甲 看到的也变了
    }
}

// ── 下面是我写的类 ──
class 人 {
    private String 名字;      // private = 只有 人 这个类自己看得见
    private int 年龄;

    // getter：只读，把字段的值交出去
    public String 取名字() {
        return 名字;
    }

    public int 取年龄() {
        return 年龄;
    }

    // setter：写之前先过一遍规矩
    public void 改名字(String 值) {
        // ⚠ null 和"只有空格"都要拦掉。先判 null 再调 trim()，
        //   写成 值.trim() == null 那是在 null 上调用方法 → 运行时崩。
        if (值 == null || 值.trim().isEmpty()) {
            return;              // 非法 → 当没听见
        }
        this.名字 = 值;          // ⚠ this.名字 才是字段；右边的 值 是参数
    }

    public void 改年龄(int 值) {
        // ⚠ 用 || 不是 &&：两个边界中任意一个越界就拒绝。
        //   写成 && 的话"小于0 且 大于150"永远不成立 —— 等于没检查。
        if (值 < 0 || 值 > 150) {
            return;
        }
        this.年龄 = 值;
    }
}
""";


    [Java检查说明("非法年龄不动、合法年龄能改 ")]
    public static JavaCheckResult 检查_1(string 代码)
    {
        var run = JavaCheck.直接运行(代码);
        if (!run.编译通过) return JavaCheck.编译失败说明(run);
        string[] 期望 = {
            "== 正常工作 ==",
            "名字=李四",
            "年龄=30",
            "== 非法值被拒绝 ==",
            "年龄还是=30",
            "名字还是=李四",
            "== 两个名字，一个对象 ==",
            "第一个名字看到=40",
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

    [Java检查说明("setter 里用了 this（不是自己赋给自己）")]
    public static JavaCheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("this.年龄 ="))
            return JavaCheckResult.不过("没找到「this.年龄 =」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("两个字段都声明成了 private")]
    public static JavaCheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("private int 年龄;"))
            return JavaCheckResult.不过("没找到「private int 年龄;」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("getter 写成了返回值的方法（不是 void、不是打印）")]
    public static JavaCheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("public int 取年龄()"))
            return JavaCheckResult.不过("没找到「public int 取年龄()」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("setter 的签名带一个参数、返回 void")]
    public static JavaCheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("public void 改年龄(int"))
            return JavaCheckResult.不过("没找到「public void 改年龄(int」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("非法年龄的两个边界都检查了")]
    public static JavaCheckResult 检查_6(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("值 > 150"))
            return JavaCheckResult.不过("没找到「值 > 150」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("没把 public 写到别的类上（只有 Main 是 public）")]
    public static JavaCheckResult 检查_7(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains("public class 人"))
            return JavaCheckResult.不过("出现了不该有的写法：public class 人");
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
