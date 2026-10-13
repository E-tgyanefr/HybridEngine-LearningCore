"""错答案：染色时不判起点的界，越界直接崩。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "50", "对.py"), encoding="utf-8").read())


def 染色面积(网格, 行, 列):
    行数 = len(网格)
    列数 = len(网格[0]) if 行数 else 0
    颜色 = 网格[行][列]                 # ❌ 没判界就读
    见过 = set()
    栈 = [(行, 列)]
    while 栈:
        r, c = 栈.pop()
        if not (0 <= r < 行数 and 0 <= c < 列数) or (r, c) in 见过:
            continue
        if 网格[r][c] != 颜色:
            continue
        见过.add((r, c))
        for dr, dc in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            栈.append((r + dr, c + dc))
    return len(见过)
