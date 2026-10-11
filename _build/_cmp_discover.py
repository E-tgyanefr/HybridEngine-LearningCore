# 验证：直接读 Lessons/*.cs 解析 `[课程]` / `[Java课程]`，能否得到和
# `dotnet run`（现在的办法）**完全一样**的课程表。
#
# ⚠ 必须完全一样再切换 —— 否则界面上的课名会和检查时说的课名不一致。
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
sys.path.insert(0, LC)
os.chdir(LC)

from hybridengine_light import learnmodel as M                  # noqa: E402

#: `[课程("01", "第 01 课：…"` —— C / C++ / C# 用它
#: `[Java课程("01", "第 01 课：…"` —— Java 用它
属性 = re.compile(r'\[\s*(?:\w*课?程|课程)\s*\(\s*"(\d\d)"\s*,\s*"([^"]+)"')

def 直接解析(工程: os.PathLike) -> list[tuple[str, str]]:
    d = os.path.join(工程, "Lessons")
    if not os.path.isdir(d):
        return []
    出 = []
    for f in sorted(os.listdir(d)):
        if not f.endswith(".cs"):
            continue
        t = io.open(os.path.join(d, f), encoding="utf-8").read()
        m = 属性.search(t)
        if m:
            出.append((m.group(1), m.group(2)))
    return 出


工程名 = {"csharp": "cs-lessons", "cpp": "cpp-lessons",
          "java": "java-lessons", "c": "c-lessons"}

import time
全部一致 = True
for 键 in M.编译型轨道:
    工程 = os.path.join(LC, "csharp", 工程名[键])
    t0 = time.time()
    快 = 直接解析(工程)
    快耗时 = time.time() - t0

    t0 = time.time()
    慢 = M.发现(键)
    慢耗时 = time.time() - t0

    慢列 = [(k.编号, k.标题) for k in 慢]
    一致 = 快 == 慢列
    全部一致 = 全部一致 and 一致
    print("  %-7s 直接解析 %2d 课（%.3f 秒）· dotnet %2d 课（%.2f 秒）· %s"
          % (键, len(快), 快耗时, len(慢), 慢耗时, "一致" if 一致 else "**不一致**"))
    if not 一致:
        print("      直接解析:", 快[:3])
        print("      dotnet  :", 慢列[:3])

print()
print("  结论：%s" % ("可以直接解析，课程表完全一致" if 全部一致
                    else "有差异，**不能**直接切换"))
