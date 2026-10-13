"""第 05 课参考答案。"""

from hybridengine_light import pyengine as E
from hybridengine_light.pyengine import components as C


class 自动加分(E.组件):
    """每 0.2 秒给分数 +1。

    ⚠ 也可以用"每帧加 dt*5"那种写法 —— 那更精确地与帧率无关。
      这里用累计时间，是为了让学员看到"累计 dt"这个常见套路。
    """

    def __init__(self, 间隔: float = 0.2, 每次: int = 1):
        self.间隔 = 间隔
        self.每次 = 每次
        self.累计 = 0.0

    def 每帧(self, dt):
        self.累计 += dt
        while self.累计 >= self.间隔:
            self.累计 -= self.间隔
            分 = self.宿主.取(C.分数)
            if 分 is not None:
                分.加(self.每次)


def 搭一个游戏(后端):
    游戏 = E.游戏(后端=后端, 尺寸=100, 宽=400, 高=400)
    场景 = 游戏.场景
    场景.设服务("输入", 游戏.输入)

    玩家 = 场景.造("玩家", 50, 50)
    玩家.加(C.圆点(半径=3, 颜色="绿"),
            C.按键驱动(速度=40),
            C.跟随相机())

    敌人 = 场景.造("敌人", 80, 80)
    敌人.加(C.圆点(半径=3, 颜色="红"),
            C.匀速移动(vx=-8, vy=0, 边界=(0, 100)))

    HUD = 场景.造("HUD", 0, 0)
    HUD.加(C.分数(初值=0), 自动加分(间隔=0.2))
    return 游戏
