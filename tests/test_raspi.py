"""树莓派模拟器的测试。

════ 为什么单独测 ════

树莓派和单片机在**程序形态**上有一个关键差别，它决定了课程怎么教、
以及模拟器必须保证什么语义：

    单片机：写一个 loop()，程序永远在转
    树莓派：跑着完整 Linux，你的程序是一个**普通进程**，有开始有结束

由此推出一条学生最容易搞错的语义：
**程序结束不会自动复位引脚。** 真实板子上，程序退出后引脚保持最后的状态 ——
不显式关掉，LED 就一直亮着。

所以「闪一下」这种函数必须**自己**把灯关掉。这是树莓派第 01 课的核心检查点，
也是这一组测试要保护的东西。
"""

import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from hybridengine_light.raspi import 树莓派, GPIO, 引脚错误, 传感器  # noqa: E402


class Test引脚(unittest.TestCase):
    def setUp(self):
        self.派 = 树莓派()

    def test_用的是BCM编号(self):
        """树莓派按**芯片编号**（BCM）而不是排针位置编号。

        所以常见的是 17 / 18 / 27 / 22 —— 不是 1~40 顺序排下来的。
        """
        s = self.派.可用引脚()
        for 号 in ("17", "18", "27", "22"):
            self.assertIn(号, s)

    def test_引脚号写错要报错(self):
        """⚠ 真实板子上这**不会报错** —— 那个引脚只是毫无反应。

        模拟器报错是刻意的：帮学生养成"先确认引脚号"的习惯。
        这条行为必须钉住 —— 哪天它变成静默失败，测试要红。
        """
        with self.assertRaises(引脚错误):
            self.派.GPIO.设模式(99, GPIO.输出)

    def test_引脚号要是整数(self):
        with self.assertRaises(引脚错误):
            self.派.GPIO.设模式("十七", GPIO.输出)

    def test_没设模式就写要报错(self):
        """真实板子上这也是**没反应**，不报错。模拟器替他报出来。"""
        with self.assertRaises(引脚错误):
            self.派.GPIO.写(4, GPIO.高)

    def test_输出能写不能读(self):
        self.派.GPIO.设模式(17, GPIO.输出)
        self.派.GPIO.写(17, GPIO.高)
        self.assertTrue(self.派._取引脚(17).高)
        with self.assertRaises(引脚错误):
            self.派.GPIO.读(17)

    def test_输入能读不能写(self):
        self.派.GPIO.设模式(2, GPIO.输入)
        with self.assertRaises(引脚错误):
            self.派.GPIO.写(2, GPIO.高)

    def test_清理复位所有引脚(self):
        self.派.GPIO.设模式(17, GPIO.输出)
        self.派.GPIO.写(17, GPIO.高)
        self.派.GPIO.清理()
        self.assertFalse(self.派._取引脚(17).高)


class TestLED(unittest.TestCase):
    def setUp(self):
        self.派 = 树莓派()
        self.led = self.派.接LED(17)

    def test_初始是灭的(self):
        self.assertEqual(self.led.状态(), "灭")

    def test_亮灭翻转(self):
        self.led.亮()
        self.assertEqual(self.led.状态(), "亮")
        self.led.灭()
        self.assertEqual(self.led.状态(), "灭")
        self.led.翻转()
        self.assertEqual(self.led.状态(), "亮")
        self.led.翻转()
        self.assertEqual(self.led.状态(), "灭")

    def test_接LED自动配好模式(self):
        """接LED 是"设模式 + 拿到一个顺手的小对象"的合体。

        不该要求调用方再手动 设模式 —— 那是 C 式 API 的负担，
        而这个模拟器是给初学者的。
        """
        self.派.GPIO.写(17, GPIO.高)          # 能写 = 模式已经配好了
        self.assertEqual(self.led.状态(), "亮")


class Test按钮(unittest.TestCase):
    def setUp(self):
        self.派 = 树莓派()
        self.派.GPIO.设模式(2, GPIO.输入)

    def test_没按是False(self):
        self.assertFalse(self.派.GPIO.读(2))

    def test_按下是True(self):
        """⚠ 教学语义，不是电压语义。

        真实电路里上拉的按钮按下时引脚是**低**电平（因为接地）。
        但那个反转对学编程毫无帮助：学生按下按钮却读到 False，
        只会怀疑自己写错了代码。所以模拟器统一成"按下 = True"。
        """
        self.派.按下(2)
        self.assertTrue(self.派.GPIO.读(2))

    def test_松开回到False(self):
        self.派.按下(2)
        self.派.松开(2)
        self.assertFalse(self.派.GPIO.读(2))

    def test_按输出引脚要报错(self):
        self.派.GPIO.设模式(17, GPIO.输出)
        with self.assertRaises(引脚错误):
            self.派.按下(17)


class Test传感器(unittest.TestCase):
    def setUp(self):
        self.派 = 树莓派()
        self.温度 = self.派.加传感器("温度", "°C", (0.0, 50.0), 25.0)

    def test_读初始值(self):
        self.assertEqual(self.温度.读(), 25.0)

    def test_能改读数(self):
        """不设的话读数永远不变，"根据读数做判断"就没法验证。"""
        self.温度.设为(38.5)
        self.assertEqual(self.温度.读(), 38.5)

    def test_超范围要报错(self):
        with self.assertRaises(ValueError):
            self.温度.设为(999.0)

    def test_按名字直接访问(self):
        self.assertEqual(self.派.温度.读(), 25.0)

    def test_多个传感器互不干扰(self):
        self.派.加传感器("光线", "lux", (0.0, 1000.0), 300.0)
        self.温度.设为(40.0)
        self.assertEqual(self.派.光线.读(), 300.0, "改一个不该影响另一个")

    def test_传感器们能列出全部(self):
        self.派.加传感器("湿度", "%", (0.0, 100.0), 60.0)
        self.assertEqual(len(self.派.传感器们), 2)


class Test数据记录(unittest.TestCase):
    """树莓派最典型的用法之一：把传感器数据记下来。"""

    def setUp(self):
        self.派 = 树莓派()

    def test_写入能读回(self):
        self.派.写文件("/home/pi/数据.csv", "25.0\n")
        self.assertEqual(self.派.读文件("/home/pi/数据.csv"), "25.0\n")

    def test_是追加不是覆盖(self):
        """⚠ 记录数据必须**追加**。

        覆盖的话只剩最后一条 —— 那记录就没有意义了。
        这是第 03 课的核心检查点。
        """
        for v in ("25.0", "26.5", "38.5"):
            self.派.写文件("/home/pi/数据.csv", v + "\n")
        内容 = self.派.读文件("/home/pi/数据.csv")
        self.assertEqual(内容, "25.0\n26.5\n38.5\n")
        self.assertEqual(len(内容.strip().split("\n")), 3, "三条记录都该在")

    def test_读不存在的文件返回空(self):
        self.assertEqual(self.派.读文件("/不存在"), "")

    def test_文件列表(self):
        self.派.写文件("a.txt", "1")
        self.派.写文件("b.txt", "2")
        self.assertEqual(self.派.文件列表(), ["a.txt", "b.txt"])


class Test时间(unittest.TestCase):
    def test_推进时间(self):
        派 = 树莓派()
        派.时间推进(1.5)
        self.assertAlmostEqual(派.时间, 1.5)

    def test_睡等于推进时间(self):
        """模拟 time.sleep() —— 树莓派上很常用（轮询、防抖、控制节奏）。"""
        派 = 树莓派()
        派.睡(2.0)
        self.assertAlmostEqual(派.时间, 2.0)

    def test_时间不能倒流(self):
        with self.assertRaises(ValueError):
            树莓派().时间推进(-1)


class Test树莓派与单片机的关键差别(unittest.TestCase):
    """⚠ 这一组是本文件的核心。"""

    def test_程序结束不复位引脚(self):
        """**整条树莓派课程线最重要的一条语义。**

        单片机教程里常见的假设是"程序一直在 loop 里转"，所以每轮都会重设引脚。
        但树莓派上你的程序是一个普通进程 —— **退出后引脚保持最后的状态**。

        所以「闪一下」这种函数必须自己把灯关掉。
        如果哪天模拟器改成"函数返回就自动复位"，这条测试要红 ——
        因为那会让学生学到一个真实硬件上不成立的假设。
        """
        派 = 树莓派()
        led = 派.接LED(17)

        def 只点亮不关():
            led.亮()

        只点亮不关()          # 函数返回了
        self.assertEqual(led.状态(), "亮",
                         "函数返回后引脚不该被自动复位 —— 真实树莓派就是这样")

    def test_显式关掉才复位(self):
        派 = 树莓派()
        led = 派.接LED(17)
        led.亮()
        led.灭()
        self.assertEqual(led.状态(), "灭")

    def test_日志按发生顺序记(self):
        """检查要靠日志判断"先亮后灭"这个顺序 —— 所以日志必须有顺序。"""
        派 = 树莓派()
        led = 派.接LED(17)
        led.亮()
        led.灭()
        序列 = [l for l in 派.日志 if "GPIO 17" in l and ("高" in l or "低" in l)]
        self.assertEqual(len(序列), 2)
        self.assertIn("高", 序列[0], "第一条应该是拉高")
        self.assertIn("低", 序列[1], "第二条应该是拉低")


class Test报告(unittest.TestCase):
    def test_报告列出引脚传感器文件(self):
        派 = 树莓派()
        派.接LED(17)
        派.加传感器("温度", "°C", (0.0, 50.0), 25.0)
        派.写文件("/home/pi/数据.csv", "25\n")
        r = 派.报告()
        self.assertIn("GPIO 17", r)
        self.assertIn("温度", r)
        self.assertIn("数据.csv", r)

    def test_没配置过时有提示(self):
        self.assertIn("还没有配置过任何引脚", 树莓派().报告())

    def test_日志为空时有提示(self):
        self.assertIn("没有记录到任何操作", 树莓派().日志文本())


if __name__ == "__main__":
    unittest.main(verbosity=2)
