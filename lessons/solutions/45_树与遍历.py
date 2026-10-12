"""第 45 课参考答案 —— 树与遍历。"""

from collections import deque


class 树节点:
    def __init__(self, 值):
        self.值 = 值
        self.左 = None
        self.右 = None


def 前序(根):
    if 根 is None:                  # ⚠ 三条遍历 + 高度，第一行都要判空
        return []
    return [根.值] + 前序(根.左) + 前序(根.右)       # 根、左、右


def 中序(根):
    if 根 is None:
        return []
    return 中序(根.左) + [根.值] + 中序(根.右)       # 左、根、右


def 后序(根):
    if 根 is None:
        return []
    return 后序(根.左) + 后序(根.右) + [根.值]       # 左、右、根


def 层序(根):
    if 根 is None:
        return []
    队 = deque([根])
    出 = []
    while 队:
        点 = 队.popleft()
        出.append(点.值)
        if 点.左:
            队.append(点.左)
        if 点.右:
            队.append(点.右)
    return 出


def 高度(根):
    if 根 is None:
        return 0
    return 1 + max(高度(根.左), 高度(根.右))
