"""同一份画面 · 两条后端的端到端对照。

⚠ 为什么必须做这件事：
   队友的 0.1130 ms/帧 和我的 4.855 ms/帧 是**两份不同代码画的** ——
   他自己的话说「我的场景是我自己定义的格点，未必和当初量 4.855 时逐像素相同」。
   ⇒ 那个比值**不能**当结论。

   这一份用**同一份场景定义**：
     · 同样 1000 个对象、同样的世界坐标铺法
     · Tk 后端走 引擎.绘制()（清屏 + 提交 + update）
     · C++ 后端走 …⚠ 见下
"""
import os
import statistics
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
#: ⚠ **不能硬编码盘符** —— 本仓的 `_check_platform.py` 是**登记制**，
#:   "清单外的平台命中 = 失败"。而这里**根本不需要**硬编码：
#:   用 `__file__` 往上推两级就是仓库根，跨平台都对。
#:   （我第一版写死了 `D:\...` ⇒ 把三平台 CI 的 `Platform self-check` 弄红了。）
根 = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, 根)
os.chdir(根)

from hybridengine_light import pyengine as E
from hybridengine_light.pyengine import components as C

N = 1000
宽, 高 = 800, 450

print("  ══ 同一份场景：%d 个圆 · %dx%d ══" % (N, 宽, 高))


def 造场景():
    s = E.场景("cmp")
    for i in range(N):
        o = s.造("o%d" % i, 4 + (i % 40) * 20.0, 4 + (i // 40) * 18.0)
        o.加(C.圆点(半径=1.8, 颜色="红"))
    return s


def 量(名, f, 次=60, 预热=20):
    for _ in range(预热):
        f()
    样 = []
    for _ in range(次):
        t0 = time.perf_counter()
        f()
        样.append((time.perf_counter() - t0) * 1000)
    print("    %-34s 中位 %8.4f ms  最小 %8.4f" % (名, statistics.median(样), min(样)))
    return statistics.median(样)


#: ① 纯逻辑（录制后端，不真画）—— 上限参考
s1 = 造场景()
g1 = E.游戏(场景=s1, 宽=宽, 高=高, 后端=E.录制后端())
t1 = 量("① 录制后端（纯逻辑，不真画）", lambda: g1.一步(1 / 60))

#: ② tkinter 真画
s2 = 造场景()
g2 = E.游戏(场景=s2, 宽=宽, 高=高)
g2.一步(1 / 60)
画布 = len(g2.后端._canvas.find_all())
t2 = 量("② Tk后端（真画到画布，item=%d）" % 画布, lambda: g2.一步(1 / 60))
g2.关()

print()
print("    ⇒ 绘制那部分：%.4f ms（%.1f%%）" % (t2 - t1, 100.0 * (t2 - t1) / t2))
