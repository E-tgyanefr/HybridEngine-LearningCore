"""错答案：用列表每次扫最小值（答案对，但没用到 heapq）。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "53", "对.py"), encoding="utf-8").read())


def 最短路(图, 起点):
    最短 = {点: float("inf") for 点 in 图}
    最短[起点] = 0
    定下 = set()
    while len(定下) < len(图):
        剩下 = [点 for 点 in 图 if 点 not in 定下]
        点 = min(剩下, key=lambda p: 最短[p])       # ❌ 这里是 O(n)，该用小根堆
        定下.add(点)
        if 最短[点] == float("inf"):
            break
        for 邻, 权 in 图[点]:
            if 最短[点] + 权 < 最短[邻]:
                最短[邻] = 最短[点] + 权
    return 最短
