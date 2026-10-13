"""错答案：第一个不小于用从头挨着扫的写法（答案对，但不是倍增）。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "52", "对.py"), encoding="utf-8").read())


def 第一个不小于(表, 目标):
    i = 0
    while i < len(表) and 表[i] < 目标:      # ❌ O(n) 线性扫，不是「翻着倍跳」
        i += 1
    return i
