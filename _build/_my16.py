# 把 cpp-lessons/Lessons 里「我的课」同步到隔离测试工程，重建，再跑指定几课的检查。
#   python _build/_my16.py            # 只重建
#   python _build/_my16.py 19 20 21   # 重建并跑这几课
import os
import re
import shutil
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "csharp", "cpp-lessons", "Lessons")
T = os.path.join(os.environ["TEMP"], "cpl17")
L = os.path.join(T, "Lessons")
env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"
# ⚠⚠ 把「学生代码的编译临时目录」指到一个**私有**的 TEMP 里。
#   为什么必须这么做（实测踩到的大坑）：
#     `CppRunner.清理旧目录` 会把**别的进程**留下的 run* 目录删掉，
#     而这台机器上同时有好几个助手在跑各自的课程检查 ——
#     于是我刚建出来的编译目录会被**别人的**检查顺手删掉，
#     g++ 报 `cc1plus: fatal error: main.cpp: No such file or directory`。
#     实测：在公共 TEMP 下建一个目录，1.6 秒后就没了。
#   ⇒ 每次检查用一个自己的 TEMP，互不打扰。
私有TEMP = r"C:\lc_tmpx"
os.makedirs(私有TEMP, exist_ok=True)
if os.path.isdir(私有TEMP):
    env["TEMP"] = 私有TEMP
    env["TMP"] = 私有TEMP
DLL = os.path.join(T, "bin", "Debug", "net8.0", "HybridEngineLight.CppLessons.dll")
HARNESS = os.path.join(os.environ["TEMP"], "refl16")

# 别人正在写的课（当前编译不过）—— 跳过它们是**临时**办法，等他们写完就不用了。
# 只同步编号不超过 MAX 的课（我的课是 16~25；26~32 是别人已经写完的参考）。
MAX = int(os.environ.get("MAX_LESSON") or "32")

for f in os.listdir(L):                       # 先把旧副本清掉，免得留下编译不过的残骸
    os.remove(os.path.join(L, f))
n = 0
for f in sorted(os.listdir(SRC)):
    m = re.match(r"^第(\d\d)课", f)
    if not f.endswith(".cs") or not m:
        continue
    if int(m.group(1)) > MAX:
        continue
    shutil.copy2(os.path.join(SRC, f), os.path.join(L, f))
    n += 1
shutil.copy2(os.path.join(ROOT, "csharp", "cpp-lessons", "Program.cs"), T)
print("同步了 %d 个课程文件" % n)

r = subprocess.run(["dotnet", "build"], cwd=T, env=env, capture_output=True, timeout=600)
out = (r.stdout + r.stderr).decode("utf-8", "replace")
if "Build succeeded" not in out:
    print(out[-4000:])
    sys.exit(1)
print("构建通过")

for 编号 in sys.argv[1:]:
    # ⚠⚠ 每次跑之前把 harness 里那份**旧副本**删掉。
    #   反射 harness 自己也引用了同一个程序集，构建时会把它复制进自己的 bin ——
    #   于是 Assembly.LoadFrom(新路径) 仍然会拿到**已经加载过的旧副本**，
    #   我因此白调了半小时：改了代码、重建了、跑出来还是旧行为。
    hb = os.path.join(HARNESS, "bin", "Debug", "net8.0")
    for f in ("HybridEngineLight.CppLessons.dll", "HybridEngineLight.CppLessons.pdb"):
        p = os.path.join(hb, f)
        if os.path.exists(p):
            os.remove(p)
    r = subprocess.run(["dotnet", "run", "--no-build", "--", "第%s课" % 编号, "-", DLL],
                       cwd=HARNESS, env=env, capture_output=True, timeout=900)
    print("=" * 60)
    print("第 %s 课" % 编号)
    print((r.stdout + r.stderr).decode("utf-8", "replace"))
