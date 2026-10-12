"""第 49 课参考答案 —— 图与遍历。"""

from collections import deque


def 建邻接表(点数, 边列表):
    # ⚠ 先把 0..点数-1 全建出来 —— 孤点也要有键，否则遍历时 KeyError
    表 = {点: [] for 点 in range(点数)}
    for 甲, 乙 in 边列表:
        if 甲 == 乙:               # 自环忽略（矩阵那边也填 0）
            continue
        表[甲].append(乙)
        表[乙].append(甲)          # ⚠ 无向图：两边都要写
    return 表


def 建邻接矩阵(点数, 边列表):
    矩 = [[0] * 点数 for _ in range(点数)]
    for 甲, 乙 in 边列表:
        if 甲 != 乙:               # 自环不算一条能走的边
            矩[甲][乙] = 矩[乙][甲] = 1
    return 矩


def 深度优先(邻接表, 起点):
    去过 = []

    def 走(点):
        去过.append(点)             # 进门先标记
        for 邻 in 邻接表.get(点, []):
            if 邻 not in 去过:      # 出门前先问
                走(邻)

    走(起点)
    return 去过


def 广度优先(邻接表, 起点):
    去过 = [起点]
    队 = deque([起点])              # deque 的 popleft 是 O(1)
    while 队:
        点 = 队.popleft()
        for 邻 in 邻接表.get(点, []):
            if 邻 not in 去过:
                去过.append(邻)
                队.append(邻)       # ⚠ 进队时就标记，别等到出队
    return 去过


def 连通块个数(点数, 边列表):
    表 = 建邻接表(点数, 边列表)
    去过 = set()
    块 = 0
    for 点 in range(点数):
        if 点 in 去过:
            continue
        块 += 1                     # 从没去过的点出发，能走到的归一块
        栈 = [点]
        while 栈:
            当 = 栈.pop()
            if 当 in 去过:
                continue
            去过.add(当)
            栈.extend(表[当])
    return 块


def 两点连通(点数, 边列表, 甲, 乙):
    # 从 甲 走一遍，看 乙 在不在走到的点里
    return 乙 in 深度优先(建邻接表(点数, 边列表), 甲)
