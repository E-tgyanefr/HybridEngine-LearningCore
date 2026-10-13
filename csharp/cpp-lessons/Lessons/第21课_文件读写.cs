// 第 21 课：文件读写 —— 把数据存到硬盘上
//
// ══ 这一课要教什么 ══
// 前面的课都是"程序一关，数据就没了"。这一课学三件事：
//     ① 往文件里写：`std::ofstream 出(名字);` 然后像用 std::cout 一样 `出 << …`
//     ② 从文件里读：`std::ifstream 入(名字);` 然后像用 std::cin 一样 `入 >> …`
//     ③ 一行一行读：`std::getline(入, 行);`
//
// 还有一件竞赛里天天用的事：**文件重定向**（把 cin/cout 接到文件上），
// 这样比赛的输入输出就走文件，程序本身一个字都不用改。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点：**程序必须真的去读那个文件，而不是把答案写死。**
//   做法：检查跑三遍，每遍先写一份**内容不同**的输入文件，再看输出文
//   件跟着变不变。写死的实现三遍输出一样，一眼就露馅。
//   这比"文本里有没有 ifstream"硬得多 —— 它验的是**行为**。
// 另一条：`#include <fstream>` 必须有（否则编译器根本不认识 ofstream），
//   本机报错原文见下面模板里的坑。
//
// ⚠ 给维护者：这一课学生写的是**完整程序**（自带 main），所以一律 `直接运行`，
//   绝不能用 `编译并运行`（骨架里也有 main，会 redefinition）。
//   程序的工作目录是随机的临时目录 —— 所以路径一律用**相对名**，
//   由课程在他那个目录里写好输入文件、再读他写出来的输出文件。

using System.Collections.Generic;
using System.IO;
using HybridEngineLight;
using HybridEngineLight.CppLessons;

[课程("21", "第 21 课：文件读写 —— 把数据存到硬盘上",
    "1. 写一个**完整程序**（自带 main 和 #include）：从文件 data.in 里读入一串整数（空格或换行隔开），把其中**所有**偶数（含第一行那个）的个数写到 data.out\n" +
    "2. 读的时候要用 std::ifstream、写的时候要用 std::ofstream（记得 #include <fstream>）\n" +
    "3. 程序要真的从文件读 —— 把答案写死是过不了的")]
public static class 第21课
{
    public static string 模板 => """
// 第 21 课：文件读写 —— 把数据存到硬盘上
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**怎么让数据活在程序外面**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    ifstream / ofstream / getline，以及竞赛里的文件重定向
//
//  需要先会
//    第 02 课：输入与计算（cin / cout 就是这一课要换成文件的东西）
//    第 04 课：循环（读到文件末尾靠它）
//
// ─────────────────────────────────────────────────────
//  先介绍：程序一关，数据就没了
// ─────────────────────────────────────────────────────
//
// 前面所有课的数据都在内存里：变量、数组 —— 程序一退出，全没了。
// 想让数据留下来（成绩单、日志、比赛的测试数据），就得写进**文件**。
//
// ─────────────────────────────────────────────────────
//  一、往文件里写：ofstream
// ─────────────────────────────────────────────────────
//
//     #include <fstream>              ← ⚠ 少了这一行，编译器不认识 ofstream
//
//     std::ofstream 出("结果.txt");   ← 建一个"输出文件流"，名字就是文件名
//     出 << "你好" << '\n';           ← 用法和 std::cout **一模一样**
//     出 << 42 << '\n';
//     出.close();                     ← 用完关掉（不写也行，程序退出时会自动关）
//
// 关键认识：**文件流和 cout 是同一种东西**，只是输出的目标从屏幕换成了文件。
//   所以 `<<` 怎么用，这里就怎么用。
//
// ⚠ 文件**不存在就自动新建**，**存在就被清空重写** —— 这一点要小心。
//
// ─────────────────────────────────────────────────────
//  二、从文件里读：ifstream
// ─────────────────────────────────────────────────────
//
//     std::ifstream 入("数据.txt");
//     if (!入) { return 1; }          ← 文件没打开成功时，入 会变成"假"
//
//     int 甲;
//     入 >> 甲;                       ← 用法和 std::cin **一模一样**
//
// 和 cin 的区别只有一条：**cin 读键盘，ifstream 读文件**。
//   连"读不到就失败"这个行为都一样 —— 所以下面那个读到末尾的写法也通用。
//
// ─────────────────────────────────────────────────────
//  三、一行一行读：getline
// ─────────────────────────────────────────────────────
//
//     入 >> 甲        按"一个数/一个词"读，**自动跳过空格和换行**
//     getline(入, 行) 按"一整行"读，读到换行为止（换行本身不给你）
//
// 什么时候用 getline：要读一整行文字（比如"张三 90 分"这种一行一条的记录）。
//
// 一直读到文件末尾的标准写法：
//
//     std::string 行;
//     while (std::getline(入, 行)) {      ← 读不到了就退出循环
//         …
//     }
//
// ─────────────────────────────────────────────────────
//  四、竞赛里的文件重定向
// ─────────────────────────────────────────────────────
//
// 比赛时输入输出常常走文件。最省事的写法是在 main 开头加两行：
//
//     int main() {
//         freopen("data.in", "r", stdin);      ← 让 cin 从这个文件读
//         freopen("data.out", "w", stdout);    ← 让 cout 写进这个文件
//         …
//     }
//
// 加完这两行，**程序里其它的代码一个字都不用改** —— `cin >> 甲` 自己就变成读文件了。
// （"重定向"这个词就是这个意思：把标准输入/输出**接到别处去**。）
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：忘了 #include <fstream>
// ─────────────────────────────────────────────────────
//
// 本机 g++ 16.1.0 的原话是：
//
//     error: variable 'std::ofstream \U000051fa' has initializer but incomplete type
//     note: 'std::ofstream' is defined in header '<fstream>';
//           this is probably fixable by adding '#include <fstream>'
//
// （\U000051fa 就是「出」字被转义了 —— g++ 报错里的中文标识符都长这样。）
//
// 翻译过来：**ofstream 只有个名字，编译器不知道它长什么样，因为头文件没进来。**
//   g++ 这次很贴心，note 里直接把补救办法写上了：加上 `#include <fstream>`。
//
// ⚠ 注意 `<iostream>` 里**没有** ofstream。cin/cout 在 `<iostream>`，
//   文件流在 `<fstream>` —— 两个都要 include。
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   CSV 解析、二进制文件 → 用到再查。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  ⚠ 这一课和前面不一样：**你要写一个完整的程序**（自己写 #include 和 main）。
//
//  要求：
//    1. 从文件 `data.in` 里读入一串整数（个数不定，空格或换行隔开，读到读不出来为止）
//    2. 数一数这一串里**偶数**有几个（注意：是把**所有**数都数进去）
//    3. 把这个个数写到文件 `data.out` 里（就一个数，后面换行）
//
//  提示：
//    · `#include <fstream>` 别忘了（还有 `<iostream>`）
//    · 读到末尾的标准写法：`while (入 >> 甲) { … }` —— 读不到了它自己会退出
//    · 输出文件的**名字必须是 data.out**
//
//  ⚠ 检查会换三份内容不同的 data.in 各跑一遍 —— 把答案写死是过不了的。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写你的完整程序 ──
#include <iostream>
#include <fstream>

int main()
{
    // 在这里写
    return 0;
}
""";

    public static string 参考答案 => """
// 参考答案 —— 文件读写（完整程序）
//
// 流程：开输入文件 → 一个一个数读进来 → 数偶数 → 开输出文件 → 写个数

#include <iostream>
#include <fstream>

int main() {
    std::ifstream 入("data.in");        /* 打开输入文件（读） */
    std::ofstream 出("data.out");       /* 打开输出文件（写；不存在就新建） */

    int 偶数个数 = 0;
    int 数;
    while (入 >> 数) {                  /* 读到读不出来为止：自动跳过空格和换行 */
        if (数 % 2 == 0) {
            偶数个数++;
        }
    }

    出 << 偶数个数 << '\n';             /* 写进文件，和用 cout 一模一样 */
    return 0;
}
""";

    // ══════════════════ 检查 ══════════════════

    [检查说明("★ 真的从 data.in 里读：换三份不同的输入，答案要跟着变")]
    public static CheckResult 检查_真的读文件(string 代码)
    {
        // ⚠ 核心检查点：用**行为**验证"真的读了文件"。
        //   三份输入文件的偶数个数各不相同 ⇒ 写死答案的一定对不上。
        //   输入就是空格/换行隔开的一串整数（**没有个数前缀**）——
        //   这样"while (入 >> 数) 读到读不出来为止"就是唯一自然的写法，不会有歧义。
        var 用例 = new (string 输入, string 期望)[]
        {
            ("1 2 3 4 5\n", "2"),
            ("10 11 12 13 14 15 16\n", "4"),
            ("1 3 5 7\n", "0"),
        };

        var 错 = new List<string>();
        for (int i = 0; i < 用例.Length; i++)
        {
            var (输入, 期望) = 用例[i];
            string 得到 = 跑一遍(代码, 输入, out string 出错);
            if (出错 != null) return CheckResult.不过(出错);
            if (得到 == null)
                错.Add("      第 " + (i + 1) + " 份输入：程序没有写出 data.out 文件");
            else if (得到 != 期望)
                错.Add("      第 " + (i + 1) + " 份输入（" + 输入.Replace("\n", " / ") + "）：data.out 里是 "
                       + 显示(得到) + "，应该是 " + 期望);
        }
        if (错.Count == 0)
            return CheckResult.过("三份不同的输入都算对了 —— 确实是从文件里读的");

        return CheckResult.不过(
            "换了输入之后结果不对：\n" + string.Join("\n", 错) + "\n" +
            "      检查会换三份内容不同的 data.in 各跑一遍，所以**把答案写死是过不了的**。\n" +
            "      正确流程：\n" +
            "          std::ifstream 入(\"data.in\");\n" +
            "          int 数, 偶数个数 = 0;\n" +
            "          while (入 >> 数) { if (数 % 2 == 0) 偶数个数++; }\n" +
            "          std::ofstream 出(\"data.out\");\n" +
            "          出 << 偶数个数 << '\\n';\n" +
            "      ⚠ 文件名必须是 data.in 和 data.out，一个字都不能差。");
    }

    [检查说明("源码里用了 ifstream / ofstream（不是 freopen 之外的歪门邪道）")]
    public static CheckResult 检查_用了文件流(string 代码)
    {
        string 净 = 去掉注释(代码);
        var 缺 = new List<string>();
        if (!净.Contains("#include <fstream>")) 缺.Add("      #include <fstream>");
        if (!净.Contains("ifstream")) 缺.Add("      std::ifstream（读文件）");
        if (!净.Contains("ofstream")) 缺.Add("      std::ofstream（写文件）");
        if (缺.Count == 0)
            return CheckResult.过("用到了 ifstream / ofstream，也 include 了 <fstream>");

        return CheckResult.不过(
            "源码里少了这些：\n" + string.Join("\n", 缺) + "\n" +
            "      这一课要练的就是这两个类：\n" +
            "          std::ifstream 入(\"data.in\");    读文件（用法和 std::cin 一样）\n" +
            "          std::ofstream 出(\"data.out\");   写文件（用法和 std::cout 一样）\n" +
            "      ⚠ `<iostream>` 里**没有**文件流 —— 必须再 `#include <fstream>`，\n" +
            "        否则报 `variable 'std::ofstream …' has initializer but incomplete type`。");
    }

    [检查说明("程序能正常跑完（没有崩、没有死循环）")]
    public static CheckResult 检查_能跑完(string 代码)
    {
        string 得到 = 跑一遍(代码, "3\n2 4 6\n", out string 出错);
        if (出错 != null) return CheckResult.不过(出错);
        if (得到 == null)
            return CheckResult.不过("程序跑完了，但没有生成 data.out 文件。\n" +
                "      检查一下有没有用 `std::ofstream 出(\"data.out\");` 打开输出文件，\n" +
                "      以及文件名有没有拼错（是 data.out，不是 out.txt）。");
        return CheckResult.过("程序正常跑完，也写出了 data.out");
    }

    // ══════════════════ 跑一遍 ══════════════════

    /// <summary>
    /// 在**自己的一个临时目录**里跑一次学生的完整程序：
    /// 先写好 data.in，跑完把 data.out 读出来。
    /// 返回 null 表示没有 data.out（文件没建、程序没跑起来都会被当成 null）；
    /// 出错不为 null 表示该直接把这一条判不过。
    ///
    /// ⚠ 为什么不能用 `CppCheck.直接运行`：
    ///   它把程序跑在运行器自己的临时目录里，**我拿不到那个目录**，
    ///   也就没法在程序启动前把 data.in 放好、跑完再把 data.out 读出来。
    ///   课程库没有"指定工作目录"的开关，所以这一课是唯一自己起进程的地方。
    ///   （学生写的是完整程序 ⇒ 无论如何都不能用 `编译并运行`，那会撞 main。）
    /// </summary>
    private static string 跑一遍(string 代码, string 输入内容, out string 出错)
    {
        出错 = null;
        string 目录 = Path.Combine(私有目录(), "第21课_" + System.Threading.Interlocked.Increment(ref 序号));
        try
        {
            Directory.CreateDirectory(目录);
            File.WriteAllText(Path.Combine(目录, "data.in"), 输入内容, new System.Text.UTF8Encoding(false));
            return 在目录里跑(代码, 目录, out 出错);
        }
        finally
        {
            尽力删(目录);
        }
    }

    private static int 序号;
    private static string 私有目录()
    {
        // ⚠ 路径必须**全是 ASCII** —— 实测 MinGW 的 ld.exe 无法把 exe 写进含中文的路径
        //   （报 `cannot open output file …: No such file or directory`，看着像权限问题）。
        string 根 = @"C:\lc_tmp_lesson21";
        Directory.CreateDirectory(根);
        return 根;
    }

    private static void 尽力删(string 目录)
    {
        try { Directory.Delete(目录, recursive: true); } catch { /* 删不掉就算了 */ }
    }

    /// <summary>编译 + 运行 + 读输出文件。出错时把给学生看的说明写进 出错。</summary>
    private static string 在目录里跑(string 代码, string 目录, out string 出错)
    {
        出错 = null;
        string src = Path.Combine(目录, "main.cpp");
        string exe = Path.Combine(目录, "prog.exe");
        File.WriteAllText(src, 代码, new System.Text.UTF8Encoding(false));

        string 编译器 = CppRunner.Compiler;
        if (编译器 == null) { 出错 = CppRunner.InstallHint; return null; }

        var 编译 = new System.Diagnostics.ProcessStartInfo(编译器)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        编译.ArgumentList.Add("-std=c++17");
        编译.ArgumentList.Add("-O1");
        编译.ArgumentList.Add("-Wall");
        编译.ArgumentList.Add("-o");
        编译.ArgumentList.Add(exe);
        编译.ArgumentList.Add(src);
        string 编译输出;
        using (var p = System.Diagnostics.Process.Start(编译))
        {
            编译输出 = p.StandardError.ReadToEnd() + p.StandardOutput.ReadToEnd();
            if (!p.WaitForExit(60000)) { try { p.Kill(true); } catch { } }
        }
        if (!File.Exists(exe))
        {
            if (编译输出.Contains("incomplete type") || 编译输出.Contains("<fstream>"))
            {
                出错 =
                    "编译没能通过 —— 多半是忘了 `#include <fstream>`。\n" +
                    "      本机 g++ 16.1.0 的原话是：\n" +
                    "          error: variable 'std::ofstream …' has initializer but incomplete type\n" +
                    "          note: 'std::ofstream' is defined in header '<fstream>';\n" +
                    "                this is probably fixable by adding '#include <fstream>'\n" +
                    "      ⇒ 在 `#include <iostream>` 下面再加一行 `#include <fstream>`。\n" +
                    "      （`<iostream>` 里只有 cin / cout，文件流在 `<fstream>` 里。）";
                return null;
            }
            出错 = 报错说明(编译输出);
            return null;
        }

        var 跑 = new System.Diagnostics.ProcessStartInfo(exe)
        {
            WorkingDirectory = 目录,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using (var p = System.Diagnostics.Process.Start(跑))
        {
            p.StandardOutput.ReadToEnd();
            string 标准错误 = p.StandardError.ReadToEnd();
            if (!p.WaitForExit(60000))
            {
                try { p.Kill(true); } catch { }
                出错 =
                    "程序运行超时（超过 60 秒）—— 检查有没有死循环。\n" +
                    "      这一课最常见的死循环是：`while (入 >> 数)` 写成了 `while (true)`，\n" +
                    "      或者循环里忘了真正读下一个数。";
                return null;
            }
            if (p.ExitCode != 0)
            {
                出错 = "程序崩了（退出码 " + p.ExitCode + "）。\n" + 标准错误;
                return null;
            }
        }

        string 出 = Path.Combine(目录, "data.out");
        if (!File.Exists(出)) return null;
        return File.ReadAllText(出).Trim();
    }

    /// <summary>把 g++ 的报错译成给学生看的话（走课程库那套翻译）。</summary>
    private static string 报错说明(string 编译输出)
    {
        var raw = new CppResult();
        raw.CompileOutput = 编译输出;
        raw.CompileFailed = true;
        raw.Error = CppRunner.FirstError(编译输出);
        var run = new CppRun { Raw = raw };
        return CppCheck.编译失败说明(run).Detail;
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

    private static string 显示(string s) => s == null ? "（没拿到）" : (s.Length == 0 ? "（空）" : s);
}
