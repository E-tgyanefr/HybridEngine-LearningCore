// 第 17~40 课共用的**检查工具**。
//
// ══ 为什么单独一个文件，而不是每课抄一份 ══
// 这批课是同一个作者一次写的，检查里「拼调用点 / 按标记对答案 / 去掉注释 /
// 取函数体 / 编译缓存」这五件事**每课都一样**。抄 24 份的代价不是多几行，
// 而是**改一处要改 24 处** —— 这个项目在别处已经因此栽过好几次。
//
// ⚠⚠ 它**不放在 Lessons\ 里**，这是有意的：
//   `Lessons\` 的约定是「一课一个文件」，而 `_build\_verify_compiled.py`
//   会把那个目录下**每个** .cs 都当成一课去解析参考答案；
//   解析不出来就记一笔「没验到」，而"没验到"会让整个验收直接判失败。
//   ⇒ 放在工程根目录下：csproj 照样编译它，验收脚本却不会把它当课程。
//
// ⚠ Lesson 17 was written before this file existed and carries its own copy of
//   these helpers on purpose (it doubles as the hand-written "standard shape"
//   that the rest of the batch is modelled on).

using System;
using System.Collections.Generic;
using HybridEngineLight;

namespace HybridEngineLight.CsLessons;

/// <summary>第 17~40 课共用的检查工具。</summary>
public static class 课工具
{
    // ── 编译缓存：一次检查里所有检查方法共用同一份学生代码 ──
    // 每次 `CppCheck.编译并运行` 都要起一次 dotnet（一两秒），
    // 而一课的检查有五六条、输入是同一份代码 —— 不缓存就是白等十几秒。
    private static string _上次代码;
    private static string _上次调用点;
    private static CppRun _上次运行;

    /// <summary>编译并运行学生代码（同一份代码只编译一次）。</summary>
    public static CppRun 跑一次(string 代码, string 调用点)
    {
        if (_上次运行 != null && _上次代码 == 代码 && _上次调用点 == 调用点)
            return _上次运行;
        _上次代码 = 代码;
        _上次调用点 = 调用点;
        // ⚠ `CppLesson.限定` 必须走：调用点在**另一个类**（入口）里，
        //   学生定义的方法名不限定就找不到（CS0103）。
        _上次运行 = CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 调用点));
        return _上次运行;
    }

    /// <summary>「函数都在」用的探针：编译一次、调一遍所有要求的方法。</summary>
    public static CppRun 探针(string 代码, string 探针代码) =>
        CppCheck.编译并运行(代码, null, CppLesson.限定(代码, 探针代码));

    /// <summary>
    /// 拼调用点：每组一次调用、打一行「标记|值」，**各自 try/catch**。
    ///
    /// ⚠ 为什么各自 try/catch：学生代码抛异常时只影响那一组。
    ///   不包的话整次运行会崩，所有检查一起报「程序没跑完」，
    ///   学生看不出是哪一组出的问题。
    /// </summary>
    public static string 造调用点(string 准备, params (string 标记, string 表达式)[] 用例们)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(准备).Append('\n');
        foreach (var (标记, 表达式) in 用例们)
        {
            sb.Append("try { Console.WriteLine(\"").Append(标记).Append("|\" + (")
              .Append(表达式)
              .Append(")); } catch (Exception __e) { Console.WriteLine(\"")
              .Append(标记).Append("|崩溃:\" + __e.GetType().Name); }\n");
        }
        return sb.ToString();
    }

    /// <summary>
    /// 同上，但每组可以**先跑几条语句**再取值 —— 给「就地排序」这类 void 方法用：
    ///     语句 = "int[] 甲 = { 5, 1, 4, 2, 8 }; 冒泡排序(甲);"
    ///     表达式 = "string.Join(\" \", 甲)"
    /// 每组在自己的 try 里，所以局部变量不会互相撞。
    /// </summary>
    public static string 造语句调用点(string 准备,
        params (string 标记, string 语句, string 表达式)[] 用例们)
    {
        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(准备)) sb.Append(准备).Append('\n');
        foreach (var (标记, 语句, 表达式) in 用例们)
        {
            sb.Append("try { ").Append(语句)
              .Append(" Console.WriteLine(\"").Append(标记).Append("|\" + (")
              .Append(表达式)
              .Append(")); } catch (Exception __e) { Console.WriteLine(\"")
              .Append(标记).Append("|崩溃:\" + __e.GetType().Name); }\n");
        }
        return sb.ToString();
    }

    /// <summary>
    /// 比一批「标记|值」。
    /// ⚠ 按**标记**找那一行，不按行号 —— 学生自己多打一行调试输出也不会错位。
    /// </summary>
    public static CheckResult 比一比(CppRun run, string 提示,
        params (string 标记, string 期望, string 说明)[] 用例们)
    {
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        var 错 = new List<string>();
        foreach (var (标记, 期望, 说明) in 用例们)
        {
            string 得到 = 取标记(run, 标记);
            if (得到 == null) 错.Add("      " + 说明 + "：程序没有给出这一组的输出");
            else if (得到.StartsWith("崩溃", StringComparison.Ordinal))
                错.Add("      " + 说明 + "：这一组**抛异常**了（" + 得到 + "）");
            else if (得到 != 期望)
                错.Add("      " + 说明 + "：得到 " + 显示(得到) + "，应该是 " + 显示(期望));
        }
        if (错.Count == 0) return CheckResult.过("这几组都对");
        return CheckResult.不过("结果不对：\n" + string.Join("\n", 错) + "\n" + 提示);
    }

    /// <summary>取出「标记|」这一行后面的值。没有这一行就返回 null。</summary>
    public static string 取标记(CppRun run, string 标记)
    {
        string 头 = 标记 + "|";
        foreach (var l in run.行)
        {
            string s = l.Trim();
            if (s.StartsWith(头, StringComparison.Ordinal)) return s.Substring(头.Length).Trim();
        }
        return null;
    }

    /// <summary>某个标记是不是「崩溃:XXX」（学生代码抛了异常）。</summary>
    public static bool 崩了(CppRun run, string 标记) =>
        (取标记(run, 标记) ?? "").StartsWith("崩溃", StringComparison.Ordinal);

    public static string 显示(string s) => string.IsNullOrEmpty(s) ? "（空）" : s;

    /// <summary>去掉注释，免得注释里的字样骗过文本检查。</summary>
    public static string 去掉注释(string s)
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

    /// <summary>取某个方法名的「{ … }」内容（配对花括号）。找不到返回 null。</summary>
    public static string 函数体(string 净源码, string 方法名)
    {
        int 名 = 净源码.IndexOf(方法名 + "(", StringComparison.Ordinal);
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

    /// <summary>体里有没有这些词里的任意一个（文本判据常用）。</summary>
    public static bool 含(string 体, params string[] 词)
    {
        if (体 == null) return false;
        foreach (var w in 词) if (体.Contains(w)) return true;
        return false;
    }

    /// <summary>「函数都在」那条检查的统一失败提示。</summary>
    public static CheckResult 函数缺失(CppRun run, string 签名清单) =>
        CheckResult.不过(
            "有方法没找到（或者签名和题目不一样）。这一课要求的签名是：\n" + 签名清单 +
            "\n      " + 首条错误(run));

    /// <summary>编译信息里的第一行 error（给「函数没写」这类提示用）。</summary>
    public static string 首条错误(CppRun run)
    {
        foreach (var line in run.编译信息.Split('\n'))
        {
            string s = line.Trim();
            if (s.Contains("error CS")) return "编译器说：" + s;
        }
        return "";
    }
}
