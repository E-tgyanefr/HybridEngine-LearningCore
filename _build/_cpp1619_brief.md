# 16~25 课作者须知（内部草稿，不是交付物）

本文件是给并行写课的助手看的。**必读**，照着做。

工程根目录：`D:\Users\etgya\Desktop\Project\引擎\LearnCode`

## 0. 先读这三份（按顺序）

1. `csharp\cpp-lessons\课程作者契约.md` —— 硬性格式
2. `csharp\cpp-lessons\Lessons\第07课_字符串.cs` —— **模板形状的标准范本**（第一行带 `// 第 07 课：…` 的头部注释、分节线、⚠ 坑、轮到你了、怎么自己测）
3. `csharp\cpp-lessons\Lessons\第11课_递归.cs` —— 第二范本
4. `csharp\HybridEngineLight\CppLesson.cs` —— `CppCheck` / `CheckResult` / `CppRun` 的真实签名

## 1. 你只能改这两个文件

- `csharp\cpp-lessons\Lessons\第NN课_xxx.cs`
- 别的一个字都不许改（别的作者在同时写别的课）。**不要**改 `_build\` 下的脚本。

## 2. 绝对不要自己跑构建

**不要执行 `dotnet build` / `dotnet run` / `python _build\_verify_compiled.py`。**
多个人同时跑会抢同一份 `obj\` 目录，互相把编译搞坏。检查由主作者串行统一跑。
你可以（也鼓励）单独用 `g++` 跑一小段 C++ 来确认事实，但**别碰 dotnet，别碰工程的 .cpp 答题文件**。

## 3. 硬性格式（错一条整课作废）

1. 文件**第一行**必须是 `// 第 NN 课：<标题>`（半角冒号）。
2. 必须有 `[课程("NN", "第 NN 课：<标题>", "1. …\n2. …\n3. …")]`。
3. 检查方法名必须以 `检查_` 开头，签名**固定**
   `public static CheckResult 检查_xxx(string 代码)`。
4. **每条**检查都要有 `[检查说明("给学生看的一句话")]`。
5. 至少一条**核心检查点**（标 ★），防住"输出对但没学到东西"的偷懒解法。
6. `参考答案` 必须真的能通过**这一课的所有检查**（主作者会逐课跑）。
7. `模板 => """…"""` 必须照第 07/11 课的形状：
   - 第一行 `// 第 NN 课：<标题>`
   - `怎么读这一课` → `这一课你会学到` → `需要先会`（写"第 MM 课：xxx"）
   - `先介绍：…` → 若干"速查表式"小节（`一、` `二、` `三、`）
   - `⚠ 这一课唯一要记住的坑：…` + **配一段 g++ 真实报错原文**（下面给了几条实测的）
   - `这一课不碰什么` → `轮到你了 —— 在下面写` → `怎么自己测` → `── 在下面写 ──` + 可写的空骨架
   - 分节线用 `// ─────────────────────────────────────────────────────`（53 个半角 `─`）
   - 「轮到你了」下面的条目必须是 `// 1. xxx`（**两个斜杠 + 空格 + 数字 + 点**）
8. **正文里不许出现英文双引号**（`"`）。中文引号一律用 `「」`。
   - 例外：C# 源码里的字符串字面量本身（`[课程(...)]`、`[检查说明(...)]`、
     C++ 代码字面量如 `"\\n"`）—— 那是代码，不是正文。
   - 模板/答案里的 C++ 代码要用双引号的地方，尽量用 `'\\n'` 之类的**单引号字符**绕开。

## 4. 两个已踩过的坑（契约里写了，这里再强调）

- **学生写完整程序（自带 main）→ 用 `CppCheck.直接运行`**，不要用 `编译并运行`（会 `redefinition of 'int main()'`）。
- **不要用函数外的探针去访问学生 main 里的局部变量** —— 看不见。
  验"变量真的参与了计算"要用**文本替换 + 重编译**（`第01课_输出.cs` 的 `改写初值`），
  或者干脆**喂两组不同的调用点/输入文件**，看输出会不会跟着变。

## 5. 本机实测事实（g++ 16.1.0，-std=c++17 -O1 -Wall，不用再猜）

| 事实 | 实测值 |
|---|---|
| 冒泡排序 20000 个 int（三重循环手写） | 约 244 ms |
| 冒泡排序 100000 个 int | 约 6~10 s（**不要放进检查**，太慢） |
| `std::sort` 对 20 条记录（分数 10/20/0 循环），同分顺序被**打乱** | 输出 `11 20 17 14 8 5 2 …` |
| `std::stable_sort` 同样数据，同分**保持原顺序** | 输出 `2 5 8 11 14 17 20 …` |
| 试除法判 999999937（大质数）用 `d*d<=n` | 0 ms |
| 试除法判 999999937 用 `d<n`（朴素） | 2527 ms |
| 埃氏筛到 1e6 | 2 ms，π(1e6)=78498 |
| 试除法列 200000 以内素数 | 11 ms |
| 朴素试除到 100000 | 669 ms |
| 缺 `#include <algorithm>` 调 `std::sort` 的报错原文 | `error: 'sort' is not a member of 'std'; did you mean 'qsort'?` |
| 二维数组参数不写列数的报错原文 | `error: declaration of '\U0000683c' as multidimensional array must have bounds for all dimensions except the first` |
| 忘 `#include <fstream>` 的报错原文 | `error: variable 'std::ofstream \U000051fa' has initializer but incomplete type`，并附一句 `note: 'std::ofstream' is defined in header '<fstream>'; this is probably fixable by adding '#include <fstream>'` |
| 忘 `#include <cmath>` 用 `sqrt` 的报错原文 | `error: 'sqrt' was not declared in this scope` |
| ⚠ `std::sort` 传 `>=` 而不是 `>`（不满足严格弱序） | **编译通过、不报警告**，小数据下还可能"看起来对"，数据一大直接崩或结果乱 |
| `CppRunner` 编译参数 | `-std=c++17 -O1 -Wall`；运行超时 **60 秒**判"程序运行超时" |
| 中文标识符 | g++ 下可用，但**报错里会被转义**（`\U000053cc`）⇒ 报错识别只匹配 ASCII 片段 |

## 6. 检查函数里的实用套路

```csharp
// 「标记|值」输出 + 取值，学生自己加打印也不会冲散对答案
private static string 取值(CppRun run, string 标记) {
    foreach (var 行 in run.行) {
        int i = 行.IndexOf('|');
        if (i > 0 && 行.Substring(0, i).Trim() == 标记) return 行.Substring(i + 1).Trim();
    }
    return null;
}
// 编译失败/崩了的统一前置判断
if (run.Raw.CompileFailed) return CppCheck.编译失败说明(run);
if (run.Raw.Error != null) return CheckResult.不过("程序没跑完就退出了：" + run.Raw.Error + …);
// 只看编译信息（不看输出）—— 专门抓"未初始化变量"这种 UB
var run = CppCheck.编译并运行(学生代码, null, 调用点);
if (run.编译通过 && run.编译信息.Contains("uninitialized")) return CheckResult.不过(…);
// 只看源码文本（抓"没用真的算法"）
private static string 去注释(string s) { /* 照抄第 11/13 课的 去掉注释 */ }
```

缓存：同一课里多条检查会对同一份代码各编译一次。照第 06/07 课的写法加
`private static string 上次的代码; private static CppRun 上次的运行;` + `跑一次(代码)`
—— 一次检查就能省下好几秒。

## 7. 报错提示的质量要求

失败提示必须让学生知道**下一步做什么**。结构：**发生了什么 → 你写的是什么 →
应该怎么写 →（可选）顺便记住什么**。

✗ 「输出不对。」
✓ 「期望输出 6，实际输出 5。你的循环应该是 `i <= n` 而不是 `i < n` —— 想想 n=3 时要加几个数。」

## 8. 交付报告

写完后回一段简短报告：两个文件路径 + 行数 + 每课的核心检查点是哪一条 +
你打算怎么用偷懒解法验它（**不要跑 dotnet**）+ 遇到的真问题。
