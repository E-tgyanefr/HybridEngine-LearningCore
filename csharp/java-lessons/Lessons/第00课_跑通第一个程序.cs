using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("00", "第 00 课：跑通第一个程序 —— 把 JDK 装好",
    """
1. 确认 javac / java / dotnet 都在（本课会真的去调它们）
2. 搞清楚 Java 的程序结构和别的语言哪里不一样
3. 让编译器报一次错，并且学会从第一条 error 读起
""")]
public static class 第00课
{
    public static string 模板 => """
// 第 00 课：跑通第一个程序 —— 把 JDK 装好
//
// 这一课**不教语法**，只干一件事：让你这台机器真的能编译、能运行。
//
// ─────────────────────────────────────────────────────
//  先介绍：Java 不是「装上就能跑」的语言
// ─────────────────────────────────────────────────────
//
// 它要 JDK，而网上大量教程让人装的是 JRE。**JRE 里没有 javac**，
// 于是他永远编不过，还找不到原因。这一步值得单独占一课。
//
//  跑通之后你拿到的是**信心**：知道自己的机器真的能编、能跑，
// 后面每一课的报错才都是「我写错了」，而不是「环境又怎么了」。
//
// ─────────────────────────────────────────────────────
//  一、需要装两样（都免费）
// ─────────────────────────────────────────────────────
//
//   ① .NET SDK —— 课程运行器。它替你调 javac 和 java、跑起来看结果。
//
//        winget install Microsoft.DotNet.SDK.8
//
//   ② JDK —— Java 的开发包。**不是 JRE。**
//
//        winget install Microsoft.OpenJDK.21
//
//      或者去 https://adoptium.net 下 Temurin。
//      装完**重开一个窗口**（让 PATH 生效），敲这两条：
//
//        javac -version
//        java  -version
//
//      两条都能看到版本号才算成。
//
// ⚠⚠ **JDK ≠ JRE** —— 这是这一课最重要的一句话：
//
//      JRE = Java Runtime Environment   只能**运行**别人编好的程序
//      JDK = Java Development Kit       = JRE + **javac 编译器** + 工具
//
//   只装 JRE 的话，`java` 有、`javac` 没有 —— 而你要自己写代码，必须要 javac。
//
// ─────────────────────────────────────────────────────
//  二、Java 的程序结构和别的语言不一样
// ─────────────────────────────────────────────────────
//
//  ① **不能有游离的代码。** 每一样东西都必须装在一个**类**里面。
//
//        Python 可以:   print("hi")                    ← 文件顶层直接写
//        C 可以:        int main() { printf(...); }    ← 函数里直接写
//        Java 不行:     必须 类 → 方法 → 语句，一层套一层
//
//  ② **入口方法长这样**，一个字都不能错：
//
//        public static void main(String[] args) { ... }
//
//  ③ **public 的类名必须和文件名一致**（区分大小写）。
//
//        文件叫 Main.java  ⇒  类必须叫 public class Main
//
//  这一条**别的语言都没有**。C 的 main 是小写的函数，
//  而 Java 的 main 是方法、类才是外壳 —— 别把两件事混起来。
//
// ─────────────────────────────────────────────────────
//  三、Java 比 C 多一层：字节码
// ─────────────────────────────────────────────────────
//
//      Main.java
//         ↓  javac Main.java     编译器
//      Main.class                 **字节码**（不是机器码）
//         ↓  java Main           JVM 解释执行
//
// ⚠ 运行的时候写的是 **java Main**：一个**类名**，不带任何后缀。
//
//      java Main             ✓
//      java Main.class       ✗ 错误: 找不到或无法加载主类 Main.class
//
// ─────────────────────────────────────────────────────
//  四、这一课唯一要练的手艺：读第一条 error
// ─────────────────────────────────────────────────────
//
// 报错长这样（这是**故意写错**的代码）：
//
//      Main.java:5: 错误: 需要';'
//          System.out.println("你好")
//                                 ^
//      1 个错误
//
// 拆开看：**哪个文件、第几行、什么错**。先看行号。
//
// 两条铁律：
//
//   ① **只看第一条 error。** 后面那几十条多数是它引起的连锁反应。
//   ② **报错行号常常要往上多看一行。** 编译器是读到「它不认识的东西」
//      才报的错，而那个东西往往是**上一行**漏了什么造成的。
//
// ⚠ 本机是中文环境，javac 的报错就是中文的（`错误: 需要';'`）。
//   换一台英文环境的机器，同样的错会写成 `error: ';' expected` ——
//   **是同一件事**，别被语言吓住。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  这一课没有算法要写，只有三件事：
//
//    1. 确认 javac、java、dotnet 都在
//       （界面首页的「环境自检」会显示；命令行敲 javac -version 也行）
//
//    2. 让编译器**报一次错**，并且读懂它
//       下面留了一个完整的程序。先故意漏掉一个分号 → 检查 → 看报错 →
//       把分号补上 → 再检查。
//
//    3. 让它打印出下面这三行，就算跑通了：
//
//          工具链就绪
//          我准备好学 Java 了
//          类 → main → 语句
//
//   三行的**顺序**别改 —— 检查是按顺序比对的。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。
//     命令行（在源码目录里）：dotnet run --project csharp/java-lessons 检查 00
//     卡住了看答案（先自己写一遍）：… 答案 00
//
//   ⚠ 如果第一项检查（工具链）就红了，**那是环境问题，不是你代码的问题** ——
//     先看命令行敲 javac -version 有没有反应，别去改代码。
//
// ── 在下面写你的代码 ──

public class Main {
    public static void main(String[] args) {
        System.out.println("工具链就绪");
        /* ⚠ 先故意把下面这一行的分号删掉，检查一次，看看 javac 怎么说。
           读完报错再把分号补回来。 */
        System.out.println("我准备好学 Java 了");
        System.out.println("类 → main → 语句");
    }
}
""";

    public static string 参考答案 => """
/* 这一课练的是「读报错」—— 先故意漏一个分号编译一次，读完报错再补回来。

   报错大概长这样：
       Main.java:5: error: ';' expected
   意思是「第 5 行这里我以为该有个分号」。

   ⚠ 如果报错是「类 Main 是公共的, 应在名为 Main.java 的文件中声明」，
     那是**类名和文件名不一致** —— Java 特有的规矩。

   程序结构是三层：类 → 方法 → 语句。
   Java 里没有「游离的代码」，任何一行都必须待在这三层里面。 */
public class Main {
    public static void main(String[] args) {
        System.out.println("工具链就绪");
        System.out.println("我准备好学 Java 了");
        System.out.println("类 → main → 语句");
    }
}
""";


    [Java检查说明("工具链就绪（gcc 与 dotnet 都在）")]
    public static JavaCheckResult 检查_1a(string 代码)
    {
        return JavaLesson.工具链就绪(代码);
    }

    [Java检查说明("程序能编译、能跑起来（说明 javac / java 都就绪）")]
    public static JavaCheckResult 检查_1b(string 代码)
    {
        var run = JavaCheck.直接运行(代码);
        if (!run.编译通过) return JavaCheck.编译失败说明(run);
        string[] 期望 = {
            "工具链就绪",
            "我准备好学 Java 了",
            "类 → main → 语句",
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

    [Java检查说明("类名与文件名一致（public class Main）")]
    public static JavaCheckResult 检查_2(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("public class Main"))
            return JavaCheckResult.不过("没找到「public class Main」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("入口方法写对了（public static void main）")]
    public static JavaCheckResult 检查_3(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("public static void main"))
            return JavaCheckResult.不过("没找到「public static void main」—— 这一课要求用到它。");
        return JavaCheckResult.过();
    }

    [Java检查说明("用的是 System.out.println（Java 的打印方式）")]
    public static JavaCheckResult 检查_4(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("System.out.println"))
            return JavaCheckResult.不过("没找到「System.out.println」—— 这一课要求用到它。");
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
