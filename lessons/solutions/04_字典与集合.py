"""第 04 课参考答案 —— 字典。"""

库存 = {'苹果': 3, '香蕉': 5, '橘子': 1}


def 查数量(库存表, 商品名):
    """⚠ 查不到要返回 0，不能抛 KeyError —— 那是正常情况。"""
    return 库存表.get(商品名, 0)


def 总件数(库存表):
    和 = 0
    for 名, 数 in 库存表.items():      # ⚠ 括号不能省
        和 += 数
    return 和


def 最多的是谁(库存表):
    if not 库存表:                     # ⚠ 空字典特判
        return None
    最多名 = None
    最多数 = None
    for 名, 数 in 库存表.items():
        if 最多数 is None or 数 > 最多数:
            最多数 = 数
            最多名 = 名
    return 最多名
