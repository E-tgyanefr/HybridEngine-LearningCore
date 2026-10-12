"""错答案：泛洪时忘了标记「见过」，同一块被数好几次。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "50", "对.py"), encoding="utf-8").read())


def 数岛屿(网格):
    行数 = len(网格)
    列数 = len(网格[0]) if 行数 else 0

    def 灌(r, c):
        if not (0 <= r < 行数 and 0 <= c < 列数):
            return
        if 网格[r][c] != 1:
            return
        for dr, dc in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            灌(r + dr, c + dc)          # ❌ 没有「见过」，有环就绕不出来

    块 = 0
    for r in range(行数):
        for c in range(列数):
            if 网格[r][c] == 1:
                块 += 1
                灌(r, c)
    return 块
