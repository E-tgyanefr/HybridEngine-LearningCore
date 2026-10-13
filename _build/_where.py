"""让 GUI 自己报**控件的绝对坐标** —— 判断"是截图偏了"还是"布局真错了"。

⚠⚠ 为什么还需要这个（我已经在截图上栽了四次）：

  截出来的图里，主页 hero 明显偏右。有两种可能：

    ① **我截错了** —— 抓的那块区域不是应用窗口（别的窗口盖着 / 坐标偏了）
    ② **布局真错了** —— 那个 `place(relx=0.5, anchor="center")` 没生效

  这两者**光看图分不出来**，而我已经因为"看图"误判过一次
  （第一次说"布局坏了"，实际是 Edge 盖在上面；Tk 量出来是完美居中的）。

  ⇒ 唯一的办法：**让应用自己把关键控件的绝对坐标报出来**，
    再和它报的客户区比：

        客户区 1180 宽 @ rootx=8
        hero   框宽 568 @ rootx=314     ⇒ 左边距 = 306，右边距 = 306 ✓

    如果 hero 的 rootx 在客户区里是居中的，那**布局没问题，是我截错了**。

⚠ 这条判据是**自足**的：不依赖屏幕、不依赖窗口在哪、不依赖谁在前面。
  这正是我前面缺的东西。
"""
import json
import os
import sys

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）
sys.path.insert(0, LC)
os.chdir(LC)

from hybridengine_light.learn_gui import 界面   # noqa: E402

a = 界面()
a.geometry("1180x760+0+0")


def 报():
    a.update_idletasks()
    a.update()
    客户 = {"x": a.winfo_rootx(), "y": a.winfo_rooty(),
            "w": a.winfo_width(), "h": a.winfo_height()}
    print("  客户区：%(w)dx%(h)d @ (%(x)d,%(y)d)" % 客户)

    #: 主页那个居中的框 = self.主页 的第一个子控件
    主页 = getattr(a, "主页", None)
    if 主页 is None:
        print("  !! 没有 self.主页")
    else:
        子 = 主页.winfo_children()
        print("  主页子控件：%d 个" % len(子))
        for i, w in enumerate(子):
            左 = w.winfo_rootx() - 客户["x"]
            右 = 客户["w"] - 左 - w.winfo_width()
            上 = w.winfo_rooty() - 客户["y"]
            居中 = "**居中**" if abs(左 - 右) <= 4 else "偏了 %d" % abs(左 - 右)
            print("    [%d] %-8s %4dx%-4d  左%4d 右%4d 上%4d  ⇒ %s"
                  % (i, w.winfo_class(), w.winfo_width(), w.winfo_height(),
                     左, 右, 上, 居中))
        if 子:
            框 = 子[0]
            print("  那个框里的控件：")
            for w in 框.winfo_children():
                try:
                    字 = w.cget("text")
                except Exception:
                    字 = ""
                print("      %-8s %4dx%-4d  %s"
                      % (w.winfo_class(), w.winfo_width(), w.winfo_height(),
                         str(字)[:38]))


a.after(2200, 报)
a.after(3200, a.destroy)
a.mainloop()
print("  量完。")
