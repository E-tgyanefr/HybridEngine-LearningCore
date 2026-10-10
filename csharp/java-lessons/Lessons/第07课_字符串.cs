// 第 07 课：字符串 —— Java 最著名的那个坑
//
// ══ 这一课要教什么 ══
// 字符串是最常用的东西，也是 Java 里"最会坑人"的东西。这一课有两件事必须讲透：
//
//   ① **比较内容要用 .equals()，不能用 ==**
//        String a = "hi";
//        String b = new String("hi");
//        a == b          // false —— 比的是「是不是同一个对象」
//        a.equals(b)     // true  —— 比的是「内容一不一样」
//      为什么 == 有时候"看起来是对的"？因为字面量 "hi" 会被放进常量池：
//        String c = "hi";  a == c  →  true（同一个对象，凑巧相等）
//      所以学生写 == 很可能**自己试出来是对的** —— 直到字符串是从别处来的
//      （new 出来的、拼出来的、读进来的）才突然出错。这是本课的核心检查点。
//
//   ② **字符串是不可变的（immutable）**
//        s.toUpperCase();        // ← 什么都没发生：它返回一个**新**字符串，s 一个字没改
//        s = s.toUpperCase();    // ← 这样才是"把 s 换成大写版"
//      凡是要"改"字符串的方法，都得把返回值接住。
//      （对照第 06 课的 ArrayList：表.add(x) 会当场改变表本身 —— 两者行为不一样。）
//
// ══ 检查设计说明（给维护者）══
// 核心检查点：**用 == 比字符串内容**。
//   抓法就是任务里说的那招 —— 用例里放一个 `new String("hi")`：
//        内容相同("hi", "hi")                    → == 也能蒙对（常量池同一个对象）
//        内容相同(new String("hi"), "hi")         → == 给出 false，equals 给出 true
//        内容相同(new String("你好"), new String("你好")) → 同上
//   所以只要学生用了 ==，就必定在这些用例上翻车；
//   而"字面量对不对"那一组他还是对的 —— 这正好也是给学生看的证据：
//   **== 不是一直错，是"有时候碰巧对"** —— 那才是最危险的。
//
//   另一个检查点：变大写 必须**接住返回值**（s.toUpperCase() 不改 s）。
//   学生写 `s.toUpperCase(); return s;` 会得到小写原串 → 被抓。
//
// ⚠ 给维护者：这一课不需要 import（String / char 都在 java.lang 里），
//   所以用 JavaCheck 的默认骨架就行 —— 学生只写方法，main 由课程提供
//   （和第 05、06 课同一个形状；好处是"忘了 static"照样会被编译器抓住）。

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("07", "第 07 课：字符串 —— 内容比较必须用 equals",
    "1. static int 长度(String s)：返回字符个数（s.length()，**要括号**）\n" +
    "2. static char 第一个(String s)：返回第一个字符（s.charAt(0)）\n" +
    "3. static String 变大写(String s)：返回全大写的版本（**不能改 s 本身**）\n" +
    "4. static int 找位置(String s, String 片段)：返回片段第一次出现的位置，找不到返回 -1\n" +
    "5. static String 前三个(String s)：返回前 3 个字符；不足 3 个就原样返回\n" +
    "6. static boolean 内容相同(String a, String b)：两个字符串的**内容**是否一样")]
public static class 第07课
{
    public static string 模板 => """
// 第 07 课：字符串
//
// 要做（只写**方法**，不用写 class、不用写 main —— 课程替你补上）：
//   1. static int 长度(String s)                  返回字符个数
//   2. static char 第一个(String s)               返回第一个字符
//   3. static String 变大写(String s)             返回全大写的版本（s 本身不许改）
//   4. static int 找位置(String s, String 片段)    片段第一次出现的位置；找不到返回 -1
//   5. static String 前三个(String s)             前 3 个字符；不足 3 个就原样返回
//   6. static boolean 内容相同(String a, String b) 两个字符串的**内容**是否一样
//
// 常用的几个方法（注意：**每个都带括号**，它们是方法不是属性）：
//     s.length()            有几个字符
//     s.charAt(0)           第 0 个字符（下标从 0 开始，和数组一样）
//     s.toUpperCase()       转成大写（**返回新字符串，s 本身不变**）
//     s.indexOf("na")       片段第一次出现的位置；找不到返回 -1（不是 0！）
//     s.substring(0, 3)     从下标 0 开始、到 3 **之前**（半开区间：含 0 不含 3）
//
// ⚠ 本课最重要的两条，务必看懂：
//
//   ① 比较字符串的**内容**只能用 .equals()，绝对不能用 ==
//         String a = "hi";
//         String b = new String("hi");
//         a == b            → false   ✗ == 比的是「是不是同一个对象」
//         a.equals(b)       → true    ✓ equals 比的才是「内容一不一样」
//      为什么 == 有时候看着是对的？因为直接写出来的字面量 "hi" 会共用一个对象：
//         String c = "hi";   a == c   → true   ← 碰巧对，骗人的
//      一旦字符串是 new 出来的、拼出来的、从别处传进来的，== 立刻就错。
//      写的时候把这一步当成纪律：**看到比较字符串，就用 equals**。
//      （顺便：a.equals(b) 里如果 a 可能是 null 会抛 NullPointerException，
//        老手会把常量写在前面："hi".equals(a) —— 常量不可能是 null。）
//
//   ② 字符串**改不了**。凡是"改"字符串的方法，都是返回一个**新**字符串：
//         s.toUpperCase();               ✗ s 一个字没变（返回值被丢掉了）
//         s = s.toUpperCase();           ✓ 把返回的新字符串接住
//         String 大 = s.toUpperCase();    ✓ 或者接到另一个变量里
//      （对照第 06 课的 ArrayList：表.add(x) 会当场改变表本身。两者行为不一样。）
//
// 写完后运行：dotnet run --project csharp/java-lessons 检查 07

// ── 在下面写你的方法 ──

""";

    public static string 参考答案 => """
// 1. 长度：length() 是**方法**，要括号（数组的 a.length 才不带括号）
static int 长度(String s) {
    return s.length();
}

// 2. 第一个字符：charAt(0)，下标从 0 开始
static char 第一个(String s) {
    return s.charAt(0);
}

// 3. 变大写：把新字符串**返回**出去 —— s 本身永远不会变
static String 变大写(String s) {
    return s.toUpperCase();
}

// 4. 找位置：找不到时 indexOf 返回 -1
static int 找位置(String s, String 片段) {
    return s.indexOf(片段);
}

// 5. 前三个：substring(0, 3) 取的是下标 0、1、2 三个字符（含 0 不含 3）
//    不足 3 个字符时不能直接 substring —— 会抛 StringIndexOutOfBoundsException
static String 前三个(String s) {
    if (s.length() < 3) {
        return s;
    }
    return s.substring(0, 3);
}

// 6. 内容相同：**必须用 equals**，不能用 ==
static boolean 内容相同(String a, String b) {
    return a.equals(b);
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("长度：s.length() 是方法，**要括号**（和数组的 a.length 正好相反）")]
    public static JavaCheckResult 检查_长度(string 代码)
    {
        var run = 编译并调用(代码, @"长度(""hello"")", @"长度("""")", @"长度(""你好"")");
        if (!run.编译通过) return 编译不通过(run);

        var 结果 = 比对(run, ("长度(\"hello\")", "5"), ("长度(\"\")", "0"), ("长度(\"你好\")", "2"));
        if (结果.Ok) return 结果;
        return JavaCheckResult.不过(结果.Detail + "\n" +
            "      字符串的长度用 s.length()（**是方法，要括号**）。\n" +
            "      ⚠ 这里和第 06 课的数组正好相反，两个都记一遍：\n" +
            "          int[] a;    a.length      ← 数组：属性，**不加括号**\n" +
            "          String s;   s.length()    ← 字符串：方法，**要括号**\n" +
            "      空串 \"\" 的长度是 0（合法，不用特殊处理）；\n" +
            "      一个汉字算一个字符，所以 \"你好\".length() 是 2。");
    }

    [Java检查说明("第一个字符：charAt(0)，下标从 0 开始")]
    public static JavaCheckResult 检查_第一个(string 代码)
    {
        var run = 编译并调用(代码, @"第一个(""java"")", @"第一个(""你好"")", @"第一个(""A"")");
        if (!run.编译通过) return 编译不通过(run);

        var 结果 = 比对(run, ("第一个(\"java\")", "j"), ("第一个(\"你好\")", "你"), ("第一个(\"A\")", "A"));
        if (结果.Ok) return 结果;
        return JavaCheckResult.不过(结果.Detail + "\n" +
            "      取第 0 个字符用 s.charAt(0)：\n" +
            "          return s.charAt(0);\n" +
            "      ⚠ 两个常见写法错误：\n" +
            "        · 用方括号：s[0]  —— 方括号只对数组有效，字符串不行\n" +
            "        · 从 1 开始数：charAt(1) 拿到的是**第二个**字符\n" +
            "      返回类型是 char（单个字符），不是 String —— 用双引号包一个字母是\n" +
            "      String（\"j\"），用单引号才是 char（'j'）。");
    }

    [Java检查说明("变大写：必须**接住返回值** —— s 本身改不了")]
    public static JavaCheckResult 检查_变大写(string 代码)
    {
        // 为什么要连着原串一起打印：字符串不可变，s 不可能被改掉 ——
        // 但学生写 `s.toUpperCase(); return s;` 时，"没接住返回值"这件事
        // 只有在**看返回值**的时候才暴露。印出「原串/返回」两行，
        // 学生一眼就能看出：改的是返回值，不是原串。
        var run = 编译并调用语句(代码,
            @"String s = ""hello"";",
            @"String r = 变大写(s);",
            @"System.out.println(""原串:"" + s);",
            @"System.out.println(""返回:"" + r);",
            @"System.out.println(""第二个:"" + 变大写(""Java 编程""));");
        if (!run.编译通过) return 编译不通过(run);

        var 结果 = 比对(run, ("原串（不该变）", "原串:hello"),
                             ("变大写(\"hello\") 的返回", "返回:HELLO"),
                             ("变大写(\"Java 编程\") 的返回", "第二个:JAVA 编程"));
        if (结果.Ok)
            return JavaCheckResult.过(
                "变大写(\"hello\") 返回 HELLO，而原串 s 还是 hello ——\n" +
                "      这就是**字符串不可变**：toUpperCase 造了一个新字符串交给你，\n" +
                "      s 本身一个字都没变。所以「改字符串」的方法，返回值一定要接住：\n" +
                "          s = s.toUpperCase();            ✓ 把新的接回 s\n" +
                "          String 大 = s.toUpperCase();     ✓ 或者接到别的变量\n" +
                "          s.toUpperCase();                ✗ 返回值被丢掉 = 什么都没做");

        return JavaCheckResult.不过(结果.Detail + "\n" +
            "      检查两点：\n" +
            "        · 有没有把 toUpperCase() 的**返回值**交出去：\n" +
            "              return s.toUpperCase();          ✓\n" +
            "              s.toUpperCase(); return s;       ✗ 返回的是没变过的原串\n" +
            "        · 顺便注意：toUpperCase 只改英文字母，汉字不受影响 ——\n" +
            "              \"Java 编程\".toUpperCase() 是 \"JAVA 编程\"");
    }

    [Java检查说明("找位置：返回第一次出现的下标；找不到是 -1（不是 0）")]
    public static JavaCheckResult 检查_找位置(string 代码)
    {
        var run = 编译并调用(代码,
            @"找位置(""banana"", ""na"")",
            @"找位置(""banana"", ""x"")",
            @"找位置(""banana"", ""banana"")",
            @"找位置(""你好世界"", ""世界"")");
        if (!run.编译通过) return 编译不通过(run);

        var 结果 = 比对(run, ("找位置(\"banana\", \"na\")", "2"),
                             ("找位置(\"banana\", \"x\") —— 找不到", "-1"),
                             ("找位置(\"banana\", \"banana\")", "0"),
                             ("找位置(\"你好世界\", \"世界\")", "2"));
        if (结果.Ok) return 结果;
        return JavaCheckResult.不过(结果.Detail + "\n" +
            "      用 s.indexOf(片段)：\n" +
            "          return s.indexOf(片段);\n" +
            "      ⚠ 三条要记牢的：\n" +
            "        · 下标从 **0** 开始 —— \"banana\" 里 \"na\" 出现在 2（不是 3）\n" +
            "        · 在开头出现就是 **0**，所以「0」不是「没找到」的意思\n" +
            "        · **找不到返回 -1**（不是 0，也不是报错）——\n" +
            "          所以判断有没有要用 if (位置 >= 0)，别写 if (位置 == 0)");
    }

    [Java检查说明("前三个：substring(0, 3) 含头不含尾；不足 3 个字符要先判断")]
    public static JavaCheckResult 检查_前三个(string 代码)
    {
        var run = 编译并调用(代码,
            @"""A["" + 前三个(""abcdef"") + ""]""",
            @"""B["" + 前三个(""abc"") + ""]""",
            @"""C["" + 前三个(""ab"") + ""]""",
            @"""D["" + 前三个("""") + ""]""");
        if (run.Raw.CompileFailed) return 编译不通过(run);

        // 不足 3 个字符时若直接 substring(0, 3)，Java 会抛
        // StringIndexOutOfBoundsException（不是"返回能给的部分"）。
        // 这个报错本身很有教育意义，单独翻译。
        if (含(run.Raw.RunOutput, "StringIndexOutOfBoundsException"))
            return JavaCheckResult.不过(
                "字符串太短的时候出错了：抛出了 StringIndexOutOfBoundsException。\n" +
                "      这不是「返回能给的几个字符」，而是**直接报错停下**：\n" +
                "          \"ab\".substring(0, 3)     ✗ 越界（只有 2 个字符，却要 3 个）\n" +
                "      所以要先看一眼长度：\n" +
                "          if (s.length() < 3) {\n" +
                "              return s;              ← 不足 3 个就原样返回\n" +
                "          }\n" +
                "          return s.substring(0, 3);\n" +
                "      ⚠ 顺便看清 substring 的区间：substring(0, 3) 取的是\n" +
                "        下标 0、1、2 —— **含 0，不含 3**（半开区间，到 3 之前为止）。");

        var 结果 = 比对(run, ("前三个(\"abcdef\")", "A[abc]"),
                             ("前三个(\"abc\") —— 正好三个", "B[abc]"),
                             ("前三个(\"ab\") —— 不足三个", "C[ab]"),
                             ("前三个(\"\") —— 空串", "D[]"));
        if (结果.Ok) return 结果;
        return JavaCheckResult.不过(结果.Detail + "\n" +
            "      用 s.substring(0, 3)：**从 0 开始，到 3 之前**（含 0 不含 3）。\n" +
            "        substring(0, 3)   ✓ 三个字符\n" +
            "        substring(0, 2)   ✗ 只有两个\n" +
            "        substring(1, 3)   ✗ 从第二个字符开始，只有两个\n" +
            "      字符串不足 3 个字符时要先判断（s.length() < 3），否则会抛异常。");
    }

    [Java检查说明("内容相同：必须用 equals 比内容 —— 用 == 会在这里翻车（本课重点）")]
    public static JavaCheckResult 检查_用equals比内容(string 代码)
    {
        // ⚠ 本课的核心检查点。
        //
        // 关键在第二、三个用例：它们用 **new String(...)** 造字符串。
        //   · 用 equals → 三个 true（正确）
        //   · 用 ==     → 第一个 true（字面量共用常量池里的对象，碰巧对），
        //                 第二、三个 false —— 立刻露馅。
        // 所以"用 == 比内容"这个错误**一定会被抓住**，
        // 而且抓出来的证据正好能讲清 == 为什么危险：它不是一直错，是"有时候碰巧对"。
        var run = 编译并调用(代码,
            @"内容相同(""hi"", ""hi"")",
            @"内容相同(new String(""hi""), ""hi"")",
            @"内容相同(new String(""你好""), new String(""你好""))",
            @"内容相同(""hi"", ""HI"")",
            @"内容相同(""hi"", ""hi "")");
        if (!run.编译通过) return 编译不通过(run);

        var 实际 = 行们(run);
        if (实际.Count == 5)
        {
            // 「字面量那组对了、new String 那组错了」= 用 == 的典型指纹
            bool 用等号的指纹 = 实际[0] == "true" && 实际[1] == "false" && 实际[2] == "false";
            if (用等号的指纹)
                return JavaCheckResult.不过(
                    "找到了：你在用 == 比较字符串的内容。\n" +
                    "      证据（这就是 == 的典型表现）：\n" +
                    "          内容相同(\"hi\", \"hi\")                        → 你答 true   （碰巧对的）\n" +
                    "          内容相同(new String(\"hi\"), \"hi\")             → 你答 false  ✗ 应该 true\n" +
                    "          内容相同(new String(\"你好\"), new String(\"你好\")) → 你答 false  ✗ 应该 true\n" +
                    "      ⚠ 注意第一行 —— *== 不是一直错，是「有时候碰巧对」*，这才最危险：\n" +
                    "        · 直接写出来的字面量 \"hi\"，Java 会让它们共用同一个对象，所以 == 看起来对\n" +
                    "        · 只要是 new 出来的、拼出来的、从别处传进来的字符串，== 立刻变 false\n" +
                    "      为什么？== 比的是「**是不是同一个对象**」，equals 比的才是「内容一不一样」：\n" +
                    "          a == b          比「是不是同一张身份证」\n" +
                    "          a.equals(b)     比「两个东西长得一不一样」\n" +
                    "      改法（一句话的纪律）：\n" +
                    "          static boolean 内容相同(String a, String b) {\n" +
                    "              return a.equals(b);          ✓ 比内容\n" +
                    "          }\n" +
                    "      ⚠ 小技巧：a 万一是 null，a.equals(b) 会抛 NullPointerException。\n" +
                    "        老手会把不可能为 null 的那边写在前面：\"hi\".equals(a)。\n" +
                    "      顺便记住：这个坑不只坑字符串 —— Integer 也一样\n" +
                    "        （Integer 127 == 127 是 true，128 == 128 却是 false，因为有缓存）。");
        }

        var 结果 = 比对(run, ("内容相同(\"hi\", \"hi\")", "true"),
                             ("内容相同(new String(\"hi\"), \"hi\")", "true"),
                             ("两个 new String(\"你好\")", "true"),
                             ("内容相同(\"hi\", \"HI\") —— 大小写不同", "false"),
                             ("内容相同(\"hi\", \"hi \") —— 末尾多个空格", "false"));
        if (结果.Ok)
            return JavaCheckResult.过(
                "五个用例全对（包括用 new String 造出来的那两个）—— 你确实在比**内容**");
        return JavaCheckResult.不过(结果.Detail + "\n" +
            "      这一条要的是「两个字符串的**内容**一不一样」，写法：\n" +
            "          return a.equals(b);\n" +
            "      ⚠ 内容不同时也要返回 false（别写成「只要差不多就算相同」）：\n" +
            "          \"hi\" 和 \"HI\"   → false（大小写不同就是不同）\n" +
            "          \"hi\" 和 \"hi \"  → false（末尾那个空格也算内容）");
    }

    [Java检查说明("六个方法是不是都写了（名字、返回类型、参数都要对，而且都要有 static）")]
    public static JavaCheckResult 检查_方法都写了吗(string 代码)
    {
        var 签名们 = new (string 名字, string 正则, string 样子)[]
        {
            ("长度", @"static\s+int\s+长度\s*\(\s*String\s+\w+\s*\)",
                "static int 长度(String s)"),
            ("第一个", @"static\s+char\s+第一个\s*\(\s*String\s+\w+\s*\)",
                "static char 第一个(String s)"),
            ("变大写", @"static\s+String\s+变大写\s*\(\s*String\s+\w+\s*\)",
                "static String 变大写(String s)"),
            ("找位置", @"static\s+int\s+找位置\s*\(\s*String\s+\w+\s*,\s*String\s+\w+\s*\)",
                "static int 找位置(String s, String 片段)"),
            ("前三个", @"static\s+String\s+前三个\s*\(\s*String\s+\w+\s*\)",
                "static String 前三个(String s)"),
            ("内容相同", @"static\s+boolean\s+内容相同\s*\(\s*String\s+\w+\s*,\s*String\s+\w+\s*\)",
                "static boolean 内容相同(String a, String b)"),
        };

        var 缺 = new List<string>();
        string 净 = 去注释(代码);
        foreach (var (名字, 正则, 样子) in 签名们)
            if (!Regex.IsMatch(净, 正则)) 缺.Add($"      {名字}  —— 要写成：{样子}");

        if (缺.Count == 0)
            return JavaCheckResult.过("六个方法的返回类型、参数和 static 都写对了");

        return JavaCheckResult.不过(
            "这几个方法没有找到（或者签名和题目不一样）：\n" +
            string.Join("\n", 缺) + "\n" +
            "      一个方法的四个部分缺一不可：\n" +
            "          static  返回类型  方法名 (参数类型 参数名) { … }\n" +
            "        · **static 不能漏**（第 05 课讲过：main 是 static 的，只调得到 static 方法）\n" +
            "        · String 的 S 是**大写**（Java 区分大小写，string 不是类型）\n" +
            "        · 单个字符的类型是 char（不带引号写在小括号里），一串字符才是 String\n" +
            "        · 方法名要和题目一模一样\n" +
            "      ⚠ 不用写 class / main / import —— 课程都替你写好了，你只写方法。");
    }

    // ══════════════════ 小工具 ══════════════════

    private static JavaRun 编译并调用(string 代码, params string[] 表达式们)
    {
        var sb = new StringBuilder();
        foreach (var e in 表达式们) sb.Append("        System.out.println(").Append(e).Append(");\n");
        return JavaCheck.编译并运行(代码, null, sb.ToString());
    }

    private static JavaRun 编译并调用语句(string 代码, params string[] 语句们)
    {
        var sb = new StringBuilder();
        foreach (var s in 语句们) sb.Append("        ").Append(s).Append('\n');
        return JavaCheck.编译并运行(代码, null, sb.ToString());
    }

    /// <summary>编译没过时：先给针对性说明，没有就交给通用翻译。</summary>
    private static JavaCheckResult 编译不通过(JavaRun run)
    {
        string 信息 = run.编译信息;

        // ① 字符串的 length 少了括号 —— 本课和数组最容易混的一处
        //    实测原文：错误: 找不到符号  符号: 变量 length  位置: 类型为String的变量 s
        if (含(信息, "variable length", "变量 length"))
            return JavaCheckResult.不过(
                "字符串的 length 少了两个括号。\n" +
                "      编译器报的是「找不到符号：变量 length」—— 它在找一个字段，\n" +
                "      而 String 上根本没有叫 length 的字段，只有 length() 这个方法。\n" +
                "          s.length()      ✓ 字符串：方法，**要括号**\n" +
                "          s.length        ✗\n" +
                "      ⚠ 和第 06 课的数组正好相反，别混：\n" +
                "          int[] a;   a.length      ← 数组：属性，**不加括号**\n" +
                "          String s;  s.length()    ← 字符串：方法，**要括号**\n" +
                "      为什么会这样？数组的长度是 Java 语言**内建**的东西（不是类），\n" +
                "      而 String 是人写出来的类，长度只能做成方法。");

        // ② 字符串用了方括号取字符
        if (含(信息, "array required", "需要数组") || (含(信息, "cannot find symbol", "找不到符号") && 含(信息, "String") && 含(信息, "]")))
            return JavaCheckResult.不过(
                "字符串不能用方括号取字符。\n" +
                "          方括号 a[i] 只对**数组**有效；字符串要用 charAt：\n" +
                "          s.charAt(0)      ✓ 第一个字符\n" +
                "          s[0]             ✗ 编译不过\n" +
                "      ⚠ 不过下标都是从 0 开始数的，这一点两者一样。");

        // ③ 忘了 static（第 05 课的正题，这里再提醒一次）
        if (含(信息, "static context", "静态上下文"))
            return JavaCheckResult.不过(
                "有的方法没有写 static，从 main 里调不到它。\n" +
                "      main 是 static 的，只有 static 方法能直接按名字调用：\n" +
                "          static int 长度(String s) { return s.length(); }\n" +
                "      每个方法前面都要有 static（第 05 课专门讲过这一条）。");

        // ④ 声明了返回类型却没有 return
        if (含(信息, "missing return statement", "缺少返回语句"))
            return JavaCheckResult.不过(
                "方法声明了要交出结果，但方法体里**没有 return**。\n" +
                "      比如 变大写 要写成：\n" +
                "          static String 变大写(String s) { return s.toUpperCase(); }\n" +
                "      ⚠ 这里有个本课特有的坑：**只写 s.toUpperCase(); 是不够的** ——\n" +
                "        它返回一个新字符串，你不 return，那个新字符串就丢了。\n" +
                "        （字符串改不了，所有「改」都是「返回一个新的」。）");

        // ⑤ 方法名/类型对不上
        if (含(信息, "cannot find symbol", "找不到符号"))
            return JavaCheckResult.不过(
                "编译器不认识这个名字 —— 通常是有个方法没写、或者名字/类型和题目不一样。\n" +
                "      这一课要求的六个方法：\n" +
                "          static int 长度(String s)\n" +
                "          static char 第一个(String s)\n" +
                "          static String 变大写(String s)\n" +
                "          static int 找位置(String s, String 片段)\n" +
                "          static String 前三个(String s)\n" +
                "          static boolean 内容相同(String a, String b)\n" +
                "      ⚠ 常见原因：方法名写了英文（length 之类）—— 名字要和题目一模一样；\n" +
                "        或者把 String 写成了小写 string（Java 区分大小写，没有 string 这个类型）。");

        if (含(信息, "cannot be applied to given types", "应用到给定类型"))
            return JavaCheckResult.不过(
                "调用时给的参数和方法声明的对不上（个数或类型不一样）。\n" +
                "      这一课要求的参数：找位置 和 内容相同 要**两个** String，其余都要一个 String。");

        if (含(信息, "incompatible types", "不兼容的类型"))
            return JavaCheckResult.不过(
                "类型对不上。这一课最容易撞的两处：\n" +
                "        · 第一个 的返回类型是 char（单个字符），\n" +
                "          如果你 return \"j\";（双引号）那是 String，要用单引号 'j'\n" +
                "          或者干脆返回 s.charAt(0) 交给编译器去认\n" +
                "        · 前三个 / 变大写 的返回类型是 String，别把 charAt 的结果直接返回");

        return JavaCheck.编译失败说明(run);
    }

    /// <summary>把实际打印的行和期望逐条比对。</summary>
    private static JavaCheckResult 比对(JavaRun run, params (string 说明, string 期望)[] 用例们)
    {
        var 实际 = 行们(run);
        if (实际.Count != 用例们.Length)
            return JavaCheckResult.不过(
                $"应该打印 {用例们.Length} 行（每个用例一行），实际打印了 {实际.Count} 行：\n" +
                "          " + (实际.Count == 0 ? "（没有任何输出）" : string.Join(" ⏎ ", 实际)) + "\n" +
                "      常见原因：方法里忘了 return（那就没有值可打印），\n" +
                "      或者程序中途抛了异常（比如 substring / charAt 用到了字符串外面去）。");

        var 错 = new List<string>();
        for (int i = 0; i < 用例们.Length; i++)
            if (实际[i] != 用例们[i].期望)
                错.Add($"      {用例们[i].说明}：得到 {显示(实际[i])}，应该是 {显示(用例们[i].期望)}");

        if (错.Count == 0) return JavaCheckResult.过($"全部 {用例们.Length} 个用例都正确");
        return JavaCheckResult.不过($"通过 {用例们.Length - 错.Count}/{用例们.Length}：\n" + string.Join("\n", 错));
    }

    private static List<string> 行们(JavaRun run) => run.行.Select(l => l.Trim()).ToList();

    /// <summary>报错文本里认不认得出这些片段（中英都要认，见第 05 课文件头的说明）。</summary>
    private static bool 含(string 文本, params string[] 片段)
    {
        foreach (var f in 片段)
            if (文本.Contains(f, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>去掉注释再做语法（正则）检查 —— 注释里的字会骗过正则（第 05 课实测踩过）。</summary>
    private static string 去注释(string 代码)
    {
        string 无块注释 = Regex.Replace(代码, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(无块注释, @"//[^\n]*", " ");
    }

    private static string 显示(string s) => s.Length == 0 ? "（空）" : s;
}
