# 第 08 课：把一个「人」写成一个类 —— 综合练习


# ─────────────────────────────────────────────────────
#  先介绍：这一课在考什么
# ─────────────────────────────────────────────────────
#
# 不是新的语法 —— 前七课的东西都用得上：
#
#     第 03 课  列表与循环        成绩表、求平均/最高
#     第 05 课  函数与默认参数    `成绩表=None` 这种默认值陷阱
#     第 06 课  处理异常          空列表不要崩
#     第 07 课  类                self、__init__、方法
#
# ⚠ 有一个**很常见的 Python 陷阱**，这一课专门考它：
#
#     def __init__(self, 姓名, 成绩表=[]):     ← ❌ 千万别这么写！
#         self.成绩表 = 成绩表
#
#   看起来没问题，但默认值 `[]` **只在定义函数时创建一次**，
#   所以**所有**不传成绩表的学生会共用同一个列表：
#
#     a = 学生("小明"); b = 学生("小红")
#     a.加成绩(100)
#     print(b.成绩表)      ⇒ [100]   ← 小红莫名其妙也有分了！
#
#   ⇒ 正确写法是用 `None` 当默认值，进函数再造列表：
#
#     def __init__(self, 姓名, 成绩表=None):
#         self.成绩表 = [] if 成绩表 is None else 成绩表
#
#   这个坑叫「可变默认参数」，是 Python 最著名的坑之一。
#   检查里会**专门测它** —— 两个学生必须互不影响。
#
# ─────────────────────────────────────────────────────
#
# ─────────────────────────────────────────────────────
#  轮到你了 —— 在下面的答题文件里写这几件事
# ─────────────────────────────────────────────────────
#
#   写一个类 学生：
#     · 造出来时给 `姓名` 和一个 `成绩表`（默认空列表）
#     · 方法 `加成绩(分数)` 往成绩表里加一分
#     · 方法 `平均分()` 返回平均分；**一分都没有就返回 0**
#     · 方法 `最高分()` 返回最高分；**一分都没有就返回 0**
#     · 方法 `评语()` 返回：
#         没有成绩   → "还没有成绩"
#         平均 >= 90 → "优秀"
#         平均 >= 60 → "及格"
#         否则       → "要加油"
#
#   写完再写一个函数 全班报告(学生表)，返回多行文字，
#   每个学生一行：`姓名 平均分=xx.x 评语`
#
#  怎么自己测
# ─────────────────────────────────────────────────────
#     python -m hybridengine_light 检查 08


def CHECK_学生类(答案):
    if not hasattr(答案, "学生"):
        return False, "还没有定义「学生」类。"
    类 = 答案.学生
    if not isinstance(类, type):
        return False, f"「学生」应当是类，现在是 {type(类).__name__}。"
    try:
        a = 类("小明")
    except Exception as e:                                      # noqa: BLE001
        return False, (f"学生('小明') 造不出来：{type(e).__name__}: {e}\n"
                       f"      ⚠ 成绩表要**有默认值**，只传姓名也能造。")
    if getattr(a, "姓名", None) != "小明":
        return False, f".姓名 应当是 '小明'，实际是 {getattr(a, '姓名', None)!r}。"
    if not hasattr(a, "成绩表"):
        return False, "学生对象上没有 .成绩表。"
    if list(a.成绩表) != []:
        return False, f"不传成绩表时应当是空列表，实际是 {a.成绩表!r}。"
    return True, "带默认值的学生类能造出来"


def CHECK_加成绩与平均(答案):
    if not hasattr(答案, "学生"):
        return False, "还没有定义「学生」类。"
    类 = 答案.学生
    try:
        a = 类("小明")
    except Exception as e:                                      # noqa: BLE001
        return False, f"造学生失败：{type(e).__name__}: {e}"
    if not hasattr(a, "加成绩") or not callable(a.加成绩):
        return False, "没有 加成绩(分数) 方法。"
    for n in (90, 80, 70):
        a.加成绩(n)
    if list(a.成绩表) != [90, 80, 70]:
        return False, (f"加了 90/80/70 之后成绩表应当是 [90, 80, 70]，"
                       f"实际是 {a.成绩表!r}。\n"
                       f"      ⚠ 用 self.成绩表.append(分数)。")
    if not hasattr(a, "平均分") or not callable(a.平均分):
        return False, "没有 平均分() 方法。"
    得 = a.平均分()
    if abs(得 - 80.0) > 1e-9:
        return False, (f"平均分() 应当是 80.0，实际是 {得!r}。\n"
                       f"      ⚠ 一个斜杠 / 才有小数。")
    b = 类("小红")
    try:
        空 = b.平均分()
    except ZeroDivisionError:
        return False, ("没有成绩时 平均分() 除以 0 崩了。\n"
                       "      写法：if not self.成绩表: return 0")
    except Exception as e:                                      # noqa: BLE001
        return False, f"空成绩时平均分() 出错了：{type(e).__name__}: {e}"
    if 空 != 0:
        return False, f"没有成绩时平均分() 应当返回 0，实际是 {空!r}。"
    if not hasattr(b, "最高分") or not callable(b.最高分):
        return False, "没有 最高分() 方法。"
    if b.最高分() != 0:
        return False, f"没有成绩时最高分() 应当返回 0，实际是 {b.最高分()!r}。"
    if a.最高分() != 90:
        return False, f"最高分() 应当是 90，实际是 {a.最高分()!r}。"
    return True, "加成绩 / 平均分 / 最高分（含空成绩）都对"


def CHECK_两个学生互不影响(答案):
    """⚠⚠ 这条专门测「可变默认参数」陷阱。"""
    if not hasattr(答案, "学生"):
        return False, "还没有定义「学生」类。"
    类 = 答案.学生
    try:
        a = 类("小明")
        b = 类("小红")
    except Exception as e:                                      # noqa: BLE001
        return False, f"造学生失败：{type(e).__name__}: {e}"
    a.加成绩(100)
    if b.成绩表:
        return False, (f"给小明加了 100 分，小红的成绩表**也变了**：{b.成绩表!r}\n"
                       f"      ⚠⚠ 这就是「可变默认参数」陷阱：\n"
                       f"          def __init__(self, 姓名, 成绩表=[]):   ← 错！\n"
                       f"        默认的 [] 只在**定义时创建一次**，所有不传的学生共用它。\n"
                       f"        正确写法：\n"
                       f"          def __init__(self, 姓名, 成绩表=None):\n"
                       f"              self.成绩表 = [] if 成绩表 is None else 成绩表")
    if list(a.成绩表) != [100]:
        return False, f"小明的成绩表应当是 [100]，实际是 {a.成绩表!r}。"
    c = 类("小刚", [60])
    if list(c.成绩表) != [60]:
        return False, f"学生('小刚', [60]) 的成绩表应当是 [60]，实际是 {c.成绩表!r}。"
    return True, "两个学生互不影响（可变默认参数陷阱没踩）"


def CHECK_评语(答案):
    if not hasattr(答案, "学生"):
        return False, "还没有定义「学生」类。"
    类 = 答案.学生
    if not hasattr(类("x"), "评语") or not callable(类("x").评语):
        return False, "没有 评语() 方法。"
    for 分数们, 期望 in (([], "还没有成绩"),
                        ([90], "优秀"),
                        # ⚠ 89.5 **不是**优秀（门槛是 >= 90）。
                        #   我第一版这里写了「优秀」，与自己的判据矛盾 ——
                        #   那是"检查冤枉正确答案"。用参考答案跑一遍才发现。
                        ([89, 90], "及格"),
                        ([60], "及格"),
                        ([59], "要加油"),
                        ([100, 80], "优秀")):
        s = 类("测试", list(分数们))
        得 = s.评语()
        if 得 != 期望:
            return False, (f"成绩 {分数们}（平均 {s.平均分()}）的评语应当是 "
                           f"'{期望}'，实际是 {得!r}。\n"
                           f"      ⚠ 三个分界：没有成绩 / >= 90 / >= 60。\n"
                           f"        注意顺序：先判「没有成绩」，再判分数。")
    return True, "四档评语与边界都对"


def CHECK_全班报告(答案):
    if not hasattr(答案, "全班报告"):
        return False, "还没有定义「全班报告」函数。"
    if not callable(答案.全班报告):
        return False, "「全班报告」不是函数。"
    if not hasattr(答案, "学生"):
        return False, "还没有定义「学生」类。"
    try:
        们 = [答案.学生("小明", [90, 80]), 答案.学生("小红")]
        文 = 答案.全班报告(们)
    except Exception as e:                                      # noqa: BLE001
        return False, f"全班报告([...]) 出错了：{type(e).__name__}: {e}"
    if not isinstance(文, str):
        return False, (f"「全班报告」应当返回**一段文字**，现在返回的是 {type(文).__name__}。\n"
                       f"      ⚠ 把每行拼起来再返回（用 '\\n'.join(行们) 最省事）。")
    if "小明" not in 文 or "小红" not in 文:
        return False, f"报告里应当出现两个学生的姓名，实际是：\n{文}"
    if "85" not in 文:
        return False, (f"报告里应当有小明的平均分 85（(90+80)/2），实际是：\n{文}\n"
                       f"      ⚠ 平均分保留一位小数可以直接用 {平均:.1f} 这种写法。")
    if "还没有成绩" not in 文:
        return False, f"小红没有成绩，报告里应当有「还没有成绩」，实际是：\n{文}"
    if "\n" not in 文:
        return False, "两个学生应当是**两行**（用 \\n 分隔）。"
    if 答案.全班报告([]) != "":
        return False, (f"空名单应当返回**空文字**，实际是 {答案.全班报告([])!r}。\n"
                       f"      ⚠ '\\n'.join([]) 正好是空文字 —— 用 join 就自然对了。")
    return True, "两行报告、含空名单边界"
