"""Python 引擎的判据（`hybridengine_light/pyengine`）。

## 为什么这套判据特别值得写

引擎的错**大多不会立刻炸**，而是"看着能跑但结果不对"：

· 父子变换算错 ⇒ 子对象跟着父走的时候位置偏一点（截图看不出来）
· 固定步长没做 ⇒ 同一份代码在快/慢机器上结果不同（本机永远看不出）
· 斜走没归一 ⇒ 斜着走快 41%（"手感不对"但说不上哪不对）
· 输入只清了一半 ⇒ 按住键会连发（偶尔才注意到）

⇒ 这些**都能用判据钉住**，而肉眼看画面是钉不住的。

## 用 `录制后端` 而不是截图

`录制后端` 把绘制指令记成列表 ⇒ 判据能直接问"画了几个圆、在哪个位置"。
比截屏比像素稳得多（不受字体/缩放/系统主题影响）。
"""
from __future__ import annotations

import math
import os
import sys
import unittest

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, LC)

from hybridengine_light import pyengine as E                      # noqa: E402
from hybridengine_light.pyengine import components as C           # noqa: E402


class 场景图测试(unittest.TestCase):
    def test_父子变换_子跟着父走(self):
        s = E.场景()
        父 = s.造("父", 10, 10)
        子 = E.场景对象("子", 5, 0)
        父.加子(子)
        self.assertEqual(子.世界位置, E.向量(15, 10))
        父.变换.位置 = E.向量(20, 20)
        self.assertEqual(子.世界位置, E.向量(25, 20),
                         "父移动后子没跟着 —— 父子变换没生效")

    def test_缩放会作用到子(self):
        s = E.场景()
        父 = s.造("父", 0, 0)
        父.变换.缩放 = 2.0
        子 = E.场景对象("子", 10, 0)
        父.加子(子)
        self.assertEqual(子.世界位置, E.向量(20, 0), "父的缩放没作用到子的位置")

    def test_旋转会作用到子(self):
        s = E.场景()
        父 = s.造("父", 0, 0)
        父.变换.旋转 = math.pi / 2          # 90°
        子 = E.场景对象("子", 10, 0)
        父.加子(子)
        p = 子.世界位置
        self.assertAlmostEqual(p.x, 0, places=9)
        self.assertAlmostEqual(p.y, 10, places=9)

    def test_找和遍历顺序确定(self):
        s = E.场景()
        a = s.造("a")
        b = s.造("b")
        # ⚠ 场景对象要用 `加子`；`加` 是"加组件"的接口。
        #   我第一版写成 `a.加(场景对象...)` —— 于是它被当成组件，
        #   一挂上就去找 `挂上时()`，报 AttributeError。
        a.加子(E.场景对象("a1"))
        a.加子(E.场景对象("a2"))
        self.assertIs(s.找("a1"), a.子对象[0])
        # ⚠ 顺序必须是**挂载顺序**（不是字典序）—— 否则同代码不同机器表现不同
        名字 = [o.名字 for o in s.遍历()]
        self.assertEqual(名字, ["a", "a1", "a2", "b"])

    def test_重复加子不会出现两次(self):
        s = E.场景()
        a = s.造("a")
        b = s.造("b")
        c = E.场景对象("c")
        a.加子(c)
        b.加子(c)                       # 换爹
        self.assertNotIn(c, a.子对象, "换爹后旧爹还留着它")
        self.assertIn(c, b.子对象)


class 组件测试(unittest.TestCase):
    def test_生命周期四个钩子按序(self):
        日志 = []

        class 探针(E.组件):
            def 挂上时(self): 日志.append("挂上")
            def 每帧(self, dt): 日志.append("帧")
            def 绘制(self, 笔): 日志.append("绘制")
            def 卸载时(self): 日志.append("卸载")

        s = E.场景()
        o = s.造("x")
        c = 探针()
        o.加(c)
        o.更新(1 / 60)
        o.绘制(None)
        o.移除(c)
        self.assertEqual(日志, ["挂上", "帧", "绘制", "卸载"])

    def test_取_返回第一个_按挂载顺序(self):
        s = E.场景()
        o = s.造("x")
        a, b = C.圆点(颜色="红"), C.圆点(颜色="蓝")
        o.加(a, b)
        self.assertIs(o.取(C.圆点), a, "`取` 没按挂载顺序返回第一个")

    def test_遍历中移除组件不会崩(self):
        """⚠ 组件在 `每帧` 里把自己移除是真场景（一次性效果）。"""
        s = E.场景()
        o = s.造("x")

        class 自删(E.组件):
            def 每帧(self, dt):
                self.宿主.移除(self)

        o.加(自删())
        o.更新(1 / 60)          # 不该抛
        self.assertEqual(len(o.组件们), 0)

    def test_组件拿得到宿主与场景(self):
        s = E.场景()
        o = s.造("x")
        c = C.圆点()
        o.加(c)
        self.assertIs(c.宿主, o)
        self.assertIs(o._场景, s)


class 绘制测试(unittest.TestCase):
    def _游戏(self):
        g = E.游戏(场景=E.场景("t"), 后端=E.录制后端(), 尺寸=100, 宽=400, 高=400)
        g.场景.设服务("输入", g.输入)
        return g

    def test_圆点画在宿主位置(self):
        # ⚠ 默认相机放在视口正中 ⇒ "世界坐标 = 屏幕坐标"。
        #   这条判据**同时**守着那个默认值：一旦默认相机挪了，这里就红。
        g = self._游戏()
        o = g.场景.造("p", 30, 40)
        o.加(C.圆点(半径=5, 颜色="红"))
        g.一步()
        圆 = g.后端.有圆()
        self.assertEqual(len(圆), 1)
        self.assertEqual(圆[0][1:4], (30.0, 40.0, 5.0))

    def test_方块居中选项(self):
        g = self._游戏()
        o = g.场景.造("p", 30, 40)
        o.加(C.方块(宽=10, 高=6, 居中=True))
        g.一步()
        矩 = g.后端.有矩形()
        self.assertEqual(len(矩), 1)
        self.assertEqual((矩[0][1], 矩[0][2]), (25.0, 37.0))

    def test_标签内容会跟着改(self):
        g = self._游戏()
        o = g.场景.造("p", 10, 10)
        t = C.标签(内容="你好")
        o.加(t)
        g.一步()
        self.assertIn("你好", g.后端.有文字()[0][3])
        t.内容 = "再见"
        g.一步()
        self.assertIn("再见", g.后端.有文字()[0][3])

    def test_相机把画面移过去(self):
        """⚠ 这条守的是我踩过的坑：相机偏移"赋值了但没应用"。"""
        g = self._游戏()
        o = g.场景.造("p", 50, 50)
        o.加(C.圆点(半径=2))
        g.相机.x, g.相机.y = 60.0, 60.0       # 相机中心在 (60,60)
        g.一步()
        圆 = g.后端.有圆()[0]
        # 视口 100，相机在 (60,60) ⇒ 世界 (50,50) 应画到屏幕 (40,40)
        self.assertAlmostEqual(圆[1], 40.0, places=6)
        self.assertAlmostEqual(圆[2], 40.0, places=6)

    def test_子对象的绘制位置跟着父(self):
        g = self._游戏()
        父 = g.场景.造("父", 20, 20)
        子 = E.场景对象("子", 5, 5)
        父.加子(子)
        子.加(C.圆点(半径=1))
        g.一步()
        圆 = g.后端.有圆()[0]
        self.assertAlmostEqual(圆[1], 25.0, places=6)


class 输入测试(unittest.TestCase):
    def test_按键与刚按下(self):
        i = E.输入()
        i._按("d")
        self.assertTrue(i.按键("d"))
        self.assertTrue(i.刚按下("d"))
        i.帧末清理()
        self.assertTrue(i.按键("d"), "清理不该影响'按着'")
        self.assertFalse(i.刚按下("d"), "清理后'刚按下'应当为假（否则按住会连发）")

    def test_斜走不加速(self):
        i = E.输入()
        i._按("d")
        i._按("w")
        d = i.方向()
        self.assertAlmostEqual(d.长度(), 1.0, places=12,
                               msg="斜着走的速度不是 1 —— 会比直着走快 41%")

    def test_没按方向键时方向为零(self):
        i = E.输入()
        self.assertEqual(i.方向(), E.向量(0, 0))

    def test_方向键与WASD等价(self):
        a, b = E.输入(), E.输入()
        a._按("a")
        b._按("left")
        self.assertEqual(a.方向(), b.方向())

    def test_大小写不敏感(self):
        i = E.输入()
        i._按("A")
        self.assertTrue(i.按键("a"))


class 相机测试(unittest.TestCase):
    def test_不平滑时立刻到位(self):
        相 = E.相机2D(视口=(100, 100))
        目标 = E.场景对象("t", 200, 130)
        相.跟随 = 目标
        for _ in range(5):
            相.每帧(1 / 60)
        self.assertEqual((相.x, 相.y), (200.0, 130.0))

    def test_平滑会逐渐逼近(self):
        相 = E.相机2D(视口=(100, 100))
        目标 = E.场景对象("t", 100, 0)
        相.跟随 = 目标
        相.平滑 = 0.1
        相.每帧(1 / 60)
        self.assertGreater(相.x, 0.0)
        self.assertLess(相.x, 100.0, "平滑时不该一步到位")
        for _ in range(120):
            相.每帧(1 / 60)
        self.assertAlmostEqual(相.x, 100.0, delta=0.5)

    def test_平滑与帧率无关(self):
        """⚠ 最要紧的一条：用 `*0.9` 那种写法会在 120fps 下快一倍。

        同一段**总时长**、不同步长 ⇒ 结果应当几乎一样。
        """
        def 跑(步长, 次数):
            相 = E.相机2D(视口=(100, 100))
            相.跟随 = E.场景对象("t", 100, 0)
            相.平滑 = 0.2
            for _ in range(次数):
                相.每帧(步长)
            return 相.x
        a = 跑(1 / 60, 120)      # 2 秒
        b = 跑(1 / 120, 240)     # 同样 2 秒
        self.assertAlmostEqual(a, b, delta=1.0,
                               msg=f"不同帧率下结果不同（{a:.3f} vs {b:.3f}）—— 平滑没做成帧率无关")

    def test_边界夹取(self):
        # ⚠ 必须设 `跟随` —— 不跟随时相机**不动**（那是刻意的省事路径：
        #   没有目标就没什么可平滑的）。我第一版忘了设，于是 999 原样返回。
        相 = E.相机2D(视口=(100, 100))
        相.边界 = (0, 0, 300, 300)
        相.跟随 = E.场景对象("t", 999, 999)
        相.平滑 = 0
        相.每帧(1 / 60)
        self.assertEqual((相.x, 相.y), (250.0, 250.0))
        相.跟随.变换.位置 = E.向量(-999, -999)
        相.每帧(1 / 60)
        self.assertEqual((相.x, 相.y), (50.0, 50.0))

    def test_世界比视口小时居中(self):
        # ⚠ 夹取在这种情形下会算反（下界 > 上界）—— 必须特判
        相 = E.相机2D(视口=(100, 100))
        相.边界 = (0, 0, 40, 40)
        相.跟随 = E.场景对象("t", 999, 999)
        相.平滑 = 0
        相.每帧(1 / 60)
        self.assertEqual(相.x, 20.0, "世界比视口小时应当居中，而不是被夹到负数区间")

    def test_世界屏幕坐标互转(self):
        相 = E.相机2D(视口=(100, 100))
        相.x, 相.y = 50, 50
        屏 = 相.世界转屏幕(60, 40)
        self.assertEqual(屏, (60.0, 40.0))
        回 = 相.屏幕转世界(*屏)
        self.assertAlmostEqual(回[0], 60.0, places=9)
        self.assertAlmostEqual(回[1], 40.0, places=9)


class 运动组件测试(unittest.TestCase):
    def test_匀速移动累积正确(self):
        g = E.游戏(场景=E.场景("t"), 后端=E.录制后端(), 尺寸=100, 宽=200, 高=200)
        o = g.场景.造("b", 0, 0)
        o.加(C.匀速移动(vx=10, vy=0))
        for _ in range(10):
            g.场景.更新(0.1)
        self.assertAlmostEqual(o.变换.位置.x, 10.0, places=9)

    def test_边界反弹而且位置被夹住(self):
        g = E.游戏(场景=E.场景("t"), 后端=E.录制后端(), 尺寸=100, 宽=200, 高=200)
        o = g.场景.造("b", 90, 0)
        m = C.匀速移动(vx=10, vy=0, 边界=(0, 100))
        o.加(m)
        g.场景.更新(1.0)               # 一步冲过边界
        self.assertEqual(o.变换.位置.x, 100.0, "冲过边界后位置没被夹住")
        self.assertLess(m.vx, 0, "碰到右边界后没有反向")
        # ⚠ 我第一版断言"再走一步 x 变小" —— 而位置**被夹在边界上**，
        #   反向之后下一步仍然落在 100 附近，`< 100` 不成立。
        #   正确的判据是"**不再往右跑出去**"（这才是边界的作用）。
        g.场景.更新(1.0)
        self.assertLessEqual(o.变换.位置.x, 100.0, "反向之后又跑出边界了")
        g.场景.更新(1.0)
        self.assertLess(o.变换.位置.x, 100.0, "反向之后应当往回走")

    def test_重力落地不抖(self):
        g = E.游戏(场景=E.场景("t"), 后端=E.录制后端(), 尺寸=100, 宽=200, 高=200)
        o = g.场景.造("b", 50, 50)
        gr = C.重力(加速度=-60, 地面=0)
        o.加(gr)
        for _ in range(600):
            g.场景.更新(1 / 60)
        self.assertAlmostEqual(o.变换.位置.y, 0.0, places=9)
        self.assertTrue(gr.在地面)
        self.assertEqual(gr.vy, 0.0, "落地后速度没清零 —— 会抖")

    def test_空中不能连跳(self):
        gr = C.重力(地面=0)
        o = E.场景().造("b", 0, 5)
        o.加(gr)
        gr.vy = 10                      # 假装正在上升
        gr.在地面 = False
        self.assertFalse(gr.跳(), "空中竟然能跳 —— 会连跳")

    def test_按键驱动用场景里的输入服务(self):
        g = E.游戏(场景=E.场景("t"), 后端=E.录制后端(), 尺寸=100, 宽=200, 高=200)
        g.场景.设服务("输入", g.输入)
        o = g.场景.造("p", 50, 50)
        o.加(C.按键驱动(速度=60))
        g.输入._按("d")
        g.场景.更新(0.5)
        self.assertAlmostEqual(o.变换.位置.x, 80.0, places=9)


class 玩法零件测试(unittest.TestCase):
    def test_计时器按间隔触发(self):
        次 = []
        t = C.计时器(间隔=0.1, 回调=lambda 宿, n: 次.append(n))
        o = E.场景().造("x")
        o.加(t)
        for _ in range(25):
            o.更新(0.1)              # 2.5 秒 / 0.1 ⇒ 25 次
        self.assertEqual(len(次), 25)
        self.assertEqual(次[0], 1)

    def test_分数能加(self):
        f = C.分数(初值=0)
        o = E.场景().造("x")
        o.加(f)
        f.加(3)
        f.加(4)
        self.assertEqual(f.值, 7)

    def test_生命周期到点后标记死亡并被场景清掉(self):
        """⚠ 组件不该自己从容器里删 —— 那是遍历中改容器。"""
        g = E.游戏(场景=E.场景("t"), 后端=E.录制后端(), 尺寸=100, 宽=200, 高=200)
        o = g.场景.造("b", 0, 0)
        o.加(C.生命周期(寿命=0.5))
        g.场景.更新(0.3)
        self.assertTrue(o.活着, "还没到寿命就被清掉了")
        self.assertIn(o, g.场景.根)
        g.场景.更新(0.3)
        self.assertFalse(o.活着)
        self.assertNotIn(o, g.场景.根, "到寿命后没被场景清掉")

    def test_尾迹在遍历中造对象不会崩(self):
        """⚠ 这条守的是"遍历中改容器"—— `尾迹.每帧` 正好在遍历里。"""
        g = E.游戏(场景=E.场景("t"), 后端=E.录制后端(), 尺寸=100, 宽=200, 高=200)
        o = g.场景.造("p", 10, 10)
        o.加(C.尾迹(间隔=0.01, 寿命=0.05))
        for _ in range(30):
            g.场景.更新(0.01)        # 不该抛
        self.assertGreater(len(g.场景.根), 1, "尾迹没造出任何对象")

    def test_圆形碰撞(self):
        s = E.场景()
        a = s.造("a", 0, 0)
        b = s.造("b", 3, 4)              # 距离 5
        ca, cb = C.圆形碰撞(半径=3), C.圆形碰撞(半径=2)
        a.加(ca)
        b.加(cb)
        self.assertTrue(ca.碰到(cb), "距离 5 = 半径和 5，边界相接应当算碰到")
        b.变换.位置 = E.向量(4, 4)       # 距离 ~5.66
        self.assertFalse(ca.碰到(cb))

    def test_闪烁会换掉圆点的颜色(self):
        s = E.场景()
        o = s.造("x")
        d = C.圆点(颜色="红")
        o.加(d, C.闪烁(周期=0.1, 颜色a="红", 颜色b="白"))
        o.更新(0.15)
        self.assertEqual(d.颜色, "白")
        o.更新(0.15)
        self.assertEqual(d.颜色, "红")


class 循环测试(unittest.TestCase):
    def test_固定步长_逻辑不受真实耗时影响(self):
        """⚠ 这是整个引擎最要紧的一条设计约束。

        同一段**逻辑时长**，无论跑多少帧、真实耗时多少，
        场景状态必须一致 —— 否则"我的游戏在别人电脑上不一样"。
        """
        def 跑到(目标时长, 绘制次数):
            g = E.游戏(场景=E.场景("t"), 后端=E.录制后端(), 尺寸=100, 宽=200, 高=200)
            o = g.场景.造("b", 0, 0)
            o.加(C.匀速移动(vx=10, vy=0))
            n = int(round(目标时长 / g.步长))
            for _ in range(n):
                g.场景.更新(g.步长)
            return o.变换.位置.x
        self.assertAlmostEqual(跑到(1.0, 0), 10.0, places=9)

    def test_一步会更新并绘制并清输入边沿(self):
        g = E.游戏(场景=E.场景("t"), 后端=E.录制后端(), 尺寸=100, 宽=200, 高=200)
        g.场景.设服务("输入", g.输入)
        g.输入._按("d")
        self.assertTrue(g.输入.刚按下("d"))
        g.一步()
        self.assertFalse(g.输入.刚按下("d"), "一步之后'刚按下'应当被清掉")
        self.assertTrue(g.输入.按键("d"), "'按着'不该被清掉")
        self.assertEqual(g.帧数, 1)
        self.assertEqual(g.后端.帧数, 1)

    def test_跑够帧数会自己停(self):
        g = E.游戏(场景=E.场景("t"), 后端=E.录制后端(), 尺寸=100, 宽=200, 高=200)
        g.跑(帧数=5)
        self.assertGreaterEqual(g.帧数, 5)

    def test_无头后端还开着为真(self):
        # `录制后端` 永远"还开着"，所以 `跑(帧数=N)` 是唯一退出方式
        self.assertTrue(E.录制后端().还开着)


class 后端测试(unittest.TestCase):
    def test_录制后端记录了指令(self):
        b = E.录制后端()
        b.清屏("白")
        b.画圆(1, 2, 3, "红")
        b.画矩形(1, 2, 3, 4, "蓝")
        b.画线(0, 0, 1, 1, "黑", 1)
        b.画文字(0, 0, "x", "黑", 12)
        self.assertEqual(len(b.指令), 5, "清屏 1 + 圆/矩/线/文字 4 = 5")
        b.呈现()
        self.assertEqual(b.帧数, 1)
        # ⚠ `呈现` 会**清空当前帧指令**（与真实后端"每帧重画"语义一致），
        #   但把快照留在 `帧历史` 里。所以这里查历史，不是查 `指令`。
        self.assertEqual(len(b.指令), 0, "呈现后当前帧应当清空")
        self.assertEqual(len(b.帧历史), 1)
        self.assertEqual(len(b.帧历史[0]), 5)

    def test_颜色名中文与英文都对(self):
        self.assertTrue(E.颜色("红"))
        self.assertTrue(E.颜色("#ff0000"))
        # ⚠ 未知名字**不该报错** —— 交给底下去提示（与 Stage 同一约定）
        self.assertEqual(E.颜色("不存在的颜色"), "不存在的颜色")


class 样例游戏测试(unittest.TestCase):
    """贪吃蛇是"引擎能用"的活证据 —— 它的正确性也该有判据。

    ⚠ 特别是**颜色**这一条：我踩过两次同一个坑（相对层级 + 吞异常），
      表现是 `TkError: unknown color name "黑"` —— 报错现场离真因隔了四层。
    """

    def test_颜色名会转成十六进制(self):
        # ⚠ 这条守着"颜色没生效但一路静默传下去"那类错
        for 名 in ("黑", "白", "红", "绿", "青", "灰"):
            v = E.颜色(名)
            self.assertRegex(v, r"^#[0-9a-fA-F]{6}$",
                             f"颜色({名!r}) 返回 {v!r} —— 没被转换（中文名传到 tkinter 会炸）")

    def test_十六进制原样通过(self):
        self.assertEqual(E.颜色("#ff0000"), "#ff0000")

    def test_未知颜色名原样返回不报错(self):
        # 与 stage.color_of 同一约定：交给底下去提示，这里不拦
        self.assertEqual(E.颜色("这是不存在的颜色"), "这是不存在的颜色")

    def test_贪吃蛇能无头跑起来(self):
        from hybridengine_light.pyengine import 录制后端
        from hybridengine_light.pyengine.games import snake
        后 = 录制后端()
        g = snake.造游戏(后端=后)
        n = g.跑(帧数=60)
        self.assertGreaterEqual(n, 60)
        self.assertGreater(len(后.有圆()), 3, "蛇和食物都该是圆")
        self.assertGreater(len(后.有线()), 10, "场地网格该有线")
        self.assertGreater(len(后.有文字()), 0, "分数该显示出来")

    def test_贪吃蛇会自己往前走(self):
        """⚠ 守"游戏真的在动"—— 一个不动的游戏也能画出东西来。"""
        from hybridengine_light.pyengine import 录制后端
        from hybridengine_light.pyengine.games import snake
        后 = 录制后端()
        g = snake.造游戏(后端=后, 速度=7.0)
        头 = g.场景.找("蛇头")
        起 = 头.变换.位置.x
        g.跑(帧数=60)
        self.assertNotEqual(头.变换.位置.x, 起, "60 帧之后蛇头没动")

    def test_蛇身跟在蛇头后面一格(self):
        from hybridengine_light.pyengine import 录制后端
        from hybridengine_light.pyengine.games import snake
        g = snake.造游戏(后端=录制后端(), 速度=7.0)
        g.跑(帧数=90)
        头 = g.场景.找("蛇头").变换.位置
        一 = g.场景.找("蛇身1").变换.位置
        距 = E.向量(头.x - 一.x, 头.y - 一.y).长度()
        self.assertAlmostEqual(距, snake.格, delta=0.6,
                               msg=f"蛇身1 离蛇头 {距:.2f}，应当约一格 {snake.格}")

    def test_引擎命令能列出样例(self):
        from hybridengine_light import cmd_engine
        self.assertIn("贪吃蛇", cmd_engine.样例)
        self.assertEqual(cmd_engine.cmd_引擎(__import__("pathlib").Path("."), []), 0)
        self.assertEqual(cmd_engine.cmd_引擎(__import__("pathlib").Path("."), ["说明"]), 0)
        self.assertEqual(cmd_engine.cmd_引擎(__import__("pathlib").Path("."), ["不存在"]), 1)


if __name__ == "__main__":
    unittest.main()
