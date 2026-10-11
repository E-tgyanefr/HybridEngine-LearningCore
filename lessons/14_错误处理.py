# 第 14 课：try / except —— 会出错，也要会处理出错
# ─────────────────────────────────────────────────────
#  这一课你会学到
# ─────────────────────────────────────────────────────
#    try / except：会出错，也要会处理出错
#
#  需要先会
#    第 09 课：函数


# ─────────────────────────────────────────────────────
#  先介绍：有些"不对"是正常的
# ─────────────────────────────────────────────────────
#
# 前面几课你见过的报错，都是"你写错了"。
# 但有些"不对"是**正常情况**，程序不该崩：
#
#     用户输入 "abc"，你要转成整数     —— 转不了很正常
#     用户想除以 0                    —— 不该崩
#     想取第 100 个元素，而列表只有 3 个 —— 不该崩
#
# 处理办法是 `try / except`：
#
#     try:
#         可能出错的那句
#     except 具体的错误类型:
#         出错了怎么办
#
#   ⇒ 读作：「**试着做这个；要是出了这种错，就改做那个。**」
#
# ─────────────────────────────────────────────────────
#  常见错误类型（要认得）
# ─────────────────────────────────────────────────────
#
#     ValueError         值不对          int("abc")
#     TypeError          类型不对        int(None)、"18" + 1
#     ZeroDivisionError  除以 0          10 / 0
#     IndexError         下标越界        [1,2][9]
#     KeyError           字典没这个键    {"a":1}["b"]
#     NameError          名字没定义      引号忘了 / 拼错了
#
#   ⚠ IndexError 和 KeyError 是**不同**的类，别抓错。
#
# ─────────────────────────────────────────────────────
#  ⚠⚠ 这一课唯一要记住的规矩：except 要写具体类型
# ─────────────────────────────────────────────────────
#
#     try:
#         ...
#     except:                  ❌ 什么都接 —— **千万别**
#     except Exception:        ⚠ 也很粗
#     except ValueError:       ✅ 明确
#
#   为什么空的 `except:` 危险：它会把**你自己写错的代码**也一起吞掉。
#
#     try:
#         成绩 = [90, 75]
#         print(成绩[5])            ← 你写错了（越界）
#     except:
#         print("出错了")            ← 被你接住了，程序不报错
#
#   于是你以为"越界这件事已经处理好了"，其实是**你写错了下标**。
#   程序不报错但结果不对 —— 那是最难查的一类问题。
#
#   ⇒ **只接你预料到的那种错误。**
#
# ─────────────────────────────────────────────────────
#  接住之后要给一个合理的结果
# ─────────────────────────────────────────────────────
#
#     except ValueError:
#         return None          ← "我没做成"的通用说法
#
#   ⚠ 为什么不用 0？因为 0 是个**合法结果**，
#     调用者分不清"转成了 0"还是"没转成"。
#     `None` 是"什么都没有"，不会有这个歧义。
#
# ─────────────────────────────────────────────────────
#  这一课只讲这一件事
# ─────────────────────────────────────────────────────
#   这一课只讲怎么"接住"错误。怎么**抛出**错误（raise）以后再说。
#
# ─────────────────────────────────────────────────────
#  轮到你了 —— 在答题文件里写这几件事
# ─────────────────────────────────────────────────────
#   1. 写函数 转成整数(文字)：能转就返回整数，**转不了返回 None**（不能崩）
#   2. 写函数 安全相除(a, b)：返回 a / b，**b 是 0 就返回 None**
#
# ─────────────────────────────────────────────────────
#  怎么自己测
# ─────────────────────────────────────────────────────
#     python -m hybridengine_light 检查 14


def CHECK_转成整数(答案):
    if not hasattr(答案, "转成整数"):
        return False, "还没有定义「转成整数」函数。"
    if not callable(答案.转成整数):
        return False, "「转成整数」不是函数。"
    for 文字, 期望 in (("42", 42), ("-7", -7), ("0", 0), (" 15 ", 15)):
        try:
            得 = 答案.转成整数(文字)
        except Exception as e:                                  # noqa: BLE001
            return False, (f"转成整数({文字!r}) 抛了异常：{type(e).__name__}: {e}\n"
                           f"      ⚠ 能转的情况不该出错。")
        if 得 != 期望:
            return False, (f"转成整数({文字!r}) 应当是 {期望}，实际是 {得!r}。\n"
                           f"      ⚠ 最省事的写法就是 `return int(文字)` 并接住 ValueError。")
    for 坏 in ("abc", "", "3.5"):
        try:
            得 = 答案.转成整数(坏)
        except Exception as e:                                  # noqa: BLE001
            return False, (f"转成整数({坏!r}) **抛异常了**（{type(e).__name__}）——\n"
                           f"      这一课要的正是「转不了不要崩，返回 None」。\n"
                           f"      写法：\n"
                           f"          try:\n"
                           f"              return int(文字)\n"
                           f"          except ValueError:\n"
                           f"              return None")
        if 得 is not None:
            return False, (f"转成整数({坏!r}) 转不了，应当返回 **None**，实际是 {得!r}。\n"
                           f"      ⚠ 返回 0 不行 —— 0 是合法结果，调用者分不清是"
                           f"「转成了 0」还是「没转成」。")
    return True, "能转的对 / 转不了的返回 None"


def CHECK_安全相除(答案):
    if not hasattr(答案, "安全相除"):
        return False, "还没有定义「安全相除」函数。"
    if not callable(答案.安全相除):
        return False, "「安全相除」不是函数。"
    if abs(答案.安全相除(10, 4) - 2.5) > 1e-9:
        return False, (f"安全相除(10, 4) 应当是 2.5，实际是 {答案.安全相除(10, 4)!r}。\n"
                       f"      ⚠ 一个斜杠 `/` 才有小数。")
    try:
        得 = 答案.安全相除(1, 0)
    except ZeroDivisionError:
        return False, ("除以 0 时抛了 ZeroDivisionError —— 这一课要接住它。\n"
                       "      写法：\n"
                       "          try:\n"
                       "              return a / b\n"
                       "          except ZeroDivisionError:\n"
                       "              return None\n"
                       "      （先判断 `if b == 0: return None` 也一样对。）")
    except Exception as e:                                      # noqa: BLE001
        return False, f"除以 0 时出错了：{type(e).__name__}: {e}"
    if 得 is not None:
        return False, f"除以 0 应当返回 None，实际是 {得!r}。"
    if 答案.安全相除(0, 5) != 0:
        return False, "安全相除(0, 5) 应当是 0.0（0 除以非零是有意义的）。"
    return True, "正常相除 / 除以 0 / 0 做被除数 都对"


def CHECK_except写了具体类型(答案):
    """⚠⚠ 这一课的核心规矩：不能写空的 `except:`。"""
    import ast
    import io
    import re
    if not hasattr(答案, "__file__"):
        return False, "找不到你的答题文件。"
    源码 = io.open(答案.__file__, encoding="utf-8").read()
    # 找 `except` 后面**什么都没写**就冒号的。
    #
    # ⚠⚠ 判据本身很容易写错 —— 我第一版写的是 `re.findall(r"except\s*:", 源码)`，
    #   而 `except TypeError:` 里 `except ` 后面紧跟的是 `T`，
    #   但**换行空白**会让 `\s*` 一路吃过去…… 实测把正确的
    #   `except TypeError:` 也判成了"空的 except"，直接冤枉正确答案。
    #   ⇒ 必须**真的解析**：用 ast 看每个 handler 的 type 是不是 None。
    空的 = []
    try:
        tree = ast.parse(源码)
    except SyntaxError:
        tree = None
    if tree is not None:
        for node in ast.walk(tree):
            if isinstance(node, ast.ExceptHandler) and node.type is None:
                空的.append(node.lineno)
    if 空的:
        return False, (f"发现了 {len(空的)} 处**空的 `except:`** —— 这样是危险的。\n"
                       f"      ⚠⚠ 空的 except 会把**你自己写错的代码**也一起吞掉：\n"
                       f"          try:\n"
                       f"              成绩 = [90, 75]\n"
                       f"              print(成绩[5])        ← 你写错了（越界）\n"
                       f"          except:\n"
                       f"              print('出错了')        ← 被接住了，程序不报错\n"
                       f"        于是你以为处理好了，其实是下标写错了 ——\n"
                       f"        程序不报错但结果不对，那是最难查的一类问题。\n"
                       f"      ⇒ 只接你预料到的那种：`except ValueError:` / `except ZeroDivisionError:`")
    if "except" not in 源码:
        return False, ("文件里没有 `try` / `except` —— 这一课就是练它。\n"
                       "      写法：\n"
                       "          try:\n"
                       "              return int(文字)\n"
                       "          except ValueError:\n"
                       "              return None")
    return True, "except 都写了具体错误类型"
