import io
import os
import sys
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
S = os.path.join(LC, "csharp", "cs-lessons", "Lessons")

# 各课的 限定() 里，除了补方法名的 public，还要补**顶层类名**的 public。
旧 = """    private static string 限定(string 代码, string 调用点)
    {
        var 名字们 = new List<string>();"""

新 = """    private static string 限定(string 代码, string 调用点)
    {
        // ⚠⚠ **类**也要公开（第 07/08 课踩到）：
        //   学员写的 `class 计数器` 默认是 `internal`，而库生成的 `入口`
        //   与它平级却在**同一个程序集**里 —— 严格说能访问，但我们的
        //   调用点在 `入口` 类内，写 `学生代码.计数器` 时要求
        //   **嵌套可见性**成立，而 `学生代码` 是 public 静态类、
        //   里面的 `计数器` 若是 internal 就不可见 ⇒
        //       error CS0122: '计数器' 不可访问，因为它具有一定的保护级别
        //   ⇒ 和"方法补 public"同理，把**顶层类**也补上 public。
        //     方法补 public 在 `补公开` 里做，类在这里补。
        var 类名们 = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(
                     代码, @"(?m)^\\s*(?:public\\s+|internal\\s+|private\\s+)?(?:static\\s+)?(?:sealed\\s+)?(?:abstract\\s+)?class\\s+(\\w+)"))
        {
            string 全 = m.Value;
            if (全.Contains("public") || 全.Contains("internal")
                || 全.Contains("private") || 全.Contains("protected")) continue;
            类名们.Add(m.Groups[1].Value);
        }
        foreach (var 名 in 类名们)
            调用点 = System.Text.RegularExpressions.Regex.Replace(
                调用点, @"(?<![\\w.])" + System.Text.RegularExpressions.Regex.Escape(名) + @"\\b",
                "学生代码." + 名);

        var 名字们 = new List<string>();"""

n = 0
for f in sorted(os.listdir(S)):
    if not f.endswith(".cs"):
        continue
    P = os.path.join(S, f)
    t = io.open(P, encoding="utf-8").read()
    if 旧 not in t:
        print("  ! %s: 限定() 锚点未找到" % f)
        continue
    io.open(P, "w", encoding="utf-8", newline="").write(t.replace(旧, 新, 1))
    n += 1
print("  %d 课的 限定() 已加类名限定" % n)

# 另外：`补公开` 也要给**顶层类**补 public（否则 学生代码.计数器 仍不可访问）
P = os.path.join(LC, "csharp", "HybridEngineLight", "CppLesson.cs")
t = io.open(P, encoding="utf-8").read()
旧2 = """        var 出 = new System.Text.StringBuilder();
        foreach (var 原行 in 成员.Split('\\n'))
        {
            string 行 = 原行;
            string 去空格 = 行.TrimStart();"""
新2 = """        var 出 = new System.Text.StringBuilder();
        foreach (var 原行 in 成员.Split('\\n'))
        {
            string 行 = 原行;
            string 去空格 = 行.TrimStart();
            // ⚠ **类**也要补 public（第 07/08 课踩到）：
            //   嵌套在 `学生代码` 里的 `class 计数器` 若没有 public，
            //   `学生代码.计数器` 就不可访问：
            //       error CS0122: '计数器' 不可访问，因为它具有一定的保护级别
            //   ⇒ 与"方法补 public"同理，课程替他补上，不让学员记这个细节。
            if (去空格.StartsWith("class ", StringComparison.Ordinal))
            {
                int 缩进0 = 行.Length - 去空格.Length;
                行 = 行.Substring(0, 缩进0) + "public " + 去空格;
                出.AppendLine(行);
                continue;
            }"""
if 旧2 in t:
    io.open(P, "w", encoding="utf-8", newline="").write(t.replace(旧2, 新2, 1))
    print("  CppLesson.cs: 补公开 已同时处理类")
else:
    print("  ! CppLesson.cs 锚点未找到")
