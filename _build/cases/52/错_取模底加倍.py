"""错答案：快速幂取模里底数该平方，写成了加倍。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "52", "对.py"), encoding="utf-8").read())


def 幂取模(底, 指数, 模):
    结果 = 1 % 模
    while 指数 > 0:
        if 指数 % 2 == 1:
            结果 = 结果 * 底 % 模
        底 += 底                 # ❌ 应该是 底 *= 底
        指数 //= 2
    return 结果
