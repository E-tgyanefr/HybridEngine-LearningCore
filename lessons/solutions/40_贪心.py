"""第 40 课参考答案 —— 贪心。"""


def 贪心找零(零钱表, 金额):
    剩下 = 金额
    张数 = 0
    # ⚠ 必须从大到小：贪心的"眼前最好"就是"面额最大"
    for 面额 in sorted(零钱表, reverse=True):
        张数 += 剩下 // 面额
        剩下 %= 面额
    return 张数 if 剩下 == 0 else -1


def 选中的区间(区间表):
    选 = []
    上一个结束 = None
    # ⚠ 按结束时间排（不是开始时间）：结束越早，后面剩的余地越大
    for 开始, 结束 in sorted(区间表, key=lambda x: x[1]):
        if 上一个结束 is None or 开始 >= 上一个结束:
            选.append((开始, 结束))
            上一个结束 = 结束
    return 选


def 区间调度(区间表):
    return len(选中的区间(区间表))
