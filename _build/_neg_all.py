# 跑十课的反面测试，把结果写成一份摘要。
#   python _build/_neg_all.py
import io
import os
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DLL = os.path.join(os.environ["TEMP"], "cpl17", "bin", "Debug", "net8.0",
                   "HybridEngineLight.CppLessons.dll")
NEG = os.path.join(ROOT, "_build", "_neg")
OUT = os.path.join(ROOT, "_build", "_neg_out")
HARNESS = os.path.join(os.environ["TEMP"], "refl16")
env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"
os.makedirs(OUT, exist_ok=True)

摘要 = []
for n in ["16", "17", "18", "19", "20", "21", "22", "23", "24", "25"]:
    文件 = [f for f in sorted(os.listdir(NEG)) if f.startswith(n + "_")]
    if not 文件:
        摘要.append("第 %s 课：**没有反面用例**" % n)
        continue
    f = os.path.join(NEG, 文件[0])
    r = subprocess.run(["dotnet", "run", "--no-build", "--", "第%s课" % n, f, DLL],
                       cwd=HARNESS, env=env, capture_output=True, timeout=1800)
    out = (r.stdout + r.stderr).decode("utf-8", "replace")
    io.open(os.path.join(OUT, "neg%s.txt" % n), "w", encoding="utf-8").write(out)
    行 = out.splitlines()
    分 = [x.strip() for x in 行 if x.strip().startswith("== ")]
    不过 = [x.strip() for x in 行 if x.strip().startswith("XX ")]
    摘要.append("第 %s 课  反面用例 %s   %s" % (n, 文件[0], 分[0] if 分 else "（没跑到结果）"))
    if not 不过:
        摘要.append("        ⚠⚠ 一条都没拦住！")
    else:
        for x in 不过:
            摘要.append("        " + x)
    摘要.append("")

文本 = "\n".join(摘要)
io.open(os.path.join(OUT, "摘要.txt"), "w", encoding="utf-8").write(文本)
print(文本)
