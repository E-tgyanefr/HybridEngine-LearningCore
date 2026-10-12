"""错答案：快速幂的结果从 0 起，乘什么都还是 0。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "52", "对.py"), encoding="utf-8").read())


def 幂(底, 指数):
    结果 = 0                    # ❌ 乘法要拿 1 起头
    while 指数 > 0:
        if 指数 % 2 == 1:
            结果 *= 底
        底 *= 底
        指数 //= 2
    return 结果
