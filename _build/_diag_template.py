# -*- coding: utf-8 -*-
"""临时诊断：python 轨道的「答题模板」现在到底是什么（只读）。"""
import io
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, ROOT)
from hybridengine_light import learnmodel as M          # noqa: E402

for 轨道 in ("python", "ai", "embedded", "raspi", "enginecourse"):
    课们 = M.发现(轨道)
    if not 课们:
        print("%-14s 一课都没发现" % 轨道)
        continue
    k = 课们[0]
    try:
        路 = M._答案文件(k)
    except Exception as e:
        路 = "（取不到：%s）" % e
    存在 = os.path.isfile(str(路))
    大小 = os.path.getsize(str(路)) if 存在 else 0
    try:
        文 = M.读答案(k)
        n = len([x for x in 文.split("\n") if x.strip()])
    except Exception as e:
        文, n = "（读答案抛 %s）" % e, -1
    print("%-14s 课=%s" % (轨道, k.编号))
    print("     答案文件：%s" % 路)
    print("     存在=%s  大小=%d  非空行=%d" % (存在, 大小, n))
    if n >= 0:
        for l in 文.split("\n")[:14]:
            print("       | " + l[:96])
    print()

print("=== lessons/answers 前 6 个 ===")
D = os.path.join(ROOT, "lessons", "answers")
if os.path.isdir(D):
    for f in sorted(os.listdir(D))[:6]:
        p = os.path.join(D, f)
        t = io.open(p, encoding="utf-8", errors="replace").read()
        print("   %-34s %6d B  非空行 %d" % (f, os.path.getsize(p),
                                            len([x for x in t.split("\n") if x.strip()])))
else:
    print("   （没有这个目录）")
