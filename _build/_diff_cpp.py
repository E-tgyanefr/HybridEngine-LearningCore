import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
sys.path.insert(0, LC)
os.chdir(LC)

from hybridengine_light import learnmodel as M                  # noqa: E402

属性 = re.compile(r'\[\s*(?:\w*课?程|课程)\s*\(\s*"(\d\d)"\s*,\s*"([^"]+)"')

d = os.path.join(LC, "csharp", "cpp-lessons", "Lessons")
快 = []
for f in sorted(os.listdir(d)):
    if not f.endswith(".cs"):
        continue
    t = io.open(os.path.join(d, f), encoding="utf-8").read()
    m = 属性.search(t)
    if m:
        快.append((m.group(1), m.group(2)))

慢 = [(k.编号, k.标题) for k in M.发现("cpp")]

print("  === 逐条对比 ===")
for i in range(max(len(快), len(慢))):
    a = 快[i] if i < len(快) else None
    b = 慢[i] if i < len(慢) else None
    标 = "  " if a == b else "XX"
    print("  %s %d  直接解析=%r" % (标, i + 1, a))
    print("        dotnet  =%r" % (b,))
print()
print("  .cs 文件：")
for f in sorted(os.listdir(d)):
    if f.endswith(".cs"):
        print("    " + f)
