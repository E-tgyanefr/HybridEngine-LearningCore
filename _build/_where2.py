"""量学习界面的几何 —— 确认三栏和按钮组排布正确。

⚠ 沿用"问 Tk 自己"的做法（见 `_where.py` 的注释）：
  不看截图 —— 这个环境里截图被别的窗口污染过四次。
"""
import os
import sys

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）
sys.path.insert(0, LC)
os.chdir(LC)

from hybridengine_light.learn_gui import 界面   # noqa: E402

a = 界面()
a.geometry("1180x760+0+0")
a.after(1200, lambda: a.进课程学习())


def 量():
    a.update_idletasks()
    a.update()
    X, Y = a.winfo_rootx(), a.winfo_rooty()
    W, H = a.winfo_width(), a.winfo_height()
    print("  客户区：%dx%d @ (%d,%d)" % (W, H, X, Y))

    print("  === 底栏按钮（左组 / 右组 / 状态）===")
    #: 底栏是父容器的倒数第二个子控件（最后一个是那条 1px 线）
    for w in a.winfo_children():
        if w.winfo_class() == "Frame" and w.winfo_height() > 40:
            子 = w.winfo_children()
            有按钮 = [c for c in 子 if c.winfo_class() == "Button"]
            if len(有按钮) >= 3:
                print("    底栏 %dx%d" % (w.winfo_width(), w.winfo_height()))
                for c in 子:
                    if c.winfo_class() in ("Button", "Label", "Canvas"):
                        名 = ""
                        try:
                            名 = c.cget("text")
                        except Exception:
                            pass
                        左 = c.winfo_rootx() - X
                        print("      %-8s 左%4d  宽%4d  %s"
                              % (c.winfo_class(), 左, c.winfo_width(),
                                 str(名)[:22]))
                break

    print("  === 三栏 ===")
    for w in a.winfo_children():
        for p in w.winfo_children():
            if p.winfo_class() == "PanedWindow":
                for c in p.winfo_children():
                    左 = c.winfo_rootx() - X
                    print("    %-8s 左%4d 宽%4d 高%4d"
                          % (c.winfo_class(), 左, c.winfo_width(), c.winfo_height()))
    print("  === 编辑器 ===")
    码 = getattr(a, "代码", None)
    if 码 is not None:
        print("    代码区 %dx%d  内边距 padx=%s pady=%s 行距 %s/%s"
              % (码.winfo_width(), 码.winfo_height(),
                 码.cget("padx"), 码.cget("pady"),
                 码.cget("spacing1"), 码.cget("spacing3")))


a.after(2400, 量)
a.after(3400, a.destroy)
a.mainloop()
print("  量完。")
