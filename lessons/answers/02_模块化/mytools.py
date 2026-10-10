"""工具模块 —— 只装函数，不写 input / print。"""

单位 = "平方米"


def 面积(长, 宽):
    return 长 * 宽


def 周长(长, 宽):
    return 2 * (长 + 宽)


def 是正方形(长, 宽):
    return 长 == 宽
