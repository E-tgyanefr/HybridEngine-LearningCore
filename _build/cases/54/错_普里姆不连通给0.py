"""错答案：普里姆碰到不连通返回 0（应当返回 None）。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "54", "对.py"), encoding="utf-8").read())


def 普里姆(点数, 边列表):
    if 点数 <= 1:
        return 0
    连 = {i: [] for i in range(点数)}
    for 权, 甲, 乙 in 边列表:
        if 甲 != 乙:
            连[甲].append((权, 乙))
            连[乙].append((权, 甲))
    进树, 总 = {0}, 0
    while len(进树) < 点数:
        最好 = None
        for 点 in 进树:
            for 权, 邻 in 连[点]:
                if 邻 not in 进树 and (最好 is None or 权 < 最好[0]):
                    最好 = (权, 邻)
        if 最好 is None:
            return 0                # ❌ 不连通应当返回 None
        总 += 最好[0]
        进树.add(最好[1])
    return 总
