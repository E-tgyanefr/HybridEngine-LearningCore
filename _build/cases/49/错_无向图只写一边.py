"""错答案：无向图只写了一条边（从 1 就搜不到 0）。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "49", "对.py"), encoding="utf-8").read())


def 建邻接表(点数, 边列表):
    表 = {点: [] for 点 in range(点数)}
    for 甲, 乙 in 边列表:
        表[甲].append(乙)
    return 表
