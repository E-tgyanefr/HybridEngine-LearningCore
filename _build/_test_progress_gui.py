"""实测桌面版的新进度功能 —— 一步一步验，不靠截图。

⚠ 做法（沿用 `_measure_gui.py` 那套）：
  不问屏幕、不问眼睛，**问 Tk 自己**（`cget("text")` / 画布 `coords`）。
  截图会受"别的窗口盖在上面"影响（我为此误判过一次"布局坏了"），
  而这些读数不会。

验四件事：
  ① 进学习界面后进度条建出来了、分母对（= 树上的课数）
  ② 选一课 → 8 秒后「读过」被记下、树上多一个 ✓、进度条变长
  ③ 存到磁盘了（`.学习进度.json`）—— 关掉再开能读回来
  ④ **快速扫过不该算读过**（这是"读过"这个信号可信的关键）
"""
import os
import sys
import time

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）
sys.path.insert(0, LC)
os.chdir(LC)

from hybridengine_light import progress as P          # noqa: E402
from hybridengine_light.learn_gui import 界面          # noqa: E402

#: ⚠ 先备份学员的真实进度，验完还回去 —— 不能因为测试把人家的记录擦了
备份 = P.读()
进度文件 = LC + os.sep + P.文件名
原件 = None
if os.path.exists(进度文件):
    原件 = open(进度文件, "rb").read()

#: 从零开始验，这样读数才有意义
P.写({})

应用 = 界面()
结果 = []


def 步(名, 好, 详情=""):
    结果.append((名, 好, 详情))
    print("  %s %s%s" % ("✓" if 好 else "✗", 名, ("  " + 详情) if 详情 else ""))


def 找课节点():
    """树上第一个挂着课的节点。"""
    for 节点, 课 in 应用._树到课.items():
        return 节点, 课
    return None, None


def 阶段1():
    """进学习界面，看进度条建出来没有。"""
    应用.进课程学习()
    应用.update()
    print("  --- 阶段 1：进度条 ---")
    总 = len(应用._树到课)
    步("学习界面有课程节点", 总 > 0, "树上 %d 课" % 总)
    步("进度条对象建出来了", 应用._进度条 is not None)
    步("进度字有内容", bool(应用.进度字.cget("text")),
        "「%s」" % 应用.进度字.cget("text"))
    #: ⚠ 载入没完成 ⇒ 应当是**不确定进度**（来回滑）+「还在载入」，
    #:   而不是百分比 —— 早显示百分比会让条在载入完成时**倒退**。
    步("载入中显示「还在载入」而不是百分比",
        "还在载入" in 应用.进度字.cget("text")
        and "%" not in 应用.进度字.cget("text"),
        "「%s」" % 应用.进度字.cget("text"))
    应用.after(300, 阶段2)


def 阶段2():
    """选一课 → 立刻看（这时**不该**算读过）→ 等 8 秒。"""
    print("  --- 阶段 2：停留够久才算读过 ---")
    节点, 课 = 找课节点()
    if 课 is None:
        步("找到一课", False)
        应用.after(200, 收场)
        return
    应用.树.selection_set(节点)
    应用._选课()
    应用.update()
    步("选课成功", 应用.当前课 is 课, "第 %s 课" % 课.编号)
    步("**刚进课时不算读过**", 应用._课号(课) not in P.读(),
        "（防「扫一遍全变读过」）")
    应用.after(8600, 阶段3)


def 阶段3():
    """8 秒之后 —— 该记下了。"""
    print("  --- 阶段 3：8 秒后 ---")
    节点, 课 = 找课节点()
    存 = P.读()
    步("读过被记下", 应用._课号(课) in 存, "键=%s" % 应用._课号(课))
    #: 树上文字应当有 ✓
    文字 = 应用.树.item(节点, "text")
    步("树上加了 ✓", "✓" in 文字, "「%s」" % 文字)
    步("进度字变了", "1/" in 应用.进度字.cget("text"),
        "「%s」" % 应用.进度字.cget("text"))
    应用.after(300, 阶段4)


def 阶段4():
    """快速扫过**不该**算读过。"""
    print("  --- 阶段 4：快速扫过不算读过 ---")
    键们 = list(应用._树到课.items())
    if len(键们) < 3:
        步("有足够的课可扫", False)
        应用.after(200, 收场)
        return
    #: 连着选三课，每课只停 200ms（模拟用方向键狂扫）
    for 节点, 课 in 键们[1:4]:
        应用.树.selection_set(节点)
        应用._选课()
        应用.update()
        time.sleep(0.2)
    存 = P.读()
    扫过 = [应用._课号(课) for _, 课 in 键们[1:4]]
    新记 = [k for k in 扫过 if k in 存]
    步("扫过的三课**没被**记成读过", not 新记,
        "（记了 %s）" % 新记 if 新记 else "（一条都没记）")
    应用.after(200, 收场)


def 收场():
    print("  --- 结果 ---")
    好 = sum(1 for _, k, _ in 结果 if k)
    print("  %d/%d 项通过" % (好, len(结果)))
    #: ⚠ 把学员的真实进度还回去
    if 原件 is not None:
        with open(进度文件, "wb") as f:
            f.write(原件)
        print("  已还原原来的进度文件")
    else:
        P.写(备份)
        try:
            os.remove(进度文件)
        except OSError:
            pass
        print("  已清掉测试产生的进度文件（原来没有）")
    应用.destroy()
    sys.exit(0 if 好 == len(结果) else 1)


应用.after(1400, 阶段1)
应用.mainloop()
