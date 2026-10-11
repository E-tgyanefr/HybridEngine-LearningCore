"""第 14 课参考答案 —— try / except。"""


def 转成整数(文字):
    try:
        return int(文字)
    # ⚠ 写**具体**的错误类型，不要写空的 `except:`
    #   空的 except 会把你自己写错的代码也吞掉 —— 那时程序不报错但结果是错的。
    except ValueError:          # 内容不对（"abc"）
        return None
    except TypeError:           # 类型不对（None）—— 顺手也接住
        return None


def 安全相除(a, b):
    try:
        return a / b
    except ZeroDivisionError:
        # ⚠ 返回 None，不是 0 —— 0 是合法结果，分不清是"算出来是 0"还是"没算成"
        return None
