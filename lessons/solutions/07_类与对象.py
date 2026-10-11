"""第 07 课参考答案 —— 类。"""


class 计数器:
    def __init__(self, 初值=0):
        # ⚠ 一定写在 __init__ 里、用 self.值 —— 那才是「每个对象自己的」
        self.值 = 初值

    def 加(self, n):
        self.值 += n

    def 重置(self):
        self.值 = 0


class 存钱罐:
    def __init__(self, 初值=0):
        self.余额 = 初值

    def 存(self, n):
        self.余额 += n

    def 取(self, n):
        # ⚠⚠ 顺序是关键：**先检查，通过才扣**。
        #   反过来的话，一次失败的取款已经把余额改坏了。
        if n > self.余额:
            return False
        self.余额 -= n
        return True
