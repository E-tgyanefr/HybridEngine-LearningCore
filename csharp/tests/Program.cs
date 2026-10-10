// HybridEngine-light 的测试 —— 纯控制台，零测试框架依赖。
//
// 为什么不引入 xUnit/MSTest：这个包的目标就是「装完即用、不依赖第三方」。
// 连测试都要额外装包，就跟这个目标自相矛盾了。（Python 版同理，用自带的 unittest。）
//
// 跑： dotnet run --project tests

using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngine-light;

internal static class Tests
{
    private static int _pass, _fail;
    private static readonly List<string> Failures = new();

    private static void Check(bool ok, string what)
    {
        if (ok) { _pass++; }
        else { _fail++; Failures.Add(what); Console.WriteLine($"  FAIL: {what}"); }
    }

    /// <summary>非泛型版本 —— 泛型方法没法直接当委托传给 CppTests。</summary>
    private static void EqObj(object actual, object expected, string what)
        => Check(Equals(actual, expected), $"{what}（期望 {expected}，实际 {actual}）");

    private static void Eq<T>(T actual, T expected, string what)
        => Check(EqualityComparer<T>.Default.Equals(actual, expected),
                 $"{what}（期望 {expected}，实际 {actual}）");

    private static int Main()
    {
        Console.WriteLine("=== HybridEngine-light (C#) 测试 ===\n");

        Coordinates();
        Shapes();
        Colors();
        Rendering();
        ErrorTranslation();
        SafeRunner();
        CppTests.Run(Check, EqObj);

        Console.WriteLine($"\n=== 通过 {_pass}，失败 {_fail} ===");
        return _fail == 0 ? 0 : 1;
    }

    // ── 坐标换算：最容易悄悄错的地方（Python 版就在这儿踩过）──
    private static void Coordinates()
    {
        // 方形画布：100 虚拟单位 → 60 列 × 20 字符行（= 40 像素行）
        var s = new Stage(size: 100, width: 100, height: 50);
        var (px, py) = s.ToPixel(0, 0);
        Eq(px, 0.0, "原点 x 在屏幕最左");
        Eq(py, 99.0, "原点 y 在屏幕最下（像素行 50*2-1=99）");

        (px, py) = s.ToPixel(100, 100);
        Eq(px, 100.0, "右上角 x 在屏幕最右");
        Eq(py, -1.0, "右上角 y 在屏幕最上");

        // ⚠ 非方形画布：两个轴必须各用各的比例。
        //   Python 版这里错过一次（y 误用了宽度比例），方形画布看不出来。
        var ns = new Stage(size: 100, width: 500, height: 250);
        (px, py) = ns.ToPixel(50, 50);
        Eq(px, 250.0, "非方形：x 按宽度比例缩放");
        // 注意 Height 是**字符行数**（250），不是像素行数。像素行数 = 250×2 = 500。
        //   ky = (Height*2)/Size = 500/100 = 5
        //   y  = (Height*2) - 1 - 50*ky = 499 - 250 = 249
        // ⚠ 我第一次把这个期望值"修正"成 399，是因为误把 Height 当成了像素行数 ——
        //   代码一直是对的，是我改测试改错了。**改动前先读实现。**
        Eq(py, 249.0, "非方形：y 按高度比例缩放（499-250=249）");

        // y 轴方向：数学坐标 y 变大 → 屏幕像素 y 变小
        var s2 = new Stage(size: 100, width: 100, height: 50);
        var (_, yLow) = s2.ToPixel(0, 20);
        var (_, yHigh) = s2.ToPixel(0, 80);
        Check(yHigh < yLow, "y 更大时像素 y 更小（往上）");
    }

    // ── 形状记录：不需要终端 ──
    private static void Shapes()
    {
        var s = new Stage();
        s.Dot(1, 1).Box(2, 2, 3, 3).Line(0, 0, 9, 9).Text(5, 5, "hi");
        var kinds = s.Snapshot().Select(x => x.GetType().Name).ToList();
        Eq(string.Join(",", kinds), "Dot,Box,Line,Text", "四条指令按顺序记录");

        var s2 = new Stage();
        Check(ReferenceEquals(s2.Dot(1, 1).Dot(2, 2), s2), "每个绘图方法返回自身（可链式）");

        var s3 = new Stage();
        s3.Dot(1, 1).Clear();
        Eq(s3.Snapshot().Count, 0, "Clear 清空记录");

        var s4 = new Stage();
        s4.Dot(10, 20, r: 7, color: "红");
        var d = (Dot)s4.Snapshot()[0];
        Eq(d.X, 10.0, "Dot 保存 x");
        Eq(d.Y, 20.0, "Dot 保存 y");
        Eq(d.R, 7.0, "Dot 保存半径");
        Eq(d.Color, "红", "Dot 保存颜色");
    }

    // ── 颜色 ──
    private static void Colors()
    {
        Eq(Parser.Color("红"), (0xe5, 0x48, 0x4d), "中文颜色名");
        Eq(Parser.Color("red"), (0xe5, 0x48, 0x4d), "英文颜色名");
        Eq(Parser.Color("RED"), (0xe5, 0x48, 0x4d), "英文名不分大小写");
        Eq(Parser.Color("#123456"), ((byte)0x12, (byte)0x34, (byte)0x56), "十六进制写法");
        Eq(Parser.Color("不存在的颜色"), Parser.Unknown, "未知颜色名返回显眼色（不抛异常）");
        Eq(Parser.Color(null), (0x00, 0x00, 0x00), "null 当黑色处理");
        Check(Parser.IsKnown("红"), "IsKnown 认得中文名");
        Check(!Parser.IsKnown("量子色"), "IsKnown 不认得乱写的");
    }

    // ── 渲染：验证真的产出了像素（这是"能出画面"的机器可验形式）──
    private static void Rendering()
    {
        var s = new Stage(size: 100, width: 20, height: 5);
        string blank = s.Render();
        Check(blank.Contains("\x1b[38;2;"), "渲染输出含 ANSI 真彩色转义码");
        Check(blank.Contains('\u2580'), "用了半方块字符（一个字符位=两个像素）");

        int lines = blank.TrimEnd('\n').Split('\n').Length;
        Eq(lines, 5, "行数等于字符行数");

        // 画一个红点后，输出里应出现红色的前景码
        var s2 = new Stage(size: 100, width: 20, height: 5);
        s2.Dot(50, 50, r: 30, color: "红");
        string withDot = s2.Render();
        // ⚠ 不能断言"更长" —— ANSI 码长短不一（黑色的 0;0;0 比红色的 229;72;77 短），
        //   所以内容变了长度也可能变短。我第一次就是这么写错的。要比的是"不同"。
        Check(withDot != blank, "画了点之后输出内容改变（真的有像素被改）");
        Check(withDot.Contains("38;2;229;72;77"), "红点的前景色是 #e5484d");

        // 空白画布不该含红色的前景码
        Check(!blank.Contains("38;2;229;72;77"), "空白画布不含红色");

        // 越界不该抛异常 —— 新手不该被这个卡住
        var s3 = new Stage(size: 100, width: 10, height: 3);
        bool ok = true;
        try { s3.Dot(-500, -500, r: 10).Box(200, 200, 50, 50).Line(-9, -9, 999, 999).Show(); }
        catch { ok = false; }
        Check(ok, "越界绘图不抛异常");
    }

    // ── 报错翻译：核心功能，逐条钉住 ──
    private static void ErrorTranslation()
    {
        string Msg(Action act)
        {
            try { act(); }
            catch (Exception e) { return Explain.For(e); }
            throw new InvalidOperationException("这段代码本该报错");
        }

        // NullReferenceException —— C# 新手第一大杀手
        var m = Msg(() => { string? s = null; _ = s!.Length; });
        Check(m.Contains("还是空"), "NullReference 解释提到 null");
        Check(m.Contains("怎么改"), "NullReference 给了修改建议");
        Check(m.Contains("!= null"), "NullReference 建议用 != null 判断");

        // 数组越界 + 从 0 开始
        m = Msg(() => { var a = new[] { 1, 2, 3 }; _ = a[5]; });
        Check(m.Contains("从 0 开始"), "越界解释强调下标从 0 开始");
        Check(m.Contains("< 数组.Length") || m.Contains("Length"), "越界解释提到用 Length 定界");

        // int.Parse 失败
        m = Msg(() => _ = int.Parse("九十五"));
        Check(m.Contains("TryParse"), "格式错误建议改用 TryParse");
        Check(m.Contains("用户"), "格式错误提到用户输入这个常见来源");

        // 除零
        m = Msg(() => { int z = 0; _ = 1 / z; });
        Check(m.Contains("除数"), "除零解释点明是除数");
        Check(m.Contains("Infinity") || m.Contains("无穷"), "除零解释区分了浮点数不崩");

        // 字典键不存在
        m = Msg(() => { var d = new Dictionary<string, int> { ["a"] = 1 }; _ = d["b"]; });
        Check(m.Contains("TryGetValue"), "字典缺键建议 TryGetValue");

        // 空序列 First()
        m = Msg(() => _ = Array.Empty<int>().First());
        Check(m.Contains("FirstOrDefault"), "空序列建议 FirstOrDefault");

        // 找不到文件
        m = Msg(() => _ = System.IO.File.ReadAllText("绝对不存在的文件_99.txt"));
        Check(m.Contains("工作目录"), "文件找不到时提到工作目录这个坑");

        // 编译错误（用消息文本模拟 —— 编译器不在运行时，但编号识别逻辑要能测）
        m = Explain.For(new Exception("error CS0103: 名称“你好”在当前上下文中不存在"));
        Check(m.Contains("区分大小写"), "CS0103 提到 C# 区分大小写");

        m = Explain.For(new Exception("error CS1002: 应输入 ;"));
        Check(m.Contains("分号"), "CS1002 提到分号");

        m = Explain.For(new Exception("error CS1513: 应输入 }"));
        Check(m.Contains("大括号"), "CS1513 提到大括号配对");

        m = Explain.For(new Exception("error CS0019: 运算符“+”无法应用于“string”和“int”"));
        Check(m.Contains("插值"), "CS0019 建议字符串插值");

        m = Explain.For(new Exception("error CS0246: 未能找到类型或命名空间名"));
        Check(m.Contains("using"), "CS0246 提到 using");

        // 认不出来时要**承认**，而不是编
        m = Explain.For(new Exception("某种没人见过的错误"));
        Check(m.Contains("没有内置解释"), "未知错误承认没有内置解释");
        Check(m.Contains("某种没人见过的错误"), "未知错误保留原文");

        // 原文必须始终保留
        m = Msg(() => { int z = 0; _ = 1 / z; });
        Check(m.Contains("DivideByZeroException"), "始终保留原始报错类型");
        Check(m.Contains("   at ") || m.Contains("at "), "始终保留调用栈");

        // 每条解释都必须给出可操作建议，不能只说"你错了"
        var cases = new (string Name, Action Act)[]
        {
            ("NullReference", () => { string? x = null; _ = x!.Length; }),
            ("IndexOutOfRange", () => { var a = new int[1]; _ = a[9]; }),
            ("Format", () => _ = int.Parse("x")),
            ("DivideByZero", () => { int z = 0; _ = 1 / z; }),
            ("KeyNotFound", () => { var d = new Dictionary<string, int>(); _ = d["k"]; }),
            ("ArgumentNull", () => _ = System.IO.File.ReadAllText(null!)),
        };
        foreach (var (name, act) in cases)
        {
            try { act(); }
            catch (Exception e)
            {
                var text = Explain.For(e);
                Check(text.Contains("怎么改"), $"{name} 的解释包含「怎么改」");
            }
        }
    }

    // ── Safe.Run ──
    private static void SafeRunner()
    {
        bool ok = Safe.Run(() => { var s = new Stage(); s.Dot(1, 1); });
        Check(ok, "Safe.Run 成功时返回 true");

        // 失败时不抛异常（它自己打印解释）
        var sw = Console.Out;
        Console.SetOut(System.IO.TextWriter.Null);   // 别把解释打到测试输出里
        bool bad;
        try { bad = Safe.Run(() => throw new InvalidOperationException("测试用错误")); }
        finally { Console.SetOut(sw); }
        Check(!bad, "Safe.Run 失败时返回 false（不抛异常）");

        Eq(Safe.Run<int>(() => 42), 42, "Safe.Run<T> 返回正确值");
    }
}
