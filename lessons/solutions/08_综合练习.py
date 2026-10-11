"""第 08 课参考答案 —— 综合练习。"""


class 学生:
    def __init__(self, 姓名, 成绩表=None):
        self.姓名 = 姓名
        # ⚠⚠ 千万别写 成绩表=[]！
        #   默认的 [] 只在**定义时创建一次** ⇒ 所有不传成绩表的学生
        #   会共用同一个列表（给小明加分，小红也跟着有分）。
        #   正确写法：默认 None，进来再造列表。
        self.成绩表 = [] if 成绩表 is None else 成绩表

    def 加成绩(self, 分数):
        self.成绩表.append(分数)

    def 平均分(self):
        if not self.成绩表:         # ⚠ 空列表要特判，否则除以 0
            return 0
        return sum(self.成绩表) / len(self.成绩表)

    def 最高分(self):
        if not self.成绩表:
            return 0
        最大 = self.成绩表[0]        # ⚠ 起点用第一个元素，不能写 0（负数会错）
        for x in self.成绩表:
            if x > 最大:
                最大 = x
        return 最大

    def 评语(self):
        # ⚠ 顺序：先判「没有成绩」，再判分数档
        if not self.成绩表:
            return "还没有成绩"
        平均 = self.平均分()
        if 平均 >= 90:
            return "优秀"
        if 平均 >= 60:
            return "及格"
        return "要加油"


def 全班报告(学生表):
    """每个学生一行：姓名 平均分=xx.x 评语"""
    行们 = []
    for s in 学生表:
        行们.append(f"{s.姓名} 平均分={s.平均分():.1f} {s.评语()}")
    # ⚠ 用 "\n".join —— 空列表时正好返回空文字（边界自然对）
    return "\n".join(行们)
