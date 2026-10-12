# 把 draft 里的「参考答案」抽出来，用 gcc 真跑一遍，再核对每条 输出含序列/输出逐行 检查
import io, os, re, subprocess, sys, shutil
sys.stdout.reconfigure(encoding="utf-8", errors="replace")
D = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode\_build\_c31_40_试验"
OUT = r"C:\lc_tmpx\ref"
os.makedirs(OUT, exist_ok=True)

课程 = {}
cur = None
for f in ("draft_31_35.txt", "draft_36_40.txt"):
    txt = io.open(os.path.join(D, f), encoding="utf-8-sig").read()
    for line in txt.split("\n"):
        if line.startswith("@@@ "):
            cur = line[4:].strip(); 课程[cur] = {"答案": [], "检查": []}; continue
        if cur is None or "|" not in line: continue
        k, _, v = line.partition("|")
        if k == "答案": 课程[cur]["答案"].append(v)
        elif k == "检查": 课程[cur]["检查"].append(v)

坏 = 0
for 名 in sorted(课程):
    课 = 课程[名]
    src = os.path.join(OUT, 名[:2] + ".c")
    exe = os.path.join(OUT, 名[:2] + ".exe")
    io.open(src, "w", encoding="utf-8", newline="").write("\n".join(课["答案"]) + "\n")
    r = subprocess.run(["gcc", "-std=c11", "-O1", "-Wall", "-o", exe, src],
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0:
        print("XX %s 编译失败：\n%s" % (名, (r.stderr or r.stdout)[:800])); 坏 += 1; continue
    警 = (r.stderr or "").strip()
    p = subprocess.run([exe], capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=30)
    lines = [x.strip() for x in (p.stdout or "").split("\n") if x.strip()]
    print("=" * 70)
    print("OK %s  退出码=%d  输出 %d 行%s" % (名, p.returncode, len(lines),
          ("   ⚠ 有警告：" + 警.splitlines()[0][:90]) if 警 else ""))
    for l in lines: print("      |" + l)
    if p.returncode != 0:
        print("   XX 运行退出码非 0"); 坏 += 1
    for i, 规 in enumerate(课["检查"], 1):
        片 = 规.split("|")
        形态 = 片[1] if len(片) > 1 else "?"
        if 形态 not in ("输出含序列", "输出逐行"): continue
        要 = 片[2:]
        if 形态 == "输出逐行":
            ok = lines == [x.strip() for x in 要]
            哪 = "逐行" if ok else "行数/内容不符"
        else:
            从 = 0; ok = True; 哪 = ""
            for x in 要:
                找 = next((j for j in range(从, len(lines)) if lines[j] == x.strip()), -1)
                if 找 < 0: ok = False; 哪 = "找不到「%s」" % x; break
                从 = 找 + 1
        if not ok: 坏 += 1
        print("   %s 检查%d [%s] %s" % ("✓" if ok else "✗", i, 形态, "过" if ok else 哪))
print("=" * 70)
print("总失败项：", 坏)