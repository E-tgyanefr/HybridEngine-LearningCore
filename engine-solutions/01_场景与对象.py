"""第 01 课参考答案。"""

from hybridengine_light import pyengine as E
from hybridengine_light.pyengine import components as C


def 搭一个游戏(后端):
    游戏 = E.游戏(后端=后端, 尺寸=100, 宽=400, 高=400)
    玩家 = 游戏.场景.造("玩家", 50, 50)
    玩家.加(C.圆点(半径=5, 颜色="红"))
    return 游戏
