"""设计令牌的判据 —— 尤其是**对比度**。

⚠⚠ 为什么这个测试必须存在：

  「大改 UI」时最容易翻车的地方就是配色 —— 调一个深色，某个
  文字忽然就看不清了。而**肉眼在深色底上特别不准**：
  两张图并排看，"感觉还行"和"真的看不清"之间没有清晰界限。

  而这件事**机器能判**：WCAG 给了公式，对比度 >= 4.5:1 就是正文的线。
  ⇒ 把"好看"交给眼睛，把"看不清"交给判据。

⚠ 这条测试还顺手锁住了另一件事：**不许有人绕过 `theme.py` 手写颜色**。
  下面那个扫描会把 `learn_gui.py` 里硬编码的十六进制颜色揪出来。
  不锁的话，半年后界面里又会散布几十个魔数色 —— 那正是这次
  「把决定集中到一处」要解决的问题。
"""
from __future__ import annotations

import os
import re
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from hybridengine_light import theme as T   # noqa: E402

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


class 对比度测试(unittest.TestCase):
    """WCAG AA：正文 >= 4.5:1。"""

    #: (名字, 前景, 背景) —— 界面上**真实出现过**的组合
    组合 = [
        ("正文 / 底", T.文字, T.底),
        ("正文 / 面板", T.文字, T.面板),
        ("正文 / 浮起", T.文字, T.浮起),
        ("次要文字 / 面板", T.文字次, T.面板),
        ("次要文字 / 底", T.文字次, T.底),
        ("弱文字 / 面板", T.文字弱, T.面板),
        ("强调 / 底", T.强调, T.底),
        ("强调 / 面板", T.强调, T.面板),
        #: ⚠ 实心按钮是**亮底深字**（见 theme.py 里那段推算）——
        #:   白字只有 2.77:1，过不了。这条锁住那个决定。
        ("实心按钮上的字", T.强调底字, T.强调底),
        ("强调亮 / 底", T.强调亮, T.底),
        ("好 / 面板", T.好, T.面板),
        ("坏 / 面板", T.坏, T.面板),
        ("警示 / 面板", T.警示, T.面板),
        ("信息 / 面板", T.信息, T.面板),
        #: 代码高亮：在编辑器底上
        ("码关键字 / 编辑底", T.码_关键字, T.底),
        ("码字符串 / 编辑底", T.码_字符串, T.底),
        ("码注释 / 编辑底", T.码_注释, T.底),
        ("码数字 / 编辑底", T.码_数字, T.底),
        ("码函数 / 编辑底", T.码_函数, T.底),
    ]

    def test_全部组合都过_AA(self):
        for 名, 前, 后 in self.组合:
            比 = T.对比度(前, 后)
            self.assertGreaterEqual(
                比, 4.5,
                f"「{名}」对比度只有 {比:.2f}:1（{前} on {后}）—— "
                f"低于 WCAG AA 的 4.5:1")

    def test_弱文字是下限_不许再暗(self):
        """`文字弱` 刻意调到刚好过 AA —— 它要是再暗一点就该红了。

        ⚠ 这条是**反向**判据：防止有人觉得"弱文字还是太亮"，
          顺手调暗一档 —— 那就掉到 4.5 以下了。

        ⚠⚠ **两个背景都要量**。这个测试的第一版只量了「面板」，
          而真正的教训是：弱文字在「底」和「面板」上**都会出现**
          （列表的次级说明在面板上，状态栏在底上）。
          只量一个背景 = 漏掉一半。现在两个都量。
        """
        for 背景名, 背景 in (("底", T.底), ("面板", T.面板)):
            比 = T.对比度(T.文字弱, 背景)
            self.assertGreaterEqual(比, 4.5,
                                    f"弱文字在「{背景名}」上只有 {比:.2f}:1")
            self.assertLess(比, 6.5,
                            f"弱文字在「{背景名}」上有 {比:.2f}:1 —— "
                            f"它本该是'弱'的，这么亮就和次要文字分不开了")

    def test_三级文字要有层次(self):
        """正文 > 次要 > 弱 —— 必须**明显拉开**，不然层次等于没有。"""
        正 = T.对比度(T.文字, T.底)
        次 = T.对比度(T.文字次, T.底)
        弱 = T.对比度(T.文字弱, T.底)
        self.assertGreater(正, 次 + 2.0, f"正文 {正:.1f} 和次要 {次:.1f} 太近")
        self.assertGreater(次, 弱 + 0.8, f"次要 {次:.1f} 和弱 {弱:.1f} 太近")

    def test_背景是三层的而且逐渐变亮(self):
        """底 < 面板 < 浮起 —— 层次反了的话"浮起来"的东西会看起来是凹的。"""
        self.assertLess(T.亮度(T.底), T.亮度(T.面板))
        self.assertLess(T.亮度(T.面板), T.亮度(T.浮起))

    def test_强调比_VSCode_的蓝更亮(self):
        """⚠ 这条记的是一个**刻意的决定**（见 theme.py 的注释）。

        VS Code 的 #0078d4 在这个暗底上只有约 4.4:1 —— 刚过线，
        对视力一般的成年人不够。换更亮的蓝是有意的，别再改回去。
        """
        我们 = T.对比度(T.强调, T.底)
        vs = T.对比度("#0078d4", T.底)
        self.assertGreater(我们, vs, "强调色又调回 VS Code 那种暗蓝了")
        self.assertGreater(我们, 6.0, f"强调色只有 {我们:.2f}:1，不够亮")


class 令牌本身测试(unittest.TestCase):
    def test_间距都是4的倍数(self):
        for 名 in ("间_极小", "间_小", "间_中", "间_大", "间_特大", "间_巨大"):
            值 = getattr(T, 名)
            self.assertEqual(值 % 4, 0, f"{名} = {值} 不是 4 的倍数")

    def test_间距严格递增(self):
        们 = [T.间_极小, T.间_小, T.间_中, T.间_大, T.间_特大, T.间_巨大]
        self.assertEqual(们, sorted(们), "间距档位不是递增的")
        self.assertEqual(len(set(们)), len(们), "间距档位有重复")

    def test_颜色都是合法的十六进制(self):
        for 名 in dir(T):
            if 名.startswith("_"):
                continue
            值 = getattr(T, 名)
            if isinstance(值, str) and 值.startswith("#"):
                self.assertRegex(值, r"^#[0-9a-fA-F]{6}$", f"{名} = {值} 格式不对")

    def test_字体族不为空且没有_Segoe_UI(self):
        """⚠ 这条锁住一个**实测踩到的**字体问题。

        `Segoe UI` 原来被用作全部文字的字体，但它对中文是**回退**的 ——
        中英文混排时字重/基线/行高都不一致（中文由雅黑代打），
        看起来"有点土"而原因很难一眼看出来。
        ⇒ 界面文字统一用 `Microsoft YaHei UI`（中英文都它管）。
        """
        self.assertTrue(T.界)
        self.assertNotIn("Segoe UI", T.界,
                         "界面字体又用回 Segoe UI 了 —— 它对中文是回退的")
        self.assertTrue(T.码)


class 不许手写颜色测试(unittest.TestCase):
    """⚠ 扫描界面代码，把硬编码的十六进制颜色揪出来。

    不锁的话，这次"把决定集中到一处"的努力半年后就白费了 ——
    界面里又会散布几十个魔数色。
    """

    #: 允许出现十六进制字面量的文件：
    #:   · `theme.py` —— 令牌本身
    #:   · `anim.py` —— 动效原语要自带默认色，不能反过来依赖 learn_gui
    #:   · `stage.py` —— **游戏画布**的调色板。那不是"界面主题"：
    #:       `"黑": "#000000"` 这种是给学员画图用的**素材色**，
    #:       和"按钮该是什么蓝"是两件事。硬拉进 theme 会让
    #:       "改界面配色"变成"改学员游戏的颜色"—— 那是**副作用**，不是特性。
    例外 = {"theme.py", "anim.py", "stage.py"}

    def test_界面代码里没有硬编码颜色(self):
        甲 = os.path.join(LC, "hybridengine_light")
        违规 = []
        for 名 in sorted(os.listdir(甲)):
            if not 名.endswith(".py") or 名 in self.例外:
                continue
            p = os.path.join(甲, 名)
            #: ⚠ 用 `with` 开文件 —— 裸 `open()` 会留一堆 ResourceWarning，
            #:   把真正的失败信息淹掉（我第一次跑就是这样，20 条输出里
            #:   一条有用的都没有）。
            with open(p, encoding="utf-8") as f:
                for i, 行 in enumerate(f, 1):
                    #: 只在"看起来是颜色字面量"时报：`"#rrggbb"` 或 `'#rrggbb'`
                    找 = re.search(r"""["']#[0-9a-fA-F]{3,8}["']""", 行)
                    if 找 and "noqa" not in 行:
                        违规.append(f"{名}:{i}  {行.strip()[:70]}")
        self.assertEqual(
            违规, [],
            "界面代码里直接写了颜色 —— 应该从 theme 取：\n    "
            + "\n    ".join(违规))


if __name__ == "__main__":
    unittest.main(verbosity=2)
