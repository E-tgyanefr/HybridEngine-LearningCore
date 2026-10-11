# 第 06 课：会报错，也要会处理报错


# ─────────────────────────────────────────────────────
#  先介绍：报错不是「你写错了」，是 Python 在告诉你哪里不对
# ─────────────────────────────────────────────────────
#
# 前五课你已经见过不少报错。这一课要学的是**另一面**：
# 有些「不对」是**正常的**，程序不该崩。
#
#     用户输入了 "abc"，你要转成整数 —— 转不了很正常，不该崩
#     用户想除以 0 —— 不该崩
#     想取第 100 个元素，而列表只有 3 个 —— 不该崩
#
# ⇒ 用 try / except 接住：
#
#     try:
#         可能出错的那句
#     except 具体的错误类型:
#         出错了怎么办
#
# ─────────────────────────────────────────────────────
#  ⚠⚠ 两条最重要的规矩
# ─────────────────────────────────────────────────────
#
#  ① **except 要写具体的错误类型**，不要空着。
#
#         except:               ← ❌ 什么都接
#         except Exception:     ← ⚠ 也很粗
#         except ValueError:    ← ✅ 明确
#
#     空的 except: 会把**你自己写错的代码**也一起吞掉 ——
#     那时程序不报错，但结果是错的，你会查很久。
#
#  ② **接住之后要给出一个合理的结果**，不是打个字就算了。
#
#         except ValueError:
#             return None       ← ✅ 调用者知道「没转成」
#
# 这一课三个函数要求的都是 return None ——
# 那是「我没做成」的通用说法，比抛异常温柔，也比返回 0 诚实
# （因为 0 是个合法结果，分不清是「转成了 0」还是「没转成」）。
#
# ─────────────────────────────────────────────────────
#  三种错误分别是什么
# ─────────────────────────────────────────────────────
#
#     int("abc")        ValueError          值不对（类型对，内容不对）
#     int(None)         TypeError           类型不对
#     10 / 0            ZeroDivisionError
#     [1,2][9]          IndexError          下标越界
#     {"a":1}["b"]      KeyError            字典里没这个键（第 04 课见过）
#
#   ⚠ IndexError 和 KeyError 是**不同**的类，别抓错。
#     拿不准时可以用 (IndexError, KeyError) 一次抓两个。
#
# ─────────────────────────────────────────────────────
#
# ─────────────────────────────────────────────────────
#  轮到你了 —— 在下面的答题文件里写这几件事
# ─────────────────────────────────────────────────────
#
#   1. 写函数 转成整数(文字)，把文字转成整数返回；
#      **转不了就返回 None**（不要让它抛异常）
#   2. 写函数 安全相除(a, b)，返回 a / b；
#      **b 是 0 就返回 None**
#   3. 写函数 取第几个(列表, 第几个)，返回那个元素；
#      **下标越界就返回 None**
#
#  怎么自己测
# ─────────────────────────────────────────────────────
#     python -m hybridengine_light 检查 06


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
                           f"      ⚠ 最省事的写法就是 return int(文字) 并接住 ValueError。")
    for 坏 in ("abc", "", "3.5", "１２３abc"):
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
                       f"      ⚠ 一个斜杠 / 才有小数；两个斜杠 // 会丢小数。")
    try:
        得 = 答案.安全相除(1, 0)
    except ZeroDivisionError:
        return False, ("除以 0 时抛了 ZeroDivisionError —— 这一课要接住它。\n"
                       "      写法：\n"
                       "          try:\n"
                       "              return a / b\n"
                       "          except ZeroDivisionError:\n"
                       "              return None\n"
                       "      （也可以先判断 if b == 0: return None，两种都对。）")
    except Exception as e:                                      # noqa: BLE001
        return False, f"除以 0 时出错了：{type(e).__name__}: {e}"
    if 得 is not None:
        return False, f"除以 0 应当返回 None，实际是 {得!r}。"
    if 答案.安全相除(0, 5) != 0:
        return False, "安全相除(0, 5) 应当是 0.0（0 除以非零是有意义的）。"
    return True, "正常相除 / 除以 0 / 0 做被除数 都对"


def CHECK_取第几个(答案):
    if not hasattr(答案, "取第几个"):
        return False, "还没有定义「取第几个」函数。"
    if not callable(答案.取第几个):
        return False, "「取第几个」不是函数。"
    if 答案.取第几个([10, 20, 30], 1) != 20:
        return False, (f"取第几个([10,20,30], 1) 应当是 20，实际是 "
                       f"{答案.取第几个([10, 20, 30], 1)!r}。\n"
                       f"      ⚠ Python 下标**从 0 开始**：第 0 个是 10，第 1 个是 20。")
    try:
        得 = 答案.取第几个([1, 2], 99)
    except IndexError:
        return False, ("下标越界时抛了 IndexError —— 这一课要接住它。\n"
                       "      写法：\n"
                       "          try:\n"
                       "              return 列表[第几个]\n"
                       "          except IndexError:\n"
                       "              return None")
    except Exception as e:                                      # noqa: BLE001
        return False, f"越界时出错了：{type(e).__name__}: {e}"
    if 得 is not None:
        return False, f"越界应当返回 None，实际是 {得!r}。"
    负 = None
    try:
        负 = 答案.取第几个([1, 2, 3], -1)
    except Exception:                                          # noqa: BLE001
        pass
    if 负 == 3:
        return True, "越界返回 None；而且负数下标也对（Python 允许从右边数）"
    return True, "越界返回 None"
