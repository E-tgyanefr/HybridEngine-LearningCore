"""小游戏 1：猜数字（命令行版）

════ 为什么第一个游戏是这个 ════

它是最小的**完整程序**：有输入、有状态、有循环、有胜负、有重来。
而且只用第 01~04 课的知识就能读懂 —— 不引入任何新东西。

════ 玩法 ════
    电脑想一个 1~100 的数，你来猜。每猜一次它会告诉你大了还是小了。
    猜中为止，然后告诉你用了几次。

════ 读代码的建议 ════
    先看 main()（它只有十来行，是整体流程），再回头看各个小函数。
    这就是"模块化"的好处：**读的顺序可以跟写的顺序不一样**，
    因为每个函数只做一件事。

直接运行： python games/猜数字.py
"""

# ⚠ `tuple[str, int, int]` 这种写法**在 3.9 上会运行时崩**
#   （`TypeError: 'type' object is not subscriptable`）——
#   加了下面这行，注解就变成字符串、不执行，3.9 也能跑。
#   项目自己的基线是 3.9（见 _build/build_src.py 的版本闸门），
#   而原来这个文件漏了这行 ⇒ **在 3.9 上跑不起来**。
from __future__ import annotations

import random

# ── 难度设置 ──
# 把"配置"和"逻辑"分开是刻意的：想加难度只改这里，不用碰下面的逻辑。
难度表 = {
    "1": ("简单", 1, 50),
    "2": ("普通", 1, 100),
    "3": ("困难", 1, 1000),
}
最多提示次数 = {          # 只是个温和的提示，不是硬性限制
    "简单": 8,
    "普通": 10,
    "困难": 15,
}


def 选难度() -> tuple[str, int, int]:
    """让玩家选难度，返回 (难度名, 最小数, 最大数)。"""
    print()
    print("  选个难度：")
    for k, (名字, 小, 大) in 难度表.items():
        print(f"    {k}. {名字}    （{小} ~ {大}）")
    while True:
        try:
            c = input("  输入 1/2/3：").strip()
        except EOFError:
            return 难度表["2"]          # 输入没了就用默认难度，别卡在这里
        if c in 难度表:
            名字, 小, 大 = 难度表[c]
            return 名字, 小, 大
        print("  请输入 1、2 或 3。")


def 读一个数(小: int, 大: int) -> int | None:
    """读一个范围内的整数。输入 q 返回 None（表示放弃）。

    为什么要单独一个函数：**处理用户输入是最啰嗦的事** ——
    要判断是不是数字、要在不在范围内、要给提示、要允许退出。
    把它关在一个函数里，主流程才看得清。
    """
    while True:
        try:
            raw = input(f"  猜一个 {小}~{大} 的数（输入 q 放弃）：").strip()
        except EOFError:
            # 输入被用完了（比如输出被重定向到文件、或用户按了 Ctrl+D）。
            # ⚠ 必须处理：不处理的话 input() 返回空串 → int('') 抛 ValueError →
            #   被下面的 except 接住 → 无限循环刷屏。我实测撞到过。
            print()
            return None
        if raw.lower() == "q":
            return None
        try:
            n = int(raw)
        except ValueError:
            print("  那不是一个数字。再试一次。")
            continue
        if not (小 <= n <= 大):
            print(f"  要在 {小} 到 {大} 之间。")
            continue
        return n


def 判断(猜: int, 答案: int) -> str:
    """返回 '大了' / '小了' / '中了'。"""
    if 猜 > 答案:
        return "大了"
    if 猜 < 答案:
        return "小了"
    return "中了"


def 玩一局(难度: str, 小: int, 大: int) -> None:
    """玩一局。这不返回胜负 —— 它自己就把过程讲完了。"""
    答案 = random.randint(小, 大)
    次数 = 0
    print()
    print(f"  我想好了一个 {小}~{大} 的数。开始猜吧。")
    提示 = 最多提示次数[难度]
    if 提示:
        print(f"  （{难度}难度，一般 {提示} 次以内能猜到。）")

    while True:
        猜 = 读一个数(小, 大)
        if 猜 is None:
            print(f"  放弃了？答案是 {答案}。")
            return
        次数 += 1
        结果 = 判断(猜, 答案)

        if 结果 == "中了":
            print()
            print(f"  🎉 对了！就是 {答案}。你用了 {次数} 次。")
            if 次数 <= 提示:
                print("  猜得不错。")
            else:
                print("  次数有点多，试试每次都取中间值 —— 那是最快的策略。")
            return

        # 给方向提示时顺便缩小范围，玩家更容易往下走
        剩 = 次数
        print(f"  {结果}。（第 {剩} 次）")
        if 结果 == "大了":
            大 = 猜 - 1
        else:
            小 = 猜 + 1
        if 小 == 大:
            print(f"  只剩一个可能了：{小}")


def main() -> None:
    print("═" * 40)
    print("  猜数字")
    print("═" * 40)
    while True:
        难度, 小, 大 = 选难度()
        玩一局(难度, 小, 大)
        try:
            再 = input("\n  再来一局？（回车=是，n=退出）：").strip().lower()
        except EOFError:
            再 = "n"
        if 再 == "n":
            print("\n  下次见。\n")
            return


# ── 这一行是 Python 的标准写法 ──
# 意思是"只有直接运行这个文件时才执行 main()"。
# 为什么需要它：别人 import 你的模块时不该把游戏跑起来 ——
# 这正是「模块化」的一部分：既能当程序跑，也能当零件被引用。
if __name__ == "__main__":
    main()
