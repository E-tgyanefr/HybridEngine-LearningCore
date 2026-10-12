"""第 31 课参考答案 —— 算法复杂度。"""


def 平方次数(n):
    # 两层循环，各跑 n 次 ⇒ n × n 次
    次数 = 0
    for i in range(n):
        for j in range(n):
            次数 += 1
    return 次数


def 数出次数(n):
    # ⚠ 不许数一遍：n=100000 时两层循环是 50 亿次（几分钟），公式是瞬间
    #   0 + 1 + … + (n-1) = n(n-1)/2
    return n * (n - 1) // 2


def 线性查找(表, 要找的):
    for i in range(len(表)):
        if 表[i] == 要找的:
            return i                   # 找到就返回下标（重复时给第一个）
    return -1                          # 全找完没有 ⇒ -1


def 有没有重复(表):
    # ⚠ 两层循环 = O(n²)。内层从 i+1 开始：自己和自己不用比
    for i in range(len(表)):
        for j in range(i + 1, len(表)):
            if 表[i] == 表[j]:
                return True
    return False
