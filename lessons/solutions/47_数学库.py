"""第 47 课参考答案 —— 数学库 math。"""

import math


def 圆面积(半径):
    # ⚠ 用 math.pi，不要手写 3.14 —— 面积越大差得越多。
    return math.pi * 半径 ** 2


def 斜边(甲, 乙):
    return math.sqrt(甲 ** 2 + 乙 ** 2)


def 向下取整(x):
    # ⚠ floor 不是 int：int(-2.5) 是 -2（向零截断），floor(-2.5) 才是 -3。
    return math.floor(x)


def 向上取整(x):
    return math.ceil(x)


def 最大公约数(甲, 乙):
    # math.gcd 自己会处理 0 和负数：gcd(0, 0) 是 0，gcd(-12, 18) 是 6。
    return math.gcd(甲, 乙)


def 二进制位数(数):
    # ⚠ 先挡掉 0 和负数 —— math.log2(0) 会抛 ValueError: math domain error
    if 数 <= 0:
        return 0
    return math.floor(math.log2(数)) + 1


def 约等于(甲, 乙):
    # ⚠ 不要自己写 abs(甲 - 乙) < 1e-9：那是绝对误差。
    #    isclose 比的是相对误差，1e9 和 1e9 + 0.5 也算相等。
    return math.isclose(甲, 乙)
