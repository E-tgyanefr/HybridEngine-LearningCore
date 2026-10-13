"""AI 入门课程（ai/）的测试。

════ 为什么 AI 课比别的课更需要这套测试 ════

AI 课的检查全都在问一件事：**"你有没有真的算出来？"**
而这件事没法靠比对输出判断 —— 学生把答案写死，输出能一模一样。

所以每一课的检查都用了同一个手法：**换一批随机数据再问一次**。
代价是这个手法有个隐蔽的失败模式：

  · 随机数据造得太好分 → 写死答案也能碰巧过（检查太松）
  · 随机数据造得太刁钻 → 正确解法偶尔也过不了（检查太严，学生会疯）

两种错都**不会稳定复现**，跑一次看不出来。所以这里对每一课都跑三类测试：

  ① 参考答案 → 必须全过（而且要跑很多次，排除偶发失败）
  ② "写死答案"的解法 → 核心检查必须抓住
  ③ "用错算法"的解法 → 对应的检查必须给出具体原因

════ 为什么阈值定得这么"松"（比如准确率 0.8）════

因为感知机一判对全部训练数据就停了，它学到的分界线不一定是"真规律"那条。
换一批新数据时偶尔会有一两个点落在两条线之间 —— 这是算法的真实行为，
不是学生写错了。阈值必须容得下它，否则正确解法会被偶发判错。
（0.8 依然拦得住"一律猜多数类"这种假解法：它的准确率只有 ~50%。）
"""

import os
import sys
import unittest
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from hybridengine_light.check import load_module, run_checks  # noqa: E402

ROOT = Path(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
AI = ROOT / "ai"
SOLUTIONS = ROOT / "ai-solutions"


def _课(名: str):
    return load_module(AI / 名, name=f"ai_lesson_{名.replace('.', '_')}")


def _答案(名: str):
    """参考答案（从文件加载，所以 __file__ 在，"没偷偷用第三方库"那条也能跑）。"""
    return load_module(SOLUTIONS / 名, name=f"ai_sol_{名.replace('.', '_')}")


def _合成(名: str, 源码: str):
    """把一段源码当成"学生的答案"加载。

    故意**不设 __file__** —— 这样"没偷偷用第三方库"那条会自动跳过，
    反面粉丝测试只考算法本身。
    """
    mod = type(sys)(f"ai_fake_{名}")
    exec(compile(源码, f"<{名}>", "exec"), mod.__dict__)
    return mod


def _失败项(rep):
    """把"没过的"渲染成能读的几行。

    ⚠⚠ **这段原来只列 `not r.ok` 的结果 —— 于是有两类失败会渲染成空字符串**：

        · `rep.total == 0`（一条 `CHECK_` 都没跑到）
          ⇒ `all_ok` 假（它要求 `total > 0`），但 `results` 是空的
          ⇒ **消息里冒号后面什么都没有**
        · `rep.error` 被设（`check.py` 里只有一种：课程没定义任何 `CHECK_`）

    ⚠ 这个缺陷是**三平台 CI 逼出来的**：macOS job 上出现

        AssertionError: False is not true : 05_西瓜分类器.py 第 6 次没全过：
                                        ↑ 冒号后面是空的

      而我为了搞清楚"到底哪条检查挂了"**烧了一整轮**：
      CI 日志里没有、本机跑 200 次也复现不出来（0 挂）——
      因为**消息里根本没写是哪一类失败**。

    ⇒ 规矩：**失败消息必须能区分失败的种类。**
      "没全过"这三个字对排错毫无价值；"一条检查都没跑到"和
      "第 3 条检查挂了"是两件完全不同的事，修法也完全不同。
    """
    行 = [f"{r.name}: {r.detail}" for r in rep.results if not r.ok]
    if 行:
        return 行
    #: —— 下面这几类"从结果列表里看不出来"的失败，必须单独说 ——
    if rep.error:
        return [f"（检查本身报错）{rep.error}"]
    if rep.total == 0:
        return ["（一条 CHECK_ 都没跑到 —— 课程模块里没有 CHECK_ 开头的函数，"
                "或者它加载到一半就出错了）"]
    #: 走到这里 = "有结果且都 ok，而 all_ok 却是假" ⇒ 那是 check.py 的口径 bug
    return [f"（口径矛盾：results 有 {rep.total} 条且全 ok，但 all_ok 是假 —— "
            f"这本身是 check.py 的 bug）"]


class 公共断言(unittest.TestCase):
    def 全过(self, 课名, 答案=None, 次=1, 说明=""):
        课 = _课(课名)
        答 = 答案 if 答案 is not None else _答案(课名)
        for i in range(次):
            rep = run_checks(课, 答)
            self.assertTrue(rep.all_ok,
                            f"{课名} 第 {i + 1} 次没全过{说明}：\n" + "\n".join(_失败项(rep)))
        return rep

    def 抓到(self, 课名, 答案, 检查名里的字, 提示里应出现=None):
        """跑一遍，要求「名字里含某字」的那条检查失败，并且提示里说了原因。"""
        rep = run_checks(_课(课名), 答案)
        self.assertFalse(rep.all_ok, f"{课名}：这个偷懒解法不该全过")
        项 = [r for r in rep.results if 检查名里的字 in r.name]
        self.assertTrue(项, f"{课名}：找不到名字含「{检查名里的字}」的检查")
        self.assertFalse(项[0].ok, f"{课名}：「{项[0].name}」没抓住偷懒解法")
        self.assertTrue(项[0].detail.strip(), f"{课名}：「{项[0].name}」失败了却没说明原因")
        if 提示里应出现:
            self.assertIn(提示里应出现, 项[0].detail,
                          f"{课名}：「{项[0].name}」的提示没说清问题")
        return rep


class Test第01课线性回归(公共断言):
    """核心检查点：拟合必须真的从数据算出来。"""

    def test_参考答案全过_跑10次(self):
        # 每一课的检查里都有随机数据，跑多次才能确认没有偶发失败
        self.全过("01_线性回归.py", 次=10)

    def test_写死的拟合被抓(self):
        bad = _合成("bad01", (
            "def 预测(直线, x):\n"
            "    k, b = 直线\n"
            "    return k * x + b\n"
            "def 误差平方和(xs, ys, 直线):\n"
            "    return sum((预测(直线, x) - y) ** 2 for x, y in zip(xs, ys))\n"
            "def 拟合(xs, ys):\n"
            "    return 2.0, 1.0\n"          # ← 课程例子里那条线
        ))
        self.抓到("01_线性回归.py", bad, "核心_换一批数据", "写死")
        self.抓到("01_线性回归.py", bad, "核心_随机数据")

    def test_用绝对值代替平方被抓(self):
        bad = _合成("abs01", (
            "def 预测(直线, x):\n"
            "    k, b = 直线\n"
            "    return k * x + b\n"
            "def 误差平方和(xs, ys, 直线):\n"
            "    return sum(abs(预测(直线, x) - y) for x, y in zip(xs, ys))\n"
            "def 拟合(xs, ys):\n"
            "    n = len(xs)\n"
            "    mx = sum(xs) / n\n"
            "    my = sum(ys) / n\n"
            "    分子 = sum((x - mx) * (y - my) for x, y in zip(xs, ys))\n"
            "    分母 = sum((x - mx) ** 2 for x in xs)\n"
            "    k = 分子 / 分母\n"
            "    return k, my - k * mx\n"
        ))
        rep = self.抓到("01_线性回归.py", bad, "正负误差不会抵消", "绝对值")
        # 拟合是对的，所以核心那两条必须过 —— 免得把"只错了一半"的学生吓跑
        核心 = [r for r in rep.results if "核心" in r.name]
        self.assertTrue(all(r.ok for r in 核心), _失败项(rep))


class Test第02课感知机(公共断言):
    """核心检查点：权重必须是被训练出来的，不是写死的。"""

    def test_参考答案全过_跑10次(self):
        self.全过("02_感知机.py", 次=10)

    def test_写死的权重被抓(self):
        bad = _合成("bad02", (
            "def 阶跃(z):\n"
            "    return 1 if z >= 0 else 0\n"
            "def 预测(权重, 偏置, 输入):\n"
            "    总分 = 偏置\n"
            "    for i in range(len(输入)):\n"
            "        总分 += 权重[i] * 输入[i]\n"
            "    return 阶跃(总分)\n"
            "def 训练(数据, 标签, 学习率=0.1, 最多轮数=100):\n"
            "    return [0.2, 0.2], -0.3\n"      # ← AND 的正确答案，背下来的
        ))
        self.抓到("02_感知机.py", bad, "核心_权重必须真的训练出来", "写死")
        self.抓到("02_感知机.py", bad, "核心_换一批随机数据")

    def test_更新公式符号写反被抓(self):
        bad = _合成("sign02", (
            "def 阶跃(z):\n"
            "    return 1 if z >= 0 else 0\n"
            "def 预测(权重, 偏置, 输入):\n"
            "    总分 = 偏置\n"
            "    for i in range(len(输入)):\n"
            "        总分 += 权重[i] * 输入[i]\n"
            "    return 阶跃(总分)\n"
            "def 训练(数据, 标签, 学习率=0.1, 最多轮数=100):\n"
            "    权重 = [0.0] * len(数据[0])\n"
            "    偏置 = 0.0\n"
            "    for _ in range(最多轮数):\n"
            "        错 = 0\n"
            "        for 输入, 正确 in zip(数据, 标签):\n"
            "            误差 = 正确 - 预测(权重, 偏置, 输入)\n"
            "            if 误差:\n"
            "                错 += 1\n"
            "                for i in range(len(权重)):\n"
            "                    权重[i] -= 学习率 * 误差 * 输入[i]\n"   # ← 符号反了
            "                偏置 -= 学习率 * 误差\n"
            "        if 错 == 0:\n"
            "            break\n"
            "    return 权重, 偏置\n"
        ))
        self.抓到("02_感知机.py", bad, "学会AND")

    def test_在预测里写死XOR被抓(self):
        """偷偷加料让 XOR 也能"学会" —— 这条检查专门抓它。"""
        bad = _合成("xor02", (
            "def 阶跃(z):\n"
            "    return 1 if z >= 0 else 0\n"
            "def 预测(权重, 偏置, 输入):\n"
            "    if list(输入) in ([0, 1], [1, 0]):\n"
            "        return 1\n"                    # ← 针对具体输入的 if
            "    if list(输入) in ([0, 0], [1, 1]):\n"
            "        return 0\n"
            "    总分 = 偏置\n"
            "    for i in range(len(输入)):\n"
            "        总分 += 权重[i] * 输入[i]\n"
            "    return 阶跃(总分)\n"
            "def 训练(数据, 标签, 学习率=0.1, 最多轮数=100):\n"
            "    权重 = [0.0] * len(数据[0])\n"
            "    偏置 = 0.0\n"
            "    for _ in range(最多轮数):\n"
            "        错 = 0\n"
            "        for 输入, 正确 in zip(数据, 标签):\n"
            "            误差 = 正确 - 预测(权重, 偏置, 输入)\n"
            "            if 误差:\n"
            "                错 += 1\n"
            "                for i in range(len(权重)):\n"
            "                    权重[i] += 学习率 * 误差 * 输入[i]\n"
            "                偏置 += 学习率 * 误差\n"
            "        if 错 == 0:\n"
            "            break\n"
            "    return 权重, 偏置\n"
        ))
        self.抓到("02_感知机.py", bad, "不应该假装学会", "不可能")


class Test第03课梯度下降(公共断言):
    """核心检查点：必须真的沿梯度走，而且学习率的两端要看得见。"""

    def test_参考答案全过_跑10次(self):
        self.全过("03_梯度下降.py", 次=10)

    def test_把课程例子写死被抓(self):
        bad = _合成("bad03", (
            "def 数值梯度(函数, x, 步长=1e-5):\n"
            "    return 2 * (x - 3)\n"          # ← 假设所有函数都是 (x-3)²
            "def 梯度下降(函数, 起点, 学习率=0.1, 步数=100):\n"
            "    x = 起点\n"
            "    脚印 = [x]\n"
            "    for _ in range(步数):\n"
            "        x = x + (3 - x) * 0.2\n"   # ← 朝写死的答案 3 走
            "        脚印.append(x)\n"
            "    return 脚印\n"
        ))
        self.抓到("03_梯度下降.py", bad, "数值梯度算得对", "写死")
        self.抓到("03_梯度下降.py", bad, "核心_能走到最低点")

    def test_前向差分漏掉那个2被抓(self):
        bad = _合成("fwd03", (
            "def 数值梯度(函数, x, 步长=1e-5):\n"
            "    return (函数(x + 步长) - 函数(x)) / 步长\n"    # 前向差分，误差大
            "def 梯度下降(函数, 起点, 学习率=0.1, 步数=100):\n"
            "    x = 起点\n"
            "    脚印 = [x]\n"
            "    for _ in range(步数):\n"
            "        x = x - 学习率 * 数值梯度(函数, x)\n"
            "        脚印.append(x)\n"
            "    return 脚印\n"
        ))
        # 前向差分的误差是 O(步长)：步长 1e-5 时误差约 1e-5 量级，
        # 检查的容差是 1e-4 的相对容差 —— 所以它**应该**能过。
        # 这条测试是防止有人把容差收紧到"只有中心差分能过"，
        # 那样会把一个数学上正确的写法判错。
        self.全过("03_梯度下降.py", bad, 次=3, 说明="（前向差分）")

    def test_符号写反被抓(self):
        bad = _合成("up03", (
            "def 数值梯度(函数, x, 步长=1e-5):\n"
            "    return (函数(x + 步长) - 函数(x - 步长)) / (2 * 步长)\n"
            "def 梯度下降(函数, 起点, 学习率=0.1, 步数=100):\n"
            "    x = 起点\n"
            "    脚印 = [x]\n"
            "    for _ in range(步数):\n"
            "        x = x + 学习率 * 数值梯度(函数, x)\n"    # ← 往上爬
            "        脚印.append(x)\n"
            "    return 脚印\n"
        ))
        self.抓到("03_梯度下降.py", bad, "核心_误差一直在下降", "变大了")

    def test_给学习率加保护被抓(self):
        """自作聪明地限幅 / 提前停 —— 会掩盖"学习率太大就飞"的真相。"""
        bad = _合成("clip03", (
            "def 数值梯度(函数, x, 步长=1e-5):\n"
            "    return (函数(x + 步长) - 函数(x - 步长)) / (2 * 步长)\n"
            "def 梯度下降(函数, 起点, 学习率=0.1, 步数=100):\n"
            "    x = 起点\n"
            "    脚印 = [x]\n"
            "    for _ in range(步数):\n"
            "        下一个 = x - 学习率 * 数值梯度(函数, x)\n"
            "        if abs(下一个 - x) > 0.5:\n"
            "            下一个 = x + (0.5 if 下一个 > x else -0.5)\n"   # ← 限幅
            "        x = 下一个\n"
            "        脚印.append(x)\n"
            "    return 脚印\n"
        ))
        self.抓到("03_梯度下降.py", bad, "学习率太大", "飞")


class Test第04课K近邻(公共断言):
    """核心检查点：分类结果必须由距离决定。"""

    def test_参考答案全过_跑10次(self):
        self.全过("04_K近邻.py", 次=10)

    def test_不看距离的分类被抓(self):
        bad = _合成("bad04", (
            "def 距离(a, b):\n"
            "    return sum((p - q) ** 2 for p, q in zip(a, b)) ** 0.5\n"
            "def 找邻居(训练集, 查询点, k):\n"
            "    带距离 = [(距离(点, 查询点), 点, 标签) for 点, 标签 in 训练集]\n"
            "    带距离.sort(key=lambda t: t[0])\n"
            "    return [(点, 标签) for _d, 点, 标签 in 带距离[:k]]\n"
            "def 分类(训练集, 查询点, k):\n"
            "    票 = {}\n"
            "    for _点, 标签 in 训练集:\n"
            "        票[标签] = 票.get(标签, 0) + 1\n"
            "    return max(票, key=票.get)\n"        # ← 从来不看查询点
            "def 准确率(训练集, 测试集, k):\n"
            "    if not 测试集:\n"
            "        return 0.0\n"
            "    return sum(1 for 点, y in 测试集 if 分类(训练集, 点, k) == y) / len(测试集)\n"
        ))
        self.抓到("04_K近邻.py", bad, "核心_换个测试点", "跟着变")
        self.抓到("04_K近邻.py", bad, "核心_分类由距离决定")

    def test_曼哈顿距离被抓(self):
        bad = _合成("manh04", (
            "def 距离(a, b):\n"
            "    return sum(abs(p - q) for p, q in zip(a, b))\n"    # ← 曼哈顿
            "def 找邻居(训练集, 查询点, k):\n"
            "    带距离 = sorted(训练集, key=lambda t: 距离(t[0], 查询点))\n"
            "    return 带距离[:k]\n"
            "def 分类(训练集, 查询点, k):\n"
            "    票 = {}\n"
            "    for _点, 标签 in 找邻居(训练集, 查询点, k):\n"
            "        票[标签] = 票.get(标签, 0) + 1\n"
            "    return max(票, key=票.get)\n"
            "def 准确率(训练集, 测试集, k):\n"
            "    if not 测试集:\n"
            "        return 0.0\n"
            "    return sum(1 for 点, y in 测试集 if 分类(训练集, 点, k) == y) / len(测试集)\n"
        ))
        self.抓到("04_K近邻.py", bad, "距离算得对", "曼哈顿")

    def test_忘了开根号被抓(self):
        bad = _合成("sqrt04", (
            "def 距离(a, b):\n"
            "    return sum((p - q) ** 2 for p, q in zip(a, b))\n"   # ← 没开根号
            "def 找邻居(训练集, 查询点, k):\n"
            "    带距离 = sorted(训练集, key=lambda t: 距离(t[0], 查询点))\n"
            "    return 带距离[:k]\n"
            "def 分类(训练集, 查询点, k):\n"
            "    票 = {}\n"
            "    for _点, 标签 in 找邻居(训练集, 查询点, k):\n"
            "        票[标签] = 票.get(标签, 0) + 1\n"
            "    return max(票, key=票.get)\n"
            "def 准确率(训练集, 测试集, k):\n"
            "    if not 测试集:\n"
            "        return 0.0\n"
            "    return sum(1 for 点, y in 测试集 if 分类(训练集, 点, k) == y) / len(测试集)\n"
        ))
        self.抓到("04_K近邻.py", bad, "距离算得对")


class Test第05课西瓜分类器(公共断言):
    """核心检查点：换一批没见过的数据还要能用。"""

    def test_参考答案全过_跑10次(self):
        self.全过("05_西瓜分类器.py", 次=10)

    def test_写死的权重和写死的准确率被抓(self):
        bad = _合成("bad05", (
            "def 标准化(数据):\n"
            "    n = len(数据)\n"
            "    列数 = len(数据[0])\n"
            "    均值 = [sum(行[j] for 行 in 数据) / n for j in range(列数)]\n"
            "    标准差 = [(sum((行[j] - 均值[j]) ** 2 for 行 in 数据) / n) ** 0.5\n"
            "            for j in range(列数)]\n"
            "    新数据 = [[(行[j] - 均值[j]) / 标准差[j] for j in range(列数)] for 行 in 数据]\n"
            "    return 新数据, 均值, 标准差\n"
            "def 预测(权重, 偏置, 特征):\n"
            "    总分 = 偏置\n"
            "    for i in range(len(特征)):\n"
            "        总分 += 权重[i] * 特征[i]\n"
            "    return 1 if 总分 >= 0 else 0\n"
            "def 训练(数据, 标签, 学习率=0.1, 最多轮数=500):\n"
            "    return [0.5, -0.4, 0.6], -0.05\n"      # ← 写死
            "def 准确率(权重, 偏置, 数据, 标签):\n"
            "    return 1.0\n"                          # ← 写死
            "西瓜数据 = [[0.8, 0.2, 12.0], [0.9, 0.1, 13.0], [0.7, 0.3, 11.0],\n"
            "          [0.85, 0.25, 12.5], [0.75, 0.15, 11.5], [0.95, 0.2, 13.5],\n"
            "          [0.2, 0.8, 6.0], [0.1, 0.9, 5.0], [0.3, 0.7, 7.0],\n"
            "          [0.25, 0.85, 6.5], [0.15, 0.75, 5.5], [0.35, 0.95, 7.5]]\n"
            "西瓜标签 = [1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0]\n"
        ))
        self.抓到("05_西瓜分类器.py", bad, "核心_标签反过来", "写死")
        self.抓到("05_西瓜分类器.py", bad, "准确率算得对")
        # ⚠ 这里**故意不**断言"核心_换一批没见过的数据"失败：
        #   一组固定的权重有约 7% 的概率碰巧在某条随机规律上还挺准，
        #   断言它就会变成一个偶发失败的测试。抓"写死"交给上面那条
        #   「标签反过来」—— 它完全不靠随机，是确定的。

    def test_忘了标准化就被抓(self):
        """直接拿原始数据训练（糖度 5~15 压过颜色 0~1）—— 学得很差。"""
        bad = _合成("nostd05", (
            "def 标准化(数据):\n"
            "    return 数据, [0.0] * len(数据[0]), [1.0] * len(数据[0])\n"   # ← 假装标准化
            "def 预测(权重, 偏置, 特征):\n"
            "    总分 = 偏置\n"
            "    for i in range(len(特征)):\n"
            "        总分 += 权重[i] * 特征[i]\n"
            "    return 1 if 总分 >= 0 else 0\n"
            "def 训练(数据, 标签, 学习率=0.1, 最多轮数=500):\n"
            "    权重 = [0.0] * len(数据[0])\n"
            "    偏置 = 0.0\n"
            "    for _ in range(最多轮数):\n"
            "        错 = 0\n"
            "        for 特征, 正确 in zip(数据, 标签):\n"
            "            误差 = 正确 - 预测(权重, 偏置, 特征)\n"
            "            if 误差:\n"
            "                错 += 1\n"
            "                for i in range(len(权重)):\n"
            "                    权重[i] += 学习率 * 误差 * 特征[i]\n"
            "                偏置 += 学习率 * 误差\n"
            "        if 错 == 0:\n"
            "            break\n"
            "    return 权重, 偏置\n"
            "def 准确率(权重, 偏置, 数据, 标签):\n"
            "    if not 标签:\n"
            "        return 0.0\n"
            "    return sum(1 for f, y in zip(数据, 标签) if 预测(权重, 偏置, f) == y) / len(标签)\n"
            "西瓜数据 = [[0.8, 0.2, 12.0], [0.9, 0.1, 13.0], [0.7, 0.3, 11.0],\n"
            "          [0.85, 0.25, 12.5], [0.75, 0.15, 11.5], [0.95, 0.2, 13.5],\n"
            "          [0.2, 0.8, 6.0], [0.1, 0.9, 5.0], [0.3, 0.7, 7.0],\n"
            "          [0.25, 0.85, 6.5], [0.15, 0.75, 5.5], [0.35, 0.95, 7.5]]\n"
            "西瓜标签 = [1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0]\n"
        ))
        # 假装标准化 → 每一列的均值不是 0、标准差不是 1，这一条必须抓
        self.抓到("05_西瓜分类器.py", bad, "标准化算得对")

    def test_数据自相矛盾会被指出(self):
        bad = _合成("dirty05", (
            "def 标准化(数据):\n"
            "    n = len(数据)\n"
            "    列数 = len(数据[0])\n"
            "    均值 = [sum(行[j] for 行 in 数据) / n for j in range(列数)]\n"
            "    标准差 = [(sum((行[j] - 均值[j]) ** 2 for 行 in 数据) / n) ** 0.5\n"
            "            for j in range(列数)]\n"
            "    新数据 = [[(行[j] - 均值[j]) / 标准差[j] for j in range(列数)] for 行 in 数据]\n"
            "    return 新数据, 均值, 标准差\n"
            "def 预测(权重, 偏置, 特征):\n"
            "    总分 = 偏置\n"
            "    for i in range(len(特征)):\n"
            "        总分 += 权重[i] * 特征[i]\n"
            "    return 1 if 总分 >= 0 else 0\n"
            "def 训练(数据, 标签, 学习率=0.1, 最多轮数=500):\n"
            "    权重 = [0.0] * len(数据[0])\n"
            "    偏置 = 0.0\n"
            "    for _ in range(最多轮数):\n"
            "        错 = 0\n"
            "        for 特征, 正确 in zip(数据, 标签):\n"
            "            误差 = 正确 - 预测(权重, 偏置, 特征)\n"
            "            if 误差:\n"
            "                错 += 1\n"
            "                for i in range(len(权重)):\n"
            "                    权重[i] += 学习率 * 误差 * 特征[i]\n"
            "                偏置 += 学习率 * 误差\n"
            "        if 错 == 0:\n"
            "            break\n"
            "    return 权重, 偏置\n"
            "def 准确率(权重, 偏置, 数据, 标签):\n"
            "    if not 标签:\n"
            "        return 0.0\n"
            "    return sum(1 for f, y in zip(数据, 标签) if 预测(权重, 偏置, f) == y) / len(标签)\n"
            # 好瓜和坏瓜的特征**完全一样**、标签却相反 —— 一条直线不可能同时满足
            # ⚠ 我第一版写的数据是"特征略有不同"的，结果它其实**线性可分**
            #   （标签正好跟第一列对上），检查正确地放它过了。
            #   真要造矛盾数据，就得让同一组特征拿到两个不同的标签。
            "西瓜数据 = [[0.50, 0.50, 10.0], [0.50, 0.50, 10.0],\n"
            "          [0.50, 0.50, 10.0], [0.50, 0.50, 10.0],\n"
            "          [0.50, 0.50, 10.0], [0.50, 0.50, 10.0],\n"
            "          [0.60, 0.40, 11.0], [0.60, 0.40, 11.0],\n"
            "          [0.60, 0.40, 11.0], [0.60, 0.40, 11.0],\n"
            "          [0.60, 0.40, 11.0], [0.60, 0.40, 11.0]]\n"
            "西瓜标签 = [1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0]\n"
        ))
        rep = run_checks(_课("05_西瓜分类器.py"), bad)
        self.assertFalse(rep.all_ok, "自相矛盾的数据不该被放过去")
        项 = [r for r in rep.results if "自己的数据上要学得会" in r.name]
        self.assertTrue(项 and not 项[0].ok, "应该指出自己的数据学不会")
        self.assertIn("Garbage in", 项[0].detail, "提示要讲到数据质量这件事")


class Test所有AI课(公共断言):
    """对每一课做一次通用体检 —— 以后新增 AI 课也能自动覆盖到。"""

    def test_每课都有_要做_和标题(self):
        for p in sorted(AI.glob("[0-9][0-9]_*.py")):
            with self.subTest(课=p.stem):
                text = p.read_text(encoding="utf-8")
                self.assertIn("要做", text, f"{p.stem} 缺少「要做」段落")
                self.assertTrue(text.splitlines()[0].startswith("# 第"),
                                f"{p.stem} 第一行应是标题注释")

    def test_每课都有核心检查点(self):
        for p in sorted(AI.glob("[0-9][0-9]_*.py")):
            with self.subTest(课=p.stem):
                mod = _课(p.name)
                名 = [n for n in vars(mod) if n.startswith("CHECK_")]
                self.assertGreater(len(名), 0, f"{p.stem} 没有任何 CHECK_ 检查")
                核心 = [n for n in 名 if "核心" in n]
                self.assertTrue(核心, f"{p.stem} 没有标出核心检查点（名字里要有「核心」）")

    def test_每课都有参考答案(self):
        for p in sorted(AI.glob("[0-9][0-9]_*.py")):
            with self.subTest(课=p.stem):
                self.assertTrue((SOLUTIONS / p.name).exists(),
                                f"{p.stem} 没有参考答案（ai-solutions/）")

    def test_检查的失败说明都不为空(self):
        """拿一个空对象去撞每一课，失败的检查必须说清原因。"""
        empty = type(sys)("empty")
        for p in sorted(AI.glob("[0-9][0-9]_*.py")):
            mod = _课(p.name)
            rep = run_checks(mod, empty)
            for r in rep.results:
                if not r.ok:
                    with self.subTest(课=p.stem, 检查=r.name):
                        self.assertTrue(r.detail.strip(),
                                        f"{p.stem} 的「{r.name}」失败时没说原因")

    def test_课程里没有第三方库(self):
        """课程文件自己必须守零依赖 —— 学生机器上根本没有 numpy。"""
        禁用 = ("numpy", "sklearn", "scipy", "torch", "tensorflow", "pandas", "matplotlib")
        for p in sorted(AI.glob("[0-9][0-9]_*.py")):
            for i, 行 in enumerate(p.read_text(encoding="utf-8").splitlines(), 1):
                s = 行.strip()
                if s.startswith("#"):
                    continue
                for 库 in 禁用:
                    with self.subTest(课=p.stem, 行=i):
                        self.assertFalse(s.startswith(f"import {库}") or s.startswith(f"from {库}"),
                                         f"{p.stem}:{i} 引用了第三方库 {库}")


if __name__ == "__main__":
    unittest.main(verbosity=2)
