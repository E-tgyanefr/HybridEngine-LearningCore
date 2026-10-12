# 探针（25~34 课专用）：把这一批课要引用的报错原文**真跑一遍**取出来。
# 课程里写的每一句报错都必须来自这里，不能凭记忆写。
# ⚠ 文件名带 _25_34：第 18~24 课的作者也在 _build 下放探针，别撞。
import os
import signal
import sys
import tempfile
import threading
import traceback

sys.stdout.reconfigure(encoding="utf-8", errors="replace")


def 跑(名, fn):
    try:
        fn()
    except BaseException as e:                      # noqa: BLE001
        末 = traceback.format_exc().strip().splitlines()[-1]
        print(f"[{名}] {type(e).__name__}: {e}")
        print(f"     末行：{末}")
    else:
        print(f"[{名}] （没有报错）")
    print()


def 一():
    次数 = 0

    def 加一():
        次数 += 1
    加一()


def 二():
    t = (1, 2)
    t[0] = 9


def 三():
    def 下去(n):
        return 下去(n + 1)
    下去(0)


def 四():
    表 = [3, 1]
    for j in range(len(表)):
        if 表[j] > 表[j + 1]:
            pass


def 五():
    排好的 = [3, 1, 2].sort()
    print(排好的[0])


def 六():
    with open("这个文件根本不存在.txt", encoding="utf-8") as f:
        f.read()


def 七():
    int("abc")


def 八():
    class 我的错:
        pass
    try:
        raise ValueError("x")
    except 我的错:
        pass


def 九():
    f = open(os.path.join(tempfile.gettempdir(), "_probe_io.txt"), "w", encoding="utf-8")
    with f:
        f.write("hi")
    f.write("再来一次")


def 十():
    f = open(os.path.join(tempfile.gettempdir(), "_probe_io2.txt"), "w", encoding="utf-8")
    f.write("先写一句")
    f.close()
    print("   关掉之后再读：")
    f.read()


def 十一():
    """Ctrl+C 打断一个跑不完的循环 —— 学员真实看到的样子。"""
    def 打断():
        import time
        time.sleep(0.3)
        signal.raise_signal(signal.SIGINT)
    threading.Thread(target=打断, daemon=True).start()
    try:
        和 = 0
        for i in range(10 ** 9):
            和 += i
    except KeyboardInterrupt:
        print("   ---- 打断时屏幕上的原文 ----")
        print(traceback.format_exc().rstrip())
        print("   ----------------------------")
        raise


def 十二():
    s = "abc"
    s[0] = "A"


def 十三():
    t = (1, 2)
    t.append(3)


def 十四():
    分 = 90
    print("总分：" + 分)


def 十五():
    """表开小了：递推里最经典的一格越界。"""
    n = 5
    表 = [0] * n
    表[n] = 1


def 十六():
    """range 里放小数。"""
    for i in range(2, 9.0):
        pass


def 十七():
    """递归深度真的撑爆（不是无限递归，是数据太大）。"""
    def 求和(数字们):
        if not 数字们:
            return 0
        return 数字们[0] + 求和(数字们[1:])
    print(求和(list(range(5000))))


for 名, fn in [("局部变量先用后赋值", 一), ("元组改元素", 二),
               ("无限递归", 三), ("冒泡 j+1 越界", 四),
               ("sort() 的返回值", 五), ("读不存在的文件", 六),
               ("int('abc')", 七), ("自定义异常没继承", 八),
               ("with 块外再用文件对象", 九), ("文件关掉再读", 十),
               ("Ctrl+C 打断", 十一), ("字符串改字符", 十二),
               ("元组 append", 十三), ("文字加数字", 十四),
               ("递推表开小一格", 十五), ("range 放小数", 十六),
               ("递归求和 5000 个", 十七)]:
    跑(名, fn)

# 几个要写进课里的**实测数字**：不实测不许写进课程。
import time as _t

for n in (100, 1000, 3000):
    起 = _t.perf_counter()
    次数 = 0
    for i in range(n):
        for j in range(n):
            次数 += 1
    花 = _t.perf_counter() - 起
    print(f"O(n²) 实测 n={n:>5}  次数={次数:>9}  耗时={花:.4f} 秒  （每次≈{花 / 次数 * 1e9:.1f} 纳秒）")

for n in (10, 20, 25, 30):
    起 = _t.perf_counter()

    def 慢斐(数):
        if 数 < 2:
            return 数
        return 慢斐(数 - 1) + 慢斐(数 - 2)
    值 = 慢斐(n)
    花 = _t.perf_counter() - 起
    print(f"O(2ⁿ) 慢斐波那契实测 n={n:>3}  值={值:>8}  耗时={花:.4f} 秒")

起 = _t.perf_counter()


def 快斐(n):
    表 = [0, 1]
    for i in range(2, n + 1):
        表.append(表[i - 1] + 表[i - 2])
    return 表[n]


print("递推 fib(200) =", 快斐(200), "  耗时 %.6f 秒" % (_t.perf_counter() - 起))

起 = _t.perf_counter()
print("in 列表 1000 次 × 100000 元素：", end=" ")
大表 = list(range(100000))
命中 = sum(1 for _ in range(1000) if 99999 in 大表)
print("%.4f 秒" % (_t.perf_counter() - 起))

print()
print("python:", sys.version)
