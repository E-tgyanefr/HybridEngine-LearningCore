# -*- coding: utf-8 -*-
"""果蝇神经模拟的测试 —— **模块本身** + **第 55 课的正反两条判据**。

════════ 为什么这一课要配一份"正反两条"的测试 ════════

这一仓踩过一个大坑：**只有反面验证**（拿错答案去问"检查抓不抓得住"），
没有正面验证（拿参考答案去问"检查放不放得过"）。后果是
`M.参考答案(课)` 那条路出了问题**一直没人发现**。

所以这里两条都做：

    · **正面**：参考答案（以及 `M.参考答案()` 出来的那份）跑本课的 `CHECK_*`
      ⇒ **一个 `×` 都不许有**
    · **反面**：五个"看起来在跑"的错答案 ⇒ 每一个都必须被**指定的那条**检查拒掉
      ⇒ 而且**必须真的跑到**（报告里连 √/× 都没有 ⇒ 判失败，不是"通过"）

⚠ 最后那条「没跑到算失败」不是洁癖：`run_checks` 在答案**加载不了**时
  会产生一份**一个记号都没有**的报告 —— 那和"全过了"长得一模一样。
"""

import ast
import os
import sys
import types
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from hybridengine_light import 果蝇                                     # noqa: E402
from hybridengine_light.check import load_module, run_checks, report    # noqa: E402

课路 = ROOT / "lessons" / "55_果蝇神经模拟.py"
解路 = ROOT / "lessons" / "solutions" / "55_果蝇神经模拟.py"


def _答案对象(源码: str, 名: str = "错答案"):
    """把一段源码变成"像学员答案"的模块对象。"""
    m = types.ModuleType(名)
    exec(compile(源码, 名, "exec"), m.__dict__)          # noqa: S102 —— 测试里故意的
    return m


def _跑(答案) -> tuple[object, str]:
    课 = load_module(课路, name="lesson_55_测试")
    rep = run_checks(课, 答案)
    return rep, report(rep)


class 果蝇模块测试(unittest.TestCase):
    """`hybridengine_light/果蝇.py` 本身。"""

    def test_同种子逐位一致(self):
        """⭐ 确定性：同种子 ⇒ **逐位相同**（不是"差不多"）。"""
        a = 果蝇.仿真(果蝇.默认参数())
        b = 果蝇.仿真(果蝇.默认参数())
        self.assertEqual(a.每步放电数, b.每步放电数)
        self.assertEqual(a.平均放电率, b.平均放电率)

    def test_换种子结果不同(self):
        a = 果蝇.仿真(果蝇.默认参数(种子=1))
        b = 果蝇.仿真(果蝇.默认参数(种子=2))
        self.assertNotEqual(a.每步放电数, b.每步放电数,
                            "换了种子结果还是同一个 —— 随机没起作用")

    def test_只用标准库(self):
        """零第三方依赖 —— 用 AST 数 import，不靠肉眼看。"""
        树 = ast.parse(Path(果蝇.__file__).read_text(encoding="utf-8"))
        名 = set()
        for 节 in ast.walk(树):
            if isinstance(节, ast.Import):
                名.update(a.name.split(".")[0] for a in 节.names)
            elif isinstance(节, ast.ImportFrom) and 节.module:
                名.add(节.module.split(".")[0])
        self.assertTrue(名 <= {"random", "dataclasses", "sys", "__future__"},
                        "果蝇.py 引了标准库之外的东西：%s"
                        % sorted(名 - {"random", "dataclasses", "sys", "__future__"}))

    def test_不碰图形库(self):
        """headless：源码里不许出现任何图形库（模块从头到尾不开窗口）。"""
        文 = Path(果蝇.__file__).read_text(encoding="utf-8")
        for 禁 in ("tkinter", "matplotlib", "PIL", "pygame", "numpy"):
            self.assertNotIn("import %s" % 禁, 文, "果蝇.py 不该 import %s" % 禁)

    def test_默认参数下放电率在合理区间(self):
        """⭐ 「结果对不对」：稀疏放电（少数人在说、多数人在听）。"""
        结 = 果蝇.仿真(果蝇.默认参数())
        self.assertGreater(结.平均放电率, 0.01, "几乎不放电 —— 网络没被点着")
        self.assertLess(结.平均放电率, 0.25, "超过 1/4 的神经元每步都发放 —— 烧起来了")
        self.assertEqual(结.步数, 果蝇.默认参数().仿真步数)
        self.assertEqual(len(结.每步放电数), 结.步数)

    def test_抑制真的压住兴奋(self):
        """⭐ 同一张网、同一份输入，只把抑制关掉 ⇒ 放电率必须**大涨**。"""
        有, 无 = 果蝇.抑制对比()
        self.assertGreater(无.平均放电率, 有.平均放电率,
                           "关掉抑制之后放电率没上去 —— 负权重没生效")
        self.assertGreater(无.平均放电率 / 有.平均放电率, 1.5,
                           "抑制只压住了一点点，演示不出「刹车」这件事")
        self.assertGreater(无.平均放电率, 0.15, "关掉抑制也没烧起来 —— 兴奋太弱")

    def test_抑制对比只改抑制这一件事(self):
        """对照实验的网络必须**逐位相同**（否则分不清是谁的功劳）。"""
        p = 果蝇.默认参数()
        有 = 果蝇.造网(p)
        无 = 果蝇.造网(果蝇.默认参数(抑制强度=0.0))
        self.assertEqual(有.边, 无.边, "两次的网络不一样，对照实验不成立")

    def test_边数大致符合连接概率(self):
        参 = 果蝇.默认参数(神经元数=120, 连接概率=0.1)
        网 = 果蝇.造网(参)
        期望 = 120 * 119 * 0.1
        self.assertGreater(网.边数, 0.6 * 期望)
        self.assertLess(网.边数, 1.4 * 期望)
        self.assertTrue(all(前 != 后 for 前, 后, _ in 网.边), "出现了自环")

    def test_参数不合法会当场报错(self):
        for 坏 in (dict(神经元数=1), dict(连接概率=2.0), dict(仿真步数=0),
                   dict(漏电=1.0), dict(兴奋比例=1.0)):
            with self.subTest(**坏):
                with self.assertRaises(ValueError):
                    果蝇.默认参数(**坏)

    def test_字段名写错会报错(self):
        """⚠ 静默忽略会让你以为改了、其实没改。"""
        with self.assertRaises(TypeError):
            果蝇.默认参数(神经元数目=100)

    def test_命令行能跑出数字(self):
        """headless：命令行只打数字，返回 0。"""
        import io as _io
        import contextlib
        缓 = _io.StringIO()
        with contextlib.redirect_stdout(缓):
            码 = 果蝇.命令行(["--神经元数", "60", "--仿真步数", "40"])
        self.assertEqual(码, 0)
        出 = 缓.getvalue()
        self.assertIn("平均放电率", 出)
        self.assertIn("生成的", 出, "命令行必须自报「连接矩阵是生成的」")

    def test_命令行对照模式(self):
        import io as _io
        import contextlib
        缓 = _io.StringIO()
        with contextlib.redirect_stdout(缓):
            码 = 果蝇.命令行(["--神经元数", "60", "--仿真步数", "40", "--对照"])
        self.assertEqual(码, 0)
        self.assertIn("没抑制", 缓.getvalue())

    def test_命令行参数写错返回2(self):
        import io as _io
        import contextlib
        with contextlib.redirect_stdout(_io.StringIO()):
            self.assertEqual(果蝇.命令行(["--没有这个参数", "1"]), 2)
            self.assertEqual(果蝇.命令行(["--神经元数", "零"]), 2)


class 第55课正面判据(unittest.TestCase):
    """⭐⭐ **参考答案必须过得了自己的检查**（这一仓曾经漏掉的那条判据）。"""

    def test_参考答案源码全过(self):
        解 = load_module(解路, name="sol_55_测试")
        _, 文 = _跑(解)
        self.assertEqual(文.count("×"), 0,
                         "参考答案被自己的检查拒了：\n" + 文)
        self.assertEqual(文.count("√"), 5, "不是 5 条检查都跑了：\n" + 文)

    def test_参考答案经真实路径也全过(self):
        """走 `M.参考答案(课)`（学员真正走的那条路），不是直接读文件。"""
        from hybridengine_light import learnmodel as M
        课 = [c for c in M.发现("python") if c.编号 == "55"]
        self.assertTrue(课, "Python 线里找不到第 55 课")
        源码 = M.参考答案(课[0])
        self.assertIn("def 造连接", 源码, "M.参考答案 没给出可用的代码：\n" + 源码[:300])
        解 = _答案对象(源码, "参考答案_经M")
        _, 文 = _跑(解)
        self.assertEqual(文.count("×"), 0,
                         "经 M.参考答案() 拿到的答案过不了自己的检查：\n" + 文)


class 第55课反面判据(unittest.TestCase):
    """⭐⭐ 错答案必须被**指定的那一条**检查拒掉。"""

    #: (名字, 错答案源码, 必须被拒的那条检查名)
    #: ⚠ 检查名要写准 —— 光看"有没有报错"会把"编译/加载失败"也算成抓住。
    错答案 = [
        ("造连接用全局random_不确定",
         "import random\n"
         "def 造连接(神经元数, 连接概率, 种子):\n"
         "    边 = []\n"
         "    for 前 in range(神经元数):\n"
         "        for 后 in range(神经元数):\n"
         "            if 前 != 后 and random.random() < 连接概率:\n"
         "                边.append((前, 后, 0.09 if 前 < 神经元数 * 0.8 else -0.35))\n"
         "    return 边\n",
         "造连接"),

        ("步进忘了发放后清零",
         "def 步进(膜电位, 输入, 漏电=0.85, 阈值=1.0):\n"
         "    新, 发放 = [], []\n"
         "    for i, v in enumerate(膜电位):\n"
         "        v = v * 漏电 + 输入[i]\n"
         "        if v >= 阈值:\n"
         "            发放.append(i)\n"
         "        新.append(v)\n"
         "    return 新, 发放\n",
         "步进"),

        #  ⚠ 这一条是**最值钱**的：它的放电率**落在合理区间里**、
        #    同种子也可复现 —— 光看"数目对不对"完全抓不住它。
        #    抓住它的是「连接概率 0.08→0.30 结果必须变」那条：
        #    边造了却从不传播 ⇒ 两次**一模一样**。
        ("跑仿真造了边却不传播",
         "import random\n"
         "def 跑仿真(神经元数, 连接概率, 步数, 种子):\n"
         "    随机 = random.Random(f\"{种子}-输入\")\n"
         "    电位 = [0.0] * 神经元数\n"
         "    每步 = []\n"
         "    for _ in range(步数):\n"
         "        输入 = [0.0] * 神经元数\n"
         "        for _ in range(20):\n"
         "            输入[随机.randrange(神经元数)] += 1.0\n"
         "        新 = []\n"
         "        c = 0\n"
         "        for i, v in enumerate(电位):\n"
         "            v = v * 0.85 + 输入[i]\n"
         "            if v >= 1.0:\n"
         "                c += 1\n"
         "                v = 0.0\n"
         "            新.append(v)\n"
         "        电位 = 新\n"
         "        每步.append(c)\n"
         "    总 = sum(每步)\n"
         "    return 总 / (神经元数 * 步数), 每步\n",
         "跑仿真"),

        ("用现成的返回编出来的结果",
         "class _假:\n"
         "    def __init__(self, 步):\n"
         "        self.平均放电率 = 0.5\n"
         "        self.每步放电数 = [0] * 步\n"
         "        self.步数 = 步\n"
         "def 用现成的(**改):\n"
         "    return _假(int(改.get(\"仿真步数\", 200)))\n",
         "用现成的"),

        ("用现成的无视抑制强度",
         "def 用现成的(**改):\n"
         "    from hybridengine_light import 果蝇\n"
         "    改.pop(\"抑制强度\", None)\n"
         "    return 果蝇.仿真(果蝇.默认参数(**改))\n",
         "抑制真的压住了兴奋"),
    ]

    def test_每条错答案都被指定的检查拒掉(self):
        for 名, 源码, 期望检查 in self.错答案:
            with self.subTest(错答案=名):
                解 = _答案对象(源码, "错_%s" % 名)
                rep, 文 = _跑(解)

                #  ⚠⚠ 「没跑到」判失败 —— 报告里连一个 √/× 都没有，
                #     说明答案根本没被加载/检查根本没执行。那**不是**通过。
                self.assertGreater(rep.total, 0,
                                   "【没跑到】%s：一条检查都没执行 ⇒ 判失败\n%s" % (名, 文))
                self.assertTrue(文.count("√") + 文.count("×") > 0,
                                "【没跑到】%s：报告里没有任何记号 ⇒ 判失败\n%s" % (名, 文))

                命中 = [r for r in rep.results if r.name == 期望检查 and not r.ok]
                self.assertTrue(命中,
                                "【没抓住】%s：期望「%s」被拒，实际没被拒。\n"
                                "各条结果：%s\n%s"
                                % (名, 期望检查,
                                   [(r.name, r.ok) for r in rep.results], 文))
                self.assertTrue(命中[0].detail.strip(),
                                "【没说清】%s 的「%s」拒了，但没给说明" % (名, 期望检查))

    def test_反面判据本身没空转(self):
        """⚠ 自检：错答案**不能**全部通过 —— 否则这些判据是摆设。"""
        全过 = []
        for 名, 源码, _ in self.错答案:
            _, 文 = _跑(_答案对象(源码, "判_%s" % 名))
            if "全部通过" in 文:
                全过.append(名)
        self.assertEqual(全过, [], "这些错答案被判成全部通过了：%s" % 全过)


if __name__ == "__main__":
    unittest.main(verbosity=2)
