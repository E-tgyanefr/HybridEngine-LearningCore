# 探针 2（25~34 课专用）：再取几条报错原文 + 实测数字。
import sys
import traceback

sys.stdout.reconfigure(encoding="utf-8", errors="replace")


def 跑(名, fn):
    try:
        fn()
    except BaseException as e:                      # noqa: BLE001
        print(f"[{名}] {type(e).__name__}: {e}")
    else:
        print(f"[{名}] （没有报错）")
    print()


def 一():
    def 数一数():
        局部 = 3
        return 局部
    print("   函数里读得到：", 数一数())
    print(局部)                                  # noqa: F821


def 二():
    表 = [0] * (10 ** 12)


def 三():
    """列表传进函数后被改 —— 外面那个也变了（不报错，静默）。"""
    def 加进去(表):
        表.append(99)
    a = [1, 2]
    加进去(a)
    print("   外面看到的 a =", a)


def 四():
    """元组传进函数 —— 改不动。"""
    def 改(元组):
        元组[0] = 99
    改((1, 2))


def 五():
    """字符串传进函数 —— 也改不动。"""
    def 改(文字):
        文字[0] = 'A'
    改('abc')


def 六():
    """数字传进去改不动（但这是"重新绑定"，不会报错，只会静默）。"""
    def 加一(数):
        数 = 数 + 1
        return 数
    n = 5
    print("   加一(n) =", 加一(n), "  n 还是", n)


print("===== 第 25 课 =====")
跑("函数里的局部变量，外面看不见", 一)

print("===== 第 26 课 =====")
跑("列表被函数改了，外面也变（静默）", 三)
跑("元组改元素", 四)
跑("字符串改字符", 五)
跑("数字：函数里重绑定不影响外面（静默）", 六)

print("===== 第 31 课 =====")
跑("[0] * 10**12", 二)

import time as _t


def 慢斐(数):
    if 数 < 2:
        return 数
    return 慢斐(数 - 1) + 慢斐(数 - 2)


for n in (35, 40):
    起 = _t.perf_counter()
    值 = 慢斐(n)
    print(f"慢斐波那契 n={n} 值={值} 耗时={_t.perf_counter() - 起:.2f} 秒")

起 = _t.perf_counter()
for i in range(100000):
    pass
print("空转 10 万次耗时 %.4f 秒" % (_t.perf_counter() - 起))

# 递增列表 与 建表，用来讲"估算"
for n in (1000, 10000, 100000):
    起 = _t.perf_counter()
    表 = [0] * n
    for i in range(n):
        表[i] = i * i
    print(f"O(n) 建表 n={n:>6}  耗时={_t.perf_counter() - 起:.4f} 秒")
