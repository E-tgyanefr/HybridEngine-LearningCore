"""第 53 课参考答案 —— 单源最短路。"""

import heapq


def 最短路(图, 起点):
    最短 = {点: float("inf") for 点 in 图}      # ⚠ 每个点都要有键，走不到的就是 inf
    最短[起点] = 0
    堆 = [(0, 起点)]                            # 堆里放 (距离, 点)
    while 堆:
        距离, 点 = heapq.heappop(堆)
        if 距离 > 最短[点]:
            continue                            # 过期的记录，跳过
        for 邻, 权 in 图[点]:
            if 距离 + 权 < 最短[邻]:
                最短[邻] = 距离 + 权
                heapq.heappush(堆, (最短[邻], 邻))
    return 最短


def 朴素最短路(图, 起点):
    # Bellman-Ford：把每条边松弛 V-1 轮。慢，但负边也算得对。
    最短 = {点: float("inf") for 点 in 图}
    最短[起点] = 0
    边们 = [(甲, 邻, 权) for 甲 in 图 for 邻, 权 in 图[甲]]
    for _ in range(len(图) - 1):
        for 甲, 邻, 权 in 边们:
            if 最短[甲] + 权 < 最短[邻]:
                最短[邻] = 最短[甲] + 权
    return 最短
