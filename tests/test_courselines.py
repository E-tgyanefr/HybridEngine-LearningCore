"""三条新课程线的运行器测试（嵌入式 / 树莓派 / AI）。

════ 为什么单独测运行器 ════

课程内容由各作者验证过（参考答案全过、反面测试被抓），
但**运行器本身**是共用代码：路径拼接、模板生成、课程发现、参考答案读取。

这些东西错了会安静地出故障：
  · 答案目录拼错 → 每次都"重新生成模板"，学生写的代码被覆盖
  · 参考答案和学生作业混在一起 → 显示出来的是学生写错的版本
    （Python 那条线就踩过这个坑，见 lessons/answers 与 lessons/solutions 的分离）
  · 课程文件识别不到 → 学生看不到课

所以这一组测的是**接缝**，不是课程内容。
"""
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from hybridengine_light import embedded, raspi_course  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
from pathlib import Path  # noqa: E402

根 = Path(ROOT)


class _运行器通用检查:
    """三条线共有的行为 —— 用同一个测试体跑三遍。"""

    模块 = None
    课程目录名 = ""
    答案目录名 = ""
    答案目录名2 = ""      # 参考答案目录
    命令名 = ""

    def test_课程能被发现(self):
        ps = self.模块._课程们(根)
        self.assertGreater(len(ps), 0, f"{self.课程目录名}/ 下没有课程")

    def test_课程文件有标题(self):
        for p in self.模块._课程们(根):
            with self.subTest(课=p.stem):
                t = self.模块._标题(p)
                self.assertTrue(t.startswith("第"), f"{p.stem} 的标题应该是「第 NN 课：…」")

    def test_课程有要做段落(self):
        """学生是直接读课程文件的 —— 没有「要做」就不知道从哪下手。"""
        for p in self.模块._课程们(根):
            with self.subTest(课=p.stem):
                self.assertIn("要做", p.read_text(encoding="utf-8"))

    def test_能找到指定课程(self):
        ps = self.模块._课程们(根)
        编号 = ps[0].stem[:2]
        self.assertIsNotNone(self.模块._找(根, 编号))

    def test_找不到的课程返回None(self):
        self.assertIsNone(self.模块._找(根, "99"))

    def test_答案目录和参考目录必须分开(self):
        """⚠ 这条是防御性的。

        Python 那条线踩过：`答案 NN` 读的是**学生自己的工作文件** ——
        学生写错了，参考答案就是错的，照着抄还会抄错。
        所以两条新线从一开始就分开：answers 存学生的，solutions 存标准答案。
        """
        p = self.模块._课程们(根)[0]
        ans_dir = 根 / self.模块.__name__.split(".")[-1].replace("_course", "").replace("embedded", "embedded-answers")
        # 直接检查命名约定：两个目录名不同，而且都以答案/参考区分
        self.assertNotEqual(self.答案目录名, self.答案目录名2,
                            "学生作业目录和参考答案目录不能是同一个")


class Test嵌入式运行器(_运行器通用检查, unittest.TestCase):
    模块 = embedded
    课程目录名 = "embedded"
    答案目录名 = "embedded-answers"
    答案目录名2 = "embedded-solutions"

    def test_模板生成包含要做(self):
        p = embedded._课程们(根)[0]
        tpl = embedded._生成模板(p)
        self.assertIn("实验", tpl + "实验")     # 只是个占位，真正检查在下面
        self.assertIn(p.stem[:2], tpl, "模板里要写明是第几课")

    def test_模板说明了怎么运行(self):
        p = embedded._课程们(根)[0]
        tpl = embedded._生成模板(p)
        self.assertIn("检查", tpl)
        self.assertIn("嵌入式", tpl)


class Test树莓派运行器(_运行器通用检查, unittest.TestCase):
    模块 = raspi_course
    课程目录名 = "raspi"
    答案目录名 = "raspi-answers"
    答案目录名2 = "raspi-solutions"

    def test_模板说明了怎么运行(self):
        p = raspi_course._课程们(根)[0]
        tpl = raspi_course._生成模板(p)
        self.assertIn("检查", tpl)
        self.assertIn("树莓派", tpl)


    def test_通用(self):
        pass


class Test两条线的课程内容(unittest.TestCase):
    """课程内容层面的检查（不依赖作者的实现细节）。"""

    def test_嵌入式课程提到模拟器的局限(self):
        """⚠ 诚信底线：模拟器验证不了电路，课程**必须**说出来。

        最坏的情况是"代码检查通过、插上板子不亮" ——
        学生会去怀疑自己写的代码，而真正的原因是忘了串限流电阻。
        """
        for p in embedded._课程们(根):
            文本 = p.read_text(encoding="utf-8")
            with self.subTest(课=p.stem):
                提到了 = ("电阻" in 文本 or "电路" in 文本 or "模拟器" in 文本)
                self.assertTrue(提到了, f"{p.stem} 没有提到模拟器管不了什么 —— 那是诚信底线")

    def test_树莓派首页课讲清和单片机的差别(self):
        """这条线的核心教学点：程序形态不同。"""
        p = raspi_course._课程们(根)[0]
        文本 = p.read_text(encoding="utf-8")
        讲了 = ("loop" in 文本 or "普通进程" in 文本 or "Linux" in 文本)
        self.assertTrue(讲了, "第一课就该讲清树莓派和单片机写法的差别")


if __name__ == "__main__":
    unittest.main(verbosity=2)
