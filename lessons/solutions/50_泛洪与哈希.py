"""第 50 课参考答案 —— 泛洪与哈希。"""


def 数岛屿(网格):
    行数 = len(网格)
    列数 = len(网格[0]) if 行数 else 0
    见过 = set()

    def 灌(r, c):
        # ⚠ 先问界，再问色 —— 反过来就读到网格外面去了
        if not (0 <= r < 行数 and 0 <= c < 列数):
            return
        if 网格[r][c] != 1 or (r, c) in 见过:
            return
        见过.add((r, c))
        for dr, dc in ((1, 0), (-1, 0), (0, 1), (0, -1)):       # 只有上下左右
            灌(r + dr, c + dc)

    块 = 0
    for r in range(行数):
        for c in range(列数):
            if 网格[r][c] == 1 and (r, c) not in 见过:
                块 += 1
                灌(r, c)
    return 块


def 染色面积(网格, 行, 列):
    行数 = len(网格)
    列数 = len(网格[0]) if 行数 else 0
    if not (0 <= 行 < 行数 and 0 <= 列 < 列数):
        return 0                        # 起点越界：面积就是 0，不能崩
    颜色 = 网格[行][列]
    见过 = set()
    栈 = [(行, 列)]
    while 栈:
        r, c = 栈.pop()
        if not (0 <= r < 行数 and 0 <= c < 列数) or (r, c) in 见过:
            continue
        if 网格[r][c] != 颜色:
            continue
        见过.add((r, c))
        for dr, dc in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            栈.append((r + dr, c + dc))
    return len(见过)


def 第一个重复的(列表):
    见过 = set()                        # 集合的 in 是 O(1)，整体才是 O(n)
    for 数 in 列表:
        if 数 in 见过:
            return 数
        见过.add(数)
    return None


def 桶分布(键们, 表长):
    桶 = [[] for _ in range(表长)]      # ⚠ 空桶也要开出来，别只建有键的那些
    for 键 in 键们:
        桶[键 % 表长].append(键)
    return 桶
