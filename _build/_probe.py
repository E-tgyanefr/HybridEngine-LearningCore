# 内部工具（验证完删）：把某一课参考答案真跑一遍，打印它每个检查方法的实际输出。
# 用法：python _build\_probe.py java 19
#
# 为什么要它：检查里的「期望值」如果靠手算，很容易算错（第 19 课把插入排序
# 的中间过程数错了一次，白跑两轮）。这里直接把参考答案喂给运行器，
# 按 `标记|` 把真实输出原样打出来，照着它写期望值就不会错。
import io
import os
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
线表 = {
    "c": dict(工程="c-lessons", 答案="c-answers", 特性="课程"),
    "cpp": dict(工程="cpp-lessons", 答案="cpp-answers", 特性="课程"),
    "java": dict(工程="java-lessons", 答案="java-answers", 特性="Java课程"),
    "csharp": dict(工程="cs-lessons", 答案="cs-answers", 特性="课程"),
}
env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"

键, 编号 = sys.argv[1], sys.argv[2]
参 = 线表[键]
ANS = os.path.join(ROOT, 参["答案"])
# 让课程程序把答题文件生成出来（如果还没有）
subprocess.run(["dotnet", "run", "--project", "csharp/" + 参["工程"], "检查", 编号],
               cwd=ROOT, env=env, capture_output=True, timeout=300)
文件们 = [x for x in os.listdir(ANS) if x.startswith(编号)]
if not 文件们:
    print("没有答题文件")
    sys.exit(1)
p = os.path.join(ANS, 文件们[0])
代码 = io.open(p, encoding="utf-8").read()

# 把这段代码写成一个临时 .java，交给 javac / java 直接跑，
# 但需要骨架和调用点 —— 所以从课程 .cs 里把 骨架 / 调用点 抠出来。
课 = None
for f in os.listdir(os.path.join(ROOT, "csharp", 参["工程"], "Lessons")):
    t = io.open(os.path.join(ROOT, "csharp", 参["工程"], "Lessons", f), encoding="utf-8").read()
    if re.search(r'\[%s\("%s"' % (参["特性"], re.escape(编号)), t):
        课 = t
        break
if 课 is None:
    print("找不到课程文件")
    sys.exit(1)

骨 = re.search(r'骨架\s*(?:=>|=)\s*"""(.*?)""";', 课, re.S)
调 = re.search(r'调用点\s*(?:=>|=)\s*"""(.*?)""";', 课, re.S)
if not 骨:
    print("这一课没有自定义骨架 —— 用 JavaCheck.默认骨架，探针脚本请手工看一眼")
    sys.exit(1)
if not 调:
    print("没有找到 调用点")
    sys.exit(1)

程序 = 骨.group(1).replace("/*学生代码*/", 代码).replace("/*调用点*/", 调.group(1))
目录 = os.path.join(ROOT, "_build", "_probe_%s_%s" % (键, 编号))
os.makedirs(目录, exist_ok=True)
out = os.path.join(目录, "Main.java")
io.open(out, "w", encoding="utf-8", newline="").write(程序)
c = subprocess.run(["javac", "-encoding", "UTF-8", "-g", "-cp", ".", "Main.java"],
                   cwd=目录, capture_output=True, timeout=300)
if c.returncode != 0:
    print("── 编译没过 ──")
    print((c.stdout + c.stderr).decode("utf-8", "replace"))
    sys.exit(1)
r = subprocess.run(["java", "-Dstdout.encoding=UTF-8", "-Dstderr.encoding=UTF-8", "-cp", ".", "Main"],
                   cwd=目录, capture_output=True, timeout=300)
text = (r.stdout + r.stderr).decode("utf-8", "replace")
print("── %s %s 参考答案的真实输出 ──" % (键, 编号))
for line in text.splitlines():
    print("   " + line.rstrip())
