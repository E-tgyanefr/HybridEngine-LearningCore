import io
import os
import re
import sys
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

# ① 回退 CppCheck.入口（临时工程不引用我们的库 ⇒ 生成代码里没有 CppCheck，用不了）
P = os.path.join(LC, "csharp", "HybridEngineLight", "CppLesson.cs")
t = io.open(P, encoding="utf-8").read()
m = re.search(r"    /// <summary>\n    /// 反射入口 ——.*?\n    \}\n\n", t, re.S)
if m:
    t = t[:m.start()] + t[m.end():]
    io.open(P, "w", encoding="utf-8", newline="").write(t)
    print("  (1) 已回退反射入口（临时工程没有 CppCheck —— 用不了）")
else:
    print("  (1) ! 找不到反射入口块")

# ② 各课的 调用 辅助：在课程侧把裸方法名限定成 学生代码.xxx
旧 = """    private static CppRun 调用(string 代码, string 调用点)
    {
        // ⚠ 走**骨架 + 调用点**：骨架提供一个 `入口.Main`，里面执行调用点。
        //   调用点里用 `CppCheck.入口.调用(...)` 反射调学员的方法
        //   （因为两个类平级，直接写方法名编译不过 —— 见 CppCheck.入口 的说明）。
        return CppCheck.编译并运行(代码, null, 调用点);
    }"""

新 = """    private static CppRun 调用(string 代码, string 调用点)
    {
        return CppCheck.编译并运行(代码, null, 限定(代码, 调用点));
    }

    /// <summary>
    /// 把调用点里的**裸方法名**限定成 `学生代码.xxx`。
    ///
    /// ⚠ 为什么必须这么做（C# 课最绕的一处，改了很多遍才通）：
    ///
    ///   · 库把学员代码放进 `学生代码` 类，而它自己生成的 `入口` 是
    ///     **平级类** ⇒ `入口.Main` 里直接写 `打招呼()` 编译不过：
    ///         error CS0103: 当前上下文中不存在名称 '打招呼'
    ///
    ///   · 而调用点常常是**语句序列**（好几条、甚至 foreach），
    ///     不能给整段机械加前缀 —— 会生成
    ///         () => { 学生代码.foreach (var n in ...) ... }
    ///     ⇒ error CS1001: Identifier expected
    ///
    ///   · 反射也帮不上：临时工程**不引用我们的库**，
    ///     生成出来的代码里根本没有 `CppCheck` 可用。
    ///
    ///   ⇒ 就在**课程侧**做：把调用点里出现的、**学员自己定义的方法名**
    ///     逐个替换成 `学生代码.<名字>`。
    ///     语义等价（静态方法加类名限定不改变行为），
    ///     而且对"语句序列"和"单个调用"一视同仁 —— 同一套正则。
    ///
    ///   方法名是从**学员代码**里扫出来的，所以不会误伤
    ///   `Console.WriteLine` 这类库方法（它们的名字不在学员的定义里）。
    /// </summary>
    private static string 限定(string 代码, string 调用点)
    {
        var 名字们 = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(
                     代码, @"\\bstatic\\s+[\\w<>\\[\\]\\?,\\s]+?\\s+(\\w+)\\s*\\("))
            名字们.Add(m.Groups[1].Value);
        string 出 = 调用点;
        foreach (var 名 in 名字们)
            出 = System.Text.RegularExpressions.Regex.Replace(
                出, @"(?<![\\w.])" + System.Text.RegularExpressions.Regex.Escape(名) + @"\\s*\\(",
                "学生代码." + 名 + "(");
        return 出;
    }"""

S = os.path.join(LC, "csharp", "cs-lessons", "Lessons")
n = 0
for f in sorted(os.listdir(S)):
    if not f.endswith(".cs"):
        continue
    Q = os.path.join(S, f)
    u = io.open(Q, encoding="utf-8").read()
    if 旧 in u:
        io.open(Q, "w", encoding="utf-8", newline="").write(u.replace(旧, 新, 1))
        n += 1
    else:
        print("  ! %s: 锚点未找到" % f)
print("  (2) %d 课的 调用 辅助已改（课程侧限定类名）" % n)
