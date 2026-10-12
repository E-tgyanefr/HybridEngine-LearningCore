"""错答案：走不到的点写成了 -1。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "53", "对.py"), encoding="utf-8").read())


def 最短路(图, 起点):
    最短 = {点: -1 for 点 in 图}                # ❌ 走不到应该是 float("inf")
    最短[起点] = 0
    堆 = [(0, 起点)]
    while 堆:
        距离, 点 = heapq.heappop(堆)
        if 距离 > 最短[点]:
            continue
        for 邻, 权 in 图[点]:
            if 最短[邻] == -1 or 距离 + 权 < 最短[邻]:
                最短[邻] = 距离 + 权
                heapq.heappush(堆, (最短[邻], 邻))
    return 最短
