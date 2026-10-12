"""错答案：把斜着的四个方向也算成连通了。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "50", "对.py"), encoding="utf-8").read())

八方 = ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1))


def 数岛屿(网格):
    行数 = len(网格)
    列数 = len(网格[0]) if 行数 else 0
    见过 = set()

    def 灌(r, c):
        if not (0 <= r < 行数 and 0 <= c < 列数):
            return
        if 网格[r][c] != 1 or (r, c) in 见过:
            return
        见过.add((r, c))
        for dr, dc in 八方:             # ❌ 对角不算连通
            灌(r + dr, c + dc)

    块 = 0
    for r in range(行数):
        for c in range(列数):
            if 网格[r][c] == 1 and (r, c) not in 见过:
                块 += 1
                灌(r, c)
    return 块
