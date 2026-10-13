"""错答案：更新了字典却忘了把新距离推进堆，后面的点就扩不出去了。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "53", "对.py"), encoding="utf-8").read())


def 最短路(图, 起点):
    最短 = {点: float("inf") for 点 in 图}
    最短[起点] = 0
    堆 = [(0, 起点)]
    while 堆:
        距离, 点 = heapq.heappop(堆)
        if 距离 > 最短[点]:
            continue
        for 邻, 权 in 图[点]:
            if 距离 + 权 < 最短[邻]:
                最短[邻] = 距离 + 权       # ❌ 少了 heapq.heappush(堆, (最短[邻], 邻))
    return 最短
