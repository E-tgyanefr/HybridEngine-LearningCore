"""neural —— **轻量版的人脑级神经模拟**（纯 Python · 零第三方依赖）。

═══════════════════════════════════════════════════════════════════════
  ⚠⚠ 最先要说清的一件事：**这模拟的是哪一层**
═══════════════════════════════════════════════════════════════════════

人脑有 **860 亿神经元 / 约 100 万亿突触**。「逐细胞」地模拟它，在纯 Python 里
不是"慢"，是**做不到** —— 本仓实测逐细胞路径约 **0.06 ms / 神经元 / 步**
（见 `docs/神经模拟.md` 的实测表），乘 8.6×10¹⁰ 个神经元
⇒ 模拟**一步**要 **≈ 60 天**。量级差着十个数量级，不是调参能救的。

⇒ 所以这里走**两条不同层的路**，两条都必须标明"是哪一层"：

    ① `群体网络`  —— **大规模群体 / mean-field 层**（人脑级）
       一个「群体」= 一大群同质神经元（默认 8600 万个），
       状态是**群体的平均放电率**（一个标量），**不是**任何单个细胞的膜电位。
       这是神经科学里正经的方法（Wilson–Cowan / Amari / Wong–Wang /
       The Virtual Brain 用的都是这一层），**不是"糊弄"**。

    ② `脉冲网络`  —— **逐细胞层**（果蝇级，约 10 万神经元）
       每个神经元一个 LIF 膜电位，突触按事件逐条投递。

**这一层实现了什么 / 没实现什么**（⚠ 不许读成"模拟了人脑"）：

    ✅ 模拟了   群体平均放电率的动力学（率映射 + 群体间连接矩阵）
    ✅ 模拟了   群体之间的有向连接、兴奋/抑制符号、环路结构（局部环 + 长程）
    ✅ 模拟了   （逐细胞那条线）单个 LIF 神经元的膜电位与事件驱动突触投递
    ❌ 没模拟   单个神经元的膜电位 / 树突形态 / 离子通道（**人脑**那条线）
    ❌ 没模拟   逐突触的权重与可塑性（群体层只有"有效耦合强度"）
    ❌ 没模拟   空间结构（皮层柱/层、轴突传导延迟、几何）
    ❌ 没模拟   任何"意识 / 认知"层面的东西 —— 这是动力学模型，不是脑

═══════════════════════════════════════════════════════════════════════
  群体层的更新方程（**canonical 口径** —— 写死在这里，因为它就是"口径"）
═══════════════════════════════════════════════════════════════════════

⚠⚠ 这一节的方程**必须与专业版（C++）逐字一致**，否则"慢多少倍"比的是两件事。
   口径是与 `neural-pro`（专业版那条线）对齐后钉下来的，规格如下：

    P 个群体；`region = i/2`，`type = i%2`（0 = 兴奋 E，1 = 抑制 I）
    每个群体的状态 = **一个标量** r_i（平均放电率，Hz）

    每步两件事（同步更新，**先算完全部输入再改率**）：

        (1) I_i = I_ext_i + Σ_j W_ij · r_j
        (2) r_i ← f(I_i)

    非线性（**刻意不含超越函数** —— 两个语言的 `exp` 会差最后一位，
    纯算术才可能跨语言逐位相同）：

        f(I) = 0                     , I ≤ 0
             = R_MAX · I / (I + I_SAT), I > 0        R_MAX = 100.0, I_SAT = 20.0

    外部输入（确定性、无 sin/cos，纯算术）：

        I_ext_i = I_EXT[type_i] · (1 + 0.5·((i % 17)/8.0 − 1))
        I_EXT[E] = 12.0, I_EXT[I] = 8.0

    初值：r_i = 5.0（全同，不随机）

    拓扑（**第 i 行恰好 k = k_local + k_long 个非零**，Dale 原则：一个源只出
    一种符号）：

      · local（k_local 个）：目标 `(i ± 1 … i ± k_local/2) mod P`，
        `kernel = 1.0/d`（d = 环上群体距离 = |offset|）
      · long（k_long 个）：每行独立 RNG 抽，`kernel = 1.0/(k_local/2)`
      · 符号 `gain[target_type][source_type]`：
        [E][E] = +1.0 · [E][I] = −1.0 · [I][E] = +1.0 · [I][I] = −0.5
      · `raw_j = gain · kernel_j`，再**行归一化**
        `w_ij = raw_j · (G / Σ_j |raw_j|)`，G = 3.0
        ⇒ 每行 Σ|w| **恒 = 3.0**（输入尺度与 k 解耦；
          不归一化的话 P 一变活动就爆或死）
      · **每行按目标下标升序排序**后再存 CSR ⇒ 两种语言的累加顺序一致
        ⇒ 浮点求和也可能逐位一致

    每行 RNG（只用于抽 long-range 目标；**每行独立** ⇒ 与并行/顺序无关）：

        s = splitmix64(seed·0x9E3779B97F4A7C15 + i)
        next(): s ^= s<<13; s ^= s>>7; s ^= s<<17; return s
        抽样：j = next() % P；若 j == i 或已在行内 ⇒ 重试（≤64 次）；
              仍失败 ⇒ 从 i+1 起线性找第一个空位
        抽完整行按目标下标排序

    ⚠ 一个必须分开写的账（**不许混成一个数**）：

      · `nnz/步` = P·k  —— **CPU 真算的**（每步的乘加次数）⇒ 性能口径
      · "代表的突触数" = 神经元的突触总数 —— **算术推算**，不是 CPU 算的
        1 个矩阵元代表"群体 j 全体 → 群体 i 全体"的总有效耦合，
        背后压着 p·N_i·N_j 条真实突触。**矩阵元 ≠ 突触。**

    ⚠⚠ **要逐位对上，还有三条"末位级"的规矩**（都实测踩过，别凭直觉改）：

      ① **权重存 float32**。专业版 CSR 里的 `w` 是 4 字节 `float`，
         matvec 时才提升成 `double`。这边留着 float64 ⇒ 末位必差。
         ⇒ `array("d", array("f", 值们))`（建 float32 再回读成 double）。
      ② **累加顺序**：`acc = I_ext`，再**按目标下标升序**逐项 `acc += w·r`。
         ⇒ 所以每行必须**排序后**才存（本模块就是这么建的）。
      ③ **绝对不能用 `sum()`**：Python 3.12 的 `sum()` 对 float 走
         **Neumaier 补偿求和**（实测 `sum([0.1]*10) == 1.0`，
         而朴素循环给 `0.99999999999999988898`）。
         拿它当"顺序累加"用 ⇒ 1000 步之后 DIGEST 必错。
         （`itertools.accumulate` 是朴素累加、能对上，但慢 ~20% ⇒ 用显式循环。）

      "对没对上"不靠嘴说：`核对参照()` 跑 `参照用例` 那张表，
      和专业版 C++ 实测的 `MEAN_RATE_ALL` / `DIGEST` 逐个比。

═══════════════════════════════════════════════════════════════════════
  逐细胞层的更新方程（`脉冲网络`）
═══════════════════════════════════════════════════════════════════════

    (3) v_k ← v_k + dt·(−v_k + I_k)/τ_m          （不应期内冻结在重置电位）
    (4) I_k ← I_k·exp(−dt/τ_s) + 外部电流 + 噪声
    (5) v_k ≥ 阈值 ⇒ 记一次发放、v_k ← 重置、进入不应期、投递它的所有出边

═══════════════════════════════════════════════════════════════════════
  零第三方依赖 + 确定性 + 无窗口
═══════════════════════════════════════════════════════════════════════

· **只用标准库**（math / random / time / hashlib / array）—— 本仓铁律
  （见 `hybridengine_light/__init__.py` 开头那段）。
  ⚠ 连 numpy 都没有 ⇒ 群体层的矩阵向量乘是**纯 Python 循环**，
    这正是"轻量版慢"的根源，也是报告里那条倍数的来源。
· **确定性**：所有随机数来自**本对象自己的** RNG（群体层用规格里的
  splitmix64/xorshift，逐细胞层用 `random.Random(种子)`），**不碰全局 `random`**；
  权重生成顺序固定；求和顺序固定（不做并行归约）
  ⇒ **同种子 ⇒ 逐位相同**（`状态指纹()` / `DIGEST` 就是钉这条的）。
· **无窗口**：本模块**不 import tkinter**。要看窗口得显式建 `pyengine.Tk后端`；
  基准与判据一律走 `pyengine.录制后端`（见 `建演示`）。
"""
from __future__ import annotations

import hashlib
import random
import time
from array import array

__version__ = "0.1.0"          # ⚠ 首位保持 0：这条线还没到 1.0

__all__ = [
    "人脑神经元数", "人脑突触数", "果蝇神经元数", "果蝇突触数", "每神经元突触数",
    "R_MAX", "I_SAT", "I_EXT", "G_默认",
    "群体", "群体网络", "脉冲网络",
    "人脑网络", "果蝇网络",
    "基准_群体", "基准_逐细胞", "基准_突触吞吐", "口径网格_群体", "打印网格",
    "参照用例", "核对参照",
    "fnv1a64", "状态指纹",
    "神经组件", "建演示",
]

# ══════════════════════════ 生物学常量（口径锚点）══════════════════════════

#: 人脑神经元数（Azevedo et al. 2009 常被引用的量级：8.6×10¹⁰）
人脑神经元数 = 86_000_000_000

#: 人脑突触数（常被引用的量级：~10¹⁴，即 100 万亿）
人脑突触数 = 100_000_000_000_000

#: 平均每个神经元的突触数 —— 由上面两个数除出来（1e14 / 8.6e10 ≈ 1163）
每神经元突触数 = 人脑突触数 / 人脑神经元数

#: 果蝇全脑神经元数（FlyWire 全脑约 1.4×10⁵，常被说成"约 10 万"）
果蝇神经元数 = 100_000

#: 果蝇全脑突触数（FlyWire 量级 ~5×10⁷）。
#: ⚠ 本模块**默认不显式表示这么多** —— 见 `脉冲网络` 的说明。
果蝇突触数 = 50_000_000

#: canonical 口径的模型常量 —— **改了就不再与专业版同口径**
R_MAX = 100.0        # 最大放电率 (Hz)
I_SAT = 20.0         # 饱和电流尺度
I_EXT = (12.0, 8.0)  # (兴奋, 抑制) 的外部输入
G_默认 = 3.0         # 每行 Σ|w| 的目标值

#: 64 位掩码（Python 的 int 是任意精度，必须自己截回 64 位才能对齐 C 的 uint64）
_M64 = (1 << 64) - 1
_GOLDEN = 0x9E3779B97F4A7C15


def _splitmix64(x: int) -> int:
    """标准 splitmix64 的**输出混合**（含内部的 +GOLDEN 一步）。

    ⚠ 这一条是与专业版对齐时的**唯一歧义点**（已在协作消息里问过）：
      `splitmix64(x)` 到底含不含内部那句 `x += 0x9E3779B97F4A7C15`，
      两种读法差一个常量 ⇒ 抽出来的 long-range 目标不同 ⇒ DIGEST 不同。
      **本实现取"含"**（即标准 splitmix64 的定义）。若两边 DIGEST 对不上，
      第一个要查的就是这里 —— 但那**不影响 ms/步 的可比性**
      （每行的非零个数 k 与每步的乘加次数 nnz 与 RNG 无关）。
    """
    x = (x + _GOLDEN) & _M64
    z = x
    z = ((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9) & _M64
    z = ((z ^ (z >> 27)) * 0x94D049BB133111EB) & _M64
    return z ^ (z >> 31)


def fnv1a64(数据: bytes) -> int:
    """FNV-1a 64 —— 与专业版 `DIGEST` 同一套算法（用来跨语言对答案）。"""
    h = 0xCBF29CE484222325
    for b in 数据:
        h ^= b
        h = (h * 0x100000001B3) & _M64
    return h


def 状态指纹(数组们) -> str:
    """把若干数值序列压成 hex —— 判据用它钉"同种子 ⇒ 同结果"。

    ⚠ 用 `tobytes()` 而不是 `repr()`：`tobytes` 是**逐字节**的，
      连 −0.0 与 0.0 的差别都留着，更适合当"逐位相同"的证据；
      而且它和 C++ 里那块 `double[]` 的内存字节**是同一串**。
    """
    h = hashlib.md5()
    for a in 数组们:
        h.update(array("d", a).tobytes())
    return h.hexdigest()


# ══════════════════════════ 群体（mean-field 单元）══════════════════════════

class 群体:
    """一个 **mean-field 群体** —— 代表的是一大群同质神经元，不是单个细胞。

    ⚠ `单元数` 是"这个群体代表了几个真实神经元"，**不是**"网络里有几个对象"。
      网络里只有 `群体数` 个标量状态；8600 万个神经元被压成 1 个放电率。
      这就是 mean-field 的定义，也正是它能跑得动的原因（也是它的代价）。
    """

    __slots__ = ("名字", "单元数", "兴奋性", "外部输入")

    def __init__(self, 名字: str, 单元数: float, 兴奋性: bool = True,
                 外部输入: float = 0.0) -> None:
        self.名字 = str(名字)
        self.单元数 = float(单元数)
        self.兴奋性 = bool(兴奋性)
        self.外部输入 = float(外部输入)

    def __repr__(self) -> str:
        return "<群体 %s 单元=%.3g %s I_ext=%.2f>" % (
            self.名字, self.单元数, "兴奋" if self.兴奋性 else "抑制", self.外部输入)


# ══════════════════════════ 群体网络（人脑级）══════════════════════════════

class 群体网络:
    """大规模**群体 / mean-field** 网络 —— 人脑级那条线走这里。

    ## 规模怎么读（⚠ 两个数必须分开）

    `群体数 P` × `单元每群` = `代表单元数`（默认到 8.6×10¹⁰ = 人脑量级）。
    ⚠ `代表单元数` **只是账面上的算术换算**，它**不影响计算量** ——
      计算量由 `nnz/步 = P·k` 决定。这正是 mean-field 能让"人脑规模"跑得动
      的原因，也正是它**没有**在逐细胞模拟任何东西的证据。报告里两个数分开写。

    ## 规格

    见模块开头那一节 —— 那份方程就是口径，本类是它的 Python 实现。
    与专业版对齐的三件"逐位"要求：**行归一化**（Σ|w| 恒 = G）、
    **每行按目标下标排序**（累加顺序一致）、**RNG 每行独立**（与遍历顺序无关）。
    """

    def __init__(self, 群体数: int = 1000, 总神经元数: float = 人脑神经元数, *,
                 k_local: int = 20, k_long: int = 10, G: float = G_默认,
                 种子: int = 12345, dt: float = 1e-4,
                 抑制比例: float = 0.5) -> None:
        if 群体数 <= 0:
            raise ValueError("群体数要为正")
        if k_local < 0 or k_long < 0 or (k_local + k_long) == 0:
            raise ValueError("k_local + k_long 必须 > 0（否则没有任何连接）")
        if k_local % 2:
            raise ValueError("k_local 必须是偶数（本地邻域是 ±成对取的）")
        if 抑制比例 != 0.5:
            #: ⚠ canonical 口径里 type = i%2 ⇒ 兴奋/抑制**各占一半**。
            #:   这与人脑"抑制性神经元约 20%"不同 —— 但**口径优先于我的偏好**：
            #:   擅自改成 8:2 就不再与专业版同口径了。要改必须两边一起改。
            raise ValueError(
                "canonical 口径固定 type = i%%2 ⇒ 抑制比例只能是 0.5；"
                "给人脑 20%% 抑制的版本请用 `人脑比例网络`（会离开同口径）")

        self.群体数 = P = int(群体数)
        self.总神经元数 = float(总神经元数)
        self.k_local = int(k_local)
        self.k_long = int(k_long)
        self.k = self.k_local + self.k_long
        self.G = float(G)
        self.种子 = int(种子)
        self.dt = float(dt)
        self.抑制比例 = 0.5
        self.步数 = 0

        每群 = self.总神经元数 / P
        半 = self.k_local // 2
        self.群体们 = []
        self.外部 = [0.0] * P
        self.兴奋性 = [True] * P
        self.单元数 = [每群] * P
        for i in range(P):
            类型 = i % 2                      # 0 = E, 1 = I
            兴奋 = (类型 == 0)
            self.兴奋性[i] = 兴奋
            #: I_ext_i = I_EXT[type] · (1 + 0.5·((i%17)/8 − 1))  —— 纯算术，确定性
            self.外部[i] = I_EXT[类型] * (1.0 + 0.5 * ((i % 17) / 8.0 - 1.0))
            self.群体们.append(群体(
                名字=("%s%05d" % ("E" if 兴奋 else "I", i)),
                单元数=每群, 兴奋性=兴奋, 外部输入=self.外部[i]))

        #: 初值全同、不随机（口径就这么定的）
        self.率 = [5.0] * P
        self._新 = [0.0] * P
        #: 兴奋/抑制的下标表（时间平均要用 `sum(map(...))` 在 C 层跑）
        self._兴奋索引 = [i for i in range(P) if self.兴奋性[i]]
        self._抑制索引 = [i for i in range(P) if not self.兴奋性[i]]

        self._建连接(半)

    # ── 连接 ──────────────────────────────────────────────────────────
    def _建连接(self, 半: int) -> None:
        """建 CSR（**每行按目标下标升序**）。生成顺序固定 ⇒ 同种子逐位相同。"""
        P, k_long = self.群体数, self.k_long
        #: gain[目标类型][源类型]
        gain = ((1.0, -1.0), (1.0, -0.5))
        长核 = (1.0 / 半) if 半 else 1.0        # long-range 的 kernel
        起始 = array("i", [0])
        目标 = array("i")
        权重 = array("d")
        每一行 = []                            # 预抽的行切片（性能：见 _一步）
        每一目标 = []

        for i in range(P):
            # ① local：i ± 1 … i ± 半，kernel = 1/d
            行 = {}
            for d in range(1, 半 + 1):
                行[(i + d) % P] = 1.0 / d
                行[(i - d) % P] = 1.0 / d
            # ② long：每行独立 RNG（种子只由 seed 与行号决定 ⇒ 与遍历顺序无关）
            局部状态 = _splitmix64((self.种子 * _GOLDEN + i) & _M64)
            if 局部状态 == 0:                   # 规格里的兜底（实际不触发）
                局部状态 = _GOLDEN
            抽到 = 0
            #: ⚠⚠ 循环语义**必须逐字照抄**：最多 64 次迭代，**被拒也消耗一次
            #:   `next()`**。写成"最多 k_long·64 次"或"只在接受时推进 RNG"，
            #:   抽出来的目标就完全不同 ⇒ DIGEST 对不上（实测踩过）。
            for _ in range(64):
                if 抽到 >= k_long:
                    break
                局部状态 = _下一个状态(局部状态)
                j = 局部状态 % P
                if j == i or j in 行:
                    continue
                行[j] = 长核
                抽到 += 1
            if 抽到 < k_long:
                #: 退化路径：从 (i+1)%P 起线性找第一个空位。
                #: ⚠ 这条**不消耗任何 next()**（规格里写死的兜底）。
                偏移 = 1
                while 抽到 < k_long and 偏移 <= P:
                    j = (i + 偏移) % P
                    偏移 += 1
                    if j != i and j not in 行:
                        行[j] = 长核
                        抽到 += 1
            # ③ 排序（**累加顺序两边必须一致**）+ 符号 + 行归一化
            目标们 = sorted(行)
            raw = [gain[i % 2][j % 2] * 行[j] for j in 目标们]
            总 = 0.0
            for x in raw:
                总 += x if x >= 0.0 else -x
            比例 = (self.G / 总) if 总 else 0.0
            #: ⚠⚠ **权重存成 float32**（专业版 CSR 里的 `w` 就是 4 字节 float，
            #:   matvec 时才提升成 double）。留着 float64 ⇒ 末位必差 ⇒ DIGEST 对不上。
            #:   写法 `array("d", array("f", vals))`：建 float32 数组再回读成 double，
            #:   两步都在 C 层，比 `struct.pack/unpack` 逐元素快一个量级。
            本条权重 = array("d", array("f", [x * 比例 for x in raw]))
            本条目标 = array("i", 目标们)
            目标.extend(本条目标)
            权重.extend(本条权重)
            起始.append(len(目标))
            每一目标.append(本条目标)
            每一行.append(本条权重)

        self._起始 = 起始
        self._目标 = 目标
        self._权重 = 权重
        self._权行 = 每一行
        self._目标行 = 每一目标
        self.矩阵元数 = len(目标)          # = P·k（nnz）
        self.每行突触 = self.矩阵元数 // P if P else 0

    # ── 一步 ──────────────────────────────────────────────────────────
    def 步进(self, 步数: int = 1) -> None:
        """跑 `步数` 个 dt（**纯计算，不涉及任何窗口/渲染**）。"""
        if 步数 < 0:
            raise ValueError("步数不能为负")
        一步 = self._一步
        for _ in range(int(步数)):
            一步()

    def _一步(self) -> None:
        """一个 dt：`I = I_ext + W·r` → `r ← f(I)`（同步更新）。"""
        P = self.群体数
        率 = self.率
        取 = 率.__getitem__
        权行, 目标行 = self._权行, self._目标行
        外部, 新 = self.外部, self._新
        每行 = self.k
        R = R_MAX
        S = I_SAT
        #: ⚠⚠ 累加**必须**写成 `acc = I_ext; for p: acc += w[p]*r[t[p]]`：
        #:
        #:   ① 起点是 `I_ext`（不是 0），再**按目标下标升序**逐项加
        #:      —— 专业版就是这么写的，顺序差一个 ⇒ 末位就差。
        #:   ② **绝对不能用 `sum(...)`**：Python 3.12 的 `sum()` 对 float
        #:      走的是 **Neumaier 补偿求和**（实测 `sum([0.1]*10) == 1.0`，
        #:      而朴素循环给 0.99999999999999988898）⇒ 拿它当"顺序累加"用，
        #:      1000 步之后 DIGEST 必错。这条是实测踩出来的，见 docs。
        #:      （`itertools.accumulate` 是朴素累加、能对上，但比显式循环慢
        #:       ~20%（22 M vs 27 M nnz/s），所以这里用显式循环。）
        for i in range(P):
            w = 权行[i]
            t = 目标行[i]
            acc = 外部[i]
            for p in range(每行):
                acc += w[p] * 取(t[p])
            if acc <= 0.0:
                新[i] = 0.0
            else:
                新[i] = R * acc / (acc + S)
        #: 就地换（不重新分配两个 P 长的列表）
        self.率, self._新 = 新, 率
        self.步数 += 1

    # ── 统计 ──────────────────────────────────────────────────────────
    def 分侧平均(self) -> tuple[float, float]:
        """(兴奋性群体平均放电率, 抑制性群体平均放电率)（Hz，算术平均）。

        ⚠ canonical 口径下每个群体的 `单元数` 相同 ⇒ 算术平均与按单元加权
          **数值相同**，不存在"用哪种平均"的歧义。
        ⚠ 同样用朴素顺序求和（见 `平均放电率`）。
        """
        和 = [0.0, 0.0]
        数 = [0, 0]
        for r, e in zip(self.率, self.兴奋性):
            k = 0 if e else 1
            和[k] += r
            数[k] += 1
        return (和[0] / 数[0] if 数[0] else 0.0,
                和[1] / 数[1] if 数[1] else 0.0)

    def 平均放电率(self) -> float:
        """**最后一步快照**的全群体平均放电率（Hz）—— 与专业版 `MEAN_RATE_ALL` 同口径。

        ⚠⚠ **这个数有可能是假的** —— 见 `时间平均放电率`。canonical 口径会落进
          **小周期极限环**（k=100 那一档是周期 4；k=30 那一档不是周期 4，
          但同样剧烈跳动）⇒ 快照读数**完全由"停在第几步"决定**。
          实测（P=1000, k=30）：连续 10 步的 `MEAN_RATE_ALL` 是
          `35.28 → 0.58 → 29.65 → 48.97 → 9.43 → 4.43 → 33.57 → …`
          —— 相邻两步能差 10 倍以上。
          ⇒ 报"平均放电率"**必须报时间平均**；快照只用来跟专业版对 DIGEST。

        ⚠ 用**朴素顺序求和**（不用 `math.fsum`）：`fsum` 是精确求和，
          和专业版那个 `for` 循环累加出来的末位不一样 ⇒ 打印值会差最后几位。
          （所以本模块**不 import math** —— 一行 `import` 都省掉，
           `test_只导入标准库和本包` 那条判据顺手也钉着这一点。）
        """
        总 = 0.0
        for r in self.率:
            总 += r
        return 总 / self.群体数 if self.群体数 else 0.0

    def 时间平均放电率(self, 步数: int) -> dict:
        """再跑一趟（**不计时**）累积 Σ_t Σ_i r_i，给出**时间平均**放电率。

        ⚠⚠ 为什么必须有这条：canonical 口径会落进**小周期极限环**，
          单步快照的放电率剧烈跳动 —— "网络是不是死了"完全取决于停在第几步。
          **时间平均才是物理量。**
          · ★ **周期 4**：k=100 那一档（P=1000 / P=10000 都复现，本仓独立验证过，
            数值与专业版那条线的轨迹逐位吻合）
          · k=30 那一档（本报告的基准网格）**不是**周期 4，但快照同样在跳
            （连续 10 步：35.28 → 0.58 → 29.65 → 48.97 → …）
          ⇒ 两种情况下结论一样：**别用快照当结论**。
          （这条最早是专业版那条线发现的，见 `docs/神经模拟.md` 的原始轨迹。）

        ⚠ 求和口径：这里按**每步一次 `sum()`** 再累加，而专业版是
          "把所有 r 累进同一个和"。两者差在 1e-16 相对量级，
          所以这个数**比到 4~5 位有效数字即可**（不像 DIGEST 要求逐位）。
        """
        网 = 群体网络(群体数=self.群体数, 总神经元数=self.总神经元数,
                      k_local=self.k_local, k_long=self.k_long, G=self.G,
                      种子=self.种子, dt=self.dt)
        兴, 抑 = 网._兴奋索引, 网._抑制索引
        和兴 = 和抑 = 0.0
        for _ in range(int(步数)):
            网.步进(1)
            #: ⚠ `取` 必须**每步重新绑** —— `_一步` 走的是"两个列表互换"
            #:   （不重新分配），绑一次的话第二步就读到上一轮的旧列表了。
            取 = 网.率.__getitem__
            和兴 += sum(map(取, 兴))
            和抑 += sum(map(取, 抑))
        n步 = max(1, int(步数))
        return {
            "TIMEAVG_STEPS": 步数,
            "TIMEAVG_E": 和兴 / (len(兴) * n步) if 兴 else 0.0,
            "TIMEAVG_I": 和抑 / (len(抑) * n步) if 抑 else 0.0,
            "TIMEAVG_ALL": (和兴 + 和抑) / (self.群体数 * n步),
        }

    def 代表突触数(self) -> float:
        """**算术推算**的代表突触总数 —— ⚠ **不是** CPU 算的数。

        一个矩阵元代表"群体 j 全体 → 群体 i 全体"的总有效耦合，
        背后压着 p·N_i·N_j 条真实突触。整个网络代表的神经元有
        `总神经元数` 个、每个神经元约 `每神经元突触数` 条突触
        ⇒ 代表突触数 ≈ 总神经元数 × 每神经元突触数（人脑量级 1×10¹⁴）。
        **这个数一分钱算力都没花**，只是把生物学账本写出来。
        性能口径看 `矩阵元数`（每步真正做的乘加次数）。
        """
        return self.总神经元数 * 每神经元突触数

    def DIGEST(self) -> str:
        """最终状态 `double[]` 原始字节的 FNV-1a 64，格式 `0x` + 16 位**大写** hex。

        ⚠ 与专业版 `DIGEST=0x%016llX` **同算法、同字节**（x86 小端 IEEE-754）
          ⇒ 两个语言如果实现的是同一个计算，这个值必须**逐位相同**。
          比对时**比值不比字符串**（大小写无所谓）。
        """
        return "0x%016X" % fnv1a64(array("d", self.率).tobytes())

    def 统计(self) -> dict:
        分兴奋, 分抑制 = self.分侧平均()
        return {
            "POPS": self.群体数,
            "REPRESENTED_NEURONS": self.总神经元数,
            "NNZ": self.矩阵元数,
            "NNZ_PER_STEP": self.矩阵元数,
            "K": self.k,
            "MEAN_RATE_E": 分兴奋,
            "MEAN_RATE_I": 分抑制,
            "MEAN_RATE_ALL": self.平均放电率(),
            "STEPS": self.步数,
            "DT": self.dt,
            "REPRESENTED_SYNAPSES": self.代表突触数(),
            "DIGEST": self.DIGEST(),
        }

    def __repr__(self) -> str:
        return ("<群体网络 P=%d k=%d nnz/步=%d 代表神经元=%.3g 步数=%d r̄=%.2f Hz>"
                % (self.群体数, self.k, self.矩阵元数, self.总神经元数,
                   self.步数, self.平均放电率()))


def _下一个状态(s: int) -> int:
    """规格里的 xorshift64：`s ^= s<<13; s ^= s>>7; s ^= s<<17; return s`。

    ⚠ 每步都要 `& _M64` —— Python 的 int 不会溢出，C 的 `uint64_t` 会。
      不截位的话两边抽出来的目标完全不同（而 DIGEST 会对不上）。
    """
    s ^= (s << 13) & _M64
    s ^= (s >> 7)
    s ^= (s << 17) & _M64
    return s & _M64


# ══════════════════════════ 脉冲网络（果蝇级 · 逐细胞）══════════════════════

class 脉冲网络:
    """**逐细胞** LIF 网络 —— 果蝇规模（约 10 万神经元）。

    ## ⚠⚠ 两个必须照实说的近似

    **① 突触数是"显式表示"的，不是果蝇真实的 5×10⁷ 条。**
      真实果蝇全脑（FlyWire 量级）约 1.4e5 神经元 / 约 5e7 突触
      （平均出度 ~350）。纯 Python 里把 5e7 条突触塞进 `array` 要
      ~200 MB(索引) + ~400 MB(权重)，还要每步扫它们 —— **不现实**。
      ⇒ 本实现的 `突触数` 默认 **1e6**（平均出度 10），只占真实的 2%。
        缺掉的连通性用**背景外部电流**顶上（真实脑里也有大量背景输入）。
      ⇒ 报告里两个数**必须分开写**：「显式突触数」与「果蝇真实突触数」。

    **② 事件驱动投递。**
      每步只有**这一步发放了**的神经元才去投递自己的出边。
      所以 `ms/步` 随活动量变 —— 这是真实脉冲网络的性质，
      但报告里必须标清"在哪个放电率下测的"。

    **③ 默认参数落在哪个动力学区间**（⚠ 不写清的话数字没法读）：
      默认是**强直驱动**（`外部电流 = 1.20`，阈值 1.0）+ 每个神经元一个
      **固定的**外部电流（"淬火无序"，`驱动离散度 = 0.10`）
      ⇒ 平均放电率 **~16 Hz**（实测，见 docs）。
      **它不是一个"自发活动网络"** —— 每神经元只有 10 条突触
      （真实的 ~1/35），撑不起自发持续放电；把驱动降到阈值以下，
      网络就静默（这也是实测过的）。要自发活动就得显式表示更多突触。

      ⚠ 为什么用"固定的异质驱动"而不是每步抽噪声：见 `_一步` 里那段（实测三条理由）。
    """

    def __init__(self, 神经元数: int = 果蝇神经元数, 突触数: int = 1_000_000, *,
                 兴奋比例: float = 0.8, 种子: int = 0, dt: float = 1e-4,
                 膜时间常数: float = 2e-2, 突触时间常数: float = 5e-3,
                 阈值: float = 1.0, 重置: float = 0.0, 不应期: float = 2e-3,
                 外部电流: float = 1.20, 驱动离散度: float = 0.10,
                 兴奋权重: float = 0.3, 抑制权重: float = -1.2,
                 噪声幅度: float = 0.0) -> None:
        if 神经元数 <= 0:
            raise ValueError("神经元数要为正")
        if 突触数 < 0:
            raise ValueError("突触数不能为负")
        if not (0.0 < 兴奋比例 < 1.0):
            raise ValueError("兴奋比例要在 (0,1) 开区间内")

        self.神经元数 = N = int(神经元数)
        self.突触数 = int(突触数)
        self.dt = float(dt)
        self.阈值 = float(阈值)
        self.重置 = float(重置)
        self.不应期 = float(不应期)
        self.外部电流 = float(外部电流)
        self.驱动离散度 = float(驱动离散度)
        self.噪声幅度 = float(噪声幅度)
        self.兴奋权重 = float(兴奋权重)
        self.抑制权重 = float(抑制权重)
        self.种子 = int(种子)
        self.步数 = 0
        self.累计发放 = 0
        self.上一步发放数 = 0
        self.上一步发放: list[int] = []
        #: 最近若干次发放 `(神经元下标, 步号)` —— **只为画栅格**（`神经组件`）。
        #: ⚠ 有上限：不设上限的话跑久了内存一直涨（10 万神经元 × 16 Hz 很可观）。
        self.最近发放: list[tuple[int, int]] = []
        self.最近上限 = 4000

        self._随机 = random.Random(int(种子))
        self._逆τm = 1.0 / float(膜时间常数)
        self._逆τs = 1.0 / float(突触时间常数)

        n兴奋 = int(N * 兴奋比例)
        self.兴奋性 = [True] * n兴奋 + [False] * (N - n兴奋)

        # ── 状态 ──
        self.电压 = [0.0] * N
        self.电流 = [0.0] * N
        #: 不应期剩余步数（只存**正在不应期**的那些 ⇒ 通常很短）
        self._不应: dict[int, int] = {}
        self._不应步数 = max(1, int(round(self.不应期 / self.dt)))

        self._建突触()
        self._系数 = self.dt * self._逆τs          # dt/τ_s
        self._衰减 = 1.0 - self._系数              # 1 − dt/τ_s

        #: ⚠⚠ **每个神经元一个固定的外部电流**（"淬火无序"），不是全体一样、
        #:   也不是每步重抽的噪声。三条理由，都是实测逼出来的：
        #:
        #:   ① **每步重抽噪声**要 10⁵ 次 `gauss()`/步 ⇒ 光随机数就多花 ~30 ms/步，
        #:      而那是 RNG 的开销、不是"神经计算"的开销 —— 混进 ms/步就是脏数据。
        #:   ② **噪声加在突触电流上会被 τ_s 低通掉**：实测 σ 从 0.5 调到 10，
        #:      平均放电率一直是 0（滤波后涨落太小，跨不过阈值）。
        #:   ③ **异质驱动**本身是真实的：真实神经元兴奋性各不相同，
        #:      同一片脑区里本来就有一部分静默、一部分在放电。
        #:   ⇒ 代价是"发放规则"（不是不规则发放）。这条在文档里明说。
        随机起 = random.Random(int(种子) ^ 0x5EED)
        均值, 半宽 = self.外部电流, abs(self.驱动离散度)
        self._外部列表 = [均值 + 半宽 * (2.0 * 随机起.random() - 1.0)
                          for _ in range(N)]

    def _建突触(self) -> None:
        """建**前神经元 → 后神经元**的 CSR 出边表（顺序固定 ⇒ 确定性）。

        ⚠ 用 `k·N/S` 均匀铺前神经元，而不是"随机抽前神经元"：
          随机抽会让出度分布很散（有的神经元 0 条、有的 3 条），
          而"平均出度"会变成一个没意义的数。均匀铺 ⇒ 每个神经元出度
          ≈ S/N，报出来的 `平均出度` 才是真的。
        """
        N, S = self.神经元数, self.突触数
        随机 = self._随机
        出_起始 = array("i")
        出_目标 = array("i")
        出_权 = array("d")
        兴奋性 = self.兴奋性
        if S:
            for k in range(S):
                前 = (k * N) // S
                后 = 随机.randrange(N)
                出_目标.append(后)
                出_权.append(self.兴奋权重 if 兴奋性[前] else self.抑制权重)
            每 = S // N
            余 = S % N
            出_起始.append(0)
            for i in range(N):
                出_起始.append(出_起始[-1] + 每 + (1 if i < 余 else 0))
        else:
            出_起始.extend([0] * (N + 1))
        self._出_起始 = 出_起始
        self._出_目标 = 出_目标
        self._出_权 = 出_权

    # ── 一步 ──────────────────────────────────────────────────────────
    def 步进(self, 步数: int = 1) -> None:
        for _ in range(int(步数)):
            self._一步()

    def _一步(self) -> None:
        dt = self.dt
        逆τm = self._逆τm
        电压, 电流 = self.电压, self.电流

        # (3) 膜电位积分 —— 一个 list 推导就是 N 次解释器循环（慢的根源之一）
        新电压 = [v + dt * (-v + c) * 逆τm for v, c in zip(电压, 电流)]

        # 不应期：逐个递减计数，到点的放出来；还在不应期的冻结在重置电位
        不应 = self._不应
        if 不应:
            到点 = []
            for k in 不应:
                不应[k] -= 1
                if 不应[k] <= 0:
                    到点.append(k)
            for k in 到点:
                del 不应[k]
            重置 = self.重置
            for k in 不应:
                新电压[k] = 重置
        self.电压 = 电压 = 新电压

        # (5) 阈值扫描 —— 又是一个 N 次解释器循环
        阈值 = self.阈值
        发放 = [i for i, v in enumerate(电压) if v >= 阈值]
        self.上一步发放 = 发放
        self.上一步发放数 = len(发放)
        self.累计发放 += len(发放)
        if 发放:
            #: 只给画栅格用；有上限，不会一直涨
            最近 = self.最近发放
            步 = self.步数
            最近.extend((i, 步) for i in 发放)
            超 = len(最近) - self.最近上限
            if 超 > 0:
                del 最近[:超]

        # 重置 + 进不应期 + 事件驱动投递（只有发放的神经元才走出边）
        if 发放:
            重置 = self.重置
            不应步数 = self._不应步数
            for i in 发放:
                电压[i] = 重置
                不应[i] = 不应步数
            出_起始, 出_目标, 出_权 = self._出_起始, self._出_目标, self._出_权
            for 前 in 发放:
                for k in range(出_起始[前], 出_起始[前 + 1]):
                    电流[出_目标[k]] += 出_权[k]

        # (4) 突触电流：`τ_s·dI/dt = −I + I_in` 的**显式 Euler**
        #
        # ⚠⚠ 这里**必须乘 `dt/τ_s`** —— 第一版写成
        #       `I = I·exp(−dt/τ_s) + I_in`
        #    那是"每步**无条件注入** I_in"，而不是"按速率的 dt/τ_s 倍注入"
        #    ⇒ 稳态电流 = I_in/(1−衰减) ≈ I_in·(τ_s/dt) = **50 倍的 I_in**
        #    ⇒ 网络被自己的外部电流顶到饱和。
        #
        #    **症状是个很好的指纹**：不管把外部电流、突触权重、噪声怎么调，
        #    平均放电率都钉死在 166.67 / 333.33 Hz（= N/60、N/30 个发放/步）
        #    —— 与参数**完全无关**。看到"怎么调都一样"，先怀疑量纲，
        #    别去怀疑"网络进入了某个动力学吸引子"。
        系数 = self._系数                      # dt/τ_s
        衰减 = self._衰减                      # 1 − dt/τ_s
        外部们 = self._外部列表
        幅度 = self.噪声幅度
        if 幅度:
            gauss = self._随机.gauss
            电流[:] = [c * 衰减 + 系数 * (e + gauss(0.0, 幅度))
                       for c, e in zip(电流, 外部们)]
        else:
            电流[:] = [c * 衰减 + 系数 * e for c, e in zip(电流, 外部们)]

        self.步数 += 1

    # ── 统计 ──────────────────────────────────────────────────────────
    def 平均放电率(self) -> float:
        """累计发放数 /（神经元数 × 模拟时长）—— 单位 Hz。"""
        秒 = self.步数 * self.dt
        if 秒 <= 0 or self.神经元数 <= 0:
            return 0.0
        return self.累计发放 / (self.神经元数 * 秒)

    def 出度(self) -> float:
        return self.突触数 / self.神经元数 if self.神经元数 else 0.0

    def 状态指纹(self) -> str:
        return 状态指纹([self.电压, self.电流])

    def 统计(self) -> dict:
        return {
            "神经元数": self.神经元数,
            "突触数": self.突触数,
            "平均出度": self.出度(),
            "平均放电率": self.平均放电率(),
            "步数": self.步数,
            "dt": self.dt,
            "模拟时长": self.步数 * self.dt,
            "累计发放": self.累计发放,
            "指纹": self.状态指纹(),
        }

    def __repr__(self) -> str:
        return ("<脉冲网络 N=%d 突触=%d 出度=%.1f 步数=%d r̄=%.2f Hz>"
                % (self.神经元数, self.突触数, self.出度(), self.步数,
                   self.平均放电率()))


# ══════════════════════════ 工厂：两条线的默认参数 ══════════════════════════

def 人脑网络(群体数: int = 1000, 总神经元数: float = 人脑神经元数, *,
             k_local: int = 20, k_long: int = 10, G: float = G_默认,
             种子: int = 12345, dt: float = 1e-4) -> 群体网络:
    """按**人脑规模**与 **canonical 口径**建一个群体网络。

    默认：**1000 个群体 × 每群 8600 万单元 = 8.6×10¹⁰ 个神经元**（人脑量级），
    每群体一行 30 个非零（20 个局部环 + 10 个长程）⇒ nnz/步 = 30000。
    """
    return 群体网络(群体数=群体数, 总神经元数=总神经元数, k_local=k_local,
                    k_long=k_long, G=G, 种子=种子, dt=dt)


def 果蝇网络(神经元数: int = 果蝇神经元数, 突触数: int = 1_000_000, *,
             种子: int = 0, dt: float = 1e-4, 外部电流: float = 1.20,
             驱动离散度: float = 0.10, 噪声幅度: float = 0.0,
             **更多) -> 脉冲网络:
    """按**果蝇规模**建一个逐细胞脉冲网络（默认 10 万神经元）。

    ⚠ `突触数` 默认 1e6，**远少于**果蝇真实的 ~5e7 —— 见 `脉冲网络` 的说明。
      要显式表示更多，直接把这个参数调大（内存/时间都线性涨）。

    `**更多` 原样转给 `脉冲网络`（判据要能单独关掉某个机制来做对照实验）。
    """
    return 脉冲网络(神经元数=神经元数, 突触数=突触数, 种子=种子, dt=dt,
                    外部电流=外部电流, 驱动离散度=驱动离散度, 噪声幅度=噪声幅度,
                    **更多)


# ══════════════════════════ 基准（同口径、纯计算）══════════════════════════

def _计时(函数, 重复: int = 3) -> float:
    """跑 `重复` 次取**中位数**（毫秒）。第一次先热身一记（省得算进首次分配）。"""
    函数()
    样本 = []
    for _ in range(max(1, int(重复))):
        t0 = time.perf_counter()
        函数()
        样本.append((time.perf_counter() - t0) * 1e3)
    样本.sort()
    return 样本[len(样本) // 2]


def 基准_群体(群体数: int = 1000, 总神经元数: float = 人脑神经元数, *,
              步数: int = 1000, k_local: int = 20, k_long: int = 10,
              G: float = G_默认, 种子: int = 12345, dt: float = 1e-4,
              热身: int = 0, 时间平均: bool = True, 打印: bool = True) -> dict:
    """人脑级群体层的基准 —— **同口径**：只算，不画，不开窗口。

    计时口径 = **只包 step 循环**（建表/分配不计；与专业版 `TOTAL_MS` 同口径）。
    返回值里的键**刻意与专业版 stdout 的 KEY=VALUE 同名**，方便直接并排。

    ⚠⚠ **`热身` 默认 0，而且这不是随便定的。**
      跑热身步会让末态对应"N+热身 步"，而专业版 `--steps N` 是**正好 N 步**
      ⇒ 两边的 `MEAN_RATE_ALL` 与 `DIGEST` 就**对不上**了（实测：P=100 跑
      2000 步，热身 20 步时 DIGEST 变成 0x…C52，热身 0 时才对上 0x…CB5）。
      计时上那点热身收益（<0.1%）不值得用"可比性"去换 —— 而
      **"两边跑的是不是同一个计算"只有 DIGEST 能证明**，这条不能丢。
      ⇒ 要热身就把它当成"换了一个仿真"，别拿它跟 `--steps N` 比 DIGEST。
    """
    t建 = time.perf_counter()
    网 = 人脑网络(群体数=群体数, 总神经元数=总神经元数, k_local=k_local,
                  k_long=k_long, G=G, 种子=种子, dt=dt)
    建表秒 = time.perf_counter() - t建

    if 热身:
        网.步进(热身)
    起始步数 = 网.步数
    t0 = time.perf_counter()
    网.步进(步数)
    总秒 = time.perf_counter() - t0
    ms每步 = 总秒 / 步数 * 1e3
    nnz = 网.矩阵元数
    r = dict(网.统计())
    r.update({
        "STEPS": 步数,
        "TOTAL_MS": 总秒 * 1e3,
        "MS_PER_STEP": ms每步,
        "COUPLING_UPDATES_PER_S": nnz * 步数 / 总秒 if 总秒 else 0.0,
        "CONSTRUCT_MS": 建表秒 * 1e3,
        "WARMUP_STEPS": 起始步数,
    })
    if 时间平均:
        #: ⚠ 另起一趟、**不计时**（与专业版 `TIMEAVG_*` 同一个做法）
        r.update(网.时间平均放电率(步数))
    if 打印:
        print("  POPS                  = %d" % r["POPS"])
        print("  K (nnz/行)            = %d  （local %d + long %d）"
              % (r["K"], k_local, k_long))
        print("  NNZ_PER_STEP          = %d" % r["NNZ_PER_STEP"])
        print("  REPRESENTED_NEURONS   = %.6g  ⚠ 账面上的算术换算，非逐细胞"
              % r["REPRESENTED_NEURONS"])
        print("  REPRESENTED_SYNAPSES  = %.6g  ⚠ 算术推算，非 CPU 所算"
              % r["REPRESENTED_SYNAPSES"])
        print("  MEAN_RATE_E           = %.6f Hz  ⚠ 快照（周期环里由停在第几步决定）"
              % r["MEAN_RATE_E"])
        print("  MEAN_RATE_I           = %.6f Hz  ⚠ 快照" % r["MEAN_RATE_I"])
        print("  MEAN_RATE_ALL         = %.6f Hz  ⚠ 快照（与专业版 DIGEST 同一步数）"
              % r["MEAN_RATE_ALL"])
        if "TIMEAVG_ALL" in r:
            print("  TIMEAVG_E             = %.6f Hz  ← **时间平均才是物理量**"
                  % r["TIMEAVG_E"])
            print("  TIMEAVG_I             = %.6f Hz" % r["TIMEAVG_I"])
            print("  TIMEAVG_ALL           = %.6f Hz" % r["TIMEAVG_ALL"])
        print("  STEPS                 = %d  （热身 %d 步不计；热身>0 则 DIGEST 与 --steps 不可比）"
              % (r["STEPS"], r["WARMUP_STEPS"]))
        print("  MS_PER_STEP           = %.6f ms" % r["MS_PER_STEP"])
        print("  COUPLING_UPDATES_PER_S= %.4g  （= nnz×步数/秒，CPU 真算的）"
              % r["COUPLING_UPDATES_PER_S"])
        print("  CONSTRUCT_MS          = %.1f ms  （不计入 MS_PER_STEP）"
              % r["CONSTRUCT_MS"])
        print("  DIGEST                = %s" % r["DIGEST"])
    return r


def 基准_逐细胞(神经元数: int = 果蝇神经元数, 突触数: int = 1_000_000, *,
                步数: int = 200, 种子: int = 0, 打印: bool = True,
                热身: int = 20, **脉冲参数) -> dict:
    """果蝇级**逐细胞**的基准 —— 同口径：只算，不画。"""
    t建 = time.perf_counter()
    网 = 果蝇网络(神经元数=神经元数, 突触数=突触数, 种子=种子, **脉冲参数)
    建表秒 = time.perf_counter() - t建
    网.步进(热身)
    网.累计发放 = 0
    网.步数 = 0
    t0 = time.perf_counter()
    网.步进(步数)
    总秒 = time.perf_counter() - t0
    ms每步 = 总秒 / 步数 * 1e3
    r = {
        "NEURONS": 网.神经元数,
        "SYNAPSES": 网.突触数,
        "OUT_DEGREE": 网.出度(),
        "STEPS": 步数,
        "TOTAL_MS": 总秒 * 1e3,
        "MS_PER_STEP": ms每步,
        "MS_PER_STEP_PER_NEURON": ms每步 / 网.神经元数,
        "SPIKES_PER_STEP": 网.累计发放 / 步数,
        "SYNAPTIC_EVENTS_PER_STEP": 网.累计发放 / 步数 * 网.出度(),
        "NEURON_STEPS_PER_S": 网.神经元数 * 步数 / 总秒 if 总秒 else 0.0,
        "MEAN_RATE": 网.平均放电率(),
        "DT": 网.dt,
        "SIM_SECONDS": 步数 * 网.dt,
        "CONSTRUCT_MS": 建表秒 * 1e3,
        "DIGEST": 网.状态指纹(),
    }
    if 打印:
        print("  NEURONS               = %d  （果蝇量级 %d）"
              % (r["NEURONS"], 果蝇神经元数))
        print("  SYNAPSES (显式)       = %d  （平均出度 %.1f）"
              % (r["SYNAPSES"], r["OUT_DEGREE"]))
        print("                         ⚠ 果蝇真实约 %.3g 条 ⇒ 本实现只显式表示 %.1f%%"
              % (果蝇突触数, 100.0 * r["SYNAPSES"] / 果蝇突触数))
        print("  MEAN_RATE             = %.4f Hz" % r["MEAN_RATE"])
        print("  STEPS                 = %d  × dt=%.4g s = %.4f s 模拟时长"
              % (r["STEPS"], r["DT"], r["SIM_SECONDS"]))
        print("  MS_PER_STEP           = %.4f ms" % r["MS_PER_STEP"])
        print("  MS_PER_STEP/NEURON    = %.6f ms" % r["MS_PER_STEP_PER_NEURON"])
        print("  SPIKES_PER_STEP       = %.1f" % r["SPIKES_PER_STEP"])
        print("  SYNAPTIC_EVENTS_PER_STEP = %.1f" % r["SYNAPTIC_EVENTS_PER_STEP"])
        print("  NEURON_STEPS_PER_S    = %.4g  （CPU 真算的）"
              % r["NEURON_STEPS_PER_S"])
        print("  CONSTRUCT_MS          = %.1f ms" % r["CONSTRUCT_MS"])
        print("  DIGEST                = %s" % r["DIGEST"])
    return r


def 基准_突触吞吐(突触数: int = 1_000_000, 神经元数: int = 果蝇神经元数,
                  重复: int = 3, 打印: bool = True) -> dict:
    """**纯突触投递吞吐** —— 把每一步都逼成"全体发放"，量事件投递本身。

    为什么要单独量这条：真实脉冲网络里每步只有一小部分神经元发放，
    所以 `ms/步` 主要花在 O(N) 的整数组操作上、**不**花在突触上。
    要知道"突触这一层到底多快"，就得把发放率顶到 100%。
    """
    网 = 果蝇网络(神经元数=神经元数, 突触数=突触数, 种子=1, 噪声幅度=0.0)
    网.外部电流 = 1e9
    网.阈值 = -1e9            # 必发放

    def 一次():
        网.步进(1)
        网.电压 = [0.0] * 网.神经元数

    ms = _计时(一次, 重复)
    r = {"SYNAPSES": 网.突触数, "MS_PER_STEP": ms,
         "SYNAPTIC_EVENTS_PER_S": 网.突触数 / (ms / 1e3) if ms else 0.0}
    if 打印:
        print("  SYNAPSES              = %d  （全体发放 ⇒ 每步投递 %d 次）"
              % (r["SYNAPSES"], r["SYNAPSES"]))
        print("  MS_PER_STEP           = %.4f ms" % ms)
        print("  SYNAPTIC_EVENTS_PER_S = %.4g" % r["SYNAPTIC_EVENTS_PER_S"])
    return r


#: 与专业版对齐的那张网格（`neural-pro` 给的规格）
#: ⚠ 参数一模一样才是"同口径"：model=rate, k_local=20, k_long=10, dt=0.1ms, seed=12345
口径网格 = (
    {"群体数": 100, "步数": 2000},
    {"群体数": 1000, "步数": 1000},
    {"群体数": 10000, "步数": 500},
)

#: ⚠⚠ **专业版 C++ 实测的参照点**（`neural-pro` 在他的实现上跑出来的原始输出，
#:    不是这边算的、也不是推算的）。本模块每次跑都要能逐位复现这张表 ——
#:    这是"两边跑的是同一个计算"的**唯一硬证据**（不是"我们文档写得一样"）。
#:    字段：群体数, k_local, k_long, 步数, MEAN_RATE_ALL, DIGEST
参照用例 = (
    (8, 4, 2, 1, 28.16257945, 0x0DFF8FAC4D340322),
    (8, 4, 2, 10, 25.94342077, 0x533BA597805BA02A),
    (100, 20, 10, 1, 36.75108304, 0xDD2110086658BD53),
    (100, 20, 10, 1000, 10.60823279, 0x83A8F7990742CA15),
)


def 核对参照(打印: bool = True) -> list[tuple]:
    """跑 `参照用例`，逐个比 `MEAN_RATE_ALL` 与 `DIGEST`。

    ⚠ 返回的是**真结果**（`(参数, 平均放电率, 参考率, 率对上没, 摘要, 参考摘要, 摘要对上没)`），
      不是"跑完就返回 True"。用例 A（P=8、1 步）最值钱：
      它只做一次 `W·r` + 一次非线性 ⇒ **对不上就是拓扑或权重错**，与积分无关。

    ⚠ `DIGEST` 是全 64 位逐位比较 —— 不是"差不多"、不是"量级对"。
    """
    结果 = []
    for (P, kl, klong, S, 参考率, 参考摘要) in 参照用例:
        网 = 群体网络(群体数=P, 总神经元数=float(P), k_local=kl, k_long=klong,
                      种子=12345)
        网.步进(S)
        平均 = 网.平均放电率()
        摘要 = int(网.DIGEST(), 16)
        #: ⚠ 率允许 5e-9 的容差：专业版 stdout 只打 8 位小数，
        #:   比的是"他打印出来的字"，不是他的内部 double。
        #:   **DIGEST 不给容差** —— 它是逐字节的，差一位就是差一位。
        率对上 = abs(平均 - 参考率) < 5e-9
        摘要对上 = (摘要 == 参考摘要)
        结果.append((P, kl, klong, S, 平均, 参考率, 率对上, 摘要, 参考摘要, 摘要对上))
        if 打印:
            print("  P=%-5d k=%-3d steps=%-5d  r̄=%14.8f（参考 %14.8f %s）  "
                  "DIGEST=0x%016X %s"
                  % (P, kl + klong, S, 平均, 参考率,
                     "✅" if 率对上 else "❌", 摘要,
                     "✅" if 摘要对上 else "❌ 参考 0x%016X" % 参考摘要))
    return 结果


def 口径网格_群体(网格=口径网格, *, 打印: bool = True,
                  超时秒: float | None = None) -> list[dict]:
    """跑**与专业版同一张网格**，每格返回一行实测（跑不完的格子照实记账）。"""
    结果 = []
    for 格 in 网格:
        P, S = 格["群体数"], 格["步数"]
        if 打印:
            print("── P=%-6d steps=%-5d (nnz/步 = %d) ──" % (P, S, P * 30))
        t0 = time.perf_counter()
        try:
            r = 基准_群体(群体数=P, 步数=S, 打印=打印)
            r["墙钟秒"] = time.perf_counter() - t0
            r["跑完"] = True
        except MemoryError as e:                       # pragma: no cover
            r = {"POPS": P, "STEPS": S, "跑完": False, "错": "MemoryError: %s" % e}
            if 打印:
                print("  XX 内存不够：%s" % r["错"])
        r["群体数"], r["步数"] = P, S
        结果.append(r)
        if 打印:
            print()
        if 超时秒 is not None and time.perf_counter() - t0 > 超时秒:
            if 打印:
                print("  ⚠ 超过 %.0f s 预算，后面的格子不跑了（照实记「没跑」）"
                      % 超时秒)
            break
    return 结果


def 打印网格(结果: list[dict]) -> None:
    """把网格实测打成人看的对比表（**只打实测，不做任何外推**）。

    ⚠ 放电率打的是**时间平均** —— canonical 口径会落进小周期极限环，
      快照读数相邻两步能差 10 倍（实测 35.28 → 0.58 → 29.65 → 48.97），
      只看快照会得出"网络死了"的假结论。
    """
    print("  %-8s %-8s %-12s %-14s %-12s %s"
          % ("POPS", "STEPS", "NNZ/步", "MS_PER_STEP", "M nnz/s",
             "时间平均放电率(Hz)"))
    for r in 结果:
        if not r.get("跑完"):
            print("  %-8s %-8s %-12s %s" % (r.get("群体数"), r.get("步数"),
                                            "—", r.get("错", "没跑")))
            continue
        率 = r.get("TIMEAVG_ALL", r["MEAN_RATE_ALL"])
        print("  %-8d %-8d %-12d %-14.4f %-12.2f %.4f"
              % (r["POPS"], r["STEPS"], r["NNZ_PER_STEP"], r["MS_PER_STEP"],
                 r["COUPLING_UPDATES_PER_S"] / 1e6, 率))


# ══════════════════════════ 引擎集成（用轻量渲染画出来）════════════════════

#: ⚠ 只 import `scene`（它只依赖 `math`/`typing`）—— **不 import `render`**：
#:   `render.Tk后端` 才是碰 tkinter 的那一层，而本模块要能在无 tkinter 的
#:   环境里 import（安卓/Linux 精简系统/CI）。这条是"零依赖"的一部分。
from .pyengine.scene import 组件 as _组件          # noqa: E402


class 神经组件(_组件):
    """把神经网络的每步更新挂进 `pyengine` 的游戏循环，并画成一张图。

    ⚠ 本类**不 import tkinter** —— 它只认 `画笔接口`（`pyengine.scene.画笔接口`）。
      所以同一份代码在这里跑得起来：

          录制后端（无头，判据/基准用）  ← 不建窗口、不抢焦点
          Tk后端（真窗口，给人看）

    ## 画什么

    · 群体网络 ⇒ 每个群体一根**柱**（高度 = 放电率 / 上限），
      外加一根**平均放电率的横线**（一眼看出整体在放电还是静默）。
    · 脉冲网络 ⇒ 最近一次发放的**栅格点**（raster）。

    ⚠ `子步每帧`：引擎的 dt 是 1/60 s，神经的 dt 是 0.1 ms ⇒ 一帧要跑
      一百多个神经步。不设这个的话，要么"神经几乎不走"、要么"dt 失真"。
    """

    def __init__(self, 网络, *, 柱子数: int = 240, 子步每帧: int | None = None) -> None:
        super().__init__()
        self.网络 = 网络
        self.柱子数 = int(柱子数)
        self.子步每帧 = 子步每帧
        self.步数 = 0

    def _该跑几步(self, dt: float) -> int:
        if self.子步每帧 is not None:
            return max(1, int(self.子步每帧))
        dt神经 = getattr(self.网络, "dt", 1e-4)
        return max(1, int(round(dt / dt神经)))

    def 每帧(self, dt: float) -> None:
        n = self._该跑几步(dt)
        self.网络.步进(n)
        self.步数 += n

    def 绘制(self, 画笔) -> None:
        网 = self.网络
        if isinstance(网, 脉冲网络):
            self._画栅格(画笔, 网)
        else:
            self._画柱子(画笔, 网)

    def _画柱子(self, 画笔, 网: 群体网络) -> None:
        size = 100.0
        P = 网.群体数
        取 = min(self.柱子数, P)
        #: 均匀抽样（**确定的取法** —— 不靠集合/字典顺序）
        宽 = size / 取
        率 = 网.率
        for k in range(取):
            i = int(k * P / 取)
            h = min(1.0, max(0.0, 率[i] / R_MAX)) * size * 0.9
            画笔.矩形(k * 宽, 0.0, 宽 * 0.9, max(h, 0.05),
                      "红" if 网.兴奋性[i] else "蓝")
        y = min(1.0, 网.平均放电率() / R_MAX) * size * 0.9
        画笔.线(0.0, y, size, y, "绿", 0.3)
        画笔.文字(1.0, size * 0.94,
                  "P=%d  r̄=%.2f Hz  t=%.1f ms" % (P, 网.平均放电率(),
                                                  网.步数 * 网.dt * 1e3),
                  "黑", 8)

    def _画栅格(self, 画笔, 网: 脉冲网络) -> None:
        """把 `网.最近发放` 画成**一张栅格图**（raster）。

        横轴 = 神经元（0..N 均匀铺开）；纵轴 = 时间（越新越靠上）。
        ⚠ 光画"最近一步"在窗口里几乎什么都看不到（一步只有百来个发放）
          —— 所以画一个**时间窗口**内的所有发放。
        """
        size = 100.0
        最近 = 网.最近发放
        if 最近:
            起 = 最近[0][1]
            止 = 最近[-1][1]
            跨度 = max(1, 止 - 起)
            N = 网.神经元数
            for (神经元, 步) in 最近:
                x = (神经元 / N) * size
                y = 10.0 + ((步 - 起) / 跨度) * 78.0
                画笔.圆(x, y, 0.25, "黄")
        画笔.文字(1.0, size * 0.94,
                  "N=%d 突触=%d r̄=%.2f Hz" % (网.神经元数, 网.突触数,
                                               网.平均放电率()),
                  "黑", 8)


def 建演示(网络, *, 宽: int = 720, 高: int = 480, 标题: str = "神经模拟（轻量版）",
           后端=None, 柱子数: int = 240, 子步每帧: int | None = None):
    """把网络装成一个 `pyengine.游戏`，**默认无窗口**（用 `录制后端`）。

    为什么"默认无窗口"：这是基准与判据的入口 —— 弹窗会
    （a）在无显示环境/CI 里直接失败，（b）把渲染耗时混进模拟耗时。
    ⇒ 要看窗口，**显式**传 `后端=pyengine.Tk后端(...)`（见 `examples/` 的开关）。
    """
    from . import pyengine as E

    if 后端 is None:
        后端 = E.录制后端()
    场景 = E.场景("神经模拟")
    根 = 场景.造("网络", 0, 0)
    根.加(神经组件(网络, 柱子数=柱子数, 子步每帧=子步每帧))
    return E.游戏(场景=场景, 宽=宽, 高=高, 尺寸=100, 标题=标题, 后端=后端)
