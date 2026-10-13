"""错答案：朴素最短路只松弛一轮，负边就绕不过来了。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "53", "对.py"), encoding="utf-8").read())


def 朴素最短路(图, 起点):
    最短 = {点: float("inf") for 点 in 图}
    最短[起点] = 0
    边们 = [(甲, 邻, 权) for 甲 in 图 for 邻, 权 in 图[甲]]
    for 甲, 邻, 权 in 边们:            # ❌ 只扫一轮；要扫 len(图)-1 轮
        if 最短[甲] + 权 < 最短[邻]:
            最短[邻] = 最短[甲] + 权
    return 最短
