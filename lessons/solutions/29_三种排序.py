"""第 29 课参考答案 —— 手写三种排序（都是就地排 + return 表）。"""


def 选择排序(表):
    n = len(表)
    for i in range(n - 1):                 # 最后一个自然就位，不用挑
        最小 = i
        for j in range(i + 1, n):          # 只在 i 后面找
            if 表[j] < 表[最小]:
                最小 = j                   # 只记位置，最后换一次
        表[i], 表[最小] = 表[最小], 表[i]
    return 表


def 冒泡排序(表):
    n = len(表)
    for i in range(n - 1):
        换过 = False
        for j in range(n - 1 - i):         # ⚠ 尾巴那 i 格已排好
            if 表[j] > 表[j + 1]:
                # ⚠ 必须一行换完；分两步写会把一个值覆盖掉
                表[j], 表[j + 1] = 表[j + 1], 表[j]
                换过 = True
        if not 换过:                       # 一整趟都没换 ⇒ 已经有序
            break
    return 表


def 插入排序(表):
    for i in range(1, len(表)):
        要插的 = 表[i]
        j = i - 1
        # ⚠ `j >= 0` 不能省：少了它，表[-1] 会拿到最后一个元素而**不报错**
        while j >= 0 and 表[j] > 要插的:
            表[j + 1] = 表[j]
            j -= 1
        表[j + 1] = 要插的
    return 表


def 是排好的(表):
    for i in range(len(表) - 1):
        if 表[i] > 表[i + 1]:              # ⚠ 用 > ；用 >= 会把 (2, 2) 判成没排好
            return False
    return True
