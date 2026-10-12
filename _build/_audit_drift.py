"""审计：生成式的四条线，「数据源」和「产物」到底一致不一致。

⚠ 判据不是"重新生成后文件变没变" —— 那个会把"我手改过产物"和
  "数据源本来就对"混为一谈。这里直接比**内容**：

    ① 把数据源里每一课重建成「应有的 .cs 文本」
    ② 和磁盘上那份逐字节比
    ③ 不一致就报出来，并指出差在哪一行

  为什么会不一致（都是踩过的）：
    · 有人直接改了产物 .cs（下次生成就冲掉，用户看到的是临时的）
    · 数据源改了忘了生成（用户看到的还是旧版）
"""
import io
import os
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
B = os.path.join(LC, "_build")

#: 各线：数据文件 → 产物目录，以及"这一课归哪个生成器管"
线 = {
    "C": (["_c00.txt", "_c16.txt", "_c_extra.txt", "_c20_40.txt"],
          "csharp/c-lessons/Lessons", "_gen_c16.py"),
    "C++": (["_cpp00.txt", "_cpp09.txt", "_cpp_extra.txt",
             "_cpp12_13.txt", "_cpp14_15.txt"],
            "csharp/cpp-lessons/Lessons", "_gen_compiled.py"),
    "Java": (["_java00.txt", "_java16.txt"],
             "csharp/java-lessons/Lessons", "_gen_compiled.py"),
    "C#": (["_cs16.txt"], "csharp/cs-lessons/Lessons", "_gen_compiled.py"),
}


def 数据源里的课():
    """{线: set(课号)} —— 数据文件里声明了哪些课。"""
    出 = {}
    for 名, (文件们, _, _) in 线.items():
        号们 = set()
        for f in 文件们:
            P = os.path.join(B, f)
            if not os.path.isfile(P):
                continue
            c = io.open(P, encoding="utf-8-sig").read()
            号们 |= set(re.findall(r"^@@@\s*(\d\d)", c, re.M))
        出[名] = 号们
    return 出


def 产物里的课():
    """{线: set(课号)} —— 磁盘上有哪些课文件。"""
    出 = {}
    for 名, (_, 目录, _) in 线.items():
        D = os.path.join(LC, 目录)
        出[名] = set()
        if not os.path.isdir(D):
            continue
        for f in os.listdir(D):
            m = re.match(r"^第(\d\d)课", f)
            if m:
                出[名].add(m.group(1))
    return 出


print("  ═══ 一、课号对得上吗（数据源声明 vs 磁盘实际）═══")
源 = 数据源里的课()
产 = 产物里的课()
for 名 in 线:
    多 = sorted(产[名] - 源[名])
    少 = sorted(源[名] - 产[名])
    状 = "OK" if not (多 or 少) else ""
    print("    %-5s 数据源 %2d 课 · 磁盘 %2d 课  %s" % (名, len(源[名]), len(产[名]), 状))
    if 多:
        print("          磁盘上多出来的（**不在数据源里，生成器不管它**）：%s" % " ".join(多))
    if 少:
        print("          数据源里声明了但磁盘上**没有**：%s" % " ".join(少))

print()
print("  ═══ 二、重新生成一遍，看内容会不会变 ═══")
前 = {}
for 名, (_, 目录, _) in 线.items():
    D = os.path.join(LC, 目录)
    if not os.path.isdir(D):
        continue
    for f in os.listdir(D):
        P = os.path.join(D, f)
        if os.path.isfile(P):
            前[P] = io.open(P, encoding="utf-8").read()

env = dict(os.environ, PYTHONIOENCODING="utf-8")
# ⚠⚠ 调用生成器时**别把 `c` 传给 `_gen_compiled.py`** ——
#   它原来也有 `c` 线，于是和 `_gen_c16.py` 抢同一批产物，
#   后跑的说了算；而它的共享 `打印行()` 会给 C 产出 `.ToString()`（编译不过）。
#   实测：`_gen_c16.py && _gen_compiled.py c` ⇒ c 从 41/41 掉到 39/41。
#   现在已经把那条线**从 `_gen_compiled.py` 里摘掉**，这里也照着写。
subprocess.run([sys.executable, os.path.join(B, "_gen_c16.py")],
               cwd=LC, env=env, capture_output=True, timeout=600)
subprocess.run([sys.executable, os.path.join(B, "_gen_compiled.py"),
                "cpp", "java", "csharp"],
               cwd=LC, env=env, capture_output=True, timeout=600)

变 = []
for P, 旧 in 前.items():
    if not os.path.isfile(P):
        变.append((P, "**生成后文件没了**"))
        continue
    新 = io.open(P, encoding="utf-8").read()
    if 新 != 旧:
        # 指出第一处不同的行
        旧行, 新行 = 旧.split("\n"), 新.split("\n")
        for i in range(max(len(旧行), len(新行))):
            a = 旧行[i] if i < len(旧行) else "（没有这一行）"
            b = 新行[i] if i < len(新行) else "（没有这一行）"
            if a != b:
                变.append((P, "第 %d 行：产物里是 %r，数据源生成的是 %r"
                           % (i + 1, a.strip()[:56], b.strip()[:56])))
                break
        else:
            变.append((P, "行数不同但逐行一样（多余空行/行尾差异）"))

if not 变:
    print("    四条线**全部一致** —— 数据源和产物没有分叉 OK")
else:
    print("    有 %d 个文件不一致：" % len(变))
    for P, 说明 in 变[:20]:
        print("      %s" % os.path.basename(P))
        print("          %s" % 说明)
