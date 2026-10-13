"""第 36 课参考答案 —— 辗转相除。"""


def 最大公约数(甲, 乙):
    甲, 乙 = abs(甲), abs(乙)
    # ⚠ 写成 `while 乙:` 而不是 `while 甲 % 乙 != 0`：
    #   后者在 乙 = 0 时会 ZeroDivisionError: integer modulo by zero
    while 乙:
        甲, 乙 = 乙, 甲 % 乙
    return 甲


def 最小公倍数(甲, 乙):
    甲, 乙 = abs(甲), abs(乙)
    if 甲 == 0 or 乙 == 0:
        return 0
    # 先除后乘：中间结果小一点（Python 无所谓，C++ 的 int 会溢出）
    return 甲 // 最大公约数(甲, 乙) * 乙


def 约分(分子, 分母):
    if 分母 == 0:
        return None
    公 = 最大公约数(分子, 分母)
    分子, 分母 = 分子 // 公, 分母 // 公
    if 分母 < 0:
        分子, 分母 = -分子, -分母
    return (分子, 分母)
