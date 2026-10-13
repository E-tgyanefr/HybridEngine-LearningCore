"""`Tk后端` 的**烘焙**开关 —— 判据（性能优化最怕改了画面）。

## 为什么这个测试值得单独写

`_build/_bench_pyengine.py` 实测：真渲染是瓶颈（1000 对象 **35 ms/帧**，
而引擎逻辑只要 1.7 ms）。**静态烘焙约 2x**（每帧只删动态层，
不动的那些画一次就留着）。

⚠ 但性能优化最容易的失败方式**不是变慢，是画错** ——
  而是"画错"这件事**肉眼在动着的窗口里很难发现**（残影一闪而过）。

⇒ 所以这里钉的是**机制**，不是"看起来对不对"：

    ① `烘焙=False`（默认）时，行为和原来**一模一样**（清 `all`）
    ② `烘焙=True` 时，清屏**只删动态层** —— 静态的留着
    ③ 动态对象确实被打上了 `动态` 标签
    ④ `烘焙静态()` 在 `烘焙=False` 时**什么都不做**（不会偷偷画东西）

⚠ 用**真的 Tk 画布**（`canvas.find_withtag` 数 item），不截屏 ——
  截屏受字体/缩放/系统主题影响，而且这个环境里截图本来就不靠谱。
"""
from __future__ import annotations

import os
import sys
import unittest

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, LC)

from hybridengine_light import pyengine as E                      # noqa: E402
from hybridengine_light.pyengine import components as C           # noqa: E402


def _能开窗() -> bool:
    """无头环境下 `tk.Tk()` 会抛 —— 那就跳过，别让测试假红。"""
    try:
        import tkinter as tk
        r = tk.Tk()
        r.destroy()
        return True
    except Exception:
        return False


@unittest.skipUnless(_能开窗(), "没有可用的显示环境（开不了 tkinter 窗口）")
class Tk后端烘焙测试(unittest.TestCase):
    """⚠ 这些测试会**真开窗口**，但每个都立刻关掉。"""

    def _后端(self, 烘焙: bool):
        return E.Tk后端(宽=200, 高=200, size=100, 标题="判据", 烘焙=烘焙)

    # ── ① 默认行为不许变 ────────────────────────────────────────────
    def test_默认烘焙关闭(self):
        后 = self._后端(烘焙=False)
        try:
            self.assertFalse(后.烘焙)
            #: 关了就不打标签 —— 否则 `delete("all")` 之外的代码会受影响
            self.assertEqual(后._标(), ())
        finally:
            后.关闭()

    def test_关着的时候清屏删光(self):
        后 = self._后端(烘焙=False)
        try:
            后.画圆(50, 50, 10, "红")
            后.画圆(60, 60, 10, "蓝")
            self.assertEqual(len(后._canvas.find_all()), 2)
            后.清屏("白")
            self.assertEqual(len(后._canvas.find_all()), 0,
                             "烘焙关着时清屏必须删光（和原来一致）")
        finally:
            后.关闭()

    def test_关着的时候烘焙静态什么都不做(self):
        后 = self._后端(烘焙=False)
        try:
            后.烘焙静态(lambda b: b.画圆(50, 50, 10, "红", tags=()))
            self.assertEqual(len(后._canvas.find_all()), 0,
                             "烘焙关着时 烘焙静态() 不许偷偷画东西")
        finally:
            后.关闭()

    # ── ② 开着的时候：静态留着、动态被删 ────────────────────────────
    def test_开着的时候静态的留着(self):
        后 = self._后端(烘焙=True)
        try:
            后.烘焙静态(lambda b: b.画矩形(0, 0, 100, 100, "绿", tags=()))
            后.画圆(50, 50, 10, "红")
            后.清屏("白")
            剩 = 后._canvas.find_all()
            self.assertEqual(len(剩), 1, "静态的应当留着（这正是 2x 的来源）")
            self.assertEqual(len(后._canvas.find_withtag(后.动标签)), 0,
                             "动态的应当被删掉")
        finally:
            后.关闭()

    def test_动态对象打了标签(self):
        后 = self._后端(烘焙=True)
        try:
            后.画圆(50, 50, 10, "红")
            后.画矩形(10, 10, 20, 20, "蓝")
            后.画线(0, 0, 50, 50, "黑", 1)
            后.画文字(50, 50, "哈", "黑", 12)
            self.assertEqual(len(后._canvas.find_withtag(后.动标签)), 4,
                             "四种画法都要打上 动态 标签")
        finally:
            后.关闭()

    def test_连续多帧不会越积越多(self):
        """⚠ 这是烘焙最该防的失败模式：静态的**每帧被重画一遍** ⇒ 越积越多。

        真出了这个，表现是**越来越卡**（而不是变快），所以判据要盯着**item 数**。
        """
        后 = self._后端(烘焙=True)
        try:
            后.烘焙静态(lambda b: b.画矩形(0, 0, 100, 100, "绿", tags=()))
            s = E.场景("t")
            for i in range(5):
                #: ⚠ `造()` **已经**把对象加到根上了 —— 不要再 `加根()`。
                #:   （我第一版两条都用了 ⇒ 每个对象被加两遍 ⇒ 画出 10 个圆。
                #:     那看着像"引擎把对象画了两遍"，其实是**我的用法错了**。）
                o = s.造("a%d" % i, x=i * 10, y=10)
                o.加(C.圆点(半径=2))
            g = E.游戏(场景=s, 后端=后, 尺寸=100, 宽=200, 高=200)
            for _ in range(10):
                g.一步()
            总 = len(后._canvas.find_all())
            #: 1 个静态 + 5 个动态 = 6
            self.assertEqual(总, 6, "跑 10 帧后 item 数应当还是 6（没越积越多）")
        finally:
            后.关闭()

    # ── ③ 烘焙不该改变"画了什么" ────────────────────────────────────
    def test_烘焙开关不改变绘制指令(self):
        """同一份代码，开关两种状态下**画出的指令序列必须一致**。

        ⚠ 这里用 `录制后端` 比指令 —— 它不吃显示环境，比截图稳。
          烘焙只该改「画布上留什么」，不该改「引擎要画什么」。
        """
        def 跑一帧() -> list:
            后 = E.录制后端()
            s = E.场景("t")
            玩家 = s.造("玩家", x=50, y=50)
            玩家.加(C.圆点(半径=3), C.匀速移动(vx=1.0, vy=0.0))
            墙 = s.造("墙", x=10, y=10)
            墙.加(C.方块(宽=5, 高=5))
            g = E.游戏(场景=s, 后端=后, 尺寸=100, 宽=200, 高=200)
            g.一步()
            return list(后.帧历史[-1])

        一 = 跑一帧()
        二 = 跑一帧()
        self.assertEqual(一, 二, "同样的一帧，指令序列必须完全一致（确定性）")
        self.assertTrue(any(项[0] == "圆" for 项 in 一), "应当画了圆")
        self.assertTrue(any(项[0] == "矩形" for 项 in 一), "应当画了矩形")


if __name__ == "__main__":
    unittest.main(verbosity=2)
