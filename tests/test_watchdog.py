"""看门狗的判据 —— **先证明它真能打断死循环，再接进产品**。

## ⚠ 为什么这组判据必须先写

这个模块的失败方式**特别阴**：它"接上了但没生效"时，
所有现有测试**照样全过**（没人写死循环）——
直到某个学员在手机上写了个 `while True`，应用卡死，才发现。

⇒ 所以这里**主动造死循环**，而且造**好几种**：

    · 纯 `while True: pass`
    · 有 `try/except` 包着的死循环（**看门狗不能被人自己的 except 吞掉**）
    · 递归到爆栈的
    · 嵌套函数里的死循环（**只看最外层是不够的**）
    · `for` 大循环（不是 while，走的是另一条字节码路径）

## ⚠ 还有一条同样要紧：**不能误伤**

看门狗最大的风险不是"不生效"，是**把正常代码也打断**。
所以正对照要够多：正常算一会儿的代码必须跑完。
"""
from __future__ import annotations

import os
import sys
import time
import unittest

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if LC not in sys.path:
    sys.path.insert(0, LC)

from hybridengine_light.watchdog import 超时, 看门狗, 跑一遍, 默认秒  # noqa: E402


def _死循环():
    while True:
        pass


def _死循环_带try():
    """⚠ **它自己的 try 会吞掉异常吗？**

    `except Exception` 会——而 `超时` 就是 `Exception` 的子类。
    ⇒ 这条判据在验："看门狗打断之后，学员自己的 except 会不会把它吃掉"。
      吃掉的话循环继续 ⇒ 看门狗**形同虚设**（这是个真风险）。
    """
    while True:
        try:
            pass
        except Exception:
            pass


def _死循环_宽except():
    """**最坏的情况**：学员写了 `except BaseException`（很不该，但会有人写）。"""
    while True:
        try:
            pass
        except BaseException:
            continue


def _内层死循环():
    def 内():
        while True:
            pass
    内()


def _大for循环():
    s = 0
    for i in range(10 ** 12):        # 一辈子跑不完
        s += i
    return s


def _正常算一会儿(毫秒=50):
    t0 = time.monotonic()
    s = 0
    while time.monotonic() - t0 < 毫秒 / 1000.0:
        s += 1
    return s


def _递归(n):
    if n <= 0:
        return 0
    return 1 + _递归(n - 1)


class 看门狗测试(unittest.TestCase):
    """⭐ 核心：**真能打断**。"""

    def test_纯死循环被打断(self):
        t0 = time.monotonic()
        with self.assertRaises(超时):
            with 看门狗(秒=0.3, 每多少行=200):
                _死循环()
        #: ⚠ 不许"跑满秒数才断"——那是"没生效"，不是"生效了"
        self.assertLess(time.monotonic() - t0, 3.0, "打断得太慢，像没生效")

    def test_带try的死循环被打断(self):
        """⚠ 学员的 `except Exception` **不该**让看门狗失效。"""
        with self.assertRaises(超时):
            with 看门狗(秒=0.3, 每多少行=200):
                _死循环_带try()

    def test_宽except也拦得住(self):
        """`except BaseException` 也不该让看门狗失效。

        ⚠ 这条在验一个**设计决定**：
          `超时` 继承 `Exception`（不是 `BaseException`），
          所以 `except BaseException` **会**吞掉它 —— 吞掉之后循环继续，
          但下一个 200 行**又会**重新抛 ⇒ 最终还是会出来（只是慢一点）。
          ⇒ 判据是"最终能出来"，不要求"一次就断"。
        """
        with self.assertRaises(超时):
            with 看门狗(秒=0.3, 每多少行=200):
                _死循环_宽except()

    def test_内层函数的死循环也拦得住(self):
        """⚠ 只看最外层 frame 是不够的 —— 死循环在**内层**。"""
        with self.assertRaises(超时):
            with 看门狗(秒=0.3, 每多少行=200):
                _内层死循环()

    def test_大for循环也拦得住(self):
        """走过 `for` 和走过 `while` 是不同的字节码路径，两条都要能断。"""
        with self.assertRaises(超时):
            with 看门狗(秒=0.3, 每多少行=200):
                _大for循环()

    def test_跑一遍也能打断(self):
        with self.assertRaises(超时):
            跑一遍(_死循环, 秒=0.3)

    def test_超时消息说人话(self):
        """⚠ 提示语要告诉学员"循环停不下来"，不是甩一句 Traceback。"""
        try:
            with 看门狗(秒=0.3, 每多少行=200):
                _死循环()
        except 超时 as e:
            文 = str(e)
            self.assertIn("秒", 文)
            self.assertTrue("循环" in 文 or "停下" in 文,
                            "提示语没说清是「停不下来」：" + 文)
        else:
            self.fail("没抛超时")


class 不误伤测试(unittest.TestCase):
    """⚠ 同样要紧：**正常代码不许被打断**。"""

    def test_正常代码跑得完(self):
        n = _正常算一会儿(50)
        self.assertGreater(n, 0)

    def test_包在with里也跑得完(self):
        with 看门狗(秒=5.0):
            n = _正常算一会儿(50)
        self.assertGreater(n, 0)

    def test_循环很多次但不超时(self):
        """⚠ 这条是"每多少行看表"的**正对照**：
        跑几十万行、但没超时 ⇒ 必须跑完，不能被行数计数误判成超时。"""
        with 看门狗(秒=10.0, 每多少行=100):
            s = 0
            for i in range(200000):
                s += i
        self.assertEqual(s, 200000 * 199999 // 2)

    def test_递归不误伤(self):
        with 看门狗(秒=5.0):
            得 = _递归(500)
        self.assertEqual(得, 500)

    def test_抛别的异常不吞(self):
        """⚠ 看门狗**不吞**异常 —— 学员代码本来报的错要原样传出去。"""
        def 坏():
            raise ValueError("学员自己的错")
        with self.assertRaises(ValueError):
            with 看门狗(秒=5.0):
                坏()


class 还原测试(unittest.TestCase):
    """⚠ 退出时必须把 `sys.settrace` 还原 —— 不还原会污染后面所有代码。"""

    def test_退出后trace被还原(self):
        旧 = sys.gettrace()
        try:
            with 看门狗(秒=5.0):
                pass
            self.assertIs(sys.gettrace(), 旧, "正常退出后没还原 trace")

            try:
                with 看门狗(秒=0.3, 每多少行=200):
                    _死循环()
            except 超时:
                pass
            self.assertIs(sys.gettrace(), 旧, "**异常退出后**没还原 trace")
        finally:
            sys.settrace(旧)

    def test_不改原有的trace(self):
        """⚠ 如果外面本来就有 tracer（比如覆盖率工具），出来要还是它。"""
        痕迹 = []

        def 外tracer(frame, event, arg):
            痕迹.append(event)
            return 外tracer

        sys.settrace(外tracer)
        try:
            with 看门狗(秒=5.0):
                _正常算一会儿(20)
            得 = sys.gettrace()
            self.assertIs(得, 外tracer, "看门狗把外面的 tracer 弄丢了")
        finally:
            sys.settrace(None)

    def test_嵌套看门狗(self):
        """两层 `with` —— 出来之后必须是外层那个。"""
        with 看门狗(秒=5.0):
            外 = sys.gettrace()
            with 看门狗(秒=5.0):
                内 = sys.gettrace()
                self.assertIsNot(内, 外, "内层没有换成自己的 tracer")
            self.assertIs(sys.gettrace(), 外, "内层退出后没还原成外层的")


class 开关测试(unittest.TestCase):
    """`每多少行看表` 和 `默认秒` 的边界。"""

    def test_每多少行至少是1(self):
        """⚠ `每多少行=0` 会让 `计数 >= 0` 恒真 ⇒ 每行都看表 ⇒ 慢到不可用。"""
        w = 看门狗(秒=1.0, 每多少行=0)
        self.assertGreaterEqual(w.每多少行, 1)

    def test_默认秒是个合理值(self):
        #: ⚠ 太小会误伤正常代码；太大等于没有。
        self.assertGreaterEqual(默认秒, 3.0)
        self.assertLessEqual(默认秒, 60.0)

    def test_超了标志位(self):
        w = 看门狗(秒=0.3, 每多少行=200)
        with self.assertRaises(超时):
            with w:
                _死循环()
        self.assertTrue(w.超了, "打断之后 超了 应当是 True（给调用方一个凭据）")

    def test_没超时标志位是假(self):
        w = 看门狗(秒=5.0)
        with w:
            pass
        self.assertFalse(w.超了)


class 开销测试(unittest.TestCase):
    """⚠ **这个模块不是免费的** —— 开销要量出来、钉住，不许悄悄变坏。

    ## 为什么要专门有一组"性能"判据

    `settrace` 让**每一行**都变成一个 Python 函数调用（tracer 那个回调），
    而那是**解释器层的**，不归我们的参数控制。

    ⚠⚠ 我**猜错过一次**，记在这里：
      我以为"贵的是 `time.monotonic()`"，于是把看表间隔从 1000 调到 10000，
      以为能大幅降开销。**实测一样**（12.35x → 12.47x，差异是噪声）。
      ⇒ 这组判据就是防止下一个人再猜一次。

    ⚠ 而**能接受**的理由不是"12x 不严重"，是**摊薄之后小**：
      真实代码每行都有活干，不像这种"每行一次加法"的紧循环。
      实测一次真判分（第 00 课）是 3 毫秒 —— 相对 10 秒预算占 0.03%。
    """

    @staticmethod
    def _紧循环(n=400000):
        s = 0
        for i in range(n):
            s += i * 2
        return s

    def test_紧循环开销有上限(self):
        """⚠ 紧循环是**最坏情况**（每行都极便宜，摊不薄）。

        ⚠ 上限取 25x 是**故意的宽松**：
          这条要抓的不是"12 变成 13"，而是"**有人把它改成了每行都 trace、
          或者在里面加了什么重活**"那种量级的变化。
          钉太紧会变成一条总在飘的红（不同机器差一倍很正常）。
        """
        import time as _t

        def 量(开: bool, 次=5):
            最好 = 9e9
            for _ in range(次):
                t0 = _t.perf_counter()
                if 开:
                    with 看门狗(秒=60.0):
                        self._紧循环()
                else:
                    self._紧循环()
                最好 = min(最好, _t.perf_counter() - t0)
            return 最好

        不开 = 量(False)
        开 = 量(True)
        比 = 开 / 不开
        self.assertLess(比, 25.0, "看门狗开销 %.1fx 太大（实测应在 13x 上下）" % 比)
        #: ⚠ 反过来也要钉：如果开销突然变成 1x，那是**根本没生效**（比慢更糟）
        self.assertGreater(比, 1.5,
                           "开销只有 %.2fx —— 像是 tracer 没接上（那比慢更糟）" % 比)

    def test_真判分耗时可接受(self):
        """⚠ **这条才是要紧的那条** —— 判据是"判一次课多久"，不是"紧循环慢几倍"。

        紧循环那个 12x 很难看，但真实判分的**绝对耗时**才是学员会感觉到的。
        """
        import time as _t
        import contextlib
        import io
        from hybridengine_light import learnmodel as M

        课 = None
        for c in M.发现("python"):
            if c.编号 == "00":
                课 = c
                break
        self.assertIsNotNone(课, "找不到 python 第 00 课")

        答 = M.参考答案(课)
        次 = 5
        t0 = _t.perf_counter()
        for _ in range(次):
            with contextlib.redirect_stdout(io.StringIO()):
                M.检查(课, 答)
        每次 = (_t.perf_counter() - t0) / 次

        #: ⚠ 上限 2 秒：真判分实测 3 毫秒量级，给 2 秒是"绝对不许接近 10 秒预算"的意思
        self.assertLess(每次, 2.0, "一次判分要 %.2f 秒 —— 太慢了" % 每次)


class 机制选择测试(unittest.TestCase):
    """⭐ 两条路（`setitimer` / `settrace`）按**能力**选，不是按平台写死。

    ## ⚠⚠ 这组判据里最要紧的一条是"探测不能把能用误判成不能用"

        踩过的坑：`_settrace_可用()` 第一版把探测循环写在**已经活动的那一帧**里。
        `settrace` 的**全局 tracer 只在 `call` 事件时被调** ——
        没有新帧就**没有 `call`** ⇒ tracer 一次都不被调 ⇒ 报"不可用"。

        表现是**自相矛盾**：`机制()` 报 `none`，
        而同一进程里 `看门狗` 明明把死循环打断了。
        ⇒ 判据就是钉这个矛盾：**"说有保护"和"真的能打断"必须一致。**
    """

    def test_机制是个已知值(self):
        from hybridengine_light import watchdog as W
        self.assertIn(W.机制(), ("signal", "settrace", "none"))

    def test_有超时保护与本机能力一致(self):
        from hybridengine_light import watchdog as W
        期望 = W._信号可用() or W._settrace_可用()
        self.assertEqual(W.有超时保护(), 期望)

    def test_机制与有超时保护不矛盾(self):
        """⚠ 报了 `none` 却说"有保护"，或反过来 —— 都是 bug。"""
        from hybridengine_light import watchdog as W
        self.assertEqual(W.机制() == "none", not W.有超时保护())

    def test_说能保护就必须真能打断(self):
        """⭐⭐ **这条是核心** —— 把"探测结果"和"真本事"绑在一起。

        ⚠ 没有它，`_settrace_可用()` 那种**误判**会一路静默下去：
          界面显示"有超时保护"，而学员的死循环照样卡住。
        """
        from hybridengine_light import watchdog as W
        if not W.有超时保护():
            self.skipTest("这台机器上两条路都没有 —— 见 test_机制是个已知值")
        #: ⚠⚠ **必须用 `exec` 跑那段死循环，不能就地写 `while True`。**
        #:   原因见下面 `test_只在活动帧里不行` 那条 ——
        #:   `settrace` **对 `__enter__` 所在的那个已经在活动的帧无效**，
        #:   而就地写 `while True` 时，循环**就在那个帧里**。
        #:   ⚠ 而**生产路径用的是 `exec`**（`code.run` / `run_checks` 都是），
        #:     那是**新帧** ⇒ 能打断。⇒ 判据要复刻**生产路径**，不是复刻一个
        #:     在生产里不存在的写法。
        try:
            with W.看门狗(秒=0.4, 每多少行=200):
                exec(compile("while True:\n    pass\n", "<学员代码>", "exec"),
                     {"__name__": "__main__"})
        except W.超时:
            return                              #: 真打断了 ⇒ 与"说有保护"一致
        self.fail("说有超时保护，但死循环**没有**被打断 —— 探测结果在撒谎")

    def test_用了这个字段与实际走的路一致(self):
        from hybridengine_light import watchdog as W
        if not W.有超时保护():
            self.skipTest("没有可用的路")
        with W.看门狗(秒=5.0) as w:
            pass
        self.assertIn(w.用了, ("signal", "settrace"))
        #: ⚠ 不该报 `none` —— 那说明"探测说能用、实际没用上"
        self.assertNotEqual(w.用了, "none", "探测说能用，但 __enter__ 没走任何一条路")

    def test_超时之后信号被清掉(self):
        """⚠⚠ **定时器不清会害下一个 `with` 块莫名被打断** —— 极难查。

        做法：先跑一个超时的（0.3 秒），再跑一个**不该超时**的（给 5 秒、
        只 sleep 0.5 秒）—— 如果上一个的定时器没清，第二个会被误伤。
        """
        from hybridengine_light import watchdog as W
        if not W.有超时保护():
            self.skipTest("没有可用的路")
        try:
            with W.看门狗(秒=0.3, 每多少行=200):
                exec(compile("while True:\n    pass\n", "<学员代码>", "exec"),
                     {"__name__": "__main__"})
        except W.超时:
            pass
        #: 现在跑一个正常该跑完的
        import time as _t
        好 = False
        with W.看门狗(秒=5.0):
            _t.sleep(0.5)
            好 = True
        self.assertTrue(好, "上一个看门狗的定时器**没清**，误伤了这一个")


class 活动帧限制测试(unittest.TestCase):
    """⚠⚠ **`settrace` 对"已经在活动的帧"无效** —— 这条限制必须钉住。

    ## 实测（本机 Windows）

        `with 看门狗(...): while True: pass`  写在**模块顶层** ⇒ ❌ **不打断**
        同一个 `with` 写在**函数里**、循环再调一个函数 ⇒ ✅ 打断
        直接 `__enter__()` / `__exit__()`（不用 `with`）⇒ ✅ 打断
        `with` + `exec(compile(...))`           ⇒ ✅ 打断   ← **生产路径**

    ## 为什么

        `sys.settrace(f)` 设的是**全局 trace**，它只在**新帧产生**时被调用；
        当前**已经在活动**的那个帧**拿不到** local trace。
        ⇒ `with` 写在顶层时，`while True` 就在**同一个帧**里 ⇒ tracer 收不到它的行事件。

    ## 为什么这**不影响产品**

        生产路径三处全是 `exec` 或函数调用（`ctl._code_run` 用 `exec`，
        `check.run_checks` 调课程里的 `CHECK_*` **函数**，
        `examrun._grade_code` 用 `exec`）⇒ **都是新帧**。

        ⚠ 但**用户可能在别处用它** —— 所以这条限制要**写成判据**，
          而不是靠"我们知道"。
    """

    def test_exec跑的死循环能打断(self):
        """⭐ **生产路径** —— 这条必须过。"""
        from hybridengine_light import watchdog as W
        if not W.有超时保护():
            self.skipTest("没有可用的路")
        try:
            with W.看门狗(秒=0.4, 每多少行=200):
                exec(compile("while True:\n    pass\n", "<学员代码>", "exec"),
                     {"__name__": "__main__"})
        except W.超时:
            return
        self.fail("**生产路径**（exec）里的死循环没被打断")

    def test_函数里调用的死循环能打断(self):
        """也是生产路径：`run_checks` 调的是课程里的 `CHECK_*` **函数**。"""
        from hybridengine_light import watchdog as W
        if not W.有超时保护():
            self.skipTest("没有可用的路")

        def 死():
            while True:                 # noqa: B011
                pass

        try:
            with W.看门狗(秒=0.4, 每多少行=200):
                死()
        except W.超时:
            return
        self.fail("函数里的死循环没被打断")

    #: ⚠⚠ **不把"活动帧里打断不了"写成判据。**
    #:
    #:   我第一版写了，而且用 `os._exit(7)` 当兜底 —— 结果**测试进程被那条
    #:   测试自己杀掉**（退出码 7），整套判据跟着挂。
    #:
    #:   ⇒ 教训：**判据不该有"为了不挂住而自杀"这种设计。**
    #:     一个会杀进程的判据，坏起来比它想测的东西还难查。
    #:
    #:   那条限制记在**上面这个类的 docstring 里**就够了 ——
    #:   它是"我们知道的一件事"，不是"每次都要跑一遍的断言"。
    #:   （真要测，得有真正的硬超时机制；而本模块本身就是那个机制，
    #:     用它测它自己不成立 —— 循环依赖。）


if __name__ == "__main__":
    unittest.main(verbosity=2)
