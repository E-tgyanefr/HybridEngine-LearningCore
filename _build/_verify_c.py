# 用「参考答案」跑全部 C 课的检查。
#
# ## 为什么必须用参考答案跑一遍
#   课程文件里的检查是我写的；参考答案也是我写的。
#   **两边都错（比如都以为该用 %s 打字符串）时检查会"通过"，而课程是错的。**
#   拿参考答案跑检查至少能证明两边对同一事实的理解一致，
#   并能抓出"检查写得过严/过松"（边界值、双引号、传值那几项就是这样抓出来的）。
#
# ## ⚠ 文件名必须用**真正的路径函数**，不能自己拼
#   我第一版按 `{编号}_{标题去掉非字母数字}.c` 拼文件名 ——
#   而 07 课的标题里有 `'A' 和 "A"`，清理后与真实文件名**不一致**
#   ⇒ 检查找不到文件、重新生成了模板，于是那一课"✓0 ✗0"（看着像没跑）。
#   现在改用 `CCourse.答案路径()` 本身（单一真值）——
#   这与我给 Python 侧定的规矩是同一条：**别猜路径，去问代码**。
import io
import os
import re
import shutil
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = r"D:\Users\etgya\Desktop\Project\LearnCode"
LESSONS = os.path.join(ROOT, "csharp", "c-lessons", "Lessons")
ANS = os.path.join(ROOT, "c-answers")
GEN = os.path.join(ROOT, "_build", "pathgen")

env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"
env["PYTHONIOENCODING"] = "utf-8"

# ── ① 写一个只打印真实路径的小程序（复用 CCourse，不猜）──────────────
os.makedirs(GEN, exist_ok=True)
io.open(os.path.join(GEN, "PathGen.csproj"), "w", encoding="utf-8", newline="\r\n").write(
    '''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
    <Nullable>disable</Nullable><ImplicitUsings>disable</ImplicitUsings>
    <AssemblyName>PathGen</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\\..\\csharp\\HybridEngineLight\\HybridEngineLight.csproj" />
  </ItemGroup>
</Project>
''')
io.open(os.path.join(GEN, "Program.cs"), "w", encoding="utf-8", newline="\r\n").write(
    '''using System;
using HybridEngineLight;
internal static class P
{
    private static void Main(string[] a)
    {
        // 参数：编号 标题  → 打印 CCourse.答案路径 的真实结果
        var def = new CourseDef(a[0], a[1], "");
        Console.WriteLine(CCourse.答案路径(@"F:\\_calc_\\", def));
    }
}
''')
r = subprocess.run(["dotnet", "build", os.path.join(GEN, "PathGen.csproj"), "-v", "q", "--nologo"],
                   cwd=ROOT, env=env, capture_output=True, timeout=300)
if r.returncode != 0:
    print("  XX PathGen 编译失败：")
    print((r.stdout + r.stderr).decode("utf-8", "replace")[-800:])
    sys.exit(1)
PG = os.path.join(GEN, "bin", "Debug", "net8.0", "PathGen.dll")

os.makedirs(ANS, exist_ok=True)
结果 = {}

for f in sorted(os.listdir(LESSONS)):
    if not f.endswith(".cs"):
        continue
    t = io.open(os.path.join(LESSONS, f), encoding="utf-8").read()
    m = re.search(r'\[课程\("(\d+)",\s*"((?:[^"\\]|\\.)*)"', t)
    ans = re.search(r'public static string 参考答案 => """\n(.*?)\n""";', t, re.S)
    if not (m and ans):
        print(f"  {f}: ✗ 解析不出编号/参考答案")
        continue
    编号, 标题原文 = m.group(1), m.group(2)
    # C# 源码里的转义要还原成实际字符
    标题 = 标题原文.replace('\\"', '"').replace("\\\\", "\\")

    r = subprocess.run(["dotnet", PG, 编号, 标题], cwd=ROOT, env=env,
                       capture_output=True, timeout=120)
    路径 = (r.stdout + r.stderr).decode("utf-8", "replace").strip().split("\n")[-1].strip()
    if not 路径:
        print(f"  {f}: ✗ 取不到路径")
        continue
    真实 = os.path.join(ANS, os.path.basename(路径.replace("\\", "/")))
    io.open(真实, "w", encoding="utf-8", newline="").write(ans.group(1) + "\n")

    r = subprocess.run(["dotnet", "run", "--project", "csharp/c-lessons", "--no-build",
                        "检查", 编号], cwd=ROOT, env=env, capture_output=True, timeout=300)
    out = (r.stdout + r.stderr).decode("utf-8", "replace")
    通过 = "全部通过" in out
    n过 = out.count("✓")
    n否 = out.count("✗") - (1 if "上面 ✗ 的部分" in out else 0)
    结果[f] = 通过
    print(f"  {'OK ' if 通过 else 'XX '} {编号}  {标题[:30]:32s} ✓{n过} ✗{max(0,n否)}")
    if not 通过:
        for line in out.splitlines():
            if line.strip().startswith("✗") or "应该是" in line or "错了" in line:
                print("         " + line.strip()[:106])

print("")
好 = sum(1 for v in 结果.values() if v)
print(f"  == {好}/{len(结果)} 课用参考答案跑通 ==")
shutil.rmtree(ANS, ignore_errors=True)
shutil.rmtree(GEN, ignore_errors=True)
print(f"  c-answers 已清理: {not os.path.exists(ANS)}")
sys.exit(0 if 结果 and 好 == len(结果) else 1)
