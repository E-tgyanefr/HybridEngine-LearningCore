# 把 01 / 02 两课也改成「先介绍 → 再布置任务」。
#
# ⚠ 这两课是**手写**的（不在 `_py_lessons.txt` 里），所以直接改生成物。
#   教训记一笔：Python 课现在有**两个来源** ——
#     03~08 来自 `_build/_py_lessons.txt`（重新生成会覆盖）
#     01~02 是手写文件（重新生成不影响）
#   上一轮我给 08 改了判据、又跑了一次生成 ⇒ 改动被覆盖。
#   **改之前先确认"这个文件的真值在哪"**，否则会白改一遍。
import io
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
S = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode\lessons"

#: 每课：把「要做」段插到哪一行**之前**（按"这一行的前几个字"定位）
插到 = {
    "01_变量.py": "# ─────────",
    "02_模块化.py": "# ─────────",
}
#: 插入点定位要从"最后一段讲解"开始 —— 用「怎么自己测」更准
更准 = ("怎么自己测", "关于 `if __name__`", "怎么运行这一课")


def 处理(f: str) -> bool:
    P = os.path.join(S, f)
    行们 = io.open(P, encoding="utf-8").read().split("\n")

    # ① 找「要做」段
    起 = None
    for i, l in enumerate(行们):
        if l.startswith("#") and "要做" in l:
            起 = i
            break
    if 起 is None:
        print("  ! %s: 没有「要做」段" % f)
        return False
    止 = None
    for i in range(起 + 1, len(行们)):
        s = 行们[i].strip()
        if s.startswith("# ──"):
            止 = i
            break
    if 止 is None:
        print("  ! %s: 找不到「要做」段的结尾" % f)
        return False

    要做块 = 行们[起:止]
    其余 = 行们[:起] + 行们[止:]

    # ② 重新包装「要做」段
    新块 = ["# ─────────────────────────────────────────────────────",
            "#  轮到你了 —— 在下面的答题文件里写这几件事",
            "# ─────────────────────────────────────────────────────",
            "#"]
    for l in 要做块:
        if l.startswith("#") and "要做" in l:
            continue
        新块.append(l)

    # ③ 找插入点：最后一个讲解小节之前
    插 = len(其余)
    for 关 in 更准:
        for i, l in enumerate(其余):
            if 关 in l and l.strip().startswith("#"):
                插 = min(插, i)
    # 插点前面若紧跟空注释行，去掉以免出现两个空行
    出 = 其余[:插] + ["#"] + 新块 + ["#"] + 其余[插:]
    io.open(P, "w", encoding="utf-8", newline="\n").write("\n".join(出))
    print("  ✓ %s 已重排（「要做」移到第 %d 行起）" % (f, 插 + len(新块)))
    return True


for f in ("01_变量.py", "02_模块化.py"):
    处理(f)
