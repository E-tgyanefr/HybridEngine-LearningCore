"""「按回车关闭」的判据（`run.py::需要暂停等待`）。

## 为什么这个缺陷值得一整套判据

实测暴露的：**双击 exe 时窗口一闪而过**。

Windows 双击 exe 会给它一个**新的控制台**；程序打印完课程列表、进程一结束，
窗口就跟着关了 —— 学员看到的是"什么都没看见"。

修法**不是**无条件加 `input()`：那会把命令行用户每次都卡住。
真正的判据是「只在该暂停的时候暂停」，而这句话有 5 个输入、8 种组合 ——
**凭直觉写必然漏掉一种**（我第一版就漏了"管道"那种：`... | more` 会挂住）。

⚠ 这份测的是判据函数本身。**"双击真的会暂停"只有人工双击才算数** ——
   `sys.frozen` 在源码里永远是 False，测试环境模拟不出来。
"""
from __future__ import annotations

import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from hybridengine_light.run import 需要暂停等待            # noqa: E402


class 暂停判据测试(unittest.TestCase):
    """（冻结, 是终端, 参数个数, 有PROMPT, 用了编辑器）-> 要不要暂停。"""

    def test_双击exe要暂停(self):
        # 这是整个缺陷的核心场景
        self.assertTrue(需要暂停等待(True, True, 0, False))

    def test_命令行跑exe不暂停(self):
        # ⚠ 不区分的话，从 cmd 跑的人每次都被卡住 —— 修一个缺陷造出另一个
        self.assertFalse(需要暂停等待(True, True, 0, True))

    def test_带参数看某一课不暂停(self):
        # 正文留在屏幕上，不用暂停
        self.assertFalse(需要暂停等待(True, True, 1, False))

    def test_用过编辑器后要暂停(self):
        # ⚠ 编辑器**清屏**退出 —— 正文被擦掉。双击进来的学员会看到
        #   "一片空白 + 窗口关闭"，所以哪怕有参数也必须留一次机会。
        self.assertTrue(需要暂停等待(True, True, 1, False, 用了编辑器=True))

    def test_编辑器后但从命令行不暂停(self):
        self.assertFalse(需要暂停等待(True, True, 1, True, 用了编辑器=True))

    def test_源码用法不暂停(self):
        # 源码用法必经终端 ⇒ 窗口不会关
        self.assertFalse(需要暂停等待(False, True, 0, False))

    def test_管道或重定向不暂停(self):
        # ⚠ 最容易被漏的一种：`... | more` 或 `> out.txt` 时 input() 会阻塞/报错
        self.assertFalse(需要暂停等待(True, False, 0, False))
        self.assertFalse(需要暂停等待(True, False, 1, False, 用了编辑器=True))

    def test_五种输入都参与判定(self):
        # 反面判据：把任意一项翻掉，结果都该变（说明它真的被读了）
        基 = 需要暂停等待(True, True, 0, False)
        self.assertTrue(基)
        self.assertFalse(需要暂停等待(False, True, 0, False), "冻结没被读")
        self.assertFalse(需要暂停等待(True, False, 0, False), "终端没被读")
        self.assertFalse(需要暂停等待(True, True, 1, False), "参数个数没被读")
        self.assertFalse(需要暂停等待(True, True, 0, True), "PROMPT 没被读")


class 真会炸的路径测试(unittest.TestCase):
    """⚠ 这一条是**实测炸过之后**补的，它防的是一类特殊缺陷。

    ## 那次炸的是什么

    我在 `main()` 里写了 `os.environ.get("PROMPT")`，但 `run.py` **没 import os**。
    211 个用例全过 —— 因为源码里 `sys.frozen` 恒为 False，
    `冻结 and ...` **短路求值**，`os.environ` 那行**根本没执行**。
    只有在打包产物里才会炸：

        NameError: name 'os' is not defined

    ⇒ 所以判据不能只测"判据函数的结果对不对"（那是纯函数，喂参数就行），
      还要**强制求值那条平时被短路的路径**。

    做法：把 `sys.frozen` 临时设成 True（模拟冻结），
    再直接调用 `需要暂停等待`，并且**把 5 个参数全传满** —— 这样
    `and` 右侧一定会被求值，缺 import 就会在此处 NameError。
    """

    def test_冻结态下五个参数都会真求值(self):
        import hybridengine_light.run as R
        有 = hasattr(sys, "frozen")
        旧 = getattr(sys, "frozen", None)
        sys.frozen = True                      # 模拟打包产物
        try:
            # 参数给全 ⇒ 不短路 ⇒ 真的会读 os.environ / sys.stdin
            真 = R.需要暂停等待(True, True, 0, bool(__import__("os").environ.get("PROMPT")))
            self.assertIn(真, (True, False))   # 不抛异常就是通过
        except NameError as e:
            self.fail(f"冻结态下这条路径会 NameError：{e}（多半是某个模块少了 import）")
        finally:
            if 有:
                sys.frozen = 旧
            else:
                del sys.frozen


if __name__ == "__main__":
    unittest.main()
