"""第 16 课参考答案 —— 封装。"""


class 存钱罐:
    def __init__(self, 初值=0):
        # ⚠ 下划线开头 = "别从外面碰它"
        self._余额 = 初值

    @property
    def 余额(self):
        # ⚠ @property 让 `罐.余额` 读得到，但**赋不了值**
        #   ⇒ 想改余额只有 存() / 取() 两个入口，而它们会检查
        return self._余额

    def 存(self, n):
        self._余额 += n

    def 取(self, n):
        # ⚠⚠ 顺序是关键：**先检查，通过才动**。
        #   反过来的话，一次失败的取款已经把余额改坏了。
        # ⚠ 边界是 `>` 不是 `>=` —— 正好取光要能成功。
        if n > self._余额:
            return False
        self._余额 -= n
        return True
