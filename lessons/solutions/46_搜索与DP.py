"""第 46 课参考答案 —— 搜索与 DP。"""

from collections import deque


def 最短步数(迷宫, 起点, 终点):
    if not 迷宫 or not 迷宫[0]:
        return -1
    行数, 列数 = len(迷宫), len(迷宫[0])
    for r, c in (起点, 终点):
        if not (0 <= r < 行数 and 0 <= c < 列数) or 迷宫[r][c] == '#':
            return -1
    队 = deque([(起点, 0)])          # ⚠ 求最短路必须用队列（BFS）：一层层扩
    见过 = {起点}
    while 队:
        (r, c), 步 = 队.popleft()
        if (r, c) == 终点:
            return 步
        for dr, dc in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nr, nc = r + dr, c + dc
            if 0 <= nr < 行数 and 0 <= nc < 列数 and 迷宫[nr][nc] != '#' and (nr, nc) not in 见过:
                见过.add((nr, nc))
                队.append(((nr, nc), 步 + 1))
    return -1                        # 队空了还没到 ⇒ 走不通


def 走法数(台阶数):
    a, b = 1, 1                      # 走法数(0) = 1、走法数(1) = 1
    for _ in range(台阶数):
        a, b = b, a + b
    return a


def 装背包(重量表, 价值表, 容量):
    dp = [0] * (容量 + 1)
    for i in range(len(重量表)):
        # ⚠ 倒着扫：正着扫会让同一件物品被装两次
        for x in range(容量, 重量表[i] - 1, -1):
            dp[x] = max(dp[x], dp[x - 重量表[i]] + 价值表[i])
    return dp[容量]
