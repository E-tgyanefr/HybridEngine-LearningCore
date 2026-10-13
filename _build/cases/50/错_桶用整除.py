"""错答案：桶位置用了整除 //，而且只建有键的桶。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "50", "对.py"), encoding="utf-8").read())


def 桶分布(键们, 表长):
    桶 = []
    for 键 in 键们:
        桶.append(键 // 表长)           # ❌ 哈希取的是余数 %，不是商 //
    return 桶
