"""第 33 课参考答案 —— 异常进阶。"""


def 转成整数(文字):
    try:
        return int(文字)
    except ValueError:
        # ⚠ 换个带原文的消息再抛出去 —— 调用者才知道是哪个值错了
        raise ValueError('不是数字：' + 文字)


def 安全相除(甲, 乙):
    # ⚠ 自己先判断再 raise，别让 Python 抛出它那句「division by zero」
    if 乙 == 0:
        raise ZeroDivisionError('除数不能是 0')
    return 甲 / 乙


# ⚠ 括号里的 Exception 不能省：能被 raise / except 的必须继承 BaseException
class 查不到(Exception):
    pass


def 查分数(成绩表, 名字):
    if 名字 not in 成绩表:
        raise 查不到(名字)          # 消息就是那个名字
    return 成绩表[名字]


def 挑出数字(文字们):
    结果 = []
    for 文字 in 文字们:
        try:
            结果.append(int(文字))
        except ValueError:
            continue                # 转不成只跳过这一项，不让整个函数崩掉
    return 结果
