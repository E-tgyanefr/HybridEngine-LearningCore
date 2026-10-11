using System;
using System.Linq;
using HybridEngineLight;
using HybridEngineLight.CLessons;

[课程("15", "第 15 课：指针与数组 —— 数组名是什么",
    """
写函数 void 全部加一(int *数们, int 个数)：把数组每个元素加 1
""")]
public static class 第15课
{
    public static string 模板 => """
// 第 15 课：指针与数组 —— 数组名是什么
//
// 先介绍：数组名其实是一个地址
//
//     int 数们[3] = {1, 2, 3};
//
// `数们` 这个名字，作为表达式的值，等于 **&数们[0]**（第一个元素的地址）。
// 所以下面两种写法**完全等价**：
//
//     数们[i]
//     *(数们 + i)
//
// 搞懂这一点，就明白了三件事：
//
//   ① 为什么数组作为参数传给函数时**会丢掉长度** ——
//      因为传进去的只是"第一个元素的地址"，末尾没有"到此为止"的信息。
//      ⇒ 所以第 09 课那个函数要另外传 `个数`。
//
//   ② 为什么函数里**能改到原数组** ——
//      因为它拿到的是地址，`数们[i] = x` 写的就是原来那块内存。
//      （对比第 11 课：普通 int 参数改不到外面，数组能改到 —— 就在这里。）
//
//   ③ 为什么 `数们[i]` 和 `*(数们 + i)` 一样：
//      `数们 + i` 是"往后数 i 个格子"的地址，`*` 再把里面的值取出来。
//
// ⚠ 参数写成 `int *数们` 或 `int 数们[]` **完全一样** ——
//   编译器把它们当同一件事。看到两种写法都别慌。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//   写函数：  void 全部加一(int *数们, int 个数)
//   把数组里每个元素都加 1（要让**调用者**的数组真的变）
//
// 提示：不用写 main。

// ── 在下面写你的函数 ──

""";

    public static string 参考答案 => """
void 全部加一(int *数们, int 个数) {
    /* ⚠ 这里改的是**原数组** —— 因为拿到的是地址，不是拷贝 */
    for (int i = 0; i < 个数; i++) {
        数们[i] += 1;              /* 也可以写 *(数们 + i) += 1; */
    }
}
""";


    [检查说明("写了 全部加一 函数")]
    public static CheckResult 检查_1(string 代码)
    {
        string 去注释 = 去掉注释(代码);
        if (!去注释.Contains("全部加一"))
            return CheckResult.不过("没找到「全部加一」—— 这一课要求用到它。");
        return CheckResult.过();
    }

    [检查说明("能编译")]
    public static CheckResult 检查_2(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    int __a[] = {1, 2, 3};
    全部加一(__a, 3);
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        return CheckResult.过();
    }

    [检查说明("真的改到了调用者的数组")]
    public static CheckResult 检查_3(string 代码)
    {
        var 骨架 = """
#include <stdio.h>

/*学生代码*/

int main(void) {
    int __a0[] = {1, 2, 3};
    全部加一(__a0, 3);
    for (int __i = 0; __i < 3; __i++) printf("%d,", __a0[__i]);
    printf("\n");
    int __a1[] = {-5, 0, 5};
    全部加一(__a1, 3);
    for (int __i = 0; __i < 3; __i++) printf("%d,", __a1[__i]);
    printf("\n");
    int __a2[] = {9};
    全部加一(__a2, 1);
    for (int __i = 0; __i < 1; __i++) printf("%d,", __a2[__i]);
    printf("\n");
    return 0;
}
""";
        var run = CppCheck.编译并运行(代码, 骨架);
        if (!run.编译通过) return CppCheck.编译失败说明(run);
        string[] 期望 = {
            "2,3,4",
            "-4,1,6",
            "10"
        };
        string[] 输入 = {
            "1,2,3",
            "-5,0,5",
            "9"
        };
        for (int i = 0; i < 期望.Length; i++)
        {
            // ⚠ 骨架打印的是 "%d,"（带尾逗号）⇒ 比较前去尾逗号
            string 实际行 = i < run.行.Count
                ? run.行[i].Trim().TrimEnd(',') : "";
            if (i >= run.行.Count || 实际行 != 期望[i])
            {
                string 这是 = i < 输入.Length ? 输入[i] : "";
                return CheckResult.不过(
                    $"输入 [{这是}] 应当是 [{期望[i]}]，" +
                    $"实际是 [{(i < run.行.Count ? 实际行 : "（没输出）")}]。\n" +
                    "      ⚠ 函数拿到的是地址 ⇒ 改的就是原数组（或返回值要对）。");
            }
        }
        return CheckResult.过("每一组都对");
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
