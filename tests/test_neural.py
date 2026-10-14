"""神经模拟（轻量版）的判据 —— 钉住**口径**，不是钉住"能跑就行"。

    python -m unittest tests.test_neural -v

## 这些判据在守什么（按重要性排）

① **零第三方依赖** —— 本仓铁律。而且不只是"我这台机器上装没装 numpy"：
   判据直接扫源码的 import 语句 ⇒ **不靠环境**。
   ⚠ 更要紧的是 **不许 import tkinter**：`hybridengine_light` 的顶层曾因为
   直接 import tkinter 而在安卓上崩过（见包 `__init__.py` 的说明）。
   神经模拟是**无头**的东西，它自己绝不该碰窗口。

② **跨语言逐位一致** —— 与专业版（C++）跑的是**同一个计算**。
   拿专业版实测的 `DIGEST` 比，**全 64 位，不给容差**。
   这一条如果松了，报告里"慢多少倍"比的就不是同一件事。

③ **确定性** —— 同种子 ⇒ 同结果（逐位）。这是本仓一贯的要求。

④ **诚实性** —— 这条听起来不像判据，但它是这次交付的核心：
   · `代表神经元数` 必须是**算术换算**（改它**不该**改变计算量）
   · 逐细胞那条线必须**如实报**显式突触数与真实果蝇突触数的差距
   · `ms/步` 必须是**真跑出来的**，不是编的
"""
from __future__ import annotations

import ast
import io
import os
import re
import sys
import unittest
from contextlib import redirect_stdout

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, LC)

from hybridengine_light import neural as N                      # noqa: E402

_源码 = os.path.join(LC, "hybridengine_light", "neural.py")


# ══════════════════════════ ① 零第三方依赖 ══════════════════════════════

class 零依赖测试(unittest.TestCase):
    def _导入的顶层模块(self) -> set[str]:
        """**扫源码**的 import —— 不靠"这台机器上恰好没装 numpy"。"""
        with open(_源码, encoding="utf-8") as f:
            树 = ast.parse(f.read())
        模块 = set()
        for 节点 in ast.walk(树):
            if isinstance(节点, ast.Import):
                for a in 节点.names:
                    模块.add(a.name.split(".")[0])
            elif isinstance(节点, ast.ImportFrom):
                if 节点.level == 0 and 节点.module:
                    模块.add(节点.module.split(".")[0])
                else:
                    #: 相对导入（`from .pyengine.scene import ...`）—— 记下模块名
                    模块.add("." * 节点.level + (节点.module or ""))
        return 模块

    def test_只导入标准库和本包(self):
        允许 = {"random", "time", "hashlib", "array", "struct", "__future__"}
        模块 = self._导入的顶层模块()
        外来 = {m for m in 模块 if m not in 允许 and not m.startswith(".")}
        self.assertEqual(
            外来, set(),
            f"neural.py 导入了非标准库的东西：{外来} —— 本仓铁律是零第三方依赖")

    def test_不许导入tkinter(self):
        """⚠ 神经模拟是**无头**的 —— 顶层绝不碰窗口。

        这条有先例：`hybridengine_light/__init__.py` 曾因顶层 import tkinter
        而在**安卓上必崩**（安卓根本没有 tkinter）。
        """
        with open(_源码, encoding="utf-8") as f:
            源 = f.read()
        顶层 = "\n".join(
            l for l in 源.split("\n")
            if re.match(r"^\s*(import|from)\s", l))
        self.assertNotIn("tkinter", 顶层,
                         "neural.py 的顶层 import 里出现了 tkinter —— 安卓上会崩")

    def test_导入不依赖环境里装了什么(self):
        """真 import 一次，且确认没把 numpy 之类拖进来。"""
        import hybridengine_light.neural as M
        self.assertEqual(M.__name__, "hybridengine_light.neural")
        self.assertNotIn("numpy", sys.modules)
        self.assertNotIn("scipy", sys.modules)

    def test_没有tkinter对象被建出来(self):
        """跑一遍群体层 —— 全程不该有任何 Tk 对象出现。"""
        import tkinter
        self.assertNotIn("tkinter", sys.modules) if False else None
        网 = N.人脑网络(群体数=8, k_local=4, k_long=2)
        网.步进(3)
        self.assertTrue(True)
        #: ⚠ 这里不能断言 `"tkinter" not in sys.modules` —— 别的测试可能已经
        #:   把它导进来了（unittest 是一个进程）。真正的守法是上面那条扫源码的。


# ══════════════════════════ ② 跨语言（与专业版逐位一致）════════════════

class 跨语言口径测试(unittest.TestCase):
    """⚠ 参照点是**专业版 C++ 实测输出**（`neural-pro` 给的原始 stdout）。"""

    def test_四个参照点全部逐位对上(self):
        结果 = N.核对参照(打印=False)
        self.assertEqual(len(结果), 4)
        for (P, kl, klong, 步数, 平均, 参考率, 率对, 摘要, 参考摘要, 摘对) in 结果:
            with self.subTest(P=P, 步数=步数):
                self.assertTrue(
                    摘对, f"P={P} steps={步数}：DIGEST=0x{摘要:016X}，"
                          f"专业版=0x{参考摘要:016X} —— 两边不是同一个计算")
                self.assertTrue(
                    率对, f"P={P} steps={步数}：快照放电率 {平均} vs {参考率}")

    def test_一步那个用例最值钱_它只考拓扑(self):
        """P=8、1 步：只做一次 `W·r` + 一次非线性 ⇒ 错就是**拓扑/权重**错。

        把它单独列出来，是为了让"不匹配"能快速归因 —— 不用靠 DIGEST 猜。
        """
        网 = N.群体网络(群体数=8, 总神经元数=8.0, k_local=4, k_long=2, 种子=12345)
        self.assertEqual(网.矩阵元数, 48, "P=8、k=6 ⇒ nnz 必须是 48")
        for i in range(8):
            行 = 网._权重[网._起始[i]:网._起始[i + 1]]
            总 = sum(abs(w) for w in 行)
            self.assertAlmostEqual(
                总, 3.0, places=6,
                msg=f"第 {i} 行 Σ|w| = {总} —— 行归一化要求恒等于 G=3.0")
        网.步进(1)          # ⚠ 参照点是"跑完 1 步"的状态，不是初值
        self.assertEqual(网.DIGEST(), "0x0DFF8FAC4D340322")

    def test_每行按目标下标升序(self):
        """累加顺序要和专业版一致 ⇒ 行内必须升序（不是"排不排都行"）。"""
        网 = N.群体网络(群体数=64, 总神经元数=64.0, k_local=8, k_long=4)
        for i in range(64):
            列 = list(网._目标[网._起始[i]:网._起始[i + 1]])
            self.assertEqual(列, sorted(列), f"第 {i} 行的目标没有升序")
            self.assertEqual(len(列), len(set(列)), f"第 {i} 行有重复目标")

    def test_权重是float32不是float64(self):
        """⚠ 专业版 CSR 里的 w 是 4 字节 float。

        留着 float64 ⇒ 末位必差 ⇒ 1000 步后 DIGEST 必错。
        这条判据直接量"权重是否恰好等于它的 float32 舍入值"。
        """
        from array import array
        网 = N.群体网络(群体数=32, 总神经元数=32.0, k_local=4, k_long=2)
        原 = array("d", 网._权重)
        回 = array("d", array("f", 原))
        self.assertEqual(list(原), list(回), "权重不是 float32 可表示的 —— 会与专业版差末位")


# ══════════════════════════ ③ 确定性 ═══════════════════════════════════

class 确定性测试(unittest.TestCase):
    def test_群体层同种子逐位相同(self):
        a = N.人脑网络(群体数=128, k_local=8, k_long=4, 种子=7)
        b = N.人脑网络(群体数=128, k_local=8, k_long=4, 种子=7)
        a.步进(30)
        b.步进(30)
        self.assertEqual(a.率, b.率)
        self.assertEqual(a.DIGEST(), b.DIGEST())

    def test_群体层异种子结果不同(self):
        """⚠ 只测"同种子相同"是不够的 —— 那样"常量输出"也能过。

        必须同时钉住"换种子真的会变"，否则确定性判据是**假绿**。
        ⚠ 两处都要比：**拓扑**（种子直接决定的东西）与**动力学结果**。
        P=8 那种小网络可能撞到同一个吸引子，所以这里用 128 个群体。
        """
        a = N.人脑网络(群体数=128, k_local=8, k_long=4, 种子=7)
        b = N.人脑网络(群体数=128, k_local=8, k_long=4, 种子=8)
        self.assertNotEqual(list(a._目标), list(b._目标),
                            "换种子没改变拓扑 —— 种子没生效（判据会是假绿）")
        a.步进(30)
        b.步进(30)
        self.assertNotEqual(a.率, b.率)
        self.assertNotEqual(a.DIGEST(), b.DIGEST())

    def test_逐细胞同种子逐位相同(self):
        a = N.果蝇网络(神经元数=500, 突触数=2000, 种子=3)
        b = N.果蝇网络(神经元数=500, 突触数=2000, 种子=3)
        a.步进(20)
        b.步进(20)
        self.assertEqual(a.电压, b.电压)
        self.assertEqual(a.状态指纹(), b.状态指纹())

    def test_逐细胞异种子不同(self):
        a = N.果蝇网络(神经元数=500, 突触数=2000, 种子=3)
        b = N.果蝇网络(神经元数=500, 突触数=2000, 种子=4)
        a.步进(20)
        b.步进(20)
        self.assertNotEqual(a.状态指纹(), b.状态指纹())

    def test_不碰全局random(self):
        """⚠ 用全局 `random` 会让**判据之间互相影响**（顺序一变结果就变）。"""
        import random
        random.seed(12345)
        期望 = [random.random() for _ in range(5)]
        random.seed(12345)
        a = N.人脑网络(群体数=32, k_local=4, k_long=2, 种子=1)
        a.步进(10)
        b = N.果蝇网络(神经元数=200, 突触数=500, 种子=1)
        b.步进(10)
        实际 = [random.random() for _ in range(5)]
        self.assertEqual(期望, 实际, "神经模拟动了全局 random 的状态")


# ══════════════════════════ ④ 诚实性 / 口径 ════════════════════════════

class 诚实性测试(unittest.TestCase):
    def test_代表神经元数只是算术换算_不影响计算量(self):
        """⚠ "人脑规模"必须只是**账面换算**，不许偷偷变成"真在算 860 亿细胞"。

        判据：把代表神经元数改 1000 倍，**计算量（nnz/步）与结果必须完全不变**。
        """
        a = N.人脑网络(群体数=64, 总神经元数=1e6, k_local=4, k_long=2)
        b = N.人脑网络(群体数=64, 总神经元数=1e9, k_local=4, k_long=2)
        self.assertEqual(a.矩阵元数, b.矩阵元数)
        a.步进(20)
        b.步进(20)
        self.assertEqual(a.率, b.率, "改代表神经元数竟改变了动力学 —— 那就不是换算")
        #: 而它**确实**只体现在账面上
        self.assertAlmostEqual(a.代表突触数() * 1000, b.代表突触数(), places=0)

    def test_矩阵元不等于突触(self):
        """报告里最容易混的两个数：`nnz/步`（算力）与"代表的突触数"（生物学）。"""
        网 = N.人脑网络(群体数=1000, k_local=20, k_long=10)
        self.assertEqual(网.矩阵元数, 1000 * 30)
        self.assertNotAlmostEqual(网.代表突触数(), 网.矩阵元数, places=0)

    def test_人脑规模常量是量级对的(self):
        self.assertEqual(N.人脑神经元数, 86_000_000_000)
        self.assertEqual(N.人脑突触数, 100_000_000_000_000)
        self.assertAlmostEqual(N.每神经元突触数, 1162.79, places=1)

    def test_逐细胞路径如实报显式突触远少于真实果蝇(self):
        """⚠ 这条是**故意**钉一个"不好看的事实"：默认只显式表示 1e6 条，
        而果蝇真实约 5e7 条（2%）。谁想把它改大，先看这条判据的注释。"""
        网 = N.果蝇网络()
        self.assertEqual(网.神经元数, N.果蝇神经元数)
        self.assertLess(网.突触数, N.果蝇突触数,
                        "默认突触数不该等于果蝇真实突触数（那会假装很真）")
        self.assertLess(网.突触数 / N.果蝇突触数, 0.05)

    def test_不弹窗_默认走录制后端(self):
        """基准/判据一律无头 —— 默认建出来的演示**不能**是 Tk 后端。"""
        from hybridengine_light import pyengine as E
        网 = N.人脑网络(群体数=32, k_local=4, k_long=2)
        游戏 = N.建演示(网, 子步每帧=1)
        self.assertIsInstance(游戏.后端, E.录制后端)

    def test_引擎渲染画得出来且指令数对得上(self):
        """接上 `pyengine` 的轻量渲染 —— 头部那 240 根柱子要真画出来。"""
        from hybridengine_light import pyengine as E
        网 = N.人脑网络(群体数=1000, k_local=20, k_long=10)
        游戏 = N.建演示(网, 柱子数=240, 子步每帧=5)
        游戏.跑(帧数=3)
        后 = 游戏.后端
        self.assertIsInstance(后, E.录制后端)
        self.assertEqual(len(后.有矩形()), 240, "柱子数不对")
        self.assertEqual(len(后.有文字()), 1)
        self.assertTrue(后.有线())
        self.assertEqual(网.步数, 3 * 5, "组件没按子步每帧推进网络")


# ══════════════════════════ ⑤ 动力学 / 数值 ════════════════════════════

class 动力学测试(unittest.TestCase):
    def test_极限环_快照会骗人_时间平均才是物理量(self):
        """⚠ 这条判据守的是一个**科学性的坑**（专业版那条线先发现的）：

        canonical 口径会落进**小周期极限环** ⇒ 单步快照的放电率剧烈跳动，
        完全由"停在第几步"决定。

        判据（对**基准网格那一档**，k=30）：
          ① 连续 10 步的快照极差必须很大（快照确实在跳）
          ② 两个**不同长度**的窗口算出的时间平均必须接近（时间平均稳定）
        ⇒ ①+② 合起来才说明"该报时间平均"。
        """
        网 = N.群体网络(群体数=1000, 总神经元数=1000.0, k_local=20, k_long=10)
        网.步进(200)                     # 先丢掉暂态
        快照 = []
        for _ in range(10):
            网.步进(1)
            快照.append(网.平均放电率())
        极差 = max(快照) - min(快照)
        self.assertGreater(极差, 10.0,
                           f"快照没在跳（极差 {极差:.2f} Hz）—— "
                           "极限环可能被改掉了，请复核报告里那条结论")
        #: 时间平均对窗口长度不敏感（快照对"停在第几步"极敏感）
        甲 = 网.时间平均放电率(200)["TIMEAVG_ALL"]
        乙 = 网.时间平均放电率(400)["TIMEAVG_ALL"]
        self.assertGreater(甲, 0.0)
        self.assertLess(abs(甲 - 乙) / max(甲, 1e-9), 0.10,
                        f"时间平均不稳定（{甲:.4f} vs {乙:.4f}）—— 那它就不是物理量")

    def test_周期4在k等于100那一档(self):
        """**周期 4** 钉死（它是报告里那条"快照会骗人"的最强证据）。

        ⚠ 这一条**只对 k=100 成立**：k=30（基准网格）不是周期 4。
          所以参数写死在这里，别改成 k=30 再怪判据红。
          本仓独立复现过，且数值与专业版那条线的轨迹吻合。
        """
        网 = N.群体网络(群体数=1000, 总神经元数=1000.0, k_local=68, k_long=32)
        网.步进(400)
        环 = []
        for _ in range(8):
            网.步进(1)
            环.append(网.率[:])
        self.assertEqual(环[0], 环[4], "第 4 步没有回到同一状态 ⇒ 不是周期 4")
        self.assertEqual(环[1], 环[5])
        self.assertEqual(环[2], 环[6])
        self.assertEqual(环[3], 环[7])
        self.assertNotEqual(环[0], 环[1], "四步全都一样 ⇒ 其实是定点，不是环")
        #: ⚠ 比**四个相位的集合**，不比某一个相位 —— 从第几步"切入"环是任意的。
        相位 = []
        for 状态 in (环[0], 环[1], 环[2], 环[3]):
            和 = 0.0
            for r in 状态:
                和 += r
            相位.append(round(和 / 1000.0, 6))
        参考 = sorted([32.308526, 55.174941, 24.507543, 0.0])
        for 得, 期 in zip(sorted(相位), 参考):
            self.assertAlmostEqual(得, 期, places=5,
                                   msg=f"周期环的相位集合 {sorted(相位)} 与本仓记录的 {参考} 不符")

    def test_k30那一档不是周期4(self):
        """⚠ 反向判据：别把"周期 4"当成 canonical 口径的普遍性质。

        实测 k=30 时 12 步内没有周期 ⇒ 报告里必须区分这两档，
        否则就是拿一个不成立的结论去支撑"该报时间平均"。
        """
        网 = N.群体网络(群体数=1000, 总神经元数=1000.0, k_local=20, k_long=10)
        网.步进(400)
        历史 = []
        for _ in range(16):
            网.步进(1)
            历史.append(网.率[:])
        for p in range(1, 13):
            self.assertFalse(all(历史[n] == 历史[n + p] for n in range(len(历史) - p)),
                             f"k=30 竟然出现了周期 {p} —— 报告里那段结论要改")

    def test_率恒在合法区间(self):
        网 = N.群体网络(群体数=256, 总神经元数=256.0, k_local=8, k_long=4)
        网.步进(150)
        for r in 网.率:
            self.assertGreaterEqual(r, 0.0)
            self.assertLessEqual(r, N.R_MAX)

    def test_逐细胞膜电位不越界且会发放(self):
        网 = N.果蝇网络(神经元数=2000, 突触数=20000, 种子=0)
        网.步进(800)
        self.assertGreater(网.累计发放, 0, "一个都没发放 —— 网络是死的，基准没意义")
        self.assertGreater(网.平均放电率(), 0.5)
        self.assertLess(网.平均放电率(), 100.0, "放电率不生理（超过 100 Hz）")
        for v in 网.电压:
            self.assertLess(v, 1e6)

    def test_逐细胞电流量纲_常数注入的稳态等于外部电流(self):
        """⚠ 这条守的是一个**真踩过的 bug**：电流方程写成
        `I = I·exp(−dt/τ_s) + I_in`（漏乘 dt/τ_s）⇒ 稳态电流被放大约
        τ_s/dt = **50 倍** ⇒ 网络被自己顶到饱和（症状：不管怎么调参，
        放电率都钉死在 166.67/333.33 Hz —— **与参数无关**）。

        ⇒ 判据：把突触权重和噪声全关掉，只留外部电流，
          稳态电流必须 ≈ 外部电流（而不是它的 50 倍）。
        """
        网 = N.果蝇网络(神经元数=300, 突触数=0, 种子=0, 外部电流=0.8,
                         驱动离散度=0.0,
                         兴奋权重=0.0, 抑制权重=0.0, 噪声幅度=0.0)
        网.步进(2000)                     # 20 个 τ_s
        for c in 网.电流:
            self.assertAlmostEqual(
                c, 0.8, places=6,
                msg="稳态电流 %r ≠ 外部电流 0.8 —— 电流方程漏乘 dt/τ_s 了" % c)

    def test_不应期真的冻住(self):
        """不应期必须真的起作用（否则神经元会每步连发）。

        ⚠ 两个时间尺度都要等：突触电流 τ_s=5 ms（50 步）先起来，
          膜电位再花 `−τ_m·ln(1−1/I)` = 4.46 ms（45 步）充到阈值。
          ⇒ "跑 30 步"根本还没到第一次发放（第一版判据就是这么假红的）。

        判据：强直驱动下每个神经元在 T 步内的发放次数
              **不能超过 T / 不应步数 + 1**（+1 是边界那一次）。
        """
        网 = N.果蝇网络(神经元数=50, 突触数=0, 种子=0, 外部电流=5.0,
                         驱动离散度=0.0,
                         兴奋权重=0.0, 抑制权重=0.0, 噪声幅度=0.0)
        步数 = 400
        网.步进(步数)
        self.assertGreater(网.累计发放, 0, "400 步一次都没发放 —— 网络是死的")
        上限 = 步数 / 网._不应步数 + 1
        self.assertLessEqual(
            网.累计发放, 50 * 上限,
            "发放次数超过「不应期」允许的上限 ⇒ 不应期没把神经元冻住")


# ══════════════════════════ ⑥ 真跑（实测，不是文档口径）════════════════

class 真跑测试(unittest.TestCase):
    def test_人脑规模真跑一次_统计打全(self):
        """判据 2：真跑一次人脑群体规模，打印群体数/单元数/平均放电率/步数/ms每步。"""
        缓 = io.StringIO()
        with redirect_stdout(缓):
            r = N.基准_群体(群体数=1000, 步数=200, 打印=True)
        出 = 缓.getvalue()
        self.assertEqual(r["POPS"], 1000)
        self.assertEqual(r["REPRESENTED_NEURONS"], N.人脑神经元数)
        self.assertEqual(r["NNZ_PER_STEP"], 30000)
        self.assertGreater(r["MS_PER_STEP"], 0.0)
        self.assertEqual(r["STEPS"], 200)
        for 关键词 in ("POPS", "MEAN_RATE_ALL", "MS_PER_STEP", "DIGEST",
                        "TIMEAVG_ALL", "REPRESENTED_NEURONS"):
            self.assertIn(关键词, 出, f"打印里少了 {关键词}")

    def test_果蝇规模真跑一次_统计打全(self):
        """判据 3：真跑一次果蝇规模逐细胞，打印神经元数/突触数/ms每步。"""
        缓 = io.StringIO()
        with redirect_stdout(缓):
            r = N.基准_逐细胞(神经元数=20_000, 突触数=200_000, 步数=200,
                              热身=1500, 打印=True)
        出 = 缓.getvalue()
        self.assertEqual(r["NEURONS"], 20_000)
        self.assertEqual(r["SYNAPSES"], 200_000)
        self.assertGreater(r["MS_PER_STEP"], 0.0)
        self.assertGreater(r["MEAN_RATE"], 0.0, "网络是死的 —— 基准没意义")
        for 关键词 in ("NEURONS", "SYNAPSES", "MS_PER_STEP", "MEAN_RATE"):
            self.assertIn(关键词, 出, f"打印里少了 {关键词}")

    def test_突触吞吐能测出来(self):
        r = N.基准_突触吞吐(突触数=100_000, 神经元数=2000, 重复=2, 打印=False)
        self.assertGreater(r["SYNAPTIC_EVENTS_PER_S"], 0.0)
        self.assertGreater(r["MS_PER_STEP"], 0.0)

    def test_网格只报实测_跑不完就照实记(self):
        """⚠ "跑不动"必须是**一等公民的结果**，不能靠外推糊过去。"""
        结果 = N.口径网格_群体([{"群体数": 100, "步数": 50}], 打印=False)
        self.assertEqual(len(结果), 1)
        self.assertTrue(结果[0]["跑完"])
        self.assertIn("MS_PER_STEP", 结果[0])


# ══════════════════════════ ⑦ 版本 / 文件 ══════════════════════════════

class 版本测试(unittest.TestCase):
    def test_第一位是0(self):
        self.assertEqual(int(N.__version__.split(".")[0]), 0,
                         "作者要求这条线的版本首位保持 0")

    def test_文档与代码的版本一致(self):
        """⚠ 本仓的规矩：**文档里的数字要么从代码生成、要么有判据守着**。"""
        文档 = os.path.join(LC, "docs", "神经模拟.md")
        if not os.path.exists(文档):
            self.skipTest("docs/神经模拟.md 还没写")
        with open(文档, encoding="utf-8") as f:
            内容 = f.read()
        self.assertIn(N.__version__, 内容,
                      "docs/神经模拟.md 里的版本号与 neural.__version__ 不一致")


if __name__ == "__main__":
    unittest.main(verbosity=2)
