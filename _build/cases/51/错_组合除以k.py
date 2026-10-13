"""错答案：组合数忘了除掉 k!（只除了 k）。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "51", "对.py"), encoding="utf-8").read())


def 组合数(n, k):
    if n < 0 or k < 0 or k > n:
        return 0
    if k == 0:
        return 1
    return 排列数(n, k) // k             # ❌ C(n,k) 要除以 k!，不是除以 k
