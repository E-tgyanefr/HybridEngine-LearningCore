# 精确模拟 _gen_c16.py 的 形_函数用例 与 形_代码包含，用 gcc 真跑
import io, os, subprocess, sys
sys.stdout.reconfigure(encoding="utf-8", errors="replace")
D = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode\_build\_c31_40_试验"
OUT = r"C:\lc_tmpx\fncase"
os.makedirs(OUT, exist_ok=True)
骨架头 = '#include <stdio.h>\n\n/*学生代码*/\n\nint main(void) {\n'

课程 = {}
cur = None
for f in ("draft_31_35.txt", "draft_36_40.txt"):
    for line in io.open(os.path.join(D, f), encoding="utf-8-sig").read().split("\n"):
        if line.startswith("@@@ "):
            cur = line[4:].strip(); 课程[cur] = {"答案": [], "检查": []}; continue
        if cur is None or "|" not in line: continue
        k, _, v = line.partition("|")
        if k == "答案": 课程[cur]["答案"].append(v)
        elif k == "检查": 课程[cur]["检查"].append(v)

def 去注释(s):
    o = []; i = 0; 块 = False
    while i < len(s):
        if not 块 and i + 1 < len(s) and s[i] == '/' and s[i+1] == '/':
            while i < len(s) and s[i] != '\n': i += 1
            o.append('\n'); continue
        if not 块 and i + 1 < len(s) and s[i] == '/' and s[i+1] == '*': 块 = True; i += 2; continue
        if 块 and i + 1 < len(s) and s[i] == '*' and s[i+1] == '/': 块 = False; i += 2; continue
        if not 块: o.append(s[i])
        i += 1
    return "".join(o)

坏 = 0
for 名 in sorted(课程):
    课 = 课程[名]; 答案 = "\n".join(课["答案"])
    for i, 规 in enumerate(课["检查"], 1):
        片 = 规.split("|"); 形态 = 片[1]; 参 = 片[2:]
        if 形态 == "函数用例":
            名f = 参[0]; 对 = list(zip(参[1::2], 参[2::2]))
            打印 = "".join('    printf("%%d\\n", %s(%s));\n' % (名f, 入) for 入, _ in 对)
            程序 = 骨架头.replace("/*学生代码*/", 答案) + 打印 + "    return 0;\n}\n"
            src = os.path.join(OUT, "%s_%d.c" % (名[:2], i))
            io.open(src, "w", encoding="utf-8", newline="").write(程序)
            r = subprocess.run(["gcc", "-std=c11", "-O1", "-Wall", "-o", src[:-2] + ".exe", src],
                               capture_output=True, text=True, encoding="utf-8", errors="replace")
            if r.returncode != 0:
                print("XX %s 检查%d 编译失败:\n%s" % (名, i, (r.stderr or r.stdout)[:700])); 坏 += 1; continue
            p = subprocess.run([src[:-2] + ".exe"], capture_output=True, text=True,
                               encoding="utf-8", errors="replace", timeout=30)
            实际 = [x.strip().rstrip(",") for x in (p.stdout or "").split("\n") if x.strip()]
            期望 = [期.strip() for _, 期 in 对]
            ok = 实际 == 期望
            if not ok: 坏 += 1
            print("%s %s 检查%d [函数用例 %s] %s" % ("✓" if ok else "✗", 名, i, 名f,
                  "全对" if ok else "期望 %s 实际 %s" % (期望, 实际)))
        elif 形态 == "代码包含":
            段 = 参[0]; 去 = 去注释(答案)
            ok = 段 in 去
            if not ok: 坏 += 1
            print("%s %s 检查%d [代码包含] %r" % ("✓" if ok else "✗", 名, i, 段))
print("总失败项：", 坏)