// 第 07 课：字符串 —— 一串字符，和一个字符的区别
//
// ══ 这一课要教什么 ══
// std::string 是「一串字符」，char 是「一个字符」。它们看起来只差一对引号：
//
//     'A'   单引号 → char        （1 个字节，就是一个字符）
//     "A"   双引号 → 字符串字面量  （2 个字节：'A' 后面还藏着一个 \0）
//
// 这个区别在 C++ 里是硬性的：写成 'A' 的地方不能换成 "A"，反过来也一样。
// 学生混用是极常见的错误，所以这一课有一条检查**专门验证返回类型**。
//
// 另一件必须早知道的事：**std::string 存的是字节，不是「字」**。
// 实测（本机 g++ 16.1.0，源文件 UTF-8）：
//     std::string s = "中文";     s.size()  →  6      （不是 2）
//     std::string t = "中";       t.size()  →  3
// UTF-8 里一个汉字占 3 个字节，所以「中文」是 6 个字节。
// 于是 s.size() 在中文面前**不是字数**，s[0] 也不是第一个字。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点：**'A' 和 "A" 的区别**（引号 + 隐藏的 \0 + 类型）。
// 偷懒/踩坑的解法有两种：
//   ① std::string 首字符(...) { return s.substr(0, 1); }  —— 返回值"看着对"，
//      打印出来一模一样，但类型是字符串不是 char。
//      防法：不看打印结果，看**类型** —— sizeof(首字符("h")) 必须是 1。
//      （std::string 在本机是 32，int 是 4，只有 char 是 1。）
//   ② int 双引号大小() { return 2; } —— 直接把结论写死。
//      防法：另一条检查看源码里有没有 sizeof（值对了但没量过，一样判不过）。
// 中文那条用实测值：'中文'.size() == 6、'中'.size() == 3，
// 这两个数是真跑出来的，不是估的。
//
// ⚠ 给维护者：所有输出都用「标记|值」的格式，靠标记对答案（学生可能自己加打印）；
//   所有报错识别只匹配 ASCII 片段 —— g++ 会把中文标识符转义成 \U000053cc。

using System.Collections.Generic;
using HybridEngine-light;
using HybridEngineLight.CppLessons;

[课程("07", "第 07 课：字符串 —— 'A' 和 \"A\" 不是一回事",
    "1. 写 int 字节数(std::string s)，返回 s 的字节数（s.size()）\n" +
    "2. 写 char 首字符(std::string s)，返回第一个字符；空串返回 '\\0'（返回类型必须是 char）\n" +
    "3. 写 int 单引号大小() 和 int 双引号大小()，用 sizeof 量出 'A' 和 \"A\" 各占几个字节\n" +
    "4. 写 std::string 拼接(std::string a, std::string b)，用 + 把它们连起来\n" +
    "5. 写 std::string 前三个(std::string s)，返回前 3 个字符（不足 3 个就全部返回）\n" +
    "6. 写 int 找位置(std::string s, std::string 子)，返回 子 第一次出现的下标；找不到返回 -1")]
public static class 第07课
{
    public static string 模板 => """
// 第 07 课：字符串 —— 'A' 和 "A" 不是一回事
//
// 要做（只写函数，**不用写 main、不用写 #include** —— 课程会替你补上）：
//   1. int 字节数(std::string s)          返回 s.size()（也就是它占几个字节）
//   2. char 首字符(std::string s)         返回第一个字符；s 为空时返回 '\0'
//                                         ⚠ 返回类型必须是 char，不是 std::string
//   3. int 单引号大小()                   用 sizeof 量出 'A' 占几个字节
//      int 双引号大小()                   用 sizeof 量出 "A" 占几个字节
//   4. std::string 拼接(std::string a, std::string b)      用 + 连起来
//   5. std::string 前三个(std::string s)  返回前 3 个字符；不足 3 个就返回全部
//   6. int 找位置(std::string s, std::string 子)
//                                         返回 子 第一次出现的下标；找不到返回 -1
//
// ── ★ 本课最重要的一对区别 ──
//
//      'A'     单引号 → **char**，一个字符，sizeof 是 1
//      "A"     双引号 → **字符串字面量**，sizeof 是 2
//
//   多出来的那 1 个字节，是末尾隐藏的结束符 '\0'（C++ 用它标记「字符串到此结束」）。
//   所以这两个不能互相替换：
//          char c = 'A';         ✓
//          char c = "A";         ✗ 类型不对（"A" 是字符串，不是一个字符）
//          std::string s = "A";  ✓
//          std::string s = 'A';  ✗ 类型不对
//
// ── ★ 字符串的常用操作 ──
//
//      std::string s = "hello";
//      s.size()            5          // 有几个字节（s.length() 一样，两个名字都对）
//      s[0]                'h'        // 下标从 0 开始，取到的是 **char**
//      s + " world"        "hello world"   // 用 + 拼接
//      s.substr(0, 3)      "hel"      // 从下标 0 开始取 3 个
//      s.find("llo")       2          // 子串第一次出现的下标
//      s.find("z")         std::string::npos   // 找不到时返回一个"不可能的下标"
//
//   ⚠ find 找不到时返回的 **不是 -1**，而是 std::string::npos
//     （一个非常大的无符号数）。所以「找不到返回 -1」要自己写：
//          std::size_t 位置 = s.find(子);
//          if (位置 == std::string::npos) return -1;
//          return (int)位置;
//
// ── ⚠ 中文：size() 是**字节数**，不是字数 ──
//   实测（本机 g++ 16.1.0，源文件 UTF-8）：
//          std::string s = "中文";    s.size()  →  6      ← 不是 2！
//          std::string t = "中";      t.size()  →  3
//   UTF-8 里一个汉字要 3 个字节。所以：
//          s.size()  不是「几个字」
//          s[0]      也不是「第一个字」，而是一个字节（单独打印是乱码）
//   这一点在 Python 里完全不同（Python 的 len("中文") 是 2）。
//
// 写完后运行：dotnet run --project csharp/cpp-lessons 检查 07

// ── 在下面写你的代码 ──

""";

    public static string 参考答案 => """
// 参考答案 —— 字符串

// 1. 字节数：size() 给的是**字节数**（对中文来说不是字数）
int 字节数(std::string s) {
    return (int)s.size();
}

// 2. 首字符：返回类型是 **char**（一个字符），不是 std::string（一串字符）
char 首字符(std::string s) {
    if (s.empty()) {
        return '\0';        // 单引号：这是"一个空字符"，不是空字符串
    }
    return s[0];            // 下标 0 取到的是 char
}

// 3. 量一量：'A' 是 1 个字节，"A" 是 2 个字节（多的那个是末尾的 '\0'）
int 单引号大小() {
    return (int)sizeof('A');      // 1
}

int 双引号大小() {
    return (int)sizeof("A");      // 2  ← 'A' 再加一个隐藏的 '\0'
}

// 4. 拼接：std::string 直接用 + 就能连起来
std::string 拼接(std::string a, std::string b) {
    return a + b;
}

// 5. 前三个：substr(起点下标, 取几个)
//    取的数量超出长度也没关系，substr 会自动只取到末尾
std::string 前三个(std::string s) {
    return s.substr(0, 3);
}

// 6. 找位置：找不到时 find 返回的是 npos，不是 -1，要自己转
int 找位置(std::string s, std::string 子) {
    std::size_t 位置 = s.find(子);
    if (位置 == std::string::npos) {
        return -1;              // 约定：找不到就用 -1 表示
    }
    return (int)位置;
}
""";

    // ══════════════════ 检查 ══════════════════

    [检查说明("字节数：英文串和空串")]
    public static CheckResult 检查_字节数(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停 != null) return CheckResult.不过(停);

        return 比一比(run,
            ("字节abc", "3", "字节数(\"abc\")　3 个字符 = 3 个字节"),
            ("字节空", "0", "字节数(\"\")　空串一个字节都没有"));
    }

    [检查说明("★ 字节数：中文的 size() 是字节数，不是字数")]
    public static CheckResult 检查_字节数_中文(string 代码)
    {
        // ⚠ 这一课的核心陷阱之一：中文在 std::string 里按**字节**算。
        // 下面这两个期望值是真跑出来的（g++ 16.1.0，UTF-8 源文件）：
        //     "中文".size() == 6      "中".size() == 3
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停 != null) return CheckResult.不过(停);

        string 两个 = 取值(run, "字节中文");
        string 一个 = 取值(run, "字节中");
        if (两个 == null || 一个 == null)
            return CheckResult.不过("没能拿到中文那两组的结果 —— 函数可能没被调用到，或者程序中途崩了。");

        if (两个 == "6" && 一个 == "3")
            return CheckResult.过("字节数(\"中文\") = 6、字节数(\"中\") = 3 —— 字节数，不是字数");

        return CheckResult.不过(
            "字节数(\"中文\") 得到 " + 显示(两个) + "（应该是 6），" +
            "字节数(\"中\") 得到 " + 显示(一个) + "（应该是 3）。\n" +
            (两个 == "2"
                ? "      你数的是「字」—— 2 个字确实是对的，但 std::string 给出的**不是这个数**。\n"
                : "") +
            "      **这不是 bug，这就是 C++ 的真相**：std::string 存的是**字节**。\n" +
            "      UTF-8 里一个汉字占 3 个字节，所以「中文」= 3 + 3 = 6 个字节。\n" +
            "      （本机实测，不是估计：「中文\".size() == 6、「中」.size() == 3）\n" +
            "      后果是：\n" +
            "          s.size()   不是「几个字」\n" +
            "          s[0]       不是「第一个字」，而是这个汉字的第一个字节\n" +
            "      对比一下别的语言：Python 的 len(\"中文\") 是 2，C# 的 \"中文\".Length 也是 2。\n" +
            "      **C++ 的 std::string.size() 是 6** —— 它只说字节，不说字符。\n" +
            "      这一条的正确写法其实就是老老实实用 size()：\n" +
            "          int 字节数(std::string s) { return (int)s.size(); }");
    }

    [检查说明("首字符：普通串、单字符串都要对")]
    public static CheckResult 检查_首字符(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停 != null) return CheckResult.不过(停);

        var 错 = new List<string>();
        string 嗨 = 取值(run, "首h");
        string 啊 = 取值(run, "首A");
        string 空 = 取值(run, "首空");
        if (嗨 != "h") 错.Add("      首字符(\"hello\") 得到 " + 显示(嗨) + "，应该是 h");
        if (啊 != "A") 错.Add("      首字符(\"A\") 得到 " + 显示(啊) + "，应该是 A");
        if (空 == null || !(空 == "" || 空 == "\0"))
            错.Add("      首字符(\"\") 得到 " + 显示(空) + "，应该是空字符 '\\0'");

        if (错.Count == 0) return CheckResult.过("首字符(\"hello\")=h、首字符(\"A\")=A、首字符(\"\")=空字符");

        return CheckResult.不过(
            "首字符的结果不对：\n" + string.Join("\n", 错) + "\n" +
            "      取值用下标就行：\n" +
            "          char 首字符(std::string s) {\n" +
            "              if (s.empty()) return '\\0';   // 空串要单独处理\n" +
            "              return s[0];\n" +
            "          }\n" +
            "      两个容易错的地方：\n" +
            "        · 空串返回的必须写成 '\\0'（单引号，一个空字符）——\n" +
            "          写成 \"\" 是**空字符串**，类型对不上；写成 ' ' 是空格，也不对。\n" +
            "        · s[0] 给的是**第一个字节**。对英文来说正好是第一个字母，\n" +
            "          对中文来说是半个汉字（这也是上一条检查讲的事）。");
    }

    [检查说明("★ 首字符的返回类型必须是 char（不是 std::string）")]
    public static CheckResult 检查_首字符是char不是string(string 代码)
    {
        // ⚠ 这一条是本课的核心检查点：不看打印结果，看**类型**。
        //
        // 为什么不能用打印结果判断：不管返回 char 还是 std::string（哪怕内容是
        // "h" 这个字符串），std::cout 打出来的都是同一个 h —— 打印看不出区别。
        // sizeof 看得出来：char = 1 字节，std::string = 32 字节（本机实测），int = 4。
        const string 调用点 = """
std::cout << "大小|" << sizeof(首字符("h")) << "\n";
""";
        var run = CppCheck.编译并运行(代码, null, 调用点);
        if (!run.编译通过)
        {
            string 提示 = 没写函数说明(run, "首字符");
            return 提示 != null ? CheckResult.不过(提示) : CppCheck.编译失败说明(run);
        }

        string 大小 = 取值(run, "大小");
        if (大小 == "1")
            return CheckResult.过("sizeof(首字符(\"h\")) = 1 —— 返回的确实是一个 char");

        string 实际情况 =
            大小 == "32" ? "你交出的是一个 std::string（它占 32 个字节）"
            : 大小 == "4" ? "你交出的是一个 int（它占 4 个字节）"
            : "你交出的东西占 " + 显示(大小) + " 个字节";

        return CheckResult.不过(
            "「首字符」的返回类型不对：" + 实际情况 + "，而一个 char 只占 1 个字节。\n" +
            "      多半是这么写的：\n" +
            "          std::string 首字符(std::string s) { return s.substr(0, 1); }   ✗\n" +
            "      它打印出来也是 h，看着一模一样 —— 但类型是「一串字符」，不是「一个字符」。\n" +
            "      这就是本课要分清的一对：\n" +
            "          'h'    单引号 → char        1 个字节，就是一个字符\n" +
            "          \"h\"    双引号 → 字符串      2 个字节：'h' 后面还有隐藏的 '\\0'\n" +
            "      改法：返回类型写 char，值用下标取：\n" +
            "          char 首字符(std::string s) { if (s.empty()) return '\\0'; return s[0]; }\n" +
            "      顺便记住：单引号里**只能有一个字符**；'ab' 这种写法在 C++ 里是错的。");
    }

    [检查说明("★ 单引号和双引号的大小：'A' 是 1，\"A\" 是 2")]
    public static CheckResult 检查_引号的大小(string 代码)
    {
        // ⚠ 本课的第二个核心检查点：那个多出来的字节，就是隐藏的 '\0'。
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停 != null) return CheckResult.不过(停);

        string 单 = 取值(run, "单引号");
        string 双 = 取值(run, "双引号");

        if (单 == "1" && 双 == "2")
            return CheckResult.过("sizeof('A') = 1、sizeof(\"A\") = 2 —— 多出来的 1 个字节就是末尾的 '\\0'");

        return CheckResult.不过(
            "量出来的大小是：单引号 'A' = " + 显示(单) +
            "，双引号 \"A\" = " + 显示(双) + "。\n" +
            "      正确答案是 **1 和 2**：\n" +
            "          sizeof('A')     → 1    单引号是一个 char，就 1 个字节\n" +
            "          sizeof(\"A\")     → 2    双引号是字符串，'A' 后面还藏着一个 '\\0'\n" +
            "      C++ 用 '\\0'（叫「空字符」）标记「字符串到此结束」。\n" +
            "      你平时看不见它，但它是真实存在的 1 个字节 —— 这正是\n" +
            "      std::string(\"A\").size() 是 1 而 sizeof(\"A\") 是 2 的原因\n" +
            "      （字符串对象只存内容，字面量数组还带着结束符）。\n" +
            "      怎么写：\n" +
            "          int 单引号大小() { return (int)sizeof('A'); }\n" +
            "          int 双引号大小() { return (int)sizeof(\"A\"); }");
    }

    [检查说明("那两个大小要用 sizeof 真的量，不是直接把 1 和 2 写死")]
    public static CheckResult 检查_用了sizeof(string 代码)
    {
        // 这条只看源码文本 —— 因为 return 1; 和 return 2; 谁都会写，
        // 而本课要学的是"怎么让编译器告诉你一个东西占几个字节"。
        // （值的正确性由上面那条检查负责，这里只负责提醒。）
        if (System.Text.RegularExpressions.Regex.IsMatch(代码, @"sizeof\s*\("))
            return CheckResult.过("源码里用到了 sizeof —— 是量出来的，不是写死的");

        return CheckResult.不过(
            "没有在代码里找到 sizeof。\n" +
            "      这一条想让你学的不是「1 和 2 这两个数」，而是**怎么问编译器**：\n" +
            "          sizeof('A')    编译器告诉你它占几个字节 → 1\n" +
            "          sizeof(\"A\")     → 2\n" +
            "          sizeof(int)     → 4\n" +
            "          sizeof(甲)      → 数组 甲 一共占几个字节\n" +
            "      sizeof 是**编译期**就得出结果的（不是运行时算的），所以它完全不花时间。\n" +
            "      直接写 return 1; 虽然答案碰巧对，但你没学会怎么量 ——\n" +
            "      换个类型（比如 double、long long）就再也答不上来了。");
    }

    [检查说明("拼接：+ 连起来就行（空串也要对）")]
    public static CheckResult 检查_拼接(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停 != null) return CheckResult.不过(停);

        return 比一比(run,
            ("拼接", "你好世界", "拼接(\"你好\", \"世界\")"),
            ("拼接空", "abc", "拼接(\"\", \"abc\")　空串 + 字符串 = 原来那个"));
    }

    [检查说明("前三个：够 3 个取 3 个，不够就有多少给多少")]
    public static CheckResult 检查_前三个(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停 != null) return CheckResult.不过(停);

        return 比一比(run,
            ("前三个", "hel", "前三个(\"hello\")"),
            ("前三个短", "hi", "前三个(\"hi\")　只有 2 个字符，就返回 2 个"),
            ("前三个空", "", "前三个(\"\")　空串要返回空串，不能崩"));
    }

    [检查说明("找位置：开头、中间、找不到三种情况")]
    public static CheckResult 检查_找位置(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停 != null) return CheckResult.不过(停);

        return 比一比(run,
            ("找位置", "6", "找位置(\"hello world\", \"world\")"),
            ("找开头", "0", "找位置(\"hello\", \"he\")　出现在**开头**时下标是 0（不是 -1）"),
            ("找不到", "-1", "找位置(\"hello\", \"z\")　找不到要自己返回 -1"));
    }

    // ══════════════════ 跑一次、比一比 ══════════════════

    private const string 调用点 = """
std::cout << "字节abc|" << 字节数("abc") << "\n";
std::cout << "字节空|" << 字节数("") << "\n";
std::cout << "字节中文|" << 字节数("中文") << "\n";
std::cout << "字节中|" << 字节数("中") << "\n";
std::cout << "首h|" << 首字符("hello") << "\n";
std::cout << "首A|" << 首字符("A") << "\n";
std::cout << "首空|" << 首字符("") << "\n";
std::cout << "单引号|" << 单引号大小() << "\n";
std::cout << "双引号|" << 双引号大小() << "\n";
std::cout << "拼接|" << 拼接("你好", "世界") << "\n";
std::cout << "拼接空|" << 拼接("", "abc") << "\n";
std::cout << "前三个|" << 前三个("hello") << "\n";
std::cout << "前三个短|" << 前三个("hi") << "\n";
std::cout << "前三个空|" << 前三个("") << "\n";
std::cout << "找位置|" << 找位置("hello world", "world") << "\n";
std::cout << "找开头|" << 找位置("hello", "he") << "\n";
std::cout << "找不到|" << 找位置("hello", "z") << "\n";
""";

    private static string 上次的代码;
    private static CppRun 上次的运行;

    private static CppRun 跑一次(string 代码)
    {
        if (上次的运行 != null && 上次的代码 == 代码) return 上次的运行;
        上次的代码 = 代码;
        上次的运行 = CppCheck.编译并运行(代码, null, 调用点);
        return 上次的运行;
    }

    private static string 先别急着比(CppRun run)
    {
        if (run.Raw.CompileFailed)
            return CppCheck.编译失败说明(run).Detail;
        if (run.Raw.Error != null)
            return
                "程序没跑完就退出了：" + run.Raw.Error + "\n" +
                "      这一课最容易让程序崩的是下标：s[0] 在**空串**上取是不安全的，\n" +
                "      先判断 s.empty() 再取，或者干脆用 s.at(0)（越界时它会抛异常告诉你）。";
        return null;
    }

    private static CheckResult 比一比(CppRun run, params (string 标记, string 期望, string 说明)[] 用例们)
    {
        var 错 = new List<string>();
        foreach (var (标记, 期望, 说明) in 用例们)
        {
            string 得到 = 取值(run, 标记);
            if (得到 == null) 错.Add("      " + 说明 + "：程序没有给出这一行的结果");
            else if (得到 != 期望) 错.Add("      " + 说明 + "：得到 " + 显示(得到) + "，应该是 " + 显示(期望));
        }
        if (错.Count == 0) return CheckResult.过("这几组都对");

        return CheckResult.不过(
            "结果不对：\n" + string.Join("\n", 错) + "\n" +
            "      字符串的常用操作：\n" +
            "          s.size()         几个字节\n" +
            "          s[0]             下标取字符（char）\n" +
            "          a + b            拼接\n" +
            "          s.substr(从哪, 取几个)   截一段\n" +
            "          s.find(子)       找位置（找不到是 npos，不是 -1）");
    }

    private static string 没写函数说明(CppRun run, string 函数名)
    {
        if (!run.编译信息.Contains("was not declared in this scope")) return null;
        return
            "没有找到「" + 函数名 + "」这个函数，编译器不认识这个名字。\n" +
            "      这一课要求的签名是：\n" +
            "          char 首字符(std::string s)\n" +
            "      检查一下名字有没有写错、参数是不是 std::string。\n" +
            "      （只写函数就行，不用写 main 和 #include —— 课程会替你补上。）";
    }

    private static string 取值(CppRun run, string 标记)
    {
        foreach (var 行 in run.行)
        {
            int i = 行.IndexOf('|');
            if (i > 0 && 行.Substring(0, i).Trim() == 标记) return 行.Substring(i + 1).Trim();
        }
        return null;
    }

    private static string 显示(string s) => s == null ? "（没拿到）" : (s.Length == 0 ? "（空）" : s);
}
