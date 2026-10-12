// 第 19 课：排序稳定性与 Comparator —— 分数一样的时候谁在前面
//
// ══ 这一课要教什么 ══
// 上一课只关心「排成从小到大」。可现实中排序的键常常**不是全部**：
// 成绩表按分数排序，分数一样的那几个人的先后怎么办？
// 「稳定」这个词说的就是这件事：**相等的元素，排完之后相对次序不变**。
//
// 这一课要建立两件事：
//   ① 稳定性不是「好不好的口味问题」，是**能不能用的正确性问题** ——
//      按班级排一遍、再按分数排一遍，稳定排序能保住班级内部的次序。
//   ② Java 里怎么把「比什么」交给排序：`Comparator`。
//      `Arrays.sort(数组, 比较器)` / `list.sort(比较器)` / `Comparator.comparingInt(...)`。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点一：**插入排序稳定，选择排序不稳定** —— 而且这个差别必须被真跑出来。
//   名字表 甲 = {小明, 小红, 小刚, 小美}，分数 = {90, 85, 90, 80}。
//   两个 90 分（小明、小美）在插入排序后保持「小明在前」，在选择排序后会**换位**。
//   学生如果把两个方法都写成插入排序，「不稳定」那一条就会失败 ——
//   这正是要他亲手看见的差别。（这个用例是我实测过的，见 检查_不稳定排序 的注释。）
// 核心检查点二：**不许用现成的排序**（同第 18 课）——
//   去掉注释后出现 Arrays.sort / Collections.sort 就判不过。
//   但 ⚠ 允许 `表.sort(比较器)`：第 3 个方法要的正是「用比较器排序」。
//   （所以禁的是 Arrays.sort(数组) / Collections.sort(表) 这两种「不带比较器的整体排序」。）
// 第三条：比较器要真的按「先长度、再字典」两级比较 ——
//   喂一份**长度相同但字典序不同**的数据（java / abc，都是 3 个字母），
//   只写了一级比较（或者干脆不倒）的会露馅。
//
// ⚠ 学生只写方法；骨架预置了 java.util.* 的 import。

using System.Collections.Generic;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("19", "第 19 课：排序稳定性与 Comparator",
    "1. 写 static void 按分数稳定排序(int[] 分数, String[] 名字)：分数从小到大，分数一样的保持原来的先后\n" +
    "2. 写 static void 按分数不稳定排序(int[] 分数, String[] 名字)：用**选择排序**（它天然不稳定），分数一样的会换位\n" +
    "3. 写 static Comparator<String> 按长度再按字典()：先比长度，长度相同再比字典序\n" +
    "4. 表里那句「分数一样时谁在前面」的区别，要能通过输出真看出来")]
public static class 第19课
{
    private const string 骨架 = """
import java.util.*;

public class Main {
/*学生代码*/

    public static void main(String[] args) {
/*调用点*/
    }
}
""";

    public static string 模板 => """
// 第 19 课：排序稳定性与 Comparator
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**「分数一样的时候谁在前面」是有对错的** —— 它叫稳定性。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    排序的稳定性；Comparator（把「比什么」交给排序）
//
//  需要先会
//    第 18 课：手写冒泡 / 选择 / 插入
//    第 14 课：接口（Comparator 就是一个接口）
//
// ─────────────────────────────────────────────────────
//  先介绍：排序的键常常不是全部
// ─────────────────────────────────────────────────────
//
// 一张成绩表：姓名 + 分数。按分数排序之后，两个都是 90 分的人，
// 谁应该排在前面？算法本身回答不了这个问题 —— 它只知道「90 等于 90」。
//
// 这就是「稳定」要说的：
//
//     稳定排序    相等的元素，排完之后**相对次序不变**
//     不稳定排序  相等的元素，排完之后次序**可能被打乱**
//
// ⚠ 为什么这不是小事：很多程序是**分两步排序**的 ——
//   先按班级排一遍，再按分数排一遍。第二次排序如果稳定，
//   同班的人在新表里**仍然挨在一起、仍然按班级排**；
//   如果不稳定，第一步排的就白排了。
//
// ─────────────────────────────────────────────────────
//  一、谁稳定、谁不稳定
// ─────────────────────────────────────────────────────
//
//     稳定      冒泡（写对的话）、插入、归并（第 29 课）
//     不稳定    选择、快排（第 30 课）、堆排
//
// 为什么插入排序稳定：它把新元素插进前面那一段时，
//   条件是 `数[j] > 拿着`（**严格大于**）—— 等于的时候不往左挪，
//   所以后来的人永远排在前人的右边。
//
// 为什么选择排序不稳定：它是「一轮换一次」的**远距离交换**。
//   看这个例子（按分数从小到大）：
//
//       分数  90  85  90  80
//       名字  小明 小红 小刚 小美      ← 两个 90 分：小明在前，小刚在后
//
//   稳定排序（插入排序）之后：  小美 小红 小明 小刚
//       小美（80）挪到最前，两个 90 分的小明、小刚**仍然小明在前**。
//
//   不稳定排序（选择排序）之后：小美 小红 小刚 小明
//       分数一样是排好了，可小明和小刚**换了位** ——
//       第一轮那次远距离交换（把末尾的小美换到最前面）顺手把小明甩到了最后。
//
// ─────────────────────────────────────────────────────
//  二、Comparator：把「比什么」交给排序
// ─────────────────────────────────────────────────────
//
//     Comparator<String> 比长度 = (a, b) -> a.length() - b.length();
//     表.sort(比长度);                      ← ArrayList / List 都能这么排
//     Arrays.sort(数组, 比长度);            ← 数组用这个
//
// 这个 `(a, b) -> …` 是 **lambda**（第 14 课见过接口，这里是把接口「就地实现」）。
// 它返回一个数，约定和 Comparator 一样：
//
//     负数   a 应该排在 b **前面**
//     0      两个一样大（**稳定排序会保住它们的原有次序**）
//     正数   a 应该排在 b **后面**
//
// ⚠ 返回值只要符号对就行，**不要**写死 -1 / 1 ——
//   写 `return a.length() < b.length() ? -1 : 1;` 是把「相等」当成「后面」，
//   排序结果会莫名其妙（而且不稳定排序的后果会被放大）。
//
// ─────────────────────────────────────────────────────
//  三、两级比较：先比这个，相同再比那个
// ─────────────────────────────────────────────────────
//
//     Comparator<String> 比长度再比字典 = (a, b) -> {
//         if (a.length() != b.length()) {
//             return a.length() - b.length();      ← 第一级：长度
//         }
//         return a.compareTo(b);                    ← 第二级：字典序
//     };
//
// ⚠ 字符串比大小要用 **compareTo**（第 07 课讲过：`==` 比的是「是不是同一个对象」，
//   比内容要用 `equals`；比**先后**要用 `compareTo`）。
//
// Java 还给了现成的写法（了解即可）：
//
//     Comparator.comparingInt(String::length).thenComparing(Comparator.naturalOrder())
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：以为「排好了」就等于「排对了」
// ─────────────────────────────────────────────────────
//
// 这一课的坑不在编译期，而在**你以为对**：
//
//     static void 按分数稳定排序(int[] 分数, String[] 名字) {
//         // 照着第 18 课的选择排序抄一遍 —— 编译通过、分数也确实从小到大
//     }
//
// 你会看到：**分数一列完全正确**，名字一列却悄悄换了位。
// 检查会把两个都是 90 分的人拎出来看谁在前面。
//
// 为什么：`equals` 意义上的「相等」在排序里是一条**规则**，不是「随便」：
//   稳定排序承诺「相等的不动」，选择排序**没有这个承诺**。
//   同一份代码在 Java、C++、Python 里表现一模一样（Python 的 sorted 是稳定的，
//   这一点和 Java 的 Arrays.sort（对基本类型）不同：**基本类型用双轴快排，不稳定**；
//   而对象数组 / List 用归并的变体，**稳定** —— 这也是 Java 的一处真实差异）。
//
// ⇒ 要稳定就**自己写插入排序**（或者用 List 的 sort）。先确认你要的是不是稳定。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   更快的排序（归并、快排）→ 第 29、30 课。
//   复杂度为什么是 O(n²) → 第 20 课。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 你**只写方法**：不用写 class，不用写 main，也不用写 import ——
//    课程已经把 java.util.* 都 import 好了。每个方法都要带 **static**。
//
//   1. static void 按分数稳定排序(int[] 分数, String[] 名字)
//        按 分数 从小到大排，**分数相同时保持名字原来的先后**。
//        ⚠ 分数 和 名字 是**同步**的：跟着分数走的还有名字。
//        要求：自己写**插入排序**（它是稳定的）。
//        ⚠ 不许用 Arrays.sort / Collections.sort（那是直接调库；
//          不过 `表.sort(比较器)` 在第 3 个方法里是允许的）。
//
//   2. static void 按分数不稳定排序(int[] 分数, String[] 名字)
//        同样的活，但这次用**选择排序**（每轮找最小下标、和第 i 个交换）。
//        它天然不稳定 —— 这一条是为了让你**亲眼看见**那个差别，不是让你写对。
//
//   3. static Comparator<String> 按长度再按字典()
//        返回一个比较器：先比长度（短的在前），长度相同再比字典序。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
    static void 按分数稳定排序(int[] 分数, String[] 名字) {
    }

    static void 按分数不稳定排序(int[] 分数, String[] 名字) {
    }

    static Comparator<String> 按长度再按字典() {
        return null;
    }
""";

    public static string 参考答案 => """
// 参考答案 —— 稳定性与 Comparator（只写方法，不用写 class / main / import）

// 1. 稳定排序：自己写插入排序
//    它稳定的原因就在那一行 `分数[j] > 拿着` —— **严格大于**。
//    遇到相等的（90 和 90）不再往左挪，于是后来的人永远留在右边。
//    ⚠ 分数和名字要**一起**挪，否则分数对了、名字对不上。
static void 按分数稳定排序(int[] 分数, String[] 名字) {
    for (int i = 1; i < 分数.length; i++) {
        int 拿着分 = 分数[i];
        String 拿着名 = 名字[i];
        int j = i - 1;
        while (j >= 0 && 分数[j] > 拿着分) {      /* ⚠ 严格大于，不能写成 >= */
            分数[j + 1] = 分数[j];
            名字[j + 1] = 名字[j];
            j--;
        }
        分数[j + 1] = 拿着分;
        名字[j + 1] = 拿着名;
    }
}

// 2. 不稳定排序：选择排序（每轮一次远距离交换）
//    那个「远距离交换」就是罪魁祸首：它会把一个元素从很后面直接甩到前面，
//    路上所有和它相等的人就被它跨过去了。
static void 按分数不稳定排序(int[] 分数, String[] 名字) {
    for (int i = 0; i < 分数.length - 1; i++) {
        int 最小 = i;
        for (int j = i + 1; j < 分数.length; j++) {
            if (分数[j] < 分数[最小]) {
                最小 = j;
            }
        }
        if (最小 != i) {
            int 临时分 = 分数[i];
            分数[i] = 分数[最小];
            分数[最小] = 临时分;

            String 临时名 = 名字[i];
            名字[i] = 名字[最小];
            名字[最小] = 临时名;
        }
    }
}

// 3. 两级比较器：先长度，再字典序
//    ⚠ 返回值的**符号**才是关键：负数=排前面，0=一样大，正数=排后面。
//      两级都要照顾到 —— 只写第一级的话，长度相同的那些会乱序。
static Comparator<String> 按长度再按字典() {
    return (a, b) -> {
        if (a.length() != b.length()) {
            return a.length() - b.length();
        }
        return a.compareTo(b);        /* ⚠ 比先后用 compareTo，不是 ==，也不是 equals */
    };
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("三个方法都在，签名也对（含返回 Comparator<String>）")]
    public static JavaCheckResult 检查_函数都在(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        if (!Regex.IsMatch(净, @"static\s+void\s+按分数稳定排序\s*\(\s*int\s*\[\s*\]\s*\w+\s*,\s*String\s*\[\s*\]\s*\w+\s*\)"))
            缺.Add("      按分数稳定排序　（要写成：static void 按分数稳定排序(int[] 分数, String[] 名字)）");
        if (!Regex.IsMatch(净, @"static\s+void\s+按分数不稳定排序\s*\(\s*int\s*\[\s*\]\s*\w+\s*,\s*String\s*\[\s*\]\s*\w+\s*\)"))
            缺.Add("      按分数不稳定排序　（要写成：static void 按分数不稳定排序(int[] 分数, String[] 名字)）");
        if (!Regex.IsMatch(净, @"static\s+Comparator\s*<\s*String\s*>\s+按长度再按字典\s*\(\s*\)"))
            缺.Add("      按长度再按字典　（要写成：static Comparator<String> 按长度再按字典()）");

        if (缺.Count == 0) return JavaCheckResult.过("三个方法都在，签名也对");
        return JavaCheckResult.不过(
            "有方法没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      注意三点：\n" +
            "        · 数组类型写成 int[] 分数 / String[] 名字（方括号跟类型）\n" +
            "        · **static 不能漏**\n" +
            "        · 第 3 个返回的是 Comparator<String>，尖括号里的 String 不能省");
    }

    [Java检查说明("★ 稳定排序真的稳定：两个 90 分的人排完还是一前一后（而且是手写的插入排序）")]
    public static JavaCheckResult 检查_稳定排序(string 代码)
    {
        // ⚠ 这一条是本课的核心检查点。
        string 净 = 去掉注释(代码);
        if (净.Contains("Arrays.sort") || 净.Contains("Collections.sort"))
            return JavaCheckResult.不过(
                "用了现成的整体排序（Arrays.sort / Collections.sort）。\n" +
                "      这一课要你**自己写插入排序** —— 它的稳定性来自那一行 `分数[j] > 拿着`\n" +
                "      （严格大于），这是你必须亲手写一遍才看得见的东西。\n" +
                "      ⇒ 把 Arrays.sort 换成两层循环的插入排序。");

        string 体 = 函数体(净, "按分数稳定排序");
        if (体 == null)
            return JavaCheckResult.不过("没找到 按分数稳定排序 的函数体 —— 先确认这个方法写全了（花括号配对）。");
        if (!Regex.IsMatch(体, @"\b(for|while)\b"))
            return JavaCheckResult.不过(
                "按分数稳定排序 的函数体里一个循环都没有。\n" +
                "      这一课要练的是**插入排序怎么写才稳定**，所以循环要自己写：\n" +
                "          for (int i = 1; i < 分数.length; i++) {\n" +
                "              int 拿着分 = 分数[i];\n" +
                "              int j = i - 1;\n" +
                "              while (j >= 0 && 分数[j] > 拿着分) { …往右挪… }\n" +
                "          }");

        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("稳定分|", "[80, 85, 90, 90]", "按分数稳定排序 之后，分数一列是 [80, 85, 90, 90]（80 是小美、85 是小红、两个 90 是小明和小刚）"),
            ("稳定名|", "小美 小红 小明 小刚", "⚠ 两个 90 分的人（小明、小刚）排完必须**还是小明在前** —— 这就是「稳定」。得到的是别的顺序，说明你写的不是稳定排序（或者名字没跟着分数一起挪）"));
    }

    [Java检查说明("★ 不稳定排序真的不稳定：选择排序会把两个 90 分的人换位")]
    public static JavaCheckResult 检查_不稳定排序(string 代码)
    {
        // ⚠ 第二条核心检查点。学生如果把两个方法都写成插入排序，
        //   「不稳定」这一条就会失败 —— 而这正是这一课要他看见的差别。
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("不稳分|", "[80, 85, 90, 90]", "按分数不稳定排序 之后，分数一列也是 [80, 85, 90, 90]（分数总是排对的）"),
            ("不稳名|", "小美 小红 小刚 小明", "⚠ 选择排序**不稳定**：一次远距离交换把小明从「小美前面」甩到了最后。\n" +
                "            如果你这里得到的是「小美 小红 小明 小刚」（和稳定排序一样），\n" +
                "            说明你把这一步也写成了插入排序 —— 改成**选择排序**：\n" +
                "            每轮找最小下标，然后和第 i 个**交换**（就是这一换，把相等的次序打乱了）"));
    }

    [Java检查说明("★ 比较器是两级比较：长度相同的还要按字典序排")]
    public static JavaCheckResult 检查_比较器(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("比较器|", "[c, ab, zz, abc, java]", "按长度再按字典() 排 {java, c, abc, ab, zz} ⇒ 长度 1 的 c；长度 2 的 ab、zz（字典序）；长度 3 的 abc；长度 4 的 java"));
    }

    [Java检查说明("名字是跟着分数一起挪的（不是只排了分数）")]
    public static JavaCheckResult 检查_名字跟着走(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("原始名|", "小明 小红 小刚 小美", "调用点先打印一次原始顺序（这是课程打的，用来对照）"),
            ("稳定名|", "小美 小红 小明 小刚", "排完之后每个名字都要跟着自己的分数走：80→小美（排最前）、85→小红、两个 90 分的小明和小刚保持原有一前一后"));
    }

    // ══════════════════ 工具 ══════════════════

    private const string 调用点 = """
        int[] 分数 = {90, 85, 90, 80};
        String[] 名字 = {"小明", "小红", "小刚", "小美"};
        System.out.println("原始名|" + String.join(" ", 名字));

        int[] 分数2 = {90, 85, 90, 80};
        String[] 名字2 = {"小明", "小红", "小刚", "小美"};
        按分数稳定排序(分数2, 名字2);
        System.out.println("稳定分|" + Arrays.toString(分数2));
        System.out.println("稳定名|" + String.join(" ", 名字2));

        int[] 分数3 = {90, 85, 90, 80};
        String[] 名字3 = {"小明", "小红", "小刚", "小美"};
        按分数不稳定排序(分数3, 名字3);
        System.out.println("不稳分|" + Arrays.toString(分数3));
        System.out.println("不稳名|" + String.join(" ", 名字3));

        ArrayList<String> 词 = new ArrayList<>(Arrays.asList("java", "c", "abc", "ab", "zz"));
        词.sort(按长度再按字典());
        System.out.println("比较器|" + 词);
""";

    private static string 上次的代码 = "";
    private static JavaRun 上次的运行;

    private static JavaRun 跑一次(string 代码)
    {
        if (上次的运行 != null && 上次的代码 == 代码) return 上次的运行;
        上次的代码 = 代码;
        var 包 = JavaCheck.编译并运行(代码, 骨架, 调用点);
        上次的运行 = 包.编译通过 ? 包 : null;
        return 包;
    }

    private static string 先别急着比(JavaRun run)
    {
        if (run.Raw.CompileFailed)
            return JavaCheck.编译失败说明(run).Detail;
        if (run.Raw.Error != null)
            return "程序没跑完就退出了：\n      " + run.Raw.Error + "\n" +
                   "      这一课最容易在这里崩的是**返回值写成了 null**：\n" +
                   "      按长度再按字典() 如果 return null，调用点一排序就抛\n" +
                   "          NullPointerException\n" +
                   "      ⇒ 要真的返回一个比较器（lambda 也行）。";
        return "";
    }

    private static JavaCheckResult 比(JavaRun run, params (string 标记, string 期望, string 说明)[] 用例们)
    {
        var 错 = new List<string>();
        foreach (var (标记, 期望, 说明) in 用例们)
        {
            string 得到 = 看看(run, 标记);
            if (得到.Length == 0) 错.Add("      " + 说明 + "：没有找到这个标记的输出");
            else if (!一样(得到, 期望))
                错.Add("      " + 说明 + "：得到「" + 得到 + "」，应该是「" + 期望 + "」");
        }
        if (错.Count == 0) return JavaCheckResult.过($"这几组都对（{用例们.Length} 组）");

        return JavaCheckResult.不过(
            "结果不对：\n" + string.Join("\n", 错) + "\n" +
            "      这一课两个要点：\n" +
            "        · **稳定**：分数相同时保持原有先后。插入排序稳定，因为条件是\n" +
            "          `分数[j] > 拿着分`（严格大于）—— 相等就不动。\n" +
            "        · **Comparator**：返回值只要**符号**对 —— 负数排前面、0 一样大、正数排后面。\n" +
            "          两级比较要先判第一级，不同就返回它，相同才去比第二级。");
    }

    private static string 看看(JavaRun run, string 标记)
    {
        foreach (var 行 in run.行)
        {
            int 位 = 行.IndexOf(标记, System.StringComparison.Ordinal);
            if (位 >= 0) return 行.Substring(位 + 标记.Length).Trim();
        }
        return "";
    }

    private static bool 一样(string 甲, string 乙) =>
        Regex.Replace(甲.Trim(), @"\s+", " ") == Regex.Replace(乙.Trim(), @"\s+", " ");

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

    private static string 函数体(string 净源码, string 函数名)
    {
        int 名 = 净源码.IndexOf(函数名 + "(", System.StringComparison.Ordinal);
        if (名 < 0) return null;
        int 起 = 净源码.IndexOf('{', 名);
        if (起 < 0) return null;
        int 深 = 0;
        for (int i = 起; i < 净源码.Length; i++)
        {
            if (净源码[i] == '{') 深++;
            else if (净源码[i] == '}')
            {
                深--;
                if (深 == 0) return 净源码.Substring(起 + 1, i - 起 - 1);
            }
        }
        return null;
    }
}
