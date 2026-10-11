"""通用：把 `<<<模板开始>>>…<<<模板结束>>>` 的稿子落盘到**指定课程线**的课文件。

用法：
    python _apply_any.py <稿子文件> <线名>
例如：
    python _apply_any.py _build/_cpp_模板扩写.md cpp-lessons
    python _apply_any.py _build/_java_模板扩写.md java-lessons

⚠ 先校验（不允许三个连续双引号 / 单个竖线 / 太短），校验不过**一个字都不写**。
⚠ 写完必须：编译 → 验证。三步都过才算数。
"""
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
三引号 = '"' * 3

稿 = [a for a in sys.argv[1:] if not a.startswith("--")]
源, 线 = 稿[0], 稿[1]
D = os.path.join(LC, "csharp", 线, "Lessons")
原文 = io.open(源, encoding="utf-8").read()

节 = re.findall(
    r"^=====\s*(\d\d)\s*=====\s*\n<<<模板开始>>>\n(.*?)\n<<<模板结束>>>\s*$",
    原文, re.M | re.S)
print("  解析到 %d 节：%s" % (len(节), " ".join(n for n, _ in 节)))
if not 节:
    print("  XX 一节都没解析出来")
    sys.exit(1)

错 = []
for 号, 文 in 节:
    if 三引号 in 文:
        错.append("%s：有三个连续双引号" % 号)
    for m in re.finditer(r"(?<!\\)\|", 文):
        段 = 文[max(0, m.start() - 1):m.start() + 2]
        if "||" in 段:
            continue
        错.append("%s：有单个竖线（%r）" % (号, 文[max(0, m.start() - 24):m.start() + 12]))
        break
    n = len([x for x in 文.split("\n") if x.strip()])
    if n < 60:
        错.append("%s：只有 %d 行（太短）" % (号, n))
if 错:
    print("  XX 校验不过，**不写**：")
    for e in 错:
        print("     " + e)
    sys.exit(1)

定界 = re.compile(r'(模板 => """)' + "(.*?)" + '(""";)', re.S)
替 = 0
for 号, 文 in sorted(节.items() if isinstance(节, dict) else 节):
    候选 = [f for f in os.listdir(D) if f.startswith("第" + 号 + "课")]
    if len(候选) != 1:
        print("  XX %s：找到 %d 个文件" % (号, len(候选)))
        continue
    P = os.path.join(D, 候选[0])
    t = io.open(P, encoding="utf-8").read()
    m = 定界.search(t)
    if not m:
        print("  XX %s：找不到模板定界符" % 号)
        continue
    旧 = len([x for x in m.group(2).split("\n") if x.strip()])
    t = t[:m.start(2)] + "\n" + 文.strip("\n") + "\n" + t[m.end(2):]
    io.open(P, "w", encoding="utf-8", newline="").write(t)
    新 = len([x for x in 文.split("\n") if x.strip()])
    print("    %s  %-40s %3d -> %3d 行" % (号, 候选[0][:38], 旧, 新))
    替 += 1
print("  落盘 %d 课" % 替)
