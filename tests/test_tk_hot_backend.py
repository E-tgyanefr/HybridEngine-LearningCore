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


@unittest.skipUnless(_能开窗(), "没有可用的显示环境（开不了 tkinter 窗口）")
class Tk后端视野裁剪测试(unittest.TestCase):
    """视野裁剪 —— **快不一定对**，所以这里钉的是"不许裁错"。

    ## ⚠ 为什么录制后端要关掉裁剪

    判据靠 `录制后端.指令` 数"画了几个圆"。如果录制后端也裁剪，
    那些"故意画在视野外"的判据会**假红** —— 而被裁掉的正是它们要看的。

    ⇒ 所以：**真实后端裁剪（性能），录制后端不裁剪（语义）。**
      这个差异是**有意的**，不是漏了。下面两条就是钉这件事。
    """

    def test_录制后端不裁剪(self):
        self.assertFalse(E.录制后端.允许裁剪,
                         "录制后端必须不裁剪，否则判据会假红")

    def test_真实后端默认裁剪(self):
        self.assertTrue(E.Tk后端.允许裁剪, "真实后端默认应当裁剪（性能）")

    def test_视野外的对象不提交绘制(self):
        """画布上的 item 数 = 视野内的那些，不是全部。

        ⚠ 这条同时防两个方向：
          · 裁太多了 ⇒ item 数**少于**期望（边缘的没了，画面缺东西）
          · 没裁 ⇒ item 数 = 全部（性能优化失效）
        """
        后 = E.Tk后端(宽=200, 高=200, size=100, 标题="裁剪")
        try:
            #: 画 5 个：2 个在视野内（虚拟 10..90），3 个远在视野外
            后.画圆(50, 50, 2, "红")            # 视野内
            后.画圆(20, 20, 2, "红")            # 视野内
            后.画圆(500, 50, 2, "蓝")           # 远右
            后.画圆(50, -500, 2, "蓝")          # 远下
            后.画圆(-900, 900, 2, "蓝")         # 远左上
            n = len(后._canvas.find_all())
            self.assertEqual(n, 2, "应当只画视野内的那 2 个（实际 %d）" % n)
        finally:
            后.关闭()

    def test_关掉裁剪就全画(self):
        后 = E.Tk后端(宽=200, 高=200, size=100, 标题="不裁")
        后.允许裁剪 = False
        try:
            后.画圆(50, 50, 2, "红")
            后.画圆(500, 50, 2, "蓝")
            后.画圆(-900, 900, 2, "蓝")
            self.assertEqual(len(后._canvas.find_all()), 3,
                             "关掉裁剪后必须全都画（一个不落）")
        finally:
            后.关闭()

    def test_边缘压线的不许被裁掉(self):
        """⚠ 裁剪最容易出的错：**只看中心点** ⇒ 画面边缘会闪。

        MARGIN 就是为这个留的 —— 下面这些"中心在视野外、但边缘压线"的
        必须还能画出来。
        """
        后 = E.Tk后端(宽=200, 高=200, size=100, 标题="边缘")
        try:
            #: 虚拟 100 是右边界；画在 102（中心刚出去），MARGIN=10 ⇒ 该留着
            后.画圆(102, 50, 2, "红")
            self.assertEqual(len(后._canvas.find_all()), 1,
                             "中心刚出界但边缘压线的，必须留着（否则画面边缘会闪）")
        finally:
            后.关闭()


if __name__ == "__main__":
    unittest.main(verbosity=2)
