// HybridEngineLight.CCourse —— C 语言课程的定位与路径。
//
// ══ 为什么不复制一份运行器 ══
//
//   C 与 C++ **用同一个编译器**（gcc/g++ 是同一套），差别只有：
//     · 标准旗标：-std=c11 vs -std=c++17
//     · 源文件后缀：.c vs .cpp
//     · 骨架与打印：printf vs std::cout
//   这些都由 `CppRunner.用C` 与 `CppCheck` 处理了。
//
//   ⚠ 复制一份 360 行的运行器只为了改这几个字，会让以后的 bug 要修两处 ——
//     而那份代码里每个坑（旧目录复用、删完才判编译成功、退出码必须看）
//     都是**实测撞出来的**，绝不该有第二份。
//
// ══ 这个文件只负责一件事 ══
//
//   「C 的答题文件放哪」。C++ 用 `cpp-answers/`、Java 用 `java-answers/`，
//   C 用 `c-answers/` —— 三者互不干扰，学生一眼能分清。
using System;
using System.IO;
using System.Text;

namespace HybridEngineLight;

public static class CCourse
{
    /// <summary>C 课答题文件所在目录（相对仓库根）。</summary>
    public const string 答题目录 = "c-answers";

    /// <summary>某一课的答题文件路径。</summary>
    public static string 答案路径(string 根目录, CourseDef def) =>
        Path.Combine(根目录, 答题目录, $"{def.编号}_{清理(def.标题)}.c");

    /// <summary>从标题里取一段干净的名字做文件名（与 CppCourse 同一套规则）。</summary>
    private static string 清理(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s)
            if (char.IsLetterOrDigit(c)) sb.Append(c);
        return sb.Length > 0 ? sb.ToString() : "作业";
    }

    /// <summary>第一次检查时自动生成答题模板。</summary>
    public static string 生成模板(CourseDef def)
    {
        if (def.模板.Length > 0) return def.模板;
        var sb = new StringBuilder();
        sb.Append("/* C 语言 */\n");
        sb.Append("// ").Append(def.标题).Append('\n');
        sb.Append("//\n// 要做：\n");
        foreach (var l in def.要做.Split('\n'))
            sb.Append("//   ").Append(l.TrimEnd()).Append('\n');
        sb.Append("//\n// 写完之后运行：\n");
        sb.Append("//   dotnet run --project csharp/c-lessons 检查 ").Append(def.编号).Append('\n');
        sb.Append("//\n// ── 在下面写你的代码 ──\n\n");
        return sb.ToString();
    }
}
