"""错答案：杨辉三角每行全填 1，形状对但中间的数不对。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "51", "对.py"), encoding="utf-8").read())


def 杨辉三角(层数):
    if 层数 <= 0:
        return []
    return [[1] * (i + 1) for i in range(层数)]     # ❌ 中间的数应当是上一行两数之和
