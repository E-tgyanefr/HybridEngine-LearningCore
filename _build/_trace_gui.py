# 在真实 GUI 流程里打出每一条课程线通知的**原始值**。
import os
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）
sys.path.insert(0, LC)
os.chdir(LC)

import tkinter as tk                                        # noqa: E402
from hybridengine_light import learn_gui as G                # noqa: E402
from hybridengine_light import learnmodel as M               # noqa: E402

原收课程线 = G.界面._收课程线


def 包(self, 键, 结果):
    父 = self._待填父节点.get(键, "（没有占位！）")
    print("  [_收课程线] 键=%-7s 结果=%-14r 占位=%s" % (键, 结果, 父))
    原收课程线(self, 键, 结果)


G.界面._收课程线 = 包

原收任务 = G.界面._收任务


def 包2(self):
    try:
        while True:
            项 = self._任务队列.get_nowait()
            if isinstance(项, tuple):
                print("  [_收任务] 队列元组 = %r" % (项,))
            self._任务队列.put(项)
            break
    except Exception:
        pass
    原收任务(self)


root = tk.Tk()
root.withdraw()
app = G.界面()
print("  === 进课程学习 ===")
app.进课程学习()
print("  _待填父节点 keys =", list(app._待填父节点.keys()))
for i in range(200):
    root.update()
    root.after(100)
    root.update()
    if not getattr(app, "_载入中", False) and i > 6:
        break
for _ in range(30):
    root.update()
    root.after(50)

print()
print("  === 载入后 _待填父节点 keys =", list(app._待填父节点.keys()))
print()
print("  === 树（只看语言层）===")
for c in app.树.get_children(""):
    print("  " + app.树.item(c, "text"))
    for d in app.树.get_children(c):
        print("    " + app.树.item(d, "text"))
root.destroy()
