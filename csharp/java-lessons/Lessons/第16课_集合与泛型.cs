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
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**用 ArrayList 装会变多的东西**，以及尖括号是什么。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    集合与泛型入门：ArrayList 和 <...> 是什么
//
//  需要先会
//    第 06 课：ArrayList；第 14 课：接口（泛型常见于接口）
//
// ─────────────────────────────────────────────────────
//  先介绍：数组的两个不方便
// ─────────────────────────────────────────────────────
//
//     ① 长度定死 —— 第 06 课就说了，`new int[3]` 就只能装三个
//     ② 类型定死 —— `int[]` 装不了文字，`String[]` 装不了数字
//
// ArrayList 解决第一个，泛型 `<...>` 说明第二个。
//
// ─────────────────────────────────────────────────────
//  一、ArrayList 的基本用法
// ─────────────────────────────────────────────────────
//
//     import java.util.ArrayList;                 ←  要 import
//
//     ArrayList<String> 名字们 = new ArrayList<>();
//
//     名字们.add("小明");             末尾加一个（**它会改变这张表本身**）
//     名字们.get(0)                  第 0 个        ← 方法，不是方括号
//     名字们.size()                  有几个         ← 方法，有括号
//     名字们.remove(0)               删掉第 0 个
//     名字们.contains("小明")        true / false
//
// ⚠ 和数组的写法对照着记（第 06 课踩过）：
//
//     int[] 数组;                  数组.length      ← **没有括号**
//     ArrayList 表;                表.size()        ← **有括号**
//     表.get(i)                    ← 不是 表[i]
//
// ─────────────────────────────────────────────────────
//  二、泛型 <...>：说清楚这个容器装什么
// ─────────────────────────────────────────────────────
//
//     ArrayList<String>  装文字
//     ArrayList<Integer> 装整数      ⚠ 是 Integer，不是 int
//     ArrayList<Double>  装小数
//
// ⚠ 尖括号里**只能写对象类型**，基本类型要先包一层：
//
//     int      → Integer
//     double   → Double
//     boolean  → Boolean
//
// 写 `ArrayList<int>` 会报：错误: 意外的类型 / 需要: 引用 / 找到: int
//
// 泛型的好处：拿出来就是 String，**不用转类型**。
//
//     String 第一个 = 名字们.get(0);        ✓ 直接就是 String
//
//     （没有泛型的年代要写 (String) 名字们.get(0) 强转 —— 那是老代码的样子。）
//
// ─────────────────────────────────────────────────────
//  三、遍历
// ─────────────────────────────────────────────────────
//
//     for (String 名 : 名字们) { ... }              增强 for，最省事
//     for (int i = 0; i < 名字们.size(); i++) { }   需要下标时
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：边遍历边删
// ─────────────────────────────────────────────────────
//
//     for (int i = 0; i < 名字们.size(); i++) {
//         if (名字们.get(i).equals("小明")) {
//             名字们.remove(i);            ← 删完下标就乱了
//         }
//     }
//
// 你会看到：**编译通过、没有警告**，只是有的该删的没删、有的被跳过了。
//
// 为什么：remove 之后，后面的元素**整体往前挪了一格**，
//   而 i 照样加 1 ⇒ 你跳过了紧跟在被删元素后面的那一个。
//
// ⇒ 边遍历边删的时候，**删完不要 i++**：
//
//     if (该删) { 名字们.remove(i); } else { i++; }
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   HashMap / HashSet → 这门课不涉及（思路一样，换一种容器）。
//   自己写泛型类 → 不涉及。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  按题目要求写几个操作 ArrayList 的方法，并按规定顺序打印。
//  ⚠ 记住要 `import java.util.ArrayList;`（写在文件最上面）。
//   检查会真的去 new 一张表、调你的方法，然后比对输出。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
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
