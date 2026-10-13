"""第 48 课参考答案 —— 二维动态规划。"""


def 路径数(行, 列):
    # ⚠ 边界：行或列为 0 时没有格子可走，返回 0
    if 行 <= 0 or 列 <= 0:
        return 0
    # 第一行、第一列全是 1（贴着边走只有一条路）—— 开表时直接填 1 最省事
    dp = [[1] * 列 for _ in range(行)]
    for i in range(1, 行):
        for j in range(1, 列):
            dp[i][j] = dp[i - 1][j] + dp[i][j - 1]
    return dp[-1][-1]


def 路径表(行, 列):
    if 行 <= 0 or 列 <= 0:
        return []
    dp = [[1] * 列 for _ in range(行)]
    for i in range(1, 行):
        for j in range(1, 列):
            dp[i][j] = dp[i - 1][j] + dp[i][j - 1]
    return dp


def 最长公共子序列(甲, 乙):
    # dp 比字符串多一行一列：第 0 行 / 第 0 列是边界，全 0
    dp = [[0] * (len(乙) + 1) for _ in range(len(甲) + 1)]
    for i in range(1, len(甲) + 1):
        for j in range(1, len(乙) + 1):
            # ⚠ dp 的下标从 1 开始，字符要从 0 开始取 —— 所以是 甲[i-1]
            if 甲[i - 1] == 乙[j - 1]:
                dp[i][j] = dp[i - 1][j - 1] + 1
            else:
                dp[i][j] = max(dp[i - 1][j], dp[i][j - 1])
    return dp[-1][-1]
