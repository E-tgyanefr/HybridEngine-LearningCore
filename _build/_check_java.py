# 内部工具（验证完删）：构建 + 把 17~40 课的参考答案写进 java-answers + 逐课跑检查。
# 用法：python _build\_check_java.py            （全部 17~40）
#       python _build\_check_java.py 19 20      （只跑这几课）
import io
import os
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"

命令 = sys.argv[1:] or [str(n) for n in range(17, 41)]

b = subprocess.run(["dotnet", "build", "csharp/java-lessons/JavaLessons.csproj"],
                   cwd=ROOT, env=env, capture_output=True, timeout=600)
bout = (b.stdout + b.stderr).decode("utf-8", "replace")
if b.returncode != 0:
    print("构建失败：")
    for line in bout.splitlines():
        if "error" in line:
            print("   " + line.strip())
    sys.exit(1)

subprocess.run(["python", "_build/_answer.py", "java"] + 命令,
               cwd=ROOT, env=env, timeout=1800)

好, 坏 = [], []
for 编号 in 命令:
    r = subprocess.run(["dotnet", "run", "--project", "csharp/java-lessons", "检查", 编号],
                       cwd=ROOT, env=env, capture_output=True, timeout=1800)
    out = (r.stdout + r.stderr).decode("utf-8", "replace")
    过 = "全部通过" in out
    末 = ""
    for line in out.splitlines():
        s = line.strip()
        if s.startswith("全部通过") or s.startswith("通过 "):
            末 = s
    print("  %s %s  %s" % ("OK " if 过 else "XX ", 编号, 末))
    if 过:
        好.append(编号)
        continue
    坏.append(编号)
    # 把每条 ✗ 的**第一条**明细打出来（够定位了，全文太长）
    for i, line in enumerate(out.splitlines()):
        s = line.strip()
        if s.startswith("✗"):
            print("        " + s)
            细节 = 0
            for j in range(i + 1, min(i + 14, len(out.splitlines()))):
                t = out.splitlines()[j].strip()
                if t.startswith("✓") or t.startswith("✗"):
                    break
                if t and 细节 < 4:
                    print("            " + t[:150])
                    细节 += 1

print("\n  通过 %d/%d" % (len(好), len(命令)))
if 坏:
    print("  还要改：" + " ".join(坏))
sys.exit(0 if not 坏 else 1)
