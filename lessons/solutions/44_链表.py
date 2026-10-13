"""第 44 课参考答案 —— 链表。"""


class 节点:
    def __init__(self, 值):
        self.值 = 值
        self.下一个 = None


def 建表(表):
    头 = None
    for 值 in reversed(表):          # 从后往前插，头插最省事
        新 = 节点(值)
        新.下一个 = 头
        头 = 新
    return 头


def 转列表(头):
    出 = []
    当前 = 头
    while 当前:                      # ⚠ 不是 while 当前.下一个
        出.append(当前.值)
        当前 = 当前.下一个
    return 出


def 插入(头, 位置, 值):
    新 = 节点(值)
    if 位置 <= 0 or 头 is None:
        新.下一个 = 头
        return 新                    # ⚠ 插在头上 → 返回新头
    前 = 头
    for _ in range(位置 - 1):
        if 前.下一个 is None:
            break
        前 = 前.下一个
    新.下一个 = 前.下一个            # ⚠ 顺序：先接后面，再改前面
    前.下一个 = 新
    return 头


def 删除(头, 值):
    if 头 is None:
        return None
    if 头.值 == 值:
        return 头.下一个             # ⚠ 删头 → 必须返回新头
    前 = 头
    while 前.下一个:
        if 前.下一个.值 == 值:
            前.下一个 = 前.下一个.下一个
            return 头
        前 = 前.下一个
    return 头
