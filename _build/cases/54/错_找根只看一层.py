"""错答案：找根只往上看一层，链式合并之后就不对了。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "54", "对.py"), encoding="utf-8").read())


def 找根(父, x):
    return 父[x]                    # ❌ 要一路跟到 父[x] == x
