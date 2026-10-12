"""错答案：排列数直接用了 n 的阶乘（P(5,2) 算成 120）。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "51", "对.py"), encoding="utf-8").read())


def 排列数(n, k):
    if n < 0 or k < 0 or k > n:
        return 0
    return math.factorial(n)            # ❌ 应该是 n × (n-1) × … 只乘 k 个
