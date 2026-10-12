"""错答案：DFS 从头到尾没记「去过」—— 图里有环就会一直绕。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "49", "对.py"), encoding="utf-8").read())


def 深度优先(邻接表, 起点):
    去过 = []

    def 走(点):
        去过.append(点)
        for 邻 in 邻接表.get(点, []):
            走(邻)                  # ❌ 少了 if 邻 not in 去过

    走(起点)
    return 去过
