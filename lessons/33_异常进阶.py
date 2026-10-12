# 第 33 课：异常进阶 —— 分清是哪种错，以及自己造一种错
# ─────────────────────────────────────────────────────
#  这一课你会学到
# ─────────────────────────────────────────────────────
#    多个 except、finally、raise，以及自定义异常类
#
#  需要先会
#    第 14 课：错误处理（try / except 入门）
#    第 15 课：类与 __init__
#
# ─────────────────────────────────────────────────────
#  怎么读这一课
# ─────────────────────────────────────────────────────
#
#   先从头读一遍讲解，再照着最后的「轮到你了」写。
#   卡住了看答案（先自己写一遍）—— 命令在「怎么自己测」那一节。
#
# ─────────────────────────────────────────────────────
#  先介绍：一个 except 兜不住所有事
# ─────────────────────────────────────────────────────
#
# 第 14 课你学会了用 try/except 兜住错误。可真实程序里的「错」有好几种：
#
#     输入的不是数字（该让用户重输）  除数是 0（该跳过这一条）  文件找不到（该查路径）
#
# 它们的处理方式完全不同 —— 用一个 `except:` 全兜住，等于什么也没处理。
# 这一课讲三件事：**分开接**（多个 except）、**无论如何都要做**（finally）、
# **自己造一种错**（raise + 自定义异常）。
#
# ─────────────────────────────────────────────────────
#  一、多个 except：从上往下试，只进第一个对得上的
# ─────────────────────────────────────────────────────
#
#     try:
#         分数 = int(输入)
#         平均 = 100 / 分数
#     except ValueError:
#         print('这不是一个数字')
#     except ZeroDivisionError:
#         print('不能除以 0')
#
# ⚠ 顺序有讲究：`except Exception` 这种「大网」必须放**最后**，
#   否则后面那几条永远轮不到 —— 什么都先被它接走了（成了死代码）。
#
# ─────────────────────────────────────────────────────
#  二、常见的几种错，认个脸（考试常给一段代码问会报什么）
# ─────────────────────────────────────────────────────
#
#     异常名                什么时候出现
#     ───────────────────  ────────────────────────────
#     ValueError           int('abc')：类型对但内容不对
#     TypeError            'a' + 1：类型对不上
#     ZeroDivisionError    10 / 0
#     IndexError           表[10]：下标越界
#     KeyError             字典[不存在的键]
#     FileNotFoundError    打开一个不存在的文件
#
# ⚠ 报错长这样（真跑过）：ValueError: invalid literal for int() with base 10: 'abc'
#   读法：**异常名：说明**。前面那个词就是你该写进 except 的东西。
#
# ─────────────────────────────────────────────────────
#  三、except … as e：把错误本身接住
# ─────────────────────────────────────────────────────
#
#     try:
#         分数 = int('abc')
#     except ValueError as e:
#         print('出错啦：', e)        ⇒ 出错啦： invalid literal for int() with base 10: 'abc'
#
# ⚠ `e` 只在 except 那几行里活着；要留到后面用，先存起来：消息 = str(e)。
#
# ─────────────────────────────────────────────────────
#  四、finally：无论如何都要做的那一步
# ─────────────────────────────────────────────────────
#
#     try:
#         做点可能出错的事()
#     except ValueError:
#         print('接住了')
#     finally:
#         print('这一步总会执行')      ← 没出错执行、出错了执行、return 了也执行
#
# 典型用途：关文件、存进度、把界面恢复原样。
# ⚠ 但「关文件」第 32 课的 `with` 已经替你做了 —— 能用 with 就别写 finally。
# ─────────────────────────────────────────────────────
#  五、raise：轮到你喊停的时候
# ─────────────────────────────────────────────────────
#
#     def 设年龄(岁):
#         if 岁 < 0:
#             raise ValueError('年龄不能是负数：' + str(岁))
#         return 岁
#
# 调用的人可以照常接住它：
#
#     try:
#         设年龄(-1)
#     except ValueError as e:
#         print('拦住了：', e)         ⇒ 拦住了： 年龄不能是负数：-1
#
# ⚠ 什么时候该 raise：**这个值继续往下算只会得到更离谱的结果**，与其悄悄算出一个
#   假答案，不如当场喊停。（第 16 课的存钱罐选择 return False ——「失败很常见」的场景。）
#
#   ⚠ raise 后面那个消息要**说清是什么值**：`raise ValueError('错了')` 帮不了任何人。
#
# ─────────────────────────────────────────────────────
#  六、自定义异常：给自己的错起个名字
# ─────────────────────────────────────────────────────
#
#     class 余额不够(Exception):        ← ⚠ 括号里的 Exception 不能省
#         pass
#
#     def 取钱(余额, 要取):
#         if 要取 > 余额:
#             raise 余额不够('余额只有 ' + str(余额) + '，取不了 ' + str(要取))
#         return 余额 - 要取
#
# 调用方就能只接住「这一种错」，不和别的错混在一起：
#
#     try:
#         取钱(10, 50)
#     except 余额不够 as e:
#         print('取款失败：', e)        ⇒ 取款失败： 余额只有 10，取不了 50
#
# 好处：读代码的人一眼就知道「这个函数会抛 余额不够」，而不是笼统的「可能会出错」。
#
# ─────────────────────────────────────────────────────
#  ⚠ 这一课唯一要记住的坑：自定义异常忘了继承 Exception
# ─────────────────────────────────────────────────────
#
#     class 余额不够:              ← 忘了 (Exception)
#         pass
#
#     try:
#         raise 余额不够('不够了')
#     except 余额不够:             ← 在这里炸
#         pass
#
#     ⇒ TypeError: catching classes that do not inherit from BaseException is not allowed
#
# ⚠ 报错发生在 `except` 那一行，而不在 class 那一行 —— 第一次见很难联想过去。
#   真因是：能被 raise / except 的东西必须继承 **BaseException**（Exception 就是它的子类）。
# ⇒ 一句话：**自定义异常一律写 `class 名字(Exception):`**，哪怕里面只写一个 pass。
#
# ─────────────────────────────────────────────────────
#  这一课只讲这一件事
# ─────────────────────────────────────────────────────
#   异常继承树、assert、with 背后的 __enter__ / __exit__、logging —— 用到再说。
#   这一课只讲：分开接、finally、raise、自定义异常。
#
# ─────────────────────────────────────────────────────
#  轮到你了 —— 在答题文件里写这几件事
# ─────────────────────────────────────────────────────
# 1. 写函数 转成整数(文字)：转得成返回整数；转不成 **raise ValueError**，
#    消息里要带上原来的文字（例如 不是数字：abc）
# 2. 写函数 安全相除(甲, 乙)：正常返回 甲 / 乙；乙是 0 时 **raise ZeroDivisionError**，
#    消息自己要写一句（不许让它自己去崩）
# 3. 写类 查不到(Exception)，再写函数 查分数(成绩表, 名字)：
#    名字在表里就返回分数；不在就 **raise 查不到(名字)**（消息里要有这个名字）
# 4. 写函数 挑出数字(文字们)：把列表里**能转成整数**的挑出来组成新列表，
#    转不成的跳过（不许崩）；空列表返回 []
#
# ─────────────────────────────────────────────────────
#  怎么自己测
# ─────────────────────────────────────────────────────
#    python -m hybridengine_light 检查 33


def CHECK_转成整数(答案):
    f = 答案.__dict__.get("转成整数")
    if not callable(f):
        return False, ("还没有定义函数「转成整数」。\n"
                       "      写法：def 转成整数(文字):\n"
                       "              try:\n"
                       "                  return int(文字)\n"
                       "              except ValueError:\n"
                       "                  raise ValueError('不是数字：' + 文字)")
    for 文字, 想 in (('7', 7), ('0', 0), ('-3', -3), ('123', 123)):
        try:
            得 = f(文字)
        except Exception as e:                              # noqa: BLE001
            return False, (f"转成整数({文字!r}) 不该出错，应当返回 {想}，"
                           f"却抛了 {type(e).__name__}: {e}。\n"
                           f"      ⚠ 提示：别用 文字.isdigit() 去判断 —— 它不认负号，"
                           f"'-3' 会被它判成「不是数字」。\n"
                           f"        用 try/except 接住 int() 的报错才是对的。")
        if 得 != 想:
            return False, (f"转成整数({文字!r}) 应当是 {想}，得到 {得!r}。")
    for 坏 in ('abc', '7.5', ''):
        try:
            得 = f(坏)
        except ValueError as e:
            if 坏 and 坏 not in str(e):
                return False, (f"转成整数({坏!r}) 抛了 ValueError，但消息里没有原文。\n"
                               f"        现在消息是 {str(e)!r}\n"
                               f"      ⚠ 写清楚是哪个值错了：\n"
                               f"          raise ValueError('不是数字：' + 文字)")
            continue
        except Exception as e:                              # noqa: BLE001
            return False, (f"转成整数({坏!r}) 抛的是 {type(e).__name__}，应当是 ValueError。\n"
                           f"      ⚠ int('{坏}') 自己抛的就是 ValueError —— "
                           f"把原来的异常换个消息再 raise 出去就行。")
        return False, (f"转成整数({坏!r}) 应当抛 ValueError，却返回了 {得!r}。\n"
                       f"      ⚠ 这一课要求：转不成要**主动报错**，不要返回 None 或 -1 ——\n"
                       f"        调用的人就没法把「输入错了」和「结果真的是 -1」分开。")
    return True, "四组正常值都对，三组坏值都抛了 ValueError 且消息里带着原值"


def CHECK_安全相除(答案):
    f = 答案.__dict__.get("安全相除")
    if not callable(f):
        return False, ("还没有定义函数「安全相除」。\n"
                       "      写法：def 安全相除(甲, 乙):\n"
                       "              if 乙 == 0:\n"
                       "                  raise ZeroDivisionError('除数不能是 0')\n"
                       "              return 甲 / 乙")
    for 甲, 乙, 想 in ((6, 3, 2.0), (1, 1, 1.0), (0, 5, 0.0), (-4, 2, -2.0), (5, 2, 2.5)):
        try:
            得 = f(甲, 乙)
        except Exception as e:                              # noqa: BLE001
            return False, f"安全相除({甲}, {乙}) 出错了：{type(e).__name__}: {e}"
        if 得 != 想:
            return False, (f"安全相除({甲}, {乙}) 应当是 {想}，得到 {得!r}。\n"
                           f"      ⚠ 用 / 就是小数结果（5/2 得 2.5，不是 2）。")
    for 甲, 乙 in ((5, 0), (0, 0), (-1, 0)):
        try:
            得 = f(甲, 乙)
        except ZeroDivisionError as e:
            消息 = str(e).strip()
            if not 消息:
                return False, (f"安全相除({甲}, {乙}) 抛的 ZeroDivisionError 没有消息。\n"
                               f"      ⚠ 写一句人话：raise ZeroDivisionError('除数不能是 0')")
            # ⚠ 判据要卡住「谁抛的」：Python 自己抛的那句是固定的几个词。
            #   第一版我只检查「消息非空」，于是 `return 甲 / 乙` 这种
            #   **根本没判断**的写法照样通过 —— 而这一课要的正是那个 if。
            if 消息 in ("division by zero", "float division by zero",
                       "integer division or modulo by zero"):
                return False, (f"安全相除({甲}, {乙}) 抛的是 **Python 自己**那个报错"
                               f"（消息是 {消息!r}）—— 说明你没有先判断。\n"
                               f"      ⚠ 这一课要求自己写一句：\n"
                               f"          if 乙 == 0:\n"
                               f"              raise ZeroDivisionError('除数不能是 0')\n"
                               f"          return 甲 / 乙")
            continue
        except Exception as e:                              # noqa: BLE001
            return False, (f"安全相除({甲}, {乙}) 抛的是 {type(e).__name__}，"
                           f"应当是 ZeroDivisionError。\n"
                           f"      ⚠ 自己先判断再 raise，别让 Python 替你去除：\n"
                           f"          if 乙 == 0:\n"
                           f"              raise ZeroDivisionError('除数不能是 0')")
        return False, (f"安全相除({甲}, {乙}) 应当抛 ZeroDivisionError，却返回了 {得!r}。\n"
                       f"      ⚠ 除以 0 必须主动报错 —— 返回 0 或 None 会让调用者算出一个假答案。")
    return True, "五组正常值都对（含 0 和负数），三组除零都抛了带消息的 ZeroDivisionError"


def CHECK_查不到和查分数(答案):
    类 = 答案.__dict__.get("查不到")
    if not isinstance(类, type):
        return False, ("还没有定义异常类「查不到」。\n"
                       "      写法：class 查不到(Exception):\n"
                       "                pass\n"
                       "      ⚠ 括号里的 Exception 不能省 —— 这一课要记的坑就是它。")
    if not issubclass(类, Exception):
        return False, ("「查不到」没有继承 Exception。\n"
                       "      ⚠ 不继承的话，`except 查不到:` 会直接报：\n"
                       "          TypeError: catching classes that do not inherit from "
                       "BaseException is not allowed\n"
                       "        改成：class 查不到(Exception):")
    f = 答案.__dict__.get("查分数")
    if not callable(f):
        return False, ("还没有定义函数「查分数」。\n"
                       "      写法：def 查分数(成绩表, 名字):\n"
                       "              if 名字 not in 成绩表:\n"
                       "                  raise 查不到(名字)\n"
                       "              return 成绩表[名字]")
    for 表, 名字, 想 in (({'小明': 90}, '小明', 90), ({'a': 0}, 'a', 0),
                        ({'a': -1, 'b': 2}, 'b', 2)):
        try:
            得 = f(dict(表), 名字)
        except Exception as e:                              # noqa: BLE001
            return False, (f"查分数({表!r}, {名字!r}) 不该出错，应当返回 {想}，"
                           f"却抛了 {type(e).__name__}: {e}")
        if 得 != 想:
            return False, f"查分数({表!r}, {名字!r}) 应当是 {想}，得到 {得!r}。"
    for 表, 名字 in (({}, '任何人'), ({'小明': 90}, '小红'), ({'a': 1}, 'b')):
        try:
            得 = f(dict(表), 名字)
        except 类 as e:                                     # noqa: BLE001
            if 名字 not in str(e):
                return False, (f"查分数({表!r}, {名字!r}) 抛了「查不到」，但消息里没有那个名字。\n"
                               f"        现在消息是 {str(e)!r}\n"
                               f"      ⚠ 写清楚查的是谁：raise 查不到(名字)")
            continue
        except Exception as e:                              # noqa: BLE001
            return False, (f"查分数({表!r}, {名字!r}) 抛的是 {type(e).__name__}，"
                           f"应当是自定义的「查不到」。\n"
                           f"      ⚠ 查不到是正常情况，要抛出**你自己的**那种错，"
                           f"调用者才好单独处理它。")
        return False, (f"查分数({表!r}, {名字!r}) 应当抛「查不到」，却返回了 {得!r}。")
    return True, "三类正常查询都对，三类查不到都抛了带名字的「查不到」"


def CHECK_挑出数字(答案):
    f = 答案.__dict__.get("挑出数字")
    if not callable(f):
        return False, ("还没有定义函数「挑出数字」。\n"
                       "      写法：def 挑出数字(文字们):\n"
                       "              结果 = []\n"
                       "              for 文字 in 文字们:\n"
                       "                  try:\n"
                       "                      结果.append(int(文字))\n"
                       "                  except ValueError:\n"
                       "                      continue\n"
                       "              return 结果")
    for 文字们, 想 in (([], []), (['1', '2'], [1, 2]), (['1', 'x', '3'], [1, 3]),
                      (['x'], []), (['-1', '0'], [-1, 0]), (['7', '7.5', '8'], [7, 8])):
        try:
            得 = f(list(文字们))
        except Exception as e:                              # noqa: BLE001
            return False, (f"挑出数字({文字们!r}) 崩了：{type(e).__name__}: {e}\n"
                           f"      ⚠ 这一题的重点就是**不许崩**：转不成的跳过，"
                           f"继续处理下一个。")
        if 得 != 想:
            return False, (f"挑出数字({文字们!r}) 应当是 {想!r}，得到 {得!r}。\n"
                           f"      ⚠ 转不成的跳过（continue），别整个函数崩掉；\n"
                           f"        空列表返回 []；'7.5' 这种带小数点的 int() 转不了，要跳过。")
    return True, "六组都对（空列表 / 全能转 / 混着坏值 / 全坏 / 负数 / 带小数点）"
