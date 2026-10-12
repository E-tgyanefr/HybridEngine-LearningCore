"""错答案：组合数用阶乘硬算，没挡 k > n，负的阶乘直接崩。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "51", "对.py"), encoding="utf-8").read())


def 组合数(n, k):
    # ❌ 少了 if not 0 <= k <= n: return 0 —— k > n 时 n - k 是负数
    return math.factorial(n) // (math.factorial(k) * math.factorial(n - k))
