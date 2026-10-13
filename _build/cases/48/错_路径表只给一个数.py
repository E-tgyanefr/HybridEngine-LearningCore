"""错答案：路径表返回的是右下角那个数，不是整张表。"""


def 路径数(行, 列):
    if 行 <= 0 or 列 <= 0:
        return 0
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
    return dp[-1][-1]


def 最长公共子序列(甲, 乙):
    dp = [[0] * (len(乙) + 1) for _ in range(len(甲) + 1)]
    for i in range(1, len(甲) + 1):
        for j in range(1, len(乙) + 1):
            if 甲[i - 1] == 乙[j - 1]:
                dp[i][j] = dp[i - 1][j - 1] + 1
            else:
                dp[i][j] = max(dp[i - 1][j], dp[i][j - 1])
    return dp[-1][-1]
