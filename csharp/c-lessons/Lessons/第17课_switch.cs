using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("17", "第 17 课：switch —— 一个值对上好几个可能",
    """
写函数 季节(月)（用 switch 加贯穿实现）：3-5 月返回 "春"、6-8 月返回 "夏"、9-11 月返回 "秋"、12/1/2 月返回 "冬"、其它返回 "不认识"
""")]
public static class 第17课
{
    public static string 模板 => """
// 第 17 课：switch —— 一个值对上好几个可能
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    switch：一个值对上好几个可能
//
//  需要先会
//    第 06 课：if / else
//
// ─────────────────────────────────────────────────────
//  先介绍：和 if 是两种不同的东西
// ─────────────────────────────────────────────────────
//
//     if      —— **判断条件**（可以是很复杂的表达式）
//     switch  —— **把同一个值和一堆候选值比相等**
//
//         switch (月) {
//             case 3:
//                 ...
//                 break;
//             case 4:
//             case 5:
//                 ...
//                 break;
//             default:
//                 ...
//         }
//
// 用哪个？一条实用判据：
//
//     「等于…等于…等于…」        → switch 更清楚
//     「大于、小于、并且、或者」   → 只能用 if
//
// ⚠ switch 里**只能比整数和字符**，不能比字符串、不能比小数。
//   （`case "你好":` 编译不过 —— 字符串比较要用 strcmp，第 10 课讲的那件事。）
//
//  ⚠⚠ 这一课唯一要记住的坑：break
//
// **switch 不会自己停下来。** 一个 case 做完了，它会**继续往下做下一个 case**。
// 这叫「贯穿」（fallthrough），是 C 里最坑人的设计之一。
//
//     switch (2) {
//         case 1: printf("一\n");        // 不执行
//         case 2: printf("二\n");        // 执行 —— 从这里进去
//         case 3: printf("三\n");        // **也执行！** 因为上面没 break
//     }
//
//     实际输出：二、三          ← 不是你以为的"只有二"
//
// ⇒ **每个 case 末尾几乎都要写 break;**，忘了它就等于让程序"漏"下去。
//
// ⚠ 但贯穿**有时候是故意的** —— 而且这是这一课要你用的写法。
//   想表达"3、4、5 都算春天"，可以叠着写：
//
//     case 3:
//     case 4:
//     case 5:
//         return "春";          ← 三个标签共用同一段代码
//
//   这比写逻辑或运算符（月 == 3 或 月 == 4 或 月 == 5）短，也不容易漏掉一个。
//   ⇒ 所以贯穿既是坑、也是工具 —— **关键是你要知道自己有没有用它。**
//
// ⚠ 别忘 default：所有 case 都不匹配时走它。
//   没有 default 又不匹配 —— 什么都不做，而你要自己保证"什么都不做"是对的。
//
// ─────────────────────────────────────────────────────
//  用法与例子
// ─────────────────────────────────────────────────────
//
//  ① 基本形状：`switch (一个整数表达式)` + 一堆 `case 常量:`。
//
//      switch (月) {
//          case 1:
//              printf("一月\n");
//              break;                  /* ⚠ 停下 */
//          case 2:
//              printf("二月\n");
//              break;
//          default:
//              printf("不认识\n");
//              break;
//      }
//
//  ② case 后面只能跟**编译期常量**：整数、字符字面量。
//
//      case 3:        ✅ 整数
//      case 'A':      ✅ 字符（字符本质上是整数编码）
//      case 变量:     ❌ 变量不是常量
//      case 3.5:      ❌ 小数不行（坑 3）
//      case "春":     ❌ 字符串不行（坑 2）
//
//  ③ **故意贯穿**：几个标签叠在一起，共用同一段代码。
//     这是这一课要你用的写法：
//
//      case 3:
//      case 4:
//      case 5:
//          return "春";
//
//      ⚠ 前两个 case 后面**什么都不写**（不写 break）—— 这是"故意的"。
//        但最后一个（case 5）处理完必须 break 或 return，否则会漏到夏天去。
//
//  ④ 在函数里，`return` 本身就能结束 switch（不需要再补 break）：
//
//      char* 季节(int 月) {
//          switch (月) {
//              case 3: case 4: case 5:
//                  return "春";        /* return 已经终止了整个函数 */
//              ...
//          }
//      }
//
//  一个完整的、能跑的贯穿例子：
//
//      #include <stdio.h>
//
//      int main(void) {
//          int 月 = 2;
//          switch (月) {
//              case 1: printf("一\n");
//              case 2: printf("二\n");
//              case 3: printf("三\n");
//          }
//          return 0;
//      }
//
//  真实输出（⚠ 注意是两行）：
//
//      二
//      三
//
//  ⚠ 这就是贯穿。把 `case 2:` 那行末尾补上 `break;`，输出才会只剩「二」。
//    这个 10 秒钟的实验，比看十遍解释都管用 —— **建议你真的跑一遍。**
//
// ─────────────────────────────────────────────────────
//  常见坑
// ─────────────────────────────────────────────────────
//
//  ⚠ 坑 1：忘了 break —— 贯穿到你没想要的地方
//
//      错误写法：
//          switch (等级) {
//              case 3: printf("优\n");
//              case 2: printf("良\n");
//              case 1: printf("及格\n");
//          }
//
//      你会看到什么：
//          ⚠ gcc 通常**一声不吭**（除非打开 -Wimplicit-fallthrough）。
//          等级 3 的时候，三行**全都**打印出来：优、良、及格。
//
//      为什么：switch 的真实语义是"**跳到**匹配的那个标签，然后一直往下执行"，
//      不是"只执行那一段"。`break` 才是真正的"到此为止"。
//      ⇒ 规矩：**每个 case 末尾都写 break**，
//        除非你**故意**贯穿 —— 而故意的时候，最好写一行注释说明：
//            case 3:                 /* ⚠ 故意贯穿，3/4/5 同一段 */
//            case 4:
//            case 5:
//        这样读代码的人（包括三个月后的你）才不会以为是漏写。
//
//  ⚠ 坑 2：拿字符串当 case
//
//      错误写法：
//          char* 季节(int 月) {
//              switch (月) {
//                  case "春":              /* ⚠ 字符串 */
//                      return "春";
//              }
//              return "?";
//          }
//
//      你会看到什么（gcc 原文）：
//
//          main.c:3:9: error: pointers are not permitted as case values
//              3 │         case "春":
//                │         ^~~~
//
//      为什么：`"春"` 在 C 里是一个**指针**（字符串第一个字符的地址），
//      而 case 标签要求"编译期就能算出来的整数"。
//      两个字符串即使内容一样，地址也不同 —— 所以"用字符串做 case"这件事
//      在 C 里从根上就不成立。
//      ⇒ 要比字符串，只能用 `strcmp`（第 10 课）配 if：
//          if (strcmp(名字, "春") == 0) { ... }
//
//  ⚠ 坑 3：case 后面写小数，或者忘了 default
//
//      错误写法一（小数）：
//          switch (月) {
//              case 3.5:               /* ⚠ 小数 */
//                  break;
//          }
//
//      你会看到什么（gcc 原文）：
//
//          main.c:5:9: error: case label does not reduce to an integer constant
//              5 │         case 3.5:
//                │         ^~~~
//
//      为什么：switch 在 C 里**只对整数做相等比较**，小数压根不参与。
//      "does not reduce to an integer constant" 的意思就是
//      "这个名字我没法在编译期算成一个整数"。
//      ⇒ 小数的分支只能用 if。
//
//      错误写法二（没有 default）：
//          char* 季节(int 月) {
//              switch (月) {
//                  case 3: return "春";
//                  ...
//                  /* ⚠ 没有 default */
//              }
//          }
//
//      你会看到什么：
//          gcc 会警告：warning: control reaches end of non-void function [-Wreturn-type]
//          运行时 `季节(13)` 返回一个**随机字符串地址** —— 打印出来是乱码，
//          甚至直接崩。
//
//      为什么：13 不匹配任何 case，switch 整段被跳过，
//      于是函数执行到 `}` 也没 return。而"非 void 函数走到末尾"返回的是残留值（第 08 课）。
//      ⇒ **switch 里那个 default 就是兜底**。它既是逻辑上的"其它情况"，
//        也是编译器眼里的"一定有条路能返回"。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   写函数：  char* 季节(int 月)
//
//      3 4 5   → "春"
//      6 7 8   → "夏"
//      9 10 11 → "秋"
//      12 1 2  → "冬"
//      其它    → "不认识"
//
//   ⚠ 用 switch，而且**故意用贯穿**把 3/4/5 叠在一起 —— 这一课练的就是这个。
//   ⚠ 返回字符串要用双引号：return "春";（不是 '春'，单引号是单个字符）
//
// 提示：不用写 main。
//
// ⚠ 期望结果：
//
//      季节(3)  ⇒ 春      季节(5)  ⇒ 春       ← 5 是春天的最后一月
//      季节(6)  ⇒ 夏      季节(8)  ⇒ 夏
//      季节(9)  ⇒ 秋      季节(11) ⇒ 秋
//      季节(12) ⇒ 冬      季节(2)  ⇒ 冬
//      季节(13) ⇒ 不认识  季节(0)  ⇒ 不认识   ← 靠 default
//
// ⚠ 检查会看你源码里有没有 `switch` —— 这一课就是练它，用 if 串不算过。
//   另外别忘了返回类型是 `char*`（字符串的地址），不是 char。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//   点界面上的「检查」。
//   命令行：dotnet run --project csharp/c-lessons 检查 17
//   看这一课要干什么：dotnet run --project csharp/c-lessons 17
//   看答案：dotnet run --project csharp/c-lessons 答案 17
//
// ── 在下面写 ──
""";

    public static string 参考答案 => """
char* 季节(int 月) {
    switch (月) {
        /* ⚠ 这三个 case 故意叠在一起 —— 贯穿用在这里是对的：
              它们共用同一段代码，比写 if (月==3 || 月==4 || 月==5) 更清楚。
              ⚠ 注意最后那个 case 5 后面的代码，末尾一定要 break。 */
        case 3:
        case 4:
        case 5:
            return "春";
        case 6:
        case 7:
        case 8:
            return "夏";
        case 9:
        case 10:
        case 11:
            return "秋";
        case 12:
        case 1:
        case 2:
            return "冬";
        default:
            /* 其它月份（比如 13）走到这里。
               ⚠ 别省掉 default：没有它、又不匹配时，函数会掉到末尾，
                 而"非 void 函数掉到末尾"返回的是**随机值**。 */
            return "不认识";
    }
}
""";


    [检查说明("写了 季节 函数")]
    public static CheckResult 检查_1(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%d\n", 季节(3));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过)
        {
            if (run.编译信息.Contains("季节"))
                return CheckResult.不过("没找到「季节」这个函数 —— 或者签名和题目要求不一致。");
            return CppCheck.编译失败说明(run);
        }
        return CheckResult.过("「季节」可以正常调用");
    }

    [检查说明("四季都判对了")]
    public static CheckResult 检查_2(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%s\n", 季节(3));
    printf("%s\n", 季节(6));
    printf("%s\n", 季节(9));
    printf("%s\n", 季节(12));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "春",
            "夏",
            "秋",
            "冬"
        };
        string[] 输入 = {
            "3",
            "6",
            "9",
            "12"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
                return CheckResult.不过(
                    $"输入 [{(i < 输入.Length ? 输入[i] : "")}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 返回的是 C 字符串（双引号），不是字符（单引号）。");
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("边界都对（5/8/11/2 是各季的最后一月）")]
    public static CheckResult 检查_3(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%s\n", 季节(5));
    printf("%s\n", 季节(8));
    printf("%s\n", 季节(11));
    printf("%s\n", 季节(2));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "春",
            "夏",
            "秋",
            "冬"
        };
        string[] 输入 = {
            "5",
            "8",
            "11",
            "2"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
                return CheckResult.不过(
                    $"输入 [{(i < 输入.Length ? 输入[i] : "")}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 返回的是 C 字符串（双引号），不是字符（单引号）。");
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("不认识的月份返回「不认识」（说明 default 没漏）")]
    public static CheckResult 检查_4(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    printf("%s\n", 季节(13));
    printf("%s\n", 季节(0));
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "不认识",
            "不认识"
        };
        string[] 输入 = {
            "13",
            "0"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
                return CheckResult.不过(
                    $"输入 [{(i < 输入.Length ? 输入[i] : "")}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 返回的是 C 字符串（双引号），不是字符（单引号）。");
        }
        return CheckResult.过("每一组都对");
    }

    [检查说明("用了 switch（这一课就是练它）")]
    public static CheckResult 检查_5(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("switch"))
            return CheckResult.不过("没找到「switch」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    private static string 去掉注释(string s)
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
