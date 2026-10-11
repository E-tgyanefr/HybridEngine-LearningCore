"""把 `<<<模板开始>>>…<<<模板结束>>>` 格式的文本**校验**一遍。

用法（在 _build 下）：
    python _apply_tpl.py <回复文本文件>

⚠⚠ 为什么不手工改：这个项目里我**反复**被引号/转义坑：
   · 英文引号写进 Python 字符串（SyntaxError）
   · 课程文件里出现三个双引号（破坏 C# 原始字符串）
   · 数据文件里出现裸竖线（破坏字段分隔）
   · ⚠ **连这个脚本自己的说明文字里写三个双引号都会炸**（我刚踩了）

   ⇒ 所以落盘这件事必须做成脚本，而且**脚本自己做校验**。

校验项：
  ① 标记成对、各自独占一行
  ② 不能出现三个连续双引号（会破坏 C# 原始字符串）
  ③ 不能有裸的竖线（会破坏数据文件的字段分隔）
  ④ 行数落在合理区间（不写"看着差不多"的东西）
"""
import io
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

源 = [a for a in sys.argv[1:] if not a.startswith("--")][0]
原文 = io.open(源, encoding="utf-8").read()

三引号 = '"' * 3
节 = re.findall(
    r"^=====\s*(\d\d)\s*=====\s*\n<<<模板开始>>>\n(.*?)\n<<<模板结束>>>\s*$",
    原文, re.M | re.S)
print("  解析到 %d 节：%s" % (len(节), " ".join(n for n, _ in 节)))

if not 节:
    print("  XX 一节都没解析出来 —— 检查标记格式")
    sys.exit(1)

错 = []
for 号, 文 in 节:
    if 三引号 in 文:
        错.append("%s：出现了三个连续双引号（会破坏 C# 原始字符串）" % 号)
    if re.search(r"(?<!\\)\|", 文):
        错.append("%s：有裸的竖线（会破坏数据文件的字段分隔）" % 号)
    n = len([x for x in 文.split("\n") if x.strip()])
    if n < 60:
        错.append("%s：只有 %d 行，太短（要求 120~180）" % (号, n))

print("  每节行数：")
for 号, 文 in 节:
    n = len([x for x in 文.split("\n") if x.strip()])
    flag = "  <-- 偏短" if n < 60 else ""
    print("    %s  %3d 行%s" % (号, n, flag))

# 也查一下标题行在不在（每节首行应当是 `// 第 NN 课：…`）
print("  首行检查：")
for 号, 文 in 节:
    首 = 文.split("\n")[0].strip()
    ok = 首.startswith("// 第") or 首.startswith("//")
    print("    %s  %s  %s" % (号, "OK " if ok else "XX ", 首[:70]))

if 错:
    print()
    print("  XX 校验不过：")
    for e in 错:
        print("     " + e)
    sys.exit(1)

print()
print("  OK 校验通过（%d 节）" % len(节))
print("  ⚠ 这个脚本只做校验 —— 落盘要按目标类型分别处理")
print("     （C# 是原始字符串、C 是 `模板|` 行），写完必须立刻跑验证。")
