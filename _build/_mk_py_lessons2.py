import io
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

Build = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode\_build"
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
S = os.path.join(LC, "lessons")
SOL = os.path.join(S, "solutions")

# ⚠⚠ 这一版**不再在 Python 字符串里写课程内容**。
#
#   前两版我在生成器里把 C#/Python 代码当字符串拼，结果陷进转义地狱：
#   课程正文里既有中文引号又有 ASCII 引号，而它们又嵌在
#   Python 字符串里 —— 我改了 5 轮，每轮都换一处新的语法错。
#
#   ⇒ 换成**纯文本 + 分隔符**：
#       课程内容写在 .txt 里（原样字节，零转义），
#       生成器只负责按 `@@@ <文件名>` 切开、写到对应位置。
#
#   这就是"**不要在两层引号里写第三层引号**"的教训 ——
#   凡是要生成代码，就让内容待在它自己的文件里。
TXT = os.path.join(Build, "_py_lessons.txt")
文本 = io.open(TXT, encoding="utf-8").read()

块 = {}
当前 = None
行缓冲 = []
for 行 in 文本.split("\n"):
    if 行.startswith("@@@ "):
        if 当前:
            块[当前] = "\n".join(行缓冲)
        当前 = 行[4:].strip()
        行缓冲 = []
    else:
        行缓冲.append(行)
if 当前:
    块[当前] = "\n".join(行缓冲)

n = 0
for 名, 体 in 块.items():
    体 = 体.rstrip("\n") + "\n"
    if 名.startswith("sol/"):
        dst = os.path.join(SOL, 名[4:] + ".py")
    else:
        dst = os.path.join(S, 名 + ".py")
    io.open(dst, "w", encoding="utf-8", newline="\n").write(体)
    print("  %s  (%d 字符)" % (os.path.relpath(dst, LC), len(体)))
    n += 1
print("  共写入 %d 个文件" % n)
