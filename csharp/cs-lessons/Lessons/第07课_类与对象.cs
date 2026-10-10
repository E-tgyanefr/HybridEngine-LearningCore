using System;
using System.Collections.Generic;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CsLessons;

[课程("07", "第 07 课：类与对象 —— 把数据和行为放在一起",
    "写一个 `class 计数器`：\n" +
    "  · 有一个只读属性 `int 值`\n" +
    "  · 构造时可以从某个数开始（默认 0）\n" +
    "  · 方法 `加(int n)` 累加，`重置()` 归零\n" +
    "  再写 `class 银行账户`：`存(int n)` / `取(int n)`，取多了要拒绝")]
public static class 第07课
{
    public static string 模板 => """
// 第 07 课：类与对象 —— 把数据和行为放在一起
//
// 要做两个类：
//
//     class 计数器
//     {
//         public int 值 { get; }        // 只读：外面不能直接改
//         public 计数器(int 初值 = 0)    // 构造时可以给起点
//         public void 加(int n)
//         public void 重置()
//     }
//
//     class 银行账户
//     {
//         public int 余额 { get; }
//         public 银行账户(int 初值 = 0)
//         public void 存(int n)
//         public bool 取(int n)         // 钱不够返回 false，且**不能**扣
//     }
//
// ══ 这一课的重点：**把数据和行为关在一起** ══
//
//   没有类的时候，你会写：
//       int 余额 = 0;
//       void 存(int n) { 余额 += n; }        // 余额是个"谁都能改"的变量
//       void 取(int n) { 余额 -= n; }        // 忘了检查够不够 → 余额变负数
//
//   有类之后，"余额"和"改余额的规则"在**同一个地方**：
//   想把余额改成负数？做不到 —— 唯一的入口是 存/取，而 取 会检查。
//
//   ⇒ 这叫**封装**：不是为了"藏起来"，而是为了让**规则只有一个出口**。
//
// ⚠ 三个语法点：
//   ① `{ get; }` 是**只读属性** —— 只有 get，外面赋不了值
//      想改只能通过类自己的方法
//   ② 构造函数名与类名相同、没有返回类型：`public 计数器(int 初值 = 0)`
//   ③ `= 0` 是**默认参数** —— 写 `new 计数器()` 和 `new 计数器(5)` 都能用
//
// 写完后运行：dotnet run --project csharp/cs-lessons 检查 07

// ── 在下面写你的两个类 ──

""";

    public static string 参考答案 => """
class 计数器
{
    public int 值 { get; private set; }

    public 计数器(int 初值 = 0)
    {
        值 = 初值;
    }

    public void 加(int n) => 值 += n;

    public void 重置() => 值 = 0;
}

class 银行账户
{
    public int 余额 { get; private set; }

    public 银行账户(int 初值 = 0)
    {
        余额 = 初值;
    }

    public void 存(int n)
    {
        if (n <= 0) return;          // 不合法就什么也不做
        余额 += n;
    }

    public bool 取(int n)
    {
        if (n <= 0 || n > 余额) return false;   // ⚠ 钱不够必须**拒绝并保持不变**
        余额 -= n;
        return true;
    }
}
""";

    [检查说明("两个类都写了")]
    public static CheckResult 检查_签名(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        foreach (var n in new[] { "class 计数器", "class 银行账户" })
            if (!去注释.Contains(n))
                return CheckResult.不过($"没找到「{n}」。");
        return CheckResult.过();
    }

    [检查说明("计数器：构造 / 加 / 重置 都对")]
    public static CheckResult 检查_计数器(string 代码)
    {
        var 骨架 = """
using System;

public static class 入口
{
/*学生代码*/

    public static void Main()
    {
        var a = new 计数器();
        Console.WriteLine(a.值);
        a.加(3); a.加(4);
        Console.WriteLine(a.值);
        a.重置();
        Console.WriteLine(a.值);

        var b = new 计数器(100);
        Console.WriteLine(b.值);
        b.加(1);
        Console.WriteLine(b.值);
    }
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "0", "7", "0", "100", "101" };
        var 行 = run.行;
        if (行.Count < 5) return CheckResult.不过($"应该打印 5 行，实际 {行.Count} 行。");
        for (int i = 0; i < 5; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是 {期望[i]}，实际是「{行[i].Trim()}」。\n" +
                    "      ⚠ 注意第 4 个：`new 计数器(100)` 要能从 100 开始\n" +
                    "        ⇒ 构造函数要有 `int 初值 = 0` 这个参数。");
        return CheckResult.过("默认 0 起 / 累加 / 重置 / 指定初值 都对");
    }

    [检查说明("银行账户：钱不够时**拒绝**且余额不变")]
    public static CheckResult 检查_账户(string 代码)
    {
        var 骨架 = """
using System;

public static class 入口
{
/*学生代码*/

    public static void Main()
    {
        var a = new 银行账户(100);
        Console.WriteLine(a.余额);
        a.存(50);
        Console.WriteLine(a.余额);
        Console.WriteLine(a.取(30) + ":" + a.余额);
        Console.WriteLine(a.取(999) + ":" + a.余额);   // 取不出来，余额不能变
        Console.WriteLine(a.取(120) + ":" + a.余额);   // 正好取光
    }
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = { "100", "150", "True:120", "False:120", "True:0" };
        var 行 = run.行;
        if (行.Count < 5) return CheckResult.不过($"应该打印 5 行，实际 {行.Count} 行。");
        for (int i = 0; i < 5; i++)
            if (行[i].Trim() != 期望[i])
                return CheckResult.不过(
                    $"第 {i + 1} 个用例应当是「{期望[i]}」，实际是「{行[i].Trim()}」。\n" +
                    "      ⚠ 最要紧的是第 4 个：**取不出来的时候余额必须一点都不变**。\n" +
                    "        常见错法：先 `余额 -= n;` 再检查 —— 那时已经扣了。\n" +
                    "        正确顺序：**先检查，通过才扣**。");
        return CheckResult.过("存取 / 拒绝 / 正好取光 都对");
    }

    [检查说明("余额是只读属性（外面不能直接改）")]
    public static CheckResult 检查_只读(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("{ get;"))
            return CheckResult.不过(
                "没看到只读属性写法 `{ get; ... }`。\n" +
                "      这一课要练的就是封装：余额/值只能由类自己的方法改。");
        return CheckResult.过();
    }

    internal static string 去掉注释(string s)
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
