// 第 22 课：文件读写 —— 程序关闭之后，数据还在
//
// ══ 这一课要教什么 ══
// 前面所有课的数据都活在内存里：程序一结束，全没了。
// 「把结果存下来」和「把昨天的数据读回来」是程序真正有用的开始。
//
// 这一课要建立三件事：
//   ① 路径是一个**对象**：`Path`（`Paths.get(路径)`），不是一串字符串拼来拼去。
//   ② 两个最省事的入口：`Files.write(...)` 写一组行、`Files.readAllLines(...)` 读成 List。
//   ③ **try-with-resources** —— Java 里管「用完一定要关」的东西的写法。
//      它是 Java 对「忘了关文件」这个老问题的回答：`try (资源) { … }`，
//      出了这个花括号，资源自动关，**哪怕中间抛了异常**。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点：**必须真的用了文件 API**（不许把内容写死在内存里返回）。
//   学生最偷懒的做法：
//       static String[] 读文件(String 路径) { return new String[]{"甲", "乙"}; }
//   输出一模一样，但一个字节都没落盘 —— 而且「换个路径再读一遍」就露馅。
//   防法（三层）：
//     ① 去掉注释后源码里必须出现 `Files.` 或 `BufferedReader`（真的碰了文件 API）
//     ② 数行数 里必须出现 `try (`（try-with-resources，这一课的正题）
//     ③ 调用点**换第二个文件再走一遍同样的流程** —— 只对第一份内容写死的会错
//   实测：写死的实现在第二份文件（三行）上仍然返回两行，被 ② ③ 一起抓住。
// 另外：检查会真在临时目录里落盘，运行结束后把文件删掉（不弄脏学生的机器）。
//
// ⚠ 学生只写方法；骨架里预置了 java.io / java.nio.file / java.util 的 import，
//   而且 main 上带 `throws Exception`（文件操作会抛 IOException，
//   这一课的重点不是异常处理，所以让它们往上抛）。

using System.Collections.Generic;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("22", "第 22 课：文件读写 —— 把数据存下来",
    "1. 写 static void 写文件(String 路径, String[] 行)：把每一行写进这个文件（覆盖原有内容）\n" +
    "2. 写 static String[] 读文件(String 路径)：把文件里的每一行读回来，一行一个元素\n" +
    "3. 写 static int 数行数(String 路径)：数一数这个文件有几行 —— **必须用 try-with-resources**\n" +
    "4. 三个方法都要真的读写磁盘（不许把内容写死在内存里返回），签名上写 throws Exception")]
public static class 第22课
{
    private const string 骨架 = """
import java.io.*;
import java.nio.file.*;
import java.util.*;

public class Main {
/*学生代码*/

    public static void main(String[] args) throws Exception {
/*调用点*/
    }
}
""";

    public static string 模板 => """
// 第 22 课：文件读写 —— 把数据存下来
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**怎么把数据存到硬盘上，再读回来**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    文件读写：Files、Path、BufferedReader、try-with-resources
//
//  需要先会
//    第 07 课：字符串
//    第 15 课：异常（文件操作会抛 IOException）
//
// ─────────────────────────────────────────────────────
//  先介绍：内存里的东西，程序一关就没了
// ─────────────────────────────────────────────────────
//
// 前 21 课所有的变量、数组、表，都住在**内存**里。
// 程序一结束，它们就消失了。
//
// 要「昨天的成绩今天还能看」，就得落到**硬盘**上 —— 也就是文件。
//
// 两个动作，一辈子就这两件事：
//
//     写   把内存里的东西搬到文件里
//     读   把文件里的东西搬回内存里
//
// ─────────────────────────────────────────────────────
//  一、路径是一个对象：Path
// ─────────────────────────────────────────────────────
//
//     Path 路径 = Paths.get("成绩.txt");
//     Path 路径 = Paths.get("C:", "temp", "成绩.txt");      ← 这样拼更稳（跨系统）
//
// ⚠ 别用字符串拼路径（`"C:\\temp\\" + 名字`）—— Windows 用反斜杠、Linux 用正斜杠，
//   拼出来的东西换个系统就不认了。`Paths.get(...)` 会按当前系统拼对。
//
// ─────────────────────────────────────────────────────
//  二、两个最省事的入口：Files.write / Files.readAllLines
// ─────────────────────────────────────────────────────
//
//     // 写：把一串行写进文件（**覆盖**原有内容）
//     Files.write(路径, 行列表);
//
//     // 读：把文件读成一个 List<String>，一行一个元素
//     List<String> 行 = Files.readAllLines(路径);
//
//     行.size()       有几行
//     行.get(0)       第一行
//
// ⚠ 文件不存在时 `readAllLines` 会抛
//
//     java.nio.file.NoSuchFileException: 成绩.txt
//
//   这是**好事**：它明确告诉你「这个文件不在」，而不是悄悄给你一个空列表。
//
// 想要 String[] 就转一下：
//
//     List<String> 表 = Files.readAllLines(路径);
//     return 表.toArray(new String[0]);
//
// ─────────────────────────────────────────────────────
//  三、try-with-resources：用完一定要关的东西
// ─────────────────────────────────────────────────────
//
// `Files.readAllLines` 很省事，可它一次性把整个文件塞进内存 ——
// 文件有几个 G 的时候就不行了。真按行处理要用 `BufferedReader`：
//
//     BufferedReader 读 = new BufferedReader(new FileReader("成绩.txt"));
//     String 一行 = 读.readLine();
//     读.close();                        ← ⚠ 用完必须关！
//
// 「必须关」的原因：文件是**操作系统**的资源，不关就一直占着 ——
//   Windows 上别人会打不开这个文件，程序开久了还会把句柄用光。
//
// 问题在于：**中间抛异常的话，close() 就轮不到执行了**。
// 于是 Java 给了专门的写法：
//
//     try (BufferedReader 读 = new BufferedReader(new FileReader("成绩.txt"))) {
//         String 一行;
//         while ((一行 = 读.readLine()) != null) {      ← 读到末尾返回 null
//             // 处理这一行
//         }
//     }                                                  ← 出了这个花括号，自动 close()
//
// ⚠ 三个细节：
//   · 资源声明写在 **try 后面的圆括号里** —— 不是花括号里
//   · `readLine()` **读到文件末尾返回 null**（不是空串）—— 循环条件就靠它
//   · 空行读出来是 `""`（长度 0 的字符串），**不是 null** —— 两者要分清
//
// 想同时管两个资源就加分号：
//
//     try (BufferedReader 读 = …; BufferedWriter 写 = …) { … }
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：把内容写死在内存里冒充读写
// ─────────────────────────────────────────────────────
//
//     static String[] 读文件(String 路径) {
//         return new String[] {"甲", "乙"};        ← 编译通过、输出也「对」
//     }
//
// 你会看到：**检查里的输出可能一模一样** —— 因为文件里本来就只有这两行。
//
// 为什么它一定是错的：这个函数根本不认识「路径」这个参数。
//   换一个文件、换一台机器、把文件删掉，它给出的还是这两行 ——
//   那不是读文件，那是背答案。
//
// 检查怎么抓：换个路径再走一遍同样的流程，另外还会检查源码里
//   有没有真的出现文件 API（`Files.` / `BufferedReader`）。
//
// 附：最常见的真实报错是**路径找不着**，原文长这样：
//
//     Exception in thread "main" java.nio.file.NoSuchFileException: 成绩.txt
//
//   ⇒ 先确认这个文件**真的存在**（先写后读），再看路径对不对。
//     相对路径是相对于「程序运行时所在的那个目录」，不是源码所在的目录。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   异常怎么接（try / catch / finally）→ 第 15 课。
//   把对象存成文件（序列化）→ 不涉及。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 你**只写方法**：不用写 class，不用写 main，import 也已经准备好了
//    （java.io / java.nio.file / java.util）。每个方法都要带 **static**，
//    而且签名末尾要写 `throws Exception`（文件操作会抛 IOException）。
//
//   1. static void 写文件(String 路径, String[] 行) throws Exception
//        把 行 里的每一行写进 路径 指向的文件（**覆盖**原有内容）。
//        用 Files.write(Paths.get(路径), Arrays.asList(行)) 最省事。
//
//   2. static String[] 读文件(String 路径) throws Exception
//        把文件里的每一行读回来，返回 String[]。
//        用 Files.readAllLines(Paths.get(路径))，再 toArray。
//
//   3. static int 数行数(String 路径) throws Exception
//        返回这个文件有几行。
//        ⚠ 这一个**必须用 try-with-resources + BufferedReader** 一行一行数
//          （不是调 readAllLines 然后 size()）—— 这是这一课的正题。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
//   检查会在系统的临时目录里真的建文件、写进去、再读回来，
//   跑完把文件删掉 —— 不会弄脏你的电脑。
//
// ── 在下面写 ──
    static void 写文件(String 路径, String[] 行) throws Exception {
    }

    static String[] 读文件(String 路径) throws Exception {
        return null;
    }

    static int 数行数(String 路径) throws Exception {
        return 0;
    }
""";

    public static string 参考答案 => """
// 参考答案 —— 文件读写（只写方法，不用写 class / main / import）

// 1. 写：把一串行一次性写进文件（覆盖原有内容）
//    Arrays.asList(行) 把数组包成 List —— Files.write 要的是「一串可迭代的行」
static void 写文件(String 路径, String[] 行) throws Exception {
    Files.write(Paths.get(路径), Arrays.asList(行));
}

// 2. 读：一次性读成 List，再转成数组
//    文件不存在时它会抛 NoSuchFileException —— 那正是我们想要的信息
static String[] 读文件(String 路径) throws Exception {
    List<String> 表 = Files.readAllLines(Paths.get(路径));
    return 表.toArray(new String[0]);          /* ⚠ 参数写成 new String[0] 是标准写法 */
}

// 3. 一行一行数：这一课的正题 —— try-with-resources
//    ⚠ 资源声明写在 try 后面的**圆括号里**；出了花括号自动 close()。
//      readLine() 读到末尾返回 null（空行返回的是 ""，不是 null）。
static int 数行数(String 路径) throws Exception {
    int 行数 = 0;
    try (BufferedReader 读 = new BufferedReader(new FileReader(路径))) {
        String 一行;
        while ((一行 = 读.readLine()) != null) {
            行数 = 行数 + 1;
        }
    }                                          /* ← 到这里 读 已经自动关掉了 */
    return 行数;
}
""";

    // ══════════════════ 检查 ══════════════════

    [Java检查说明("三个方法都在，签名也对（含 throws Exception）")]
    public static JavaCheckResult 检查_函数都在(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        if (!Regex.IsMatch(净, @"static\s+void\s+写文件\s*\(\s*String\s+\w+\s*,\s*String\s*\[\s*\]\s*\w+\s*\)"))
            缺.Add("      写文件　（static void 写文件(String 路径, String[] 行) throws Exception）");
        if (!Regex.IsMatch(净, @"static\s+String\s*\[\s*\]\s+读文件\s*\(\s*String\s+\w+\s*\)"))
            缺.Add("      读文件　（static String[] 读文件(String 路径) throws Exception）");
        if (!Regex.IsMatch(净, @"static\s+int\s+数行数\s*\(\s*String\s+\w+\s*\)"))
            缺.Add("      数行数　（static int 数行数(String 路径) throws Exception）");

        if (缺.Count == 0) return JavaCheckResult.过("三个方法都在，签名也对");
        return JavaCheckResult.不过(
            "有方法没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      三个方法的签名：\n" +
            "          static void     写文件(String 路径, String[] 行) throws Exception\n" +
            "          static String[] 读文件(String 路径) throws Exception\n" +
            "          static int      数行数(String 路径) throws Exception\n" +
            "      ⚠ throws Exception 不能省：文件操作会抛 IOException，\n" +
            "        方法自己不接的话必须在签名上声明「我可能抛出去」。\n" +
            "      ⚠ 数组返回类型写成 String[]（方括号跟着类型）。");
    }

    [Java检查说明("★ 真的碰了文件 API：源码里有 Files. 或 BufferedReader")]
    public static JavaCheckResult 检查_真的用了文件API(string 代码)
    {
        // ⚠ 这一条是本课的核心检查点之一。
        //   把内容写死在内存里返回，输出可以一模一样（见 检查_换个文件也对 的说明）。
        string 净 = 去掉注释(代码);
        bool 有Files = 净.Contains("Files.");
        bool 有Reader = 净.Contains("BufferedReader");
        if (有Files && 有Reader)
            return JavaCheckResult.过("用了 Files.* 和 BufferedReader —— 是真的在读写磁盘");

        var 缺 = new List<string>();
        if (!有Files) 缺.Add("      · 没看到 Files.（Files.write / Files.readAllLines）");
        if (!有Reader) 缺.Add("      · 没看到 BufferedReader（数行数 那一题要用它）");
        return JavaCheckResult.不过(
            "这一课要求真的读写磁盘，但源码里没找到该用的东西：\n" + string.Join("\n", 缺) + "\n" +
            "      最省事的两个入口：\n" +
            "          Files.write(Paths.get(路径), Arrays.asList(行));\n" +
            "          List<String> 表 = Files.readAllLines(Paths.get(路径));\n" +
            "      按行读要用：\n" +
            "          try (BufferedReader 读 = new BufferedReader(new FileReader(路径))) { … }\n" +
            "      ⚠ 把内容**写死在 return 里**也能让输出看起来对 ——\n" +
            "        但那个函数根本不认识「路径」这个参数，换个文件给出的还是同一份内容。");
    }

    [Java检查说明("★ 数行数 用的是 try-with-resources（资源写在 try 后面的圆括号里）")]
    public static JavaCheckResult 检查_用了trywithresources(string 代码)
    {
        string 净 = 去掉注释(代码);
        string 体 = 函数体(净, "数行数");
        if (体 == null)
            return JavaCheckResult.不过("没找到 数行数 的函数体 —— 先确认这个方法写全了（花括号配对）。");

        if (!体.Contains("try") || !体.Contains("("))
            return JavaCheckResult.不过(
                "数行数 里没有 try-with-resources。\n" +
                "      这一课的正题就是它：**用完一定要关的资源**要写在 try 后面的圆括号里：\n" +
                "          try (BufferedReader 读 = new BufferedReader(new FileReader(路径))) {\n" +
                "              String 一行;\n" +
                "              while ((一行 = 读.readLine()) != null) { … }\n" +
                "          }        ← 出了这个花括号，读 自动关掉，**抛异常也照样关**\n" +
                "      ⚠ 资源写在**圆括号**里，不是花括号里。\n" +
                "      ⚠ 也可以直接数 Files.readAllLines(...).size() —— 但那不是这一课要练的写法，\n" +
                "        题目明确要求用 BufferedReader 一行一行数。");

        if (!体.Contains("readLine"))
            return JavaCheckResult.不过(
                "数行数 里没有看到 readLine()。\n" +
                "      按行读的循环长这样（⚠ 读到末尾返回的是 **null**，不是空串）：\n" +
                "          String 一行;\n" +
                "          while ((一行 = 读.readLine()) != null) { 行数 = 行数 + 1; }");

        return JavaCheckResult.过("数行数 用的是 try-with-resources + readLine —— 这一课的正题做对了");
    }

    [Java检查说明("写进去、读回来、数行数，三段都真的对了")]
    public static JavaCheckResult 检查_第一份文件(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("读回|", "甲 乙 丙", "写进去 {甲, 乙, 丙} 再读回来，三行都在"),
            ("行数|", "3", "数行数 应当是 3"),
            ("大小|", "3", "读回来的数组长度是 3"),
            ("第二行|", "乙", "读回来的第 1 个元素（下标 1）是 乙"),
            ("已删|", "true", "检查跑完把临时文件删掉了"));
    }

    [Java检查说明("★ 换一个文件也对（把内容写死在内存里的写法会在这里露馅）")]
    public static JavaCheckResult 检查_换个文件也对(string 代码)
    {
        // ⚠ 这一条是本课的核心检查点之二：
        //   「读文件」如果写死成 return new String[]{...}，第一个文件看着对，
        //   换成第二个文件（内容、行数都不一样）就立刻错。
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停.Length > 0) return JavaCheckResult.不过(停);
        return 比(run,
            ("二读回|", "1 2 3 4 5", "第二个文件里是 {1, 2, 3, 4, 5}，读回来应当是这五行"),
            ("二行数|", "5", "第二个文件有 5 行"),
            ("二大小|", "5", "读回来的数组长度是 5"),
            ("空读回|", "0", "⚠ 第三个文件是**空的**：读回来应当是 0 行（不是崩，也不是残留上一次的内容）"),
            ("空行数|", "0", "空文件的行数是 0"));
    }

    // ══════════════════ 工具 ══════════════════

    private const string 调用点 = """
        String 目录 = System.getProperty("java.io.tmpdir");
        String 甲路径 = 目录 + java.io.File.separator + "learncode_22_甲.txt";
        String 乙路径 = 目录 + java.io.File.separator + "learncode_22_乙.txt";
        String 空路径 = 目录 + java.io.File.separator + "learncode_22_空.txt";

        写文件(甲路径, new String[]{"甲", "乙", "丙"});
        String[] 甲回来 = 读文件(甲路径);
        System.out.println("读回|" + String.join(" ", 甲回来));
        System.out.println("行数|" + 数行数(甲路径));
        System.out.println("大小|" + 甲回来.length);
        System.out.println("第二行|" + 甲回来[1]);

        写文件(乙路径, new String[]{"1", "2", "3", "4", "5"});
        String[] 乙回来 = 读文件(乙路径);
        System.out.println("二读回|" + String.join(" ", 乙回来));
        System.out.println("二行数|" + 数行数(乙路径));
        System.out.println("二大小|" + 乙回来.length);

        写文件(空路径, new String[]{});
        String[] 空回来 = 读文件(空路径);
        System.out.println("空读回|" + 空回来.length);
        System.out.println("空行数|" + 数行数(空路径));

        boolean 删了 = new java.io.File(甲路径).delete();
        删了 = new java.io.File(乙路径).delete() && 删了;
        删了 = new java.io.File(空路径).delete() && 删了;
        System.out.println("已删|" + 删了);
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
        if (run.Raw.CompileFailed) return JavaCheck.编译失败说明(run).Detail;
        if (run.Raw.Error != null)
        {
            string 说明 = "程序没跑完就退出了：\n      " + run.Raw.Error + "\n";
            if (run.Raw.Error.Contains("NoSuchFile") || run.Raw.Error.Contains("找不到"))
                说明 += "      ⚠ 这是文件**不存在**：先确认你是不是**先写后读**的。\n" +
                        "        相对路径是相对「程序运行时所在的目录」，不是源码目录。";
            else if (run.Raw.Error.Contains("NullPointer"))
                说明 += "      ⚠ 空指针：读文件 返回了 null（应该返回一个数组，哪怕是空的）。";
            return 说明;
        }
        return "";
    }

    private static JavaCheckResult 比(JavaRun run, params (string 标记, string 期望, string 说明)[] 用例们)
    {
        var 错 = new List<string>();
        foreach (var (标记, 期望, 说明) in 用例们)
        {
            string 得到 = 看看(run, 标记);
            if (得到.Length == 0) 错.Add("      " + 说明 + "：没有找到这个标记的输出");
            else if (得到.Trim() != 期望.Trim())
                错.Add("      " + 说明 + "：得到「" + 得到 + "」，应该是「" + 期望 + "」");
        }
        if (错.Count == 0) return JavaCheckResult.过($"这几组都对（{用例们.Length} 组）");

        return JavaCheckResult.不过(
            "结果不对：\n" + string.Join("\n", 错) + "\n" +
            "      这一课三个动作要连起来看：\n" +
            "          写   Files.write(Paths.get(路径), Arrays.asList(行));\n" +
            "          读   Files.readAllLines(Paths.get(路径)) → toArray(new String[0])\n" +
            "          数   try (BufferedReader 读 = new BufferedReader(new FileReader(路径))) { … }\n" +
            "      ⚠ 读完不要忘了空文件：0 行、长度为 0 的数组，都不该崩。\n" +
            "      ⚠ 如果第一个文件对了、第二个错了，那多半是「读」把内容写死了 ——\n" +
            "        它必须真的按**传进来的路径**去读。");
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
