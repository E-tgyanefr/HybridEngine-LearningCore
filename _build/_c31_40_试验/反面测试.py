# -*- coding: utf-8 -*-
"""反面测试：把「学生真会犯的错答案」喂给**与生成器同构**的检查逻辑，
看每一课到底抓不抓得住。

⚠ 每一段都照着 `_gen_c16.py` 对应的 `形_*` 函数翻译，必须一模一样：
    · 输出含序列：按顺序找这几行，允许中间夹别的输出
    · 代码包含：**先去掉注释**再找片段
    · 函数用例：printf("%d\\n", f(输入)) 逐行比（比较前 Trim + TrimEnd(',')）
    · 数组合用例(返回)：int __a0[] = {元素}; printf("%d\\n", f(__a0, 个数));
      ⚠ 生成器对「改数组」模式还会多打一行 "1,2,3," + 换行，这里只用到「返回」模式

⚠ 「拦住」= 错答案**没通过**这条检查。反过来（错答案通过了）就是**放过**。
"""
import io
import os
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
D = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode\_build\_c31_40_试验"
OUT = r"C:\lc_tmpx\neg"
os.makedirs(OUT, exist_ok=True)
骨架头 = '#include <stdio.h>\n\n/*学生代码*/\n\nint main(void) {\n'


def 去注释(s):
    o, i, 块 = [], 0, False
    while i < len(s):
        if not 块 and i + 1 < len(s) and s[i] == '/' and s[i + 1] == '/':
            while i < len(s) and s[i] != '\n':
                i += 1
            o.append('\n'); continue
        if not 块 and i + 1 < len(s) and s[i] == '/' and s[i + 1] == '*':
            块 = True; i += 2; continue
        if 块 and i + 1 < len(s) and s[i] == '*' and s[i + 1] == '/':
            块 = False; i += 2; continue
        if not 块:
            o.append(s[i])
        i += 1
    return "".join(o)


课程 = {}
cur = None
for f in ("draft_31_35.txt", "draft_36_40.txt"):
    for line in io.open(os.path.join(D, f), encoding="utf-8-sig").read().split("\n"):
        if line.startswith("@@@ "):
            cur = line[4:].strip()[:2]; 课程[cur] = []; continue
        if cur is None or "|" not in line:
            continue
        k, _, v = line.partition("|")
        if k == "检查":
            课程[cur].append(v)


def 编译运行(源文本, 名字):
    源 = os.path.join(OUT, 名字 + ".c")
    程序 = os.path.join(OUT, 名字 + ".exe")
    if os.path.exists(程序):
        os.remove(程序)
    io.open(源, "w", encoding="utf-8", newline="").write(源文本)
    r = subprocess.run(["gcc", "-std=c11", "-O1", "-Wall", "-o", 程序, 源],
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0 or not os.path.exists(程序):
        return None, [], (r.stderr or r.stdout)
    try:
        p = subprocess.run([程序], capture_output=True, text=True,
                           encoding="utf-8", errors="replace", timeout=8)
    except subprocess.TimeoutExpired:
        return True, ["<程序超时>"], ""
    return True, [x.strip() for x in (p.stdout or "").split("\n") if x.strip()], ""


def 判一条(号, i, 规, 错答案):
    片 = 规.split("|")
    形态, 参 = 片[1], 片[2:]
    if 形态 == "输出含序列":
        好, 行们, 错 = 编译运行(错答案, "n%s_%d" % (号, i))
        if 好 is None:
            return True, "编译不过"
        从, 全对 = 0, True
        for x in 参:
            找 = next((j for j in range(从, len(行们)) if 行们[j] == x.strip()), -1)
            if 找 < 0:
                return True, "输出里没有「%s」".replace("%s", x)
            从 = 找 + 1
        return False, "**错答案的输出居然全对上了**"
    if 形态 == "代码包含":
        段 = 参[0]
        if 段 not in 去注释(错答案):
            return True, "源码里没有「%s」" % 段
        return False, "源码里有「%s」→ 单靠这条拦不住" % 段
    if 形态 == "函数用例":
        名, 对 = 参[0], list(zip(参[1::2], 参[2::2]))
        打印 = "".join('    printf("%%d\\n", %s(%s));\n' % (名, 入) for 入, _ in 对)
        好, 行们, 错 = 编译运行(骨架头.replace("/*学生代码*/", 错答案) + 打印 + "    return 0;\n}\n",
                              "n%s_%df" % (号, i))
        if 好 is None:
            return True, "编译不过"
        实际 = [x.rstrip(",") for x in 行们]
        期望 = [期.strip() for _, 期 in 对]
        if 实际 != 期望:
            return True, "实际 %s ≠ 期望 %s" % (实际, 期望)
        return False, "**错答案居然全对了**"
    if 形态 == "数组合用例":
        参2 = 参[1:] if 参 and 参[0] in ("返回", "改数组") else 参
        名, 对 = 参2[0], list(zip(参2[1::2], 参2[2::2]))
        块, 期望列 = [], []
        for 数, 期 in 对:
            元素 = [x for x in 数.split(",") if x != ""]
            if not 元素:
                continue
            j = len(块)
            块.append("    int __a%d[] = {%s};\n" % (j, ", ".join(元素)))
            块.append('    printf("%%d\\n", %s(__a%d, %d));\n' % (名, j, len(元素)))
            期望列.append(期.strip())
        好, 行们, 错 = 编译运行(骨架头.replace("/*学生代码*/", 错答案) + "".join(块) + "    return 0;\n}\n",
                              "n%s_%da" % (号, i))
        if 好 is None:
            return True, "编译不过"
        if 行们 != 期望列:
            return True, "实际 %s ≠ 期望 %s" % (行们, 期望列)
        return False, "**错答案居然全对了**"
    return False, "（这个形态没在本脚本里实现）"


拦 = 放 = 0
for 号 in sorted(课程):
    路径 = os.path.join(D, "neg_%s.c" % 号)
    if not os.path.exists(路径):
        continue
    错答案 = io.open(路径, encoding="utf-8").read()
    print("=" * 72)
    print("第 %s 课  错答案 = %s" % (号, io.open(os.path.join(D, "neg_%s.说明" % 号),
                                                encoding="utf-8").read().strip()
                                     if os.path.exists(os.path.join(D, "neg_%s.说明" % 号)) else ""))
    for i, 规 in enumerate(课程[号], 1):
        抓住, 说明 = 判一条(号, i, 规, 错答案)
        拦 += 1 if 抓住 else 0
        放 += 0 if 抓住 else 1
        print("   %s 检查%d [%s] %s"
              % ("✓拦住" if 抓住 else "✗放过", i, 规.split("|")[1], 说明))
print("=" * 72)
print("共 %d 条检查：拦住 %d，放过 %d" % (拦 + 放, 拦, 放))
