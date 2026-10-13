"""「自己试试」（自由练习）—— 判据。

## 作者原话

    「**就是在自己尝试中来一个入口就行**」

主页原来只有两个入口（游戏编写 / 课程学习），而两个都要求你**先选**
（选语言 / 选游戏线）—— "我想随便写两句试试"这个最自然的动作**没有入口**。

## ⚠ 这个功能的危险不在"能不能用"，在"会不会弄坏别的东西"

它借用了课程体系那一整套机制（`课` / `运行` / `保存答案` / `答题文件`）。
借用的好处是一行不用改就能跑，坏处是**它有可能污染课程体系** ——
所以下面这些判据钉的多半是"**不许**"：

    ① 不许出现在任何课程线里（`发现()` 找不到它）
    ② 不许指向任何课程文件（源文件/答题文件都必须是它自己那个新文件）
    ③ 不许因为轨道表缺条目而炸
    ④ `显示()` 不许吐出内部标记（`__自由__` 那种）

⚠ 界面那一半（按钮真的在、点开真的进得去）由 `tests/test_gui_flow.py`
  和实测覆盖；这里钉的是**模型层**，因为它才是会被别处复用的部分。
"""
from __future__ import annotations

import os
import sys
import unittest

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, LC)

from hybridengine_light import learnmodel as M                          # noqa: E402


class 自由练习测试(unittest.TestCase):

    def setUp(self):
        self.k = M.新建自由课()

    # ── ① 它必须"像一课"，否则现有机制用不了 ────────────────────────
    def test_是一课且开箱即用(self):
        self.assertIsInstance(self.k, M.课)
        self.assertTrue(self.k.开箱即用,
                        "必须是开箱即用（零依赖）—— 否则会走编译型那条需要 dotnet 的分支")

    def test_自由练习标记为真(self):
        self.assertTrue(self.k.自由练习)
        #: 反过来：真课程不能被误判成自由练习
        for 轨 in ("python", "games", "enginecourse"):
            try:
                课们 = M.发现(轨)
            except Exception:
                continue
            for c in 课们:
                self.assertFalse(c.自由练习,
                                 "真课程 %s 被误判成自由练习了" % c.显示())

    # ── ② 最关键：不许污染课程体系 ──────────────────────────────────
    def test_不出现在任何课程线里(self):
        """⚠⚠ 这是**最要紧**的一条。

        它真混进某条线的话：学员会在课程树里看到一个叫"自由练习"的课、
        它会占掉一个课号、还可能被记进度。
        """
        编号 = self.k.编号
        全 = []
        for 轨 in M.轨道:
            try:
                全 += [c.编号 for c in M.发现(轨)]
            except Exception:
                pass
        self.assertNotIn(编号, 全,
                         "自由练习的编号混进了课程线：%s" % 编号)

    def test_源文件和答题文件是同一个新文件(self):
        """⚠ 它不能指向任何课程文件 —— 那会让"保存"覆盖成品。

        （`_答案文件()` 里有过一次这个 bug：`return k.源文件` ⇒
           GUI 的答案框显示课程本身，学员一保存就覆盖课程。）
        """
        self.assertEqual(self.k.源文件, self.k.答题文件,
                         "自由练习的'题目'就是'你的作品'，必须是同一个文件")
        名 = self.k.答题文件.name
        self.assertEqual(名, "我的游戏.py", "落点应当是 games/我的游戏.py（实际 %s）" % 名)
        self.assertEqual(self.k.答题文件.parent.name, "games")
        #: 不在任何课程目录里
        for 目录 in ("lessons", "engine", "csharp", "ai", "embedded", "raspi"):
            self.assertNotIn(os.sep + 目录 + os.sep,
                             str(self.k.答题文件),
                             "自由练习的文件落进了课程目录 %s" % 目录)

    # ── ③ 界面能显示的形态 ─────────────────────────────────────────
    def test_显示不吐内部标记(self):
        """`__自由__  自由练习` 那种输出像没做完。"""
        语 = self.k.显示()
        self.assertEqual(语, "自由练习")
        self.assertNotIn("__", 语)

    def test_轨道在轨道表里(self):
        """⚠ 借的那条轨道必须真实存在 —— 否则 `开箱即用` 会 KeyError。"""
        self.assertIn(self.k.轨道, M.轨道)

    # ── ④ 每次调都要给一个**新的**课对象 ────────────────────────────
    def test_两次调用互不影响(self):
        a, b = M.新建自由课(), M.新建自由课()
        self.assertIsNot(a, b, "每次都要新造 —— 复用同一个对象会让界面串状态")
        self.assertEqual(a.答题文件, b.答题文件, "但落点应当一致（同一个文件）")


if __name__ == "__main__":
    unittest.main(verbosity=2)
