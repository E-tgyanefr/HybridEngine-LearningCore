# 逐条实测：每条课程线第一次打开课时，**生成的答题文件**长什么样。
# 走的是和命令行/界面**完全同一条**路径（`_确保模板`）。
import os
import shutil
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）
sys.path.insert(0, LC)
os.chdir(LC)

from hybridengine_light import learnmodel as M                  # noqa: E402

关键 = ("先介绍", "轮到你了", "常见坑", "怎么自己测", "这一课")
print("  %-13s %-7s %-8s %s" % ("课程线", "模板行数", "含讲解", "模板开头三行"))
print("  " + "─" * 96)
for 键 in list(M.即时轨道) + list(M.编译型轨道):
    课们 = M.发现(键)
    if not 课们:
        print("  %-13s （没有课）" % 键)
        continue
    k = 课们[0]
    # 清掉旧模板，强制重新生成，才能看到"第一次打开"的样子
    try:
        路径 = M._答案文件(k)
        if 路径.exists():
            路径.unlink()
        M._确保模板(k)
        文 = 路径.read_text(encoding="utf-8")
    except Exception as e:
        文 = ""
        print("  %-13s 生成失败：%s: %s" % (键, type(e).__name__, e))
        continue
    行 = 文.splitlines()
    命中 = sum(1 for x in 关键 if x in 文)
    前 = " / ".join(l.strip() for l in 行[:3] if l.strip())[:52]
    print("  %-13s %5d 行  %d/%d 项  %s" % (键, len(行), 命中, len(关键), 前))
