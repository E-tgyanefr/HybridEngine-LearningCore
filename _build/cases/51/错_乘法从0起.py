"""错答案：乘法原理拿 0 做初始值，空列表算出来是 0。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "51", "对.py"), encoding="utf-8").read())


def 乘法原理(各组数量):
    结果 = 0                    # ❌ 乘法要拿 1 起头，空列表才是「一种方案」
    for 数 in 各组数量:
        结果 *= 数
    return 结果
