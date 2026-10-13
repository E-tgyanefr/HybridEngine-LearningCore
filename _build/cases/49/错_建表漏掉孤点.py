"""错答案：只给「出现过」的点建键，孤点直接没有（取不到就 KeyError）。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "49", "对.py"), encoding="utf-8").read())


def 建邻接表(点数, 边列表):
    表 = {}
    for 甲, 乙 in 边列表:
        表.setdefault(甲, []).append(乙)
        表.setdefault(乙, []).append(甲)
    return 表
