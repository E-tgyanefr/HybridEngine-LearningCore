"""错答案：BFS 在出队时才判断，同一个点会进队好几次（结果里有重复）。"""
import io
import os

_根 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
exec(io.open(os.path.join(_根, "_build", "cases", "49", "对.py"), encoding="utf-8").read())


def 广度优先(邻接表, 起点):
    去过 = []
    队 = deque([起点])
    while 队:
        点 = 队.popleft()
        去过.append(点)
        for 邻 in 邻接表.get(点, []):
            队.append(邻)           # ❌ 进队时不判重，出队才看见
    return 去过
