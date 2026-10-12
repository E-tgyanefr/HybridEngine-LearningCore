# 给 16/17/18 三课补上「环境抽风就重试一次」的包装。
#
# 为什么：运行器把编译目录建在公共 TEMP 下，而 `CppRunner.清理旧目录` 会删掉
# **别的进程**留下的 run* 目录 —— 这台机器上同时有好几个助手在跑课程检查，
# 于是刚建出来的目录会被别人的检查顺手删掉，g++ 报
#     ld.exe: cannot open output file …prog.exe: No such file or directory
# 这不是学生代码的问题，重试一次就好。19~25 课已经带了这层包装。
import io
import os
import re

ROOT = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode\csharp\cpp-lessons\Lessons"
旧的 = """    private static CppRun 跑一次(string 代码)
    {
        if (上次的运行 != null && 上次的代码 == 代码) return 上次的运行;
        上次的代码 = 代码;
        上次的运行 = CppCheck.编译并运行(代码, null, 调用点);
        return 上次的运行;
    }
"""
新的 = """    private static CppRun 跑一次(string 代码)
    {
        if (上次的运行 != null && 上次的代码 == 代码) return 上次的运行;
        上次的代码 = 代码;
        上次的运行 = 编译并运行(代码, null, 调用点);
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
    ///       cc1plus.exe: fatal error: …main.cpp: No such file or directory
    ///   这是**环境**问题（而且常常只发生在一次检查的第一次编译上），不是学生
    ///   代码的问题。同一份代码重跑一次就好了 —— 不重试的话，学生看到的是
    ///   「编译没能通过」，会一头雾水地去改本来没错的代码。
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
"""

for 文件 in ("第16课_二维数组.cs", "第17课_排序入门.cs", "第18课_排序稳定性.cs"):
    p = os.path.join(ROOT, 文件)
    t = io.open(p, encoding="utf-8").read()
    if 旧的 not in t:
        print("?? %s 里没找到那段「跑一次」" % 文件)
        continue
    # 第 16 课的「跑一次」多了个 occ 参数版本，所以单独数一下
    n = t.count("CppCheck.编译并运行(代码, null, 调用点)")
    t = t.replace("上次的运行 = CppCheck.编译并运行(代码, null, 调用点);",
                  "上次的运行 = 编译并运行(代码, null, 调用点);")
    # 第 18 课用的是自定义骨架
    t = t.replace("上次的运行 = CppCheck.编译并运行(代码, 骨架, 调用点);",
                  "上次的运行 = 编译并运行(代码, 骨架, 调用点);")
    t = t.replace(旧的.replace("CppCheck.编译并运行(代码, null, 调用点)", "编译并运行(代码, null, 调用点)"), 新的)
    # 别的直接调用点（检查_函数都在 之类）也换成带重试的
    t = t.replace("CppCheck.编译并运行(代码, null,", "编译并运行(代码, null,")
    t = t.replace("CppCheck.编译并运行(代码, 骨架,", "编译并运行(代码, 骨架,")
    io.open(p, "w", encoding="utf-8", newline="").write(t)
    print("OK %s（换了 %d 处）" % (文件, n))
