"""错答案：不查环，直接拿最小的 n-1 条边（会挑出一条带环的「树」）。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "54", "对.py"), encoding="utf-8").read())


def 最小生成树(点数, 边列表):
    if 点数 <= 1:
        return 0, []
    选 = [e for e in sorted(边列表) if e[1] != e[2]][:点数 - 1]     # ❌ 没查环
    if len(选) != 点数 - 1:
        return None, []
    return sum(e[0] for e in 选), 选
