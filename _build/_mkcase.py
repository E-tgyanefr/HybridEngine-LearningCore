"""把 `_build/cases/<课号>/` 下的一堆 .py 汇成 `_build/_case<课号>.json`。

为什么要有这个脚本：用例代码里全是中文、引号和换行，手写 JSON 要一层层转义，
一不留神就写出一个语法错的 JSON（而报错信息还很难看出是转义问题）。
写成一个 .py 文件就完全不用转义。

用法：
    python _build/_mkcase.py 47

目录约定：
    _build/cases/47/对.py            参考答案
    _build/cases/47/错_π写成3.14.py  错答案（文件名去掉「错_」就是标签）
"""
import io
import json
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
BUILD = os.path.dirname(os.path.abspath(__file__))


def main():
    号 = sys.argv[1]
    d = os.path.join(BUILD, "cases", 号)
    if not os.path.isdir(d):
        print("  找不到用例目录：%s" % d)
        return 1
    对 = None
    错 = []
    for 名 in sorted(os.listdir(d)):
        if not 名.endswith(".py"):
            continue
        文 = io.open(os.path.join(d, 名), encoding="utf-8").read()
        if 名.startswith("对"):
            对 = 文
        elif 名.startswith("错"):
            错.append([名[2:-3], 文])
    if 对 is None:
        print("  目录里没有 `对.py`")
        return 1
    出 = os.path.join(BUILD, "_case%s.json" % 号)
    io.open(出, "w", encoding="utf-8", newline="\n").write(
        json.dumps({"对": 对, "错": 错}, ensure_ascii=False, indent=1))
    print("  写好 %s（对 1 份，错 %d 份）" % (出, len(错)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
