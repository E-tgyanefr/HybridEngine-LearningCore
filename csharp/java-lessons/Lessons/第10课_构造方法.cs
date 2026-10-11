using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("10", "第 10 课：构造方法重载与 this() —— 造对象的不同入口",
    """
1. 写一个 学生 类，三个字段全部 private：private String 名字; / private int 分数; / private String 班级;
2. 写三个构造方法：学生(名字,分数,班级) 全存；学生(名字,分数) 班级默认"未分班"；学生(名字) 分数默认 0、班级默认"未分班"
3. 后两个构造方法必须用 this(...) 调用第一个，不许自己重复写赋值
4. 写 public String 介绍() 返回 名字 + "/" + 分数 + "/" + 班级；main 里用三个构造方法各造一个对象并打印介绍()
""")]
public static class 第10课
{
    public static string 模板 => """
// 第 10 课：构造方法重载与 this() —— 造对象的不同入口
//
// 先介绍：为什么会有"好几个构造方法"
//
// 上一课我们是这么造对象的：
//
//     人 甲 = new 人();
//     甲.改名字("张三");
//     甲.改年龄(20);
//
// 三行才能得到一个"能用的"对象。如果这个对象有八个字段呢？八行。
// 而且**中间任何一步忘掉，你手上就是一个半成品** ——
// 名字有了、年龄还是 0，后面用起来全是坑。
//
// 构造方法解决的就是这件事：**让对象一诞生就是完整的**。
//
//     人 甲 = new 人("张三", 20);     ← 一行，而且不可能忘
//
// 但事情没这么简单。现实里"造一个学生"的场合有好几种：
//
//     · 报到的时候只知道名字        → 分数、班级还不知道
//     · 考完试了，知道名字和分数    → 班级还不知道
//     · 转班进来，三样都知道        → 齐全
//
// 三种场合，你不可能要求调用者"你自己把不知道的填空了再传进来"。
// 于是就有**三个构造方法**，各自负责一种场合 —— 这就是构造方法重载。
//
// 重载（overload）的规矩很简单：
//   **方法名一样（对构造方法来说就是类名），参数列表不一样。**
//   参数个数不同、类型不同、顺序不同都算不一样。
//   ⚠ 光**返回类型**不同不算重载 —— 而构造方法本来就没有返回类型，
//     所以构造方法的重载只能靠参数区分。
//
// ─────────────────────────────────────────────────────
//  用法与例子：三个构造方法，最"全"的那个干实事
//
//     class 学生 {
//         private String 名字;
//         private int 分数;
//         private String 班级;
//
//         public 学生(String 名字, int 分数, String 班级) {   ← 干实事的那个
//             this.名字 = 名字;
//             this.分数 = 分数;
//             this.班级 = 班级;
//         }
//
//         public 学生(String 名字, int 分数) {
//             this(名字, 分数, "未分班");      ← 调用上面那个，补一个默认班级
//         }
//
//         public 学生(String 名字) {
//             this(名字, 0, "未分班");         ← 补两个默认值
//         }
//     }
//
// 用起来是这样，编译器**按参数找**该用哪个：
//
//     new 学生("小明", 92, "三班")   → 第一个（三个参数）
//     new 学生("小明", 92)           → 第二个（两个参数）
//     new 学生("小明")               → 第三个（一个参数）
//
// `this(...)` 这个写法值得单独说一句：
//   它**不是**"我自己这个对象"（那是 this.分数 里的 this），
//   在这里它是一个**调用另一个构造方法的指令**，
//   意思是"先把那个构造方法跑完，再回来"。
//   所以 new 学生("小明") 实际发生的是：
//       学生("小明") → 学生("小明", 0, "未分班") → 在里面逐字段赋值
//   一条链走到底，赋值只写了一处。
//
// 为什么要这么绕？因为**默认值只应该写在一个地方**。
// 如果三个构造方法各自写一遍 "未分班"，哪天要改成"待定"，
// 你得改三处，漏一处就是一个查不出来的不一致。
//
// ⚠ 还有一件事上一课欠着：构造方法里也要校验
//
//     public 学生(String 名字, int 分数, String 班级) {
//         if (分数 < 0) { 分数 = 0; }        ← 改的是**参数**，不是字段
//         if (分数 > 100) { 分数 = 100; }
//         this.分数 = 分数;                  ← 夹紧之后再存进字段
//     }
//
// 先夹紧**参数**、最后统一存一次。如果写成 this.分数 = 分数; 之后再判断，
// 那判断就白做了。
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：给构造方法写了返回类型 —— 症状是 new 的时候说"找不到构造器"
//
//     public void 学生(String 名字) { ... }      ← 多了个 void
//
// 你会看到：
//     错误: 无法将类 学生中的构造器 学生应用到给定类型;
//     需要: 没有参数
//     找到: String
//     原因: 实际参数列表和形式参数列表长度不同
// **报错里根本看不到 void 两个字** —— 所以这条特别难自己发现。
// 第 08 课讲过：构造方法**没有返回类型，连 void 都不写**。
//
// ⚠ 坑 2：this(...) 没放在第一行
//
//     public 学生(String 名字) {
//         System.out.println("造学生");
//         this(名字, 0, "未分班");       ← ✗
//     }
//
// 你会看到：
//     错误: 对this的调用必须是构造器中的第一个语句
// 为什么：this(...) 的意思是"从头开始按另一个构造方法初始化这个对象"，
// 半路才开始初始化，前面的语句就作用在一个还没构造完的对象上 —— 没意义。
// 想打印点东西，把它挪到 this(...) 的**后面**。
//
// ⚠ 坑 3：以为"我写了带参数的构造方法，无参的 new 学生() 还在"
//
//     学生 甲 = new 学生();        ← ✗ 编译错误
//
// 你会看到：错误: 无法将类 学生中的构造器 学生应用到给定类型; 需要: 没有参数
// 为什么：**只要你写了任何一个构造方法，编译器就不再送你那个默认的无参构造方法了。**
// 它以前存在，是因为编译器看到你一个都没写，怕你没法造对象，替你补了一个。
// 需要无参版本就自己写一个：public 学生() { this("无名"); }
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   main 里就三件事，按顺序：
//
//     学生 甲 = new 学生("小明");                  ← 一个参数：分数 0、班级 未分班
//     学生 乙 = new 学生("小红", 92);              ← 两个参数：班级 未分班
//     学生 丙 = new 学生("小刚", 88, "三班");      ← 三个参数：全都给
//
//     System.out.println(甲.介绍());
//     System.out.println(乙.介绍());
//     System.out.println(丙.介绍());
//
// 介绍() 返回"名字/分数/班级"：
//     小明/0/未分班
//     小红/92/未分班
//     小刚/88/三班
//
// ⚠ 第二个构造方法里**唯一**的语句应该就是这一行：
//     this(名字, 分数, "未分班");
//   不要再写一遍 this.名字 = 名字; —— 那是重复劳动，也是这一课要避免的东西。

// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/java-lessons 检查 10
//
//   看这一课要干什么：dotnet run --project csharp/java-lessons 10
//   看答案（先自己写一遍）：dotnet run --project csharp/java-lessons 答案 10
""";

    public static string 参考答案 => """
// 第 10 课参考答案：构造方法重载与 this()
//
// ⚠ public class Main 必须是文件里第一个"非注释、非空行"的语句。
//    学生 类放在 Main 后面，而且不能写 public（一个文件只能有一个 public 类）。
//
// 三个构造方法的组织方式叫"构造器链"：最全的那个干实事，
// 其余两个用 this(...) 把缺的参数补齐了再交给它。
// 好处是**赋值只写一次** —— 以后要改默认班级，只改一处。

public class Main {
    public static void main(String[] args) {
        // 三种"造学生"的场合，各自走一个构造方法
        学生 甲 = new 学生("小明");                  // → this("小明", 0, "未分班")
        学生 乙 = new 学生("小红", 92);              // → this("小红", 92, "未分班")
        学生 丙 = new 学生("小刚", 88, "三班");      // → 直接赋值

        System.out.println(甲.介绍());
        System.out.println(乙.介绍());
        System.out.println(丙.介绍());
    }
}

// ── 下面是我写的类 ──
class 学生 {
    private String 名字;
    private int 分数;
    private String 班级;

    // ① 最全的构造方法：真正往字段里写值的地方，只此一处
    public 学生(String 名字, int 分数, String 班级) {
        // 名字为空就给个兜底值（构造方法也是入口，校验不能省）
        if (名字 == null || 名字.trim().isEmpty()) {
            this.名字 = "无名";
        } else {
            this.名字 = 名字;
        }
        // ⚠ 先夹紧**参数**，最后统一存一次。
        //    如果写成 this.分数 = 分数; 之后再判断，那判断就白做了。
        if (分数 < 0) {
            分数 = 0;
        }
        if (分数 > 100) {
            分数 = 100;
        }
        this.分数 = 分数;

        if (班级 == null || 班级.trim().isEmpty()) {
            this.班级 = "未分班";
        } else {
            this.班级 = 班级;
        }
    }

    // ② 两个参数：班级用默认值，剩下的交给 ①
    public 学生(String 名字, int 分数) {
        this(名字, 分数, "未分班");      // ⚠ this(...) 必须是**第一条语句**
    }

    // ③ 一个参数：分数和班级都用默认值，剩下的交给 ①
    public 学生(String 名字) {
        this(名字, 0, "未分班");
    }

    public String 介绍() {
        return 名字 + "/" + 分数 + "/" + 班级;
    }
}
""";


    [Java检查说明("三个构造方法都能造出对象，字段值对")]
    public static JavaCheckResult 检查_1(string 代码)
    {
        var run = JavaCheck.直接运行(代码);
        if (!run.编译通过) return JavaCheck.编译失败说明(run);
        string[] 期望 = {
            "小明/0/未分班",
            "小红/92/未分班",
            "小刚/88/三班",
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

    [Java检查说明("三个构造方法都写了（参数个数各不同）")]
    public static JavaCheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("public 学生(String 名字, int 分数)"))
            return JavaCheckResult.不过("没找到「public 学生(String 名字, int 分数)」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("两个参数的构造方法用 this(...) 转交，没有自己重复赋值")]
    public static JavaCheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("this(名字, 分数, \"未分班\");"))
            return JavaCheckResult.不过("没找到「this(名字, 分数, \"未分班\");」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("一个参数的构造方法也是转交")]
    public static JavaCheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("this(名字, 0, \"未分班\");"))
            return JavaCheckResult.不过("没找到「this(名字, 0, \"未分班\");」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("构造方法没有写返回类型（void 也不行）")]
    public static JavaCheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains("void 学生("))
            return JavaCheckResult.不过("出现了不该有的写法：void 学生(");
        return JavaCheckResult.过();
    }

    [Java检查说明("三个字段都是 private")]
    public static JavaCheckResult 检查_6(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("private int 分数;"))
            return JavaCheckResult.不过("没找到「private int 分数;」—— 这一课要求用到它。");
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
