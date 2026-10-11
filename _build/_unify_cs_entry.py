import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
S = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode\csharp\cs-lessons\Lessons"

# 每课：把「自定义骨架」那段换成一条调用点。
# 做法：定位 `[检查说明("...")]` + 方法体，整块替换成新实现。
替换 = {}

# ── 第 03 课 ──────────────────────────────────────────────
替换["第03课_条件.cs"] = [
 ("判等级 的边界值（95 / 90 / 60 / 59）都对", '''    [检查说明("判等级 的边界值（95 / 90 / 60 / 59）都对")]
    public static CheckResult 检查_判等级(string 代码)
    {
        // ⚠ 统一入口：一条调用点跑完所有用例（原来这里写了一大段自定义骨架，
        //   会和库追加的 `入口` 类撞车 —— 详见 第01课 里的说明）。
        var run = 调用(代码,
            "foreach (var __n in new[] { 95, 90, 60, 59 }) Console.WriteLine(学生代码.判等级(__n));");
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "优秀", "优秀", "及格", "不及格" };
        var 行 = run.行;
        if (行.Count < 4) return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例错了：应当是“{期望[i]}”，实际是“{行[i].Trim()}”。\\n" +
                    "      四个用例是 95 / 90 / 60 / 59 —— 重点看 **90 和 60 这两个边界**。\\n" +
                    "      （`> 90` 和 `>= 90` 的差别就在这里。）");
        return CheckResult.过("95→优秀 · 90→优秀 · 60→及格 · 59→不及格");
    }
'''),
 ("星期几 用 switch（而且没有忘 break 的问题）", '''    [检查说明("星期几 用 switch（而且没有忘 break 的问题）")]
    public static CheckResult 检查_星期几(string 代码)
    {
        var run = 调用(代码,
            "for (int __i = 1; __i <= 8; __i++) Console.WriteLine(学生代码.星期几(__i));");
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "周一", "周二", "周三", "周四", "周五", "周六", "周日", "不是星期" };
        var 行 = run.行;
        if (行.Count < 8) return CheckResult.不过($"应该打印 8 行，实际 {行.Count} 行。");
        for (int i = 0; i < 8; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"星期几({i + 1}) 应当是“{期望[i]}”，实际是“{行[i].Trim()}”。\\n" +
                    "      ⚠ 注意输入 8 应当返回“不是星期” —— 别忘了 `_ =>` 那一支。");
        return CheckResult.过("1..7 都对，8 走默认分支");
    }
'''),
]

# ── 第 04 课 ──────────────────────────────────────────────
替换["第04课_循环与集合.cs"] = [
 ("求和 对不同列表都对（含空列表与负数）", '''    [检查说明("求和 对不同列表都对（含空列表与负数）")]
    public static CheckResult 检查_求和(string 代码)
    {
        var run = 调用(代码, """
            Console.WriteLine(学生代码.求和(new List<int> { 1, 2, 3, 4, 5 }));
            Console.WriteLine(学生代码.求和(new List<int> { }));
            Console.WriteLine(学生代码.求和(new List<int> { -5, 5 }));
            Console.WriteLine(学生代码.求和(new List<int> { 100 }));
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "15", "0", "0", "100" };
        string[] 名 = { "[1..5]", "空列表", "[-5,5]", "[100]" };
        var 行 = run.行;
        if (行.Count < 4) return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"求和({名[i]}) 应当是 {期望[i]}，实际是“{行[i].Trim()}”。\\n" +
                    "      ⚠ 空列表要返回 0 —— 和变量必须**先初始化为 0**。");
        return CheckResult.过("4 组输入全对（含空列表与负数）");
    }
'''),
 ("只要偶数 结果正确", '''    [检查说明("只要偶数 结果正确")]
    public static CheckResult 检查_只要偶数(string 代码)
    {
        var run = 调用(代码, """
            Console.WriteLine(string.Join(",", 学生代码.只要偶数(new List<int> { 1, 2, 3, 4, 5, 6 })));
            Console.WriteLine("[" + string.Join(",", 学生代码.只要偶数(new List<int> { 1, 3, 5 })) + "]");
            Console.WriteLine(string.Join(",", 学生代码.只要偶数(new List<int> { -2, 0, 2 })));
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "2,4,6", "[]", "-2,0,2" };
        var 行 = run.行;
        if (行.Count < 3) return CheckResult.不过($"应该打印 3 行，实际 {行.Count} 行。");
        for (int i = 0; i < 3; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 组错了：应当是“{期望[i]}”，实际是“{行[i].Trim()}”。\\n" +
                    "      ⚠ 三件事：偶数判据是 `x % 2 == 0`（不是 `x % 2 == 1`）；\\n" +
                    "        没有偶数时要返回**空列表**（不是 null）；\\n" +
                    "        **0 是偶数** —— `0 % 2 == 0`。");
        return CheckResult.过("含“没有偶数”与 0 的情况都对");
    }
'''),
]

# ── 第 05 课 ──────────────────────────────────────────────
替换["第05课_方法与参数.cs"] = [
 ("平方 对不同输入都对（含 0 与负数）", '''    [检查说明("平方 对不同输入都对（含 0 与负数）")]
    public static CheckResult 检查_平方(string 代码)
    {
        var run = 调用(代码,
            "foreach (var __n in new[] { 1, 0, -3, 10 }) Console.WriteLine(学生代码.平方(__n));");
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "1", "0", "9", "100" };
        string[] 名 = { "1", "0", "-3", "10" };
        var 行 = run.行;
        if (行.Count < 4) return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"平方({名[i]}) 应当是 {期望[i]}，实际是“{行[i].Trim()}”。\\n" +
                    "      ⚠ 负负得正：(-3) × (-3) = 9。");
        return CheckResult.过("4 组全对（含 0 与负数）");
    }
'''),
 ("加倍(ref) 真的改到了外面的变量", '''    [检查说明("加倍(ref) 真的改到了外面的变量")]
    public static CheckResult 检查_加倍(string 代码)
    {
        // ⚠ ref 参数在调用点里也要写 `ref` —— 这正是这一课要考的。
        var run = 调用(代码, """
            int __a = 5;  学生代码.加倍(ref __a);  Console.WriteLine(__a);
            int __b = 0;  学生代码.加倍(ref __b);  Console.WriteLine(__b);
            int __c = -3; 学生代码.加倍(ref __c);  Console.WriteLine(__c);
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "10", "0", "-6" };
        var 行 = run.行;
        if (行.Count < 3) return CheckResult.不过($"应该打印 3 行，实际 {行.Count} 行。");
        for (int i = 0; i < 3; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是 {期望[i]}，实际是“{行[i].Trim()}”。\\n" +
                    "      ⚠ 要让方法改到外面，参数必须写 `ref`：\\n" +
                    "         `static void 加倍(ref int x)` 且方法体里 `x = x * 2;`");
        return CheckResult.过("5→10 · 0→0 · -3→-6（真的改到了外面）");
    }
'''),
 ("试着转成整数(out) 成功/失败两条路都对", '''    [检查说明("试着转成整数(out) 成功/失败两条路都对")]
    public static CheckResult 检查_试着转(string 代码)
    {
        var run = 调用(代码, """
            int __v;
            Console.WriteLine(学生代码.试着转成整数("42", out __v) + ":" + __v);
            Console.WriteLine(学生代码.试着转成整数("-7", out __v) + ":" + __v);
            Console.WriteLine(学生代码.试着转成整数("abc", out __v));
            Console.WriteLine(学生代码.试着转成整数("", out __v));
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "True:42", "True:-7", "False", "False" };
        var 行 = run.行;
        if (行.Count < 4) return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是“{期望[i]}”，实际是“{行[i].Trim()}”。\\n" +
                    "      ⚠ 转不了的时候也要给 `值` 赋一个值（`out` 要求方法里**必须**赋值）——\\n" +
                    "         最省事的做法就是直接用 `int.TryParse(s, out 值);` 的返回值。");
        return CheckResult.过("转得了 / 转不了 两条路都对");
    }
'''),
]

# ── 第 06 课 ──────────────────────────────────────────────
替换["第06课_字符串.cs"] = [
 ("反转 对不同字符串都对（含空串与中文）", '''    [检查说明("反转 对不同字符串都对（含空串与中文）")]
    public static CheckResult 检查_反转(string 代码)
    {
        var run = 调用(代码, """
            Console.WriteLine("[" + 学生代码.反转("") + "]");
            Console.WriteLine(学生代码.反转("a"));
            Console.WriteLine(学生代码.反转("abc"));
            Console.WriteLine(学生代码.反转("你好世界"));
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "[]", "a", "cba", "界世好你" };
        var 行 = run.行;
        if (行.Count < 4) return CheckResult.不过($"应该打印 4 行，实际 {行.Count} 行。");
        for (int i = 0; i < 4; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是“{期望[i]}”，实际是“{行[i].Trim()}”。\\n" +
                    "      ⚠ 空字符串要返回空字符串（别返回 null）。\\n" +
                    "      最省事的写法：`new string(s.Reverse().ToArray())`");
        return CheckResult.过("空串 / 单字符 / 英文 / 中文 都对");
    }
'''),
 ("数元音 不分大小写", '''    [检查说明("数元音 不分大小写")]
    public static CheckResult 检查_数元音(string 代码)
    {
        var run = 调用(代码, """
            foreach (var __s in new[] { "", "xyz", "Hello", "AEIOU", "Programming" })
                Console.WriteLine(学生代码.数元音(__s));
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "0", "0", "2", "5", "3" };
        var 行 = run.行;
        if (行.Count < 5) return CheckResult.不过($"应该打印 5 行，实际 {行.Count} 行。");
        for (int i = 0; i < 5; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是 {期望[i]}，实际是“{行[i].Trim()}”。\\n" +
                    "      ⚠ 重点：**不分大小写** —— 先 `s.ToLower()` 再数。\\n" +
                    "        （忘了这步的话“AEIOU”会数成 0。）");
        return CheckResult.过("空串 / 无元音 / 小写 / 大写 / 混合 都对");
    }
'''),
]

# ── 第 07 课 ──────────────────────────────────────────────
替换["第07课_类与对象.cs"] = [
 ("计数器：构造 / 加 / 重置 都对", '''    [检查说明("计数器：构造 / 加 / 重置 都对")]
    public static CheckResult 检查_计数器(string 代码)
    {
        var run = 调用(代码, """
            var __a = new 计数器();
            Console.WriteLine(__a.值);
            __a.加(3); __a.加(4);
            Console.WriteLine(__a.值);
            __a.重置();
            Console.WriteLine(__a.值);

            var __b = new 计数器(100);
            Console.WriteLine(__b.值);
            __b.加(1);
            Console.WriteLine(__b.值);
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "0", "7", "0", "100", "101" };
        var 行 = run.行;
        if (行.Count < 5) return CheckResult.不过($"应该打印 5 行，实际 {行.Count} 行。");
        for (int i = 0; i < 5; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是 {期望[i]}，实际是“{行[i].Trim()}”。\\n" +
                    "      ⚠ 注意第 4 个：`new 计数器(100)` 要能从 100 开始\\n" +
                    "        ⇒ 构造函数要有 `int 初值 = 0` 这个参数。");
        return CheckResult.过("默认 0 起 / 累加 / 重置 / 指定初值 都对");
    }
'''),
 ("银行账户：钱不够时**拒绝**且余额不变", '''    [检查说明("银行账户：钱不够时**拒绝**且余额不变")]
    public static CheckResult 检查_账户(string 代码)
    {
        var run = 调用(代码, """
            var __a = new 银行账户(100);
            Console.WriteLine(__a.余额);
            __a.存(50);
            Console.WriteLine(__a.余额);
            Console.WriteLine(__a.取(30) + ":" + __a.余额);
            Console.WriteLine(__a.取(999) + ":" + __a.余额);
            Console.WriteLine(__a.取(120) + ":" + __a.余额);
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "100", "150", "True:120", "False:120", "True:0" };
        var 行 = run.行;
        if (行.Count < 5) return CheckResult.不过($"应该打印 5 行，实际 {行.Count} 行。");
        for (int i = 0; i < 5; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是“{期望[i]}”，实际是“{行[i].Trim()}”。\\n" +
                    "      ⚠ 最要紧的是第 4 个：**取不出来的时候余额必须一点都不变**。\\n" +
                    "        常见错法：先 `余额 -= n;` 再检查 —— 那时已经扣了。\\n" +
                    "        正确顺序：**先检查，通过才扣**。");
        return CheckResult.过("存取 / 拒绝 / 正好取光 都对");
    }
'''),
]

# ── 第 08 课 ──────────────────────────────────────────────
替换["第08课_继承与接口.cs"] = [
 ("三种图形都能算面积（正方形复用了矩形）", '''    [检查说明("三种图形都能算面积（正方形复用了矩形）")]
    public static CheckResult 检查_面积(string 代码)
    {
        var run = 调用(代码, """
            var __们 = new List<图形> { new 圆(1), new 矩形(3, 4), new 正方形(5) };
            foreach (var __g in __们)
                Console.WriteLine($"{__g.名字()}|{__g.面积():F2}");
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "圆|3.14", "矩形|12.00", "正方形|25.00" };
        var 行 = run.行;
        if (行.Count < 3) return CheckResult.不过($"应该打印 3 行，实际 {行.Count} 行。");
        for (int i = 0; i < 3; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个应当是“{期望[i]}”，实际是“{行[i].Trim()}”。\\n" +
                    "      ⚠ 中间那个别搞混：矩形(3,4) 面积是 12.00。\\n" +
                    "        正方形(5) 应当复用矩形 —— 5×5 = 25.00。");
        return CheckResult.过("圆 3.14 · 矩形 12.00 · 正方形 25.00");
    }
'''),
 ("报告() 对同一批图形都能描述（它不认识具体类型）", '''    [检查说明("报告() 对同一批图形都能描述（它不认识具体类型）")]
    public static CheckResult 检查_报告(string 代码)
    {
        var run = 调用(代码, """
            var __们 = new List<图形> { new 正方形(2), new 圆(2) };
            Console.WriteLine(学生代码.报告(__们));
            """);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        var 文 = string.Join("\\n", run.行);
        if (!文.Contains("正方形") || !文.Contains("圆"))
            return CheckResult.不过(
                $"报告里应当同时出现“正方形”和“圆”，实际是：\\n{文}\\n" +
                "      ⚠ 报告() 里要用 `g.名字()` 和 `g.面积()`，\\n" +
                "        而不是去判断“它是不是圆”。");
        if (!文.Contains("4.00") && !文.Contains("12.57"))
            return CheckResult.不过(
                $"报告里没看到面积数字，实际是：\\n{文}\\n" +
                "      写法：$\"{g.名字()} 面积={g.面积():F2}\"");
        return CheckResult.过("两种图形都被描述了");
    }
'''),
]

for f, 们 in 替换.items():
    P = os.path.join(S, f)
    L = io.open(P, encoding="utf-8").read().split("\n")
    改 = 0
    for 说明, 新块 in 们:
        # 找 `[检查说明("<说明>")]` 那一行
        起 = None
        for i, l in enumerate(L):
            if "[检查说明(\"" in l and 说明[:12] in l:
                起 = i
                break
        if 起 is None:
            print("  ! %s: 找不到「%s」" % (f, 说明[:20]))
            continue
        # 从 起 开始，找到这个方法体的收尾 `}`（花括号计数）
        深 = 0
        止 = None
        for i in range(起, len(L)):
            for ch in L[i]:
                if ch == "{":
                    深 += 1
                elif ch == "}":
                    深 -= 1
                    if 深 == 0 and i > 起:
                        止 = i
                        break
            if 止 is not None:
                break
        if 止 is None:
            print("  ! %s: 找不到「%s」的方法结尾" % (f, 说明[:20]))
            continue
        L = L[:起] + 新块.rstrip("\n").split("\n") + L[止 + 1:]
        改 += 1
    io.open(P, "w", encoding="utf-8", newline="").write("\n".join(L))
    print("  %s: 改了 %d 处" % (f, 改))
