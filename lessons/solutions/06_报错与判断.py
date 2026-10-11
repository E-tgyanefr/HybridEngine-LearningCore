"""第 06 课参考答案 —— 会报错，也要会处理报错。

⚠ 三处都用 try/except 并**写明具体错误类型**。
   不要写空的 except: —— 那会把你自己的 bug 也吞掉。
"""


def 转成整数(文字):
    try:
        return int(文字)
    except ValueError:          # 内容不对（"abc"）
        return None
    except TypeError:           # 类型不对（None）—— 顺手也接住
        return None


def 安全相除(a, b):
    try:
        return a / b
    except ZeroDivisionError:
        return None


def 取第几个(列表, 第几个):
    try:
        return 列表[第几个]
    except IndexError:
        return None
    except TypeError:           # 传了不是列表的东西
        return None
