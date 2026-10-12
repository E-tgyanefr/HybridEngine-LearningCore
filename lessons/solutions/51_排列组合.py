"""第 51 课参考答案 —— 排列组合。"""

import math


def 排列数(n, k):
    # ⚠ k 越界是「不可能」，返回 0；不挡的话算阶乘会抛
    #   ValueError: factorial() not defined for negative values
    if n < 0 or k < 0 or k > n:
        return 0
    结果 = 1
    for i in range(k):
        结果 *= n - i          # 只乘 k 个数：P(5,2) = 5 × 4
    return 结果


def 组合数(n, k):
    if n < 0 or k < 0 or k > n:
        return 0
    # 先按顺序挑（排列），再除掉 k 个的全排列
    return 排列数(n, k) // math.factorial(k)


def 乘法原理(各组数量):
    结果 = 1                   # ⚠ 空列表返回 1：一步都不做，也算一种方案
    for 数 in 各组数量:
        结果 *= 数
    return 结果


def 加法原理(各类数量):
    和 = 0
    for 数 in 各类数量:
        和 += 数
    return 和


def 杨辉三角(层数):
    if 层数 <= 0:
        return []
    三角 = [[1]]
    for i in range(1, 层数):
        上行 = 三角[-1]
        新行 = [1]
        for j in range(1, i):
            新行.append(上行[j - 1] + 上行[j])      # 正上方两个数之和
        新行.append(1)
        三角.append(新行)
    return 三角
