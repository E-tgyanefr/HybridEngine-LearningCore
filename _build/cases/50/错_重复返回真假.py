"""错答案：第一个重复的返回了 True / False，题目要的是那个元素（或 None）。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "50", "对.py"), encoding="utf-8").read())


def 第一个重复的(列表):
    见过 = set()
    for 数 in 列表:
        if 数 in 见过:
            return True                 # ❌ 要返回 数 本身
        见过.add(数)
    return None
