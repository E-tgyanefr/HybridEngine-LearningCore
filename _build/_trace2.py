# 等**足够久**（不看那个不存在的标志位），看 C 这条最终会不会到。
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
sys.path.insert(0, LC)
os.chdir(LC)

import tkinter as tk                                        # noqa: E402
from hybridengine_light import learn_gui as G                # noqa: E402

已收 = []
原收课程线 = G.界面._收课程线


def 包(self, 键, 结果):
    已收.append((键, 结果))
    print("  [收到] 键=%-7s 结果=%r" % (键, 结果))
    原收课程线(self, 键, 结果)


G.界面._收课程线 = 包

root = tk.Tk()
root.withdraw()
app = G.界面()
app.进课程学习()

# ⚠ 不用"_载入中"当判据（那个属性不存在，用它判断会让循环提前退出 ——
#   我第一次就是这么测的，得到"c 永远不到"的**假结论**）。
#   改成：固定跑够时间。
import time
起 = time.time()
while time.time() - 起 < 25:
    root.update()
    root.after(100)
    root.update()
    if len(已收) >= 4:
        break
    if int((time.time() - 起) * 2) % 6 == 0:
        print("  [等 %4.1fs] 已收到 %d/4 条" % (time.time() - 起, len(已收)))
        root.update()

print()
print("  共收到 %d/4 条，用时 %.1f 秒" % (len(已收), time.time() - 起))
print("  === 语言层 ===")
for c in app.树.get_children(""):
    print("  " + app.树.item(c, "text"))
    for d in app.树.get_children(c):
        n = len(app.树.get_children(d))
        课 = sum(1 for x in app.树.get_children(d)
                 if app.树.item(x, "text").strip()[:2].isdigit())
        print("    %-46s 子节点%d 其中课%d" % (app.树.item(d, "text")[:46], n, 课))
root.destroy()
