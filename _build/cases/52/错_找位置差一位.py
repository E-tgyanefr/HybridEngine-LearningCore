"""错答案：倍增跳跃时下标差一位，跳到表尾就 IndexError。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "52", "对.py"), encoding="utf-8").read())


def 第一个不小于(表, 目标):
    跳 = 1
    while 跳 < len(表):
        跳 *= 2
    位置 = 0
    while 跳 > 0:
        if 位置 + 跳 <= len(表) and 表[位置 + 跳] < 目标:    # ❌ 越界一位
            位置 += 跳
        跳 //= 2
    return 位置
