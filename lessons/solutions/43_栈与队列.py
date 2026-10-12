"""第 43 课参考答案 —— 栈与队列。"""

from collections import deque


def 括号匹配(文字):
    配 = {')': '(', ']': '[', '}': '{'}
    栈 = []
    for 字 in 文字:
        if 字 in '([{':
            栈.append(字)
        elif 字 in 配:
            if not 栈 or 栈.pop() != 配[字]:     # ⚠ 栈空也要判 False
                return False
    return not 栈                              # ⚠ 还有没关上的就 False


def 逆波兰(式子):
    栈 = []
    for 块 in 式子.split():
        if 块 in ('+', '-', '*'):
            右 = 栈.pop()                       # ⚠ 先弹出的是右操作数
            左 = 栈.pop()
            if 块 == '+':
                栈.append(左 + 右)
            elif 块 == '-':
                栈.append(左 - 右)
            else:
                栈.append(左 * 右)
        else:
            栈.append(int(块))
    return 栈[0]


def 模拟排队(操作表):
    队 = deque()
    出队 = []
    for 动作, 值 in 操作表:
        if 动作 == '进':
            队.append(值)
        else:
            出队.append(队.popleft() if 队 else None)   # ⚠ popleft 是 O(1)，pop(0) 是 O(n)
    return 出队
