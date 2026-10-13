# -*- coding: utf-8 -*-
r"""把「改过死路径、而且**不会写课程文件**」的脚本**每个真跑一遍**，记退出码。

════════════════════════════════════════════════════════════════
  为什么不能只看退出码
════════════════════════════════════════════════════════════════

这一轮修的 bug 本身就是**「退出码 0 但什么都没干」**：

    写死的路径指向一个不存在的目录 ⇒ `os.walk` 返回空（不报错）
    ⇒ 脚本"跑完了、没报错、什么都没做"。

所以「跑完退出码 0」**不能**当作"修好了"的证据 ——
判据必须包含**正向信号**：每个脚本都得打出它**本该打出**的东西。

本脚本因此给每个脚本配一组 `必须含` 正则：

    · 缺任何一条 ⇒ 判「**没跑到**」（= 可能又静默空转了），**算失败**
    · 命中但有数字为 0 的 ⇒ 也单列出来（例：`0 课`）

⚠ 这正是 `环境坑与教训.md` 第 22 条那条规矩：
  「反面验证的判据必须把『没跑到』也算失败（否则"脚本没起作用"和
   "脚本全过了"长得一模一样）」。这里把它用在"修完路径"这件事上。

════════════════════════════════════════════════════════════════
  两类脚本分开
════════════════════════════════════════════════════════════════

    可跑   —— 本脚本跑它们（只读/诊断类，最多往 TEMP 或 gitignore 的答题区写）
    不可跑 —— 会写 `lessons/` `csharp/*-lessons/` 的，或一次性生成器。
              跑它 = **改教学内容** ⇒ 硬要求不允许。
              `_verify_fixed_paths.py` 对它们做**静态**验证，本脚本**不跑**，
              报告里单独列「没跑」，绝不混进"已验证"里。

⚠ 串行：这些脚本里有几个会清 `lessons/answers/`、`ai-answers/` 之类的
  **共享答题区**（第 0b 条：同一目录同一时刻只能一个进程写）。
  本脚本一个一个跑，不并行。

用法：
    python _build/_run_fixed_scripts.py            # 全跑
    python _build/_run_fixed_scripts.py --只 _mkjava   # 只跑名字里含它的
    python _build/_run_fixed_scripts.py --列          # 只列表
"""
import io
import os
import re
import subprocess
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

env = dict(os.environ)
env["PYTHONIOENCODING"] = "utf-8"
env["PYTHONUNBUFFERED"] = "1"
env["DOTNET_CLI_UI_LANGUAGE"] = "en"

#: (脚本, 超时秒, [(必须含的正则, 说明)], 备注)
#: ⚠ 「必须含」里凡是带数字的，都挑**非零**的那些 —— 0 课正是要抓的症状。
脚本 = [
    ("_build/_mkjava.py", 60,
     [(r"目标文件：", "打出了目标文件"),
      (r"HybridEngine-LearningCore", "路径指向**新**仓库名 ⇒ 路径修复真的生效"),
      (r"(?<!HybridEngine-)LearnCode", "**不许**再出现老仓库名")],
     "打印类；这一条是路径修复最直接的证据"),

    ("_build/_cmp_discover.py", 420,
     [(r"直接解析", "打出了对比行"),
      (r"课（", "打出了课数")],
     "读 Lessons/*.cs vs dotnet 的课程表"),
    ("_build/_cmp_discover2.py", 420,
     [(r"直接解析", "打出了对比行"), (r"课（", "打出了课数")],
     "逐字符扫描版"),
    ("_build/_cmp_thread.py", 300,
     [(r"M\.编译型轨道 =", "打出了轨道表"), (r"主线程", "跑了主线程那一段")],
     "主线程 vs 后台线程"),
    ("_build/_diff_cpp.py", 420,
     [(r"直接解析=", "逐条对比真的输出了")],
     "C++ 课程表逐条比"),
    ("_build/_time_discover.py", 420,
     [(r"工程名/答案名", "打到了第二段"), (r"秒", "量到了时间")],
     "逐条计时"),
    ("_build/_cold.py", 300,
     [(r"全部发现完", "冷启动总量出来了"), (r"冷启动", "两次都跑了")],
     "冷启动实测"),
    ("_build/_audit_drift.py", 900,
     [(r"判据", "打到了判据段"),
      (r"全部一致|不一致", "给出了分叉结论")],
     "四条线 drift：无写操作，只比内容"),
    ("_build/_peek_templates.py", 420,
     [(r"答题模板", "打出了模板行数")],
     "量答题模板"),
    ("_build/_peek_templates2.py", 420,
     [(r"模板行数", "打出了表头")],
     "清模板重建再看（答题区，非课程）"),
    ("_build/_peek_templates3.py", 300,
     [(r"模板行数", "打出了表头")],
     "直接调模板生成器"),
    ("_build/_gesp_reference.py", 900,
     [(r"课用参考答案全过", "给出了结论行")],
     "35~46 用参考答案跑（写 lessons/answers/，gitignore 的答题区）"),

    ("_build/_peek2.py", 240, [(r"树里能看到的课数", "量到了树里的课数")], "GUI：语言列表树"),
    ("_build/_trace_gui.py", 240,
     [(r"_待填父节点 keys", "打出了占位表"), (r"进课程学习", "进了学习界面")], "GUI：通知原始值"),
    ("_build/_trace2.py", 300, [(r"共收到", "打出了收到总数")], "GUI：等够久看 C 到不到"),
    ("_build/_where.py", 240, [(r"客户区：", "GUI 报了客户区几何")], "GUI：量控件绝对坐标"),
    ("_build/_where2.py", 240, [(r"客户区：", "GUI 报了客户区几何")], "GUI：量学习界面几何"),
    ("_build/_shot.py", 300, [(r"客户区：", "报了几何才截图")], "GUI：截图"),
    ("_build/_test_progress_gui.py", 300,
     [(r"项通过", "打出了通过项数")], "GUI：进度功能（会备份/还原真实进度）"),

    ("tests/验收.py", 1800,
     [(r"课全过", "打出了逐课合计")],
     "全仓验收（会清并重建 lessons/ answers 等答题区）"),
]


def main():
    参 = sys.argv[1:]
    if "--列" in 参:
        for P, t, 必, 备注 in 脚本:
            print("  %-32s 超时%4ds  %s" % (P, t, 备注))
        return 0

    只 = None
    if "--只" in 参:
        i = 参.index("--只")
        只 = 参[i + 1] if i + 1 < len(参) else ""

    print("═" * 70)
    print("  改过死路径的脚本：逐个真跑（只跑不会写课程文件的那批）")
    print("═" * 70)
    print("  仓库根：%s" % ROOT)
    print()

    结果 = []
    for P, 超时, 必须, 备注 in 脚本:
        if 只 and 只 not in P:
            continue
        路 = os.path.join(ROOT, P.replace("/", os.sep))
        if not os.path.isfile(路):
            结果.append((P, None, "脚本不存在", [], 备注))
            print("  !! %-32s 脚本不存在" % P)
            continue
        t0 = time.time()
        try:
            r = subprocess.run([sys.executable, 路], cwd=ROOT, env=env,
                               capture_output=True, timeout=超时)
            码, 出 = r.returncode, (r.stdout + r.stderr).decode("utf-8", "replace")
        except subprocess.TimeoutExpired as e:
            码, 出 = None, ((e.stdout or b"") + (e.stderr or b"")).decode("utf-8", "replace")
        耗时 = time.time() - t0
        #  ⚠ 每个脚本的**原始输出**都留一份 —— 只打摘要的话，
        #    "跑到了但结果可疑"（比如某段是 0 课 / 全 0）事后无从复核。
        try:
            名 = os.path.basename(P).replace(".py", "")
            io.open(os.path.join(ROOT, "_build", "_log_%s.txt" % 名), "w",
                    encoding="utf-8", newline="").write(出)
        except OSError:
            pass
        #  ⚠⚠ `(?<!…)` 开头的模式是**反向**信号（"不许出现"），不能算进"必须含"。
        #    第一版把两类混在一起判断 ⇒ `_mkjava.py` 明明**打出了正确的新路径**
        #    （`…\HybridEngine-LearningCore\_build\_java_模板扩写.md`），
        #    却因为"老仓库名没出现"被判成「没跑到」—— 那是**我的判据的假阳性**。
        #    教训和这一轮其它几处一样：**"看着像失败"和"真的失败"要分得开**。
        正 = [(p, s) for p, s in 必须 if not p.startswith("(?<!")]
        反 = [(p, s) for p, s in 必须 if p.startswith("(?<!")]
        缺 = [说明 for pat, 说明 in 正 if not re.search(pat, 出)]
        多了 = [说明 for pat, 说明 in 反 if re.search(pat, 出)]
        行数 = len(出.splitlines())
        if 码 is None:
            判 = "**超时**"
        elif 缺:
            判 = "**没跑到**（缺信号）+ " + "；".join(缺)
        elif 多了:
            判 = "**反向信号命中** + " + "；".join(多了)
        elif 码 != 0:
            判 = "跑到了，但**退出码 %d**" % 码
        else:
            判 = "OK"
        结果.append((P, 码, 判, 缺, 备注))
        print("  %-4s %-32s 退出码 %-5s %5.1fs  %2d 行  %s"
              % ("OK" if 判 == "OK" else "XX", P,
                 "超时" if 码 is None else 码, 耗时, 行数,
                 备注 if 判 == "OK" else 判))
        if 判 != "OK":
            for l in 出.splitlines()[-12:]:
                print("        | " + l.rstrip()[:110])

    print()
    print("═" * 70)
    好 = [x for x in 结果 if x[2] == "OK"]
    坏 = [x for x in 结果 if x[2] != "OK"]
    print("  == %d/%d 个脚本：跑到了、退出码 0、正向信号齐全 =="
          % (len(好), len(结果)))
    if 坏:
        print("  **以下 %d 个不达标**：" % len(坏))
        for P, 码, 判, _, _ in 坏:
            print("    %-32s %s" % (P, 判))
    print("═" * 70)
    #  ⚠ 退出码："没跑到"和"超时"都算失败（不能和"全过了"长得一样）。
    return 0 if not 坏 else 1


if __name__ == "__main__":
    sys.exit(main())
