import io
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
S = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode\csharp\cs-lessons\Lessons"

旧 = '''                     代码, @"(?m)^\\s*(?:public\\s+|internal\\s+|private\\s+)?(?:static\\s+)?(?:sealed\\s+)?(?:abstract\\s+)?class\\s+(\\w+)"))'''

新 = '''                     代码, @"(?m)^\\s*(?:public\\s+|internal\\s+|private\\s+)?(?:static\\s+)?(?:sealed\\s+)?(?:abstract\\s+)?(?:class|interface|enum|struct)\\s+(\\w+)"))'''

n = 0
for f in sorted(os.listdir(S)):
    if not f.endswith(".cs"):
        continue
    P = os.path.join(S, f)
    t = io.open(P, encoding="utf-8").read()
    if 旧 in t:
        io.open(P, "w", encoding="utf-8", newline="").write(t.replace(旧, 新, 1))
        n += 1
    else:
        print("  ! %s 锚点未找到" % f)
print("  %d 课的类型扫描已覆盖 interface / enum / struct" % n)
print()
print("  ⚠ 记一笔教训：同一个规则（\"哪些声明要补修饰符\"）")
print("     出现在**两个地方**（CppLesson.补公开 与 各课.限定），")
print("     我上一轮只改了一处。**规则重复的地方，改一处必漏另一处** ——")
print("     这是本次会话里同类错误的第 4 次（前三次：骨架选择漏 C#、")
print("     占位符写法、报错解释器）。")
