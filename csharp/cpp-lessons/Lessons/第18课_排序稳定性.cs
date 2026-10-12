// 第 18 课：排序稳定性 —— 分数一样的时候，谁排在前面
//
// ══ 这一课要教什么 ══
// 排序的结果只有一种吗？—— **不是**。分数相同的两个人，谁在前谁在后，
// 不同的排序做法给出的答案可能不一样。这就是「稳定性」：
//
//     稳定：分数相同的，保持它们**原来**的先后顺序
//     不稳定：分数相同的，谁前谁后**不保证**
//
// 为什么学生必须知道：成绩表按分数排完之后，往往还要按"同分看语文"之类的
// 规则处理；如果排序这一步把原来的顺序打乱了，后面就再也补不回来。
//
// ══ 检查设计说明（给维护者）══
// 核心检查点：**`按分排序` 必须真的稳定，`按分排序_不稳定` 必须真的用 std::sort。**
//   偷懒解法有两种：
//     ① 两个函数都写成 stable_sort —— 输出"看起来对"，但学生没看见区别。
//        防法：文本检查两个函数体里各自用的是什么。
//     ② 干脆不排，原样返回 —— 输入数据本来就"分数高的在前"？不是，
//        调用点故意把数据打乱（甲 90、乙 80、丙 90…），不排就一定错。
//   另一条：比较函数写成 `>=`（不满足**严格弱序**）——
//   本机实测 g++ **既不报错也不警告**，小数据下还可能"看起来对"，
//   所以专门用一条检查盯 `return … >= …` 这种写法。
//
// ⚠ 给维护者：取输出用「标记|内容|闭标记」的写法，靠竖线切段（见 `取一段`）。

using System.Collections.Generic;
using HybridEngineLight;
using HybridEngineLight.CppLessons;

[课程("18", "第 18 课：排序稳定性 —— 分数一样的时候谁在前面",
    "1. 写 int 总分(std::vector<成绩> 表)，把所有人的分数加起来\n" +
    "2. 写 void 按分排序(std::vector<成绩>& 表)，按分数**从高到低**排，同分保持原来的先后（用 stable_sort）\n" +
    "3. 写 void 按分排序_不稳定(std::vector<成绩>& 表)，同样从高到低，但用 std::sort —— 让你亲眼看见同分顺序被打乱")]
public static class 第18课
{
    public static string 模板 => """
// 第 18 课：排序稳定性 —— 分数一样的时候，谁排在前面
// ─────────────────────────────────────────────────────
//  怎么读这一课
// ─────────────────────────────────────────────────────
//
//   先从头读一遍讲解，再照着最后的「轮到你了」写。
//   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
//
// 这一课讲一件事：**分数相同的时候，谁排在前面**。
// ─────────────────────────────────────────────────────
//  这一课你会学到
// ─────────────────────────────────────────────────────
//    排序的稳定性，以及 sort 的比较函数怎么写
//
//  需要先会
//    第 13 课：vector 与算法（sort 就是那一课学的）
//    第 12 课：结构体（成绩 里装着名字和分数）
//
// ─────────────────────────────────────────────────────
//  先介绍：同一个分数，两种排法
// ─────────────────────────────────────────────────────
//
// 五个人：甲 90、乙 80、丙 90、丁 80、戊 90（按这个顺序交上来的）。
// 按分数从高到低排，90 分的三个怎么排？80 分的两个怎么排？
//
//     稳定排法   甲 丙 戊 乙 丁      ← 同分的保持原来谁先谁后
//     不稳定排法 戊 丙 甲 丁 乙      ← 同分的顺序被打乱了
//
// 两种都是**正确的**排序结果 —— 因为「分数相同」时它们本来就不分先后。
//
// ─────────────────────────────────────────────────────
//  一、两个函数：sort 和 stable_sort
// ─────────────────────────────────────────────────────
//
//     #include <algorithm>
//
//     std::sort(表.begin(), 表.end(), 比较函数);          快，但**不稳定**
//     std::stable_sort(表.begin(), 表.end(), 比较函数);   同样快，**稳定**
//
// 记法：**要"同分保持原样"就用 stable_sort**。
//
// ─────────────────────────────────────────────────────
//  二、比较函数：告诉排序"谁该排在前面"
// ─────────────────────────────────────────────────────
//
//     std::sort(表.begin(), 表.end(),
//         [](const 成绩& 甲, const 成绩& 乙) {
//             return 甲.分数 > 乙.分数;      ← 大的排前面（从高到低）
//         });
//
// 返回 true 的意思是：**甲 应该排在 乙 前面**。
//   `>` ⇒ 分数大的在前（从高到低）；`<` ⇒ 分数小的在前（从低到高）。
//
// ⚠ 参数用 `const 成绩&`：加 `&` 是引用，免得每次比较都复制一份名字
//   （第 08 课的内容）；加 `const` 是因为比较**不该改动**数据。
//
// ─────────────────────────────────────────────────────
//  三、稳定排序有什么用
// ─────────────────────────────────────────────────────
//
// 常常有这种要求：「先按分数排，同分的按交卷先后」。做法是：
//   **先按交卷先后整理好，再用 stable_sort 按分数排** ——
//   因为稳定，同分的那几个人会保持刚才那个顺序。
// 反过来用 std::sort，这一步就白做了。
//
// ─────────────────────────────────────────────────────
//  ⚠ 这一课唯一要记住的坑：比较函数写成 >=
// ─────────────────────────────────────────────────────
//
//     return 甲.分数 >= 乙.分数;      ✗ 这是本课最阴的一个坑
//
// 你会看到：**编译通过、一个警告都没有**，元素少的时候结果还可能是对的，
//   数据一多就可能顺序错乱、甚至程序直接崩掉。
//
// 为什么：排序算法要求比较函数满足「严格弱序」—— 具体到写法上就是
//   **两个相等的元素，比较函数必须返回 false**（谁也不该排在谁前面）。
//   `>=` 在两者相等时返回 true（"甲该排在乙前面"、"乙该排在甲前面"都成立），
//   自相矛盾，排序的内部逻辑就被绕晕了。
//
// ⇒ 一律用 `>` 或 `<`，**永远不要**在比较函数里写 `>=` 或 `<=`。
//
// 附：忘了 `#include <algorithm>` 时，本机 g++ 16.1.0 的原话是
//
//     error: 'sort' is not a member of 'std'; did you mean 'qsort'?
//
// （`std::stable_sort` 也在同一个头文件里。）
//
// ─────────────────────────────────────────────────────
//  这一课不碰什么
// ─────────────────────────────────────────────────────
//   排序快不快 → 第 19 课。自己手写排序 → 第 17 课。
//
// ─────────────────────────────────────────────────────
//  轮到你了 —— 在下面写
// ─────────────────────────────────────────────────────
//
//  课程已经替你写好了这个结构体（**不要再写一遍**）：
//
//      struct 成绩 { std::string 名字; int 分数; };
//
//  写三个函数（不用写 main、不用写 #include）：
//
//    1. int 总分(std::vector<成绩> 表)
//         把 表 里所有人的分数加起来返回
//
//    2. void 按分排序(std::vector<成绩>& 表)
//         按分数**从高到低**排；**同分的保持原来的先后顺序**（用 stable_sort）
//
//    3. void 按分排序_不稳定(std::vector<成绩>& 表)
//         同样从高到低，但用 std::sort —— 这一条就是让你**亲眼看见**区别
//
//  ⚠ 参数是 `std::vector<成绩>&`（带 `&`）：要**就地**把传进来的表排好，
//    不带 `&` 排的是一份副本，外面一点变化都没有。
//
// ─────────────────────────────────────────────────────
//  怎么自己测
// ─────────────────────────────────────────────────────
//     点界面上的「检查」。卡住了看答案（先自己写一遍）。
//
// ── 在下面写 ──
int 总分(std::vector<成绩> 表)
{
}

void 按分排序(std::vector<成绩>& 表)
{
}

void 按分排序_不稳定(std::vector<成绩>& 表)
{
}
""";

    public static string 参考答案 => """
// 参考答案 —— 排序稳定性
//
// ⚠ 结构体 成绩 由课程骨架提供（struct 成绩 { std::string 名字; int 分数; };）
//   所以这里**不要**再定义一次。

// 1. 总分：范围 for 遍历，累加每个人的分数
int 总分(std::vector<成绩> 表) {
    int 和 = 0;
    for (const 成绩& 人 : 表) {
        和 += 人.分数;
    }
    return 和;
}

// 2. 稳定排序：分数从高到低，同分保持原来的先后
//    比较函数返回 true 的意思是「甲应该排在乙前面」⇒ 用 > （不能用 >=）
void 按分排序(std::vector<成绩>& 表) {
    std::stable_sort(表.begin(), 表.end(),
        [](const 成绩& 甲, const 成绩& 乙) {
            return 甲.分数 > 乙.分数;
        });
}

// 3. 不稳定排序：同样的比较函数，换成 std::sort —— 同分的顺序就不保证了
void 按分排序_不稳定(std::vector<成绩>& 表) {
    std::sort(表.begin(), 表.end(),
        [](const 成绩& 甲, const 成绩& 乙) {
            return 甲.分数 > 乙.分数;
        });
}
""";

    // ══════════════════ 检查 ══════════════════

    [检查说明("三个函数都在，签名也对")]
    public static CheckResult 检查_函数都在(string 代码)
    {
        var 缺 = new List<string>();
        foreach (var (名, 调用) in new[]
        {
            ("总分", "总分(空表)"),
            ("按分排序", "按分排序(空表)"),
            ("按分排序_不稳定", "按分排序_不稳定(空表)"),
        })
        {
            var run = 编译并运行(代码, 骨架, "std::vector<成绩> 空表;\n" + 调用 + ";\n");
            if (!run.编译通过)
            {
                if (run.编译信息.Contains(名) &&
                    (run.编译信息.Contains("was not declared") || run.编译信息.Contains("not declared")))
                    缺.Add("      " + 名);
                else
                    return CppCheck.编译失败说明(run);
            }
        }
        if (缺.Count == 0) return CheckResult.过("三个函数都在，签名也对");

        return CheckResult.不过(
            "有函数没找到（或者签名和题目不一样）：\n" + string.Join("\n", 缺) + "\n" +
            "      这一课要求的三个签名是：\n" +
            "          int  总分(std::vector<成绩> 表)\n" +
            "          void 按分排序(std::vector<成绩>& 表)\n" +
            "          void 按分排序_不稳定(std::vector<成绩>& 表)\n" +
            "      ⚠ 后两个参数带 `&`（引用）—— 要就地改传进来的那张表。\n" +
            "      ⚠ 结构体 成绩 由课程骨架提供，**不要**在代码里再定义一次。");
    }

    [检查说明("总分算对了")]
    public static CheckResult 检查_总分(string 代码)
    {
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停 != null) return CheckResult.不过(停);

        return 比一比(run,
            ("总1|", "430", "总分({甲90, 乙80, 丙90, 丁80, 戊90})"),
            ("总2|", "0", "总分({})　空表求和是 0"));
    }

    [检查说明("★ 按分排序：从高到低，而且同分的保持原来的先后（稳定）")]
    public static CheckResult 检查_稳定(string 代码)
    {
        // ⚠ 核心检查点：同一个分数，稳定排序必须保持输入顺序。
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停 != null) return CheckResult.不过(停);

        return 比一比(run,
            ("稳定1|", "甲 丙 戊 乙 丁", "按分排序　输入顺序是 甲90 乙80 丙90 丁80 戊90 —— 同分的要保持原样"),
            ("稳定2|", "小 中 大", "按分排序　三个 70 分的人，输入顺序是 小 中 大"),
            ("稳定4|", "独", "按分排序　只有一个人"));
    }

    [检查说明("★ 按分排序_不稳定：真的用 std::sort（应该和稳定版的结果不一样）")]
    public static CheckResult 检查_不稳定(string 代码)
    {
        // ⚠ 核心检查点之二：学生很可能把两个函数写成一样，
        //   那他就**没有看见**这一课要讲的区别。
        var run = 跑一次(代码);
        string 停 = 先别急着比(run);
        if (停 != null) return CheckResult.不过(停);

        string 稳定 = 取一段(run, "稳定1|", 0);
        string 不稳 = 取一段(run, "不稳1|", 0);
        if (稳定 == null || 不稳 == null)
            return CheckResult.不过("没能拿到两次排序的结果 —— 函数可能没被调用到，或者程序中途崩了。");

        string 净 = 去掉注释(代码);
        string 稳定体 = 函数体(净, "按分排序");
        string 不稳体 = 函数体(净, "按分排序_不稳定");

        if (不稳体 == null)
            return CheckResult.不过("找不到「按分排序_不稳定」的函数体 —— 花括号配对了吗？");
        if (!不稳体.Contains("std::sort("))
            return CheckResult.不过(
                "「按分排序_不稳定」里没有看到 `std::sort`。\n" +
                "      这一条是故意的：两个函数要用**不同的**排序函数，\n" +
                "      好让你亲眼比较同分的人顺序变没变：\n" +
                "          按分排序          →  std::stable_sort   （稳定）\n" +
                "          按分排序_不稳定    →  std::sort          （不稳定）\n" +
                "      ⚠ 写 `std::stable_sort` 也不行 —— `std::sort(` 和 `std::stable_sort(` 是两个字。");
        if (稳定体 == null || !稳定体.Contains("std::stable_sort("))
            return CheckResult.不过(
                "「按分排序」里没有看到 `std::stable_sort`。\n" +
                "      这一课要求它**必须稳定** —— 同分的人要保持原来的先后顺序，\n" +
                "      那就只能用 `std::stable_sort`（`std::sort` 不保证这一点）。");

        if (稳定 == 不稳)
            return CheckResult.过("稳定版 = " + 稳定 + "；不稳定版 = " + 不稳 +
                "（这一组数据下恰好相同，但两个函数确实用了不同的排序）");
        return CheckResult.过("稳定版 " + 稳定 + " ／ 不稳定版 " + 不稳 + " —— 同分顺序果然被打乱了");
    }

    [检查说明("★ 比较函数用 > 或 <，不能写 >= 或 <=")]
    public static CheckResult 检查_比较函数(string 代码)
    {
        // ⚠ 本机的真实行为：写 `>=` 时 g++ **既不报错也不警告**，
        //   所以只能用文本看。见文件头的检查设计说明。
        string 净 = 去掉注释(代码);
        var 坏 = new List<string>();
        foreach (var 行 in 净.Split('\n'))
        {
            string s = 行.Trim();
            if (s.StartsWith("return") && (s.Contains(">=") || s.Contains("<=")))
                坏.Add("      " + s);
        }
        if (坏.Count == 0)
            return CheckResult.过("比较函数里没有 >= / <= —— 满足严格弱序");

        return CheckResult.不过(
            "比较函数里出现了 `>=` 或 `<=`：\n" + string.Join("\n", 坏) + "\n" +
            "      ⚠ 这是本课最阴的一个坑：**编译通过、没有任何警告**，\n" +
            "        元素少的时候结果甚至可能是对的，数据一多就顺序错乱甚至崩。\n" +
            "      为什么：排序要求比较函数满足「严格弱序」——\n" +
            "        两个**相等**的元素，比较函数必须返回 false（谁也不排在谁前面）。\n" +
            "        而 `甲.分数 >= 乙.分数` 在两者相等时返回 true，\n" +
            "        于是「甲排在乙前」和「乙排在甲前」同时成立，排序内部就乱了。\n" +
            "      改法：从高到低用 `>`，从低到高用 `<`，**永远不要**带等号。");
    }

    // ══════════════════ 跑一次、比一比 ══════════════════

    /// <summary>
    /// 自定义骨架：**结构体 成绩 必须出现在学生代码之前**。
    /// 为什么不放在调用点里：调用点在 main 里面，而学生代码在 main 之前 ——
    /// 学生函数里用到的类型必须在那之前就声明好。
    /// </summary>
    private const string 骨架 = """
#include <iostream>
#include <string>
#include <vector>
#include <algorithm>

struct 成绩 {
    std::string 名字;
    int 分数;
};

/*学生代码*/

int main() {
    /*调用点*/
    return 0;
}
""";

    private const string 调用点 = """
std::vector<成绩> 表1 = {{"甲", 90}, {"乙", 80}, {"丙", 90}, {"丁", 80}, {"戊", 90}};
std::vector<成绩> 表2 = {{"小", 70}, {"中", 70}, {"大", 70}};
std::vector<成绩> 表3 = {{"独", 55}};
std::vector<成绩> 表4 = {{"甲", 90}, {"乙", 80}, {"丙", 90}, {"丁", 80}, {"戊", 90}};
std::vector<成绩> 表5 = {{"小", 70}, {"中", 70}, {"大", 70}};
std::vector<成绩> 表6 = {{"独", 55}};
std::vector<成绩> 汇总 = {{"甲", 90}, {"乙", 80}, {"丙", 90}, {"丁", 80}, {"戊", 90}};
std::vector<成绩> 空表;

std::cout << "总1|" << 总分(汇总) << "总1| \n";
std::cout << "总2|" << 总分(空表) << "总2| \n";

按分排序(表1);
std::cout << "稳定1|";
for (const 成绩& 人 : 表1) std::cout << 人.名字 << ' ';
std::cout << "稳定1| \n";
按分排序(表2);
std::cout << "稳定2|";
for (const 成绩& 人 : 表2) std::cout << 人.名字 << ' ';
std::cout << "稳定2| \n";
按分排序(表3);
std::cout << "稳定4|";
for (const 成绩& 人 : 表3) std::cout << 人.名字 << ' ';
std::cout << "稳定4| \n";

按分排序_不稳定(表4);
std::cout << "不稳1|";
for (const 成绩& 人 : 表4) std::cout << 人.名字 << ' ';
std::cout << "不稳1| \n";
按分排序_不稳定(表5);
std::cout << "不稳2|";
for (const 成绩& 人 : 表5) std::cout << 人.名字 << ' ';
std::cout << "不稳2| \n";
""";

    private static string 上次的代码;
    private static CppRun 上次的运行;

    private static CppRun 跑一次(string 代码)
    {
        if (上次的运行 != null && 上次的代码 == 代码) return 上次的运行;
        上次的代码 = 代码;
        上次的运行 = 编译并运行(代码, 骨架, 调用点);
        return 上次的运行;
    }

    /// <summary>
    /// 编译并运行，**环境抽风时重试一次**。
    ///
    /// ⚠ 为什么需要它（实测踩到的）：运行器每次在一个全新的临时目录里编译，
    ///   而 `CppRunner.清理旧目录` 会把**别的进程**留下的 run* 目录删掉 ——
    ///   这台机器上同时有好几个助手在跑各自的课程检查，于是刚建出来的目录
    ///   会被别人的检查顺手删掉，g++ 报：
    ///       ld.exe: cannot open output file …prog.exe: No such file or directory
    ///   这是**环境**问题（而且常常只发生在一次检查的第一次编译上），不是学生
    ///   代码的问题。同一份代码重跑一次就好了。
    /// </summary>
    private static CppRun 编译并运行(string 代码, string 骨架, string 调用点)
    {
        var run = CppCheck.编译并运行(代码, 骨架, 调用点);
        if (run.编译通过) return run;
        if (!环境抽风(run)) return run;                 // 真的是学生的错，别重试
        System.Threading.Thread.Sleep(300);
        var 再来 = CppCheck.编译并运行(代码, 骨架, 调用点);
        return 再来.编译通过 ? 再来 : run;
    }

    private static bool 环境抽风(CppRun run) =>
        run.编译信息.Contains("No such file or directory") ||
        run.编译信息.Contains("cannot open output file");

    private static string 先别急着比(CppRun run)
    {
        if (run.Raw.CompileFailed)
        {
            if (run.编译信息.Contains("was not declared") && run.编译信息.Contains("成绩"))
                return
                    "编译器不认识「成绩」这个类型。\n" +
                    "      这一课的结构体由课程骨架提供（`struct 成绩 { std::string 名字; int 分数; };`），\n" +
                    "      如果你又写了一遍，就成了重复定义。把你自己的那一份删掉。";
            return CppCheck.编译失败说明(run).Detail;
        }
        if (run.Raw.Error != null)
            return
                "程序没跑完就退出了：" + run.Raw.Error + "\n" +
                "      这一课最可能的原因是**比较函数写成了 `>=`**：\n" +
                "      它不满足严格弱序，元素一多排序内部就会越界读写 —— 表现是崩掉或者结果乱掉。\n" +
                "      改成 `>`（从高到低）或 `<`（从低到高），不要带等号。";
        return null;
    }

    private static CheckResult 比一比(CppRun run, params (string 标记, string 期望, string 说明)[] 用例们)
    {
        var 错 = new List<string>();
        foreach (var (标记, 期望, 说明) in 用例们)
        {
            string 得到 = 取一段(run, 标记, 0);
            if (得到 == null) 错.Add("      " + 说明 + "：程序没有给出这一组的输出");
            else if (!一样(得到, 期望))
                错.Add("      " + 说明 + "：得到 " + 显示(得到) + "，应该是 " + 显示(期望));
        }
        if (错.Count == 0) return CheckResult.过("这几组都对");

        return CheckResult.不过(
            "结果不对：\n" + string.Join("\n", 错) + "\n" +
            "      检查两件事：\n" +
            "        ① 比较函数的方向 —— 从高到低是 `甲.分数 > 乙.分数`；\n" +
            "        ② 参数带没带 `&` —— 不带 & 排的是副本，外面那张表纹丝不动。");
    }

    /// <summary>
    /// 取「标记|」与「闭标记」之间的内容。
    /// 开头的标记只写名字加一根竖线，闭标记是完整的「名字|」；
    /// 因此**不能拿标记本身去当右边界**（开头那个少了末尾竖线，找不到），
    /// 要用名字去找闭标记 —— 这一处我在第 17 课栽过，这里照抄修好的写法。
    /// </summary>
    private static string 取一段(CppRun run, string 标记, int 第几段)
    {
        string 名 = 标记.TrimEnd('|');
        int 见到 = 0;
        foreach (var 行 in run.行)
        {
            int 起 = 行.IndexOf(名, System.StringComparison.Ordinal);
            while (起 >= 0)
            {
                if (起 == 0 || 行[起 - 1] == ' ' || 行[起 - 1] == '\t')
                {
                    int 头 = 起 + 名.Length;
                    if (头 >= 行.Length || 行[头] != '|')
                    { 起 = 行.IndexOf(名, 起 + 1, System.StringComparison.Ordinal); continue; }
                    头++;
                    int 尾 = 行.IndexOf(标记, 头, System.StringComparison.Ordinal);
                    if (尾 < 0)
                    {
                        尾 = 行.Length;
                        if (尾 > 头 && 行[尾 - 1] == '|') 尾--;
                    }
                    string 这一段 = 行.Substring(头, 尾 - 头);
                    if (起 > 0) 这一段 = " " + 这一段;
                    if (见到++ == 第几段) return 这一段.Trim();
                    break;
                }
                起 = 行.IndexOf(名, 起 + 1, System.StringComparison.Ordinal);
            }
        }
        return null;
    }

    private static bool 一样(string 甲, string 乙) =>
        System.Text.RegularExpressions.Regex.Replace(甲.Trim(), @"\s+", " ") ==
        System.Text.RegularExpressions.Regex.Replace(乙.Trim(), @"\s+", " ");

    private static string 显示(string s) => s == null ? "（没拿到）" : (s.Length == 0 ? "（空）" : s);

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

    private static string 函数体(string 净源码, string 函数名)
    {
        int 名 = 净源码.IndexOf(函数名 + "(", System.StringComparison.Ordinal);
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
}
