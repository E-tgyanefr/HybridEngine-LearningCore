"""校验 `lessons/solutions/` 里每份参考答案**真的能通过**对应课的检查。

⚠ 为什么要单独一条：`lessons/solutions/` 是参考答案的**第二份拷贝**
  （课程文件里那份是给检查用的，这份是给 `答案 NN` 命令用的）。
  两份不一致时，学员用 `答案 NN` 看到一段代码、抄进去却过不了检查 ——
  **而两边单独跑都正常，只有对着比才看得出来。**

用法：
    python _build/_check_solutions.py          # 查全部
    python _build/_check_solutions.py 17 27    # 只查这几课

⚠ 它只动 `lessons/answers/`（gitignore 的临时区），跑完**恢复原样**。
"""
import io
import os
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOL = os.path.join(LC, "lessons", "solutions")
ANS = os.path.join(LC, "lessons", "answers")
env = dict(os.environ, PYTHONIOENCODING="utf-8")

指定 = sys.argv[1:]
课们 = sorted({f[:2] for f in os.listdir(SOL)
              if f.endswith(".py") and f[:2].isdigit()})
if 指定:
    课们 = [号 for 号 in 课们 if 号 in 指定]
if not 课们:
    print("  XX solutions 目录里没有课（或指定的课号不存在）")
    sys.exit(1)

print("  要查 %d 课：%s" % (len(课们), " ".join(课们)))
os.makedirs(ANS, exist_ok=True)
备份 = {}
好 = 坏 = 缺 = 0


def 找(D, 号):
    return [f for f in os.listdir(D) if f.startswith(号) and f.endswith(".py")]


try:
    for 号 in 课们:
        src = 找(SOL, 号)
        if not src:
            print("    ??  %s solutions 里没有" % 号)
            缺 += 1
            continue
        文 = io.open(os.path.join(SOL, src[0]), encoding="utf-8").read()
        # 去掉文件头的 docstring —— 它不是答案的一部分
        # （`答案 NN` 会原样打出来，那是给人看的说明）
        文 = re.sub(r'^\s*"""(?:.|\n)*?"""\s*', "", 文, count=1)

        # 先让课程程序生成模板，拿到**真实**的答题文件名
        # （不自己拼 —— 标题里的标点会让拼出来的名字和程序用的不一致）
        dst = 找(ANS, 号)
        if not dst:
            subprocess.run([sys.executable, "-m", "hybridengine_light", "检查", 号],
                           cwd=LC, env=env, capture_output=True, timeout=300)
            dst = 找(ANS, 号)
        if not dst:
            print("    ??  %s 建不出答题文件（**没验到**）" % 号)
            缺 += 1
            continue

        P = os.path.join(ANS, dst[0])
        if P not in 备份:
            备份[P] = io.open(P, encoding="utf-8").read() if os.path.isfile(P) else None
        io.open(P, "w", encoding="utf-8", newline="").write(文)

        # ⚠ 跑检查也要重试 —— 别的脚本会清 answers 目录，
        #   症状是「一条 ✓ 和 ✗ 都没有」（检查根本没跑），不是「没过」。
        out = ""
        for 轮 in range(4):
            r = subprocess.run([sys.executable, "-m", "hybridengine_light", "检查", 号],
                               cwd=LC, env=env, capture_output=True, timeout=600)
            out = (r.stdout + r.stderr).decode("utf-8", "replace")
            if out.count("✓") or out.count("✗"):
                break
            if 轮 < 3:
                os.makedirs(ANS, exist_ok=True)
                d2 = 找(ANS, 号)
                if d2:
                    P = os.path.join(ANS, d2[0])
                io.open(P, "w", encoding="utf-8", newline="").write(文)

        if "全部通过" in out:
            好 += 1
            过 = re.search(r"全部通过（\s*(\d+)\s*/\s*(\d+)\s*）", out)
            print("    OK  %s  %s" % (号, 过.group(0) if 过 else ""))
        else:
            坏 += 1
            m = re.search(r"通过\s*(\d+)\s*/\s*(\d+)", out)
            print("    XX  %s 只过 %s" % (号, m.group(0) if m else "拿不到结果"))
finally:
    for P, 原 in 备份.items():
        if 原 is None:
            continue
        io.open(P, "w", encoding="utf-8", newline="").write(原)

print()
print("  solutions 参考答案：全过 %d 课 · 没过 %d 课 · 没验到 %d 课"
      % (好, 坏, 缺))
sys.exit(0 if (坏 == 0 and 缺 == 0) else 1)
