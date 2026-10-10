"""动效原语的判据（`anim.py`）。

## ⚠ 为什么给"动画"写判据

因为**缓动曲线是纯函数** —— 而它最容易在"凭手感调"的过程中被改坏：

· 端点不精确 ⇒ 动画结束时停在"差一点"的位置，**看着像卡住**
· 单调性坏了 ⇒ 弹一下变成抖两下
· 回弹不小心调大 ⇒ 界面显得廉价，而且"显示的是不是最终值"变得不确定

这些都是**肉眼不一定看得出来、但用户能感觉到不对劲**的东西。
用判据把"手感"钉住，改的时候才知道有没有碰坏。

⚠ **不测**：动起来看着舒不舒服。那需要人眼。
"""
from __future__ import annotations

import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from hybridengine_light import anim as A                        # noqa: E402


class 缓动函数测试(unittest.TestCase):
    """所有缓动都必须**精确**打到端点上。"""

    缓动们 = (("线性", A.线性), ("缓出", A.缓出), ("缓入", A.缓入), ("回弹", A.回弹))

    def test_进度0严格等于起点(self):
        # ⚠ 差一点点的话，动画帧永远显示不出"起始态"
        for 名, 缓 in self.缓动们:
            self.assertEqual(A.缩放值(0, 10, 20, 缓), 10, f"{名} 起点不精确")

    def test_进度1严格等于终点(self):
        # ⚠ 这条最要紧：不然动画会停在"差一点"的位置上，**看着像卡住**
        for 名, 缓 in self.缓动们:
            self.assertEqual(A.缩放值(1, 10, 20, 缓), 20, f"{名} 终点不精确")

    def test_越界进度被夹住(self):
        for 名, 缓 in self.缓动们:
            self.assertEqual(A.缩放值(-5, 10, 20, 缓), 10, f"{名} 负进度")
            self.assertEqual(A.缩放值(9, 10, 20, 缓), 20, f"{名} 超界进度")

    def test_单调不减(self):
        # 回弹允许末尾过冲，所以只查这三个
        for 名, 缓 in (("线性", A.线性), ("缓出", A.缓出), ("缓入", A.缓入)):
            值 = [A.缩放值(i / 40, 0, 1, 缓) for i in range(41)]
            for i in range(40):
                self.assertLessEqual(值[i], 值[i + 1] + 1e-12, f"{名} 第 {i} 步回退了")

    def test_回弹真的过冲但不过分(self):
        # ⚠ 两面都要判：
        #   不过冲 ⇒ 回弹就没有存在意义（与缓出一样了）
        #   过冲太大 ⇒ 界面显得廉价，而且"显示的是不是最终值"不确定
        峰 = max(A.缩放值(i / 100, 0, 1, A.回弹) for i in range(101))
        self.assertGreater(峰, 1.0, "回弹没有过冲")
        self.assertLess(峰, 1.06, f"回弹过冲太大（峰值 {峰:.4f}）")

    def test_缓出先快后慢(self):
        self.assertGreater(A.缩放值(0.5, 0, 1, A.缓出), 0.5)

    def test_缓入先慢后快(self):
        self.assertLess(A.缩放值(0.5, 0, 1, A.缓入), 0.5)

    def test_可以反向映射(self):
        # 从大到小也要能用（比如"消失"）
        self.assertLess(A.缩放值(0.5, 1.0, 0.0, A.缓出), 0.5)


class 控件测试(unittest.TestCase):
    """有显示环境时验证控件能构造、能跑、**能停**。

    ⚠ `停()` 那一项不是凑数：动画还在跑时窗口被销毁的话，
      `after` 回调会打到已销毁的控件上，Tkinter 抛 TclError ——
      而这类错的栈里只有 Tk 内部帧，很难查。
    """

    @classmethod
    def setUpClass(cls):
        try:
            import tkinter
            r = tkinter.Tk()
            r.withdraw()          # 不弹窗
            cls.根 = r
        except Exception as e:
            raise unittest.SkipTest(f"没有显示环境：{e}")

    @classmethod
    def tearDownClass(cls):
        try:
            cls.根.destroy()
        except Exception:
            pass

    def test_旋转弧能开能停(self):
        import tkinter as tk
        c = tk.Canvas(self.根, width=40, height=40)
        a = A.旋转弧(c, 20, 20, 8)
        a.开始()
        self.根.update()
        a.停()
        self.根.update()

    def test_结果徽标能画能清(self):
        import tkinter as tk
        c = tk.Canvas(self.根, width=200, height=40)
        b = A.结果徽标(c, 20, 20)
        b.显示(True, "全过")
        self.根.update()
        self.assertGreater(len(c.find_all()), 0, "成功徽标没画出任何东西")
        b.清()
        self.根.update()
        b.显示(False, "还要改")
        self.根.update()
        self.assertGreater(len(c.find_all()), 0, "失败徽标没画出任何东西")
        b.停()

    def test_进度条能开能停(self):
        import tkinter as tk
        c = tk.Canvas(self.根, width=200, height=10)
        p = A.进度条(c, 200)
        p.开始()
        self.根.update()
        p.停()
        self.根.update()

    def test_窗口销毁时停不会抛(self):
        # ⚠ 真场景：动画跑着的时候用户把窗口关了
        import tkinter as tk
        w = tk.Toplevel(self.根)
        c = tk.Canvas(w, width=40, height=40)
        a = A.旋转弧(c, 20, 20, 8)
        a.开始()
        w.destroy()
        try:
            a.停()          # 不该抛
        except tk.TclError as e:
            self.fail(f"停() 在控件已销毁时抛了：{e}")


if __name__ == "__main__":
    unittest.main()
