using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("16", "第 16 课：集合与泛型入门 —— 数组装不下的时候",
    """
1. 写泛型类 盒子<T>：private T 里面; / public void 放(T 值); / public T 取()
2. main 里：ArrayList<String> 名单 加三个名字，打印 size()、get(1)、contains("小红")
3. main 里：HashMap<String,Integer> 分数表 放两条，打印 get("小明") 和 get("小刚")
4. 用 盒子<String> 和 盒子<Integer> 各装一个值并打印；写泛型方法 <T> void 打印两次(T 值) 并调用两次
5. 用增强 for 遍历 名单，打印 "第1个:小明" 这样的行
""")]
public static class 第16课
{
    public static string 模板 => """
// 第 16 课：集合与泛型入门 —— 数组装不下的时候
//
// 先介绍：数组的两个硬伤
//
// 第 07 课学的数组有三件事做不到：
//
//     ① **长度固定。** int[] 数们 = new int[3]; 之后想塞第 4 个？没门。
//        现实里"名单"是会长大的 —— 今天三个学生，明天转来一个。
//        你能做的只有"猜一个大一点的数"，猜小了不够用，猜大了浪费。
//
//     ② **没有现成的方法。** 想知道"名单里有没有小红"，数组没有这个功能，
//        你得自己写个 for 循环一个个比。删掉一个人？自己搬后面的元素。
//
//     ③ **类型要么写死，要么全丢。** 数组很安全（String[] 里只能放 String），
//        但一旦你想写"能装任何东西的容器"，就只能退回 Object[] ——
//        然后每次拿出来都要强转，转错了运行时才崩。
//
// 集合（collections）就是补这三个洞的：
//
//     ArrayList<String> 名单 = new ArrayList<>();
//     名单.add("小明");                ← 想加几个加几个，长度自己长
//     名单.contains("小红")            ← 有没有，直接问，不用自己写循环
//     名单.get(1)                      ← 拿第 2 个
//
// 尖括号里的 <String> 叫**泛型参数**，意思是"这个名单里装的是 String"。
// 有了它：
//     · 放别的东西进去 → **编译期**就报错（而不是运行时才崩）
//     · 拿出来就是 String → **不用强转**
//
//     名单.add(123);                              // ✗ 编译错误：不兼容的类型
//     String 第一个 = 名单.get(0);                 // ✓ 直接就是 String
//
// 集合的家族要认一下名字（这一课只用两个）：
//     List    有序、可以重复、按下标取      实现类 ArrayList
//     Set     不重复                       实现类 HashSet
//     Map     键 → 值，按键取值             实现类 HashMap
//
// ⚠ 注意 List / Map 是**接口**（上一课刚学的东西！）。
//   写 `List<String> 名单 = new ArrayList<>();` 才是老手写法：
//   左边是契约，右边是具体实现。这一课为了少一层概念，直接写 ArrayList，也没问题。
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
// ① ArrayList：会自己变长的列表
//
//     ArrayList<String> 名单 = new ArrayList<>();
//     名单.add("小明");              ← 加到末尾
//     名单.add("小红");
//     名单.add("小刚");
//
//     名单.size()                   ⇒ 3         ⚠ 是**方法**（有括号）
//     名单.get(0)                   ⇒ 小明      ⚠ 下标从 0 开始，和数组一样
//     名单.get(2)                   ⇒ 小刚
//     名单.contains("小红")          ⇒ true
//     名单.remove("小红")            ← 按**内容**删（后面元素自动往前挪）
//     名单.remove(0)                ← 按**下标**删 —— 同名两个方法，看参数类型区分
//
//   ⚠ size() 和数组的 length 千万别混：
//         数组：   数们.length        ← 没有括号，它是字段
//         ArrayList：名单.size()      ← 有括号，它是方法
//     写 数们.length() 或 名单.size 都是编译错误。这是本课第一个必考的点。
//
//   遍历（增强 for，和第 07 课数组的写法一模一样）：
//
//     for (String 名字 : 名单) {
//         System.out.println(名字);
//     }
//
//   ⚠ 需要自己数编号（"第1个"）就得另外维护一个计数器 ——
//     增强 for 不给你下标。
//
// ② HashMap：键 → 值
//
//     HashMap<String, Integer> 分数表 = new HashMap<>();
//     分数表.put("小明", 92);        ← 存：键 → 值
//     分数表.put("小红", 88);
//
//     分数表.get("小明")             ⇒ 92
//     分数表.get("小刚")             ⇒ **null**      ← ⚠ 键不存在，不是 0！
//     分数表.containsKey("小明")      ⇒ true
//     分数表.size()                 ⇒ 2
//
//   ⚠ 两个尖括号**不能省**：HashMap<String, Integer>，一个是键的类型，
//     一个是值的类型。键必须是对象类型 —— 所以写 Integer，不能写 int。
//
//   ⚠ 键不存在时 get 返回 null，这是这一课最容易踩的一脚。
//     如果你写 int 分数 = 分数表.get("小刚"); 会**当场崩**：
//         Exception in thread "main" java.lang.NullPointerException
//     因为 Java 要把 null 拆回 int（拆箱），拆不了。
//     安全写法是先问一句：if (分数表.containsKey("小刚")) { ... }
//
// ③ 泛型类：自己造一个能装任何东西的盒子
//
//     class 盒子<T> {
//         private T 里面;                  ← T 是"占位符"：用的时候才定
//         public void 放(T 值) { this.里面 = 值; }
//         public T 取() { return 里面; }
//     }
//
//   用的时候把 T 填上：
//
//     盒子<String> 名盒 = new 盒子<>();      ← T = String
//     名盒.放("Java");
//     String s = 名盒.取();                  ← 拿出来直接是 String，不用强转
//
//     盒子<Integer> 数盒 = new 盒子<>();     ← T = Integer。⚠ 不能写 <int>
//     数盒.放(300);
//
//   ⚠ 泛型里**不能用基本类型**：<int> <double> <char> 都是编译错误，
//     要写它们的包装类 <Integer> <Double> <Character>。
//     （为什么：泛型是靠"擦除"实现的，T 最终会被当成 Object 处理，
//       而 int 不是 Object。这条现在只要记住结论就行。）
//
//   ⚠ `new 盒子<>()` 里那对空尖括号是"类型推断"：右边不用重复写一遍 String。
//
// ④ 泛型方法：<T> 写在返回类型**前面**
//
//     public static <T> void 打印两次(T 值) {
//         System.out.println(值);
//         System.out.println(值);
//     }
//
//   调用的时候不用指定 T，编译器看你传什么就定什么：
//     打印两次("泛型");      → T 推断成 String
//     打印两次(7);           → T 推断成 Integer（7 自动装箱）
//
//   ⚠ 位置容易写错：<T> 必须在**返回类型之前**。
//         public static <T> void 打印两次(T 值)     ✓
//         public static void <T> 打印两次(T 值)     ✗ 语法错误
//   记法：先声明"我这儿有个类型占位符 T"，再说"我是干嘛的"。
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
// ⚠ 坑 1：把 size() 写成 size，或者把数组的 length 写成 length()
//
//     名单.size            // ✗ 错误: 找不到符号  变量 size
//     数们.length()        // ✗ 错误: 找不到符号  方法 length()
//
// 你会看到两种不一样但都看不懂的报错。为什么：
//   **数组的 length 是字段**（数组是特殊对象，长度是它的一个成员变量）
//   **集合的 size 是方法**（ArrayList 自己实现的一个方法）
// 一句话记：**数组 length 没括号，集合 size() 有括号。** 检查里专门看了这条。
//
// ⚠ 坑 2：遍历中往集合里加东西 / 删东西
//
//     for (String 名字 : 名单) {
//         if (名字.equals("小红")) {
//             名单.remove(名字);            ← ✗ 抛异常
//         }
//     }
//
// 你会看到：
//     Exception in thread "main" java.util.ConcurrentModificationException
// 为什么：增强 for 底下其实是用"迭代器"一个个往下走的，
// 它在心里记着"我走到第几个了"。你在外面把元素删掉，集合的长度变了，
// 它记的位置就对不上了 —— 于是它宁可报错也不敢继续瞎走。
// 正确做法：先把要删的**记下来**，遍历结束后再删。
// ⚠ 注意 `==` 和 equals 在这里也是同一个老坑：
//     名字.equals("小红")   ✓ 比内容        名字 == "小红"        ✗ 比地址
//
// ⚠ 坑 3：HashMap 的 get 返回 null，直接当数字用
//
//     int 分 = 表.get("不存在的人");        // ✗ 运行时 NullPointerException
//
// 你会看到：编译**通过**（Integer 能自动变成 int），跑起来崩在那一行，
// 报的是 NullPointerException，位置指向这个赋值 —— 看着莫名其妙，
// 因为那一行根本没有 "." 调用。
// 为什么：Integer → int 的自动拆箱在 null 上做不了（null 不是一个数）。
// 安全写法：
//     Integer 分 = 表.getOrDefault("不存在的人", 0);      ← 给个默认值
//     或者先 containsKey 判断。
//
// ⚠ 坑 4：以为 ArrayList 的 get 越界会返回 null
//
//     名单.get(99);         // ✗ 抛 IndexOutOfBoundsException
//
// 你会看到：Exception in thread "main" java.lang.IndexOutOfBoundsException:
//     Index 99 out of bounds for length 3
// 为什么：ArrayList 的下标越界和数组一样是**抛异常**，不是返回 null。
// （Map 那边不同：键不存在返回 null。这两个别记混 ——
//   **List 越界抛异常，Map 键不存在返回 null。**）
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   main 里四步 + 一次遍历，顺序就是检查比对的顺序：
//
//     ① ArrayList
//        打印 名单.size()                → 3
//        打印 名单.get(1)                → 小红
//        打印 名单.contains("小红")       → true
//
//     ② HashMap
//        打印 分数表.get("小明")          → 92
//        打印 分数表.get("小刚")          → null
//        （⚠ 这一行千万**不要**赋给 int，直接 println 就行）
//
//     ③ 泛型类
//        打印 名盒.取()                  → Java
//        打印 数盒.取()                  → 300
//
//     ④ 遍历 名单（用增强 for，配一个自己数的计数器 i）
//        打印 "第1个:小明" / "第2个:小红" / "第3个:小刚"
//
//     ⑤ 泛型方法
//        打印两次("泛型")                → 泛型 / 泛型
//        打印两次(7)                     → 7 / 7
//
// 期望输出一共 14 行，自己先数一遍：
//     3 / 小红 / true / 92 / null / Java / 300 /
//     第1个:小明 / 第2个:小红 / 第3个:小刚 / 泛型 / 泛型 / 7 / 7
//
// ⚠ import 别忘了（写在文件**最上面**，public class Main 之前）：
//     import java.util.ArrayList;
//     import java.util.HashMap;
//   忘了会报：错误: 找不到符号  类 ArrayList

// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/java-lessons 检查 16
//
//   看这一课要干什么：dotnet run --project csharp/java-lessons 16
//   看答案（先自己写一遍）：dotnet run --project csharp/java-lessons 答案 16
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。命令行（在源码目录里）：
//
//       dotnet run --project csharp/java-lessons 检查 
16
//
//   看这一课要干什么：dotnet run --project csharp/java-lessons 
16
//   看答案（先自己写一遍）：dotnet run --project csharp/java-lessons 答案 
16
//
//   
⚠ 检查会连着调好几个方法（加、取、查、遍历），并且**数输出的行数** —— import 少写一句，第一组就会在「找不到符号：类 ArrayList」那里红。
//
//   检查会把你的代码**真的编译、真的跑一遍**再逐条比对 —— 所以它红了就一定有原因，
//   照着那段说明改就行：它一般会直接点出你写错在哪、以及正确的写法长什么样。
""";

    public static string 参考答案 => """
// 第 16 课参考答案：集合与泛型入门
//
// ⚠ import 要写在最上面。它**不算**"别的语句" —— 不会影响
//    「public class Main 必须是文件里第一个非注释非空行」这条规矩。

import java.util.ArrayList;
import java.util.HashMap;

public class Main {
    public static void main(String[] args) {
        // ① ArrayList：会自己变长的列表
        ArrayList<String> 名单 = new ArrayList<>();
        名单.add("小明");
        名单.add("小红");
        名单.add("小刚");
        System.out.println(名单.size());              // 3    ⚠ 有括号
        System.out.println(名单.get(1));              // 小红  ⚠ 下标从 0 开始
        System.out.println(名单.contains("小红"));     // true

        // ② HashMap：键 → 值
        HashMap<String, Integer> 分数表 = new HashMap<>();
        分数表.put("小明", 92);
        分数表.put("小红", 88);
        System.out.println(分数表.get("小明"));        // 92
        // ⚠ 键不存在时 get 返回 null（不是 0）。
        //   这一行千万别写成 int 分 = 分数表.get("小刚"); —— 那一拆箱就崩。
        System.out.println(分数表.get("小刚"));        // null

        // ③ 泛型类：同一个 盒子，装 String 和装 Integer 都行
        盒子<String> 名盒 = new 盒子<>();
        名盒.放("Java");
        System.out.println(名盒.取());                // Java（不用强转）

        盒子<Integer> 数盒 = new 盒子<>();             // ⚠ 不能写 <int>
        数盒.放(300);                                 // 300 自动装箱成 Integer
        System.out.println(数盒.取());                // 300

        // ④ 遍历：增强 for 不给下标，编号要自己数
        int i = 1;
        for (String 名字 : 名单) {
            System.out.println("第" + i + "个:" + 名字);
            i = i + 1;
        }

        // ⑤ 泛型方法：T 由传进去的实参推断
        打印两次("泛型");                              // 泛型 / 泛型
        打印两次(7);                                   // 7 / 7
    }

    // ⚠ <T> 必须写在**返回类型之前**：先声明"我这儿有个类型占位符"，
    //   再说"我是干嘛的"。写成 public static void <T> 打印两次 是语法错误。
    public static <T> void 打印两次(T 值) {
        System.out.println(值);
        System.out.println(值);
    }
}

// ── 下面是我写的泛型类 ──
class 盒子<T> {
    private T 里面;

    public void 放(T 值) {
        this.里面 = 值;
    }

    public T 取() {
        return 里面;
    }
}
""";


    [Java检查说明("十四行的顺序和内容都对（含 Map 取不到时的 null）")]
    public static JavaCheckResult 检查_1(string 代码)
    {
        var run = JavaCheck.直接运行(代码);
        if (!run.编译通过) return JavaCheck.编译失败说明(run);
        string[] 期望 = {
            "3",
            "小红",
            "true",
            "92",
            "null",
            "Java",
            "300",
            "第1个:小明",
            "第2个:小红",
            "第3个:小刚",
            "泛型",
            "泛型",
            "7",
            "7",
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

    [Java检查说明("用了 ArrayList 和 HashMap（不是自己写的数组）")]
    public static JavaCheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("ArrayList<String> 名单"))
            return JavaCheckResult.不过("没找到「ArrayList<String> 名单」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("集合的长度用的是 size() 方法（不是 length）")]
    public static JavaCheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("名单.size()"))
            return JavaCheckResult.不过("没找到「名单.size()」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("Map 的 get 结果没有直接赋给 int（避开了拆箱崩溃）")]
    public static JavaCheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (去注释.Contains("int 分 ="))
            return JavaCheckResult.不过("出现了不该有的写法：int 分 =");
        return JavaCheckResult.过();
    }

    [Java检查说明("泛型类写对了：类上有 <T>、字段是 T")]
    public static JavaCheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("private T 里面;"))
            return JavaCheckResult.不过("没找到「private T 里面;」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("泛型方法的 <T> 写在返回类型前面")]
    public static JavaCheckResult 检查_6(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("public static <T> void 打印两次(T 值)"))
            return JavaCheckResult.不过("没找到「public static <T> void 打印两次(T 值)」—— 这一课要求用到它。");
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
