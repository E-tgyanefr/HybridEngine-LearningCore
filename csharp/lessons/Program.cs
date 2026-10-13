// HybridEngine-light（C# 版）演示 —— 一节课看两件事：怎么画画、报错怎么说人话
//
// 跑： dotnet run --project lessons

using System;
using System.Collections.Generic;
using HybridEngineLight;

internal static class Demo
{
    private static void Main()
    {
        Section("一、三行画出东西");
        // 这三行就是全部 —— 不需要窗口、不需要装任何东西
        var s = new Stage(size: 100, width: 56, height: 14, title: "第一个程序");
        s.Box(0, 0, 100, 8, "绿")        // 草地
         .Dot(25, 60, r: 14, color: "黄")  // 太阳
         .Line(0, 8, 100, 8, "棕", 1)     // 地平线
         .Text(6, 86, "Hello 你好", "青", 12);
        s.Show();
        Console.WriteLine();

        Section("二、报错说人话（C# 新手最常踩的 5 个坑）");
        DemoErrors();

        Section("三、Safe.Run：第一课不用学 try/catch");
        Safe.Run(() =>
        {
            var a = new[] { 1, 2, 3 };
            Console.WriteLine($"  正常代码照常运行：a[2] = {a[2]}");
        });

        Console.WriteLine();
        Console.WriteLine("（完）");
    }

    private static void DemoErrors()
    {
        var cases = new (string Title, Action Act)[]
        {
            ("用了还是 null 的东西（C# 第一大杀手）", () =>
            {
                string? 名字 = null;
                Console.WriteLine(名字!.Length);
            }),
            ("数组越界（忘了从 0 开始数）", () =>
            {
                var a = new[] { 10, 20, 30 };
                Console.WriteLine(a[3]);
            }),
            ("把输入的文字当数字算", () =>
            {
                Console.Write("请输入一个数字：");
                int n = int.Parse("九十五");
                Console.WriteLine(n);
            }),
            ("除以 0", () =>
            {
                int 除数 = 0;
                Console.WriteLine(10 / 除数);
            }),
            ("字典里没有这个键", () =>
            {
                var 库存 = new Dictionary<string, int> { ["苹果"] = 3 };
                Console.WriteLine(库存["香蕉"]);
            }),
        };

        foreach (var (title, act) in cases)
        {
            Console.WriteLine();
            Console.WriteLine($"┌─ {title}");
            try { act(); }
            catch (Exception e)
            {
                // 只打印解释里最有用的部分（完整原文太长，演示里省掉）
                var text = Explain.For(e);
                int cut = text.IndexOf("原始报错", StringComparison.Ordinal);
                Console.WriteLine(cut > 0 ? text.Substring(0, cut).TrimEnd() : text.TrimEnd());
            }
        }
    }

    private static void Section(string title)
    {
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine($"  {title}");
        Console.WriteLine("════════════════════════════════════════════════════════");
    }
}
