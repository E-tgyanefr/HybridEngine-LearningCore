"""错答案：翻倍步数从 1 开始数（0 步的情况也报成 1）。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "52", "对.py"), encoding="utf-8").read())


def 翻倍步数(n):
    数, 步 = 1, 1               # ❌ 步应该从 0 起
    while 数 < n:
        数 *= 2
        步 += 1
    return 步
