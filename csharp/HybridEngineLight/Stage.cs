// HybridEngineLight.Stage —— 给新手的第一块画布（C# 版）
//
// 与 Python 版共用同一套设计原则（见仓库根 README）：
//   1. 零第三方依赖
//   2. 不用像素坐标 —— 虚拟坐标 100×100，原点在**左下角**（和数学课一致）
//   3. 颜色可以直接写中文
//   4. 绘图指令与显示分离（便于测试，也让"动画"变得自然）
//
// ══ C# 版为什么画在控制台，而不是窗口 ══
// C# 里能画窗口的选项都不满足"零依赖 + 跨平台"：
//   · WinForms        —— 随 .NET 提供，但只在 Windows，且要 net8.0-windows
//   · System.Drawing  —— 同样 Windows-only（跨平台的那部分已弃用）
// 控制台用 ANSI 真彩色转义码是唯一两头都满足的路子，而且：
//   · 学习阶段看到"字符组成的画面"完全够用（能画点、线、方块、文字）
//   · 顺带让学生接触到"坐标 → 像素 → 终端"这条链路，而不是被 GUI 细节淹没
//
// 显示技巧：用半方块字符 `▀`（U+2580）。一个字符位能同时表示**上、下两个像素**
// （前景色画上半、背景色画下半），于是等效分辨率直接翻倍。
// 这是终端里画图的标准做法，不是取巧。

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace HybridEngine-light;

/// <summary>画布上的一条绘图指令。与显示无关，便于测试。</summary>
public abstract record Shape;
public sealed record Dot(double X, double Y, double R, string Color) : Shape;
public sealed record Box(double X, double Y, double W, double H, string Color) : Shape;
public sealed record Line(double X1, double Y1, double X2, double Y2, string Color, double Thick) : Shape;
public sealed record Text(double X, double Y, string S, string Color, int FontSize) : Shape;

/// <summary>
/// 一块画布。坐标是 0..Size 的虚拟坐标，**原点在左下角**。
///
/// 典型用法（新手第一个程序）：
/// <code>
/// var s = new Stage();
/// s.Dot(50, 50, r: 20, color: "红");
/// s.Wait();
/// </code>
/// </summary>
public sealed class Stage
{
    public int Size { get; }
    public int Width { get; }
    public int Height { get; }
    public string Title { get; }
    public string Background { get; set; } = "白";

    private readonly List<Shape> _shapes = new();
    private readonly StringBuilder _sb = new();

    /// <param name="size">虚拟坐标范围（默认 100，即 0..100）</param>
    /// <param name="width">屏幕上的字符列数</param>
    /// <param name="height">屏幕上的字符行数 ×2 = 像素行数（半方块）</param>
    public Stage(int size = 100, int width = 60, int height = 20, string title = "hybridengine_light")
    {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), "画布尺寸必须大于 0");
        Size = size;
        Width = width;
        Height = height;
        Title = title;
    }

    // ── 坐标换算：虚拟坐标 → 像素格 ──
    //
    // ⚠ 这里在 Python 版踩过一次坑：y 的缩放误用了宽度比例，方形画布看不出问题，
    //   一旦 width ≠ height 就错位。两个轴必须各用各的比例。C# 版一开始就分开。
    public (double Px, double Py) ToPixel(double x, double y)
    {
        double kx = Width / (double)Size;
        double ky = (Height * 2) / (double)Size;   // 像素行数 = 字符行数 ×2
        return (x * kx, (Height * 2) - 1 - y * ky); // y 翻转：数学坐标 → 屏幕坐标
    }
    public double ScaleR(double r) => r * (Width / (double)Size);

    // ── 绘图指令（只记录，不画）──
    public Stage Dot(double x, double y, double r = 5, string color = "黑")
    { _shapes.Add(new Dot(x, y, r, color)); return this; }

    /// <summary>以 (x, y) 为**左下角**画一个 w×h 的实心矩形。</summary>
    public Stage Box(double x, double y, double w, double h, string color = "蓝")
    { _shapes.Add(new Box(x, y, w, h, color)); return this; }

    public Stage Line(double x1, double y1, double x2, double y2, string color = "黑", double thick = 1)
    { _shapes.Add(new Line(x1, y1, x2, y2, color, thick)); return this; }

    public Stage Text(double x, double y, string s, string color = "黑", int fontSize = 0)
    { _shapes.Add(new Text(x, y, s, color, fontSize)); return this; }

    /// <summary>擦掉画布上所有东西。</summary>
    public Stage Clear() { _shapes.Clear(); return this; }

    /// <summary>当前记录的图形指令。测试用 —— 不需要终端、不需要显示。</summary>
    public IReadOnlyList<Shape> Snapshot() => _shapes;

    // ── 真正显示 ──
    /// <summary>
    /// 把画布渲染成字符串（每行一个字符行，含 ANSI 真彩色转义码）。
    /// 之所以单独暴露成纯函数：**测试不需要终端也能验证渲染结果**。
    /// </summary>
    public string Render()
    {
        int px = Width, py = Height * 2;
        var buf = new (byte R, byte G, byte B)[px * py];
        var bg = Parser.Color(Background);
        for (int i = 0; i < buf.Length; i++) buf[i] = bg;

        foreach (var sh in _shapes) DrawShape(buf, px, py, sh);

        _sb.Clear();
        for (int row = 0; row < Height; row++)
        {
            int top = row * 2, bot = top + 1;
            for (int col = 0; col < px; col++)
            {
                var t = buf[top * px + col];
                var b = buf[bot * px + col];
                // `▀`：前景色填上半，背景色填下半 —— 一个字符位表示两个像素
                _sb.Append("\x1b[38;2;").Append(t.R).Append(';').Append(t.G).Append(';').Append(t.B)
                    .Append("m\x1b[48;2;").Append(b.R).Append(';').Append(b.G).Append(';').Append(b.B)
                    .Append("m\u2580");
            }
            _sb.Append("\x1b[0m\n");
        }
        return _sb.ToString();
    }

    private void DrawShape((byte R, byte G, byte B)[] buf, int px, int py, Shape sh)
    {
        switch (sh)
        {
            case Dot d:
            {
                var (cx, cy) = ToPixel(d.X, d.Y);
                double r = Math.Max(0.5, ScaleR(d.R));
                var c = Parser.Color(d.Color);
                for (int y = (int)Math.Floor(cy - r); y <= (int)Math.Ceiling(cy + r); y++)
                    for (int x = (int)Math.Floor(cx - r); x <= (int)Math.Ceiling(cx + r); x++)
                    {
                        double dx = x - cx, dy = y - cy;
                        if (dx * dx + dy * dy <= r * r) Put(buf, px, py, x, y, c);
                    }
                break;
            }
            case Box b:
            {
                var (x0, y0) = ToPixel(b.X, b.Y + b.H);   // 左下角在屏幕上更靠下
                var (x1, y1) = ToPixel(b.X + b.W, b.Y);
                var c = Parser.Color(b.Color);
                for (int y = (int)Math.Round(y0); y <= (int)Math.Round(y1); y++)
                    for (int x = (int)Math.Round(x0); x <= (int)Math.Round(x1); x++)
                        Put(buf, px, py, x, y, c);
                break;
            }
            case Line ln:
            {
                var (ax, ay) = ToPixel(ln.X1, ln.Y1);
                var (bx, by) = ToPixel(ln.X2, ln.Y2);
                var c = Parser.Color(ln.Color);
                double len = Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
                int steps = Math.Max(1, (int)Math.Ceiling(len * 2));
                for (int i = 0; i <= steps; i++)
                {
                    double t = i / (double)steps;
                    double x = ax + (bx - ax) * t, y = ay + (by - ay) * t;
                    Put(buf, px, py, (int)Math.Round(x), (int)Math.Round(y), c);
                }
                break;
            }
            case Text tx:
            {
                // 终端里写字不能逐像素控制字形，所以按字符格落位（够学习用）。
                var (cx, cy) = ToPixel(tx.X, tx.Y);
                var c = Parser.Color(tx.Color);
                int row = (int)Math.Round(cy / 2), col = (int)Math.Round(cx);
                if (row >= 0 && row < Height && col >= 0 && col < px)
                {
                    for (int i = 0; i < tx.S.Length && col + i < px; i++)
                    {
                        Put(buf, px, py, col + i, row * 2, c);
                        Put(buf, px, py, col + i, row * 2 + 1, c);
                    }
                }
                break;
            }
        }
    }

    private static void Put((byte R, byte G, byte B)[] buf, int px, int py, int x, int y,
                            (byte R, byte G, byte B) c)
    {
        if (x < 0 || y < 0 || x >= px || y >= py) return;   // 越界不报错 —— 新手不该被这个卡住
        buf[y * px + x] = c;
    }

    /// <summary>把画面输出到控制台。</summary>
    public Stage Show()
    {
        Console.Write(Render());
        return this;
    }

    /// <summary>停住画面让人看。不传参数就等按键。</summary>
    public Stage Wait(double? seconds = null)
    {
        Show();
        if (seconds is null)
        {
            Console.WriteLine("（按任意键继续）");
            Console.ReadKey(intercept: true);
        }
        else
        {
            System.Threading.Thread.Sleep((int)(seconds.Value * 1000));
        }
        return this;
    }
}
