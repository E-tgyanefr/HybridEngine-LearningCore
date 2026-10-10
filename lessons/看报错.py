"""现场演示报错翻译器 —— 每个新手都会踩的坑，看它怎么讲。

运行： python lessons/看报错.py

这个文件本身就是"课程"：你不需要看懂代码，
只要看**同样的错误，普通 Python 和 learncode 分别说了什么**。
"""

import sys, os
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from learncode import explain

# 每个用例 = (说明, 一段会出错的代码)
CASES = [
    ("把数字和文字加在一起（第一天必踩）", lambda: "你考了" + 95 + "分"),
    ("忘了给名字加引号", lambda: 你好),
    ("列表取越界（忘了从 0 数）", lambda: [10, 20, 30][3]),
    ("字典里没有这个键", lambda: {"苹果": 3}["香蕉"]),
    ("除以 0", lambda: 10 / 0),
    ("把字符串当列表用", lambda: "abc".append("d")),
    ("input() 拿到的其实是文字", lambda: int("九十五")),
]


def main():
    for i, (title, fn) in enumerate(CASES, 1):
        print()
        print("=" * 60)
        print(f"用例 {i}：{title}")
        print("=" * 60)
        try:
            fn()
        except Exception as e:                       # noqa: BLE001
            print(explain(e, filename=__file__))
        if i == 2:
            print()
            print("（下面还有 5 个用例，继续看……）")


if __name__ == "__main__":
    main()
