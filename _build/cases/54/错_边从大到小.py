"""错答案：边按权从大到小挑（贪心的方向反了）。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "54", "对.py"), encoding="utf-8").read())


def 最小生成树(点数, 边列表):
    if 点数 <= 1:
        return 0, []
    父 = list(range(点数))
    总权值, 选中 = 0, []
    for 权, 甲, 乙 in sorted(边列表, reverse=True):     # ❌ 要从**小**到大
        if 甲 == 乙:
            continue
        if 合并(父, 甲, 乙):
            总权值 += 权
            选中.append((权, 甲, 乙))
            if len(选中) == 点数 - 1:
                break
    if len(选中) != 点数 - 1:
        return None, []
    return 总权值, 选中
