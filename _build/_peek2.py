# 实测 GUI 的语言列表树里到底有什么。
#
# ⚠ 为什么要单独测：命令行 `__诊断__` 能看到 c=16 课，
#   但 **GUI 是另一条路径**（`_填语言列表` + `_后台载入编译型`）。
#   「这边能跑」不等于「那边能看见」。
#
# ⚠⚠ 必须在主线程里做：Tkinter 不是线程安全的。
import os
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
sys.path.insert(0, LC)
os.chdir(LC)

import tkinter as tk                                        # noqa: E402
from hybridengine_light import learn_gui as G                # noqa: E402


def 树全文(树, 节点="", 层=0):
    出 = []
    for c in 树.get_children(节点):
        出.append("  " * 层 + "· " + 树.item(c, "text"))
        出.extend(树全文(树, c, 层 + 1))
    return 出


def 数课(树, 节点=""):
    n = 0
    for c in 树.get_children(节点):
        t = 树.item(c, "text")
        if t.strip()[:2].isdigit():
            n += 1
        n += 数课(树, c)
    return n


root = tk.Tk()
root.withdraw()                      # 不抢焦点、不弹窗
app = G.界面()

app.进课程学习()
起 = time.time()
for i in range(300):
    root.update()
    root.after(100)
    root.update()
    if not getattr(app, "_载入中", False):
        print("  [后台载入结束] 用时 %.1f 秒" % (time.time() - 起))
        break
    if i % 20 == 0:
        print("  [等 %5.1fs] _载入中=%s" % (time.time() - 起, getattr(app, "_载入中", "?")))
else:
    print("  [超时] 30 秒还没结束，_载入中=%s" % getattr(app, "_载入中", "?"))

for _ in range(20):
    root.update()
    root.after(50)

print()
print("  === 界面上「课程学习」里的树 ===")
for l in 树全文(app.树):
    print("  " + l)
print()
print("  树里能看到的课数 =", 数课(app.树))
root.destroy()
