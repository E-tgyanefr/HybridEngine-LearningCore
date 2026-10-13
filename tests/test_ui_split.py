"""两个内核的 UI 拆分的判据。

## 作者定的

    「学习内核与样本内核是两个不同的程序，UI 应该分别重新设计」

而且明确选了「**完全不共用，各写一份**」。

## ⚠ 所以这里钉的都是"**结构**"，不是"界面长什么样"

界面好不好看要靠眼睛（判据钉不住）。但**"有没有真的分开"是能钉的**，
而且恰恰是最容易被后人破坏的东西 —— 一句
`from .learn_gui import _画行号` 就能把"两套 UI"变回"一套 UI 加个开关"，
而**所有测试都还是会过**（功能没坏）。

⇒ 下面这些判据就是那种"坏了也没人发现"的问题的守门人。

## ⚠ 另一条：面板表必须和引擎对得上

`sample_gui.面板` 是**手写的** 15 个组件表。它和
`pyengine.components` 一旦对不上，界面上的按钮就是**假的** ——
双击下去报"组件不存在"。⇒ 判据逐个实例化，不放过。
"""
from __future__ import annotations

import ast
import io
import os
import unittest

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

#: 允许共用的"零件库" —— 只有这两个。
#: ⚠ 加东西进这个名单要慎重：它等于承认"这两个界面又要绑在一起"。
允许共用 = {"theme", "pyengine", "_paths"}


def _真import(路径: str) -> list[str]:
    """取一个 .py 里**真正的 import** 目标（用 AST，不是正则）。

    ⚠ 用 AST 而不是 grep 很要紧：注释里提到 `learn_gui` 是**正常的**
      （我在 `sample_gui.py` 里就写了 4 处来解释"为什么不共用"），
      用正则会把它们当成 import，得出假红。
    """
    t = io.open(路径, encoding="utf-8").read()
    出 = []
    for 节 in ast.walk(ast.parse(t)):
        if isinstance(节, ast.Import):
            for a in 节.names:
                出.append(a.name)
        elif isinstance(节, ast.ImportFrom):
            #: `from . import X` ⇒ module 是 None，要连 names 一起看
            基 = 节.module or ""
            if 节.level:
                出.append("." * 节.level + 基)
                for a in 节.names:
                    出.append("." * 节.level + (基 + "." if 基 else "") + a.name)
            else:
                出.append(基)
                for a in 节.names:
                    出.append(基 + "." + a.name)
    return 出


class UI拆分测试(unittest.TestCase):
    """两个界面文件必须真的分开。"""

    def setUp(self):
        self.目录 = os.path.join(LC, "hybridengine_light")
        self.学习 = os.path.join(self.目录, "learn_gui.py")
        self.样本 = os.path.join(self.目录, "sample_gui.py")

    def test_两个界面文件都在(self):
        self.assertTrue(os.path.isfile(self.学习), "learn_gui.py 不在")
        self.assertTrue(os.path.isfile(self.样本), "sample_gui.py 不在 —— 样本内核的 UI 呢？")

    def test_学习界面不import样本界面(self):
        出 = _真import(self.学习)
        撞 = [x for x in 出 if "sample_gui" in x]
        self.assertEqual(撞, [],
                         "learn_gui.py 去 import sample_gui 了 —— 两个 UI 不再是两套：%s" % 撞)

    def test_样本界面不import学习界面(self):
        """⚠ 这条最要紧 —— 它是"能不能各自演化"的分界线。"""
        出 = _真import(self.样本)
        撞 = [x for x in 出 if "learn_gui" in x]
        self.assertEqual(撞, [],
                         "sample_gui.py 去 import learn_gui 了 —— "
                         "那它就不是「另一个程序」了：%s" % 撞)

    def test_样本界面只共用零件库(self):
        """`sample_gui` 只准从包内拿 `theme` / `pyengine` 这类**零件**。"""
        出 = _真import(self.样本)
        包内 = [x for x in 出 if x.startswith(".")]
        for 名 in 包内:
            裸 = 名.lstrip(".").split(".")[-1]
            if 裸 in ("", "theme", "pyengine", "components", "sample_gui"):
                continue
            #: `from .pyengine.components import *` 这类是允许的
            if 裸 in ("components", "scene", "engine", "render", "games"):
                continue
            self.assertIn(裸, 允许共用,
                          "sample_gui 从包内拿了不该拿的东西：%s（只准共用 %s）"
                          % (名, sorted(允许共用)))

    def test_两个界面都继承tk根窗口(self):
        """入口约定：两个都能 `界面().mainloop()`。

        ⚠ 这条是**给 `run.py` 用的** —— 它按名字取 `main`，
          两个界面必须都提供 `main()`。
        """
        for 名, 路 in (("learn_gui", self.学习), ("sample_gui", self.样本)):
            t = io.open(路, encoding="utf-8").read()
            树 = ast.parse(t)
            有main = any(isinstance(n, ast.FunctionDef) and n.name == "main"
                         for n in 树.body)
            self.assertTrue(有main, "%s 没有顶层 main() —— run.py 取不到入口" % 名)


class 面板表测试(unittest.TestCase):
    """`sample_gui.面板` 必须和引擎里的组件**对得上**。

    ⚠ 对不上的后果是**界面上的按钮是假的** —— 双击就报"组件不存在"。
      而那种错**只在用户点到那一个按钮时才暴露**。
    """

    def setUp(self):
        import sys
        if LC not in sys.path:
            sys.path.insert(0, LC)
        from hybridengine_light import sample_gui as SG
        self.SG = SG

    def test_面板表非空(self):
        self.assertGreater(len(self.SG.面板), 5, "面板表太小了")

    def test_面板里每个组件都真能实例化(self):
        """⭐ 逐个造一遍 —— 这是这份表唯一有判别力的判据。"""
        坏 = []
        for (名, 组, 说明, 参数) in self.SG.面板:
            类 = getattr(self.SG.E.components, 名, None)
            if 类 is None:
                坏.append("%s（引擎里没这个组件）" % 名)
                continue
            try:
                类(**参数)
            except Exception as e:
                坏.append("%s（%s: %s）" % (名, type(e).__name__, str(e)[:60]))
        self.assertEqual(坏, [], "面板表里有造不出来的组件：\n      " + "\n      ".join(坏))

    def test_分组名都在分组顺序里(self):
        """组名写错的话那个组件**在界面上根本不出现**（静默消失）。"""
        for (名, 组, _, _) in self.SG.面板:
            self.assertIn(组, self.SG.分组顺序,
                          "组件 %s 的分组 %r 不在 分组顺序 里 ⇒ 界面上看不到它"
                          % (名, 组))

    def test_说明书非空(self):
        for (名, _, 说明, _) in self.SG.面板:
            self.assertTrue(说明.strip(), "组件 %s 没有说明" % 名)

    def test_参数都是关键字能过的(self):
        """参数必须全是关键字 —— 位置参数会在组件改签名时静默错位。"""
        for (名, _, _, 参数) in self.SG.面板:
            self.assertIsInstance(参数, dict, "%s 的参数不是 dict" % 名)


class 界面分叉点测试(unittest.TestCase):
    """`run.py` 里那行"开哪个界面"的常量。"""

    def setUp(self):
        import sys
        if LC not in sys.path:
            sys.path.insert(0, LC)
        from hybridengine_light import run as R
        self.R = R

    def test_有分叉常量(self):
        self.assertTrue(hasattr(self.R, "图形界面模块"),
                        "run.py 里没有 图形界面模块 —— 两个产品没法开不同的界面")

    def test_分叉点的值是能导入的模块名(self):
        import importlib
        名 = self.R.图形界面模块
        self.assertTrue(名.startswith("hybridengine_light."),
                        "图形界面模块 应当是完整的模块名：%s" % 名)
        m = importlib.import_module(名)          #: 导不进来就直接炸（好过静默）
        self.assertTrue(callable(getattr(m, "main", None)),
                        "%s 没有 main()" % 名)


if __name__ == "__main__":
    unittest.main(verbosity=2)
