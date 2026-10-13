# -*- coding: utf-8 -*-
r"""打印 C / Java / C++ / C# 课程文件里的**参考答案**（带行号）。

为什么要有它：`_verify_*_negative.py` 的错例表用「(旧, 新) 字符串锚点」，
锚点必须是参考答案里的**原文**；而参考答案藏在课程 `.cs` 文件的
`public static string 参考答案 => '''…''';` 里（C# 的原始字符串字面量）。
手工一段段复制粘贴容易抄错（抄错会被自检拦下，但白跑一轮）。

用法：
    python _build/_probe_c_ref.py c 00 13 14
    python _build/_probe_c_ref.py java 19 22
⚠ 只读，不改任何课程文件。
"""
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

线 = {
    "c": os.path.join(ROOT, "csharp", "c-lessons", "Lessons"),
    "java": os.path.join(ROOT, "csharp", "java-lessons", "Lessons"),
    "cpp": os.path.join(ROOT, "csharp", "cpp-lessons", "Lessons"),
    "csharp": os.path.join(ROOT, "csharp", "cs-lessons", "Lessons"),
}
_参考 = re.compile(r'public\s+static\s+string\s+参考答案\s*=>\s*"""(.*?)""";', re.S)


def main():
    参 = sys.argv[1:]
    if len(参) < 2 or 参[0] not in 线:
        print(__doc__)
        return 2
    目录 = 线[参[0]]
    for n in 参[1:]:
        匹配 = [f for f in os.listdir(目录)
                if f.startswith("第%s课" % n) and f.endswith(".cs")]
        if not 匹配:
            print("  !! 第 %s 课找不到（%s）" % (n, 目录))
            continue
        文 = io.open(os.path.join(目录, 匹配[0]), encoding="utf-8").read()
        m = _参考.search(文)
        print("#" * 24, "第 %s 课 %s" % (n, 匹配[0]))
        if not m:
            print("   （取不到参考答案）")
            continue
        for i, l in enumerate(m.group(1).strip().splitlines(), 1):
            print("%3d| %s" % (i, l))
        print()
    return 0


if __name__ == "__main__":
    sys.exit(main())
