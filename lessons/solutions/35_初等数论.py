"""第 35 课参考答案 —— 初等数论。"""


def 是质数(n):
    if n < 2:
        return False
    # ⚠ 边界要含 √n：写成 `试除数 * 试除数 < n` 的话，9 会被判成质数
    试除数 = 2
    while 试除数 * 试除数 <= n:
        if n % 试除数 == 0:
            return False
        试除数 += 1
    return True


def 分解质因数(n):
    if n < 2:
        return []
    出 = []
    试除数 = 2
    while 试除数 * 试除数 <= n:
        while n % 试除数 == 0:
            出.append(试除数)
            n //= 试除数
        试除数 += 1
    # 循环结束时剩下的 n 如果大于 1，它自己就是质数（97、999983 就是这样留下的）
    if n > 1:
        出.append(n)
    return 出


def 约数们(n):
    if n <= 0:
        return []
    出 = []
    约 = 1
    while 约 * 约 <= n:
        if n % 约 == 0:
            出.append(约)
            另一半 = n // 约
            if 另一半 != 约:
                出.append(另一半)
        约 += 1
    return sorted(出)


def 约数个数(n):
    return len(约数们(n))
