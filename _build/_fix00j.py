"""给 Java 00 课补 `常见坑`，并修掉重复的分隔线。"""
import io
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
P = (r"D:\Users\etgya\Desktop\Project\引擎\LearnCode\csharp"
     r"\java-lessons\Lessons\第00课_跑通第一个程序.cs")
分隔 = "// ─────────────────────────────────────────────────────"
t = io.open(P, encoding="utf-8").read()
m = re.search(r'(模板 => """)(.*?)(""";)', t, re.S)
文 = m.group(2)

# ① 修重复的分隔线（两行一样的挨着）
文 = 文.replace(分隔 + "\n" + 分隔 + "\n", 分隔 + "\n")

# ② 补 `常见坑`：插在「四、这一课唯一要练的手艺」之前
if "常见坑" not in 文:
    锚 = "//  四、这一课唯一要练的手艺"
    i = 文.find(锚)
    assert i > 0, "锚点没找到"
    j = 文.rfind("\n", 0, i) + 1
    块 = (分隔 + "\n//  常见坑\n" + 分隔 + "\n//\n"
          "// ⚠ 坑 1：装了 JRE 而不是 JDK\n"
          "//\n"
          "//     winget install Microsoft.OpenJDK.21     ← 这个是对的做法\n"
          "//     而 `Java SE Runtime` 之类的包**没有编译器**\n"
          "//\n"
          "// 你会看到：敲 `javac` 说\n"
          "//     'javac' 不是内部或外部命令，也不是可运行的程序\n"
          "// 为什么：`javac`（编译器）在 **JDK** 里；\n"
          "//   JRE / Runtime 只包含 `java`（运行已编译程序的那个）。\n"
          "// ⇒ 用 `javac -version` 验证 —— 能出版本号才说明装对了。\n"
          "//   （只看 `java -version` 不够：JRE 也有 `java`。）\n"
          "//\n"
          "// ⚠ 坑 2：装完不重开窗口\n"
          "//\n"
          "// 你会看到：还是"不是内部或外部命令"，而安装时明明提示成功了。\n"
          "// 为什么：安装程序改的是 **PATH 环境变量**，\n"
          "//   而已经打开的窗口用的是**打开那一刻**的 PATH 副本。\n"
          "// ⇒ 关掉重开一个。⚠ 这条在 C 线（装 gcc）和 C# 线（装 .NET SDK）\n"
          "//   **一模一样** —— 凡是"装完说找不到命令"，先想这一条。\n"
          "//\n"
          "// ⚠ 坑 3：以为"没输出就是没跑"\n"
          "//\n"
          "// 你会看到：点了检查，几秒钟没反应。\n"
          "// 为什么：**第一次**检查要编译整个课程程序，会慢一两秒，\n"
          "//   而且它不打印"正在编译"之类的东西。\n"
          "// ⇒ 等一下就好。真卡住（超过十几秒）再看是不是杀毒软件在拦。\n"
          "//\n")
    文 = 文[:j] + 块 + 文[j:]

t = t[:m.start(2)] + 文 + t[m.end(2):]
io.open(P, "w", encoding="utf-8", newline="").write(t)

要 = ("先介绍", "用法与例子", "常见坑", "轮到你了", "怎么自己测")
n = len([x for x in 文.split("\n") if x.strip()])
print("    Java 00 课：%d 行，关键词 %d/5，缺：%s"
      % (n, sum(1 for w in 要 if w in 文),
         " ".join(w for w in 要 if w not in 文) or "（无）"))
