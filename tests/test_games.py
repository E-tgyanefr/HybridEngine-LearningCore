"""小游戏的逻辑测试。

════ 为什么游戏也要测 ════

游戏的**画面**没法自动测（要人看），但**规则**完全可以 ——
而且规则出错是最要命的：赢了不判赢、电脑会下在已占的格子上，
这些 bug 玩家遇到只会觉得"这游戏坏了"，不会去报告。

所以这里把两个游戏的**规则部分**单独测。
这也正是"模块化"的好处：规则和画面分开写之后，规则才有可能被单独验证。

    · 猜数字：判断大小、输入解析
    · 井字棋：胜负判定、合法落子、电脑策略（能赢就赢、该挡就挡）
"""

import importlib.util
import os
import sys
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def _load(path: str, name: str):
    """按文件路径加载模块 —— 游戏文件名是中文，没法直接 import。"""
    spec = importlib.util.spec_from_file_location(name, path)
    mod = importlib.util.module_from_spec(spec)
    sys.modules[name] = mod
    spec.loader.exec_module(mod)
    return mod


猜数字 = _load(os.path.join(ROOT, "games", "猜数字.py"), "game_guess")
井字棋 = _load(os.path.join(ROOT, "games", "井字棋.py"), "game_ttt")


class Test猜数字规则(unittest.TestCase):
    def test_判断大小(self):
        self.assertEqual(猜数字.判断(50, 30), "大了")
        self.assertEqual(猜数字.判断(10, 30), "小了")
        self.assertEqual(猜数字.判断(30, 30), "中了")

    def test_难度表合法(self):
        """每个难度的范围必须是合法的（小 < 大），否则游戏一开始就坏。"""
        for k, (名字, 小, 大) in 猜数字.难度表.items():
            with self.subTest(难度=名字):
                self.assertLess(小, 大, f"{名字} 的范围反了")
                self.assertIn(名字, 猜数字.最多提示次数, f"{名字} 缺提示次数")


class Test井字棋规则(unittest.TestCase):
    def setUp(self):
        self.b = 井字棋.新棋盘()

    def test_新棋盘是空的(self):
        self.assertEqual(len(self.b), 9)
        self.assertTrue(all(x == 井字棋.空 for x in self.b))

    def test_赢家_横(self):
        for 线 in [(0, 1, 2), (3, 4, 5), (6, 7, 8)]:
            b = 井字棋.新棋盘()
            for i in 线:
                b[i] = 井字棋.我
            with self.subTest(线=线):
                self.assertEqual(井字棋.赢家(b), 井字棋.我)

    def test_赢家_竖(self):
        for 线 in [(0, 3, 6), (1, 4, 7), (2, 5, 8)]:
            b = 井字棋.新棋盘()
            for i in 线:
                b[i] = 井字棋.电脑
            with self.subTest(线=线):
                self.assertEqual(井字棋.赢家(b), 井字棋.电脑)

    def test_赢家_斜(self):
        for 线 in [(0, 4, 8), (2, 4, 6)]:
            b = 井字棋.新棋盘()
            for i in 线:
                b[i] = 井字棋.我
            with self.subTest(线=线):
                self.assertEqual(井字棋.赢家(b), 井字棋.我)

    def test_没赢家(self):
        b = 井字棋.新棋盘()
        b[0] = 井字棋.我
        b[1] = 井字棋.电脑
        self.assertEqual(井字棋.赢家(b), 井字棋.空)

    def test_平局(self):
        # 一个真实的平局局面
        b = [井字棋.我, 井字棋.电脑, 井字棋.我,
             井字棋.我, 井字棋.电脑, 井字棋.电脑,
             井字棋.电脑, 井字棋.我, 井字棋.我]
        self.assertTrue(井字棋.平局(b))
        self.assertEqual(井字棋.赢家(b), 井字棋.空)

    def test_非平局(self):
        b = 井字棋.新棋盘()
        self.assertFalse(井字棋.平局(b), "空棋盘不该算平局")

    def test_能落子(self):
        self.assertTrue(井字棋.能落子(self.b, 0))
        self.assertFalse(井字棋.能落子(self.b, 9), "越界不该允许")
        self.assertFalse(井字棋.能落子(self.b, -1), "负数下标不该允许")
        self.b[4] = 井字棋.我
        self.assertFalse(井字棋.能落子(self.b, 4), "已占的格子不该允许")

    def test_落子(self):
        井字棋.落子(self.b, 3, 井字棋.我)
        self.assertEqual(self.b[3], 井字棋.我)


class Test电脑策略(unittest.TestCase):
    """电脑策略必须能赢就赢、该挡就挡 —— 否则游戏太蠢，学生学不到东西。"""

    def test_能赢就赢(self):
        b = 井字棋.新棋盘()
        b[0] = 井字棋.电脑
        b[1] = 井字棋.电脑
        # 位置 2 能连成三个 → 必须下在 2
        self.assertEqual(井字棋.电脑走(b), 2, "电脑能一步赢却没赢")

    def test_能赢就赢_另一条线(self):
        b = 井字棋.新棋盘()
        b[0] = 井字棋.电脑
        b[4] = 井字棋.电脑
        self.assertEqual(井字棋.电脑走(b), 8, "斜线能赢却没赢")

    def test_该挡就挡(self):
        b = 井字棋.新棋盘()
        b[3] = 井字棋.我
        b[4] = 井字棋.我
        # 位置 5 是玩家能赢的地方 → 必须挡
        self.assertEqual(井字棋.电脑走(b), 5, "玩家要赢了却没挡")

    def test_赢优先于挡(self):
        """自己能赢的时候不该去挡 —— 顺序反了就会一直防守。"""
        b = 井字棋.新棋盘()
        b[0] = 井字棋.电脑
        b[1] = 井字棋.电脑          # 电脑在 2 能赢
        b[6] = 井字棋.我
        b[7] = 井字棋.我            # 玩家在 8 能赢
        self.assertEqual(井字棋.电脑走(b), 2, "应该先赢，而不是去挡")

    def test_总是走在空格上(self):
        """不管什么局面，电脑都不能下在已占的格子上。"""
        import random
        random.seed(1234)
        for _ in range(200):
            b = 井字棋.新棋盘()
            # 随机铺一些棋子
            for i in range(9):
                if random.random() < 0.5:
                    b[i] = random.choice([井字棋.我, 井字棋.电脑])
            if 井字棋.赢家(b) != 井字棋.空 or 井字棋.平局(b):
                continue
            if all(x != 井字棋.空 for x in b):
                continue
            位 = 井字棋.电脑走(b)
            with self.subTest(棋盘=b):
                self.assertTrue(0 <= 位 < 9, "落点越界")
                self.assertEqual(b[位], 井字棋.空, "下在了已占的格子上")

    def test_占中间(self):
        """开局没威胁时应该占中间（4）—— 那是井字棋的最优首步。"""
        b = 井字棋.新棋盘()
        b[0] = 井字棋.我            # 玩家占了角，电脑该占中间
        self.assertEqual(井字棋.电脑走(b), 4)


class Test坐标换算(unittest.TestCase):
    """格中心坐标 —— 这个是画面和逻辑的接缝，最容易错。"""

    def test_九个格子各不相同(self):
        pts = [井字棋.格中心(i) for i in range(9)]
        self.assertEqual(len(set(pts)), 9, "有两个格子算到了同一个位置")

    def test_第0格在左上(self):
        # ⚠ 注意坐标语义：格中心返回的是**虚拟坐标**（原点左下角、y 向上），
        #   所以"视觉上更靠上"对应 **y 更大**。我第一版把这条断言写反了 ——
        #   测试写错和代码写错一样浪费时间，所以这里把语义写明。
        _, y0 = 井字棋.格中心(0)
        _, y8 = 井字棋.格中心(8)
        self.assertGreater(y0, y8, "第 0 格在视觉上方 → 虚拟 y 应该更大")

    def test_第8格在右下(self):
        x0, _ = 井字棋.格中心(0)
        x8, _ = 井字棋.格中心(8)
        self.assertLess(x0, x8, "第 8 格应该比第 0 格更靠右")


if __name__ == "__main__":
    unittest.main(verbosity=2)
