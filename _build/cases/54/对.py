"""第 54 课参考答案 —— 最小生成树。"""


def 找根(父, x):
    # ⚠ 必须一路跟到底。写成 return 父[x] 在链式合并之后就会判错
    while 父[x] != x:
        x = 父[x]
    return x


def 合并(父, 甲, 乙):
    根甲, 根乙 = 找根(父, 甲), 找根(父, 乙)
    if 根甲 == 根乙:
        return False            # 本来就在一个集合里
    父[根甲] = 根乙
    return True


def 最小生成树(点数, 边列表):
    if 点数 <= 1:
        return 0, []            # 一个点（或没有点）本来就是一整棵树
    父 = list(range(点数))
    总权值, 选中 = 0, []
    for 权, 甲, 乙 in sorted(边列表):          # ① 按权从小到大
        if 甲 == 乙:
            continue                        # 自环没用
        if 合并(父, 甲, 乙):                # ② 不在同一集合就选它
            总权值 += 权
            选中.append((权, 甲, 乙))
            if len(选中) == 点数 - 1:       # ③ 够了就停
                break
    if len(选中) != 点数 - 1:
        return None, []                     # 凑不够 = 不连通
    return 总权值, 选中


def 普里姆(点数, 边列表):
    if 点数 <= 1:
        return 0
    连 = {i: [] for i in range(点数)}
    for 权, 甲, 乙 in 边列表:
        if 甲 != 乙:
            连[甲].append((权, 乙))
            连[乙].append((权, 甲))
    进树, 总 = {0}, 0
    while len(进树) < 点数:
        最好 = None
        for 点 in 进树:                      # 已进树的点连出去的边全看一遍
            for 权, 邻 in 连[点]:
                if 邻 not in 进树 and (最好 is None or 权 < 最好[0]):
                    最好 = (权, 邻)
        if 最好 is None:
            return None                     # 拉不动了：图不连通
        总 += 最好[0]
        进树.add(最好[1])
    return 总
