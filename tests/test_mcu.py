"""单片机模拟器的测试。

════ 为什么模拟器本身要测 ════

因为**所有嵌入式课程都建在它上面**。它的语义错了，
上面的课程就全错 —— 而且是"看起来能跑、结论是错的"那种错。

我自己就踩过一个：第一版照搬真实电压语义（上拉的按钮按下是**低**电平），
结果"没按"和"按下"都返回 False —— 学生按下按钮读到"没按"，
只会怀疑自己写错了代码。

所以这里把模拟器的**语义**逐条钉住：
  · 按下 = 读到 True（教学语义，不是电压语义）
  · 引脚越界要报错（真实硬件上不会）
  · 两个反向输出接一起要报短路（真实硬件上会烧引脚）
  · 悬空输入要提醒
  · 时间必须手动推进
"""

import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from hybridengine_light.mcu import 模拟器, 引脚编号错误, 短路错误  # noqa: E402


class Test引脚(unittest.TestCase):
    def setUp(self):
        self.板 = 模拟器(引脚数=8, 名字="测试板")

    def test_输出引脚初始是灭的(self):
        led = self.板.输出引脚(2, 名字="LED")
        self.assertEqual(led.状态(), "灭")

    def test_高就亮_低就灭(self):
        led = self.板.输出引脚(2)
        led.高()
        self.assertEqual(led.状态(), "亮")
        led.低()
        self.assertEqual(led.状态(), "灭")

    def test_翻转(self):
        led = self.板.输出引脚(2)
        led.翻转()
        self.assertEqual(led.状态(), "亮")
        led.翻转()
        self.assertEqual(led.状态(), "灭")

    def test_引脚越界要报错(self):
        """⚠ 真实硬件上写错引脚号**不会报错**，板子只是毫无反应。

        模拟器在这里报错，是为了帮学生养成"先确认引脚号"的习惯。
        这条行为是**刻意的**，所以钉住它 —— 哪天它变成静默失败，测试要红。
        """
        with self.assertRaises(引脚编号错误):
            self.板.输出引脚(99)

    def test_引脚号要是整数(self):
        with self.assertRaises(引脚编号错误):
            self.板.输出引脚("二")

    def test_引脚数太少要拒绝(self):
        with self.assertRaises(ValueError):
            模拟器(引脚数=1)


class Test按钮语义(unittest.TestCase):
    """⚠ 这一组是本文件最重要的部分。

    模拟器用的是**教学语义**：按下 = 读到 True。
    不是真实电压语义（上拉按钮按下时引脚是低电平）。

    为什么这么定：学生按下按钮，期待读到"按下了"。
    照搬电压语义会让他读到 False，只会怀疑自己写错了代码 ——
    那个反转对学编程毫无帮助，纯属噪音。
    """

    def setUp(self):
        self.板 = 模拟器(引脚数=8)
        self.btn = self.板.输入引脚(3, 名字="按钮", 上下拉="上拉")

    def test_没按是False(self):
        self.assertFalse(self.板.读到(self.btn))

    def test_按下是True(self):
        """⚠ 这条如果反了，所有按钮课程都会教错。"""
        self.板.按下(self.btn)
        self.assertTrue(self.板.读到(self.btn))

    def test_松开回到False(self):
        self.板.按下(self.btn)
        self.板.松开(self.btn)
        self.assertFalse(self.板.读到(self.btn))

    def test_下拉也一样(self):
        """上下拉的区别不该影响"按下读到 True"这个语义。"""
        板 = 模拟器(引脚数=8)
        btn = 板.输入引脚(4, 上下拉="下拉")
        self.assertFalse(板.读到(btn))
        板.按下(btn)
        self.assertTrue(板.读到(btn), "下拉接法的按钮按下也该读到 True")

    def test_读输出引脚要报错(self):
        led = self.板.输出引脚(2)
        with self.assertRaises(ValueError):
            self.板.读到(led)

    def test_按输出引脚要报错(self):
        led = self.板.输出引脚(2)
        with self.assertRaises(ValueError):
            self.板.按下(led)


class Test短路保护(unittest.TestCase):
    def test_反向输出接一起要报短路(self):
        """⚠ 真实硬件上这会烧引脚。模拟器报错是在替学生省钱。"""
        板 = 模拟器(引脚数=8)
        a = 板.输出引脚(5, 初始="高")
        b = 板.输出引脚(6, 初始="低")
        with self.assertRaises(短路错误):
            板.接到一起(a, b)

    def test_同向输出接一起不报错(self):
        板 = 模拟器(引脚数=8)
        a = 板.输出引脚(5, 初始="高")
        b = 板.输出引脚(6, 初始="高")
        板.接到一起(a, b)      # 不该抛
        self.assertTrue(True)


class Test悬空警告(unittest.TestCase):
    def test_没设上下拉要提醒(self):
        """真实电路里悬空的输入读到的是不确定的值 —— 这是新手常见的坑。"""
        板 = 模拟器(引脚数=8)
        板.输入引脚(7, 名字="悬空")
        报告 = 板.报告()
        self.assertIn("悬空", 报告)

    def test_设了上下拉就不提醒(self):
        板 = 模拟器(引脚数=8)
        板.输入引脚(7, 上下拉="上拉")
        报告 = 板.报告()
        self.assertNotIn("悬空", 报告)


class Test时间(unittest.TestCase):
    def test_时间从头开始(self):
        self.assertEqual(模拟器().时间, 0.0)

    def test_推进时间(self):
        板 = 模拟器()
        板.时间推进(1.5)
        self.assertAlmostEqual(板.时间, 1.5)

    def test_时间不能倒流(self):
        with self.assertRaises(ValueError):
            模拟器().时间推进(-1)

    def test_为什么必须手动推进(self):
        """真实硬件里时间是真的在走的。

        模拟器里如果不手动推，`while 板.时间 < 1: pass` 会永远跑不完 ——
        所以这个方法是**必需**的，不是可选装饰。这条测试说明它的存在理由。
        """
        板 = 模拟器()
        次数 = 0
        while 板.时间 < 1.0 and 次数 < 100:
            板.时间推进(0.25)
            次数 += 1
        self.assertGreaterEqual(板.时间, 1.0)
        self.assertEqual(次数, 4, "每推进 0.25 秒，4 次刚好到 1 秒")


class Test报告与日志(unittest.TestCase):
    def test_报告列出用过的引脚(self):
        板 = 模拟器(引脚数=8)
        板.输出引脚(2, 名字="LED")
        r = 板.报告()
        self.assertIn("引脚 2", r)
        self.assertIn("输出", r)

    def test_没配置过引脚时有提示(self):
        r = 模拟器().报告()
        self.assertIn("还没有配置过任何引脚", r)

    def test_日志记录每次操作(self):
        板 = 模拟器()
        led = 板.输出引脚(2)
        led.高()
        日志 = 板.日志文本()
        self.assertIn("高", 日志)

    def test_日志带时间戳(self):
        板 = 模拟器()
        板.时间推进(2.5)
        板.输出引脚(2)
        self.assertIn("2.50s", 板.日志文本())


class Test课程可用性(unittest.TestCase):
    """确认课程真正需要的能力都在 —— 这是模拟器存在的理由。"""

    def test_能模拟按钮控制LED(self):
        """课程 02 的核心场景。"""
        板 = 模拟器(引脚数=8)
        led = 板.输出引脚(2, 名字="LED")
        btn = 板.输入引脚(3, 名字="按钮", 上下拉="上拉")

        # 模拟一段真实程序：读按钮 → 决定 LED
        for 期望 in (False, True, True, False):
            if 期望:
                板.按下(btn)
            else:
                板.松开(btn)
            if 板.读到(btn):
                led.高()
            else:
                led.低()
            self.assertEqual(led.状态() == "亮", 期望)

    def test_能模拟闪烁(self):
        """课程 03 的核心场景。"""
        板 = 模拟器(引脚数=8)
        led = 板.输出引脚(2)
        序列 = []
        for _ in range(6):
            板.时间推进(0.5)
            led.翻转()
            序列.append(led.状态())
        self.assertEqual(序列, ["亮", "灭", "亮", "灭", "亮", "灭"])

    def test_能模拟状态机(self):
        """课程 04 的核心场景：按一下换一种模式。"""
        板 = 模拟器(引脚数=8)
        btn = 板.输入引脚(3, 上下拉="上拉")
        模式表 = ["灭", "慢闪", "快闪"]
        当前 = 0

        def 按一下():
            nonlocal 当前
            板.按下(btn)
            板.松开(btn)
            当前 = (当前 + 1) % len(模式表)

        for 期望 in (1, 2, 0, 1):
            按一下()
            self.assertEqual(当前, 期望)
            self.assertEqual(模式表[当前], 模式表[期望])


if __name__ == "__main__":
    unittest.main(verbosity=2)
