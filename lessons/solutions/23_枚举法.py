"""第 23 课参考答案 —— 枚举法。"""


def 是素数(n):
    # ⚠ 小于 2 的一律不是素数（1 也不是）
    if n < 2:
        return False
    # ⚠ range() 只吃整数：不能写 n / 2，也不能写 n ** 0.5
    for 试除数 in range(2, n // 2 + 1):
        if n % 试除数 == 0:
            return False
    return True


def 找素数(上限):
    出 = []
    for n in range(2, 上限 + 1):        # +1 不能省，否则漏掉"上限本身"
        if 是素数(n):
            出.append(n)
    return 出


def 水仙花数():
    出 = []
    for n in range(100, 1000):          # 三位数
        百, 十, 个 = n // 100, n // 10 % 10, n % 10
        if 百 ** 3 + 十 ** 3 + 个 ** 3 == n:
            出.append(n)
    return 出


def 百钱百鸡():
    出 = []
    for 公 in range(0, 21):             # 100 ÷ 5 = 20，最多 20 只公鸡
        for 母 in range(0, 34):         # 100 ÷ 3 ≈ 33，最多 33 只母鸡
            小 = 100 - 公 - 母           # ⚠ 用等式算出来，省掉一层循环
            if 小 >= 0 and 小 % 3 == 0 and 5 * 公 + 3 * 母 + 小 // 3 == 100:
                出.append((公, 母, 小))
    return 出
