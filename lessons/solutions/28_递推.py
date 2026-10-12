"""第 28 课参考答案 —— 递推（从前往后填表）。"""


def 爬楼梯(n):
    # ⚠ 起点是 [1, 1]：f(0)=1（原地不动算 1 种）
    表 = [1, 1]
    for i in range(2, n + 1):
        表.append(表[i - 1] + 表[i - 2])
    return 表[n]


def 跨三级(n):
    # 能跨 1/2/3 级 ⇒ 每一格由**前三格**推出来
    表 = [1, 1, 2]
    for i in range(3, n + 1):
        表.append(表[i - 1] + 表[i - 2] + 表[i - 3])
    return 表[n]


def 斐波那契表(n):
    # 递推：每个值只算一次，n=200 也是瞬间
    表 = [0, 1]
    for i in range(2, n + 1):
        表.append(表[i - 1] + 表[i - 2])
    return 表[n]


def 最大子段和(数字们):
    if not 数字们:
        return 0
    # 表[i] = 以第 i 个数**结尾**的最大和
    表 = [数字们[0]]
    for i in range(1, len(数字们)):
        表.append(max(表[i - 1] + 数字们[i], 数字们[i]))
    return max(表)
