# Java 第 17~40 课 · 作者注意事项（内部材料，验证完会删掉）

> 你是谁：在 `D:\Users\etgya\Desktop\Project\引擎\LearnCode` 这个中文编程教学项目里
> 补写 Java 课程。C++ 那边已经有 41 课，Java 只有 00~16，要把 17~40 补齐，
> 内容照 C++ 那边的算法主线，但**用 Java 的惯用法实现**。
>
> **只加你自己的文件**：`csharp\java-lessons\Lessons\第NN课_<主题>.cs`。
> 不要改 `_build\` 下任何脚本、不要改 `HybridEngineLight` 库、不要碰 00~16 课。

## 一、最权威的样板：已经写好并验证通过的 `第17课_二维数组.cs`

**动手之前先完整读一遍它**（`csharp\java-lessons\Lessons\第17课_二维数组.cs`）。
它每一处都是照着现有课程的既有形状来的，而且已经实跑通过。
格式上有疑问，以它为准。

另外可以对照读：
- `csharp\java-lessons\Lessons\第06课_数组与集合.cs` —— 最早用「骨架 + 调用点」的课
- `csharp\java-lessons\Lessons\第16课_集合与泛型.cs` —— 最后定型的课
- `csharp\cpp-lessons\Lessons\第17课_排序入门.cs` —— C++ 那边同一课题的标准形状

## 二、文件骨架

```csharp
// 第 NN 课：<主题> —— <一句话>
//
// ══ 这一课要教什么 ══
// <两三句。说清「为什么需要这个知识」，不要复述语法>
//
// ══ 检查设计说明（给维护者）══
// <核心检查点是哪一条？学生能用什么偷懒办法蒙过去？那条检查怎么防住的？>

using System.Collections.Generic;
using System.Text.RegularExpressions;
using HybridEngineLight;
using HybridEngineLight.JavaLessons;

[Java课程("NN", "第 NN 课：<标题>",
    "1. <要求一>\n" +
    "2. <要求二>\n" +
    "3. <要求三>")]
public static class 第NN课
{
    private const string 骨架 = """
public class Main {
/*学生代码*/

    public static void main(String[] args) {
/*调用点*/
    }
}
""";

    public static string 模板 => """
...（见第四节）
""";

    public static string 参考答案 => """
...（见第六节）
""";

    [Java检查说明("<一句给学生看的话>")]
    public static JavaCheckResult 检查_xxx(string 代码) { ... }
    ...
}
```

硬性规矩（违反运行器就发现不了，或者学生那边炸）：

1. 特性必须是 `[Java课程(...)]` —— **不是** `[课程(...)]`（那是 C++/C# 线的）。
2. 检查方法名必须以 `检查_` 开头，签名固定
   `public static JavaCheckResult 检查_xxx(string 代码)`，
   而且**每条都要有 `[Java检查说明("...")]`**。
3. 文件第一行必须是 `// 第 NN 课：...`。
4. `"""..."""` 原始字符串里**不能出现三个连续的双引号**，
   也不要以 `"` 结尾，也不要以 `"` 开头。
   （Java 代码里要打印引号，用 `\"` 转义写法，别在 C# 里写裸的三个引号。）
5. **模板和参考答案的正文里不要用英文双引号**，中文引号一律用「」。
   ⚠ 例外：`Java代码本身` 里的引号（比如 `System.out.println("行和甲|" + …)`）必须保留英文引号 ——
   那是代码，不是讲解。这条规矩管的是**讲解文字**。

## 三、学生代码怎么被编译：骨架 + 调用点（最重要的一条）

Java 的公共类名必须和文件名一致，而运行器固定编译 `Main.java`。
所以这一门课的惯例是：**学生只写方法**，不写 class、不写 main、不写 import ——
骨架把它们塞进 `class Main` 里。

于是：

- 学生的方法必须是 `static`（骨架从 `main` 里调）。
- **调用点**是一段 Java 语句，插在骨架的 `main` 里，负责造数据、调学生的方法、
  把结果打到屏幕上，格式统一是 `System.out.println("标记|" + 表达式);`
- 参考答案**只能是一串方法**：
  **绝不能带 `public class Main {`、`public static void main`、`import ...`。**
  （带了就会变成「类里套类」，或者和骨架里的 main 撞名 ——
   C++ 契约里记着这个坑：学生会以为是自己写错了。）
  需要 `ArrayList` / `Arrays` 之类时，骨架里预先写好 import。

## 四、模板（`模板 => """…"""`）的固定形状

照 `第17课_二维数组.cs` 一节不差地抄结构，只换内容：

```
// 第 NN 课：<标题>
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**<一句话>**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    <主题>
//
//  需要先会
//    第 XX 课：…
//    第 XX 课：…
//
// ─────────────────────────────────────────────────────
//  先介绍：<为什么需要它>
// ─────────────────────────────────────────────────────
//
// <具体例子，不要空话>
//
// ─────────────────────────────────────────────────────
//  一、<第一块知识>
// ─────────────────────────────────────────────────────
//
// <速查表式的对照，用 4 空格缩进的代码块>
//
// ─────────────────────────────────────────────────────
//  二、<第二块>
// ─────────────────────────────────────────────────────
//
// ─────────────────────────────────────────────────────
//  三、<第三块>
// ─────────────────────────────────────────────────────
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：<那个坑>
// ─────────────────────────────────────────────────────
//
// <错误写法>
//
// 你会看到（javac / java 的真实报错原文）：
//
//     Exception in thread "main" java.lang.……: ……
//
// 为什么：<讲清机制>
// ⇒ <怎么改>
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   <别的课的事> → 第 XX 课。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 你**只写方法**：不用写 class，不用写 main，也不用写 import ——
//    课程已经把这些都包好了。每个方法都要带 **static**。
//
//   1. static ……
//   2. static ……
//   （编号用 // 1. 这种格式，和「要做」一致）
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
    static … 方法名(…) {
        return …;
    }
```

**「⚠ 这一课唯一要记住的坑」那一节的报错**：
必须是你这一课真的会撞上的、而且是**真实的** javac / java 报错原文
（可以自己去 `_build\..\` 外面用 javac 试一下，或者从
`csharp\HybridEngineLight\ExplainJava.cs` 里抄一条已经实测过的原文）。
不许编。宁可写一条你确定存在的（比如 `NullPointerException`、
`ArrayIndexOutOfBoundsException`、`ConcurrentModificationException`、
`StackOverflowError`），也不要瞎写。

**模板末尾填的学生方法签名必须和调用点里调用的名字、参数、返回类型完全一致。**

## 五、检查怎么写

### 5.1 可用的工具

```csharp
var run = JavaCheck.编译并运行(学生代码, 骨架, 调用点);   // 主用这个
var run = JavaCheck.直接运行(学生代码);                    // 学生写完整程序时才用
JavaCheck.编译失败说明(run)                                // 返回 JavaCheckResult（含中文翻译）
run.编译通过  run.行（List<string>）  run.全部输出  run.编译信息
run.Raw.Error（运行时错误）  run.Raw.CompileFailed
JavaRunner.FirstError(信息)                                // 挑第一条 error
JavaCheckResult.过("说明")  JavaCheckResult.不过("说明")
```

⚠ `编译失败说明(run)` 返回的是 **JavaCheckResult**，直接 `return` 它：
`if (run.Raw.CompileFailed) return JavaCheck.编译失败说明(run);`

### 5.2 每个文件里内联一份这个「小工具」段（原样照抄，别改）

```csharp
    // ══════════════════ 小工具 ══════════════════

    /// <summary>
    /// 这一课的调用点。用「标记|值」取答案 —— 报错时才能指名道姓地说
    /// 「列和(戊, 3, 1) 应该是 7」，而不是「第 6 行不对」。
    /// </summary>
    private const string 调用点 = """
        <造数据 + 一行一条 System.out.println("标记|" + 表达式);>
""";

    // 一次检查会给每个检查方法各跑一次，而它们的输入是同一份代码 ——
    // 编译一次就够了（每次编译要起一次 javac，一两秒）。
    private static string 上次的代码 = "";
    private static JavaRun 上次的运行;

    private static JavaRun 跑一次(string 代码)
    {
        if (上次的运行 != null && 上次的代码 == 代码) return 上次的运行;
        上次的代码 = 代码;
        var 包 = JavaCheck.编译并运行(代码, 骨架, 调用点);
        // ⚠ 编译不过时不缓存 —— 那说明还没跑到「比结果」这一步。
        上次的运行 = 包.编译通过 ? 包 : null;
        return 包;
    }

    /// <summary>空串 = 可以接着比结果；非空 = 这条检查到此为止的说明。</summary>
    private static string 先别急着比(JavaRun run)
    {
        if (run.Raw.CompileFailed)
            return JavaCheck.编译失败说明(run).Detail;
        if (run.Raw.Error != null) return "程序没跑完就退出了：\n      " + run.Raw.Error;
        return "";
    }

    /// <summary>比一批「标记|值」。同一个标记在调用点里出现几次，就给几组。</summary>
    private static JavaCheckResult 比(JavaRun run, params (string 标记, string 期望, string 说明)[] 用例们)
    {
        var 错 = new List<string>();
        foreach (var (标记, 期望, 说明) in 用例们)
        {
            string 得到 = 看看(run, 标记);
            if (得到.Length == 0)
                错.Add("      " + 说明 + "：没有找到这个标记的输出");
            else if (得到.Trim() != 期望.Trim())
                错.Add("      " + 说明 + "：得到「" + 得到 + "」，应该是「" + 期望 + "」");
        }
        if (错.Count == 0) return JavaCheckResult.过($"这几组都对（{用例们.Length} 组）");

        return JavaCheckResult.不过(
            "结果不对：\n" + string.Join("\n", 错) + "\n" +
            "<这一课最容易错在哪、怎么改 —— 两三行>");
    }

    /// <summary>取「标记|」之后到行尾的内容。</summary>
    private static string 看看(JavaRun run, string 标记)
    {
        foreach (var 行 in run.行)
        {
            int 位 = 行.IndexOf(标记, System.StringComparison.Ordinal);
            if (位 >= 0) return 行.Substring(位 + 标记.Length).Trim();
        }
        return "";
    }

    /// <summary>去掉注释，免得注释里的字样骗过文本检查。</summary>
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
```

### 5.3 每个文件 4~6 条检查

其中**至少一条是标了 ★ 的「核心检查点」**（`[Java检查说明("★ …")]`）——
它要防住「能输出对但没学到东西」的偷懒解法。

报错文字要求（这是课程质量的关键）：
**发生了什么 → 你写的是什么 → 应该怎么写 →（可选）顺便记住什么。**
反面例子：「输出不对。」
正面例子：「期望 7，实际 5。你按第 0 行的长度遍历了，锯齿数据里第 2 行只有 1 格 ——
按列走时每行都要问 `格[i].length`。」

### 5.4 性能类检查

要「淘汰暴力解法」时用**短超时**，别等 60 秒：
`JavaRunner.BuildAndRun(程序, 超时毫秒)` —— 但它不带骨架，所以要么自己拼骨架，
要么用 `JavaCheck.编译并运行` 然后靠输入规模把暴力解法逼到超时。
推荐后者：**把 n 取得足够大**，让暴力解法自己撞上 60 秒默认超时
（症状是 run.Raw.Error 里有「运行超时」）。第一次写课时如果拿不准，
就先用大 n 试一遍，确认暴力解法真的超时、而正确答案 1 秒内跑完。

## 六、参考答案（`参考答案 => """…"""`）

- 是**真的能通过你写的所有检查**的代码。（我们之后会实跑验证，跑不过会被退回来。）
- **只有方法**，没有 class / main / import。
- 可以有中文注释解释关键那一行。
- 要写成**学生看了能学到东西**的样子，不要为了短而牺牲可读性。

## 七、写完必须自己验证（硬性）

```powershell
$env:DOTNET_CLI_UI_LANGUAGE="en"
cd D:\Users\etgya\Desktop\Project\引擎\LearnCode

dotnet build csharp/java-lessons/JavaLessons.csproj
dotnet run --project csharp/java-lessons                  # 应当看到你写的那几课
dotnet run --project csharp/java-lessons 检查 NN           # 第一次会生成答题文件
```

然后把自己写的参考答案写进答题文件再跑检查。**有一条专用脚本**：

```powershell
python _build\_answer.py java NN            # 把第 NN 课的参考答案写进 java-answers\
dotnet run --project csharp/java-lessons 检查 NN
```

反复跑到 `全部通过（N/N）`。⚠ 别跳过红的 —— 看它说什么，改到全过。

⚠⚠ **不要跑 `python _build\_verify_compiled.py`** —— 那会把整个 `java-answers`
目录清空，和别的作者互相踩。总验证由主负责人独占跑。

## 八、反面测试（硬性，每课至少一个）

写一个「偷懒但输出对」的答案，确认被你的核心检查点抓住。做法：
把它写进 `java-answers\NN_*.java`（覆盖参考答案），跑 `检查 NN`，**复制真实输出**，
然后把参考答案写回去。把结论记下来（哪一课、什么偷懒写法、被哪条检查抓住、原话是什么）。

偷懒写法的例子：
- 把答案按调用点的固定数据写死（`return 7;`、打表）
- 用现成的库函数绕过要考的知识点（`Arrays.sort`、`Arrays.binarySearch`）
- 只对某一份数据凑对（换一份数据就错）
- 用暴力/递归硬算（n 一大就超时）

## 九、交付

**不要**在回复里贴正文。回复只要：
1. 加了哪几课的**文件路径**和**行数**（`(Get-Content 文件).Count`）
2. 每课 `dotnet run … 检查 NN` 的**最后一行**（`全部通过（N/N）—— 这一课过了！` 或失败原文）
3. 反面测试结论：哪一课、什么偷懒写法、被哪条检查抓住、报错原话
4. 遇到的真问题
