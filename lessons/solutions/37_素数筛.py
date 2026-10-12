"""第 37 课参考答案 —— 素数筛。"""


def 筛质数(n):
    if n < 2:
        return []
    # ⚠ 长度是 n + 1（下标 0…n）。写成 [True] * n 时，标记 n 本身会 IndexError
    标记 = [True] * (n + 1)
    标记[0] = 标记[1] = False           # ⚠ 0 和 1 不是质数，最容易漏
    质数 = 2
    while 质数 * 质数 <= n:
        if 标记[质数]:
            # 从 p*p 开始：2p、3p…(p-1)p 早被更小的质数划掉了
            倍数 = 质数 * 质数
            while 倍数 <= n:
                标记[倍数] = False
                倍数 += 质数
        质数 += 1
    return [i for i in range(n + 1) if 标记[i]]


def 质数个数(n):
    return len(筛质数(n))


def 最小质因数表(n):
    表 = [0] * (n + 1)
    for i in range(2, n + 1):
        if 表[i] == 0:                  # i 是质数
            j = i
            while j <= n:
                if 表[j] == 0:          # 只标还没被更小的质数标过的
                    表[j] = i
                j += i
    return 表
