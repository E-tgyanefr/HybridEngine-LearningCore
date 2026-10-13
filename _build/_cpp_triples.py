# -*- coding: utf-8 -*-
"""临时：把 _verify_cpp_negative.py 的错例表摘成 (课号/说明/期望) 三元组。
只读，用来对齐「形态」。"""
import ast
import io
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
P = os.path.join(ROOT, "_build", "_verify_cpp_negative.py")
树 = ast.parse(io.open(P, encoding="utf-8").read())
出 = []
for node in 树.body:
    if isinstance(node, ast.Assign) and getattr(node.targets[0], "id", "") == "错例":
        表 = ast.literal_eval(node.value)
        出.append("共 %d 条" % len(表))
        for 项 in 表:
            编, 码, 说明, 期望 = 项[:4]
            空转 = "  [空转]" if len(项) > 4 else ""
            # ⚠ 换行必须换成字面 \n —— 说明里带换行会让下面一行对不上
            出.append("%s | %-56s | %s%s"
                      % (编, 说明.replace("\n", " ")[:56],
                         期望.replace("\n", " ")[:62], 空转))
        break
# ⚠ 自己写文件，不用 shell 重定向 —— pwsh 的 `>` 会写成 UTF-16，
#   下游按 UTF-8 读就成了乱码（刚踩过）。
io.open(os.path.join(ROOT, "_build", "_cpp_triples.txt"),
        "w", encoding="utf-8", newline="\n").write("\n".join(出) + "\n")
print("写出 %d 行" % len(出))
