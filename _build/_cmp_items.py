"""⚠ 统一到**同一份真实工作量**再比 —— 用"可见 item 数"当分母。

为什么必须这样：
  · 我在 10-13 量到的 `tkinter 4.855 ms` 是「**1000 个对象铺满视野**」⇒ 可见 250 个
  · 队友量自己的后端时是「1000 个圆铺满视野」⇒ 可见 1000 个
  ⇒ **两个数的分母不同**，直接比就是"两个口径硬凑"（本仓犯过）。

  这一份把两边都钉在**同一批坐标**上，并**报出真实可见 item 数**。
"""
import os
import statistics
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
根 = r"D:\Users\etgya\Desktop\Project\引擎\HybridEngine-LearningCore"
sys.path.insert(0, 根)
os.chdir(根)
from hybridengine_light import pyengine as E
from hybridengine_light.pyengine import components as C

宽, 高 = 800, 450


def 量(名, f, 次=60, 预热=20):
    for _ in range(预热):
        f()
    样 = []
    for _ in range(次):
        t0 = time.perf_counter()
        f()
        样.append((time.perf_counter() - t0) * 1000)
    print("    %-40s 中位 %8.4f ms" % (名, statistics.median(样)))
    return statistics.median(样)


#: ⚠ 视野就是虚拟单位 0..100 ⇒ 想让 N 个对象全可见，就得让世界坐标全落在 0..100
def 造满视野(N):
    """N 个对象铺满 0..100（**全部可见**）。"""
    s = E.场景("full")
    列 = 1
    while 列 * 列 < N:
        列 += 1
    for i in range(N):
        r_i, c_i = divmod(i, 列)
        o = s.造("o%d" % i, 2.0 + c_i * (96.0 / 列), 2.0 + r_i * (96.0 / 列))
        o.加(C.圆点(半径=1.2, 颜色="红"))
    return s


for N in (100, 300, 1000):
    s = 造满视野(N)
    g = E.游戏(场景=s, 宽=宽, 高=高)
    g.一步(1 / 60)
    可见 = len(g.后端._canvas.find_all())
    中 = 量("Tk后端 · %4d 个对象（画布 item=%d）" % (N, 可见), lambda: g.一步(1 / 60))
    g.关()
    print("         ⇒ 每个 item %.2f µs" % (中 * 1000 / max(可见, 1)))
