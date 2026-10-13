# 数一数**每条课程线**的答题模板有多少行、里面有没有讲解。
# 这才是学员打开时看到的东西，必须逐条实测而不是猜文件名。
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）
sys.path.insert(0, LC)
os.chdir(LC)

from hybridengine_light import learnmodel as M                  # noqa: E402

关键 = ("先介绍", "轮到你了", "用法", "常见坑", "怎么自己测")
for 键 in list(M.即时轨道) + list(M.编译型轨道):
    try:
        课们 = M.发现(键)
    except Exception as e:
        课们 = []
        print("  %-13s 发现失败：%s" % (键, e))
        continue
    if not 课们:
        print("  %-13s （没有课）" % 键)
        continue
    k = 课们[0]
    try:
        文 = M.看课(k)
    except Exception as e:
        文 = ""
        print("  %-13s 看课失败：%s" % (键, e))
    try:
        模 = M.答题模板(k) if hasattr(M, "答题模板") else None
    except Exception:
        模 = None
    if 模 is None:
        # 找模型层里真正给界面用的那个函数名
        for 名 in ("取模板", "模板", "stub", "题目模板", "答题"):
            if hasattr(M, 名):
                模 = getattr(M, 名)(k)
                break
    n = len(模.splitlines()) if isinstance(模, str) else -1
    命中 = [x for x in 关键 if isinstance(模, str) and x in 模]
    print("  %-13s 课%2d · 看课%3d 行 · 答题模板%4s 行 · 命中%s"
          % (键, len(课们), len(文.splitlines()), n, 命中 or "（无讲解）"))
