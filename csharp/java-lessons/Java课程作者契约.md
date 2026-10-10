# Java 课程作者契约

这份文档是给**并行写 Java 课的作者**看的。照着做，你和别人产出的课程才能拼到一起。

**先读参考实现**：`csharp/java-lessons/Lessons/第01课_输出.cs` —— 那是已经双向验证过的模板。

## 你要交付什么

一个文件：`csharp/java-lessons/Lessons/第NN课_主题.cs`

**只碰这一个文件。** 不要改任何其它文件（别人正在同时写别的课）。

## 骨架

```csharp
// 第 NN 课：<主题> —— <一句话说明这一课要教什么>
//
// ══ 这一课要教什么 ══
// <两三句。说清"为什么需要这个知识">
//
// ══ 检查设计说明（给维护者）══
// <核心检查点是哪一条？学生能用什么偷懒办法蒙过去？那条检查怎么防住的？>

using System.Linq;
using LearnCode;
using LearnCode.JavaLessons;

[Java课程("NN", "第 NN 课：<标题>",
    "1. <要求一>\n" +
    "2. <要求二>")]
public static class 第NN课
{
    public static string 模板 => """
// 第 NN 课：<标题>
//
// 要做：
//   ...
//
// 写完后运行：dotnet run --project csharp/java-lessons 检查 NN

// ── 在下面写你的代码 ──

""";

    public static string 参考答案 => """
// 必须**真的能通过所有检查**的代码
""";

    [Java检查说明("<一句给学生看的话>")]
    public static JavaCheckResult 检查_xxxx(string 代码)
    {
        // ...
    }
}
```

## 硬性规则

1. **必须**有 `[Java课程(...)]` 特性，否则运行器发现不了你这一课。
2. 检查方法名必须以 `检查_` 开头，签名固定
   `public static JavaCheckResult 检查_xxx(string 代码)`。
3. **每条检查都要加 `[Java检查说明("...")]`** —— 那是给学生看的，函数名不是。
4. **至少要有一条"核心检查点"** —— 防住"能输出对但没学到东西"的偷懒解法。
5. **`参考答案` 必须真的能通过你写的所有检查**（自己跑一遍验证）。
6. ⚠ **公共类名必须是 `Main`** —— Java 要求公共类名和文件名一致，而 `JavaRunner` 固定用 `Main.java`。

## 可用的工具（都在 `JavaCheck` 里）

```csharp
var run = JavaCheck.编译并运行(学生代码);                  // 自动包上 class Main + main
var run = JavaCheck.编译并运行(学生代码, 骨架, 调用点);
var run = JavaCheck.直接运行(学生代码);                    // 学生自己写完整类时用

JavaCheck.逐个用例(学生代码, new JavaCheck.用例("说明","调用表达式","期望输出"), ...)
JavaCheck.输出逐行(学生代码, "第一行", "第二行")
JavaCheck.输出包含(学生代码, "片段")
JavaCheck.能编译(学生代码)
JavaCheck.编译失败说明(run)      // 编译失败的标准提示（含中文报错翻译）

JavaCheckResult.过("给学生看的说明")
JavaCheckResult.不过("给学生看的说明")
```

**`run` 上有什么**：`run.编译通过`、`run.行`、`run.全部输出`、`run.编译信息`、`run.Raw.CompileFailed`、`run.Raw.Error`

## ⚠ 已验证的 Java 特有坑（务必利用，别重踩）

### 一、`static` 是第一大困惑

`main` 是 static 的，所以它**不能直接调用非 static 的方法**。
学生写 `int 加倍(int n)` 然后从 main 里调 → 编译错误。

**但注意（同事实测）**：JDK 25 **放宽了 main 的写法** ——
`public void main(...)`、`static void main(...)`、`public static void main()` 都能跑。
所以"忘了 static"这类错误**可能不在这台机器上暴露**。
课程里该讲清楚，并且可以用 `ExplainJava.MainSignatureNote(代码)` 提醒（标准写法返回空串）。

### 二、字符串比较必须用 `.equals()`

```java
String a = "hi";
String b = new String("hi");
a == b        // false！比的是「是不是同一个对象」
a.equals(b)   // true
```

这是 Java 最著名的坑。**核心检查点用这个**。

### 三、`Integer` 缓存导致 `==` 行为随数值变

```java
Integer a = 127, b = 127;   a == b   // true（-128~127 有缓存）
Integer c = 128, d = 128;   c == d   // false！
```

### 四、数组 vs 字符串的长度写法不同

`数组.length`（属性，无括号） vs `字符串.length()`（方法，要括号）。
学员写错很常见。

### 五、整数除法静默截断

`7 / 2` 得 3，**编译不报错、运行不警告** —— 是静默的语义陷阱。

### 六、其他

- 局部变量未初始化 → Java **直接编译报错**（这点比 C++ 好，C++ 是"碰巧算对"）
- `double` 赋给 `int` → Java **不允许隐式窄化**，必须显式 `(int)`，而且是**截断**不是四舍五入
- `ArrayIndexOutOfBoundsException` → Java **会检查下标**（比 C++ 安全）
- `NullPointerException` → Java 头号杀手
- `NumberFormatException` → `Integer.parseInt("abc")` 时抛，Java 没有 `TryParse` 那样的等价物

## 说明文字的要求（课程质量的关键）

失败提示必须让学生知道**下一步做什么**。反面例子：

> ✗ 输出不对。

正面例子：

> ✗ 期望 6，实际 5。
>      你的循环应该是 `i <= n` 而不是 `i < n` —— 想想 n=3 时要加几个数。

**结构**：发生了什么 → 你写的是什么 → 应该怎么写 → （可选）顺便记住什么。

## 怎么验证你的课（**必做**）

```bash
dotnet build csharp/java-lessons/JavaLessons.csproj
dotnet run --project csharp/java-lessons              # 确认你那一课被发现了
dotnet run --project csharp/java-lessons 检查 NN       # 生成答题文件
# 把参考答案写进 java-answers/NN_xxx.java，再跑检查 → 必须全过
```

然后做**反面测试**：写一个"看起来对但偷懒/踩了那个典型坑"的解法，
确认你的核心检查点抓住了它。**把实际输出贴进最终回复。**

## 环境

- `javac` / `java` 都在 PATH（openjdk 25.0.3）
- .NET SDK 8.0
- PowerShell 是 5.1，**不支持 `&&`**
- 中文标识符可用（已验证），课程里用中文是有意的
- 每次检查起一次 javac，约 1-2 秒

## 课程清单

| 编号 | 主题 | 状态 |
|---|---|---|
| 01 | 输出与变量 | ✅ 已完成（模板，参考它） |
| 02 | 输入与计算 | 待写 |
| 03 | 条件判断 | 待写 |
| 04 | 循环 | 待写 |
| 05 | 函数与 static | 待写 |
| 06 | 数组与集合 | 待写 |
| 07 | 字符串 | 待写 |
| 08 | 类与对象 | 待写 |
