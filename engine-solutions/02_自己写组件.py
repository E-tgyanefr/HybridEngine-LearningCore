"""第 02 课参考答案。"""

from hybridengine_light import pyengine as E
from hybridengine_light.pyengine import components as C


class 往右跑(E.组件):
    """每帧往右移动 —— 最简单的"自己写的组件"。"""

    def __init__(self, 速度: float = 20.0):
        self.速度 = 速度

    def 每帧(self, dt):
        p = self.宿主.变换.位置
        # ⚠ 是 速度 * dt，不是"每帧加固定值"—— 不然快电脑上跑得更快
        self.宿主.变换.位置 = E.向量(p.x + self.速度 * dt, p.y)


def 搭一个游戏(后端):
    游戏 = E.游戏(后端=后端, 尺寸=100, 宽=400, 高=400)
    圆 = 游戏.场景.造("圆", 50, 50)
    圆.加(C.圆点(半径=5, 颜色="蓝"), 往右跑(速度=20))
    return 游戏
