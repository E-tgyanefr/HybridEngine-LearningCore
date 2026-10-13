"""第 41 课参考答案 —— 分治与归并。"""


def 合并两个有序表(左, 右):
    出 = []
    甲 = 乙 = 0
    while 甲 < len(左) and 乙 < len(右):
        # <= 而不是 <：相等时先取左边的，归并才**稳定**
        if 左[甲] <= 右[乙]:
            出.append(左[甲])
            甲 += 1
        else:
            出.append(右[乙])
            乙 += 1
    # ⚠ 这两句不能省：没走完的那一摞要接上（省了就会少元素，而且不报错）
    出.extend(左[甲:])
    出.extend(右[乙:])
    return 出


def 归并排序(表):
    if len(表) <= 1:                    # 递归出口
        return list(表)
    中 = len(表) // 2
    左 = 归并排序(表[:中])
    右 = 归并排序(表[中:])
    return 合并两个有序表(左, 右)


def 是升序(表):
    for i in range(1, len(表)):
        if 表[i - 1] > 表[i]:
            return False
    return True
