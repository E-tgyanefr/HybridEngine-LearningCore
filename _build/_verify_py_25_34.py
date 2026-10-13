# 25~34 课的验证脚本 —— **先证明不冤枉对的，再证明会拒绝错的**。
#
# ## 为什么两段都要跑（这是第四次踩同一个坑）
#   "参考答案全过" 只证明检查**不冤枉对的**；
#   如果检查永远返回"通过"，它同样能"参考答案全过" —— 那样的检查对学员毫无价值。
#   ⇒ 必须再喂**错误答案**，证明它真的会拒绝。
#
# ## 与 _verify_py_neg2.py 的三个区别
#   ① 只动 25~34 的答题文件 —— **不 rmtree lessons/answers**：
#      第 18~24 课的作者可能同时在跑他自己的验证，整个删目录会互相踩。
#   ② 先删掉我这几课的答题文件，重新走一遍「首次检查自动生成模板」，
#      确认学员第一次跑命令时**真的能拿到模板**（不是空文件）。
#   ③ 结尾把参考答案放回原位，不留下一个"错答案"占着答题文件。
#
# 用法：  python _build/_verify_py_25_34.py [编号…]     （不给编号＝全部 25~34）
import os
import shutil
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
S = os.path.join(LC, "lessons")
ANS = os.path.join(S, "answers")
SOL = os.path.join(S, "solutions")
env = dict(os.environ)
env["PYTHONIOENCODING"] = "utf-8"
env["PYTHONUNBUFFERED"] = "1"

# ── 错答案表：(编号, 代码, 这条错在哪, 报告里必须出现的字样) ──────────────
# ⚠ 「必须出现的字样」是**关键的**：只证明"没全过"不够 ——
#   可能是别的地方崩了，而真正要抓的那件事没被抓到。
错例 = [
    ("25", "次数 = 0\n\n\ndef 加一():\n    次数 += 1\n    return 次数\n\n\n"
           "def 加几个(n):\n    global 次数\n    次数 += n\n    return 次数\n\n\n"
           "def 双倍(n):\n    return n * 2\n\n\ndef 现在几次():\n    return 次数\n",
     "想改全局却忘了写 global", "global 次数"),
    ("25", "次数 = 0\n\n\ndef 加一():\n    return 次数 + 1\n\n\n"
           "def 加几个(n):\n    global 次数\n    次数 += n\n    return 次数\n\n\n"
           "def 双倍(n):\n    return n * 2\n\n\ndef 现在几次():\n    return 次数\n",
     "只 return 次数+1，没真的改全局", "还是 0"),
    ("25", "次数 = 0\n\n\ndef 加一():\n    global 次数\n    次数 += 1\n    return 次数\n\n\n"
           "def 加几个(n):\n    global 次数\n    for _ in range(n):\n        次数 += 1\n    return 次数\n\n\n"
           "def 双倍(n):\n    return n * 2\n\n\ndef 现在几次():\n    return 次数\n",
     "用 range(n) 循环 ⇒ n 是负数时加不上", "加几个(-3)"),
    ("25", "次数 = 0\n\n\ndef 加一():\n    global 次数\n    次数 += 1\n    return 次数\n\n\n"
           "def 加几个(n):\n    global 次数\n    次数 += n\n    return 次数\n\n\n"
           "def 双倍(n):\n    global 次数\n    次数 += n\n    return 次数 * 0 + n * 2\n\n\n"
           "def 现在几次():\n    return 次数\n",
     "双倍偷偷碰了全局", "不碰全局"),
    ("26", "def 加进去(表, 东西):\n    表 = 表 + [东西]\n\n\n"
           "def 清空(表):\n    表.clear()\n\n\n"
           "def 复制一份再加(表, 东西):\n    新的 = 表[:]\n    新的.append(东西)\n"
           "    return 新的\n\n\ndef 排好序(表):\n    return sorted(表)\n\n\n"
           "def 塞进元组(元组, 东西):\n    return 元组 + (东西,)\n",
     "加进去写成 表 = 表 + [东西]（没改到外面）", "应当是 1 个"),
    ("26", "def 加进去(表, 东西):\n    表.append(东西)\n\n\n"
           "def 清空(表):\n    表 = []\n\n\n"
           "def 复制一份再加(表, 东西):\n    新的 = 表[:]\n    新的.append(东西)\n"
           "    return 新的\n\n\ndef 排好序(表):\n    return sorted(表)\n\n\n"
           "def 塞进元组(元组, 东西):\n    return 元组 + (东西,)\n",
     "清空写成 表 = []（外面那个没空）", "应当是 []"),
    ("26", "def 加进去(表, 东西):\n    表.append(东西)\n\n\n"
           "def 清空(表):\n    表.clear()\n\n\n"
           "def 复制一份再加(表, 东西):\n    表.append(东西)\n    return 表\n\n\n"
           "def 排好序(表):\n    return sorted(表)\n\n\n"
           "def 塞进元组(元组, 东西):\n    return 元组 + (东西,)\n",
     "复制一份再加 把传进来的表改了", "传进来的表"),
    ("26", "def 加进去(表, 东西):\n    表.append(东西)\n\n\n"
           "def 清空(表):\n    表.clear()\n\n\n"
           "def 复制一份再加(表, 东西):\n    新的 = 表[:]\n    新的.append(东西)\n"
           "    return 新的\n\n\ndef 排好序(表):\n    表.sort()\n    return 表\n\n\n"
           "def 塞进元组(元组, 东西):\n    return 元组 + (东西,)\n",
     "排好序 用了 .sort() ⇒ 原表被就地打乱", "应当一点不动"),
    ("26", "def 加进去(表, 东西):\n    表.append(东西)\n\n\n"
           "def 清空(表):\n    表.clear()\n\n\n"
           "def 复制一份再加(表, 东西):\n    新的 = 表[:]\n    新的.append(东西)\n"
           "    return 新的\n\n\ndef 排好序(表):\n    return sorted(表)\n\n\n"
           "def 塞进元组(元组, 东西):\n    元组.append(东西)\n    return 元组\n",
     "对元组用 append（元组改不了）", "元组不能 append"),
    ("27", "def 阶乘(n):\n    return n * 阶乘(n - 1)\n\n\n"
           "def 斐波那契(n):\n    if n < 2:\n        return n\n"
           "    return 斐波那契(n - 1) + 斐波那契(n - 2)\n\n\n"
           "def 汉诺塔(盘子数):\n    if 盘子数 == 0:\n        return 0\n"
           "    return 汉诺塔(盘子数 - 1) * 2 + 1\n\n\n"
           "def 递归求和(数字们):\n    if not 数字们:\n        return 0\n"
           "    return 数字们[0] + 递归求和(数字们[1:])\n",
     "阶乘忘了基准情形 ⇒ 无限递归", "RecursionError"),
    ("27", "def 阶乘(n):\n    if n == 0:\n        return 1\n    return n * 阶乘(n - 1)\n\n\n"
           "def 斐波那契(n):\n    if n < 2:\n        return 1\n"
           "    return 斐波那契(n - 1) + 斐波那契(n - 2)\n\n\n"
           "def 汉诺塔(盘子数):\n    if 盘子数 == 0:\n        return 0\n"
           "    return 汉诺塔(盘子数 - 1) * 2 + 1\n\n\n"
           "def 递归求和(数字们):\n    if not 数字们:\n        return 0\n"
           "    return 数字们[0] + 递归求和(数字们[1:])\n",
     "斐波那契基准情形写错（fib(0) 给了 1）", "应当是 0"),
    ("27", "def 阶乘(n):\n    if n == 0:\n        return 1\n    return n * 阶乘(n - 1)\n\n\n"
           "def 斐波那契(n):\n    if n < 2:\n        return n\n"
           "    return 斐波那契(n - 1) + 斐波那契(n - 2)\n\n\n"
           "def 汉诺塔(盘子数):\n    return 2 ** 盘子数 - 1\n\n\n"
           "def 递归求和(数字们):\n    if not 数字们:\n        return 0\n"
           "    return 数字们[0] + 递归求和(数字们[1:])\n",
     "汉诺塔用公式蒙对（没递归）", "没有调用自己"),
    ("27", "def 阶乘(n):\n    if n == 0:\n        return 1\n    return n * 阶乘(n - 1)\n\n\n"
           "def 斐波那契(n):\n    if n < 2:\n        return n\n"
           "    return 斐波那契(n - 1) + 斐波那契(n - 2)\n\n\n"
           "def 汉诺塔(盘子数):\n    if 盘子数 == 0:\n        return 0\n"
           "    return 汉诺塔(盘子数 - 1) * 2 + 1\n\n\n"
           "def 递归求和(数字们):\n    if not 数字们:\n        return None\n"
           "    return 数字们[0] + 递归求和(数字们[1:])\n",
     "递归求和 空列表返回 None", "应当是 0"),
    ("27", "def 阶乘(n):\n    if n == 0:\n        return 0\n    return n * 阶乘(n - 1)\n\n\n"
           "def 斐波那契(n):\n    if n < 2:\n        return n\n"
           "    return 斐波那契(n - 1) + 斐波那契(n - 2)\n\n\n"
           "def 汉诺塔(盘子数):\n    if 盘子数 == 0:\n        return 0\n"
           "    return 汉诺塔(盘子数 - 1) * 2 + 1\n\n\n"
           "def 递归求和(数字们):\n    if not 数字们:\n        return 0\n"
           "    return 数字们[0] + 递归求和(数字们[1:])\n",
     "0! 写成了 0", "应当是 1"),
    ("28", "def 爬楼梯(n):\n    表 = [1, 1]\n    for i in range(2, n + 1):\n"
           "        表.append(表[i - 1] + 表[i - 2])\n    return 表[n]\n\n\n"
           "def 跨三级(n):\n    表 = [1, 1, 2]\n    for i in range(3, n + 1):\n"
           "        表.append(表[i - 1] + 表[i - 2] + 表[i - 3])\n    return 表[n]\n\n\n"
           "def 斐波那契表(n):\n    if n < 2:\n        return n\n"
           "    return 斐波那契表(n - 1) + 斐波那契表(n - 2)\n\n\n"
           "def 最大子段和(数字们):\n    if not 数字们:\n        return 0\n"
           "    表 = [数字们[0]]\n    for i in range(1, len(数字们)):\n"
           "        表.append(max(表[i - 1] + 数字们[i], 数字们[i]))\n    return max(表)\n",
     "斐波那契表 用了递归 ⇒ n=200 超时", "跑了 3 秒还没算完"),
    ("28", "def 爬楼梯(n):\n    表 = [0, 1]\n    for i in range(2, n + 1):\n"
           "        表.append(表[i - 1] + 表[i - 2])\n    return 表[n]\n\n\n"
           "def 跨三级(n):\n    表 = [1, 1, 2]\n    for i in range(3, n + 1):\n"
           "        表.append(表[i - 1] + 表[i - 2] + 表[i - 3])\n    return 表[n]\n\n\n"
           "def 斐波那契表(n):\n    表 = [0, 1]\n    for i in range(2, n + 1):\n"
           "        表.append(表[i - 1] + 表[i - 2])\n    return 表[n]\n\n\n"
           "def 最大子段和(数字们):\n    if not 数字们:\n        return 0\n"
           "    表 = [数字们[0]]\n    for i in range(1, len(数字们)):\n"
           "        表.append(max(表[i - 1] + 数字们[i], 数字们[i]))\n    return max(表)\n",
     "爬楼梯起点写成 [0, 1]（错位一格）", "爬楼梯(0) 应当是 1"),
    ("28", "def 爬楼梯(n):\n    表 = [1, 1]\n    for i in range(2, n + 1):\n"
           "        表.append(表[i - 1] + 表[i - 2])\n    return 表[n]\n\n\n"
           "def 跨三级(n):\n    表 = [1, 1, 2]\n    for i in range(3, n + 1):\n"
           "        表.append(表[i - 1] + 表[i - 2])\n    return 表[n]\n\n\n"
           "def 斐波那契表(n):\n    表 = [0, 1]\n    for i in range(2, n + 1):\n"
           "        表.append(表[i - 1] + 表[i - 2])\n    return 表[n]\n\n\n"
           "def 最大子段和(数字们):\n    if not 数字们:\n        return 0\n"
           "    表 = [数字们[0]]\n    for i in range(1, len(数字们)):\n"
           "        表.append(max(表[i - 1] + 数字们[i], 数字们[i]))\n    return max(表)\n",
     "跨三级 只按前两格推（漏了跨 3 级）", "应当是 4"),
    ("28", "def 爬楼梯(n):\n    表 = [1, 1]\n    for i in range(2, n + 1):\n"
           "        表.append(表[i - 1] + 表[i - 2])\n    return 表[n]\n\n\n"
           "def 跨三级(n):\n    表 = [1, 1, 2]\n    for i in range(3, n + 1):\n"
           "        表.append(表[i - 1] + 表[i - 2] + 表[i - 3])\n    return 表[n]\n\n\n"
           "def 斐波那契表(n):\n    表 = [0, 1]\n    for i in range(2, n + 1):\n"
           "        表.append(表[i - 1] + 表[i - 2])\n    return 表[n]\n\n\n"
           "def 最大子段和(数字们):\n    if not 数字们:\n        return 0\n"
           "    表 = [数字们[0]]\n    for i in range(1, len(数字们)):\n"
           "        表.append(max(表[i - 1] + 数字们[i], 0))\n    return max(表)\n",
     "最大子段和把负数那一段当成 0", "应当是 -1"),
    ("28", "def 爬楼梯(n):\n    表 = [1, 1]\n    for i in range(2, n + 1):\n"
           "        表.append(表[i - 1] + 表[i - 2])\n    return 表[n]\n\n\n"
           "def 跨三级(n):\n    表 = [1, 1, 2]\n    for i in range(3, n + 1):\n"
           "        表.append(表[i - 1] + 表[i - 2] + 表[i - 3])\n    return 表[n]\n\n\n"
           "def 斐波那契表(n):\n    表 = [0] * n\n    for i in range(2, n + 1):\n"
           "        表[i] = 表[i - 1] + 表[i - 2]\n    return 表[n]\n\n\n"
           "def 最大子段和(数字们):\n    if not 数字们:\n        return 0\n"
           "    表 = [数字们[0]]\n    for i in range(1, len(数字们)):\n"
           "        表.append(max(表[i - 1] + 数字们[i], 数字们[i]))\n    return max(表)\n",
     "表开小了（[0] * n 少一格）", "IndexError"),
    ("29", "def 选择排序(表):\n    return sorted(表)\n\n\n"
           "def 冒泡排序(表):\n    n = len(表)\n    for i in range(n - 1):\n"
           "        for j in range(n - 1 - i):\n            if 表[j] > 表[j + 1]:\n"
           "                表[j], 表[j + 1] = 表[j + 1], 表[j]\n    return 表\n\n\n"
           "def 插入排序(表):\n    for i in range(1, len(表)):\n        要插的 = 表[i]\n"
           "        j = i - 1\n        while j >= 0 and 表[j] > 要插的:\n"
           "            表[j + 1] = 表[j]\n            j -= 1\n"
           "        表[j + 1] = 要插的\n    return 表\n\n\n"
           "def 是排好的(表):\n    for i in range(len(表) - 1):\n"
           "        if 表[i] > 表[i + 1]:\n            return False\n    return True\n",
     "选择排序偷用 sorted（没手写）", "手写"),
    ("29", "def 选择排序(表):\n    n = len(表)\n    for i in range(n - 1):\n"
           "        最小 = i\n        for j in range(i + 1, n):\n"
           "            if 表[j] < 表[最小]:\n                最小 = j\n"
           "        表[i], 表[最小] = 表[最小], 表[i]\n    return 表\n\n\n"
           "def 冒泡排序(表):\n    n = len(表)\n    for i in range(n - 1):\n"
           "        for j in range(n):\n            if 表[j] > 表[j + 1]:\n"
           "                表[j], 表[j + 1] = 表[j + 1], 表[j]\n    return 表\n\n\n"
           "def 插入排序(表):\n    for i in range(1, len(表)):\n        要插的 = 表[i]\n"
           "        j = i - 1\n        while j >= 0 and 表[j] > 要插的:\n"
           "            表[j + 1] = 表[j]\n            j -= 1\n"
           "        表[j + 1] = 要插的\n    return 表\n\n\n"
           "def 是排好的(表):\n    for i in range(len(表) - 1):\n"
           "        if 表[i] > 表[i + 1]:\n            return False\n    return True\n",
     "冒泡内层写成 range(n) ⇒ 越界", "IndexError"),
    ("29", "def 选择排序(表):\n    n = len(表)\n    for i in range(n - 1):\n"
           "        最小 = i\n        for j in range(i + 1, n):\n"
           "            if 表[j] < 表[最小]:\n                最小 = j\n"
           "        表[i], 表[最小] = 表[最小], 表[i]\n    return 表\n\n\n"
           "def 冒泡排序(表):\n    n = len(表)\n    for i in range(n - 1):\n"
           "        for j in range(n - 1 - i):\n            if 表[j] > 表[j + 1]:\n"
           "                表[j] = 表[j + 1]\n                表[j + 1] = 表[j]\n"
           "    return 表\n\n\n"
           "def 插入排序(表):\n    for i in range(1, len(表)):\n        要插的 = 表[i]\n"
           "        j = i - 1\n        while j >= 0 and 表[j] > 要插的:\n"
           "            表[j + 1] = 表[j]\n            j -= 1\n"
           "        表[j + 1] = 要插的\n    return 表\n\n\n"
           "def 是排好的(表):\n    for i in range(len(表) - 1):\n"
           "        if 表[i] > 表[i + 1]:\n            return False\n    return True\n",
     "交换分两步写 ⇒ 两个格子变成同一个值", "不能丢也不能多"),
    ("29", "def 选择排序(表):\n    n = len(表)\n    for i in range(n - 1):\n"
           "        最小 = i\n        for j in range(i + 1, n):\n"
           "            if 表[j] < 表[最小]:\n                最小 = j\n"
           "        表[i], 表[最小] = 表[最小], 表[i]\n    return 表\n\n\n"
           "def 冒泡排序(表):\n    n = len(表)\n    for i in range(n - 1):\n"
           "        for j in range(n - 1 - i):\n            if 表[j] > 表[j + 1]:\n"
           "                表[j], 表[j + 1] = 表[j + 1], 表[j]\n    return 表\n\n\n"
           "def 插入排序(表):\n    for i in range(1, len(表)):\n        要插的 = 表[i]\n"
           "        j = i - 1\n        while 表[j] > 要插的:\n"
           "            表[j + 1] = 表[j]\n            j -= 1\n"
           "        表[j + 1] = 要插的\n    return 表\n\n\n"
           "def 是排好的(表):\n    for i in range(len(表) - 1):\n"
           "        if 表[i] > 表[i + 1]:\n            return False\n    return True\n",
     "插入排序漏了 j >= 0（表[-1] 不报错）", "IndexError"),
    ("29", "def 选择排序(表):\n    n = len(表)\n    for i in range(n - 1):\n"
           "        最小 = i\n        for j in range(i + 1, n):\n"
           "            if 表[j] < 表[最小]:\n                最小 = j\n"
           "        表[i], 表[最小] = 表[最小], 表[i]\n    return 表\n\n\n"
           "def 冒泡排序(表):\n    n = len(表)\n    for i in range(n - 1):\n"
           "        for j in range(n - 1 - i):\n            if 表[j] > 表[j + 1]:\n"
           "                表[j], 表[j + 1] = 表[j + 1], 表[j]\n    return 表\n\n\n"
           "def 插入排序(表):\n    for i in range(1, len(表)):\n        要插的 = 表[i]\n"
           "        j = i - 1\n        while j >= 0 and 表[j] > 要插的:\n"
           "            表[j + 1] = 表[j]\n            j -= 1\n"
           "        表[j + 1] = 要插的\n    return 表\n\n\n"
           "def 是排好的(表):\n    for i in range(len(表) - 1):\n"
           "        if 表[i] >= 表[i + 1]:\n            return False\n    return True\n",
     "是排好的 用了 >= ⇒ (2,2) 被判成没排好", "应当是 True"),
    ("30", "def 成绩排名(名单):\n    名单.sort(key=lambda 对: 对[1], reverse=True)\n\n\n"
           "def 按长度排(文字们):\n    return sorted(文字们, key=len)\n\n\n"
           "def 给名次(名单):\n    结果 = []\n"
           "    for i, (姓名, 分数) in enumerate(名单):\n"
           "        if i > 0 and 分数 == 名单[i - 1][1]:\n            名次 = 结果[-1][2]\n"
           "        else:\n            名次 = i + 1\n"
           "        结果.append((姓名, 分数, 名次))\n    return 结果\n",
     "把 sort() 的 None 当结果收下了", "NoneType"),
    ("30", "def 成绩排名(名单):\n    return sorted(名单, key=lambda 对: 对[1])\n\n\n"
           "def 按长度排(文字们):\n    return sorted(文字们, key=len)\n\n\n"
           "def 给名次(名单):\n    结果 = []\n"
           "    for i, (姓名, 分数) in enumerate(名单):\n"
           "        if i > 0 and 分数 == 名单[i - 1][1]:\n            名次 = 结果[-1][2]\n"
           "        else:\n            名次 = i + 1\n"
           "        结果.append((姓名, 分数, 名次))\n    return 结果\n",
     "成绩排名 忘了 reverse（变成从低到高）", "分数高的要在前面"),
    ("30", "def 成绩排名(名单):\n    排好的 = sorted(名单, key=lambda 对: 对[1])\n"
           "    排好的.reverse()\n    return 排好的\n\n\n"
           "def 按长度排(文字们):\n    return sorted(文字们, key=len)\n\n\n"
           "def 给名次(名单):\n    结果 = []\n"
           "    for i, (姓名, 分数) in enumerate(名单):\n"
           "        if i > 0 and 分数 == 名单[i - 1][1]:\n            名次 = 结果[-1][2]\n"
           "        else:\n            名次 = i + 1\n"
           "        结果.append((姓名, 分数, 名次))\n    return 结果\n",
     "升序后整体 reverse ⇒ 同分被翻乱（不稳定）", "同分的顺序被打乱"),
    ("30", "def 成绩排名(名单):\n"
           "    return sorted(名单, key=lambda 对: 对[1], reverse=True)\n\n\n"
           "def 按长度排(文字们):\n    return sorted(文字们, key=len)\n\n\n"
           "def 给名次(名单):\n    结果 = []\n    名次 = 0\n"
           "    for i, (姓名, 分数) in enumerate(名单):\n"
           "        if i > 0 and 分数 == 名单[i - 1][1]:\n            pass\n"
           "        else:\n            名次 = 名次 + 1\n"
           "        结果.append((姓名, 分数, 名次))\n    return 结果\n",
     "名次写成 +1 递增（没跳跃）", "('c', 80, 3)"),
    ("30", "def 成绩排名(名单):\n"
           "    return sorted(名单, key=lambda 对: 对[1], reverse=True)\n\n\n"
           "def 按长度排(文字们):\n    return sorted(文字们, key=lambda 文字: -len(文字))\n\n\n"
           "def 给名次(名单):\n    结果 = []\n"
           "    for i, (姓名, 分数) in enumerate(名单):\n"
           "        if i > 0 and 分数 == 名单[i - 1][1]:\n            名次 = 结果[-1][2]\n"
           "        else:\n            名次 = i + 1\n"
           "        结果.append((姓名, 分数, 名次))\n    return 结果\n",
     "按长度排 写成了从长到短", "应当是"),
    ("31", "def 平方次数(n):\n    return n * n\n\n\n"
           "def 数出次数(n):\n    return n * (n - 1) // 2\n\n\n"
           "def 线性查找(表, 要找的):\n    for i in range(len(表)):\n"
           "        if 表[i] == 要找的:\n            return i\n    return -1\n\n\n"
           "def 有没有重复(表):\n    for i in range(len(表)):\n"
           "        for j in range(i + 1, len(表)):\n"
           "            if 表[i] == 表[j]:\n                return True\n    return False\n",
     "平方次数直接写 n*n（没有真的数）", "没有两层循环"),
    ("31", "def 平方次数(n):\n    次数 = 0\n    for i in range(n):\n"
           "        for j in range(n):\n            次数 += 1\n    return 次数\n\n\n"
           "def 数出次数(n):\n    次数 = 0\n    for i in range(n):\n"
           "        for j in range(i):\n            次数 += 1\n    return 次数\n\n\n"
           "def 线性查找(表, 要找的):\n    for i in range(len(表)):\n"
           "        if 表[i] == 要找的:\n            return i\n    return -1\n\n\n"
           "def 有没有重复(表):\n    for i in range(len(表)):\n"
           "        for j in range(i + 1, len(表)):\n"
           "            if 表[i] == 表[j]:\n                return True\n    return False\n",
     "数出次数 真去循环了 ⇒ n=100000 超时", "跑了 2 秒还没出来"),
    ("31", "def 平方次数(n):\n    次数 = 0\n    for i in range(n):\n"
           "        for j in range(n):\n            次数 += 1\n    return 次数\n\n\n"
           "def 数出次数(n):\n    return n * (n + 1) // 2\n\n\n"
           "def 线性查找(表, 要找的):\n    for i in range(len(表)):\n"
           "        if 表[i] == 要找的:\n            return i\n    return -1\n\n\n"
           "def 有没有重复(表):\n    for i in range(len(表)):\n"
           "        for j in range(i + 1, len(表)):\n"
           "            if 表[i] == 表[j]:\n                return True\n    return False\n",
     "数出次数公式写成 n(n+1)/2", "应当是 0"),
    ("31", "def 平方次数(n):\n    次数 = 0\n    for i in range(n):\n"
           "        for j in range(n):\n            次数 += 1\n    return 次数\n\n\n"
           "def 数出次数(n):\n    return n * (n - 1) // 2\n\n\n"
           "def 线性查找(表, 要找的):\n    for i in range(len(表)):\n"
           "        if 表[i] == 要找的:\n            return i\n\n\n"
           "def 有没有重复(表):\n    for i in range(len(表)):\n"
           "        for j in range(i + 1, len(表)):\n"
           "            if 表[i] == 表[j]:\n                return True\n    return False\n",
     "线性查找 找不到时返回 None", "应当是 -1"),
    ("31", "def 平方次数(n):\n    次数 = 0\n    for i in range(n):\n"
           "        for j in range(n):\n            次数 += 1\n    return 次数\n\n\n"
           "def 数出次数(n):\n    return n * (n - 1) // 2\n\n\n"
           "def 线性查找(表, 要找的):\n    for i in range(len(表)):\n"
           "        if 表[i] == 要找的:\n            return i\n    return -1\n\n\n"
           "def 有没有重复(表):\n    for i in range(len(表)):\n"
           "        for j in range(len(表)):\n"
           "            if 表[i] == 表[j]:\n                return True\n    return False\n",
     "有没有重复 内层从 0 开始（自己和自己比）", "应当是 False"),
    ("32", "def 写几句(路径, 文字们):\n    with open(路径, 'w', encoding='utf-8') as 文件:\n"
           "        for 句 in 文字们:\n            文件.write(句)\n\n\n"
           "def 读全部(路径):\n    with open(路径, encoding='utf-8') as 文件:\n"
           "        return 文件.read()\n\n\n"
           "def 读成列表(路径):\n    with open(路径, encoding='utf-8') as 文件:\n"
           "        return [行.strip() for 行 in 文件]\n\n\n"
           "def 数行数(路径):\n    with open(路径, encoding='utf-8') as 文件:\n"
           "        return len(文件.readlines())\n",
     "写几句忘了写 '\\n'（全挤在一行）", "每行末尾少了换行"),
    ("32", "def 写几句(路径, 文字们):\n    文件 = open(路径, 'w', encoding='utf-8')\n"
           "    for 句 in 文字们:\n        文件.write(句 + '\\n')\n    文件.close()\n\n\n"
           "def 读全部(路径):\n    文件 = open(路径, encoding='utf-8')\n"
           "    内容 = 文件.read()\n    文件.close()\n    return 内容\n\n\n"
           "def 读成列表(路径):\n    文件 = open(路径, encoding='utf-8')\n"
           "    行们 = [行.strip() for 行 in 文件]\n    文件.close()\n    return 行们\n\n\n"
           "def 数行数(路径):\n    文件 = open(路径, encoding='utf-8')\n"
           "    数 = len(文件.readlines())\n    文件.close()\n    return 数\n",
     "全用手动 open/close，没有 with", "四个函数都"),
    ("32", "def 写几句(路径, 文字们):\n    with open(路径, 'w') as 文件:\n"
           "        for 句 in 文字们:\n            文件.write(句 + '\\n')\n\n\n"
           "def 读全部(路径):\n    with open(路径, encoding='utf-8') as 文件:\n"
           "        return 文件.read()\n\n\n"
           "def 读成列表(路径):\n    with open(路径, encoding='utf-8') as 文件:\n"
           "        return [行.strip() for 行 in 文件]\n\n\n"
           "def 数行数(路径):\n    with open(路径, encoding='utf-8') as 文件:\n"
           "        return len(文件.readlines())\n",
     "写文件时忘了 encoding（中文系统上是 GBK）", "不是 UTF-8"),
    ("32", "def 写几句(路径, 文字们):\n    with open(路径, 'w', encoding='utf-8') as 文件:\n"
           "        for 句 in 文字们:\n            文件.write(句 + '\\n')\n\n\n"
           "def 读全部(路径):\n    with open(路径, encoding='utf-8') as 文件:\n"
           "        return 文件.read()\n\n\n"
           "def 读成列表(路径):\n    with open(路径, encoding='utf-8') as 文件:\n"
           "        return 文件.readlines()\n\n\n"
           "def 数行数(路径):\n    with open(路径, encoding='utf-8') as 文件:\n"
           "        return len(文件.readlines())\n",
     "读成列表 没去掉行尾的换行", "应当是 ['小明 90', '小红 95']"),
    ("32", "def 写几句(路径, 文字们):\n    with open(路径, 'w', encoding='utf-8') as 文件:\n"
           "        for 句 in 文字们:\n            文件.write(句 + '\\n')\n\n\n"
           "def 读全部(路径):\n    with open(路径, encoding='utf-8') as 文件:\n"
           "        return 文件.read()\n\n\n"
           "def 读成列表(路径):\n    with open(路径, encoding='utf-8') as 文件:\n"
           "        return [行.strip() for 行 in 文件]\n\n\n"
           "def 数行数(路径):\n    with open(路径, encoding='utf-8') as 文件:\n"
           "        return 文件.read().count('\\n')\n",
     "数行数 用数 '\\n' 的个数（末行没换行就少 1）", "应当是 1"),
    ("32", "def 写几句(路径, 文字们):\n    with open(路径, 'w', encoding='utf-8') as 文件:\n"
           "        for 句 in 文字们:\n            文件.write(句 + '\\n')\n\n\n"
           "def 读全部(路径):\n    with open(路径, encoding='utf-8') as 文件:\n"
           "        文件.read()\n\n\n"
           "def 读成列表(路径):\n    with open(路径, encoding='utf-8') as 文件:\n"
           "        return [行.strip() for 行 in 文件]\n\n\n"
           "def 数行数(路径):\n    with open(路径, encoding='utf-8') as 文件:\n"
           "        return len(文件.readlines())\n",
     "读全部 忘了 return", "得到 None"),
    ("33", "def 转成整数(文字):\n    try:\n        return int(文字)\n"
           "    except ValueError:\n        raise ValueError('不是数字：' + 文字)\n\n\n"
           "def 安全相除(甲, 乙):\n    if 乙 == 0:\n"
           "        raise ZeroDivisionError('除数不能是 0')\n    return 甲 / 乙\n\n\n"
           "class 查不到:\n    pass\n\n\n"
           "def 查分数(成绩表, 名字):\n    if 名字 not in 成绩表:\n"
           "        raise 查不到(名字)\n    return 成绩表[名字]\n\n\n"
           "def 挑出数字(文字们):\n    结果 = []\n    for 文字 in 文字们:\n"
           "        try:\n            结果.append(int(文字))\n"
           "        except ValueError:\n            continue\n    return 结果\n",
     "自定义异常忘了继承 Exception", "没有继承 Exception"),
    ("33", "def 转成整数(文字):\n    if 文字.isdigit():\n        return int(文字)\n"
           "    raise ValueError('不是数字：' + 文字)\n\n\n"
           "def 安全相除(甲, 乙):\n    if 乙 == 0:\n"
           "        raise ZeroDivisionError('除数不能是 0')\n    return 甲 / 乙\n\n\n"
           "class 查不到(Exception):\n    pass\n\n\n"
           "def 查分数(成绩表, 名字):\n    if 名字 not in 成绩表:\n"
           "        raise 查不到(名字)\n    return 成绩表[名字]\n\n\n"
           "def 挑出数字(文字们):\n    结果 = []\n    for 文字 in 文字们:\n"
           "        try:\n            结果.append(int(文字))\n"
           "        except ValueError:\n            continue\n    return 结果\n",
     "用 isdigit() 判断 ⇒ 不认负号", "isdigit"),
    ("33", "def 转成整数(文字):\n    try:\n        return int(文字)\n"
           "    except ValueError:\n        raise ValueError('不是数字：' + 文字)\n\n\n"
           "def 安全相除(甲, 乙):\n    return 甲 / 乙\n\n\n"
           "class 查不到(Exception):\n    pass\n\n\n"
           "def 查分数(成绩表, 名字):\n    if 名字 not in 成绩表:\n"
           "        raise 查不到(名字)\n    return 成绩表[名字]\n\n\n"
           "def 挑出数字(文字们):\n    结果 = []\n    for 文字 in 文字们:\n"
           "        try:\n            结果.append(int(文字))\n"
           "        except ValueError:\n            continue\n    return 结果\n",
     "安全相除 不自己判断，靠 Python 崩", "你没有先判断"),
    ("33", "def 转成整数(文字):\n    try:\n        return int(文字)\n"
           "    except ValueError:\n        return None\n\n\n"
           "def 安全相除(甲, 乙):\n    if 乙 == 0:\n"
           "        raise ZeroDivisionError('除数不能是 0')\n    return 甲 / 乙\n\n\n"
           "class 查不到(Exception):\n    pass\n\n\n"
           "def 查分数(成绩表, 名字):\n    if 名字 not in 成绩表:\n"
           "        raise 查不到(名字)\n    return 成绩表[名字]\n\n\n"
           "def 挑出数字(文字们):\n    结果 = []\n    for 文字 in 文字们:\n"
           "        try:\n            结果.append(int(文字))\n"
           "        except ValueError:\n            continue\n    return 结果\n",
     "转成整数 失败时返回 None（没 raise）", "应当抛 ValueError"),
    ("33", "def 转成整数(文字):\n    try:\n        return int(文字)\n"
           "    except ValueError:\n        raise ValueError('不是数字：' + 文字)\n\n\n"
           "def 安全相除(甲, 乙):\n    if 乙 == 0:\n"
           "        raise ZeroDivisionError('除数不能是 0')\n    return 甲 / 乙\n\n\n"
           "class 查不到(Exception):\n    pass\n\n\n"
           "def 查分数(成绩表, 名字):\n    if 名字 not in 成绩表:\n"
           "        raise 查不到(名字)\n    return 成绩表[名字]\n\n\n"
           "def 挑出数字(文字们):\n    结果 = []\n    for 文字 in 文字们:\n"
           "        结果.append(int(文字))\n    return 结果\n",
     "挑出数字 不接异常 ⇒ 一项坏值就崩", "不许崩"),
    ("34", "def 读成绩(路径):\n    成绩们 = []\n"
           "    with open(路径, encoding='utf-8') as 文件:\n"
           "        for 行 in 文件:\n            行 = 行.strip()\n"
           "            if not 行:\n                continue\n"
           "            姓名, 分数 = 行.split()\n            成绩们.append((姓名, 分数))\n"
           "    return 成绩们\n\n\n"
           "def 算平均(成绩们):\n    if not 成绩们:\n        return 0.0\n"
           "    总分 = 0\n    for 姓名, 分数 in 成绩们:\n        总分 += 分数\n"
           "    return 总分 / len(成绩们)\n\n\n"
           "def 排出名次(成绩们):\n"
           "    排好的 = sorted(成绩们, key=lambda 对: 对[1], reverse=True)\n"
           "    结果 = []\n    for i, (姓名, 分数) in enumerate(排好的):\n"
           "        if i > 0 and 分数 == 排好的[i - 1][1]:\n            名次 = 结果[-1][0]\n"
           "        else:\n            名次 = i + 1\n"
           "        结果.append((名次, 姓名, 分数))\n    return 结果\n\n\n"
           "def 写成绩(路径, 带名次的):\n"
           "    with open(路径, 'w', encoding='utf-8') as 文件:\n"
           "        for 名次, 姓名, 分数 in 带名次的:\n"
           "            文件.write(str(名次) + ' ' + 姓名 + ' ' + str(分数) + '\\n')\n\n\n"
           "def 处理一遍(原路径, 新路径):\n    成绩们 = 读成绩(原路径)\n"
           "    名次们 = 排出名次(成绩们)\n    写成绩(新路径, 名次们)\n    return 名次们\n",
     "读成绩 忘了 int(分数)", "int(分数)"),
    ("34", "def 读成绩(路径):\n    成绩们 = []\n"
           "    with open(路径, encoding='utf-8') as 文件:\n"
           "        for 行 in 文件:\n            行 = 行.strip()\n"
           "            if not 行:\n                continue\n"
           "            姓名, 分数 = 行.split(' ')\n            成绩们.append((姓名, int(分数)))\n"
           "    return 成绩们\n\n\n"
           "def 算平均(成绩们):\n    if not 成绩们:\n        return 0.0\n"
           "    总分 = 0\n    for 姓名, 分数 in 成绩们:\n        总分 += 分数\n"
           "    return 总分 / len(成绩们)\n\n\n"
           "def 排出名次(成绩们):\n"
           "    排好的 = sorted(成绩们, key=lambda 对: 对[1], reverse=True)\n"
           "    结果 = []\n    for i, (姓名, 分数) in enumerate(排好的):\n"
           "        if i > 0 and 分数 == 排好的[i - 1][1]:\n            名次 = 结果[-1][0]\n"
           "        else:\n            名次 = i + 1\n"
           "        结果.append((名次, 姓名, 分数))\n    return 结果\n\n\n"
           "def 写成绩(路径, 带名次的):\n"
           "    with open(路径, 'w', encoding='utf-8') as 文件:\n"
           "        for 名次, 姓名, 分数 in 带名次的:\n"
           "            文件.write(str(名次) + ' ' + 姓名 + ' ' + str(分数) + '\\n')\n\n\n"
           "def 处理一遍(原路径, 新路径):\n    成绩们 = 读成绩(原路径)\n"
           "    名次们 = 排出名次(成绩们)\n    写成绩(新路径, 名次们)\n    return 名次们\n",
     "读成绩 用 split(' ') ⇒ 两个空格就崩", "行.split()"),
    ("34", "def 读成绩(路径):\n    成绩们 = []\n"
           "    with open(路径, encoding='utf-8') as 文件:\n"
           "        for 行 in 文件:\n            行 = 行.strip()\n"
           "            if not 行:\n                continue\n"
           "            姓名, 分数 = 行.split()\n            成绩们.append((姓名, int(分数)))\n"
           "    return 成绩们\n\n\n"
           "def 算平均(成绩们):\n    总分 = 0\n    for 姓名, 分数 in 成绩们:\n"
           "        总分 += 分数\n    return 总分 / len(成绩们)\n\n\n"
           "def 排出名次(成绩们):\n"
           "    排好的 = sorted(成绩们, key=lambda 对: 对[1], reverse=True)\n"
           "    结果 = []\n    for i, (姓名, 分数) in enumerate(排好的):\n"
           "        if i > 0 and 分数 == 排好的[i - 1][1]:\n            名次 = 结果[-1][0]\n"
           "        else:\n            名次 = i + 1\n"
           "        结果.append((名次, 姓名, 分数))\n    return 结果\n\n\n"
           "def 写成绩(路径, 带名次的):\n"
           "    with open(路径, 'w', encoding='utf-8') as 文件:\n"
           "        for 名次, 姓名, 分数 in 带名次的:\n"
           "            文件.write(str(名次) + ' ' + 姓名 + ' ' + str(分数) + '\\n')\n\n\n"
           "def 处理一遍(原路径, 新路径):\n    成绩们 = 读成绩(原路径)\n"
           "    名次们 = 排出名次(成绩们)\n    写成绩(新路径, 名次们)\n    return 名次们\n",
     "算平均 没管空列表", "ZeroDivisionError"),
    ("34", "def 读成绩(路径):\n    成绩们 = []\n"
           "    with open(路径, encoding='utf-8') as 文件:\n"
           "        for 行 in 文件:\n            行 = 行.strip()\n"
           "            if not 行:\n                continue\n"
           "            姓名, 分数 = 行.split()\n            成绩们.append((姓名, int(分数)))\n"
           "    return 成绩们\n\n\n"
           "def 算平均(成绩们):\n    if not 成绩们:\n        return 0.0\n"
           "    总分 = 0\n    for 姓名, 分数 in 成绩们:\n        总分 += 分数\n"
           "    return 总分 / len(成绩们)\n\n\n"
           "def 排出名次(成绩们):\n"
           "    排好的 = sorted(成绩们, key=lambda 对: 对[1], reverse=True)\n"
           "    结果 = []\n    名次 = 0\n"
           "    for i, (姓名, 分数) in enumerate(排好的):\n"
           "        if i > 0 and 分数 == 排好的[i - 1][1]:\n            pass\n"
           "        else:\n            名次 = 名次 + 1\n"
           "        结果.append((名次, 姓名, 分数))\n    return 结果\n\n\n"
           "def 写成绩(路径, 带名次的):\n"
           "    with open(路径, 'w', encoding='utf-8') as 文件:\n"
           "        for 名次, 姓名, 分数 in 带名次的:\n"
           "            文件.write(str(名次) + ' ' + 姓名 + ' ' + str(分数) + '\\n')\n\n\n"
           "def 处理一遍(原路径, 新路径):\n    成绩们 = 读成绩(原路径)\n"
           "    名次们 = 排出名次(成绩们)\n    写成绩(新路径, 名次们)\n    return 名次们\n",
     "名次没跳跃（写成 +1 递增）", "名次编号不对"),
    ("34", "def 读成绩(路径):\n    成绩们 = []\n"
           "    with open(路径, encoding='utf-8') as 文件:\n"
           "        for 行 in 文件:\n            行 = 行.strip()\n"
           "            if not 行:\n                continue\n"
           "            姓名, 分数 = 行.split()\n            成绩们.append((姓名, int(分数)))\n"
           "    return 成绩们\n\n\n"
           "def 算平均(成绩们):\n    if not 成绩们:\n        return 0.0\n"
           "    总分 = 0\n    for 姓名, 分数 in 成绩们:\n        总分 += 分数\n"
           "    return 总分 / len(成绩们)\n\n\n"
           "def 排出名次(成绩们):\n"
           "    排好的 = sorted(成绩们, key=lambda 对: 对[1], reverse=True)\n"
           "    结果 = []\n    for i, (姓名, 分数) in enumerate(排好的):\n"
           "        if i > 0 and 分数 == 排好的[i - 1][1]:\n            名次 = 结果[-1][0]\n"
           "        else:\n            名次 = i + 1\n"
           "        结果.append((名次, 姓名, 分数))\n    return 结果\n\n\n"
           "def 写成绩(路径, 带名次的):\n"
           "    with open(路径, 'w', encoding='utf-8') as 文件:\n"
           "        for 名次, 姓名, 分数 in 带名次的:\n"
           "            文件.write(str(名次) + ' ' + 姓名 + ' ' + str(分数))\n\n\n"
           "def 处理一遍(原路径, 新路径):\n    成绩们 = 读成绩(原路径)\n"
           "    名次们 = 排出名次(成绩们)\n    写成绩(新路径, 名次们)\n    return 名次们\n",
     "写成绩 忘了换行（全挤在一行）", "末尾要"),
    ("34", "def 读成绩(路径):\n    成绩们 = []\n"
           "    with open(路径, encoding='utf-8') as 文件:\n"
           "        for 行 in 文件:\n            行 = 行.strip()\n"
           "            if not 行:\n                continue\n"
           "            姓名, 分数 = 行.split()\n            成绩们.append((姓名, int(分数)))\n"
           "    return 成绩们\n\n\n"
           "def 算平均(成绩们):\n    if not 成绩们:\n        return 0.0\n"
           "    总分 = 0\n    for 姓名, 分数 in 成绩们:\n        总分 += 分数\n"
           "    return 总分 / len(成绩们)\n\n\n"
           "def 排出名次(成绩们):\n"
           "    排好的 = sorted(成绩们, key=lambda 对: 对[1], reverse=True)\n"
           "    结果 = []\n    for i, (姓名, 分数) in enumerate(排好的):\n"
           "        if i > 0 and 分数 == 排好的[i - 1][1]:\n            名次 = 结果[-1][0]\n"
           "        else:\n            名次 = i + 1\n"
           "        结果.append((名次, 姓名, 分数))\n    return 结果\n\n\n"
           "def 写成绩(路径, 带名次的):\n"
           "    with open(路径, 'w', encoding='utf-8') as 文件:\n"
           "        for 名次, 姓名, 分数 in 带名次的:\n"
           "            文件.write(str(名次) + ' ' + 姓名 + ' ' + str(分数) + '\\n')\n\n\n"
           "def 处理一遍(原路径, 新路径):\n    成绩们 = 读成绩(原路径)\n"
           "    名次们 = 排出名次(成绩们)\n    写成绩(新路径, 名次们)\n",
     "处理一遍 忘了 return 名次表", "别忘 return"),
    ("34", "def 读成绩(路径):\n    文件 = open(路径, encoding='utf-8')\n"
           "    成绩们 = []\n    for 行 in 文件:\n        行 = 行.strip()\n"
           "        if not 行:\n            continue\n"
           "        姓名, 分数 = 行.split()\n        成绩们.append((姓名, int(分数)))\n"
           "    文件.close()\n    return 成绩们\n\n\n"
           "def 算平均(成绩们):\n    if not 成绩们:\n        return 0.0\n"
           "    总分 = 0\n    for 姓名, 分数 in 成绩们:\n        总分 += 分数\n"
           "    return 总分 / len(成绩们)\n\n\n"
           "def 排出名次(成绩们):\n"
           "    排好的 = sorted(成绩们, key=lambda 对: 对[1], reverse=True)\n"
           "    结果 = []\n    for i, (姓名, 分数) in enumerate(排好的):\n"
           "        if i > 0 and 分数 == 排好的[i - 1][1]:\n            名次 = 结果[-1][0]\n"
           "        else:\n            名次 = i + 1\n"
           "        结果.append((名次, 姓名, 分数))\n    return 结果\n\n\n"
           "def 写成绩(路径, 带名次的):\n"
           "    with open(路径, 'w', encoding='utf-8') as 文件:\n"
           "        for 名次, 姓名, 分数 in 带名次的:\n"
           "            文件.write(str(名次) + ' ' + 姓名 + ' ' + str(分数) + '\\n')\n\n\n"
           "def 处理一遍(原路径, 新路径):\n    成绩们 = 读成绩(原路径)\n"
           "    名次们 = 排出名次(成绩们)\n    写成绩(新路径, 名次们)\n    return 名次们\n",
     "读成绩 用手动 open/close（没有 with）", "读文件也要用它"),
]

编号们 = sys.argv[1:] or [str(n) for n in range(25, 35)]
课文件 = {}
for f in sorted(os.listdir(S)):
    if f.endswith(".py") and len(f) > 2 and f[:2].isdigit() and f[:2] in 编号们:
        课文件[f[:2]] = f

print()
print("═" * 62)
print(f"  25~34 课验证（本次：{' '.join(sorted(课文件))}）")
print("═" * 62)


def 跑检查(编):
    r = subprocess.run([sys.executable, "-m", "hybridengine_light", "检查", 编],
                       cwd=LC, env=env, capture_output=True, timeout=300)
    return (r.stdout + r.stderr).decode("utf-8", "replace")


def 答题文件(编):
    们 = [f for f in os.listdir(ANS) if f.startswith(编)] if os.path.isdir(ANS) else []
    return os.path.join(ANS, 们[0]) if 们 else None


os.makedirs(ANS, exist_ok=True)
好 = 0
总 = 0

# ── 第一段：参考答案必须全过（顺带验证「首次自动生成模板」） ────────────
print()
print("── ① 参考答案 + 模板生成 ──")
for 编 in sorted(课文件):
    名 = 课文件[编]
    参考 = os.path.join(SOL, 名)
    总 += 1
    if not os.path.isfile(参考):
        print(f"  XX {编}  没有参考答案 {名}")
        continue
    # 删掉我这一课的答题文件 ⇒ 走"首次检查自动生成模板"
    答案 = 答题文件(编)
    if 答案:
        os.remove(答案)
    out1 = 跑检查(编)
    答案 = 答题文件(编)
    if not 答案:
        print(f"  XX {编}  首次检查没有生成答题文件")
        continue
    with open(答案, encoding="utf-8") as f:
        模板 = f.read()
    if "轮到你了" not in 模板 or len(模板) < 1500:
        print(f"  XX {编}  生成的模板不完整（{len(模板)} 字，没有讲解/要求）")
        continue
    shutil.copyfile(参考, 答案)
    out2 = 跑检查(编)
    过 = "全部通过" in out2
    好 += 1 if 过 else 0
    print(f"  {'OK ' if 过 else 'XX '} {编}  模板 {len(模板):>5} 字 · "
          f"{out2.count('√')} 项全过 · {名}")
    if not 过:
        for line in out2.splitlines():
            s = line.strip()
            if s.startswith("×") or "应当" in s or "没有" in s or "⚠" in s:
                print("        " + s[:104])

print()
print(f"  == 参考答案 {好}/{总} 课全过 ==")

# ── 第二段：错答案必须被拒，而且拒绝的理由要对 ────────────────────────
print()
print("── ② 错答案必须被拒 ──")
按课 = {}
for 编, 代码, 说明, 期望 in 错例:
    if 编 in 课文件:
        按课.setdefault(编, []).append((代码, 说明, 期望))

坏 = 0
坏总 = 0
for 编 in sorted(课文件):
    for 序, (代码, 说明, 期望) in enumerate(按课.get(编, []), 1):
        坏总 += 1
        答案 = 答题文件(编)
        with open(答案, "w", encoding="utf-8") as f:
            f.write(代码)
        out = 跑检查(编)
        全过 = "全部通过" in out
        抓到 = 期望 in out
        对 = (not 全过) and 抓到
        坏 += 1 if 对 else 0
        print(f"  {'OK ' if 对 else 'XX '} {编}-{序}  {说明}")
        if 全过:
            print("        **错答案被判成通过了** —— 这条检查是摆设")
        elif not 抓到:
            print(f"        报错了，但报告里没有「{期望}」—— 提示指错了地方")
            for line in out.splitlines()[:14]:
                print("        | " + line.rstrip()[:110])
    # 每一课跑完，把参考答案放回去
    shutil.copyfile(os.path.join(SOL, 课文件[编]), 答题文件(编))

print()
print(f"  == 错答案 {坏}/{坏总} 个被正确拒绝 ==")
print()
结论 = (好 == 总) and (坏 == 坏总) and 坏总 > 0
print("═" * 62)
print("  结论：%s" % ("两段都通过 ✅" if 结论 else "有失败 ❌"))
print("═" * 62)
sys.exit(0 if 结论 else 1)
