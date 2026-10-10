# 第 02 课：模块化 —— 把代码拆成两个文件

# 需要多文件

# 要做：
#   这一课的作业是**两个文件**，不是一个大文件：
#
#   mytools.py   ── 装工具函数（面积、周长、是正方形、单位）
#   main.py      ── 主程序，用 import 把 mytools 拿进来用
#
#   具体每条要求写在那两个文件的注释里。
#   第一次运行检查命令时会自动帮你建好这两个文件。

# ─────────────────────────────────────────────────────
#  为什么要有这一课
# ─────────────────────────────────────────────────────
#
# 第 01 课里所有东西都写在一个文件里，那样写几百行就开始难受了：
#   · 找一个函数要滚很久
#   · 两个功能用了同名的变量，会互相打架
#   · 想把「算面积」这部分拿给另一个程序用，只能复制粘贴
#
# 模块化就是把一个大文件切成几个小文件，每个文件只负责一件事。
# Python 里，**一个 .py 文件就是一个模块** —— 就这么简单。
#
#     文件 mytools.py 里定义了 面积()
#     另一个文件 main.py 里写：  import mytools
#     然后就能用：               mytools.面积(10, 5)
#
# ─────────────────────────────────────────────────────
#  三个新手最常卡住的地方
# ─────────────────────────────────────────────────────
#
# ① 两个文件要放在**同一个目录**里。
#    import 找的是「和当前文件同级、或者 Python 搜索路径里」的模块。
#    放错目录是 "ModuleNotFoundError" 的头号原因。
#
# ② import 后面跟的是**文件名（不带 .py）**，不是函数名。
#      ✅ import mytools          →  mytools.面积(10, 5)
#      ✅ from mytools import 面积 →  面积(10, 5)
#      ❌ import mytools.面积      →  报错：它以为 mytools 是个包
#
# ③ 被 import 的文件**不要**在里面写 input() 或 print()。
#    模块是给别人用的零件，零件不该自己跑起来跟人说话。
#    这就是为什么游戏文件末尾都有这一行：
#        if __name__ == "__main__":
#            main()
#    它的意思是「只有直接运行这个文件时才执行 main」——
#    别人 import 你的时候不会把你的程序跑起来。
#
# ─────────────────────────────────────────────────────
#  这一课的检查在验证什么
# ─────────────────────────────────────────────────────
#
# 检查会**分别**看两个文件：
#   · mytools.py 里的函数必须真的算对（不是硬编码）
#   · main.py 的 描述() 必须**通过 mytools 去算**，而不是自己重写一遍
#
# 第二条是关键。把函数抄一份到 main.py 里也能输出正确结果，
# 但那样就完全没有"模块化"了 —— 而那正是这一课唯一要考的东西。
# 所以检查会把 mytools 里的函数换掉，看 main 的输出会不会跟着变。

# ─────────────────────────────────────────────────────
#  写完之后
# ─────────────────────────────────────────────────────
#   python -m hybridengine_light 检查 02
#
# 你不需要自己写测试。卡住了可以看参考答案：
#   python -m hybridengine_light 答案 02     ← 先自己写一遍再看


# ── 下面是检查用的代码，不用改 ──

def _取(答案, 名字):
    """从答案里取一个东西。取不到就抛异常 —— 由调用方转成友好提示。"""
    return getattr(答案, 名字)


def CHECK_两个文件都在(答案):
    """main.py 与 mytools.py 必须都能被加载，而且 main 里要能用到 mytools。"""
    if not hasattr(答案, "mytools"):
        return False, (
            "main.py 里没有把 mytools 引进来。\n"
            "改法：在 main.py 开头写一行\n"
            "    import mytools\n"
            "（注意：import 后面是文件名，不带 .py；而且不用加引号）")
    return True, "main.py 成功 import 了 mytools"


def CHECK_mytools_有函数(答案):
    缺 = []
    for 名字 in ("面积", "周长", "是正方形"):
        if not hasattr(答案.mytools, 名字):
            缺.append(名字)
    if 缺:
        return False, (f"mytools.py 里还缺这些函数：{'、'.join(缺)}\n"
                       f"写法：def {缺[0]}(长, 宽):\n         return ...")
    return True, "三个函数都在"


def CHECK_面积算得对(答案):
    f = 答案.mytools.面积
    错的 = []
    for 长, 宽, 期望 in [(10, 5, 50), (3, 3, 9), (1, 7, 7), (0.5, 4, 2)]:
        try:
            得到 = f(长, 宽)
        except Exception as e:                        # noqa: BLE001
            return False, f"面积({长}, {宽}) 调用出错：{type(e).__name__}: {e}"
        if 得到 != 期望:
            错的.append(f"面积({长}, {宽}) 得到 {得到}，应该是 {期望}")
    if 错的:
        return False, "算错了：\n      " + "\n      ".join(错的)
    return True, "面积计算正确"


def CHECK_周长算得对(答案):
    f = 答案.mytools.周长
    错的 = []
    for 长, 宽, 期望 in [(10, 5, 30), (3, 3, 12), (2, 8, 20)]:
        try:
            得到 = f(长, 宽)
        except Exception as e:                        # noqa: BLE001
            return False, f"周长({长}, {宽}) 调用出错：{type(e).__name__}: {e}"
        if 得到 != 期望:
            错的.append(f"周长({长}, {宽}) 得到 {得到}，应该是 {期望}")
    if 错的:
        return False, ("算错了：\n      " + "\n      ".join(错的) +
                       "\n      提示：周长 = 2 × (长 + 宽)")
    return True, "周长计算正确"


def CHECK_是正方形判得对(答案):
    f = 答案.mytools.是正方形
    for 长, 宽, 期望 in [(3, 3, True), (3, 4, False), (5, 5, True)]:
        try:
            得到 = f(长, 宽)
        except Exception as e:                        # noqa: BLE001
            return False, f"是正方形({长}, {宽}) 调用出错：{type(e).__name__}: {e}"
        if bool(得到) != 期望:
            return False, (f"是正方形({长}, {宽}) 得到 {得到}，应该是 {期望}\n"
                           f"      返回的应该是 True/False（布尔值），不是数字或文字")
    return True, "正方形判断正确"


def CHECK_单位常量(答案):
    if not hasattr(答案.mytools, "单位"):
        return False, ("mytools.py 里没有「单位」这个变量。\n"
                       "改法：在 mytools.py 里加一行\n"
                       '    单位 = "平方米"')
    值 = 答案.mytools.单位
    if 值 != "平方米":
        return False, f"单位 应该是 \"平方米\"，现在是 {值!r}"
    return True, "单位 = \"平方米\""


def CHECK_描述在main里(答案):
    if not hasattr(答案, "描述"):
        return False, ("main.py 里没有 描述() 函数。\n"
                       "这一课的 main.py 要提供一个 描述(长, 宽) 函数。")
    return True, "描述() 在 main.py 里"


def CHECK_描述格式对(答案):
    if not hasattr(答案, "描述"):
        return False, "还没有 描述() 函数"
    try:
        得到 = 答案.描述(10, 5)
    except Exception as e:                            # noqa: BLE001
        return False, (f"描述(10, 5) 调用出错：{type(e).__name__}: {e}\n"
                       f"      常见原因：忘了在 main.py 里 import mytools，"
                       f"或者函数名写错了。")
    期望 = "10 × 5 的面积是 50 平方米，周长是 30"
    if 得到 != 期望:
        return False, (f"格式不太一样。\n"
                       f"      期望：{期望}\n"
                       f"      实际：{得到!r}\n"
                       f"      注意「×」是乘号不是字母 x，逗号和句号的位置也要一致。")
    return True, "描述格式正确"


def CHECK_描述真的用了mytools(答案):
    """这一条才是本课的重点：描述() 必须**通过模块**去算，而不是自己重写一遍。

    怎么验：把 mytools 里的函数**全部**临时换掉，看描述() 的输出会不会跟着变。
    会变 = 真的用了模块；不变 = 自己重写了一份（那就没有模块化）。

    ⚠ 审查发现过一个漏洞：原来只替换「面积」和「单位」，**不替换「周长」**。
      于是「面积走模块、周长手算 2*(长+宽)」能拿 9/9 满分 ——
      而课程第 56-61 行明写「必须通过 mytools 去算，而不是自己重写一遍」。
      只验证一半，学生就只需要模块化一半。
    """
    if not (hasattr(答案, "描述") and hasattr(答案, "mytools")):
        return False, "需要先有 mytools 模块和 描述() 函数"

    模块 = 答案.mytools
    需要 = ("面积", "周长", "单位")
    缺的 = [n for n in 需要 if not hasattr(模块, n)]
    if 缺的:
        return False, f"mytools 里还缺：{'、'.join(缺的)}"

    # 每个依赖换成一个**好认的哨兵值**
    哨兵 = {"面积": lambda 长, 宽: 777, "周长": lambda 长, 宽: 888, "单位": "哨兵单位"}
    原始 = {n: getattr(模块, n) for n in 需要}

    try:
        for n, v in 哨兵.items():
            setattr(模块, n, v)
        换过之后 = str(答案.描述(10, 5))
    except Exception as e:                            # noqa: BLE001
        return False, f"替换 mytools 的内容后，描述() 出错了：{type(e).__name__}: {e}"
    finally:
        for n, v in 原始.items():
            setattr(模块, n, v)

    # 三个哨兵里，哪些**没有**出现在描述() 的输出里 —— 那些就是被自己重写的
    没用上的 = []
    if "777" not in 换过之后:
        没用上的.append("面积")
    if "888" not in 换过之后:
        没用上的.append("周长")
    if "哨兵单位" not in 换过之后:
        没用上的.append("单位")

    if not 没用上的:
        return True, "描述() 确实通过 mytools 计算（面积、周长、单位都用上了）—— 模块真的接上了"

    提示 = {
        "面积": "mytools.面积(长, 宽)",
        "周长": "mytools.周长(长, 宽)",
        "单位": "mytools.单位",
    }
    return False, (
        f"描述() 没有真的用 mytools 里的这些：{'、'.join(没用上的)}\n"
        f"      我把 mytools 里的函数全换成固定返回 777 / 888 / 「哨兵单位」的版本后，\n"
        f"      描述(10, 5) 返回的是：\n"
        f"          {换过之后}\n"
        f"      如果这些都真的走了模块，输出里应该同时出现 777、888 和「哨兵单位」。\n"
        f"      改法：\n"
        + "".join(f"          {提示[n]}   ← 用这个，不要自己再算一遍\n" for n in 没用上的)
        + "      注意：**用了模块的一半也算没做到** —— "
          "这一课要考的就是把计算都交给模块。")
