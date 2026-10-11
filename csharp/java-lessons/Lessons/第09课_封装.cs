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
// 这一课讲一件事：**字段不许外面直接改，只能通过方法改**。
//
// ─────────────────────────────────────────────────────
//  先介绍：为什么要藏起来
// ─────────────────────────────────────────────────────
//
// 第 08 课的 猫 有一个 `int 年龄;` 字段。**任何人都能写 `甲.年龄 = -5;`**，
// 程序不会拦你 —— 而一只 -5 岁的猫是没意义的。
//
// 封装就一句话：**字段改成 private，只留方法对外**。
// 于是「设置」这件事有了一个入口，你就能在入口处检查。
//
// ─────────────────────────────────────────────────────
//  一、private / public
// ─────────────────────────────────────────────────────
//
//     public class 人 {
//         private String 名字;        ← 外面**看不见、改不了**
//         private int 年龄;
//
//         public String 取名字() { return 名字; }        ← 只读的入口
//         public int 取年龄()   { return 年龄; }
//
//         public void 改名字(String 值) { ... }          ← 改的入口，这里能检查
//         public void 改年龄(int 值)    { ... }
//     }
//
// ⚠ 外面写 `甲.年龄 = 200;` 会**编译不过**：
//
//     错误: 年龄 在 人 中是 private 访问控制
//
//   这正是你要的效果 —— 编译器替你拦住了。
//
// ─────────────────────────────────────────────────────
//  二、在入口处检查（封装真正的价值）
// ─────────────────────────────────────────────────────
//
//     public void 改年龄(int 值) {
//         if (值 < 0 || 值 > 150) { return; }      ← 不合理就直接不改
//         年龄 = 值;
//     }
//
// ⚠ 判断条件用的是 **||**，不是 &&：
//
//     值 < 0 || 值 > 150     ✓ 两个中**有一个**不合理就拒绝
//     值 < 0 && 值 > 150     ✗ 要求**同时**小于 0 又大于 150 —— 永远不成立
//
//   写成 && 等于完全没有检查，而且**编译器一声不吭**。
//
// 名字同理：
//
//     if (值 == null || 值.trim().isEmpty()) { return; }
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：两个名字，一个对象
// ─────────────────────────────────────────────────────
//
//     人 第一个 = new 人("张三", 20);
//     人 第二个 = 第一个;              ← 不是复制！只是起了第二个名字
//     第二个.改年龄(40);
//     System.out.println(第一个.取年龄());     ⇒ 40
//
// 你会看到：改「第二个」，「第一个」也跟着变了。
//
// 为什么：Java 的类类型变量里装的是**引用**（对象在哪），不是对象本身。
//   赋值只是把地址抄了一份 ⇒ 两个名字指向**同一个对象**。
//
// ⚠ 这一点和 C++ 的 struct **相反** —— 那边 `乙 = 甲;` 会真的复制一份。
//   ⇒ 想造第二个独立的对象，必须再 `new` 一次。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   构造方法（怎么在 new 的时候就检查）→ 第 10 课。
//   static 与 final → 第 11 课。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 先写两个类：一个 `人`（private 字段 + 四个方法），一个 `public class Main`。
//    两个类可以放在同一个文件里，但 public 的只能有一个（必须是 Main）。
//
//  main 里按这个顺序写（检查就按这个顺序比对）：
//
//    System.out.println("== 正常工作 ==");
//    // new 一个人，用 setter 改成：名字 "李四"，年龄 30
//    // 打印："名字=" + 取名字()
//    // 打印："年龄=" + 取年龄()
//
//    System.out.println("== 非法值被拒绝 ==");
//    // 试 改年龄(200)  → 超过上界，应该不变
//    // 试 改年龄(-1)   → 小于下界，应该不变
//    // 试 改名字(null) → 应该不变
//    // 打印："年龄还是=" + 取年龄()
//    // 打印："名字还是=" + 取名字()
//
//    System.out.println("== 两个名字，一个对象 ==");
//    // 再声明一个同类型的变量，把它**赋成**第一个人（不要 new）
//    // 通过第二个名字 改年龄(40)
//    // 打印："第一个名字看到=" + 第一个.取年龄()
//
//  判年龄：值 < 0 || 值 > 150 → return
//  判名字：值 == null || 值.trim().isEmpty() → return
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
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
