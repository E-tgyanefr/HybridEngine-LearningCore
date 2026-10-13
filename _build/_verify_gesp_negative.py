# 反面验证（GESP 五级 / 六级，第 35~46 课）：喂**错误答案**，检查必须报错。
#
# ## 为什么必须做
#   "参考答案全过"只证明检查**不冤枉对的**。
#   如果检查永远返回"通过"，那它同时也"参考答案全过" —— 但对学员毫无价值。
#   ⇒ 必须证明它**会拒绝错的**。
#
# ## 用法
#     python _build/_verify_gesp_negative.py            # 全部
#     python _build/_verify_gesp_negative.py 35 36      # 只跑某几课
#
# ## 判据
#   每个错例都写清「期望被拒的检查」，脚本只认 **`× 检查名`** 那一行 ——
#   光在全输出里搜字串会把"别的检查的提示里提到了这个词"也算成抓到。
import os
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
# ⚠ 路径**从 `__file__` 推**，不写死绝对路径（原来写死 `…\引擎\LearnCode`，
#   而仓库后来搬到了 `…\引擎\HybridEngine-LearningCore` ⇒ 那是个空壳目录）。
def _仓库根():
    """从本文件的所在目录往上找仓库根（含 `lessons/` 的那一级）。"""
    d = os.path.dirname(os.path.abspath(__file__))          # <仓库>\_build
    for _ in range(3):
        if os.path.isdir(os.path.join(d, "lessons")):
            return d
        d = os.path.dirname(d)
    return os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 兜底：不变

LC = _仓库根()
print("  仓库根：%s" % LC)
ANS = os.path.join(LC, "lessons", "answers")
env = dict(os.environ)
env["PYTHONIOENCODING"] = "utf-8"

# 每项：(课号, 错在哪, 期望被拒的检查, 错误答案源码)
错例 = [
    ("35", "是质数的循环边界少了个等号（9/25/49 会判成质数）", "是质数", '''\
def 是质数(n):
    if n < 2:
        return False
    d = 2
    while d * d < n:
        if n % d == 0:
            return False
        d += 1
    return True


def 分解质因数(n):
    if n < 2:
        return []
    出 = []
    d = 2
    while d * d <= n:
        while n % d == 0:
            出.append(d)
            n //= d
        d += 1
    if n > 1:
        出.append(n)
    return 出


def 约数们(n):
    if n <= 0:
        return []
    出 = []
    d = 1
    while d * d <= n:
        if n % d == 0:
            出.append(d)
            if n // d != d:
                出.append(n // d)
        d += 1
    return sorted(出)


def 约数个数(n):
    return len(约数们(n))
'''),
    ("35", "分解质因数忘了把最后剩下的质数接上（97 得到 []）", "分解质因数", '''\
def 是质数(n):
    if n < 2:
        return False
    d = 2
    while d * d <= n:
        if n % d == 0:
            return False
        d += 1
    return True


def 分解质因数(n):
    if n < 2:
        return []
    出 = []
    d = 2
    while d * d <= n:
        while n % d == 0:
            出.append(d)
            n //= d
        d += 1
    return 出


def 约数们(n):
    if n <= 0:
        return []
    出 = []
    d = 1
    while d * d <= n:
        if n % d == 0:
            出.append(d)
            if n // d != d:
                出.append(n // d)
        d += 1
    return sorted(出)


def 约数个数(n):
    return len(约数们(n))
'''),
    ("35", "约数们只数到 n-1（漏了 n 自己），约数个数跟着少一个", "约数们", '''\
def 是质数(n):
    if n < 2:
        return False
    d = 2
    while d * d <= n:
        if n % d == 0:
            return False
        d += 1
    return True


def 分解质因数(n):
    if n < 2:
        return []
    出 = []
    d = 2
    while d * d <= n:
        while n % d == 0:
            出.append(d)
            n //= d
        d += 1
    if n > 1:
        出.append(n)
    return 出


def 约数们(n):
    return [i for i in range(1, n)]


def 约数个数(n):
    return len(约数们(n))
'''),
    # ── 36 辗转相除 ──────────────────────────────────────────────
    ("36", "gcd 写成 while 甲 % 乙 != 0（最大公约数(5, 0) 直接崩）", "最大公约数", '''\
def 最大公约数(甲, 乙):
    甲, 乙 = abs(甲), abs(乙)
    while 甲 % 乙 != 0:
        甲, 乙 = 乙, 甲 % 乙
    return 乙


def 最小公倍数(甲, 乙):
    甲, 乙 = abs(甲), abs(乙)
    if 甲 == 0 or 乙 == 0:
        return 0
    return 甲 // 最大公约数(甲, 乙) * 乙


def 约分(分子, 分母):
    if 分母 == 0:
        return None
    公 = 最大公约数(分子, 分母)
    分子, 分母 = 分子 // 公, 分母 // 公
    if 分母 < 0:
        分子, 分母 = -分子, -分母
    return (分子, 分母)
'''),
    ("36", "gcd 用试除法硬试（结果对，但不是辗转相除）", "用了辗转相除", '''\
def 最大公约数(甲, 乙):
    甲, 乙 = abs(甲), abs(乙)
    if 甲 == 0 and 乙 == 0:
        return 0
    试 = min(甲, 乙) or max(甲, 乙)
    while 试 > 1:
        if 甲 % 试 == 0 and 乙 % 试 == 0:
            return 试
        试 -= 1
    return 试


def 最小公倍数(甲, 乙):
    甲, 乙 = abs(甲), abs(乙)
    if 甲 == 0 or 乙 == 0:
        return 0
    return 甲 // 最大公约数(甲, 乙) * 乙


def 约分(分子, 分母):
    if 分母 == 0:
        return None
    公 = 最大公约数(分子, 分母)
    分子, 分母 = 分子 // 公, 分母 // 公
    if 分母 < 0:
        分子, 分母 = -分子, -分母
    return (分子, 分母)
'''),
    ("36", "最小公倍数忘了取绝对值（(-4, 6) 得到 -12）", "最小公倍数", '''\
def 最大公约数(甲, 乙):
    甲, 乙 = abs(甲), abs(乙)
    while 乙:
        甲, 乙 = 乙, 甲 % 乙
    return 甲


def 最小公倍数(甲, 乙):
    if 甲 == 0 or 乙 == 0:
        return 0
    return 甲 * 乙 // 最大公约数(甲, 乙)


def 约分(分子, 分母):
    if 分母 == 0:
        return None
    公 = 最大公约数(分子, 分母)
    分子, 分母 = 分子 // 公, 分母 // 公
    if 分母 < 0:
        分子, 分母 = -分子, -分母
    return (分子, 分母)
'''),
    ("36", "约分没把分母的负号挪到分子（(-4, -6) 得到 (-2, -3)）", "约分", '''\
def 最大公约数(甲, 乙):
    甲, 乙 = abs(甲), abs(乙)
    while 乙:
        甲, 乙 = 乙, 甲 % 乙
    return 甲


def 最小公倍数(甲, 乙):
    甲, 乙 = abs(甲), abs(乙)
    if 甲 == 0 or 乙 == 0:
        return 0
    return 甲 // 最大公约数(甲, 乙) * 乙


def 约分(分子, 分母):
    if 分母 == 0:
        return None
    公 = 最大公约数(分子, 分母)
    return (分子 // 公, 分母 // 公)
'''),
    # ── 37 素数筛 ────────────────────────────────────────────────
    ("37", "标记表长度写成 [True] * n（筛质数(10) 抛 IndexError）", "筛质数", '''\
def 筛质数(n):
    if n < 2:
        return []
    标记 = [True] * n
    标记[0] = 标记[1] = False
    p = 2
    while p * p <= n:
        if 标记[p]:
            k = p * p
            while k <= n:
                标记[k] = False
                k += p
        p += 1
    return [i for i in range(n + 1) if 标记[i]]


def 质数个数(n):
    return len(筛质数(n))


def 最小质因数表(n):
    表 = [0] * (n + 1)
    for i in range(2, n + 1):
        if 表[i] == 0:
            j = i
            while j <= n:
                if 表[j] == 0:
                    表[j] = i
                j += i
    return 表
'''),
    ("37", "忘了把 0 和 1 标成非质数（筛质数(10) 得到 [0, 1, 2, 3, 5, 7]）", "筛质数", '''\
def 筛质数(n):
    if n < 2:
        return []
    标记 = [True] * (n + 1)
    p = 2
    while p * p <= n:
        if 标记[p]:
            k = p * p
            while k <= n:
                标记[k] = False
                k += p
        p += 1
    return [i for i in range(n + 1) if 标记[i]]


def 质数个数(n):
    return len(筛质数(n))


def 最小质因数表(n):
    表 = [0] * (n + 1)
    for i in range(2, n + 1):
        if 表[i] == 0:
            j = i
            while j <= n:
                if 表[j] == 0:
                    表[j] = i
                j += i
    return 表
'''),
    ("37", "质数个数用一个个试除（结果对，但 100000 要跑十几秒）", "质数个数", '''\
def 是质数(n):
    if n < 2:
        return False
    d = 2
    while d * d <= n:
        if n % d == 0:
            return False
        d += 1
    return True


def 质数个数(n):
    return sum(1 for i in range(2, n + 1) if 是质数(i))


def 筛质数(n):
    return [i for i in range(2, n + 1) if 是质数(i)]


def 最小质因数表(n):
    表 = [0] * (n + 1)
    for i in range(2, n + 1):
        if 表[i] == 0:
            j = i
            while j <= n:
                if 表[j] == 0:
                    表[j] = i
                j += i
    return 表
'''),
    ("37", "最小质因数表把下标本身当成了最小质因数（4 → 4）", "最小质因数表", '''\
def 筛质数(n):
    if n < 2:
        return []
    标记 = [True] * (n + 1)
    标记[0] = 标记[1] = False
    p = 2
    while p * p <= n:
        if 标记[p]:
            k = p * p
            while k <= n:
                标记[k] = False
                k += p
        p += 1
    return [i for i in range(n + 1) if 标记[i]]


def 质数个数(n):
    return len(筛质数(n))


def 最小质因数表(n):
    表 = list(range(n + 1))
    if n >= 0:
        表[0] = 0
    if n >= 1:
        表[1] = 0
    return 表
'''),
    # ── 38 二分查找 ──────────────────────────────────────────────
    ("38", "二分写成 while 左 < 右（[5] 找 5 返回 -1）", "二分查找", '''\
import bisect


def 二分查找(有序表, 目标):
    左, 右 = 0, len(有序表) - 1
    while 左 < 右:
        中 = (左 + 右) // 2
        if 有序表[中] == 目标:
            return 中
        if 有序表[中] < 目标:
            左 = 中 + 1
        else:
            右 = 中 - 1
    return -1


def 找第一个(有序表, 目标):
    得 = bisect.bisect_left(有序表, 目标)
    return 得 if 得 < len(有序表) and 有序表[得] == 目标 else -1


def 用bisect找(有序表, 目标):
    得 = bisect.bisect_left(有序表, 目标)
    return 得 if 得 < len(有序表) and 有序表[得] == 目标 else -1
'''),
    ("38", "二分用 .index() 偷懒（结果对，但不是二分）", "没偷懒", '''\
import bisect


def 二分查找(有序表, 目标):
    if 目标 in 有序表:
        return 有序表.index(目标)
    return -1


def 找第一个(有序表, 目标):
    得 = bisect.bisect_left(有序表, 目标)
    return 得 if 得 < len(有序表) and 有序表[得] == 目标 else -1


def 用bisect找(有序表, 目标):
    得 = bisect.bisect_left(有序表, 目标)
    return 得 if 得 < len(有序表) and 有序表[得] == 目标 else -1
'''),
    ("38", "找第一个 没往左继续找（返回的是任意一个位置）", "找第一个", '''\
import bisect


def 二分查找(有序表, 目标):
    左, 右 = 0, len(有序表) - 1
    while 左 <= 右:
        中 = (左 + 右) // 2
        if 有序表[中] == 目标:
            return 中
        if 有序表[中] < 目标:
            左 = 中 + 1
        else:
            右 = 中 - 1
    return -1


def 找第一个(有序表, 目标):
    return 二分查找(有序表, 目标)


def 用bisect找(有序表, 目标):
    得 = bisect.bisect_left(有序表, 目标)
    return 得 if 得 < len(有序表) and 有序表[得] == 目标 else -1
'''),
    # ── 39 二分答案 ──────────────────────────────────────────────
    ("39", "二分答案用一条条试（1 到 10 亿要试 10 亿次）", "二分答案", '''\
def 二分答案(下界, 上界, 可以吗):
    答案 = None
    for x in range(下界, 上界 + 1):
        if 可以吗(x):
            答案 = x
    return 答案


def 切绳子(绳长表, 要的段数):
    if not 绳长表 or 要的段数 <= 0:
        return 0
    答案 = 0
    for m in range(1, max(绳长表) + 1):
        if sum(绳 // m for 绳 in 绳长表) >= 要的段数:
            答案 = m
    return 答案


def 分巧克力(边长表, 人数):
    if not 边长表 or 人数 <= 0:
        return 0
    答案 = 0
    for m in range(1, max(边长表) + 1):
        if sum((s // m) ** 2 for s in 边长表) >= 人数:
            答案 = m
    return 答案
'''),
    ("39", "切绳子返回的是**最小**能行的长度（方向反了）", "切绳子", '''\
def 二分答案(下界, 上界, 可以吗):
    答案 = None
    while 下界 <= 上界:
        中 = (下界 + 上界) // 2
        if 可以吗(中):
            答案 = 中
            下界 = 中 + 1
        else:
            上界 = 中 - 1
    return 答案


def 切绳子(绳长表, 要的段数):
    if not 绳长表 or 要的段数 <= 0:
        return 0
    左, 右, 答案 = 1, max(绳长表), 0
    while 左 <= 右:
        中 = (左 + 右) // 2
        if sum(绳 // 中 for 绳 in 绳长表) >= 要的段数:
            答案 = 中
            右 = 中 - 1
        else:
            左 = 中 + 1
    return 答案


def 分巧克力(边长表, 人数):
    if not 边长表 or 人数 <= 0:
        return 0
    左, 右, 答案 = 1, max(边长表), 0
    while 左 <= 右:
        中 = (左 + 右) // 2
        if sum((s // 中) ** 2 for s in 边长表) >= 人数:
            答案 = 中
            左 = 中 + 1
        else:
            右 = 中 - 1
    return 答案
'''),
    ("39", "分巧克力按面积比算（(3,3) 分 4 个人得出 2）", "分巧克力", '''\
def 二分答案(下界, 上界, 可以吗):
    答案 = None
    while 下界 <= 上界:
        中 = (下界 + 上界) // 2
        if 可以吗(中):
            答案 = 中
            下界 = 中 + 1
        else:
            上界 = 中 - 1
    return 答案


def 切绳子(绳长表, 要的段数):
    if not 绳长表 or 要的段数 <= 0:
        return 0
    return 二分答案(1, max(绳长表),
                    lambda m: sum(绳 // m for 绳 in 绳长表) >= 要的段数) or 0


def 分巧克力(边长表, 人数):
    if not 边长表 or 人数 <= 0:
        return 0
    return 二分答案(1, max(边长表),
                    lambda m: sum(s * s for s in 边长表) // (m * m) >= 人数) or 0
'''),
    # ── 40 贪心 ──────────────────────────────────────────────────
    ("40", "贪心找零忘了先按面额从大到小排序（直接拿第一个）", "贪心找零", '''\
def 贪心找零(零钱表, 金额):
    剩下 = 金额
    张数 = 0
    for 面额 in 零钱表:
        张数 += 剩下 // 面额
        剩下 %= 面额
    return 张数 if 剩下 == 0 else -1


def 选中的区间(区间表):
    选 = []
    上一个结束 = None
    for 开始, 结束 in sorted(区间表, key=lambda x: x[1]):
        if 上一个结束 is None or 开始 >= 上一个结束:
            选.append((开始, 结束))
            上一个结束 = 结束
    return 选


def 区间调度(区间表):
    return len(选中的区间(区间表))
'''),
    ("40", "区间调度按**开始时间**排（[(1,10),(2,3),(3,4),(4,5)] 得出 1）", "区间调度", '''\
def 贪心找零(零钱表, 金额):
    剩下 = 金额
    张数 = 0
    for 面额 in sorted(零钱表, reverse=True):
        张数 += 剩下 // 面额
        剩下 %= 面额
    return 张数 if 剩下 == 0 else -1


def 选中的区间(区间表):
    选 = []
    上一个结束 = None
    for 开始, 结束 in sorted(区间表, key=lambda x: x[0]):
        if 上一个结束 is None or 开始 >= 上一个结束:
            选.append((开始, 结束))
            上一个结束 = 结束
    return 选


def 区间调度(区间表):
    return len(选中的区间(区间表))
'''),
    ("40", "区间调度要求严格大于（首尾相接 [1,2][2,3][3,4] 只选出 2 个）", "区间调度", '''\
def 贪心找零(零钱表, 金额):
    剩下 = 金额
    张数 = 0
    for 面额 in sorted(零钱表, reverse=True):
        张数 += 剩下 // 面额
        剩下 %= 面额
    return 张数 if 剩下 == 0 else -1


def 选中的区间(区间表):
    选 = []
    上一个结束 = None
    for 开始, 结束 in sorted(区间表, key=lambda x: x[1]):
        if 上一个结束 is None or 开始 > 上一个结束:
            选.append((开始, 结束))
            上一个结束 = 结束
    return 选


def 区间调度(区间表):
    return len(选中的区间(区间表))
'''),
    # ── 41 分治与归并 ────────────────────────────────────────────
    ("41", "合并两个有序表忘了接尾巴（[1,2]+[3,4,5] 丢了 5）", "合并两个有序表", '''\
def 合并两个有序表(左, 右):
    出 = []
    甲 = 乙 = 0
    while 甲 < len(左) and 乙 < len(右):
        if 左[甲] <= 右[乙]:
            出.append(左[甲])
            甲 += 1
        else:
            出.append(右[乙])
            乙 += 1
    return 出


def 归并排序(表):
    if len(表) <= 1:
        return list(表)
    中 = len(表) // 2
    return 合并两个有序表(归并排序(表[:中]), 归并排序(表[中:]))


def 是升序(表):
    return all(表[i - 1] <= 表[i] for i in range(1, len(表)))
'''),
    ("41", "归并排序直接用 sorted 交差（结果全对，但没学分治）", "用了归并", '''\
def 合并两个有序表(左, 右):
    出 = []
    甲 = 乙 = 0
    while 甲 < len(左) and 乙 < len(右):
        if 左[甲] <= 右[乙]:
            出.append(左[甲])
            甲 += 1
        else:
            出.append(右[乙])
            乙 += 1
    出.extend(左[甲:])
    出.extend(右[乙:])
    return 出


def 归并排序(表):
    return sorted(表)


def 是升序(表):
    return all(表[i - 1] <= 表[i] for i in range(1, len(表)))
'''),
    ("41", "归并排序会丢元素（合并没接尾巴）", "排序结果没丢元素", '''\
def 合并两个有序表(左, 右):
    出 = []
    甲 = 乙 = 0
    while 甲 < len(左) and 乙 < len(右):
        if 左[甲] <= 右[乙]:
            出.append(左[甲])
            甲 += 1
        else:
            出.append(右[乙])
            乙 += 1
    return 出


def 归并排序(表):
    if len(表) <= 1:
        return list(表)
    中 = len(表) // 2
    return 合并两个有序表(归并排序(表[:中]), 归并排序(表[中:]))


def 是升序(表):
    return all(表[i - 1] <= 表[i] for i in range(1, len(表)))
'''),
    # ── 42 快速排序 ──────────────────────────────────────────────
    ("42", "划分的比较写成 >= （左半出现了比基准大的元素）", "划分", '''\
def 划分(表, 左, 右):
    基准 = 表[右]
    界 = 左 - 1
    for i in range(左, 右):
        if 表[i] >= 基准:
            界 += 1
            表[界], 表[i] = 表[i], 表[界]
    表[界 + 1], 表[右] = 表[右], 表[界 + 1]
    return 界 + 1


def 快排(表, 左=0, 右=None):
    右 = len(表) - 1 if 右 is None else 右
    if 左 >= 右:
        return 表
    位 = 划分(表, 左, 右)
    快排(表, 左, 位 - 1)
    快排(表, 位 + 1, 右)
    return 表


def 三数取中(表, 左, 右):
    中 = (左 + 右) // 2
    三 = sorted([(表[左], 左), (表[中], 中), (表[右], 右)])
    return 三[1][1]
'''),
    ("42", "快排返回新表、原表没动（不是原地排序）", "快排", '''\
def 划分(表, 左, 右):
    基准 = 表[右]
    界 = 左 - 1
    for i in range(左, 右):
        if 表[i] <= 基准:
            界 += 1
            表[界], 表[i] = 表[i], 表[界]
    表[界 + 1], 表[右] = 表[右], 表[界 + 1]
    return 界 + 1


def 快排(表):
    出 = list(表)
    for i in range(1, len(出)):
        j = i
        while j > 0 and 出[j - 1] > 出[j]:
            出[j - 1], 出[j] = 出[j], 出[j - 1]
            j -= 1
    return 出


def 三数取中(表, 左, 右):
    中 = (左 + 右) // 2
    三 = sorted([(表[左], 左), (表[中], 中), (表[右], 右)])
    return 三[1][1]
'''),
    ("42", "快排内部用 list.sort()（结果对，但不是手写快排）", "不用sorted", '''\
def 划分(表, 左, 右):
    基准 = 表[右]
    界 = 左 - 1
    for i in range(左, 右):
        if 表[i] <= 基准:
            界 += 1
            表[界], 表[i] = 表[i], 表[界]
    表[界 + 1], 表[右] = 表[右], 表[界 + 1]
    return 界 + 1


def 快排(表, 左=0, 右=None):
    表.sort()
    return 表


def 三数取中(表, 左, 右):
    中 = (左 + 右) // 2
    三 = sorted([(表[左], 左), (表[中], 中), (表[右], 右)])
    return 三[1][1]
'''),
    # ── 43 栈与队列 ──────────────────────────────────────────────
    ("43", "括号匹配忘了最后判栈空不空（((( 返回 True）", "括号匹配", '''\
from collections import deque


def 括号匹配(文字):
    配 = {')': '(', ']': '[', '}': '{'}
    栈 = []
    for 字 in 文字:
        if 字 in '([{':
            栈.append(字)
        elif 字 in 配:
            if not 栈 or 栈.pop() != 配[字]:
                return False
    return True


def 逆波兰(式子):
    栈 = []
    for 块 in 式子.split():
        if 块 in ('+', '-', '*'):
            右 = 栈.pop()
            左 = 栈.pop()
            栈.append(左 + 右 if 块 == '+' else 左 - 右 if 块 == '-' else 左 * 右)
        else:
            栈.append(int(块))
    return 栈[0]


def 模拟排队(操作表):
    队 = deque()
    出队 = []
    for 动作, 值 in 操作表:
        if 动作 == '进':
            队.append(值)
        else:
            出队.append(队.popleft() if 队 else None)
    return 出队
'''),
    ("43", "逆波兰弹出的两个数弄反了（7 2 - 得到 -5）", "逆波兰", '''\
from collections import deque


def 括号匹配(文字):
    配 = {')': '(', ']': '[', '}': '{'}
    栈 = []
    for 字 in 文字:
        if 字 in '([{':
            栈.append(字)
        elif 字 in 配:
            if not 栈 or 栈.pop() != 配[字]:
                return False
    return not 栈


def 逆波兰(式子):
    栈 = []
    for 块 in 式子.split():
        if 块 in ('+', '-', '*'):
            甲 = 栈.pop()
            乙 = 栈.pop()
            栈.append(甲 + 乙 if 块 == '+' else 甲 - 乙 if 块 == '-' else 甲 * 乙)
        else:
            栈.append(int(块))
    return 栈[0]


def 模拟排队(操作表):
    队 = deque()
    出队 = []
    for 动作, 值 in 操作表:
        if 动作 == '进':
            队.append(值)
        else:
            出队.append(队.popleft() if 队 else None)
    return 出队
'''),
    ("43", "队列用 list.pop(0)（结果全对，但每次出队都是 O(n)）", "用了deque", '''\
def 括号匹配(文字):
    配 = {')': '(', ']': '[', '}': '{'}
    栈 = []
    for 字 in 文字:
        if 字 in '([{':
            栈.append(字)
        elif 字 in 配:
            if not 栈 or 栈.pop() != 配[字]:
                return False
    return not 栈


def 逆波兰(式子):
    栈 = []
    for 块 in 式子.split():
        if 块 in ('+', '-', '*'):
            右 = 栈.pop()
            左 = 栈.pop()
            栈.append(左 + 右 if 块 == '+' else 左 - 右 if 块 == '-' else 左 * 右)
        else:
            栈.append(int(块))
    return 栈[0]


def 模拟排队(操作表):
    队 = []
    出队 = []
    for 动作, 值 in 操作表:
        if 动作 == '进':
            队.append(值)
        else:
            出队.append(队.pop(0) if 队 else None)
    return 出队
'''),
    # ── 44 链表 ──────────────────────────────────────────────────
    ("44", "删除头节点时只改了局部变量（删了等于没删）", "删除", '''\
class 节点:
    def __init__(self, 值):
        self.值 = 值
        self.下一个 = None


def 建表(表):
    头 = None
    for 值 in reversed(表):
        新 = 节点(值)
        新.下一个 = 头
        头 = 新
    return 头


def 转列表(头):
    出 = []
    当前 = 头
    while 当前:
        出.append(当前.值)
        当前 = 当前.下一个
    return 出


def 插入(头, 位置, 值):
    新 = 节点(值)
    if 位置 <= 0 or 头 is None:
        新.下一个 = 头
        return 新
    前 = 头
    for _ in range(位置 - 1):
        if 前.下一个 is None:
            break
        前 = 前.下一个
    新.下一个 = 前.下一个
    前.下一个 = 新
    return 头


def 删除(头, 值):
    if 头 is None:
        return None
    当前 = 头
    if 当前.值 == 值:
        当前 = 当前.下一个          # ⚠ 以为这样就把头删掉了
        return 头                   # ⚠ 交出去的还是**老的头**
    前 = 当前
    while 前.下一个:
        if 前.下一个.值 == 值:
            前.下一个 = 前.下一个.下一个
            return 头
        前 = 前.下一个
    return 头
'''),
    ("44", "转列表写成 while 当前.下一个:（漏掉最后一个节点）", "建表和转列表", '''\
class 节点:
    def __init__(self, 值):
        self.值 = 值
        self.下一个 = None


def 建表(表):
    头 = None
    for 值 in reversed(表):
        新 = 节点(值)
        新.下一个 = 头
        头 = 新
    return 头


def 转列表(头):
    出 = []
    当前 = 头
    while 当前 and 当前.下一个:
        出.append(当前.值)
        当前 = 当前.下一个
    return 出


def 插入(头, 位置, 值):
    新 = 节点(值)
    if 位置 <= 0 or 头 is None:
        新.下一个 = 头
        return 新
    前 = 头
    for _ in range(位置 - 1):
        if 前.下一个 is None:
            break
        前 = 前.下一个
    新.下一个 = 前.下一个
    前.下一个 = 新
    return 头


def 删除(头, 值):
    if 头 is None:
        return None
    if 头.值 == 值:
        return 头.下一个
    前 = 头
    while 前.下一个:
        if 前.下一个.值 == 值:
            前.下一个 = 前.下一个.下一个
            return 头
        前 = 前.下一个
    return 头
'''),
    ("44", "插入没有单独处理位置 0（把新节点插到了头后面）", "插入", '''\
class 节点:
    def __init__(self, 值):
        self.值 = 值
        self.下一个 = None


def 建表(表):
    头 = None
    for 值 in reversed(表):
        新 = 节点(值)
        新.下一个 = 头
        头 = 新
    return 头


def 转列表(头):
    出 = []
    当前 = 头
    while 当前:
        出.append(当前.值)
        当前 = 当前.下一个
    return 出


def 插入(头, 位置, 值):
    新 = 节点(值)
    if 头 is None:
        新.下一个 = None
        return 新
    前 = 头
    for _ in range(位置 - 1):
        if 前.下一个 is None:
            break
        前 = 前.下一个
    新.下一个 = 前.下一个
    前.下一个 = 新
    return 头


def 删除(头, 值):
    if 头 is None:
        return None
    if 头.值 == 值:
        return 头.下一个
    前 = 头
    while 前.下一个:
        if 前.下一个.值 == 值:
            前.下一个 = 前.下一个.下一个
            return 头
        前 = 前.下一个
    return 头
'''),
    # ── 45 树与遍历 ──────────────────────────────────────────────
    ("45", "前序没判空（前序(None) 直接 AttributeError）", "前中后序", '''\
from collections import deque


class 树节点:
    def __init__(self, 值):
        self.值 = 值
        self.左 = None
        self.右 = None


def 前序(根):
    return [根.值] + 前序(根.左) + 前序(根.右)


def 中序(根):
    if 根 is None:
        return []
    return 中序(根.左) + [根.值] + 中序(根.右)


def 后序(根):
    if 根 is None:
        return []
    return 后序(根.左) + 后序(根.右) + [根.值]


def 层序(根):
    if 根 is None:
        return []
    队 = deque([根])
    出 = []
    while 队:
        点 = 队.popleft()
        出.append(点.值)
        if 点.左:
            队.append(点.左)
        if 点.右:
            队.append(点.右)
    return 出


def 高度(根):
    if 根 is None:
        return 0
    return 1 + max(高度(根.左), 高度(根.右))
'''),
    ("45", "中序写成了根左右（跟前序一模一样）", "前中后序", '''\
from collections import deque


class 树节点:
    def __init__(self, 值):
        self.值 = 值
        self.左 = None
        self.右 = None


def 前序(根):
    if 根 is None:
        return []
    return [根.值] + 前序(根.左) + 前序(根.右)


def 中序(根):
    if 根 is None:
        return []
    return [根.值] + 中序(根.左) + 中序(根.右)


def 后序(根):
    if 根 is None:
        return []
    return 后序(根.左) + 后序(根.右) + [根.值]


def 层序(根):
    if 根 is None:
        return []
    队 = deque([根])
    出 = []
    while 队:
        点 = 队.popleft()
        出.append(点.值)
        if 点.左:
            队.append(点.左)
        if 点.右:
            队.append(点.右)
    return 出


def 高度(根):
    if 根 is None:
        return 0
    return 1 + max(高度(根.左), 高度(根.右))
'''),
    ("45", "层序其实是深搜（缺左孩子的树会塌成前序）", "层序", '''\
from collections import deque


class 树节点:
    def __init__(self, 值):
        self.值 = 值
        self.左 = None
        self.右 = None


def 前序(根):
    if 根 is None:
        return []
    return [根.值] + 前序(根.左) + 前序(根.右)


def 中序(根):
    if 根 is None:
        return []
    return 中序(根.左) + [根.值] + 中序(根.右)


def 后序(根):
    if 根 is None:
        return []
    return 后序(根.左) + 后序(根.右) + [根.值]


def 层序(根):
    出 = []
    队 = [根]
    while 队:
        点 = 队.pop()
        if 点 is None:
            continue
        出.append(点.值)
        队.append(点.右)
        队.append(点.左)
    return 出


def 高度(根):
    if 根 is None:
        return 0
    return 1 + max(高度(根.左), 高度(根.右))
'''),
    # ── 46 搜索与 DP ─────────────────────────────────────────────
    ("46", "求最短步数用了深搜（陷阱迷宫返回 12 而不是 4）", "最短步数", '''\
from collections import deque


def 最短步数(迷宫, 起点, 终点):
    if not 迷宫 or not 迷宫[0]:
        return -1
    行数, 列数 = len(迷宫), len(迷宫[0])
    见过 = set()

    def 深搜(r, c, 步):
        if not (0 <= r < 行数 and 0 <= c < 列数) or 迷宫[r][c] == '#' or (r, c) in 见过:
            return None
        见过.add((r, c))
        if (r, c) == 终点:
            return 步
        for dr, dc in ((1, 0), (0, 1), (-1, 0), (0, -1)):
            得 = 深搜(r + dr, c + dc, 步 + 1)
            if 得 is not None:
                return 得
        return None

    得 = 深搜(起点[0], 起点[1], 0)
    return -1 if 得 is None else 得


def 走法数(台阶数):
    a, b = 1, 1
    for _ in range(台阶数):
        a, b = b, a + b
    return a


def 装背包(重量表, 价值表, 容量):
    dp = [0] * (容量 + 1)
    for i in range(len(重量表)):
        for x in range(容量, 重量表[i] - 1, -1):
            dp[x] = max(dp[x], dp[x - 重量表[i]] + 价值表[i])
    return dp[容量]
'''),
    ("46", "装背包正着扫容量（同一件被装两次：(2,3) 价值 3,4 容量 4 得出 6）", "装背包", '''\
from collections import deque


def 最短步数(迷宫, 起点, 终点):
    if not 迷宫 or not 迷宫[0]:
        return -1
    行数, 列数 = len(迷宫), len(迷宫[0])
    队 = deque([(起点, 0)])
    见过 = {起点}
    while 队:
        (r, c), 步 = 队.popleft()
        if (r, c) == 终点:
            return 步
        for dr, dc in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nr, nc = r + dr, c + dc
            if 0 <= nr < 行数 and 0 <= nc < 列数 and 迷宫[nr][nc] != '#' and (nr, nc) not in 见过:
                见过.add((nr, nc))
                队.append(((nr, nc), 步 + 1))
    return -1


def 走法数(台阶数):
    a, b = 1, 1
    for _ in range(台阶数):
        a, b = b, a + b
    return a


def 装背包(重量表, 价值表, 容量):
    dp = [0] * (容量 + 1)
    for i in range(len(重量表)):
        for x in range(重量表[i], 容量 + 1):
            dp[x] = max(dp[x], dp[x - 重量表[i]] + 价值表[i])
    return dp[容量]
'''),
    ("46", "走法数的初始值写成 0（走法数(0) 返回 0）", "走法数", '''\
from collections import deque


def 最短步数(迷宫, 起点, 终点):
    if not 迷宫 or not 迷宫[0]:
        return -1
    行数, 列数 = len(迷宫), len(迷宫[0])
    队 = deque([(起点, 0)])
    见过 = {起点}
    while 队:
        (r, c), 步 = 队.popleft()
        if (r, c) == 终点:
            return 步
        for dr, dc in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nr, nc = r + dr, c + dc
            if 0 <= nr < 行数 and 0 <= nc < 列数 and 迷宫[nr][nc] != '#' and (nr, nc) not in 见过:
                见过.add((nr, nc))
                队.append(((nr, nc), 步 + 1))
    return -1


def 走法数(台阶数):
    a, b = 0, 1
    for _ in range(台阶数):
        a, b = b, a + b
    return a


def 装背包(重量表, 价值表, 容量):
    dp = [0] * (容量 + 1)
    for i in range(len(重量表)):
        for x in range(容量, 重量表[i] - 1, -1):
            dp[x] = max(dp[x], dp[x - 重量表[i]] + 价值表[i])
    return dp[容量]
'''),
]

筛选 = sys.argv[1:]


def 跑一课(编, 代码):
    """把错答案写进答题文件，跑一次 CLI，返回输出。

    ⚠ 重试是必须的：`lessons/answers/` 是**共享**目录，别的课线验证脚本会把它整个清空
      （`_verify_py_lessons.py` 就干这事）。被清空时 CLI 会**重建模板**并只打印一行提示、
      **不打报告** —— 看起来就像"检查没报错"，实际是根本没跑。实测撞到过 3 次。
    """
    文件 = None
    for _ in range(4):
        subprocess.run([sys.executable, "-m", "hybridengine_light", "检查", 编],
                       cwd=LC, env=env, capture_output=True, timeout=600)
        候选 = [f for f in os.listdir(ANS) if f.startswith(编)]
        if not 候选:
            continue
        文件 = os.path.join(ANS, 候选[0])
        with open(文件, "w", encoding="utf-8") as f:
            f.write(代码)
        r = subprocess.run([sys.executable, "-m", "hybridengine_light", "检查", 编],
                           cwd=LC, env=env, capture_output=True, timeout=600)
        out = (r.stdout + r.stderr).decode("utf-8", "replace")
        if "√" in out or "×" in out:
            return out
    return out if 文件 else None


好 = 0
总 = 0
for 编, 错在哪, 期望, 代码 in 错例:
    if 筛选 and 编 not in 筛选:
        continue
    总 += 1
    out = 跑一课(编, 代码)
    if out is None:
        print("  ! %s: 建不出答题文件" % 编)
        continue
    全过 = "全部通过" in out
    抓到 = ("× " + 期望) in out
    对 = (not 全过) and 抓到
    好 += 1 if 对 else 0
    print("  %s %s  %s" % ("OK " if 对 else "XX ", 编, 错在哪))
    if 全过:
        print("       **错误答案被判成通过了** —— 这条检查是摆设")
    elif not 抓到:
        print("       报错了，但「%s」没有 ×：" % 期望)
        for l in out.splitlines():
            if l.strip().startswith("×"):
                print("         " + l.strip()[:96])

print()
print("  == %d/%d 个错误答案被正确拒绝 ==" % (好, 总))
sys.exit(0 if 好 == 总 and 总 else 1)
