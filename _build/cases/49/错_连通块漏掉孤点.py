"""错答案：连通块只数「有边的点」，孤点被漏掉。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "49", "对.py"), encoding="utf-8").read())


def 连通块个数(点数, 边列表):
    表 = 建邻接表(点数, 边列表)
    去过, 块 = set(), 0
    for 点 in range(点数):
        if 点 in 去过:
            continue
        if not 表[点]:              # ❌ 没边的点直接跳过 —— 孤点也是一块
            continue
        块 += 1
        栈 = [点]
        while 栈:
            当 = 栈.pop()
            if 当 in 去过:
                continue
            去过.add(当)
            栈.extend(表[当])
    return 块
